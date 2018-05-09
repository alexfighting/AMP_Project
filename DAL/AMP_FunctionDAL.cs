using Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace DAL
{
    public class AMP_FunctionDAL
    {
        public static string strEBMSConn = Properties.Settings.Default.strEBMSConn;
        public static string strCompDatabase = Properties.Settings.Default.strCompDatabase;
        public static string strEmailFrom = Properties.Settings.Default.Amendment_Error_EmailAddressFrom;
        public static string strEmailTo = Properties.Settings.Default.Amendment_Error_EmailAddressTo;
        public static int nCommandTimeOut = Properties.Settings.Default.nCommandTimeOut;

        public static AMP_Rules rule;

        public static Notification_Dep_user dep;

        public static DateTime dtSnapshotCurrent, dtSnapshotPrevious;

        public static int nSnapshotCurrentID, nSnapshotPreviousID;

        public static EventInfo evt;

        public static Function_Info finfo;

        public static EventAccountMessage eMsg;


        public static string GetFuncHierarchyDescFromPrd(int nEventId, int nFuncId)
        {
            string strDesc = "";
            SqlConnection conn = new SqlConnection(strEBMSConn);

            try
            {
                conn.Open();

                string strSQL = " select EV700_FUNC_DESC,EV700_FUNC_ID,EV700_FUNC_LEVEL,EV700_PARENT_FUNC_ID from " + strCompDatabase + ".dbo.EV700_FUNC_MASTER where EV700_EVT_ID=@eventid and EV700_FUNC_ID=@funcid  ";
                SqlCommand comm = new SqlCommand(strSQL, conn);
                comm.Parameters.Add("@eventid", SqlDbType.Int).Value = nEventId;
                comm.Parameters.Add("@funcid", SqlDbType.Int).Value = nFuncId;
                comm.CommandTimeout = nCommandTimeOut;

                SqlDataReader dr = comm.ExecuteReader();
                if (dr.Read() && dr.HasRows)
                {
                    if (dr["EV700_FUNC_LEVEL"].ToString() == "1")
                    {
                        strDesc = dr["EV700_FUNC_DESC"].ToString();
                    }
                    else
                    {
                        strDesc = GetFuncHierarchyDescFromPrd(nEventId, int.Parse(dr["EV700_PARENT_FUNC_ID"].ToString())) +" -> "+ dr["EV700_FUNC_DESC"].ToString();
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

            return strDesc;
        }     

        public static string GetFuncDesc(int nEventId, int nFuncId, int nSnapshotId)
        {
            string strAcctCode = "";
            SqlConnection conn = new SqlConnection(strEBMSConn);

            try
            {
                conn.Open();

                string strSQL = " select EV700_FUNC_DESC from EV700_FUNC_MASTER where EV700_EVT_ID=@eventid and EV700_FUNC_ID=@funcid and EV700_SNAPSHOT_ID=@prevsnapshotid";
                SqlCommand comm = new SqlCommand(strSQL, conn);
                comm.Parameters.Add("@eventid", SqlDbType.Int).Value = nEventId;
                comm.Parameters.Add("@funcid", SqlDbType.Int).Value = nFuncId;
                comm.Parameters.Add("@prevsnapshotid", SqlDbType.Int).Value = nSnapshotId;
                comm.CommandTimeout = nCommandTimeOut;

                SqlDataReader dr = comm.ExecuteReader();
                if (dr.Read() && dr.HasRows)
                {
                    strAcctCode = dr["EV700_FUNC_DESC"].ToString();
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

            return strAcctCode;
        }

        public static string GetFuncStatusDesc(string strStatusCode)
        {
            string strStatusDesc = "";
            SqlConnection conn = new SqlConnection(strEBMSConn);

            try
            {
                conn.Open();

                string strSQL = " select distinct EV130_STATUS_DESC from " + strCompDatabase + ".dbo.EV130_STATUS_MASTER where EV130_EVT_FUNC_EFB='B' AND EV130_STATUS_CODE=@statuscode  ";
                SqlCommand comm = new SqlCommand(strSQL, conn);
                comm.Parameters.Add("@statuscode", SqlDbType.VarChar, 2).Value = strStatusCode;
                comm.CommandTimeout = nCommandTimeOut;

                SqlDataReader dr = comm.ExecuteReader();
                if (dr.Read() && dr.HasRows)
                {
                    strStatusDesc = dr["EV130_STATUS_DESC"].ToString() + " (" + strStatusCode + ")";
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

            return strStatusDesc;
        }

        public static Function_Info GetFuncInfoFromSnapshot(int nFuncId, int nEventId, int nSnapshotId)
        {
            Function_Info function = new Function_Info();
            SqlConnection conn = new SqlConnection(strEBMSConn);

            try
            {
                conn.Open();

                string strSQL = " SELECT EV700_FUNC_DESC, EV700_FUNC_TYPE, EV800_SPACE_DESC, EV700_SPACE, EV130_STATUS_DESC, EV700_STATUS_CODE, EV700_FUNC_LEVEL, ";
                strSQL += " cast(cast(EV700_START_DATE_ISO as date) as datetime) + cast(cast(EV700_START_TIME_ISO as time) as datetime) as startdatetime, ";
                strSQL += " cast(cast(EV700_END_DATE_ISO as date) as datetime) + cast(cast(EV700_END_TIME_ISO as time) as datetime) as enddatetime ";
                strSQL += " FROM EV700_FUNC_MASTER LEFT JOIN " + strCompDatabase + ".dbo.EV800_SPACE_MASTER on EV700_SPACE = EV800_SPACE_CODE and EV700_ORG_CODE = EV800_ORG_CODE ";
                strSQL += " INNER JOIN " + strCompDatabase + ".dbo.EV130_STATUS_MASTER on EV130_STATUS_CODE = EV700_STATUS_CODE ";
                strSQL += " where EV700_SNAPSHOT_ID=@snapshotid AND EV700_FUNC_ID=@funcid and EV700_EVT_ID=@eventid";

                SqlCommand comm = new SqlCommand(strSQL, conn);
                comm.Parameters.Add("@snapshotid", SqlDbType.Int).Value = nSnapshotId;
                comm.Parameters.Add("@funcid", SqlDbType.Int).Value = nFuncId;
                comm.Parameters.Add("@eventid", SqlDbType.Int).Value = nEventId;
                comm.CommandTimeout = nCommandTimeOut;

                SqlDataReader dr = comm.ExecuteReader();
                if (dr.Read() && dr.HasRows)
                {
                    DateTime dtStart, dtEnd;
                    function.FuncId = nFuncId;
                    function.FuncDesc = dr["EV700_FUNC_DESC"].ToString();
                    function.FuncType = dr["EV700_FUNC_TYPE"].ToString();
                    function.FuncStart = DateTime.TryParse(dr["startdatetime"].ToString(), out dtStart) ? DateTime.Parse(dr["startdatetime"].ToString()) : DateTime.MinValue;
                    function.FuncEnd = DateTime.TryParse(dr["enddatetime"].ToString(), out dtEnd) ? DateTime.Parse(dr["enddatetime"].ToString()) : DateTime.MinValue;
                    function.SpaceDesc = dr["EV800_SPACE_DESC"].ToString();
                    function.SpaceCode = dr["EV700_SPACE"].ToString();
                    function.Status = dr["EV130_STATUS_DESC"].ToString() + "(" + dr["EV700_STATUS_CODE"].ToString() + ")";
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

            return function;
        }

        public static List<Function_Info> getCurrentChangeFunctions()
        {
            List<Function_Info> lstFunctions = new List<Function_Info>();

            SqlConnection conn = new SqlConnection(strEBMSConn);

            try
            {
                conn.Open();
                SqlCommand comm = new SqlCommand("get_function_changes", conn);
                comm.CommandType = CommandType.StoredProcedure;
                comm.Parameters.Add("@eventid", SqlDbType.Int).Value = evt.EventId;
                comm.Parameters.Add("@departmentcode", SqlDbType.VarChar,20).Value =  dep.DepartmentCode;
                comm.Parameters.Add("@checksignage", SqlDbType.Bit).Value = rule.ShowFunctionSignageChange;
                
                comm.CommandTimeout = nCommandTimeOut;
                

                SqlDataAdapter da = new SqlDataAdapter(comm);
                DataTable dt = new DataTable();
                da.Fill(dt);
              
                if (dt.Rows.Count > 0)
                {
                    //check for each exist func data
                    foreach (DataRow dr in dt.Rows)
                    {
                        int nFuncId = 0;

                        string strFuncId = dr["funcid"].ToString();
                        if (int.TryParse(strFuncId, out nFuncId))
                        {
                            nFuncId = int.Parse(strFuncId);
                        }
                        Function_Info FuncInfo = GetFuncInfoFromSnapshot(nFuncId, evt.EventId, nSnapshotCurrentID);

                        FuncInfo.isFunctionChange = Convert.ToBoolean(dr["func_change"]==DBNull.Value?0: dr["func_change"]);
                        FuncInfo.isFunctionNotesChange = Convert.ToBoolean(dr["func_notes_change"] == DBNull.Value ? 0 : dr["func_notes_change"]);
                        FuncInfo.isFunctionSignageChange = Convert.ToBoolean(dr["func_signage_change"] == DBNull.Value ? 0 : dr["func_signage_change"]);
                        FuncInfo.isOrdersChange = Convert.ToBoolean(dr["func_order_change"] == DBNull.Value ? 0 : dr["func_order_change"]);
                        FuncInfo.isOrdersNotesChange = Convert.ToBoolean(dr["func_order_notes_change"] == DBNull.Value ? 0 : dr["func_order_notes_change"]);
                        FuncInfo.isOrderItemsChange = Convert.ToBoolean(dr["func_order_item_change"] == DBNull.Value ? 0 : dr["func_order_item_change"]);
                        FuncInfo.isOrderItemsNotesChange = Convert.ToBoolean(dr["func_order_item_notes_change"] == DBNull.Value ? 0 : dr["func_order_item_notes_change"]);

                        lstFunctions.Add(FuncInfo);
                    }
                }
            }
            catch (Exception ex)
            {
                AMP_Common.sendErrorException(strEmailFrom, strEmailTo, "Amendment Runtime Error", rule, evt, null, dep, nSnapshotPreviousID, nSnapshotCurrentID, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message);
            }
            finally
            {
                conn.Close();
            }

            return lstFunctions;
        }

        public static List<Function_Info> getAll_Related_Function()
        {
            List<Function_Info> lstFunctions = new List<Function_Info>();

            SqlConnection conn = new SqlConnection(strEBMSConn);

            try
            {
                conn.Open();                  
                string strSQL = "SELECT distinct  EV700_FUNC_ID as Function_Id, EV700_FUNC_TYPE as FunctionType FROM EV700_FUNC_MASTER_Curr  ";
                strSQL += " INNER JOIN AMP_Noti_FuncTypeDep on EV700_FUNC_TYPE=AMP_Noti_FuncTypeDep.FuncType and AMP_Noti_FuncTypeDep.Dept_Code=@departmentcode ";
                strSQL += " WHERE EV700_EVT_ID=@eventid ";
                strSQL += " Union All ";
                strSQL += " select distinct ER101_FUNC_ID as Function_Id, 'not_included' as FunctionType from (";
                strSQL += " SELECT  Order_Current.ER101_EVT_ID, Order_Current.ER101_FUNC_ID, ";
                strSQL += " CASE WHEN Order_Current.ER101_NEW_RES_TYPE is NULL THEN Order_Previous.ER101_NEW_RES_TYPE ELSE Order_Current.ER101_NEW_RES_TYPE END AS ER_NEW_RES_TYPE, ";
                strSQL += " CASE WHEN Order_Current.ER101_RES_CODE is NULL THEN Order_Previous.ER101_RES_CODE ELSE Order_Current.ER101_RES_CODE END AS ER_RES_CODE from ";
                strSQL += " (SELECT  ER101_EVT_ID, ER101_FUNC_ID,ER101_ORD_NBR,ER101_ORD_LINE,ER101_DESC,ER101_RES_QTY,ER101_START_DATE_ISO,ER101_START_TIME_ISO,ER101_END_DATE_ISO,ER101_END_TIME_ISO,ER101_NEW_RES_TYPE,ER101_RES_CODE,ER101_PHASE FROM ER101_ACCT_ORDER_DTL_Curr where ER101_EVT_ID=@eventid) AS Order_Current ";
                strSQL += " LEFT JOIN ";
                strSQL += " (SELECT  ER101_EVT_ID, ER101_FUNC_ID,ER101_ORD_NBR,ER101_ORD_LINE,ER101_DESC,ER101_RES_QTY,ER101_START_DATE_ISO,ER101_START_TIME_ISO,ER101_END_DATE_ISO,ER101_END_TIME_ISO,ER101_NEW_RES_TYPE,ER101_RES_CODE,ER101_PHASE FROM ER101_ACCT_ORDER_DTL_Prev where ER101_EVT_ID=@eventid) AS Order_Previous ";
                strSQL += " ON Order_Current.ER101_FUNC_ID = Order_Previous.ER101_FUNC_ID AND Order_Current.ER101_ORD_NBR = Order_Previous.ER101_ORD_NBR AND Order_Current.ER101_ORD_LINE = Order_Previous.ER101_ORD_LINE ";
                strSQL += " WHERE Order_Current.ER101_PHASE='1' AND (Order_Current.ER101_ORD_NBR is null or Order_Previous.ER101_ORD_NBR is null or Order_Current.ER101_DESC <> Order_Previous.ER101_DESC ";
                strSQL += " or Order_Current.ER101_DESC <> Order_Previous.ER101_DESC or Order_Current.ER101_RES_QTY <> Order_Previous.ER101_RES_QTY or CAST(Order_Current.ER101_START_DATE_ISO AS DATE) <> CAST(Order_Previous.ER101_START_DATE_ISO AS DATE) ";
                strSQL += " or CAST(Order_Current.ER101_START_TIME_ISO AS TIME) <> CAST(Order_Previous.ER101_START_TIME_ISO AS TIME) or CAST(Order_Current.ER101_END_DATE_ISO AS DATE) <> CAST(Order_Previous.ER101_END_DATE_ISO AS DATE) ";
                strSQL += " or CAST(Order_Current.ER101_END_TIME_ISO AS TIME) <> CAST(Order_Previous.ER101_END_TIME_ISO AS TIME))";
                strSQL += ") as taborder INNER JOIN AMP_Noti_ResDep ResChangeDep on taborder.ER_NEW_RES_TYPE=ResChangeDep.New_Res_Type and taborder.ER_RES_CODE=ResChangeDep.Res_Code ";
                strSQL += " where ResChangeDep.Noti_Dep_Code=@departmentcode and ER101_FUNC_ID not in (SELECT distinct  EV700_FUNC_ID FROM EV700_FUNC_MASTER_Curr  ";
                strSQL += " INNER JOIN AMP_Noti_FuncTypeDep on EV700_FUNC_TYPE=AMP_Noti_FuncTypeDep.FuncType and AMP_Noti_FuncTypeDep.Dept_Code=@departmentcode ";
                strSQL += " WHERE EV700_EVT_ID=@eventid )";


                SqlCommand commCurrFunc = new SqlCommand(strSQL, conn);
                commCurrFunc.Parameters.Add("@departmentcode", SqlDbType.VarChar, 20).Value = dep.DepartmentCode;
                commCurrFunc.Parameters.Add("@eventid", SqlDbType.Int).Value = evt.EventId;
                commCurrFunc.CommandTimeout = nCommandTimeOut;

                SqlDataAdapter daCurrFunc = new SqlDataAdapter(commCurrFunc);
                DataTable dtCurrFunc = new DataTable();
                daCurrFunc.Fill(dtCurrFunc);
                daCurrFunc.FillSchema(dtCurrFunc, SchemaType.Source);

                if (dtCurrFunc.Rows.Count > 0)
                {
                    //check for each exist func data
                    foreach (DataRow drCurrFunc in dtCurrFunc.Rows)
                    {
                        int nFuncId = 0;

                        string strFuncId = drCurrFunc["Function_Id"].ToString();
                        if (int.TryParse(strFuncId, out nFuncId))
                        {
                            nFuncId = int.Parse(strFuncId);
                        }
                        Function_Info FuncInfo = GetFuncInfoFromSnapshot(nFuncId, evt.EventId, nSnapshotCurrentID);

                        FuncInfo.FuncClass = drCurrFunc["FunctionType"].ToString();

                        lstFunctions.Add(FuncInfo);
                    }
                }
            }
            catch (Exception ex)
            {
                AMP_Common.sendErrorException(strEmailFrom, strEmailTo, "Amendment Runtime Error", rule, evt, null, dep, nSnapshotPreviousID, nSnapshotCurrentID, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message);
            }
            finally
            {
                conn.Close();
            }

            return lstFunctions;
        }

        public static void getFunctionHeader()
        {
            eMsg = new EventAccountMessage();
            eMsg.EventId = evt.EventId;
            eMsg.AcctCode = evt.AccountNo;
            eMsg.FuncId = finfo.FuncId;

            if (finfo.FuncClass == "Delete")
            {
                eMsg.FuncUpdated = true;

                eMsg.MSGText = "Function Deleted: " + finfo.FuncDesc;
                eMsg.MSGHTML = "<DIV style='font:10pt arial;'><p style='margin: 0 0 10px 0;text-align: left;'><span style='font: bold 10pt arial;'> Function Deleted:</span>&nbsp;" + finfo.FuncDesc;
            }
            else
            {
                if (rule.ShowHierarchyFuncDesc)
                {
                    eMsg.MSGText = "Function: " + GetFuncHierarchyDescFromPrd(evt.EventId, finfo.FuncId);
                    eMsg.MSGHTML = "<DIV style='font:10pt arial;'><p style='margin: 0 0 10px 0;text-align: left;'><span style='font: bold 10pt arial;'> Function:</span>&nbsp;" + GetFuncHierarchyDescFromPrd(evt.EventId, finfo.FuncId);
                }
                else
                {
                    eMsg.MSGText = "Function: " + finfo.FuncDesc;
                    eMsg.MSGHTML = "<DIV style='font:10pt arial;'><p style='margin: 0 0 10px 0;text-align: left;'><span style='font: bold 10pt arial;'> Function:</span>&nbsp;" + finfo.FuncDesc;
                }
            }            

            if (rule.ShowFuncId)
            {
                eMsg.MSGText += " (" + finfo.FuncId.ToString() + ")";
                eMsg.MSGHTML += "(" + finfo.FuncId.ToString() + ")";
            }

            if (!string.IsNullOrEmpty(finfo.SpaceCode))
            {
                if (rule.ShowSpaceCode)
                {
                    eMsg.MSGText += " for space " + finfo.SpaceDesc + " (" + finfo.SpaceCode + ")";
                    eMsg.MSGHTML += " for space " + finfo.SpaceDesc + " (" + finfo.SpaceCode + ")";
                }
                else
                {
                    eMsg.MSGText += " for space " + finfo.SpaceDesc;
                    eMsg.MSGHTML += " for space " + finfo.SpaceDesc;
                }
            }

            eMsg.MSGHTML += "</p><p style='margin: 0 0 10px 0;text-align: left;'></div>";
        }

        /// <summary>
        /// has function add, update info
        /// </summary>
        public static void checkFunctionChange()
        {
            string strFuncChangeType = string.Empty;

            SqlConnection conn = new SqlConnection(strEBMSConn);

            try
            {
                conn.Open();

                //check for func add
                string strSQLAddedFunc = "SELECT distinct Func_Current.EV700_FUNC_ID FROM ";
                strSQLAddedFunc += " (SELECT EV700_EVT_ID, EV700_FUNC_ID,EV700_FUNC_TYPE FROM EV700_FUNC_MASTER_Curr where EV700_EVT_ID=@eventid)AS Func_Current ";
                strSQLAddedFunc += " Left JOIN  ";
                strSQLAddedFunc += " (SELECT EV700_EVT_ID, EV700_FUNC_ID,EV700_FUNC_TYPE FROM EV700_FUNC_MASTER_Prev where EV700_EVT_ID=@eventid)AS Func_Pevious ";
                strSQLAddedFunc += " ON Func_Current.EV700_EVT_ID = Func_Pevious.EV700_EVT_ID and Func_Current.EV700_FUNC_ID = Func_Pevious.EV700_FUNC_ID ";
                strSQLAddedFunc += " WHERE Func_Pevious.EV700_FUNC_ID is null and Func_Current.EV700_FUNC_ID=@funcid";

                SqlCommand commAddedFunc = new SqlCommand(strSQLAddedFunc, conn);
                commAddedFunc.Parameters.Add("@eventid", SqlDbType.Int).Value = evt.EventId;
                commAddedFunc.Parameters.Add("@funcid", SqlDbType.Int).Value = finfo.FuncId;
                commAddedFunc.CommandTimeout = nCommandTimeOut;

                SqlDataReader drAddedFunc = commAddedFunc.ExecuteReader();

                //the current function is newly added
                if (drAddedFunc.Read())
                {
                    eMsg.FuncUpdated = true;
                    strFuncChangeType = "Add";
                    eMsg.MSGText += "Function is newly added \r\n";
                    eMsg.MSGText += "Date Time: " + finfo.FuncStart.ToString("dd/MM/yyyy HH:mm") + " - " + finfo.FuncEnd.ToString("dd/MM/yyyy HH:mm");
                    eMsg.MSGHTML += " <DIV style = 'font:10pt arial;' ><span style='font: bold 10pt arial;'>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Function is newly added</span><ul style='MARGIN:0 0 10px 40px;LIST-STYLE-TYPE:disc;'>  ";
                    eMsg.MSGHTML += "<li style=font:10pt arial;'><p style='margin: 0 0 10px 0;text-align: left;'><span style='FONT-SIZE:10pt;'>Date Time: " + finfo.FuncStart.ToString("dd/MM/yyyy HH:mm") + " - " + finfo.FuncEnd.ToString("dd/MM/yyyy HH:mm") + "</span></p></li>";
                    eMsg.MSGHTML += "  </ul></div>";
                }

                drAddedFunc.Close();

                //check for func updated.
                string strSQLUpdatededFunc = "SELECT Func_Current.EV700_EVT_ID, Func_Current.EV700_FUNC_ID, Func_Current.EV700_FUNC_DESC AS C_FUNC_DESC, Func_Current.EV700_SPACE AS C_SPACE, Func_Current.EV700_STATUS_CODE AS C_STATUS_CODE, ";
                strSQLUpdatededFunc += " CAST(CAST(Func_Current.EV700_START_DATE_ISO AS DATE) AS DATETIME) + CAST(CAST(Func_Current.EV700_START_TIME_ISO AS TIME) AS DATETIME) AS C_START_DATE_TIME_ISO, CAST(CAST(Func_Current.EV700_END_DATE_ISO AS DATE) AS DATETIME) + CAST(CAST(Func_Current.EV700_END_TIME_ISO AS TIME) AS DATETIME) AS C_END_DATE_TIME_ISO, ";
                strSQLUpdatededFunc += " Func_Previous.EV700_FUNC_DESC AS P_FUNC_DESC, Func_Previous.EV700_SPACE AS P_SPACE, Func_Previous.EV700_STATUS_CODE AS P_STATUS_CODE, CAST(CAST(Func_Previous.EV700_START_DATE_ISO AS DATE) AS DATETIME) + CAST(CAST(Func_Previous.EV700_START_TIME_ISO AS TIME) AS DATETIME) AS P_START_DATE_TIME_ISO, CAST(CAST(Func_Previous.EV700_END_DATE_ISO AS DATE) AS DATETIME) + CAST(CAST(Func_Previous.EV700_END_TIME_ISO AS TIME) AS DATETIME) AS P_END_DATE_TIME_ISO, ";
                strSQLUpdatededFunc += " Func_Previous.EV700_END_DATE_ISO AS P_END_DATE_ISO, Func_Previous.EV700_START_TIME_ISO AS P_START_TIME_ISO, Func_Previous.EV700_END_TIME_ISO AS P_END_TIME_ISO FROM  ";
                strSQLUpdatededFunc += " (SELECT EV700_EVT_ID, EV700_FUNC_ID, EV700_FUNC_DESC, EV700_SPACE,EV700_FUNC_TYPE, EV700_STATUS_CODE, EV700_START_DATE_ISO, EV700_END_DATE_ISO, EV700_START_TIME_ISO, EV700_END_TIME_ISO FROM EV700_FUNC_MASTER_Curr where EV700_EVT_ID=@eventid and EV700_FUNC_ID=@funcid)AS Func_Current ";
                strSQLUpdatededFunc += " INNER JOIN  ";
                strSQLUpdatededFunc += " (SELECT EV700_EVT_ID, EV700_FUNC_ID, EV700_FUNC_DESC, EV700_SPACE, EV700_FUNC_TYPE, EV700_STATUS_CODE, EV700_START_DATE_ISO, EV700_END_DATE_ISO, EV700_START_TIME_ISO, EV700_END_TIME_ISO FROM EV700_FUNC_MASTER_Prev where EV700_EVT_ID=@eventid and EV700_FUNC_ID=@funcid)AS Func_Previous ";
                strSQLUpdatededFunc += " ON Func_Current.EV700_EVT_ID = Func_Previous.EV700_EVT_ID and Func_Current.EV700_FUNC_ID = Func_Previous.EV700_FUNC_ID ";
                strSQLUpdatededFunc += " WHERE  (Func_Current.EV700_FUNC_DESC <> Func_Previous.EV700_FUNC_DESC or Func_Current.EV700_SPACE <> Func_Previous.EV700_SPACE or Func_Current.EV700_STATUS_CODE <> Func_Previous.EV700_STATUS_CODE ";
                strSQLUpdatededFunc += " or CAST(Func_Current.EV700_START_DATE_ISO AS DATE) <> CAST(Func_Previous.EV700_START_DATE_ISO AS DATE) or CAST(Func_Current.EV700_END_DATE_ISO AS DATE) <> CAST(Func_Previous.EV700_END_DATE_ISO AS DATE) ";
                strSQLUpdatededFunc += " or CAST(Func_Current.EV700_START_TIME_ISO AS TIME) <> CAST(Func_Previous.EV700_START_TIME_ISO AS TIME) or CAST(Func_Current.EV700_END_TIME_ISO AS TIME) <> CAST(Func_Previous.EV700_END_TIME_ISO AS TIME)) ";

                SqlCommand commUpdatededFunc = new SqlCommand(strSQLUpdatededFunc, conn);
                commUpdatededFunc.Parameters.Add("@eventid", SqlDbType.Int).Value = evt.EventId;
                commUpdatededFunc.Parameters.Add("@funcid", SqlDbType.Int).Value = finfo.FuncId;
                commUpdatededFunc.CommandTimeout = nCommandTimeOut;

                SqlDataReader drUpdatedFunc = commUpdatededFunc.ExecuteReader();

                //this func has just been updated
                if (drUpdatedFunc.Read())
                {
                    eMsg.FuncUpdated = true;
                    strFuncChangeType = "Update";
                    eMsg.MSGText += "Function has been updated \r\n";
                    eMsg.MSGHTML += "<DIV style='font:10pt arial;'><p style='margin: 0 0 10px 0;text-align: left;'>";
                    eMsg.MSGHTML += "<span style='font: bold 10pt arial;'>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Function has been updated</span> </p><ul style='MARGIN:0 0 10px 40px;LIST-STYLE-TYPE:disc;'>";

                    if (!(drUpdatedFunc["C_FUNC_DESC"].ToString().Equals(drUpdatedFunc["P_FUNC_DESC"].ToString())))
                    {
                        eMsg.MSGText += " Function Description changed from: " + drUpdatedFunc["P_FUNC_DESC"].ToString() + " to " + drUpdatedFunc["C_FUNC_DESC"].ToString() + "\r\n";
                        eMsg.MSGHTML += " <li style=font:10pt arial;'><p style='margin: 0 0 10px 0;text-align: left;'><span style='FONT-SIZE:10pt;'> Function Description changed to " + drUpdatedFunc["C_FUNC_DESC"].ToString() + " from: <strike>" + drUpdatedFunc["P_FUNC_DESC"].ToString() + "</strike> </span></p></li>";
                    }
                    if (drUpdatedFunc["C_SPACE"].ToString() != drUpdatedFunc["P_SPACE"].ToString())
                    {
                        eMsg.MSGText += " Function Space changed from: " + AMP_Common.GetSpaceDesc(drUpdatedFunc["P_SPACE"].ToString()) + " to " + AMP_Common.GetSpaceDesc(drUpdatedFunc["C_SPACE"].ToString()) + "\r\n";
                        eMsg.MSGHTML += " <li style=font:10pt arial;'><p style='margin: 0 0 10px 0;text-align: left;'><span style='FONT-SIZE:10pt;'> Function Space changed to " + AMP_Common.GetSpaceDesc(drUpdatedFunc["C_SPACE"].ToString()) + " from: <strike>" + AMP_Common.GetSpaceDesc(drUpdatedFunc["P_SPACE"].ToString()) + "</strike> </span></p></li>";
                    }
                    if (drUpdatedFunc["C_STATUS_CODE"].ToString() != drUpdatedFunc["P_STATUS_CODE"].ToString())
                    {
                        eMsg.MSGText += " Function Status changed from: " + GetFuncStatusDesc(drUpdatedFunc["P_STATUS_CODE"].ToString()) + " to " + GetFuncStatusDesc(drUpdatedFunc["C_STATUS_CODE"].ToString()) + "\r\n";
                        eMsg.MSGHTML += " <li style=font:10pt arial;'><p style='margin: 0 0 10px 0;text-align: left;'><span style='FONT-SIZE:10pt;'> Function Status changed to " + GetFuncStatusDesc(drUpdatedFunc["C_STATUS_CODE"].ToString()) + " from: <strike>" + GetFuncStatusDesc(drUpdatedFunc["P_STATUS_CODE"].ToString()) + "</strike></span></p></li>";
                    }

                    DateTime dtCFuncStart, dtCFuncEnd, dtPFuncStart, dtPFuncEnd;

                    if (DateTime.TryParse(drUpdatedFunc["C_START_DATE_TIME_ISO"].ToString(), out dtCFuncStart))
                    {
                        dtCFuncStart = DateTime.Parse(drUpdatedFunc["C_START_DATE_TIME_ISO"].ToString());
                    }
                    if (DateTime.TryParse(drUpdatedFunc["C_END_DATE_TIME_ISO"].ToString(), out dtCFuncEnd))
                    {
                        dtCFuncEnd = DateTime.Parse(drUpdatedFunc["C_END_DATE_TIME_ISO"].ToString());
                    }
                    if (DateTime.TryParse(drUpdatedFunc["P_START_DATE_TIME_ISO"].ToString(), out dtPFuncStart))
                    {
                        dtPFuncStart = DateTime.Parse(drUpdatedFunc["P_START_DATE_TIME_ISO"].ToString());
                    }
                    if (DateTime.TryParse(drUpdatedFunc["P_END_DATE_TIME_ISO"].ToString(), out dtPFuncEnd))
                    {
                        dtPFuncEnd = DateTime.Parse(drUpdatedFunc["P_END_DATE_TIME_ISO"].ToString());
                    }

                    if (dtCFuncStart != dtPFuncStart || dtCFuncEnd != dtPFuncEnd)
                    {
                        eMsg.MSGText += " Function Start/End changed to " + dtCFuncStart.ToString("dd/MM/yyyy HH:mm") + " - " + dtCFuncEnd.ToString("dd/MM/yyyy HH:mm") + " from: " + dtPFuncStart.ToString("dd/MM/yyyy HH:mm") + " - " + dtPFuncEnd.ToString("dd/MM/yyyy HH:mm") + " ";
                        eMsg.MSGHTML += " <li style=font:10pt arial;'><p style='margin: 0 0 10px 0;text-align: left;'><span style='FONT-SIZE:10pt;'> Function Start/End changed to " + dtCFuncStart.ToString("dd/MM/yyyy HH:mm") + " - " + dtCFuncEnd.ToString("dd/MM/yyyy HH:mm") + " from: <strike>" + dtPFuncStart.ToString("dd/MM/yyyy HH:mm") + " - " + dtPFuncEnd.ToString("dd/MM/yyyy HH:mm") + "</strike></span></p></li>";
                        eMsg.MSGHTML = eMsg.MSGHTML.Replace("&lt;", "<").Replace("&gt;", ">");
                    }
                    eMsg.MSGHTML += "</ul></div>";
                }//end func updated.
            }
            catch (Exception ex)
            {
                AMP_Common.sendErrorException(strEmailFrom, strEmailTo, "Amendment Runtime Error", rule, evt, finfo, dep, nSnapshotPreviousID, nSnapshotCurrentID, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message);
            }
            finally
            {
                conn.Close();
            }

        }

        public static void checkFunctionNotesChange()
        {
            SqlConnection conn = new SqlConnection(strEBMSConn);

            try
            {
                conn.Open();

                string strSQL = "SELECT CC025_Live_NOTE_TEXT,CC025_Snapshot_NOTE_TEXT,CC025_UPD_USER_ID,CC025_UPD_DATE,CC025_NOTE_DESC,CC025_Live_HTML_TEXT,CC025_Snapshot_HTML_TEXT FROM ";
                strSQL += " (SELECT CASE WHEN CC025_Live.CC025_NOTE_CLASS IS NULL THEN CC025_Snapshot.CC025_NOTE_CLASS ELSE CC025_Live.CC025_NOTE_CLASS END AS CC025_NOTE_CLASS, ";
                strSQL += " CASE WHEN CC025_Live.CC025_NOTE_CLASS IS NULL THEN CC025_Snapshot.CC025_UPD_DATE ELSE CC025_Live.CC025_UPD_DATE END AS CC025_UPD_DATE, ";
                strSQL += " CASE WHEN CC025_Live.CC025_NOTE_CLASS IS NULL THEN CC025_Snapshot.CC025_UPD_USER_ID ELSE CC025_Live.CC025_UPD_USER_ID END AS CC025_UPD_USER_ID, ";
                strSQL += " CASE WHEN CC025_Live.CC025_NOTE_CLASS IS NULL THEN CC025_Snapshot.CC025_NOTE_DESC ELSE CC025_Live.CC025_NOTE_DESC END AS CC025_NOTE_DESC, ";
                strSQL += " CASE WHEN CC025_Live.CC025_NOTE_CLASS IS NULL THEN CC025_Snapshot.CC025_NOTE_TYPE ELSE CC025_Live.CC025_NOTE_TYPE END AS CC025_NOTE_TYPE, ";
                strSQL += " CASE WHEN CC025_Live.CC025_NOTE_CLASS IS NULL THEN CC025_Snapshot.CC025_FUNC_ID ELSE CC025_Live.CC025_FUNC_ID END AS CC025_FUNC_ID, ";
                strSQL += " CASE WHEN CC025_Live.CC025_NOTE_CLASS IS NULL THEN CC025_Snapshot.CC025_ORDER ELSE CC025_Live.CC025_ORDER END AS CC025_ORDER, ";
                strSQL += " CASE WHEN CC025_Live.CC025_NOTE_CLASS IS NULL THEN CC025_Snapshot.CC025_ORD_LINE ELSE CC025_Live.CC025_ORD_LINE END AS CC025_ORD_LINE, ";
                strSQL += " CC025_Live.CC025_NOTE_TEXT as CC025_Live_NOTE_TEXT, CC025_Live.CC025_HTML_TEXT as CC025_Live_HTML_TEXT,  ";
                strSQL += " CC025_Snapshot.CC025_NOTE_TEXT as CC025_Snapshot_NOTE_TEXT, CC025_Snapshot.CC025_HTML_TEXT as CC025_Snapshot_HTML_TEXT ";
                strSQL += " FROM  ";
                strSQL += " (SELECT CC025_EVT_ID,CC025_FUNC_ID,CC025_ORDER, CC025_ORD_LINE,CC025_NOTE_TYPE,CC025_NOTE_HDR_SEQ, CC025_NOTE_CODE, CC025_NOTE_DESC,CC025_NOTE_PRT_CODE,CC025_NOTE_TEXT,CC025_NOTE_CLASS,CC025_UPD_DATE,CC025_UPD_USER_ID,CC025_HTML_TEXT,CC025_ENT_DATE,CC025_ENT_USER_ID FROM CC025_NOTES_EXT_Curr WHERE CC025_EVT_ID=@eventid AND CC025_NOTE_TYPE='EF' and CC025_FUNC_ID=@funcid ) as CC025_Live ";
                strSQL += " FULL OUTER JOIN ";
                strSQL += " (SELECT CC025_EVT_ID,CC025_FUNC_ID,CC025_ORDER, CC025_ORD_LINE,CC025_NOTE_TYPE,CC025_NOTE_HDR_SEQ, CC025_NOTE_CODE,CC025_NOTE_DESC,CC025_NOTE_PRT_CODE,CC025_NOTE_TEXT,CC025_NOTE_CLASS,CC025_UPD_DATE,CC025_UPD_USER_ID,CC025_HTML_TEXT,CC025_ENT_DATE,CC025_ENT_USER_ID FROM CC025_NOTES_EXT_Prev WHERE CC025_EVT_ID=@eventid AND CC025_NOTE_TYPE='EF' and CC025_FUNC_ID=@funcid ) as CC025_Snapshot  ";
                strSQL += " ON CC025_Live.CC025_NOTE_TYPE = CC025_Snapshot.CC025_NOTE_TYPE AND CC025_Live.CC025_NOTE_CODE = CC025_Snapshot.CC025_NOTE_CODE AND CC025_Live.CC025_NOTE_HDR_SEQ = CC025_Snapshot.CC025_NOTE_HDR_SEQ ";
                strSQL += " WHERE (CC025_Live.CC025_NOTE_CODE is null or CC025_Snapshot.CC025_NOTE_CODE is null OR CC025_Live.CC025_NOTE_TEXT<>CC025_Snapshot.CC025_NOTE_TEXT) ";
                strSQL += " ) AS NOTE_DIFF ";
                strSQL += " INNER JOIN AMP_Noti_NoteClassDep on Note_Class=CC025_NOTE_CLASS and Noti_Dept_Code=@deptcode ";


                SqlCommand comm = new SqlCommand(strSQL, conn);
                comm.Parameters.Add("@eventid", SqlDbType.Int).Value = evt.EventId;
                comm.Parameters.Add("@deptcode", SqlDbType.VarChar, 20).Value = dep.DepartmentCode;
                comm.Parameters.Add("@funcid", SqlDbType.Int).Value = finfo.FuncId;
                comm.CommandTimeout = nCommandTimeOut;

                SqlDataAdapter da = new SqlDataAdapter(comm);
                DataTable dt = new DataTable();
                da.Fill(dt);
                da.FillSchema(dt, SchemaType.Source);

                if (dt.Rows.Count > 0)
                {
                    eMsg.FuncUpdated = true;
                    foreach (DataRow dr in dt.Rows)
                    {
                        DateTime dtUpdDate = new DateTime();

                        if (dr["CC025_UPD_DATE"] != DBNull.Value)
                        {
                            if (DateTime.TryParse(dr["CC025_UPD_DATE"].ToString(), out dtUpdDate))
                            {
                                dtUpdDate = DateTime.Parse(dr["CC025_UPD_DATE"].ToString());
                            }
                        }

                        //new note
                        if (dr["CC025_Snapshot_NOTE_TEXT"] == DBNull.Value)
                        {
                            eMsg.MSGText += "New Function " + dr["CC025_NOTE_DESC"].ToString() + " Notes entered/updated by " + AMP_Common.GetUserName(dr["CC025_UPD_USER_ID"].ToString()) + " on " + dtUpdDate.ToString("dd/MM/yyyy hh:mm") + "\r\n";
                            eMsg.MSGHTML += "<DIV style='font:10pt arial;'><p style='margin: 0 0 10px 0;text-align: left;'><span style='font: bold 10pt arial;'>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;New Function Notes (" + dr["CC025_NOTE_DESC"].ToString() + ")</span> entered/updated by " + AMP_Common.GetUserName(dr["CC025_UPD_USER_ID"].ToString()) + " on " + dtUpdDate.ToString("dd/MM/yyyy hh:mm") + " </p>";
                            eMsg.MSGHTML += "<ul style='MARGIN:0 0 10px 40px;LIST-STYLE-TYPE:disc;'><li style=font:10pt arial;'><p style='MARGIN:0 0 10px 0;TEXT-ALIGN:left;'>";
                            if (dr["CC025_Live_NOTE_TEXT"].ToString().Length <= rule.NotesLength)
                            {
                                eMsg.MSGText += dr["CC025_Live_NOTE_TEXT"].ToString() + "\r\n";
                                eMsg.MSGHTML += dr["CC025_Live_HTML_TEXT"].ToString();
                            }
                            else
                            {
                                eMsg.MSGText += dr["CC025_Live_NOTE_TEXT"].ToString().Substring(0, rule.NotesLength - 3) + " ...\r\n";
                                eMsg.MSGHTML += dr["CC025_Live_NOTE_TEXT"].ToString().Substring(0, rule.NotesLength - 3) + " ...";
                            }
                            eMsg.MSGHTML += "<br></p></li></ul></DIV>";
                        }
                        //deleted notes
                        else if (dr["CC025_Live_NOTE_TEXT"] == DBNull.Value)
                        {
                            eMsg.MSGText += "Deleted Function " + dr["CC025_NOTE_DESC"].ToString() + " Notes \r\n";
                            eMsg.MSGHTML += "<DIV style='font:10pt arial;'><p style='margin: 0 0 10px 0;text-align: left;'><span style='font: bold 10pt arial;'>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; Deleted Function Notes (" + dr["CC025_NOTE_DESC"].ToString() + ")</p>";
                            eMsg.MSGHTML += "<ul style='MARGIN:0 0 10px 40px;LIST-STYLE-TYPE:disc;'><li style=font:10pt arial;'><p style='MARGIN:0 0 10px 0;TEXT-ALIGN:left;'><strike>";
                            if (dr["CC025_Snapshot_NOTE_TEXT"].ToString().Length <= rule.NotesLength)
                            {
                                eMsg.MSGText += dr["CC025_Snapshot_NOTE_TEXT"].ToString() + "\r\n";
                                eMsg.MSGHTML += dr["CC025_Snapshot_HTML_TEXT"].ToString();
                            }
                            else
                            {
                                eMsg.MSGText += dr["CC025_Snapshot_NOTE_TEXT"].ToString().Substring(0, rule.NotesLength - 3) + " ...\r\n";
                                eMsg.MSGHTML += dr["CC025_Snapshot_NOTE_TEXT"].ToString().Substring(0, rule.NotesLength - 3) + " ...";
                            }
                            eMsg.MSGHTML += "</strike><br></p></li></ul></DIV>";
                        }
                        else
                        {
                            eMsg.MSGText += "Function " + dr["CC025_NOTE_DESC"].ToString() + " Notes Updated by " + AMP_Common.GetUserName(dr["CC025_UPD_USER_ID"].ToString()) + " on " + dtUpdDate.ToString("dd/MM/yyyy hh:mm") + " \r\n";
                            eMsg.MSGHTML += "<DIV style='font:10pt arial;'><p style='margin: 0 0 10px 0;text-align: left;'><span style='font: bold 10pt arial;'>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Function Notes (" + dr["CC025_NOTE_DESC"].ToString() + ") Updated</span> by " + AMP_Common.GetUserName(dr["CC025_UPD_USER_ID"].ToString()) + " on " + dtUpdDate.ToString("dd/MM/yyyy hh:mm") + " </p>";
                            eMsg.MSGHTML += "<ul style='MARGIN:0 0 10px 40px;LIST-STYLE-TYPE:disc;'><li style=font:10pt arial;'><p style='MARGIN:0 0 10px 0;TEXT-ALIGN:left;'>";
                            if (dr["CC025_Live_NOTE_TEXT"] != DBNull.Value && dr["CC025_Snapshot_NOTE_TEXT"] != DBNull.Value)
                            {
                                if (dr["CC025_Live_NOTE_TEXT"].ToString().Length <= rule.NotesLength)
                                {
                                    eMsg.MSGText += "Update To : " + dr["CC025_Live_NOTE_TEXT"].ToString() + "\r\n";
                                    eMsg.MSGHTML += "Update To :" + dr["CC025_Live_HTML_TEXT"].ToString() + "<br/>";                                    
                                }
                                else
                                {
                                    eMsg.MSGText += "Update To : " + dr["CC025_Live_NOTE_TEXT"].ToString().Substring(0, rule.NotesLength - 3) + " ...r\n";
                                    eMsg.MSGHTML += "Update To :" + dr["CC025_Live_NOTE_TEXT"].ToString().Substring(0, rule.NotesLength - 3) + " ...< br/>";
                                    
                                }
                                if (dr["CC025_Snapshot_NOTE_TEXT"].ToString().Length <= rule.NotesLength)
                                {                                    
                                    eMsg.MSGText += "from: " + dr["CC025_Snapshot_NOTE_TEXT"].ToString() + "\r\n";
                                    eMsg.MSGHTML += "from:<strike>" + dr["CC025_Snapshot_HTML_TEXT"].ToString() + "</strike>";
                                }
                                else
                                {
                                    eMsg.MSGText += "from: " + dr["CC025_Snapshot_NOTE_TEXT"].ToString().Substring(0, rule.NotesLength - 3) + "...\r\n";
                                    eMsg.MSGHTML += "from:<strike>" + dr["CC025_Snapshot_NOTE_TEXT"].ToString().Substring(0, rule.NotesLength - 3) + " ...</strike>";
                                }

                                eMsg.MSGHTML += "<br></p></li></ul></DIV>";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AMP_Common.sendErrorException(strEmailFrom, strEmailTo, "Amendment Runtime Error", rule, evt, finfo, dep, nSnapshotPreviousID, nSnapshotCurrentID, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message);
            }
            finally
            {
                conn.Close();
            }
        }

        public static void checkFunctionSignage()
        {
            SqlConnection conn = new SqlConnection(strEBMSConn);

            try
            {
                conn.Open();

                string strSQLFuncSignage = "SELECT Func_Current.EV700_EVT_ID, Func_Current.EV700_FUNC_ID, Func_Current.EV700_FUNC_DESC AS C_FUNC_DESC, Func_Current.EV700_SPACE AS C_SPACE, Func_Current.EV700_STATUS_CODE AS C_STATUS_CODE, ";
                strSQLFuncSignage += " Func_Current.EV700_STAT_FUNC as C_STAT_FUNC,Func_Current.EV700_ALT_FUNC_DESC as C_ALT_FUNC_DESC,Func_Current.EV700_ALT_FUNC_DESC2 as C_ALT_FUNC_DESC2, ";
                strSQLFuncSignage += " Func_Previous.EV700_STAT_FUNC as P_STAT_FUNC,Func_Previous.EV700_ALT_FUNC_DESC as P_ALT_FUNC_DESC,Func_Previous.EV700_ALT_FUNC_DESC2 as P_ALT_FUNC_DESC2  FROM  ";
                strSQLFuncSignage += " (SELECT EV700_EVT_ID, EV700_FUNC_ID, EV700_FUNC_DESC, EV700_SPACE,EV700_FUNC_TYPE, EV700_STATUS_CODE,  EV700_STAT_FUNC,EV700_ALT_FUNC_DESC,EV700_ALT_FUNC_DESC2, EV700_START_DATE_ISO, EV700_END_DATE_ISO, EV700_START_TIME_ISO, EV700_END_TIME_ISO FROM EV700_FUNC_MASTER_Curr where EV700_EVT_ID=@eventid and EV700_FUNC_ID=@funcid)AS Func_Current ";
                strSQLFuncSignage += " INNER JOIN  ";
                strSQLFuncSignage += " (SELECT EV700_EVT_ID, EV700_FUNC_ID, EV700_FUNC_DESC, EV700_SPACE, EV700_FUNC_TYPE, EV700_STATUS_CODE,  EV700_STAT_FUNC,EV700_ALT_FUNC_DESC,EV700_ALT_FUNC_DESC2, EV700_START_DATE_ISO, EV700_END_DATE_ISO, EV700_START_TIME_ISO, EV700_END_TIME_ISO FROM EV700_FUNC_MASTER_Prev where EV700_EVT_ID=@eventid and EV700_FUNC_ID=@funcid)AS Func_Previous ";
                strSQLFuncSignage += " ON Func_Current.EV700_EVT_ID = Func_Previous.EV700_EVT_ID and Func_Current.EV700_FUNC_ID = Func_Previous.EV700_FUNC_ID ";
                strSQLFuncSignage += " WHERE (Func_Current.EV700_STAT_FUNC<>Func_Previous.EV700_STAT_FUNC or (Func_Current.EV700_STAT_FUNC='Y' and (Func_Current.EV700_ALT_FUNC_DESC<>Func_Previous.EV700_ALT_FUNC_DESC or Func_Current.EV700_ALT_FUNC_DESC2<>Func_Previous.EV700_ALT_FUNC_DESC2)) )";

                SqlCommand commFuncSignage = new SqlCommand(strSQLFuncSignage, conn);
                commFuncSignage.Parameters.Add("@eventid", SqlDbType.Int).Value = evt.EventId;
                commFuncSignage.Parameters.Add("@funcid", SqlDbType.Int).Value = finfo.FuncId;
                commFuncSignage.CommandTimeout = nCommandTimeOut;

                SqlDataReader drFuncSignage = commFuncSignage.ExecuteReader();

                //this function signage has just been modified
                if (drFuncSignage.Read())
                {
                    eMsg.FuncUpdated = true;
                    eMsg.MSGText += "Function Signage Updated\r\n";
                    eMsg.MSGHTML += "<DIV style='font:10pt arial;'><p style='margin: 0 0 10px 0;text-align: left;'>";
                    eMsg.MSGHTML += "<span style='FONT-SIZE:10pt;'>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Function Signage has been updated</p><ul style='MARGIN:0 0 10px 40px;LIST-STYLE-TYPE:disc;'>";
                    if (drFuncSignage["C_STAT_FUNC"].ToString() != drFuncSignage["P_STAT_FUNC"].ToString())
                    {
                        //new add 
                        if (drFuncSignage["P_STAT_FUNC"].ToString() == "N")
                        {
                            eMsg.MSGText += "New Signage Function, Wayfinding/Door Signage: " + drFuncSignage["C_ALT_FUNC_DESC"].ToString() + " Space/Door Number: " + drFuncSignage["C_ALT_FUNC_DESC2"].ToString();
                            eMsg.MSGHTML += "<p style='margin: 0 0 10px 0;text-align: left;'><span style='font: bold 10pt arial;'>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;New Signage Function, Wayfinding/Door Signage: </span> " + drFuncSignage["C_ALT_FUNC_DESC"].ToString() + " Space/Door Number: " + drFuncSignage["C_ALT_FUNC_DESC2"].ToString() + "</p>";

                        }
                        if (drFuncSignage["P_STAT_FUNC"].ToString() == "Y")
                        {
                            eMsg.MSGText += "Deleted Signage Function, Wayfinding/Door Signage: " + drFuncSignage["P_ALT_FUNC_DESC"].ToString() + " Space/Door Number: " + drFuncSignage["P_ALT_FUNC_DESC2"].ToString();
                            eMsg.MSGHTML += "<p style='margin: 0 0 10px 0;text-align: left;'><span style='font: bold 10pt arial;'>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Deleted Signage Function, Wayfinding/Door Signage: </span> " + drFuncSignage["C_ALT_FUNC_DESC"].ToString() + " Space/Door Number: " + drFuncSignage["C_ALT_FUNC_DESC2"].ToString() + "</p>";
                        }
                    }
                    else if (drFuncSignage["C_STAT_FUNC"].ToString() == drFuncSignage["P_STAT_FUNC"].ToString() && drFuncSignage["C_STAT_FUNC"].ToString() == "Y" && drFuncSignage["C_ALT_FUNC_DESC"].ToString() != drFuncSignage["P_ALT_FUNC_DESC"].ToString())
                    {
                        eMsg.MSGText += "Updated Wayfinding/Door Signage from: " + drFuncSignage["P_ALT_FUNC_DESC"].ToString() + " to: " + drFuncSignage["C_ALT_FUNC_DESC"].ToString();
                        eMsg.MSGHTML += "<p style='margin: 0 0 10px 0;text-align: left;'><span style='font: bold 10pt arial;'>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Updated Wayfinding/Door Signage </span>from: " + drFuncSignage["P_ALT_FUNC_DESC"].ToString() + " to: " + drFuncSignage["C_ALT_FUNC_DESC"].ToString() + "</p>";
                    }
                    else if (drFuncSignage["C_STAT_FUNC"].ToString() == drFuncSignage["P_STAT_FUNC"].ToString() && drFuncSignage["C_STAT_FUNC"].ToString() == "Y" && drFuncSignage["P_ALT_FUNC_DESC2"].ToString() != drFuncSignage["C_ALT_FUNC_DESC2"].ToString())
                    {
                        eMsg.MSGText += "Updated Space/Door Number from: " + drFuncSignage["P_ALT_FUNC_DESC2"].ToString() + "  " + drFuncSignage["C_ALT_FUNC_DESC2"].ToString();
                        eMsg.MSGHTML += "<p style='margin: 0 0 10px 0;text-align: left;'><span style='font: bold 10pt arial;'>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Updated Space/Door Number </span>from: " + drFuncSignage["P_ALT_FUNC_DESC2"].ToString() + " to: " + drFuncSignage["C_ALT_FUNC_DESC2"].ToString() + "</p>";
                    }

                    eMsg.MSGHTML += "</ul></div>";
                }
            }
            catch (Exception ex)
            {
                AMP_Common.sendErrorException(strEmailFrom, strEmailTo, "Amendment Runtime Error", rule, evt, finfo, dep, nSnapshotPreviousID, nSnapshotCurrentID, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message);
            }
            finally
            {
                conn.Close();
            }
        }

        public static List<Function_Info> getDelFunctions()
        {
            List<Function_Info> lstDelFunctions = new List<Function_Info>();

            SqlConnection conn = new SqlConnection(strEBMSConn);
            DataTable dtDelFunc = new DataTable();

            try
            {
                conn.Open();

                string strSQL = "SELECT distinct Func_Pevious.EV700_FUNC_ID  FROM ";
                strSQL += " (SELECT EV700_EVT_ID, EV700_FUNC_ID,EV700_FUNC_TYPE FROM EV700_FUNC_MASTER_Curr where EV700_EVT_ID=@eventid)AS Func_Current ";
                strSQL += " RIGHT JOIN  ";
                strSQL += " (SELECT EV700_EVT_ID, EV700_FUNC_ID,EV700_FUNC_TYPE FROM EV700_FUNC_MASTER_Prev where EV700_EVT_ID=@eventid)AS Func_Pevious ";
                strSQL += " ON Func_Current.EV700_EVT_ID = Func_Pevious.EV700_EVT_ID and Func_Current.EV700_FUNC_ID = Func_Pevious.EV700_FUNC_ID ";
                strSQL += " INNER JOIN AMP_Noti_FuncTypeDep on Func_Pevious.EV700_FUNC_TYPE=AMP_Noti_FuncTypeDep.FuncType and AMP_Noti_FuncTypeDep.Dept_Code=@departmentcode ";
                strSQL += " WHERE Func_Current.EV700_FUNC_ID is null ";

                SqlCommand commDelFunc = new SqlCommand(strSQL, conn);
                commDelFunc.Parameters.Add("@eventid", SqlDbType.Int).Value = evt.EventId;
                commDelFunc.Parameters.Add("@departmentcode", SqlDbType.VarChar, 20).Value = dep.DepartmentCode;
                commDelFunc.CommandTimeout = nCommandTimeOut;

                SqlDataAdapter daDelFunc = new SqlDataAdapter(commDelFunc);

                daDelFunc.Fill(dtDelFunc);
                daDelFunc.FillSchema(dtDelFunc, SchemaType.Source);
                if (dtDelFunc.Rows.Count > 0)
                {
                    foreach (DataRow drDelFunc in dtDelFunc.Rows)
                    {
                        int nFuncId = 0;

                        string strFuncId = drDelFunc["EV700_FUNC_ID"].ToString();
                        if (int.TryParse(strFuncId, out nFuncId))
                        {
                            nFuncId = int.Parse(strFuncId);
                        }
                        Function_Info FuncInfo = GetFuncInfoFromSnapshot(nFuncId, evt.EventId, nSnapshotPreviousID);

                        FuncInfo.FuncClass = "Delete";

                        lstDelFunctions.Add(FuncInfo);
                    }
                }
            }
            catch (Exception ex)
            {
                AMP_Common.sendErrorException(strEmailFrom, strEmailTo, "Amendment Runtime Error", rule, evt, finfo, dep, nSnapshotPreviousID, nSnapshotCurrentID, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message);
            }
            finally
            {
                conn.Close();
            }
            return lstDelFunctions;
        }

        public static void getDeletedFunctionItems()
        {
            SqlConnection conn = new SqlConnection(strEBMSConn);

            try
            {
                conn.Open();
                                
                string strSQLDelOrd = "select  STUFF((select  ER101_DESC  ";
                strSQLDelOrd += " from ER101_ACCT_ORDER_DTL_Prev dtl inner join AMP_Noti_ResDep dep on dtl.ER101_NEW_RES_TYPE=dep.New_Res_Type and dtl.ER101_RES_CODE=dep.Res_Code ";
                strSQLDelOrd += " where dep.Noti_Dep_Code=@deptcode and ER101_EVT_ID=@eventid and ER101_FUNC_ID=@funcid ";
                strSQLDelOrd += " for xml path('')), 1,0, '') as MSGText ";
                strSQLDelOrd += " ,STUFF((select '<li style=\"FONT-SIZE:10pt;LINE-HEIGHT:normal;FONT-WEIGHT:normal;FONT-FAMILY:Arial;FONT-STYLE:normal;\"><p style=\"margin: 0 0 10px 0;text-align: left;\"><span style=\"FONT-SIZE:10pt;\"> ' + ER101_DESC + ' has been deleted. ' +  CAST(ER101_RES_QTY AS VARCHAR) + ' ' + CAST(ER101_UOM AS VARCHAR) + ' ' + convert(varchar, CAST([ER101_START_DATE_ISO] AS DATE), 103) + ' ' + convert(varchar, CAST(ER101_START_TIME_ISO AS time), 108) + ' - ' + convert(varchar, CAST([ER101_END_DATE_ISO] AS DATE), 103) + ' ' + convert(varchar, CAST([ER101_END_TIME_ISO] AS TIME), 108) +'</span></p></li>' ";
                strSQLDelOrd += " from ER101_ACCT_ORDER_DTL_Prev dtl inner join AMP_Noti_ResDep dep on dtl.ER101_NEW_RES_TYPE=dep.New_Res_Type and dtl.ER101_RES_CODE=dep.Res_Code ";
                strSQLDelOrd += " where dep.Noti_Dep_Code=@deptcode and ER101_EVT_ID=@eventid and ER101_FUNC_ID=@funcid ";
                strSQLDelOrd += " for xml path('')), 283,0, '') as MSGTextHTML ";

                SqlCommand commDelOrd = new SqlCommand(strSQLDelOrd, conn);

                commDelOrd.Parameters.Add("@deptcode", SqlDbType.VarChar, 20).Value = dep.DepartmentCode;
                commDelOrd.Parameters.Add("@eventid", SqlDbType.Int).Value = evt.EventId;
                commDelOrd.Parameters.Add("@funcid", SqlDbType.Int).Value = finfo.FuncId;
                commDelOrd.CommandTimeout = nCommandTimeOut;

                SqlDataReader drDelOrd;
                drDelOrd = commDelOrd.ExecuteReader();
                if (drDelOrd.Read())
                {
                    if (!string.IsNullOrEmpty(drDelOrd["MSGText"].ToString()))
                    {
                        eMsg.FuncUpdated = true;
                        eMsg.MSGText += "Deleted Order Items: \r\n" + drDelOrd["MSGText"].ToString();
                        eMsg.MSGHTML += "<span style='font: bold 10pt arial;padding-left:55px;'>Deleted Order Items:</span></p><ul style='MARGIN:0 0 10px 40px;LIST-STYLE-TYPE:disc;'>" + drDelOrd["MSGTextHTML"].ToString() + "</ul>";
                        eMsg.MSGHTML = eMsg.MSGHTML.Replace("&lt;", "<").Replace("&gt;", ">");
                    }
                }
            }

            catch (Exception ex)
            {
                AMP_Common.sendErrorException(strEmailFrom, strEmailTo, "Amendment Runtime Error", rule, evt, finfo, dep, nSnapshotPreviousID, nSnapshotCurrentID, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message);
            }
            finally
            {
                conn.Close();
            }            
        }
    }
}