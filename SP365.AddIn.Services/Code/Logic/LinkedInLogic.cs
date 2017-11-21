using LinkedIn.Api.Client.Core.Profiles;
using LinkedIn.Api.Client.Owin;
using LinkedIn.Api.Client.Owin.Profiles;
using Microsoft.AspNet.Identity;
using SP365.AddIn.Services.DataAccess;
using SP365.AddIn.Services.DataAccess.Models;
using System;
using System.Linq;
using System.Security.Principal;
using System.Web;

namespace SP365.AddIn.Services.Logic
{
    public class LinkedInLogic : BaseLogic
    {
        #region Constructor

        public LinkedInLogic() : base() { }
        public LinkedInLogic(ApplicationDbContext dbContext) : base(dbContext) { }

        #endregion Constructor

        #region Properties

        private IIdentity CurrentHttpUserIdentity { get { try { return HttpContext.Current?.User?.Identity; } catch (Exception ex) { Logger.Error(ex, LogCategory.Provisioning, $@"Problem getting the Current User Identity."); } return null; } }
        public string CurrentUserId { get { return CurrentHttpUserIdentity?.GetUserId(); } }

        #endregion Properties

        #region Methods

        public string GetAccessToken(ApplicationUser user)
        {
            string accessToken = (user?.Claims?.ToList()?.Where(_ => _.ClaimType == "urn:tokens:linkedin:accesstoken")?.SingleOrDefault()?.ClaimValue ?? user?.LinkedInToken?.AccessToken);
            return accessToken;
        }

        public LinkedInFullProfile GetFullProfile(string accessToken)
        {
            if (string.IsNullOrEmpty(accessToken) == true) { throw new ArgumentNullException(nameof(accessToken)); }
            // 
            LinkedInFullProfile ret = null;
            // 
            using (var client = new LinkedInApiClient(this.OwinContext.Request, accessToken))
            {
                var profileApi = new LinkedInProfileApi(client);
                var task = profileApi.GetFullProfileAsync();
                task.Wait(LinkedInSettings.Default.Timeout);
                ret = task.Result;
            }
            // 
            return ret;
        }

        public LinkedInBasicProfile GetBasicProfile(string accessToken)
        {
            if (string.IsNullOrEmpty(accessToken) == true) { throw new ArgumentNullException(nameof(accessToken)); }
            // 
            LinkedInBasicProfile ret = null;
            // 
            using (var client = new LinkedInApiClient(this.OwinContext.Request, accessToken))
            {
                var profileApi = new LinkedInProfileApi(client);
                var task = profileApi.GetBasicProfileAsync();
                task.Wait(LinkedInSettings.Default.Timeout);
                ret = task.Result;
            }
            // 
            return ret;
        }

        #endregion Methods
    }
}
