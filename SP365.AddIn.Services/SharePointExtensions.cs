using Microsoft.IdentityModel;
using Microsoft.IdentityModel.S2S.Protocols.OAuth2;
using Microsoft.IdentityModel.S2S.Tokens;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.EventReceivers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IdentityModel.Selectors;
using System.IdentityModel.Tokens;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.ServiceModel;
using System.Text;
using System.Web;
using System.Web.Configuration;
using System.Web.Script.Serialization;
using AudienceRestriction = Microsoft.IdentityModel.Tokens.AudienceRestriction;
using AudienceUriValidationFailedException = Microsoft.IdentityModel.Tokens.AudienceUriValidationFailedException;
using SecurityTokenHandlerConfiguration = Microsoft.IdentityModel.Tokens.SecurityTokenHandlerConfiguration;
using X509SigningCredentials = Microsoft.IdentityModel.SecurityTokenService.X509SigningCredentials;

namespace SP365.AddIn.Services
{
    internal static class SharePointExtensions
    {
        #region Manage User Custom Actions

        public static void addOrUpdateUCAScriptLinkWithInternalScriptSrc(this ClientContext ctx, string ucaName, string scriptSrc, int sequence = 10)
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
        public static void addOrUpdateUCAScriptLinkWithExternalAsynchScriptSrc(this ClientContext ctx, string ucaName, string scriptSrc, int sequence = 10)
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
        public static void addOrUpdateUCAScriptLinkWithExternalSynchScriptSrc(this ClientContext ctx, string ucaName, string scriptSrc, int sequence = 10)
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
(function(){{
try{{
var r=(typeof(XMLHttpRequest)?new XMLHttpRequest():typeof(createXMLHTTPObject)=='function'?createXMLHTTPObject():null);r.open('GET','{scriptSrc}',false);r.send();
var m=(function(s){{eval(s);return (typeof(exports)!='undefined'?exports:undefined);}})(r.responseText);
}}catch(e){{
if(window.console&&window.console.log){{window.console.log('- could not load script synchronously. going to load assynchronously.');}}
document.write('<script type=""text/javascript"" src=""{scriptSrc}""></'+'script>');
}}
}})();
";
            uca.Sequence = sequence;
            uca.Update();
            // 
            ctx.Load(uca);
            ctx.ExecuteQuery();
        }
        public static void deleteUCA(this ClientContext ctx, string ucaName)
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
    }
}
