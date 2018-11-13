using System;
using System.Collections.Generic;
using System.Linq;
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

    public class TurnAround
    {
        public TurnAroundEvents Event { get; set; }
        public string PropertyID { get; set; }
        public DateTime Date1 { get; set; }
        public DateTime Date2 { get; set; }
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
        Sixty_Days_Notice = 1,
        Yardi_Lease_Renewal_Received = 2,
        Thirty_Days_Notice = 3,
        Yardi_PreMove_Out_Scheduled = 4,
        Renowalk_Budget_Started = 5,
        FotoNotes_Move_Out_Insp_Complete = 6,
        Renowalk_Project_Status_Walked,
        Yardi_Jobs_Created,
        Jobs_Assigned_To_Vendor,
        Yardi_Jobs_Started,
        FotoNotes_Interim_Insp_Complete,
        Vendor_Request_Change_Order,
        Change_Order_Approved,
        FotoNotes_QC_Insp_Complete_With_Add_Work,
        FotoNotes_QC_Insp_Complete_With_No_Add_Work,
        FotoNotes_Marketing_Insp_Complete,
        FotoNotes_Bi_Weekly_Insp_Complete,
        FotoNotes_Move_In_Insp_Complete
    }

}
