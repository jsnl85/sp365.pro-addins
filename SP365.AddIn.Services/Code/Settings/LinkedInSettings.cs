using System;
using System.Configuration;

namespace SP365.AddIn.Services
{
    public class LinkedInSettings
    {
        public static LinkedInSettings Default { get { if (_default == null) { _default = new LinkedInSettings(); } return _default; } } private static LinkedInSettings _default = null;
        // 
        public TimeSpan Timeout { get { string tmp = ConfigurationManager.AppSettings["LinkedIn.Timeout"]; TimeSpan ret = default(TimeSpan), defaultRet = TimeSpan.FromSeconds(10); return ((string.IsNullOrEmpty(tmp) == false && TimeSpan.TryParse(tmp, out ret)) ? ret : (TimeSpan?)null) ?? defaultRet; } }
    }
}
