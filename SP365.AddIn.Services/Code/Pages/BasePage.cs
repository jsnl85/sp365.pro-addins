using Microsoft.AspNet.Identity;
using SP365.AddIn.Services.DataAccess;
using SP365.AddIn.Services.DataAccess.Models;
using SP365.AddIn.Services.Logic;
using SP365.AddIn.Services.Models;
using System;
using System.Text.RegularExpressions;
using System.Web;

namespace SP365.AddIn.Services
{
    public class BasePage : System.Web.UI.Page, IDisposable
    {
        #region Properties

        protected ApplicationDbContext ApplicationDbContext { get { if (_applicationDbContext == null) { _applicationDbContext = new ApplicationDbContext(); } return _applicationDbContext; } } private ApplicationDbContext _applicationDbContext = null;
        // 
        protected UsersLogic UsersLogic { get { if (_usersLogic == null) { _usersLogic = new UsersLogic(this.ApplicationDbContext); } return _usersLogic; } } private UsersLogic _usersLogic = null;
        protected ApplicationUser CurrentUser { get { if (_currentUser == null) { _currentUser = this.UsersLogic.GetCurrentUser(resolveRoleNamesQ: true); } return _currentUser; } } private ApplicationUser _currentUser = null;
        protected UserInfo CurrentUserInfo { get { return UserInfo.Create(this.CurrentUser); } }
        // 
        protected string FriendlyUrl { get { return Regex.Replace(this.Request.RawUrl, @"default[.]aspx$", string.Empty); } }
        protected string UserId { get { return this.User.Identity.GetUserId(); } }
        protected virtual bool AllowAnonymousQ { get { return false; } }

        #endregion Properties

        #region Methods

        protected override void OnPreInit(EventArgs e)
        {
            base.OnPreInit(e);
            // 
        }
        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            // 
            if (this.AllowAnonymousQ == false && this.CurrentUser == null)
            {
                string redirectUrl = "/Login" + ((string.IsNullOrEmpty(this.FriendlyUrl) == false && this.FriendlyUrl != "/") ? "?ReturnUrl="+ HttpUtility.UrlEncode(this.FriendlyUrl) : string.Empty);
                this.Response.Redirect(redirectUrl, true);
            }
        }
        protected override void OnError(EventArgs e)
        {
            base.OnError(e);
            // 
            Exception exception = this.Server.GetLastError();
            this.Server.ClearError();
            // 
            Logger.Error(exception, LogCategory.Rendering, $@"Error accessing Page '{this.Request.RawUrl}'.");
            // 
            RedirectToErrorPage(exception);
        }

        public void RedirectToErrorPage(Exception exception)
        {
            string message = ((exception != null) ? exception.Message : null);
            RedirectToErrorPage(message);
        }
        public void RedirectToErrorPage(string message)
        {
            string errorPageUrl = "/500.htm";
            string redirectUrl = ((string.IsNullOrEmpty(message) == false) ? $@"{errorPageUrl}?aspxerror={HttpUtility.UrlEncode(message)}" : errorPageUrl);
            if (this.Request.RawUrl?.Equals(redirectUrl, StringComparison.OrdinalIgnoreCase) != true)
            {
                Logger.Verbose(LogCategory.Rendering, $@"RedirectToErrorPage :: Redirecting to '{redirectUrl}'. Page was '{this.Request.RawUrl}'.");
                this.Response.Redirect(redirectUrl, true);
            }
            else { Logger.CriticalError(LogCategory.Rendering, "RedirectToErrorPage :: Redirect Loop detected! Will not redirect anymore."); } // big problem here...
        }

        public override void Dispose()
        {
            base.Dispose();
            // 
            if (_applicationDbContext != null) { _applicationDbContext.Dispose(); _applicationDbContext = null; }
            if (_usersLogic != null) { _usersLogic.Dispose(); _usersLogic = null; }
        }

        #endregion Methods
    }
}
