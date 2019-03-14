using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;

namespace FirstKey.D365.Plug_Ins
{
    public class OnJobCreate : IPlugin
    {
        const string RENOWALK_URL = "https://hdapps.homedepot.com/RenoWalk/#/admin/property/";
        #region Secure/Unsecure Configuration Setup
        private string _secureConfig = null;
        private string _unsecureConfig = null;

        public OnJobCreate(string unsecureConfig, string secureConfig)
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

            Entity jobEntity = null;
            if (!context.InputParameters.Contains(Constants.TARGET)) { return; }
            if (((Entity)context.InputParameters[Constants.TARGET]).LogicalName != Constants.Jobs.LogicalName)
                return;

            try
            {
                switch (context.MessageName)
                {
                    case Constants.Messages.Create:
                        if (context.InputParameters.Contains(Constants.TARGET) && context.InputParameters[Constants.TARGET] is Entity)
                            jobEntity = context.InputParameters[Constants.TARGET] as Entity;
                        else
                            return;
                        break;
                    case Constants.Messages.Update:
                        if (context.InputParameters.Contains(Constants.TARGET) && context.InputParameters[Constants.TARGET] is Entity && context.PostEntityImages.Contains(Constants.POST_IMAGE))
                            jobEntity = context.PostEntityImages[Constants.POST_IMAGE] as Entity;
                        else
                            return;
                        break;
                }

                if (jobEntity == null)
                {
                    tracer.Trace($"Job entity is Null.");
                    return;
                }
                if (!jobEntity.Attributes.Contains(Constants.Jobs.Unit) || !jobEntity.Attributes.Contains(Constants.Jobs.RenowalkID))
                {
                    tracer.Trace($"Job Entity missing Unit field or Renowalk ID.");
                    return;
                }

                tracer.Trace($"Retrieving Active Project for the unit in D365.");
                EntityCollection projectEntityCollection = new EntityCollection();
                if (jobEntity.Attributes.Contains(Constants.Jobs.RenowalkID))
                {
                    tracer.Trace($"Job Renowalk ID : {jobEntity.GetAttributeValue<string>(Constants.Jobs.RenowalkID)}.");
                    tracer.Trace($"Retrieving Active Project By Renowalk ID : {jobEntity.GetAttributeValue<string>(Constants.Jobs.RenowalkID)}.");
                    projectEntityCollection = CommonMethods.RetrieveActivtProjectByRenowalkId(tracer, service, jobEntity.GetAttributeValue<string>(Constants.Jobs.RenowalkID));
                    tracer.Trace($"Retrieving Active Project By Renowalk ID : {jobEntity.GetAttributeValue<string>(Constants.Jobs.RenowalkID)} successfully completed.");
                }
                if (projectEntityCollection.Entities.Count == 0 && jobEntity.Attributes.Contains(Constants.Jobs.Unit) && jobEntity.GetAttributeValue<EntityReference>(Constants.Jobs.Unit) != null)
                {
                    tracer.Trace($"Job Unit ID : {jobEntity.GetAttributeValue<EntityReference>(Constants.Jobs.Unit).Id.ToString()}.");
                    tracer.Trace($"Retrieving Active Project By Unit ID : {jobEntity.GetAttributeValue<EntityReference>(Constants.Jobs.Unit).Id.ToString()}.");
                    projectEntityCollection = CommonMethods.RetrieveActivtProjectByUnitId(tracer, service, jobEntity.GetAttributeValue<EntityReference>(Constants.Jobs.Unit));
                }
                if (projectEntityCollection.Entities.Count > 0)
                {
                    foreach (Entity projectEntity in projectEntityCollection.Entities)
                    {
                        try
                        {
                            Entity tmpPrjEntity = new Entity(projectEntity.LogicalName);
                            tmpPrjEntity.Id = projectEntity.Id;
                            tmpPrjEntity[Constants.Projects.Job] = jobEntity.ToEntityReference();
                            tmpPrjEntity[Constants.Projects.RenowalkURL] = RENOWALK_URL + jobEntity.GetAttributeValue<string>(Constants.Jobs.RenowalkID);
                            if (jobEntity.Attributes.Contains(Constants.Jobs.JobAmount))
                                tmpPrjEntity[Constants.Projects.OriginalBudget] = jobEntity.GetAttributeValue<Money>(Constants.Jobs.JobAmount);
                            tmpPrjEntity[Constants.Projects.TotalInvoiced] = new Money(0);
                            tmpPrjEntity[Constants.Projects.ChangeOrder] = new Money(0);


                            service.Update(tmpPrjEntity);
                            tracer.Trace($"Project with ID {tmpPrjEntity.Id.ToString()} successfully updated with Job Information.");
                        }
                        catch(Exception ex)
                        {
                            tracer.Trace($"Error while updating Project. Error Message : {ex.Message}. Error Trace : {ex.StackTrace}");
                        }

                    }
                }
                else
                {
                    tracer.Trace($"No Active Project for the unit in D365.");
                }

                //TODO: Do stuff
            }
            catch (Exception e)
            {
                throw new InvalidPluginExecutionException(e.Message);
            }
        }
    }
}