using System;
using System.DirectoryServices;

namespace Zak.Setup.IIS6.contrib.Exceptions
{
    public class VirtualDirAlreadyExistException : Exception
    {
        private readonly string _dir = "";

        public VirtualDirAlreadyExistException(DirectoryEntry entry, string vDir)
        {
            _dir = entry.Path;
            if (_dir[_dir.Length - 1] != '/')
            {
                _dir = _dir + "/";
            }

            _dir = _dir + vDir;
        }

        public override string Message
        {
            get
            {
                string msg = "Virtual Dir already exist:" + _dir;
                return msg;
            }
        }
    }
}
