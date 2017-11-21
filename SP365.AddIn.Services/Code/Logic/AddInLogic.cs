using SP365.AddIn.Services.DataAccess;
using SP365.AddIn.Services.DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SP365.AddIn.Services.Logic
{
    public class AddInLogic : BaseLogic
    {
        #region Constructor

        public AddInLogic() : base() { }
        public AddInLogic(ApplicationDbContext dbContext) : base(dbContext) { }

        #endregion Constructor

        #region Properties

        #endregion Properties

        #region Methods

        public List<AppInstance> GetAllAppInstances()
        {
            var ctx = this.DbContext;
            return ctx.AppInstances.ToList();
        }
        public List<AppInstance> GetAppInstancesBySiteId(Guid siteId)
        {
            var ctx = this.DbContext;
            return ctx.AppInstances.Where(_ => _.SiteId == siteId)?.ToList();
        }
        public AppInstance GetAppInstanceByWebId(Guid webId)
        {
            var ctx = this.DbContext;
            return ctx.AppInstances.Where(_ => _.WebId == webId)?.FirstOrDefault();
        }

        public AppInstance CreateNewAppInstance(Guid siteId, Guid webId, string accessToken)
        {
            var ctx = this.DbContext;
            var lazy = ctx.AppInstances.Create();
            {
                lazy.SiteId = siteId;
                lazy.WebId = webId;
                lazy.AccessToken = accessToken;
                lazy.ModifiedDate = DateTime.UtcNow;
            }
            // 
            var ret = ctx.AppInstances.Add(lazy);
            return ret;
        }

        #endregion Methods
    }
}
