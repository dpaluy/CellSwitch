using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CellSwitch
{
    public class Logger
    {
        #region Variables
        private FileStream fs;
        private StreamWriter sw;
        private bool isLogged = false;
        #endregion

        #region Constructor
        public Logger(bool log)
        {
            string logFile = "LOGFILE.TXT";
            isLogged = log;
            fs = new FileStream(logFile, FileMode.OpenOrCreate, FileAccess.Write);
            sw = new StreamWriter(fs);

        }
        
        ~Logger()
        {
            sw.Close();
            fs.Close();
        }    

        public void log(string msg, bool _addEOL)
        {
            if (false == isLogged) return;

            if (_addEOL == true)
                sw.WriteLine(msg);
            else
                sw.Write(msg);
            sw.Flush();
            sw.Close();
        }
        #endregion

        
    }
}
