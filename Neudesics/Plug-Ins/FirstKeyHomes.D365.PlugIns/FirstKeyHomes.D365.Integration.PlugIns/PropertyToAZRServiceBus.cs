using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;

namespace FirstKeyHomes.D365.Integration.PlugIns
{
    public class PropertyToAZRServiceBus : IPlugin
    {
        #region Secure/Unsecure Configuration Setup
        private string _secureConfig = null;
        private string _unsecureConfig = null;
        const string PROPERTY_QUEUE_CONNECTION_STRING = "Endpoint=sb://fsk-d365-integration.servicebus.windows.net/;SharedAccessKeyName=ListenAndSend;SharedAccessKey=AcwFsm2HmQv9pBBXEPVKl8xBETXnWuaUYBDS8GMMA3k=";
        const string PROPERTY_QUEUE_NAME = "propertyoutboundqueue";

        public PropertyToAZRServiceBus(string unsecureConfig, string secureConfig)
        {
            _secureConfig = secureConfig;
            _unsecureConfig = unsecureConfig;
        }

        #endregion
        public void Execute(IServiceProvider serviceProvider)
        {
            ITracingService tracer = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = factory.CreateOrganizationService(context.UserId);
            try
            {
                Entity entity = null;

                IServiceEndpointNotificationService cloudService = (IServiceEndpointNotificationService)serviceProvider.GetService(typeof(IServiceEndpointNotificationService));
                if (cloudService == null)
                    throw new InvalidPluginExecutionException("Failed to retrieve the service bus service.");

                try
                {
                    tracer.Trace("Posting the execution context.");
                    string response = cloudService.Execute(new EntityReference("serviceendpoint", new Guid("1fe8dd32-bfd0-e811-a96e-000d3a16acee")), context);
                    if (!String.IsNullOrEmpty(response))
                    {
                        tracer.Trace("Response = {0}", response);
                    }
                    tracer.Trace("Done.");
                }
                catch (Exception e)
                {
                    tracer.Trace("Exception: {0}", e.ToString());
                    throw;
                }


                //TODO: Do stuff
                //switch (context.MessageName)
                //{
                //    case Constants.MessageNames.Update:
                //        if (context.InputParameters.Contains(Constants.TARGET) && context.InputParameters[Constants.TARGET] is Entity && context.PostEntityImages.Contains(Constants.POST_IMAGE))
                //        {
                //            entity = context.PostEntityImages[Constants.POST_IMAGE] as Entity;
                //        }
                //        else
                //            return;
                //        break;
                //}

                //if (entity is Entity)
                //{
                //    tracer.Trace("Sending Property Message to Queue.");
                //    tracer.Trace($"Queue Connection String : {_unsecureConfig}");

                //    //ExecuteMessage(service, entity);

                //}
            }
            catch (Exception e)
            {
                throw new InvalidPluginExecutionException(e.Message);
            }
        }

        public void ExecuteMessage(IOrganizationService service, Entity entity)
        {
            try
            {
                var client = QueueClient.CreateFromConnectionString(_unsecureConfig, PROPERTY_QUEUE_NAME);
                var message = new BrokeredMessage(CommonMethods.Serialize(entity));
                client.Send(message);


                //Task.Run(async () =>
                //{
                //    var client = QueueClient.CreateFromConnectionString(_unsecureConfig, PROPERTY_QUEUE_NAME);
                //    var message = new BrokeredMessage(CommonMethods.Serialize(entity));
                //    await client.SendAsync(message);
                //});
            }
            catch (Exception)
            {

                throw;
            }


        }
    }
}