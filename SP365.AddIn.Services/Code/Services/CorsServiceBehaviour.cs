using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Web;

namespace SP365.AddIn.Services
{
    // implementation based on MSDN blog post: https://code.msdn.microsoft.com/windowsdesktop/Implementing-CORS-support-c1f9cd4b

    #region CORS Operation Behaviour

    public class CORSEnabledOperationAttribute : Attribute, IOperationBehavior
    {
        #region Constants

        internal const string Origin = "Origin";
        internal const string AccessControlAllowOrigin = "Access-Control-Allow-Origin";
        internal const string AccessControlAllowCredentials = "Access-Control-Allow-Credentials";
        internal const string AccessControlRequestMethod = "Access-Control-Request-Method";
        internal const string AccessControlRequestHeaders = "Access-Control-Request-Headers";
        internal const string AccessControlAllowMethods = "Access-Control-Allow-Methods";
        internal const string AccessControlAllowHeaders = "Access-Control-Allow-Headers";
        internal const string PreflightSuffix = "_preflight_";

        #endregion Constants

        internal static object _lock = new object();

        public void Validate(OperationDescription operationDescription) { }
        public void AddBindingParameters(OperationDescription operationDescription, BindingParameterCollection bindingParameters) { }
        public void ApplyClientBehavior(OperationDescription operationDescription, ClientOperation clientOperation) { }
        public void ApplyDispatchBehavior(OperationDescription operationDescription, DispatchOperation dispatchOperation)
        {
            Logger.Debug(LogCategory.WebService, "ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher) :: Adding custom CORS Behaviour to WCF Endpoint.");
            // 
            CORSEnabledOperationAttribute corsEnabledOperation = operationDescription.OperationBehaviors.OfType<CORSEnabledOperationAttribute>().FirstOrDefault();
            if (corsEnabledOperation != null)
            {
                DispatchRuntime dispatchRuntime = dispatchOperation.Parent;
                if (dispatchRuntime != null)
                {
                    lock (_lock)
                    {
                        CORSEnabledMessageInspector inspector = dispatchRuntime.MessageInspectors.OfType<CORSEnabledMessageInspector>().FirstOrDefault();
                        if (inspector == null)
                        {
                            inspector = new CORSEnabledMessageInspector();
                            dispatchRuntime.MessageInspectors.Add(inspector);
                        }
                        // 
                        inspector.AddOperation(operationDescription.Name, corsEnabledOperation);
                    }
                }
                else { } // not expected :: operation does not have a parent Endpoint?..
            }
            else { } // not expected :: operation is not a [CORSEnabledOperation]?..
        }
    }

    public class CORSEnabledEndpointBehaviour : BehaviorExtensionElement, IEndpointBehavior
    {
        public override Type BehaviorType { get { return typeof(CORSEnabledEndpointBehaviour); } }
        protected override object CreateBehavior() { return new CORSEnabledEndpointBehaviour(); }

        public void Validate(ServiceEndpoint endpoint) { }
        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters) { }
        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime) { }
        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            Logger.Debug(LogCategory.WebService, "ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher) :: Adding custom CORS Behaviour to WCF Endpoint.");
            // 
            DispatchRuntime dispatchRuntime = endpointDispatcher.DispatchRuntime;
            if (dispatchRuntime != null)
            {
                lock (CORSEnabledOperationAttribute._lock)
                {
                    CORSEnabledMessageInspector inspector = dispatchRuntime.MessageInspectors.OfType<CORSEnabledMessageInspector>().FirstOrDefault();
                    if (inspector == null)
                    {
                        inspector = new CORSEnabledMessageInspector();
                        dispatchRuntime.MessageInspectors.Add(inspector);
                    }
                    // 
                    foreach (OperationDescription operationDescription in endpoint.Contract.Operations)
                    {
                        CORSEnabledOperationAttribute corsEnabledOperation = operationDescription.Behaviors.Find<CORSEnabledOperationAttribute>();
                        if (corsEnabledOperation != null)
                        {
                            inspector.AddOperation(operationDescription.Name, corsEnabledOperation);
                        }
                        else { } // not a [CORSEnabledOperation] operation
                    }
                }
            }
            else { } // not expected :: operation does not have a parent Endpoint?..
        }
    }

    public class CORSEnabledMessageInspector : IDispatchMessageInspector
    {
        private Dictionary<string, CORSEnabledOperationAttribute> _corsEnabledOperations;
        public CORSEnabledMessageInspector() { this._corsEnabledOperations = new Dictionary<string, CORSEnabledOperationAttribute>(); }

        public object AfterReceiveRequest(ref Message request, IClientChannel channel, InstanceContext instanceContext)
        {
            HttpRequestMessageProperty httpProp = (request.Properties[HttpRequestMessageProperty.Name] as HttpRequestMessageProperty);
            if (httpProp != null)
            {
                object tmp; request.Properties.TryGetValue(WebHttpDispatchOperationSelector.HttpOperationNamePropertyName, out tmp); string operationName = ((tmp as string) ?? string.Empty);
                if (httpProp != null && string.IsNullOrEmpty(operationName) == false)
                {
                    CORSEnabledOperationAttribute corsEnabledOperation = this.GetOperation(operationName);
                    if (corsEnabledOperation != null)
                    {
                        string origin = httpProp.Headers[CORSEnabledOperationAttribute.Origin];
                        if (origin != null)
                        {
                            return origin;
                        }
                        else { } // does not contain Origin Header
                    }
                    else { } // not [CORSEnabled] operation contract
                }
                else { } // not operation contract
            }
            else { } // not HttpRequest
            return null;
        }
        public void BeforeSendReply(ref Message reply, object correlationState)
        {
            string origin = (correlationState as string);
            if (origin != null)
            {
                HttpResponseMessageProperty httpProp = null;
                if (reply.Properties.ContainsKey(HttpResponseMessageProperty.Name))
                {
                    httpProp = (HttpResponseMessageProperty)reply.Properties[HttpResponseMessageProperty.Name];
                }
                else
                {
                    httpProp = new HttpResponseMessageProperty();
                    reply.Properties.Add(HttpResponseMessageProperty.Name, httpProp);
                }
                // 
                httpProp.Headers.Add(CORSEnabledOperationAttribute.AccessControlAllowOrigin, origin);
                httpProp.Headers.Add(CORSEnabledOperationAttribute.AccessControlAllowCredentials, "true");
            }
            else { } // not [CORSEnabled] operation contract
        }

        public CORSEnabledOperationAttribute GetOperation(string operationName)
        {
            CORSEnabledOperationAttribute ret = null;
            // 
            if (string.IsNullOrEmpty(operationName) == true) { throw new ArgumentNullException(nameof(operationName)); }
            // 
            if (this._corsEnabledOperations.ContainsKey(operationName) == true)
            {
                ret = this._corsEnabledOperations[operationName];
            }
            // 
            return ret;
        }
        public void AddOperation(string operationName, CORSEnabledOperationAttribute corsEnabledOperation)
        {
            if (string.IsNullOrEmpty(operationName) == true) { throw new ArgumentNullException(nameof(operationName)); }
            if (corsEnabledOperation == null) { throw new ArgumentNullException(nameof(corsEnabledOperation)); }
            // 
            if (this._corsEnabledOperations.ContainsKey(operationName) == true) { this._corsEnabledOperations[operationName] = corsEnabledOperation; }
            else { this._corsEnabledOperations.Add(operationName, corsEnabledOperation); }
        }
    }

    #endregion CORS Operation Behaviour

    #region CORS Preflight Operation Behaviour

    public class PreflightOperationBehavior : IOperationBehavior
    {
        private OperationDescription _preflightOperation = null;
        private List<string> _allowedMethods = null;
        public PreflightOperationBehavior(OperationDescription preflightOperation)
        {
            this._preflightOperation = preflightOperation;
            this._allowedMethods = new List<string>();
        }

        public void Validate(OperationDescription operationDescription) { }
        public void AddBindingParameters(OperationDescription operationDescription, BindingParameterCollection bindingParameters) { }
        public void ApplyClientBehavior(OperationDescription operationDescription, ClientOperation clientOperation) { }
        public void ApplyDispatchBehavior(OperationDescription operationDescription, DispatchOperation dispatchOperation)
        {
            Logger.Debug(LogCategory.WebService, "ApplyDispatchBehavior(OperationDescription operationDescription, DispatchOperation dispatchOperation) :: Adding custom CORS Preflight Operation Behavior to WCF Endpoint.");
            // 
            dispatchOperation.Invoker = new PreflightOperationInvoker(operationDescription.Messages[1].Action, this._allowedMethods);
        }

        public void AddAllowedMethod(string httpMethod)
        {
            this._allowedMethods.Add(httpMethod);
        }

        internal static void AddPreflightOperations(ServiceEndpoint endpoint, List<OperationDescription> corsOperations)
        {
            Dictionary<string, PreflightOperationBehavior> uriTemplates = new Dictionary<string, PreflightOperationBehavior>(StringComparer.OrdinalIgnoreCase);
            // 
            foreach (OperationDescription operation in corsOperations)
            {
                if (operation.Behaviors.Find<WebGetAttribute>() != null || operation.IsOneWay == true)
                {
                    // no need to add preflight operation for GET requests, no support for 1-way messages
                    continue;
                }
                // 
                WebInvokeAttribute originalWia = operation.Behaviors.Find<WebInvokeAttribute>();
                string originalUriTemplate = ((originalWia != null && originalWia.UriTemplate != null) ? NormalizeTemplate(originalWia.UriTemplate) : operation.Name);
                // 
                string originalMethod = ((originalWia != null && originalWia.Method != null) ? originalWia.Method : "POST");
                if (uriTemplates.ContainsKey(originalUriTemplate) == true)
                {
                    // there is already an OPTIONS operation for this URI, we can reuse it 
                    PreflightOperationBehavior operationBehavior = uriTemplates[originalUriTemplate];
                    operationBehavior.AddAllowedMethod(originalMethod);
                }
                else
                {
                    ContractDescription contract = operation.DeclaringContract;
                    PreflightOperationBehavior preflightOperationBehavior = null;
                    OperationDescription preflightOperation = CreatePreflightOperation(operation, originalUriTemplate, originalMethod, out preflightOperationBehavior);
                    uriTemplates.Add(originalUriTemplate, preflightOperationBehavior);
                    // 
                    contract.Operations.Add(preflightOperation);
                }
            }
        }
        internal static OperationDescription CreatePreflightOperation(OperationDescription operation, string originalUriTemplate, string originalMethod, out PreflightOperationBehavior preflightOperationBehavior)
        {
            ContractDescription contract = operation.DeclaringContract;
            // 
            // create new Operation Contract for the Method="OPTIONS"
            OperationDescription ret = new OperationDescription(operation.Name + CORSEnabledOperationAttribute.PreflightSuffix, contract);
            // 
            // configure Messages
            {
                MessageDescription inputMessage = new MessageDescription(operation.Messages[0].Action + CORSEnabledOperationAttribute.PreflightSuffix, MessageDirection.Input);
                inputMessage.Body.Parts.Add(new MessagePartDescription("input", contract.Namespace) { Index = 0, Type = typeof(Message) });
                ret.Messages.Add(inputMessage);
            }
            {
                MessageDescription outputMessage = new MessageDescription(operation.Messages[1].Action + CORSEnabledOperationAttribute.PreflightSuffix, MessageDirection.Output);
                outputMessage.Body.ReturnValue = new MessagePartDescription(ret.Name + "Return", contract.Namespace) { Type = typeof(Message) };
                ret.Messages.Add(outputMessage);
            }
            // 
            // configure Behaviors
            {
                // configure default Behaviors
                ret.Behaviors.Add(new WebInvokeAttribute() { Method = "OPTIONS", UriTemplate = originalUriTemplate, });
                ret.Behaviors.Add(new DataContractSerializerOperationBehavior(ret));
                // 
                // configure custom Behavior
                preflightOperationBehavior = new PreflightOperationBehavior(ret);
                preflightOperationBehavior.AddAllowedMethod(originalMethod);
                ret.Behaviors.Add(preflightOperationBehavior);
            }
            // 
            return ret;
        }
        internal static string NormalizeTemplate(string uriTemplate)
        {
            int queryIndex = uriTemplate.IndexOf('?');
            if (queryIndex >= 0)
            {
                // no query string used for this
                uriTemplate = uriTemplate.Substring(0, queryIndex);
            }
            // 
            int paramIndex;
            while ((paramIndex = uriTemplate.IndexOf('{')) >= 0)
            {
                // Replacing all named parameters with wildcards
                int endParamIndex = uriTemplate.IndexOf('}', paramIndex);
                if (endParamIndex >= 0)
                {
                    uriTemplate = uriTemplate.Substring(0, paramIndex) + '*' + uriTemplate.Substring(endParamIndex + 1);
                }
            }
            // 
            return uriTemplate;
        }
    }

    public class PreflightOperationInvoker : IOperationInvoker
    {
        private string _replyAction = null;
        private List<string> _allowedHttpMethods = null;
        public PreflightOperationInvoker(string replyAction, List<string> allowedHttpMethods) { this._replyAction = replyAction; this._allowedHttpMethods = allowedHttpMethods; }

        public object[] AllocateInputs() { return new object[1]; }
        public object Invoke(object instance, object[] inputs, out object[] outputs) { Message input = (Message)inputs[0]; outputs = null; return HandlePreflight(input); }
        public IAsyncResult InvokeBegin(object instance, object[] inputs, AsyncCallback callback, object state) { throw new NotSupportedException("Only synchronous invocation"); }
        public object InvokeEnd(object instance, out object[] outputs, IAsyncResult result) { throw new NotSupportedException("Only synchronous invocation"); }
        public bool IsSynchronous { get { return true; } }

        public Message HandlePreflight(Message input)
        {
            HttpRequestMessageProperty httpRequest = (HttpRequestMessageProperty)input.Properties[HttpRequestMessageProperty.Name];
            string origin = httpRequest.Headers[CORSEnabledOperationAttribute.Origin];
            string requestMethod = httpRequest.Headers[CORSEnabledOperationAttribute.AccessControlRequestMethod];
            string requestHeaders = httpRequest.Headers[CORSEnabledOperationAttribute.AccessControlRequestHeaders];
            // 
            Message reply = Message.CreateMessage(MessageVersion.None, _replyAction);
            HttpResponseMessageProperty httpResponse = new HttpResponseMessageProperty();
            reply.Properties.Add(HttpResponseMessageProperty.Name, httpResponse);
            // 
            httpResponse.SuppressEntityBody = true;
            httpResponse.StatusCode = HttpStatusCode.OK;
            if (origin != null) { httpResponse.Headers.Add(CORSEnabledOperationAttribute.AccessControlAllowOrigin, origin); }
            if (requestMethod != null && this._allowedHttpMethods.Contains(requestMethod) == true) { httpResponse.Headers.Add(CORSEnabledOperationAttribute.AccessControlAllowMethods, string.Join(",", this._allowedHttpMethods)); }
            if (requestHeaders != null) { httpResponse.Headers.Add(CORSEnabledOperationAttribute.AccessControlAllowHeaders, requestHeaders); }
            // 
            return reply;
        }
    }

    #endregion CORS Preflight Operation Behaviour
}
