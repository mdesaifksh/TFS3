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
                        bool isUpdateRequired = false;
                        //Access Notes
                        if (unitEntity.Attributes.Contains(Constants.Units.AccessNotes))
                        {
                            if (!string.IsNullOrEmpty(unitEntity.GetAttributeValue<string>(Constants.Units.AccessNotes)))
                            {
                                if (projectTaskEntity.Attributes.Contains(Constants.Units.AccessNotes) && unitEntity.GetAttributeValue<string>(Constants.Units.AccessNotes).Equals(projectTaskEntity.GetAttributeValue<string>(Constants.ProjectTasks.AccessNotes)))
                                {
                                    tmpEntity[Constants.ProjectTasks.AccessNotes] = unitEntity.GetAttributeValue<string>(Constants.Units.AccessNotes);
                                    isUpdateRequired = true;
                                }
                            }
                            else
                            {
                                if (projectTaskEntity.Attributes.Contains(Constants.Units.AccessNotes) && !string.IsNullOrEmpty(projectTaskEntity.GetAttributeValue<string>(Constants.ProjectTasks.AccessNotes)))
                                {
                                    tmpEntity[Constants.ProjectTasks.AccessNotes] = null;
                                    isUpdateRequired = true;
                                }
                            }
                        }
                        //LockBox Removed
                        if (unitEntity.Attributes.Contains(Constants.Units.LockBoxRemoved))
                        {
                            if (!string.IsNullOrEmpty(unitEntity.GetAttributeValue<string>(Constants.Units.LockBoxRemoved)))
                            {
                                if (projectTaskEntity.Attributes.Contains(Constants.Units.LockBoxRemoved) && unitEntity.GetAttributeValue<string>(Constants.Units.LockBoxRemoved).Equals(projectTaskEntity.GetAttributeValue<string>(Constants.ProjectTasks.LockBoxRemoved)))
                                {
                                    tmpEntity[Constants.ProjectTasks.LockBoxRemoved] = unitEntity.GetAttributeValue<DateTime>(Constants.Units.LockBoxRemoved);
                                    isUpdateRequired = true;
                                }
                            }
                            else
                            {
                                if (projectTaskEntity.Attributes.Contains(Constants.Units.LockBoxRemoved) && !string.IsNullOrEmpty(projectTaskEntity.GetAttributeValue<string>(Constants.ProjectTasks.LockBoxRemoved)))
                                {
                                    tmpEntity[Constants.ProjectTasks.LockBoxRemoved] = null;
                                    isUpdateRequired = true;
                                }
                            }
                        }
                        //Mechanical Lockbox
                        if (unitEntity.Attributes.Contains(Constants.Units.MechanicalLockBox))
                        {
                            if (!string.IsNullOrEmpty(unitEntity.GetAttributeValue<string>(Constants.Units.MechanicalLockBox)))
                            {
                                if (projectTaskEntity.Attributes.Contains(Constants.Units.MechanicalLockBox) && unitEntity.GetAttributeValue<string>(Constants.Units.MechanicalLockBox).Equals(projectTaskEntity.GetAttributeValue<string>(Constants.ProjectTasks.MechanicalLockBox)))
                                {
                                    tmpEntity[Constants.ProjectTasks.MechanicalLockBox] = unitEntity.GetAttributeValue<string>(Constants.Units.MechanicalLockBox);
                                    isUpdateRequired = true;
                                }
                            }
                            else
                            {
                                if (projectTaskEntity.Attributes.Contains(Constants.Units.MechanicalLockBox) && !string.IsNullOrEmpty(projectTaskEntity.GetAttributeValue<string>(Constants.ProjectTasks.MechanicalLockBox)))
                                {
                                    tmpEntity[Constants.ProjectTasks.MechanicalLockBox] = null;
                                    isUpdateRequired = true;
                                }
                            }
                        }
                        else
                        {
                            if (projectTaskEntity.Attributes.Contains(Constants.Units.MechanicalLockBox) && !string.IsNullOrEmpty(projectTaskEntity.GetAttributeValue<string>(Constants.ProjectTasks.MechanicalLockBox)))
                            {
                                tmpEntity[Constants.ProjectTasks.MechanicalLockBox] = null;
                                isUpdateRequired = true;
                            }
                        }
                        //Mechanical Lockbox Note
                        if (unitEntity.Attributes.Contains(Constants.Units.MechanicalLockBoxNote))
                        {
                            if (!string.IsNullOrEmpty(unitEntity.GetAttributeValue<string>(Constants.Units.MechanicalLockBoxNote)))
                            {
                                if (projectTaskEntity.Attributes.Contains(Constants.Units.MechanicalLockBoxNote) &&
                                    unitEntity.GetAttributeValue<string>(Constants.Units.MechanicalLockBoxNote).Equals(projectTaskEntity.GetAttributeValue<string>(Constants.ProjectTasks.MechanicalLockBoxNote)))
                                {
                                    tmpEntity[Constants.ProjectTasks.MechanicalLockBoxNote] = unitEntity.GetAttributeValue<string>(Constants.Units.MechanicalLockBoxNote);
                                    isUpdateRequired = true;
                                }
                            }
                            else
                            {
                                if (projectTaskEntity.Attributes.Contains(Constants.Units.MechanicalLockBoxNote) && !string.IsNullOrEmpty(projectTaskEntity.GetAttributeValue<string>(Constants.ProjectTasks.MechanicalLockBoxNote)))
                                {
                                    tmpEntity[Constants.ProjectTasks.MechanicalLockBoxNote] = null;
                                    isUpdateRequired = true;
                                }
                            }
                        }
                        else
                        {
                            if (projectTaskEntity.Attributes.Contains(Constants.Units.MechanicalLockBoxNote) && !string.IsNullOrEmpty(projectTaskEntity.GetAttributeValue<string>(Constants.ProjectTasks.MechanicalLockBoxNote)))
                            {
                                tmpEntity[Constants.ProjectTasks.MechanicalLockBoxNote] = null;
                                isUpdateRequired = true;
                            }
                        }
                        //Property Gate Code
                        if (unitEntity.Attributes.Contains(Constants.Units.PropertyGateCode))
                        {
                            if (!string.IsNullOrEmpty(unitEntity.GetAttributeValue<string>(Constants.Units.MechanicalLockBoxNote)))
                            {
                                if (projectTaskEntity.Attributes.Contains(Constants.Units.MechanicalLockBoxNote) &&
                                    unitEntity.GetAttributeValue<string>(Constants.Units.MechanicalLockBoxNote).Equals(projectTaskEntity.GetAttributeValue<string>(Constants.ProjectTasks.MechanicalLockBoxNote)))
                                {
                                    tmpEntity[Constants.ProjectTasks.PropertyGateCode] = unitEntity.GetAttributeValue<string>(Constants.Units.PropertyGateCode);
                                    isUpdateRequired = true;
                                }
                            }
                            else
                            {
                                if (projectTaskEntity.Attributes.Contains(Constants.Units.MechanicalLockBoxNote) && !string.IsNullOrEmpty(projectTaskEntity.GetAttributeValue<string>(Constants.ProjectTasks.MechanicalLockBoxNote)))
                                {
                                    tmpEntity[Constants.ProjectTasks.MechanicalLockBoxNote] = null;
                                    isUpdateRequired = true;
                                }
                            }
                        }
                        else
                        {
                            if (projectTaskEntity.Attributes.Contains(Constants.Units.MechanicalLockBoxNote) && !string.IsNullOrEmpty(projectTaskEntity.GetAttributeValue<string>(Constants.ProjectTasks.MechanicalLockBoxNote)))
                            {
                                tmpEntity[Constants.ProjectTasks.MechanicalLockBoxNote] = null;
                                isUpdateRequired = true;
                            }
                        }
                        //Rently Lockbox
                        if (unitEntity.Attributes.Contains(Constants.Units.RentlyLockBox))
                        {
                            if (!string.IsNullOrEmpty(unitEntity.GetAttributeValue<string>(Constants.Units.RentlyLockBox)))
                            {
                                if (projectTaskEntity.Attributes.Contains(Constants.Units.RentlyLockBox) &&
                                    unitEntity.GetAttributeValue<string>(Constants.Units.RentlyLockBox).Equals(projectTaskEntity.GetAttributeValue<string>(Constants.ProjectTasks.RentlyLockBox)))
                                {
                                    tmpEntity[Constants.ProjectTasks.RentlyLockBox] = unitEntity.GetAttributeValue<string>(Constants.Units.RentlyLockBox);
                                    isUpdateRequired = true;
                                }
                            }
                            else
                            {
                                if (projectTaskEntity.Attributes.Contains(Constants.Units.RentlyLockBox) && !string.IsNullOrEmpty(projectTaskEntity.GetAttributeValue<string>(Constants.ProjectTasks.RentlyLockBox)))
                                {
                                    tmpEntity[Constants.ProjectTasks.RentlyLockBox] = null;
                                    isUpdateRequired = true;
                                }
                            }
                        }
                        else
                        {
                            if (projectTaskEntity.Attributes.Contains(Constants.Units.RentlyLockBox) && !string.IsNullOrEmpty(projectTaskEntity.GetAttributeValue<string>(Constants.ProjectTasks.RentlyLockBox)))
                            {
                                tmpEntity[Constants.ProjectTasks.RentlyLockBox] = null;
                                isUpdateRequired = true;
                            }
                        }                        
                        //Rently Lock Box Note
                        if (unitEntity.Attributes.Contains(Constants.Units.RentlyLockBoxNote))
                        {
                            if (!string.IsNullOrEmpty(unitEntity.GetAttributeValue<string>(Constants.Units.RentlyLockBoxNote)))
                            {
                                if (projectTaskEntity.Attributes.Contains(Constants.Units.RentlyLockBoxNote) &&
                                    unitEntity.GetAttributeValue<string>(Constants.Units.RentlyLockBoxNote).Equals(projectTaskEntity.GetAttributeValue<string>(Constants.ProjectTasks.RentlyLockBoxNote)))
                                {
                                    tmpEntity[Constants.ProjectTasks.RentlyLockBoxNote] = unitEntity.GetAttributeValue<string>(Constants.Units.RentlyLockBoxNote);
                                    isUpdateRequired = true;
                                }
                            }
                            else
                            {
                                if (projectTaskEntity.Attributes.Contains(Constants.Units.RentlyLockBoxNote) && !string.IsNullOrEmpty(projectTaskEntity.GetAttributeValue<string>(Constants.ProjectTasks.RentlyLockBoxNote)))
                                {
                                    tmpEntity[Constants.ProjectTasks.RentlyLockBoxNote] = null;
                                    isUpdateRequired = true;
                                }
                            }
                        }
                        else
                        {
                            if (projectTaskEntity.Attributes.Contains(Constants.Units.RentlyLockBoxNote) && !string.IsNullOrEmpty(projectTaskEntity.GetAttributeValue<string>(Constants.ProjectTasks.RentlyLockBoxNote)))
                            {
                                tmpEntity[Constants.ProjectTasks.RentlyLockBoxNote] = null;
                                isUpdateRequired = true;
                            }
                        }
                        if (isUpdateRequired)
                            orgRequestCollection.Add(new UpdateRequest() { Target = tmpEntity });
                        else
                        {
                            tracer.Trace($"No updates available to update Unit Info. Existing loop. and plugin.");
                            break;
                        }
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
