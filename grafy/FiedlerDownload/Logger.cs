using System;
using System.Text;
using System.IO;

namespace Fiedler
{
    public class Logger
    {
        private string _logFile;
        
        public Logger(string logFile)
        {
            _logFile = logFile;
        }
        
        private StreamWriter _writer;

        public void Open(string logFile)
        {      
            _writer = new StreamWriter(logFile);
        }

        public void WriteMessage(string msg)
        {
            using (StreamWriter writer = new StreamWriter(_logFile,true))
            {
                writer.AutoFlush = true;
                writer.Write(msg);
            }
        }

        public static void WriteMessage(string logFile, string msg)
        {
            using (StreamWriter writer = new StreamWriter(logFile, true))
            {
                writer.AutoFlush = true;
                writer.Write(msg);
            }
        }
    }
}
