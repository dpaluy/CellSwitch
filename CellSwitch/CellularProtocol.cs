using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.ComponentModel;

namespace CellSwitch
{
    public enum STATUS { OK, CANCELED, CONNECT_9600, BUSY, NO_CARRIER, ERROR_AUTHORIZE, ERROR_COM_PORT, ERROR_AT_COMMAND };

    public class CellularProtocol
    {

        #region Protocol Variables
        private bool registered_ = false;
        private CommunicationManager cm_ = null;
        private const int MAX_NUMBER_OF_TRIES = 3;
        private const int DEFAULT_TIMEOUT = 100; // msec
        private const int SEC = 1000; // sec definition by msec
        private const string CR = "\r"; // End of line       
        #endregion

        #region Constructor
        public CellularProtocol(CommunicationManager cm)
        {
            this.cm_ = cm;
        }
        #endregion

        #region Port Ready
        /// <summary>
        /// Open com port
        /// </summary>
        /// <returns>true - if the port is open, otherwise - false</returns>
        public bool Ready()
        {
            return cm_.Start();
        }

        /// <summary>
        /// Return Protocol ready status
        /// </summary>
        public bool isReady
        {
            get { return cm_.isPortOpen; }
        }
        #endregion

        #region Register
        public bool Registered
        {
            get { return registered_; }
            set { registered_ = value; }
        }

        public STATUS register(ref string operatorName, System.ComponentModel.BackgroundWorker bgw, ref DoWorkEventArgs e)
        {
            Registered = false;
            if (false == cm_.isPortOpen)
                return STATUS.ERROR_COM_PORT;

            int i = 1;
            STATUS status;
            status = CommandATE();
            ReportProgress(ref status, bgw, ref e, ref i);
            if (STATUS.OK != status) return status;

            status = CommandAT();
            ReportProgress(ref status, bgw, ref e, ref i);
            if (STATUS.OK != status) return status;

            status = CommandCREG();
            ReportProgress(ref status, bgw, ref e, ref i);
            if (STATUS.OK != status) return status;

            string pin = "";
            status = CommandCPIN(pin);
            ReportProgress(ref status, bgw, ref e, ref i);
            if (STATUS.OK != status) return status;

            status = CommandCRC();
            ReportProgress(ref status, bgw, ref e, ref i);
            if (STATUS.OK != status) return status;

            bool identify = true;
            status = CommandCLIP(identify);
            ReportProgress(ref status, bgw, ref e, ref i);
            if (STATUS.OK != status) return status;

            bool PDU_mode = false;
            status = CommandCMGF(PDU_mode);
            ReportProgress(ref status, bgw, ref e, ref i);
            if (STATUS.OK != status) return status;

            status = CommandCOPS(ref operatorName);
            ReportProgress(ref status, bgw, ref e, ref i);
            if (STATUS.OK != status) return status;

            byte te = 0;
            byte ta = 0;
            status = CommandIFC(te, ta);
            ReportProgress(ref status, bgw, ref e, ref i);
            if (STATUS.OK != status) return status;

            bool n = true;
            byte cmd = 1;
            byte klass = 7;
            status = CommandCCWA(n, cmd, klass);
            ReportProgress(ref status, bgw, ref e, ref i);
            if (STATUS.OK != status) return status;

            Registered = true;
            return STATUS.OK;
        }

        private void ReportProgress(ref STATUS status, System.ComponentModel.BackgroundWorker bgw, ref DoWorkEventArgs e, ref int i)
        {
            if (bgw.CancellationPending)
            {
                e.Cancel = true;
                status = STATUS.CANCELED;
                return;
            }
            bgw.ReportProgress(i);
            i = (i + 10) % 100;
        }
        #endregion

        #region Dial Number
        /// <summary>
        /// Dial Data Call 
        /// </summary>
        /// <param name="number">Phone Number</param>
        /// <returns></returns>
        public STATUS DialData(string number)
        {
            STATUS status;
            status = CommandATD(number);
            return status;
        }
        #endregion

        #region Hang Up Call
        /// <summary>
        /// Disconnect call.
        /// Automatically changes to command mode
        /// </summary>
        /// <returns>Execution status</returns>
        public STATUS HangUpCall()
        {
            STATUS status = STATUS.ERROR_COM_PORT;
            try
            {
                status = CommandAT();
                if (STATUS.OK != status)
                {
                    FormTools.Wait(SEC * 2);
                    status = CommandToCommandMode();
                    //TODO: if (STATUS.OK != status) return status;
                }
                FormTools.Wait(SEC * 2);
                status = CommandATH();
            } catch (Exception e)
            {}
            return status;
        }
        #endregion

        #region AT Commands

        /// <summary>
        /// AT Command, Expect - OK
        /// </summary>
        /// <returns>Execution STATUS</returns>
        public STATUS CommandAT()
        {
            StringBuilder sb = new StringBuilder("AT");
            string atCommand = sb.ToString();
            string expectedResult = "OK";
            return sendAT_Command(atCommand, expectedResult, MAX_NUMBER_OF_TRIES, DEFAULT_TIMEOUT);
        }

        /// <summary>
        /// ATE0 Command - Cancel Echo, Expect - OK
        /// </summary>
        /// <returns>Execution STATUS</returns>
        public STATUS CommandATE()
        {
            StringBuilder sb = new StringBuilder("ATE0");
            string atCommand = sb.ToString();
            //string output = sendData(atCommand, DEFAULT_TIMEOUT);
//            STATUS status = (output.EndsWith("OK")) ? STATUS.OK : STATUS.ERROR_AT_COMMAND;
            //return status;
            string expectedResult = "OK";
            return sendAT_Command(atCommand, expectedResult, MAX_NUMBER_OF_TRIES, DEFAULT_TIMEOUT);

        }

        /// <summary>
        /// AT+CREG? Command, Expect - OK
        /// </summary>
        /// <returns>Execution STATUS</returns>
        public STATUS CommandCREG()
        {
            StringBuilder sb = new StringBuilder("AT+CREG?");
            string atCommand = sb.ToString();
            string expectedResult = "+CREG: 0,1\r\n\r\nOK";
            return sendAT_Command(atCommand, expectedResult, MAX_NUMBER_OF_TRIES, 5*SEC);
        }

        /// <summary>
        /// Enter PIN
        /// </summary>
        /// <param name="pin">PIN CODE</param>
        /// <returns>Execution STATUS</returns>
        public STATUS CommandCPIN(string pin)
        {
            // TODO: handle pin authorization
            StringBuilder sb = new StringBuilder("AT+CPIN?");
            string atCommand = sb.ToString();
            string expectedResult = "+CPIN: READY\r\n\r\nOK";
            return sendAT_Command(atCommand, expectedResult, MAX_NUMBER_OF_TRIES, 20*SEC);
        }

        /// <summary>
        /// CRC - Cellular Result Codes
        /// Enables extended format reporting
        /// </summary>
        /// <returns>Execution STATUS</returns>
        public STATUS CommandCRC()
        {
            StringBuilder sb = new StringBuilder("AT+CRC=1");
            string atCommand = sb.ToString();
            string expectedResult = "OK";
            return sendAT_Command(atCommand, expectedResult, MAX_NUMBER_OF_TRIES, (int)(0.2 * SEC));
        }

        /// <summary>
        /// CLIP - Calling line identification presentation
        /// </summary>
        /// <param name="isEnable">CLI indication</param>
        /// <returns>Execution STATUS</returns>
        public STATUS CommandCLIP(bool isEnable)
        {
            StringBuilder sb = new StringBuilder("AT+CLIP=");
            sb.AppendFormat("{0}", (true == isEnable) ? 1: 0);
            string atCommand = sb.ToString();
            string expectedResult = "OK";
            return sendAT_Command(atCommand, expectedResult, MAX_NUMBER_OF_TRIES, 180 * SEC);
        }

        /// <summary>
        /// CMGF - Message Format
        /// </summary>
        /// <param name="isPDU">true - PDU mode, false - Text mode</param>
        /// <returns>Execution STATUS</returns>
        public STATUS CommandCMGF(bool isPDU)
        {
            StringBuilder sb = new StringBuilder("AT+CMGF=");
            sb.AppendFormat("{0}", (true == isPDU) ? 0 : 1);
            string atCommand = sb.ToString();
            string expectedResult = "OK";
            return sendAT_Command(atCommand, expectedResult, MAX_NUMBER_OF_TRIES, 5 * SEC);
        }

        /// <summary>
        /// COPS - Operator Selection
        /// </summary>
        /// <returns>Execution STATUS</returns>
        public STATUS CommandCOPS(ref string operatorName)
        {
            StringBuilder sb = new StringBuilder("AT+COPS?" + CR);
            string atCommand = sb.ToString();
            string expectedResultStart = "+COPS: 0,0,";
            string expectedResultEnd = "OK";
            string output = string.Empty;
            bool result = false;
            int tries = 0;
            while (!result && tries < MAX_NUMBER_OF_TRIES)
            {
                output = sendData(atCommand, 180 * SEC);
                result = (output.StartsWith(expectedResultStart) && output.EndsWith(expectedResultEnd));
                ++tries;
            }
            int start = output.IndexOf("\"")+1;
            int end = output.LastIndexOf("\"")- start;
            operatorName = output.Substring(start, end);
            //return sendAT_Command(atCommand, expectedResult, MAX_NUMBER_OF_TRIES, 180 * SEC);
            return ((result)? STATUS.OK: STATUS.ERROR_AT_COMMAND);
        }

        /// <summary>
        /// Call waiting
        /// </summary>
        /// <param name="n">Enables/disables the presentation of an unsolicited result code</param>
        /// <param name="cmd">Enables(1)/Disables(0) or queries(2) the service at network level</param>
        /// <param name="klass">
        ///     Represents class of information
        ///     <ul>
        ///         <li>1 - voice(telephony)</li>    
        ///         <li>2 - data</li>
        ///         <li>4 - fax</li>
        ///         <li>7 - sum of all (voice+data+fax)</li>
        ///     </ul>
        /// </param>
        /// <returns>Execution STATUS</returns>
        public STATUS CommandCCWA(bool n, byte cmd, byte klass)
        {
            StringBuilder sb = new StringBuilder("AT+CCWA=");
            sb.AppendFormat("{0},{1},{2}", (true == n) ? 1 : 0, cmd, klass);
            string atCommand = sb.ToString();
            string expectedResult = "OK";
            return sendAT_Command(atCommand, expectedResult, MAX_NUMBER_OF_TRIES, 220 * SEC);
        }

        /// <summary>
        /// DTE-Modem Local Flow Control
        /// </summary>
        /// <param name="te"></param>
        /// <param name="ta"></param>
        /// <returns>Execution STATUS</returns>
        public STATUS CommandIFC(byte te, byte ta)
        {
            StringBuilder sb = new StringBuilder("AT+IFC=");
            sb.AppendFormat("{0},{1}", te, ta);
            string atCommand = sb.ToString();
            string expectedResult = "OK";
            return sendAT_Command(atCommand, expectedResult, MAX_NUMBER_OF_TRIES, DEFAULT_TIMEOUT);

        }

        /// <summary>
        /// Dial data call
        /// </summary>
        /// <param name="number">Phone number to be dialed</param>
        /// <returns></returns>
        public STATUS CommandATD(string number)
        {
            StringBuilder sb = new StringBuilder("ATD");
            sb.Append(number);
            string atCommand = sb.ToString();
            int tries = 0;
            STATUS status = STATUS.NO_CARRIER;
            while (tries < MAX_NUMBER_OF_TRIES && STATUS.OK != status && STATUS.CONNECT_9600 != status)
            {
                status = CommandAT();
                FormTools.Wait(100);
                if (STATUS.ERROR_COM_PORT == status)
                    return status;

                if (STATUS.OK == status)
                {
                    string response;
                    cm_.ReadFullLine = true;
                    FormTools.Wait(SEC);
                    cm_.ReadData(SEC); // Clear Buffer
                    response = sendData(atCommand, 60 * SEC);
                    response = cm_.ReadData(SEC);
                    FormTools.Wait(300);
                    switch(response){
                        case "BUSY":
                            status = STATUS.BUSY;
                            //TODO: Thread.Sleep(waitTime);
                            break;
                        case "NO CARRIER":
                            status = STATUS.NO_CARRIER;
                            //TODO: Thread.Sleep(waitTime);
                            break;
                        case "CONNECT 9600":
                            status = STATUS.OK;
                            break;
                        case "OK":
                            status = STATUS.OK;
                            break;
                        default:
                            status = STATUS.ERROR_AT_COMMAND;
                            break;
                    }
                }
                ++tries;
            }    // end of while loop   
            return status;
        }

        /// <summary>
        /// ATH command
        /// </summary>
        /// <returns></returns>
        public STATUS CommandATH()
        {
            StringBuilder sb = new StringBuilder("ATH");
            string atCommand = sb.ToString();
            string expectedResult = "NO CARRIER";
            STATUS status = STATUS.ERROR_AT_COMMAND;
            int tries = 0;
            cm_.AutoEOL = true;
            string output;
            while (status != STATUS.OK && tries < MAX_NUMBER_OF_TRIES)
            {
                cm_.WriteData(atCommand);
                FormTools.Wait(SEC);
                output = cm_.ReadData(DEFAULT_TIMEOUT);
                if (output.IndexOf(expectedResult) >= 0 || output.IndexOf("OK") >= 0)
                    status = STATUS.OK;

                ++tries;
            }
            return status;
        }

        /// <summary>
        /// Send +++
        /// and wait for OK
        /// </summary>
        /// <returns></returns>
        public STATUS CommandToCommandMode()
        {
            StringBuilder sb = new StringBuilder("+++");
            string atCommand = sb.ToString();
            string expectedResult = "OK";
            return sendAT_Command(atCommand, expectedResult, MAX_NUMBER_OF_TRIES, DEFAULT_TIMEOUT);
        }

        /// <summary>
        /// Return to On Line Mode from command mode
        /// </summary>
        /// <returns></returns>
        public STATUS CommandToOnLineMode()
        {
            StringBuilder sb = new StringBuilder("ATO");
            string atCommand = sb.ToString();
            string expectedResult = "OK";
            return sendAT_Command(atCommand, expectedResult, MAX_NUMBER_OF_TRIES, DEFAULT_TIMEOUT);
        }
        #endregion

        #region Send Data to COM Port
        
        /// <summary>
        /// Send Data to COM Port
        /// </summary>
        /// <param name="data">Data to be send</param>
        /// <param name="expectedResult">Expected result to be verified</param>
        /// <param name="maxNumberOfTries">Max number of tries to send this command</param>
        /// <param name="timeout">Read Max Timeout</param>
        /// <returns>Execution status</returns>
        private STATUS sendAT_Command(string data, string expectedResult, int maxNumberOfTries, int timeout)
        {
            int tries = 0;
            STATUS status = STATUS.ERROR_AT_COMMAND;
            cm_.AutoEOL = true;
            string output;
            while (status != STATUS.OK && tries < maxNumberOfTries)
            {
                cm_.WriteData(data);
                FormTools.Wait(SEC);
                output = cm_.ReadData(timeout);
                output = output.Trim();
                //status = (0 == string.Compare(expectedResult, output, true)) ? STATUS.OK : STATUS.ERROR_AT_COMMAND;
                status = (output.IndexOf(expectedResult) >= 0) ? STATUS.OK : STATUS.ERROR_AT_COMMAND;
                if (STATUS.OK != status)
                    FormTools.Wait(SEC*3);
                ++tries;
            }
            return status;
        }

        /// <summary>
        /// Send Data once to the COM Port without verification
        /// </summary>
        /// <param name="data">Data to be sent</param>
        /// <param name="timeout">Read Timeout</param>
        /// <returns>Output</returns>
        public string sendData(string data, int timeout)
        {
            string output;
            cm_.AutoEOL = true;
            cm_.WriteData(data);
            FormTools.Wait(SEC);
            output = cm_.ReadData(timeout);
            output = output.Trim();
            return output;
        }

        public string sendDataLN(string data, int timeout)
        {
            string output;
            cm_.AutoEOL = true;
            cm_.WriteData(data);
            FormTools.Wait(SEC);
            output = ReadDataLN(timeout);
            output = output.Trim();
            return output;
        }
        #endregion

        #region Read Data
        public string ReadDataLN(int timeout)
        {
            cm_.ReadFullLine = true;
            string output = string.Empty;
            output = cm_.ReadData(timeout);
            cm_.ReadFullLine = false;
            return output;
        }
        #endregion

    }
}
