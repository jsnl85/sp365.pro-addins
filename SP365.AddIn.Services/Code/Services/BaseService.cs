using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using System;
using System.IO;
using System.Security.Principal;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Web;
using System.Web.SessionState;

namespace SP365.AddIn.Services
{
    [BasePortalServiceBehavior]
    public abstract class BasePortalService : IRequiresSessionState, IDisposable
    {
        #region Constructor + InitializeService

        public static void InitializeService(ServiceConfiguration config)
        {
            // This method is called only once to initialize service-wide policies.
            //OwinAuth.ConfigureAuth(config);
        }

        public BasePortalService()
        {
            //Microsoft.Owin.Hosting.WebApp.Start<Startup>(HttpRequest.RawUrl);
        }

        #endregion Constructor + InitializeService

        #region Properties

        protected HttpContext HttpContext { get { return HttpContext.Current; } }
        protected HttpRequest HttpRequest { get { return ((this.HttpContext != null) ? this.HttpContext.Request : null); } }
        protected static HttpSessionState HttpSession { get { return ((HttpContext.Current != null) ? HttpContext.Current.Session : null); } }
        protected IPrincipal HttpContextUser { get { return ((this.HttpContext != null) ? this.HttpContext.User : null); } }
        protected IIdentity HttpContextUserIdentity { get { return ((this.HttpContextUser != null) ? this.HttpContextUser.Identity : null); } }
        //protected GenericPrincipal CurrentUser { get { return ((HttpContext != null) ? HttpContext.User as GenericPrincipal : null); } }
        //protected GenericIdentity CurrentIdentity { get { return ((CurrentUser != null) ? CurrentUser.Identity as GenericIdentity : null); } }
        //protected string CurrentUsername { get { return ((CurrentIdentity != null) ? CurrentIdentity.Name : null); } }
        protected string HttpContextUserId { get { if (_httpContextUserId == null && this.HttpContextUserIdentity != null) { _httpContextUserId = this.HttpContextUserIdentity.GetUserId(); } return _httpContextUserId; } } private string _httpContextUserId = null;
        protected string HttpContextUserName { get { if (_httpContextUserName == null && this.HttpContextUserIdentity != null) { _httpContextUserName = this.HttpContextUserIdentity.GetUserName(); } return _httpContextUserName; } } private string _httpContextUserName = null;
        protected OperationContext OperationContext { get { return OperationContext.Current; } }
        //public ClaimsPrincipal OperationCurrentPrincipal { get { return ((this.OperationContext != null) ? this.OperationContext.ClaimsPrincipal : null); } }
        public WebOperationContext WebOperationContext { get { return WebOperationContext.Current; } }
        protected static OutgoingWebResponseContext OutgoingResponse { get { return WebOperationContext.Current.OutgoingResponse; } }
        protected IOwinContext OwinContext { get { return ((this.HttpRequest != null) ? this.HttpRequest.GetOwinContext() : null); } }
        protected ApplicationUserManager UserManager { get { if (_userManager == null) { _userManager = ((this.OwinContext != null) ? this.OwinContext.GetUserManager<ApplicationUserManager>() : null); } return _userManager; } } private ApplicationUserManager _userManager = null;
        //public ApplicationUser CurrentUser { get { if (_currentUser == null && this.HttpContextUserId != null) { _currentUser = this.UserManager.get; } return _currentUser; } } private ApplicationUserManager _currentUser = null;

        #endregion Properties

        #region Methods

        protected Stream PrepareUploadStream(byte[] data, string fileName, string contentType = null)
        {
            MemoryStream ret = new MemoryStream(data);
            if (string.IsNullOrEmpty(fileName) == false) { this.WebOperationContext.OutgoingResponse.Headers["Content-Disposition"] = $@"attachment; filename={fileName}"; }
            if (string.IsNullOrEmpty(contentType) == false) { this.WebOperationContext.OutgoingResponse.ContentType = contentType; }
            if (ret.Position != 0L) { ret.Position = 0L; } // position the 'ret' stream in the start, so that the response is ready to be sent.
            return ret;
        }
        protected Stream PrepareUploadStream(Stream stream, string fileName, string contentType = null)
        {
            Stream ret = stream;
            //MemoryStream ret = new MemoryStream();
            //using (FileStream file = File.OpenRead(path)) { file.CopyTo(ret); } // copy into the 'ret' stream
            // 
            if (string.IsNullOrEmpty(fileName) == false) { this.WebOperationContext.OutgoingResponse.Headers["Content-Disposition"] = $@"attachment; filename={fileName}"; }
            if (string.IsNullOrEmpty(contentType) == false) { this.WebOperationContext.OutgoingResponse.ContentType = contentType; }
            if (ret.Position != 0L) { ret.Position = 0L; } // position the 'ret' stream in the start, so that the response is ready to be sent.
            return ret;
        }

        public void Dispose()
        {
        }

        #endregion Methods
    }
}
