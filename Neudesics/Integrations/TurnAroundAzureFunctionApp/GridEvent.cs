﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace TurnAroundAzureFunctionApp
{
    public class GridEvent<T>
    {
        public string Id { get; set; }
        public string EventType { get; set; }
        public string Subject { get; set; }
        public DateTime EventTime { get; set; }
        public T Data { get; set; }
        public string Topic { get; set; }
    }

    /*
    public class TurnAround
    {
        public TurnAroundEvents Event { get; set; }
        public string PropertyID { get; set; }
        public DateTime Date1 { get; set; }
        public DateTime? Date2 { get; set; }
        public string JobID { get; set; }
        public bool IsForce { get; set; }
        //public DateTime NotificationDate { get; set; }
        //public DateTime Pre_MoveOut_App_Date { get; set; }
        //public DateTime Pre_MoveOut_Create_Date { get; set; }
        //public DateTime Budget_Create_Date { get; set; }
        //public DateTime Inspection_Start_Date { get; set; }
        //public DateTime Inspection_Complete_Date { get; set; }
        //public DateTime Budget_Submission_Date { get; set; }
        //public DateTime Job_Create_Date { get; set; }
        //public DateTime Job_Assigned_Date { get; set; }
        //public DateTime Inspection_Complete_Date { get; set; }
    }

    public enum TurnAroundEvents
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
        MOVE_OUT_DATE_CHANGED = 1001,
        OFFER_ACCEPTED = 201,
        IR_ASSIGN_PROJECT_MANAGER = 202,
        SCHEDULE_DUE_DILLIGENCE_INSPECTION = 203,
        IR_BUDGET_START = 204,
        IR_BUDGET_APPROVAL = 205,
        OFFER_REJECTED_OR_APPROVALE = 206,
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
    }

    */
    public class DataPayLoad
    {
        public Events Event { get; set; }
        public string PropertyID { get; set; }
        public string Date1 { get; set; }
        public string Date2 { get; set; }
        public string Date3 { get; set; }
        public string JobID { get; set; }
        public string Contract_Code { get; set; }
        public bool IsForce { get; set; }
        public string FotoNotesID { get; set; }
        public string RenowalkID { get; set; }
        public string EmailID { get; set; }
        public string ApprovedBy { get; set; }
        public List<Contract> Contracts { get; set; }
    }

    public class Contract
    {
        public string Contract_Code { get; set; }
        public string Vendor_Code { get; set; }
        public string Category_Code { get; set; }
        public string Amount { get; set; }
        public string Start_Date { get; set; }
        public string ItemID { get; set; }
        public string ItemDescription { get; set; }
        public string ItemReason { get; set; }
        public string ChangeOrder { get; set; }
    }

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
        JOB_CONFIRMED_IN_YARDI = 27,
        REVISED_START_DATE = 28,
        MULTI_VENDOR = 29,
        CHANGE_ORDER = 30,
        VENDOR_SAYS_CONTRACT_STARTED = 31,
        VENDOR_SAYS_CONTRACT_COMPLETED = 32,
        CHANGE_ORDER_APPROVED = 33,
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
        IR_JOB_CONFIRMED_IN_YARDI = 227,
        IR_REVISED_START_DATE = 228,
        IR_MULTI_VENDOR = 229,
        IR_CHANGE_ORDER = 230,
        IR_VENDOR_SAYS_CONTRACT_STARTED = 231,
        IR_VENDOR_SAYS_CONTRACT_COMPLETED = 232,
        IR_CHANGE_ORDER_APPROVED = 233
    }

}
