using LinkedIn.Api.Client.Core.Profiles;
using Newtonsoft.Json;
using SP365.AddIn.Services.DataAccess.Models;
using SP365.AddIn.Services.Logic;
using System;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Web;
using System.Web;

namespace SP365.AddIn.Services
{
    [ServiceContract]
    public interface ILinkedInService
    {
        [OperationContract, CORSEnabledOperation, WebInvoke(Method = "GET", UriTemplate = "/GetFullProfile", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]LinkedInResponse GetFullProfile();
        [OperationContract, CORSEnabledOperation, WebInvoke(Method = "GET", UriTemplate = "/GetBasicProfile", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]LinkedInResponse GetBasicProfile();
    }

    [ServiceBehavior(IncludeExceptionDetailInFaults = true, InstanceContextMode = InstanceContextMode.PerSession, AddressFilterMode = AddressFilterMode.Any), AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Required)]
    public class LinkedInService : BasePortalService, ILinkedInService
    {
        public LinkedInResponse GetFullProfile()
        {
            LinkedInResponse ret = new LinkedInResponse();
            // 
            try
            {
                using (LinkedInLogic linkedInLogic = new LinkedInLogic())
                {
                    ApplicationUser user = linkedInLogic.EnsureUserIsAuthenticated();
                    // 
                    string accessToken = linkedInLogic.GetAccessToken(user);
                    if (string.IsNullOrEmpty(accessToken) == true) { throw new UserNotAuthenticatedException(); }
                    // 
                    LinkedInFullProfile profile = linkedInLogic.GetFullProfile(accessToken);
                    if (profile == null) { throw new ArgumentNullException(nameof(profile)); }
                    // 
                    string json = JsonConvert.SerializeObject(profile);
                    ret = new LinkedInResponse() { Status = "ok", ErrorMessage = null, RedirectUrl = null, JSON = json, };
                }
            }
            catch (UserNotAuthenticatedException ex) { ret = new LinkedInResponse() { Status = "error", ErrorMessage = ex.Message, RedirectUrl = getChallengeRedirectUrl(), JSON = null, }; }
            catch (Exception ex) { ret = new LinkedInResponse() { Status = "error", ErrorMessage = ex.Message, RedirectUrl = null, JSON = null, }; }
            // 
            return ret;
        }
        public LinkedInResponse GetBasicProfile()
        {
            LinkedInResponse ret = new LinkedInResponse();
            // 
            try
            {
                using (LinkedInLogic linkedInLogic = new LinkedInLogic())
                {
                    ApplicationUser user = linkedInLogic.EnsureUserIsAuthenticated();
                    // 
                    string accessToken = linkedInLogic.GetAccessToken(user);
                    if (string.IsNullOrEmpty(accessToken) == true) { throw new UserNotAuthenticatedException(); }
                    // 
                    LinkedInBasicProfile profile = linkedInLogic.GetBasicProfile(accessToken);
                    if (profile == null) { throw new ArgumentNullException(nameof(profile)); }
                    // 
                    string json = JsonConvert.SerializeObject(profile);
                    ret = new LinkedInResponse() { Status = "ok", ErrorMessage = null, RedirectUrl = null, JSON = json, };
                }
            }
            catch (UserNotAuthenticatedException ex) { ret = new LinkedInResponse() { Status = "error", ErrorMessage = ex.Message, RedirectUrl = getChallengeRedirectUrl(), JSON = null, }; }
            catch (Exception ex) { ret = new LinkedInResponse() { Status = "error", ErrorMessage = ex.Message, RedirectUrl = null, JSON = null, }; }
            // 
            return ret;
        }

        private string getChallengeRedirectUrl() { string providerName = @"LinkedIn", returnUrl = (HttpContext.Current?.Request?.UrlReferrer?.ToString() ?? "/"), hostUrl = HttpContext.Current?.Request?.Url?.GetLeftPart(UriPartial.Authority); return $@"{hostUrl?.TrimEnd(new char[] { '/' })}/Login/RegisterExternalLogin.aspx?Action=Challenge&ProviderName={providerName}&ReturnUrl={returnUrl}"; }
    }

    [DataContract]
    public class LinkedInResponse
    {
        [DataMember(Name = "status", IsRequired = false, EmitDefaultValue = false)]         public string Status { get; set; }
        [DataMember(Name = "errorMessage", IsRequired = false, EmitDefaultValue = false)]   public string ErrorMessage { get; set; }
        [DataMember(Name = "redirectUrl", IsRequired = false, EmitDefaultValue = false)]    public string RedirectUrl { get; set; }
        [DataMember(Name = "json", IsRequired = false, EmitDefaultValue = false)]           public string JSON { get; set; }
    }
}
