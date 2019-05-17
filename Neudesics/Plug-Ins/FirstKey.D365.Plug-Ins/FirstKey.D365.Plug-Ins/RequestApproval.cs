using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;

namespace FirstKey.D365.Plug_Ins
{
    public class RequestApproval : IPlugin
    {
        #region Secure/Unsecure Configuration Setup
        private string _secureConfig = null;
        private string _unsecureConfig = null;
        private const string ACTIVITYPARTY_ENTITY_NAME = "activityparty";
        private const string ACTIVITYPARTY_ATTR_PARTYID = "partyid";
        private string ServerUrl = string.Empty;
        public RequestApproval(string unsecureConfig, string secureConfig)
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
            EntityReference changeOrderEntityReference = context.InputParameters.Contains(Constants.TARGET) ? context.InputParameters[Constants.TARGET] as EntityReference : null;
            if (changeOrderEntityReference == null) return;

            try
            {
                int revision = context.InputParameters.Contains(Constants.CustomActionParam.Revision) ? int.Parse((context.InputParameters[Constants.CustomActionParam.Revision]).ToString()) : 0;
                ServerUrl = context.InputParameters.Contains(Constants.CustomActionParam.ServerUrl) ? context.InputParameters[Constants.CustomActionParam.ServerUrl].ToString() : string.Empty;
                tracer.Trace($"Server Url : {ServerUrl}");
                ExecuteContext(tracer, service, changeOrderEntityReference, revision);
                context.OutputParameters[Constants.CustomActionParam.IsSuccess] = true;
                context.OutputParameters[Constants.CustomActionParam.ErrorMessage] = string.Empty;
            }
            catch (Exception ex)
            {
                tracer.Trace(ex.Message + ex.StackTrace);
                context.OutputParameters[Constants.CustomActionParam.IsSuccess] = false;
                context.OutputParameters[Constants.CustomActionParam.ErrorMessage] = ex.Message;
            }

        }

        private void ExecuteContext(ITracingService tracer, IOrganizationService service, EntityReference changeOrderEntityReference, int revision)
        {
            EntityCollection changeOrderItemsEntityCollection = CommonMethods.RetrieveChangeOrderItems(tracer, service, changeOrderEntityReference);
            foreach (Entity changeOrderItemEntity in changeOrderItemsEntityCollection.Entities)
            {
                //Change Status to Submitted for Approval. Should be part of Inactive.
                CommonMethods.ChangeEntityStatus(tracer, service, changeOrderItemEntity.ToEntityReference(), 1, 2);
            }
            Entity changeOrderEntity = new Entity(changeOrderEntityReference.LogicalName);
            changeOrderEntity.Id = changeOrderEntityReference.Id;
            //Set to 0 - 1500
            changeOrderEntity[Constants.ChangeOrders.PendingApprovalLevel] = new OptionSetValue(963850000);
            changeOrderEntity[Constants.ChangeOrders.Revision] = revision + 1;

            service.Update(changeOrderEntity);

            tracer.Trace($"Retrieving Change Order Record.");
            changeOrderEntity = service.Retrieve(changeOrderEntity.LogicalName, changeOrderEntity.Id, new ColumnSet(true));
            if (changeOrderEntity is Entity && changeOrderEntity.Attributes.Contains(Constants.ChangeOrders.Unit) && changeOrderEntity.Attributes.Contains(Constants.ChangeOrders.PendingApprovalLevel)
                && changeOrderEntity.Attributes.Contains(Constants.ChangeOrders.ProjectID) && changeOrderEntity.Attributes.Contains(Constants.ChangeOrders.Requestor))
            {
                tracer.Trace($"Change Order successfully retrieved with Unit.");
                tracer.Trace($"Retrieving Unit Record.");

                Entity unitEntity = service.Retrieve(changeOrderEntity.GetAttributeValue<EntityReference>(Constants.ChangeOrders.Unit).LogicalName, changeOrderEntity.GetAttributeValue<EntityReference>(Constants.ChangeOrders.Unit).Id, new ColumnSet(true));
                if (unitEntity is Entity && unitEntity.Attributes.Contains(Constants.Units.Market))
                {
                    tracer.Trace($"Retrieving Budget Approver.");
                    EntityCollection budgetApproverEntityCollection = CommonMethods.RetrieveAllBudjgetApprovers(tracer, service, unitEntity.GetAttributeValue<OptionSetValue>(Constants.Units.Market).Value, changeOrderEntity.GetAttributeValue<OptionSetValue>(Constants.ChangeOrders.PendingApprovalLevel).Value);
                    if (budgetApproverEntityCollection.Entities.Count > 0)
                    {
                        tracer.Trace($"Budget Approver found.");

                        EntityCollection fromEntitycollection = new EntityCollection();
                        Entity fromParty = new Entity(ACTIVITYPARTY_ENTITY_NAME);
                        fromParty.Attributes.Add(ACTIVITYPARTY_ATTR_PARTYID, changeOrderEntity.GetAttributeValue<EntityReference>(Constants.ChangeOrders.Requestor));
                        fromEntitycollection.Entities.Add(fromParty);

                        EntityCollection toEntitycollection = new EntityCollection();

                        foreach (Entity budgetApproverEntity in budgetApproverEntityCollection.Entities)
                        {
                            if (budgetApproverEntity.Attributes.Contains("U.internalemailaddress") || budgetApproverEntity.Attributes.Contains("U.domainname"))
                            {
                                Entity toParty = new Entity(ACTIVITYPARTY_ENTITY_NAME);
                                toParty.Attributes.Add(ACTIVITYPARTY_ATTR_PARTYID, budgetApproverEntity.GetAttributeValue<EntityReference>(Constants.BudgetApprovers.ApproverID));
                                toEntitycollection.Entities.Add(toParty);
                            }
                        }

                        CommonMethods.SendRequestForApprovalEmail(tracer, service, changeOrderEntity, fromEntitycollection, toEntitycollection, ServerUrl);

                    }
                    else
                    {
                        tracer.Trace($"No Budget Approver found.");
                    }
                }
                else
                    tracer.Trace($"Unit Not Found or Unit Market Not available. Unit Guid : {changeOrderEntity.GetAttributeValue<EntityReference>(Constants.ChangeOrders.Unit).Id.ToString()}");

            }
        }



    }
}