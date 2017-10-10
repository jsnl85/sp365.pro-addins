using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.EventReceivers;

namespace SP365.AddIn.Services
{
    public class AppEventReceiver : IRemoteEventService
    {
        private const string SCRIPT_SRC = "https://sp365.pro/add-ins/", SCRIPT_PATH = "/cdn/sp365.min.js";

        public SPRemoteEventResult ProcessEvent(SPRemoteEventProperties properties)
        {
            if (properties == null) { throw new ArgumentNullException(nameof(properties)); }
            if (string.IsNullOrEmpty(properties.ErrorMessage) == false) { throw new Exception(properties.ErrorMessage); }
            if (string.IsNullOrEmpty(properties.ErrorCode) == false) { throw new Exception(properties.ErrorCode); }
            // 
            SPRemoteEventResult result = new SPRemoteEventResult();
            // 
            using (ClientContext ctx = TokenHelper.CreateAppEventClientContext(properties, useAppWeb: false))
            {
                if (ctx == null) { throw new ArgumentNullException(nameof(ctx)); }
                // 
                const string UCA_NAME = "SP365";
                Guid uniqueInstanceId = new Guid("cdcb95cd-16ee-409e-8a99-f4642c84591a"); //var web = ctx.Web; ctx.Load(web); ctx.ExecuteQuery(); string webServerRelativeUrl = (web.ServerRelativeUrl ?? "/"); Guid webId = web.Id; Guid uniqueInstanceId = webId;
                string uniqueInstanceWebFolderName = uniqueInstanceId.ToString().ToLowerInvariant().Substring(0, 8);
                Version version = properties.AppEventProperties?.Version ?? new Version(1, 0, 0, 0);
                string scriptSrc = $@"{SCRIPT_SRC}{uniqueInstanceWebFolderName}{SCRIPT_PATH}?v={version}"; //string scriptSrc = $@"{webServerRelativeUrl.TrimEnd(new char[] { '/' })}/{UCA_WEBRELATIVEFOLDERPATH.Trim(new char[] { '/' })}/app.js?v={version}";
                const int sequence = 10;
                // 
                if (properties.EventType == SPRemoteEventType.AppInstalled || properties.EventType == SPRemoteEventType.AppUpgraded)
                {
                    //ctx.addOrUpdateUCAScriptLinkWithInternalScriptSrc(UCA_NAME, scriptSrc, sequence: sequence);
                    //ctx.addOrUpdateUCAScriptLinkWithExternalAsynchScriptSrc(UCA_NAME, scriptSrc, sequence: sequence);
                    ctx.addOrUpdateUCAScriptLinkWithExternalSynchScriptSrc(UCA_NAME, scriptSrc, sequence: sequence);
                }
                else if (properties.EventType == SPRemoteEventType.AppUninstalling)
                {
                    ctx.deleteUCA(UCA_NAME);
                }
            }
            // 
            return result;
        }
        public void ProcessOneWayEvent(SPRemoteEventProperties properties) { ProcessEvent(properties); }
    }
}
