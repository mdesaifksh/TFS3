using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace Firstkey.D365.WorkflowActivity
{
    public class ResidentNoticetoMoveOutReceived : CodeActivity
    {
        [Output("IsSuccess")]
        public OutArgument<bool> IsSuccess
        {
            get;
            set;
        }

        [Input("Project")]
        [ReferenceTarget(Constants.Projects.LogicalName)]
        [RequiredArgument]
        public InArgument<EntityReference> ProjectEntityReference
        {
            get;
            set;
        }


        protected override void Execute(CodeActivityContext executionContext)
        {
            ITracingService extension = executionContext.GetExtension<ITracingService>();
            IWorkflowContext workflowContext = executionContext.GetExtension<IWorkflowContext>();
            IOrganizationServiceFactory organizationServiceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            IOrganizationService organizationService = organizationServiceFactory.CreateOrganizationService(new Guid?(workflowContext.UserId));
            try
            {
                bool flag = this.ExecuteProcess(extension, organizationService, this.ProjectEntityReference.Get(executionContext));
                this.IsSuccess.Set(executionContext, flag);
            }
            catch (Exception exception)
            {
                throw new InvalidPluginExecutionException(OperationStatus.Failed, exception.Message);
            }
        }

        private bool ExecuteProcess(ITracingService tracer, IOrganizationService service, EntityReference projectEntityReference)
        {
            Entity projectEntity = service.Retrieve(projectEntityReference.LogicalName, projectEntityReference.Id, new ColumnSet(true));
            if (projectEntity is Entity &&  projectEntity.Attributes.Contains(Constants.Projects.ProjectTemplate) && projectEntity.Attributes.Contains(Constants.Projects.Unit))
            {
                int timeZoneCode = CommonMethods.RetrieveCurrentUsersSettings(service);

                Entity unitEntity = service.Retrieve(projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.Unit).LogicalName, projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.Unit).Id, new ColumnSet(true));
                DateTime moveOutDate = DateTime.Now;
                if (unitEntity.Attributes.Contains(Constants.Units.MoveOutDate))
                    moveOutDate = CommonMethods.RetrieveLocalTimeFromUTCTime(service, timeZoneCode, unitEntity.GetAttributeValue<DateTime>(Constants.Units.MoveOutDate));
                DateTime unitStatusChange = DateTime.Now;
                if (projectEntity.Attributes.Contains(Constants.Projects.StartDate))
                    unitStatusChange = CommonMethods.RetrieveLocalTimeFromUTCTime(service, timeZoneCode, projectEntity.GetAttributeValue<DateTime>(Constants.Projects.StartDate));
                tracer.Trace($"Move Out Date : {moveOutDate}");
                tracer.Trace($"Unit Status Change Date : {unitStatusChange}");

                //RESIDENT_NOTICE_TO_MOVE_OUT_RECEIVED = 1
                EntityCollection res_Notice_MoveOut_Rec_EntityCollection = CommonMethods.RetrieveProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectTemplate), 1,"1");
                foreach (Entity e in res_Notice_MoveOut_Rec_EntityCollection.Entities)
                {
                    Entity now = new Entity(e.LogicalName)
                    {
                        Id = e.Id
                    };
                    now[Constants.ProjectTasks.ActualStart] = unitStatusChange;
                    now[Constants.ProjectTasks.ActualEnd] = unitStatusChange;
                    now[Constants.ProjectTasks.Progress] = new decimal(100);
                    now[Constants.Status.StatusCode] = new OptionSetValue(963850001);
                    service.Update(now);
                    tracer.Trace("Task for Event : RESIDENT_NOTICE_TO_MOVE_OUT_RECEIVED Successfully Updated.");
                }

                //CORPORATE_RENEWALS = 3
                EntityCollection corp_renewal_EntityCollection = CommonMethods.RetrieveProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectTemplate), 3,"3");
                foreach (Entity e in corp_renewal_EntityCollection.Entities)
                {
                    DateTime SchStartDate = CommonMethods.RetrieveLocalTimeFromUTCTime(service, timeZoneCode, e.GetAttributeValue<DateTime>(Constants.ProjectTasks.ScheduledStart));
                    Entity num = new Entity(e.LogicalName)
                    {
                        Id = e.Id
                    };
                    num[Constants.ProjectTasks.ActualStart] = unitStatusChange;
                    num[Constants.ProjectTasks.ScheduledEnd] = (moveOutDate > SchStartDate) ? moveOutDate : SchStartDate.AddHours(24);
                    num[Constants.ProjectTasks.Progress] = new decimal(1);
                    num[Constants.Status.StatusCode] = new OptionSetValue(963850000);
                    service.Update(num);
                    tracer.Trace("Task for Event : CORPORATE_RENEWALS Successfully Updated.");
                }

                //ASSIGN_PROJECT_MANAGER = 2
                EntityCollection assign_Proj_mgr_EntityCollection = CommonMethods.RetrieveProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectTemplate), 2, "2");
                foreach (Entity e in assign_Proj_mgr_EntityCollection.Entities)
                {
                    DateTime SchStartDate = CommonMethods.RetrieveLocalTimeFromUTCTime(service, timeZoneCode, e.GetAttributeValue<DateTime>(Constants.ProjectTasks.ScheduledStart));
                    DateTime dueDate = (moveOutDate.AddDays(-37) > DateTime.Now) ? ((moveOutDate.AddDays(-37) > SchStartDate) ? moveOutDate.AddDays(-37) : SchStartDate.AddHours(24)) : ((SchStartDate > DateTime.Now) ? SchStartDate.AddHours(24) : DateTime.Now);
                    tracer.Trace($"Scheduled Start Date : {SchStartDate}");
                    tracer.Trace($"Due Date : {dueDate}");
                    tracer.Trace($"Unit Status Change Date : {unitStatusChange}");
                    Entity num = new Entity(e.LogicalName)
                    {
                        Id = e.Id
                    };
                    num[Constants.ProjectTasks.ActualStart] = unitStatusChange;
                    num[Constants.ProjectTasks.ScheduledEnd] = dueDate;
                    num[Constants.ProjectTasks.Progress] = new decimal(1);
                    num[Constants.Status.StatusCode] = new OptionSetValue(963850000);
                    service.Update(num);
                    tracer.Trace("Task for Event : ASSIGN_PROJECT_MANAGER Successfully Updated.");
                }

                //MARKET_SCHEDULES_PRE_MOVE_OUT = 4
                EntityCollection mkt_sch_pre_moveout_EntityCollection = CommonMethods.RetrieveProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectTemplate), 4, "4");
                foreach (Entity e in mkt_sch_pre_moveout_EntityCollection.Entities)
                {
                    DateTime SchStartDate = CommonMethods.RetrieveLocalTimeFromUTCTime(service, timeZoneCode, e.GetAttributeValue<DateTime>(Constants.ProjectTasks.ScheduledStart));
                    DateTime dueDate = (moveOutDate.AddDays(-37) > DateTime.Now) ? ((moveOutDate.AddDays(-37) > SchStartDate) ? moveOutDate.AddDays(-37) : SchStartDate.AddHours(24)) : ((SchStartDate > DateTime.Now) ? SchStartDate.AddHours(24) : DateTime.Now);
                    tracer.Trace($"Scheduled Start Date : {SchStartDate}");
                    tracer.Trace($"Due Date : {dueDate}");
                    tracer.Trace($"Unit Status Change Date : {unitStatusChange}");
                    Entity num = new Entity(e.LogicalName)
                    {
                        Id = e.Id
                    };
                    num[Constants.ProjectTasks.ScheduledEnd] = dueDate;
                    service.Update(num);
                    tracer.Trace("Task for Event : MARKET_SCHEDULES_PRE_MOVE_OUT Successfully Updated.");
                }



                //HERO_SHOT_PICTURE = 18
                EntityCollection hero_Shot_EntityCollection = CommonMethods.RetrieveProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectTemplate), 3, "3");
                foreach (Entity e in hero_Shot_EntityCollection.Entities)
                {
                    Entity num = new Entity(e.LogicalName)
                    {
                        Id = e.Id
                    };
                    num[Constants.ProjectTasks.ScheduledStart] = moveOutDate;
                    service.Update(num);
                    tracer.Trace("Task for Event : HERO_SHOT_PICTURE Successfully Updated.");
                }
            }
            return true;
        }
    }
}
