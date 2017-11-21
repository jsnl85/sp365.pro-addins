using System;
using System.Configuration;

namespace SP365.AddIn.Services
{
    public class PageSettings
    {
        public static PageSettings Default { get { if (_default == null) { _default = new PageSettings(); } return _default; } } private static PageSettings _default = null;
        // 
        public string Version { get { return ConfigurationManager.AppSettings["Pages.Version"] ?? (_newVersionGenerated ?? (_newVersionGenerated = Guid.Empty.ToString().Substring(0, 8))); } } private string _newVersionGenerated = null;
        public bool? AllowSignUpQ { get { string tmp = ConfigurationManager.AppSettings["Pages.AllowSignUpQ"]; return ((string.IsNullOrEmpty(tmp) == false) ? bool.Parse(tmp) : (bool?)null); } }
        public bool? UseJsMinQ { get { string tmp = ConfigurationManager.AppSettings["Pages.UseJsMinQ"]; return ((string.IsNullOrEmpty(tmp) == false) ? bool.Parse(tmp) : (bool?)null); } }
        public bool? UseCssMinQ { get { string tmp = ConfigurationManager.AppSettings["Pages.UseCssMinQ"]; return ((string.IsNullOrEmpty(tmp) == false) ? bool.Parse(tmp) : (bool?)null); } }
    }
}
