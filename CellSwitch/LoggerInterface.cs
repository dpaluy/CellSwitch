using System;
using System.Collections.Generic;
using System.Text;

namespace CellSwitch
{
    public interface LoggerInterface
    {
        void logIncoming(string msg);
        void logOutgoing(string msg);
        void logNormal(string msg);
        void logWarning(string msg);
        void logError(string msg);
    }
}
