using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;

namespace FirstKey.D365.Plug_Ins
{
    /// <summary>
    /// Trigger to update Total Amount Rollup field as soon as CO Item created or updated.
    /// </summary>
    public class ChangeOrderItemCreate : IPlugin
    {
        #region Secure/Unsecure Configuration Setup
        private string _secureConfig = null;
        private string _unsecureConfig = null;

        public ChangeOrderItemCreate(string unsecureConfig, string secureConfig)
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

            Entity changeOrderItemsEntity = null;
            if (!context.InputParameters.Contains(Constants.TARGET)) { return; }
            if (((Entity)context.InputParameters[Constants.TARGET]).LogicalName != Constants.ChangeOrderItems.LogicalName)
                return;

            try
            {
                switch (context.MessageName)
                {
                    case Constants.Messages.Create:
                        if (context.InputParameters.Contains(Constants.TARGET) && context.InputParameters[Constants.TARGET] is Entity)
                            changeOrderItemsEntity = context.InputParameters[Constants.TARGET] as Entity;
                        else
                            return;
                        break;
                    case Constants.Messages.Update:
                        if (context.InputParameters.Contains(Constants.TARGET) && context.InputParameters[Constants.TARGET] is Entity && context.PostEntityImages.Contains(Constants.POST_IMAGE))
                            changeOrderItemsEntity = context.PostEntityImages[Constants.POST_IMAGE] as Entity;
                        else
                            return;
                        break;
                }

                if (changeOrderItemsEntity is Entity && changeOrderItemsEntity.Attributes.Contains(Constants.ChangeOrderItems.ChangeOrder))
                {
                    tracer.Trace($"change order lookup found.");
                    //Force Calculate Rollup Field
                    CommonMethods.CalculateRollup(service, Constants.ChangeOrders.TotalAmount, changeOrderItemsEntity.GetAttributeValue<EntityReference>(Constants.ChangeOrderItems.ChangeOrder));
                }
                else
                    tracer.Trace($"Change Order Lookup Not Found.");

            }
            catch (Exception e)
            {
                throw new InvalidPluginExecutionException(e.Message);
            }
        }
    }
}