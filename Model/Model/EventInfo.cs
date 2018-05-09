using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Model
{
    public class EventInfo
    {
        public int EventId { get; set; }
        public string EventDesc { get; set; }
        public DateTime InDate { get; set; }
        public DateTime OutDate { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string AccountNo { get; set; }
        public string Rank { get; set; }
        public string Class { get; set; }
        public string Region { get; set; }
        public string IndustryGroup { get; set; }
        public string LeadChannel { get; set; }
        public string SalesPerson { get; set; }
        public string EventPlanner { get; set; }
        public string TechPlanner { get; set; }
        public string OperationManager { get; set; }
        public string TSDService { get; set; }
        public decimal ForecastRevenue { get; set; }
        public decimal BudgetRevenue { get; set; }
        public decimal OrderedRevenue { get; set; }
        public decimal TotalRevenue { get; set; }
        public int EventDays { get; set; }        
        public int Attendees { get; set; }
        public string Status { get; set; }
    }
    
    public class Function_Info
    {
        public int FuncId { get; set; }
        public string FuncClass { get; set; }
        public string FuncDesc { get; set; }
        public string FuncType { get; set; }
        public DateTime FuncStart { get; set; }
        public DateTime FuncEnd { get; set; }
        public string SpaceDesc { get; set; }
        public string SpaceCode { get; set; }        
        public string Status { get; set; }
        public bool isFunctionChange { get; set; }
        public bool isFunctionNotesChange { get; set; }
        public bool isFunctionSignageChange { get; set; }
        public bool isOrdersChange { get; set; }
        public bool isOrdersNotesChange { get; set; }
        public bool isOrderItemsChange { get; set; }
        public bool isOrderItemsNotesChange { get; set; }
    }


    public struct Order_Info
    {
        public int EventId { get; set; }
        public int FuncId { get; set; }
        public int Order_Number { get; set; }
        public string BoothNumber { get; set; }
        public string ChangeType { get; set; }
    }
}
