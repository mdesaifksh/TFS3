using System;
using System.Diagnostics;
using System.Threading;
using System.Runtime.Serialization;

using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;

using Microsoft.Xrm.Sdk;

namespace FirstKey.D365.Plug_Ins
{
    public class D365ToAzureIntegrationCallPlugin : IPlugin
    {
        private Guid serviceEndpointId;

        public D365ToAzureIntegrationCallPlugin(string unsecureConfig, string secureConfig)
        {
            if (String.IsNullOrEmpty(unsecureConfig) || !Guid.TryParse(unsecureConfig, out serviceEndpointId))
            {
                throw new InvalidPluginExecutionException("Service endpoint ID should be passed as config.");
            }
        }

        public void Execute(IServiceProvider serviceProvider)
        {
            // Retrieve the execution context.
            ITracingService tracer = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = factory.CreateOrganizationService(context.UserId);


            try
            {
                Entity azureIntegrationCallEntity = null;
                if (!context.InputParameters.Contains(Constants.TARGET)) { return; }
                if (((Entity)context.InputParameters[Constants.TARGET]).LogicalName != Constants.AzureIntegrationCalls.LogicalName)
                    return;

                switch (context.MessageName)
                {
                    case Constants.Messages.Create:
                        if (context.InputParameters.Contains(Constants.TARGET) && context.InputParameters[Constants.TARGET] is Entity)
                            azureIntegrationCallEntity = context.InputParameters[Constants.TARGET] as Entity;
                        else
                            return;
                        break;
                    case Constants.Messages.Update:
                        if (context.InputParameters.Contains(Constants.TARGET) && context.InputParameters[Constants.TARGET] is Entity && context.PostEntityImages.Contains(Constants.POST_IMAGE))
                            azureIntegrationCallEntity = context.PostEntityImages[Constants.POST_IMAGE] as Entity;
                        else
                            return;
                        break;
                }

                if (azureIntegrationCallEntity == null || context.Depth > 2)
                {
                    tracer.Trace($"Azure Integration Call entity is Null OR Context Depth is higher than 2. Actual Depth is : {context.Depth}");
                    return;
                }
                if (!azureIntegrationCallEntity.Attributes.Contains(Constants.AzureIntegrationCalls.Direction) || !azureIntegrationCallEntity.Attributes.Contains(Constants.AzureIntegrationCalls.EventData)
                    || !azureIntegrationCallEntity.Attributes.Contains(Constants.AzureIntegrationCalls.StatusCode))
                {
                    tracer.Trace($"Azure Integration Call missing either Direction or EventData field. Failing Azure Integration Call");
                    if (azureIntegrationCallEntity is Entity)
                        UpdateAzureIntegrationCallErrorDetails(service, azureIntegrationCallEntity.ToEntityReference(), "Azure Integration Call missing either Direction or EventData field. Failing Azure Integration Call");
                    return;
                }

                if (azureIntegrationCallEntity.GetAttributeValue<OptionSetValue>(Constants.AzureIntegrationCalls.StatusCode).Value != 1)
                    return;

                if (azureIntegrationCallEntity is Entity)
                {
                    if (azureIntegrationCallEntity.GetAttributeValue<bool>(Constants.AzureIntegrationCalls.Direction))                 //OutGoing
                    {
                        IServiceEndpointNotificationService cloudService = (IServiceEndpointNotificationService)serviceProvider.GetService(typeof(IServiceEndpointNotificationService));
                        if (cloudService == null)
                            throw new InvalidPluginExecutionException("Failed to retrieve the service bus service.");

                        tracer.Trace("Posting the execution context.");
                        string response = cloudService.Execute(new EntityReference("serviceendpoint", serviceEndpointId), context);
                        if (!String.IsNullOrEmpty(response))
                        {
                            tracer.Trace("Response = {0}", response);
                        }
                        tracer.Trace("Done.");
                        CommonMethods.ChangeEntityStatus(tracer, service, azureIntegrationCallEntity.ToEntityReference(), 0, 963850000);
                    }
                    else                                                                                                    //InComing
                    {
                        tracer.Trace($"Azure Integration Call Direction is InComing.");
                    }

                }

            }
            catch (Exception e)
            {
                tracer.Trace("Exception: {0}", e.ToString());
                throw;
            }
        }

        private static void UpdateAzureIntegrationCallErrorDetails(IOrganizationService service, EntityReference azureIntCallEntityReference, string errorMessage, int StatusCode = 963850002)
        {
            Entity azureIntCallEntity = new Entity(azureIntCallEntityReference.LogicalName);
            azureIntCallEntity.Id = azureIntCallEntityReference.Id;
            azureIntCallEntity[Constants.AzureIntegrationCalls.ErrorDetails] = errorMessage;
            azureIntCallEntity[Constants.AzureIntegrationCalls.StatusCode] = new OptionSetValue(StatusCode);

            service.Update(azureIntCallEntity);
        }
    }
}
