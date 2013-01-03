using System;

namespace Zak.Setup.IIS6.contrib.Exceptions
{
    public class FileNotFoundException : Exception
    {
         private readonly string _file;

        public FileNotFoundException(string file)
        {
            _file = file;
        }

        public override string Message
        {
            get
            {
                string msg = "File not found:" + _file;
                return msg;
            }
        }
    }
}
