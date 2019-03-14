using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;
using System.Threading;
using Microsoft.Xrm.Sdk.Query;

namespace Firstkey.D365.WorkflowActivity
{
    public class IR_OfferAccepted : CodeActivity
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
            if (projectEntity is Entity && projectEntity.Attributes.Contains(Constants.Projects.ProjectTemplate) && projectEntity.Attributes.Contains(Constants.Projects.Unit))
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

                //OFFER_ACCEPTED = 1
                EntityCollection res_Notice_MoveOut_Rec_EntityCollection = CommonMethods.RetrieveProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectTemplate), 201, "1");
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
                    tracer.Trace("Task for Event : OFFER_ACCEPTED Successfully Updated.");
                }

                //ASSIGN_PROJECT_MANAGER = 2
                EntityCollection assign_Proj_mgr_EntityCollection = CommonMethods.RetrieveProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectTemplate), 202, "2");
                foreach (Entity e in assign_Proj_mgr_EntityCollection.Entities)
                {
                    tracer.Trace($"Unit Status Change Date : {unitStatusChange}");
                    Entity num = new Entity(e.LogicalName)
                    {
                        Id = e.Id
                    };
                    num[Constants.ProjectTasks.ActualStart] = unitStatusChange;
                    num[Constants.ProjectTasks.Progress] = new decimal(1);
                    num[Constants.Status.StatusCode] = new OptionSetValue(963850000);
                    service.Update(num);
                    tracer.Trace("Task for Event : ASSIGN_PROJECT_MANAGER Successfully Updated.");
                }

                //HERO_SHOT_PICTURE = 18
                EntityCollection hero_Shot_EntityCollection = CommonMethods.RetrieveProjectTaskByProjectAndTaskIdentifier(tracer, service, projectEntity.ToEntityReference(), projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectTemplate), 217, "15");
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
