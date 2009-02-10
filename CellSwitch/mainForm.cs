using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace CellSwitch
{
    public partial class mainForm : Form
    {
        #region Variables
        private int MAX_ALLOWED_USERS = 1559;
        private SwitchProtocol sp_;
        private CommunicationManager cm_;
        private enum SwitchControl { FROM, TO, NA };
        private SwitchControl switch_ = SwitchControl.NA;
        private string phones = string.Empty;
        private string switchPhoneNumber_ = string.Empty;
        private int percent = 0;
        #endregion

        #region Constructor
        public mainForm(SwitchProtocol sp, CommunicationManager cm)
        {
            InitializeComponent();
            sp_ = sp;
            cm_ = cm;
            string[] ports = CommunicationManager.GetComPortNames();
            if (ports.Length > 0)
            {
                foreach (string str in ports)
                {
                    cmbComPort.Items.Add(str);
                }
            }
            else
                cmbComPort.Items.Add("NO COM PORT!");
            cmbComPort.SelectedIndex = 0;
            SetCommunicationValues();

            removeUser.Enabled = false;
            statusProgressBar.Visible = false;
            cmbSwitchPhone.Items.Add("0549723143");
            cmbSwitchPhone.SelectedIndex = 0;
            btnConnectTtransmitter.Focus();
        }
        #endregion

        #region Set Communication Values
        private void SetCommunicationValues()
        {
            cm_.PortName = cmbComPort.Text;
            cm_.Parity = "None";
            cm_.StopBits = "1";
            cm_.DataBits = "8";
            cm_.BaudRate = "9600";
        }
        #endregion

        #region Exit Application
        private void exitToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            exitApplication();
        }

        private void mainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            exitApplication();
        }

        private void exitApplication()
        {
            if (true == sp_.Connected)
                sp_.Disconnect();
            cm_.Stop();
            Application.Exit();
        }
        #endregion

        #region About Dialog
        private void aboutToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            AboutBox frm = new AboutBox();
            frm.Show();
        }
        #endregion

        #region Logger Form
        private void logToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frmCommLog frm = frmCommLog.Instance;
            if (frm != null)
            {
                frm.Show();
                frm.Activate();
            }
        }
        #endregion

        #region Data Grid
        private void dataGridViewUsers_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            string err = "The Phone number can't be empty!\n";
#if (DEBUG)            
            string exception = "\n" + e.Exception.ToString();
#else
            string exception="";
#endif
            string caption = Application.ProductName + " - Input Error";
            FormTools.ErrBox(err + exception, caption);
        }
        #endregion

        #region Transmitter
        private void btnConnectTtransmiter_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;
            if (!bgwTransmitter.IsBusy && btnConnectTtransmitter.Text == "Connect") // Open Port
            {
                btnConnectTtransmitter.Text = "Cancel";
                if (false == cm_.Start())
                {
                    this.Cursor = Cursors.Default;
                    FormTools.ErrBox("Cannot open Transmitter Port!", "Connect to Transmitter");
                }
                else {
                    statusProgressBar.Visible = true;
                    statusProgressBar.Value = 0;
                    toolStripStatus.Text = "Connecting";
                    if (bgwTransmitter.IsBusy) bgwTransmitter.CancelAsync();
                    bgwTransmitter.RunWorkerAsync();
                }
            }
            else
            {
                bgwTransmitter.CancelAsync();
                if (bgwSwitch.IsBusy) bgwSwitch.CancelAsync();
                CloseTransmitter();
                this.Cursor = Cursors.Default;
            }
        }

        private void cmbComPort_SelectedIndexChanged(object sender, EventArgs e)
        {
            cm_.PortName = cmbComPort.Text;
        }
        #endregion
        
        #region bgwTransmitter Events
        private void bgwTransmitter_DoWork(object sender, DoWorkEventArgs e)
        {
            STATUS status = sp_.modemSetup(bgwTransmitter, ref e);
            //if (status != STATUS.OK)
            //    FormTools.ErrBox(status.ToString(), "Initialize Transmitter");
        }

        private void bgwTransmitter_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            statusProgressBar.Value = e.ProgressPercentage;
        }

        private void bgwTransmitter_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
                CloseTransmitter();
            this.Cursor = Cursors.Default;
            TransmitterReadyMainFormUpdate();
            statusProgressBar.Visible = false;
        }

        private void TransmitterReadyMainFormUpdate()
        {
            if (true == cm_.isPortOpen && true == sp_.TransmitterRegistered)
            {
                btnConnectTtransmitter.Text = "Disconnect";
                txtOperator.Text            = sp_.OperatorName;
                cmbComPort.Enabled          = false;
                grpCellSwitch.Enabled       = true;
                btnLoadFromSwitch.Enabled   = true;
                btnSendToSwitch.Enabled     = true;
                picTransmitterStatus.Image  = CellSwitch.Properties.Resources.on;
                toolStripStatus.Text        = "Transmitter Ready";
            }
            else
            {
                toolStripStatus.Text = "Transmitter is not Ready!";
                btnConnectTtransmitter.Text = "Connect";
            }
        }
        #endregion

        #region Transmitter Disconnect
        private void CloseTransmitter()
        {
            cm_.Stop();
            sp_.Connected = false;
            DisconnectMainFormUI();
            PortCloseUI();
        }
        private void DisconnectMainFormUI()
        {
            if (!sp_.Connected)
            {
                //txtDeviceID.Text = "Not Connected";
                //txtOperator.Text = "Not Connected";
                picConnStatus.Image = CellSwitch.Properties.Resources.off;
            }
        }
        private void PortCloseUI()
        {
            if (false == cm_.isPortOpen)
            {
                toolStripStatus.Text = "Transmitter Disconnected";
                btnConnectTtransmitter.Text = "Connect";
                cmbComPort.Enabled = true;
                grpCellSwitch.Enabled = false;
                btnLoadFromSwitch.Enabled = false;
                btnSendToSwitch.Enabled = false;
                picTransmitterStatus.Image = CellSwitch.Properties.Resources.off;
            }
        }
        #endregion

        #region Switch Control

        private void Disconnect()
        {
            sp_.Disconnect();
            DisconnectMainFormUI();
        }

        private void handleConnectToSwitch(System.ComponentModel.BackgroundWorker bgw, ref DoWorkEventArgs e)
        {
            percent = 1;
            ReportProgress(bgw, ref e, ref percent);
            string phone = RemoveNonDigitsFromString(switchPhoneNumber_);
            ReportProgress(bgw, ref e, ref percent);
            if (phone.Length < 5)
            {
                FormTools.ErrBox("Wrong Switch Phone Number!", "Connect To Switch");
                if (bgwSwitch.IsBusy) bgwSwitch.CancelAsync();
                return;
            }
            sp_.ConnectToSwitch(phone);
            ReportProgress(bgw, ref e, ref percent);
            if (true == sp_.Connected)
            {
                ConnectSwitchUI();
            }
            else
            {
                //FormTools.ErrBox("Connection Failed!", "Connect to Switch");
                //cmbSwitchPhone.SelectAll();
            }
            ReportProgress(bgw, ref e, ref percent);
        }

        delegate void SetTextCallback(TextBox txtBox, string text);

        private void SetText(TextBox txtBox, string text)
        {
            if (txtBox.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetText);
                this.Invoke(d, new object[] { txtBox, text });
            }
            else
            {
                txtBox.Text = text;
            }
        }

        delegate void SetImageCallback(PictureBox picBox, Image image);

        private void SetImage(PictureBox picBox, Image image)
        {
            if (picBox.InvokeRequired)
            {
                SetImageCallback d = new SetImageCallback(SetImage);
                this.Invoke(d, new object[] { picBox, image });
            }
            else
            {
                picBox.Image = image;
            }
        }
        private void ConnectSwitchUI()
        {
            if (sp_.Connected)
            {
                SetText(txtDeviceID, sp_.DeviceID);
                SetText(txtOperator, sp_.OperatorName);
                SetImage(picConnStatus, CellSwitch.Properties.Resources.on);
            }
        }

        private void cmbSwitchPhone_TextChanged(object sender, EventArgs e)
        {
            switchPhoneNumber_ = cmbSwitchPhone.Text.Trim();
            txtDeviceID.Text = "Not Connected";
        }

        private void cmbSwitchPhone_KeyPress(object sender, KeyPressEventArgs e)
        {
            if ((int)e.KeyChar >= 0x30 && (int)e.KeyChar <= 0x39)
                return;
            switch ((int)e.KeyChar)
            {
                case 0x2B: //"+"
                case 0x2D: //"-"
                case 0x20: //" ":
                    break;
                default:
                    e.Handled = true;
                    break;
            }
        }
        #endregion

        #region Load From Switch
        private void btnLoadFromSwitch_Click(object sender, EventArgs e)
        {
            startWorkGUI();
            switch_ = SwitchControl.FROM;
            phones = string.Empty;
            bgwSwitch.RunWorkerAsync();
        }
        #endregion

        #region Send to Switch
        private void btnSendToSwitch_Click(object sender, EventArgs e)
        {
            if (users.Tables[0].Rows.Count == 0)
            {
                FormTools.ErrBox("Please add some phones, before connecting!", "Send Phones");
                return;
            }
            startWorkGUI();
            StringBuilder sb = new StringBuilder();
            foreach (DataRow row in users.Tables[0].Rows)
            {
                string phone = (string)row["PhoneNumber"];
                bool enabled = (bool) row["Enabled"];
                phone = RemoveNonDigitsFromString(phone).Trim();
                if (enabled && phone.Length > 0)
                    sb.Append(phone + " ");
            }
            phones = sb.ToString();

            switch_ = SwitchControl.TO;
            bgwSwitch.RunWorkerAsync();
        }

        private string RemoveNonDigitsFromString(string s)
        {
            string newresult = "";
            try
            {
                foreach (char c in s)
                {
                    if (char.IsDigit(c))
                    {
                        newresult += c.ToString();
                    }
                }
            }
            catch { }
            return newresult;
        }
        #endregion

        #region Open File
        private void openFileToolStripButton_Click(object sender, EventArgs e)
        {
            if (false == newDataSet()) return;

            string fileLoadFrom = FormTools.openFileDialog("XML files (*.xml)|*.xml|All files (*.*)|*.*", "Open Users File");
            this.users.ReadXml(fileLoadFrom);
            dataGridView.ClearSelection();
        }
        #endregion

        #region Save File
        private void saveFileToolStripButton_Click(object sender, EventArgs e)
        {
            string fileSaveTo = FormTools.saveFileDialog("XML files (*.xml)|*.xml|All files (*.*)|*.*", "Save Users To File");
            this.users.WriteXml(fileSaveTo, XmlWriteMode.WriteSchema);
        }
        #endregion

        #region TransmitterButton Mouse events
        private void btnConnectTtransmitter_MouseHover(object sender, EventArgs e)
        {
            this.Cursor = Cursors.Default;
        }

        private void btnConnectTtransmitter_MouseLeave(object sender, EventArgs e)
        {
            if (bgwTransmitter.IsBusy)
                this.Cursor = Cursors.WaitCursor;
        }
        #endregion

        #region New DataSet
        private void newFileToolStripButton_Click(object sender, EventArgs e)
        {
            newDataSet();
        }

        private bool newDataSet()
        {
            if (users.User.Rows.Count > 0)
            {
                DialogResult dr = FormTools.ConfimBox("All the current data will be erased.\n Are you sure?", "New Data");
                if (dr == DialogResult.No)
                    return false;
            }
            this.users.Clear();
            return true;
        }
        #endregion

        #region Add new user
        private void newUserAdd_Click(object sender, EventArgs e)
        {
            UserForm uf = new UserForm(this.users);
            uf.ShowDialog();
        }
        #endregion

        #region Remove Selected User
        private void removeUser_Click(object sender, EventArgs e)
        {
            int selectedRowsCount = dataGridView.SelectedRows.Count;
            if (selectedRowsCount > 0)
            {
                if (DialogResult.No == FormTools.ConfimBox("Are you sure?", "Delete Users")) return;

                for (int i = selectedRowsCount-1; i >= 0; i--)
                    users.Tables[0].Rows[dataGridView.SelectedRows[i].Index].Delete();
            }

        }
        #endregion

        #region DataGridView Events
        private void dataGridView_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            removeUser.Enabled = true;
            totalNumber.Text = dataGridView.Rows.Count.ToString();
            if (dataGridView.Rows.Count >= MAX_ALLOWED_USERS)
                newUserAdd.Enabled = false;
        }

        private void dataGridView_RowsRemoved(object sender, DataGridViewRowsRemovedEventArgs e)
        {
            newUserAdd.Enabled = true;
            totalNumber.Text = dataGridView.Rows.Count.ToString();
            if (dataGridView.Rows.Count <= 0)
                removeUser.Enabled = false;
        }
        #endregion

        #region SearchGrid
        private void toolStripTextSearch_TextChanged(object sender, EventArgs e)
        {
            GridFilter();
        }

        private void GridFilter()
        {
            if (toolStripTextSearch.Text.Length == 0)
                gridBinding.Filter ="";
            else
            {
                gridBinding.Filter = string.Format("PhoneNumber LIKE '*{0}*'", toolStripTextSearch.Text);
                dataGridView.ClearSelection();
            }
        }

        private void toolStripSearchButton_Click(object sender, EventArgs e)
        {
            GridFilter();
        }
        #endregion

        #region Switch From/To
        private void startWorkGUI()
        {
            btnLoadFromSwitch.Enabled = false;
            btnSendToSwitch.Enabled = false;
            toolStripData.Enabled = false;
            dataGridView.Enabled = false;
            statusProgressBar.Visible = true;
            toolStripStatus.Text = "Connecting...";
            this.Cursor = Cursors.WaitCursor;
        }

        private void stopWorkGUI()
        {
            FormTools.Wait(FormTools.SEC * 2);
            btnLoadFromSwitch.Enabled = true;
            btnSendToSwitch.Enabled = true;
            toolStripData.Enabled = true;
            dataGridView.Enabled = true;
            statusProgressBar.Visible = false;
            this.Cursor = Cursors.Default;
        }

        private void bgwSwitch_DoWork(object sender, DoWorkEventArgs e)
        {
            handleConnectToSwitch(bgwSwitch, ref e);
            if (false == sp_.Connected)
            {
                FormTools.ErrBox("Switch connection failed!", "Connection");
                return;
            }
            ReportProgress(bgwSwitch, ref e, ref percent);
            switch (switch_)
            {
                case SwitchControl.FROM:
                    //SetStatusText("Getting Data...");
                    sp_.GetPhoneList(ref phones);
                    break;
                case SwitchControl.TO:
                    //SetStatusText("Sending Data...");
                    sp_.SendPhoneList(phones);
                    break;
                default:
                    break;
            }
            ReportProgress(bgwSwitch, ref e, ref percent);
            if (sp_.Connected)
            {
                string ouput = cm_.ReadData(FormTools.SEC * 60);
                Disconnect();
            }
            percent = 90;
            ReportProgress(bgwSwitch, ref e, ref percent);
        }

        private void ReportProgress(System.ComponentModel.BackgroundWorker bgw, ref DoWorkEventArgs e, ref int i)
        {
            if (bgw.CancellationPending)
            {
                e.Cancel = true;
                return;
            }
            bgw.ReportProgress(i);
            i = (i + 10) % 100;
        }

        private void bgwSwitch_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            statusProgressBar.Value = e.ProgressPercentage;
        }

        private void bgwSwitch_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            switch (switch_)
            {
                case SwitchControl.FROM:
                    this.users.Clear();
                    ParseResult();
                    toolStripStatus.Text = "All data loaded!";
                    break;
                case SwitchControl.TO:
                    toolStripStatus.Text = "Switch updated successfully!";
                    break;
                default:
                    break;
            }
            switch_ = SwitchControl.NA;
            stopWorkGUI();
        }
        #endregion

        private void ParseResult()
        {
            DataTable dtg = users.Tables[0]; 
            while (phones.Length > 0)
            {
                int i = phones.IndexOf(" ");
                string phone;
                if (i > 0)
                {
                    phone = phones.Substring(0, i);
                    phones = phones.Substring(i + 1);
                }
                else
                {
                    phone = phones;
                    phones = string.Empty;
                }
                if (phone.Length > 0)
                {
                    try
                    {
                        DataRow user = dtg.NewRow(); 
                        user["FirstName"] = string.Empty;
                        user["LastName"] = string.Empty;
                        user["PhoneNumber"] = phone;
                        user["Note"] = string.Empty;
                        user["Enabled"] = true;
                        dtg.Rows.Add(user);
                    }
                    catch (Exception)
                    {
                    }
                }
            } // End of while
            dataGridView.DataSource = dtg;
        }
    }
}