using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace FirstKey.D365.Plug_Ins
{
    public class JobStatusChange : IPlugin
    {
        #region Secure/Unsecure Configuration Setup
        private string _secureConfig = null;
        private string _unsecureConfig = null;
        private const string TURNPROCESS_PROJECT_TEMPLATE = "TURNPROCESS_PROJECT_TEMPLATE";
        private const string INITIALRENOVATION_PROJECT_TEMPLATE = "INITIALRENOVATION_PROJECT_TEMPLATE";

        public JobStatusChange(string unsecureConfig, string secureConfig)
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

            if (projectTemplateSettings == null)
            {
                tracer.Trace($"Project Template Setting not available in Plugin UnSecure Configuration.");
                return;
            }

            Entity jobEntity = null;
            if (!context.InputParameters.Contains(Constants.TARGET)) { return; }
            if (((Entity)context.InputParameters[Constants.TARGET]).LogicalName != Constants.Jobs.LogicalName)
                return;


            try
            {
                switch (context.MessageName)
                {
                    //case Constants.Messages.Create:
                    //    if (context.InputParameters.Contains(Constants.TARGET) && context.InputParameters[Constants.TARGET] is Entity)
                    //        jobEntity = context.InputParameters[Constants.TARGET] as Entity;
                    //    else
                    //        return;
                    //    break;
                    case Constants.Messages.Update:
                        if (context.InputParameters.Contains(Constants.TARGET) && context.InputParameters[Constants.TARGET] is Entity && context.PostEntityImages.Contains(Constants.POST_IMAGE))
                            jobEntity = context.PostEntityImages[Constants.POST_IMAGE] as Entity;
                        else
                            return;
                        break;
                }
                //TODO: Do stuff

                if (jobEntity == null || context.Depth > 2)
                {
                    tracer.Trace($"Job entity is Null OR Context Depth is higher than 2. Actual Depth is : {context.Depth}");
                    return;
                }

                if (!jobEntity.Attributes.Contains(Constants.Jobs.JobStatus) || (!jobEntity.Attributes.Contains(Constants.Jobs.Unit) && !jobEntity.Attributes.Contains(Constants.Jobs.RenowalkID)))
                {
                    tracer.Trace($"Job Enity missing either Job Status or Unit or Renowalk ID field. Exiting PlugIn Pipeline");
                    return;
                }

                tracer.Trace($"Job Enity Job Status : {jobEntity.GetAttributeValue<OptionSetValue>(Constants.Jobs.JobStatus).Value}");
                if (jobEntity.GetAttributeValue<OptionSetValue>(Constants.Jobs.JobStatus).Value != 963850004)
                {
                    tracer.Trace($"Job Enity Job Status is NOT Contract Created (963850004). Existing PlugIn Pipeline.");
                    return;
                }

                if (jobEntity is Entity && projectTemplateSettings is ProjectTemplateSettings)
                {
                    tracer.Trace($"Finding Active Project for Unit with ID : {jobEntity.GetAttributeValue<EntityReference>(Constants.Jobs.Unit).Id.ToString()}.");
                    EntityCollection projectEntityCollection = new EntityCollection();
                    if (jobEntity.Attributes.Contains(Constants.Jobs.RenowalkID))
                        projectEntityCollection = CommonMethods.RetrieveActivtProjectByRenowalkId(tracer, service, jobEntity.GetAttributeValue<string>(Constants.Jobs.RenowalkID));
                    if (projectEntityCollection.Entities.Count == 0 && jobEntity.Attributes.Contains(Constants.Jobs.Unit))
                        projectEntityCollection = CommonMethods.RetrieveActivtProjectByUnitId(tracer, service, jobEntity.GetAttributeValue<EntityReference>(Constants.Jobs.Unit));
                    foreach (Entity projectEntity in projectEntityCollection.Entities)
                    {
                        if (projectEntity.Attributes.Contains(Constants.Projects.ProjectTemplate))
                        {
                            Mapping mapping = (
                                from m in projectTemplateSettings.Mappings
                                where m.Key.Equals(projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectTemplate).Id.ToString(), StringComparison.OrdinalIgnoreCase)
                                select m).FirstOrDefault<Mapping>();


                            if (mapping is Mapping)
                            {
                                tracer.Trace($"Project Template is : {mapping.Name}");
                                int timeZoneCode = CommonMethods.RetrieveCurrentUsersSettings(service);

                                if (mapping.Name.Equals(TURNPROCESS_PROJECT_TEMPLATE))
                                {
                                    CalculateTurnSchStartandEndDate(tracer, service, projectEntity, jobEntity, mapping, timeZoneCode);
                                    EntityCollection job_and_contracts_sub_to_yardi_EntityCollection = CommonMethods.RetrieveOpenProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectTemplate), 10, "10");
                                    foreach (Entity e in job_and_contracts_sub_to_yardi_EntityCollection.Entities)
                                    {
                                        Entity num = new Entity(e.LogicalName)
                                        {
                                            Id = e.Id
                                        };
                                        num[Constants.ProjectTasks.ActualStart] = DateTime.Now;
                                        num[Constants.ProjectTasks.ActualEnd] = DateTime.Now;
                                        num[Constants.ProjectTasks.ActualDurationInMinutes] = (!e.Attributes.Contains(Constants.ProjectTasks.ActualStart)) ? 0 : (DateTime.Now - e.GetAttributeValue<DateTime>(Constants.ProjectTasks.ActualStart)).TotalMinutes;
                                        num[Constants.ProjectTasks.Progress] = new decimal(100);
                                        num[Constants.Status.StatusCode] = new OptionSetValue(963850001);
                                        service.Update(num);
                                        tracer.Trace("Task for Event : JOB_AND_CONTRACTS_SUBMITTED_TO_YARDI Successfully Updated.");
                                    }

                                    EntityCollection job_assign_to_vendor_in_cc_EntityCollection = CommonMethods.RetrieveOpenProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectTemplate), 9, "9");
                                    foreach (Entity e in job_assign_to_vendor_in_cc_EntityCollection.Entities)
                                    {
                                        Entity num = new Entity(e.LogicalName)
                                        {
                                            Id = e.Id
                                        };
                                        if (!e.Attributes.Contains(Constants.ProjectTasks.ActualStart))
                                            num[Constants.ProjectTasks.ActualStart] = DateTime.Now;
                                        num[Constants.ProjectTasks.ActualEnd] = DateTime.Now;
                                        num[Constants.ProjectTasks.ActualDurationInMinutes] = (!e.Attributes.Contains(Constants.ProjectTasks.ActualStart)) ? 0 : (DateTime.Now - e.GetAttributeValue<DateTime>(Constants.ProjectTasks.ActualStart)).TotalMinutes;
                                        num[Constants.ProjectTasks.Progress] = new decimal(100);
                                        num[Constants.Status.StatusCode] = new OptionSetValue(963850001);
                                        service.Update(num);
                                        tracer.Trace("Task for Event : JOB_ASSIGNMENT_TO_VENDORS_IN_CONTRACT_CREATOR Successfully Updated.");
                                    }



                                }
                                else if (mapping.Name.Equals(INITIALRENOVATION_PROJECT_TEMPLATE))
                                {
                                    EntityCollection job_and_contracts_sub_to_yardi_EntityCollection = CommonMethods.RetrieveOpenProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectTemplate), 209, "9");
                                    foreach (Entity e in job_and_contracts_sub_to_yardi_EntityCollection.Entities)
                                    {
                                        Entity num = new Entity(e.LogicalName)
                                        {
                                            Id = e.Id
                                        };
                                        num[Constants.ProjectTasks.ActualStart] = DateTime.Now;
                                        num[Constants.ProjectTasks.ActualEnd] = DateTime.Now;
                                        num[Constants.ProjectTasks.ActualDurationInMinutes] = (!e.Attributes.Contains(Constants.ProjectTasks.ActualStart)) ? 0 : (DateTime.Now - e.GetAttributeValue<DateTime>(Constants.ProjectTasks.ActualStart)).TotalMinutes;
                                        num[Constants.ProjectTasks.Progress] = new decimal(100);
                                        num[Constants.Status.StatusCode] = new OptionSetValue(963850001);
                                        service.Update(num);
                                        tracer.Trace("Task for Event : JOB_AND_CONTRACTS_SUBMITTED_TO_YARDI Successfully Updated.");
                                    }

                                    EntityCollection job_assign_to_vendor_in_cc_EntityCollection = CommonMethods.RetrieveOpenProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectTemplate), 207, "7");
                                    foreach (Entity e in job_assign_to_vendor_in_cc_EntityCollection.Entities)
                                    {
                                        Entity num = new Entity(e.LogicalName)
                                        {
                                            Id = e.Id
                                        };
                                        if (!e.Attributes.Contains(Constants.ProjectTasks.ActualStart))
                                            num[Constants.ProjectTasks.ActualStart] = DateTime.Now;
                                        num[Constants.ProjectTasks.ActualEnd] = DateTime.Now;
                                        num[Constants.ProjectTasks.ActualDurationInMinutes] = (!e.Attributes.Contains(Constants.ProjectTasks.ActualStart)) ? 0 : (DateTime.Now - e.GetAttributeValue<DateTime>(Constants.ProjectTasks.ActualStart)).TotalMinutes;
                                        num[Constants.ProjectTasks.Progress] = new decimal(100);
                                        num[Constants.Status.StatusCode] = new OptionSetValue(963850001);
                                        service.Update(num);
                                        tracer.Trace("Task for Event : JOB_ASSIGNMENT_TO_VENDORS_IN_CONTRACT_CREATOR Successfully Updated.");
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new InvalidPluginExecutionException(e.Message);
            }
        }

        private void CalculateTurnSchStartandEndDate(ITracingService tracer, IOrganizationService service, Entity projectEntity, Entity jobEntity, Mapping mapping, int timeZoneCode)
        {
            if (jobEntity.Attributes.Contains(Constants.Jobs.JobAmount))
            {
                EntityCollection jobVendorsEntityCollection = RetrieveJobVenodrsByJob(tracer, service, jobEntity.ToEntityReference());
                if (jobVendorsEntityCollection.Entities.Count > 0)
                {
                    tracer.Trace($"Project Template is : {mapping.Name}");

                    if (mapping.Name.Equals(TURNPROCESS_PROJECT_TEMPLATE))
                    {
                        if (projectEntity.Attributes.Contains(Constants.Projects.Unit))
                        {
                            Entity unitEntity = service.Retrieve(Constants.Units.LogicalName, projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.Unit).Id, new ColumnSet(Constants.Units.MoveOutDate));
                            if (unitEntity is Entity && unitEntity.Attributes.Contains(Constants.Units.MoveOutDate))
                            {
                                DateTime schJobCompletionDate = unitEntity.GetAttributeValue<DateTime>(Constants.Units.MoveOutDate).AddDays((double)Math.Ceiling(jobEntity.GetAttributeValue<Money>(Constants.Jobs.JobAmount).Value / 700));
                                Entity startDateEntity = jobVendorsEntityCollection.Entities.Where(e => e.Attributes.Contains(Constants.JobVendors.StartDate)).OrderBy(e => e.Attributes[Constants.JobVendors.StartDate]).FirstOrDefault();
                                Entity endDateEntity = jobVendorsEntityCollection.Entities.Where(e => e.Attributes.Contains(Constants.JobVendors.StartDate)).OrderByDescending(e => e.Attributes[Constants.JobVendors.EndDate]).FirstOrDefault();

                                DateTime startDate = CommonMethods.RetrieveLocalTimeFromUTCTime(service, timeZoneCode, startDateEntity.GetAttributeValue<DateTime>(Constants.JobVendors.StartDate));
                                DateTime endDate = CommonMethods.RetrieveLocalTimeFromUTCTime(service, timeZoneCode, endDateEntity.GetAttributeValue<DateTime>(Constants.JobVendors.EndDate));

                                PerformScheduledDateMoveOut(tracer, service, projectEntity, schJobCompletionDate, startDate, endDate, true);
                            }
                            else
                                tracer.Trace("Unit Entity does not contain Move Out Date.");
                        }
                        else
                            tracer.Trace("Project Entity does not contain Unit");

                    }
                    else
                    {
                        EntityCollection prop_acquired_EntityCollection = CommonMethods.RetrieveAllProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectTemplate), (int)Events.CLOSE_ESCROW);
                        if (prop_acquired_EntityCollection.Entities.Count > 0)
                        {
                            Entity propAcquiredProjectTaskEntity = prop_acquired_EntityCollection.Entities[0];
                            if(propAcquiredProjectTaskEntity is Entity && propAcquiredProjectTaskEntity.Attributes.Contains(Constants.ProjectTasks.ActualEnd))
                            {
                                Entity startDateEntity = jobVendorsEntityCollection.Entities.Where(e => e.Attributes.Contains(Constants.JobVendors.StartDate)).OrderBy(e => e.Attributes[Constants.JobVendors.StartDate]).FirstOrDefault();
                                Entity endDateEntity = jobVendorsEntityCollection.Entities.Where(e => e.Attributes.Contains(Constants.JobVendors.StartDate)).OrderByDescending(e => e.Attributes[Constants.JobVendors.EndDate]).FirstOrDefault();

                                DateTime startDate = CommonMethods.RetrieveLocalTimeFromUTCTime(service, timeZoneCode, startDateEntity.GetAttributeValue<DateTime>(Constants.JobVendors.StartDate));
                                DateTime endDate = CommonMethods.RetrieveLocalTimeFromUTCTime(service, timeZoneCode, endDateEntity.GetAttributeValue<DateTime>(Constants.JobVendors.EndDate));
                                DateTime schJobCompletionDate = CommonMethods.RetrieveLocalTimeFromUTCTime(service, timeZoneCode, propAcquiredProjectTaskEntity.GetAttributeValue<DateTime>(Constants.ProjectTasks.ActualEnd));

                                PerformScheduledDateMoveOut(tracer, service, projectEntity, schJobCompletionDate, startDate, endDate, true);

                            }
                        }
                    }
                }
            }
            else
            {
                tracer.Trace("Job Entity does not contain either Job Amount or Unit");
            }
        }

        private void PerformScheduledDateMoveOut(ITracingService tracer, IOrganizationService service, Entity projectEntity, DateTime schJobCompletionDate, DateTime startDate, DateTime endDate, bool isTurn)
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

                        Entity tmpPrjTskEntity = new Entity(projectTaskEntity.LogicalName);
                        tmpPrjTskEntity.Id = projectTaskEntity.Id;
                        bool requiredUpdate = false;

                        if (isTurn)
                        {
                            #region TURN SWITCH
                            switch (taskIdentifierEntity.GetAttributeValue<string>(Constants.TaskIdentifiers.WBSID))
                            {
                                case "11":          //VENDOR(S) SAYS JOB STARTED
                                    tmpPrjTskEntity[Constants.ProjectTasks.ScheduledStart] = startDate;
                                    tmpPrjTskEntity[Constants.ProjectTasks.ScheduledEnd] = startDate;
                                    requiredUpdate = true;
                                    break;
                                case "12":          //WORK IN PROGRESS
                                    tmpPrjTskEntity[Constants.ProjectTasks.ScheduledStart] = startDate;
                                    tmpPrjTskEntity[Constants.ProjectTasks.ScheduledEnd] = endDate;
                                    requiredUpdate = true;
                                    break;
                                case "13":          //VENDOR SAYS JOB?S COMPLETE
                                    tmpPrjTskEntity[Constants.ProjectTasks.ScheduledStart] = endDate;
                                    tmpPrjTskEntity[Constants.ProjectTasks.ScheduledEnd] = endDate;
                                    requiredUpdate = true;
                                    break;
                                case "14":          //QUALITY CONTROL INSPECTION
                                    tmpPrjTskEntity[Constants.ProjectTasks.ScheduledStart] = endDate;
                                    tmpPrjTskEntity[Constants.ProjectTasks.ScheduledEnd] = (schJobCompletionDate > endDate) ? schJobCompletionDate : endDate.AddDays(1);
                                    requiredUpdate = true;
                                    break;
                                case "15":          //JOB COMPLETED
                                    tmpPrjTskEntity[Constants.ProjectTasks.ScheduledStart] = schJobCompletionDate;
                                    tmpPrjTskEntity[Constants.ProjectTasks.ScheduledEnd] = schJobCompletionDate;
                                    requiredUpdate = true;
                                    break;
                                case "16":          //HERO SHOT PICTURE
                                    tmpPrjTskEntity[Constants.ProjectTasks.ScheduledEnd] = schJobCompletionDate;
                                    requiredUpdate = true;
                                    break;
                                case "17":          //MARKETING INSPECTION
                                    tmpPrjTskEntity[Constants.ProjectTasks.ScheduledStart] = schJobCompletionDate;
                                    tmpPrjTskEntity[Constants.ProjectTasks.ScheduledEnd] = schJobCompletionDate.AddDays(2);
                                    requiredUpdate = true;
                                    break;
                                case "18":          //BI-WEEKLY INSPECTION
                                    tmpPrjTskEntity[Constants.ProjectTasks.ScheduledStart] = schJobCompletionDate.AddDays(13);
                                    tmpPrjTskEntity[Constants.ProjectTasks.ScheduledEnd] = schJobCompletionDate.AddDays(14);
                                    requiredUpdate = true;
                                    break;

                            }

                            #endregion
                        }
                        else
                        {
                            #region RENO SWITCH
                            switch (taskIdentifierEntity.GetAttributeValue<string>(Constants.TaskIdentifiers.WBSID))
                            {
                                case "10":           //IR : VENDORS_SAYS_JOB_STARTED
                                    tmpPrjTskEntity[Constants.ProjectTasks.ScheduledStart] = startDate;
                                    tmpPrjTskEntity[Constants.ProjectTasks.ScheduledEnd] = startDate;
                                    requiredUpdate = true;
                                    break;
                                case "11":           //IR : WORK_IN_PROGRESS
                                    tmpPrjTskEntity[Constants.ProjectTasks.ScheduledStart] = startDate;
                                    tmpPrjTskEntity[Constants.ProjectTasks.ScheduledEnd] = endDate;
                                    requiredUpdate = true;
                                    break;
                                case "12":           //IR : VENDOR_SAYS_JOBS_COMPLETE
                                    tmpPrjTskEntity[Constants.ProjectTasks.ScheduledStart] = endDate;
                                    tmpPrjTskEntity[Constants.ProjectTasks.ScheduledEnd] = endDate;
                                    requiredUpdate = true;
                                    break;
                                case "13":           //IR : QUALITY_CONTROL_INSPECTION
                                    tmpPrjTskEntity[Constants.ProjectTasks.ScheduledStart] = endDate;
                                    tmpPrjTskEntity[Constants.ProjectTasks.ScheduledEnd] = (schJobCompletionDate > endDate) ? schJobCompletionDate : endDate.AddDays(1);
                                    requiredUpdate = true;
                                    break;
                                case "14":           //IR : JOB_COMPLETED
                                    tmpPrjTskEntity[Constants.ProjectTasks.ScheduledStart] = schJobCompletionDate;
                                    tmpPrjTskEntity[Constants.ProjectTasks.ScheduledEnd] = schJobCompletionDate;
                                    requiredUpdate = true;
                                    break;
                                case "15":           //IR : HERO_SHOT_PICTURE
                                    tmpPrjTskEntity[Constants.ProjectTasks.ScheduledEnd] = schJobCompletionDate;
                                    requiredUpdate = true;
                                    break;
                                case "16":           //IR : MARKETING_INSPECTION
                                    tmpPrjTskEntity[Constants.ProjectTasks.ScheduledStart] = schJobCompletionDate;
                                    tmpPrjTskEntity[Constants.ProjectTasks.ScheduledEnd] = schJobCompletionDate.AddDays(2);
                                    requiredUpdate = true;
                                    break;
                                case "17":           //IR : BI_WEEKLY_INSPECTION
                                    tmpPrjTskEntity[Constants.ProjectTasks.ScheduledStart] = schJobCompletionDate.AddDays(13);
                                    tmpPrjTskEntity[Constants.ProjectTasks.ScheduledEnd] = schJobCompletionDate.AddDays(14);
                                    requiredUpdate = true;
                                    break;
                            }
                            #endregion
                        }

                        if (requiredUpdate)
                            service.Update(tmpPrjTskEntity);

                    }
                }
            }
            //
        }

        private EntityCollection RetrieveJobVenodrsByJob(ITracingService tracer, IOrganizationService service, EntityReference jobEntityReference)
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
                        }
                    }
                },
                TopCount = 1000,
                Orders = { new OrderExpression(Constants.JobVendors.StartDate, OrderType.Ascending) }
            };

            return service.RetrieveMultiple(Query);
        }
    }
}