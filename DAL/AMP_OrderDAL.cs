using Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace DAL
{
    public class AMP_OrderDAL
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

        public static Order_Info oinfo;

        public static EventAccountMessage eMsg;
        
        public static List<Order_Info> getCurrentFunctionOrders()
        {
            List<Order_Info> lstOrders = new List<Order_Info>();

            SqlConnection conn = new SqlConnection(strEBMSConn);

            try
            {
                conn.Open();

                string strSQL = "SELECT distinct ER100_ORD_NBR from ER100_ACCT_ORDER_Curr where ER100_EVT_ID=@eventid and ER100_FUNC_ID=@funcid ";

                SqlCommand comm = new SqlCommand(strSQL, conn);
                comm.Parameters.Add("@eventid", SqlDbType.Int).Value = evt.EventId;
                comm.Parameters.Add("@funcid", SqlDbType.Int).Value = finfo.FuncId;
                comm.CommandTimeout = nCommandTimeOut;

                SqlDataAdapter da = new SqlDataAdapter(comm);
                DataTable dt = new DataTable();
                da.Fill(dt);
                da.FillSchema(dt, SchemaType.Source);

                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        int nOrderNumber;
                        if (int.TryParse(dr["ER100_ORD_NBR"].ToString(), out nOrderNumber)) nOrderNumber = int.Parse(dr["ER100_ORD_NBR"].ToString());

                        Order_Info order = getOrderInformation(nOrderNumber, nSnapshotCurrentID);
                        lstOrders.Add(order);
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

            return lstOrders;
        }

        public static Order_Info getOrderInformation(int nOrderNumber, int nSnapshotId)
        {
            Order_Info order = new Order_Info();
            
            SqlConnection conn = new SqlConnection(strEBMSConn);

            try
            {
                conn.Open();

                string strSQL = " SELECT ER100_EVT_ID, ER100_FUNC_ID, ER100_ORD_NBR, ER100_BOOTH_NBR ";
                strSQL += " FROM EBMS_Snapshot.dbo.ER100_ACCT_ORDER ";
                strSQL += "where ER100_EVT_ID=@eventid and ER100_FUNC_ID=@funcid and ER100_ORD_NBR=@ordnbr and ER100_SNAPSHOT_ID=@snapshotid";

                SqlCommand comm = new SqlCommand(strSQL, conn);
                
                comm.Parameters.Add("@eventid", SqlDbType.Int).Value = evt.EventId;
                comm.Parameters.Add("@funcid", SqlDbType.Int).Value = finfo.FuncId;
                comm.Parameters.Add("@ordnbr", SqlDbType.Int).Value = nOrderNumber;
                comm.Parameters.Add("@snapshotid", SqlDbType.Int).Value = nSnapshotId;

                comm.CommandTimeout = nCommandTimeOut;

                SqlDataReader dr = comm.ExecuteReader();
                if (dr.Read() && dr.HasRows)
                {
                    order.EventId = evt.EventId;
                    order.FuncId = finfo.FuncId;
                    order.Order_Number = nOrderNumber;
                    order.BoothNumber = dr["ER100_BOOTH_NBR"].ToString();                    
                }
                else
                {
                    order.EventId = evt.EventId;
                    order.FuncId = finfo.FuncId;
                    order.Order_Number = nOrderNumber;
                    order.BoothNumber = "";
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

            return order;
        }

        public static void getOrderHeader()
        {
            eMsg = new EventAccountMessage();
            eMsg.EventId = evt.EventId;
            eMsg.AcctCode = evt.AccountNo;
            eMsg.FuncId = finfo.FuncId;

            if (finfo.FuncType == "EXS")
            {
                if (oinfo.ChangeType == "Delete")
                {
                    eMsg.OrderUpdated = true;
                    eMsg.MSGText = "Order:" + oinfo.Order_Number.ToString() + " (Booth:" + oinfo.BoothNumber + ") has been deleted.";
                    eMsg.MSGHTML = "<DIV style='FONT-FAMILY:Arial;padding-left:40px;FONT-SIZE:10pt;'><span style='font: bold 10pt arial;'>Order: <s>" + oinfo.Order_Number + " (Booth:" + oinfo.BoothNumber + ") has been deleted.</s></span> </div>";

                }
                else
                {
                    //"booth number"
                    eMsg.MSGText = "Order:" + oinfo.Order_Number.ToString() + " (Booth:" + oinfo.BoothNumber + ")";
                    eMsg.MSGHTML = "<DIV style='FONT-FAMILY:Arial;padding-left:40px;FONT-SIZE:10pt;'><span style='font: bold 10pt arial;'>Order: " + oinfo.Order_Number + " (Booth:" + oinfo.BoothNumber + ")</span> </div>";
                }
            }
            else
            {
                if (oinfo.ChangeType == "Delete")
                {
                    eMsg.OrderUpdated = true;
                    eMsg.MSGText = "Order:" + oinfo.Order_Number.ToString() + " has been deleted.";
                    eMsg.MSGHTML = "<DIV style='FONT-FAMILY:Arial;padding-left:40px;FONT-SIZE:10pt;'><span style='font: bold 10pt arial;'>Order: <s>" + oinfo.Order_Number + " has been deleted.</s></span> </div>";

                }
                else
                {
                    eMsg.MSGText = "Order:" + oinfo.Order_Number.ToString();
                    eMsg.MSGHTML = "<DIV style='FONT-FAMILY:Arial;padding-left:40px;FONT-SIZE:10pt;'><span style='font: bold 10pt arial;'>Order: " + oinfo.Order_Number + "</span> </div>";
                }                
            }
        }
        
        public static void checkOrderChange()
        {            
            SqlConnection conn = new SqlConnection(strEBMSConn);

            try
            {
                conn.Open();

                string strSQL = "SELECT Ord_Current.ER100_ORD_NBR as CurrOrdNbr,Ord_Current.ER100_BOOTH_NBR as CurrBoothNbr, Ord_Prev.ER100_ORD_NBR as PrevOrdNbr,Ord_Prev.ER100_BOOTH_NBR as PrevBoothNbr, ";
                strSQL += " Ord_Current.ER100_ORD_ACCT as CurrOrdAcct, Ord_Prev.ER100_ORD_ACCT as PrevOrdAcct FROM ";
                strSQL += " (SELECT distinct ER100_ORD_NBR,ER100_BOOTH_NBR,ER100_ORD_ACCT from ER100_ACCT_ORDER_Curr where ER100_EVT_ID=@eventid and ER100_FUNC_ID=@funcid and ER100_ORD_NBR=@ordnbr) AS Ord_Current ";
                strSQL += " Left JOIN  ";
                strSQL += " (SELECT distinct ER100_ORD_NBR,ER100_BOOTH_NBR,ER100_ORD_ACCT from ER100_ACCT_ORDER_Prev where ER100_EVT_ID=@eventid and ER100_FUNC_ID=@funcid and ER100_ORD_NBR=@ordnbr) AS Ord_Prev on Ord_Current.ER100_ORD_NBR=Ord_Prev.ER100_ORD_NBR";                

                SqlCommand comm = new SqlCommand(strSQL, conn);
                comm.Parameters.Add("@eventid", SqlDbType.Int).Value = evt.EventId;
                comm.Parameters.Add("@funcid", SqlDbType.Int).Value = finfo.FuncId;
                comm.Parameters.Add("@ordnbr", SqlDbType.Int).Value = oinfo.Order_Number;
                comm.CommandTimeout = nCommandTimeOut;

                SqlDataReader dr = comm.ExecuteReader();

                if (dr.Read())
                {
                    //new added
                    if (dr["PrevOrdNbr"] == DBNull.Value)
                    {
                        eMsg.OrderUpdated = true;

                        eMsg.MSGText += "Order is newly added \r\n";

                        eMsg.MSGHTML += "<DIV style='FONT-FAMILY:Arial;padding-left:45px;FONT-SIZE:10pt;'><p style='margin: 0 0 10px 0;text-align: left;'>Order is newly added</p></div>";
                    }
                    if (finfo.FuncType == "EXS")
                    {
                        if (dr["PrevOrdNbr"] != DBNull.Value && dr["CurrBoothNbr"].ToString() != dr["PrevBoothNbr"].ToString())
                        {
                            eMsg.OrderUpdated = true;

                            if (dr["CurrBoothNbr"].ToString() == "" && dr["PrevBoothNbr"].ToString() != "")
                            {
                                eMsg.MSGText += "Order booth number is removed from " + dr["PrevBoothNbr"].ToString() + " for " + AMP_Common.GetAcctName(dr["PrevOrdAcct"].ToString()) +  "\r\n";
                                eMsg.MSGHTML += "<DIV style='FONT-FAMILY:Arial;padding-left:45px;FONT-SIZE:10pt;'><p style='margin: 0 0 10px 0;text-align: left;'>Order booth number is updated to " + dr["CurrBoothNbr"].ToString() + " from <strike>" + dr["PrevBoothNbr"].ToString() + "</strike> </p></div>";
                            }
                            else if (dr["CurrBoothNbr"].ToString() != "" && dr["PrevBoothNbr"].ToString() == "")
                            {
                                eMsg.MSGText += "Order booth number " + dr["CurrBoothNbr"].ToString() + "is added for " + AMP_Common.GetAcctName(dr["CurrOrdAcct"].ToString()) + "\r\n";
                                eMsg.MSGHTML += "<DIV style='FONT-FAMILY:Arial;padding-left:45px;FONT-SIZE:10pt;'><p style='margin: 0 0 10px 0;text-align: left;'>Order booth number is added for " + AMP_Common.GetAcctName(dr["CurrOrdAcct"].ToString()) + "</strike> </p></div>";
                            }
                            else
                            {
                                eMsg.MSGText += "Order booth number is updated to " + dr["CurrBoothNbr"].ToString() + " for " + AMP_Common.GetAcctName(dr["CurrOrdAcct"].ToString()) + " from " + dr["PrevBoothNbr"].ToString() + "\r\n";
                                eMsg.MSGHTML += "<DIV style='FONT-FAMILY:Arial;padding-left:45px;FONT-SIZE:10pt;'><p style='margin: 0 0 10px 0;text-align: left;'>Order booth number is updated to " + dr["CurrBoothNbr"].ToString() + " for " + AMP_Common.GetAcctName(dr["CurrOrdAcct"].ToString()) + " <strike>" + dr["PrevBoothNbr"].ToString() + "</strike> </p></div>";
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

        public static void checkOrderNotesChange()
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
                strSQL += " (SELECT CC025_EVT_ID,CC025_FUNC_ID,CC025_ORDER, CC025_ORD_LINE,CC025_NOTE_TYPE,CC025_NOTE_HDR_SEQ, CC025_NOTE_CODE, CC025_NOTE_DESC,CC025_NOTE_PRT_CODE,CC025_NOTE_TEXT,CC025_NOTE_CLASS,CC025_UPD_DATE,CC025_UPD_USER_ID,CC025_HTML_TEXT,CC025_ENT_DATE,CC025_ENT_USER_ID FROM CC025_NOTES_EXT_Curr WHERE CC025_EVT_ID=@eventid AND CC025_NOTE_TYPE='OH' and CC025_ORDER=@ordernumber) as CC025_Live ";
                strSQL += " FULL OUTER JOIN ";
                strSQL += " (SELECT CC025_EVT_ID,CC025_FUNC_ID,CC025_ORDER, CC025_ORD_LINE,CC025_NOTE_TYPE,CC025_NOTE_HDR_SEQ, CC025_NOTE_CODE,CC025_NOTE_DESC,CC025_NOTE_PRT_CODE,CC025_NOTE_TEXT,CC025_NOTE_CLASS,CC025_UPD_DATE,CC025_UPD_USER_ID,CC025_HTML_TEXT,CC025_ENT_DATE,CC025_ENT_USER_ID FROM CC025_NOTES_EXT_Prev WHERE CC025_EVT_ID=@eventid AND CC025_NOTE_TYPE='OH' and CC025_ORDER=@ordernumber) as CC025_Snapshot  ";
                strSQL += " ON CC025_Live.CC025_NOTE_TYPE = CC025_Snapshot.CC025_NOTE_TYPE AND CC025_Live.CC025_NOTE_CODE = CC025_Snapshot.CC025_NOTE_CODE AND CC025_Live.CC025_NOTE_HDR_SEQ = CC025_Snapshot.CC025_NOTE_HDR_SEQ ";
                strSQL += " WHERE (CC025_Live.CC025_NOTE_CODE is null or CC025_Snapshot.CC025_NOTE_CODE is null OR CC025_Live.CC025_NOTE_TEXT<>CC025_Snapshot.CC025_NOTE_TEXT) ";
                strSQL += " ) AS NOTE_DIFF ";
                strSQL += " INNER JOIN AMP_Noti_NoteClassDep on Note_Class=CC025_NOTE_CLASS and Noti_Dept_Code=@deptcode ";
                
                SqlCommand comm = new SqlCommand(strSQL, conn);
                comm.Parameters.Add("@eventid", SqlDbType.Int).Value = evt.EventId;
                comm.Parameters.Add("@deptcode", SqlDbType.VarChar, 20).Value = dep.DepartmentCode;
                comm.Parameters.Add("@ordernumber", SqlDbType.Int).Value = oinfo.Order_Number;
                comm.CommandTimeout = nCommandTimeOut; ;

                SqlDataAdapter da = new SqlDataAdapter(comm);
                DataTable dt = new DataTable();
                da.Fill(dt);
                da.FillSchema(dt, SchemaType.Source);

                if (dt.Rows.Count > 0)
                {
                    eMsg.OrderUpdated = true;

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
                            eMsg.MSGText += "New Order " + dr["CC025_NOTE_DESC"].ToString() + " Notes entered/updated by " + AMP_Common.GetUserName(dr["CC025_UPD_USER_ID"].ToString()) + " on " + dtUpdDate.ToString("dd/MM/yyyy hh:mm") + "\r\n";
                            eMsg.MSGHTML += "<DIV style='font:10pt arial;'><p style='margin: 0 0 10px 0;text-align: left;'><span style='font: bold 10pt arial;padding-left:45px;'>New Order Notes (" + dr["CC025_NOTE_DESC"].ToString() + ")</span> entered/updated by " + AMP_Common.GetUserName(dr["CC025_UPD_USER_ID"].ToString()) + " on " + dtUpdDate.ToString("dd/MM/yyyy hh:mm") + " </p>";
                            eMsg.MSGHTML += "<ul style='MARGIN:0 0 10px 40px;LIST-STYLE-TYPE:disc;'><li style=font:10pt arial;'><p style='MARGIN:0 0 10px 0;TEXT-ALIGN:left;'>";
                            if (dr["CC025_Live_NOTE_TEXT"].ToString().Length <= rule.NotesLength )
                            {
                                eMsg.MSGText += dr["CC025_Live_NOTE_TEXT"].ToString() + "\r\n";
                                eMsg.MSGHTML += dr["CC025_Live_HTML_TEXT"].ToString();
                            }
                            else
                            {
                                eMsg.MSGText += dr["CC025_Live_NOTE_TEXT"].ToString().Substring(0, rule.NotesLength - 3) + " ...\r\n";
                                eMsg.MSGHTML += dr["CC025_Live_HTML_TEXT"].ToString().Substring(0, rule.NotesLength - 3) + " ...";
                            }
                            eMsg.MSGHTML += "<br></p></li></ul></DIV>";
                        }
                        //deleted notes
                        else if (dr["CC025_Live_NOTE_TEXT"] == DBNull.Value)
                        {
                            eMsg.MSGText += "Deleted Order " + dr["CC025_NOTE_DESC"].ToString() + " Notes for order: " + oinfo.Order_Number + ": \r\n";
                            eMsg.MSGHTML += "<DIV style='font:10pt arial;'><p style='margin: 0 0 10px 0;text-align: left;'><span style='font: bold 10pt arial;padding-left:45px;'>Deleted Order Notes (" + dr["CC025_NOTE_DESC"].ToString() + ") </p>";
                            eMsg.MSGHTML += "<ul style='MARGIN:0 0 10px 40px;LIST-STYLE-TYPE:disc;'><li style=font:10pt arial;'><p style='MARGIN:0 0 10px 0;TEXT-ALIGN:left;'>";
                            if (dr["CC025_Snapshot_NOTE_TEXT"].ToString().Length <= rule.NotesLength)
                            {
                                eMsg.MSGText += dr["CC025_Snapshot_NOTE_TEXT"].ToString() + "\r\n";
                                eMsg.MSGHTML += dr["CC025_Snapshot_HTML_TEXT"].ToString();
                            }
                            else
                            {
                                eMsg.MSGText += dr["CC025_Snapshot_NOTE_TEXT"].ToString().Substring(0, rule.NotesLength - 3) + " ...\r\n";
                                eMsg.MSGHTML += dr["CC025_Snapshot_HTML_TEXT"].ToString().Substring(0, rule.NotesLength - 3) + " ...";
                            }
                            eMsg.MSGHTML += "<br></p></li></ul></DIV>";
                        }
                        else
                        {
                            eMsg.MSGText += "Order " + dr["CC025_NOTE_DESC"].ToString() + " Notes Updated by " + AMP_Common.GetUserName(dr["CC025_UPD_USER_ID"].ToString()) + " on " + dtUpdDate.ToString("dd/MM/yyyy hh:mm") + " \r\n";
                            eMsg.MSGHTML += "<DIV style='font:10pt arial;'><p style='margin: 0 0 10px 0;text-align: left;'><span style='font: bold 10pt arial;padding-left:45px;'>Order Notes (" + dr["CC025_NOTE_DESC"].ToString() + ") Updated</span> by " + AMP_Common.GetUserName(dr["CC025_UPD_USER_ID"].ToString()) + " on " + dtUpdDate.ToString("dd/MM/yyyy hh:mm") + " </p>";
                            eMsg.MSGHTML += "<ul style='MARGIN:0 0 10px 40px;LIST-STYLE-TYPE:disc;'><li style=font:10pt arial;'><p style='MARGIN:0 0 10px 0;TEXT-ALIGN:left;'>";
                            if (dr["CC025_Live_NOTE_TEXT"] != DBNull.Value && dr["CC025_Snapshot_NOTE_TEXT"] != DBNull.Value)
                            {
                                if (dr["CC025_Live_NOTE_TEXT"].ToString().Length <= rule.NotesLength )
                                {
                                    eMsg.MSGText += "Update To : " + dr["CC025_Live_NOTE_TEXT"].ToString() + "\r\n";                                    
                                    eMsg.MSGHTML += "Update To :" + dr["CC025_Live_HTML_TEXT"].ToString() + "<br/>";                                    
                                }
                                else
                                {
                                    eMsg.MSGText += "Update To : " + dr["CC025_Live_NOTE_TEXT"].ToString().Substring(0, rule.NotesLength - 3) + " ...r\n";
                                    eMsg.MSGHTML += "Update To :" + dr["CC025_Live_HTML_TEXT"].ToString().Substring(0, rule.NotesLength - 3) + " ...< br/>";
                                    
                                }

                                if (dr["CC025_Snapshot_NOTE_TEXT"].ToString().Length <= rule.NotesLength )
                                {
                                    eMsg.MSGText += "from:" + dr["CC025_Snapshot_NOTE_TEXT"].ToString() + "\r\n";
                                    eMsg.MSGHTML += "from:<strke>" + dr["CC025_Snapshot_HTML_TEXT"].ToString() + "</strke><br/>";
                                }
                                else
                                {
                                    eMsg.MSGText += "from: " + dr["CC025_Snapshot_NOTE_TEXT"].ToString().Substring(0, rule.NotesLength - 3) + "\r\n";
                                    eMsg.MSGHTML += "from:<strke>" + dr["CC025_Snapshot_HTML_TEXT"].ToString().Substring(0, rule.NotesLength - 3) + " ...</strke><br/>";
                                }
                                eMsg.MSGHTML += "<br></p></li></ul></DIV>";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AMP_Common.sendErrorException(strEmailFrom, strEmailTo, "Amendment Runtime Error - Order", rule, evt, finfo, dep, nSnapshotPreviousID, nSnapshotCurrentID, System.Reflection.MethodBase.GetCurrentMethod().Name, "Order Number:" + oinfo.Order_Number + "<br/>" + ex.Message);                
            }
            finally
            {
                conn.Close();
            }
            
        }

        public static void checkOrderItemChange()
        {
            SqlConnection conn = new SqlConnection(strEBMSConn);

            try
            {
                conn.Open();
                //Get updated order data 
                string strSQL = " SELECT STUFF((SELECT CAST(MSG as VARCHAR(4000)) from ";
                strSQL += " (SELECT ";
                strSQL += " CASE WHEN ord_prev.ER101_ORD_NBR is null THEN ord_curr.ER101_NEW_RES_TYPE ELSE ord_prev.ER101_NEW_RES_TYPE end as ER101_NEW_RES_TYPE, ";
                strSQL += " CASE WHEN ord_prev.ER101_ORD_NBR is null THEN ord_curr.ER101_RES_CODE ELSE ord_prev.ER101_RES_CODE end as ER101_RES_CODE, ";
                strSQL += " CASE ";
                strSQL += " WHEN ord_prev.ER101_ORD_NBR is null THEN 'Item Added. ' + ord_curr.ER101_DESC + '('+ CAST(ord_curr.ER101_ORD_NBR AS VARCHAR) + ': '+ CAST(ord_curr.ER101_ORD_LINE AS VARCHAR)+') Qty.: ' + CAST(ord_curr.ER101_RES_QTY AS VARCHAR) + ' ' + CAST(ord_curr.ER101_UOM AS VARCHAR) + ', ' + convert(varchar, ord_curr.ER101_START_DATE_ISO, 103) + ' ' + convert(varchar(5), ord_curr.ER101_START_TIME_ISO, 108) + ' - ' + convert(varchar, ord_curr.ER101_END_DATE_ISO, 103) + ' ' + convert(varchar(5), ord_curr.ER101_END_TIME_ISO, 108) + CHAR(13)+CHAR(10) ";
                strSQL += " WHEN ord_curr.ER101_ORD_NBR is null THEN 'Item Deleted. ' + ord_prev.ER101_DESC + '('+ CAST(ord_prev.ER101_ORD_NBR AS VARCHAR) + ': '+ CAST(ord_prev.ER101_ORD_LINE AS VARCHAR)+') Qty.: ' + CAST(ord_prev.ER101_RES_QTY AS VARCHAR) + ' ' + CAST(ord_prev.ER101_UOM AS VARCHAR) + ', ' + convert(varchar, ord_prev.ER101_START_DATE_ISO, 103) + ' ' + convert(varchar(5), ord_prev.ER101_START_TIME_ISO, 108) + ' - ' + convert(varchar, ord_prev.ER101_END_DATE_ISO, 103) + ' ' + convert(varchar(5), ord_prev.ER101_END_TIME_ISO, 108) + CHAR(13)+CHAR(10) ";
                strSQL += " WHEN ord_prev.ER101_ORD_NBR is not null and ord_curr.ER101_ORD_NBR is not null THEN ";
                strSQL += "    CASE ";
                strSQL += "         WHEN ord_curr.ER101_DESC <> ord_prev.ER101_DESC then ";
                strSQL += "             'Item Changed. ' + CHAR(13) + CHAR(10) + ' Previous: ' + ord_prev.ER101_DESC + '('+ CAST(ord_prev.ER101_ORD_NBR AS VARCHAR) + ': '+ CAST(ord_prev.ER101_ORD_LINE AS VARCHAR)+') Qty.: ' + CAST(ord_prev.ER101_RES_QTY AS VARCHAR) + ' ' + CAST(ord_prev.ER101_UOM AS VARCHAR) + ', ' + convert(varchar, ord_prev.ER101_START_DATE_ISO, 103) + ' ' + convert(varchar(5), ord_prev.ER101_START_TIME_ISO, 108) + ' - ' + convert(varchar, ord_prev.ER101_END_DATE_ISO, 103) + ' ' + convert(varchar(5), ord_prev.ER101_END_TIME_ISO, 108) ";
                strSQL += "                           + CHAR(13) + CHAR(10) + ' Current: ' + ord_curr.ER101_DESC + '('+ CAST(ord_curr.ER101_ORD_NBR AS VARCHAR) + ': '+ CAST(ord_curr.ER101_ORD_LINE AS VARCHAR)+') Qty.: ' + CAST(ord_prev.ER101_RES_QTY AS VARCHAR) + ' ' + CAST(ord_prev.ER101_UOM AS VARCHAR) + ', ' + convert(varchar, ord_curr.ER101_START_DATE_ISO, 103) + ' ' + convert(varchar(5), ord_curr.ER101_START_TIME_ISO, 108) + ' - ' + convert(varchar, ord_curr.ER101_END_DATE_ISO, 103) + ' ' + convert(varchar(5), ord_curr.ER101_END_TIME_ISO, 108) ";
                strSQL += "         ELSE ";
                strSQL += "             'Item Updated. ' + ord_curr.ER101_DESC + '('+ CAST(ord_curr.ER101_RES_QTY AS VARCHAR) + ' ' + CAST(ord_curr.ER101_UOM AS VARCHAR)+')' ";
                strSQL += "                           + CHAR(13) + CHAR(10) + ' Previous: Qty.: ' + CAST(ord_prev.ER101_RES_QTY AS VARCHAR) + ' ' + CAST(ord_prev.ER101_UOM AS VARCHAR) + ', ' + convert(varchar, ord_prev.ER101_START_DATE_ISO, 103) + ' ' + convert(varchar(5), ord_prev.ER101_START_TIME_ISO, 108) + ' - ' + convert(varchar, ord_prev.ER101_END_DATE_ISO, 103) + ' ' + convert(varchar(5), ord_prev.ER101_END_TIME_ISO, 108) ";
                strSQL += "                   + CHAR(13) + CHAR(10) + ' Current: Qty.: ' + CAST(ord_curr.ER101_RES_QTY AS VARCHAR) + ' ' + CAST(ord_curr.ER101_UOM AS VARCHAR) + ', ' + convert(varchar, ord_curr.ER101_START_DATE_ISO, 103) + ' ' + convert(varchar(5), ord_curr.ER101_START_TIME_ISO, 108) + ' - ' + convert(varchar, ord_curr.ER101_END_DATE_ISO, 103) + ' ' + convert(varchar(5), ord_curr.ER101_END_TIME_ISO, 108) ";
                strSQL += "     END ";
                strSQL += " END AS MSG ";
                strSQL += " FROM ";
                strSQL += " (SELECT ER101_ORD_NBR,ER101_ORD_LINE,ER101_DESC,ER101_RES_QTY,ER101_START_DATE_ISO,ER101_START_TIME_ISO,ER101_END_DATE_ISO,ER101_END_TIME_ISO,ER101_NEW_RES_TYPE,ER101_RES_CODE,ER101_UOM,ER101_PHASE FROM ER101_ACCT_ORDER_DTL_Curr where ER101_EVT_ID=@eventid and ER101_FUNC_ID=@funcid and ER101_ORD_NBR=@ordernumber) AS ord_curr ";
                strSQL += " FULL OUTER JOIN ";
                strSQL += " (SELECT ER101_ORD_NBR,ER101_ORD_LINE,ER101_DESC,ER101_RES_QTY,ER101_START_DATE_ISO,ER101_START_TIME_ISO,ER101_END_DATE_ISO,ER101_END_TIME_ISO,ER101_NEW_RES_TYPE,ER101_RES_CODE,ER101_UOM,ER101_PHASE FROM ER101_ACCT_ORDER_DTL_Prev where ER101_EVT_ID=@eventid and ER101_FUNC_ID=@funcid and ER101_ORD_NBR=@ordernumber) AS ord_prev ";
                strSQL += " ON ord_curr.ER101_ORD_NBR = ord_prev.ER101_ORD_NBR AND ord_curr.ER101_ORD_LINE = ord_prev.ER101_ORD_LINE ";
                strSQL += " WHERE (ord_curr.ER101_PHASE='1' or ord_prev.ER101_PHASE='1') and (ord_curr.ER101_ORD_NBR is null or ord_prev.ER101_ORD_NBR is null or ord_curr.ER101_DESC <> ord_prev.ER101_DESC ";
                strSQL += " or ord_curr.ER101_RES_QTY <> ord_prev.ER101_RES_QTY or CAST(ord_curr.ER101_START_DATE_ISO As DATE) <> CAST(ord_prev.ER101_START_DATE_ISO As DATE) ";
                strSQL += " or CAST(ord_curr.ER101_START_TIME_ISO AS TIME)<> CAST(ord_prev.ER101_START_TIME_ISO AS TIME) or CAST(ord_curr.ER101_END_DATE_ISO As DATE)<> CAST(ord_prev.ER101_END_DATE_ISO As DATE) ";
                strSQL += " or CAST(ord_curr.ER101_END_TIME_ISO AS TIME)<> CAST(ord_prev.ER101_END_TIME_ISO AS TIME)) ";
                strSQL += " ) as taborder INNER JOIN AMP_Noti_ResDep ResChangeDep on taborder.ER101_NEW_RES_TYPE=ResChangeDep.New_Res_Type and taborder.ER101_RES_CODE=ResChangeDep.Res_Code ";
                strSQL += " where ResChangeDep.Noti_Dep_Code=@deptcode for xml path('')),1,0,'') as MSGText, ";
                strSQL += " STUFF((SELECT CAST(MSG as VARCHAR(8000)) from ";
                strSQL += " (SELECT ";
                strSQL += " CASE WHEN ord_prev.ER101_ORD_NBR is null THEN ord_curr.ER101_ORD_NBR ELSE ord_prev.ER101_ORD_NBR end as ER101_ORD_NBR, ";
                strSQL += " CASE WHEN ord_prev.ER101_ORD_NBR is null THEN ord_curr.ER101_ORD_LINE ELSE ord_prev.ER101_ORD_LINE end as ER101_ORD_LINE, ";
                strSQL += " CASE WHEN ord_prev.ER101_ORD_NBR is null THEN ord_curr.ER101_LIN_NBR ELSE ord_prev.ER101_LIN_NBR end as ER101_LIN_NBR, ";
                strSQL += " CASE WHEN ord_prev.ER101_ORD_NBR is null THEN ord_curr.ER101_NEW_RES_TYPE ELSE ord_prev.ER101_NEW_RES_TYPE end as ER101_NEW_RES_TYPE, ";
                strSQL += " CASE WHEN ord_prev.ER101_ORD_NBR is null THEN ord_curr.ER101_RES_CODE ELSE ord_prev.ER101_RES_CODE end as ER101_RES_CODE, ";
                strSQL += " '<tr style=#39;display:table-row;background:#f6f6f6;#39;>' + ";
                strSQL += " CASE WHEN ord_prev.ER101_ORD_NBR is null then ";
                strSQL += "         CASE WHEN ord_curr.ER101_ORD_LINE = ord_curr.ER101_LIN_NBR THEN ";
                strSQL += "         '<td>+ ' + ord_curr.ER101_DESC + '</td><td>' + CAST(ord_curr.ER101_RES_QTY AS VARCHAR) + '</td><td>' + CAST(ord_curr.ER101_UOM AS VARCHAR) + '</td><td>' + convert(varchar, ord_curr.ER101_START_DATE_ISO, 103) + '</td><td>' + convert(varchar(5), ord_curr.ER101_START_TIME_ISO, 108) + '</td><td>' + convert(varchar, ord_curr.ER101_END_DATE_ISO, 103) +'</td><td>'+ convert(varchar(5), ord_curr.ER101_END_TIME_ISO, 108) + '</td>'";
                if (rule.ShowPackageItemDateTime)
                {
                    strSQL += "         ELSE '<td>+ <small>' + ord_curr.ER101_DESC + '</small></td><td><small>' + CAST(ord_curr.ER101_RES_QTY AS VARCHAR) + '</small></td><td><small>' + CAST(ord_curr.ER101_UOM AS VARCHAR) + '</small></td><td><small>' + convert(varchar, ord_curr.ER101_START_DATE_ISO, 103) + '</small></td><td><small>' + convert(varchar(5), ord_curr.ER101_START_TIME_ISO, 108) + '</small></td><td><small>' + convert(varchar, ord_curr.ER101_END_DATE_ISO, 103) +'</small></td><td><small>'+ convert(varchar(5), ord_curr.ER101_END_TIME_ISO, 108) + '</small></td>'";
                }
                else
                {
                    strSQL += "         ELSE '<td>+ <small>' + ord_curr.ER101_DESC + '</small></td><td><small>' + CAST(ord_curr.ER101_RES_QTY AS VARCHAR) + '</small></td><td><small>' + CAST(ord_curr.ER101_UOM AS VARCHAR) + '</small></td><td></td><td></td><td></td><td></td>'";
                }
                strSQL += "    END";
                strSQL += " WHEN ord_curr.ER101_ORD_NBR is null then ";
                strSQL += "     CASE WHEN ord_prev.ER101_ORD_LINE = ord_prev.ER101_LIN_NBR THEN ";
                strSQL += "         '<td>-  <s>' + ord_prev.ER101_DESC + '</small></s></td><td><s><small>' + CAST(ord_prev.ER101_RES_QTY AS VARCHAR) + '</small></s></td><td><s><small>' + CAST(ord_prev.ER101_UOM AS VARCHAR) + '</small></s></td><td><s><small>' + convert(varchar, ord_prev.ER101_START_DATE_ISO, 103) + '</small></s></td><td><s><small>' + convert(varchar(5), ord_prev.ER101_START_TIME_ISO , 108) + '</small></s></td><td><s><small>' + convert(varchar, ord_prev.ER101_END_DATE_ISO, 103) + '</small></s></td><td><s><small>' + convert(varchar(5), ord_prev.ER101_END_TIME_ISO, 108) + '</small></s></td>' ";
                if (rule.ShowPackageItemDateTime)
                {
                    strSQL += "         ELSE '<td>-  <s><small>' + ord_prev.ER101_DESC + '</small></s></td><td><s><small>' + CAST(ord_prev.ER101_RES_QTY AS VARCHAR) + '</small></s></td><td><s><small>' + CAST(ord_prev.ER101_UOM AS VARCHAR) + '</small></s></td><td><s><small>' + convert(varchar, ord_prev.ER101_START_DATE_ISO, 103) + '</small></s></td><td><s><small>' + convert(varchar(5), ord_prev.ER101_START_TIME_ISO , 108) + '</small></s></td><td><s><small>' + convert(varchar, ord_prev.ER101_END_DATE_ISO, 103) + '</small></s></td><td><s><small>' + convert(varchar(5), ord_prev.ER101_END_TIME_ISO, 108) + '</small></s></td>' ";
                }
                else
                {
                    strSQL += "         ELSE '<td>-  <s><small>' + ord_prev.ER101_DESC + '</small></s></td><td><s><small>' + CAST(ord_prev.ER101_RES_QTY AS VARCHAR) + '</small></s></td><td><s><small>' + CAST(ord_prev.ER101_UOM AS VARCHAR) + '</small></s></td><td></td><td></td><td></td><td></td>' ";
                }
                strSQL += "     END ";
                strSQL += "     WHEN ord_prev.ER101_ORD_NBR is not null and ord_curr.ER101_ORD_NBR is not null THEN ";
                strSQL += "     CASE WHEN ord_curr.ER101_ORD_LINE = ord_curr.ER101_LIN_NBR THEN";
                strSQL += "         '<td>' + CASE WHEN ord_curr.ER101_DESC<>ord_prev.ER101_DESC THEN '<s>' + ord_prev.ER101_DESC + '</s><br/>' + ord_curr.ER101_DESC else ord_curr.ER101_DESC END + '</td>' +";
                strSQL += "         '<td>' + CASE WHEN ord_prev.ER101_RES_QTY<>ord_curr.ER101_RES_QTY THEN '<s>' + CAST(ord_prev.ER101_RES_QTY as VARCHAR) + '</s><br/>' + CAST(ord_curr.ER101_RES_QTY as VARCHAR) else CAST(ord_curr.ER101_RES_QTY as VARCHAR) END + '</td>' +";
                strSQL += "         '<td>' + CASE WHEN ord_prev.ER101_UOM<>ord_curr.ER101_UOM THEN '<s>' + ord_prev.ER101_UOM + '</s><br/>' + ord_curr.ER101_UOM else ord_curr.ER101_UOM END + '</td>' +";
                strSQL += "         '<td>' + CASE WHEN CAST(ord_prev.ER101_START_DATE_ISO AS DATE)<> CAST(ord_curr.ER101_START_DATE_ISO AS DATE) THEN '<s>' + convert(varchar, ord_prev.ER101_START_DATE_ISO, 103) + '</s><br/>' + convert(varchar, ord_curr.ER101_START_DATE_ISO, 103) else convert(varchar, ord_curr.ER101_START_DATE_ISO, 103) END + '</td>' +";
                strSQL += "         '<td>' + CASE WHEN CAST(ord_prev.ER101_START_TIME_ISO AS TIME)<> CAST(ord_curr.ER101_START_TIME_ISO AS TIME) THEN '<s>' + convert(varchar(5), ord_prev.ER101_START_TIME_ISO, 108) + '</s><br/>' + convert(varchar(5), ord_curr.ER101_START_TIME_ISO, 108) else convert(varchar(5), ord_curr.ER101_START_TIME_ISO, 108) END + '</td>' +";
                strSQL += "         '<td>' + CASE WHEN CAST(ord_prev.ER101_END_DATE_ISO AS DATE)<> CAST(ord_curr.ER101_END_DATE_ISO AS DATE) THEN '<s>' + convert(varchar, ord_prev.ER101_END_DATE_ISO, 103) + '</s><br/>' + convert(varchar, ord_curr.ER101_END_DATE_ISO, 103) else convert(varchar, ord_curr.ER101_END_DATE_ISO, 103) END + '</td>' +";
                strSQL += "         '<td>' + CASE WHEN CAST(ord_prev.ER101_END_TIME_ISO AS TIME)<> CAST(ord_curr.ER101_END_TIME_ISO AS TIME) THEN '<s>' + convert(varchar(5), ord_prev.ER101_END_TIME_ISO, 108) + '</s><br/>' + convert(varchar(5), ord_curr.ER101_END_TIME_ISO, 108) else convert(varchar(5), ord_curr.ER101_END_TIME_ISO, 108) END + '</td>'";
                strSQL += "    ELSE";
                if (rule.ShowPackageItemDateTime)
                {
                    strSQL += "       '<td><small>' + CASE WHEN ord_curr.ER101_DESC<>ord_prev.ER101_DESC THEN '<s>' + ord_prev.ER101_DESC + '</s><br/>' + ord_curr.ER101_DESC else ord_curr.ER101_DESC END + '</small></td>' +";
                    strSQL += "       '<td><small>' + CASE WHEN ord_prev.ER101_RES_QTY<>ord_curr.ER101_RES_QTY THEN '<s>' + CAST(ord_prev.ER101_RES_QTY as VARCHAR) + '</s><br/>' + CAST(ord_curr.ER101_RES_QTY as VARCHAR) else CAST(ord_curr.ER101_RES_QTY as VARCHAR) END + '</small></td>' +";
                    strSQL += "       '<td><small>' + CASE WHEN ord_prev.ER101_UOM<>ord_curr.ER101_UOM THEN '<s>' + ord_prev.ER101_UOM + '</s><br/>' + ord_curr.ER101_UOM else ord_curr.ER101_UOM END + '</small></td>' +";
                    strSQL += "       '<td><small>' + CASE WHEN CAST(ord_prev.ER101_START_DATE_ISO AS DATE)<> CAST(ord_curr.ER101_START_DATE_ISO AS DATE) THEN '<s>' + convert(varchar, ord_prev.ER101_START_DATE_ISO, 103) + '</s><br/>' + convert(varchar, ord_curr.ER101_START_DATE_ISO, 103) else convert(varchar, ord_curr.ER101_START_DATE_ISO, 103) END + '</small></td>' +";
                    strSQL += "       '<td><small>' + CASE WHEN CAST(ord_prev.ER101_START_TIME_ISO AS TIME)<> CAST(ord_curr.ER101_START_TIME_ISO AS TIME) THEN '<s>' + convert(varchar(5), ord_prev.ER101_START_TIME_ISO, 108) + '</s><br/>' + convert(varchar(5), ord_curr.ER101_START_TIME_ISO, 108) else convert(varchar(5), ord_curr.ER101_START_TIME_ISO, 108) END + '</small></td>' +";
                    strSQL += "       '<td><small>' + CASE WHEN CAST(ord_prev.ER101_END_DATE_ISO AS DATE)<> CAST(ord_curr.ER101_END_DATE_ISO AS DATE) THEN '<s>' + convert(varchar, ord_prev.ER101_END_DATE_ISO, 103) + '</s><br/>' + convert(varchar, ord_curr.ER101_END_DATE_ISO, 103) else convert(varchar, ord_curr.ER101_END_DATE_ISO, 103) END + '</small></td>' +";
                    strSQL += "       '<td><small>' + CASE WHEN CAST(ord_prev.ER101_END_TIME_ISO AS TIME)<> CAST(ord_curr.ER101_END_TIME_ISO AS TIME) THEN '<s>' + convert(varchar(5), ord_prev.ER101_END_TIME_ISO, 108) + '</s><br/>' + convert(varchar(5), ord_curr.ER101_END_TIME_ISO, 108) else convert(varchar(5), ord_curr.ER101_END_TIME_ISO, 108) END + '</small></td>' ";
                }
                else
                {
                    strSQL += "       '<td><small>' + CASE WHEN ord_curr.ER101_DESC<>ord_prev.ER101_DESC THEN '<s>' + ord_prev.ER101_DESC + '</s><br/>' + ord_curr.ER101_DESC else ord_curr.ER101_DESC END + '</small></td>' +";
                    strSQL += "       '<td><small>' + CASE WHEN ord_prev.ER101_RES_QTY<>ord_curr.ER101_RES_QTY THEN '<s>' + CAST(ord_prev.ER101_RES_QTY as VARCHAR) + '</s><br/>' + CAST(ord_curr.ER101_RES_QTY as VARCHAR) else CAST(ord_curr.ER101_RES_QTY as VARCHAR) END + '</small></td>' +";
                    strSQL += "       '<td><small>' + CASE WHEN ord_prev.ER101_UOM<>ord_curr.ER101_UOM THEN '<s>' + ord_prev.ER101_UOM + '</s><br/>' + ord_curr.ER101_UOM else ord_curr.ER101_UOM END + '</small></td>' +";
                    strSQL += "       '<td><small>' + CASE WHEN CAST(ord_prev.ER101_START_DATE_ISO AS DATE)<> CAST(ord_curr.ER101_START_DATE_ISO AS DATE) THEN '<s>' + convert(varchar, ord_prev.ER101_START_DATE_ISO, 103) + '</s><br/>' + convert(varchar, ord_curr.ER101_START_DATE_ISO, 103) else '' END + '</small></td>' +";
                    strSQL += "       '<td><small>' + CASE WHEN CAST(ord_prev.ER101_START_TIME_ISO AS TIME)<> CAST(ord_curr.ER101_START_TIME_ISO AS TIME) THEN '<s>' + convert(varchar(5), ord_prev.ER101_START_TIME_ISO, 108) + '</s><br/>' + convert(varchar(5), ord_curr.ER101_START_TIME_ISO, 108) else '' END + '</small></td>' +";
                    strSQL += "       '<td><small>' + CASE WHEN CAST(ord_prev.ER101_END_DATE_ISO AS DATE)<> CAST(ord_curr.ER101_END_DATE_ISO AS DATE) THEN '<s>' + convert(varchar, ord_prev.ER101_END_DATE_ISO, 103) + '</s><br/>' + convert(varchar, ord_curr.ER101_END_DATE_ISO, 103) else '' END + '</small></td>' +";
                    strSQL += "       '<td><small>' + CASE WHEN CAST(ord_prev.ER101_END_TIME_ISO AS TIME)<> CAST(ord_curr.ER101_END_TIME_ISO AS TIME) THEN '<s>' + convert(varchar(5), ord_prev.ER101_END_TIME_ISO, 108) + '</s><br/>' + convert(varchar(5), ord_curr.ER101_END_TIME_ISO, 108) else '' END + '</small></td>' ";
                }
                strSQL += "     END ";
                strSQL += " END + '</tr>' AS MSG FROM ";
                strSQL += " (SELECT ER101_ORD_NBR,ER101_ORD_LINE,ER101_LIN_NBR,ER101_DESC,ER101_RES_QTY,ER101_START_DATE_ISO,ER101_START_TIME_ISO,ER101_END_DATE_ISO,ER101_END_TIME_ISO,ER101_NEW_RES_TYPE,ER101_RES_CODE,ER101_UOM,ER101_PHASE FROM ER101_ACCT_ORDER_DTL_Curr where ER101_EVT_ID=@eventid and ER101_FUNC_ID=@funcid and ER101_ORD_NBR=@ordernumber) AS ord_curr ";
                strSQL += " FULL OUTER JOIN ";
                strSQL += " (SELECT ER101_ORD_NBR,ER101_ORD_LINE,ER101_LIN_NBR, ER101_DESC,ER101_RES_QTY,ER101_START_DATE_ISO,ER101_START_TIME_ISO,ER101_END_DATE_ISO,ER101_END_TIME_ISO,ER101_NEW_RES_TYPE,ER101_RES_CODE,ER101_UOM,ER101_PHASE FROM ER101_ACCT_ORDER_DTL_Prev where ER101_EVT_ID=@eventid and ER101_FUNC_ID=@funcid and ER101_ORD_NBR=@ordernumber) AS ord_prev ";
                strSQL += " ON ord_curr.ER101_ORD_NBR = ord_prev.ER101_ORD_NBR AND ord_curr.ER101_ORD_LINE = ord_prev.ER101_ORD_LINE ";
                strSQL += " WHERE (ord_curr.ER101_PHASE='1' or ord_prev.ER101_PHASE='1') and (ord_curr.ER101_ORD_NBR is null or ord_prev.ER101_ORD_NBR is null or ord_curr.ER101_DESC <> ord_prev.ER101_DESC ";
                strSQL += " or ord_curr.ER101_RES_QTY <> ord_prev.ER101_RES_QTY or CAST(ord_curr.ER101_START_DATE_ISO As DATE) <> CAST(ord_prev.ER101_START_DATE_ISO As DATE) ";
                strSQL += " or CAST(ord_curr.ER101_START_TIME_ISO AS TIME)<> CAST(ord_prev.ER101_START_TIME_ISO AS TIME) or CAST(ord_curr.ER101_END_DATE_ISO As DATE)<> CAST(ord_prev.ER101_END_DATE_ISO As DATE) ";
                strSQL += " or CAST(ord_curr.ER101_END_TIME_ISO AS TIME)<> CAST(ord_prev.ER101_END_TIME_ISO AS TIME)) ";
                strSQL += " ) as taborder INNER JOIN AMP_Noti_ResDep ResChangeDep on taborder.ER101_NEW_RES_TYPE=ResChangeDep.New_Res_Type and taborder.ER101_RES_CODE=ResChangeDep.Res_Code ";
                strSQL += " where ResChangeDep.Noti_Dep_Code=@deptcode order by taborder.ER101_ORD_NBR, taborder.ER101_ORD_LINE for xml path('')),1,0,'') as MSGTextHTML ";

                SqlCommand comm = new SqlCommand(strSQL, conn);
                comm.Parameters.Add("@eventid", SqlDbType.Int).Value = evt.EventId;
                comm.Parameters.Add("@funcid", SqlDbType.Int).Value = finfo.FuncId;
                comm.Parameters.Add("@ordernumber", SqlDbType.Int).Value = oinfo.Order_Number;
                comm.Parameters.Add("@deptcode", SqlDbType.VarChar, 20).Value = dep.DepartmentCode;
                comm.CommandTimeout = nCommandTimeOut;

                SqlDataAdapter da = new SqlDataAdapter(comm);
                DataTable dt = new DataTable();
                da.Fill(dt);
                da.FillSchema(dt, SchemaType.Source);

                if (dt.Rows.Count > 0)
                { 
                    foreach (DataRow dr in dt.Rows)
                    {
                        if (!string.IsNullOrEmpty(dr["MSGText"].ToString()))
                        {
                            eMsg.OrderUpdated = true;

                            eMsg.MSGText += "Order Items: \r\n" + dr["MSGText"].ToString();
                            eMsg.MSGHTML += "<DIV style='FONT-FAMILY:Arial;padding-left:45px;FONT-SIZE:10pt;'>";
                            eMsg.MSGHTML += "<p style='margin: 0 0 10px 0;text-align: left;'><span style='font: bold 10pt arial;'>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Order Items:</span></p>";
                            eMsg.MSGHTML += " <table style='margin: 0 0 40px 0; padding-left:45px; width:100%; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.2); display:table;'><tr style='font-weight: 9; color:#ffffff;background:#ea6153;'><td>Description</td><td>Units</td><td>U/M</td><td>Start Date</td><td>Start Time</td><td>End Date</td><td>End Time</td></tr>" + dr["MSGTextHTML"].ToString() + "</table></div>";
                            eMsg.MSGHTML = eMsg.MSGHTML.Replace("&lt;", "<").Replace("&gt;", ">").Replace("#39;", "'");

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AMP_Common.sendErrorException(strEmailFrom, strEmailTo, "Amendment Runtime Error - Order", rule, evt, finfo, dep, nSnapshotPreviousID, nSnapshotCurrentID, System.Reflection.MethodBase.GetCurrentMethod().Name, "Order Number:" + oinfo.Order_Number + "<br/>" + ex.Message);
            }
            finally
            {
                conn.Close();
            }            
        }

        public static void checkOrderItemNotesChange()
        {
            SqlConnection conn = new SqlConnection(strEBMSConn);

            try
            {
                conn.Open();
                string strSQL = "SELECT CC025_Live_NOTE_TEXT,CC025_Snapshot_NOTE_TEXT,CC025_UPD_USER_ID,CC025_UPD_DATE,CC025_NOTE_DESC,CC025_FUNC_ID ,CC025_Live_HTML_TEXT,CC025_Snapshot_HTML_TEXT ,";
                strSQL += " CC025_ORDER,CC025_ORD_LINE, CC025_NOTE_TYPE , ER101_DESC ";
                strSQL += " FROM ";
                strSQL += " (SELECT CASE WHEN CC025_Live.CC025_NOTE_CLASS IS NULL THEN CC025_Snapshot.CC025_NOTE_CLASS ELSE CC025_Live.CC025_NOTE_CLASS END AS CC025_NOTE_CLASS, ";
                strSQL += " CASE WHEN CC025_Live.CC025_NOTE_CLASS IS NULL THEN CC025_Snapshot.CC025_UPD_DATE ELSE CC025_Live.CC025_UPD_DATE END AS CC025_UPD_DATE, ";
                strSQL += " CASE WHEN CC025_Live.CC025_NOTE_CLASS IS NULL THEN CC025_Snapshot.CC025_UPD_USER_ID ELSE CC025_Live.CC025_UPD_USER_ID END AS CC025_UPD_USER_ID, ";
                strSQL += " CASE WHEN CC025_Live.CC025_NOTE_CLASS IS NULL THEN CC025_Snapshot.CC025_NOTE_DESC ELSE CC025_Live.CC025_NOTE_DESC END AS CC025_NOTE_DESC, ";
                strSQL += " CASE WHEN CC025_Live.CC025_NOTE_CLASS IS NULL THEN CC025_Snapshot.CC025_NOTE_TYPE ELSE CC025_Live.CC025_NOTE_TYPE END AS CC025_NOTE_TYPE, ";
                strSQL += " CASE WHEN CC025_Live.CC025_NOTE_CLASS IS NULL THEN CC025_Snapshot.CC025_FUNC_ID ELSE CC025_Live.CC025_FUNC_ID END AS CC025_FUNC_ID, ";
                strSQL += " CASE WHEN CC025_Live.CC025_NOTE_CLASS IS NULL THEN CC025_Snapshot.CC025_ORDER ELSE CC025_Live.CC025_ORDER END AS CC025_ORDER, ";
                strSQL += " CASE WHEN CC025_Live.CC025_NOTE_CLASS IS NULL THEN CC025_Snapshot.CC025_ORD_LINE ELSE CC025_Live.CC025_ORD_LINE END AS CC025_ORD_LINE, ";
                strSQL += " CC025_Live.CC025_NOTE_TEXT as CC025_Live_NOTE_TEXT, CC025_Live.CC025_HTML_TEXT as CC025_Live_HTML_TEXT, ";
                strSQL += " CC025_Snapshot.CC025_NOTE_TEXT as CC025_Snapshot_NOTE_TEXT,CC025_Snapshot.CC025_HTML_TEXT as CC025_Snapshot_HTML_TEXT ";
                strSQL += " FROM  ";
                strSQL += " (SELECT CC025_EVT_ID,CC025_FUNC_ID,CC025_ORDER, CC025_ORD_LINE,CC025_NOTE_TYPE,CC025_NOTE_HDR_SEQ, CC025_NOTE_CODE, CC025_NOTE_DESC,CC025_NOTE_PRT_CODE,CC025_NOTE_TEXT,CC025_NOTE_CLASS,CC025_UPD_DATE,CC025_UPD_USER_ID,CC025_HTML_TEXT,CC025_ENT_DATE,CC025_ENT_USER_ID FROM CC025_NOTES_EXT_Curr WHERE CC025_EVT_ID=@eventid AND CC025_NOTE_TYPE ='OD' and CC025_ORDER=@ordernumber) as CC025_Live ";
                strSQL += " FULL OUTER JOIN ";
                strSQL += " (SELECT CC025_EVT_ID,CC025_FUNC_ID,CC025_ORDER, CC025_ORD_LINE,CC025_NOTE_TYPE,CC025_NOTE_HDR_SEQ, CC025_NOTE_CODE,CC025_NOTE_DESC,CC025_NOTE_PRT_CODE,CC025_NOTE_TEXT,CC025_NOTE_CLASS,CC025_UPD_DATE,CC025_UPD_USER_ID,CC025_HTML_TEXT,CC025_ENT_DATE,CC025_ENT_USER_ID FROM CC025_NOTES_EXT_Prev WHERE CC025_EVT_ID=@eventid AND CC025_NOTE_TYPE ='OD' and CC025_ORDER=@ordernumber) as CC025_Snapshot ";
                strSQL += " ON CC025_Live.CC025_NOTE_TYPE = CC025_Snapshot.CC025_NOTE_TYPE AND CC025_Live.CC025_NOTE_CODE = CC025_Snapshot.CC025_NOTE_CODE AND CC025_Live.CC025_NOTE_HDR_SEQ = CC025_Snapshot.CC025_NOTE_HDR_SEQ ";
                strSQL += " WHERE (CC025_Live.CC025_NOTE_CODE is null or CC025_Snapshot.CC025_NOTE_CODE is null OR CC025_Live.CC025_NOTE_TEXT<>CC025_Snapshot.CC025_NOTE_TEXT) ";
                strSQL += " ) AS NOTE_DIFF ";
                strSQL += " INNER JOIN AMP_Noti_NoteClassDep on Note_Class=CC025_NOTE_CLASS and Noti_Dept_Code=@deptcode ";
                strSQL += " INNER JOIN (SELECT ER101_ORD_NBR,ER101_ORD_LINE,ER101_DESC,ER101_RES_QTY,ER101_START_DATE_ISO,ER101_START_TIME_ISO,ER101_END_DATE_ISO,ER101_END_TIME_ISO,ER101_NEW_RES_TYPE,ER101_RES_CODE,ER101_UOM,ER101_PHASE FROM ER101_ACCT_ORDER_DTL_Curr where ER101_EVT_ID=@eventid) AS Order_Current  ";
                strSQL += " ON Order_Current.ER101_ORD_NBR = CC025_ORDER and Order_Current.ER101_ORD_LINE=CC025_ORD_LINE ";                

                SqlCommand comm = new SqlCommand(strSQL, conn);
                comm.Parameters.Add("@eventid", SqlDbType.Int).Value = evt.EventId;
                comm.Parameters.Add("@deptcode", SqlDbType.VarChar, 20).Value = dep.DepartmentCode;
                comm.Parameters.Add("@ordernumber", SqlDbType.Int).Value = oinfo.Order_Number;
                comm.CommandTimeout = nCommandTimeOut;

                SqlDataAdapter da = new SqlDataAdapter(comm);
                DataTable dt = new DataTable();
                da.Fill(dt);
                da.FillSchema(dt, SchemaType.Source);

                if (dt.Rows.Count > 0)
                {          
                    
                    if (oinfo.Order_Number == 899032)
                    {
                        string strA = "test";
                    }
                    eMsg.OrderUpdated = true;

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
                            eMsg.MSGText += "New Order Item Notes (" + dr["CC025_NOTE_DESC"].ToString() + ") entered/updated by " + AMP_Common.GetUserName(dr["CC025_UPD_USER_ID"].ToString()) + " on " + dtUpdDate.ToString("dd/MM/yyyy hh:mm") + " for order item: " + dr["ER101_DESC"].ToString() + ": \r\n";
                            eMsg.MSGHTML += "<DIV style='font:10pt arial;'><p style='margin: 0 0 10px 0;text-align: left;'><span style='font: bold 10pt arial; padding-left:50px;'>New order item notes (" + dr["CC025_NOTE_DESC"].ToString() + ")</span> entered/updated by " + AMP_Common.GetUserName(dr["CC025_UPD_USER_ID"].ToString()) + " on " + dtUpdDate.ToString("dd/MM/yyyy hh:mm") + " for: " + dr["ER101_DESC"].ToString() + ": </p>";
                            eMsg.MSGHTML += "<ul style='MARGIN:0 0 10px 40px;LIST-STYLE-TYPE:disc;'><li style=font:10pt arial;'><p style='MARGIN:0 0 10px 0;TEXT-ALIGN:left;'>";
                            if (dr["CC025_Live_NOTE_TEXT"].ToString().Length <= rule.NotesLength )
                            {
                                eMsg.MSGText += dr["CC025_Live_NOTE_TEXT"].ToString() + "\r\n";
                                eMsg.MSGHTML += dr["CC025_Live_HTML_TEXT"].ToString();
                            }
                            else
                            {
                                eMsg.MSGText += dr["CC025_Live_NOTE_TEXT"].ToString().Substring(0, rule.NotesLength - 3) + " ...\r\n";
                                eMsg.MSGHTML += dr["CC025_Live_HTML_TEXT"].ToString().Substring(0, rule.NotesLength - 3) + " ...";
                            }
                            eMsg.MSGHTML += "<br></p></li></ul></DIV>";
                        }
                        //deleted notes
                        else if (dr["CC025_Live_NOTE_TEXT"] == DBNull.Value)
                        {
                            eMsg.MSGText += "Deleted Order Item " + dr["CC025_NOTE_DESC"].ToString() + " Notes for order item: " + dr["ER101_DESC"].ToString() + ": \r\n";
                            eMsg.MSGHTML += "<DIV style='font:10pt arial;'><p style='margin: 0 0 10px 0;text-align: left;'><span style='font: bold 10pt arial;padding-left:50px;'>Deleted order item notes (" + dr["CC025_NOTE_DESC"].ToString() + ")</span> for: " + dr["ER101_DESC"].ToString() + ": </p>";
                            eMsg.MSGHTML += "<ul style='MARGIN:0 0 10px 40px;LIST-STYLE-TYPE:disc;'><li style=font:10pt arial;'><p style='MARGIN:0 0 10px 0;TEXT-ALIGN:left;'>";
                            if (dr["CC025_Snapshot_NOTE_TEXT"].ToString().Length <= rule.NotesLength)
                            {
                                eMsg.MSGText += dr["CC025_Snapshot_NOTE_TEXT"].ToString() + "\r\n";
                                eMsg.MSGHTML += dr["CC025_Snapshot_HTML_TEXT"].ToString();
                            }
                            else
                            {
                                eMsg.MSGText += dr["CC025_Snapshot_NOTE_TEXT"].ToString().Substring(0, rule.NotesLength - 3) + " ...\r\n";
                                eMsg.MSGHTML += dr["CC025_Snapshot_HTML_TEXT"].ToString().Substring(0, rule.NotesLength - 3) + " ...";
                            }
                            eMsg.MSGHTML += "<br></p></li></ul></DIV>";
                        }
                        else
                        {
                            eMsg.MSGText += "Order item " + dr["CC025_NOTE_DESC"].ToString() + " notes updated by " + AMP_Common.GetUserName(dr["CC025_UPD_USER_ID"].ToString()) + " on " + dtUpdDate.ToString("dd/MM/yyyy hh:mm") + " for: " + dr["ER101_DESC"].ToString() + ": \r\n";
                            eMsg.MSGHTML += "<DIV style='font:10pt arial;'><p style='margin: 0 0 10px 0;text-align: left;'><span style='font: bold 10pt arial;padding-left:50px;'>Order item notes (" + dr["CC025_NOTE_DESC"].ToString() + ") updated</span> by " + AMP_Common.GetUserName(dr["CC025_UPD_USER_ID"].ToString()) + " on " + dtUpdDate.ToString("dd/MM/yyyy hh:mm") + " for: " + dr["ER101_DESC"].ToString() + "</p>";
                            eMsg.MSGHTML += "<ul style='MARGIN:0 0 10px 40px;LIST-STYLE-TYPE:disc;'><li style=font:10pt arial;'><p style='MARGIN:0 0 10px 0;TEXT-ALIGN:left;'>";
                            if (dr["CC025_Live_NOTE_TEXT"] != DBNull.Value && dr["CC025_Snapshot_NOTE_TEXT"] != DBNull.Value)
                            {
                                if (dr["CC025_Live_NOTE_TEXT"].ToString().Length <= rule.NotesLength)
                                {
                                    eMsg.MSGText += "Update To : " + dr["CC025_Live_NOTE_TEXT"].ToString() + "\r\n";
                                    eMsg.MSGHTML += "Update to :" + dr["CC025_Live_HTML_TEXT"].ToString() + "<br/>";
                                }
                                else
                                {
                                    eMsg.MSGText += "Update to:" + dr["CC025_Live_NOTE_TEXT"].ToString().Substring(0, rule.NotesLength - 3) + " ...\r\n";
                                    eMsg.MSGHTML += "Update To :" + dr["CC025_Live_HTML_TEXT"].ToString().Substring(0, rule.NotesLength - 3) + " ...< br/>";
                                }

                                if (dr["CC025_Snapshot_NOTE_TEXT"].ToString().Length <= rule.NotesLength)
                                {
                                    eMsg.MSGText += " from:" + dr["CC025_Snapshot_NOTE_TEXT"].ToString() + "\r\n";
                                    eMsg.MSGHTML += " from:" + dr["CC025_Snapshot_HTML_TEXT"].ToString() + "<br/>";
                                }
                                else
                                {
                                    eMsg.MSGText += "from : " + dr["CC025_Snapshot_NOTE_TEXT"].ToString().Substring(0, rule.NotesLength - 3) + " ...r\n";
                                    eMsg.MSGHTML += "from:" + dr["CC025_Snapshot_HTML_TEXT"].ToString().Substring(0, rule.NotesLength - 3) + " ...<br/>";
                                }
                                eMsg.MSGHTML += "<br></p></li></ul></DIV>";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AMP_Common.sendErrorException(strEmailFrom, strEmailTo, "Amendment Runtime Error - Order", rule, evt, finfo, dep, nSnapshotPreviousID, nSnapshotCurrentID, System.Reflection.MethodBase.GetCurrentMethod().Name, "Order Number:" + oinfo.Order_Number + "<br/>" + ex.Message);
            }
            finally
            {
                conn.Close();
            }
        }

        public static List<Order_Info> getDeletedOrders()
        {
            List<Order_Info> lstOrders = new List<Order_Info>();

            SqlConnection conn = new SqlConnection(strEBMSConn);

            try
            {
                conn.Open();

                string strSQL = "SELECT distinct Order_Prev.ER100_ORD_NBR from ER100_ACCT_ORDER_Curr Order_Current right join ER100_ACCT_ORDER_Prev Order_Prev ";
                strSQL += " on Order_Current.[ER100_EVT_ID]=Order_Prev.[ER100_EVT_ID] and Order_Current.[ER100_FUNC_ID]=Order_Prev.[ER100_FUNC_ID] ";
                strSQL += " and Order_Current.ER100_ORD_NBR=Order_Prev.ER100_ORD_NBR  ";
                strSQL += " where Order_Prev.ER100_EVT_ID=@eventid and Order_Prev.ER100_FUNC_ID=@funcid and Order_Current.ER100_ORD_NBR is null";

                SqlCommand comm = new SqlCommand(strSQL, conn);
                comm.Parameters.Add("@eventid", SqlDbType.Int).Value = evt.EventId;
                comm.Parameters.Add("@funcid", SqlDbType.Int).Value = finfo.FuncId;
                comm.CommandTimeout = nCommandTimeOut;

                SqlDataAdapter da = new SqlDataAdapter(comm);
                DataTable dt = new DataTable();
                da.Fill(dt);
                da.FillSchema(dt, SchemaType.Source);

                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        int nOrderNumber;
                        if (int.TryParse(dr["ER100_ORD_NBR"].ToString(), out nOrderNumber)) nOrderNumber = int.Parse(dr["ER100_ORD_NBR"].ToString());

                        Order_Info order = getOrderInformation(nOrderNumber,nSnapshotPreviousID);
                        order.ChangeType = "Delete";
                        lstOrders.Add(order);
                    }
                }
            }
            catch (Exception ex)
            {
                AMP_Common.sendErrorException(strEmailFrom, strEmailTo, "Amendment Runtime Error - Order", rule, evt, finfo, dep, nSnapshotPreviousID, nSnapshotCurrentID, System.Reflection.MethodBase.GetCurrentMethod().Name, "<br/>" + ex.Message);
            }
            finally
            {
                conn.Close();
            }

            return lstOrders;
        }

        public static void checkDeltedOrderItems()
        {
            SqlConnection conn = new SqlConnection(strEBMSConn);

            try
            {
                conn.Open();
                //Get updated order data 
                string strSQL = " SELECT STUFF((SELECT CAST(MSG as VARCHAR(4000)) from  ";
                strSQL += " (SELECT ER101_NEW_RES_TYPE,ER101_RES_CODE, 'Item Deleted. ' + ER101_DESC + '('+  CAST(ER101_ORD_NBR AS VARCHAR) + ': '+ CAST(ER101_ORD_LINE AS VARCHAR)+') Qty.: ' +  CAST(ER101_RES_QTY AS VARCHAR) + ' ' + CAST(ER101_UOM AS VARCHAR) + ', ' + convert(varchar, CAST(ER101_START_DATE_ISO AS DATE), 103) + ' ' + convert(varchar, CAST(ER101_START_TIME_ISO AS time), 108) + ' - ' + convert(varchar, CAST(ER101_END_DATE_ISO AS DATE), 103) + ' ' + convert(varchar, CAST(ER101_END_TIME_ISO AS TIME), 108) + CHAR(13)+CHAR(10) AS MSG  FROM ER101_ACCT_ORDER_DTL_Prev where ER101_EVT_ID=@eventid and ER101_FUNC_ID=@funcid and ER101_ORD_NBR=@ordernumber and ER101_PHASE='1') as taborder  ";
                strSQL += " 	INNER JOIN AMP_Noti_ResDep ResChangeDep on taborder.ER101_NEW_RES_TYPE=ResChangeDep.New_Res_Type and taborder.ER101_RES_CODE=ResChangeDep.Res_Code   ";
                strSQL += " 	where ResChangeDep.Noti_Dep_Code=@deptcode for xml path('')),1,0,'') as MSGText,  ";
                strSQL += " STUFF((SELECT CAST(MSG as VARCHAR(8000)) from  ";
                strSQL += " (SELECT ER101_NEW_RES_TYPE,ER101_RES_CODE, ";
                strSQL += " 		'<tr style=#39;display:table-row;background:#f6f6f6;#39;><td>' +   ";
                strSQL += " 		CASE WHEN ER101_ORD_LINE = ER101_LIN_NBR THEN ' - <strike>' + ER101_DESC + '</strike>'  ";
                strSQL += " 			ELSE ' - <strike><small>' + ER101_DESC + '</small></strike>'  ";
                strSQL += " 		END	+ '</td><td>' +  ";
                strSQL += " 		CASE WHEN ER101_ORD_LINE = ER101_LIN_NBR THEN '<strike>' + CAST(ER101_RES_QTY AS VARCHAR) + '</strike>'  ";
                strSQL += " 			ELSE '<small><strike>' + CAST(ER101_RES_QTY AS VARCHAR) + '</strike></small>'  ";
                strSQL += " 		END + '</td><td>' +  ";
                strSQL += " 		CASE WHEN ER101_ORD_LINE = ER101_LIN_NBR THEN '<strike>' + CAST(ER101_UOM AS VARCHAR) + '</strike>'  ";
                strSQL += " 			ELSE '<small><strike>' + CAST(ER101_UOM AS VARCHAR) + '</strike></small>'  ";
                strSQL += " 		END+ '</td><td>' +  ";
                strSQL += " 		CASE WHEN ER101_ORD_LINE = ER101_LIN_NBR THEN '<strike>' + convert(varchar, ER101_START_DATE_ISO , 103) + '</strike>'  ";
                strSQL += " 		 ELSE ''  ";
                strSQL += " 	  END   + '</td><td>' +   ";
                strSQL += " 	  CASE WHEN ER101_ORD_LINE = ER101_LIN_NBR THEN '<strike>' + convert(varchar(5), ER101_START_TIME_ISO , 108) + '</strike>'  ";
                strSQL += " 		 ELSE ''  ";
                strSQL += " 	  END + '</td><td>' +   ";
                strSQL += " 	  CASE WHEN ER101_ORD_LINE = ER101_LIN_NBR THEN '<strike>' + convert(varchar, ER101_END_DATE_ISO, 103) + '</strike>'  ";
                strSQL += " 		 ELSE ''  ";
                strSQL += " 	  END  + '</td><td>' +   ";
                strSQL += " 	  CASE WHEN ER101_ORD_LINE = ER101_LIN_NBR THEN '<strike>' + convert(varchar(5), ER101_END_TIME_ISO , 108) + '</strike>'  ";
                strSQL += " 		 ELSE ''  ";
                strSQL += "       END ";
                strSQL += "    AS MSG  FROM ER101_ACCT_ORDER_DTL_Prev where ER101_EVT_ID=@eventid and ER101_FUNC_ID=@funcid and ER101_ORD_NBR=@ordernumber and ER101_PHASE='1') as taborder  ";
                strSQL += " INNER JOIN AMP_Noti_ResDep ResChangeDep on taborder.ER101_NEW_RES_TYPE=ResChangeDep.New_Res_Type and taborder.ER101_RES_CODE=ResChangeDep.Res_Code   ";
                strSQL += " where ResChangeDep.Noti_Dep_Code=@deptcode for xml path('')),1,0,'') as MSGTextHTML ";

                SqlCommand comm = new SqlCommand(strSQL, conn);
                comm.Parameters.Add("@eventid", SqlDbType.Int).Value = evt.EventId;
                comm.Parameters.Add("@funcid", SqlDbType.Int).Value = finfo.FuncId;
                comm.Parameters.Add("@ordernumber", SqlDbType.Int).Value = oinfo.Order_Number;
                comm.Parameters.Add("@deptcode", SqlDbType.VarChar, 20).Value = dep.DepartmentCode;
                comm.CommandTimeout = nCommandTimeOut;

                SqlDataAdapter da = new SqlDataAdapter(comm);
                DataTable dt = new DataTable();
                da.Fill(dt);
                da.FillSchema(dt, SchemaType.Source);

                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        if (!string.IsNullOrEmpty(dr["MSGText"].ToString()))
                        {
                            eMsg.OrderUpdated = true;

                            eMsg.MSGText += "Deleted Order Items: \r\n" + dr["MSGText"].ToString();
                            eMsg.MSGHTML += "<DIV style='font:10pt arial;'>";
                            eMsg.MSGHTML += "<p style='margin: 0 0 10px 0;text-align: left;'><span style='font: bold 10pt arial;padding-left:45px;'>Deleted Order:</span> " + oinfo.Order_Number + "</p>";
                            eMsg.MSGHTML += "<p style='margin: 0 0 10px 0;text-align: left;'><span style='font: bold 10pt arial;padding-left:50px;'>Deleted Order Items:</span></p>";
                            eMsg.MSGHTML += " <table style='margin: 0 0 40px 0; width: 100 %; box - shadow: 0 1px 3px rgba(0, 0, 0, 0.2); display: table; '><tr style='font - weight: 9; color: #ffffff;background: #ea6153;'><td>Description</td><td>Units</td><td>U/M</td><td>Start Date</td><td>Start Time</td><td>End Date</td><td>End Time</td></tr>" + dr["MSGTextHTML"].ToString() + "</table></div>";
                            eMsg.MSGHTML = eMsg.MSGHTML.Replace("&lt;", "<").Replace("&gt;", ">").Replace("#39;", "'");

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AMP_Common.sendErrorException(strEmailFrom, strEmailTo, "Amendment Runtime Error - Order", rule, evt, finfo, dep, nSnapshotPreviousID, nSnapshotCurrentID, System.Reflection.MethodBase.GetCurrentMethod().Name, "Order Number:" + oinfo.Order_Number + "<br/>" + ex.Message);
            }
            finally
            {
                conn.Close();
            }            
        }
    }
}
