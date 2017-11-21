using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.WebParts;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Hosting;
using System.Xml;

namespace SP365.AddIn.Services
{
    internal static class SharePointExtensions
    {
        #region ClientContext extensions

        public static ClientContext OpenAppOnlyClientContext(Uri siteUri)
        {
            if (siteUri == null) { throw new ArgumentNullException(nameof(siteUri)); }
            // 
            string realm = TokenHelper.GetRealmFromTargetUrl(siteUri);
            var token = TokenHelper.GetAppOnlyAccessToken(TokenHelper.SharePointPrincipal, siteUri.Authority, realm);
            string accessToken = token.AccessToken;
            // 
            return TokenHelper.GetClientContextWithAccessToken(siteUri?.ToString(), accessToken);
        }
        public static ClientContext OpenAppOnlyClientContext(Uri siteUri, string accessToken)
        {
            if (siteUri == null) { throw new ArgumentNullException(nameof(siteUri)); }
            if (string.IsNullOrEmpty(accessToken) == true) { throw new ArgumentNullException(nameof(accessToken)); }
            // 
            return TokenHelper.GetClientContextWithAccessToken(siteUri?.ToString(), accessToken);
        }

        #endregion ClientContext extensions

        #region Manage User Custom Actions

        public static void AddOrUpdateUCAScriptLink(this ClientContext ctx, string ucaName, string scriptSrc, int sequence = 10, bool isInternalQ = false, bool asyncQ = true)
        {
            if (isInternalQ == true) { AddOrUpdateUCAScriptLinkWithInternalScriptSrc(ctx, ucaName: ucaName, scriptSrc: scriptSrc, sequence: sequence); }
            else if (asyncQ == true) { AddOrUpdateUCAScriptLinkWithExternalAsynchScriptSrc(ctx, ucaName: ucaName, scriptSrc: scriptSrc, sequence: sequence); }
            else { AddOrUpdateUCAScriptLinkWithExternalSynchScriptSrc(ctx, ucaName: ucaName, scriptSrc: scriptSrc, sequence: sequence); }
        }
        private static void AddOrUpdateUCAScriptLinkWithInternalScriptSrc(this ClientContext ctx, string ucaName, string scriptSrc, int sequence = 10)
        {
            var web = ctx.Web;
            var userCustomActions = web.UserCustomActions; ctx.Load(userCustomActions);
            ctx.ExecuteQuery();
            // 
            var uca = userCustomActions.FirstOrDefault(_ => _.Name == ucaName);
            if (uca == null) { uca = userCustomActions.Add(); uca.Name = ucaName; }
            uca.Location = "ScriptLink";
            uca.ScriptSrc = scriptSrc;
            uca.ScriptBlock = null;
            uca.Sequence = sequence;
            uca.Update();
            // 
            ctx.Load(uca);
            ctx.ExecuteQuery();
        }
        private static void AddOrUpdateUCAScriptLinkWithExternalAsynchScriptSrc(this ClientContext ctx, string ucaName, string scriptSrc, int sequence = 10)
        {
            var web = ctx.Web;
            var userCustomActions = web.UserCustomActions; ctx.Load(userCustomActions);
            ctx.ExecuteQuery();
            // 
            var uca = userCustomActions.FirstOrDefault(_ => _.Name == ucaName);
            if (uca == null) { uca = userCustomActions.Add(); uca.Name = ucaName; }
            uca.Location = "ScriptLink";
            uca.ScriptSrc = null;
            uca.ScriptBlock = $@"
document.write('<script type=""text/javascript"" src=""{scriptSrc}""></'+'script>');";
            uca.Sequence = sequence;
            uca.Update();
            // 
            ctx.Load(uca);
            ctx.ExecuteQuery();
        }
        private static void AddOrUpdateUCAScriptLinkWithExternalSynchScriptSrc(this ClientContext ctx, string ucaName, string scriptSrc, int sequence = 10)
        {
            var web = ctx.Web;
            var userCustomActions = web.UserCustomActions; ctx.Load(userCustomActions);
            ctx.ExecuteQuery();
            // 
            var uca = userCustomActions.FirstOrDefault(_ => _.Name == ucaName);
            if (uca == null) { uca = userCustomActions.Add(); uca.Name = ucaName; }
            uca.Location = "ScriptLink";
            uca.ScriptSrc = null;
            uca.ScriptBlock = $@"
(function(scriptSrc){{
try{{
var r=(typeof(XMLHttpRequest)?new XMLHttpRequest():typeof(createXMLHTTPObject)=='function'?createXMLHTTPObject():null);r.open('GET',scriptSrc,false);r.send();
var m=(function(s){{eval(s);return (typeof(exports)!='undefined'?exports:undefined);}})(r.responseText);
}}catch(e){{
if(window.console&&window.console.log){{window.console.log('- could not load script synchronously. going to load assynchronously.');}}
document.write('<script type=""text/javascript"" src=""'+scriptSrc+'""></'+'script>');
}}
}})('{scriptSrc}');
";
            uca.Sequence = sequence;
            uca.Update();
            // 
            ctx.Load(uca);
            ctx.ExecuteQuery();
        }
        public static void RemoveUCA(this ClientContext ctx, string ucaName)
        {
            var web = ctx.Web;
            var userCustomActions = web.UserCustomActions; ctx.Load(userCustomActions);
            ctx.ExecuteQuery();
            // 
            var uca = userCustomActions.FirstOrDefault(_ => _.Name == ucaName);
            if (uca != null)
            {
                uca.DeleteObject();
                ctx.Load(uca);
                ctx.ExecuteQuery();
            }
        }

        #endregion Manage User Custom Actions

        #region Manage Web Part Gallery

        private static string[] SupportedWebPartFileExtensions { get { if (_supportedWebPartFileExtensions == null) { _supportedWebPartFileExtensions = new string[] { ".dwp", ".webpart", }; } return _supportedWebPartFileExtensions; } } private static string[] _supportedWebPartFileExtensions = null;

        public static ListItemCollection GetWebPartGalleryItems(this ClientContext ctx)
        {
            var list = ctx.Web.GetCatalog((int)ListTemplateType.WebPartCatalog);
            var camlQuery = CamlQuery.CreateAllItemsQuery(1000);
            var items = list.GetItems(camlQuery);
            ctx.Load(items);
            ctx.ExecuteQuery();
            return items;
        }
        public static void AddOrUpdateCustomWebPartGalleryItems(this ClientContext ctx, string folderName, string groupName, string[] recommendationSettings = null)
        {
            if (string.IsNullOrEmpty(folderName) == true) { throw new ArgumentNullException(nameof(folderName)); }
            if (string.IsNullOrEmpty(groupName) == true) { throw new ArgumentNullException(nameof(groupName)); }
            // 
            DirectoryInfo directory = new DirectoryInfo(HostingEnvironment.MapPath($@"~/AddIns/{folderName}"));
            if (directory.Exists == false) { throw new ArgumentNullException(nameof(directory)); }
            // 
            var list = ctx.Web.GetCatalog((int)ListTemplateType.WebPartCatalog);
            var folder = list.RootFolder;
            ctx.Load(folder);
            ctx.ExecuteQuery();
            // 
            foreach (FileInfo file in directory.GetFiles("*", SearchOption.AllDirectories))
            {
                if (file.Exists == false) { continue; } // throw new ArgumentNullException(nameof(file));
                if (SupportedWebPartFileExtensions.Any(_ => (file.Extension ?? string.Empty).Equals(_, StringComparison.OrdinalIgnoreCase) == true) == false) { continue; } // throw new ArgumentNullException(nameof(file));
                // 
                using (var stream = file.OpenRead())
                {
                    FileCreationInformation fileInfo = new FileCreationInformation();
                    fileInfo.ContentStream = stream;
                    fileInfo.Overwrite = true;
                    fileInfo.Url = file.Name;
                    // 
                    var spFile = folder.Files.Add(fileInfo);
                    var item = spFile.ListItemAllFields;
                    ctx.Load(item);
                    ctx.ExecuteQuery();
                    // 
                    item["Group"] = groupName;
                    if (recommendationSettings?.Any(_ => string.IsNullOrEmpty(_) == false) == true) { item["QuickAddGroups"] = string.Join(";#", recommendationSettings.Where(_ => string.IsNullOrEmpty(_) == false)); }
                    item.Update();
                    ctx.ExecuteQuery();
                }
            }
        }
        public static void RemoveCustomWebPartGalleryItems(this ClientContext ctx, string groupName)
        {
            if (string.IsNullOrEmpty(groupName) == true) { throw new ArgumentNullException(nameof(groupName)); }
            // 
            groupName = groupName.Trim();
            var list = ctx.Web.GetCatalog((int)ListTemplateType.WebPartCatalog);
            var camlQuery = new CamlQuery() { ViewXml = $@"<View><Query><Where><Eq><FieldRef Name='Group'/><Value Type='Text'>{groupName}</Value></Eq></Where></Query><RowLimit>1000</RowLimit></View>", };
            var items = list.GetItems(camlQuery);
            ctx.Load(items);
            ctx.ExecuteQuery();
            // 
            foreach (var item in items)
            {
                if (item == null) { throw new ArgumentNullException(nameof(item)); }
                var itemGroup = ((item["Group"] as string) ?? string.Empty);
                if (itemGroup.Equals(groupName.Trim(), StringComparison.OrdinalIgnoreCase) == false) { continue; } //if (Regex.IsMatch(itemGroup, Regex.Escape(groupName), RegexOptions.IgnoreCase) == false) { continue; }
                item.DeleteObject();
                ctx.ExecuteQuery();
            }
        }

        public static string GetWebPartDefinitionXml(string folderName, string webPartFileName)
        {
            if (string.IsNullOrEmpty(folderName) == true) { throw new ArgumentNullException(nameof(folderName)); }
            if (string.IsNullOrEmpty(webPartFileName) == true) { throw new ArgumentNullException(nameof(webPartFileName)); }
            // 
            DirectoryInfo directory = new DirectoryInfo(HostingEnvironment.MapPath($@"~/AddIns/{folderName}"));
            if (directory.Exists == false) { throw new ArgumentNullException(nameof(directory)); }
            // 
            FileInfo wpFile = directory.GetFiles(webPartFileName, SearchOption.AllDirectories).SingleOrDefault();
            if (wpFile?.Exists != true) { throw new ArgumentNullException(nameof(wpFile)); }
            // 
            string ret = null;
            using (var stream = wpFile.OpenText()) { ret = stream.ReadToEnd(); }
            // 
            return ret;
        }

        #endregion Manage Web Part Gallery

        #region Pages

        public static string GetWelcomePageServerRelativeUrl(this ClientContext ctx)
        {
            var web = ctx.Web; ctx.Load(web, _ => _.ServerRelativeUrl);
            var rootFolder = web.RootFolder; ctx.Load(rootFolder, _ => _.WelcomePage);
            ctx.ExecuteQuery();
            // 
            string pageServerRelativeUrl = $@"{web.ServerRelativeUrl}/{rootFolder.WelcomePage}";
            return pageServerRelativeUrl;
        }

        public static void AddOrUpdateWebPartToPage(this ClientContext ctx, string pageServerRelativeUrl, string webPartXml, string webPartTitle = null, string zoneId = null, int? zoneIndex = null)
        {
            if (string.IsNullOrEmpty(pageServerRelativeUrl) == true) { throw new ArgumentNullException(nameof(pageServerRelativeUrl)); }
            if (string.IsNullOrEmpty(webPartXml) == true) { throw new ArgumentNullException(nameof(webPartXml)); }
            // 
            var page = ctx.Web.GetFileByServerRelativeUrl(pageServerRelativeUrl); ctx.Load(page);
            var item = page.ListItemAllFields; //ctx.Load(item, _ => _.ParentList);
            var list = item.ParentList; ctx.Load(list, _ => _.ForceCheckout, _ => _.EnableVersioning, _ => _.EnableVersioning, _ => _.EnableMinorVersions, _ => _.EnableModeration);
            ctx.ExecuteQuery();
            bool checkoutRequiredQ = (list.ForceCheckout == true);
            CheckinType checkInType = ((list.EnableVersioning == true) ? (list.EnableMinorVersions == true) ? CheckinType.MinorCheckIn : CheckinType.OverwriteCheckIn : CheckinType.OverwriteCheckIn);
            bool moderationEnabledQ = (list.EnableModeration == true);
            // 
            bool pageChangedQ = false, pageCheckedOutQ = false;
            try
            {
                // Check out
                if (checkoutRequiredQ == true && page.CheckOutType != CheckOutType.Online) { page.CheckOut(); pageCheckedOutQ = true; }
                // 
                // Gets the webparts available on the page
                var wpm = page.GetLimitedWebPartManager(PersonalizationScope.Shared);
                ctx.Load(wpm.WebParts, _ => _.Include(_2 => _2.WebPart.Title));
                ctx.ExecuteQuery();
                // 
                // Check if the current webpart already exists.
                var webPartDef = ((string.IsNullOrEmpty(webPartTitle) == false) ? wpm.WebParts.Cast<WebPartDefinition>().Where(_ => _.WebPart.Title == webPartTitle).FirstOrDefault() : null);
                if (webPartDef == null)
                {
                    // Import the webpart xml
                    var importedWebPartDef = wpm.ImportWebPart(webPartXml);
                    #region Try getting the Title,ZoneID,ZoneIndex from 'webPartXml'
                    string importedWebPartDefTitle = null; try { importedWebPartDefTitle = importedWebPartDef.WebPart.Title; } catch { }
                    string importedWebPartDefZoneID = null;
                    int? importedWebPartDefZoneIndex = null; try { importedWebPartDefZoneIndex = importedWebPartDef.WebPart.ZoneIndex; } catch { }
                    try
                    {
                        XmlDocument xmlDoc = new XmlDocument(); xmlDoc.LoadXml(webPartXml); XmlElement xmlWebPartDef = xmlDoc.DocumentElement;
                        if (importedWebPartDefTitle == null) { importedWebPartDefTitle = xmlWebPartDef.SelectSingleNode(@"*[local-name()='Title']")?.InnerText; }
                        if (importedWebPartDefZoneID == null) { importedWebPartDefZoneID = xmlWebPartDef.SelectSingleNode(@"*[local-name()='ZoneID']")?.InnerText; }
                        if (importedWebPartDefZoneIndex == null) { var tmp = xmlWebPartDef.SelectSingleNode(@"*[local-name()='PartOrder']")?.InnerText; var tmp2 = 0; if (int.TryParse(tmp, out tmp2) == true) { importedWebPartDefZoneIndex = tmp2; } }
                    }
                    catch (Exception ex) { Logger.Warning(ex, $@"Could not parse the webPartXml to retrive the WebPart Defintion Title and ZoneId."); }
                    #endregion Try getting the Title,ZoneID,ZoneIndex from 'webPartXml'
                    // 
                    webPartDef = ((string.IsNullOrEmpty(importedWebPartDefTitle) == false) ? wpm.WebParts.Cast<WebPartDefinition>().Where(_ => _.WebPart.Title == importedWebPartDefTitle).FirstOrDefault() : null);
                    if (webPartDef == null)
                    {
                        webPartDef = importedWebPartDef;
                        if (string.IsNullOrEmpty(webPartTitle) == true) { webPartTitle = ((string.IsNullOrEmpty(importedWebPartDefTitle) == false) ? importedWebPartDefTitle : "Untitled WebPart"); } // try to determine the 'zoneId', when this is not provided explicitly.
                        if (string.IsNullOrEmpty(zoneId) == true) { zoneId = ((string.IsNullOrEmpty(importedWebPartDefZoneID) == false) ? importedWebPartDefZoneID : "wpz"); } // try to determine the 'zoneId', when this is not provided explicitly.
                        if (zoneIndex == null) { zoneIndex = importedWebPartDefZoneIndex ?? 0; } // try to determine the 'zoneIndex', when this is not provided explicitly.
                        // 
                        // Add it to page
                        webPartDef.WebPart.Title = webPartTitle;
                        var addedWebPartDef = wpm.AddWebPart(webPartDef.WebPart, zoneId, zoneIndex ?? 0); ctx.Load(addedWebPartDef, _ => _.Id);
                        //page = ctx.Web.GetFileByServerRelativeUrl(pageServerRelativeUrl); ctx.Load(page); // re-load page file
                        item = page.ListItemAllFields; ctx.Load(item); // load page item
                        ctx.ExecuteQuery();
                        // 
                        if (item != null)
                        {
                            bool itemChangedQ = false;
                            foreach (string pageFieldName in new string[] { "WikiField", "PublishingPageContent" }.Where(_ => item.FieldValues.ContainsKey(_)))
                            {
                                item[pageFieldName] += $@"

<div class=""ms-rtestate-read ms-rte-wpbox"">
    <div class=""ms-rtestate-notify  ms-rtestate-read {addedWebPartDef.Id}"" id=""div_{addedWebPartDef.Id}""></div>
    <div id=""vid_{addedWebPartDef.Id}"" style=""display:none;"">
    </div>
</div>

";
                                itemChangedQ = true;
                            }
                            if (itemChangedQ == true)
                            {
                                item.Update();
                                ctx.ExecuteQuery();
                            }
                        }
                        // 
                        pageChangedQ = true;
                    }
                }
                if (pageChangedQ == false && webPartDef != null)
                {
                    bool changedQ = false;
                    if (string.IsNullOrEmpty(webPartTitle) == false) { webPartDef.WebPart.Title = webPartTitle; changedQ = true; }
                    //if (zoneId != null) { webPartDef.WebPart.ZoneId = zoneId ?? ""; changedQ = true; }
                    //if (zoneIndex != null) { webPartDef.WebPart.ZoneIndex = zoneIndex ?? 0; changedQ = true; }
                    if (changedQ == true)
                    {
                        // Update it on page
                        webPartDef.SaveWebPartChanges();
                        ctx.ExecuteQuery();
                        // 
                        pageChangedQ = true;
                    }
                }
            }
            finally
            {
                if (pageCheckedOutQ == true)
                {
                    if (pageChangedQ == true)
                    {
                        // Check in
                        page.CheckIn($@"Added WebPart '{webPartTitle}' to the Page.", checkInType);
                    }
                    else
                    {
                        // Discard Check out
                        page.UndoCheckOut();
                    }
                }
            }
        }
        public static void RemoveWebPartFromPage(this ClientContext ctx, string pageServerRelativeUrl, string webPartTitle)
        {
            if (string.IsNullOrEmpty(pageServerRelativeUrl) == true) { throw new ArgumentNullException(nameof(pageServerRelativeUrl)); }
            if (string.IsNullOrEmpty(webPartTitle) == true) { throw new ArgumentNullException(nameof(webPartTitle)); }
            // 
            var page = ctx.Web.GetFileByServerRelativeUrl(pageServerRelativeUrl); ctx.Load(page);
            var item = page.ListItemAllFields; //ctx.Load(item, _ => _.ParentList);
            var list = item.ParentList; ctx.Load(list, _ => _.ForceCheckout, _ => _.EnableVersioning, _ => _.EnableVersioning, _ => _.EnableMinorVersions, _ => _.EnableModeration);
            ctx.ExecuteQuery();
            bool checkoutRequiredQ = (list.ForceCheckout == true);
            CheckinType checkInType = ((list.EnableVersioning == true) ? (list.EnableMinorVersions == true) ? CheckinType.MinorCheckIn : CheckinType.OverwriteCheckIn : CheckinType.OverwriteCheckIn);
            bool moderationEnabledQ = (list.EnableModeration == true);
            // 
            bool pageChangedQ = false, pageCheckedOutQ = false;
            try
            {
                // Check out
                if (checkoutRequiredQ == true && page.CheckOutType != CheckOutType.Online) { page.CheckOut(); pageCheckedOutQ = true; }
                // 
                // Gets the webparts available on the page
                var wpm = page.GetLimitedWebPartManager(PersonalizationScope.Shared);
                ctx.Load(wpm.WebParts, _ => _.Include(_2 => _2.WebPart.Title));
                ctx.ExecuteQuery();
                // 
                // Check if the current webpart already exists.
                var webPartDef = wpm.WebParts.Cast<WebPartDefinition>().Where(_ => _.WebPart.Title == webPartTitle).FirstOrDefault();
                if (webPartDef != null)
                {
                    // Remove it from page
                    webPartDef.DeleteWebPart();
                    ctx.ExecuteQuery();
                    // 
                    pageChangedQ = true;
                }
                // 
            }
            finally
            {
                if (pageCheckedOutQ == true)
                {
                    if (pageChangedQ == true)
                    {
                        // Check in
                        page.CheckIn($@"Removed WebPart '{webPartTitle}' from the Page.", checkInType);
                    }
                    else
                    {
                        // Discard Check out
                        page.UndoCheckOut();
                    }
                }
            }
        }

        #endregion Pages
    }
}
