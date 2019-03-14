
namespace Firstkey.D365.WorkflowActivity
{
    public class Constants
    {
        public const string TARGET = "Target";
        public const string POST_IMAGE = "PostImage";
        public const string ENTITY_REFERENCE = "entityReference";
        public const string IS_SUCCESS = "IsSuccess";
        public const string ERROR_MESSAGE = "ErrorMessage";

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
            public const string Progress = "msdyn_progress";

            public const string UnitId = "fkh_unitid";
            public const string AccessNotes = "fkh_accessnotes";
            public const string LockBoxRemoved = "fkh_lockboxremoved";
            public const string MechanicalLockBox = "fkh_mechanicallockbox";
            public const string MechanicalLockBoxNote = "fkh_mechanicallockboxnote";
            public const string PropertyGateCode = "fkh_propertygatecode";
            public const string RentlyLockBox = "fkh_rentlylockbox";
            public const string RentlyLockBoxNote = "fkh_rentlylockboxnote";
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

        public class Units
        {
            public const string LogicalName = "po_unit";
            public const string PrimaryKey = "po_unitid";

            public const string SFUnitId = "fkh_sfunitid";
            public const string UnitId = "po_unitidnum";
            public const string Name = "po_name";
            public const string LeaseEnd = "po_leaseend";
            public const string MoveInDate = "po_unitmoveindate";
            public const string MoveOutDate = "po_unitmoveoutdate";
            public const string SFUnitID = "fkh_sfunitid";
            public const string AccessNotes = "fkh_accessnotes";
            public const string LockBoxRemoved = "po_lockboxremoved";
            public const string MechanicalLockBox = "po_mechanicallockbox";
            public const string MechanicalLockBoxNote = "po_mechanicallockboxnote";
            public const string PropertyGateCode = "po_propertygatecode";
            public const string RentlyLockBox = "po_rentlylockbox";
            public const string RentlyLockBoxNote = "po_rentlylockboxnote";

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

    }
}
