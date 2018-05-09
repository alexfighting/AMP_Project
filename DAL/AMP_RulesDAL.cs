using Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace DAL
{
    public class AMP_RulesDAL
    {
        public static string strCompDatabase = Properties.Settings.Default.strCompDatabase;
        public static string strEBMSConn = Properties.Settings.Default.strEBMSConn;
        public static string strEmailFrom = Properties.Settings.Default.Amendment_Error_EmailAddressFrom;
        public static string strEmailTo = Properties.Settings.Default.Amendment_Error_EmailAddressTo;
        public static int nCommandTimeOut = Properties.Settings.Default.nCommandTimeOut;

        public static AMP_Rules rule = new AMP_Rules();     

        public static DateTime dtSnapshotCurrent, dtSnapshotPrevious;

        public static int nSnapshotCurrentID, nSnapshotPreviousID;

        /// <summary>
        ///  withinseconds =1200 means from the nextrun time till now, if it's within 1200 seconds (20 minutes, still can run)
        /// </summary>
        /// <param name="strDeptCode"></param>
        /// <param name="checktime"></param>
        /// <param name="withinseconds"></param>
        /// <returns></returns>
        public static List<AMP_Rules> getEventRules(string strDeptCode, DateTime checktime, int withinseconds = 1200)
        {
            List<AMP_Rules> lstrule = new List<AMP_Rules>();
            SqlConnection conn = new SqlConnection(strEBMSConn);

            try
            {
                conn.Open();

                string strSQL = "SELECT RuleId, RuleName, RuleType, ShortLeadStatusList, CreatedBy, CreatedDate, Status, TriggerMinutes,EventStatusList, LastRun, NextRun, Noti_Dep_Code, EventNotifyDaysFrom, EventNotifyDaysTo, EventStatusFrom, EventStatusTo, EmailSubject,ShowFuncId, ShowSpaceCode, ShowHierarchyFuncDesc, ShowPackageItemDateTime,ShowFuncSignage, NotesLength ";
                strSQL += " FROM AMP_New_Rules where Status='A' ";
                strSQL += "  and ruletype='EVENT' and (datediff(second,  NextRun,  @checkDateTime) >0 or NextRun is null ) and Noti_Dep_Code=@deptcode";

                SqlCommand comm = new SqlCommand(strSQL, conn);
                comm.Parameters.Add("@deptcode", SqlDbType.VarChar, 20).Value = strDeptCode;
                comm.Parameters.Add("@checkDateTime", SqlDbType.DateTime).Value = checktime;
                comm.Parameters.Add("@withinseconds", SqlDbType.Int).Value = withinseconds;
                comm.CommandTimeout = nCommandTimeOut;

                SqlDataAdapter da = new SqlDataAdapter(comm);

                DataTable dt = new DataTable();
                da.Fill(dt);
                da.FillSchema(dt, SchemaType.Source);

                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        AMP_Rules rule = new AMP_Rules();
                        rule.RuleId = dr["RuleId"].ToString();
                        rule.Rule_Name = dr["RuleName"].ToString();
                        rule.Rule_Type = dr["RuleType"].ToString();
                        rule.ShortLeadStatusList = dr["ShortLeadStatusList"].ToString();
                        rule.Created_By = dr["CreatedBy"].ToString();
                        rule.Created_Date = dr["CreatedDate"] == DBNull.Value ? DateTime.MinValue : DateTime.Parse(dr["CreatedDate"].ToString());
                        rule.Rule_Status = dr["Status"].ToString();
                        rule.TriggerMinutes = dr["TriggerMinutes"] == DBNull.Value ? 1 : int.Parse(dr["TriggerMinutes"].ToString());
                        rule.Next_Run = dr["NextRun"] == DBNull.Value ? DateTime.Now : DateTime.Parse(dr["NextRun"].ToString());
                        rule.Last_Run = dr["LastRun"] == DBNull.Value ? rule.Next_Run.AddMinutes(-rule.TriggerMinutes) : DateTime.Parse(dr["LastRun"].ToString());
                        rule.Notify_Dept_Code = dr["Noti_Dep_Code"].ToString();
                        rule.Notify_EventDay_From = dr["EventNotifyDaysFrom"] == DBNull.Value ? 0 : int.Parse(dr["EventNotifyDaysFrom"].ToString());
                        rule.Notify_EventDay_To = dr["EventNotifyDaysTo"] == DBNull.Value ? 0 : int.Parse(dr["EventNotifyDaysTo"].ToString());
                        rule.EventStatusFrom = dr["EventStatusFrom"].ToString();
                        rule.EventStatusTo = dr["EventStatusTo"].ToString();
                        string strS = null;
                        rule.EventStatusList = dr["EventStatusList"] == DBNull.Value ? strS : dr["EventStatusList"].ToString();
                        rule.EmailSubject = dr["EmailSubject"].ToString();
                        rule.ShowFuncId = false;
                        if (dr["ShowFuncId"] != DBNull.Value)
                        {
                            bool isShowFuncId = false;
                            if (bool.TryParse(dr["ShowFuncId"].ToString(), out isShowFuncId))
                            {
                                isShowFuncId = bool.Parse(dr["ShowFuncId"].ToString());
                                if (isShowFuncId) rule.ShowFuncId = true;
                            }
                        }
                        rule.ShowSpaceCode = false;
                        if (dr["ShowSpaceCode"] != DBNull.Value)
                        {                            
                            bool isShowSpaceCode = false;
                            if (bool.TryParse(dr["ShowSpaceCode"].ToString(), out isShowSpaceCode))
                            {
                                isShowSpaceCode = bool.Parse(dr["ShowSpaceCode"].ToString());
                                if (isShowSpaceCode) rule.ShowSpaceCode = true;
                            }
                        }

                        rule.ShowHierarchyFuncDesc = false;
                        if (dr["ShowHierarchyFuncDesc"] != DBNull.Value)
                        {                        
                            bool isShowHierarchyFuncDesc = false;
                            if (bool.TryParse(dr["ShowHierarchyFuncDesc"].ToString(), out isShowHierarchyFuncDesc))
                            {
                                isShowHierarchyFuncDesc = bool.Parse(dr["ShowHierarchyFuncDesc"].ToString());
                                if (isShowHierarchyFuncDesc) rule.ShowHierarchyFuncDesc = true;
                            }
                        }
                        rule.ShowPackageItemDateTime = false;

                        if (dr["ShowPackageItemDateTime"] != DBNull.Value)
                        {
                            bool isShowPackageItemDateTime = false;
                            if (bool.TryParse(dr["ShowPackageItemDateTime"].ToString(), out isShowPackageItemDateTime))
                            {
                                isShowPackageItemDateTime = bool.Parse(dr["ShowPackageItemDateTime"].ToString());
                                if (isShowPackageItemDateTime) rule.ShowPackageItemDateTime = true;
                            }
                        }

                        rule.ShowFunctionSignageChange = false;

                        if (dr["ShowFuncSignage"] != DBNull.Value)
                        {
                            bool isShowFuncSignage = false;
                            if (bool.TryParse(dr["ShowFuncSignage"].ToString(), out isShowFuncSignage))
                            {
                                isShowFuncSignage = bool.Parse(dr["ShowFuncSignage"].ToString());
                                if (isShowFuncSignage) rule.ShowFunctionSignageChange = true;
                            }
                        }

                        rule.NotesLength = 200;
                        if (dr["NotesLength"] != DBNull.Value)
                        {                            
                            int nNotesLength = 200;
                            if (int.TryParse(dr["NotesLength"].ToString(), out nNotesLength))
                            {
                                nNotesLength = int.Parse(dr["NotesLength"].ToString());
                                rule.NotesLength = nNotesLength;
                            }
                        }

                        lstrule.Add(rule);
                    }
                }

            }
            catch (Exception ex)
            {
                AMP_Common.sendException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name, strEmailFrom, strEmailTo, ex.Message);
            }
            finally
            {
                conn.Close();
            }
            return lstrule;
        }

        public static AMP_Rules getCommonEventRule()
        {
            AMP_Rules rule = new AMP_Rules();
            SqlConnection conn = new SqlConnection(strEBMSConn);

            try
            {
                conn.Open();

                string strSQL = "SELECT RuleId, RuleName, RuleType, ShortLeadStatusList, CreatedBy, CreatedDate, Status, TriggerMinutes,EventStatusList, LastRun, NextRun, Noti_Dep_Code, EventNotifyDaysFrom, EventNotifyDaysTo, EventStatusFrom, EventStatusTo, EmailSubject,ShowFuncId, ShowSpaceCode, ShowHierarchyFuncDesc, ShowPackageItemDateTime,ShowFuncSignage, NotesLength ";
                strSQL += " FROM AMP_New_Rules where Status='A' ";
                strSQL += "  and ruletype='EVENT' and Noti_Dep_Code is null";

                SqlCommand comm = new SqlCommand(strSQL, conn);
                comm.CommandTimeout = nCommandTimeOut;

                SqlDataAdapter da = new SqlDataAdapter(comm);

                SqlDataReader dr = comm.ExecuteReader();

                if (dr.Read() && dr.HasRows)
                {
                    rule.RuleId = dr["RuleId"].ToString();
                    rule.Rule_Name = dr["RuleName"].ToString();
                    rule.Rule_Type = dr["RuleType"].ToString();
                    rule.ShortLeadStatusList = dr["ShortLeadStatusList"].ToString();
                    rule.Created_By = dr["CreatedBy"].ToString();
                    rule.Created_Date = dr["CreatedDate"] == DBNull.Value ? DateTime.MinValue : DateTime.Parse(dr["CreatedDate"].ToString());
                    rule.Rule_Status = dr["Status"].ToString();
                    rule.TriggerMinutes = dr["TriggerMinutes"] == DBNull.Value ? 1 : int.Parse(dr["TriggerMinutes"].ToString());
                    rule.Next_Run = dr["NextRun"] == DBNull.Value ? DateTime.Now : DateTime.Parse(dr["NextRun"].ToString());
                    rule.Last_Run = dr["LastRun"] == DBNull.Value ? rule.Next_Run.AddMinutes(-rule.TriggerMinutes) : DateTime.Parse(dr["LastRun"].ToString());
                    rule.Notify_Dept_Code = dr["Noti_Dep_Code"].ToString();
                    rule.Notify_EventDay_From = dr["EventNotifyDaysFrom"] == DBNull.Value ? 0 : int.Parse(dr["EventNotifyDaysFrom"].ToString());
                    rule.Notify_EventDay_To = dr["EventNotifyDaysTo"] == DBNull.Value ? 0 : int.Parse(dr["EventNotifyDaysTo"].ToString());
                    rule.EventStatusFrom = dr["EventStatusFrom"].ToString();
                    rule.EventStatusTo = dr["EventStatusTo"].ToString();
                    string strS = null;
                    rule.EventStatusList = dr["EventStatusList"] == DBNull.Value ? strS : dr["EventStatusList"].ToString();
                    rule.EmailSubject = dr["EmailSubject"].ToString();
                    if (dr["ShowFuncId"] == DBNull.Value)
                    {
                        rule.ShowFuncId = false;
                    }
                    else
                    {
                        rule.ShowFuncId = false;
                        bool isShowFuncId = false;
                        if (bool.TryParse(dr["ShowFuncId"].ToString(), out isShowFuncId))
                        {
                            isShowFuncId = bool.Parse(dr["ShowFuncId"].ToString());
                            if (isShowFuncId) rule.ShowFuncId = true;
                        }
                    }

                    if (dr["ShowSpaceCode"] == DBNull.Value)
                    {
                        rule.ShowSpaceCode = false;
                    }
                    else
                    {
                        rule.ShowSpaceCode = false;
                        bool isShowSpaceCode = false;
                        if (bool.TryParse(dr["ShowSpaceCode"].ToString(), out isShowSpaceCode))
                        {
                            isShowSpaceCode = bool.Parse(dr["ShowSpaceCode"].ToString());
                            if (isShowSpaceCode) rule.ShowSpaceCode = true;
                        }
                    }

                    if (dr["ShowHierarchyFuncDesc"] == DBNull.Value)
                    {
                        rule.ShowHierarchyFuncDesc = false;
                    }
                    else
                    {
                        rule.ShowHierarchyFuncDesc = false;
                        bool isShowHierarchyFuncDesc = false;
                        if (bool.TryParse(dr["ShowHierarchyFuncDesc"].ToString(), out isShowHierarchyFuncDesc))
                        {
                            isShowHierarchyFuncDesc = bool.Parse(dr["ShowHierarchyFuncDesc"].ToString());
                            if (isShowHierarchyFuncDesc) rule.ShowHierarchyFuncDesc = true;
                        }
                    }

                    
                    rule.ShowPackageItemDateTime = false;

                    if (dr["ShowPackageItemDateTime"] != DBNull.Value)
                    {
                        bool isShowPackageItemDateTime = false;
                        if (bool.TryParse(dr["ShowPackageItemDateTime"].ToString(), out isShowPackageItemDateTime))
                        {
                            isShowPackageItemDateTime = bool.Parse(dr["ShowPackageItemDateTime"].ToString());
                            if (isShowPackageItemDateTime) rule.ShowPackageItemDateTime = true;
                        }
                    }

                    rule.ShowFunctionSignageChange = false;

                    if (dr["ShowFuncSignage"] != DBNull.Value)
                    {
                        bool isShowFuncSignage = false;
                        if (bool.TryParse(dr["ShowFuncSignage"].ToString(), out isShowFuncSignage))
                        {
                            isShowFuncSignage = bool.Parse(dr["ShowFuncSignage"].ToString());
                            if (isShowFuncSignage) rule.ShowFunctionSignageChange = true;
                        }
                    }
                    
                    rule.NotesLength = 200;
                    if (dr["NotesLength"] != DBNull.Value)
                    {
                        int nNotesLength = 200;
                        if (int.TryParse(dr["NotesLength"].ToString(), out nNotesLength))
                        {
                            nNotesLength = int.Parse(dr["NotesLength"].ToString());
                            rule.NotesLength = nNotesLength;
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                AMP_Common.sendException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name, strEmailFrom, strEmailTo, ex.Message);
            }
            finally
            {
                conn.Close();
            }
            return rule;
        }  

        public static void UpdateNextRun()
        {
            DateTime dtNextRun = dtSnapshotCurrent.AddMinutes(rule.TriggerMinutes);

            SqlConnection conn = new SqlConnection(strEBMSConn);

            try
            {
                conn.Open();

                string strSQL = "update AMP_New_Rules set LastRun=@lastrun, NextRun=@nextrun where RuleId=@ruleid";
                SqlCommand comm = new SqlCommand(strSQL, conn);

                comm.Parameters.Add("@lastrun", SqlDbType.DateTime).Value = dtSnapshotCurrent;
                comm.Parameters.Add("@nextrun", SqlDbType.DateTime).Value = dtNextRun;
                comm.Parameters.Add("@ruleid", SqlDbType.Int).Value = rule.RuleId;
                comm.CommandTimeout = nCommandTimeOut;

                comm.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
            finally
            {
                conn.Close();
            }
        }
    }
}
