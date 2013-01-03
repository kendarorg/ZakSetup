using System;

namespace Zak.Setup.IIS6.contrib.Exceptions
{
    public class WebsiteWithoutRootException :Exception
    {
        private readonly string _websiteName = "";

        public WebsiteWithoutRootException(string website)
        {
            _websiteName = website;
        }

        public override string Message
        {
            get
            {
                string msg = "Website has no root. Website:" + _websiteName;
                return msg;
            }
        }
    }
}
