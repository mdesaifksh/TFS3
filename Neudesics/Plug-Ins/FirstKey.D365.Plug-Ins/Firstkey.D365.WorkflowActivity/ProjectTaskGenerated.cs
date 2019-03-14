using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;
using System.Threading;

namespace Firstkey.D365.WorkflowActivity
{
    public class ProjectTaskGenerated : CodeActivity
    {
        [Output("IsGenerated?")]
        public OutArgument<bool> IsGenerated
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
                this.IsGenerated.Set(executionContext, flag);
            }
            catch (Exception exception)
            {
                throw new InvalidPluginExecutionException(OperationStatus.Failed, exception.Message);
            }
        }

        private bool ExecuteProcess(ITracingService tracer, IOrganizationService service, EntityReference projectEntityReference)
        {
            int currentCount;
            int totalCount = 0;
            int cnt = 0;
            bool flag = false;
            while (true)
            {
                currentCount = 0;
                tracer.Trace($"Checking Project Task Count for Project with ReTry {cnt}");
                currentCount = CommonMethods.CountprojectTaskForProject(tracer, service, projectEntityReference);
                if (cnt > 2)
                {
                    break;
                }
                tracer.Trace($"Last Task Count : {totalCount}. Current Task Count : {currentCount} . Went for Sleep for 1500 ms. Retry Count : {cnt}");
                totalCount = currentCount;
                Thread.Sleep(1500);
                cnt++;
            }
            flag = ((currentCount == 0 ? true : currentCount != totalCount) ? false : true);
            tracer.Trace($"Total Task Count : {totalCount} generated for Project.");
            return flag;
        }
    }
}

