using System.Configuration;

namespace SP365.AddIn.Services
{
    public class NotificationSettings
    {
        public static NotificationSettings Default { get { if (_default == null) { _default = new NotificationSettings(); } return _default; } } private static NotificationSettings _default = null;
        // 
        public string Smtp_Host { get { return ConfigurationManager.AppSettings["Smtp.Host"]; } }
        public int Smtp_Port { get { string tmp = ConfigurationManager.AppSettings["Smtp.Port"]; return ((string.IsNullOrEmpty(tmp) == false) ? int.Parse(tmp) : (int?)null) ?? 25; } }
        public string Smtp_From { get { return ConfigurationManager.AppSettings["Smtp.From"]; } }
        // 
        public string EmailTemplate_Default_Body { get { return @"
{body}
<br/>
<p>Kind regards,
sp365.pro</p>
"; } }
    }
}
