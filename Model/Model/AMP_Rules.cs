using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Model
{
    public class AMP_Rules
    {
        public string RuleId { get; set; }
        public string Rule_Name { get; set; }
        public string Rule_Type { get; set; }
        public string ShortLeadStatusList { get; set; }
        public string Created_By { get; set; }
        public DateTime Created_Date { get; set; }
        public string Rule_Status { get; set; }
        public int TriggerMinutes { get; set; }
        public DateTime Last_Run { get; set; }
        public DateTime Next_Run { get; set; }
        public string Notify_Dept_Code { get; set; }
        public int Notify_EventDay_From { get; set; }
        public int Notify_EventDay_To { get; set; }
        public string EventStatusFrom { get; set; }
        public string EventStatusTo { get; set; }
        public string EventStatusList { get; set; }
        public string EmailSubject { get; set; }
        public bool ShowFuncId { get; set; }
        public bool ShowSpaceCode { get; set; }
        public bool ShowHierarchyFuncDesc { get; set; }
        public bool ShowPackageItemDateTime { get; set; }
        public bool ShowFunctionSignageChange { get; set; }
        public int NotesLength { get; set; }
    }
}
