using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using SP365.AddIn.Services.DataAccess;
using SP365.AddIn.Services.DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Web;

namespace SP365.AddIn.Services.Logic
{
    public class UsersLogic : BaseLogic
    {
        #region Constructor

        public UsersLogic() : base() { }
        public UsersLogic(ApplicationDbContext dbContext) : base(dbContext) { }

        #endregion Constructor

        #region Properties

        private static IIdentity CurrentHttpUserIdentity { get { try { return HttpContext.Current?.User?.Identity; } catch (Exception ex) { Logger.Error(ex, LogCategory.Provisioning, $@"Problem getting the Current User Identity."); } return null; } }
        public static string CurrentUserId { get { return CurrentHttpUserIdentity?.GetUserId(); } }

        #endregion Properties

        #region Methods

        public List<ApplicationUser> GetUsers(bool resolveRoleNamesQ = false)
        {
            var ctx = this.DbContext;
            // 
            List<ApplicationUser> ret = ret = ctx.Users.ToList();
            if (resolveRoleNamesQ == true) { ret.ForEach(_ => { if (_ != null) { ResolveUserRoleNames(_); } }); }
            return ret;
        }
        public ApplicationUser AddUser(string email, string password = null, string firstName = null, string lastName = null, string phoneNumber = null, string company = null)
        {
            if (string.IsNullOrEmpty(email) == true) { throw new ArgumentNullException(nameof(email)); }
            // 
            return this.AddUser(new ApplicationUser()
            {
                UserName = email,
                Email = email,
                CreatedDate = DateTime.UtcNow,
                // 
                FirstName = firstName,
                LastName = lastName,
                PhoneNumber = phoneNumber,
                Company = company,
            }, password);
        }
        public ApplicationUser AddUser(ApplicationUser user, string password = null)
        {
            var ctx = this.DbContext;
            // 
            ApplicationUser ret = null;
            // 
            if (user == null || string.IsNullOrEmpty(user.Email) == true) { throw new ArgumentNullException(nameof(user)); }
            if (ctx.Users.Any(_ => _.Email != null && _.Email.Equals(user.Email, StringComparison.OrdinalIgnoreCase) == true) == true) { throw new PortalException($@"The email '{user.Email}' is already in use."); }
            // 
            IdentityResult result = this.UserManager.Create(user);
            if (result.Succeeded == false) { throw new IdentityResultException(result.Errors); }
            // 
            if (string.IsNullOrEmpty(password) == false)
            {
                result = this.UserManager.AddPassword(user.Id, password);
                if (result.Succeeded == false) { throw new IdentityResultException(result.Errors); }
            }
            // 
            ret = this.GetUser(user.Id, resolveRoleNamesQ: false);
            // 
            return ret;
        }
        public ApplicationUser UpdateUser(ApplicationUser user)
        {
            ApplicationUser ret = null;
            // 
            if (user == null || string.IsNullOrEmpty(user.Id) == true) { throw new ArgumentNullException(nameof(user)); }
            // 
            this.UserManager.Update(user);
            ret = this.GetUser(user.Id, resolveRoleNamesQ: true);
            // 
            return ret;
        }
        public IdentityRole GetUserRole(string roleName)
        {
            if (string.IsNullOrEmpty(roleName) == true) { throw new ArgumentNullException(nameof(roleName)); }
            // 
            var ctx = this.DbContext;
            IdentityRole ret = ctx.Roles.Single(_ => _.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase) == true);
            return ret;
        }
        public IdentityRole GetUserRoleById(string roleId)
        {
            if (string.IsNullOrEmpty(roleId) == true) { throw new ArgumentNullException(nameof(roleId)); }
            // 
            var ctx = this.DbContext;
            IdentityRole ret = ctx.Roles.Single(_ => _.Id.Equals(roleId, StringComparison.OrdinalIgnoreCase) == true);
            return ret;
        }

        public void SignIn(ApplicationUser user, bool isPersistentQ = false, bool rememberBrowserQ = false, bool updateLastLogonDateQ = false)
        {
            AuthSettings settings = AuthSettings.Default;
            // 
            if (user == null || string.IsNullOrEmpty(user.Email) == true) { throw new ArgumentNullException(nameof(user)); }
            // 
            // Sign-in the User
            this.SignInManager.SignIn(user, isPersistentQ, rememberBrowserQ);
            // 
            if (updateLastLogonDateQ == true)
            {
                this.UpdateLastLogonDate(user);
            }
        }
        public SignInStatus SignIn(string email, string password, bool rememberMeQ = false, bool updateLastLogonDateQ = false)
        {
            AuthSettings settings = AuthSettings.Default;
            SignInStatus? ret = null;
            // 
            if (string.IsNullOrEmpty(email) == true) { throw new ArgumentNullException(nameof(email)); }
            if (string.IsNullOrEmpty(password) == true) { throw new ArgumentNullException(nameof(password)); }
            // 
            // Validate the user/password against the Database
            if (ret == null) 
            {
                ret = this.SignInManager.PasswordSignIn(email, password, isPersistent: rememberMeQ, shouldLockout: (settings.EnableUserLockoutQ ?? true));
            }
            // 
            if (ret == SignInStatus.Success && updateLastLogonDateQ == true)
            {
                ApplicationUser user = null;
                try { user = this.GetUserByEmail(email); }
                catch (Exception ex) { Logger.Error(ex, LogCategory.Provisioning, $@"Could not get user by email."); }
                this.UpdateLastLogonDate(user);
            }
            // 
            return (ret ?? SignInStatus.Failure);
        }
        public void Unlock(ApplicationUser user)
        {
            if (user == null || string.IsNullOrEmpty(user.Id) == true) { throw new ArgumentNullException(nameof(user)); }
            // 
            if (this.UserManager.IsLockedOut(user.Id) == true)
            {
                bool oldLockoutEnabledQ = this.UserManager.GetLockoutEnabled(user.Id);
                if (oldLockoutEnabledQ != false)
                {
                    {
                        IdentityResult result = this.UserManager.SetLockoutEnabled(user.Id, false);
                        if (result.Succeeded == false) { throw new IdentityResultException(result.Errors); }
                    }
                    {
                        IdentityResult result = this.UserManager.SetLockoutEnabled(user.Id, oldLockoutEnabledQ);
                        if (result.Succeeded == false) { throw new IdentityResultException(result.Errors); }
                    }
                }
                // 
                {
                    IdentityResult result = this.UserManager.ResetAccessFailedCount(user.Id);
                    if (result.Succeeded == false) { throw new IdentityResultException(result.Errors); }
                }
            }
        }
        public void SetPassword(ApplicationUser user, string password, bool ignorePasswordValidatorQ = false, bool unlockUserQ = true)
        {
            if (user == null || string.IsNullOrEmpty(user.Id) == true) { throw new ArgumentNullException(nameof(user)); }
            if (string.IsNullOrEmpty(password) == true) { throw new ArgumentNullException(nameof(password)); }
            // 
            if (this.UserManager.HasPassword(user.Id) == true)
            {
                IdentityResult result = this.UserManager.RemovePassword(user.Id);
                if (result.Succeeded == false) { throw new IdentityResultException(result.Errors); }
            }
            // 
            {
                var oldPasswordValidator = this.UserManager.PasswordValidator;
                if (ignorePasswordValidatorQ == true) { this.UserManager.PasswordValidator = new PasswordValidator() { RequireDigit = false, RequiredLength = 0, RequireLowercase = false, RequireNonLetterOrDigit = false, RequireUppercase = false, }; }
                try
                {
                    IdentityResult result = this.UserManager.AddPassword(user.Id, password);
                    if (result.Succeeded == false) { throw new IdentityResultException(result.Errors); }
                }
                finally { if (ignorePasswordValidatorQ == true) { this.UserManager.PasswordValidator = oldPasswordValidator; } }
            }
            // 
            if (unlockUserQ == true)
            {
                try { this.Unlock(user); }
                catch (Exception ex) { Logger.Error(ex, LogCategory.Provisioning, $@"Unexpected Error Unlocking the account."); }
            }
        }
        public void ChangePassword(ApplicationUser applicationUser, string oldPassword, string newPassword, bool ignorePasswordValidatorQ = false)
        {
            if (applicationUser == null || string.IsNullOrEmpty(applicationUser.Id) == true) { throw new ArgumentNullException(nameof(applicationUser)); }
            if (string.IsNullOrEmpty(newPassword) == true) { throw new ArgumentNullException(nameof(newPassword)); }
            // 
            if (this.UserManager.HasPassword(applicationUser.Id) == true)
            {
                if (string.IsNullOrEmpty(oldPassword) == true) { throw new ArgumentNullException(nameof(oldPassword)); }
                // 
                if (this.VerifyPassword(applicationUser, oldPassword) == false) { throw new PortalException($@"The provided password is not valid!"); }
                // 
                var oldPasswordValidator = this.UserManager.PasswordValidator;
                if (ignorePasswordValidatorQ == true) { this.UserManager.PasswordValidator = null; }
                try
                {
                    IdentityResult result = this.UserManager.ChangePassword(applicationUser.Id, oldPassword, newPassword);
                    if (result.Succeeded == false) { throw new IdentityResultException(result.Errors); }
                }
                finally { if (ignorePasswordValidatorQ == true) { this.UserManager.PasswordValidator = oldPasswordValidator; } }
            }
            else
            {
                var oldPasswordValidator = this.UserManager.PasswordValidator;
                if (ignorePasswordValidatorQ == true) { this.UserManager.PasswordValidator = null; }
                try
                {
                    IdentityResult result = this.UserManager.AddPassword(applicationUser.Id, newPassword);
                    if (result.Succeeded == false) { throw new IdentityResultException(result.Errors); }
                }
                finally { if (ignorePasswordValidatorQ == true) { this.UserManager.PasswordValidator = oldPasswordValidator; } }
            }
        }
        public void UpdateLastLogonDate(ApplicationUser user)
        {
            try
            {
                if (user != null)
                {
                    user.LastLogonDate = DateTime.UtcNow;
                    this.UserManager.Update(user);
                }
                else { } // user not signed-in
            }
            catch (Exception ex) { Logger.Error(ex, LogCategory.Provisioning, $@"Could not update the LastLogonDate for user."); }
        }

        public bool VerifyPassword(ApplicationUser user, string password)
        {
            bool ret = this.UserManager.CheckPassword(user, password);
            return ret;
        }

        public void SignOut()
        {
            this.AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
        }

        #endregion Methods
    }
}
