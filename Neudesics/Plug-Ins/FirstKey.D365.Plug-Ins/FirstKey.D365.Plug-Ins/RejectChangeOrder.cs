using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;

namespace FirstKey.D365.Plug_Ins
{
    public class RejectChangeOrder : IPlugin
    {
        #region Secure/Unsecure Configuration Setup
        private string _secureConfig = null;
        private string _unsecureConfig = null;
        private const string ACTIVITYPARTY_ENTITY_NAME = "activityparty";
        private const string ACTIVITYPARTY_ATTR_PARTYID = "partyid";
        private string ServerUrl = string.Empty;
        private string Reason = string.Empty;

        public RejectChangeOrder(string unsecureConfig, string secureConfig)
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
                Reason = context.InputParameters.Contains(Constants.CustomActionParam.Reason) ? context.InputParameters[Constants.CustomActionParam.Reason].ToString() : string.Empty;
                tracer.Trace($"Server Url : {ServerUrl}");
                tracer.Trace($"Reject Reason : {Reason}");

                string errorMessage = ExecuteContext(tracer, service, changeOrderEntityReference, revision, context.UserId);
                if (string.IsNullOrEmpty(errorMessage))
                {
                    context.OutputParameters[Constants.CustomActionParam.IsSuccess] = true;
                    context.OutputParameters[Constants.CustomActionParam.ErrorMessage] = string.Empty;
                }
                else
                {
                    tracer.Trace("Error Message " + errorMessage);
                    context.OutputParameters[Constants.CustomActionParam.IsSuccess] = false;
                    context.OutputParameters[Constants.CustomActionParam.ErrorMessage] = errorMessage;
                }
            }
            catch (Exception ex)
            {
                tracer.Trace(ex.Message + ex.StackTrace);
                context.OutputParameters[Constants.CustomActionParam.IsSuccess] = false;
                context.OutputParameters[Constants.CustomActionParam.ErrorMessage] = ex.Message;
            }
        }

        private string ExecuteContext(ITracingService tracer, IOrganizationService service, EntityReference changeOrderEntityReference, int revision, Guid currentSystemUserID)
        {
            string errorMessage = string.Empty;
            Entity changeOrderEntity = service.Retrieve(changeOrderEntityReference.LogicalName, changeOrderEntityReference.Id, new ColumnSet(true));
            if (changeOrderEntity is Entity && changeOrderEntity.Attributes.Contains(Constants.ChangeOrders.PendingApprovalLevel) && changeOrderEntity.Attributes.Contains(Constants.ChangeOrders.Unit))
            {
                if (changeOrderEntity.Attributes.Contains(Constants.Status.StatusCode) && changeOrderEntity.GetAttributeValue<OptionSetValue>(Constants.Status.StatusCode).Value == 963850005)
                {
                    Entity unitEntity = service.Retrieve(Constants.Units.LogicalName, changeOrderEntity.GetAttributeValue<EntityReference>(Constants.ChangeOrders.Unit).Id, new ColumnSet(true));
                    if (unitEntity is Entity && unitEntity.Attributes.Contains(Constants.Units.Market))
                    {
                        tracer.Trace("Is Rejector is Part of Approve Process");

                        EntityCollection approverEntityCollection = CommonMethods.ApproverOrderList(tracer, service, changeOrderEntity.GetAttributeValue<OptionSetValue>(Constants.ChangeOrders.PendingApprovalLevel).Value,
    unitEntity.GetAttributeValue<OptionSetValue>(Constants.Units.Market).Value, changeOrderEntity.GetAttributeValue<EntityReference>(Constants.ChangeOrders.ProjectTemplateID), currentSystemUserID);

                        if (approverEntityCollection.Entities.Count > 0)
                        {


                            Entity tmpChangeOrderEntity = new Entity(changeOrderEntity.LogicalName);
                            tmpChangeOrderEntity.Id = changeOrderEntity.Id;
                            tmpChangeOrderEntity[Constants.ChangeOrders.PendingApprovalLevel] = null;

                            service.Update(tmpChangeOrderEntity);


                            //Set Status to Rejected...
                            CommonMethods.ChangeEntityStatus(tracer, service, changeOrderEntityReference, 0, 963850002);
                            EntityCollection changeOrderItemsEntityCollection = CommonMethods.RetrieveChangeOrderItems(tracer, service, changeOrderEntityReference);
                            foreach (Entity changeOrderItemEntity in changeOrderItemsEntityCollection.Entities)
                            {
                                //Change Status to Rejected. Should be part of Active.
                                CommonMethods.ChangeEntityStatus(tracer, service, changeOrderItemEntity.ToEntityReference(), 0, 963850001);
                            }

                            //Send Rejection Email...
                            Entity fromSystemUserEntity = CommonMethods.RetrieveCRMEMailSystemUser(tracer, service);
                            if (fromSystemUserEntity is Entity)
                            {
                                EntityCollection fromEntitycollection = new EntityCollection();
                                Entity fromParty = new Entity(ACTIVITYPARTY_ENTITY_NAME);
                                fromParty[ACTIVITYPARTY_ATTR_PARTYID] = fromSystemUserEntity.ToEntityReference();
                                fromEntitycollection.Entities.Add(fromParty);

                                EntityCollection toEntitycollection = new EntityCollection();
                                Entity toParty = new Entity(ACTIVITYPARTY_ENTITY_NAME);
                                toParty[ACTIVITYPARTY_ATTR_PARTYID] = changeOrderEntity.GetAttributeValue<EntityReference>(Constants.ChangeOrders.Requestor);
                                toEntitycollection.Entities.Add(toParty);



                                string recordUrl = $"{ServerUrl}/main.aspx?etn={changeOrderEntity.LogicalName}&pagetype=entityrecord&id={changeOrderEntity.Id.ToString()}";
                                Entity emailActivity = new Entity(Constants.Emails.LogicalName);
                                emailActivity[Constants.Emails.Subject] = $"Change Order Rejected: {changeOrderEntity.GetAttributeValue<EntityReference>(Constants.ChangeOrders.ProjectID).Name}";
                                emailActivity[Constants.Emails.To] = toEntitycollection;
                                emailActivity[Constants.Emails.From] = fromEntitycollection;
                                emailActivity[Constants.Emails.DirectionCode] = true;
                                emailActivity[Constants.Emails.RegardingObject] = changeOrderEntity.ToEntityReference();
                                emailActivity[Constants.Emails.Description] = $"{changeOrderEntity.GetAttributeValue<EntityReference>(Constants.ChangeOrders.Requestor).Name}  has rejected your request for a change order for the project {changeOrderEntity.GetAttributeValue<EntityReference>(Constants.ChangeOrders.ProjectID).Name} with the following comment.<br/>{Reason}<br/><br/>Please make necessary changes to the change order and resubmit for approval.<br/><br/>Click here to access the change order. <br/><a href ='{recordUrl}'>{changeOrderEntity.GetAttributeValue<string>(Constants.ChangeOrders.Name)}</a>   ";

                                Guid emailId = service.Create(emailActivity);
                                SendEmailRequest sendEmailreq = new SendEmailRequest
                                {
                                    EmailId = emailId,
                                    TrackingToken = "",
                                    IssueSend = true
                                };
                                SendEmailResponse sendEmailresp = (SendEmailResponse)service.Execute(sendEmailreq);

                                //                            emailActivity.Id = service.Create(emailActivity);

                                //                            emailActivity.Id = service.Create(emailActivity);
                            }
                        }
                        else
                            errorMessage = "You are not an approver in the current market. Consequently, you may not approve this change order.";
                    }
                    else
                        errorMessage = "Unit Not Found or Unit Record does not have Market.";
                }
                else
                    errorMessage = "Change Order is Not Submit For Approval OR Change Order Already Approved.";
            }
            else
                errorMessage = "Change Order Entity Not Found or Change Order entity does not have Approval Level or Unit";
            return errorMessage;
        }
    }
}