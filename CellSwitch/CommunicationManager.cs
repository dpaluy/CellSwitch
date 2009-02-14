using System;
using System.Text;
using System.Drawing;
using System.IO.Ports;
using System.Windows.Forms;

namespace CellSwitch
{
    public class CommunicationManager
    {
        #region Enums
        /// <summary>
        /// enumeration to hold our transmission types
        /// </summary>
        public enum TransmissionType { Text, Hex }

        /// <summary>
        /// enumeration to hold our message types
        /// </summary>
        public enum MessageType { Incoming, Outgoing, Normal, Warning, Error };
        #endregion

        #region Variables
        private bool _addEOL;
        private string _baudRate = string.Empty;
        private string _parity = string.Empty;
        private string _stopBits = string.Empty;
        private string _dataBits = string.Empty;
        private string _portName = string.Empty;
        private bool _readFullLine;
        private TransmissionType _transType;
        private RichTextBox _displayWindow = null;
        private Color[] MessageColor = { Color.Blue, Color.Green, Color.Black, Color.Orange, Color.Red };
        private SerialPort _port = null;
        #endregion

        #region Handlers
        //public delegate void DataReceiveHandler(string data);
        //public event DataReceiveHandler DataReceiveEvent;

        //public delegate void ErrorHandler(string message);
        //public event ErrorHandler ErrorEvent;
        #endregion

        #region Properties

        /// <summary>
        /// Automatic EOL
        /// </summary>
        public bool AutoEOL
        {
            get { return _addEOL;  }
            set { _addEOL = value; }
        }

        /// <summary>
        /// Return port status
        /// </summary>
        public bool isPortOpen
        {
            get { return _port != null && _port.IsOpen; }
        }

        /// <summary>
        /// Property to hold the BaudRate
        /// </summary>
        public string BaudRate
        {
            get { return _baudRate; }
            set { _baudRate = value; }
        }

        /// <summary>
        /// property to hold the Parity
        /// </summary>
        public string Parity
        {
            get { return _parity; }
            set { _parity = value; }
        }

        /// <summary>
        /// Property to hold the ReadFullLine
        /// </summary>
        public bool ReadFullLine
        {
            get { return _readFullLine; }
            set { _readFullLine = value; }
        }

        /// <summary>
        /// property to hold the StopBits
        /// </summary>
        public string StopBits
        {
            get { return _stopBits; }
            set { _stopBits = value; }
        }

        /// <summary>
        /// property to hold the DataBits
        /// </summary>
        public string DataBits
        {
            get { return _dataBits; }
            set { _dataBits = value; }
        }

        /// <summary>
        /// property to hold the PortName
        /// </summary>
        public string PortName
        {
            get { return _portName; }
            set { _portName = value; }
        }

        /// <summary>
        /// property to hold our TransmissionType
        /// </summary>
        public TransmissionType CurrentTransmissionType
        {
            get { return _transType; }
            set { _transType = value; }
        }

        /// <summary>
        /// property to hold our display window
        /// </summary>
        public RichTextBox DisplayWindow
        {
            get { return _displayWindow; }
            set { _displayWindow = value; }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Empty Constructor
        /// </summary>
        public CommunicationManager()
        {
            InitializeParameters(string.Empty, string.Empty, string.Empty, string.Empty, "COM1", null);
        }

        /// <summary>
        /// Constructor to set the properties of our Manager Class
        /// </summary>
        /// <param name="baud">Desired BaudRate</param>
        /// <param name="par">Desired Parity</param>
        /// <param name="sBits">Desired StopBits</param>
        /// <param name="dBits">Desired DataBits</param>
        /// <param name="name">Desired PortName</param>
        public CommunicationManager(string baud, string par, string sBits, string dBits, string name, RichTextBox rtb)
        {
            InitializeParameters(baud, par, sBits, dBits, name, rtb);
        }

        private void InitializeParameters(string baud, string par, string sBits, string dBits, string name, RichTextBox rtb)
        {
            BaudRate = baud;
            Parity = par;
            StopBits = sBits;
            DataBits = dBits;
            PortName = name;
            DisplayWindow = rtb;
            _addEOL = true;
            _readFullLine = false;
        }
        #endregion

        #region Port Start Stop
        public bool Start()
        {
            Stop();

            _port = new SerialPort();
            SetComPortProperties();
            //_port.DataReceived += new SerialDataReceivedEventHandler(_port_DataReceived);
            //_port.ErrorReceived += new SerialErrorReceivedEventHandler(_port_ErrorReceived);
            _port.Open();
            //display message
            DisplayData(MessageType.Normal, "Port opened at " + DateTime.Now + "\n");
            return _port.IsOpen;
        }

        public void Stop()
        {
            if (_port != null && _port.IsOpen)
            {
                _port.Close();
            }
            if (_port != null)
            {
                _port.Dispose();
                _port = null;
            }
        }

        private void SetComPortProperties()
        {
            //set the properties of our SerialPort Object
            _port.BaudRate = int.Parse(_baudRate);    //BaudRate
            _port.DataBits = int.Parse(_dataBits);    //DataBits
            _port.StopBits = (StopBits)Enum.Parse(typeof(StopBits), _stopBits);    //StopBits
            _port.Parity = (Parity)Enum.Parse(typeof(Parity), _parity);    //Parity
            _port.PortName = _portName;   //PortName
            _port.ReadTimeout = 500;
            _port.WriteTimeout = 500;
            _port.Handshake = Handshake.None;
        }
        #endregion

        #region WriteData
        public void WriteData(string msg)
        {
            if (_port == null || !(_port.IsOpen == true))
            {
                DisplayData(MessageType.Error, "\nOpen Port before sending data!");
                return;
            }
            switch (CurrentTransmissionType)
            {
                case TransmissionType.Text:
                    //send the message to the port
                    _port.Write(msg);
                    FormTools.Wait(100);
                    SendEndOfLine();
                    FormTools.Wait(100);
                    //display the message
                    DisplayData(MessageType.Outgoing, "\n" + msg + " ");
                    break;
                case TransmissionType.Hex:
                    try
                    {
                        //convert the message to byte array
                        byte[] newMsg = HexToByte(msg);
                        //send the message to the port
                        _port.Write(newMsg, 0, newMsg.Length);
                        SendEndOfLine();
                        //convert back to hex and display
                        DisplayData(MessageType.Outgoing, "\n" + ByteToHex(newMsg) + " ");
                    }
                    catch (FormatException ex)
                    {
                        //display error message
                        DisplayData(MessageType.Error, ex.Message + "\n");
                    }
                    finally
                    {
                        if (null != _displayWindow)
                            _displayWindow.SelectAll();
                    }
                    break;
                default:
                    //send the message to the port
                    _port.Write(msg);
                    SendEndOfLine();
                    //display the message
                    DisplayData(MessageType.Outgoing, "\n" + msg + " ");
                    break;
            }
        }

        /// <summary>
        /// Method to send END_OF_LINE (0x0D) if connection is open
        /// </summary>
        public void SendEndOfLine()
        {
            byte[] end_of_line = { 0x0D };
            if ((_port.IsOpen == true) && (true == _addEOL))
                _port.Write(end_of_line, 0, 1);
        }
        #endregion

        #region Data Received
        public string ReadData(int timeout)
        {
            const int bufferSize = 1024;
            byte[] data = null;
            string msg = string.Empty;
            try
            {
                _port.ReadTimeout = timeout;
                if (ReadFullLine)
                {
                    msg = _port.ReadLine();
                }
                else
                {
                    data = new byte[bufferSize];
                    int iret = _port.Read(data, 0, bufferSize);
                    msg = System.Text.Encoding.ASCII.GetString(data, 0, iret);
                }

                msg = msg.Trim();
                if (msg.Length > 0)
                {
                    if (!ReadFullLine && CurrentTransmissionType == TransmissionType.Hex)
                    {
                        DisplayData(MessageType.Incoming, ByteToHex(data));
                    }
                    else
                    {
                        DisplayData(MessageType.Incoming, msg);
                    }
                }
            } catch (Exception e){
                msg = "Error: " + e.ToString();
            }
            return msg;
        }

        //private void _port_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        //{
        //    DisplayData(MessageType.Error, e.ToString() + "\n");
        //    RaiseErrorEvent(e.ToString());
        //}

        //private void _port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        //{
        //    const int bufferSize = 1024;
        //    byte[] data = null;
        //    string msg = string.Empty;
        //    if (ReadFullLine)
        //    {
        //        msg = _port.ReadLine();
        //    }
        //    else
        //    {
        //        data = new byte[bufferSize];
        //        int iret = _port.Read(data, 0, bufferSize);
        //        msg = System.Text.Encoding.ASCII.GetString(data, 0, iret);
        //    }

        //    msg = msg.Trim();
        //    if (msg.Length > 0)
        //    {
        //        if (!ReadFullLine && CurrentTransmissionType == TransmissionType.Hex)
        //        {
        //            DisplayData(MessageType.Incoming, ByteToHex(data));
        //        }
        //        else
        //        {
        //            DisplayData(MessageType.Incoming, msg);
        //        }
        //        RaiseDataRecv(msg);
        //    }
        //}

        //private void RaiseDataRecv(string data)
        //{
        //    if (DataReceiveEvent != null)
        //        DataReceiveEvent(data);
        //}

        //private void RaiseErrorEvent(string message)
        //{
        //    if (ErrorEvent != null)
        //        ErrorEvent(message);
        //}
        #endregion

        #region HexToByte
        /// <summary>
        /// method to convert hex string into a byte array
        /// </summary>
        /// <param name="msg">string to convert</param>
        /// <returns>a byte array</returns>
        private byte[] HexToByte(string msg)
        {
            //remove any spaces from the string
            msg = msg.Replace(" ", "");
            //create a byte array the length of the
            //divided by 2 (Hex is 2 characters in length)
            byte[] comBuffer = new byte[msg.Length / 2];
            //loop through the length of the provided string
            for (int i = 0; i < msg.Length; i += 2)
                //convert each set of 2 characters to a byte
                //and add to the array
                comBuffer[i / 2] = (byte)Convert.ToByte(msg.Substring(i, 2), 16);
            //return the array
            return comBuffer;
        }
        #endregion

        #region ByteToHex
        /// <summary>
        /// method to convert a byte array into a hex string
        /// </summary>
        /// <param name="comByte">byte array to convert</param>
        /// <returns>a hex string</returns>
        private string ByteToHex(byte[] comByte)
        {
            if (comByte == null)
                return string.Empty;

            //create a new StringBuilder object
            StringBuilder builder = new StringBuilder(comByte.Length * 3);
            //loop through each byte in the array
            foreach (byte data in comByte)
                //convert the byte to a string and add to the stringbuilder
                builder.Append(Convert.ToString(data, 16).PadLeft(2, '0').PadRight(3, ' '));
            //return the converted value
            return builder.ToString().ToUpper();
        }
        #endregion

        #region DisplayData
        /// <summary>
        /// method to display the data to & from the port
        /// on the screen
        /// </summary>
        /// <param name="type">MessageType of the message</param>
        /// <param name="msg">Message to display</param>
        [STAThread]
        private void DisplayData(MessageType type, string msg)
        {
            if (null != _displayWindow)
                _displayWindow.Invoke(new EventHandler(delegate
                    {
                        _displayWindow.SelectedText = string.Empty;
                        _displayWindow.SelectionFont = 
                            new Font(_displayWindow.SelectionFont, FontStyle.Bold);
                        _displayWindow.SelectionColor = MessageColor[(int)type];
                        _displayWindow.AppendText(msg);
                        _displayWindow.ScrollToCaret();
                    }));
        }
        #endregion

        #region SetParityValues
        public void SetParityValues(object obj)
        {
            foreach (string str in Enum.GetNames(typeof(Parity)))
            {
                ((ComboBox)obj).Items.Add(str);
            }
        }
        #endregion

        #region SetStopBitValues
        public void SetStopBitValues(object obj)
        {
            foreach (string str in Enum.GetNames(typeof(StopBits)))
            {
                ((ComboBox)obj).Items.Add(str);
            }
        }
        #endregion

        #region GetPortNameValues
        public static string[] GetComPortNames()
        {
            string[] ports = SerialPort.GetPortNames();
            Array.Sort(ports);
            return ports;
        }
        #endregion
    }
}
