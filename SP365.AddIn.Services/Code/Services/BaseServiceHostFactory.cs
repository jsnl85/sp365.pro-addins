using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Description;

namespace SP365.AddIn.Services
{
    // implementation based on MSDN blog post: https://code.msdn.microsoft.com/windowsdesktop/Implementing-CORS-support-c1f9cd4b
    // Add <%@ ServiceHost ... Factory="SP365.AddIn.Services.BaseServiceHostFactory, SP365.AddIn.Services, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null" %>

    public class BaseServiceHostFactory : ServiceHostFactory
    {
        protected override ServiceHost CreateServiceHost(Type serviceType, Uri[] baseAddresses)
        {
            return new BaseServiceHost(serviceType, baseAddresses);
        }
    }

    public class BaseServiceHost : ServiceHost
    {
        Type _contractType = null;
        public BaseServiceHost(Type serviceType, Uri[] baseAddresses) : base(serviceType, baseAddresses) { this._contractType = this.getContractType(serviceType); }

        protected override void OnOpening()
        {
            base.OnOpening();
            // 
            //bool neeedToAddEndpointQ = false;
            //if (neeedToAddEndpointQ == true)
            //{
            //    ServiceEndpoint endpoint = this.AddServiceEndpoint(this._contractType, new WebHttpBinding(), "");
            //    addPreflightOperations(endpoint);
            //}
            // 
            //endpoint.Behaviors.Add(new WebHttpBehavior());
            //endpoint.Behaviors.Add(new CORSEnabledEndpointBehaviour());
        }

        private void addPreflightOperations(ServiceEndpoint endpoint)
        {
            List<OperationDescription> corsEnabledOperations = endpoint.Contract.Operations.Where(_ => _.Behaviors.Find<CORSEnabledOperationAttribute>() != null).ToList();
            if (corsEnabledOperations != null && corsEnabledOperations.Any() == true)
            {
                PreflightOperationBehavior.AddPreflightOperations(endpoint, corsEnabledOperations);
            }
        }
        private Type getContractType(Type serviceType)
        {
            if (hasServiceContract(serviceType))
            {
                return serviceType;
            }

            Type[] possibleContractTypes = serviceType.GetInterfaces()
                .Where(i => hasServiceContract(i))
                .ToArray();

            switch (possibleContractTypes.Length)
            {
                case 0:
                    throw new InvalidOperationException("Service type " + serviceType.FullName + " does not implement any interface decorated with the ServiceContractAttribute.");
                case 1:
                    return possibleContractTypes[0];
                default:
                    throw new InvalidOperationException("Service type " + serviceType.FullName + " implements multiple interfaces decorated with the ServiceContractAttribute, not supported by this factory.");
            }
        }
        private static bool hasServiceContract(Type type)
        {
            return Attribute.IsDefined(type, typeof(ServiceContractAttribute), false);
        }
    }
}
