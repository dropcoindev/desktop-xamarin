﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TurtleWallet
{
    public partial class wallet : Form
    {
        public static System.Windows.Forms.Timer statsTimer = new System.Windows.Forms.Timer();
        public static int watchdogTimeout = 3;
        public static int watchdogMaxTry = 3;
        public static int currentTimeout = 0;
        public static int currentTry = 0;
        public static int staticFee = 10;
        public static Label _selectedTab;
        public static List<string> cachedTrx = new List<string>();
        public static List<ListViewItem> firstRunTrx = new List<ListViewItem>();
        public static Process runningDaemon;
        public static WindowLogger windowLogger;
        public static int globalRefreshCount = 0;

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

        //List view header formatters
        public static void ColorListViewHeader(ref ListView list, Color backColor, Color foreColor)
        {
            list.OwnerDraw = true;
            list.DrawColumnHeader +=
                new DrawListViewColumnHeaderEventHandler
                (
                    (sender, e) => HeaderDraw(sender, e, backColor, foreColor)
                );
            list.DrawItem += new DrawListViewItemEventHandler(BodyDraw);
            list.DrawSubItem += List_DrawSubItem;
        }

        private static void List_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            e.DrawDefault = true;
        }

        private static void HeaderDraw(object sender, DrawListViewColumnHeaderEventArgs e, Color backColor, Color foreColor)
        {
            e.Graphics.FillRectangle(new SolidBrush(backColor), e.Bounds);
            e.Graphics.DrawRectangle(SystemPens.GradientInactiveCaption,
        new Rectangle(e.Bounds.X, 0, e.Bounds.Width, e.Bounds.Height));
            e.Graphics.DrawString(e.Header.Text, e.Font, new SolidBrush(foreColor), e.Bounds);
        }
        private static void BodyDraw(object sender, DrawListViewItemEventArgs e)
        {
            e.DrawDefault = true;
        }

        public wallet(string _wallet, string _pass, System.Diagnostics.Process wd)
        {
            InitializeComponent();
            runningDaemon = wd;
            wallet.ColorListViewHeader(ref txList, Color.FromArgb(29, 29, 29), Color.FromArgb(187, 186, 185));
            _selectedTab = homeButton;
            WalletPath = _wallet;
            WalletPassword = _pass;
            Properties.Settings.Default.walletPath = _wallet;
            Properties.Settings.Default.hasWallet = true;
            Properties.Settings.Default.Save();
            //feeAmountText.Text = Properties.Settings.Default.defaultFee.ToString();
            //feeAmountText.Enabled = false;
            windowLogger = new WindowLogger();
            walletTabControl.SelectedIndex = 0;
            feeComboBox.SelectedIndex = 0;
        }

        private void Wallet_Load(object sender, EventArgs e)
        {
            versionLabel.Text = "v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            windowLogger.Log(LogTextbox, "TurtleCoin Wallet " + "v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version + " has started ...");
            new Thread(new ThreadStart(Update_live_stats)).Start();
            windowLogger.Log(LogTextbox, "Live Stats Update thread started ...");
            statsTimer.Interval = 30000;
            statsTimer.Tick += StatsTimer_Tick;
            statsTimer.Start();
            windowLogger.Log(LogTextbox, "Live Stats Update timer started ...");
            resyncer.RunWorkerAsync();
            windowLogger.Log(LogTextbox, "Network Sync Thread started ...");
        }

        private void StatsTimer_Tick(object sender, EventArgs e)
        {
            this.updateLabel.BeginInvoke((MethodInvoker)delegate ()
            {
                updateLabel.Text = "Updating LiveStats ...";
                updateLabel.ForeColor = Color.FromArgb(255, 128, 0);
            });
            new Thread(new ThreadStart(Update_live_stats)).Start();
        }

        private void Update_live_stats()
        {
            try
            {
                var jobj = ConnectionManager.Get_live_stats();
                if(jobj.Item1 == false)
                {
                    this.heightAmountLabel.BeginInvoke((MethodInvoker)delegate () { heightAmountLabel.Text = "N/A"; });
                    this.difficultyAmountLabel.BeginInvoke((MethodInvoker)delegate () { difficultyAmountLabel.Text = "N/A"; });
                    this.updateLabel.BeginInvoke((MethodInvoker)delegate ()
                    {
                        updateLabel.Text = "LiveStats update failed ...";
                        updateLabel.ForeColor = Color.FromArgb(205, 12, 47);
                    });
                    System.Threading.Thread.Sleep(2000);
                    this.updateLabel.BeginInvoke((MethodInvoker)delegate ()
                    {
                        updateLabel.Text = "Wallet Idle ...";
                        updateLabel.ForeColor = Color.Gray;
                    });
                    return;
                }
                var stats = (Newtonsoft.Json.Linq.JObject)jobj.Item2["network"];
                if(stats.ContainsKey("difficulty"))
                    this.difficultyAmountLabel.BeginInvoke((MethodInvoker)delegate () { difficultyAmountLabel.Text = stats["difficulty"].ToString(); });
                else
                {
                    this.difficultyAmountLabel.BeginInvoke((MethodInvoker)delegate () { difficultyAmountLabel.Text = "N/A"; });
                }
                if (stats.ContainsKey("height"))
                    this.heightAmountLabel.BeginInvoke((MethodInvoker)delegate () { heightAmountLabel.Text = stats["height"].ToString(); });
                else
                {
                    this.heightAmountLabel.BeginInvoke((MethodInvoker)delegate () { heightAmountLabel.Text = "N/A"; });
                }
                this.updateLabel.BeginInvoke((MethodInvoker)delegate ()
                {
                    updateLabel.Text = "Updated livestats ...";
                    updateLabel.ForeColor = Color.FromArgb(0, 192, 0);
                });
                System.Threading.Thread.Sleep(2000);
                this.updateLabel.BeginInvoke((MethodInvoker)delegate ()
                {
                    updateLabel.Text = "Wallet Idle ...";
                    updateLabel.ForeColor = Color.Gray;
                });
            }
            catch (Exception)
            {
                try
                {
                    this.heightAmountLabel.BeginInvoke((MethodInvoker)delegate () { heightAmountLabel.Text = "N/A"; });
                    this.difficultyAmountLabel.BeginInvoke((MethodInvoker)delegate () { difficultyAmountLabel.Text = "N/A"; });
                    this.updateLabel.BeginInvoke((MethodInvoker)delegate ()
                    {
                        updateLabel.Text = "Livestats update failed ...";
                        updateLabel.ForeColor = Color.FromArgb(205, 12, 47);
                    });
                    windowLogger.Log(LogTextbox, "Livestats update failed ...");
                    System.Threading.Thread.Sleep(2000);
                    this.updateLabel.BeginInvoke((MethodInvoker)delegate ()
                    {
                        updateLabel.Text = "Wallet Idle ...";
                        updateLabel.ForeColor = Color.Gray;
                    });
                }
                catch
                {
                    //empty catch when application exits
                }
            }
        }

        private void Refresh_ui()
        {
            globalRefreshCount++;
            Newtonsoft.Json.Linq.JObject status = null;
            Newtonsoft.Json.Linq.JToken blocks = null;
            try
            {
                this.updateLabel.BeginInvoke((MethodInvoker)delegate ()
                {
                    updateLabel.Text = "Syncing Network ...";
                    updateLabel.ForeColor = Color.FromArgb(255, 128, 0);
                });
                var balances = ConnectionManager.Request("getBalance");
                if (balances.Item1 == false)
                {
                    throw new Exception("getBalance call failed: " + balances.Item2);
                }
                float availableBal = (float)(balances.Item3["availableBalance"]) / 100;
                float lockedBal = (float)(balances.Item3["lockedAmount"]) / 100;
                this.availableAmountLabel.BeginInvoke((MethodInvoker)delegate () { this.availableAmountLabel.Text = availableBal.ToString("0.00") + " TRTL"; });
                this.lockedAmountLabel.BeginInvoke((MethodInvoker)delegate () { this.lockedAmountLabel.Text = lockedBal.ToString("0.00") + " TRTL"; });

                var Addresses = ConnectionManager.Request("getAddresses");
                if (Addresses.Item1 == false)
                {
                    throw new Exception("getAddresses call failed: " + balances.Item2);
                }
                var addressList = Addresses.Item3["addresses"];
                this.myAddressText.BeginInvoke((MethodInvoker)delegate () { myAddressText.Text = addressList[0].ToString(); });

                status = ConnectionManager.Request("getStatus").Item3;
                var parameters = new Dictionary<string, object>()
                {
                    { "blockCount",  (int)status["blockCount"] },
                    { "firstBlockIndex", 1 },
                    { "addresses", addressList }
                };
                var blocksRet = ConnectionManager.Request("getTransactions", parameters);
                if (blocksRet.Item1 == false)
                {
                    throw new Exception("getTransactions call failed: " + balances.Item2);
                }
                blocks = blocksRet.Item3["items"];
                currentTimeout = 0;
                currentTry = 0;
            }
            catch (Exception)
            {
                this.updateLabel.BeginInvoke((MethodInvoker)delegate ()
                {
                    updateLabel.Text = "Daemon error, retrying ...";
                    windowLogger.Log(LogTextbox, "Daemon error, retrying ...");
                    updateLabel.ForeColor = Color.FromArgb(205, 12, 47);
                });
                if (currentTimeout >= watchdogTimeout)
                {
                    if (currentTry <= watchdogMaxTry)
                    {
                        //daemon restart
                    }
                    else
                    {
                        MessageBox.Show("Turtle Wallet has tried numerous times to relaunch the needed daemon and has failed. Please relaunch the wallet!", "Walletd daemon could not be recovered!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        windowLogger.Log(LogTextbox, "Turtle Wallet has tried numerous times to relaunch the needed daemon and has failed. Please relaunch the wallet!");
                        this.Close();
                    }
                }
                else
                    currentTimeout++;
                return;
            }
            if (blocks == null)
                return;
            bool _trxFound = false;
            foreach(var block in blocks.Reverse())
            {
                var bblock = (Newtonsoft.Json.Linq.JObject)block;
                if(bblock.ContainsKey("transactions"))
                {
                    foreach(var transaction in block["transactions"])
                    {
                        if (cachedTrx.Contains(transaction["transactionHash"].ToString()))
                            continue;
                        string address = "";
                        long desired_transfer_amount = 0;
                        if ((long)transaction["amount"] < 0)
                        {
                            desired_transfer_amount = ((long)transaction["amount"] + (long)transaction["fee"]) * -1;
                        }
                        else
                        {
                            desired_transfer_amount = ((long)transaction["amount"]);
                        }

                        foreach(var transfer in transaction["transfers"])
                        {
                            if((long)transfer["amount"] == desired_transfer_amount)
                            {
                                address = transfer["address"].ToString();
                            }
                        }

                        List<ListViewItem.ListViewSubItem> subitems = new List<ListViewItem.ListViewSubItem>();

                        if((long)transaction["unlockTime"] == 0 || (long)transaction["unlockTime"] <= (long)status["blockCount"] - 40)
                        {
                            var confirmItem = new System.Windows.Forms.ListViewItem.ListViewSubItem(null, "  🐢  ✔", System.Drawing.Color.Green, System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(29)))), ((int)(((byte)(29))))), new System.Drawing.Font("Segoe UI Semibold", 13.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0))));
                            subitems.Add(confirmItem);
                        }
                        else
                        {
                            var confirmItem = new System.Windows.Forms.ListViewItem.ListViewSubItem(null, "  🐢  ✘", System.Drawing.Color.DarkRed, System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(29)))), ((int)(((byte)(29))))), new System.Drawing.Font("Segoe UI Semibold", 13.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0))));
                            subitems.Add(confirmItem);
                        }

                        if((long)transaction["amount"] > 0)
                        {
                            var directionItem = new System.Windows.Forms.ListViewItem.ListViewSubItem(null, "IN\u2007\u2007⇚\u2007\u2007\u2007", System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(0))))), System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(29)))), ((int)(((byte)(29))))), new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0))));
                            subitems.Add(directionItem);
                        }
                        else
                        {
                            var directionItem = new System.Windows.Forms.ListViewItem.ListViewSubItem(null, "OUT ⇛\u2007\u2007\u2007", System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(0))))), System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(29)))), ((int)(((byte)(29))))), new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0))));
                            subitems.Add(directionItem);
                        }

                        var amountItem = new System.Windows.Forms.ListViewItem.ListViewSubItem(null, ((long)(transaction["amount"]) / 100).ToString("0.00"), System.Drawing.Color.White, System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(29)))), ((int)(((byte)(29))))), new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0))));
                        subitems.Add(amountItem);

                        System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
                        dtDateTime = dtDateTime.AddSeconds((long)(transaction["timestamp"])).ToLocalTime();
                        var ts = dtDateTime.ToString("yyyy/MM/dd HH:mm:ss tt");
                        var dateItem = new System.Windows.Forms.ListViewItem.ListViewSubItem(null, ts, System.Drawing.Color.White, System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(29)))), ((int)(((byte)(29))))), new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0))));
                        subitems.Add(dateItem);

                        var addItem = new System.Windows.Forms.ListViewItem.ListViewSubItem(null, address, System.Drawing.Color.White, System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(29)))), ((int)(((byte)(29))))), new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0))));
                        subitems.Add(addItem);

                        System.Windows.Forms.ListViewItem trxItem = new System.Windows.Forms.ListViewItem(subitems.ToArray(), -1)
                        {
                            UseItemStyleForSubItems = false
                        };
                        if (globalRefreshCount > 1)
                            txList.BeginInvoke((MethodInvoker)delegate ()
                            {
                                txList.Items.Insert(0, trxItem);
                            });
                        else
                            firstRunTrx.Add(trxItem);
                                //txList.Items.Add(trxItem);
                        cachedTrx.Add(transaction["transactionHash"].ToString());
                        _trxFound = true;
                        windowLogger.Log(LogTextbox, "Found transaction " + transaction["transactionHash"].ToString() + ". Added to list ...");
                    }
                }
            }

            if (globalRefreshCount == 1)
            {
                txList.BeginInvoke((MethodInvoker)delegate ()
                {
                    txList.Items.AddRange(firstRunTrx.ToArray());
                });
            }
            if (_trxFound)
            {
                foreach (ColumnHeader column in txList.Columns)
                {
                    column.Width = -2;
                }
            }
            string titleUpdate = "TurtleCoin Wallet - Network Sync [" + status["blockCount"].ToString() + " / " + status["knownBlockCount"].ToString() + "] | Peers: " + status["peerCount"].ToString() + " | Updated: " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss tt");
            this.BeginInvoke((MethodInvoker)delegate ()
            {
                this.Text = titleUpdate;
                this.Update();
            });

        }

        private void HomeButton_MouseEnter(object sender, EventArgs e)
        {
            var currentButton = (Label)sender;
            if (_selectedTab != currentButton)
            {
                var backcolor = Color.FromArgb(44, 44, 44);
                var forcolor = Color.FromArgb(39, 170, 107);
                currentButton.BackColor = backcolor;
                currentButton.ForeColor = forcolor;
            }
        }

        private void SendButton_MouseEnter(object sender, EventArgs e)
        {
            var currentButton = (Label)sender;
            if (_selectedTab != currentButton)
            {
                var backcolor = Color.FromArgb(44, 44, 44);
                var forcolor = Color.FromArgb(39, 170, 107);
                currentButton.BackColor = backcolor;
                currentButton.ForeColor = forcolor;
            }
        }

        private void LogButton_MouseEnter(object sender, EventArgs e)
        {
            var currentButton = (Label)sender;
            if (_selectedTab != currentButton)
            {
                var backcolor = Color.FromArgb(44, 44, 44);
                var forcolor = Color.FromArgb(39, 170, 107);
                currentButton.BackColor = backcolor;
                currentButton.ForeColor = forcolor;
            }
        }

        private void RpcButton_MouseEnter(object sender, EventArgs e)
        {
            var currentButton = (Label)sender;
            if (_selectedTab != currentButton)
            {
                var backcolor = Color.FromArgb(44, 44, 44);
                var forcolor = Color.FromArgb(39, 170, 107);
                currentButton.BackColor = backcolor;
                currentButton.ForeColor = forcolor;
            }
        }

        private void HomeButton_MouseLeave(object sender, EventArgs e)
        {
            var currentButton = (Label)sender;
            if (_selectedTab != currentButton)
            {
                var backcolor = Color.FromArgb(52, 52, 52);
                var forcolor = Color.FromArgb(224, 224, 224);
                currentButton.BackColor = backcolor;
                currentButton.ForeColor = forcolor;
            }
        }

        private void SendButton_MouseLeave(object sender, EventArgs e)
        {
            var currentButton = (Label)sender;
            if (_selectedTab != currentButton)
            {
                var backcolor = Color.FromArgb(52, 52, 52);
                var forcolor = Color.FromArgb(224, 224, 224);
                currentButton.BackColor = backcolor;
                currentButton.ForeColor = forcolor;
            }
        }

        private void LogButton_MouseLeave(object sender, EventArgs e)
        {
            var currentButton = (Label)sender;
            if (_selectedTab != currentButton)
            {
                var backcolor = Color.FromArgb(52, 52, 52);
                var forcolor = Color.FromArgb(224, 224, 224);
                currentButton.BackColor = backcolor;
                currentButton.ForeColor = forcolor;
            }
        }

        private void RpcButton_MouseLeave(object sender, EventArgs e)
        {
            var currentButton = (Label)sender;
            if (_selectedTab != currentButton)
            {
                var backcolor = Color.FromArgb(52, 52, 52);
                var forcolor = Color.FromArgb(224, 224, 224);
                currentButton.BackColor = backcolor;
                currentButton.ForeColor = forcolor;
            }
        }

        private void CopyAddressButton_MouseEnter(object sender, EventArgs e)
        {
            var currentButton = (Label)sender;
            var backcolor = Color.FromArgb(44, 44, 44);
            var forcolor = Color.FromArgb(39, 170, 107);
            currentButton.BackColor = backcolor;
            currentButton.ForeColor = forcolor;
        }

        private void CopyAddressButton_MouseLeave(object sender, EventArgs e)
        {
            var currentButton = (Label)sender;
            var backcolor = Color.FromArgb(52,52,52);
            var forcolor = Color.FromArgb(224, 224, 224);
            currentButton.BackColor = backcolor;
            currentButton.ForeColor = forcolor;
        }

        private void HomeButton_Click(object sender, EventArgs e)
        {
            var currentButton = (Label)sender;
            if (_selectedTab != currentButton)
            {
                viewKeyText.Text = "";
                spendKeyText.Text = "";
                backupPasswordText.Text = "";
                var backcolor = Color.FromArgb(52, 52, 52);
                var forcolor = Color.FromArgb(224, 224, 224);
                _selectedTab.BackColor = backcolor;
                _selectedTab.ForeColor = forcolor;

                walletTabControl.SelectedIndex = 0;
                _selectedTab = currentButton;

                backcolor = Color.FromArgb(82, 82, 82);
                forcolor = Color.FromArgb(39, 170, 107);
                _selectedTab.BackColor = backcolor;
                _selectedTab.ForeColor = forcolor;
            }
        }

        private void SendButton_Click(object sender, EventArgs e)
        {
            var currentButton = (Label)sender;
            if (_selectedTab != currentButton)
            {
                viewKeyText.Text = "";
                spendKeyText.Text = "";
                backupPasswordText.Text = "";
                var backcolor = Color.FromArgb(52, 52, 52);
                var forcolor = Color.FromArgb(224, 224, 224);
                _selectedTab.BackColor = backcolor;
                _selectedTab.ForeColor = forcolor;

                walletTabControl.SelectedIndex = 1;
                _selectedTab = currentButton;

                backcolor = Color.FromArgb(82,82,82);
                forcolor = Color.FromArgb(39, 170, 107);
                _selectedTab.BackColor = backcolor;
                _selectedTab.ForeColor = forcolor;
            }
        }

        private void ListView1_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            e.Graphics.FillRectangle(Brushes.Blue, e.Bounds);
            e.DrawText();
        }

        private void ListView1_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            e.DrawDefault = true;
        }

        private void CopyAddressButton_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(myAddressText.Text);
            MessageBox.Show("Address copied to clipboard!", "TurtleCoin Wallet");
        }

        private void Resyncer_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                while (true)
                {
                    Refresh_ui();
                    System.Threading.Thread.Sleep(5000);
                    this.updateLabel.BeginInvoke((MethodInvoker)delegate ()
                    {
                        updateLabel.Text = "Wallet Idle ...";
                        updateLabel.ForeColor = Color.Gray;
                    });
                }
            }
            catch
            {
                //Empty catch for when the app exits, and the thread didnt have time to shut down.
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            updateLabel.Text = "Saving wallet, Please wait..";
            saverWorker.RunWorkerAsync();
            MessageBox.Show("Saving Wallet, Please wait...", "TurtleCoin Wallet");
            e.Cancel = true;
        }

        private void SendTrtlButton_Click(object sender, EventArgs e)
        {
            string sendAddr = recipientAddressText.Text;
            string paymentID = paymentIdText.Text;
            int amount = 0;
            int fee = 10;

            if (!sendAddr.StartsWith("TRTL") || sendAddr.Length != 99)
            {
                MessageBox.Show("The address you are sending to is invalid, " +
                                "please check it. It should start with TRTL and " +
                                "be 99 characters long.", "TurtleCoin Wallet", 
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            string myAddr = myAddressText.Text;
            if(sendAddr == myAddr)
            {
                MessageBox.Show("Sending to yourself is not supported.", "TurtleCoin Wallet", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            try
            {
                amount = (int)(sendAmountText.Value * 100);
                if(amount <= 0)
                {
                    MessageBox.Show("Invalid send amount.", "TurtleCoin Wallet", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Invalid send amount.", "TurtleCoin Wallet", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }


            if (feeComboBox.SelectedIndex == 0)
                fee = Percent(amount, 0.005);
            else if (feeComboBox.SelectedIndex == 1)
                fee = Percent(amount, 0.01);
            else if (feeComboBox.SelectedIndex == 2)
                fee = Percent(amount, 0.025);
            else if (feeComboBox.SelectedIndex == 3)
                fee = Percent(amount, 0.05);

            if (fee < 10)
                fee = 10;

            int mixins = (int)mixinNumeric.Value;

            List<int> TransactionAmounts = new List<int>();
            if(amount > 50000000)
            {
                int wholeTrxs = (int)Math.Floor(amount / (double)50000000);
                int diff = amount - (wholeTrxs * 50000000);
                for (int i = 0; i < wholeTrxs; i++)
                {
                    TransactionAmounts.Add(50000000);
                }
                TransactionAmounts.Add(diff);
            }
            else
            {
                TransactionAmounts.Add(amount);
            }
            var transfers = new List<Dictionary<string, object>>();
            foreach (var Tamount in TransactionAmounts)
            {
                transfers.Add(new Dictionary<string, object>() { { "amount", Tamount }, { "address", sendAddr } });
            }

            var args = new Dictionary<string, object>()
            {
                { "anonymity", mixins },
                { "fee", fee },
                { "transfers", transfers },
                { "paymentId", paymentID}
            };

            try
            {
                var resp = ConnectionManager.Request("sendTransaction", args);
                if(resp.Item1 == false)
                {
                    MessageBox.Show("Error occured on send:" + Environment.NewLine + resp.Item2, "TurleCoin Wallet", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    windowLogger.Log(LogTextbox, "Error occured on send:" + Environment.NewLine + resp.Item2);
                    return;
                }
                string txhash = resp.Item3["transactionHash"].ToString();
                MessageBox.Show("Transaction send was successful!" + Environment.NewLine + "Amount: " + ((float)amount / 100).ToString() + Environment.NewLine + "Mix: " + mixins.ToString() + Environment.NewLine + "To: " + sendAddr + Environment.NewLine + "Trx hash: " + txhash, "TurtleCoin Wallet", MessageBoxButtons.OK,MessageBoxIcon.Information);
                windowLogger.Log(LogTextbox, "Transaction send was successful!" + Environment.NewLine + "Amount: " + ((float)amount / 100).ToString() + Environment.NewLine + "Mix: " + mixins.ToString() + Environment.NewLine + "To: " + sendAddr + Environment.NewLine + "Trx hash: " + txhash);
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error occured on send:" + Environment.NewLine + ex.Message, "TurleCoin Wallet", MessageBoxButtons.OK, MessageBoxIcon.Error);
                windowLogger.Log(LogTextbox, "Error occured on send:" + Environment.NewLine + ex.Message);
            }


        }

        private void SendRPCButton_Click(object sender, EventArgs e)
        {
            if(methodTextbox.Text == "")
            {
                MessageBox.Show("Invalid method on RPC send", "TurtleCoin Wallet", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (argTextbox.Text == "")
            {
                MessageBox.Show("Invalid argument on RPC send. If there are no arguments, use '{}'.", "TurtleCoin Wallet", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            try
            {
                var req = ConnectionManager._requestRPC(methodTextbox.Text, argTextbox.Text);
                rpcTextbox.AppendText(Environment.NewLine + req + Environment.NewLine);
            }
            catch(Exception ex)
            {
                rpcTextbox.AppendText(Environment.NewLine + "ERROR: " + ex.ToString() + Environment.NewLine);
            }
        }

        private void LogButton_Click(object sender, EventArgs e)
        {
            var currentButton = (Label)sender;
            if (_selectedTab != currentButton)
            {
                viewKeyText.Text = "";
                spendKeyText.Text = "";
                backupPasswordText.Text = "";
                var backcolor = Color.FromArgb(52, 52, 52);
                var forcolor = Color.FromArgb(224, 224, 224);
                _selectedTab.BackColor = backcolor;
                _selectedTab.ForeColor = forcolor;

                walletTabControl.SelectedIndex = 2;
                _selectedTab = currentButton;

                backcolor = Color.FromArgb(82, 82, 82);
                forcolor = Color.FromArgb(39, 170, 107);
                _selectedTab.BackColor = backcolor;
                _selectedTab.ForeColor = forcolor;
            }
        }

        private void RpcButton_Click(object sender, EventArgs e)
        {
            var currentButton = (Label)sender;
            if (_selectedTab != currentButton)
            {
                viewKeyText.Text = "";
                spendKeyText.Text = "";
                backupPasswordText.Text = "";
                var backcolor = Color.FromArgb(52, 52, 52);
                var forcolor = Color.FromArgb(224, 224, 224);
                _selectedTab.BackColor = backcolor;
                _selectedTab.ForeColor = forcolor;

                walletTabControl.SelectedIndex = 3;
                _selectedTab = currentButton;

                backcolor = Color.FromArgb(82, 82, 82);
                forcolor = Color.FromArgb(39, 170, 107);
                _selectedTab.BackColor = backcolor;
                _selectedTab.ForeColor = forcolor;
            }
        }

        private void SendAmountText_ValueChanged(object sender, EventArgs e)
        {
            if(sendAmountText.Value <= 100000)
            {
                mixinNumeric.Value = 3;
                feeComboBox.SelectedIndex = 0;
                sendButton.Enabled = true;
                sendButton.Cursor = System.Windows.Forms.Cursors.Hand;
                ToolTip disabledTooltip = new ToolTip();
                disabledTooltip.SetToolTip(sendButton, "Send TRTL!");
            }
            else if(sendAmountText.Value <= 500000)
            {
                mixinNumeric.Value = 5;
                feeComboBox.SelectedIndex = 1;
                sendButton.Enabled = true;
                sendButton.Cursor = System.Windows.Forms.Cursors.Hand;
                ToolTip disabledTooltip = new ToolTip();
                disabledTooltip.SetToolTip(sendButton, "Send TRTL!");
            }
            else if (sendAmountText.Value <= 1500000)
            {
                mixinNumeric.Value = 8;
                feeComboBox.SelectedIndex = 2;
                sendButton.Enabled = true;
                sendButton.Cursor = System.Windows.Forms.Cursors.Hand;
                ToolTip disabledTooltip = new ToolTip();
                disabledTooltip.SetToolTip(sendButton, "Send TRTL!");
            }
            else if (sendAmountText.Value >= 1500001)
            {
                mixinNumeric.Value = 13;
                feeComboBox.SelectedIndex = 3;
                sendButton.Enabled = true;
                sendButton.Cursor = System.Windows.Forms.Cursors.Hand;
                ToolTip disabledTooltip = new ToolTip();
                disabledTooltip.SetToolTip(sendButton, "Send TRTL!");
            }
            else
            {
                sendButton.Enabled = false;
                sendButton.Cursor = System.Windows.Forms.Cursors.No;
                ToolTip disabledTooltip = new ToolTip();
                disabledTooltip.SetToolTip(sendButton, "Invalid Amount!");
            }
        }

        public static int Percent(int number, double percent)
        {
            return (int)Math.Round(((double)number * percent) / 100);
        }

        private void BackupSubmitbutton_Click(object sender, EventArgs e)
        {
            if(backupPasswordText.Text == "")
            {
                MessageBox.Show("Please enter a valid password.", "TurtleCoin Wallet");
                return;
            }
            if(backupPasswordText.Text != WalletPassword)
            {
                MessageBox.Show("Incorrect password!", "TurtleCoin Wallet");
                return;
            }

            var viewresp = ConnectionManager.Request("getViewKey", new Dictionary<string, object> { });
            if (viewresp.Item1 == false)
            {
                MessageBox.Show("Error occured on getViewKey:" + Environment.NewLine + viewresp.Item2, "TurleCoin Wallet", MessageBoxButtons.OK, MessageBoxIcon.Error);
                windowLogger.Log(LogTextbox, "Error occured on getViewKey:" + Environment.NewLine + viewresp.Item2);
                return;
            }

            string viewkey = viewresp.Item3["viewSecretKey"].ToString();
            viewKeyText.Text = viewkey;

            var args = new Dictionary<string, object>()
            {
                { "address", myAddressText.Text }
            };

            var spendresp = ConnectionManager.Request("getSpendKeys", args);

            if (spendresp.Item1 == false)
            {
                MessageBox.Show("Error occured on getSpendKeys:" + Environment.NewLine + spendresp.Item2, "TurleCoin Wallet", MessageBoxButtons.OK, MessageBoxIcon.Error);
                windowLogger.Log(LogTextbox, "Error occured on getSpendKeys:" + Environment.NewLine + spendresp.Item2);
                return;
            }

            string spendkey = spendresp.Item3["spendSecretKey"].ToString();
            spendKeyText.Text = spendkey;
            MessageBox.Show("Wallet keys successfully unlocked!", "TurtleCoin Wallet");
        }

        private void BackupButton_Click(object sender, EventArgs e)
        {
            var currentButton = (Label)sender;
            if (_selectedTab != currentButton)
            {
                viewKeyText.Text = "";
                spendKeyText.Text = "";
                backupPasswordText.Text = "";
                var backcolor = Color.FromArgb(52, 52, 52);
                var forcolor = Color.FromArgb(224, 224, 224);
                _selectedTab.BackColor = backcolor;
                _selectedTab.ForeColor = forcolor;

                walletTabControl.SelectedIndex = 4;
                _selectedTab = currentButton;

                backcolor = Color.FromArgb(82, 82, 82);
                forcolor = Color.FromArgb(39, 170, 107);
                _selectedTab.BackColor = backcolor;
                _selectedTab.ForeColor = forcolor;
            }
        }

        private void SaverWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                var saveresp = ConnectionManager.Request("save", new Dictionary<string, object> { });
                if (saveresp.Item1 == false)
                {
                    MessageBox.Show("Error occured trying to save the wallet state:" + Environment.NewLine + saveresp.Item2, "TurleCoin Wallet", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch { }
            
        }

        private void SaverWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            runningDaemon.Kill();
            Environment.Exit(0);
        }
    }
}
