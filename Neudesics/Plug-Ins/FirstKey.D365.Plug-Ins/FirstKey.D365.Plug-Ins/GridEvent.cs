using System;
using System.Runtime.Serialization;

namespace FirstKey.D365.Plug_Ins
{
    [DataContract]
    public class GridEvent<T>
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }
        [DataMember(Name = "eventType")]
        public string EventType { get; set; }
        [DataMember(Name = "subject")]
        public string Subject { get; set; }
        [DataMember(Name = "eventTime")]
        public string EventTime { get; set; }
        [DataMember(Name = "data")]
        public T data { get; set; }
        [DataMember(Name = "topic")]
        public string Topic { get; set; }
    }

    [DataContract]
    public class DataPayLoad
    {
        [DataMember]
        public Events Event { get; set; }
        [DataMember]
        public string PropertyID { get; set; }
        [DataMember]
        public string Date1 { get; set; }
        [DataMember]
        public string Date2 { get; set; }
        [DataMember]
        public string Date3 { get; set; }
        [DataMember]
        public bool IsForce { get; set; }
        [DataMember]
        public string JobID { get; set; }
        [DataMember]
        public string FotoNotesID { get; set; }
        [DataMember]
        public string RenowalkID { get; set; }
        [DataMember]
        public string EmailID { get; set; }
    }

    [DataContract]
    public enum Events
    {
        RESIDENT_NOTICE_TO_MOVE_OUT_RECEIVED = 1,
        ASSIGN_PROJECT_MANAGER = 2,
        CORPORATE_RENEWALS = 3,
        MARKET_SCHEDULES_PRE_MOVE_OUT = 4,
        PRE_MOVE_OUT_INSPECTION = 5,
        MOVE_OUT_INSPECTION = 6,
        BUDGET_START = 7,
        BUDGET_APPROVAL = 8,
        JOB_ASSIGNMENT_TO_VENDORS_IN_CONTRACT_CREATOR = 9,
        JOB_AND_CONTRACTS_SUBMITTED_TO_YARDI = 10,
        VENDORS_SAYS_JOB_STARTED = 11,
        WORK_IN_PROGRESS = 12,
        VENDOR_REQUESTS_CHANGE_ORDER_IF_NECESSARY = 13,
        CHANGE_ORDER_APPROVED = 14,
        VENDOR_SAYS_JOBS_COMPLETE = 15,
        QUALITY_CONTROL_INSPECTION = 16,
        JOB_COMPLETED = 17,
        HERO_SHOT_PICTURE = 18,
        MARKETING_INSPECTION = 19,
        BI_WEEKLY_INSPECTION = 20,
        MOVE_IN_INSPECTION_COMPLETED = 21,
        REVISED_COMPLETION_DATE = 22,
        SCHEDULED_CLOSING_DATE = 23,
        DUE_DILLIGENCE_DEADLINE = 24,
        DD_INSPECTION_APPROVED = 25,
        CLOSING_DOCS_APPROVED = 26,
        MOVE_OUT_DATE_CHANGED = 1001,
        OFFER_ACCEPTED = 201,
        IR_ASSIGN_PROJECT_MANAGER = 202,
        SCHEDULE_DUE_DILLIGENCE_INSPECTION = 203,
        IR_BUDGET_START = 204,
        IR_BUDGET_APPROVAL = 205,
        OFFER_REJECTED_OR_APPROVAL = 206,
        IR_JOB_ASSIGNMENT_TO_VENDORS_IN_CONTRACT_CREATOR = 207,
        CLOSE_ESCROW = 208,
        IR_JOB_AND_CONTRACTS_SUBMITTED_TO_YARDI = 209,
        IR_VENDORS_SAYS_JOB_STARTED = 210,
        IR_WORK_IN_PROGRESS = 211,
        IR_VENDOR_REQUESTS_CHANGE_ORDER_IF_NECESSARY = 212,
        IR_CHANGE_ORDER_APPROVED = 213,
        IR_VENDOR_SAYS_JOBS_COMPLETE = 214,
        IR_QUALITY_CONTROL_INSPECTION = 215,
        IR_JOB_COMPLETED = 216,
        IR_HERO_SHOT_PICTURE = 217,
        IR_MARKETING_INSPECTION = 218,
        IR_BI_WEEKLY_INSPECTION = 219,
        IR_MOVE_IN_INSPECTION_COMPLETED = 220,
        IR_REVISED_COMPLETION_DATE = 222,
        IR_SCHEDULED_CLOSING_DATE = 223,
        IR_DUE_DILLIGENCE_DEADLINE = 224,
        IR_DD_INSPECTION_APPROVED = 225,
        IR_CLOSING_DOCS_APPROVED = 226,
    }
}
