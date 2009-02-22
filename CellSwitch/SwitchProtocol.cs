using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;

namespace CellSwitch
{
    public class SwitchProtocol
    {
        #region Variables
        private const int MAX_DATA_SIZE = 998;
        //private string phonesTest = buildPhones();
        private CellularProtocol cp_;
        private bool connected_ = false;
        private string transmitterID_ = string.Empty;
        private string operatorName = string.Empty;
        private string[] phones;

        public string[] Phones
        {
            get { return phones; }
            set { phones = value; }
        }

        public string TransmitterID
        {
            get { return transmitterID_; }
        }

        public string OperatorName
        {
            get { return operatorName; }
            set { operatorName = value; }
        }

        public bool Connected
        {
            set {
                connected_ = value;
                if (!connected_) operatorName = string.Empty;
            }
            get { return connected_; }
        }

        public string DeviceID
        {
            get { return transmitterID_; }
        }

        public bool TransmitterRegistered
        {
            get { return cp_.Registered; }
            set { cp_.Registered = value; }
        }
        #endregion

        #region Constructors
        public SwitchProtocol(CellularProtocol cp)
        {
            cp_ = cp;
        }
        #endregion

        #region MODEM setup
        public STATUS modemSetup(System.ComponentModel.BackgroundWorker bgw, ref DoWorkEventArgs e)
        {
            STATUS status = STATUS.ERROR_COM_PORT;
            try
            {
                if (false == cp_.isReady && false == cp_.Ready())
                    return STATUS.ERROR_COM_PORT;
                status = cp_.register(ref operatorName, bgw, ref e);
            }
            catch (Exception) 
            {
            }
            return status;
        }
        #endregion

        #region Connect
        /// <summary>
        /// Connect to switch:
        /// Open COM PORT
        /// Register GSM
        /// CALL switch
        /// Authenticate
        /// </summary>
        /// <param name="phone">Switch Phone number</param>
        /// <returns>Execution status</returns>
        public STATUS ConnectToSwitch(string phone)
        {
            if (false == cp_.isReady && false == cp_.Ready())
                return STATUS.ERROR_COM_PORT;

            STATUS status = cp_.DialData(phone);
            if (STATUS.OK != status) return status;

            // Authorize
            
            string ready = cp_.ReadDataLN(FormTools.SEC * 10);
            if (0 != String.Compare(ready, "READY"))
            {
                if (0 == String.Compare(ready, "NO CARRIER"))
                {
                    return STATUS.NO_CARRIER;
                }
                else
                {
                    cp_.HangUpCall();
                    return STATUS.BUSY;   
                }
            }

            transmitterID_ = cp_.ReadDataLN(FormTools.SEC * 60);
           
            Connected = true;
            return STATUS.OK;
        }        

        #endregion

        #region Disconnect
        
        public void Disconnect()
        {
            if (true == this.Connected)
            {
                STATUS status = cp_.HangUpCall();
                transmitterID_ = string.Empty;
                Connected = !((STATUS.NO_CARRIER == status) || (STATUS.OK == status));
                if (true == Connected)
                {
                    FormTools.ErrBox("Disconnection failed!\n Reset transmitter!", "Disconnect");
                }
            }
        }
        #endregion

        #region Authorize
        public void Authorize()
        {
            //// Authorize Command
            //byte[] data = new byte[] { 0x11, 0x11, 0x80 };
            //Command cmd = new Command(0x21, 0, 0, data);
            //SendCommand(cmd);
            //Connected = true; //TODO
        }
        #endregion

        #region Get Phone List
        public void GetPhoneList(ref string phones)
        {
            byte[] data = new byte[] { 0x00 };
            Command cmd = new Command(0x21, 0x00, 0x00, data);
            phones = SendCommandLN(cmd, FormTools.SEC * 100);
        }
        #endregion

        #region Send Phone List

        private string setData(ref string phones)
        {
            int last_index = (phones.Length > MAX_DATA_SIZE) ? MAX_DATA_SIZE : phones.Length;
            StringBuilder sb = new StringBuilder(phones.Substring(0, last_index));
            if (phones.Length < MAX_DATA_SIZE && (sb.Length > 0) )
                sb.Append("$$");
            string data = sb.ToString();
            phones = phones.Substring(last_index);
            return data;
        }

        public void SendPhoneList(string phones)
        {
            string output = "";
            int tries = 0;

            string data = setData(ref phones);
            Command cmd = new Command(0x23, 0x00, 0x00, data);
            StringBuilder sb = new StringBuilder("S");
            sb.Append(cmd.ToString());
            data = sb.ToString();
            while (data.Length > 0 && tries < 3)
            {
                output = SendDataLN(data, FormTools.SEC * 20);
                if (output.IndexOf("ACK") == 0)
                {
                    data = setData(ref phones);
                    tries = 0;
                }
                else
                    tries++;
            }
        }

        private static string buildPhones()
        {
            string phones;
            StringBuilder sb = new StringBuilder();
            for (int i = 1; i < 198; i++)
            {
                StringBuilder phone = new StringBuilder();
                phone.Append(i);
                phone.Append("0541234567 ");
                sb.Append(phone.ToString());
            }
            sb.Append("0546618046 ");
            phones = sb.ToString();
            return phones;
        }
        #endregion

        #region Send Command
        private string SendCommand(Command cmd, int timeout)
        {
            StringBuilder sb = new StringBuilder("S");
            sb.Append(cmd.ToString());
            sb.Append("$$"); // End of packet
            string output = string.Empty;
            output = cp_.sendData(sb.ToString(), timeout);
            if (output.LastIndexOf("$$") > 0)
                output = output.Substring(0, output.Length - 2);
            return output;
        }

        private string SendCommandLN(Command cmd, int timeout)
        {
            StringBuilder sb = new StringBuilder("S");
            sb.Append(cmd.ToString());
            sb.Append("$$"); // End of packet
            string output = string.Empty;
            output = cp_.sendDataLN(sb.ToString(), timeout);
            
            return output;
        }

        private string SendDataLN(string data, int timeout)
        {
            string output = string.Empty;
            output = cp_.sendDataLN(data, timeout);
            return output;
        }
        #endregion       
        
    }
    #region Command
    public class Command
    {
        #region Variables
        
        /// <summary>
        /// Length size in bytes
        /// </summary>
        public const int LENGTH_SIZE = 2;
        /// <summary>
        /// MAX DATA SIZE in bytes
        /// </summary>
        public const int MAX_DATA_SIZE = 0xFFF0;

        private string str = string.Empty;
        private byte opCode_;
        private byte ad1_;
        private byte ad2_;
        private short length_;
        private byte[] data_;
        private byte cs_;
        #endregion

        #region Constructor
        public Command(){}

        public Command(byte opCode, byte ad1, byte ad2, byte[] data) 
        {
            setCommand(opCode, ad1, ad2, data);
        }

        public Command(byte opCode, byte ad1, byte ad2, string datas)
        {
            byte[] data = new byte[] {0};
            setCommand(opCode, ad1, ad2, data);
            this.str = datas;
        }
        #endregion

        #region Properties
        /// <summary>
        /// OpCode - Hex command - 1 byte
        /// </summary>
        public byte OpCode{
            get { return opCode_; }
            set { opCode_ = value; }
        }

        /// <summary>
        /// Address1 - 1 byte
        /// </summary>
        public byte Address1
        {
            get { return ad1_; }
            set { ad1_ = value; }
        }

        /// <summary>
        /// Address2 - 1 byte
        /// </summary>
        public byte Address2
        {
            get { return ad2_; }
            set { ad2_ = value; }
        }

        /// <summary>
        /// Packet length represented in bytes
        /// </summary>
        public byte[] ByteLength
        {
            get 
            { 
                byte[] buf = SwitchBytesLittleEndian(BitConverter.GetBytes(length_));
                return buf;
            }
            set { length_ = BitConverter.ToInt16(SwitchBytesLittleEndian(value), 0); }
        }

       public short Length
        {
            get { return length_; }
            set { length_ = value; }
        }

        /// <summary>
        /// Packet data - variable length
        /// Represented as an array of bytes
        /// </summary>
        public byte[] Data
        {
            get { return data_; }
            set {
                data_ = new byte[value.Length];
                Array.Copy(value, data_, value.Length);
                Length = (short)value.Length;
            }
        }

        /// <summary>
        /// Checksum
        /// </summary>
        public byte CS
        {
            get { return cs_; }
            set { cs_ = value; }
        }
        
        #endregion
        
        #region Methods
        /// <summary>
        /// Set new values to command
        /// </summary>
        /// <param name="opCode">Op Code</param>
        /// <param name="ad1">Address 1</param>
        /// <param name="ad2">Address 2</param>
        /// <param name="data">Data</param>
        public void setCommand(byte opCode, byte ad1, byte ad2, byte[] data)
        {
            this.opCode_ = opCode;
            this.ad1_ = ad1;
            this.ad2_ = ad2;
            this.data_ = data;
            Length = (short)data.Length;
            //CalculateCS();
        }

        /// <summary>
        /// Build byte array for a command
        /// </summary>
        /// <returns>Command byte array without CS</returns>
        private byte[] getCommand(){
            int commandLength = (Length + 5);
            byte[] buf = new byte[commandLength];
            int index = 0;
            buf[index++] = OpCode;
            buf[index++] = Address1;
            buf[index++] = Address2;
            buf[index++] = ByteLength[0];
            buf[index++] = ByteLength[1];
            Array.Copy(data_, data_.GetLowerBound(0), buf, index, Length);
            index += Length;
            
            return buf;
        }

        private string getCommandStr()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("23");
            sb.Append(str);
            string buf = sb.ToString();
            return buf;
        }
        /// <summary>
        /// Fixes array of bytes while in Little Endian
        /// </summary>
        /// <param name="b">Byte array to be changed</param>
        /// <returns>Byte Array in Big Endian</returns>
        public static byte[] SwitchBytesLittleEndian(byte[] b)
        {
            byte[] buf = new byte[b.Length];
            Array.Copy(b, buf, b.Length);
            // TODO: handle any buffer length
            if (true == BitConverter.IsLittleEndian)
            {
                buf[0] ^= buf[1];
                buf[1] ^= buf[0];
                buf[0] ^= buf[1];
            }
            return buf;
        }

        /// <summary>
        /// Calculate Checksum
        /// CS = Sum of all packet bytes
        /// </summary>
        public void CalculateCS()
        {
            byte cs = 0;
            CS = 0;
            byte[] buf = getCommand();
            foreach(byte b in buf){
                cs += b;
            }
            CS = cs;
        }

        /// <summary>
        /// Return HEX string of command
        /// </summary>
        /// <returns>HEX string command</returns>
        public override string ToString()
        {
            if (this.str.Length > 0)
            {
                return getCommandStr();
            }
            else
            {
                byte[] buf = getCommand();
                string hex = BitConverter.ToString(buf);
                hex = hex.Replace("-", "");
                return hex;
            } 
        }
        #endregion
    };
    #endregion
}
