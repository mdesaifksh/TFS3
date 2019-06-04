using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace FirstKey.D365.Plug_Ins
{
    public class ApproveChangeOrder : IPlugin
    {
        #region Secure/Unsecure Configuration Setup
        private string _secureConfig = null;
        private string _unsecureConfig = null;
        private const string ACTIVITYPARTY_ENTITY_NAME = "activityparty";
        private const string ACTIVITYPARTY_ATTR_PARTYID = "partyid";
        private static string ServerUrl = string.Empty;
        private const string TURNPROCESS_PROJECT_TEMPLATE = "TURNPROCESS_PROJECT_TEMPLATE";
        private const string INITIALRENOVATION_PROJECT_TEMPLATE = "INITIALRENOVATION_PROJECT_TEMPLATE";

        public ApproveChangeOrder(string unsecureConfig, string secureConfig)
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
            ProjectTemplateSettings projectTemplateSettings = null;

            if (changeOrderEntityReference == null) return;

            if (!string.IsNullOrEmpty(_unsecureConfig))
            {
                StringReader stringReader = new StringReader(_unsecureConfig);
                XmlSerializer serializer = new XmlSerializer(typeof(ProjectTemplateSettings));

                projectTemplateSettings = (ProjectTemplateSettings)serializer.Deserialize(stringReader);
            }

            if (projectTemplateSettings == null)
            {
                tracer.Trace("projectTemplateSettings is NULL. UnSecure Plugin Configuration Not Found.");
                return;
            }


            try
            {
                int revision = context.InputParameters.Contains(Constants.CustomActionParam.Revision) ? int.Parse((context.InputParameters[Constants.CustomActionParam.Revision]).ToString()) : 0;
                ServerUrl = context.InputParameters.Contains(Constants.CustomActionParam.ServerUrl) ? context.InputParameters[Constants.CustomActionParam.ServerUrl].ToString() : string.Empty;
                tracer.Trace($"Server Url : {ServerUrl}");
                string errorMessage = ExecuteContext(tracer, service, changeOrderEntityReference, revision, projectTemplateSettings, context.UserId);
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

        public static string ExecuteContext(ITracingService tracer, IOrganizationService service, EntityReference changeOrderEntityReference, int revision, ProjectTemplateSettings projectTemplateSettings, Guid currentSystemUserID)
        {
            string errorMessage = string.Empty;
            //ReCalculate Lookup...
            CommonMethods.CalculateRollup(service, Constants.ChangeOrders.TotalAmount, changeOrderEntityReference);

            Entity changeOrderEntity = service.Retrieve(changeOrderEntityReference.LogicalName, changeOrderEntityReference.Id, new ColumnSet(true));
            if (changeOrderEntity is Entity && changeOrderEntity.Attributes.Contains(Constants.ChangeOrders.PendingApprovalLevel) && changeOrderEntity.Attributes.Contains(Constants.ChangeOrders.Unit) && changeOrderEntity.Attributes.Contains(Constants.ChangeOrders.ProjectTemplateID))
            {
                if (changeOrderEntity.Attributes.Contains(Constants.Status.StatusCode) && changeOrderEntity.GetAttributeValue<OptionSetValue>(Constants.Status.StatusCode).Value == 963850005)
                {

                    Entity unitEntity = service.Retrieve(Constants.Units.LogicalName, changeOrderEntity.GetAttributeValue<EntityReference>(Constants.ChangeOrders.Unit).Id, new ColumnSet(true));
                    if (unitEntity is Entity && unitEntity.Attributes.Contains(Constants.Units.Market))
                    {
                        tracer.Trace("Is Approver is Part of Approve Process");

                        EntityCollection approverEntityCollection = CommonMethods.ApproverOrderList(tracer, service, changeOrderEntity.GetAttributeValue<OptionSetValue>(Constants.ChangeOrders.PendingApprovalLevel).Value,
                            unitEntity.GetAttributeValue<OptionSetValue>(Constants.Units.Market).Value, changeOrderEntity.GetAttributeValue<EntityReference>(Constants.ChangeOrders.ProjectTemplateID), currentSystemUserID);

                        if (approverEntityCollection.Entities.Count > 0)
                        {
                            int nextApprovalLevel = 0;
                            bool nextApprovelRequired = false;
                            //All levels are approved. Mark Change Order as Approved.
                            CreateChangeOrderApproveRecord(tracer, service, changeOrderEntity, approverEntityCollection.Entities[0].ToEntityReference());
                            if (changeOrderEntity.GetAttributeValue<OptionSetValue>(Constants.ChangeOrders.PendingApprovalLevel).Value != 963850005)
                            {
                                //Perform Approve Process.
                                nextApprovelRequired = IsNextApprovalRequired(tracer, service, changeOrderEntity.GetAttributeValue<Money>(Constants.ChangeOrders.TotalAmount).Value,
                                    changeOrderEntity.GetAttributeValue<OptionSetValue>(Constants.ChangeOrders.PendingApprovalLevel).Value, changeOrderEntity.GetAttributeValue<EntityReference>(Constants.ChangeOrders.ProjectTemplateID), projectTemplateSettings, out nextApprovalLevel);

                            }
                            if (!nextApprovelRequired)
                            {

                                Entity tmpChangeOrderEntity = new Entity(changeOrderEntity.LogicalName);
                                tmpChangeOrderEntity.Id = changeOrderEntity.Id;
                                tmpChangeOrderEntity[Constants.ChangeOrders.PendingApprovalLevel] = null;

                                service.Update(tmpChangeOrderEntity);
                                //Set Status to Approved...

                                CommonMethods.ChangeEntityStatus(tracer, service, changeOrderEntityReference, 1, 963850006);

                                EntityCollection changeOrderItemsEntityCollection = CommonMethods.RetrieveChangeOrderItems(tracer, service, changeOrderEntityReference);

                                foreach (Entity changeOrderItemEntity in changeOrderItemsEntityCollection.Entities)
                                {
                                    //Change Status to Approved. Should be part of Inactive.
                                    CommonMethods.ChangeEntityStatus(tracer, service, changeOrderItemEntity.ToEntityReference(), 1, 963850002);
                                }

                                if (changeOrderEntity.Attributes.Contains(Constants.ChangeOrders.ProjectID))
                                {
                                    Entity projectEntity = service.Retrieve(changeOrderEntity.GetAttributeValue<EntityReference>(Constants.ChangeOrders.ProjectID).LogicalName, changeOrderEntity.GetAttributeValue<EntityReference>(Constants.ChangeOrders.ProjectID).Id, new ColumnSet(true));
                                    if (projectEntity is Entity)
                                    {
                                        if (unitEntity is Entity && (unitEntity.Attributes.Contains(Constants.Units.UnitId) || unitEntity.Attributes.Contains(Constants.Units.SFCode)))
                                        {
                                            CreateOutGoingAzureIntegrationCallRecord(service, tracer, projectEntity, changeOrderEntity, changeOrderItemsEntityCollection, projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectTemplate),
                                                projectTemplateSettings, (unitEntity.Attributes.Contains(Constants.Units.UnitId)) ? unitEntity.GetAttributeValue<string>(Constants.Units.UnitId) : unitEntity.GetAttributeValue<string>(Constants.Units.SFCode));
                                        }
                                    }
                                }



                                //Send Confirmation Email...
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
                                    emailActivity[Constants.Emails.Subject] = $"Change Order Approved: {changeOrderEntity.GetAttributeValue<EntityReference>(Constants.ChangeOrders.ProjectID).Name}";
                                    emailActivity[Constants.Emails.To] = toEntitycollection;
                                    emailActivity[Constants.Emails.From] = fromEntitycollection;
                                    emailActivity[Constants.Emails.DirectionCode] = true;
                                    emailActivity[Constants.Emails.RegardingObject] = changeOrderEntity.ToEntityReference();
                                    emailActivity[Constants.Emails.Description] = $"The change order for the project {changeOrderEntity.GetAttributeValue<EntityReference>(Constants.ChangeOrders.ProjectID).Name} has been approved.<br/><br/> Click here to access the change order. <br/><a href ='{recordUrl}'>{changeOrderEntity.GetAttributeValue<string>(Constants.ChangeOrders.Name)}</a>   ";

                                    emailActivity.Id = service.Create(emailActivity);
                                    try
                                    {
                                        //SendEmailResponse emailResponse = CommonMethods.SendEmail(tracer, service, emailActivity.ToEntityReference());
                                        //if (emailResponse is SendEmailResponse)
                                        //    tracer.Trace("Email successfully sent.");
                                        //else
                                        //    errorMessage = "Operation successfully completed but Email Send failed.";
                                    }
                                    catch(Exception ex)
                                    {
                                        tracer.Trace(ex.Message + Environment.NewLine + ex.StackTrace);
                                    }
                                        
                                }
                                else
                                    errorMessage = $"System User Not found with Email Address : {Constants.CRMEmail}. Operation Successfully performed but Email will not be generated.";


                            }
                            else
                            {
                                //Go To Next Approver...
                                tracer.Trace($"Retrieving Budget Approver.");
                                EntityCollection budgetApproverEntityCollection = CommonMethods.RetrieveAllBudjgetApprovers(tracer, service, unitEntity.GetAttributeValue<OptionSetValue>(Constants.Units.Market).Value, nextApprovalLevel, changeOrderEntity.GetAttributeValue<EntityReference>(Constants.ChangeOrders.ProjectTemplateID));

                                if (budgetApproverEntityCollection.Entities.Count > 0)
                                {
                                    tracer.Trace($"Budget Approver found.");

                                    Entity fromSystemUserEntity = CommonMethods.RetrieveCRMEMailSystemUser(tracer, service);
                                    if (fromSystemUserEntity is Entity)
                                    {
                                        EntityCollection fromEntitycollection = new EntityCollection();
                                        Entity fromParty = new Entity(ACTIVITYPARTY_ENTITY_NAME);
                                        fromParty.Attributes.Add(ACTIVITYPARTY_ATTR_PARTYID, fromSystemUserEntity.ToEntityReference());
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
                                        errorMessage = $"System User Not found with Email Address : {Constants.CRMEmail}. Operation Successfully performed but Email will not be generated.";

                                    Entity tmpChangeOrderEntity = new Entity(changeOrderEntity.LogicalName);
                                    tmpChangeOrderEntity.Id = changeOrderEntity.Id;
                                    tmpChangeOrderEntity[Constants.ChangeOrders.PendingApprovalLevel] = new OptionSetValue(nextApprovalLevel);

                                    service.Update(tmpChangeOrderEntity);
                                }
                                else
                                {
                                    tracer.Trace($"No Budget Approver found.");
                                }

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
            {
                errorMessage = "Change Order Entity Not Found or Change Order entity does not have Approval Level or Unit";
            }

            return errorMessage;
        }

        private static void CreateChangeOrderApproveRecord(ITracingService tracer, IOrganizationService service, Entity changeOrderEntity, EntityReference BudgetApproverEntityReference)
        {
            Entity changeOrderApproverEntity = new Entity(Constants.ChangeOrderApprovers.LogicalName);
            changeOrderApproverEntity[Constants.ChangeOrderApprovers.ChangeOrder] = changeOrderEntity.ToEntityReference();
            changeOrderApproverEntity[Constants.ChangeOrderApprovers.Revision] = changeOrderEntity.GetAttributeValue<int>(Constants.ChangeOrders.Revision);
            changeOrderApproverEntity[Constants.ChangeOrderApprovers.BudgetApproverID] = BudgetApproverEntityReference;
            changeOrderApproverEntity[Constants.Status.StatusCode] = new OptionSetValue(963850000);     //Approved

            service.Create(changeOrderApproverEntity);

        }



        private static bool IsNextApprovalRequired(ITracingService tracer, IOrganizationService service, decimal amount, int CurrentApprovalLevel, EntityReference projectTemplateEntityReference, ProjectTemplateSettings projectTemplateSettings, out int nextApprovalLevel)
        {
            bool isnextApprovalRequire = false;
            nextApprovalLevel = 0;
            Mapping mapping = (
                from m in projectTemplateSettings.Mappings
                where m.Key.Equals(projectTemplateEntityReference.Id.ToString(), StringComparison.OrdinalIgnoreCase)
                select m).FirstOrDefault<Mapping>();

            if (mapping is Mapping)
            {
                if (mapping.Name.Equals(TURNPROCESS_PROJECT_TEMPLATE))
                {
                    if (amount > 1000 && CurrentApprovalLevel == 963850000)          //0 - 1000
                    {
                        isnextApprovalRequire = true;
                        nextApprovalLevel = 963850001;
                    }
                    else if (amount > 2000 && CurrentApprovalLevel == 963850001)    //1000 - 2000
                    {
                        isnextApprovalRequire = true;
                        nextApprovalLevel = 963850002;
                    }
                    else if (amount > 10000 && CurrentApprovalLevel == 963850002)    //2000 - 10000
                    {
                        isnextApprovalRequire = true;
                        nextApprovalLevel = 963850005;
                    }
                }
                else
                {
                    if (amount > 2000 && CurrentApprovalLevel == 963850003)          //0 - 2000
                    {
                        isnextApprovalRequire = true;
                        nextApprovalLevel = 963850002;
                    }
                    else if (amount > 10000 && CurrentApprovalLevel == 963850002)    //2000 - 10000
                    {
                        isnextApprovalRequire = true;
                        nextApprovalLevel = 963850005;
                    }
                }
            }
            return isnextApprovalRequire;
        }


        private static void CreateOutGoingAzureIntegrationCallRecord(IOrganizationService _service, ITracingService tracer, Entity projectEntity, Entity changeOrderEntity, EntityCollection changeOrderItemsEntityCollection, EntityReference projectTemplateEntityReference, ProjectTemplateSettings projectTemplateSettings, string propertyID)
        {
            Mapping mapping = (
                    from m in projectTemplateSettings.Mappings
                    where m.Key.Equals(projectTemplateEntityReference.Id.ToString(), StringComparison.OrdinalIgnoreCase)
                    select m).FirstOrDefault<Mapping>();

            if (mapping is Mapping)
            {
                tracer.Trace($"Project Template is : {mapping.Name}");

                EntityCollection projectTeamMemberEntityCollection = RetrieveProjectTeamMembers(tracer, _service, projectEntity.ToEntityReference());
                EntityCollection projectTaskEntityCollection = RetrieveAllProjectTaskByProjectWithContractID(tracer, _service, projectEntity.ToEntityReference());
                Dictionary<Guid, string> vendorContractCode = new Dictionary<Guid, string>();
                Dictionary<Guid, string> vendorCodeDictionary = new Dictionary<Guid, string>();
                List<Contract> contractList = new List<Contract>();
                foreach (Entity changeOrderItemEntity in changeOrderItemsEntityCollection.Entities)
                {
                    if (changeOrderItemEntity.Attributes.Contains(Constants.ChangeOrderItems.Vendor))
                    {
                        string contractCode = string.Empty;
                        //Check whether It is existing Vendor or not.
                        if (vendorContractCode.ContainsKey(changeOrderItemEntity.GetAttributeValue<EntityReference>(Constants.ChangeOrderItems.Vendor).Id))
                        {
                            contractCode = vendorContractCode[changeOrderItemEntity.GetAttributeValue<EntityReference>(Constants.ChangeOrderItems.Vendor).Id];
                        }
                        else
                        {
                            if (projectTeamMemberEntityCollection is EntityCollection)
                            {
                                Entity prjTeamMemberEntity = projectTeamMemberEntityCollection.Entities.Where(e => e.Attributes.Contains("B.accountid") && ((EntityReference)e.GetAttributeValue<AliasedValue>("B.accountid").Value).Id.Equals(changeOrderItemEntity.GetAttributeValue<EntityReference>(Constants.ChangeOrderItems.Vendor).Id)).FirstOrDefault();
                                if (prjTeamMemberEntity is Entity)
                                {
                                    Entity prjTaskEntity = projectTaskEntityCollection.Entities.Where(e => e.Attributes.Contains(Constants.ProjectTasks.AssignedTeamMembers) && e.GetAttributeValue<EntityReference>(Constants.ProjectTasks.AssignedTeamMembers).Id.Equals(prjTeamMemberEntity.Id)).FirstOrDefault();
                                    if (prjTaskEntity is Entity)
                                    {
                                        vendorContractCode.Add(changeOrderItemEntity.GetAttributeValue<EntityReference>(Constants.ChangeOrderItems.Vendor).Id, prjTaskEntity.GetAttributeValue<string>(Constants.ProjectTasks.ContractID));
                                        contractCode = prjTaskEntity.GetAttributeValue<string>(Constants.ProjectTasks.ContractID);
                                    }
                                    else
                                        vendorContractCode.Add(changeOrderItemEntity.GetAttributeValue<EntityReference>(Constants.ChangeOrderItems.Vendor).Id, string.Empty);
                                }
                                else
                                    vendorContractCode.Add(changeOrderItemEntity.GetAttributeValue<EntityReference>(Constants.ChangeOrderItems.Vendor).Id, string.Empty);
                            }
                            else
                                vendorContractCode.Add(changeOrderItemEntity.GetAttributeValue<EntityReference>(Constants.ChangeOrderItems.Vendor).Id, string.Empty);
                        }
                        Contract c = new Contract();
                        c.Amount = changeOrderItemEntity.GetAttributeValue<Money>(Constants.ChangeOrderItems.Amount).Value.ToString();
                        c.Category_Code = changeOrderItemEntity.GetAttributeValue<AliasedValue>("JC.fkh_jobcategorycode").Value.ToString();
                        c.Contract_Code = contractCode;
                        c.ItemDescription = changeOrderItemEntity.GetAttributeValue<string>(Constants.ChangeOrderItems.Description);
                        c.Start_Date = DateTime.Now.ToString();
                        if (vendorCodeDictionary.ContainsKey(changeOrderItemEntity.GetAttributeValue<EntityReference>(Constants.ChangeOrderItems.Vendor).Id))
                            c.Vendor_Code = vendorCodeDictionary[changeOrderItemEntity.GetAttributeValue<EntityReference>(Constants.ChangeOrderItems.Vendor).Id];
                        else
                        {
                            Entity vendorEntity = _service.Retrieve(changeOrderItemEntity.GetAttributeValue<EntityReference>(Constants.ChangeOrderItems.Vendor).LogicalName, changeOrderItemEntity.GetAttributeValue<EntityReference>(Constants.ChangeOrderItems.Vendor).Id, new ColumnSet(true));
                            if (vendorEntity is Entity && vendorEntity.Attributes.Contains(Constants.Vendors.AccountCode))
                            {
                                c.Vendor_Code = vendorEntity.GetAttributeValue<string>(Constants.Vendors.AccountCode);
                                vendorCodeDictionary.Add(vendorEntity.Id, vendorEntity.GetAttributeValue<string>(Constants.Vendors.AccountCode));
                            }
                        }
                        //c.Vendor_Code = 
                        contractList.Add(c);
                    }
                }


                List<GridEvent<DataPayLoad>> gridEventDataPayloadList = new List<GridEvent<DataPayLoad>>();
                GridEvent<DataPayLoad> gridEventDataPayload = new GridEvent<DataPayLoad>();
                gridEventDataPayload.EventTime = DateTime.Now.ToString();
                gridEventDataPayload.EventType = "allEvents";
                gridEventDataPayload.Id = Guid.NewGuid().ToString();
                if (mapping.Name.Equals(TURNPROCESS_PROJECT_TEMPLATE))
                    gridEventDataPayload.Subject = $"Turn Process : {Events.CHANGE_ORDER.ToString()}";
                else
                    gridEventDataPayload.Subject = $"Initial Renovation : {Events.IR_CHANGE_ORDER.ToString()}";
                gridEventDataPayload.data = new DataPayLoad();
                gridEventDataPayload.data.Date1 = DateTime.Now.ToString();
                if (mapping.Name.Equals(TURNPROCESS_PROJECT_TEMPLATE))
                    gridEventDataPayload.data.Event = Events.CHANGE_ORDER;
                else
                    gridEventDataPayload.data.Event = Events.IR_CHANGE_ORDER;
                gridEventDataPayload.data.IsForce = false;
                gridEventDataPayload.data.PropertyID = propertyID;
                gridEventDataPayload.data.EmailID = string.Empty;
                gridEventDataPayload.data.FotoNotesID = string.Empty;
                gridEventDataPayload.data.JobID = string.Empty;
                gridEventDataPayload.data.RenowalkID = projectEntity.GetAttributeValue<string>(Constants.Projects.RenowalkID);
                gridEventDataPayload.data.Contracts = contractList;

                gridEventDataPayloadList.Add(gridEventDataPayload);

                Entity azIntCallEntity = new Entity(Constants.AzureIntegrationCalls.LogicalName);
                azIntCallEntity[Constants.AzureIntegrationCalls.EventData] = CommonMethods.Serialize(gridEventDataPayloadList);
                azIntCallEntity[Constants.AzureIntegrationCalls.Direction] = true;
                if (mapping.Name.Equals(TURNPROCESS_PROJECT_TEMPLATE))
                    azIntCallEntity[Constants.AzureIntegrationCalls.EventName] = Events.CHANGE_ORDER.ToString();
                else
                    azIntCallEntity[Constants.AzureIntegrationCalls.EventName] = Events.IR_CHANGE_ORDER.ToString();
                _service.Create(azIntCallEntity);

            }
            else
            {
                tracer.Trace($"Project Template Mapping Not found in PlugIn Setting for Project Template : {projectTemplateEntityReference.Id.ToString()}");
            }
        }

        public static EntityCollection RetrieveAllProjectTaskByProjectWithContractID(ITracingService tracer, IOrganizationService service, EntityReference projectEntityRefernce)
        {
            QueryExpression queryExpression = new QueryExpression(Constants.ProjectTasks.LogicalName)
            {
                ColumnSet = new ColumnSet(true)
            };
            queryExpression.Criteria.AddCondition(new ConditionExpression(Constants.ProjectTasks.Project, ConditionOperator.Equal, projectEntityRefernce.Id));
            queryExpression.Criteria.AddCondition(new ConditionExpression(Constants.ProjectTasks.ContractID, ConditionOperator.NotNull));


            queryExpression.TopCount = 100;
            return service.RetrieveMultiple(queryExpression);
        }

        private static EntityCollection RetrieveProjectTeamMembers(ITracingService tracer, IOrganizationService service, EntityReference projectEntityReference)
        {
            QueryExpression Query = new QueryExpression
            {
                EntityName = Constants.ProjectTeams.LogicalName,
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    FilterOperator = LogicalOperator.And,
                    Conditions =
                    {
                        new ConditionExpression
                        {
                            AttributeName = Constants.ProjectTeams.Project,
                            Operator = ConditionOperator.Equal,
                            Values = { projectEntityReference.Id }
                        }
                    }
                },
                TopCount = 100
            };

            LinkEntity linkEntity = new LinkEntity(Constants.ProjectTeams.LogicalName, Constants.BookableResources.LogicalName, Constants.ProjectTeams.BookableResource, Constants.BookableResources.PrimaryKey, JoinOperator.Inner)
            {
                Columns = new ColumnSet(Constants.BookableResources.AccountID),
                EntityAlias = "B"
            };
            linkEntity.LinkCriteria.AddCondition(new ConditionExpression(Constants.BookableResources.ResourceType, ConditionOperator.Equal, 5));
            linkEntity.LinkCriteria.AddCondition(new ConditionExpression(Constants.BookableResources.AccountID, ConditionOperator.NotNull));

            Query.LinkEntities.Add(linkEntity);


            EntityCollection projectTeamEntityCollection = service.RetrieveMultiple(Query);
            if (projectTeamEntityCollection.Entities.Count > 0)
                return projectTeamEntityCollection;
            else
                return null;
        }


    }
}