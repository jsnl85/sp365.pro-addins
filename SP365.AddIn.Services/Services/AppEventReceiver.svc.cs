using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.EventReceivers;
using SP365.AddIn.Services.Logic;
using System;
using System.Collections.Generic;
using System.IO;

namespace SP365.AddIn.Services
{
    public class AppEventReceiver : IRemoteEventService
    {
        private const string SCRIPT_SRC = "https://sp365.pro/add-ins/", SCRIPT_PATH = "/cdn/sp365.min.js";

        #region Variables

        private static object _dictionaryLock = new object(); private static Dictionary<Guid, object> _webLocks = null;
#if UPDATETENANT
        private static object _tenantLock = new object();
#endif

        #endregion Variables

        #region Methods

        public SPRemoteEventResult ProcessEvent(SPRemoteEventProperties properties)
        {
            if (properties == null) { throw new ArgumentNullException(nameof(properties)); }
            if (string.IsNullOrEmpty(properties.ErrorMessage) == false) { throw new Exception(properties.ErrorMessage); }
            if (string.IsNullOrEmpty(properties.ErrorCode) == false) { throw new Exception(properties.ErrorCode); }
            // 
            SPRemoteEventResult ret = new SPRemoteEventResult();
            // 
            using (ClientContext ctx = TokenHelper.CreateAppEventClientContext(properties, useAppWeb: false))
            using (AddInLogic addInLogic = new AddInLogic())
            {
                if (ctx == null) { throw new ArgumentNullException(nameof(ctx)); }
                // 
                #region Get further Metadata via CSOM
                string token = properties?.ContextToken;
                Version version = (properties.AppEventProperties?.Version ?? new Version(1, 0, 0, 0));
                var site = ctx.Site; ctx.Load(site); var web = ctx.Web; ctx.Load(web); ctx.ExecuteQuery(); string siteUrl = (site.PrimaryUri ?? ctx?.Url), webUrl = (web.Url ?? properties.AppEventProperties?.HostWebFullUrl?.ToString()), webServerRelativeUrl = (web.ServerRelativeUrl ?? "/"), appWebUrl = properties?.AppEventProperties?.AppWebFullUrl?.ToString(); Guid siteId = site.Id; Guid webId = web.Id;
                Guid uniqueInstanceId = new Guid("cdcb95cd-16ee-409e-8a99-f4642c84591a"); //Guid uniqueInstanceId = webId;
                string uniqueCDNPath = uniqueInstanceId.ToString().ToLowerInvariant().Substring(0, 8);
                // 
                object webLock = new object();
                lock (_dictionaryLock)
                {
                    if (_webLocks == null) { _webLocks = new Dictionary<Guid, object>(); }
                    if (_webLocks.ContainsKey(webId) == false) { _webLocks.Add(webId, webLock); }
                    else { webLock = _webLocks[webId]; }
                }
                #endregion Get further Metadata via CSOM
                // 
                #region Update Web
                // NOTE: there might be more than one calls to the AppEventReceiver. therefore, it is important to apply a lock for the code not to be run twice at the same time. note that the lock will be a different object for each different webId.
                lock (webLock)
                {
                    #region Add User Custom Action registering the custom JS file
                    const string UCA_NAME = "SP365";
                    string scriptSrc = $@"{SCRIPT_SRC}{uniqueCDNPath}{SCRIPT_PATH}?v={version}"; //string scriptSrc = $@"{webServerRelativeUrl.TrimEnd(new char[] { '/' })}/{UCA_WEBRELATIVEFOLDERPATH.Trim(new char[] { '/' })}/app.js?v={version}";
                    const int sequence = 10;
                    // 
                    if (properties.EventType == SPRemoteEventType.AppInstalled || properties.EventType == SPRemoteEventType.AppUpgraded)
                    {
                        ctx.AddOrUpdateUCAScriptLink(UCA_NAME, scriptSrc, sequence: sequence, isInternalQ: false, asyncQ: false);
                    }
                    else if (properties.EventType == SPRemoteEventType.AppUninstalling)
                    {
                        ctx.RemoveUCA(UCA_NAME);
                    }
                    #endregion Add User Custom Action registering the custom JS file
                    // 
                    #region Add Custom WebParts to the Gallery
                    const string WEBPARTS_FOLDER_NAME = "cdcb95cd", WEBPARTS_GROUP_NAME = "SP365 Add-In WebParts"; string[] WEBPARTS_RECOMMENDATIONS = new string[] { "My Site", "Portal", "Publishing Portal", "Team Site", "Blog", };
                    if (properties.EventType == SPRemoteEventType.AppInstalled || properties.EventType == SPRemoteEventType.AppUpgraded)
                    {
                        ctx.AddOrUpdateCustomWebPartGalleryItems(WEBPARTS_FOLDER_NAME, WEBPARTS_GROUP_NAME);
                    }
                    else if (properties.EventType == SPRemoteEventType.AppUninstalling)
                    {
                        ctx.RemoveCustomWebPartGalleryItems(WEBPARTS_GROUP_NAME);
                    }
                    #endregion Add Custom WebParts to the Gallery
                    // 
                    #region Add Custom WebParts to the Homepage
                    string welcomePageServerRelativeUrl = ctx.GetWelcomePageServerRelativeUrl();
                    string linkedInConnectorWebPartXml = SharePointExtensions.GetWebPartDefinitionXml(WEBPARTS_FOLDER_NAME, @"SP365.LinkedInConnector.dwp"), linkedInConnectorWebPartDefaultTitle = @"SP365 LinkedIn Connector";
                    string smartTilesWebPartXml = SharePointExtensions.GetWebPartDefinitionXml(WEBPARTS_FOLDER_NAME, @"SP365.SmartTiles.dwp"), smartTilesWebPartDefaultTitle = @"SP365 Smart Tiles";
                    // 
                    if (properties.EventType == SPRemoteEventType.AppInstalled || properties.EventType == SPRemoteEventType.AppUpgraded)
                    {
                        ctx.AddOrUpdateWebPartToPage(pageServerRelativeUrl: welcomePageServerRelativeUrl, webPartXml: linkedInConnectorWebPartXml, webPartTitle: linkedInConnectorWebPartDefaultTitle);
                        ctx.AddOrUpdateWebPartToPage(pageServerRelativeUrl: welcomePageServerRelativeUrl, webPartXml: smartTilesWebPartXml, webPartTitle: smartTilesWebPartDefaultTitle);
                    }
                    else if (properties.EventType == SPRemoteEventType.AppUninstalling)
                    {
                        //ctx.RemoveWebPartFromPage(pageServerRelativeUrl: welcomePageServerRelativeUrl, webPartTitle: linkedInConnectorWebPartDefaultTitle);
                        //ctx.RemoveWebPartFromPage(pageServerRelativeUrl: welcomePageServerRelativeUrl, webPartTitle: smartTilesWebPartDefaultTitle);
                    }
                    #endregion Add Custom WebParts to the Homepage
                    // 
                    #region Try Creating/Updating record under 'dbo.AppInstances'
                    try
                    {
                        var appInstance = (addInLogic.GetAppInstanceByWebId(webId) ?? addInLogic.CreateNewAppInstance(siteId, webId, token));
                        appInstance.SiteId = siteId;
                        appInstance.WebId = webId;
                        appInstance.SiteUrl = siteUrl;
                        appInstance.WebUrl = webUrl;
                        appInstance.AccessToken = token;
                        appInstance.ModifiedDate = DateTime.UtcNow;
                        appInstance.AppWebUrl = appWebUrl;
                        appInstance.UniqueCDNPath = uniqueCDNPath;
                        appInstance.Version = version?.ToString();
                        appInstance.Status = ((properties.EventType > 0) ? properties.EventType.ToString() : null);
                        addInLogic.UpdateAll();
                    }
                    catch (Exception ex) { Logger.Error(ex, $@"Could not add the AppInstance after the AppEventReceiver completed on Web '{webUrl}'."); }
                    #endregion Try Creating/Updating record under 'dbo.AppInstances'
                }
                #endregion Update Web
                // 
                #region #if UPDATETENANT => Update Tenant
#if UPDATETENANT
                // NOTE: there might be more than one calls to the AppEventReceiver. therefore, it is important to apply a lock for the code not to be run twice at the same time.
                lock (_tenantLock)
                {
                #region Add Custom Extensions
                    const string SP365EXTENSION_FOLDER_NAME = "398099f4"; Guid PRODUCTID_SP365EXTENSION_APPLICATIONCUSTOMISER = new Guid(@"398099F4-E409-4309-8B57-BA1E47E7DB97"); // Guid PRODUCTID_SP365ADDIN_CLASSIC = new Guid(@"B1717448-51EF-42AD-9027-123CCBCA70A8");
                    try
                    {
                        var appInfo = ctx.GetTenantAppByProductId(PRODUCTID_SP365EXTENSION_APPLICATIONCUSTOMISER);
                        if (appInfo == null)
                        {
                            if (properties.EventType == SPRemoteEventType.AppInstalled || properties.EventType == SPRemoteEventType.AppUpgraded)
                            {
                                var appInstance = ctx.InstallTenantApp($@"~/AddIns/{SP365EXTENSION_FOLDER_NAME}/pkg/SP365.AddIn.sppkg");
                            }
                            else if (properties.EventType == SPRemoteEventType.AppUninstalling)
                            {
                                appInfo = ctx.UninstallTenantApp(appInfo.ProductId);
                            }
                        }
                    }
                    catch (Exception ex) { Logger.Warning(ex, $@"Could not Install the SP365 Modern Add-In on the Tenant, to support Modern UI."); }
                #endregion Add Custom Extensions
                }
#endif
                #endregion #if UPDATETENANT => Update Tenant
            }
            // 
            return ret;
        }
        public void ProcessOneWayEvent(SPRemoteEventProperties properties) { ProcessEvent(properties); }

        #endregion Methods
    }
}
