using System;
using System.Linq;
using ConfigMgr = System.Configuration.ConfigurationManager;

namespace SP365.AddIn.Services
{
    public class AuthSettings
    {
        public static AuthSettings Default { get { if (_default == null) { _default = new AuthSettings(); } return _default; } } private static AuthSettings _default = null;
        // 
        public string LoginPath { get { return ConfigMgr.AppSettings["Auth.LoginPath"]; } }
        public TimeSpan? ExpireTimeSpan { get { string tmp = ConfigMgr.AppSettings["Auth.ExpireTimeSpan"]; return ((string.IsNullOrEmpty(tmp) == false) ? TimeSpan.Parse(tmp) : (TimeSpan?)null); } }
        public bool? SlidingExpiration { get { string tmp = ConfigMgr.AppSettings["Auth.SlidingExpiration"]; return ((string.IsNullOrEmpty(tmp) == false) ? bool.Parse(tmp) : (bool?)null); } }
        public string CookieDomain { get { return ConfigMgr.AppSettings["Auth.CookieDomain"]; } }
        public bool? CookieHttpOnly { get { string tmp = ConfigMgr.AppSettings["Auth.CookieHttpOnly"]; return ((string.IsNullOrEmpty(tmp) == false) ? bool.Parse(tmp) : (bool?)null); } }
        public bool? CookieAlwaysSecure { get { string tmp = ConfigMgr.AppSettings["Auth.CookieAlwaysSecure"]; return ((string.IsNullOrEmpty(tmp) == false) ? bool.Parse(tmp) : (bool?)null); } }
        public bool? EnforceUserValidationQ { get { string tmp = ConfigMgr.AppSettings["Auth.EnforceUserValidationQ"]; return ((string.IsNullOrEmpty(tmp) == false) ? bool.Parse(tmp) : (bool?)null); } }
        public bool? EnforcePasswordValidationQ { get { string tmp = ConfigMgr.AppSettings["Auth.EnforcePasswordValidationQ"]; return ((string.IsNullOrEmpty(tmp) == false) ? bool.Parse(tmp) : (bool?)null); } }
        public bool? EnableTwoFactorAuthenticationQ { get { string tmp = ConfigMgr.AppSettings["Auth.EnableTwoFactorAuthenticationQ"]; return ((string.IsNullOrEmpty(tmp) == false) ? bool.Parse(tmp) : (bool?)null); } }
        public bool? EnableAntiXsrfTokenVerificationQ { get { string tmp = ConfigMgr.AppSettings["Auth.EnableAntiXsrfTokenVerificationQ"]; return ((string.IsNullOrEmpty(tmp) == false) ? bool.Parse(tmp) : (bool?)null); } }
        public bool? EnableUserLockoutQ { get { string tmp = ConfigMgr.AppSettings["Auth.EnableUserLockoutQ"]; return ((string.IsNullOrEmpty(tmp) == false) ? bool.Parse(tmp) : (bool?)null); } }
        // 
        public string SP365AddIn_AppId { get { return ((string.IsNullOrEmpty(ConfigMgr.AppSettings["Auth.SP365AddIn.AppId"]) == false) ? ConfigMgr.AppSettings["Auth.SP365AddIn.AppId"] : (string.IsNullOrEmpty(ConfigMgr.AppSettings["ClientId"]) == false) ? ConfigMgr.AppSettings["ClientId"] : ConfigMgr.AppSettings.Get("HostedAppName")); } }
        public string SP365AddIn_Secret { get { return ((string.IsNullOrEmpty(ConfigMgr.AppSettings["Auth.SP365AddIn.Secret"]) == false) ? ConfigMgr.AppSettings["Auth.SP365AddIn.Secret"] : (string.IsNullOrEmpty(ConfigMgr.AppSettings["ClientSecret"]) == false) ? ConfigMgr.AppSettings["ClientSecret"] : ConfigMgr.AppSettings.Get("HostedAppSigningKey")); } }
        // 
        public string Facebook_AppId { get { return ConfigMgr.AppSettings["Auth.Facebook.AppId"]; } }
        public string Facebook_Secret { get { return ConfigMgr.AppSettings["Auth.Facebook.Secret"]; } }
        public string Facebook_ApiVersion { get { string tmp = (ConfigMgr.AppSettings["Auth.Facebook.ApiVersion"] ?? string.Empty).ToLower(); return ((string.IsNullOrEmpty(tmp) == true) ? "v2.8" : (tmp[0] != 'v') ? $"v{tmp}" : tmp); } }
        public string Facebook_CallbackPath { get { return ConfigMgr.AppSettings["Auth.Facebook.CallbackPath"]; } }
        // 
        public string Google_AppId { get { return ConfigMgr.AppSettings["Auth.Google.AppId"]; } }
        public string Google_Secret { get { return ConfigMgr.AppSettings["Auth.Google.Secret"]; } }
        public string Google_CallbackPath { get { return ConfigMgr.AppSettings["Auth.Google.CallbackPath"]; } }
        // 
        public string Microsoft_AppId { get { return ConfigMgr.AppSettings["Auth.Microsoft.AppId"]; } }
        public string Microsoft_Secret { get { return ConfigMgr.AppSettings["Auth.Microsoft.Secret"]; } }
        public string Microsoft_CallbackPath { get { return ConfigMgr.AppSettings["Auth.Microsoft.CallbackPath"]; } }
        // 
        public string LinkedIn_AppId { get { return ConfigMgr.AppSettings["Auth.LinkedIn.AppId"]; } }
        public string LinkedIn_Secret { get { return ConfigMgr.AppSettings["Auth.LinkedIn.Secret"]; } }
        public string[] LinkedIn_Scope { get { return (ConfigMgr.AppSettings["Auth.LinkedIn.Scope"] ?? string.Empty).Split(new char[] { ',', ';', '|', }).Select(_ => _.Trim()).Where(_ => string.IsNullOrEmpty(_) == false).ToArray(); } }
        public string LinkedIn_CallbackPath { get { return ConfigMgr.AppSettings["Auth.LinkedIn.CallbackPath"]; } }
        // 
        public string GitHub_AppId { get { return ConfigMgr.AppSettings["Auth.GitHub.AppId"]; } }
        public string GitHub_Secret { get { return ConfigMgr.AppSettings["Auth.GitHub.Secret"]; } }
        public string GitHub_CallbackPath { get { return ConfigMgr.AppSettings["Auth.GitHub.CallbackPath"]; } }
        // 
        public string Twitter_AppId { get { return ConfigMgr.AppSettings["Auth.Twitter.AppId"]; } }
        public string Twitter_Secret { get { return ConfigMgr.AppSettings["Auth.Twitter.Secret"]; } }
        public string Twitter_CallbackPath { get { return ConfigMgr.AppSettings["Auth.Twitter.CallbackPath"]; } }
    }
}
