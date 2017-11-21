using System;
using System.Linq;
using ConfigMgr = System.Configuration.ConfigurationManager;

namespace SP365.AddIn.Services
{
    public class PTVSettings
    {
        public static PTVSettings Default { get { if (_default == null) { _default = new PTVSettings(); } return _default; } } private static PTVSettings _default = null;
        public const string DefaultBaseApiHostName = @"timetableapi.ptv.vic.gov.au";
        // 
        public string BaseApiUrl { get { string val = ConfigMgr.AppSettings["PTV.BaseApiUrl"]; return ((string.IsNullOrEmpty(val) == false) ? val : ""); } }
        public string UserId { get { return ConfigMgr.AppSettings["PTV.UserId"]; } }
        public int? UserIdAsInt { get { int tmp = default(int); return (int.TryParse(this.UserId, out tmp) ? tmp : (int?)null); } }
        public string ApiKey { get { return ConfigMgr.AppSettings["PTV.ApiKey"]; } }
        public string Email { get { return ConfigMgr.AppSettings["PTV.Email"]; } }
    }
}
