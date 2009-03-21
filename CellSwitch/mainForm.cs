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
using System.Data.OleDb;

namespace CellSwitch
{
    public partial class mainForm : Form
    {
        #region Variables
        private enum COLUMNS { FIRST_NAME, LAST_NAME, PHONE, NOTE };
        private int MAX_ALLOWED_USERS = 2000;
        private SwitchProtocol sp_;
        private CommunicationManager cm_;
        private enum SwitchControl { FROM, TO, NA };
        private SwitchControl switch_ = SwitchControl.NA;
        private string phones = string.Empty;
        private string switchPhoneNumber_ = string.Empty;
        private int percent = 0;
        private bool sendDataResult = false;
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
            cmbSwitchPhone.Items.Add(Properties.Settings.Default.SwitchPhone);
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
            Properties.Settings.Default.SwitchPhone = cmbSwitchPhone.Text;
            Properties.Settings.Default.Save();
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
            if (fileLoadFrom.Length > 0)
            {
                this.users.ReadXml(fileLoadFrom);
                dataGridView.ClearSelection();
            }
        }
        #endregion

        #region Save File
        private void saveFileToolStripButton_Click(object sender, EventArgs e)
        {
            string fileSaveTo = FormTools.saveFileDialog("XML files (*.xml)|*.xml|All files (*.*)|*.*", "Save Users To File");
            if (fileSaveTo.Length > 0)
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
                    sendDataResult = sp_.SendPhoneList(phones);
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
                    if (sendDataResult == false)
                    {
                        toolStripStatus.Text = "Switch updated failed!";
                        FormTools.ErrBox("Switch updated failed!", "Switch Update Status");
                    }
                    else
                        toolStripStatus.Text = "Switch updated successfully!";
                    break;
                default:
                    break;
            }
            switch_ = SwitchControl.NA;
            stopWorkGUI();
        }
        #endregion

        #region Parse Result
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
        #endregion

        #region User
        private void addNewUser(string firstName, string lastName, string phone, string note)
        {
            DataRow user = this.users.Tables[0].NewRow();
            user["FirstName"] = firstName;
            user["LastName"] = lastName;
            user["PhoneNumber"] = phone;
            user["Note"] = note;
            user["Enabled"] = true;
            this.users.Tables[0].Rows.Add(user);
        }

        private void addNewRow(DataRow row, int[] colIndex)
        {
            string firstName = string.Empty;
            try
            {
                firstName = (string)row[colIndex[(int)COLUMNS.FIRST_NAME]];
            }
            catch (Exception) { }

            string lastName = string.Empty;
            try {
                lastName = (string)row[colIndex[(int)COLUMNS.LAST_NAME]];
            } catch (Exception){}
            

            string phone = string.Empty;
            try{
                phone = (string)row[colIndex[(int)COLUMNS.PHONE]];
            } catch (InvalidCastException)
            {
                double num = (double)row[colIndex[(int)COLUMNS.PHONE]];
                phone = num.ToString();
            }
            
            string note = string.Empty;
            try{
                  note = (string)row[colIndex[(int)COLUMNS.NOTE]];
            } catch (Exception) {}                
            
            addNewUser(firstName, lastName, phone, note);
        }


        #endregion

        #region XSL tools
        /// <summary>
        /// This mehtod retrieves the excel sheet names from 
        /// an excel workbook.
        /// </summary>
        /// <param name="excelFile">The excel file.</param>
        /// <returns>String[]</returns>
        private String[] GetExcelSheetNames(string excelFile)
        {
            OleDbConnection objConn = null;
            System.Data.DataTable dt = null;

            try
            {
                // Connection String. Change the excel file to the file you
                // will search.
                string ext = System.IO.Path.GetExtension(excelFile).ToLower();
                string connString = defineExcelConnection(ext, excelFile);
                
                // Create connection object by using the preceding connection string.
                objConn = new OleDbConnection(connString);
                // Open connection with the database.
                objConn.Open();
                // Get the data table containg the schema guid.
                dt = objConn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);

                if (dt == null)
                {
                    return null;
                }

                String[] excelSheets = new String[dt.Rows.Count];
                int i = 0;

                // Add the sheet name to the string array.
                foreach (DataRow row in dt.Rows)
                {
                    excelSheets[i] = row["TABLE_NAME"].ToString();
                    i++;
                }

                return excelSheets;
            }
            catch (Exception )
            {
                return null;
            }
            finally
            {
                // Clean up.
                if (objConn != null)
                {
                    objConn.Close();
                    objConn.Dispose();
                }
                if (dt != null)
                {
                    dt.Dispose();
                }
            }
        }

        private string defineExcelConnection(string ext, string sheetToCreate)
        {
            string strCn = string.Empty;
            switch (ext)
            {
                case ".xls":
                    strCn = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + sheetToCreate + "; Extended Properties='Excel 8.0;HDR=YES'";
                    break;
                case ".xlsx":
                    strCn = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + sheetToCreate + ";Extended Properties='Excel 12.0 Xml;HDR=YES' ";
                    break;
                case ".xlsb":
                    strCn = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + sheetToCreate + ";Extended Properties='Excel 12.0;HDR=YES' ";
                    break;
                case ".xlsm":
                    strCn = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + sheetToCreate + ";Extended Properties='Excel 12.0 Macro;HDR=YES' ";
                    break;
                default:
                    FormTools.ErrBox("Unknown Excel Format!", "Import/Export from/to Excel");
                    break;
            }
            return strCn;
        }

        private int[] defineCols( DataSet ds )
        {
            int[] colIndex = new int[4];
	        Array.Clear(colIndex, 0, colIndex.Length);
            int j=0;
            foreach(DataColumn theCol in ds.Tables[0].Columns)
            {
                if (theCol.ColumnName.StartsWith("First"))
                    colIndex[(int)COLUMNS.FIRST_NAME] = j;
                else if (theCol.ColumnName.StartsWith("Last"))
                    colIndex[(int)COLUMNS.LAST_NAME] = j;
                else if (theCol.ColumnName.StartsWith("Phone"))
                    colIndex[(int)COLUMNS.PHONE] = j;
                else if (theCol.ColumnName.StartsWith("Note"))
                    colIndex[(int)COLUMNS.NOTE] = j;
                j++;
            }
            return colIndex;
        }

        #endregion

        #region Xsl Import
        public void import_xsl()
        {
            System.Data.OleDb.OleDbDataAdapter da = null;
            DataSet ds = null;
            string filename = string.Empty;
            filename = FormTools.openFileDialog(
                "Excel 97-2003 files (*.xls)|*.xls|Excel 2007 files (*.xlsx)|*.xlsx|All files (*.*)|*.*", "Import from...");
            if (filename.Length > 0)
            {
                string ext = System.IO.Path.GetExtension(filename).ToLower();
                string strConn = defineExcelConnection(ext, filename);
                if (strConn.Length <= 0)
                    return;
                try
                {
                    ds = new DataSet();
                    
                    String[] SheetsArray = GetExcelSheetNames(filename);

                    //You must use the $ after the object you reference in the spreadsheet
                    string sheet = (SheetsArray[0].EndsWith("$")) ? SheetsArray[0] : SheetsArray[0] + "$";
                    string sql = "SELECT * FROM [" + sheet + "]";
                    da = new System.Data.OleDb.OleDbDataAdapter(sql, strConn);
                    da.Fill(ds);

                    int[] colIndex = defineCols(ds);

                    foreach (DataRow theRow in ds.Tables[0].Rows)
                    {
                        addNewRow(theRow, colIndex);
                    }
                } catch (Exception err)
                {
                    FormTools.ErrBox(err.Message, "Importing from Excel");
                }
                finally
                {
                    // Clean up.
                    if (da != null)
                        da.Dispose();
                    if (ds != null)
                    {
                        ds.Dispose();
                    }
                }
                dataGridView.ClearSelection();
            }
        }

        private void importToolStripMenuItem_Click(object sender, EventArgs e)
        {
            import_xsl();
        }
        #endregion

        #region Xsl Export
        private void ExportToExcel(string sheetToCreate, DataTable dtToExport, string tableName)
        {
            List<DataRow> rows = new List<DataRow>();
            foreach (DataRow row in dtToExport.Rows) rows.Add(row);
            subExportToExcel(sheetToCreate, rows, dtToExport, tableName);
        }

        private void subExportToExcel(string sheetToCreate, List<DataRow> selectedRows, DataTable origDataTable, string tableName)
        {
            char Space = ' ';
            string dest = sheetToCreate;
            int i = 0;
            while (File.Exists(dest))
            {
                dest = Path.GetDirectoryName(sheetToCreate) + "\\" + Path.GetFileName(sheetToCreate) + i + Path.GetExtension(sheetToCreate);
                i += 1;
            }
            sheetToCreate = dest;
            if (tableName == null) tableName = string.Empty;
            tableName = tableName.Trim().Replace(Space, '_');
            if (tableName.Length == 0) tableName = origDataTable.TableName.Replace(Space, '_');
            if (tableName.Length == 0) tableName = "NoTableName";
            if (tableName.Length > 30) tableName = tableName.Substring(0, 30);
            //Excel names are less than 31 chars
            string queryCreateExcelTable = "CREATE TABLE [" + tableName + "] (";
            Dictionary<string, string> colNames = new Dictionary<string, string>();
            foreach (DataColumn dc in origDataTable.Columns)
            {
                //Cause the query to name each of the columns to be created.
                string modifiedcolName = dc.ColumnName.Replace(Space, '_').Replace('.', '#');
                string origColName = dc.ColumnName;
                colNames.Add(modifiedcolName, origColName);
                queryCreateExcelTable += "[" + modifiedcolName + "]" + " text,";
            }
            queryCreateExcelTable = queryCreateExcelTable.TrimEnd(new char[] { Convert.ToChar(",") }) + ")";
            //adds the closing parentheses to the query string
            if (selectedRows.Count > 65000 && sheetToCreate.ToLower().EndsWith(".xls"))
            {
                //use Excel 2007 for large sheets.
                sheetToCreate = sheetToCreate.ToLower().Replace(".xls", string.Empty) + ".xlsx";
            }
            string ext = System.IO.Path.GetExtension(sheetToCreate).ToLower();
            string strConn = defineExcelConnection(ext, sheetToCreate);
            if (strConn.Length <= 0)
                return;

            System.Data.OleDb.OleDbConnection cn = new System.Data.OleDb.OleDbConnection(strConn);
            System.Data.OleDb.OleDbCommand cmd = new System.Data.OleDb.OleDbCommand(queryCreateExcelTable, cn);
            cn.Open();
            cmd.ExecuteNonQuery();
            System.Data.OleDb.OleDbDataAdapter da = new System.Data.OleDb.OleDbDataAdapter("SELECT * FROM [" + tableName + "]", cn);
            System.Data.OleDb.OleDbCommandBuilder cb = new System.Data.OleDb.OleDbCommandBuilder(da);
            //creates the INSERT INTO command
            cb.QuotePrefix = "[";
            cb.QuoteSuffix = "]";
            cmd = cb.GetInsertCommand();
            //gets a hold of the INSERT INTO command.
            foreach (DataRow row in selectedRows)
            {
                foreach (System.Data.OleDb.OleDbParameter param in cmd.Parameters)
                    param.Value = row[colNames[param.SourceColumn]];
                cmd.ExecuteNonQuery(); //INSERT INTO command.
            }
            cn.Close();
            cn.Dispose();
            da.Dispose();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        public void export_xsl()
        {
            string filename = string.Empty;
            filename = FormTools.saveFileDialog(
                "Excel 97-2003 files (*.xls)|*.xls|Excel 2007 files (*.xlsx)|*.xlsx|All files (*.*)|*.*",
                "Export to Excel...");
            if (filename.Length > 0)
            {
                try
                {
                    ExportToExcel(filename, users.Tables[0], "Gizmo Gate");
                }
                catch (Exception err)
                {
                    FormTools.ErrBox(err.Message, "Exporting to Excel");
                }
            }
        }

        private void exportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            export_xsl();
        }
        #endregion
    }
}