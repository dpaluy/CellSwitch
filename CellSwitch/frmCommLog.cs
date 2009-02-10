using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using CellSwitch;

namespace CellSwitch
{
    public partial class frmCommLog : Form
    {
        private static readonly frmCommLog instance = new frmCommLog();
        private CommunicationManager comm = null;
        private bool execScript = false;
        string transType = string.Empty;

        public static frmCommLog Instance
        {
            get { return instance; }
        }

        public CommunicationManager commManager
        {
            get { return comm; }
            set { comm = value; }
        }

        private frmCommLog()
        {
            InitializeComponent();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            LoadValues();
            SetDefaults();
            SetControlState();
            OpenPortUI();
        }

        private void cmdOpen_Click(object sender, EventArgs e)
        {
            comm.Parity   = cboParity.Text;
            comm.StopBits = cboStop.Text;
            comm.DataBits = cboData.Text;
            comm.BaudRate = cboBaud.Text;
            comm.DisplayWindow = rtbDisplay;
            comm.Start();

            OpenPortUI();
        }

        private void OpenPortUI()
        {
            if (comm.isPortOpen)
            {
                groupBoxCom.Enabled = false;
                cmdOpen.Enabled     = false;
                cmdClose.Enabled    = true;
                cmdSend.Enabled     = true;
                btnSendEOL.Enabled  = true;
                txtSend.Enabled     = true;
                txtSend.Focus();
            }
        }

        /// <summary>
        /// Method to initialize serial port
        /// values to standard defaults
        /// </summary>
        private void SetDefaults()
        {
            if (true == comm.isPortOpen)
            {
                int cboIndex = cboPort.FindString(comm.PortName);
                cboPort.SelectedIndex = (cboIndex > 0) ? cboIndex : 0;
                cboIndex = cboBaud.FindString(comm.BaudRate);
                cboBaud.SelectedIndex = (cboIndex > 0) ? cboIndex : 0;
                cboIndex = cboData.FindString(comm.DataBits);
                cboData.SelectedIndex = (cboIndex > 0) ? cboIndex : 0;
                cboParity.Text = comm.Parity;
                cboStop.Text = comm.StopBits;
            }
            else
            {
                cboPort.SelectedIndex = 0;
                cboBaud.SelectedText = "9600";
                cboParity.SelectedIndex = 0;
                cboStop.SelectedIndex = 1;
                cboData.SelectedIndex = 1;
            }
        }

        /// <summary>
        /// methods to load our serial
        /// port option values
        /// </summary>
        private void LoadValues()
        {
            string[] ports = CommunicationManager.GetComPortNames();
            foreach (string str in ports)
            {
                cboPort.Items.Add(str);
            }
            comm.SetParityValues(cboParity);
            comm.SetStopBitValues(cboStop);
            comm.DisplayWindow = this.rtbDisplay;            
        }

        /// <summary>
        /// method to set the state of controls
        /// when the form first loads
        /// </summary>
        private void SetControlState()
        {
            rdoText.Checked = true;
            cmdSend.Enabled = false;
            cmdClose.Enabled = false;

        }

        private void cmdSend_Click(object sender, EventArgs e)
        {
            sendData();
        }

        private void rdoHex_CheckedChanged(object sender, EventArgs e)
        {
            if (rdoHex.Checked == true)
            {
                comm.CurrentTransmissionType = CommunicationManager.TransmissionType.Hex;
            }
            else
            {
                comm.CurrentTransmissionType = CommunicationManager.TransmissionType.Text;
            }
        }

        private void cmdClose_Click(object sender, EventArgs e)
        {
            comm.Stop();
            closePortUI();
        }

        private void closePortUI()
        {
            if (false == comm.isPortOpen)
            {
                cmdOpen.Enabled     = true;
                groupBoxCom.Enabled = true;
                cmdClose.Enabled    = false;
                cmdSend.Enabled     = false;
                btnSendEOL.Enabled  = false;
                txtSend.Enabled     = false;
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            txtSend.Clear();
            rtbDisplay.Clear();
        }

        private void checkBoxAutoEOL_CheckedChanged(object sender, EventArgs e)
        {
            comm.AutoEOL = (true == checkBoxAutoEOL.Checked)? true: false;

        }

        private void button1_Click(object sender, EventArgs e)
        {
            comm.SendEndOfLine();
        }

        private void txtSend_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 0x0D)
            {
                sendData();       
                e.Handled = true;
            }
        }

        private void sendData()
        {
            if (true == chkBoxUpperCase.Checked)
                txtSend.Text = txtSend.Text.ToUpper();
            comm.WriteData(txtSend.Text);
            txtSend.SelectAll();
        }

        private void cboPort_SelectedIndexChanged(object sender, EventArgs e)
        {
            comm.PortName = cboPort.Text;
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            string filter = "Script Files (*.at)|*.at|Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
            string title = "Open File";
            string fileName = FormTools.openFileDialog(filter, title);
            ExecuteScript(fileName);
        }

        private void ExecuteScript(string fileName)
        {
            FileStream strm;
            try
            {
                strm = new FileStream(fileName, FileMode.Open, FileAccess.Read);
                StreamReader rdr = new StreamReader(strm);
                execScript = true;
                string str;
                while ((rdr.Peek() >= 0) && (true == execScript) )
                {
                    str = rdr.ReadLine();
                    RunAtCommand(str);
                    FormTools.Wait(5000);
                }
                execScript = false;
            }
            catch (Exception ex)
            {
                FormTools.ErrBox(ex.ToString(), "Script File Error");
                execScript = false;
            }
        }

        private void RunAtCommand(string atCmd)
        {
            const string COMMENT = "'";
            if (false == atCmd.StartsWith(COMMENT))
            {
                txtSend.Text = atCmd;
                sendData();
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string filter = "Text Files (*.txt)|*.txt|Log Files (*.log)|*.log";
            string title = "Save File";
            string fileName = FormTools.saveFileDialog(filter, title).Trim();
            if (fileName.Length > 0)
            {
                FileInfo fi = new FileInfo(fileName);
                StreamWriter sw = fi.CreateText();
                sw.Write(rtbDisplay.ToString());
                sw.Close();
            }
        }

        private void frmCommLog_KeyPress(object sender, KeyPressEventArgs e)
        {
            if ((execScript == true) && (e.KeyChar == 0x1B))
            {
                execScript = false;
                e.Handled = true;
            }
        }

        private void frmCommLog_FormClosed(object sender, FormClosedEventArgs e)
        {
            comm.DisplayWindow = null;
            this.Hide();
        }    
    }
}