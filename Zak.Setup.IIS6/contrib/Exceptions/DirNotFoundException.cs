using System;

namespace Zak.Setup.IIS6.contrib.Exceptions
{
    /// <summary>
    /// Directory not found or illegal.
    /// </summary>
    public class DirNotFoundException : Exception
    {
        private readonly string _dir;

        public DirNotFoundException(string dir)
        {
            _dir = dir;
        }

        public override string Message
        {
            get
            {
                return "Directory not found:" + _dir;
            }
        }
    }
}
