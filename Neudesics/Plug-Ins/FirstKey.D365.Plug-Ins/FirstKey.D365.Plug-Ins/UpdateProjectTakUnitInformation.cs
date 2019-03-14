using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Serialization;

namespace FirstKey.D365.Plug_Ins
{
    /// <summary>
    /// Summary: Plugin is used to update all Open Project's Project Task Unit Information.
    /// Need To Trigger in Async.
    /// Need to have POST Image.
    /// Needs to register in Post Operation.
    /// </summary>
    public class UpdateProjectTakUnitInformation : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            ITracingService tracer = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = factory.CreateOrganizationService(context.UserId);

            Entity unitEntity = null;
            if (!context.InputParameters.Contains(Constants.TARGET)) { return; }
            if (((Entity)context.InputParameters[Constants.TARGET]).LogicalName != Constants.Units.LogicalName)
                return;

            try
            {
                switch (context.MessageName)
                {

                    case Constants.Messages.Update:
                        if (context.InputParameters.Contains(Constants.TARGET) && context.InputParameters[Constants.TARGET] is Entity && context.PostEntityImages.Contains(Constants.POST_IMAGE))
                            unitEntity = context.PostEntityImages[Constants.POST_IMAGE] as Entity;
                        else
                            return;
                        break;
                }

                if (unitEntity is Entity)
                {
                    OrganizationRequestCollection orgRequestCollection = new OrganizationRequestCollection();
                    int cnt = 0;

                    //Retrieve All Project Task from System...
                    List<Entity> projectTaskEntityList = CommonMethods.RetrieveAllProjectTaskByUnit(tracer, service, unitEntity.ToEntityReference());
                    foreach (Entity projectTaskEntity in projectTaskEntityList)
                    {
                        Entity tmpEntity = new Entity(projectTaskEntity.LogicalName);
                        tmpEntity.Id = projectTaskEntity.Id;

                        //Access Notes
                        if (unitEntity.Attributes.Contains(Constants.Units.AccessNotes))
                            tmpEntity[Constants.ProjectTasks.AccessNotes] = unitEntity.GetAttributeValue<string>(Constants.Units.AccessNotes);
                        //LockBox Removed
                        if (unitEntity.Attributes.Contains(Constants.Units.LockBoxRemoved))
                            tmpEntity[Constants.ProjectTasks.LockBoxRemoved] = unitEntity.GetAttributeValue<DateTime>(Constants.Units.LockBoxRemoved);
                        //Mechanical Lockbox
                        if (unitEntity.Attributes.Contains(Constants.Units.MechanicalLockBox))
                            tmpEntity[Constants.ProjectTasks.MechanicalLockBox] = unitEntity.GetAttributeValue<string>(Constants.Units.MechanicalLockBox);
                        //Mechanical Lockbox Note
                        if (unitEntity.Attributes.Contains(Constants.Units.MechanicalLockBoxNote))
                            tmpEntity[Constants.ProjectTasks.MechanicalLockBoxNote] = unitEntity.GetAttributeValue<string>(Constants.Units.MechanicalLockBoxNote);
                        //Property Gate Code
                        if (unitEntity.Attributes.Contains(Constants.Units.PropertyGateCode))
                            tmpEntity[Constants.ProjectTasks.PropertyGateCode] = unitEntity.GetAttributeValue<string>(Constants.Units.PropertyGateCode);
                        //Rently Lockbox
                        if (unitEntity.Attributes.Contains(Constants.Units.RentlyLockBox))
                            tmpEntity[Constants.ProjectTasks.RentlyLockBox] = unitEntity.GetAttributeValue<string>(Constants.Units.RentlyLockBox);
                        //Access Notes
                        if (unitEntity.Attributes.Contains(Constants.Units.RentlyLockBoxNote))
                            tmpEntity[Constants.ProjectTasks.RentlyLockBoxNote] = unitEntity.GetAttributeValue<string>(Constants.Units.RentlyLockBoxNote);

                        orgRequestCollection.Add(new UpdateRequest() { Target = tmpEntity });
                        cnt++;
                        if (cnt > 998)
                        {
                            CommonMethods.PerformExecuteMultipleRequest(service, orgRequestCollection);
                            orgRequestCollection = new OrganizationRequestCollection();
                            cnt = 0;
                        }
                    }
                    if (cnt > 0)
                        CommonMethods.PerformExecuteMultipleRequest(service, orgRequestCollection);
                }
            }
            catch (Exception e)
            {
                throw new InvalidPluginExecutionException(e.Message);
            }
        }
    }
}
