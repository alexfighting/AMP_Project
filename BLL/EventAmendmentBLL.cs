using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DAL;
using Model;

namespace BLL
{
    public class EventAmendmentBLL
    {

        public static void checkAmendmentManually(string strDepartmentCode, int nPrevSnapshotId, int nCurrSnapshotId, string strEventCode = "All")
        {
            Notification_Dep_user dep = AMP_DepartmentDAL.getDepartment(strDepartmentCode);

            DateTime dtSnapshotCurrent, dtSnapshotPrevious;

            dtSnapshotPrevious = AMP_Common.getSnapshotDateTimeFromId(nPrevSnapshotId);
            dtSnapshotCurrent = AMP_Common.getSnapshotDateTimeFromId(nCurrSnapshotId);
			DateTime dtNow = DateTime.Now;							  

            AMP_Rules common_rule = AMP_RulesDAL.getCommonEventRule();

            AMP_EventDAL.dep = dep;
            AMP_EventDAL.rule = common_rule;
            AMP_RulesDAL.rule = common_rule;
            AMP_FunctionDAL.rule = common_rule;
            AMP_OrderDAL.rule = common_rule;

            AMP_EventDAL.dtSnapshotCurrent = dtSnapshotCurrent;
            AMP_EventDAL.dtSnapshotPrevious = dtSnapshotPrevious;

            AMP_EventDAL.nSnapshotCurrentID = nCurrSnapshotId;
            AMP_EventDAL.nSnapshotPreviousID = nPrevSnapshotId;
            
            AMP_RulesDAL.nSnapshotCurrentID = nCurrSnapshotId;
            AMP_RulesDAL.nSnapshotPreviousID = nPrevSnapshotId;

            AMP_RulesDAL.dtSnapshotCurrent = dtSnapshotCurrent;
            AMP_RulesDAL.dtSnapshotPrevious = dtSnapshotPrevious;

            List<EventInfo> lstEvent = new List<EventInfo>();

            if (strEventCode == "All")
            {
                lstEvent = AMP_EventDAL.getAllEventsFromRule();
            }
            else
            {
                EventInfo evt = AMP_EventDAL.getEventInfo(int.Parse(strEventCode));
                lstEvent.Add(evt);
            }

            CheckEvent_Amendment(lstEvent, dep, nCurrSnapshotId, nPrevSnapshotId);            
			AMP_Notification.sendEmail("AMDRun@mcec.com.au", "azheng@mcec.com.au", "status of running for " + (DateTime.Now.Subtract(dtNow).TotalSeconds), " seconds. Start at:" + dtNow.ToString("dd/MM/yyyy hh:mm") + " finish at " + DateTime.Now.ToString("dd/MM/yyyy hh:mm"));																																																																		   

        }

        /// <summary>
        /// checkamendment for all departments, all events, all functions and orders, all notes, all documents.
        /// </summary>
        public static void checkAmendmentbyDepartment_AllofEvent()
        {
            //get the program running time, all department, all rules need to use this time as start time
            // this is trying to avoid if some department running for a long time, and total program need to run > 30 minutes

            DateTime dtNow = DateTime.Now;

            //loop 1, list all the departments need to run amendments.

            List<Notification_Dep_user> lstUser = AMP_DepartmentDAL.getDepartments();

            foreach (Notification_Dep_user dep in lstUser)
            {
                AMP_EventDAL.dep = dep;

                int nSnapshotCurrent, nSnapshotPrevious;
                DateTime dtSnapshotCurrent, dtSnapshotPrevious;

                //loop 2, list all rules for this department. rule is defined to run hourly/daily/... with different event filters. this is trying to shrink the message numbers send to user.

                List<AMP_Rules> lstrules = AMP_RulesDAL.getEventRules(dep.DepartmentCode, dtNow);

                foreach (AMP_Rules rule in lstrules)
                {
                    if (rule.RuleId != null)
                    {
                        AMP_EventDAL.rule = rule;
                        AMP_RulesDAL.rule = rule;
                        AMP_FunctionDAL.rule = rule;
                        AMP_OrderDAL.rule = rule;

                        if (rule.Last_Run != null && rule.Last_Run != DateTime.MinValue)
                        {
                            nSnapshotCurrent = AMP_Common.getRecentSnapshotID(dtNow);
                            nSnapshotPrevious = AMP_Common.getRecentSnapshotID(rule.Last_Run);

                            dtSnapshotCurrent = AMP_Common.getRecentSnapshotDateTime(dtNow);
                            dtSnapshotPrevious = AMP_Common.getRecentSnapshotDateTime(rule.Last_Run);

                        }
                        else
                        {
                            nSnapshotCurrent = AMP_Common.getRecentSnapshotID(dtNow);
                            nSnapshotPrevious = AMP_Common.getRecentSnapshotID(dtNow.AddMinutes(-rule.TriggerMinutes));

                            dtSnapshotCurrent = AMP_Common.getRecentSnapshotDateTime(dtNow);
                            dtSnapshotPrevious = AMP_Common.getRecentSnapshotDateTime(dtNow.AddMinutes(-rule.TriggerMinutes));
                        }
                        AMP_EventDAL.nSnapshotCurrentID = nSnapshotCurrent;
                        AMP_EventDAL.nSnapshotPreviousID = nSnapshotPrevious;

                        AMP_EventDAL.dtSnapshotCurrent = dtSnapshotCurrent;
                        AMP_EventDAL.dtSnapshotPrevious = dtSnapshotPrevious;

                        AMP_RulesDAL.nSnapshotCurrentID = nSnapshotCurrent;
                        AMP_RulesDAL.nSnapshotPreviousID = nSnapshotPrevious;

                        AMP_RulesDAL.dtSnapshotCurrent = dtSnapshotCurrent;
                        AMP_RulesDAL.dtSnapshotPrevious = dtSnapshotPrevious;

                        //get all event in the current department rule for 'EVENT', including cancelled, turnover
                        List<EventInfo> lstEvent = AMP_EventDAL.getAllEventsFromRule();

                        CheckEvent_Amendment(lstEvent, dep,  nSnapshotCurrent, nSnapshotPrevious);
                    }
                    AMP_RulesDAL.UpdateNextRun();
                }
            }//end foreach rule

            if (dtNow.Year == 2018 && dtNow.Month<=5 )
            {
                AMP_Notification.sendEmail("AMDRun@mcec.com.au", "azheng@mcec.com.au", "status of running for " + (DateTime.Now.Subtract(dtNow).TotalSeconds.ToString("G")) + " seconds.", "Start at:" + dtNow.ToString("dd/MM/yyyy hh:mm") + " finish at " + DateTime.Now.ToString("dd/MM/yyyy hh:mm"));
            }           
        }

        private static void CheckEvent_Amendment(List<EventInfo> lstEvent, Notification_Dep_user dep, int nCurrSnapshotId, int nPrevSnapshotId)
        {
            if (lstEvent.Count > 0)
            {
                AMP_Common.initDB(nCurrSnapshotId, nPrevSnapshotId, lstEvent);
                AMP_EventDAL.dep = dep;

                foreach (EventInfo evt in lstEvent)
                {
                    AMP_EventDAL.evt = evt;
                    AMP_EventDAL.eMsg = new EventAccountMessage();

                    if (AMP_EventDAL.checkEventCancelled()) AMP_EventDAL.SendMSG();                    
                    else if (AMP_EventDAL.checkEventNewOrShortLead()) AMP_EventDAL.SendMSG();
                    else if (evt.Status != "80" && evt.Status != "86" && AMP_EventDAL.rule.ShortLeadStatusList.IndexOf(evt.Status.ToString()) > -1)
                    {
                        AMP_EventDAL.getEventAmendmentHead();
                        AMP_EventDAL.checkEventStatusChange();
                        AMP_EventDAL.checkEventUpdate();
                        AMP_EventDAL.checkEventNotesChange();

                        EventAccountMessage function_amendment = checkFunction_Amendment(evt, dep, nCurrSnapshotId, nPrevSnapshotId);
                        
                        if (function_amendment.FuncUpdated)
                        {
                            AMP_EventDAL.eMsg.EventUpdated = true;
                            AMP_EventDAL.eMsg.MSGText += function_amendment.MSGText;
                            AMP_EventDAL.eMsg.MSGHTML += function_amendment.MSGHTML;
                        }

                        AMP_EventDAL.checkDocumentChange();
                        if (AMP_EventDAL.eMsg.EventUpdated) AMP_EventDAL.SendMSG();
                    }
                }
            }
        }

        private static EventAccountMessage checkFunction_Amendment(EventInfo evt, Notification_Dep_user dep, int nCurrSnapshotId, int nPrevSnapshotId)
        {
            AMP_FunctionDAL.dep = dep;
            AMP_FunctionDAL.evt = evt;            
            AMP_FunctionDAL.nSnapshotCurrentID = nCurrSnapshotId;
            AMP_FunctionDAL.nSnapshotPreviousID = nPrevSnapshotId;

            EventAccountMessage function_amendment = new EventAccountMessage();

            //List<Function_Info> lstFunctions = AMP_FunctionDAL.getAll_Related_Function();
            List<Function_Info> lstFunctions = AMP_FunctionDAL.getCurrentChangeFunctions();

            //loop functions
            foreach (Function_Info function in lstFunctions)
            {
                AMP_FunctionDAL.finfo = function;

                AMP_FunctionDAL.getFunctionHeader();

                if (function.isFunctionChange)
                    AMP_FunctionDAL.checkFunctionChange();

                if (function.isFunctionNotesChange)
                    AMP_FunctionDAL.checkFunctionNotesChange();

                if (function.isFunctionNotesChange)
                    AMP_FunctionDAL.checkFunctionSignage();

                EventAccountMessage order_amendment = new EventAccountMessage();
                if (function.isOrdersChange || function.isOrdersNotesChange || function.isOrderItemsChange || function.isOrderItemsNotesChange)
                {
                    order_amendment = checkOrder_Amendment(evt, function, dep, nCurrSnapshotId, nPrevSnapshotId);
                }
                    

                if (AMP_FunctionDAL.eMsg.FuncUpdated || order_amendment.OrderUpdated)
                {
                    function_amendment.FuncUpdated = true;
                    function_amendment.MSGText += AMP_FunctionDAL.eMsg.MSGText + order_amendment.MSGText;
                    function_amendment.MSGHTML += AMP_FunctionDAL.eMsg.MSGHTML + order_amendment.MSGHTML;
                }
            }

            List<Function_Info> lstDeletedFunctions = AMP_FunctionDAL.getDelFunctions();

            foreach (Function_Info function in lstDeletedFunctions)
            {
                AMP_FunctionDAL.finfo = function;

                AMP_FunctionDAL.getFunctionHeader();

                AMP_FunctionDAL.getDeletedFunctionItems();

                if (AMP_FunctionDAL.eMsg.FuncUpdated)
                {
                    function_amendment.FuncUpdated = true;
                    function_amendment.MSGText += AMP_FunctionDAL.eMsg.MSGText;
                    function_amendment.MSGHTML += AMP_FunctionDAL.eMsg.MSGHTML;
                }
            }

            return function_amendment;
        }

        private static EventAccountMessage checkOrder_Amendment(EventInfo evt, Function_Info finfo, Notification_Dep_user dep, int nCurrSnapshotId, int nPrevSnapshotId)
        {
            AMP_OrderDAL.dep = dep;
            AMP_OrderDAL.evt = evt;
            AMP_OrderDAL.finfo = finfo;
            AMP_OrderDAL.nSnapshotCurrentID = nCurrSnapshotId;
            AMP_OrderDAL.nSnapshotPreviousID = nPrevSnapshotId;

            EventAccountMessage order_amendment_msg = new EventAccountMessage();

            List<Order_Info> lstCurrentOrders = AMP_OrderDAL.getCurrentFunctionOrders();           

            foreach (Order_Info ord in lstCurrentOrders)
            {              
                AMP_OrderDAL.oinfo = ord;

                AMP_OrderDAL.getOrderHeader();

                if (finfo.isOrdersChange)
                    AMP_OrderDAL.checkOrderChange();
                
                if (finfo.isOrdersNotesChange)
                    AMP_OrderDAL.checkOrderNotesChange();

                if (finfo.isOrderItemsChange)
                    AMP_OrderDAL.checkOrderItemChange();
                
                if(finfo.isOrderItemsNotesChange)
                    AMP_OrderDAL.checkOrderItemNotesChange();

                if (AMP_OrderDAL.eMsg.OrderUpdated)
                {
                    order_amendment_msg.OrderUpdated = true;
                    order_amendment_msg.MSGText += AMP_OrderDAL.eMsg.MSGText;
                    order_amendment_msg.MSGHTML += AMP_OrderDAL.eMsg.MSGHTML;
                }
            }
            
            List<Order_Info> lstDelOrders = AMP_OrderDAL.getDeletedOrders();

            foreach (Order_Info delord in lstDelOrders)
            {
                AMP_OrderDAL.oinfo = delord;

                AMP_OrderDAL.getOrderHeader();

                AMP_OrderDAL.checkDeltedOrderItems();

                if (AMP_OrderDAL.eMsg.OrderUpdated)
                {
                    order_amendment_msg.OrderUpdated = true;
                    order_amendment_msg.MSGText += AMP_OrderDAL.eMsg.MSGText;
                    order_amendment_msg.MSGHTML += AMP_OrderDAL.eMsg.MSGHTML;
                }
            }
            return order_amendment_msg;
        }
    }
}
