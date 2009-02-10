using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace CellSwitch
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            try {
                Application.EnableVisualStyles();
                //Application.SetCompatibleTextRenderingDefault(false);

                CommunicationManager cm = new CommunicationManager();
                CellularProtocol cp = new CellularProtocol(cm);
                SwitchProtocol sp = new SwitchProtocol(cp);
                frmCommLog commLog = frmCommLog.Instance;
                commLog.commManager = cm;

                Application.Run(new mainForm(sp, cm));
            } catch (Exception e)
            {
                FormTools.ErrBox(e.ToString(), "Error");
            }
        }
    }
}