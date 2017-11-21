using Owin;
using System;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Web;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SP365.AddIn.Services
{
    [ServiceContract]
    public interface IPTVService
    {
        [OperationContract, CORSEnabledOperation, WebInvoke(Method = "GET", UriTemplate = "/Redirect/{url}", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)] void Redirect(string url);
    }

    [ServiceBehavior(IncludeExceptionDetailInFaults = true, InstanceContextMode = InstanceContextMode.PerSession, AddressFilterMode = AddressFilterMode.Any), AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Required)]
    public class PTVService : BasePortalService, IPTVService
    {
        public void Redirect(string url)
        {
            string redirectAbsoluteUrlWithSignature = GetPTVApiUrlWithSignature(url, scheme: this.HttpRequest?.Url?.Scheme);
            // 
            WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.Redirect;
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Location", redirectAbsoluteUrlWithSignature);
        }

        private static readonly Regex _rxPTVPath = new Regex(@"/(Services/)?PTV(\.svc/Redirect)?(?<url>/.+$)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static void RegisterRouteMapping(IAppBuilder app)
        {
            app.Use((context, next) =>
            {
                string pathWithQuery = ((context.Request.Path != null && string.IsNullOrEmpty(context.Request.Path.Value) == false) ? (context.Request.QueryString != null && string.IsNullOrEmpty(context.Request.QueryString.Value) == false) ? $@"{context.Request.Path.Value}?{context.Request.QueryString.Value}" : context.Request.Path.Value : null);
                if (string.IsNullOrEmpty(pathWithQuery) == false && _rxPTVPath.IsMatch(pathWithQuery) == true)
                {
                    string url = _rxPTVPath.Match(pathWithQuery)?.Groups["url"]?.Value;
                    if (string.IsNullOrEmpty(url) == false)
                    {
                        //if (context.Request.User != null && context.Request.User.IsInRole("Employee")) { return context.Response.WriteAsync("You are not authorized to use this resource."); }
                        // 
                        string redirectUrl = PTVService.GetPTVApiUrlWithSignature(url, scheme: (context.Request.IsSecure ? "https" : "http"));
                        if (string.IsNullOrEmpty(redirectUrl) == false)
                        {
                            context.Response.Redirect(redirectUrl);
                            return Task.FromResult<object>(null);
                        }
                    }
                }
                return next.Invoke();
            });
        }

        public static string GetPTVApiUrlWithSignature(string url, string scheme = "https")
        {
            Uri baseApiUri = (
                Uri.IsWellFormedUriString(url, UriKind.Absolute) ? new Uri(url)
                : (string.IsNullOrEmpty(PTVSettings.Default.BaseApiUrl) == false) ? new Uri(PTVSettings.Default.BaseApiUrl)
                : new Uri($@"{scheme ?? "https"}://{PTVSettings.DefaultBaseApiHostName}")
            );
            string serverRelativeUrl = ((Uri.IsWellFormedUriString(url, UriKind.Relative) ? url : baseApiUri.PathAndQuery));
            string serverRelativeUrlWithSignature = appendPTVSignatureToUrl(serverRelativeUrl);
            return new Uri(baseApiUri, serverRelativeUrlWithSignature)?.ToString();
        }

        private static string appendPTVSignatureToUrl(string serverRelativeUrl)
        {
            // supplied by PTV
            string apiKey = PTVSettings.Default.ApiKey, userId = PTVSettings.Default.UserId;
            // 
            serverRelativeUrl = string.Format("{0}{1}devid={2}", serverRelativeUrl, serverRelativeUrl.Contains("?") ? "&" : "?", userId);
            ASCIIEncoding encoding = new ASCIIEncoding();
            byte[] keyBytes = encoding.GetBytes(apiKey), urlBytes = encoding.GetBytes(serverRelativeUrl);
            byte[] tokenBytes = new System.Security.Cryptography.HMACSHA1(keyBytes).ComputeHash(urlBytes);
            var sb = new StringBuilder(); Array.ForEach<byte>(tokenBytes, x => sb.Append(x.ToString("X2"))); // convert signature to string
            string urlWithSignature = string.Format("{0}&signature={1}", serverRelativeUrl, sb.ToString()); // add signature to url
            // 
            return urlWithSignature;
        }
    }
}
