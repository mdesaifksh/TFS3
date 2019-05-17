using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;

namespace FirstKey.D365.Plug_Ins
{
    /// <summary>
    /// Trigger to update Total Amount Rollup field as soon as CO created. 
    /// Need to set to 0. 
    /// </summary>
    public class ChangeOrderCreate : IPlugin
    {
        #region Secure/Unsecure Configuration Setup
        private string _secureConfig = null;
        private string _unsecureConfig = null;

        public ChangeOrderCreate(string unsecureConfig, string secureConfig)
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
            IOrganizationService service = factory.CreateOrganizationService(null);

            Entity changeOrderEntity = null;
            if (!context.InputParameters.Contains(Constants.TARGET)) { return; }
            if (((Entity)context.InputParameters[Constants.TARGET]).LogicalName != Constants.ChangeOrders.LogicalName)
                return;

            try
            {
                switch (context.MessageName)
                {
                    case Constants.Messages.Create:
                        if (context.InputParameters.Contains(Constants.TARGET) && context.InputParameters[Constants.TARGET] is Entity)
                            changeOrderEntity = context.InputParameters[Constants.TARGET] as Entity;
                        else
                            return;
                        break;

                }

                if (changeOrderEntity is Entity)
                {
                    Entity tmp = new Entity(changeOrderEntity.LogicalName);
                    tmp.Id = changeOrderEntity.Id;
                    tmp[Constants.ChangeOrders.Revision] = 0;

                    if (!changeOrderEntity.Attributes.Contains(Constants.ChangeOrders.Currency))
                    {
                        tracer.Trace("Default Transaction Currency NOT found.");

                        Entity orgEntity = service.Retrieve("organization", context.OrganizationId, new ColumnSet(true));
                        if(orgEntity is Entity && orgEntity.Attributes.Contains("basecurrencyid"))
                        {
                            tracer.Trace("Default Transaction Currency found in Org Record.");
                            tmp[Constants.ChangeOrders.Currency] = orgEntity.GetAttributeValue<EntityReference>("basecurrencyid");
                        }
                    }

                    service.Update(tmp);
                    //Force Calculate Rollup Field
                    CommonMethods.CalculateRollup(service, Constants.ChangeOrders.TotalAmount, changeOrderEntity.ToEntityReference());
                }
            }
            catch (Exception e)
            {
                throw new InvalidPluginExecutionException(e.Message);
            }
        }
    }
}