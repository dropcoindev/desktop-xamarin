﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TurtleWallet
{
    public partial class CreateWalletPrompt : Form
    {
        public string WalletPath
        {
            get;
            set;
        }

        public string WalletPassword
        {
            get;
            set;
        }

        public CreateWalletPrompt()
        {
            InitializeComponent();
            this.Text = "Turtle Wallet";
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            Utilities.CloseProgram(e);
        }

        private void CreateWalletButton_MouseEnter(object sender, EventArgs e)
        {
            var backcolor = Color.FromArgb(44, 44, 44);
            var forcolor = Color.FromArgb(39, 170, 107);
            var currentButton = (Label)sender;
            currentButton.BackColor = backcolor;
            currentButton.ForeColor = forcolor;
        }

        private void CreateWalletButton_MouseLeave(object sender, EventArgs e)
        {
            var backcolor = Color.FromArgb(52, 52, 52);
            var forcolor = Color.FromArgb(224, 224, 224);
            var currentButton = (Label)sender;
            currentButton.BackColor = backcolor;
            currentButton.ForeColor = forcolor;
        }

        private void CancelButton_MouseEnter(object sender, EventArgs e)
        {
            var backcolor = Color.FromArgb(44, 44, 44);
            var forcolor = Color.FromArgb(39, 170, 107);
            var currentButton = (Label)sender;
            currentButton.BackColor = backcolor;
            currentButton.ForeColor = forcolor;
        }

        private void CancelButton_MouseLeave(object sender, EventArgs e)
        {
            var backcolor = Color.FromArgb(52, 52, 52);
            var forcolor = Color.FromArgb(224, 224, 224);
            var currentButton = (Label)sender;
            currentButton.BackColor = backcolor;
            currentButton.ForeColor = forcolor;
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = MessageBox.Show("Are you sure you want to cancel your Turtle Wallet creation?", "Cancel wallet creation?", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                Utilities.SetAppClosing(false);
                this.DialogResult = DialogResult.Cancel;
                Utilities.Close(this);
            }
        }

        private void CreateWalletButton_Click(object sender, EventArgs e)
        {
            CreateWallet();
        }

        private void WalletNameText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                CreateWallet();
            }
        }

        private void CreateWallet()
        {
            var curDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var _walletFile = System.IO.Path.Combine(curDir, walletNameText.Text + ".wallet");

            if (walletNameText.Text == "")
            {
                MessageBox.Show("Please enter a valid wallet name", "Turtle Wallet Creation");
                return;
            }
            else if (walletNameText.Text.Any(c => !Char.IsLetterOrDigit(c)))
            {
                MessageBox.Show("Wallet name cannot contain special characters", "Turtle Wallet Creation");
                return;
            }
            else if (System.IO.File.Exists(_walletFile))
            {
                MessageBox.Show("A wallet with that name already exists! Choose a different name or choose the \"Select Existing Wallet\" option instead.", "Turtle Wallet Creation");
                return;
            }

            if (passwordText.Text == "")
            {
                MessageBox.Show("Please enter a valid password", "Turtle Wallet Creation");
                return;
            }
            else if (passwordText.Text.Length < 6)
            {
                MessageBox.Show("Please enter a password that is larger than 6 characters", "Turtle Wallet Creation");
                return;
            }

            if (passwordText.Text != passwordConfirmText.Text)
            {
                MessageBox.Show("Passwords do not match", "Turtle Wallet Creation");
                return;
            }

            var walletdexe = System.IO.Path.Combine(curDir, "walletd.exe");

            if (IsRunningOnMono())
            {
                walletdexe = System.IO.Path.Combine(curDir, "walletd");
            }

            if (!System.IO.File.Exists(walletdexe))
            {
                MessageBox.Show("The 'walletd' daemon is missing from the folder the wallet is currently running from! Please place 'walletd' next to your wallet exe and run again!", "Turtle Wallet Creation", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.DialogResult = DialogResult.Abort;
                Utilities.Close(this);
            }

            createProgressbar.Visible = true;
            StringBuilder tmpstdout = new StringBuilder();
            try
            {
                Process p = new Process();
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                p.StartInfo.FileName = walletdexe;
                p.StartInfo.Arguments = CLIEncoder.Encode(new string[] {"-w", _walletFile, "-p", passwordText.Text, "-g"});
                p.OutputDataReceived += (sender, args) => tmpstdout.AppendLine(args.Data);
                p.Start();
                p.BeginOutputReadLine();
                p.WaitForExit(10000);
                p.CancelOutputRead();

                if (!System.IO.File.Exists(_walletFile))
                {
                    MessageBox.Show("Wallet failed to create after communicating with daemon. Please reinstall the wallet, close any other wallets you may have open, and try again", "Turtle Wallet Creation", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.DialogResult = DialogResult.Abort;
                    Utilities.Close(this);
                }
                else
                {
                    WalletPath = _walletFile;
                    WalletPassword = passwordText.Text;
                    MessageBox.Show("Wallet successfully created at: " + Environment.NewLine + _walletFile + Environment.NewLine + "IMPORTANT:" + Environment.NewLine + "As soon as the main GUI to the wallet opens, you should proceed to the 'BACKUP KEYS' tab to save your secret and view key in case of wallet failure in the future!", "Turtle Wallet Creation", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Utilities.SetAppClosing(false);
                    this.DialogResult = DialogResult.OK;
                    Utilities.Close(this);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("An exception occured while attempting to create the wallet." + Environment.NewLine + "Error:" + Environment.NewLine + ex.Message, "Turtle Wallet Creation", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.DialogResult = DialogResult.Abort;
                Utilities.Close(this);
            }
        }

        public static bool IsRunningOnMono()
        {
            return Type.GetType("Mono.Runtime") != null;
        }

        private void PasswordText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                CreateWallet();
            }
        }

        private void PasswordConfirmText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                CreateWallet();
            }
        }
    }
}
