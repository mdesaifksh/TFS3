
namespace FirstKey.D365.Plug_Ins
{
    public class Constants
    {
        public const string TARGET = "Target";
        public const string POST_IMAGE = "PostImage";
        public const string ENTITY_REFERENCE = "entityReference";
        public const string IS_SUCCESS = "IsSuccess";
        public const string ERROR_MESSAGE = "ErrorMessage";
        public const string IR_BPF_ID = "DF604C07-AE6B-419D-AEA3-C3B1E402D231";
        public const string TURN_BPF_ID = "D42A0F2F-F98B-4948-92B9-73681E4CCD53";

        public class Messages
        {
            public const string Create = "Create";
            public const string Update = "Update";
            public const string Win = "Win";
            public const string SetState = "SetState";
            public const string SetStateDynamic = "SetStateDynamicEntity";
        }

        public class Status
        {
            public const string StatusCode = "statuscode";
            public const string StateCode = "statecode";
        }

        public class BookableResources {
            //bookableresource
            public const string LogicalName = "bookableresource";
            public const string PrimaryKey = "bookableresourceid";

            public const string AccountID = "accountid";
            public const string ContactID = "contactid";
            public const string HourlyRate = "msdyn_hourlyrate";
            /// <summary>
            /// Value: 1, Label: Generic
            /// Value: 2, Label: Contact
            /// Value: 3, Label: User
            /// Value: 4, Label: Equipment
            /// Value: 5, Label: Account
            /// Value: 6, Label: Crew
            /// Value: 7, Label: Facility
            /// Value: 8, Label: Pool
            /// </summary>
            public const string ResourceType = "resourcetype";
            public const string TimeZone = "timezone";
            public const string Name = "name";
            public const string OwnerID = "ownerid";
        }

        public class ProjectTeams
        {
            public const string LogicalName = "msdyn_projectteam";
            public const string PrimaryKey = "msdyn_projectteamid";

            public const string BookableResource = "msdyn_bookableresourceid";
            public const string Project = "msdyn_project";
            public const string OwnerID = "ownerid";
            public const string Name = "msdyn_name";
        }

        public class Vendors
        {
            public const string LogicalName = "account";
            public const string PrimaryKey = "accountid";

            public const string Name = "name";
            public const string AccountCode = "po_accountcode";
        }


            public class Projects
        { 
            public const string LogicalName = "msdyn_project";
            public const string PrimaryKey = "msdyn_projectid";

            public const string ProjectTemplate = "msdyn_projecttemplate";
            public const string Unit = "fkh_unitid";
            public const string Subject = "msdyn_subject";
            public const string StartDate = "msdyn_scheduledstart";
            public const string DueDate = "msdyn_scheduledend";
            public const string ActualStartDate = "msdyn_actualstart";
            public const string ActualEndDate = "msdyn_actualend";
            public const string ProjectManager = "msdyn_projectmanager";
            public const string RenowalkURL = "fkh_renowalkurl";
            public const string RenowalkID = "fkh_renowalkid";
            public const string Job = "fkh_jobid";
            public const string OriginalBudget = "fkh_originalbudget";
            public const string ChangeOrder = "fkh_changeorder";
            public const string TotalInvoiced = "fkh_totalinvoiced";
            public const string Process = "processid";
            public const string RevisedCompletionDate = "fkh_revisedjobcompletiondate";
            public const string RevisedStartDate = "fkh_revisedjobstartdate";
            public const string InitialJSONDataPayLoad = "fkh_initialjsondatapayload";
            public const string CurrentResidentMoveOutDate = "fkh_currentresidentmoveoutdate";
            public const string ActualJobStartDate = "fkh_actualjobstartdate";
            public const string ActualJobEndDate = "fkh_actualjobenddate";
            public const string ScheduledJobStartDate = "fkh_estimatedjobstartdate";
            public const string ScheduledJobCompletionDate = "fkh_estimatedjobcompletiondate";
        }

        public class ProjectTasks
        {
            public const string LogicalName = "msdyn_projecttask";
            public const string PrimaryKey = "msdyn_projecttaskid";

            public const string Project = "msdyn_project";
            public const string TaskIdentifier = "fkh_taskidentifierid";
            public const string Subject = "msdyn_subject";
            public const string WBSID = "msdyn_wbsid";
            public const string ParentTask = "msdyn_parenttask";
            public const string ScheduledStart = "msdyn_scheduledstart";
            public const string ScheduledEnd = "msdyn_scheduledend";
            public const string ActualStart = "msdyn_actualstart";
            public const string ActualEnd = "msdyn_actualend";
            public const string ActualDurationInMinutes = "msdyn_actualdurationminutes";
            public const string Progress = "msdyn_progress";

            public const string UnitId = "fkh_unitid";
            public const string AccessNotes = "fkh_accessnotes";
            public const string LockBoxRemoved = "fkh_lockboxremoved";
            public const string MechanicalLockBox = "fkh_mechanicallockbox";
            public const string MechanicalLockBoxNote = "fkh_mechanicallockboxnote";
            public const string PropertyGateCode = "fkh_propertygatecode";
            public const string RentlyLockBox = "fkh_rentlylockbox";
            public const string RentlyLockBoxNote = "fkh_rentlylockboxnote";
            public const string Owner = "ownerid";
            public const string Sequence = "fkh_sequence";
            //Entity Reference to Project Team
            public const string AssignedTeamMembers = "msdyn_assignedteammembers";
            public const string ScheduledDurationMinutes = "msdyn_scheduleddurationminutes";
            public const string CurrentResidenceMoveOutDate = "fkh_currentresidentmoveoutdate";
            public const string ContractID = "fkh_contractid";
        }

        public class ProjectTasksDependencies
        {
            public const string LogicalName = "msdyn_projecttaskdependency";
            public const string PrimaryKey = "msdyn_projecttaskdependencyid";

            /// <summary>
            /// Value: 192350000, Label: Finish-to-Start
            /// Value: 192350001, Label: Start-to-Start
            /// Value: 192350002, Label: Finish-to-Finish
            /// Value: 192350004, Label: Start-to-Finish
            /// </summary>
            public const string LinkType = "msdyn_linktype";
            public const string PredecessorTask = "msdyn_predecessortask";
            public const string SuccessorTask = "msdyn_successortask";
            public const string Project = "msdyn_project";

        }

            public class TaskIdentifiers
        {
            public const string LogicalName = "fkh_taskidentifier";
            public const string PrimaryKey = "fkh_taskidentifierid";

            public const string IdentifierNumber = "fkh_identifiernumber";
            public const string Name = "fkh_name";
            public const string ProjectTemplateId = "fkh_projecttemplateid";
            public const string WBSID = "fkh_wbsid";
        }

        public class Jobs
        {
            public const string LogicalName = "fkh_job";
            public const string PrimaryKey = "fkh_jobid";

            public const string Unit = "fkh_unit";
            public const string JobAmount = "fkh_jobamount";
            public const string RenowalkID = "fkh_renowalkid";
            /// <summary>
            /// 963850004 - Contract Created
            /// 963850000 - Accepted
            /// 963850001 - Pending
            /// 963850002 - Rejected
            /// 963850003 - Sent To Yardi
            /// </summary>
            public const string JobStatus = "fkh_jobstatus";
        }

        public class JobCategories
        {
            public const string LogicalName = "fkh_jobcategory";
            public const string PrimaryKey = "fkh_jobcategoryid";

            public const string GLCode = "fkh_glcode";
        }

        public class JobVendors
        {
            public const string LogicalName = "fkh_jobvendor";
            public const string PrimaryKey = "fkh_jobvendorid";

            public const string StartDate = "fkh_startdate";
            public const string EndDate = "fkh_enddate";
            public const string JobID = "fkh_job_jobvendorinid";
            public const string VendorTotal = "fkh_vendortotal";
            public const string VendorID = "fkh_account_jobvendorinid";
        }

        public static class Emails
        {
            public const string LogicalName = "email";
            public const string PrimaryKey = "activityid";

            public const string To = "to";
            public const string From = "from";
            public const string DirectionCode = "directioncode";
            public const string RegardingObject = "regardingobjectid";
            public const string Subject = "subject";
            public const string Description = "description";
        }

        public class SystemUsers
        {
            public const string LogicalName = "systemuser";
            public const string PrimaryKey = "systemuserid";

            public const string PrimaryEmail = "internalemailaddress";
            public const string UserName = "domainname";
        }

        public class Units
        {
            public const string LogicalName = "po_unit";
            public const string PrimaryKey = "po_unitid";

            public const string UnitId = "po_unitidnum";
            public const string Name = "po_name";
            public const string LeaseEnd = "po_leaseend";
            public const string MoveInDate = "po_unitmoveindate";
            public const string MoveOutDate = "po_unitmoveoutdate";
            public const string SFCode = "fkh_sfcode";
            public const string AccessNotes = "fkh_accessnotes";
            public const string LockBoxRemoved = "po_lockboxremoved";
            public const string MechanicalLockBox = "po_mechanicallockbox";
            public const string MechanicalLockBoxNote = "po_mechanicallockboxnote";
            public const string PropertyGateCode = "po_propertygatecode";
            public const string RentlyLockBox = "po_rentlylockbox";
            public const string RentlyLockBoxNote = "po_rentlylockboxnote";
            public const string UnitAddressLine1 = "po_unitaddline1";
            public const string UnitSyncToMobile = "fkh_synctomobile";
            public const string ScheduledAcquisitionDate = "fkh_scheduledacquisitiondate";
            public const string Market = "po_unitmarket";

        }


        public class AzureIntegrationCalls
        {
            public const string LogicalName = "fkh_azureintegrationcall";
            public const string PrimaryKey = "fkh_azureintegrationcallid";

            public const string EventData = "fkh_eventdata";
            /// <summary>
            /// Incoming - false
            /// Outgoing - true
            /// </summary>
            public const string Direction = "fkh_direction";
            public const string EventName = "fkh_name";
            /// <summary>
            /// To Be Processed - 1
            /// Completed - Successfully - 963850000
            /// Completed - Errors - 963850001
            /// Completed - Failed - 963850002
            /// </summary>
            public const string StatusCode = "statuscode";
            public const string ErrorDetails = "fkh_errordetails";
        }

        public class Appointments
        {
            public const string LogicalName = "appointment";
            public const string PrimaryKey = "activityid";

            public const string Subject = "subject";
            public const string Regarding = "regardingobjectid";
            public const string ScheduledStart = "scheduledstart";
            public const string ScheduledEnd = "scheduledend";
            public const string CreatedOn = "createdon";
        }

        public class ChangeOrders
        {
            public const string LogicalName = "fkh_changeorder";
            public const string PrimaryKey = "fkh_changeorderid";

            public const string Name = "fkh_name";
            public const string ProjectID = "fkh_projectid";
            public const string TotalAmount = "fkh_totalamount";
            public const string Reason = "fkh_reasons";
            public const string Requestor = "fkh_requestorid";
            /// <summary>
            /// Value: 963850000, Label: 0 - 1500
            /// Value: 963850001, Label: 1500 - 2500
            /// Value: 963850002, Label: 2500 - 10000
            /// Value: 963850003, Label: 10000 - 50000
            /// Value: 963850004, Label: 50000 - 100000
            /// Value: 963850005, Label: >100000
            /// </summary>
            public const string PendingApprovalLevel= "fkh_pendingapprovallevel";
            public const string Revision = "fkh_revision";
            public const string Unit = "fkh_unitid";
            public const string Currency = "transactioncurrencyid";
        }

        public class ChangeOrderItems
        {
            public const string LogicalName = "fkh_changeorderitem";
            public const string PrimaryKey = "fkh_changeorderitemid";

            public const string Name = "fkh_name";
            public const string ChangeOrder = "fkh_changeorderid";
            public const string Amount = "fkh_amount";
            public const string Vendor = "fkh_vendorid";
            public const string JobCategory = "fkh_jobcategoryid";
            public const string Description = "fkh_description";

        }

        public class ChangeOrderApprovers
        {
            public const string LogicalName = "fkh_changeorderapproval";
            public const string PrimaryKey = "fkh_changeorderapprovalid";

            public const string Name = "fkh_name";
            public const string ChangeOrder = "fkh_changeorderid";
            public const string Revision = "fkh_revision";
            /// <summary>
            /// Entity Reference of Budget Approver
            /// </summary>
            public const string BudgetApproverID = "fkh_budgetapproverid";
            public const string Comment = "fkh_comments";

        }

        public class BudgetApprovers
        {
            public const string LogicalName = "fkh_budgetapprover";
            public const string PrimaryKey = "fkh_budgetapproverid";

            /// <summary>
            /// Value: 963850000, Label: 0 - 1500
            /// Value: 963850001, Label: 1500 - 2500
            /// Value: 963850002, Label: 2500 - 10000
            /// Value: 963850003, Label: 10000 - 50000
            /// Value: 963850004, Label: 50000 - 100000
            /// Value: 963850005, Label: >100000
            /// </summary>
            public const string Level = "fkh_level";
            /// <summary>
            /// Value: 936710000, Label: Atlanta
            /// Value: 936710023, Label: Birmingham
            /// Value: 936710024, Label: California
            /// Value: 936710025, Label: Charleston
            /// Value: 936710015, Label: Charlotte
            /// Value: 936710001, Label: Chicago
            /// Value: 936710003, Label: Cincinnati
            /// Value: 936710004, Label: Columbus
            /// Value: 936710017, Label: Dallas
            /// Value: 936710026, Label: Florida
            /// Value: 936710006, Label: Ft Myers
            /// Value: 936710029, Label: Greensboro
            /// Value: 936710016, Label: Houston
            /// Value: 936710002, Label: Indianapolis
            /// Value: 936710007, Label: Jacksonville
            /// Value: 936710010, Label: Kansas City
            /// Value: 936710013, Label: Las Vegas
            /// Value: 936710020, Label: Louisville
            /// Value: 936710012, Label: Memphis
            /// Value: 936710005, Label: Miami
            /// Value: 936710027, Label: Minneapolis
            /// Value: 936710022, Label: NA
            /// Value: 936710008, Label: Orlando
            /// Value: 936710028, Label: Pennsylvania
            /// Value: 936710014, Label: Phoenix
            /// Value: 936710030, Label: Pittsburgh
            /// Value: 936710019, Label: Raleigh
            /// Value: 936710018, Label: San Antonio
            /// Value: 936710011, Label: St Louis
            /// Value: 936710009, Label: Tampa
            /// Value: 936710021, Label: Winston-Salem            
            /// </summary>
            public const string Market = "fkh_market";
            public const string ApproverID = "fkh_approverid";


        }

        public static class CustomActionParam
        {
            public const string IsSuccess = "IsSuccess";
            public const string ErrorMessage = "ErrorMessage";
            public const string Revision = "Revision";
            public const string ServerUrl = "ServerUrl";
            public const string Reason = "Reason";
        }

    }
}
