using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Text;
using System.Web.Services.Protocols;

namespace SP365.AddIn.Services
{
    public class BasePortalServiceBehaviorAttribute : Attribute, IServiceBehavior
    {
        #region Methods

        public void Validate(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase) { }
        public void AddBindingParameters(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase, Collection<ServiceEndpoint> endpoints, BindingParameterCollection bindingParameters) { }
        public void ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase host)
        {
            Logger.Debug(LogCategory.WebService, "ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase host) :: Adding custom Portal Behaviour to WCF.");
            // 
            foreach (ChannelDispatcher channelDispatcher in host.ChannelDispatchers)
            {
                #region Add custom ErrorHandlers
                channelDispatcher.ErrorHandlers.Add(new BasePortalServiceErrorHandler());
                #endregion Add custom ErrorHandlers
                // 
                #region //Configure each Endpoint
                //foreach (EndpointDispatcher endpointDispatcher in channelDispatcher.Endpoints)
                //{
                //    #region Add custom ErrorHandlers
                //    BasePortalServiceErrorHandler existingErrorHandler = endpointDispatcher.ChannelDispatcher.ErrorHandlers.OfType<BasePortalServiceErrorHandler>().FirstOrDefault();
                //    if (existingErrorHandler == null)
                //    {
                //        Logger.Debug(LogCategory.WebService, string.Format("ApplyDispatchBehavior :: Adding custom ErrorHandler to WCF Endpoint '{0}'.", endpointDispatcher.EndpointAddress.Uri));
                //        endpointDispatcher.ChannelDispatcher.ErrorHandlers.Add(new BasePortalServiceErrorHandler());
                //    }
                //    else // already added
                //    {
                //        Logger.Verbose(LogCategory.WebService, string.Format("ApplyDispatchBehavior :: Custom ErrorHandler already added to WCF Endpoint '{0}'.", endpointDispatcher.EndpointAddress.Uri));
                //    }
                //    #endregion Add custom ErrorHandlers
                //    // 
                //    #region //Add custom MessageInspectors
                //    //endpointDispatcher.DispatchRuntime.MessageInspectors.Add(new ValidateSPFormDigestInspector());
                //    #endregion //Add custom MessageInspectors
                //}
                #endregion //Configure each Endpoint
            }
        }

        #endregion Methods
    }

    public class BasePortalServiceErrorHandler : IErrorHandler
    {
        #region Properties

        //protected Type TypeSPThreadContext { get { if (_typeSPThreadContext == null) { _typeSPThreadContext = typeof(SPUtility).Assembly.GetType("Microsoft.SharePoint.Utilities.SPThreadContext"); } return _typeSPThreadContext; } } private Type _typeSPThreadContext = null;
        //protected PropertyInfo PropertyInfoSPThreadContextItems { get { if (TypeSPThreadContext != null) { _propertyInfoSPThreadContextItems = TypeSPThreadContext.GetProperty("Items", BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance); } return _propertyInfoSPThreadContextItems; } } private PropertyInfo _propertyInfoSPThreadContextItems = null;
        //protected IDictionary SPThreadContextItems { get { PropertyInfo pi = PropertyInfoSPThreadContextItems; return ((pi != null) ? (pi.GetValue(null, null) as IDictionary) : null); } }
        //protected Exception SPThreadContextItemsRecoverException { get { IDictionary items = SPThreadContextItems; return ((items != null) ? (items["Recover_ErrorMessage"] as Exception) : null); } }

        #endregion Properties

        #region Methods

        public bool HandleError(Exception error)
        {
            return true;
        }
        public void ProvideFault(Exception error, MessageVersion version, ref Message fault)
        {
            Message newFault = this.GetNewFaultMessage(version, error);
            //Logger.Debug(LogCategory.WebService, string.Format("ProvideFault :: Did an override of the Fault Message to return as JSON with error '{0}'.", newFault));
            fault = newFault; // override
        }
        protected virtual Message GetNewFaultMessage(MessageVersion version, Exception error)
        {
            Exception ex = error;
            // 
            if (ex != null && ex.Message == "Access is denied.") { Logger.Warning(LogCategory.WebService, "Please note: The default SecurityException behaviour is not overriden. Any Exception thrown as a SecurityException will end up in a 'text/html' response."); } // NOTE: code below will not be able to handle SecurityException's... as the Threads are aborted before this completes.
            //Exception spEx = SPThreadContextItemsRecoverException;
            //if (spEx != null) { ex = spEx; }
            // 
            bool serviceDebug = (OperationContext.Current.EndpointDispatcher.ChannelDispatcher.IncludeExceptionDetailInFaults == true);
            // 
            List<Type> knownTypes = new List<Type>();
            PropertyInfo piDetail = ((error != null && error is FaultException) ? error.GetType().GetProperty("Detail") : null); object faultDetail = ((piDetail != null) ? piDetail.GetGetMethod().Invoke(error, null) : null);
            PropertyInfo piStatusCode = ((error != null && error is FaultException) ? error.GetType().GetProperty("StatusCode") : null); HttpStatusCode? faultStatusCode = (((piStatusCode != null) ? piStatusCode.GetGetMethod().Invoke(error, null) : null) as HttpStatusCode?);
            PropertyInfo piStatusDescription = ((error != null && error is FaultException) ? error.GetType().GetProperty("StatusDescription") : null); string faultStatusDescription = (((piStatusDescription != null) ? piStatusDescription.GetGetMethod().Invoke(error, null) : null) as string);
            if (faultDetail != null) { knownTypes.Add(faultDetail.GetType()); }
            else
            {
                if (faultDetail is Exception) { ex = (faultDetail as Exception); }
                // 
                const string defaultMessage = @"Sorry, an unexpected error occurred.";
                string message = null, fullMessage = null;
                // override the default reason message
                //if (ex is ActiveDirectoryUserNotFoundException) { message = @"The user could not be found."; }
                //else if (ex is ActiveDirectoryUserLockedOutException) { message = @"The user is locked out! Please reset your password."; }
                //else if (ex is ActiveDirectoryException) { } // ignore any other sensitive messages (with usernames)
                //else if (ex is UserNotAuthenticatedException) { faultStatusCode = HttpStatusCode.Unauthorized; faultStatusDescription = "Unauthorized"; }
                if (ex is SecurityException) { message = @"Access is denied. Please make sure you're Signed-in and have correct permissions to access this resource."; faultStatusCode = HttpStatusCode.Unauthorized; faultStatusDescription = "Unauthorized"; }
                else if (ex is ArgumentNullException) { message = @"Invalid request! One or more properties were not provided."; }
                else if (ex is NullReferenceException) { message = defaultMessage; }
                else if (ex is System.Data.Entity.Validation.DbEntityValidationException)
                {
                    System.Data.Entity.Validation.DbEntityValidationException dbEx = (ex as System.Data.Entity.Validation.DbEntityValidationException);
                    string dbErrorMessages = string.Join($@";{Environment.NewLine}", dbEx.EntityValidationErrors.Select(_ => $@"Entity:{_.Entry.Entity}. Errors:{string.Join(", ", _.ValidationErrors.SelectMany(_2 => $@"[{_2.PropertyName}]: {_2.ErrorMessage}"))}"));
                    message = defaultMessage;
                    faultDetail = $@"There was a Database error ({ex.Message}). The validation errors were: {dbErrorMessages}";
                }
                //else if (ex is Microsoft.Xrm.Sdk.SaveChangesException)
                //{
                //    message = defaultMessage;
                //    // 
                //    Microsoft.Xrm.Sdk.SaveChangesException ex2 = (ex as Microsoft.Xrm.Sdk.SaveChangesException);
                //    string crmErrorMessage = string.Format(@"CRM Error on SaveChangesResults - {0}", ((ex2.Results != null) ? string.Join(", ", ex2.Results.Select(_ => _.Error)) : "<null>"));
                //    Logger.Error(ex, LogCategory.WebService, crmErrorMessage);
                //    // 
                //    if (serviceDebug == true) { fullMessage = string.Join(Environment.NewLine, new string[] { crmErrorMessage, fullMessage, }.Where(_ => string.IsNullOrEmpty(_) == false)); }
                //}
                else if (ex is PortalException) { message = ex.Message; }
                //else if (ex is Exception && string.IsNullOrEmpty(ex.Message) == false) { message = ex.Message; }
                // 
                if (string.IsNullOrEmpty(message) == true) { message = defaultMessage; }
                if (string.IsNullOrEmpty(fullMessage) == true && ex != null && serviceDebug == true) { fullMessage = getFullMessage(ex); }
                // 
                Logger.Error(ex, LogCategory.WebService, string.Format("ProvideFault :: Did an override of the Fault Message with Message '{0}'.", message));
                // 
                faultDetail = new FaultInfo() { Message = message, Type = ((ex != null) ? ex.GetType().FullName : null), Details = ((serviceDebug == true) ? fullMessage : null), };
                knownTypes.Add(typeof(FaultInfo));
            }
            // 
            Message faultMessage = Message.CreateMessage(version, "", faultDetail, new DataContractJsonSerializer(faultDetail.GetType(), knownTypes));
            faultMessage.Properties.Add(WebBodyFormatMessageProperty.Name, new WebBodyFormatMessageProperty(WebContentFormat.Json));
            HttpResponseMessageProperty rmp = new HttpResponseMessageProperty() { StatusCode = (faultStatusCode ?? HttpStatusCode.BadRequest), StatusDescription = (faultStatusDescription ?? "Bad Request"), };
            rmp.Headers[System.Net.HttpResponseHeader.ContentType] = "application/json";
            faultMessage.Properties.Add(HttpResponseMessageProperty.Name, rmp);
            // 
            return faultMessage;
        }
        private static string getFullMessage(Exception ex)
        {
            return formatExceptionAux(ex, 0);
        }
        private static string formatExceptionAux(Exception ex, int indentation)
        {
            string ind = new string('\t', indentation);
            StringBuilder str = new StringBuilder();
            str.AppendLine(string.Format(@"{0}{1}: {2}", ind, ex.GetType().FullName, ex.Message));
            str.AppendLine(string.Format(@"{0}Stack: {1}", ind, ex.StackTrace));
            if (ex.Data != null && ex.Data.Count > 0)
            {
                int i = 0;
                string[] keyDataPairs = new string[ex.Data.Count];
                foreach (object key in ex.Data.Keys)
                {
                    object val = ex.Data[key];
                    keyDataPairs[i++] = string.Format("{0}:{1}", key, val);
                }
                str.AppendLine(string.Format(@"{0}Data: {{{1}}}", ind, string.Join(",", keyDataPairs)));
            }
            // 
            SoapException sEx = (ex as SoapException);
            if (sEx != null && sEx.Detail != null) { str.AppendLine(string.Format(@"{0}DetailXml: {1}", ind, sEx.Detail.OuterXml)); }
            // 
            //Microsoft.Xrm.Sdk.SaveChangesException crmEx = (ex as Microsoft.Xrm.Sdk.SaveChangesException);
            //if (crmEx != null) { str.AppendLine(string.Format(@"{0}CrmResults: {1}", ind, ((crmEx.Results != null) ? string.Join(", ", crmEx.Results.Select(_ => _.Error)) : "<null>"))); }
            // 
            if (ex.InnerException != null) { str.AppendLine(string.Format(@"{0}InnerException: {1}", ind, formatExceptionAux(ex.InnerException, indentation + 1))); }
            return str.ToString();
        }

        #endregion Methods
    }

    #region Helper Types

    [DataContract]
    public sealed class FaultInfo
    {
        public FaultInfo() { }
        public FaultInfo(Exception ex) : this((string)((ex != null) ? ex.Message : null), ex) { }
        public FaultInfo(string message, Exception ex) { this.Type = ((ex != null) ? ex.GetType().FullName : null); this.Message = message; this.Details = ((ex != null) ? Logger.FormatException(ex) : null); ; }

        [DataMember(Name = "errorType", EmitDefaultValue = false, IsRequired = false)]          public string Type { get; set; }
        [DataMember(Name = "errorThrown", EmitDefaultValue = false, IsRequired = false)]        public string Message { get; set; }
        [DataMember(Name = "errorThrownFull", EmitDefaultValue = false, IsRequired = false)]    public string Details { get; set; }
    }

    #endregion Helper Types
}
