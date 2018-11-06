
namespace D365Test
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

        public class ProjectTasks
        {
            public const string LogicalName = "msdyn_projecttask";
            public const string PrimaryKey = "msdyn_projecttaskid";

            public const string Project = "msdyn_project";
            public const string TaskIdentifier = "fkh_taskidentifierid";
            public const string Subject = "msdyn_subject";
            public const string WBSID = "msdyn_wbsid";
        }

        public class Projects
        {
            public const string LogicalName = "msdyn_project";
            public const string PrimaryKey = "msdyn_projectid";

            public const string ProjectTemplate = "msdyn_projecttemplate";
        }

    }
}
