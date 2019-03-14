using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;

namespace FirstKey.D365.Plug_Ins
{
    public class ProjectTaskPreOperation : IPlugin
    {
        #region Secure/Unsecure Configuration Setup
        private string _secureConfig = null;
        private string _unsecureConfig = null;

        public ProjectTaskPreOperation(string unsecureConfig, string secureConfig)
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

            try
            {
                Entity projectTaskEntity = null;
                switch (context.MessageName)
                {
                    case Constants.Messages.Create:
                        if (context.InputParameters[Constants.TARGET] is Entity)
                        {
                            if (((Entity)context.InputParameters[Constants.TARGET]).LogicalName != Constants.ProjectTasks.LogicalName)
                                return;
                            else
                                projectTaskEntity = context.InputParameters[Constants.TARGET] as Entity;
                        }
                        break;
                }

                if (projectTaskEntity is Entity && projectTaskEntity.Attributes.Contains(Constants.ProjectTasks.Project) && projectTaskEntity.Attributes.Contains(Constants.ProjectTasks.WBSID))
                {

                    tracer.Trace($"Project Task contains Project as well as WBSID.");
                    Entity projectEntity = service.Retrieve(projectTaskEntity.GetAttributeValue<EntityReference>(Constants.ProjectTasks.Project).LogicalName,
                        projectTaskEntity.GetAttributeValue<EntityReference>(Constants.ProjectTasks.Project).Id, new ColumnSet(true));

                    if (projectEntity is Entity && projectEntity.Attributes.Contains(Constants.Projects.ProjectTemplate) && projectEntity.Attributes.Contains(Constants.Projects.Unit))
                    {
                        int timeZoneCode = CommonMethods.RetrieveCurrentUsersSettings(service);

                        DateTime unitStatusChange = DateTime.Now;
                        if (projectEntity.Attributes.Contains(Constants.Projects.StartDate))
                            unitStatusChange = CommonMethods.RetrieveLocalTimeFromUTCTime(service, timeZoneCode, projectEntity.GetAttributeValue<DateTime>(Constants.Projects.StartDate));

                        tracer.Trace($"Project contains Project Template.");
                        Entity projectTemplateTaskEntity = RetrieveProjectTemplateTask(service, tracer, projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectTemplate),
                            projectTaskEntity.GetAttributeValue<string>(Constants.ProjectTasks.WBSID));

                        if (projectTemplateTaskEntity is Entity)
                        {
                            projectTaskEntity[Constants.ProjectTasks.TaskIdentifier] = projectTemplateTaskEntity.GetAttributeValue<EntityReference>(Constants.ProjectTasks.TaskIdentifier);

                        }
                        else if (projectTaskEntity.Attributes.Contains(Constants.ProjectTasks.TaskIdentifier) && string.IsNullOrEmpty(projectTaskEntity.GetAttributeValue<EntityReference>(Constants.ProjectTasks.TaskIdentifier).Name))
                        {
                            Entity taskIdentiEntity = service.Retrieve(projectTaskEntity.GetAttributeValue<EntityReference>(Constants.ProjectTasks.TaskIdentifier).LogicalName, projectTaskEntity.GetAttributeValue<EntityReference>(Constants.ProjectTasks.TaskIdentifier).Id, new ColumnSet(true));
                            if (taskIdentiEntity is Entity && taskIdentiEntity.Attributes.Contains(Constants.TaskIdentifiers.Name))
                                projectTaskEntity[Constants.ProjectTasks.TaskIdentifier] = new EntityReference() { Name = taskIdentiEntity.GetAttributeValue<string>(Constants.TaskIdentifiers.Name), Id = taskIdentiEntity.Id, LogicalName = taskIdentiEntity.LogicalName};
                        }

                        //Map Property Information...
                        Entity unitEntity = service.Retrieve(projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.Unit).LogicalName, projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.Unit).Id, new ColumnSet(true));
                        if (unitEntity is Entity)
                        {
                            tracer.Trace($"Unit Found. {unitEntity.Id.ToString()}");
                            //if (unitEntity.Attributes.Contains(Constants.Units.MoveOutDate))
                            //    moveOutDate = CommonMethods.RetrieveLocalTimeFromUTCTime(service, timeZoneCode, unitEntity.GetAttributeValue<DateTime>(Constants.Units.MoveOutDate));

                            //Map Unit
                            projectTaskEntity[Constants.ProjectTasks.UnitId] = unitEntity.ToEntityReference();

                            //Access Notes
                            if (unitEntity.Attributes.Contains(Constants.Units.AccessNotes))
                                projectTaskEntity[Constants.ProjectTasks.AccessNotes] = unitEntity.GetAttributeValue<string>(Constants.Units.AccessNotes);
                            //LockBox Removed
                            if (unitEntity.Attributes.Contains(Constants.Units.LockBoxRemoved))
                                projectTaskEntity[Constants.ProjectTasks.LockBoxRemoved] = unitEntity.GetAttributeValue<DateTime>(Constants.Units.LockBoxRemoved);
                            //Mechanical Lockbox
                            if (unitEntity.Attributes.Contains(Constants.Units.MechanicalLockBox))
                                projectTaskEntity[Constants.ProjectTasks.MechanicalLockBox] = unitEntity.GetAttributeValue<string>(Constants.Units.MechanicalLockBox);
                            //Mechanical Lockbox Note
                            if (unitEntity.Attributes.Contains(Constants.Units.MechanicalLockBoxNote))
                                projectTaskEntity[Constants.ProjectTasks.MechanicalLockBoxNote] = unitEntity.GetAttributeValue<string>(Constants.Units.MechanicalLockBoxNote);
                            //Property Gate Code
                            if (unitEntity.Attributes.Contains(Constants.Units.PropertyGateCode))
                                projectTaskEntity[Constants.ProjectTasks.PropertyGateCode] = unitEntity.GetAttributeValue<string>(Constants.Units.PropertyGateCode);
                            //Rently Lockbox
                            if (unitEntity.Attributes.Contains(Constants.Units.RentlyLockBox))
                                projectTaskEntity[Constants.ProjectTasks.RentlyLockBox] = unitEntity.GetAttributeValue<string>(Constants.Units.RentlyLockBox);
                            //Access Notes
                            if (unitEntity.Attributes.Contains(Constants.Units.RentlyLockBoxNote))
                                projectTaskEntity[Constants.ProjectTasks.RentlyLockBoxNote] = unitEntity.GetAttributeValue<string>(Constants.Units.RentlyLockBoxNote);

                            if (projectTaskEntity.Attributes.Contains(Constants.ProjectTasks.TaskIdentifier) && !string.IsNullOrEmpty(projectTaskEntity.GetAttributeValue<EntityReference>(Constants.ProjectTasks.TaskIdentifier).Name))
                            {
                                tracer.Trace($"Project Task with Task Identifier ID : {projectTaskEntity.GetAttributeValue<EntityReference>(Constants.ProjectTasks.TaskIdentifier).Id.ToString()}.");
                                tracer.Trace($"Project Task with Task Identifier : {projectTaskEntity.GetAttributeValue<EntityReference>(Constants.ProjectTasks.TaskIdentifier).Name}.");
                                if (projectTaskEntity.GetAttributeValue<EntityReference>(Constants.ProjectTasks.TaskIdentifier).Name.StartsWith("TurnAround"))
                                {
                                    DateTime scheduledStartDate, scheduledEndDate;
                                    DateTime Date2 = unitStatusChange;
                                    bool date2Found = true;
                                    if (projectEntity.Attributes.Contains(Constants.Projects.InitialJSONDataPayLoad))
                                    {
                                        try
                                        {
                                            DataPayLoad dataPayLoad = CommonMethods.Deserialize<DataPayLoad>(projectEntity.GetAttributeValue<string>(Constants.Projects.InitialJSONDataPayLoad));
                                            if (dataPayLoad is DataPayLoad)
                                            {
                                                tracer.Trace($"Initial Data PayLoad . {projectEntity.GetAttributeValue<string>(Constants.Projects.InitialJSONDataPayLoad)}");
                                                if (!DateTime.TryParse(dataPayLoad.Date2, out Date2))
                                                {
                                                    Date2 = unitStatusChange;
                                                    date2Found = false;
                                                }
                                                tracer.Trace($"Initial Data PayLoad Date 2. {Date2.ToString()}");
                                            }
                                            else
                                            {
                                                Date2 = unitStatusChange;
                                                date2Found = false;
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            tracer.Trace($"Error Thrown during Deserialing Initial Data PayLoad :  {projectEntity.GetAttributeValue<string>(Constants.Projects.InitialJSONDataPayLoad)} {Environment.NewLine} Error : {ex.Message}");
                                            date2Found = false;
                                        }
                                    }
                                    //DateTime SchStartDate = CommonMethods.RetrieveLocalTimeFromUTCTime(service, timeZoneCode, projectTaskEntity.GetAttributeValue<DateTime>(Constants.ProjectTasks.ScheduledStart));

                                    Date2 = CommonMethods.RetrieveLocalTimeFromUTCTime(service, timeZoneCode, Date2);
                                    Date2 = Date2.ChangeTime(6, 0, 0, 0);
                                    tracer.Trace($"Local Date 2. {Date2.ToString()}");

                                    switch (projectTaskEntity.GetAttributeValue<string>(Constants.ProjectTasks.WBSID))
                                    {
                                        case "1":           // RESIDENT NOTICE TO MOVE OUT RECEIVED
                                            projectTaskEntity[Constants.ProjectTasks.ActualStart] = unitStatusChange;
                                            projectTaskEntity[Constants.ProjectTasks.ActualEnd] = unitStatusChange;
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledStart] = unitStatusChange;
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledEnd] = unitStatusChange;
                                            projectTaskEntity[Constants.ProjectTasks.Progress] = new decimal(100);
                                            projectTaskEntity[Constants.Status.StatusCode] = new OptionSetValue(963850001);
                                            break;
                                        case "2":           //TurnAround : ASSIGN PROJECT MANAGER
                                            scheduledEndDate = (Date2.AddDays(-37) > unitStatusChange) ? Date2.AddDays(-37) : ((Date2 > unitStatusChange) ? Date2 : unitStatusChange);
                                            //Date2 = (Date2.AddDays(-37) > DateTime.Now) ? ((Date2.AddDays(-37) > unitStatusChange) ? Date2.AddDays(-37) : unitStatusChange.AddHours(24)) : ((unitStatusChange > DateTime.Now) ? unitStatusChange.AddHours(24) : DateTime.Now);
                                            tracer.Trace($"Scheduled Start Date : {unitStatusChange}");
                                            tracer.Trace($"Scheduled End Date : {scheduledEndDate}");
                                            projectTaskEntity[Constants.ProjectTasks.ActualStart] = unitStatusChange;
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledEnd] = Date2;
                                            projectTaskEntity[Constants.ProjectTasks.Progress] = new decimal(1);
                                            projectTaskEntity[Constants.Status.StatusCode] = new OptionSetValue(963850000);
                                            break;
                                        case "3":           //CORPORATE RENEWALS
                                            tracer.Trace($"Scheduled Start Date : {unitStatusChange}");
                                            //tracer.Trace($"Scheduled End Date : {scheduledEndDate}");
                                            projectTaskEntity[Constants.ProjectTasks.ActualStart] = unitStatusChange;
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledStart] = unitStatusChange;
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledEnd] = (Date2 > unitStatusChange) ? Date2 : unitStatusChange;
                                            projectTaskEntity[Constants.ProjectTasks.Progress] = new decimal(1);
                                            projectTaskEntity[Constants.Status.StatusCode] = new OptionSetValue(963850000);
                                            break;
                                        case "4":           //MARKET SCHEDULES PRE-MOVE-OUT
                                            scheduledEndDate = (Date2.AddDays(-30) > unitStatusChange) ? Date2.AddDays(-30) : ((Date2 > unitStatusChange) ? Date2 : unitStatusChange);
                                            //Date2 = (Date2.AddDays(-30) > DateTime.Now) ? ((Date2.AddDays(-30) > unitStatusChange) ? Date2.AddDays(-30) : unitStatusChange.AddHours(24)) : ((unitStatusChange > DateTime.Now) ? unitStatusChange.AddHours(24) : DateTime.Now.AddHours(24));
                                            tracer.Trace($"Scheduled Start Date : {unitStatusChange}");
                                            tracer.Trace($"Scheduled End Date : {scheduledEndDate}");
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledStart] = unitStatusChange;
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledEnd] = scheduledEndDate;
                                            break;
                                        case "5":           //PRE-MOVE-OUT INSPECTION
                                            scheduledEndDate = (Date2.AddDays(-1) > unitStatusChange) ? Date2.AddDays(-1) : ((Date2 > unitStatusChange) ? Date2 : unitStatusChange);
                                            scheduledStartDate = (Date2.AddDays(-30) > unitStatusChange) ? Date2.AddDays(-30) : ((Date2 > unitStatusChange) ? Date2 : unitStatusChange);
                                            //scheduledStartDate = (Date2.AddDays(-30) > DateTime.Now) ? ((Date2.AddDays(-30) > unitStatusChange) ? Date2.AddDays(-30) : unitStatusChange.AddHours(-24)) : ((unitStatusChange > DateTime.Now) ? unitStatusChange.AddHours(-24) : DateTime.Now.AddHours(-24));
                                            //Date2 = (Date2.AddDays(-30) > DateTime.Now) ? ((Date2.AddDays(-30) > unitStatusChange) ? Date2.AddDays(-30) : unitStatusChange.AddHours(24)) : ((unitStatusChange > DateTime.Now) ? unitStatusChange.AddHours(24) : DateTime.Now.AddHours(24));
                                            tracer.Trace($"Scheduled Start Date : {scheduledStartDate}");
                                            tracer.Trace($"Scheduled End Date : {scheduledEndDate}");
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledStart] = scheduledStartDate;
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledEnd] = scheduledEndDate;
                                            break;
                                        case "6":           //MOVE-OUT INSPECTION
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledStart] = Date2;
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledEnd] = Date2;
                                            break;
                                        case "7":           //BUDGET START
                                            tracer.Trace($"BUDGET START Date 2. {Date2.ToString()}");
                                            tracer.Trace($"BUDGET START ScheduledStart. {Date2.ToString()}");
                                            tracer.Trace($"BUDGET START ScheduledEnd. {Date2.AddHours(24).ToString()}");

                                            projectTaskEntity[Constants.ProjectTasks.ScheduledStart] = Date2;
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledEnd] = Date2.AddHours(24);
                                            break;
                                        case "8":           //BUDGET APPROVAL
                                            tracer.Trace($"BUDGET APPROVAL Date 2. {Date2.ToString()}");
                                            tracer.Trace($"BUDGET APPROVAL ScheduledStart. {Date2.AddSeconds(1).ToString()}");
                                            tracer.Trace($"BUDGET APPROVAL ScheduledEnd. {Date2.AddHours(24).ToString()}");

                                            projectTaskEntity[Constants.ProjectTasks.ScheduledStart] = Date2.AddSeconds(1);
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledEnd] = Date2.AddHours(24);
                                            break;
                                        case "9":           //JOB ASSIGNMENT TO VENDOR(S) IN CONTRACT CREATOR
                                            tracer.Trace($"JOB ASSIGNMENT TO VENDOR(S) IN CONTRACT CREATOR Date 2. {Date2.ToString()}");
                                            tracer.Trace($"JOB ASSIGNMENT TO VENDOR(S) IN CONTRACT CREATOR ScheduledStart. {Date2.AddSeconds(2).ToString()}");
                                            tracer.Trace($"JOB ASSIGNMENT TO VENDOR(S) IN CONTRACT CREATOR ScheduledEnd. {Date2.AddHours(24).ToString()}");

                                            projectTaskEntity[Constants.ProjectTasks.ScheduledStart] = Date2.AddSeconds(2);
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledEnd] = Date2.AddHours(24);
                                            break;
                                        case "10":          //JOB AND CONTRACT(S) SUBMITTED TO YARDI
                                            tracer.Trace($"JOB AND CONTRACT(S) SUBMITTED TO YARDI Date 2. {Date2.ToString()}");
                                            tracer.Trace($"JOB AND CONTRACT(S) SUBMITTED TO YARDI ScheduledStart. {Date2.AddSeconds(3).ToString()}");
                                            tracer.Trace($"JOB AND CONTRACT(S) SUBMITTED TO YARDI ScheduledEnd. {Date2.AddHours(24).ToString()}");

                                            projectTaskEntity[Constants.ProjectTasks.ScheduledStart] = Date2.AddSeconds(3);
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledEnd] = Date2.AddHours(24);
                                            break;
                                        case "11":          //VENDOR(S) SAYS JOB STARTED
                                            tracer.Trace($"VENDOR(S) SAYS JOB STARTED Date 2. {Date2.ToString()}");
                                            tracer.Trace($"VENDOR(S) SAYS JOB STARTED ScheduledStart. {Date2.AddDays(1).ToString()}");
                                            tracer.Trace($"VENDOR(S) SAYS JOB STARTED ScheduledEnd. {Date2.AddDays(2).ToString()}");

                                            projectTaskEntity[Constants.ProjectTasks.ScheduledStart] = Date2.AddDays(1);
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledEnd] = Date2.AddDays(2);
                                            break;
                                        case "12":          //WORK IN PROGRESS
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledStart] = Date2.AddDays(2);
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledEnd] = Date2.AddDays(3);
                                            break;
                                        case "13":          //VENDOR SAYS JOB?S COMPLETE
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledStart] = Date2.AddDays(3);
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledEnd] = Date2.AddDays(4);
                                            break;
                                        case "14":          //QUALITY CONTROL INSPECTION
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledStart] = Date2.AddDays(4);
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledEnd] = Date2.AddDays(5);
                                            break;
                                        case "15":          //JOB COMPLETED
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledStart] = Date2.AddDays(5);
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledEnd] = Date2.AddDays(6);
                                            break;
                                        case "16":          //HERO SHOT PICTURE
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledStart] = unitStatusChange;
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledEnd] = Date2.AddDays(6);
                                            break;
                                        case "17":          //MARKETING INSPECTION
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledStart] = Date2.AddDays(6);
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledEnd] = Date2.AddDays(7);
                                            break;
                                        case "18":          //BI-WEEKLY INSPECTION
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledStart] = Date2.AddDays(19);
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledEnd] = Date2.AddDays(20);
                                            break;
                                        case "19":          //MOVE-IN INSPECTION
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledStart] = Date2.AddDays(44);
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledEnd] = Date2.AddDays(45);
                                            break;
                                    }
                                }
                                else
                                {
                                    DateTime scheduledClosingDate = unitStatusChange, scheduledDDDate = unitStatusChange;
                                    bool schClosingDateFound = true, schDDDateFound = true;
                                    if (projectEntity.Attributes.Contains(Constants.Projects.InitialJSONDataPayLoad))
                                    {
                                        try
                                        {
                                            DataPayLoad dataPayLoad = CommonMethods.Deserialize<DataPayLoad>(projectEntity.GetAttributeValue<string>(Constants.Projects.InitialJSONDataPayLoad));
                                            if (dataPayLoad is DataPayLoad)
                                            {
                                                tracer.Trace($"Initial Data PayLoad . {projectEntity.GetAttributeValue<string>(Constants.Projects.InitialJSONDataPayLoad)}");
                                                if (!DateTime.TryParse(dataPayLoad.Date2, out scheduledClosingDate))
                                                {
                                                    scheduledClosingDate = unitStatusChange;
                                                    schClosingDateFound = false;
                                                }
                                                if (!DateTime.TryParse(dataPayLoad.Date3, out scheduledDDDate))
                                                {
                                                    scheduledDDDate = unitStatusChange;
                                                    schDDDateFound = false;
                                                }
                                            }
                                            else
                                            {
                                                scheduledClosingDate = unitStatusChange;
                                                scheduledDDDate = unitStatusChange;
                                                schClosingDateFound = false;
                                                schDDDateFound = false;
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            tracer.Trace($"Error Thrown during Deserialing Initial Data PayLoad :  {projectEntity.GetAttributeValue<string>(Constants.Projects.InitialJSONDataPayLoad)} {Environment.NewLine} Error : {ex.Message}");
                                            schClosingDateFound = false;
                                            schDDDateFound = false;
                                        }
                                    }

                                    //DateTime SchStartDate = CommonMethods.RetrieveLocalTimeFromUTCTime(service, timeZoneCode, projectTaskEntity.GetAttributeValue<DateTime>(Constants.ProjectTasks.ScheduledStart));
                                    switch (projectTaskEntity.GetAttributeValue<string>(Constants.ProjectTasks.WBSID))
                                    {
                                        case "1":           //IR : OFFER_ACCEPTED
                                            projectTaskEntity[Constants.ProjectTasks.ActualStart] = unitStatusChange;
                                            projectTaskEntity[Constants.ProjectTasks.ActualEnd] = unitStatusChange;
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledStart] = unitStatusChange;
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledEnd] = unitStatusChange;
                                            projectTaskEntity[Constants.ProjectTasks.Progress] = new decimal(100);
                                            projectTaskEntity[Constants.Status.StatusCode] = new OptionSetValue(963850001);
                                            break;
                                        case "2":           //IR : ASSIGN_PROJECT_MANAGER
                                            projectTaskEntity[Constants.ProjectTasks.ActualStart] = unitStatusChange;
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledStart] = unitStatusChange;
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledEnd] = unitStatusChange.AddDays(1);
                                            projectTaskEntity[Constants.ProjectTasks.Progress] = new decimal(1);
                                            projectTaskEntity[Constants.Status.StatusCode] = new OptionSetValue(963850000);
                                            break;
                                        case "3":           //IR : SCHEDULE_DUE_DILLIGENCE_INSPECTION
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledStart] = unitStatusChange;
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledEnd] = unitStatusChange.AddDays(2);
                                            break;
                                        case "4":           //IR : BUDGET_START
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledStart] = unitStatusChange.AddDays(2);
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledEnd] = unitStatusChange.AddDays(3);
                                            break;
                                        case "5":           //IR : BUDGET_APPROVAL
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledStart] = (schDDDateFound) ? scheduledDDDate.AddDays(-2) : unitStatusChange.AddDays(3);
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledEnd] = (schDDDateFound) ? scheduledDDDate.AddDays(-1) : unitStatusChange.AddDays(4);
                                            break;
                                        case "6":           //IR : OFFER_REJECTED_OR_APPROVAL
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledStart] = (schDDDateFound) ? scheduledDDDate.AddDays(-1) : unitStatusChange.AddDays(4);
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledEnd] = (schDDDateFound) ? scheduledDDDate : unitStatusChange.AddDays(5);
                                            break;
                                        case "7":           //IR : JOB_ASSIGNMENT_TO_VENDORS_IN_CONTRACT_CREATOR
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledStart] = (schDDDateFound) ? scheduledDDDate : unitStatusChange.AddDays(5);
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledEnd] = (schClosingDateFound) ? scheduledClosingDate.AddDays(-3) : unitStatusChange.AddDays(6);
                                            break;
                                        case "8":           //IR : CLOSE_ESCROW
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledStart] = (schClosingDateFound) ? scheduledClosingDate : unitStatusChange.AddDays(6);
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledEnd] = (schClosingDateFound) ? scheduledClosingDate : unitStatusChange.AddDays(6);
                                            break;
                                        case "9":           //IR : JOB_AND_CONTRACTS_SUBMITTED_TO_YARDI
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledStart] = (schClosingDateFound) ? scheduledClosingDate : unitStatusChange.AddDays(6);
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledEnd] = (schClosingDateFound) ? scheduledClosingDate.AddDays(1) : unitStatusChange.AddDays(7);
                                            break;
                                        case "10":           //IR : VENDORS_SAYS_JOB_STARTED
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledStart] = (schClosingDateFound) ? scheduledClosingDate.AddDays(1) : unitStatusChange.AddDays(7);
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledEnd] = (schClosingDateFound) ? scheduledClosingDate.AddDays(1) : unitStatusChange.AddDays(7);
                                            break;
                                        case "11":           //IR : WORK_IN_PROGRESS
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledStart] = (schClosingDateFound) ? scheduledClosingDate.AddDays(1) : unitStatusChange.AddDays(7);
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledEnd] = (schClosingDateFound) ? scheduledClosingDate.AddDays(2) : unitStatusChange.AddDays(8);
                                            break;
                                        case "12":           //IR : VENDOR_SAYS_JOBS_COMPLETE
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledStart] = (schClosingDateFound) ? scheduledClosingDate.AddDays(2) : unitStatusChange.AddDays(8);
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledEnd] = (schClosingDateFound) ? scheduledClosingDate.AddDays(3) : unitStatusChange.AddDays(9);
                                            break;
                                        case "13":           //IR : QUALITY_CONTROL_INSPECTION
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledStart] = (schClosingDateFound) ? scheduledClosingDate.AddDays(3) : unitStatusChange.AddDays(9);
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledEnd] = (schClosingDateFound) ? scheduledClosingDate.AddDays(4) : unitStatusChange.AddDays(10);
                                            break;
                                        case "14":           //IR : JOB_COMPLETED
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledStart] = (schClosingDateFound) ? scheduledClosingDate.AddDays(4) : unitStatusChange.AddDays(10);
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledEnd] = (schClosingDateFound) ? scheduledClosingDate.AddDays(4) : unitStatusChange.AddDays(10);
                                            break;
                                        case "15":           //IR : HERO_SHOT_PICTURE
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledStart] = unitStatusChange;
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledEnd] = (schClosingDateFound) ? scheduledClosingDate.AddDays(4) : unitStatusChange.AddDays(10);
                                            break;
                                        case "16":           //IR : MARKETING_INSPECTION
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledStart] = (schClosingDateFound) ? scheduledClosingDate.AddDays(4) : unitStatusChange.AddDays(10);
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledEnd] = (schClosingDateFound) ? scheduledClosingDate.AddDays(5) : unitStatusChange.AddDays(11);
                                            break;
                                        case "17":           //IR : BI_WEEKLY_INSPECTION
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledStart] = (schClosingDateFound) ? scheduledClosingDate.AddDays(18) : unitStatusChange.AddDays(24);
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledEnd] = (schClosingDateFound) ? scheduledClosingDate.AddDays(19) : unitStatusChange.AddDays(25);
                                            break;
                                        case "18":           //IR : MOVE_IN_INSPECTION_COMPLETED
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledStart] = (schClosingDateFound) ? scheduledClosingDate.AddDays(49) : unitStatusChange.AddDays(55);
                                            projectTaskEntity[Constants.ProjectTasks.ScheduledEnd] = (schClosingDateFound) ? scheduledClosingDate.AddDays(50) : unitStatusChange.AddDays(56);
                                            break;
                                    }
                                }
                            }

                        }
                    }
                    else
                        tracer.Trace($"Project does NOT contains Project Template.");
                }
                else
                    tracer.Trace($"Project Task does NOT contains Project or WBSID.");

                //TODO: Do stuff
            }
            catch (Exception e)
            {
                throw new InvalidPluginExecutionException(e.Message);
            }
        }

        private Entity RetrieveProjectTemplateTask(IOrganizationService service, ITracingService tracer, EntityReference projectTemplateEntityReference, string WBSID)
        {
            tracer.Trace($"Retrieve Project Template Task with WBSID : {WBSID}.");

            QueryExpression Query = new QueryExpression
            {
                EntityName = Constants.ProjectTasks.LogicalName,
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    FilterOperator = LogicalOperator.And,
                    Conditions =
                    {
                        new ConditionExpression
                        {
                            AttributeName = Constants.ProjectTasks.WBSID,
                            Operator = ConditionOperator.Equal,
                            Values = { WBSID }
                        },
                        new ConditionExpression
                        {
                            AttributeName = Constants.ProjectTasks.TaskIdentifier,
                            Operator = ConditionOperator.NotNull
                        },
                        new ConditionExpression
                        {
                            AttributeName = Constants.ProjectTasks.Project,
                            Operator = ConditionOperator.Equal,
                            Values = { projectTemplateEntityReference.Id }
                        }
                    }
                },
                TopCount = 1
            };

            EntityCollection projectTaskEntityCollection = service.RetrieveMultiple(Query);
            if (projectTaskEntityCollection.Entities.Count > 0)
            {
                tracer.Trace($"Project Template Task found.");
                return projectTaskEntityCollection.Entities[0];
            }
            else
            {
                tracer.Trace($"Project Template Task NOT found.");
                return null;
            }
        }


    }
}