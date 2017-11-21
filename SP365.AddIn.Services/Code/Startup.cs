using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Owin;
using Owin.Security.Providers.LinkedIn;
using SP365.AddIn.Services.DataAccess;
using SP365.AddIn.Services.DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[assembly: OwinStartupAttribute(typeof(SP365.AddIn.Services.Startup))]
namespace SP365.AddIn.Services
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            AuthSettings settings = new AuthSettings();

            #region Register Route Mappings
            PTVService.RegisterRouteMapping(app);
            #endregion Register Route Mappings

            #region Configure the Service instances, User manager and Signin manager to use a single instance per request
            app.CreatePerOwinContext(ApplicationDbContext.Create);
            app.CreatePerOwinContext<ApplicationUserManager>(ApplicationUserManager.Create);
            app.CreatePerOwinContext<ApplicationSignInManager>(ApplicationSignInManager.Create);
            #endregion Configure the Service instances, User manager and Signin manager to use a single instance per request

            #region Configure Owin to use Authentication Cookie(s) (default behaviour)
            CookieAuthenticationOptions cookieOptions = new CookieAuthenticationOptions
            {
                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
                Provider = new CookieAuthenticationProvider
                {
                    OnValidateIdentity = SecurityStampValidator.OnValidateIdentity<ApplicationUserManager, ApplicationUser>(
                        validateInterval: TimeSpan.FromMinutes(30),
                        regenerateIdentity: delegate (ApplicationUserManager manager, ApplicationUser user)
                        {
                            return user.GenerateUserIdentityAsync(manager);
                        }
                    )
                }
            };
            if (string.IsNullOrEmpty(settings.LoginPath) == false) { cookieOptions.LoginPath = new PathString(settings.LoginPath); }
            if (settings.ExpireTimeSpan != null) { cookieOptions.ExpireTimeSpan = settings.ExpireTimeSpan.Value; }
            if (settings.SlidingExpiration != null) { cookieOptions.SlidingExpiration = settings.SlidingExpiration.Value; }
            if (string.IsNullOrEmpty(settings.CookieDomain) == false) { cookieOptions.CookieDomain = settings.CookieDomain; }
            if (settings.CookieHttpOnly != null) { cookieOptions.CookieHttpOnly = (settings.CookieHttpOnly ?? true); } // NOTE: set to 'false' to allow javascript to send Cookie information (need to send Cookie information) through to remote CORS-enabled WCF services
            if (settings.CookieAlwaysSecure != null) { cookieOptions.CookieSecure = ((settings.CookieAlwaysSecure == null) ? CookieSecureOption.SameAsRequest : (settings.CookieAlwaysSecure == true) ? CookieSecureOption.Always : CookieSecureOption.Never); }
            // Enable the application to use a cookie to store information for the signed in user and to use a cookie to temporarily store information about a user logging in with a third party login provider. Configure the sign in cookie
            app.UseCookieAuthentication(cookieOptions);
            // Use a cookie to temporarily store information about a user logging in with a third party login provider
            app.UseExternalSignInCookie(DefaultAuthenticationTypes.ExternalCookie);
            if ((settings.EnableTwoFactorAuthenticationQ ?? false) == true)
            {
                // Enables the application to temporarily store user information when they are verifying the second factor in the two-factor authentication process.
                app.UseTwoFactorSignInCookie(DefaultAuthenticationTypes.TwoFactorCookie, TimeSpan.FromMinutes(5));
                // Enables the application to remember the second login verification factor such as phone or email. Once you check this option, your second step of verification during the login process will be remembered on the device where you logged in from. This is similar to the RememberMe option when you log in.
                app.UseTwoFactorRememberBrowserCookie(DefaultAuthenticationTypes.TwoFactorRememberBrowserCookie);
            }
            else { }
            #endregion Configure Owin to use Authentication Cookie(s) (default behaviour)

            #region Configure additional OAuth Authentication Providers
#if AUTH_GOOGLE
            if (string.IsNullOrEmpty(settings.Google_AppId) == false && string.IsNullOrEmpty(settings.Google_Secret) == false && string.IsNullOrEmpty(settings.Google_CallbackPath) == false)
            {
                GoogleOAuth2AuthenticationOptions options = new GoogleOAuth2AuthenticationOptions()
                {
                    ClientId = settings.Google_AppId,
                    ClientSecret = settings.Google_Secret,
                    Scope = { "openid", "profile", "email" }, // , "https://www.googleapis.com/auth/plus.login", "https://www.googleapis.com/auth/plus.me", "https://www.googleapis.com/auth/userinfo.email"
                    CallbackPath = new PathString(settings.Google_CallbackPath),
                    Provider = new CustomGoogleOAuth2AuthenticationProvider(),
                    //BackchannelHttpHandler = new CustomGoogleBackChannelHandler(), //BackchannelCertificateValidator = GetTrustedCertificatesValidator(),
                    SignInAsAuthenticationType = DefaultAuthenticationTypes.ExternalCookie,
                };
                app.UseGoogleAuthentication(options);
            }
#endif
#if AUTH_FACEBOOK
            if (string.IsNullOrEmpty(settings.Facebook_AppId) == false && string.IsNullOrEmpty(settings.Facebook_Secret) == false && string.IsNullOrEmpty(settings.Facebook_CallbackPath) == false)
            {
                FacebookAuthenticationOptions options = new FacebookAuthenticationOptions()
                {
                    AppId = settings.Facebook_AppId,
                    AppSecret = settings.Facebook_Secret,
                    Scope = { "email", "public_profile", }, //, "user_friends"
                    CallbackPath = new PathString(settings.Facebook_CallbackPath),
                    Provider = new CustomFacebookAuthenticationProvider(),
                    BackchannelHttpHandler = new CustomFacebookBackChannelHandler(), //BackchannelCertificateValidator = GetTrustedCertificatesValidator(),
                    UserInformationEndpoint = $@"https://graph.facebook.com/{settings.Facebook_ApiVersion}/me?fields=id,name,email,first_name,last_name,location", //$@"https://graph.facebook.com/me?fields=id,email,first_name,last_name", //,company
                    SignInAsAuthenticationType = DefaultAuthenticationTypes.ExternalCookie,
                };
                app.UseFacebookAuthentication(options);
            }
#endif
#if AUTH_MICROSOFT
            if (string.IsNullOrEmpty(settings.Microsoft_AppId) == false && string.IsNullOrEmpty(settings.Microsoft_Secret) == false && string.IsNullOrEmpty(settings.Microsoft_CallbackPath) == false)
            {
                MicrosoftAccountAuthenticationOptions options = new MicrosoftAccountAuthenticationOptions()
                {
                    ClientId = settings.Microsoft_AppId,
                    ClientSecret = settings.Microsoft_Secret,
                    CallbackPath = new PathString(settings.Microsoft_CallbackPath),
                    Scope = { "User.Read", }, // "wl.basic", "wl.emails"
                    Provider = new CustomMicrosoftAccountAuthenticationProvider(),
                    //BackchannelHttpHandler = new CustomMicrosoftBackChannelHandler(), //BackchannelCertificateValidator = GetTrustedCertificatesValidator(),
                    SignInAsAuthenticationType = DefaultAuthenticationTypes.ExternalCookie,
                };
                app.UseMicrosoftAccountAuthentication(options);
            }
#endif
#if AUTH_LINKEDIN
            if (string.IsNullOrEmpty(settings.LinkedIn_AppId) == false && string.IsNullOrEmpty(settings.LinkedIn_Secret) == false && string.IsNullOrEmpty(settings.LinkedIn_CallbackPath) == false)
            {
                LinkedInAuthenticationOptions options = new LinkedInAuthenticationOptions()
                {
                    ClientId = settings.LinkedIn_AppId,
                    ClientSecret = settings.LinkedIn_Secret,
                    CallbackPath = new PathString(settings.LinkedIn_CallbackPath),
                    //Scope = { "r_basicprofile", "r_emailaddress", }, //Scope = (IList<string>)(settings.LinkedIn_Scope ?? new string[] { "r_basicprofile" }).ToList(),
                    Provider = new CustomLinkedInAuthenticationProvider(),
                    //BackchannelHttpHandler = new CustomLinkedInBackChannelHandler(), //BackchannelCertificateValidator = GetTrustedCertificatesValidator(),
                    SignInAsAuthenticationType = DefaultAuthenticationTypes.ExternalCookie,
                };
                app.UseLinkedInAuthentication(options);
            }
#endif
#if AUTH_TWITTER
            if (string.IsNullOrEmpty(settings.Twitter_AppId) == false && string.IsNullOrEmpty(settings.Twitter_Secret) == false && string.IsNullOrEmpty(settings.Twitter_CallbackPath) == false)
            {
                TwitterAuthenticationOptions options = new TwitterAuthenticationOptions()
                {
                    ConsumerKey = settings.Twitter_AppId,
                    ConsumerSecret = settings.Twitter_Secret,
                    CallbackPath = new PathString(settings.Twitter_CallbackPath),
                    //Scope = { "read", "email", },
                    Provider = new CustomTwitterAuthenticationProvider(),
                    BackchannelCertificateValidator = GetTrustedCertificatesValidator(),
                    SignInAsAuthenticationType = DefaultAuthenticationTypes.ExternalCookie,
                };
                app.UseTwitterAuthentication(options);
            }
#endif
#if AUTH_GITHUB
            if (string.IsNullOrEmpty(settings.GitHub_AppId) == false && string.IsNullOrEmpty(settings.GitHub_Secret) == false && string.IsNullOrEmpty(settings.GitHub_CallbackPath) == false)
            {
                GitHubAuthenticationOptions options = new GitHubAuthenticationOptions()
                {
                    ClientId = settings.GitHub_AppId,
                    ClientSecret = settings.GitHub_Secret,
                    CallbackPath = new PathString(settings.GitHub_CallbackPath),
                    Scope = { "user", },
                    Provider = new CustomGitHubAuthenticationProvider(),
                    //BackchannelHttpHandler = new CustomGitHubBackChannelHandler(), //BackchannelCertificateValidator = GetTrustedCertificatesValidator(),
                    SignInAsAuthenticationType = DefaultAuthenticationTypes.ExternalCookie,
                };
                app.UseGitHubAuthentication(options);
            }
#endif
            #endregion Configure additional OAuth Authentication Providers
        }

        #region Helper Types

#if AUTH_GOOGLE
        public class CustomGoogleOAuth2AuthenticationProvider : GoogleOAuth2AuthenticationProvider
        {
            // auxiliary functions
            private void addClaim(System.Security.Claims.ClaimsIdentity identity, string claimName, string claimValue) { if (string.IsNullOrEmpty(claimValue) == false) { identity.AddClaim(new System.Security.Claims.Claim(claimName, claimValue)); } }
            private string getValue(Newtonsoft.Json.Linq.JObject obj, string propertyName) { Newtonsoft.Json.Linq.JToken value = null; if (obj.TryGetValue(propertyName, StringComparison.OrdinalIgnoreCase, out value) == true) { return value?.ToString(); } return null; }
            private IEnumerable<string> getValuesInPath(Newtonsoft.Json.Linq.JObject obj, string propertyPath) { IEnumerable<Newtonsoft.Json.Linq.JToken> values = obj.SelectTokens(propertyPath, false); if (values != null && values.Any() == true) { foreach (Newtonsoft.Json.Linq.JToken value in values) { yield return value.ToString(); } } }
            // 
            public override Task Authenticated(GoogleOAuth2AuthenticatedContext context)
            {
                try
                {
                    addClaim(context.Identity, "urn:tokens:googleplus:accesstoken", context.AccessToken);
                    // 
                    addClaim(context.Identity, "urn:tokens:googleplus:familyname", context.FamilyName);
                    addClaim(context.Identity, "urn:tokens:googleplus:givenname", context.GivenName);
                    addClaim(context.Identity, "urn:tokens:googleplus:avatarurl", getValuesInPath(context.User, "image.url").FirstOrDefault()); //?.Replace("sz=50", "sz=240")
                    addClaim(context.Identity, "urn:tokens:googleplus:company", getValue(context.User, "company"));
                    // 
                    //foreach (string oranisationName in getValuesInPath(context.User, "organizations.name")) { addClaim(context.Identity, "urn:tokens:googleplus:organization", oranisationName); }
                    //addClaim(context.Identity, "urn:tokens:googleplus:gender", getValue(context.User, "gender"));
                    //addClaim(context.Identity, "urn:tokens:googleplus:occupation", getValue(context.User, "occupation"));
                    //addClaim(context.Identity, "urn:tokens:googleplus:aboutme", getValue(context.User, "aboutme"));
                    //addClaim(context.Identity, "urn:tokens:googleplus:language", getValue(context.User, "language"));
                }
                catch (Exception ex) { Logger.Error(ex, LogCategory.Claims, "There was an issue reading the information from the Google Authentication context."); }
                // 
                return base.Authenticated(context);
            }
        }
#endif
#if AUTH_FACEBOOK
        public class CustomFacebookAuthenticationProvider : FacebookAuthenticationProvider
        {
            // auxiliary functions
            private void addClaim(System.Security.Claims.ClaimsIdentity identity, string claimName, string claimValue) { if (string.IsNullOrEmpty(claimValue) == false) { identity.AddClaim(new System.Security.Claims.Claim(claimName, claimValue)); } }
            private string getValue(Newtonsoft.Json.Linq.JObject obj, string propertyName) { Newtonsoft.Json.Linq.JToken value = null; if (obj.TryGetValue(propertyName, StringComparison.OrdinalIgnoreCase, out value) == true) { return value?.ToString(); } return null; }
            private IEnumerable<string> getValuesInPath(Newtonsoft.Json.Linq.JObject obj, string propertyPath) { IEnumerable<Newtonsoft.Json.Linq.JToken> values = obj.SelectTokens(propertyPath, false); if (values != null && values.Any() == true) { foreach (Newtonsoft.Json.Linq.JToken value in values) { yield return value.ToString(); } } }
            // 
            public override Task Authenticated(FacebookAuthenticatedContext context)
            {
                try
                {
                    addClaim(context.Identity, "urn:tokens:facebook:accesstoken", context.AccessToken);
                    //addClaim(context.Identity, "urn:tokens:facebook:accesstokenexpiration", context.ExpiresIn?.ToString());
                    // 
                    addClaim(context.Identity, "urn:tokens:facebook:familyname", getValue(context.User, "last_name"));
                    addClaim(context.Identity, "urn:tokens:facebook:givenname", getValue(context.User, "first_name"));
                    addClaim(context.Identity, "urn:tokens:facebook:avatarurl", $@"https://graph.facebook.com/{context.Id}/picture"); //?width=240&height=240
                    addClaim(context.Identity, "urn:tokens:facebook:company", getValue(context.User, "company"));
                    // 
                    //addClaim(context.Identity, "urn:tokens:facebook:username", context.UserName);
                    //addClaim(context.Identity, "urn:tokens:facebook:fullname", context.Name);
                    //addClaim(context.Identity, "urn:tokens:facebook:location", getValue(context.User, "location"));
                }
                catch (Exception ex) { Logger.Error(ex, LogCategory.Claims, "There was an issue reading the information from the Facebook Authentication context."); }
                // 
                return base.Authenticated(context);
            }
        }
        public class CustomFacebookBackChannelHandler : HttpClientHandler // WebRequestHandler
        {
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                if (request.RequestUri.AbsolutePath.Contains("/oauth") == false)
                {
                    request.RequestUri = new Uri(request.RequestUri.AbsoluteUri.Replace("?access_token", "&access_token"));
                }
                return await base.SendAsync(request, cancellationToken);
            }
        }
#endif
#if AUTH_MICROSOFT
        public class CustomMicrosoftAccountAuthenticationProvider : MicrosoftAccountAuthenticationProvider
        {
            // auxiliary functions
            private void addClaim(System.Security.Claims.ClaimsIdentity identity, string claimName, string claimValue) { if (string.IsNullOrEmpty(claimValue) == false) { identity.AddClaim(new System.Security.Claims.Claim(claimName, claimValue)); } }
            private string getValue(Newtonsoft.Json.Linq.JObject obj, string propertyName) { Newtonsoft.Json.Linq.JToken value = null; if (obj.TryGetValue(propertyName, StringComparison.OrdinalIgnoreCase, out value) == true) { return value?.ToString(); } return null; }
            private IEnumerable<string> getValuesInPath(Newtonsoft.Json.Linq.JObject obj, string propertyPath) { IEnumerable<Newtonsoft.Json.Linq.JToken> values = obj.SelectTokens(propertyPath, false); if (values != null && values.Any() == true) { foreach (Newtonsoft.Json.Linq.JToken value in values) { yield return value.ToString(); } } }
            // 
            public override void ApplyRedirect(MicrosoftAccountApplyRedirectContext context)
            {
                context = new MicrosoftAccountApplyRedirectContext(context.OwinContext, context.Options, context.Properties, context.RedirectUri + "&display=touch"); // Mobile devices support
                // 
                base.ApplyRedirect(context);
            }
            public override Task Authenticated(MicrosoftAccountAuthenticatedContext context)
            {
                try
                {
                    addClaim(context.Identity, "urn:tokens:microsoft:accesstoken", context.AccessToken);
                    // 
                    addClaim(context.Identity, "urn:tokens:microsoft:familyname", getValue(context.User, "last_name"));
                    addClaim(context.Identity, "urn:tokens:microsoft:givenname", getValue(context.User, "first_name"));
                    addClaim(context.Identity, "urn:tokens:microsoft:avatarurl", $@"https://apis.live.net/v5.0/{context.Id}/picture"); //?width=240&height=240
                    addClaim(context.Identity, "urn:tokens:microsoft:company", getValue(context.User, "company"));
                    // 
                    //addClaim(context.Identity, "providerKey", context.Identity.AuthenticationType);
                    //addClaim(context.Identity, System.Security.Claims.ClaimTypes.Name, context.Identity.FindFirstValue(System.Security.Claims.ClaimTypes.Name));
                }
                catch (Exception ex) { Logger.Error(ex, LogCategory.Claims, "There was an issue reading the information from the Facebook Authentication context."); }
                // 
                return base.Authenticated(context);
            }
        }
#endif
#if AUTH_LINKEDIN
        public class CustomLinkedInAuthenticationProvider : LinkedInAuthenticationProvider
        {
            // auxiliary functions
            private void addClaim(System.Security.Claims.ClaimsIdentity identity, string claimName, string claimValue) { if (string.IsNullOrEmpty(claimValue) == false) { identity.AddClaim(new System.Security.Claims.Claim(claimName, claimValue)); } }
            private string getValue(Newtonsoft.Json.Linq.JObject obj, string propertyName) { Newtonsoft.Json.Linq.JToken value = null; if (obj.TryGetValue(propertyName, StringComparison.OrdinalIgnoreCase, out value) == true) { return value?.ToString(); } return null; }
            private IEnumerable<string> getValuesInPath(Newtonsoft.Json.Linq.JObject obj, string propertyPath) { IEnumerable<Newtonsoft.Json.Linq.JToken> values = obj.SelectTokens(propertyPath, false); if (values != null && values.Any() == true) { foreach (Newtonsoft.Json.Linq.JToken value in values) { yield return value.ToString(); } } }
            // 
            public override Task Authenticated(LinkedInAuthenticatedContext context)
            {
                try
                {
                    addClaim(context.Identity, "urn:tokens:linkedin:accesstoken", context.AccessToken);
                    // 
                    if (context.Identity.HasClaim(_ => _.Type == "urn:linkedin:lastname") == false) { addClaim(context.Identity, "urn:tokens:linkedin:familyname", getValue(context.User, "last-name")); }
                    if (context.Identity.HasClaim(_ => _.Type == "urn:linkedin:firstname") == false) { addClaim(context.Identity, "urn:tokens:linkedin:givenname", getValue(context.User, "first-name")); }
                    //addClaim(context.Identity, "urn:tokens:linkedin:avatarurl", ""); // $@"https://media.licdn.com/media/p/7/000/1b2/0a9/{context.Id}.jpg"
                    addClaim(context.Identity, "urn:tokens:linkedin:company", getValue(context.User, "company"));
                }
                catch (Exception ex) { Logger.Error(ex, LogCategory.Claims, "There was an issue reading the information from the Facebook Authentication context."); }
                // 
                return base.Authenticated(context);
            }
        }
#endif
#if AUTH_TWITTER
        public class CustomTwitterAuthenticationProvider : TwitterAuthenticationProvider
        {
            // auxiliary functions
            private void addClaim(System.Security.Claims.ClaimsIdentity identity, string claimName, string claimValue) { if (string.IsNullOrEmpty(claimValue) == false) { identity.AddClaim(new System.Security.Claims.Claim(claimName, claimValue)); } }
            private string getValue(Newtonsoft.Json.Linq.JObject obj, string propertyName) { Newtonsoft.Json.Linq.JToken value = null; if (obj.TryGetValue(propertyName, StringComparison.OrdinalIgnoreCase, out value) == true) { return value?.ToString(); } return null; }
            private IEnumerable<string> getValuesInPath(Newtonsoft.Json.Linq.JObject obj, string propertyPath) { IEnumerable<Newtonsoft.Json.Linq.JToken> values = obj.SelectTokens(propertyPath, false); if (values != null && values.Any() == true) { foreach (Newtonsoft.Json.Linq.JToken value in values) { yield return value.ToString(); } } }
            // 
            public override Task Authenticated(TwitterAuthenticatedContext context)
            {
                try
                {
                    addClaim(context.Identity, "urn:tokens:twitter:accesstoken", context.AccessToken);
                    addClaim(context.Identity, "urn:tokens:twitter:accesssecret", context.AccessTokenSecret);
                    // 
                    //addClaim(context.Identity, "urn:tokens:twitter:familyname", getValue(context.User, "last_name"));
                    //addClaim(context.Identity, "urn:tokens:twitter:givenname", getValue(context.User, "first_name"));
                    //addClaim(context.Identity, "urn:tokens:twitter:avatarurl", "");
                    //addClaim(context.Identity, "urn:tokens:twitter:company", getValue(context.User, "company"));
                    // 
                    //addClaim(context.Identity, "urn:tokens:twitter:userid", context.UserId);
                    //addClaim(context.Identity, "urn:tokens:twitter:screenname", context.ScreenName);
                }
                catch (Exception ex) { Logger.Error(ex, LogCategory.Claims, "There was an issue reading the information from the Facebook Authentication context."); }
                // 
                return base.Authenticated(context);
            }
        }
#endif
#if AUTH_GITHUB
        public class CustomGitHubAuthenticationProvider : GitHubAuthenticationProvider
        {
            // auxiliary functions
            private void addClaim(System.Security.Claims.ClaimsIdentity identity, string claimName, string claimValue) { if (string.IsNullOrEmpty(claimValue) == false) { identity.AddClaim(new System.Security.Claims.Claim(claimName, claimValue)); } }
            private string getValue(Newtonsoft.Json.Linq.JObject obj, string propertyName) { Newtonsoft.Json.Linq.JToken value = null; if (obj.TryGetValue(propertyName, StringComparison.OrdinalIgnoreCase, out value) == true) { return value?.ToString(); } return null; }
            private IEnumerable<string> getValuesInPath(Newtonsoft.Json.Linq.JObject obj, string propertyPath) { IEnumerable<Newtonsoft.Json.Linq.JToken> values = obj.SelectTokens(propertyPath, false); if (values != null && values.Any() == true) { foreach (Newtonsoft.Json.Linq.JToken value in values) { yield return value.ToString(); } } }
            // 
            public override Task Authenticated(GitHubAuthenticatedContext context)
            {
                try
                {
                    addClaim(context.Identity, "urn:tokens:github:accesstoken", context.AccessToken);
                    // 
                    addClaim(context.Identity, "urn:tokens:github:familyname", getValue(context.User, "last_name"));
                    addClaim(context.Identity, "urn:tokens:github:givenname", getValue(context.User, "first_name"));
                    addClaim(context.Identity, "urn:tokens:github:avatarurl", getValue(context.User, "avatar_url")); // i.e. same as: $@"https://avatars1.githubusercontent.com/u/{context.Id}?v=3"); // &s=240
                    addClaim(context.Identity, "urn:tokens:github:company", getValue(context.User, "company"));
                    // 
                    //addClaim(context.Identity, "urn:tokens:github:username", context.UserName);
                    //addClaim(context.Identity, "urn:tokens:github:login", getValue(context.User, "login"));
                    //addClaim(context.Identity, "urn:tokens:github:location", getValue(context.User, "location"));
                    //addClaim(context.Identity, "urn:tokens:github:location", getValue(context.User, "location"));
                }
                catch (Exception ex) { Logger.Error(ex, LogCategory.Claims, "There was an issue reading the information from the Facebook Authentication context."); }
                // 
                return base.Authenticated(context);
            }
        }
#endif
        public static CertificateSubjectKeyIdentifierValidator GetTrustedCertificatesValidator()
        {
            return new CertificateSubjectKeyIdentifierValidator(new[]
            {
                "A5EF0B11CEC04103A34A659048B21CE0572D7D47", // VeriSign Class 3 Secure Server CA - G2
                "0D445C165344C1827E1D20AB25F40163D8BE79A5", // VeriSign Class 3 Secure Server CA - G3
                "7FD365A7C2DDECBBF03009F34339FA02AF333133", // VeriSign Class 3 Public Primary Certification Authority - G5
                "39A55D933676616E73A761DFA16A7E59CDE66FAD", // Symantec Class 3 Secure Server CA - G4
                "5168FF90AF0207753CCCD9656462A212B859723B", // DigiCert SHA2 High Assurance Server C‎A 
                "B13EC36903F8BF4701D498261A0802EF63642BC3"  // DigiCert High Assurance EV Root CA
#if DEBUG
                , "A04DA6E64ED6227E73EC7B694ACBBfE928887BC2"// DO_NOT_TRUST_FiddlerRoot
#endif
            });
        }
        #endregion Helper Types
    }
}
