using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using SP365.AddIn.Services.DataAccess.Models;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace SP365.AddIn.Services
{
    public partial class RegisterExternalLogin : BaseUnsecuredPage
    {
        protected string ReturnUrl { get { var ret = HttpUtility.UrlDecode(this.Request.QueryString["ReturnUrl"] ?? "/"); return ((new Regex(@"^[/]Login[/]?", RegexOptions.IgnoreCase).IsMatch(ret) == true) ? "/" : ret); } }
        protected string Action { get { return this.Request.QueryString["action"]; } }
        protected string ErrorMessage { get { return this.Request.QueryString["error"]; } }
        protected string ProviderName { get { return (string)ViewState["ProviderName"] ?? this.Page.Request.QueryString["Provider"] ?? this.Page.Request.QueryString["ProviderName"] ?? string.Empty; } private set { ViewState["ProviderName"] = value; } }
        protected string ProviderAccountKey { get { return (string)ViewState["ProviderAccountKey"] ?? string.Empty; } private set { ViewState["ProviderAccountKey"] = value; } }

        // NOTE: this page can be used to 'Challenge' a Sign-in / Create a new Account / Add a Login to an existing Account (by passing the Action=Challenge parameter)
        // NOTE: this page CANNOT be used to 'Remove' a Sign-in

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            // 
            bool isChallengeQ = (string.IsNullOrEmpty(this.Action) == false && this.Action.Equals(@"Challenge", StringComparison.OrdinalIgnoreCase) == true);
            if (isChallengeQ == true)
            {
                if (string.IsNullOrEmpty(this.ProviderName) == true) { this.RedirectToErrorPage("Please provide the provider to sign-in with."); }
                RegisterExternalLogin.ChallengeProvider(HttpContext.Current, this.ProviderName, this.ReturnUrl);
                this.RedirectToErrorPage($@"Could not sign-in with the provider '{this.ProviderName}'.");
            }
        }
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            // 
            // Process the result from an auth provider in the request
            this.ProviderName = this.ProviderName; // to save against the ViewState (only useful when presenting the Page Form)
            if (string.IsNullOrEmpty(this.ProviderName) == true) { redirectOnFail(); return; }
            // 
            if (this.IsPostBack == false)
            {
                var loginInfo = this.UsersLogic.AuthenticationManager.GetExternalLoginInfo();
                if (loginInfo == null) { redirectOnFail(); return; }
                // 
                var user = this.UsersLogic.UserManager.Find(loginInfo.Login);
                if (user != null) // The provider OAuth identity was recognised in our database, and it was already associated with an account. Go ahead, and SignIn with that account.
                {
                    setAccessTokensFromClaims(user, loginInfo);
                    setPersonalFieldsFromClaims(user, loginInfo);
                    this.UsersLogic.UserManager.Update(user);
                    // 
                    this.UsersLogic.SignIn(user, isPersistentQ: false, rememberBrowserQ: false, updateLastLogonDateQ: true);
                    IdentityHelper.RedirectToReturnUrl(this.ReturnUrl, this.Response);
                }
                else if (this.User.Identity.IsAuthenticated == true) // There was no account found using this OAuth identity, but the user is signed-in already. Go ahead, and update the account to have this additional OAuth identity.
                {
                    // Apply Xsrf check when linking
                    var verifiedloginInfo = this.UsersLogic.AuthenticationManager.GetExternalLoginInfo(IdentityHelper.XsrfKey, this.UserId);
                    if (verifiedloginInfo == null) { redirectOnFail(); return; }
                    // 
                    ApplicationUser existingUser = this.UsersLogic.UserManager.FindById(this.UserId);
                    if (existingUser == null) { throw new ArgumentNullException(nameof(existingUser)); }
                    setAccessTokensFromClaims(existingUser, loginInfo);
                    setPersonalFieldsFromClaims(existingUser, loginInfo);
                    this.UsersLogic.UserManager.Update(existingUser);
                    // 
                    var result = this.UsersLogic.UserManager.AddLogin(this.UserId, verifiedloginInfo.Login);
                    if (result.Succeeded == false) { addErrors(result); return; }
                    IdentityHelper.RedirectToReturnUrl(this.ReturnUrl, this.Response);
                }
                else if (string.IsNullOrEmpty(loginInfo.Email) == false && (user = this.UsersLogic.UserManager.FindByEmail(loginInfo.Email)) != null) // There was no account found using this OAuth identity, and the user is not signed-in. Look for an account matching the provided email, and automatically update the account to use this OAuth identity + automatically sign-in the user.
                {
                    setAccessTokensFromClaims(user, loginInfo);
                    setPersonalFieldsFromClaims(user, loginInfo);
                    this.UsersLogic.UserManager.Update(user);
                    // 
                    var result = this.UsersLogic.UserManager.AddLogin(user.Id, loginInfo.Login);
                    if (result.Succeeded == false) { addErrors(result); }
                    else 
                    {
                        this.UsersLogic.SignIn(user, isPersistentQ: false, rememberBrowserQ: false, updateLastLogonDateQ: true);
                        IdentityHelper.RedirectToReturnUrl(this.ReturnUrl, this.Response);
                    }
                }
                else // first time the user is being created. just create the account immediately (no need to prompt for email field)
                {
                    if (PageSettings.Default.AllowSignUpQ == false) { this.RedirectToErrorPage($@"Apologies, but we have temporarily stopped new users from Signing up. Please follow us to check when this is available again."); }
                    // 
                    ApplicationUser newUser = new ApplicationUser() { UserName = loginInfo.Email, Email = loginInfo.Email, CreatedDate = DateTime.UtcNow, };
                    setAccessTokensFromClaims(newUser, loginInfo);
                    setPersonalFieldsFromClaims(newUser, loginInfo);
                    try
                    {
                        newUser = this.UsersLogic.AddUser(newUser); // try to add new user
                        IdentityResult result = this.UsersLogic.UserManager.AddLogin(newUser.Id, loginInfo.Login); // associate loginInfo to new user
                        if (result.Succeeded == false) { redirectOnFail(string.Join(". ", result.Errors)); return; }
                    }
                    catch (IdentityResultException ex) { redirectOnFail(string.Join(". ", ex.Errors)); return; }
                    // 
                    this.UsersLogic.SignIn(newUser, isPersistentQ: false, rememberBrowserQ: false, updateLastLogonDateQ: true);
                    IdentityHelper.RedirectToReturnUrl(this.ReturnUrl, Response);
                    // 
                    //// send 'new user confirmation email'
                    //var code = this.UsersLogic.UserManager.GenerateEmailConfirmationToken(newUser.Id);
                    //string confirmationUrl = IdentityHelper.GetUserConfirmationRedirectUrl(code, newUser.Id, this.Context.Request);
                }
            }
        }

        public static void ChallengeProvider(HttpContext context, string providerName, string returnUrl)
        {
            string redirectUrl = $@"/Login/RegisterExternalLogin.aspx?Provider={HttpUtility.UrlEncode(providerName)}&ReturnUrl={HttpUtility.UrlEncode(returnUrl)}"; // Request a redirect to the external login provider //ResolveUrl(...);
            var properties = new AuthenticationProperties() { RedirectUri = redirectUrl };
            if (context.User.Identity.IsAuthenticated == true) { properties.Dictionary[IdentityHelper.XsrfKey] = context.User.Identity.GetUserId(); } // Add xsrf verification when linking accounts
            context.GetOwinContext().Authentication.Challenge(properties, providerName);
            context.Response.StatusCode = 401;
            context.Response.End();
        }

        protected void setAccessTokensFromClaims(ApplicationUser user, ExternalLoginInfo loginInfo)
        {
#if AUTH_GOOGLE
            {
                string accessToken = loginInfo.ExternalIdentity.Claims.Where(_ => _.Type == "urn:tokens:googleplus:accesstoken").Select(_ => _.Value).FirstOrDefault();
                if (string.IsNullOrEmpty(accessToken) == false) { user.SetToken(ApplicationUser.PROVIDERNAME_GOOGLE, accessToken, secretToken: null, expires: null); }
            }
#endif
#if AUTH_FACEBOOK
            {
                string accessToken = loginInfo.ExternalIdentity.Claims.Where(_ => _.Type == "urn:tokens:facebook:accesstoken").Select(_ => _.Value).FirstOrDefault();
                if (string.IsNullOrEmpty(accessToken) == false) { user.SetToken(ApplicationUser.PROVIDERNAME_FACEBOOK, accessToken, secretToken: null, expires: null); }
            }
#endif
#if AUTH_MICROSOFT
            {
                string accessToken = loginInfo.ExternalIdentity.Claims.Where(_ => _.Type == "urn:tokens:microsoft:accesstoken").Select(_ => _.Value).FirstOrDefault();
                if (string.IsNullOrEmpty(accessToken) == false) { user.SetToken(ApplicationUser.PROVIDERNAME_MICROSOFT, accessToken, secretToken: null, expires: null); }
            }
#endif
#if AUTH_LINKEDIN
            {
                string accessToken = loginInfo.ExternalIdentity.Claims.Where(_ => _.Type == "urn:tokens:linkedin:accesstoken").Select(_ => _.Value).FirstOrDefault();
                if (string.IsNullOrEmpty(accessToken) == false) { user.SetToken(ApplicationUser.PROVIDERNAME_LINKEDIN, accessToken, secretToken: null, expires: null); }
            }
#endif
#if AUTH_TWITTER
            {
                string accessToken = loginInfo.ExternalIdentity.Claims.Where(_ => _.Type == "urn:tokens:twitter:accesstoken").Select(_ => _.Value).FirstOrDefault();
                string accessSecret = loginInfo.ExternalIdentity.Claims.Where(_ => _.Type == "urn:tokens:twitter:accesssecret").Select(_ => _.Value).FirstOrDefault();
                if (string.IsNullOrEmpty(accessToken) == false) { user.SetToken(ApplicationUser.PROVIDERNAME_GOOGLE, accessToken, secretToken: user.TwitterSecretToken, expires: null); }
            }
#endif
#if AUTH_GITHUB
            {
                string accessToken = loginInfo.ExternalIdentity.Claims.Where(_ => _.Type == "urn:tokens:github:accesstoken").Select(_ => _.Value).FirstOrDefault();
                if (string.IsNullOrEmpty(accessToken) == false) { user.SetToken(ApplicationUser.PROVIDERNAME_GITHUB, accessToken, secretToken: null, expires: null); }
            }
#endif
        }
        protected void setPersonalFieldsFromClaims(ApplicationUser user, ExternalLoginInfo loginInfo)
        {
            if (string.IsNullOrEmpty(user.LastName) == true)
            {
                string[] claimTypes = new string[] { "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname", "urn:tokens:googleplus:familyname", "urn:tokens:facebook:familyname", "urn:tokens:microsoft:familyname", "urn:linkedin:lastname", "urn:tokens:linkedin:familyname", "urn:tokens:twitter:familyname", "urn:tokens:github:familyname", };
                string claimValue = loginInfo.ExternalIdentity.Claims.Where(_ => claimTypes.Any(_2 => _.Type.Equals(_2, StringComparison.OrdinalIgnoreCase) == true) == true).Select(_ => _.Value).FirstOrDefault();
                if (string.IsNullOrEmpty(claimValue) == false) { user.LastName = claimValue; }
            }
            if (string.IsNullOrEmpty(user.FirstName) == true)
            {
                string[] claimTypes = new string[] { "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname", "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/familyName", "urn:tokens:googleplus:givenname", "urn:linkedin:firstname", "urn:tokens:facebook:givenname", "urn:tokens:microsoft:givenname", "urn:tokens:linkedin:givenname", "urn:tokens:twitter:givenname", "urn:tokens:github:givenname", };
                string claimValue = loginInfo.ExternalIdentity.Claims.Where(_ => claimTypes.Any(_2 => _.Type.Equals(_2, StringComparison.OrdinalIgnoreCase) == true) == true).Select(_ => _.Value).FirstOrDefault();
                if (string.IsNullOrEmpty(claimValue) == false) { user.FirstName = claimValue; }
            }
            if (string.IsNullOrEmpty(user.AvatarUrl) == true)
            {
                string[] claimTypes = new string[] { "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/avatarurl", "urn:tokens:googleplus:avatarurl", "urn:tokens:facebook:avatarurl", "urn:tokens:microsoft:avatarurl", "urn:tokens:linkedin:avatarurl", "urn:tokens:twitter:avatarurl", "urn:tokens:github:avatarurl", };
                string claimValue = loginInfo.ExternalIdentity.Claims.Where(_ => claimTypes.Any(_2 => _.Type.Equals(_2, StringComparison.OrdinalIgnoreCase) == true) == true).Select(_ => _.Value).FirstOrDefault();
                if (string.IsNullOrEmpty(claimValue) == false) { user.AvatarUrl = claimValue; }
            }
            if (string.IsNullOrEmpty(user.Company) == true)
            {
                string[] claimTypes = new string[] { "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/company", "urn:tokens:googleplus:company", "urn:tokens:facebook:company", "urn:tokens:microsoft:company", "urn:tokens:linkedin:company", "urn:tokens:twitter:company", "urn:tokens:github:company", };
                string claimValue = loginInfo.ExternalIdentity.Claims.Where(_ => claimTypes.Any(_2 => _.Type.Equals(_2, StringComparison.OrdinalIgnoreCase) == true) == true).Select(_ => _.Value).FirstOrDefault();
                if (string.IsNullOrEmpty(claimValue) == false) { user.Company = claimValue; }
            }
        }

        private void redirectOnFail()
        {
            redirectOnFail(this.ErrorMessage);
        }
        private void redirectOnFail(string message)
        {
            string redirectUrl = (
                (string.IsNullOrEmpty(message) == false) ?
                    (this.User.Identity.IsAuthenticated == true) ? $@"/?aspxerror={message}" : $@"/Login?aspxerror={message}"
                    : (this.User.Identity.IsAuthenticated == true) ? $@"/" : $@"/Login"
            );
            this.Response.Redirect(redirectUrl, true);
        }
        private void addErrors(IdentityResult result) 
        {
            foreach (var error in result.Errors) 
            {
                this.ModelState.AddModelError("", error);
            }
        }
        private void addErrors(IdentityResultException result)
        {
            foreach (var error in result.Errors)
            {
                this.ModelState.AddModelError("", error);
            }
        }
    }
}
