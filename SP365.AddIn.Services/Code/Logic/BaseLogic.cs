using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using SP365.AddIn.Services.DataAccess;
using SP365.AddIn.Services.DataAccess.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web;

namespace SP365.AddIn.Services.Logic
{
    public abstract class BaseLogic : IDisposable
    {
        #region Constructor

        protected BaseLogic() { }
        protected BaseLogic(ApplicationDbContext dbContext) { this._dbContext = dbContext; _dbContextNeedsDisposingQ = false; }

        #endregion Constructor

        #region Properties

        protected ApplicationDbContext DbContext { get { if (_dbContext == null) { _dbContext = new ApplicationDbContext(); _dbContextNeedsDisposingQ = true; } return _dbContext; } } private ApplicationDbContext _dbContext = null; private bool _dbContextNeedsDisposingQ = false;
        // 
        protected HttpContext Context { get { return HttpContext.Current; } }
        public IOwinContext OwinContext { get { return this.Context.GetOwinContext(); } }
        public IAuthenticationManager AuthenticationManager { get { return this.OwinContext.Authentication; } }
        public ApplicationUserManager UserManager { get { if (_userManager == null) { _userManager = this.OwinContext.GetUserManager<ApplicationUserManager>(); } return _userManager; } } private ApplicationUserManager _userManager = null;
        public ApplicationSignInManager SignInManager { get { if (_signInManager == null) { _signInManager = this.OwinContext.GetUserManager<ApplicationSignInManager>(); } return _signInManager; } } private ApplicationSignInManager _signInManager = null;
        public ApplicationUser CurrentUser { get { if (_currentUser == null) { _currentUser = this.GetCurrentUser(); } return _currentUser; } } private ApplicationUser _currentUser = null;

        #endregion Properties

        #region Methods

        public void UpdateAll()
        {
            var ctx = this.DbContext;
            ctx.SaveChanges();
        }

        public void Dispose()
        {
            if (_dbContext != null && _dbContextNeedsDisposingQ == true) { _dbContext.Dispose(); _dbContext = null; _dbContextNeedsDisposingQ = false; }
        }

        #endregion Methods

        #region Auxiliaries

        public ApplicationUser GetCurrentUser(bool resolveRoleNamesQ = false)
        {
            ApplicationUser ret = null;
            // 
            var user = this.AuthenticationManager.User;
            if (user != null && user.Identity.IsAuthenticated == true)
            {
                string userId = user.Identity.GetUserId();
                ret = this.UserManager.FindById(userId);
                if (ret != null && resolveRoleNamesQ == true) { ResolveUserRoleNames(ret); }
            }
            else
            {
                try
                {
                    ClaimsPrincipal cookiePrincipal = GetOwinPrincipalFromCookie($@".AspNet.{DefaultAuthenticationTypes.ApplicationCookie}"); // ".AspNet.ApplicationCookie"
                    if (cookiePrincipal != null && cookiePrincipal.Identity.IsAuthenticated == true)
                    {
                        string userId = user.Identity.GetUserId();
                        ret = this.UserManager.FindById(userId);
                        if (ret != null && resolveRoleNamesQ == true) { ResolveUserRoleNames(ret); }
                    }
                }
                catch (Exception ex) { Logger.Error(ex, LogCategory.Provisioning, $@"Could not determine the identity of the Current User using the Request Cookie."); }
            }
            // 
            return ret;
        }
        protected ClaimsPrincipal GetOwinPrincipalFromCookie(string cookieName)
        {
            ClaimsPrincipal ret = null;
            // 
            if (this.Context.Request.Cookies.AllKeys.Contains(cookieName) == true)
            {
                HttpCookie cookie = this.Context.Request.Cookies[cookieName];
                if (cookie != null && string.IsNullOrEmpty(cookie.Value) == false)
                {
                    ret = GetOwinPrincipalFromBase64String(cookie.Value);
                }
                else { throw new PortalException("Principal Cookie value is not valid!"); }
            }
            else { } //else { throw new PortalException("Principal Cookie was not present."); }
            // 
            return ret;
        }
        protected ClaimsPrincipal GetOwinPrincipalFromBase64String(string base64String)
        {
            ClaimsPrincipal ret = null;
            // 
            if (string.IsNullOrEmpty(base64String) == false)
            {
                base64String = base64String.Replace('-', '+').Replace('_', '/');
                var padding = 3 - ((base64String.Length + 3) % 4);
                if (padding != 0) { base64String = base64String + new string('=', padding); }
                var bytes = Convert.FromBase64String(base64String);
                bytes = System.Web.Security.MachineKey.Unprotect(bytes, "Microsoft.Owin.Security.Cookies.CookieAuthenticationMiddleware", "ApplicationCookie", "v1"); // NOTE: this will only work if MachineKey is the same between servers.
                // 
                using (var memory = new MemoryStream(bytes))
                using (var compression = new GZipStream(memory, CompressionMode.Decompress))
                using (var reader = new BinaryReader(compression))
                {
                    reader.ReadInt32();
                    string authenticationType = reader.ReadString();
                    reader.ReadString();
                    reader.ReadString();
                    int count = reader.ReadInt32();
                    var claims = new Claim[count];
                    for (int index = 0; index != count; ++index)
                    {
                        string type = reader.ReadString();
                        type = type == "\0" ? ClaimTypes.Name : type;
                        string value = reader.ReadString();
                        string valueType = reader.ReadString();
                        valueType = valueType == "\0" ? "http://www.w3.org/2001/XMLSchema#string" : valueType;
                        string issuer = reader.ReadString();
                        issuer = issuer == "\0" ? "LOCAL AUTHORITY" : issuer;
                        string originalIssuer = reader.ReadString();
                        originalIssuer = originalIssuer == "\0" ? issuer : originalIssuer;
                        claims[index] = new Claim(type, value, valueType, issuer, originalIssuer);
                    }
                    var identity = new ClaimsIdentity(claims, authenticationType, ClaimTypes.Name, ClaimTypes.Role);
                    var principal = new ClaimsPrincipal(identity);
                }
            }
            // 
            return ret;
        }
        // 
        public ApplicationUser GetUser(string userId, bool resolveRoleNamesQ = false)
        {
            ApplicationUser ret = null;
            // 
            if (string.IsNullOrEmpty(userId) == true) { throw new ArgumentNullException(nameof(userId)); }
            // 
            ret = this.UserManager.FindById(userId); //ret = this.IdentityDbContext.Users.SingleOrDefault(_ => _.Id == userId);
            if (ret != null && resolveRoleNamesQ == true) { ResolveUserRoleNames(ret); }
            // 
            return ret;
        }
        public ApplicationUser GetUserByEmail(string email, bool resolveRoleNamesQ = false)
        {
            ApplicationUser ret = null;
            // 
            if (string.IsNullOrEmpty(email) == true) { throw new ArgumentNullException(nameof(email)); }
            // 
            ret = this.UserManager.FindByEmail(email); //ret = this.IdentityDbContext.Users.SingleOrDefault(_ => _.Email == email);
            if (ret != null && resolveRoleNamesQ == true) { ResolveUserRoleNames(ret); }
            // 
            return ret;
        }
        protected void ResolveUserRoleNames(ApplicationUser user)
        {
            if (user == null) { throw new ArgumentNullException(nameof(user)); }
            // 
            if (user.Roles?.Any() == true)
            {
                List<string> resolvedRoleNames = new List<string>(user.Roles.Count);
                foreach (string roleId in user.Roles.Select(_ => _.RoleId))
                {
                    string name = this.DbContext.Roles.Where(_ => _.Id == roleId).Select(_ => _.Name).SingleOrDefault();
                    if (string.IsNullOrEmpty(name) == false) { resolvedRoleNames.Add(name); }
                }
                user.ResolvedRoleNames = resolvedRoleNames;
            }
        }

        public ApplicationUser EnsureUserIsAuthenticated(bool resolveRoleNamesQ = false)
        {
            ApplicationUser ret = null;
            // 
            ret = GetCurrentUser(resolveRoleNamesQ: resolveRoleNamesQ);
            if (ret == null) { throw new UserNotAuthenticatedException(); }
            // 
            return ret;
        }
        protected void EnsureUserIsAdmin()
        {
            this.EnsureUserHasRole("Administrator");
        }
        public void EnsureUserHasRole(string role)
        {
            ApplicationUser currentUser = EnsureUserIsAuthenticated(resolveRoleNamesQ: true);
            EnsureUserHasRole(currentUser, role);
        }
        public void EnsureUserHasAnyRole(ApplicationUser user, params string[] roles)
        {
            if (roles?.Any() != true && user.Roles.Any() == true) { } // valid
            else if (roles.Any(_ => IsUser(user, _) == true) == false) // invalid
            {
                throw new PortalException($@"The user has not been granted any of the roles {string.Join("/", roles)}.") { HttpStatusCode = HttpStatusCode.Unauthorized, };
            }
        }
        public void EnsureUserHasRole(ApplicationUser user, string role)
        {
            if (IsUser(user, role) == false)
            {
                throw new PortalException($@"The user has not been granted the {role} role.") { HttpStatusCode = HttpStatusCode.Unauthorized, };
            }
        }
        public bool IsUserAdmin()
        {
            ApplicationUser currentUser = this.GetCurrentUser(resolveRoleNamesQ: true);
            return (currentUser != null && this.IsUserAdmin(currentUser));
        }
        public bool IsUserAdmin(ApplicationUser user)
        {
            return (user != null && this.IsUser(user, ApplicationDbContext.RoleName_Administrator));
        }
        public bool IsUser(string role)
        {
            ApplicationUser currentUser = GetCurrentUser(resolveRoleNamesQ: true);
            return (currentUser != null && IsUser(currentUser, role) == true);
        }
        public bool IsUser(ApplicationUser user, string role)
        {
            if (user == null) { throw new ArgumentNullException(nameof(user)); }
            if (string.IsNullOrEmpty(role) == true) { throw new ArgumentNullException(nameof(role)); }
            // 
            return (user != null && user.ResolvedRoleNames != null && user.ResolvedRoleNames.Any(_ => _.Equals(role, StringComparison.OrdinalIgnoreCase) == true) == true);
        }

        #endregion Auxiliaries
    }
}
