#if UPDATETENANT
using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Hosting;

namespace SP365.AddIn.Services
{
    internal static class TenantExtensions
    {
        #region Manage Tenant Apps

        public static List<AppInfo> GetTenantApps(this ClientContext ctx)
        {
            List<AppInfo> ret = null;
            // 
            Tenant tenant = new Tenant(ctx);
            tenant.Context.Load(tenant);
            tenant.Context.ExecuteQuery(); // tenant.Context.ExecuteQueryRetry();
            // 
            var appCatalogAppInfoCollection = tenant.GetAppInfoByName(string.Empty); // string.Empty will get ALL Apps
            tenant.Context.Load(appCatalogAppInfoCollection);
            tenant.Context.ExecuteQuery(); // tenant.Context.ExecuteQueryRetry();
            // 
            ret = appCatalogAppInfoCollection?.ToList();
            // 
            return ret;
        }
        public static AppInfo GetTenantAppByProductId(this ClientContext ctx, Guid productId)
        {
            if (productId == default(Guid)) { throw new ArgumentNullException(nameof(productId)); }
            // 
            AppInfo ret = null;
            // 
            Tenant tenant = new Tenant(ctx);
            tenant.Context.Load(tenant);
            tenant.Context.ExecuteQuery(); // tenant.Context.ExecuteQueryRetry();
            // 
            var appCatalogAppInfoCollection = tenant.GetAppInfoByProductId(productId);
            tenant.Context.Load(appCatalogAppInfoCollection);
            tenant.Context.ExecuteQuery(); // tenant.Context.ExecuteQueryRetry();
            // 
            ret = appCatalogAppInfoCollection?.SingleOrDefault();
            // 
            return ret;
        }
        public static AppInstance InstallTenantApp(this ClientContext ctx, string addInFilePath)
        {
            if (string.IsNullOrEmpty(addInFilePath) == true) { throw new ArgumentNullException(nameof(addInFilePath)); }
            if (addInFilePath.StartsWith("~") == true) { addInFilePath = HostingEnvironment.MapPath(addInFilePath); }
            FileInfo addInFile = new FileInfo(addInFilePath);
            if(addInFile?.Exists != true) { throw new ArgumentNullException(nameof(addInFile)); }
            return InstallTenantApp(ctx, addInFile);
        }
        public static AppInstance InstallTenantApp(this ClientContext ctx, FileInfo addInFile)
        {
            if(addInFile?.Exists != true) { throw new ArgumentNullException(nameof(addInFile)); }
            // 
            AppInstance ret = null;
            // 
            Tenant tenant = new Tenant(ctx);
            tenant.Context.Load(tenant);
            tenant.Context.ExecuteQuery(); // tenant.Context.ExecuteQueryRetry();
            // 
            using (var packageStream = addInFile.OpenRead())
            {
                var appInstance = ctx.Web.LoadAndInstallApp(packageStream);
                ctx.Load(appInstance);
                ctx.ExecuteQuery();
                // 
                if (appInstance != null && appInstance.Status == AppInstanceStatus.Initialized) { ret = appInstance; }
                else { throw new Exception($@"There was an error Deploying the Tenant App '{addInFile.Name}'."); }
            }
            // 
            return ret;
        }

        #endregion Manage Tenant Apps
    }
}
#endif
