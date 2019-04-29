using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Serialization;

namespace FirstKey.D365.Plug_Ins
{
    public class AzureIntegrationCallAsync : IPlugin
    {
        #region Secure/Unsecure Configuration Setup
        private string _secureConfig = null;
        private string _unsecureConfig = null;
        private const string TURNPROCESS_PROJECT_TEMPLATE = "TURNPROCESS_PROJECT_TEMPLATE";
        private const string INITIALRENOVATION_PROJECT_TEMPLATE = "INITIALRENOVATION_PROJECT_TEMPLATE";



        public AzureIntegrationCallAsync(string unsecureConfig, string secureConfig)
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
            ProjectTemplateSettings projectTemplateSettings = null;

            if (!string.IsNullOrEmpty(_unsecureConfig))
            {
                StringReader stringReader = new StringReader(_unsecureConfig);
                XmlSerializer serializer = new XmlSerializer(typeof(ProjectTemplateSettings));

                projectTemplateSettings = (ProjectTemplateSettings)serializer.Deserialize(stringReader);
            }



            Entity azureIntegrationCallEntity = null;
            if (!context.InputParameters.Contains(Constants.TARGET)) { return; }
            if (((Entity)context.InputParameters[Constants.TARGET]).LogicalName != Constants.AzureIntegrationCalls.LogicalName)
                return;

            try
            {
                //Entity entity = (Entity)context.InputParameters["Target"];

                //TODO: Do stuff
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
                    if (!azureIntegrationCallEntity.GetAttributeValue<bool>(Constants.AzureIntegrationCalls.Direction))                 //Incoming
                    {
                        if (projectTemplateSettings is ProjectTemplateSettings)
                            ProcessIncomingIntegrationCall(tracer, service, azureIntegrationCallEntity, projectTemplateSettings);
                        else
                            UpdateAzureIntegrationCallErrorDetails(service, azureIntegrationCallEntity.ToEntityReference(), "projectTemplateSettings is NULL. UnSecure Plugin Configuration Not Found.");
                    }
                    else                                                                                                    //Outgoing
                    {
                        //DESIGN FOR OUTGOING CALL
                        tracer.Trace($"Azure Integration Call Direction is OutGoing.");
                    }

                }
            }
            catch (Exception e)
            {
                throw new InvalidPluginExecutionException(e.Message);
            }
        }

        public static void ProcessIncomingIntegrationCall(ITracingService tracer, IOrganizationService service, Entity azureIntegrationCallEntity, ProjectTemplateSettings projectTemplateSettings)
        {
            tracer.Trace($"Azure Integration Call Direction is Incoming. Deserializing EventData");
            try
            {
                string payload = azureIntegrationCallEntity.GetAttributeValue<string>(Constants.AzureIntegrationCalls.EventData).Replace("\"{", "{").Replace("\\", "").Replace('\'', '"');
                List<GridEvent<DataPayLoad>> GridEventList = CommonMethods.Deserialize<List<GridEvent<DataPayLoad>>>(payload);
                if (GridEventList is List<GridEvent<DataPayLoad>> && GridEventList.Count > 0)
                {
                    foreach (GridEvent<DataPayLoad> gridEvent in GridEventList)
                    {
                        if (gridEvent.data is DataPayLoad)
                        {
                            tracer.Trace($"Processing Events");
                            ProcessEvents(tracer, service, azureIntegrationCallEntity.ToEntityReference(), gridEvent, projectTemplateSettings);
                        }
                    }
                }
                else
                {
                    UpdateAzureIntegrationCallErrorDetails(service, azureIntegrationCallEntity.ToEntityReference(), "No Object found for Event Grid.");
                }
            }
            catch (Exception ex)
            {
                tracer.Trace(ex.Message);
                UpdateAzureIntegrationCallErrorDetails(service, azureIntegrationCallEntity.ToEntityReference(), ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }

        private static void ProcessEvents(ITracingService tracer, IOrganizationService service, EntityReference azureIntCallEntityReference, GridEvent<DataPayLoad> gridEvent, ProjectTemplateSettings settings)
        {
            Events @event;
            DateTime date1;
            DateTime date2;
            DateTime date3;
            tracer.Trace("Entered in ProcessEvents methods");
            tracer.Trace($"Retrieving Unit {gridEvent.data.PropertyID} in D365");
            Entity propertyEntity = CommonMethods.RetrieveUnitByUnitId(tracer, service, gridEvent.data.PropertyID);
            int timeZoneCode = CommonMethods.RetrieveCurrentUsersSettings(service);

            if (propertyEntity == null)
                AzureIntegrationCallAsync.UpdateAzureIntegrationCallErrorDetails(service, azureIntCallEntityReference, $"Unit with ID : {gridEvent.data.PropertyID} NOT found in D365 System.", 963850002);
            else
            {
                tracer.Trace($"Unit with ID : {gridEvent.data.PropertyID} found in D365 System.");
                EntityCollection projectEntityCollection = CommonMethods.RetrieveActivtProjectByUnitId(tracer, service, propertyEntity.ToEntityReference());
                if (gridEvent.data.Event == Events.RESIDENT_NOTICE_TO_MOVE_OUT_RECEIVED || gridEvent.data.Event == Events.OFFER_ACCEPTED)        //KICK OF TURN PROJECT OR IR PROJECT
                {
                    @event = gridEvent.data.Event;
                    tracer.Trace($"Processing Event : {@event.ToString()}.");
                    if (projectEntityCollection.Entities.Count != 0)
                        AzureIntegrationCallAsync.UpdateAzureIntegrationCallErrorDetails(service, azureIntCallEntityReference, $"There is an active Project for Property {gridEvent.data.PropertyID} in system. Only one Active Project is allowed in the system.", 963850002);
                    else
                    {
                        Mapping mapping = null;
                        if (@event == Events.RESIDENT_NOTICE_TO_MOVE_OUT_RECEIVED)
                            mapping = (
                                from m in settings.Mappings
                                where m.Name.Equals(TURNPROCESS_PROJECT_TEMPLATE, StringComparison.OrdinalIgnoreCase)
                                select m).FirstOrDefault<Mapping>();
                        else if (@event == Events.OFFER_ACCEPTED)
                            mapping = (
                                from m in settings.Mappings
                                where m.Name.Equals(INITIALRENOVATION_PROJECT_TEMPLATE, StringComparison.OrdinalIgnoreCase)
                                select m).FirstOrDefault<Mapping>();
                        if (mapping == null)
                            AzureIntegrationCallAsync.UpdateAzureIntegrationCallErrorDetails(service, azureIntCallEntityReference, "Mapping for  TURNPROCESS_PROJECT_TEMPLATE or INITIALRENOVATION_PROJECT_TEMPLATE not found in Plugin Configuration.", 963850002);
                        else
                        {
                            Entity projectTemplateEntity = service.Retrieve(Constants.Projects.LogicalName, new Guid(mapping.Key), new ColumnSet(true));
                            if (projectTemplateEntity == null)
                                AzureIntegrationCallAsync.UpdateAzureIntegrationCallErrorDetails(service, azureIntCallEntityReference, $"Project Template {mapping.Name} with Key {mapping.Key} NOT found in the system.", 963850002);
                            else
                            {
                                if (!DateTime.TryParse(gridEvent.data.Date1, out date1))
                                    date1 = DateTime.Now;
                                tracer.Trace($"Date 1 : {date1}");
                                if (DateTime.TryParse(gridEvent.data.Date2, out date2))
                                {
                                    DateTime localDate2 = CommonMethods.RetrieveLocalTimeFromUTCTime(service, timeZoneCode, date2);
                                    tracer.Trace($"local Date 2 : {localDate2}");
                                    //Update Move Out Date on Unit.
                                    Entity tmpPropertyEntity = new Entity(propertyEntity.LogicalName);
                                    tmpPropertyEntity.Id = propertyEntity.Id;
                                    tmpPropertyEntity[Constants.Units.MoveOutDate] = localDate2;
                                    propertyEntity[Constants.Units.MoveOutDate] = localDate2;

                                    service.Update(tmpPropertyEntity);
                                }
                                Entity projectEntity = CommonMethods.CreateProjectFromProjectTemplate(tracer, service, propertyEntity, projectTemplateEntity, date1, timeZoneCode, gridEvent);
                                if (projectEntity is Entity)
                                {
                                    tracer.Trace($"Setting up default BPF.");
                                    Guid processid = Guid.Empty;
                                    if (@event == Events.RESIDENT_NOTICE_TO_MOVE_OUT_RECEIVED)
                                    {
                                        processid = new Guid(Constants.TURN_BPF_ID);
                                        tracer.Trace($"Turn Process BPF ID : {processid.ToString()}");
                                    }
                                    else
                                    {
                                        processid = new Guid(Constants.IR_BPF_ID);
                                        tracer.Trace($"IR BPF ID : {processid.ToString()}");
                                    }

                                    if (!processid.Equals(Guid.Empty))
                                        service.Execute(new SetProcessRequest() { Target = projectEntity.ToEntityReference(), NewProcess = new EntityReference("workflow", processid) });

                                    CommonMethods.ChangeEntityStatus(tracer, service, azureIntCallEntityReference, 0, 963850000);
                                }
                                else
                                    AzureIntegrationCallAsync.UpdateAzureIntegrationCallErrorDetails(service, azureIntCallEntityReference, $"Error Occurred while Creating new Project from Project Template {mapping.Name}.", 963850002);
                            }
                        }
                    }
                }


                else if (projectEntityCollection.Entities.Count != 0)
                {
                    if (!DateTime.TryParse(gridEvent.data.Date1, out date1))
                        date1 = DateTime.Now;
                    if (!DateTime.TryParse(gridEvent.data.Date2, out date2))
                        date2 = DateTime.Now;
                    if (!DateTime.TryParse(gridEvent.data.Date3, out date3))
                        date3 = DateTime.Now;

                    DateTime localDate1 = CommonMethods.RetrieveLocalTimeFromUTCTime(service, timeZoneCode, date1);
                    tracer.Trace($"local Date 1 : {localDate1}");
                    DateTime localDate2 = CommonMethods.RetrieveLocalTimeFromUTCTime(service, timeZoneCode, date2);
                    tracer.Trace($"local Date 2 : {localDate2}");
                    DateTime localDate3 = CommonMethods.RetrieveLocalTimeFromUTCTime(service, timeZoneCode, date3);
                    tracer.Trace($"local Date 3 : {localDate3}");


                    switch (gridEvent.data.Event)
                    {
                        #region TURN_PROCESS_EVENTS
                        case Events.MOVE_OUT_DATE_CHANGED:
                            #region MOVE_OUT_DATE_CHANGED_1001
                            localDate1 = localDate1.ChangeTime(6, 0, 0, 0);
                            foreach (Entity projectEntity in projectEntityCollection.Entities)
                            {
                                Entity prjTemp = new Entity(projectEntity.LogicalName);
                                prjTemp.Id = projectEntity.Id;

                                prjTemp[Constants.Projects.DueDate] = (localDate1.AddHours(24) > DateTime.Now) ? localDate1.AddHours(24) : DateTime.Now;

                                service.Update(prjTemp);

                                PerformScheduledDateMoveOut(tracer, service, projectEntity, timeZoneCode, localDate1);
                            }
                            CommonMethods.ChangeEntityStatus(tracer, service, azureIntCallEntityReference, 0, 963850000);
                            #endregion
                            break;
                        case Events.ASSIGN_PROJECT_MANAGER:
                            #region ASSIGN_PROJECT_MANAGER_02
                            foreach (Entity projectEntity in projectEntityCollection.Entities)
                            {
                                EntityCollection assign_Proj_mgr_EntityCollection = CommonMethods.RetrieveOpenProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectTemplate), 2, "2");
                                foreach (Entity e in assign_Proj_mgr_EntityCollection.Entities)
                                {

                                    Entity num = new Entity(e.LogicalName)
                                    {
                                        Id = e.Id
                                    };
                                    num[Constants.ProjectTasks.ActualEnd] = localDate1;
                                    DateTime actStart = DateTime.Now;
                                    if (e.Attributes.Contains(Constants.ProjectTasks.ActualStart))
                                        actStart = CommonMethods.RetrieveLocalTimeFromUTCTime(service, timeZoneCode, e.GetAttributeValue<DateTime>(Constants.ProjectTasks.ActualStart));
                                    if (e.Attributes.Contains(Constants.ProjectTasks.ScheduledStart))
                                    {
                                        DateTime schStDate = CommonMethods.RetrieveLocalTimeFromUTCTime(service, timeZoneCode, e.GetAttributeValue<DateTime>(Constants.ProjectTasks.ScheduledStart));
                                        if (schStDate > localDate1)
                                            num[Constants.ProjectTasks.ScheduledStart] = actStart.AddHours(-1);
                                    }

                                    if (e.Attributes.Contains(Constants.ProjectTasks.ScheduledEnd))
                                    {
                                        DateTime schEndDate = CommonMethods.RetrieveLocalTimeFromUTCTime(service, timeZoneCode, e.GetAttributeValue<DateTime>(Constants.ProjectTasks.ScheduledEnd));
                                        if (schEndDate < actStart)
                                            num[Constants.ProjectTasks.ScheduledEnd] = localDate1.AddHours(1);
                                    }

                                    num[Constants.ProjectTasks.ActualDurationInMinutes] = (!e.Attributes.Contains(Constants.ProjectTasks.ActualStart)) ? 0 : (((localDate1 - actStart).TotalMinutes > 0) ? (int)Math.Floor((localDate1 - actStart).TotalMinutes) : 0);
                                    num[Constants.ProjectTasks.Progress] = new decimal(100);
                                    num[Constants.Status.StatusCode] = new OptionSetValue(963850001);
                                    service.Update(num);
                                    tracer.Trace("Task for Event : ASSIGN_PROJECT_MANAGER Successfully Updated.");
                                }

                                EntityCollection mkt_sch_pre_moveout_EntityCollection = CommonMethods.RetrieveOpenProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectTemplate), 4, "4");
                                foreach (Entity e in mkt_sch_pre_moveout_EntityCollection.Entities)
                                {
                                    Entity num = new Entity(e.LogicalName)
                                    {
                                        Id = e.Id
                                    };
                                    num[Constants.ProjectTasks.ActualStart] = localDate1;
                                    num[Constants.ProjectTasks.Progress] = new decimal(1);
                                    num[Constants.Status.StatusCode] = new OptionSetValue(963850000);
                                    service.Update(num);
                                    tracer.Trace("Task for Event : MARKET_SCHEDULES_PRE_MOVE_OUT Successfully Updated.");
                                }

                                tracer.Trace("Assigning Project Manager as Owner for each Project Task.");
                                AssignProjectManagerAsTaskOwner(tracer, service, projectEntity);
                            }
                            CommonMethods.ChangeEntityStatus(tracer, service, azureIntCallEntityReference, 0, 963850000);
                            #endregion
                            break;
                        case Events.CORPORATE_RENEWALS:
                            #region CORPORATE_RENEWALS_03
                            foreach (Entity projectEntity in projectEntityCollection.Entities)
                            {
                                Entity pTemp = new Entity(projectEntity.LogicalName);
                                pTemp.Id = projectEntity.Id;
                                pTemp[Constants.Projects.ActualEndDate] = DateTime.Now;
                                service.Update(pTemp);

                                CommonMethods.ChangeEntityStatus(tracer, service, projectEntity.ToEntityReference(), 1, 192350000);
                                CommonMethods.SetUnitSyncJobFlag(tracer, service, propertyEntity.ToEntityReference(), false);
                            }
                            CommonMethods.ChangeEntityStatus(tracer, service, azureIntCallEntityReference, 0, 963850000);
                            #endregion
                            break;
                        case Events.MARKET_SCHEDULES_PRE_MOVE_OUT:
                            tracer.Trace($"Processing  : {Events.MARKET_SCHEDULES_PRE_MOVE_OUT.ToString()} Event.");
                            #region MARKET_SCHEDULES_PRE_MOVE_OUT_04

                            foreach (Entity projectEntity in projectEntityCollection.Entities)
                            {
                                EntityCollection mkt_sch_pre_moveout_EntityCollection = CommonMethods.RetrieveOpenProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectTemplate), 4, "4");
                                foreach (Entity e in mkt_sch_pre_moveout_EntityCollection.Entities)
                                {
                                    Entity num = new Entity(e.LogicalName)
                                    {
                                        Id = e.Id
                                    };
                                    num[Constants.ProjectTasks.ActualEnd] = localDate1;
                                    DateTime actStart = DateTime.Now;
                                    if (e.Attributes.Contains(Constants.ProjectTasks.ActualStart))
                                        actStart = CommonMethods.RetrieveLocalTimeFromUTCTime(service, timeZoneCode, e.GetAttributeValue<DateTime>(Constants.ProjectTasks.ActualStart));
                                    num[Constants.ProjectTasks.ActualDurationInMinutes] = (!e.Attributes.Contains(Constants.ProjectTasks.ActualStart)) ? 0 : (((localDate1 - actStart).TotalMinutes > 0) ? (int)Math.Floor((localDate1 - actStart).TotalMinutes) : 0);
                                    num[Constants.ProjectTasks.Progress] = new decimal(100);
                                    num[Constants.Status.StatusCode] = new OptionSetValue(963850001);
                                    service.Update(num);
                                    tracer.Trace("Task for Event : MARKET_SCHEDULES_PRE_MOVE_OUT Successfully Updated.");
                                }

                                EntityCollection pre_moveout_Inspection_EntityCollection = CommonMethods.RetrieveOpenProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectTemplate), 5, "5");
                                foreach (Entity e in pre_moveout_Inspection_EntityCollection.Entities)
                                {
                                    Entity num = new Entity(e.LogicalName)
                                    {
                                        Id = e.Id
                                    };
                                    num[Constants.ProjectTasks.ScheduledStart] = date2;
                                    num[Constants.ProjectTasks.ScheduledEnd] = date3;
                                    service.Update(num);
                                    tracer.Trace("Task for Event : PRE_MOVE_OUT_INSPECTION Successfully Updated.");
                                }
                            }
                            CommonMethods.ChangeEntityStatus(tracer, service, azureIntCallEntityReference, 0, 963850000);
                            #endregion
                            break;
                        case Events.BUDGET_START:
                            #region BUDGET_START_07
                            foreach (Entity projectEntity in projectEntityCollection.Entities)
                            {
                                EntityCollection budget_start_EntityCollection = CommonMethods.RetrieveOpenProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectTemplate), (int)Events.BUDGET_START, "7");
                                foreach (Entity e in budget_start_EntityCollection.Entities)
                                {
                                    Entity num = new Entity(e.LogicalName)
                                    {
                                        Id = e.Id
                                    };
                                    if (!e.Attributes.Contains(Constants.ProjectTasks.ActualStart))
                                        num[Constants.ProjectTasks.ActualStart] = localDate1;
                                    num[Constants.ProjectTasks.ActualEnd] = localDate1;
                                    DateTime actStart = DateTime.Now;
                                    if (e.Attributes.Contains(Constants.ProjectTasks.ActualStart))
                                        actStart = CommonMethods.RetrieveLocalTimeFromUTCTime(service, timeZoneCode, e.GetAttributeValue<DateTime>(Constants.ProjectTasks.ActualStart));
                                    num[Constants.ProjectTasks.ActualDurationInMinutes] = (!e.Attributes.Contains(Constants.ProjectTasks.ActualStart)) ? 0 : (((localDate1 - actStart).TotalMinutes > 0) ? (int)Math.Floor((localDate1 - actStart).TotalMinutes) : 0);
                                    num[Constants.ProjectTasks.Progress] = new decimal(100);
                                    num[Constants.Status.StatusCode] = new OptionSetValue(963850001);
                                    service.Update(num);
                                    tracer.Trace($"Task for Event : {Events.BUDGET_START.ToString()} Successfully Updated.");
                                }

                                EntityCollection budget_approval_EntityCollection = CommonMethods.RetrieveOpenProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectTemplate), (int)Events.BUDGET_APPROVAL, "8");
                                foreach (Entity e in budget_approval_EntityCollection.Entities)
                                {
                                    Entity num = new Entity(e.LogicalName)
                                    {
                                        Id = e.Id
                                    };
                                    num[Constants.ProjectTasks.ActualStart] = localDate1;
                                    num[Constants.ProjectTasks.Progress] = new decimal(1);
                                    num[Constants.Status.StatusCode] = new OptionSetValue(963850000);
                                    service.Update(num);
                                    tracer.Trace($"Task for Event : {Events.BUDGET_APPROVAL.ToString()} Successfully Updated.");
                                }
                            }
                            CommonMethods.ChangeEntityStatus(tracer, service, azureIntCallEntityReference, 0, 963850000);
                            #endregion
                            break;
                        case Events.BUDGET_APPROVAL:
                            #region BUDGET_APPROVAL_08
                            foreach (Entity projectEntity in projectEntityCollection.Entities)
                            {
                                EntityCollection budget_start_EntityCollection = CommonMethods.RetrieveOpenProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectTemplate), (int)Events.BUDGET_START, "7");
                                foreach (Entity e in budget_start_EntityCollection.Entities)
                                {
                                    Entity num = new Entity(e.LogicalName)
                                    {
                                        Id = e.Id
                                    };
                                    if (!e.Attributes.Contains(Constants.ProjectTasks.ActualStart))
                                        num[Constants.ProjectTasks.ActualStart] = localDate1;
                                    num[Constants.ProjectTasks.ActualEnd] = localDate1;
                                    DateTime actStart = DateTime.Now;
                                    if (e.Attributes.Contains(Constants.ProjectTasks.ActualStart))
                                        actStart = CommonMethods.RetrieveLocalTimeFromUTCTime(service, timeZoneCode, e.GetAttributeValue<DateTime>(Constants.ProjectTasks.ActualStart));
                                    num[Constants.ProjectTasks.ActualDurationInMinutes] = (!e.Attributes.Contains(Constants.ProjectTasks.ActualStart)) ? 0 : (((localDate1 - actStart).TotalMinutes > 0) ? (int)Math.Floor((localDate1 - actStart).TotalMinutes) : 0);
                                    num[Constants.ProjectTasks.Progress] = new decimal(100);
                                    num[Constants.Status.StatusCode] = new OptionSetValue(963850001);
                                    service.Update(num);
                                    tracer.Trace($"Task for Event : {Events.BUDGET_START.ToString()} Successfully Updated.");
                                }

                                EntityCollection budget_approval_EntityCollection = CommonMethods.RetrieveOpenProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectTemplate), (int)Events.BUDGET_APPROVAL, "8");
                                foreach (Entity e in budget_approval_EntityCollection.Entities)
                                {
                                    tracer.Trace($"Actual End : {localDate1.ToString()} .");
                                    Entity num = new Entity(e.LogicalName)
                                    {
                                        Id = e.Id
                                    };
                                    if (!e.Attributes.Contains(Constants.ProjectTasks.ActualStart))
                                        num[Constants.ProjectTasks.ActualStart] = localDate1;
                                    num[Constants.ProjectTasks.ActualEnd] = localDate1;
                                    DateTime actStart = DateTime.Now;
                                    if (e.Attributes.Contains(Constants.ProjectTasks.ActualStart))
                                        actStart = CommonMethods.RetrieveLocalTimeFromUTCTime(service, timeZoneCode, e.GetAttributeValue<DateTime>(Constants.ProjectTasks.ActualStart));
                                    tracer.Trace($"Actual Start : {actStart.ToString()} .");
                                    tracer.Trace($"Total Minutes : {(localDate1 - actStart).TotalMinutes.ToString()} .");

                                    num[Constants.ProjectTasks.ActualDurationInMinutes] = (!e.Attributes.Contains(Constants.ProjectTasks.ActualStart)) ? 0 : (((localDate1 - actStart).TotalMinutes > 0) ? (int)Math.Floor((localDate1 - actStart).TotalMinutes) : 0);
                                    num[Constants.ProjectTasks.Progress] = new decimal(100);
                                    num[Constants.Status.StatusCode] = new OptionSetValue(963850001);
                                    service.Update(num);
                                    tracer.Trace($"Task for Event : {Events.BUDGET_APPROVAL.ToString()} Successfully Updated.");
                                }
                            }
                            CommonMethods.ChangeEntityStatus(tracer, service, azureIntCallEntityReference, 0, 963850000);
                            #endregion
                            break;
                        case Events.JOB_AND_CONTRACTS_SUBMITTED_TO_YARDI:
                            #region JOB_AND_CONTRACTS_SUBMITTED_TO_YARDI_10
                            tracer.Trace($"Processing  : {Events.JOB_AND_CONTRACTS_SUBMITTED_TO_YARDI.ToString()} Event.");
                            DateTime jobCreationDate = CommonMethods.RetrieveLocalTimeFromUTCTime(service, timeZoneCode, date1);
                            tracer.Trace($"local Date 1 : {jobCreationDate}");

                            foreach (Entity projectEntity in projectEntityCollection.Entities)
                            {
                                EntityCollection job_and_contracts_sub_to_yardi_EntityCollection = CommonMethods.RetrieveOpenProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectTemplate), 10, "10");
                                foreach (Entity e in job_and_contracts_sub_to_yardi_EntityCollection.Entities)
                                {
                                    Entity num = new Entity(e.LogicalName)
                                    {
                                        Id = e.Id
                                    };
                                    num[Constants.ProjectTasks.ActualEnd] = jobCreationDate;
                                    DateTime actStart = DateTime.Now;
                                    if (e.Attributes.Contains(Constants.ProjectTasks.ActualStart))
                                        actStart = CommonMethods.RetrieveLocalTimeFromUTCTime(service, timeZoneCode, e.GetAttributeValue<DateTime>(Constants.ProjectTasks.ActualStart));
                                    num[Constants.ProjectTasks.ActualDurationInMinutes] = (!e.Attributes.Contains(Constants.ProjectTasks.ActualStart)) ? 0 : (((localDate1 - actStart).TotalMinutes > 0) ? (int)Math.Floor((localDate1 - actStart).TotalMinutes) : 0);
                                    num[Constants.ProjectTasks.Progress] = new decimal(100);
                                    num[Constants.Status.StatusCode] = new OptionSetValue(963850001);
                                    service.Update(num);
                                    tracer.Trace("Task for Event : JOB_AND_CONTRACTS_SUBMITTED_TO_YARDI Successfully Updated.");
                                }
                            }
                            CommonMethods.ChangeEntityStatus(tracer, service, azureIntCallEntityReference, 0, 963850000);
                            #endregion
                            break;
                        case Events.VENDOR_SAYS_JOBS_COMPLETE:
                            #region VENDOR_SAYS_JOBS_COMPLETE_15
                            tracer.Trace($"Processing  : {Events.VENDOR_SAYS_JOBS_COMPLETE.ToString()} Event.");
                            tracer.Trace($"local Date 1 : {date1}");
                            foreach (Entity projectEntity in projectEntityCollection.Entities)
                            {
                                EntityCollection qc_inspection_EntityCollection = CommonMethods.RetrieveOpenProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectTemplate), 16, "14");
                                foreach (Entity e in qc_inspection_EntityCollection.Entities)
                                {
                                    Entity num = new Entity(e.LogicalName)
                                    {
                                        Id = e.Id
                                    };
                                    num[Constants.ProjectTasks.ScheduledStart] = date1;
                                    num[Constants.ProjectTasks.ScheduledEnd] = date1.AddDays(1);
                                    service.Update(num);
                                    tracer.Trace("Task for Event : VENDOR_SAYS_JOBS_COMPLETE Successfully Updated.");
                                }
                            }
                            CommonMethods.ChangeEntityStatus(tracer, service, azureIntCallEntityReference, 0, 963850000);
                            #endregion
                            break;
                        case Events.MULTI_VENDOR:
                            #region MULTI_VENDOR_29
                            if (gridEvent.data.Contracts is List<Contract> && gridEvent.data.Contracts.Count > 0)
                            {
                                foreach (Entity projectEntity in projectEntityCollection.Entities)
                                {
                                    if (projectEntity.Attributes.Contains(Constants.Projects.ProjectTemplate))
                                    {
                                        Mapping mapping = (
                                            from m in settings.Mappings
                                            where m.Key.Equals(projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectTemplate).Id.ToString(), StringComparison.OrdinalIgnoreCase)
                                            select m).FirstOrDefault<Mapping>();
                                        if (mapping is Mapping)
                                        {
                                            EntityCollection jobVendorEntityCollection = new EntityCollection();
                                            if (projectEntity.Attributes.Contains(Constants.Projects.Job))
                                            {
                                                jobVendorEntityCollection = RetrieveJobVenodrsByJob(tracer, service, projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.Job));
                                            }
                                            else if (projectEntity.Attributes.Contains(Constants.Projects.RenowalkID))
                                            {
                                                jobVendorEntityCollection = CommonMethods.RetrieveJobVenodrsByRenowalkID(tracer, service, projectEntity.GetAttributeValue<string>(Constants.Projects.RenowalkID));
                                            }
                                            else if (!string.IsNullOrEmpty(gridEvent.data.RenowalkID))
                                            {
                                                jobVendorEntityCollection = CommonMethods.RetrieveJobVenodrsByRenowalkID(tracer, service, gridEvent.data.RenowalkID);
                                            }

                                            if (jobVendorEntityCollection.Entities.Count > 0)
                                            {
                                                string error = CreateVendorProjectTask(tracer, service, projectEntity, jobVendorEntityCollection, gridEvent.data.Contracts, mapping, timeZoneCode);
                                                if (string.IsNullOrEmpty(error))
                                                    CommonMethods.ChangeEntityStatus(tracer, service, azureIntCallEntityReference, 0, 963850000);
                                                else
                                                    AzureIntegrationCallAsync.UpdateAzureIntegrationCallErrorDetails(service, azureIntCallEntityReference, error, 963850001);

                                            }
                                            else
                                                AzureIntegrationCallAsync.UpdateAzureIntegrationCallErrorDetails(service, azureIntCallEntityReference, $"No Job Vendor Record found in D365 system for Job with Renowalk ID : {projectEntity.GetAttributeValue<string>(Constants.Projects.RenowalkID)}.", 963850002);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                AzureIntegrationCallAsync.UpdateAzureIntegrationCallErrorDetails(service, azureIntCallEntityReference, $"No Contracts found in Data PayLoad.", 963850002);
                            }

                            #endregion
                            break;
                        #region commented_work
                        /*
                case Events.CORPORATE_RENEWALS:
                    #region CORPORATE_RENEWALS_03
                    foreach (Entity projectEntity in projectEntityCollection.Entities)
                    {
                        EntityCollection corporate_renewal_entityCollection = CommonMethods.RetrieveOpenProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), null, (int)Events.CORPORATE_RENEWALS, false);
                        foreach (Entity e in corporate_renewal_entityCollection.Entities)
                        {
                            Entity sTempEntity = new Entity(e.LogicalName)
                            {
                                Id = e.Id
                            };
                            sTempEntity[Constants.ProjectTasks.ActualStart] = date1;
                            sTempEntity[Constants.ProjectTasks.ActualEnd] = date1;
                            sTempEntity[Constants.ProjectTasks.Progress] = new decimal(100);
                            sTempEntity[Constants.Status.StatusCode] = new OptionSetValue(963850001);
                            service.Update(sTempEntity);
                            @event = Events.CORPORATE_RENEWALS;
                            tracer.Trace($"Task for Event : {Events.CORPORATE_RENEWALS.ToString()} Successfully Updated.");
                        }
                        EntityCollection yardi_lease_renew_entityCollection = CommonMethods.RetrieveOpenProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), null, (int)Events.CORPORATE_RENEWALS, false);
                        foreach (Entity e in yardi_lease_renew_entityCollection.Entities)
                        {
                            Entity sTempEntity = new Entity(e.LogicalName)
                            {
                                Id = e.Id
                            };
                            sTempEntity[Constants.ProjectTasks.ActualStart] = date1;
                            sTempEntity[Constants.ProjectTasks.ActualEnd] = date1;
                            sTempEntity[Constants.ProjectTasks.Progress] = new decimal(100);
                            sTempEntity[Constants.Status.StatusCode] = new OptionSetValue(963850001);
                            service.Update(sTempEntity);
                            @event = Events.CORPORATE_RENEWALS;
                            tracer.Trace($"Task for Event : {Events.CORPORATE_RENEWALS.ToString()} Successfully Updated.");
                        }
                        EntityCollection yardi_premoveout_sched_entityCollection = CommonMethods.RetrieveOpenProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), null, (int)Events.CORPORATE_RENEWALS, false);
                        foreach (Entity e in yardi_premoveout_sched_entityCollection.Entities)
                        {
                            Entity sTempEntity = new Entity(e.LogicalName)
                            {
                                Id = e.Id
                            };
                            sTempEntity[Constants.ProjectTasks.ActualStart] = date1;
                            sTempEntity[Constants.ProjectTasks.Progress] = new decimal(1);
                            sTempEntity[Constants.Status.StatusCode] = new OptionSetValue(963850000);
                            service.Update(sTempEntity);
                            @event = Events.CORPORATE_RENEWALS;
                            tracer.Trace($"Task for Event : {Events.CORPORATE_RENEWALS.ToString()} Successfully Updated.");
                        }
                    }
                    CommonMethods.ChangeEntityStatus(tracer, service, azureIntCallEntityReference, 0, 963850000);
                    #endregion
                    break;
                case Events.MARKET_SCHEDULES_PRE_MOVE_OUT:
                    #region MARKET_SCHEDULES_PRE_MOVE_OUT_04
                    foreach (Entity projectEntity in projectEntityCollection.Entities)
                    {
                        EntityCollection yardi_premove_out_sch_entityCollection = CommonMethods.RetrieveOpenProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), null, (int)Events.MARKET_SCHEDULES_PRE_MOVE_OUT, false);
                        foreach (Entity e in yardi_premove_out_sch_entityCollection.Entities)
                        {
                            Entity sTempEntity = new Entity(e.LogicalName)
                            {
                                Id = e.Id
                            };
                            sTempEntity[Constants.ProjectTasks.ActualEnd] = date1;
                            sTempEntity[Constants.ProjectTasks.Progress] = new decimal(100);
                            sTempEntity[Constants.Status.StatusCode] = new OptionSetValue(963850001);
                            service.Update(sTempEntity);
                            @event = Events.MARKET_SCHEDULES_PRE_MOVE_OUT;
                            tracer.Trace($"Task for Event : {Events.MARKET_SCHEDULES_PRE_MOVE_OUT.ToString()} Successfully Updated.");
                        }
                        EntityCollection renowalk_budget_started_entityCollection = CommonMethods.RetrieveOpenProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), null, (int)Events.MARKET_SCHEDULES_PRE_MOVE_OUT, false);
                        foreach (Entity e in renowalk_budget_started_entityCollection.Entities)
                        {
                            Entity sTempEntity = new Entity(e.LogicalName)
                            {
                                Id = e.Id
                            };
                            sTempEntity[Constants.ProjectTasks.ScheduledStart] = date2;
                            service.Update(sTempEntity);
                            @event = Events.MARKET_SCHEDULES_PRE_MOVE_OUT;
                            tracer.Trace($"Task for Event : {Events.MARKET_SCHEDULES_PRE_MOVE_OUT.ToString()} Successfully Updated.");
                        }
                    }
                    CommonMethods.ChangeEntityStatus(tracer, service, azureIntCallEntityReference, 0, 963850000);
                    #endregion
                    break;
                case Events.PRE_MOVE_OUT_INSPECTION:
                    #region PRE_MOVE_OUT_INSPECTION_05  
                    foreach (Entity projectEntity in projectEntityCollection.Entities)
                    {
                        EntityCollection renowalk_budget_started_entityCollection = CommonMethods.RetrieveOpenProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), null, (int)Events.PRE_MOVE_OUT_INSPECTION, false);
                        foreach (Entity e in renowalk_budget_started_entityCollection.Entities)
                        {
                            Entity sTempEntity = new Entity(e.LogicalName)
                            {
                                Id = e.Id
                            };
                            sTempEntity[Constants.ProjectTasks.ActualStart] = date1;
                            sTempEntity[Constants.ProjectTasks.Progress] = new decimal(1);
                            sTempEntity[Constants.Status.StatusCode] = new OptionSetValue(963850000);
                            service.Update(sTempEntity);
                            @event = Events.PRE_MOVE_OUT_INSPECTION;
                            tracer.Trace($"Task for Event : {Events.PRE_MOVE_OUT_INSPECTION.ToString()} Successfully Updated.");
                        }
                    }
                    CommonMethods.ChangeEntityStatus(tracer, service, azureIntCallEntityReference, 0, 963850000);
                    #endregion
                    break;
                case Events.FotoNotes_Move_Out_Insp_Complete:
                    #region FOTONOTES_MOVE_OUT_INSP_COMPLETE_06
                    foreach (Entity projectEntity in projectEntityCollection.Entities)
                    {
                        EntityCollection renowalk_budget_started_entityCollection = CommonMethods.RetrieveOpenProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), null, (int)Events.Renowalk_Budget_Started, false);
                        foreach (Entity e in renowalk_budget_started_entityCollection.Entities)
                        {
                            Entity sTempEntity = new Entity(e.LogicalName)
                            {
                                Id = e.Id
                            };
                            sTempEntity[Constants.ProjectTasks.ActualEnd] = date1;
                            sTempEntity[Constants.ProjectTasks.Progress] = new decimal(100);
                            sTempEntity[Constants.Status.StatusCode] = new OptionSetValue(963850001);
                            service.Update(sTempEntity);
                            @event = Events.Renowalk_Budget_Started;
                            tracer.Trace($"Task for Event : {Events.Renowalk_Budget_Started.ToString()} Successfully Updated.");
                        }
                    }
                    foreach (Entity projectEntity in projectEntityCollection.Entities)
                    {

                        EntityCollection fotonotes_move_out_insp_comp_entityCollection = CommonMethods.RetrieveOpenProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), null, (int)Events.FotoNotes_Move_Out_Insp_Complete, false);
                        foreach (Entity e in fotonotes_move_out_insp_comp_entityCollection.Entities)
                        {
                            Entity sTempEntity = new Entity(e.LogicalName)
                            {
                                Id = e.Id
                            };
                            sTempEntity[Constants.ProjectTasks.ActualStart] = date1;
                            sTempEntity[Constants.ProjectTasks.ActualEnd] = date2;
                            sTempEntity[Constants.ProjectTasks.Progress] = new decimal(100);
                            sTempEntity[Constants.Status.StatusCode] = new OptionSetValue(963850001);
                            service.Update(sTempEntity);
                            @event = Events.FotoNotes_Move_Out_Insp_Complete;
                            tracer.Trace($"Task for Event : {Events.FotoNotes_Move_Out_Insp_Complete.ToString()} Successfully Updated.");
                        }
                    }
                    foreach (Entity projectEntity in projectEntityCollection.Entities)
                    {
                        EntityCollection renowalk_project_status_walked_entityCollection = CommonMethods.RetrieveOpenProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), null, (int)Events.Renowalk_Project_Status_Walked, false);
                        foreach (Entity e in renowalk_project_status_walked_entityCollection.Entities)
                        {
                            Entity sTempEntity = new Entity(e.LogicalName)
                            {
                                Id = e.Id
                            };
                            sTempEntity[Constants.ProjectTasks.ActualStart] = date2;
                            sTempEntity[Constants.ProjectTasks.Progress] = new decimal(1);
                            sTempEntity[Constants.Status.StatusCode] = new OptionSetValue(963850000);
                            service.Update(sTempEntity);
                            @event = Events.Renowalk_Project_Status_Walked;
                            tracer.Trace($"Task for Event : {Events.Renowalk_Project_Status_Walked.ToString()} Successfully Updated.");
                        }
                    }
                    CommonMethods.ChangeEntityStatus(tracer, service, azureIntCallEntityReference, 0, 963850000);
                    #endregion
                    break;
                case Events.Renowalk_Project_Status_Walked:
                    #region RENOWALK_PROJECT_STATUS_WALKED_07
                    foreach (Entity projectEntity in projectEntityCollection.Entities)
                    {
                        EntityCollection renowalk_project_status_walked_entityCollection = CommonMethods.RetrieveOpenProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), null, (int)Events.Renowalk_Project_Status_Walked, false);
                        foreach (Entity e in renowalk_project_status_walked_entityCollection.Entities)
                        {
                            Entity sTempEntity = new Entity(e.LogicalName)
                            {
                                Id = e.Id
                            };
                            sTempEntity[Constants.ProjectTasks.ActualEnd] = date1;
                            sTempEntity[Constants.ProjectTasks.Progress] = new decimal(100);
                            sTempEntity[Constants.Status.StatusCode] = new OptionSetValue(963850001);
                            service.Update(sTempEntity);
                            @event = Events.Renowalk_Project_Status_Walked;
                            tracer.Trace($"Task for Event : {Events.Renowalk_Project_Status_Walked.ToString()} Successfully Updated.");
                        }
                        EntityCollection yardi_jobs_compl_entityCollection = CommonMethods.RetrieveOpenProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), null, (int)Events.Yardi_Jobs_Created, false);
                        foreach (Entity e in yardi_jobs_compl_entityCollection.Entities)
                        {
                            Entity sTempEntity = new Entity(e.LogicalName)
                            {
                                Id = e.Id
                            };
                            sTempEntity[Constants.ProjectTasks.ActualStart] = date1;
                            sTempEntity[Constants.ProjectTasks.Progress] = new decimal(1);
                            sTempEntity[Constants.Status.StatusCode] = new OptionSetValue(963850000);
                            service.Update(sTempEntity);
                            @event = Events.Yardi_Jobs_Created;
                            tracer.Trace($"Task for Event : {Events.Yardi_Jobs_Created.ToString()} Successfully Updated.");
                        }
                    }
                    CommonMethods.ChangeEntityStatus(tracer, service, azureIntCallEntityReference, 0, 963850000);
                    #endregion
                    break;
                case Events.Yardi_Jobs_Created:
                    #region YARDI_JOBS_CREATED_08
                    foreach (Entity projectEntity in projectEntityCollection.Entities)
                    {
                        EntityCollection yardi_jobs_created_entityCollection = CommonMethods.RetrieveOpenProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), null, (int)Events.Yardi_Jobs_Created, false);
                        foreach (Entity e in yardi_jobs_created_entityCollection.Entities)
                        {
                            Entity sTempEntity = new Entity(e.LogicalName)
                            {
                                Id = e.Id
                            };
                            sTempEntity[Constants.ProjectTasks.ActualEnd] = date1;
                            sTempEntity[Constants.ProjectTasks.Progress] = new decimal(100);
                            sTempEntity[Constants.Status.StatusCode] = new OptionSetValue(963850001);
                            service.Update(sTempEntity);
                            @event = Events.Yardi_Jobs_Created;
                            tracer.Trace($"Task for Event : {Events.Yardi_Jobs_Created.ToString()} Successfully Updated.");
                        }
                        EntityCollection job_assigned_to_vendor_entityCollection = CommonMethods.RetrieveOpenProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), null, (int)Events.Jobs_Assigned_To_Vendor, false);
                        foreach (Entity e in job_assigned_to_vendor_entityCollection.Entities)
                        {
                            Entity sTempEntity = new Entity(e.LogicalName)
                            {
                                Id = e.Id
                            };
                            sTempEntity[Constants.ProjectTasks.ActualStart] = date1;
                            sTempEntity[Constants.ProjectTasks.Progress] = new decimal(1);
                            sTempEntity[Constants.Status.StatusCode] = new OptionSetValue(963850000);
                            service.Update(sTempEntity);
                            @event = Events.Jobs_Assigned_To_Vendor;
                            tracer.Trace($"Task for Event : {Events.Jobs_Assigned_To_Vendor.ToString()} Successfully Updated.");
                        }
                    }
                    CommonMethods.ChangeEntityStatus(tracer, service, azureIntCallEntityReference, 0, 963850000);
                    #endregion
                    break;
                case Events.Jobs_Assigned_To_Vendor:
                    #region JOBS_ASSIGNED_TO_VENDOR_09
                    foreach (Entity projectEntity in projectEntityCollection.Entities)
                    {
                        EntityCollection job_assigned_to_vendor_entityCollection = CommonMethods.RetrieveOpenProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), null, (int)Events.Jobs_Assigned_To_Vendor, false);
                        foreach (Entity e in job_assigned_to_vendor_entityCollection.Entities)
                        {
                            Entity sTempEntity = new Entity(e.LogicalName)
                            {
                                Id = e.Id
                            };
                            sTempEntity[Constants.ProjectTasks.ActualEnd] = date1;
                            sTempEntity[Constants.ProjectTasks.Progress] = new decimal(100);
                            sTempEntity[Constants.Status.StatusCode] = new OptionSetValue(963850001);
                            service.Update(sTempEntity);
                            @event = Events.Jobs_Assigned_To_Vendor;
                            tracer.Trace($"Task for Event : {Events.Jobs_Assigned_To_Vendor.ToString()} Successfully Updated.");
                        }
                        EntityCollection fotonotes_interim_insp_compl_entityCollection = CommonMethods.RetrieveOpenProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), null, (int)Events.FotoNotes_Interim_Insp_Complete, false);
                        foreach (Entity e in fotonotes_interim_insp_compl_entityCollection.Entities)
                        {
                            Entity sTempEntity = new Entity(e.LogicalName)
                            {
                                Id = e.Id
                            };
                            sTempEntity[Constants.ProjectTasks.ActualStart] = date1;
                            sTempEntity[Constants.ProjectTasks.Progress] = new decimal(1);
                            sTempEntity[Constants.Status.StatusCode] = new OptionSetValue(963850000);
                            service.Update(sTempEntity);
                            @event = Events.FotoNotes_Interim_Insp_Complete;
                            tracer.Trace($"Task for Event : {Events.FotoNotes_Interim_Insp_Complete.ToString()} Successfully Updated.");
                        }
                    }
                    CommonMethods.ChangeEntityStatus(tracer, service, azureIntCallEntityReference, 0, 963850000);
                    #endregion
                    break;
                case Events.FotoNotes_Interim_Insp_Complete:
                    #region FOTONOTES_INTERIM_INSPECTION_COMPLETE_10
                    foreach (Entity projectEntity in projectEntityCollection.Entities)
                    {
                        EntityCollection fotonotes_interim_insp_compl_entityCollection = CommonMethods.RetrieveOpenProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), null, (int)Events.FotoNotes_Interim_Insp_Complete, false);
                        foreach (Entity e in fotonotes_interim_insp_compl_entityCollection.Entities)
                        {
                            Entity sTempEntity = new Entity(e.LogicalName)
                            {
                                Id = e.Id
                            };
                            sTempEntity[Constants.ProjectTasks.Progress] = new decimal(50);
                            sTempEntity[Constants.Status.StatusCode] = new OptionSetValue(963850000);
                            service.Update(sTempEntity);
                            @event = Events.FotoNotes_Interim_Insp_Complete;
                            tracer.Trace($"Task for Event : {Events.FotoNotes_Interim_Insp_Complete.ToString()} Successfully Updated.");
                        }
                    }
                    CommonMethods.ChangeEntityStatus(tracer, service, azureIntCallEntityReference, 0, 963850000);
                    #endregion
                    break;
                case Events.Yardi_Jobs_Completed:
                    #region YARDI_JOBS_COMPLETED_11
                    foreach (Entity projectEntity in projectEntityCollection.Entities)
                    {
                        EntityCollection fotonotes_interim_insp_compl_entityCollection = CommonMethods.RetrieveOpenProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), null, (int)Events.FotoNotes_Interim_Insp_Complete, false);
                        foreach (Entity e in fotonotes_interim_insp_compl_entityCollection.Entities)
                        {
                            Entity sTempEntity = new Entity(e.LogicalName)
                            {
                                Id = e.Id
                            };
                            sTempEntity[Constants.ProjectTasks.ActualEnd] = date1;
                            sTempEntity[Constants.ProjectTasks.Progress] = new decimal(100);
                            sTempEntity[Constants.Status.StatusCode] = new OptionSetValue(963850001);
                            service.Update(sTempEntity);
                            @event = Events.FotoNotes_Interim_Insp_Complete;
                            tracer.Trace($"Task for Event : {Events.FotoNotes_Interim_Insp_Complete.ToString()} Successfully Updated.");
                        }
                        EntityCollection yardi_jobs_compl_entityCollection = CommonMethods.RetrieveOpenProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), null, (int)Events.Yardi_Jobs_Completed, false);
                        foreach (Entity e in yardi_jobs_compl_entityCollection.Entities)
                        {
                            Entity sTempEntity = new Entity(e.LogicalName)
                            {
                                Id = e.Id
                            };
                            sTempEntity[Constants.ProjectTasks.ActualStart] = date1;
                            sTempEntity[Constants.ProjectTasks.ActualEnd] = date1;
                            sTempEntity[Constants.ProjectTasks.Progress] = new decimal(100);
                            sTempEntity[Constants.Status.StatusCode] = new OptionSetValue(963850001);
                            service.Update(sTempEntity);
                            @event = Events.Yardi_Jobs_Completed;
                            tracer.Trace($"Task for Event : {Events.Yardi_Jobs_Completed.ToString()} Successfully Updated.");
                        }
                        EntityCollection fotonotes_insp_compl_with_add_work_entityCollection = CommonMethods.RetrieveOpenProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), null, (int)Events.FotoNotes_QC_Insp_Complete_With_Add_Work, false);
                        foreach (Entity e in fotonotes_insp_compl_with_add_work_entityCollection.Entities)
                        {
                            Entity sTempEntity = new Entity(e.LogicalName)
                            {
                                Id = e.Id
                            };
                            sTempEntity[Constants.ProjectTasks.ActualStart] = date1;
                            sTempEntity[Constants.ProjectTasks.Progress] = new decimal(1);
                            sTempEntity[Constants.Status.StatusCode] = new OptionSetValue(963850000);
                            service.Update(sTempEntity);
                            @event = Events.FotoNotes_QC_Insp_Complete_With_Add_Work;
                            tracer.Trace($"Task for Event : {Events.FotoNotes_QC_Insp_Complete_With_Add_Work.ToString()} Successfully Updated.");
                        }
                    }
                    CommonMethods.ChangeEntityStatus(tracer, service, azureIntCallEntityReference, 0, 963850000);
                    #endregion
                    break;
                case Events.FotoNotes_QC_Insp_Complete_With_Add_Work:
                    #region FOTONOTES_QC_INSPECTION_COMPLETE_WITH_ADD_WORK_12
                    bool isError = false;
                    string errorMessage = string.Empty;
                    foreach (Entity projectEntity in projectEntityCollection.Entities)
                    {
                        EntityCollection qc_insp_comp_with_add_Work_entityCollection = CommonMethods.RetrieveOpenProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), null, (int)Events.FotoNotes_QC_Insp_Complete_With_Add_Work, false);
                        if (qc_insp_comp_with_add_Work_entityCollection.Entities.Count <= 0)
                        {
                            isError = true;
                            @event = Events.FotoNotes_QC_Insp_Complete_With_Add_Work;
                            errorMessage = $"Turn Process Event  {Events.FotoNotes_QC_Insp_Complete_With_Add_Work.ToString()} previously Completed. Event Identify as Duplicate Event.";
                        }
                        else
                        {
                            Entity projectTaskEntity = qc_insp_comp_with_add_Work_entityCollection.Entities[0];
                            EntityCollection fotonotes_interim_insp_compl_entityCollection = CommonMethods.RetrieveAllProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), null, (int)Events.FotoNotes_Interim_Insp_Complete, false);
                            if (fotonotes_interim_insp_compl_entityCollection.Entities.Count > 0)
                            {
                                Entity e = fotonotes_interim_insp_compl_entityCollection.Entities[0];
                                Entity sTempEntity = new Entity(projectTaskEntity.LogicalName)
                                {
                                    Attributes = e.Attributes
                                };
                                sTempEntity.Id = new Guid();
                                if (sTempEntity.Attributes.Contains(Constants.ProjectTasks.PrimaryKey))
                                    sTempEntity.Attributes.Remove(Constants.ProjectTasks.PrimaryKey);
                                if (sTempEntity.Attributes.Contains(Constants.ProjectTasks.ActualStart))
                                    sTempEntity.Attributes.Remove(Constants.ProjectTasks.ActualStart);
                                if (sTempEntity.Attributes.Contains(Constants.ProjectTasks.ActualEnd))
                                    sTempEntity.Attributes.Remove(Constants.ProjectTasks.ActualEnd);
                                sTempEntity[Constants.ProjectTasks.WBSID] = $"{projectTaskEntity.GetAttributeValue<string>(Constants.ProjectTasks.WBSID)}.1";
                                sTempEntity[Constants.ProjectTasks.ParentTask] = projectTaskEntity.ToEntityReference();
                                sTempEntity[Constants.ProjectTasks.ScheduledStart] = date1;
                                sTempEntity[Constants.ProjectTasks.ScheduledEnd] = date1.AddHours(24);
                                sTempEntity[Constants.ProjectTasks.Progress] = new decimal(0);
                                sTempEntity[Constants.Status.StatusCode] = new OptionSetValue(1);
                                service.Create(sTempEntity);
                                @event = Events.FotoNotes_Interim_Insp_Complete;
                                tracer.Trace($"Task for Event : {Events.FotoNotes_Interim_Insp_Complete.ToString()} Successfully Created.");
                            }
                            EntityCollection yardi_jobs_compl_entityCollection = CommonMethods.RetrieveAllProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), null, (int)Events.Yardi_Jobs_Completed, false);
                            if (yardi_jobs_compl_entityCollection.Entities.Count > 0)
                            {
                                Entity e = yardi_jobs_compl_entityCollection.Entities[0];
                                Entity sTempEntity1 = new Entity(projectTaskEntity.LogicalName)
                                {
                                    Attributes = e.Attributes
                                };
                                sTempEntity1.Id = new Guid();
                                if (sTempEntity1.Attributes.Contains(Constants.ProjectTasks.PrimaryKey))
                                    sTempEntity1.Attributes.Remove(Constants.ProjectTasks.PrimaryKey);
                                if (sTempEntity1.Attributes.Contains(Constants.ProjectTasks.ActualStart))
                                    sTempEntity1.Attributes.Remove(Constants.ProjectTasks.ActualStart);
                                if (sTempEntity1.Attributes.Contains(Constants.ProjectTasks.ActualEnd))
                                    sTempEntity1.Attributes.Remove(Constants.ProjectTasks.ActualEnd);
                                sTempEntity1[Constants.ProjectTasks.WBSID] = string.Concat(projectTaskEntity.GetAttributeValue<string>(Constants.ProjectTasks.WBSID), ".2");
                                sTempEntity1[Constants.ProjectTasks.ParentTask] = projectTaskEntity.ToEntityReference();
                                sTempEntity1[Constants.ProjectTasks.ScheduledStart] = date1.AddHours(24);
                                sTempEntity1[Constants.ProjectTasks.ScheduledEnd] = date1.AddHours(48);
                                sTempEntity1[Constants.ProjectTasks.Progress] = new decimal(0);
                                sTempEntity1[Constants.Status.StatusCode] = new OptionSetValue(1);
                                service.Create(sTempEntity1);
                                @event = Events.Yardi_Jobs_Completed;
                                tracer.Trace($"Task for Event : {Events.Yardi_Jobs_Completed.ToString()} Successfully Created.");
                            }

                            Entity sTempEntity2 = new Entity(projectTaskEntity.LogicalName)
                            {
                                Attributes = projectTaskEntity.Attributes
                            };
                            sTempEntity2.Id = new Guid();
                            if (sTempEntity2.Attributes.Contains(Constants.ProjectTasks.PrimaryKey))
                                sTempEntity2.Attributes.Remove(Constants.ProjectTasks.PrimaryKey);
                            if (sTempEntity2.Attributes.Contains(Constants.ProjectTasks.ActualStart))
                                sTempEntity2.Attributes.Remove(Constants.ProjectTasks.ActualStart);
                            if (sTempEntity2.Attributes.Contains(Constants.ProjectTasks.ActualEnd))
                                sTempEntity2.Attributes.Remove(Constants.ProjectTasks.ActualEnd);
                            sTempEntity2[Constants.ProjectTasks.WBSID] = string.Concat(projectTaskEntity.GetAttributeValue<string>(Constants.ProjectTasks.WBSID), ".3");
                            sTempEntity2[Constants.ProjectTasks.ParentTask] = projectTaskEntity.ToEntityReference();
                            sTempEntity2[Constants.ProjectTasks.ScheduledStart] = date1.AddHours(24);
                            sTempEntity2[Constants.ProjectTasks.ScheduledEnd] = date1.AddHours(48);
                            sTempEntity2[Constants.ProjectTasks.Progress] = new decimal(0);
                            sTempEntity2[Constants.Status.StatusCode] = new OptionSetValue(1);
                            service.Create(sTempEntity2);
                            @event = Events.FotoNotes_QC_Insp_Complete_With_Add_Work;
                            tracer.Trace($"Task for Event : {Events.FotoNotes_QC_Insp_Complete_With_Add_Work.ToString()} Successfully Created.");

                            Entity tmpEntity = new Entity(projectTaskEntity.LogicalName)
                            {
                                Id = projectTaskEntity.Id
                            };
                            tmpEntity[Constants.ProjectTasks.ActualEnd] = date1;
                            tmpEntity[Constants.ProjectTasks.Progress] = new decimal(100);
                            tmpEntity[Constants.Status.StatusCode] = new OptionSetValue(963850001);
                            service.Update(tmpEntity);
                            @event = Events.FotoNotes_QC_Insp_Complete_With_Add_Work;
                            tracer.Trace($"Task for Event : {Events.FotoNotes_QC_Insp_Complete_With_Add_Work.ToString()} Successfully Updated.");
                        }
                    }
                    if (isError)
                        AzureIntegrationCallAsync.UpdateAzureIntegrationCallErrorDetails(service, azureIntCallEntityReference, errorMessage, 963850002);
                    else
                        CommonMethods.ChangeEntityStatus(tracer, service, azureIntCallEntityReference, 0, 963850000);
                    #endregion
                    break;
                case Events.FotoNotes_QC_Insp_Complete_With_No_Add_Work:
                    #region FOTONOTES_QC_INSP_COMPLETE_WITH_NO_ADD_WORK_13
                    foreach (Entity projectEntity in projectEntityCollection.Entities)
                    {
                        EntityCollection qc_insp_comp_with_add_Work_entityCollection = CommonMethods.RetrieveOpenProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), null, (int)Events.FotoNotes_QC_Insp_Complete_With_Add_Work, true);
                        if (qc_insp_comp_with_add_Work_entityCollection.Entities.Count > 0)
                        {
                            Entity e = qc_insp_comp_with_add_Work_entityCollection.Entities[0];
                            Entity sTempEntity = new Entity(e.LogicalName)
                            {
                                Id = e.Id
                            };
                            sTempEntity[Constants.ProjectTasks.ActualEnd] = date1;
                            sTempEntity[Constants.ProjectTasks.Progress] = new decimal(100);
                            sTempEntity[Constants.Status.StatusCode] = new OptionSetValue(963850001);
                            service.Update(sTempEntity);
                            @event = Events.FotoNotes_QC_Insp_Complete_With_Add_Work;
                            tracer.Trace($"Task for Event : {Events.FotoNotes_QC_Insp_Complete_With_Add_Work.ToString()} Successfully Updated.");
                        }
                        EntityCollection yardi_jobs_compl_entityCollection = CommonMethods.RetrieveOpenProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), null, (int)Events.Yardi_Jobs_Completed, true);
                        if (yardi_jobs_compl_entityCollection.Entities.Count > 0)
                        {
                            Entity e = yardi_jobs_compl_entityCollection.Entities[0];
                            Entity sTempEntity = new Entity(e.LogicalName)
                            {
                                Id = e.Id
                            };
                            sTempEntity[Constants.ProjectTasks.ActualStart] = date1;
                            sTempEntity[Constants.ProjectTasks.ActualEnd] = date1;
                            sTempEntity[Constants.ProjectTasks.Progress] = new decimal(100);
                            sTempEntity[Constants.Status.StatusCode] = new OptionSetValue(963850001);
                            service.Update(sTempEntity);
                            @event = Events.Yardi_Jobs_Completed;
                            tracer.Trace($"Task for Event : {Events.Yardi_Jobs_Completed.ToString()} Successfully Updated.");
                        }
                        EntityCollection fotonotes_mkt_insp_compl_entityCollection = CommonMethods.RetrieveOpenProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), null, (int)Events.FotoNotes_Marketing_Insp_Complete, false);
                        if (fotonotes_mkt_insp_compl_entityCollection.Entities.Count > 0)
                        {
                            Entity e = fotonotes_mkt_insp_compl_entityCollection.Entities[0];
                            Entity sTempEntity = new Entity(e.LogicalName)
                            {
                                Id = e.Id
                            };
                            sTempEntity[Constants.ProjectTasks.ActualStart] = date1;
                            sTempEntity[Constants.ProjectTasks.Progress] = new decimal(1);
                            sTempEntity[Constants.Status.StatusCode] = new OptionSetValue(963850000);
                            service.Update(sTempEntity);
                            @event = Events.FotoNotes_Marketing_Insp_Complete;
                            tracer.Trace($"Task for Event : {Events.FotoNotes_Marketing_Insp_Complete.ToString()} Successfully Updated.");
                        }
                    }
                    CommonMethods.ChangeEntityStatus(tracer, service, azureIntCallEntityReference, 0, 963850000);
                    #endregion
                    break;
                case Events.FotoNotes_Marketing_Insp_Complete:
                    #region FOTONOTES_MARKETING_INSPECTION_COMPLETE_14
                    foreach (Entity projectEntity in projectEntityCollection.Entities)
                    {
                        EntityCollection fotonotes_mkt_insp_compl_entityCollection = CommonMethods.RetrieveOpenProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), null, (int)Events.FotoNotes_Marketing_Insp_Complete, false);
                        if (fotonotes_mkt_insp_compl_entityCollection.Entities.Count > 0)
                        {
                            Entity e = fotonotes_mkt_insp_compl_entityCollection.Entities[0];
                            Entity sTempEntity = new Entity(e.LogicalName)
                            {
                                Id = e.Id
                            };
                            sTempEntity[Constants.ProjectTasks.ActualEnd] = date1;
                            sTempEntity[Constants.ProjectTasks.Progress] = new decimal(100);
                            sTempEntity[Constants.Status.StatusCode] = new OptionSetValue(963850001);
                            service.Update(sTempEntity);
                            @event = Events.FotoNotes_Marketing_Insp_Complete;
                            tracer.Trace($"Task for Event : {Events.FotoNotes_Marketing_Insp_Complete.ToString()} Successfully Updated.");
                        }
                        EntityCollection fotonotes_bi_weekly_insp_compl_entityCollection = CommonMethods.RetrieveOpenProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), null, (int)Events.FotoNotes_Bi_Weekly_Insp_Complete, false);
                        if (fotonotes_bi_weekly_insp_compl_entityCollection.Entities.Count > 0)
                        {
                            Entity e = fotonotes_bi_weekly_insp_compl_entityCollection.Entities[0];
                            Entity sTempEntity = new Entity(e.LogicalName)
                            {
                                Id = e.Id
                            };
                            sTempEntity[Constants.ProjectTasks.ScheduledStart] = date1.AddDays(14);
                            sTempEntity[Constants.Status.StatusCode] = new OptionSetValue(1);
                            service.Update(sTempEntity);
                            @event = Events.FotoNotes_Bi_Weekly_Insp_Complete;
                            tracer.Trace($"Task for Event : {Events.FotoNotes_Bi_Weekly_Insp_Complete.ToString()} Successfully Updated.");
                        }
                    }
                    CommonMethods.ChangeEntityStatus(tracer, service, azureIntCallEntityReference, 0, 963850000);
                    #endregion
                    break;
                case Events.FotoNotes_Bi_Weekly_Insp_Complete:
                    #region FOTONOTES_BI_WEEKLY_INSPECTION_COMPLETE_15
                    foreach (Entity projectEntity in projectEntityCollection.Entities)
                    {
                        EntityCollection fotonotes_bi_weekly_insp_compl_entityCollection = CommonMethods.RetrieveOpenProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), null, (int)Events.FotoNotes_Bi_Weekly_Insp_Complete, false);
                        if (fotonotes_bi_weekly_insp_compl_entityCollection.Entities.Count > 0)
                        {
                            Entity e = fotonotes_bi_weekly_insp_compl_entityCollection.Entities[0];
                            Entity sTempEntity = new Entity(e.LogicalName)
                            {
                                Id = e.Id
                            };
                            sTempEntity[Constants.ProjectTasks.ActualEnd] = date1;
                            sTempEntity[Constants.ProjectTasks.Progress] = new decimal(100);
                            sTempEntity[Constants.Status.StatusCode] = new OptionSetValue(963850001);
                            service.Update(sTempEntity);
                            @event = Events.FotoNotes_Bi_Weekly_Insp_Complete;
                            tracer.Trace($"Task for Event : {Events.FotoNotes_Bi_Weekly_Insp_Complete.ToString()} Successfully Updated.");

                            Entity bi_weekly_insp_Comp_Entity = new Entity(e.LogicalName)
                            {
                                Attributes = e.Attributes
                            };
                            guid = new Guid();
                            bi_weekly_insp_Comp_Entity.Id = guid;
                            if (bi_weekly_insp_Comp_Entity.Attributes.Contains(Constants.ProjectTasks.PrimaryKey))
                            {
                                bi_weekly_insp_Comp_Entity.Attributes.Remove(Constants.ProjectTasks.PrimaryKey);
                            }
                            if (bi_weekly_insp_Comp_Entity.Attributes.Contains(Constants.ProjectTasks.ActualStart))
                            {
                                bi_weekly_insp_Comp_Entity.Attributes.Remove(Constants.ProjectTasks.ActualStart);
                            }
                            if (bi_weekly_insp_Comp_Entity.Attributes.Contains(Constants.ProjectTasks.ActualEnd))
                            {
                                bi_weekly_insp_Comp_Entity.Attributes.Remove(Constants.ProjectTasks.ActualEnd);
                            }
                            bi_weekly_insp_Comp_Entity[Constants.ProjectTasks.WBSID] = string.Concat(e.GetAttributeValue<string>(Constants.ProjectTasks.WBSID), ".1");
                            bi_weekly_insp_Comp_Entity[Constants.ProjectTasks.ParentTask] = e.ToEntityReference();
                            bi_weekly_insp_Comp_Entity[Constants.ProjectTasks.ScheduledStart] = date1.AddDays(14);
                            bi_weekly_insp_Comp_Entity[Constants.ProjectTasks.ScheduledEnd] = date1.AddDays(14);
                            bi_weekly_insp_Comp_Entity[Constants.ProjectTasks.Progress] = new decimal(0);
                            bi_weekly_insp_Comp_Entity[Constants.Status.StatusCode] = new OptionSetValue(1);
                            service.Create(bi_weekly_insp_Comp_Entity);
                            @event = Events.FotoNotes_Bi_Weekly_Insp_Complete;
                            tracer.Trace($"Task for Event : {Events.FotoNotes_Bi_Weekly_Insp_Complete.ToString()} Successfully Created.");
                        }
                    }
                    CommonMethods.ChangeEntityStatus(tracer, service, azureIntCallEntityReference, 0, 963850000);
                    #endregion
                    break;
                case Events.FotoNotes_Move_In_Insp_Complete:
                    #region FOTONOTES_MOVE_IN_INSP_COMPLETE_16
                    foreach (Entity projectEntity in projectEntityCollection.Entities)
                    {
                        EntityCollection fotonotes_move_in_insp_compl_entityCollection = CommonMethods.RetrieveOpenProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), null, (int)Events.FotoNotes_Move_In_Insp_Complete, false);
                        if (fotonotes_move_in_insp_compl_entityCollection.Entities.Count > 0)
                        {
                            Entity e = fotonotes_move_in_insp_compl_entityCollection.Entities[0];
                            Entity sTempEntity = new Entity(e.LogicalName)
                            {
                                Id = e.Id
                            };
                            sTempEntity[Constants.ProjectTasks.ActualEnd] = date1;
                            sTempEntity[Constants.ProjectTasks.Progress] = new decimal(100);
                            sTempEntity[Constants.Status.StatusCode] = new OptionSetValue(963850001);
                            service.Update(sTempEntity);
                            @event = Events.FotoNotes_Move_In_Insp_Complete;
                            tracer.Trace($"Task for Event : {Events.FotoNotes_Move_In_Insp_Complete.ToString()} Successfully Updated.");
                        }
                        EntityCollection fotonotes_bi_weekly_insp_compl_entityCollection = CommonMethods.RetrieveOpenProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), null, (int)Events.FotoNotes_Bi_Weekly_Insp_Complete, true);
                        if (fotonotes_bi_weekly_insp_compl_entityCollection.Entities.Count > 0)
                        {
                            Entity e = fotonotes_bi_weekly_insp_compl_entityCollection.Entities[0];
                            Entity sTempEntity = new Entity(e.LogicalName)
                            {
                                Id = e.Id
                            };
                            sTempEntity[Constants.Status.StateCode] = new OptionSetValue(1);
                            sTempEntity[Constants.Status.StatusCode] = new OptionSetValue(2);
                            service.Update(sTempEntity);
                            @event = Events.FotoNotes_Bi_Weekly_Insp_Complete;
                            tracer.Trace($"Task for Event : {Events.FotoNotes_Bi_Weekly_Insp_Complete.ToString()} Successfully Cancelled.");
                        }
                        CommonMethods.ChangeEntityStatus(tracer, service, projectEntity.ToEntityReference(), 1, 192350000);
                    }
                    CommonMethods.ChangeEntityStatus(tracer, service, azureIntCallEntityReference, 0, 963850000);
                    #endregion
                    break;
                    */
                        #endregion
                        #endregion
                        #region INITIAL RENOVATION EVENTS
                        case Events.IR_ASSIGN_PROJECT_MANAGER:
                            #region IR_ASSIGN_PROJECT_MANAGER_202
                            foreach (Entity projectEntity in projectEntityCollection.Entities)
                            {
                                EntityCollection assign_Proj_mgr_EntityCollection = CommonMethods.RetrieveOpenProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectTemplate), 202, "2");
                                foreach (Entity e in assign_Proj_mgr_EntityCollection.Entities)
                                {

                                    Entity num = new Entity(e.LogicalName)
                                    {
                                        Id = e.Id
                                    };
                                    num[Constants.ProjectTasks.ActualEnd] = localDate1;
                                    DateTime actStart = DateTime.Now;
                                    if (e.Attributes.Contains(Constants.ProjectTasks.ActualStart))
                                        actStart = CommonMethods.RetrieveLocalTimeFromUTCTime(service, timeZoneCode, e.GetAttributeValue<DateTime>(Constants.ProjectTasks.ActualStart));
                                    num[Constants.ProjectTasks.ActualDurationInMinutes] = (!e.Attributes.Contains(Constants.ProjectTasks.ActualStart)) ? 0 : (((localDate1 - actStart).TotalMinutes > 0) ? (int)Math.Floor((localDate1 - actStart).TotalMinutes) : 0);
                                    num[Constants.ProjectTasks.Progress] = new decimal(100);
                                    num[Constants.Status.StatusCode] = new OptionSetValue(963850001);
                                    service.Update(num);
                                    tracer.Trace($"Task for Event : {Events.IR_ASSIGN_PROJECT_MANAGER.ToString()} Successfully Updated.");
                                }

                                EntityCollection sch_due_deligence_EntityCollection = CommonMethods.RetrieveOpenProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectTemplate), (int)Events.SCHEDULE_DUE_DILLIGENCE_INSPECTION, "3");
                                foreach (Entity e in sch_due_deligence_EntityCollection.Entities)
                                {
                                    Entity num = new Entity(e.LogicalName)
                                    {
                                        Id = e.Id
                                    };
                                    num[Constants.ProjectTasks.ActualStart] = localDate1;
                                    num[Constants.ProjectTasks.Progress] = new decimal(1);
                                    num[Constants.Status.StatusCode] = new OptionSetValue(963850000);
                                    service.Update(num);
                                    tracer.Trace($"Task for Event : {Events.SCHEDULE_DUE_DILLIGENCE_INSPECTION.ToString()} Successfully Updated.");
                                }
                                tracer.Trace("Assigning Project Manager as Owner for each Project Task.");
                                AssignProjectManagerAsTaskOwner(tracer, service, projectEntity);
                            }
                            CommonMethods.ChangeEntityStatus(tracer, service, azureIntCallEntityReference, 0, 963850000);
                            #endregion
                            break;
                        case Events.SCHEDULE_DUE_DILLIGENCE_INSPECTION:
                            #region SCHEDULE_DUE_DILLIGENCE_INSPECTION_203
                            foreach (Entity projectEntity in projectEntityCollection.Entities)
                            {
                                EntityCollection sch_due_deligence_EntityCollection = CommonMethods.RetrieveOpenProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectTemplate), (int)Events.SCHEDULE_DUE_DILLIGENCE_INSPECTION, "3");
                                foreach (Entity e in sch_due_deligence_EntityCollection.Entities)
                                {
                                    Entity num = new Entity(e.LogicalName)
                                    {
                                        Id = e.Id
                                    };
                                    DateTime actStart = DateTime.Now;
                                    if (e.Attributes.Contains(Constants.ProjectTasks.ActualStart))
                                        actStart = CommonMethods.RetrieveLocalTimeFromUTCTime(service, timeZoneCode, e.GetAttributeValue<DateTime>(Constants.ProjectTasks.ActualStart));

                                    num[Constants.ProjectTasks.ActualEnd] = localDate1;
                                    num[Constants.ProjectTasks.ActualDurationInMinutes] = (!e.Attributes.Contains(Constants.ProjectTasks.ActualStart)) ? 0 : (((localDate1 - actStart).TotalMinutes > 0) ? (int)Math.Floor((localDate1 - actStart).TotalMinutes) : 0);
                                    num[Constants.ProjectTasks.Progress] = new decimal(100);
                                    num[Constants.Status.StatusCode] = new OptionSetValue(963850001);
                                    service.Update(num);
                                    tracer.Trace($"Task for Event : {Events.SCHEDULE_DUE_DILLIGENCE_INSPECTION.ToString()} Successfully Updated.");
                                }

                                EntityCollection budget_started_EntityCollection = CommonMethods.RetrieveOpenProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectTemplate), (int)Events.IR_BUDGET_START, "4");
                                foreach (Entity e in budget_started_EntityCollection.Entities)
                                {
                                    Entity num = new Entity(e.LogicalName)
                                    {
                                        Id = e.Id
                                    };
                                    num[Constants.ProjectTasks.ActualStart] = localDate1;
                                    tracer.Trace($"App Scheduled Start : {date2.ToString()}");
                                    tracer.Trace($"App Scheduled End : {date3.ToString()}");

                                    num[Constants.ProjectTasks.ScheduledStart] = localDate2;
                                    num[Constants.ProjectTasks.ScheduledEnd] = localDate3;
                                    num[Constants.ProjectTasks.Progress] = new decimal(1);
                                    num[Constants.Status.StatusCode] = new OptionSetValue(963850000);
                                    service.Update(num);
                                    tracer.Trace($"Task for Event : {Events.IR_BUDGET_START.ToString()} Successfully Updated.");
                                }

                            }
                            CommonMethods.ChangeEntityStatus(tracer, service, azureIntCallEntityReference, 0, 963850000);
                            #endregion
                            break;
                        case Events.IR_BUDGET_START:
                            #region IR_BUDGET_START_204
                            foreach (Entity projectEntity in projectEntityCollection.Entities)
                            {
                                EntityCollection budget_start_EntityCollection = CommonMethods.RetrieveOpenProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectTemplate), (int)Events.IR_BUDGET_START, "4");
                                foreach (Entity e in budget_start_EntityCollection.Entities)
                                {
                                    Entity num = new Entity(e.LogicalName)
                                    {
                                        Id = e.Id
                                    };
                                    if (!e.Attributes.Contains(Constants.ProjectTasks.ActualStart))
                                        num[Constants.ProjectTasks.ActualStart] = localDate1;
                                    num[Constants.ProjectTasks.ActualEnd] = localDate1;
                                    DateTime actStart = DateTime.Now;
                                    if (e.Attributes.Contains(Constants.ProjectTasks.ActualStart))
                                        actStart = CommonMethods.RetrieveLocalTimeFromUTCTime(service, timeZoneCode, e.GetAttributeValue<DateTime>(Constants.ProjectTasks.ActualStart));
                                    num[Constants.ProjectTasks.ActualDurationInMinutes] = (!e.Attributes.Contains(Constants.ProjectTasks.ActualStart)) ? 0 : (((localDate1 - actStart).TotalMinutes > 0) ? (int)Math.Floor((localDate1 - actStart).TotalMinutes) : 0);
                                    num[Constants.ProjectTasks.Progress] = new decimal(100);
                                    num[Constants.Status.StatusCode] = new OptionSetValue(963850001);
                                    service.Update(num);
                                    tracer.Trace($"Task for Event : {Events.IR_BUDGET_START.ToString()} Successfully Updated.");
                                }

                                EntityCollection budget_approval_EntityCollection = CommonMethods.RetrieveOpenProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectTemplate), (int)Events.IR_BUDGET_APPROVAL, "5");
                                foreach (Entity e in budget_approval_EntityCollection.Entities)
                                {
                                    Entity num = new Entity(e.LogicalName)
                                    {
                                        Id = e.Id
                                    };
                                    num[Constants.ProjectTasks.ActualStart] = localDate1;
                                    num[Constants.ProjectTasks.Progress] = new decimal(1);
                                    num[Constants.Status.StatusCode] = new OptionSetValue(963850000);
                                    service.Update(num);
                                    tracer.Trace($"Task for Event : {Events.IR_BUDGET_APPROVAL.ToString()} Successfully Updated.");
                                }
                            }
                            CommonMethods.ChangeEntityStatus(tracer, service, azureIntCallEntityReference, 0, 963850000);
                            #endregion
                            break;
                        case Events.IR_BUDGET_APPROVAL:
                            #region IR_BUDGET_APPROVAL_205
                            foreach (Entity projectEntity in projectEntityCollection.Entities)
                            {
                                EntityCollection budget_start_EntityCollection = CommonMethods.RetrieveOpenProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectTemplate), (int)Events.IR_BUDGET_START, "4");
                                foreach (Entity e in budget_start_EntityCollection.Entities)
                                {
                                    Entity num = new Entity(e.LogicalName)
                                    {
                                        Id = e.Id
                                    };
                                    if (!e.Attributes.Contains(Constants.ProjectTasks.ActualStart))
                                        num[Constants.ProjectTasks.ActualStart] = localDate1;
                                    num[Constants.ProjectTasks.ActualEnd] = localDate1;
                                    DateTime actStart = DateTime.Now;
                                    if (e.Attributes.Contains(Constants.ProjectTasks.ActualStart))
                                        actStart = CommonMethods.RetrieveLocalTimeFromUTCTime(service, timeZoneCode, e.GetAttributeValue<DateTime>(Constants.ProjectTasks.ActualStart));
                                    num[Constants.ProjectTasks.ActualDurationInMinutes] = (!e.Attributes.Contains(Constants.ProjectTasks.ActualStart)) ? 0 : (((localDate1 - actStart).TotalMinutes > 0) ? (int)Math.Floor((localDate1 - actStart).TotalMinutes) : 0);
                                    num[Constants.ProjectTasks.Progress] = new decimal(100);
                                    num[Constants.Status.StatusCode] = new OptionSetValue(963850001);
                                    service.Update(num);
                                    tracer.Trace($"Task for Event : {Events.IR_BUDGET_START.ToString()} Successfully Updated.");
                                }

                                EntityCollection budget_approval_EntityCollection = CommonMethods.RetrieveOpenProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectTemplate), (int)Events.IR_BUDGET_APPROVAL, "5");
                                foreach (Entity e in budget_approval_EntityCollection.Entities)
                                {
                                    Entity num = new Entity(e.LogicalName)
                                    {
                                        Id = e.Id
                                    };
                                    if (!e.Attributes.Contains(Constants.ProjectTasks.ActualStart))
                                        num[Constants.ProjectTasks.ActualStart] = localDate1;
                                    num[Constants.ProjectTasks.ActualEnd] = localDate1;
                                    DateTime actStart = DateTime.Now;
                                    if (e.Attributes.Contains(Constants.ProjectTasks.ActualStart))
                                        actStart = CommonMethods.RetrieveLocalTimeFromUTCTime(service, timeZoneCode, e.GetAttributeValue<DateTime>(Constants.ProjectTasks.ActualStart));
                                    num[Constants.ProjectTasks.ActualDurationInMinutes] = (!e.Attributes.Contains(Constants.ProjectTasks.ActualStart)) ? 0 : (((localDate1 - actStart).TotalMinutes > 0) ? (int)Math.Floor((localDate1 - actStart).TotalMinutes) : 0);
                                    num[Constants.ProjectTasks.Progress] = new decimal(100);
                                    num[Constants.Status.StatusCode] = new OptionSetValue(963850001);
                                    service.Update(num);
                                    tracer.Trace($"Task for Event : {Events.IR_BUDGET_APPROVAL.ToString()} Successfully Updated.");
                                }
                            }
                            CommonMethods.ChangeEntityStatus(tracer, service, azureIntCallEntityReference, 0, 963850000);
                            #endregion
                            break;
                        case Events.OFFER_REJECTED_OR_APPROVAL:
                            #region OFFER_REJECTED_OR_APPROVAL_206
                            foreach (Entity projectEntity in projectEntityCollection.Entities)
                            {
                                EntityCollection sch_due_deligence_EntityCollection = CommonMethods.RetrieveAllProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectTemplate), (int)Events.SCHEDULE_DUE_DILLIGENCE_INSPECTION);
                                foreach (Entity e in sch_due_deligence_EntityCollection.Entities)
                                {
                                    if (e.Attributes.Contains(Constants.Status.StatusCode) && e.GetAttributeValue<OptionSetValue>(Constants.Status.StatusCode).Value == 963850001)
                                    {
                                        //Try to find if there is an active appointment
                                        EntityCollection appointmentCollection = RetrieveScheduledAppointmentByProjectTask(service, tracer, e.ToEntityReference());
                                        tracer.Trace($"Total {appointmentCollection.Entities.Count} Open and/or Scheduled Appointment retrieved for Task {Events.SCHEDULE_DUE_DILLIGENCE_INSPECTION.ToString()}.");

                                        foreach (Entity appointmentEntity in appointmentCollection.Entities)
                                        {
                                            tracer.Trace($"Cancelling appointment with ID {appointmentEntity.Id.ToString()}.");
                                            CommonMethods.ChangeEntityStatus(tracer, service, appointmentEntity.ToEntityReference(), 2, 4);
                                            tracer.Trace($"Appointment with ID {appointmentEntity.Id.ToString()} successfully cancelled.");
                                        }

                                    }
                                }

                                //Update Project with End Date.
                                Entity pTemp = new Entity(projectEntity.LogicalName);
                                pTemp.Id = projectEntity.Id;
                                pTemp[Constants.Projects.ActualEndDate] = DateTime.Now;
                                service.Update(pTemp);
                                //Close Project as Inactive.

                                CommonMethods.ChangeEntityStatus(tracer, service, projectEntity.ToEntityReference(), 1, 192350000);
                                CommonMethods.SetUnitSyncJobFlag(tracer, service, propertyEntity.ToEntityReference(), false);
                                tracer.Trace($"Task for Event : {Events.OFFER_REJECTED_OR_APPROVAL.ToString()} Successfully Completed.");

                            }
                            #endregion
                            CommonMethods.ChangeEntityStatus(tracer, service, azureIntCallEntityReference, 0, 963850000);
                            break;
                        case Events.CLOSE_ESCROW:
                            #region CLOSE_ESCROW_208
                            foreach (Entity projectEntity in projectEntityCollection.Entities)
                            {
                                EntityCollection prop_acquired_EntityCollection = CommonMethods.RetrieveOpenProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectTemplate), (int)Events.CLOSE_ESCROW, "8");
                                foreach (Entity e in prop_acquired_EntityCollection.Entities)
                                {
                                    Entity num = new Entity(e.LogicalName)
                                    {
                                        Id = e.Id
                                    };

                                    if (!e.Attributes.Contains(Constants.ProjectTasks.ActualStart))
                                        num[Constants.ProjectTasks.ActualStart] = localDate1;

                                    num[Constants.ProjectTasks.ActualEnd] = localDate1;
                                    DateTime actStart = DateTime.Now;
                                    if (e.Attributes.Contains(Constants.ProjectTasks.ActualStart))
                                        actStart = CommonMethods.RetrieveLocalTimeFromUTCTime(service, timeZoneCode, e.GetAttributeValue<DateTime>(Constants.ProjectTasks.ActualStart));
                                    num[Constants.ProjectTasks.ActualDurationInMinutes] = (!e.Attributes.Contains(Constants.ProjectTasks.ActualStart)) ? 0 : (((localDate1 - actStart).TotalMinutes > 0) ? (int)Math.Floor((localDate1 - actStart).TotalMinutes) : 0);
                                    num[Constants.ProjectTasks.Progress] = new decimal(100);
                                    num[Constants.Status.StatusCode] = new OptionSetValue(963850001);
                                    service.Update(num);
                                    tracer.Trace($"Task for Event : {Events.CLOSE_ESCROW.ToString()} Successfully Updated.");
                                }

                                /*
                                EntityCollection offer_acc_or_rej_EntityCollection = CommonMethods.RetrieveOpenProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectTemplate), (int)Events.OFFER_REJECTED_OR_APPROVAL, "6");
                                foreach (Entity e in offer_acc_or_rej_EntityCollection.Entities)
                                {
                                    Entity num = new Entity(e.LogicalName)
                                    {
                                        Id = e.Id
                                    };
                                    num[Constants.ProjectTasks.ActualEnd] = localDate1;
                                    DateTime actStart = DateTime.Now;
                                    if (e.Attributes.Contains(Constants.ProjectTasks.ActualStart))
                                        actStart = CommonMethods.RetrieveLocalTimeFromUTCTime(service, timeZoneCode, e.GetAttributeValue<DateTime>(Constants.ProjectTasks.ActualStart));
                                    num[Constants.ProjectTasks.ActualDurationInMinutes] = (!e.Attributes.Contains(Constants.ProjectTasks.ActualStart)) ? 0 : (((localDate1 - actStart).TotalMinutes > 0) ? (int)Math.Floor((localDate1 - actStart).TotalMinutes) : 0);
                                    num[Constants.ProjectTasks.Progress] = new decimal(100);
                                    num[Constants.Status.StatusCode] = new OptionSetValue(963850001);
                                    service.Update(num);
                                    tracer.Trace($"Task for Event : {Events.OFFER_REJECTED_OR_APPROVAL.ToString()} Successfully Updated.");
                                }
                                */

                                EntityCollection job_contract_sub_yardi_EntityCollection = CommonMethods.RetrieveOpenProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectTemplate), (int)Events.IR_JOB_AND_CONTRACTS_SUBMITTED_TO_YARDI, "9");
                                foreach (Entity e in job_contract_sub_yardi_EntityCollection.Entities)
                                {
                                    Entity num = new Entity(e.LogicalName)
                                    {
                                        Id = e.Id
                                    };
                                    num[Constants.ProjectTasks.ActualStart] = localDate1;
                                    num[Constants.ProjectTasks.Progress] = new decimal(1);
                                    num[Constants.Status.StatusCode] = new OptionSetValue(963850000);
                                    service.Update(num);
                                    tracer.Trace($"Task for Event : {Events.IR_JOB_AND_CONTRACTS_SUBMITTED_TO_YARDI.ToString()} Successfully Updated.");
                                }
                                localDate1 = localDate1.ChangeTime(6, 0, 0, 0);
                                PropertyAcquiredScheduledDateChange(tracer, service, projectEntity, localDate1);
                            }
                            CommonMethods.ChangeEntityStatus(tracer, service, azureIntCallEntityReference, 0, 963850000);
                            #endregion
                            break;
                        case Events.IR_VENDOR_SAYS_JOBS_COMPLETE:
                            #region IR_VENDOR_SAYS_JOBS_COMPLETE_214
                            tracer.Trace($"Processing  : {Events.IR_VENDOR_SAYS_JOBS_COMPLETE.ToString()} Event.");
                            tracer.Trace($"local Date 1 : {date1}");
                            foreach (Entity projectEntity in projectEntityCollection.Entities)
                            {
                                EntityCollection qc_inspection_EntityCollection = CommonMethods.RetrieveOpenProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectTemplate), 215, "13");
                                foreach (Entity e in qc_inspection_EntityCollection.Entities)
                                {
                                    Entity num = new Entity(e.LogicalName)
                                    {
                                        Id = e.Id
                                    };
                                    num[Constants.ProjectTasks.ScheduledStart] = date1;
                                    num[Constants.ProjectTasks.ScheduledEnd] = date1.AddDays(1);
                                    service.Update(num);
                                    tracer.Trace("Task for Event : IR_VENDOR_SAYS_JOBS_COMPLETE Successfully Updated.");
                                }
                            }
                            CommonMethods.ChangeEntityStatus(tracer, service, azureIntCallEntityReference, 0, 963850000);
                            #endregion
                            break;
                        case Events.IR_SCHEDULED_CLOSING_DATE:
                            #region IR_SCHEDULED_CLOSING_DATE_223
                            List<Entity> TaskIndentifierEntityList = CommonMethods.RetrieveActivtTaskIdentifier(tracer, service);
                            foreach (Entity projectEntity in projectEntityCollection.Entities)
                            {
                                ChangeScheduledDateOnSchClosingDate(tracer, service, projectEntity, TaskIndentifierEntityList, localDate1, timeZoneCode);
                            }
                            CommonMethods.ChangeEntityStatus(tracer, service, azureIntCallEntityReference, 0, 963850000);
                            #endregion
                            break;
                        case Events.IR_DUE_DILLIGENCE_DEADLINE:
                            #region IR_DUE_DILLIGENCE_DEADLINE_224
                            foreach (Entity projectEntity in projectEntityCollection.Entities)
                            {
                                EntityCollection budget_approval_EntityCollection = CommonMethods.RetrieveOpenProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectTemplate), (int)Events.IR_BUDGET_APPROVAL, "5");
                                foreach (Entity e in budget_approval_EntityCollection.Entities)
                                {
                                    Entity num = new Entity(e.LogicalName)
                                    {
                                        Id = e.Id
                                    };
                                    num[Constants.ProjectTasks.ScheduledStart] = localDate1.AddDays(-2);
                                    num[Constants.ProjectTasks.ScheduledEnd] = localDate1.AddDays(-1);
                                    service.Update(num);
                                    tracer.Trace($"Task for Event : {Events.IR_BUDGET_APPROVAL.ToString()} Successfully Updated.");
                                }

                                EntityCollection offer_acc_or_rej_EntityCollection = CommonMethods.RetrieveOpenProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectTemplate), (int)Events.OFFER_REJECTED_OR_APPROVAL, "6");
                                foreach (Entity e in offer_acc_or_rej_EntityCollection.Entities)
                                {
                                    Entity num = new Entity(e.LogicalName)
                                    {
                                        Id = e.Id
                                    };
                                    num[Constants.ProjectTasks.ScheduledStart] = localDate1.AddDays(-1);
                                    num[Constants.ProjectTasks.ScheduledEnd] = localDate1;
                                    service.Update(num);
                                    tracer.Trace($"Task for Event : {Events.OFFER_REJECTED_OR_APPROVAL.ToString()} Successfully Updated.");
                                }

                                EntityCollection job_assign_to_vendor_EntityCollection = CommonMethods.RetrieveOpenProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectTemplate), (int)Events.IR_JOB_ASSIGNMENT_TO_VENDORS_IN_CONTRACT_CREATOR, "7");
                                foreach (Entity e in job_assign_to_vendor_EntityCollection.Entities)
                                {
                                    Entity num = new Entity(e.LogicalName)
                                    {
                                        Id = e.Id
                                    };
                                    num[Constants.ProjectTasks.ScheduledStart] = localDate1;
                                    service.Update(num);
                                    tracer.Trace($"Task for Event : {Events.IR_JOB_ASSIGNMENT_TO_VENDORS_IN_CONTRACT_CREATOR.ToString()} Successfully Updated.");
                                }
                            }
                            CommonMethods.ChangeEntityStatus(tracer, service, azureIntCallEntityReference, 0, 963850000);
                            #endregion
                            break;
                        case Events.IR_DD_INSPECTION_APPROVED:
                            #region IR_DD_INSPECTION_APPROVED_225
                            foreach (Entity projectEntity in projectEntityCollection.Entities)
                            {
                                EntityCollection offer_acc_or_rej_EntityCollection = CommonMethods.RetrieveOpenProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectTemplate), (int)Events.OFFER_REJECTED_OR_APPROVAL, "6");
                                foreach (Entity e in offer_acc_or_rej_EntityCollection.Entities)
                                {
                                    Entity num = new Entity(e.LogicalName)
                                    {
                                        Id = e.Id
                                    };
                                    num[Constants.ProjectTasks.ScheduledStart] = localDate1.AddDays(-1);
                                    num[Constants.ProjectTasks.ScheduledEnd] = localDate1;
                                    DateTime actStart = localDate1;
                                    if (e.Attributes.Contains(Constants.ProjectTasks.ActualStart))
                                        actStart = CommonMethods.RetrieveLocalTimeFromUTCTime(service, timeZoneCode, e.GetAttributeValue<DateTime>(Constants.ProjectTasks.ActualStart));
                                    num[Constants.ProjectTasks.ActualDurationInMinutes] = (!e.Attributes.Contains(Constants.ProjectTasks.ActualStart)) ? 0 : (((localDate1 - actStart).TotalMinutes > 0) ? (int)Math.Floor((localDate1 - actStart).TotalMinutes) : 0);
                                    num[Constants.ProjectTasks.Progress] = new decimal(100);
                                    num[Constants.Status.StatusCode] = new OptionSetValue(963850001);
                                    service.Update(num);
                                    tracer.Trace($"Task for Event : {Events.OFFER_REJECTED_OR_APPROVAL.ToString()} Successfully Updated.");
                                }
                            }
                            CommonMethods.ChangeEntityStatus(tracer, service, azureIntCallEntityReference, 0, 963850000);
                            #endregion
                            break;
                        case Events.IR_CLOSING_DOCS_APPROVED:
                            #region IR_CLOSING_DOCS_APPROVED_226
                            foreach (Entity projectEntity in projectEntityCollection.Entities)
                            {
                                EntityCollection job_assign_to_vendor_EntityCollection = CommonMethods.RetrieveOpenProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectTemplate), (int)Events.IR_JOB_ASSIGNMENT_TO_VENDORS_IN_CONTRACT_CREATOR, "7");
                                foreach (Entity e in job_assign_to_vendor_EntityCollection.Entities)
                                {
                                    Entity num = new Entity(e.LogicalName)
                                    {
                                        Id = e.Id
                                    };
                                    num[Constants.ProjectTasks.ScheduledStart] = localDate1;
                                    num[Constants.ProjectTasks.ScheduledEnd] = localDate1.AddDays(1);
                                    service.Update(num);
                                    tracer.Trace($"Task for Event : {Events.IR_JOB_ASSIGNMENT_TO_VENDORS_IN_CONTRACT_CREATOR.ToString()} Successfully Updated.");
                                }
                            }
                            CommonMethods.ChangeEntityStatus(tracer, service, azureIntCallEntityReference, 0, 963850000);
                            #endregion
                            break;
                        case Events.IR_MULTI_VENDOR:
                            #region IR_MULTI_VENDOR_229
                            if (gridEvent.data.Contracts is List<Contract> && gridEvent.data.Contracts.Count > 0)
                            {
                                foreach (Entity projectEntity in projectEntityCollection.Entities)
                                {
                                    if (projectEntity.Attributes.Contains(Constants.Projects.ProjectTemplate))
                                    {
                                        Mapping mapping = (
                                            from m in settings.Mappings
                                            where m.Key.Equals(projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectTemplate).Id.ToString(), StringComparison.OrdinalIgnoreCase)
                                            select m).FirstOrDefault<Mapping>();
                                        if (mapping is Mapping)
                                        {
                                            EntityCollection jobVendorEntityCollection = new EntityCollection();
                                            if (projectEntity.Attributes.Contains(Constants.Projects.Job))
                                            {
                                                jobVendorEntityCollection = RetrieveJobVenodrsByJob(tracer, service, projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.Job));
                                            }
                                            else if (projectEntity.Attributes.Contains(Constants.Projects.RenowalkID))
                                            {
                                                jobVendorEntityCollection = CommonMethods.RetrieveJobVenodrsByRenowalkID(tracer, service, projectEntity.GetAttributeValue<string>(Constants.Projects.RenowalkID));
                                            }
                                            else if (!string.IsNullOrEmpty(gridEvent.data.RenowalkID))
                                            {
                                                jobVendorEntityCollection = CommonMethods.RetrieveJobVenodrsByRenowalkID(tracer, service, gridEvent.data.RenowalkID);
                                            }

                                            if (jobVendorEntityCollection.Entities.Count > 0)
                                            {
                                                string error = CreateVendorProjectTask(tracer, service, projectEntity, jobVendorEntityCollection, gridEvent.data.Contracts, mapping, timeZoneCode);
                                                if (string.IsNullOrEmpty(error))
                                                    CommonMethods.ChangeEntityStatus(tracer, service, azureIntCallEntityReference, 0, 963850000);
                                                else
                                                    AzureIntegrationCallAsync.UpdateAzureIntegrationCallErrorDetails(service, azureIntCallEntityReference, error, 963850001);

                                            }
                                            else
                                                AzureIntegrationCallAsync.UpdateAzureIntegrationCallErrorDetails(service, azureIntCallEntityReference, $"No Job Vendor Record found in D365 system for Job with Renowalk ID : {projectEntity.GetAttributeValue<string>(Constants.Projects.RenowalkID)}.", 963850002);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                AzureIntegrationCallAsync.UpdateAzureIntegrationCallErrorDetails(service, azureIntCallEntityReference, $"No Contracts found in Data PayLoad.", 963850002);
                            }
                            #endregion
                            break;
                            #endregion
                    }
                }

                else
                    AzureIntegrationCallAsync.UpdateAzureIntegrationCallErrorDetails(service, azureIntCallEntityReference, $"There is NO active Project for Property {gridEvent.data.PropertyID} in system. At least one Active Project is required to perform {gridEvent.data.Event.ToString()} Event.", 963850002);
            }
        }

        #region MULTI_VENDOR_METHODS

        private static string CreateVendorProjectTask(ITracingService tracer, IOrganizationService service, Entity projectEntity, EntityCollection jobVendorsEntityCollection, List<Contract> contractList, Mapping mapping, int timeZoneCode)
        {
            int cnt = 0;
            string error = string.Empty;

            int cnt_vsjs = 0, cnt_wip = 0, cnt_vsjc = 0, cnt_qci = 0;
            Entity vendorSaysJobStartedEntity = null, workInProgressEntity = null, vendorSaysJobCompleteEntity = null, qcInspectionEntity = null, jobandContractSubToYardiEntity = null;
            //Retrieve Venodr Says Job Started Task.
            EntityCollection vendor_says_job_started_EntityCollection = CommonMethods.RetrieveAllProjectTaskIncludingChildByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectTemplate),
                mapping.Name.Equals(TURNPROCESS_PROJECT_TEMPLATE) ? 11 : 210, mapping.Name.Equals(TURNPROCESS_PROJECT_TEMPLATE) ? "11" : "10");

            cnt_vsjs = vendor_says_job_started_EntityCollection.Entities.Count;
            vendorSaysJobStartedEntity = vendor_says_job_started_EntityCollection.Entities.Where(e => !e.Attributes.Contains(Constants.ProjectTasks.ParentTask)).FirstOrDefault();

            //Retrieve Work In Progress Task.
            EntityCollection work_in_progress_EntityCollection = CommonMethods.RetrieveAllProjectTaskIncludingChildByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectTemplate),
                 mapping.Name.Equals(TURNPROCESS_PROJECT_TEMPLATE) ? 12 : 211, mapping.Name.Equals(TURNPROCESS_PROJECT_TEMPLATE) ? "12" : "11");

            cnt_wip = work_in_progress_EntityCollection.Entities.Count;
            workInProgressEntity = work_in_progress_EntityCollection.Entities.Where(e => !e.Attributes.Contains(Constants.ProjectTasks.ParentTask)).FirstOrDefault();

            //Retrieve Vendor Says Job Completed Task.
            EntityCollection vendor_says_job_complete_EntityCollection = CommonMethods.RetrieveAllProjectTaskIncludingChildByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectTemplate),
                 mapping.Name.Equals(TURNPROCESS_PROJECT_TEMPLATE) ? 15 : 214, mapping.Name.Equals(TURNPROCESS_PROJECT_TEMPLATE) ? "13" : "12");

            cnt_vsjc = vendor_says_job_complete_EntityCollection.Entities.Count;
            vendorSaysJobCompleteEntity = vendor_says_job_complete_EntityCollection.Entities.Where(e => !e.Attributes.Contains(Constants.ProjectTasks.ParentTask)).FirstOrDefault();

            //Retrieve Quality Control Inspection Task.
            EntityCollection qc_inspection_EntityCollection = CommonMethods.RetrieveAllProjectTaskIncludingChildByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectTemplate),
                 mapping.Name.Equals(TURNPROCESS_PROJECT_TEMPLATE) ? 16 : 215, mapping.Name.Equals(TURNPROCESS_PROJECT_TEMPLATE) ? "14" : "13");

            cnt_qci = qc_inspection_EntityCollection.Entities.Count;
            qcInspectionEntity = qc_inspection_EntityCollection.Entities.Where(e => !e.Attributes.Contains(Constants.ProjectTasks.ParentTask)).FirstOrDefault();

            EntityCollection job_and_contracts_sub_to_yardi_EntityCollection = CommonMethods.RetrieveAllProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectTemplate),
    mapping.Name.Equals(TURNPROCESS_PROJECT_TEMPLATE) ? 10 : 209);
            foreach (Entity e in job_and_contracts_sub_to_yardi_EntityCollection.Entities)
            {
                jobandContractSubToYardiEntity = e;
                break;
            }

            if (vendorSaysJobStartedEntity != null && vendorSaysJobCompleteEntity != null && workInProgressEntity != null && qcInspectionEntity != null)
            {
                foreach (Contract contract in contractList)
                {
                    if (!string.IsNullOrEmpty(contract.Vendor_Code))
                    {
                        //Try to find Job Vendor for the same...
                        Entity jobVendorEntity = jobVendorsEntityCollection.Entities.Where(j => j.Attributes.Contains("V.po_accountcode") && j.GetAttributeValue<AliasedValue>("V.po_accountcode").Value.ToString().Trim(' ').Equals(contract.Vendor_Code, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                        if (jobVendorEntity is Entity)
                        {
                            DateTime startDate = CommonMethods.RetrieveLocalTimeFromUTCTime(service, timeZoneCode, jobVendorEntity.GetAttributeValue<DateTime>(Constants.JobVendors.StartDate));
                            startDate = startDate.ChangeTime(6, 0, 0, 0);
                            DateTime endDate = CommonMethods.RetrieveLocalTimeFromUTCTime(service, timeZoneCode, jobVendorEntity.GetAttributeValue<DateTime>(Constants.JobVendors.EndDate));
                            endDate = endDate.ChangeTime(6, 0, 0, 0);
                            Entity bookableResourceEntity = null, ProjectTeamEntity = null;

                            string bookableResourceName = jobVendorEntity.GetAttributeValue<AliasedValue>("V.name").Value.ToString();
                            tracer.Trace($"Vendor - {jobVendorEntity.GetAttributeValue<AliasedValue>("V.name").Value.ToString()} Start Date : {startDate}");
                            tracer.Trace($"Vendor - {jobVendorEntity.GetAttributeValue<AliasedValue>("V.name").Value.ToString()} End Date : {endDate}");
                            if (bookableResourceName.Length > 100)
                                bookableResourceName = bookableResourceName.Substring(0, 96) + "...";
                            bookableResourceEntity = GetOrCreateBookableResource(tracer, service, jobVendorEntity.GetAttributeValue<EntityReference>(Constants.JobVendors.VendorID), timeZoneCode, bookableResourceName);

                            if (bookableResourceEntity is Entity)
                            {
                                ProjectTeamEntity = CreateProjectTeam(tracer, service, projectEntity.ToEntityReference(), bookableResourceEntity.ToEntityReference(), jobVendorEntity.GetAttributeValue<AliasedValue>("V.name").Value.ToString());
                                if (ProjectTeamEntity is Entity)
                                {
                                    //Vendor Says Job Started...
                                    Entity existingVendorSaysJobStartedEntity = vendor_says_job_started_EntityCollection.Entities.Where(e => e.Attributes.Contains(Constants.ProjectTasks.ContractID) && e.GetAttributeValue<string>(Constants.ProjectTasks.ContractID).Equals(contract.Contract_Code, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                                    if (existingVendorSaysJobStartedEntity == null)
                                    {
                                        Entity tmp = CommonMethods.CloneEntitySandbox(vendorSaysJobStartedEntity);
                                        tmp.Id = CreateProjectTask(tracer, service, tmp, startDate, startDate.ChangeTime(23, 59, 0, 0), vendorSaysJobStartedEntity.GetAttributeValue<string>(Constants.ProjectTasks.WBSID) + "." + cnt_vsjs.ToString()
                                            , 0, contract.Contract_Code, vendorSaysJobStartedEntity.ToEntityReference(), ProjectTeamEntity.ToEntityReference());

                                        CreateProjectTaskDepenecyRecord(tracer, service, projectEntity.ToEntityReference(), jobandContractSubToYardiEntity.ToEntityReference(), tmp.ToEntityReference());
                                        cnt_vsjs++;
                                    }
                                    else
                                        error += $"Project Task Vendor Says Job Started already Exist. WBS ID : {existingVendorSaysJobStartedEntity.GetAttributeValue<string>(Constants.ProjectTasks.WBSID)}, Vendor Code : {contract.Vendor_Code}, Contract Code : {contract.Contract_Code}  {Environment.NewLine}";

                                    //Work In Progress
                                    Entity existingWorkInProgressEntity = work_in_progress_EntityCollection.Entities.Where(e => e.Attributes.Contains(Constants.ProjectTasks.ContractID) && e.GetAttributeValue<string>(Constants.ProjectTasks.ContractID).Equals(contract.Contract_Code, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                                    if (existingWorkInProgressEntity == null)
                                    {
                                            Entity tmp = CommonMethods.CloneEntitySandbox(workInProgressEntity);
                                            tmp.Id = CreateProjectTask(tracer, service, tmp, startDate, endDate, workInProgressEntity.GetAttributeValue<string>(Constants.ProjectTasks.WBSID) + "." + cnt_wip.ToString()
                                                , 0, contract.Contract_Code, workInProgressEntity.ToEntityReference(), ProjectTeamEntity.ToEntityReference());

                                            CreateProjectTaskDepenecyRecord(tracer, service, projectEntity.ToEntityReference(), vendorSaysJobStartedEntity.ToEntityReference(), tmp.ToEntityReference());
                                        cnt_wip++;
                                    }
                                    else
                                        error += $"Project Task Work In Progress already Exist. WBS ID : {existingWorkInProgressEntity.GetAttributeValue<string>(Constants.ProjectTasks.WBSID)}, Vendor Code : {contract.Vendor_Code}, Contract Code : {contract.Contract_Code}  {Environment.NewLine}";

                                    //Vendor Says Job Completed.
                                    Entity existingVendorSaysJobCompletedEntity = vendor_says_job_complete_EntityCollection.Entities.Where(e => e.Attributes.Contains(Constants.ProjectTasks.ContractID) && e.GetAttributeValue<string>(Constants.ProjectTasks.ContractID).Equals(contract.Contract_Code, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                                    if (existingVendorSaysJobCompletedEntity == null)
                                    {
                                            Entity tmp = CommonMethods.CloneEntitySandbox(vendorSaysJobCompleteEntity);
                                            tmp.Id = CreateProjectTask(tracer, service, tmp, endDate, endDate.ChangeTime(23, 59, 0, 0), vendorSaysJobCompleteEntity.GetAttributeValue<string>(Constants.ProjectTasks.WBSID) + "." + cnt_vsjc.ToString()
                                                , 0, contract.Contract_Code, vendorSaysJobCompleteEntity.ToEntityReference(), ProjectTeamEntity.ToEntityReference());

                                        CreateProjectTaskDepenecyRecord(tracer, service, projectEntity.ToEntityReference(), workInProgressEntity.ToEntityReference(), tmp.ToEntityReference());
                                        cnt_vsjc++;
                                    }
                                    else
                                        error += $"Project Task Vendors Says Job Completed already Exist. WBS ID : {existingVendorSaysJobCompletedEntity.GetAttributeValue<string>(Constants.ProjectTasks.WBSID)}, Vendor Code : {contract.Vendor_Code}, Contract Code : {contract.Contract_Code}  {Environment.NewLine}";

                                    //QC Inspection
                                    Entity existingQCInspectionEntity = qc_inspection_EntityCollection.Entities.Where(e => e.Attributes.Contains(Constants.ProjectTasks.ContractID) && e.GetAttributeValue<string>(Constants.ProjectTasks.ContractID).Equals(contract.Contract_Code, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                                    if (existingQCInspectionEntity == null)
                                    {
                                        Entity tmp = CommonMethods.CloneEntitySandbox(qcInspectionEntity);
                                        tmp.Id = CreateProjectTask(tracer, service, tmp, endDate, endDate.AddDays(1).ChangeTime(23, 59, 0, 0), qcInspectionEntity.GetAttributeValue<string>(Constants.ProjectTasks.WBSID) + "." + cnt_qci.ToString()
                                            , 0, contract.Contract_Code, qcInspectionEntity.ToEntityReference(), ProjectTeamEntity.ToEntityReference());

                                        CreateProjectTaskDepenecyRecord(tracer, service, projectEntity.ToEntityReference(), vendorSaysJobCompleteEntity.ToEntityReference(), tmp.ToEntityReference());
                                        cnt_qci++;
                                    }
                                    else
                                        error += $"Project Task QC Inspection already Exist. WBS ID : {existingQCInspectionEntity.GetAttributeValue<string>(Constants.ProjectTasks.WBSID)} Vendor Code : {contract.Vendor_Code}, Contract Code : {contract.Contract_Code} {Environment.NewLine}";
                                }

                            }

                        }
                        else
                            error += $"Job Vendor Not found for Vendor Code : {contract.Vendor_Code}. {Environment.NewLine}";
                    }
                    else
                    {
                        error += $"Vendor Code is missing in one of the Contract Line Item. {Environment.NewLine}";
                    }
                }
            }

            //Entity vendorSaysJobStartedEntity = null, workInProgressEntity = null, vendorSaysJobCompleteEntity = null, qcInspectionEntity = null, jobandContractSubToYardiEntity = null;
            return error;
        }

        private static Guid CreateProjectTask(ITracingService tracer, IOrganizationService service, Entity num, DateTime startDate, DateTime endDate, string WBSID, decimal progress, string contractCode,
            EntityReference parentTaskEntityReference, EntityReference projectTeamEntityReference)
        {
            //num[Constants.ProjectTasks.Subject] = workInProgressEntity.GetAttributeValue<string>(Constants.ProjectTasks.Subject) + " " + vendorName;
            num[Constants.ProjectTasks.ScheduledStart] = startDate;
            num[Constants.ProjectTasks.ScheduledEnd] = endDate;
            num[Constants.ProjectTasks.WBSID] = WBSID;
            num[Constants.ProjectTasks.Progress] = progress;
            num[Constants.ProjectTasks.ContractID] = contractCode;
            num[Constants.Status.StatusCode] = new OptionSetValue(1);
            num[Constants.ProjectTasks.ParentTask] = parentTaskEntityReference;
            num[Constants.ProjectTasks.ScheduledDurationMinutes] = 1440;
            num[Constants.ProjectTasks.AssignedTeamMembers] = projectTeamEntityReference;

            return service.Create(num);
        }

        private static void CreateProjectTaskDepenecyRecord(ITracingService tracer, IOrganizationService service, EntityReference projectEntityReference, EntityReference predecessorTaskEntityReference, EntityReference successorTaskEntityReference)
        {
            Entity ProjectTaskDependencyEntity = new Entity(Constants.ProjectTasksDependencies.LogicalName);
            ProjectTaskDependencyEntity[Constants.ProjectTasksDependencies.LinkType] = new OptionSetValue(192350000);
            ProjectTaskDependencyEntity[Constants.ProjectTasksDependencies.PredecessorTask] = predecessorTaskEntityReference;
            ProjectTaskDependencyEntity[Constants.ProjectTasksDependencies.SuccessorTask] = successorTaskEntityReference;
            ProjectTaskDependencyEntity[Constants.ProjectTasksDependencies.Project] = projectEntityReference;

            ProjectTaskDependencyEntity.Id = service.Create(ProjectTaskDependencyEntity);
        }

        private static Entity GetOrCreateBookableResource(ITracingService tracer, IOrganizationService service, EntityReference vendorEntityReference, int timeZone, string Name)
        {
            QueryExpression Query = new QueryExpression
            {
                EntityName = Constants.BookableResources.LogicalName,
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    FilterOperator = LogicalOperator.And,
                    Conditions =
                    {
                        new ConditionExpression
                        {
                            AttributeName = Constants.BookableResources.AccountID,
                            Operator = ConditionOperator.Equal,
                            Values = { vendorEntityReference.Id }
                        },
                        new ConditionExpression
                        {
                            AttributeName = Constants.Status.StateCode,
                            Operator = ConditionOperator.Equal,
                            Values = { 0 }
                        }
                    }
                },
                TopCount = 1
            };

            EntityCollection bookableResourceEntityCollection = service.RetrieveMultiple(Query);
            if (bookableResourceEntityCollection.Entities.Count > 0)
                return bookableResourceEntityCollection.Entities[0];
            else
            {

                Entity bookableResourceEntity = new Entity(Constants.BookableResources.LogicalName);
                bookableResourceEntity[Constants.BookableResources.AccountID] = vendorEntityReference;
                bookableResourceEntity[Constants.BookableResources.ResourceType] = new OptionSetValue(5);
                bookableResourceEntity[Constants.BookableResources.TimeZone] = timeZone;
                bookableResourceEntity[Constants.BookableResources.Name] = Name;

                bookableResourceEntity.Id = service.Create(bookableResourceEntity);

                return bookableResourceEntity;
            }
        }

        private static Entity CreateProjectTeam(ITracingService tracer, IOrganizationService service, EntityReference projectEntityReference, EntityReference bookableResourceReference, string projecTeamName)
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
                        },
                        new ConditionExpression
                        {
                            AttributeName = Constants.ProjectTeams.BookableResource,
                            Operator = ConditionOperator.Equal,
                            Values = { bookableResourceReference.Id }
                        }
                    }
                },
                TopCount = 1
            };

            EntityCollection projectTeamEntityCollection = service.RetrieveMultiple(Query);
            if (projectTeamEntityCollection.Entities.Count > 0)
                return projectTeamEntityCollection.Entities[0];
            else
            {
                Entity ProjectTeamEntity = new Entity(Constants.ProjectTeams.LogicalName);
                ProjectTeamEntity[Constants.ProjectTeams.Project] = projectEntityReference;
                ProjectTeamEntity[Constants.ProjectTeams.BookableResource] = bookableResourceReference;
                ProjectTeamEntity[Constants.ProjectTeams.Name] = projecTeamName;

                ProjectTeamEntity.Id = service.Create(ProjectTeamEntity);

                return ProjectTeamEntity;
            }
        }


        public static EntityCollection RetrieveJobVenodrsByJob(ITracingService tracer, IOrganizationService service, EntityReference jobEntityReference)
        {
            QueryExpression Query = new QueryExpression
            {
                EntityName = Constants.JobVendors.LogicalName,
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    FilterOperator = LogicalOperator.And,
                    Conditions =
                    {
                        new ConditionExpression
                        {
                            AttributeName = Constants.JobVendors.JobID,
                            Operator = ConditionOperator.Equal,
                            Values = { jobEntityReference.Id }
                        },
                        new ConditionExpression
                        {
                            AttributeName = Constants.JobVendors.StartDate,
                            Operator = ConditionOperator.NotNull
                        },
                        new ConditionExpression
                        {
                            AttributeName = Constants.JobVendors.EndDate,
                            Operator = ConditionOperator.NotNull
                        },
                        new ConditionExpression
                        {
                            AttributeName = Constants.JobVendors.VendorID,
                            Operator = ConditionOperator.NotNull
                        }
                    }
                },
                TopCount = 1000,
                Orders = { new OrderExpression(Constants.JobVendors.StartDate, OrderType.Ascending) }
            };

            LinkEntity vendorlinkEntity = new LinkEntity(Constants.JobVendors.LogicalName, Constants.Vendors.LogicalName, Constants.JobVendors.VendorID, Constants.Vendors.PrimaryKey, JoinOperator.Inner)
            {
                Columns = new ColumnSet(Constants.Vendors.PrimaryKey, Constants.Vendors.AccountCode, Constants.Vendors.Name),
                EntityAlias = "V"
            };
            vendorlinkEntity.LinkCriteria.AddCondition(new ConditionExpression(Constants.Vendors.AccountCode, ConditionOperator.NotNull));
            Query.LinkEntities.Add(vendorlinkEntity);

            LinkEntity joblinkEntity = new LinkEntity(Constants.JobVendors.LogicalName, Constants.Jobs.LogicalName, Constants.JobVendors.JobID, Constants.Jobs.PrimaryKey, JoinOperator.Inner)
            {
                Columns = new ColumnSet(Constants.Jobs.PrimaryKey, Constants.Jobs.RenowalkID),
                EntityAlias = "J"
            };
            Query.LinkEntities.Add(joblinkEntity);

            return service.RetrieveMultiple(Query);
        }
        #endregion

        private static void AssignProjectManagerAsTaskOwner(ITracingService tracer, IOrganizationService service, Entity projectEntity)
        {
            tracer.Trace($"Retrieving All Task for Project.");
            if (projectEntity.Attributes.Contains(Constants.Projects.ProjectManager))
            {
                EntityCollection projectTaskEntityCollection = RetrieveAllProjectTaskByProject(tracer, service, projectEntity.ToEntityReference());
                foreach (Entity projectTaskEntity in projectTaskEntityCollection.Entities)
                {
                    Entity tmpPT = new Entity(projectTaskEntity.LogicalName);
                    tmpPT.Id = projectTaskEntity.Id;
                    tmpPT[Constants.ProjectTasks.Owner] = projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectManager);

                    service.Update(tmpPT);
                }
            }
        }

        public static EntityCollection RetrieveAllProjectTaskByProject(ITracingService tracer, IOrganizationService service, EntityReference projectEntityRefernce)
        {
            LinkEntity linkEntity = new LinkEntity(Constants.ProjectTasks.LogicalName, Constants.TaskIdentifiers.LogicalName, Constants.ProjectTasks.TaskIdentifier, Constants.TaskIdentifiers.PrimaryKey, JoinOperator.Inner)
            {
                Columns = new ColumnSet(true),
                EntityAlias = "TI"
            };
            linkEntity.LinkCriteria.AddCondition(new ConditionExpression(Constants.TaskIdentifiers.IdentifierNumber, ConditionOperator.NotIn, new int[] { 18, 19, 20, 21, 217, 218, 219, 220 }));

            QueryExpression Query = new QueryExpression
            {
                EntityName = Constants.ProjectTasks.LogicalName,
                ColumnSet = new ColumnSet(true),
                LinkEntities = { linkEntity },
                Criteria = new FilterExpression
                {
                    FilterOperator = LogicalOperator.And,
                    Conditions =
                    {
                        new ConditionExpression
                        {
                            AttributeName = Constants.ProjectTasks.Project,
                            Operator = ConditionOperator.Equal,
                            Values = { projectEntityRefernce.Id }
                        }
                    }
                },
                TopCount = 100
            };

            return service.RetrieveMultiple(Query);
        }

        private static EntityCollection RetrieveScheduledAppointmentByProjectTask(IOrganizationService service, ITracingService tracer, EntityReference projectTaskEntityReference)
        {
            tracer.Trace($"Retrieving Scheduled Appointment By ProjectTask.");
            QueryExpression Query = new QueryExpression
            {
                EntityName = Constants.Appointments.LogicalName,
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    FilterOperator = LogicalOperator.And,
                    Conditions =
                    {
                        new ConditionExpression
                        {
                            AttributeName = Constants.Appointments.Regarding,
                            Operator = ConditionOperator.Equal,
                            Values = { projectTaskEntityReference.Id }
                        },
                        new ConditionExpression
                        {
                            AttributeName = Constants.Status.StateCode,
                            Operator = ConditionOperator.In,
                            Values = { 3,0 }
                        }
                    }
                },
                TopCount = 100
            };

            return service.RetrieveMultiple(Query);
        }

        private static void UpdateAzureIntegrationCallErrorDetails(IOrganizationService service, EntityReference azureIntCallEntityReference, string errorMessage, int StatusCode = 963850002)
        {
            Entity azureIntCallEntity = new Entity(azureIntCallEntityReference.LogicalName);
            azureIntCallEntity.Id = azureIntCallEntityReference.Id;
            azureIntCallEntity[Constants.AzureIntegrationCalls.ErrorDetails] = errorMessage;
            azureIntCallEntity[Constants.AzureIntegrationCalls.StatusCode] = new OptionSetValue(StatusCode);

            service.Update(azureIntCallEntity);
        }

        #region SCH_START_AND_END_DATE_OPERATIONS
        private static void PerformScheduledDateMoveOut(ITracingService tracer, IOrganizationService service, Entity projectEntity, int timeZoneCode, DateTime localDate1)
        {
            List<Entity> TaskIndentifierEntityList = CommonMethods.RetrieveActivtTaskIdentifier(tracer, service);

            EntityCollection projectTaskEntityCollection = CommonMethods.RetrieveAllOpenProjectTaskByProject(tracer, service, projectEntity.ToEntityReference());

            foreach (Entity projectTaskEntity in projectTaskEntityCollection.Entities)
            {
                if (projectTaskEntity.Attributes.Contains(Constants.ProjectTasks.TaskIdentifier))
                {
                    Entity taskIdentifierEntity = TaskIndentifierEntityList.Where(e => e.Id.Equals(projectTaskEntity.GetAttributeValue<EntityReference>(Constants.ProjectTasks.TaskIdentifier).Id)).FirstOrDefault();
                    if (taskIdentifierEntity is Entity && taskIdentifierEntity.Attributes.Contains(Constants.TaskIdentifiers.WBSID))
                    {
                        tracer.Trace($"Task Identifier Found ID : {projectTaskEntity.GetAttributeValue<EntityReference>(Constants.ProjectTasks.TaskIdentifier).Id} Name : {projectTaskEntity.GetAttributeValue<EntityReference>(Constants.ProjectTasks.TaskIdentifier).Name}");
                        tracer.Trace($"Task Identifier WBS ID : {taskIdentifierEntity.GetAttributeValue<string>(Constants.TaskIdentifiers.WBSID)}");
                        DateTime SchStartDate = CommonMethods.RetrieveLocalTimeFromUTCTime(service, timeZoneCode, projectTaskEntity.GetAttributeValue<DateTime>(Constants.ProjectTasks.ScheduledStart));
                        DateTime dueDate;
                        tracer.Trace($"Scheduled Start Date : {SchStartDate}");
                        tracer.Trace($"Move Out Date : {localDate1}");
                        bool requiredUpdate = false;
                        Entity tmpPrjTskEntity = new Entity(projectTaskEntity.LogicalName);
                        tmpPrjTskEntity.Id = projectTaskEntity.Id;

                        switch (taskIdentifierEntity.GetAttributeValue<string>(Constants.TaskIdentifiers.WBSID))
                        {
                            case "2":           //IR : ASSIGN_PROJECT_MANAGER
                                dueDate = (localDate1.AddDays(-37) > DateTime.Now) ? ((localDate1.AddDays(-37) > SchStartDate) ? localDate1.AddDays(-37) : SchStartDate.AddHours(24)) : ((SchStartDate > DateTime.Now) ? SchStartDate.AddHours(24) : DateTime.Now);
                                tracer.Trace($"Due Date : {dueDate}");
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledEnd] = dueDate;
                                requiredUpdate = true;
                                break;
                            case "3":           //CORPORATE RENEWALS
                                dueDate = localDate1;
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledEnd] = (dueDate > SchStartDate) ? dueDate : SchStartDate.AddHours(24);
                                requiredUpdate = true;
                                break;
                            case "4":           //MARKET SCHEDULES PRE-MOVE-OUT
                                dueDate = (localDate1.AddDays(-30) > DateTime.Now) ? ((localDate1.AddDays(-30) > SchStartDate) ? localDate1.AddDays(-30) : SchStartDate.AddHours(24)) : ((SchStartDate > DateTime.Now) ? SchStartDate.AddHours(24) : DateTime.Now.AddHours(24));
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledEnd] = dueDate;
                                requiredUpdate = true;
                                break;
                            case "5":           //PRE-MOVE-OUT INSPECTION
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledStart] = localDate1.AddDays(-30);
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledEnd] = localDate1.AddHours(-1);
                                requiredUpdate = true;
                                break;
                            case "6":           //MOVE-OUT INSPECTION
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledStart] = localDate1;
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledEnd] = localDate1;
                                requiredUpdate = true;
                                break;
                            case "7":           //BUDGET START
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledStart] = localDate1;
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledEnd] = localDate1.AddHours(24);
                                requiredUpdate = true;
                                break;
                            case "8":           //BUDGET APPROVAL
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledStart] = localDate1.AddSeconds(1);
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledEnd] = localDate1.AddHours(24);
                                requiredUpdate = true;
                                break;
                            case "9":           //JOB ASSIGNMENT TO VENDOR(S) IN CONTRACT CREATOR
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledStart] = localDate1.AddSeconds(2);
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledEnd] = localDate1.AddHours(24);
                                requiredUpdate = true;
                                break;
                            case "10":          //JOB AND CONTRACT(S) SUBMITTED TO YARDI
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledStart] = localDate1.AddSeconds(3);
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledEnd] = localDate1.AddHours(24);
                                requiredUpdate = true;
                                break;
                            case "11":          //VENDOR(S) SAYS JOB STARTED
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledStart] = localDate1.AddDays(1);
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledEnd] = localDate1.AddDays(2);
                                requiredUpdate = true;
                                break;
                            case "12":          //WORK IN PROGRESS
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledStart] = localDate1.AddDays(2);
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledEnd] = localDate1.AddDays(3);
                                requiredUpdate = true;
                                break;
                            case "13":          //VENDOR SAYS JOB?S COMPLETE
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledStart] = localDate1.AddDays(3);
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledEnd] = localDate1.AddDays(4);
                                requiredUpdate = true;
                                break;
                            case "14":          //QUALITY CONTROL INSPECTION
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledStart] = localDate1.AddDays(4);
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledEnd] = localDate1.AddDays(5);
                                requiredUpdate = true;
                                break;
                            case "15":          //JOB COMPLETED
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledStart] = localDate1.AddDays(5);
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledEnd] = localDate1.AddDays(6);
                                requiredUpdate = true;
                                break;
                            case "16":          //HERO SHOT PICTURE
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledEnd] = localDate1.AddDays(6);
                                requiredUpdate = true;
                                break;
                            case "17":          //MARKETING INSPECTION
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledStart] = localDate1.AddDays(6);
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledEnd] = localDate1.AddDays(7);
                                requiredUpdate = true;
                                break;
                            case "18":          //BI-WEEKLY INSPECTION
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledStart] = localDate1.AddDays(19);
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledEnd] = localDate1.AddDays(20);
                                requiredUpdate = true;
                                break;
                            case "19":          //MOVE-IN INSPECTION
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledStart] = localDate1.AddDays(44);
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledEnd] = localDate1.AddDays(45);
                                requiredUpdate = true;
                                break;
                        }
                        if (requiredUpdate)
                            service.Update(tmpPrjTskEntity);

                    }
                    else
                        tracer.Trace($"Either Task Identifier Not Found or WBS ID is missing in Task Identifier Record. ID : {projectTaskEntity.GetAttributeValue<EntityReference>(Constants.ProjectTasks.TaskIdentifier).Id} Name : {projectTaskEntity.GetAttributeValue<EntityReference>(Constants.ProjectTasks.TaskIdentifier).Name}");

                }
                else
                    tracer.Trace($"Project Task does not have Task Identifier.");

            }
        }

        private static void ChangeScheduledDateOnSchClosingDate(ITracingService tracer, IOrganizationService service, Entity projectEntity, List<Entity> TaskIndentifierEntityList, DateTime localDate1, int timeZoneCode)
        {

            EntityCollection projectTaskEntityCollection = CommonMethods.RetrieveAllOpenProjectTaskByProject(tracer, service, projectEntity.ToEntityReference());
            foreach (Entity projectTaskEntity in projectTaskEntityCollection.Entities)
            {
                if (projectTaskEntity.Attributes.Contains(Constants.ProjectTasks.TaskIdentifier))
                {

                    Entity taskIdentifierEntity = TaskIndentifierEntityList.Where(e => e.Id.Equals(projectTaskEntity.GetAttributeValue<EntityReference>(Constants.ProjectTasks.TaskIdentifier).Id)).FirstOrDefault();
                    if (taskIdentifierEntity is Entity && taskIdentifierEntity.Attributes.Contains(Constants.TaskIdentifiers.WBSID))
                    {
                        DateTime SchStartDate = CommonMethods.RetrieveLocalTimeFromUTCTime(service, timeZoneCode, projectTaskEntity.GetAttributeValue<DateTime>(Constants.ProjectTasks.ScheduledStart));
                        Entity tmpPrjTskEntity = new Entity(projectTaskEntity.LogicalName);
                        tmpPrjTskEntity.Id = projectTaskEntity.Id;
                        bool requiredUpdate = false;
                        switch (taskIdentifierEntity.GetAttributeValue<string>(Constants.TaskIdentifiers.WBSID))
                        {
                            case "7":           //IR : JOB_ASSIGNMENT_TO_VENDORS_IN_CONTRACT_CREATOR
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledEnd] = (localDate1.AddDays(-3) > SchStartDate) ? localDate1.AddDays(-3) : SchStartDate.AddDays(6);
                                requiredUpdate = true;
                                break;
                            case "8":           //IR : CLOSE_ESCROW
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledStart] = localDate1;
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledEnd] = localDate1;
                                requiredUpdate = true;
                                break;
                            case "9":           //IR : JOB_AND_CONTRACTS_SUBMITTED_TO_YARDI
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledStart] = localDate1;
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledEnd] = localDate1.AddDays(1);
                                requiredUpdate = true;
                                break;
                            case "10":           //IR : VENDORS_SAYS_JOB_STARTED
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledStart] = localDate1.AddDays(1);
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledEnd] = localDate1.AddDays(1);
                                requiredUpdate = true;
                                break;
                            case "11":           //IR : WORK_IN_PROGRESS
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledStart] = localDate1.AddDays(1);
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledEnd] = localDate1.AddDays(2);
                                requiredUpdate = true;
                                break;
                            case "12":           //IR : VENDOR_SAYS_JOBS_COMPLETE
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledStart] = localDate1.AddDays(2);
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledEnd] = localDate1.AddDays(3);
                                requiredUpdate = true;
                                break;
                            case "13":           //IR : QUALITY_CONTROL_INSPECTION
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledStart] = localDate1.AddDays(3);
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledEnd] = localDate1.AddDays(4);
                                requiredUpdate = true;
                                break;
                            case "14":           //IR : JOB_COMPLETED
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledStart] = localDate1.AddDays(4);
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledEnd] = localDate1.AddDays(4);
                                requiredUpdate = true;
                                break;
                            case "15":           //IR : HERO_SHOT_PICTURE
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledEnd] = localDate1.AddDays(4);
                                requiredUpdate = true;
                                break;
                            case "16":           //IR : MARKETING_INSPECTION
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledStart] = localDate1.AddDays(4);
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledEnd] = localDate1.AddDays(5);
                                requiredUpdate = true;
                                break;
                            case "17":           //IR : BI_WEEKLY_INSPECTION
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledStart] = localDate1.AddDays(18);
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledEnd] = localDate1.AddDays(19);
                                requiredUpdate = true;
                                break;
                            case "18":           //IR : MOVE_IN_INSPECTION_COMPLETED
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledStart] = localDate1.AddDays(49);
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledEnd] = localDate1.AddDays(50);
                                requiredUpdate = true;
                                break;
                        }
                        if (requiredUpdate)
                            service.Update(tmpPrjTskEntity);

                    }
                }
            }
            //
        }

        private static void PropertyAcquiredScheduledDateChange(ITracingService tracer, IOrganizationService service, Entity projectEntity, DateTime localDate1)
        {
            List<Entity> TaskIndentifierEntityList = CommonMethods.RetrieveActivtTaskIdentifier(tracer, service);

            EntityCollection projectTaskEntityCollection = CommonMethods.RetrieveAllOpenProjectTaskByProject(tracer, service, projectEntity.ToEntityReference());

            foreach (Entity projectTaskEntity in projectTaskEntityCollection.Entities)
            {
                if (projectTaskEntity.Attributes.Contains(Constants.ProjectTasks.TaskIdentifier))
                {

                    Entity taskIdentifierEntity = TaskIndentifierEntityList.Where(e => e.Id.Equals(projectTaskEntity.GetAttributeValue<EntityReference>(Constants.ProjectTasks.TaskIdentifier).Id)).FirstOrDefault();
                    if (taskIdentifierEntity is Entity && taskIdentifierEntity.Attributes.Contains(Constants.TaskIdentifiers.WBSID))
                    {
                        Entity tmpPrjTskEntity = new Entity(projectTaskEntity.LogicalName);
                        tmpPrjTskEntity.Id = projectTaskEntity.Id;
                        bool requiredUpdate = false;
                        switch (taskIdentifierEntity.GetAttributeValue<string>(Constants.TaskIdentifiers.WBSID))
                        {
                            case "8":           //IR : CLOSE_ESCROW
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledStart] = localDate1;
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledEnd] = localDate1;
                                requiredUpdate = true;
                                break;
                            case "9":           //IR : JOB_AND_CONTRACTS_SUBMITTED_TO_YARDI
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledStart] = localDate1;
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledEnd] = localDate1.AddDays(1);
                                requiredUpdate = true;
                                break;
                            case "10":           //IR : VENDORS_SAYS_JOB_STARTED
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledStart] = localDate1.AddDays(1);
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledEnd] = localDate1.AddDays(1);
                                requiredUpdate = true;
                                break;
                            case "11":           //IR : WORK_IN_PROGRESS
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledStart] = localDate1.AddDays(1);
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledEnd] = localDate1.AddDays(2);
                                requiredUpdate = true;
                                break;
                            case "12":           //IR : VENDOR_SAYS_JOBS_COMPLETE
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledStart] = localDate1.AddDays(2);
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledEnd] = localDate1.AddDays(3);
                                requiredUpdate = true;
                                break;
                            case "13":           //IR : QUALITY_CONTROL_INSPECTION
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledStart] = localDate1.AddDays(3);
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledEnd] = localDate1.AddDays(4);
                                requiredUpdate = true;
                                break;
                            case "14":           //IR : JOB_COMPLETED
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledStart] = localDate1.AddDays(4);
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledEnd] = localDate1.AddDays(4);
                                requiredUpdate = true;
                                break;
                            case "15":           //IR : HERO_SHOT_PICTURE
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledEnd] = localDate1.AddDays(4);
                                requiredUpdate = true;
                                break;
                            case "16":           //IR : MARKETING_INSPECTION
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledStart] = localDate1.AddDays(4);
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledEnd] = localDate1.AddDays(5);
                                requiredUpdate = true;
                                break;
                            case "17":           //IR : BI_WEEKLY_INSPECTION
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledStart] = localDate1.AddDays(18);
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledEnd] = localDate1.AddDays(19);
                                requiredUpdate = true;
                                break;
                            case "18":           //IR : MOVE_IN_INSPECTION_COMPLETED
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledStart] = localDate1.AddDays(49);
                                tmpPrjTskEntity[Constants.ProjectTasks.ScheduledEnd] = localDate1.AddDays(50);
                                requiredUpdate = true;
                                break;
                        }
                        if (requiredUpdate)
                            service.Update(tmpPrjTskEntity);

                    }
                }
            }
            //
        }

        #endregion
    }
}