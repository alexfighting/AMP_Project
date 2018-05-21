using Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace DAL
{
    public class AMP_Common
    {
        //connection string
        public static string strEBMSConn = Properties.Settings.Default.strEBMSConn;
        //current live database name
        public static string strCompDatabase = Properties.Settings.Default.strCompDatabase;
        public static string strEmailFrom = Properties.Settings.Default.Amendment_Error_EmailAddressFrom;
        public static string strEmailTo = Properties.Settings.Default.Amendment_Error_EmailAddressTo;
        public static int nCommandTimeOut = Properties.Settings.Default.nCommandTimeOut;


        public static string GetAcctName(string strAcctCode)
        {
            string strAcctName = "";
            SqlConnection conn = new SqlConnection(strEBMSConn);

            try
            {
                conn.Open();

                string strSQL = " select distinct EV870_NAME from " + strCompDatabase + ".dbo.EV870_ACCT_MASTER where EV870_ACCT_CODE=@acctcode and EV870_ORG_CODE='10'  ";
                SqlCommand comm = new SqlCommand(strSQL, conn);
                comm.Parameters.Add("@acctcode", SqlDbType.VarChar, 8).Value = strAcctCode;
                comm.CommandTimeout = nCommandTimeOut;

                SqlDataReader dr = comm.ExecuteReader();
                if (dr.Read() && dr.HasRows)
                {
                    strAcctName = dr["EV870_NAME"].ToString() + " (" + strAcctCode + ")";
                }
            }
            catch (Exception ex)
            {
                sendException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name, strEmailFrom, strEmailTo, ex.Message);
            }
            finally
            {
                conn.Close();
            }

            return strAcctName;
        }

        public static string GetSpaceDesc(string strSpaceCode)
        {
            string strSpaceDesc = "";
            SqlConnection conn = new SqlConnection(strEBMSConn);

            try
            {
                conn.Open();

                string strSQL = " select distinct EV800_SPACE_DESC from " + strCompDatabase + ".dbo.EV800_SPACE_MASTER where EV800_SPACE_CODE=@spacecode  ";
                SqlCommand comm = new SqlCommand(strSQL, conn);
                comm.Parameters.Add("@spacecode", SqlDbType.VarChar, 6).Value = strSpaceCode;
                comm.CommandTimeout = nCommandTimeOut;

                SqlDataReader dr = comm.ExecuteReader();
                if (dr.Read() && dr.HasRows)
                {
                    strSpaceDesc = dr["EV800_SPACE_DESC"].ToString() + " (" + strSpaceCode + ")";
                }
            }
            catch (Exception ex)
            {
                sendException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name, strEmailFrom, strEmailTo, ex.Message);
            }
            finally
            {
                conn.Close();
            }

            return strSpaceDesc;
        }

        public static string GetAcctCode(int nEventId)
        {
            string strAcctCode = "";
            SqlConnection conn = new SqlConnection(strEBMSConn);

            try
            {
                conn.Open();

                string strSQL = " select EV200_CUST_NBR from " + strCompDatabase + ".dbo.EV200_EVENT_MASTER where EV200_EVT_ID=@eventid  ";
                SqlCommand comm = new SqlCommand(strSQL, conn);
                comm.Parameters.Add("@eventid", SqlDbType.Int).Value = nEventId;
                comm.CommandTimeout = nCommandTimeOut;

                SqlDataReader dr = comm.ExecuteReader();
                if (dr.Read() && dr.HasRows)
                {
                    strAcctCode = dr["EV200_CUST_NBR"].ToString();
                }
            }
            catch (Exception ex)
            {
                sendException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name, strEmailFrom, strEmailTo, ex.Message);
            }
            finally
            {
                conn.Close();
            }

            return strAcctCode;
        }

        public static string GetUserName(string strUserId)
        {
            string strUserName = "";
            SqlConnection conn = new SqlConnection(strEBMSConn);

            try
            {
                conn.Open();

                string strSQL = " select MM405_USER_NAME from " + strCompDatabase + ".dbo.MM405_USER_MASTER_EXT where MM405_USER_ID=@userid  ";
                SqlCommand comm = new SqlCommand(strSQL, conn);
                comm.Parameters.Add("@userid", SqlDbType.VarChar, 10).Value = strUserId;
                comm.CommandTimeout = nCommandTimeOut;

                SqlDataReader dr = comm.ExecuteReader();
                if (dr.Read() && dr.HasRows)
                {
                    strUserName = dr["MM405_USER_NAME"].ToString();
                }
            }
            catch (Exception ex)
            {
                sendException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name, strEmailFrom, strEmailTo, ex.Message);
            }
            finally
            {
                conn.Close();
            }

            return strUserName;
        }

        public static string GetDocumentHeading(int nDocHDGSEQ)
        {
            string strDoc_HDG_Desc = "";
            SqlConnection conn = new SqlConnection(strEBMSConn);

            try
            {
                conn.Open();

                string strSQL = " select MM442_HDG_DESC from " + strCompDatabase + ".dbo.MM442_DOC_HEADINGS where MM442_HDG_SEQ=@hdgseq and MM442_ORG_CODE='10'  ";
                SqlCommand comm = new SqlCommand(strSQL, conn);
                comm.Parameters.Add("@hdgseq", SqlDbType.Int).Value = nDocHDGSEQ;
                comm.CommandTimeout = nCommandTimeOut;

                SqlDataReader dr = comm.ExecuteReader();
                if (dr.Read() && dr.HasRows)
                {
                    strDoc_HDG_Desc = dr["MM442_HDG_DESC"].ToString();
                }
            }
            catch (Exception ex)
            {
                sendException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name, strEmailFrom, strEmailTo, ex.Message);
            }
            finally
            {
                conn.Close();
            }

            return strDoc_HDG_Desc;
        }

        public static DateTime getRecentSnapshotDateTime(DateTime dtDateTime)
        {
            DateTime dt = new DateTime();
            SqlConnection conn = new SqlConnection(strEBMSConn);

            try
            {
                conn.Open();

                string strSQL = " select top 1 SnapshotDateTime from SnapshotTimes where datediff(second, SnapshotDateTime,  @dtdatetime)>0  order by datediff(second, SnapshotDateTime,  @dtdatetime) asc  ";
                SqlCommand comm = new SqlCommand(strSQL, conn);
                comm.Parameters.Add("@dtdatetime", SqlDbType.DateTime).Value = dtDateTime;
                comm.CommandTimeout = nCommandTimeOut;

                SqlDataReader dr = comm.ExecuteReader();
                if (dr.Read() && dr.HasRows)
                {
                    dt = DateTime.Parse(dr["SnapshotDateTime"].ToString());
                }
            }
            catch (Exception ex)
            {
                sendException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name, strEmailFrom, strEmailTo, ex.Message);
            }
            finally
            {
                conn.Close();
            }
            return dt;
        }

        /// <summary>
        /// this function is trying to get the latest snapshotid from snapshottimes table.
        ///   top 1 of  order by datediff(second, SnapshotDateTime,  getdate()) asc makes the most recent record.
        /// </summary>
        /// <param name="dtDateTime"></param>
        /// <returns></returns>
        public static int getRecentSnapshotID(DateTime dtDateTime)
        {
            int nSnapshotID = 0;
            SqlConnection conn = new SqlConnection(strEBMSConn);

            try
            {
                conn.Open();

                string strSQL = " select top 1 SnapshotID from SnapshotTimes where datediff(second, SnapshotDateTime,  @dtdatetime)>=0  order by datediff(second, SnapshotDateTime,  @dtdatetime) asc  ";
                SqlCommand comm = new SqlCommand(strSQL, conn);
                comm.Parameters.Add("@dtdatetime", SqlDbType.DateTime).Value = dtDateTime;
                comm.CommandTimeout = nCommandTimeOut;

                SqlDataReader dr = comm.ExecuteReader();
                if (dr.Read() && dr.HasRows)
                {
                    nSnapshotID = int.Parse(dr["SnapshotID"].ToString());
                }
            }
            catch (Exception ex)
            {
                sendException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name, strEmailFrom, strEmailTo, ex.Message);
            }
            finally
            {
                conn.Close();
            }
            return nSnapshotID;
        }

        public static DateTime getSnapshotDateTimeFromId(int nSnapshotId)
        {
            DateTime Snapshotdate = DateTime.MinValue;
            SqlConnection conn = new SqlConnection(strEBMSConn);

            try
            {
                conn.Open();

                string strSQL = " select SnapshotDateTime from SnapshotTimes where SnapshotID=@snapshotid";
                SqlCommand comm = new SqlCommand(strSQL, conn);
                comm.Parameters.Add("@snapshotid", SqlDbType.Int).Value = nSnapshotId;
                comm.CommandTimeout = nCommandTimeOut;

                SqlDataReader dr = comm.ExecuteReader();
                if (dr.Read() && dr.HasRows)
                {
                    Snapshotdate = DateTime.Parse(dr["SnapshotDateTime"].ToString());
                }
            }
            catch (Exception ex)
            {
                sendException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name, strEmailFrom, strEmailTo, ex.Message);
            }
            finally
            {
                conn.Close();
            }
            return Snapshotdate;
        }

        public static void sendException(Exception ex, string strFunctionName, string strEmailFrom, string strEmailTo, string strSubject)
        {
            string strMachineName = Environment.MachineName;
            string strOSVersion = Environment.OSVersion.VersionString;
            string strUsername = Environment.UserName;
            string strBody = "";
            strBody = "There's error with function " + strFunctionName + ". <br/><br/>  Error Messages:<br/> " + ex.Message;

            AMP_Notification.sendEmail(strEmailFrom, strEmailTo, strSubject, strBody);
        }

        public static void sendErrorException(string strEmailFrom, string strEmailTo, string strSubject, AMP_Rules rule, EventInfo evt, Function_Info finfo, Notification_Dep_user dep, int nPrevSnapshotId, int nCurrSnapshotId, string strFunctionName, string logText)
        {
            SaveErrorLog(rule, evt, finfo, dep, nPrevSnapshotId, nCurrSnapshotId, strFunctionName, logText);
            string strBody = "";
            strBody = "There's error with function " + strFunctionName + ". <br/><br/> ";
            strBody = "Department: " + dep.DepartmentDesc + " (" + dep.DepartmentCode + ") <br/> ";
            strBody = "Department User: " + dep.UserId + " /" + dep.EmailAddress + "<br/> ";
            strBody = "Rule: " + rule.Rule_Name + " (" + rule.RuleId + ")  <br/> ";
            strBody = "Snapshot Previous:" + getSnapshotDateTimeFromId(nPrevSnapshotId) + " (" + nPrevSnapshotId.ToString() + ") <br/>";
            strBody = "Snapshot Current:" + getSnapshotDateTimeFromId(nCurrSnapshotId) + " (" + nCurrSnapshotId.ToString() + ") <br/>";
            strBody = "Event: " + evt.EventDesc + " (" + evt.EventId.ToString() + ") <br/>";
            strBody = "Function: " + finfo.FuncDesc + " <br/><br/>";
            strBody += "Error Messages:<br/> " + logText;

            AMP_Notification.sendEmail(strEmailFrom, strEmailTo, strSubject, strBody);
        }

        public static void SavePerformanceData(int nEventId, int nFuncNumbsers, DateTime dtStart, DateTime dtEnd)
        {
            SqlConnection conn = new SqlConnection(strEBMSConn);

            try
            {
                conn.Open();

                string strSQL = " INSERT INTO dbo.AMP_Performance (EventId,FuncNumbers,StartDateTime,EndDateTime,RunningMinutes) values ";
                strSQL += " (@eventid, @numberoffunc,@startdatetime, @enddatetime,@runningminutes ) ";
                SqlCommand comm = new SqlCommand(strSQL, conn);

                comm.Parameters.Add("@eventid", SqlDbType.Int).Value = nEventId;
                comm.Parameters.Add("@numberoffunc", SqlDbType.Int).Value = nFuncNumbsers;
                comm.Parameters.Add("@startdatetime", SqlDbType.DateTime).Value = dtStart;
                comm.Parameters.Add("@enddatetime", SqlDbType.DateTime).Value = dtEnd;

                comm.Parameters.Add("@runningminutes", SqlDbType.Int).Value = int.Parse((dtEnd - dtStart).TotalSeconds.ToString("R"));
                comm.CommandTimeout = nCommandTimeOut;

                comm.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                sendException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name, strEmailFrom, strEmailTo, ex.Message);
            }
            finally
            {
                conn.Close();
            }
        }

        public static void SaveLog(string strRuleCode, string strRuleName, DateTime dtPrev, DateTime dtCurr, string strDeptCode, int nRecords, string strFunctionName, string strUserId, string strEmailAddress, string logText)
        {

            SqlConnection conn = new SqlConnection(strEBMSConn);

            try
            {
                conn.Open();

                string strSQL = " insert into AMP_Logs (RuleCode, RuleName, SnapshotPrevious, SnapshotCurrent, DepartmentCode, RunDate, Records, Function_Name, UserId, EmailAddress, LogMessage) values (@rulecode, @rulename,@prevsnapshot, @currsnapshot, @deptcode, @dtRun, @records, @functionname, @userid, @emailaddress, @errmsg ) ";
                SqlCommand comm = new SqlCommand(strSQL, conn);

                comm.Parameters.Add("@rulecode", SqlDbType.VarChar, 20).Value = strRuleCode;
                comm.Parameters.Add("@rulename", SqlDbType.VarChar, 255).Value = strRuleName;
                comm.Parameters.Add("@prevsnapshot", SqlDbType.DateTime).Value = dtPrev;
                comm.Parameters.Add("@currsnapshot", SqlDbType.DateTime).Value = dtCurr;
                comm.Parameters.Add("@deptcode", SqlDbType.VarChar, 20).Value = strDeptCode;
                comm.Parameters.Add("@dtRun", SqlDbType.DateTime).Value = DateTime.Now;
                comm.Parameters.Add("@records", SqlDbType.Int).Value = nRecords;
                comm.Parameters.Add("@functionname", SqlDbType.VarChar, 50).Value = strFunctionName;
                comm.Parameters.Add("@userid", SqlDbType.VarChar, 50).Value = strUserId;
                comm.Parameters.Add("@emailaddress", SqlDbType.VarChar, 200).Value = strEmailAddress;
                comm.Parameters.Add("@errmsg", SqlDbType.NText).Value = logText;
                comm.CommandTimeout = nCommandTimeOut;

                comm.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                sendException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name, strEmailFrom, strEmailTo, ex.Message);
            }
            finally
            {
                conn.Close();
            }

            //Notification.sendEmail("Amendment_Error@mcec.com.au", "azheng@mcec.com.au", "EBMS Amendment Runtime Error", "<div>Rule Name: " + strRuleName + "<br/> Department Code:" + strDeptCode + "<br/> Function Name: " + strFunctionName + "<br/>Error Message : " + logText + "</div>");
        }

        public static void SaveErrorLog(AMP_Rules rule, EventInfo evt, Function_Info finfo, Notification_Dep_user dep, int nPrevSnapshotId, int nCurrSnapshotId, string strFunctionName, string logText)
        {

            SqlConnection conn = new SqlConnection(strEBMSConn);

            try
            {
                conn.Open();

                string strSQL = " insert into AMP_Error_Logs (RuleCode ,RuleName ,SnapshotPreviousId ,SnapshotCurrentId ,EventId ,Function_Id ,DepartmentCode ,DepartmentDesc ,Dep_User_Id ,EmailAddress ,RunDate ,Function_Name ,logMessage) ";
                strSQL += " values ";
                strSQL += " (@rulecode, @rulename,@prevsnapshotid, @currsnapshotid,@eventid, @functionid, @deptcode,@deptdesc, @deptuser, @emailaddress, @rundate,@funcname, @errmsg ) ";
                SqlCommand comm = new SqlCommand(strSQL, conn);

                comm.Parameters.Add("@rulecode", SqlDbType.VarChar, 20).Value = rule.RuleId;
                comm.Parameters.Add("@rulename", SqlDbType.VarChar, 255).Value = rule.Rule_Name;
                comm.Parameters.Add("@prevsnapshotid", SqlDbType.Int).Value = nPrevSnapshotId;
                comm.Parameters.Add("@currsnapshotid", SqlDbType.Int).Value = nCurrSnapshotId;
                comm.Parameters.Add("@eventid", SqlDbType.Int).Value = evt.EventId;
                if (finfo != null)
                    comm.Parameters.Add("@functionid", SqlDbType.Int).Value = finfo.FuncId;
                else
                    comm.Parameters.Add("@functionid", SqlDbType.Int).Value = 0;
                if (dep != null)
                {
                    comm.Parameters.Add("@deptcode", SqlDbType.VarChar, 20).Value = dep.DepartmentCode;
                    comm.Parameters.Add("@deptdesc", SqlDbType.VarChar, 255).Value = dep.DepartmentDesc;
                    comm.Parameters.Add("@deptuser", SqlDbType.VarChar, 20).Value = dep.UserId;
                    comm.Parameters.Add("@emailaddress", SqlDbType.VarChar, 200).Value = dep.EmailAddress;
                }
                else
                {
                    comm.Parameters.Add("@deptcode", SqlDbType.VarChar, 20).Value = "";
                    comm.Parameters.Add("@deptdesc", SqlDbType.VarChar, 255).Value = "";
                    comm.Parameters.Add("@deptuser", SqlDbType.VarChar, 20).Value = "";
                    comm.Parameters.Add("@emailaddress", SqlDbType.VarChar, 200).Value = "";
                }
                comm.Parameters.Add("@rundate", SqlDbType.DateTime).Value = DateTime.Now;
                comm.Parameters.Add("@funcname", SqlDbType.VarChar, 50).Value = strFunctionName;
                comm.Parameters.Add("@errmsg", SqlDbType.NText).Value = logText;
                comm.CommandTimeout = nCommandTimeOut;

                comm.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                sendException(ex, "SaveErrorLog", "Amendment_Error@mcec.com.au", "azheng@mcec.com.au", "EBMS Amendment Runtime Error");
            }
            finally
            {
                conn.Close();
            }
        }

        public static void initDB(int nCurrSnapshotId, int nPrevSnapshotId, List<EventInfo> lstEvent)
        {
            ClearFunctionSnapshot();
            InsertCurrentFunction(nCurrSnapshotId, lstEvent);
            InsertPrevFunction(nPrevSnapshotId, lstEvent);

            ClearNotesSnapshot();
            InsertCurrentNotes(nCurrSnapshotId, lstEvent);
            InsertPrevNotes(nPrevSnapshotId, lstEvent);

            ClearOrderSnapshot();
            InsertCurrOrder(nCurrSnapshotId, lstEvent);
            InsertPrevOrder(nPrevSnapshotId, lstEvent);

            ClearOrderDetailSnapshot();
            InsertCurrentOrderDetail(nCurrSnapshotId, lstEvent);
            InsertPrevOrderDetail(nPrevSnapshotId, lstEvent);

            ClearDocumentSnapshot();
            InsertCurrentDocument(nCurrSnapshotId, lstEvent);
            InsertPrevDocument(nPrevSnapshotId, lstEvent);
        }

        public static bool isSnapshotFinished(DateTime dtCurrent)
        {
            bool isFinished = false;

            SqlConnection conn = new SqlConnection(strEBMSConn);

            try
            {
                conn.Open();

                string strSQL = "select top 1 SnapshotStatus as SnapshotStatus from SnapshotTimes where datediff(second,  SnapshotDateTime,  @currentdatetime) >0 order by SnapshotDateTime desc ";
                SqlCommand comm = new SqlCommand(strSQL, conn);
                comm.Parameters.Add("@currentdatetime", SqlDbType.DateTime).Value = dtCurrent;

                comm.CommandTimeout = nCommandTimeOut;

                SqlDataReader dr = comm.ExecuteReader();
                if (dr.Read())
                {
                    isFinished = Convert.ToBoolean(dr["SnapshotStatus"] == DBNull.Value ? 0 : dr["SnapshotStatus"]);
                }
            }
            catch (Exception ex)
            {
                sendException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name, strEmailFrom, strEmailTo, ex.Message);
            }
            finally
            {
                conn.Close();
            }

            return isFinished;
        }


        #region initCompareTables

        private static void ClearFunctionSnapshot()
        {
            SqlConnection conn = new SqlConnection(strEBMSConn);

            try
            {
                conn.Open();

                string strSQL1 = "TRUNCATE TABLE EV700_FUNC_MASTER_Curr";

                SqlCommand comm1 = new SqlCommand(strSQL1, conn);
                comm1.CommandTimeout = nCommandTimeOut;

                comm1.ExecuteNonQuery();

                string strSQL2 = "TRUNCATE TABLE EV700_FUNC_MASTER_Prev";

                SqlCommand comm2 = new SqlCommand(strSQL2, conn);
                comm2.CommandTimeout = nCommandTimeOut;
                comm2.ExecuteNonQuery();

            }
            catch (Exception ex)
            {
                sendException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name, strEmailFrom, strEmailTo, ex.Message);
            }
            finally
            {
                conn.Close();
            }
        }

        private static void InsertCurrentFunction(int nSnapshotId, List<EventInfo> lstEvent)
        {
            SqlConnection conn = new SqlConnection(strEBMSConn);

            try
            {
                conn.Open();

                string strSQL = "INSERT INTO EV700_FUNC_MASTER_Curr (EV700_ORG_CODE, EV700_EVT_ID, EV700_FUNC_ID, EV700_FUNC_SEQ, EV700_FUNC_LEVEL, EV700_FUNC_DESC, EV700_SPACE, EV700_FUNC_TYPE, EV700_STATUS_CODE,  EV700_STAT_FUNC,EV700_ALT_FUNC_DESC,EV700_ALT_FUNC_DESC2,EV700_START_DATE_ISO, ";
                strSQL += " EV700_END_DATE_ISO, EV700_START_TIME_ISO, EV700_END_TIME_ISO, EV700_ENT_DATE_ISO, EV700_UPD_DATE_ISO, EV700_ENT_TIME_ISO )  ";
                strSQL += " select EV700_ORG_CODE, EV700_EVT_ID, EV700_FUNC_ID, EV700_FUNC_SEQ, EV700_FUNC_LEVEL, EV700_FUNC_DESC, EV700_SPACE, EV700_FUNC_TYPE, EV700_STATUS_CODE, EV700_STAT_FUNC,EV700_ALT_FUNC_DESC,EV700_ALT_FUNC_DESC2, EV700_START_DATE_ISO, EV700_END_DATE_ISO, EV700_START_TIME_ISO, ";
                strSQL += " EV700_END_TIME_ISO, EV700_ENT_DATE_ISO, EV700_UPD_DATE_ISO, EV700_ENT_TIME_ISO  from EV700_FUNC_MASTER  ";
                strSQL += " where EV700_SNAPSHOT_ID=" + nSnapshotId.ToString() + "AND EV700_EVT_ID  in (1000";
                if (lstEvent.Count > 0)
                {
                    foreach (EventInfo evt in lstEvent)
                    {
                        strSQL += "," + evt.EventId;
                    }
                }

                strSQL += ")";

                SqlCommand comm = new SqlCommand(strSQL, conn);
                comm.CommandTimeout = nCommandTimeOut;

                comm.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                sendException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name, strEmailFrom, strEmailTo, ex.Message);
            }
            finally
            {
                conn.Close();
            }
        }

        private static void InsertPrevFunction(int nSnapshotId, List<EventInfo> lstEvent)
        {
            SqlConnection conn = new SqlConnection(strEBMSConn);

            try
            {
                conn.Open();

                string strSQL = "INSERT INTO EV700_FUNC_MASTER_Prev (EV700_ORG_CODE, EV700_EVT_ID, EV700_FUNC_ID, EV700_FUNC_SEQ, EV700_FUNC_LEVEL, EV700_FUNC_DESC, EV700_SPACE, EV700_FUNC_TYPE, EV700_STATUS_CODE, EV700_STAT_FUNC,EV700_ALT_FUNC_DESC,EV700_ALT_FUNC_DESC2, EV700_START_DATE_ISO, ";
                strSQL += " EV700_END_DATE_ISO, EV700_START_TIME_ISO, EV700_END_TIME_ISO, EV700_ENT_DATE_ISO, EV700_UPD_DATE_ISO, EV700_ENT_TIME_ISO )  ";
                strSQL += " select EV700_ORG_CODE, EV700_EVT_ID, EV700_FUNC_ID, EV700_FUNC_SEQ, EV700_FUNC_LEVEL, EV700_FUNC_DESC, EV700_SPACE, EV700_FUNC_TYPE, EV700_STATUS_CODE, EV700_STAT_FUNC,EV700_ALT_FUNC_DESC,EV700_ALT_FUNC_DESC2, EV700_START_DATE_ISO, EV700_END_DATE_ISO, EV700_START_TIME_ISO, ";
                strSQL += " EV700_END_TIME_ISO, EV700_ENT_DATE_ISO, EV700_UPD_DATE_ISO, EV700_ENT_TIME_ISO  from EV700_FUNC_MASTER  ";
                strSQL += " where EV700_SNAPSHOT_ID=" + nSnapshotId.ToString() + "AND EV700_EVT_ID  in (1000";
                if (lstEvent.Count > 0)
                {
                    foreach (EventInfo evt in lstEvent)
                    {
                        strSQL += "," + evt.EventId;
                    }
                }

                strSQL += ")";

                SqlCommand comm = new SqlCommand(strSQL, conn);
                comm.CommandTimeout = nCommandTimeOut;
                comm.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                sendException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name, strEmailFrom, strEmailTo, ex.Message);
            }
            finally
            {
                conn.Close();
            }
        }

        private static void ClearNotesSnapshot()
        {
            SqlConnection conn = new SqlConnection(strEBMSConn);

            try
            {
                conn.Open();

                string strSQL1 = "TRUNCATE TABLE CC025_NOTES_EXT_Curr";

                SqlCommand comm1 = new SqlCommand(strSQL1, conn);
                comm1.CommandTimeout = nCommandTimeOut;
                comm1.ExecuteNonQuery();

                string strSQL2 = "TRUNCATE TABLE CC025_NOTES_EXT_Prev";

                SqlCommand comm2 = new SqlCommand(strSQL2, conn);
                comm2.CommandTimeout = nCommandTimeOut;
                comm2.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                sendException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name, strEmailFrom, strEmailTo, ex.Message);
            }
            finally
            {
                conn.Close();
            }
        }

        private static void InsertCurrentNotes(int nSnapshotId, List<EventInfo> lstEvent)
        {
            SqlConnection conn = new SqlConnection(strEBMSConn);

            try
            {
                conn.Open();

                string strSQL = " INSERT INTO CC025_NOTES_EXT_Curr (CC025_ORG_CODE ,CC025_NOTE_TYPE ,CC025_NOTE_CODE ,CC025_NOTE_HDR_SEQ ,CC025_NOTE_DESC ,CC025_NOTE_PRT_CODE ,CC025_NOTE_TEXT ,";
                strSQL += " CC025_NOTE_CLASS ,CC025_UPD_DATE ,CC025_UPD_USER_ID ,CC025_ENT_DATE ,CC025_ENT_USER_ID ,CC025_EVT_ID ,CC025_FUNC_ID ,CC025_EXT_ACCT_CODE ,CC025_UPD_TIME ,";
                strSQL += " CC025_ENT_TIME ,CC025_INVOICE ,CC025_ORDER ,CC025_ORD_LINE ,CC025_AR_SEQ ,CC025_CONTRACT_NBR ,CC025_CONTRACT_SEQ ,CC025_HTML_TEXT)";
                strSQL += " SELECT CC025_ORG_CODE ,CC025_NOTE_TYPE ,CC025_NOTE_CODE ,CC025_NOTE_HDR_SEQ ,CC025_NOTE_DESC ,CC025_NOTE_PRT_CODE ,CC025_NOTE_TEXT ,CC025_NOTE_CLASS ,";
                strSQL += " CC025_UPD_DATE ,CC025_UPD_USER_ID ,CC025_ENT_DATE ,CC025_ENT_USER_ID ,CC025_EVT_ID ,CC025_FUNC_ID ,CC025_EXT_ACCT_CODE ,CC025_UPD_TIME ,CC025_ENT_TIME ,";
                strSQL += " CC025_INVOICE ,CC025_ORDER ,CC025_ORD_LINE ,CC025_AR_SEQ ,CC025_CONTRACT_NBR ,CC025_CONTRACT_SEQ ,CC025_HTML_TEXT ";
                strSQL += " FROM CC025_NOTES_EXT WHERE CC025_SNAPSHOT_ID=" + nSnapshotId.ToString() + "AND CC025_EVT_ID  in (1000";
                if (lstEvent.Count > 0)
                {
                    foreach (EventInfo evt in lstEvent)
                    {
                        strSQL += "," + evt.EventId;
                    }
                }

                strSQL += ")";

                SqlCommand comm = new SqlCommand(strSQL, conn);
                comm.CommandTimeout = nCommandTimeOut;

                comm.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                sendException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name, strEmailFrom, strEmailTo, ex.Message);
            }
            finally
            {
                conn.Close();
            }
        }

        private static void InsertPrevNotes(int nSnapshotId, List<EventInfo> lstEvent)
        {
            SqlConnection conn = new SqlConnection(strEBMSConn);

            try
            {
                conn.Open();

                string strSQL = " INSERT INTO CC025_NOTES_EXT_Prev (CC025_ORG_CODE ,CC025_NOTE_TYPE ,CC025_NOTE_CODE ,CC025_NOTE_HDR_SEQ ,CC025_NOTE_DESC ,CC025_NOTE_PRT_CODE ,CC025_NOTE_TEXT ,";
                strSQL += " CC025_NOTE_CLASS ,CC025_UPD_DATE ,CC025_UPD_USER_ID ,CC025_ENT_DATE ,CC025_ENT_USER_ID ,CC025_EVT_ID ,CC025_FUNC_ID ,CC025_EXT_ACCT_CODE ,CC025_UPD_TIME ,";
                strSQL += " CC025_ENT_TIME ,CC025_INVOICE ,CC025_ORDER ,CC025_ORD_LINE ,CC025_AR_SEQ ,CC025_CONTRACT_NBR ,CC025_CONTRACT_SEQ ,CC025_HTML_TEXT)";
                strSQL += " SELECT CC025_ORG_CODE ,CC025_NOTE_TYPE ,CC025_NOTE_CODE ,CC025_NOTE_HDR_SEQ ,CC025_NOTE_DESC ,CC025_NOTE_PRT_CODE ,CC025_NOTE_TEXT ,CC025_NOTE_CLASS ,";
                strSQL += " CC025_UPD_DATE ,CC025_UPD_USER_ID ,CC025_ENT_DATE ,CC025_ENT_USER_ID ,CC025_EVT_ID ,CC025_FUNC_ID ,CC025_EXT_ACCT_CODE ,CC025_UPD_TIME ,CC025_ENT_TIME ,";
                strSQL += " CC025_INVOICE ,CC025_ORDER ,CC025_ORD_LINE ,CC025_AR_SEQ ,CC025_CONTRACT_NBR ,CC025_CONTRACT_SEQ ,CC025_HTML_TEXT ";
                strSQL += " FROM CC025_NOTES_EXT WHERE CC025_SNAPSHOT_ID=" + nSnapshotId.ToString() + "AND CC025_EVT_ID  in (1000";
                if (lstEvent.Count > 0)
                {
                    foreach (EventInfo evt in lstEvent)
                    {
                        strSQL += "," + evt.EventId;
                    }
                }

                strSQL += ")";

                SqlCommand comm = new SqlCommand(strSQL, conn);
                comm.CommandTimeout = nCommandTimeOut;

                comm.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                sendException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name, strEmailFrom, strEmailTo, ex.Message);
            }
            finally
            {
                conn.Close();
            }
        }

        private static void ClearOrderSnapshot()
        {
            SqlConnection conn = new SqlConnection(strEBMSConn);

            try
            {
                conn.Open();

                string strSQL1 = "TRUNCATE TABLE ER100_ACCT_ORDER_Curr";

                SqlCommand comm1 = new SqlCommand(strSQL1, conn);
                comm1.CommandTimeout = nCommandTimeOut;
                comm1.ExecuteNonQuery();

                string strSQL2 = "TRUNCATE TABLE ER100_ACCT_ORDER_Prev";

                SqlCommand comm2 = new SqlCommand(strSQL2, conn);
                comm2.CommandTimeout = nCommandTimeOut;
                comm2.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                sendException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name, strEmailFrom, strEmailTo, ex.Message);
            }
            finally
            {
                conn.Close();
            }
        }

        private static void InsertCurrOrder(int nSnapshotID, List<EventInfo> lstEvent)
        {
            SqlConnection conn = new SqlConnection(strEBMSConn);

            try
            {
                conn.Open();

                string strSQL = " INSERT INTO dbo.ER100_ACCT_ORDER_Curr(ER100_ORG_CODE, ER100_ORD_NBR, ER100_EVT_ID, ER100_ORD_TYPE, ER100_FUNC_ID, ER100_ORD_DATE, ER100_ORD_STS, ";
                strSQL += " ER100_ORD_ACCT, ER100_BILL_TO_CUST, ER100_ORD_TOT, ER100_ORD_TAX, ER100_BOOTH_NBR, ER100_ENT_DATE_ISO, ER100_ENT_USER_ID, ER100_UPD_DATE_ISO,  ";
                strSQL += " ER100_UPD_USER_ID, ER100_ASSIGNMENT_NAME, ER100_ORD_ACCT_REP, ER100_EXHIBITOR_ID) ";
                strSQL += " SELECT ER100_ORG_CODE, ER100_ORD_NBR, ER100_EVT_ID, ER100_ORD_TYPE, ER100_FUNC_ID, ER100_ORD_DATE, ER100_ORD_STS,  ";
                strSQL += " ER100_ORD_ACCT, ER100_BILL_TO_CUST, ER100_ORD_TOT, ER100_ORD_TAX, ER100_BOOTH_NBR, ER100_ENT_DATE_ISO, ER100_ENT_USER_ID, ER100_UPD_DATE_ISO,  ";
                strSQL += " ER100_UPD_USER_ID, ER100_ASSIGNMENT_NAME, ER100_ORD_ACCT_REP, ER100_EXHIBITOR_ID FROM ER100_ACCT_ORDER WHERE ER100_SNAPSHOT_ID= " + nSnapshotID.ToString() + " AND ER100_EVT_ID  in (1000";
                if (lstEvent.Count > 0)
                {
                    foreach (EventInfo evt in lstEvent)
                    {
                        strSQL += "," + evt.EventId;
                    }
                }
                strSQL += ")";

                SqlCommand comm = new SqlCommand(strSQL, conn);
                comm.CommandTimeout = nCommandTimeOut;

                comm.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                sendException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name, strEmailFrom, strEmailTo, ex.Message);
            }
            finally
            {
                conn.Close();
            }
        }

        private static void InsertPrevOrder(int nSnapshotID, List<EventInfo> lstEvent)
        {
            SqlConnection conn = new SqlConnection(strEBMSConn);

            try
            {
                conn.Open();

                string strSQL = " INSERT INTO dbo.ER100_ACCT_ORDER_Prev(ER100_ORG_CODE, ER100_ORD_NBR, ER100_EVT_ID, ER100_ORD_TYPE, ER100_FUNC_ID, ER100_ORD_DATE, ER100_ORD_STS, ";
                strSQL += " ER100_ORD_ACCT, ER100_BILL_TO_CUST, ER100_ORD_TOT, ER100_ORD_TAX, ER100_BOOTH_NBR, ER100_ENT_DATE_ISO, ER100_ENT_USER_ID, ER100_UPD_DATE_ISO,  ";
                strSQL += " ER100_UPD_USER_ID, ER100_ASSIGNMENT_NAME, ER100_ORD_ACCT_REP, ER100_EXHIBITOR_ID) ";
                strSQL += " SELECT ER100_ORG_CODE, ER100_ORD_NBR, ER100_EVT_ID, ER100_ORD_TYPE, ER100_FUNC_ID, ER100_ORD_DATE, ER100_ORD_STS,  ";
                strSQL += " ER100_ORD_ACCT, ER100_BILL_TO_CUST, ER100_ORD_TOT, ER100_ORD_TAX, ER100_BOOTH_NBR, ER100_ENT_DATE_ISO, ER100_ENT_USER_ID, ER100_UPD_DATE_ISO,  ";
                strSQL += " ER100_UPD_USER_ID, ER100_ASSIGNMENT_NAME, ER100_ORD_ACCT_REP, ER100_EXHIBITOR_ID FROM ER100_ACCT_ORDER WHERE ER100_SNAPSHOT_ID= " + nSnapshotID.ToString() + "AND ER100_EVT_ID  in (1000";
                if (lstEvent.Count > 0)
                {
                    foreach (EventInfo evt in lstEvent)
                    {
                        strSQL += "," + evt.EventId;
                    }
                }

                strSQL += ")";

                SqlCommand comm = new SqlCommand(strSQL, conn);
                comm.CommandTimeout = nCommandTimeOut;

                comm.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                sendException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name, strEmailFrom, strEmailTo, ex.Message);
            }
            finally
            {
                conn.Close();
            }
        }


        private static void ClearOrderDetailSnapshot()
        {
            SqlConnection conn = new SqlConnection(strEBMSConn);

            try
            {
                conn.Open();

                string strSQL1 = "TRUNCATE TABLE ER101_ACCT_ORDER_DTL_Curr";

                SqlCommand comm1 = new SqlCommand(strSQL1, conn);
                comm1.CommandTimeout = nCommandTimeOut;
                comm1.ExecuteNonQuery();

                string strSQL2 = "TRUNCATE TABLE ER101_ACCT_ORDER_DTL_Prev";

                SqlCommand comm2 = new SqlCommand(strSQL2, conn);
                comm2.CommandTimeout = nCommandTimeOut;
                comm2.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                sendException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name, strEmailFrom, strEmailTo, ex.Message);
            }
            finally
            {
                conn.Close();
            }
        }

        private static void InsertCurrentOrderDetail(int nSnapshotID, List<EventInfo> lstEvent)
        {
            SqlConnection conn = new SqlConnection(strEBMSConn);

            try
            {
                conn.Open();

                string strSQL = "Insert into ER101_ACCT_ORDER_DTL_Curr(ER101_ORG_CODE, ER101_ORD_NBR, ER101_ORD_LINE,ER101_LIN_NBR, ER101_EVT_ID, ER101_FUNC_ID, ER101_SETUP_ID, ER101_DEPT, ER101_ENT_DATE_ISO, ER101_ENT_USER_ID, ER101_UPD_DATE_ISO, ";
                strSQL += " ER101_UPD_USER_ID, ER101_PHASE, ER101_RES_CLASS, ER101_NEW_RES_TYPE, ER101_RES_CODE, ER101_RES_QTY, ER101_UOM, ER101_START_DATE_ISO, ER101_END_DATE_ISO, ER101_START_TIME_ISO, ER101_END_TIME_ISO, ";
                strSQL += " ER101_DESC, ER101_MGMT_RPT_CODE) select ER101_ORG_CODE, ER101_ORD_NBR, ER101_ORD_LINE, ER101_LIN_NBR, ER101_EVT_ID, ER101_FUNC_ID, ER101_SETUP_ID, ER101_DEPT, ER101_ENT_DATE_ISO, ER101_ENT_USER_ID, ";
                strSQL += " ER101_UPD_DATE_ISO, ER101_UPD_USER_ID, ER101_PHASE, ER101_RES_CLASS, ER101_NEW_RES_TYPE, ER101_RES_CODE, ER101_RES_QTY, ER101_UOM, ER101_START_DATE_ISO, ER101_END_DATE_ISO, ER101_START_TIME_ISO, ";
                strSQL += " ER101_END_TIME_ISO, ER101_DESC, ER101_MGMT_RPT_CODE from ER101_ACCT_ORDER_DTL where ER101_SNAPSHOT_ID=" + nSnapshotID.ToString() + "AND ER101_EVT_ID  in (1000";
                if (lstEvent.Count > 0)
                {
                    foreach (EventInfo evt in lstEvent)
                    {
                        strSQL += "," + evt.EventId;
                    }
                }

                strSQL += ")";

                SqlCommand comm = new SqlCommand(strSQL, conn);
                comm.CommandTimeout = nCommandTimeOut;

                comm.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                sendException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name, strEmailFrom, strEmailTo, ex.Message);
            }
            finally
            {
                conn.Close();
            }
        }

        private static void InsertPrevOrderDetail(int nSnapshotID, List<EventInfo> lstEvent)
        {
            SqlConnection conn = new SqlConnection(strEBMSConn);

            try
            {
                conn.Open();

                string strSQL = "Insert into ER101_ACCT_ORDER_DTL_Prev(ER101_ORG_CODE, ER101_ORD_NBR, ER101_ORD_LINE,ER101_LIN_NBR, ER101_EVT_ID, ER101_FUNC_ID, ER101_SETUP_ID, ER101_DEPT, ER101_ENT_DATE_ISO, ER101_ENT_USER_ID, ER101_UPD_DATE_ISO, ";
                strSQL += " ER101_UPD_USER_ID, ER101_PHASE, ER101_RES_CLASS, ER101_NEW_RES_TYPE, ER101_RES_CODE, ER101_RES_QTY, ER101_UOM, ER101_START_DATE_ISO, ER101_END_DATE_ISO, ER101_START_TIME_ISO, ER101_END_TIME_ISO, ";
                strSQL += " ER101_DESC, ER101_MGMT_RPT_CODE) select ER101_ORG_CODE, ER101_ORD_NBR, ER101_ORD_LINE,ER101_LIN_NBR, ER101_EVT_ID, ER101_FUNC_ID, ER101_SETUP_ID, ER101_DEPT, ER101_ENT_DATE_ISO, ER101_ENT_USER_ID, ";
                strSQL += " ER101_UPD_DATE_ISO, ER101_UPD_USER_ID, ER101_PHASE, ER101_RES_CLASS, ER101_NEW_RES_TYPE, ER101_RES_CODE, ER101_RES_QTY, ER101_UOM, ER101_START_DATE_ISO, ER101_END_DATE_ISO, ER101_START_TIME_ISO, ";
                strSQL += " ER101_END_TIME_ISO, ER101_DESC, ER101_MGMT_RPT_CODE from ER101_ACCT_ORDER_DTL where ER101_SNAPSHOT_ID= " + nSnapshotID.ToString() + "AND ER101_EVT_ID  in (1000";
                if (lstEvent.Count > 0)
                {
                    foreach (EventInfo evt in lstEvent)
                    {
                        strSQL += "," + evt.EventId;
                    }
                }

                strSQL += ")";

                SqlCommand comm = new SqlCommand(strSQL, conn);
                comm.CommandTimeout = nCommandTimeOut;

                comm.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                sendException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name, strEmailFrom, strEmailTo, ex.Message);
            }
            finally
            {
                conn.Close();
            }
        }

        private static void ClearDocumentSnapshot()
        {
            SqlConnection conn = new SqlConnection(strEBMSConn);

            try
            {
                conn.Open();

                string strSQL1 = "TRUNCATE TABLE MM446_DOC_ENTRY_Curr";

                SqlCommand comm1 = new SqlCommand(strSQL1, conn);
                comm1.CommandTimeout = nCommandTimeOut;
                comm1.ExecuteNonQuery();

                string strSQL2 = "TRUNCATE TABLE MM446_DOC_ENTRY_Prev";

                SqlCommand comm2 = new SqlCommand(strSQL2, conn);
                comm2.CommandTimeout = nCommandTimeOut;
                comm2.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                sendException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name, strEmailFrom, strEmailTo, ex.Message);
            }
            finally
            {
                conn.Close();
            }
        }

        private static void InsertCurrentDocument(int nSnapshotCurrentID, List<EventInfo> lstEvent)
        {
            SqlConnection conn = new SqlConnection(strEBMSConn);

            try
            {
                conn.Open();

                string strSQL = "INSERT INTO MM446_DOC_ENTRY_Curr (MM446_DOC_CLASS, MM446_DOC_SEQ_KEY, MM446_DOC_ENTRY_ID, MM446_DOC_DESC, MM446_DOC_PATH, MM446_DOC_SUBJ, MM446_TIMES_VIEW, MM446_ORG_CODE, MM446_EVENT, MM446_EV_FUNC, MM446_ACCOUNT, MM446_AC_CONTACT, MM446_ENT_USER_ID, MM446_UPD_USER_ID, MM446_HAS_ATTACH, MM446_REF_DOC_CLASS, MM446_REF_DOC_SEQ_KEY, MM446_DOC_PURGED, MM446_SENSITIVITY, MM446_MERGE_FILE, MM446_ENT_STAMP, MM446_UPD_STAMP, MM446_CONTRACT_SEQ, MM446_HEADING_SEQ_1, MM446_HEADING_SEQ_2, MM446_DOC_STS, MM446_STS_STAMP, MM446_STS_USER_ID, MM446_VERSION_CONTROL, MM446_SEND_REC_FLAG, MM446_WORD_MERGE) ";
                strSQL += " SELECT MM446_DOC_CLASS, MM446_DOC_SEQ_KEY, MM446_DOC_ENTRY_ID, MM446_DOC_DESC, MM446_DOC_PATH, MM446_DOC_SUBJ, MM446_TIMES_VIEW, MM446_ORG_CODE, MM446_EVENT, MM446_EV_FUNC, MM446_ACCOUNT, MM446_AC_CONTACT, MM446_ENT_USER_ID, MM446_UPD_USER_ID, MM446_HAS_ATTACH, MM446_REF_DOC_CLASS, MM446_REF_DOC_SEQ_KEY, MM446_DOC_PURGED, MM446_SENSITIVITY, MM446_MERGE_FILE, MM446_ENT_STAMP, MM446_UPD_STAMP, MM446_CONTRACT_SEQ, MM446_HEADING_SEQ_1, MM446_HEADING_SEQ_2, MM446_DOC_STS, MM446_STS_STAMP, MM446_STS_USER_ID, MM446_VERSION_CONTROL, MM446_SEND_REC_FLAG, MM446_WORD_MERGE from MM446_DOC_ENTRY  ";
                strSQL += " where MM446_SNAPSHOT_ID=" + nSnapshotCurrentID.ToString() + "AND MM446_EVENT  in (1000";
                if (lstEvent.Count > 0)
                {
                    foreach (EventInfo evt in lstEvent)
                    {
                        strSQL += "," + evt.EventId;
                    }
                }

                strSQL += ")";

                SqlCommand comm = new SqlCommand(strSQL, conn);
                comm.CommandTimeout = nCommandTimeOut;

                comm.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                sendException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name, strEmailFrom, strEmailTo, ex.Message);
            }
            finally
            {
                conn.Close();
            }
        }

        private static void InsertPrevDocument(int nSnapshotPreviousID, List<EventInfo> lstEvent)
        {
            SqlConnection conn = new SqlConnection(strEBMSConn);

            try
            {
                conn.Open();

                string strSQL = "INSERT INTO MM446_DOC_ENTRY_Prev (MM446_DOC_CLASS, MM446_DOC_SEQ_KEY, MM446_DOC_ENTRY_ID, MM446_DOC_DESC, MM446_DOC_PATH, MM446_DOC_SUBJ, MM446_TIMES_VIEW, MM446_ORG_CODE, MM446_EVENT, MM446_EV_FUNC, MM446_ACCOUNT, MM446_AC_CONTACT, MM446_ENT_USER_ID, MM446_UPD_USER_ID, MM446_HAS_ATTACH, MM446_REF_DOC_CLASS, MM446_REF_DOC_SEQ_KEY, MM446_DOC_PURGED, MM446_SENSITIVITY, MM446_MERGE_FILE, MM446_ENT_STAMP, MM446_UPD_STAMP, MM446_CONTRACT_SEQ, MM446_HEADING_SEQ_1, MM446_HEADING_SEQ_2, MM446_DOC_STS, MM446_STS_STAMP, MM446_STS_USER_ID, MM446_VERSION_CONTROL, MM446_SEND_REC_FLAG, MM446_WORD_MERGE) ";
                strSQL += " SELECT MM446_DOC_CLASS, MM446_DOC_SEQ_KEY, MM446_DOC_ENTRY_ID, MM446_DOC_DESC, MM446_DOC_PATH, MM446_DOC_SUBJ, MM446_TIMES_VIEW, MM446_ORG_CODE, MM446_EVENT, MM446_EV_FUNC, MM446_ACCOUNT, MM446_AC_CONTACT, MM446_ENT_USER_ID, MM446_UPD_USER_ID, MM446_HAS_ATTACH, MM446_REF_DOC_CLASS, MM446_REF_DOC_SEQ_KEY, MM446_DOC_PURGED, MM446_SENSITIVITY, MM446_MERGE_FILE, MM446_ENT_STAMP, MM446_UPD_STAMP, MM446_CONTRACT_SEQ, MM446_HEADING_SEQ_1, MM446_HEADING_SEQ_2, MM446_DOC_STS, MM446_STS_STAMP, MM446_STS_USER_ID, MM446_VERSION_CONTROL, MM446_SEND_REC_FLAG, MM446_WORD_MERGE from MM446_DOC_ENTRY  ";
                strSQL += " where MM446_SNAPSHOT_ID=" + nSnapshotPreviousID.ToString() + "AND MM446_EVENT  in (1000";
                if (lstEvent.Count > 0)
                {
                    foreach (EventInfo evt in lstEvent)
                    {
                        strSQL += "," + evt.EventId;
                    }
                }

                strSQL += ")";

                SqlCommand comm = new SqlCommand(strSQL, conn);
                comm.CommandTimeout = nCommandTimeOut;

                comm.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                sendException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name, strEmailFrom, strEmailTo, ex.Message);
            }
            finally
            {
                conn.Close();
            }
        }

        #endregion
    }
}
