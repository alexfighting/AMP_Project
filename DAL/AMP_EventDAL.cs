using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Model;
using System.Data.SqlClient;
using System.Data;

namespace DAL
{
    public class AMP_EventDAL
    {
        public static string strEBMSConn = Properties.Settings.Default.strEBMSConn;
        public static string strCompDatabase = Properties.Settings.Default.strCompDatabase;
        public static string strEmailFrom = Properties.Settings.Default.Amendment_Error_EmailAddressFrom;
        public static string strEmailTo = Properties.Settings.Default.Amendment_Error_EmailAddressTo;
        public static int nCommandTimeOut = Properties.Settings.Default.nCommandTimeOut;

        public static AMP_Rules rule;

        public static Notification_Dep_user dep;

        public static EventInfo evt;

        public static EventAccountMessage eMsg;

        public static DateTime dtSnapshotCurrent, dtSnapshotPrevious;

        public static int nSnapshotCurrentID, nSnapshotPreviousID;


        /// <summary>
        /// get event information from event id
        /// </summary>
        /// <param name="strEventId"></param>
        /// <returns></returns>
        public static EventInfo getEventInfo(int nEventId)
        {
            EventInfo evt = new EventInfo();

            if (!string.IsNullOrEmpty(nEventId.ToString()))
            {

                SqlConnection conn = new SqlConnection(strEBMSConn);

                try
                {
                    conn.Open();

                    string strSQL = " SELECT EV200_EVENT_MASTER.EV200_EVT_DESC,EV200_EVT_Status, EV200_EVENT_MASTER.EV200_EVT_ID, CAST(CAST(EV200_EVENT_MASTER.EV200_EVT_IN_DATE AS DATE) AS DATETIME) + CAST(CAST(EV200_EVENT_MASTER.EV200_EVT_IN_TIME AS DATE) AS DATETIME) AS EVT_IN_DATETIME, ";
                    strSQL += " CAST(CAST(EV200_EVENT_MASTER.EV200_EVT_OUT_DATE AS DATE) AS DATETIME) + CAST(CAST(EV200_EVENT_MASTER.EV200_EVT_OUT_TIME AS DATE) AS DATETIME) AS EVT_OUT_DATETIME, ";
                    strSQL += " CAST(CAST(EV200_EVENT_MASTER.EV200_EVT_START_DATE AS DATE) AS DATETIME) + CAST(CAST(EV200_EVENT_MASTER.EV200_EVT_START_TIME AS DATE) AS DATETIME) AS EVT_START_DATETIME, ";
                    strSQL += " CAST(CAST(EV200_EVENT_MASTER.EV200_EVT_END_DATE AS DATE) AS DATETIME) + CAST(CAST(EV200_EVENT_MASTER.EV200_EVT_END_TIME AS DATE) AS DATETIME) AS EVT_END_DATETIME, ";
                    strSQL += " EV870_ACCT_MASTER_SLSP.EV870_NAME AS SLSPNAME, EV870_ACCT_MASTER_EP.EV870_NAME AS EPNAME, ";
                    strSQL += " EV870_ACCT_MASTER_TP.EV870_NAME AS TPNAME, EV870_ACCT_MASTER_OM.EV870_NAME AS OMNAME ";
                    strSQL += " FROM  (((EV200_EVENT_MASTER EV200_EVENT_MASTER LEFT OUTER JOIN " + strCompDatabase + ".dbo.EV870_ACCT_MASTER EV870_ACCT_MASTER_SLSP ON (EV200_EVENT_MASTER.EV200_ORG_CODE=EV870_ACCT_MASTER_SLSP.EV870_ORG_CODE) AND (EV200_EVENT_MASTER.EV200_SLSPER=EV870_ACCT_MASTER_SLSP.EV870_ACCT_CODE)) ";
                    strSQL += " LEFT OUTER JOIN " + strCompDatabase + ".dbo.EV870_ACCT_MASTER EV870_ACCT_MASTER_EP ON (EV200_EVENT_MASTER.EV200_ORG_CODE=EV870_ACCT_MASTER_EP.EV870_ORG_CODE) AND (EV200_EVENT_MASTER.EV200_COORD_1=EV870_ACCT_MASTER_EP.EV870_ACCT_CODE)) ";
                    strSQL += " LEFT OUTER JOIN " + strCompDatabase + ".dbo.EV870_ACCT_MASTER EV870_ACCT_MASTER_TP ON (EV200_EVENT_MASTER.EV200_ORG_CODE=EV870_ACCT_MASTER_TP.EV870_ORG_CODE) AND (EV200_EVENT_MASTER.EV200_COORD_2=EV870_ACCT_MASTER_TP.EV870_ACCT_CODE)) ";
                    strSQL += " LEFT OUTER JOIN " + strCompDatabase + ".dbo.EV870_ACCT_MASTER EV870_ACCT_MASTER_OM ON (EV200_EVENT_MASTER.EV200_ORG_CODE=EV870_ACCT_MASTER_OM.EV870_ORG_CODE) AND (EV200_EVENT_MASTER.EV200_EVT_COORD4=EV870_ACCT_MASTER_OM.EV870_ACCT_CODE) ";

                    strSQL += " WHERE EV200_EVT_ID=@eventid AND  EV200_EVENT_MASTER.EV200_SNAPSHOT_ID=@nsnapshotcurrentid";

                    SqlCommand comm = new SqlCommand(strSQL, conn);
                    comm.Parameters.Add("@nsnapshotcurrentid", SqlDbType.Int).Value = nSnapshotCurrentID;
                    comm.Parameters.Add("@eventid", SqlDbType.Int).Value = nEventId;

                    SqlDataReader dr = comm.ExecuteReader();

                    if (dr.Read() && dr.HasRows)
                    {
                        evt.EventId = nEventId;
                        evt.EventDesc = dr["EV200_EVT_DESC"].ToString();
                        evt.StartDate = dr["EVT_START_DATETIME"] == DBNull.Value ? DateTime.MinValue : DateTime.Parse(dr["EVT_START_DATETIME"].ToString());
                        evt.EndDate = dr["EVT_END_DATETIME"] == DBNull.Value ? DateTime.MinValue : DateTime.Parse(dr["EVT_END_DATETIME"].ToString());
                        evt.InDate = dr["EVT_IN_DATETIME"] == DBNull.Value ? DateTime.MinValue : DateTime.Parse(dr["EVT_IN_DATETIME"].ToString());
                        evt.OutDate = dr["EVT_OUT_DATETIME"] == DBNull.Value ? DateTime.MinValue : DateTime.Parse(dr["EVT_OUT_DATETIME"].ToString());
                        evt.SalesPerson = dr["SLSPNAME"].ToString();
                        evt.EventPlanner = dr["EPNAME"].ToString();
                        evt.TechPlanner = dr["TPNAME"].ToString();
                        evt.OperationManager = dr["OMNAME"].ToString();
                        evt.AccountNo = AMP_Common.GetAcctCode(nEventId);
                        evt.Status = dr["EV200_EVT_Status"].ToString();
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
            }

            return evt;
        }

        public static string GetEventDesc(int nEventId)
        {
            string strEventDesc = "";
            SqlConnection conn = new SqlConnection(strEBMSConn);

            try
            {
                conn.Open();

                string strSQL = " select EV200_EVT_DESC from " + strCompDatabase + ".dbo.EV200_EVENT_MASTER where EV200_EVT_ID=@eventid  ";
                SqlCommand comm = new SqlCommand(strSQL, conn);
                comm.Parameters.Add("@eventid", SqlDbType.Int).Value = nEventId;

                SqlDataReader dr = comm.ExecuteReader();
                if (dr.Read() && dr.HasRows)
                {
                    strEventDesc = dr["EV200_EVT_DESC"].ToString();
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

            return strEventDesc;
        }


        private static string GetEvtStatusDesc(string strStatusCode)
        {
            string strStatusDesc = "";
            SqlConnection conn = new SqlConnection(strEBMSConn);

            try
            {
                conn.Open();

                string strSQL = " select distinct EV130_STATUS_DESC from " + strCompDatabase + ".dbo.EV130_STATUS_MASTER where EV130_EVT_FUNC_EFB in ('E','B') AND EV130_STATUS_CODE=@statuscode  ";
                SqlCommand comm = new SqlCommand(strSQL, conn);
                comm.Parameters.Add("@statuscode", SqlDbType.VarChar, 2).Value = strStatusCode;

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

        /// <summary>
        /// get all event information from rule
        /// </summary>
        /// <returns></returns>
        public static List<EventInfo> getAllEventsFromRule()
        {
            List<EventInfo> lstEvents = new List<EventInfo>();

            DataTable dtEvents = new DataTable();
            SqlConnection conn = new SqlConnection(strEBMSConn);

            try
            {
                conn.Open();

                if (rule != null)
                {
                    if (rule.RuleId != null)
                    {
                        string strSQL = "select distinct EV200_EVT_ID from " + strCompDatabase + ".dbo.EV200_EVENT_MASTER ";

                        strSQL += " where 1=1 ";

                        if (rule.Notify_EventDay_From >= 0 && rule.Notify_EventDay_To > 0)
                        {
                            strSQL += " and CAST(DATEADD(d,@notifyeventto, CAST(@snapshotcurrent as date)) AS DATE) >= CAST(EV200_EVT_IN_DATE AS DATE) AND CAST(DATEADD(D, @notifyeventfrom, CAST(@snapshotcurrent as date)) AS DATE) <= CAST(EV200_EVT_OUT_DATE AS DATE) ";
                        }
                        if (!string.IsNullOrEmpty(rule.EventStatusFrom) && !string.IsNullOrEmpty(rule.EventStatusTo))
                        {
                            strSQL += " and EV200_EVT_Status between @eventstatusfrom and @eventstatusto ";
                        }
                        if (!string.IsNullOrEmpty(rule.EventStatusList))
                        {
                            strSQL += " and EV200_EVT_Status in (" + rule.EventStatusList + ") ";
                        }

                        SqlCommand comm = new SqlCommand(strSQL, conn);

                        if (rule.Notify_EventDay_From >= 0 && rule.Notify_EventDay_To > 0)
                        {
                            comm.Parameters.Add("@snapshotcurrent", SqlDbType.Date).Value = dtSnapshotCurrent;
                            comm.Parameters.Add("@notifyeventto", SqlDbType.Int).Value = rule.Notify_EventDay_To;
                            comm.Parameters.Add("@notifyeventfrom", SqlDbType.Int).Value = rule.Notify_EventDay_From;
                        }
                        if (!string.IsNullOrEmpty(rule.EventStatusFrom) && !string.IsNullOrEmpty(rule.EventStatusTo))
                        {
                            comm.Parameters.Add("@eventstatusfrom", SqlDbType.VarChar, 3).Value = rule.EventStatusFrom;
                            comm.Parameters.Add("@eventstatusto", SqlDbType.VarChar, 3).Value = rule.EventStatusTo;
                        }
                        comm.CommandTimeout = nCommandTimeOut;

                        SqlDataAdapter da1 = new SqlDataAdapter(comm);

                        da1.Fill(dtEvents);
                        da1.FillSchema(dtEvents, SchemaType.Source);

                        if (dtEvents.Rows.Count > 0)
                        {
                            foreach (DataRow dr in dtEvents.Rows)
                            {
                                if (!string.IsNullOrEmpty(dr["EV200_EVT_ID"].ToString()))
                                {
                                    string strEventId = dr["EV200_EVT_ID"].ToString();

                                    int nEventId = 0;

                                    if (int.TryParse(strEventId, out nEventId))
                                    {
                                        nEventId = int.Parse(strEventId);
                                    }

                                    EventInfo evt = new EventInfo();

                                    evt = getEventInfo(nEventId);
                                    lstEvents.Add(evt);
                                }
                            }
                        }
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

            return lstEvents;
        }

        public static bool checkEventCancelled()
        {
            eMsg = new EventAccountMessage();
            bool isCancelled = false;

            SqlConnection conn = new SqlConnection(strEBMSConn);

            try
            {
                conn.Open();

                //get the current event under the rule for New Event data.
                string strSQL = "select Event_Live.EV200_EVT_ID AS EV200_EVT_ID,Event_Live.EV200_EVT_STATUS as StatusEnd,Event_Snapshot.EV200_EVT_STATUS as StatusStart   From ";
                strSQL += " (select EV200_ORG_CODE, EV200_EVT_ID, EV200_EVT_DESC, EV200_EVT_STATUS from EV200_EVENT_MASTER where EV200_SNAPSHOT_ID=@nsnapshotcurrentid and EV200_EVT_ID=@eventid) As Event_Live INNER JOIN ";
                strSQL += " (select EV200_ORG_CODE, EV200_EVT_ID, EV200_EVT_DESC, EV200_EVT_STATUS from EV200_EVENT_MASTER where EV200_SNAPSHOT_ID=@nsnapshotpreviousid and EV200_EVT_ID=@eventid) As Event_Snapshot ";
                strSQL += " on Event_Snapshot.EV200_ORG_CODE=Event_Live.EV200_ORG_CODE and Event_Snapshot.EV200_EVT_ID = Event_Live.EV200_EVT_ID ";
                strSQL += " where Event_Snapshot.EV200_EVT_STATUS<> Event_Live.EV200_EVT_STATUS and  Event_Live.EV200_EVT_STATUS in ('80','86')  ";

                if (!string.IsNullOrEmpty(rule.ShortLeadStatusList))
                {
                    strSQL += " and Event_Snapshot.Ev200_EVT_STATUS in (" + rule.ShortLeadStatusList + ") ";
                }

                SqlCommand comm = new SqlCommand(strSQL, conn);
                comm.Parameters.Add("@nsnapshotcurrentid", SqlDbType.Int).Value = nSnapshotCurrentID;
                comm.Parameters.Add("@nsnapshotpreviousid", SqlDbType.Int).Value = nSnapshotPreviousID;
                comm.Parameters.Add("@eventid", SqlDbType.Int).Value = evt.EventId;
                comm.CommandTimeout = nCommandTimeOut;

                SqlDataAdapter da = new SqlDataAdapter(comm);
                DataTable dtCancelEvent = new DataTable();
                da.Fill(dtCancelEvent);
                da.FillSchema(dtCancelEvent, SchemaType.Source);

                if (dtCancelEvent.Rows.Count > 0)
                {
                    isCancelled = true;

                    rule.EmailSubject = "Released Event Cancelled " + evt.EventDesc + " (" + evt.EventId.ToString("G") + ")";

                    eMsg.EventId = evt.EventId;
                    eMsg.AcctCode = evt.AccountNo;
                    eMsg.MessageType = "ACE";

                    eMsg.MSGText = "Released Event Cancelled " + evt.EventDesc + " (" + evt.EventId + ") \r\n";
                    eMsg.MSGText += "Start/End Date: " + evt.StartDate.ToString("dd/MM/yyyy hh:mm") + " - " + evt.EndDate.ToString("dd/MM/yyyy hh:mm") + "\r\n";
                    eMsg.MSGText += "In/Out Date: " + evt.InDate.ToString("dd/MM/yyyy hh:mm") + " - " + evt.OutDate.ToString("dd/MM/yyyy hh:mm") + "\r\n";
                    eMsg.MSGText += "Sales Person: " + evt.SalesPerson + "\r\n";
                    eMsg.MSGText += "Event Planner: " + evt.EventPlanner + "\r\n";
                    eMsg.MSGText += "Technology Planner: " + evt.TechPlanner + "\r\n";
                    eMsg.MSGText += "Operation Manager: " + evt.OperationManager + "\r\n\r\n";

                    eMsg.MSGHTML = "<DIV style='font:10pt arial;'><p style='margin: 0 0 10px 0;text-align: left;'><span style='font: bold 10pt arial;color:red'>Released Event Cancellled </span>&nbsp;<br/>" + evt.EventDesc + " (" + evt.EventId + ")" + " </p>";
                    eMsg.MSGHTML += "<p style='margin: 0 0 10px 0;text-align: left;'><span>Start/End Date:</span> " + evt.StartDate.ToString("dd/MM/yyyy hh:mm") + " - " + evt.EndDate.ToString("dd/MM/yyyy hh:mm") + "</p><p style='margin: 0 0 10px 0;text-align: left;'> ";
                    eMsg.MSGHTML += "<span>In/Out Date:</span> " + evt.InDate.ToString("dd/MM/yyyy hh:mm") + " - " + evt.OutDate.ToString("dd/MM/yyyy hh:mm") + "</p><p style='margin: 0 0 10px 0;text-align: left;'> ";
                    eMsg.MSGHTML += "<span>Sales Person:</span> " + evt.SalesPerson + "</p><p style='margin: 0 0 10px 0;text-align: left;'> ";
                    eMsg.MSGHTML += "<span>Event Planner:</span> " + evt.EventPlanner + "</p><p style='margin: 0 0 10px 0;text-align: left;'> ";
                    eMsg.MSGHTML += "<span>Technology Planner:</span> " + evt.TechPlanner + "</p><p style='margin: 0 0 10px 0;text-align: left;'> ";
                    eMsg.MSGHTML += "<span>Operation Manager:</span> " + evt.OperationManager + "</p> ";
                    eMsg.MSGHTML += "</p></div>";

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

            return isCancelled;
        }

        public static bool checkEventNewOrShortLead()
        {
            eMsg = new EventAccountMessage();
            bool isShortLeaded = false;

            SqlConnection conn = new SqlConnection(strEBMSConn);

            try
            {
                conn.Open();

                //get the current event under the rule for New Event data.
                string strSQL = "select Event_Live.EV200_EVT_ID AS EV200_EVT_ID,Event_Live.EV200_EVT_STATUS as StatusEnd,Event_Snapshot.EV200_EVT_STATUS as StatusStart, Datediff(D, getdate(), Event_Live.EV200_EVT_IN_DATE) as nDays  From ";
                strSQL += " (select EV200_ORG_CODE, EV200_EVT_ID, EV200_EVT_DESC, EV200_EVT_STATUS,EV200_EVT_IN_DATE  from EV200_EVENT_MASTER where EV200_SNAPSHOT_ID=@nsnapshotcurrentid and EV200_EVT_ID=@eventid) As Event_Live LEFT JOIN ";
                strSQL += " (select EV200_ORG_CODE, EV200_EVT_ID, EV200_EVT_DESC, EV200_EVT_STATUS from EV200_EVENT_MASTER where EV200_SNAPSHOT_ID=@nsnapshotpreviousid and EV200_EVT_ID=@eventid) As Event_Snapshot ";
                strSQL += " on Event_Snapshot.EV200_ORG_CODE=Event_Live.EV200_ORG_CODE and Event_Snapshot.EV200_EVT_ID = Event_Live.EV200_EVT_ID ";
                strSQL += " where  Event_Snapshot.EV200_EVT_STATUS<>Event_Live.EV200_EVT_STATUS  ";
                if (!string.IsNullOrEmpty(rule.ShortLeadStatusList))
                {
                    strSQL += " and Event_Live.Ev200_EVT_STATUS in (" + rule.ShortLeadStatusList + ") ";
                }
                if (!string.IsNullOrEmpty(rule.ShortLeadStatusList))
                {
                    strSQL += " and Event_Snapshot.Ev200_EVT_STATUS not in (" + rule.ShortLeadStatusList + ") ";
                }
                strSQL += " or Event_Snapshot.Ev200_EVT_STATUS is null";


                SqlCommand comm = new SqlCommand(strSQL, conn);
                comm.Parameters.Add("@nsnapshotcurrentid", SqlDbType.Int).Value = nSnapshotCurrentID;
                comm.Parameters.Add("@nsnapshotpreviousid", SqlDbType.Int).Value = nSnapshotPreviousID;


                comm.Parameters.Add("@eventid", SqlDbType.Int).Value = evt.EventId;
                comm.CommandTimeout = nCommandTimeOut;

                SqlDataReader dr = comm.ExecuteReader();
                if (dr.Read())
                {
                    eMsg.EventId = evt.EventId;
                    eMsg.AcctCode = evt.AccountNo;

                    int nDays = 0;
                    nDays = int.Parse(dr["nDays"] == DBNull.Value ? "0" : dr["nDays"].ToString());

                    if (nDays <= 12)
                    {
                        rule.EmailSubject = "Short Lead Event Released " + evt.EventDesc + " (" + evt.EventId.ToString("G") + ")";
                        eMsg.MessageType = "ASR";
                        isShortLeaded = true;
                        eMsg.MSGText = "Short Lead Event Released " + evt.EventDesc + " (" + evt.EventId + ") \r\n";
                        eMsg.MSGHTML = "<DIV style='font:10pt arial;'><p style='margin: 0 0 10px 0;text-align: left;'><span style='font: bold 10pt arial;'>Short Lead Event Released </span>&nbsp;<br/>" + evt.EventDesc + " (" + evt.EventId + ")" + " </p>";
                    }
                    else
                    {
                        rule.EmailSubject = "New Event Released " + evt.EventDesc + " (" + evt.EventId.ToString("G") + ")";
                        eMsg.MessageType = "ANR";
                        isShortLeaded = true;
                        eMsg.MSGText = "New Event Released " + evt.EventDesc + " (" + evt.EventId + ") \r\n";
                        eMsg.MSGHTML = "<DIV style='font:10pt arial;'><p style='margin: 0 0 10px 0;text-align: left;'><span style='font: bold 10pt arial;'>New Event Released </span>&nbsp;<br/>" + evt.EventDesc + " (" + evt.EventId + ")" + " </p>";
                    }

                    eMsg.MSGText += "Start/End Date: " + evt.StartDate.ToString("dd/MM/yyyy hh:mm") + " - " + evt.EndDate.ToString("dd/MM/yyyy hh:mm") + "\r\n";
                    eMsg.MSGText += "In/Out Date: " + evt.InDate.ToString("dd/MM/yyyy hh:mm") + " - " + evt.OutDate.ToString("dd/MM/yyyy hh:mm") + "\r\n";
                    eMsg.MSGText += "Sales Person: " + evt.SalesPerson + "\r\n";
                    eMsg.MSGText += "Event Planner: " + evt.EventPlanner + "\r\n";
                    eMsg.MSGText += "Technology Planner: " + evt.TechPlanner + "\r\n";
                    eMsg.MSGText += "Operation Manager: " + evt.OperationManager + "\r\n\r\n";

                    eMsg.MSGHTML += "<p style='margin: 0 0 10px 0;text-align: left;'><span>Start/End Date:</span> " + evt.StartDate.ToString("dd/MM/yyyy hh:mm") + " - " + evt.EndDate.ToString("dd/MM/yyyy hh:mm") + "</p><p style='margin: 0 0 10px 0;text-align: left;'> ";
                    eMsg.MSGHTML += "<span>In/Out Date:</span> " + evt.InDate.ToString("dd/MM/yyyy hh:mm") + " - " + evt.OutDate.ToString("dd/MM/yyyy hh:mm") + "</p><p style='margin: 0 0 10px 0;text-align: left;'> ";
                    eMsg.MSGHTML += "<span>Sales Person:</span> " + evt.SalesPerson + "</p><p style='margin: 0 0 10px 0;text-align: left;'> ";
                    eMsg.MSGHTML += "<span>Event Planner:</span> " + evt.EventPlanner + "</p><p style='margin: 0 0 10px 0;text-align: left;'> ";
                    eMsg.MSGHTML += "<span>Technology Planner:</span> " + evt.TechPlanner + "</p><p style='margin: 0 0 10px 0;text-align: left;'> ";
                    eMsg.MSGHTML += "<span>Operation Manager:</span> " + evt.OperationManager + "</p></div>";

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

            return isShortLeaded;
        }

        public static void getEventAmendmentHead()
        {
            eMsg = new EventAccountMessage();

            string strMSGText = "Event Amendments \r\n Event: " + evt.EventDesc + " (" + evt.EventId + ") \r\n";

            string strMSGHTML = "<DIV style='font:10pt arial;'><p style='margin: 0 0 10px 0;text-align: left;'><span style='FONT-SIZE:11pt;FONT-WEIGHT:bold;COLOR:Red;'> Event Amendments</span></p><p style='MARGIN:0 0 10px 0;TEXT-ALIGN:left;'><span style='font: bold 10pt arial;'><u>" + evt.EventDesc + " (" + evt.EventId + ") </u></span> </p>";
            strMSGHTML += "</div>";

            rule.EmailSubject = "Event Amendment for event: " + evt.EventDesc + " (" + evt.EventId.ToString("G") + ")";

            eMsg.MessageType = "AMD";
            eMsg.EventId = evt.EventId;
            eMsg.AcctCode = evt.AccountNo;
            eMsg.MSGText = strMSGText;
            eMsg.MSGHTML = strMSGHTML;
        }

        public static void checkEventStatusChange()
        {
            SqlConnection conn = new SqlConnection(strEBMSConn);

            try
            {
                conn.Open();

                //get the current event under the rule for New Event data.
                string strSQL = "select Event_Live.EV200_EVT_ID AS EV200_EVT_ID,Event_Live.EV200_EVT_STATUS as StatusEnd,Event_Snapshot.EV200_EVT_STATUS as StatusStart   From ";
                strSQL += " (select EV200_ORG_CODE, EV200_EVT_ID, EV200_EVT_DESC, EV200_EVT_STATUS from EV200_EVENT_MASTER where EV200_SNAPSHOT_ID=@nsnapshotcurrentid AND EV200_EVT_ID=@eventid) As Event_Live INNER JOIN ";
                strSQL += " (select EV200_ORG_CODE, EV200_EVT_ID, EV200_EVT_DESC, EV200_EVT_STATUS from EV200_EVENT_MASTER where EV200_SNAPSHOT_ID=@nsnapshotpreviousid AND EV200_EVT_ID=@eventid) As Event_Snapshot ";
                strSQL += "  on Event_Snapshot.EV200_ORG_CODE=Event_Live.EV200_ORG_CODE and Event_Snapshot.EV200_EVT_ID = Event_Live.EV200_EVT_ID ";
                strSQL += " where  Event_Snapshot.EV200_EVT_STATUS<>Event_Live.EV200_EVT_STATUS ";

                if (!string.IsNullOrEmpty(rule.ShortLeadStatusList))
                {
                    strSQL += " and Event_Live.Ev200_EVT_STATUS in (" + rule.ShortLeadStatusList + ") ";
                }

                SqlCommand comm = new SqlCommand(strSQL, conn);
                comm.Parameters.Add("@nsnapshotcurrentid", SqlDbType.Int).Value = nSnapshotCurrentID;
                comm.Parameters.Add("@nsnapshotpreviousid", SqlDbType.Int).Value = nSnapshotPreviousID;
                comm.Parameters.Add("@eventid", SqlDbType.Int).Value = evt.EventId;
                comm.CommandTimeout = nCommandTimeOut;

                SqlDataReader drStatus = comm.ExecuteReader();

                if (drStatus.Read())
                {
                    string strEventStatus0 = drStatus["StatusStart"].ToString();
                    string strStatusDesc0 = GetEvtStatusDesc(strEventStatus0);
                    string strEventStatus1 = drStatus["StatusEnd"].ToString();
                    string strStatusDesc1 = GetEvtStatusDesc(strEventStatus1);
                    eMsg.EventUpdated = true;
                    eMsg.MSGText += "Event Status Change from " + strStatusDesc0 + " to " + strStatusDesc1 + " \r\n";
                    eMsg.MSGHTML += "<DIV style='font:10pt arial;'><p style='margin: 0 0 10px 0;text-align: left;'><span style='font: bold 10pt arial;'>Event Status Change </span> to " + strStatusDesc1 + "&nbsp;from :" + strStatusDesc0 + "</p><p style='margin: 0 0 10px 0;text-align: left;'></div>";
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
        }

        public static void checkEventUpdate()
        {
            SqlConnection conn = new SqlConnection(strEBMSConn);

            try
            {
                conn.Open();

                string strSQL = "select Event_Live.EV200_EVT_ID AS EVT_ID, Event_Live.EV200_EVT_DESC AS C_EVT_DESC, Event_Snapshot.EV200_EVT_DESC AS P_EVT_DESC, Event_Live.EV200_EST_ATTEND as Live_EST_ATTEND, Event_Snapshot.EV200_EST_ATTEND AS Snapshot_EST_ATTEND, ";
                strSQL += " CAST(CAST(Event_Live.EV200_EVT_IN_DATE AS DATE) AS DATETIME) + CAST(CAST(Event_Live.EV200_EVT_IN_TIME AS TIME) AS DATETIME) AS C_EVT_IN_DATETIME,  ";
                strSQL += " CAST(CAST(Event_Snapshot.EV200_EVT_IN_DATE AS DATE) AS DATETIME) + CAST(CAST(Event_Snapshot.EV200_EVT_IN_TIME AS TIME) AS DATETIME) AS P_EVT_IN_DATETIME, ";
                strSQL += " CAST(CAST(Event_Live.EV200_EVT_START_DATE AS DATE) AS DATETIME) + CAST(CAST(Event_Live.EV200_EVT_START_TIME AS TIME) AS DATETIME) AS C_EVT_START_DATETIME,  ";
                strSQL += " CAST(CAST(Event_Snapshot.EV200_EVT_START_DATE AS DATE) AS DATETIME) + CAST(CAST(Event_Snapshot.EV200_EVT_START_TIME AS TIME) AS DATETIME) AS P_EVT_START_DATETIME, ";
                strSQL += " CAST(CAST(Event_Live.EV200_EVT_OUT_DATE AS DATE) AS DATETIME) + CAST(CAST(Event_Live.EV200_EVT_OUT_TIME AS TIME) AS DATETIME) AS C_EVT_OUT_DATETIME,  ";
                strSQL += " CAST(CAST(Event_Snapshot.EV200_EVT_OUT_DATE AS DATE) AS DATETIME) + CAST(CAST(Event_Snapshot.EV200_EVT_OUT_TIME AS TIME) AS DATETIME) AS P_EVT_OUT_DATETIME, ";
                strSQL += " CAST(CAST(Event_Live.EV200_EVT_END_DATE AS DATE) AS DATETIME) + CAST(CAST(Event_Live.EV200_EVT_END_TIME AS TIME) AS DATETIME) AS C_EVT_END_DATETIME,  ";
                strSQL += " CAST(CAST(Event_Snapshot.EV200_EVT_END_DATE AS DATE) AS DATETIME) + CAST(CAST(Event_Snapshot.EV200_EVT_END_TIME AS TIME) AS DATETIME) AS P_EVT_END_DATETIME ";
                strSQL += " From (select EV200_ORG_CODE, EV200_EVT_ID, EV200_EVT_DESC, EV200_CONFIDENTIAL, EV200_EVT_CATEGORY, EV200_EVT_CLASS, EV200_EVT_TYPE, EV200_EVT_RANK, EV200_EVT_STATUS, EV200_SLSPER, EV200_COORD_1, EV200_COORD_2, EV200_CUST_NBR, EV200_ANCHOR_VENUE, EV200_CANCEL_STAMP, EV200_CANCEL_USER_ID, EV200_CANCEL_REASON, EV200_SENSITIVITY, EV200_CANCEL_APPL, EV200_BKG_ENT_STAMP, EV200_BKG_ENT_USER, EV200_BKG_CHG_STAMP, EV200_BKG_CHG_USER, EV200_EVT_COORD3, EV200_EVT_COORD4, EV200_MASTER_EVT, EV200_PREV_EVT, EV200_UPD_STAMP, EV200_ENT_STAMP, EV200_EVT_IN_DATE, EV200_EVT_IN_TIME, EV200_EVT_START_DATE, EV200_EVT_START_TIME, EV200_EVT_END_DATE, EV200_EVT_END_TIME, EV200_EVT_OUT_DATE, EV200_EVT_OUT_TIME, EV200_RELEASE_DATE,EV200_EST_ATTEND, EV200_EVENT_DAYS from EV200_EVENT_MASTER where EV200_SNAPSHOT_ID=@nsnapshotcurrentid) As Event_Live INNER JOIN ";
                strSQL += " (select EV200_ORG_CODE, EV200_EVT_ID, EV200_EVT_DESC, EV200_CONFIDENTIAL, EV200_EVT_CATEGORY, EV200_EVT_CLASS, EV200_EVT_TYPE, EV200_EVT_RANK, EV200_EVT_STATUS, EV200_SLSPER, EV200_COORD_1, EV200_COORD_2, EV200_CUST_NBR, EV200_ANCHOR_VENUE, EV200_CANCEL_STAMP, EV200_CANCEL_USER_ID, EV200_CANCEL_REASON, EV200_SENSITIVITY, EV200_CANCEL_APPL, EV200_BKG_ENT_STAMP, EV200_BKG_ENT_USER, EV200_BKG_CHG_STAMP, EV200_BKG_CHG_USER, EV200_EVT_COORD3, EV200_EVT_COORD4, EV200_MASTER_EVT, EV200_PREV_EVT, EV200_UPD_STAMP, EV200_ENT_STAMP, EV200_EVT_IN_DATE, EV200_EVT_IN_TIME, EV200_EVT_START_DATE, EV200_EVT_START_TIME, EV200_EVT_END_DATE, EV200_EVT_END_TIME, EV200_EVT_OUT_DATE, EV200_EVT_OUT_TIME, EV200_RELEASE_DATE,EV200_EST_ATTEND, EV200_EVENT_DAYS from EV200_EVENT_MASTER where EV200_SNAPSHOT_ID=@nsnapshotpreviousid) As Event_Snapshot ";
                strSQL += " on Event_Snapshot.EV200_ORG_CODE=Event_Live.EV200_ORG_CODE and Event_Snapshot.EV200_EVT_ID = Event_Live.EV200_EVT_ID ";
                strSQL += " where (Event_Live.EV200_EVT_DESC <> Event_Snapshot.EV200_EVT_DESC or CAST(Event_Live.EV200_EVT_IN_DATE AS DATE) <> CAST(Event_Snapshot.EV200_EVT_IN_DATE AS DATE) ";
                strSQL += " or CAST(Event_Live.EV200_EVT_IN_TIME AS TIME) <> CAST(Event_Snapshot.EV200_EVT_IN_TIME AS TIME) or CAST(Event_Live.EV200_EVT_START_DATE AS DATE) <> CAST(Event_Snapshot.EV200_EVT_START_DATE AS DATE) ";
                strSQL += " or CAST(Event_Live.EV200_EVT_START_TIME AS TIME) <> CAST(Event_Snapshot.EV200_EVT_START_TIME AS TIME) or CAST(Event_Live.EV200_EVT_END_DATE AS DATE) <> CAST(Event_Snapshot.EV200_EVT_END_DATE AS DATE) ";
                strSQL += " or CAST(Event_Live.EV200_EVT_END_TIME AS TIME) <> CAST(Event_Snapshot.EV200_EVT_END_TIME AS TIME) or CAST(Event_Live.EV200_EVT_OUT_DATE AS DATE) <> CAST(Event_Snapshot.EV200_EVT_OUT_DATE AS DATE) ";
                strSQL += " or CAST(Event_Live.EV200_EVT_OUT_TIME AS TIME) <> CAST(Event_Snapshot.EV200_EVT_OUT_TIME AS TIME) or Event_Live.EV200_EST_ATTEND<>Event_Snapshot.EV200_EST_ATTEND)";
                strSQL += " and Event_Live.EV200_EVT_ID=@eventid";

                SqlCommand comm = new SqlCommand(strSQL, conn);
                comm.Parameters.Add("@nsnapshotcurrentid", SqlDbType.Int).Value = nSnapshotCurrentID;
                comm.Parameters.Add("@nsnapshotpreviousid", SqlDbType.Int).Value = nSnapshotPreviousID;
                comm.Parameters.Add("@eventid", SqlDbType.Int).Value = evt.EventId;
                comm.CommandTimeout = nCommandTimeOut;

                SqlDataAdapter da = new SqlDataAdapter(comm);
                DataTable dtNewEvent = new DataTable();
                da.Fill(dtNewEvent);
                da.FillSchema(dtNewEvent, SchemaType.Source);

                if (dtNewEvent.Rows.Count > 0)
                {
                    eMsg.MSGText += "Event Updated:  \r\n";
                    eMsg.MSGHTML += "<DIV style='font:10pt arial;'><p style='margin: 0 0 10px 0;text-align: left;'><span style='font: bold 10pt arial;'>Event Updated: </p><p style='margin: 0 0 10px 0;text-align: left;'></div><DIV style='font:10pt arial;'><p style='margin: 0 0 10px 0;text-align: left;'><ul>";

                    foreach (DataRow drEvent in dtNewEvent.Rows)
                    {
                        eMsg.EventUpdated = true;

                        if (drEvent["C_EVT_DESC"].ToString() != drEvent["P_EVT_DESC"].ToString())
                        {
                            eMsg.MSGText += " Event Description changed from: " + drEvent["P_EVT_DESC"].ToString() + " to: " + drEvent["C_EVT_DESC"].ToString();
                            eMsg.MSGHTML += "<li style=font:10pt arial;'><p style='margin: 0 0 10px 0;text-align: left;'><span style='FONT-SIZE:10pt;'>Description changed to: " + drEvent["C_EVT_DESC"].ToString() + " from: <s>" + drEvent["P_EVT_DESC"].ToString() + "  </s></span></p></li>";
                        }

                        DateTime dtPStart, dtPEnd, dtPIn, dtPout, dtCStart, dtCEnd, dtCIn, dtCOut;

                        if (DateTime.TryParse(drEvent["P_EVT_START_DATETIME"].ToString(), out dtPStart))
                        {
                            dtPStart = DateTime.Parse(drEvent["P_EVT_START_DATETIME"].ToString());
                        }
                        if (DateTime.TryParse(drEvent["P_EVT_END_DATETIME"].ToString(), out dtPEnd))
                        {
                            dtPEnd = DateTime.Parse(drEvent["P_EVT_END_DATETIME"].ToString());
                        }
                        if (DateTime.TryParse(drEvent["P_EVT_IN_DATETIME"].ToString(), out dtPIn))
                        {
                            dtPIn = DateTime.Parse(drEvent["P_EVT_IN_DATETIME"].ToString());
                        }
                        if (DateTime.TryParse(drEvent["P_EVT_OUT_DATETIME"].ToString(), out dtPout))
                        {
                            dtPout = DateTime.Parse(drEvent["P_EVT_OUT_DATETIME"].ToString());
                        }
                        if (DateTime.TryParse(drEvent["C_EVT_START_DATETIME"].ToString(), out dtCStart))
                        {
                            dtCStart = DateTime.Parse(drEvent["C_EVT_START_DATETIME"].ToString());
                        }
                        if (DateTime.TryParse(drEvent["C_EVT_END_DATETIME"].ToString(), out dtCEnd))
                        {
                            dtCEnd = DateTime.Parse(drEvent["C_EVT_END_DATETIME"].ToString());
                        }
                        if (DateTime.TryParse(drEvent["C_EVT_IN_DATETIME"].ToString(), out dtCIn))
                        {
                            dtCIn = DateTime.Parse(drEvent["C_EVT_IN_DATETIME"].ToString());
                        }
                        if (DateTime.TryParse(drEvent["C_EVT_OUT_DATETIME"].ToString(), out dtCOut))
                        {
                            dtCOut = DateTime.Parse(drEvent["C_EVT_OUT_DATETIME"].ToString());
                        }

                        if (dtCStart != dtPStart || dtCEnd != dtPEnd)
                        {
                            eMsg.MSGText += " Event Start/End Date Time changed from: " + dtPStart.ToString("dd/MM/yyyy hh:mm") + " - " + dtPEnd.ToString("dd/MM/yyyy hh:mm") + " to: " + dtCStart.ToString("dd/MM/yyyy hh:mm") + " - " + dtCEnd.ToString("dd/MM/yyyy hh:mm");
                            eMsg.MSGHTML += "<li style=font:10pt arial;'><p style='margin: 0 0 10px 0;text-align: left;'><span style='FONT-SIZE:10pt;'>Start/End Date changed</span> to: " + dtCStart.ToString("dd/MM/yyyy hh:mm") + " - " + dtCEnd.ToString("dd/MM/yyyy hh:mm") + " from: " + dtPStart.ToString("dd/MM/yyyy hh:mm") + " - " + dtPEnd.ToString("dd/MM/yyyy hh:mm") + " </p></li>";
                        }

                        if (dtCIn != dtPIn || dtCOut != dtPout)
                        {
                            eMsg.MSGText += " Event In/Out Date Time changed from: " + dtPIn.ToString("dd/MM/yyyy hh:mm") + " - " + dtPout.ToString("dd/MM/yyyy hh:mm") + " to: " + dtCIn.ToString("dd/MM/yyyy hh:mm") + " - " + dtCOut.ToString("dd/MM/yyyy hh:mm");
                            eMsg.MSGHTML += "<li style=font:10pt arial;'><p style='margin: 0 0 10px 0;text-align: left;'><span style='FONT-SIZE:10pt;'>In/Out Date changed</span> to: " + dtCIn.ToString("dd/MM/yyyy hh:mm") + " - " + dtCOut.ToString("dd/MM/yyyy hh:mm") + " from: " + dtPIn.ToString("dd/MM/yyyy hh:mm") + " - " + dtPout.ToString("dd/MM/yyyy hh:mm") + " </p></li>";
                        }
                        if (drEvent["Live_EST_ATTEND"].ToString() != drEvent["Snapshot_EST_ATTEND"].ToString())
                        {
                            eMsg.MSGText += " Event Forecast Attendees changed from: " + drEvent["Snapshot_EST_ATTEND"].ToString() + " to: " + drEvent["Live_EST_ATTEND"].ToString();
                            eMsg.MSGHTML += "<li style=font:10pt arial;'><p style='margin: 0 0 10px 0;text-align: left;'><span style='FONT-SIZE:10pt;'>Forecast Attendance changed</span> from: " + drEvent["Snapshot_EST_ATTEND"].ToString() + " to: " + drEvent["Live_EST_ATTEND"].ToString() + " </p></li>";
                        }
                    }
                    eMsg.MSGHTML += "</ul></p></div>";
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

        }

        public static void checkEventNotesChange()
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
                strSQL += " (SELECT CC025_EVT_ID,CC025_FUNC_ID,CC025_ORDER, CC025_ORD_LINE,CC025_NOTE_TYPE,CC025_NOTE_HDR_SEQ, CC025_NOTE_CODE, CC025_NOTE_DESC,CC025_NOTE_PRT_CODE,CC025_NOTE_TEXT,CC025_NOTE_CLASS,CC025_UPD_DATE,CC025_UPD_USER_ID,CC025_HTML_TEXT,CC025_ENT_DATE,CC025_ENT_USER_ID FROM CC025_NOTES_EXT_Curr where CC025_EVT_ID=@eventid) as CC025_Live ";
                strSQL += " FULL OUTER JOIN ";
                strSQL += " (SELECT CC025_EVT_ID,CC025_FUNC_ID,CC025_ORDER, CC025_ORD_LINE,CC025_NOTE_TYPE,CC025_NOTE_HDR_SEQ, CC025_NOTE_CODE,CC025_NOTE_DESC,CC025_NOTE_PRT_CODE,CC025_NOTE_TEXT,CC025_NOTE_CLASS,CC025_UPD_DATE,CC025_UPD_USER_ID,CC025_HTML_TEXT,CC025_ENT_DATE,CC025_ENT_USER_ID FROM CC025_NOTES_EXT_Prev WHERE  CC025_EVT_ID=@eventid) as CC025_Snapshot  ";
                strSQL += " ON CC025_Live.CC025_NOTE_TYPE = CC025_Snapshot.CC025_NOTE_TYPE AND CC025_Live.CC025_NOTE_CODE = CC025_Snapshot.CC025_NOTE_CODE AND CC025_Live.CC025_NOTE_HDR_SEQ = CC025_Snapshot.CC025_NOTE_HDR_SEQ ";
                strSQL += " WHERE (CC025_Live.CC025_NOTE_CODE is null or CC025_Snapshot.CC025_NOTE_CODE is null OR CC025_Live.CC025_NOTE_TEXT<>CC025_Snapshot.CC025_NOTE_TEXT) ";
                strSQL += " ) AS NOTE_DIFF ";
                strSQL += " INNER JOIN AMP_Noti_NoteClassDep on Note_Class=CC025_NOTE_CLASS and Noti_Dept_Code=@deptcode ";
                strSQL += " WHERE CC025_NOTE_TYPE='EV' ";


                SqlCommand comm = new SqlCommand(strSQL, conn);
                comm.Parameters.Add("@eventid", SqlDbType.Int).Value = evt.EventId;
                comm.Parameters.Add("@deptcode", SqlDbType.VarChar, 20).Value = dep.DepartmentCode;
                comm.CommandTimeout = nCommandTimeOut;

                SqlDataAdapter da = new SqlDataAdapter(comm);
                DataTable dt = new DataTable();
                da.Fill(dt);
                da.FillSchema(dt, SchemaType.Source);

                if (dt.Rows.Count > 0)
                {
                    eMsg.EventUpdated = true;

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
                            eMsg.MSGText += "New Event Notes entered/updated by " + AMP_Common.GetUserName(dr["CC025_UPD_USER_ID"].ToString()) + " on " + dtUpdDate.ToString("dd/MM/yyyy hh:mm") + "\r\n";
                            eMsg.MSGHTML += "<DIV style='font:10pt arial;'><p style='margin: 0 0 10px 0;text-align: left;'><span style='font: bold 10pt arial;'>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; New Event Notes (" + dr["CC025_NOTE_DESC"].ToString() + ")</span> entered/updated by " + AMP_Common.GetUserName(dr["CC025_UPD_USER_ID"].ToString()) + " on " + dtUpdDate.ToString("dd/MM/yyyy hh:mm") + " </p>";
                            eMsg.MSGHTML += "<ul style='MARGIN:0 0 10px 40px;LIST-STYLE-TYPE:disc;'><li style=font:10pt arial;'><p style='MARGIN:0 0 10px 0;TEXT-ALIGN:left;'>";
                            if (dr["CC025_Live_NOTE_TEXT"].ToString().Length <= rule.NotesLength)
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
                            eMsg.MSGText += "Deleted Event " + dr["CC025_NOTE_DESC"].ToString() + " Notes \r\n";
                            eMsg.MSGHTML += "<DIV style='font:10pt arial;'><p style='margin: 0 0 10px 0;text-align: left;'><span style='font: bold 10pt arial;'>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Deleted Event Notes (" + dr["CC025_NOTE_DESC"].ToString() + ")</span> </p>";
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
                            eMsg.MSGText += dr["CC025_NOTE_DESC"].ToString() + "Event Notes Updated by " + AMP_Common.GetUserName(dr["CC025_UPD_USER_ID"].ToString()) + " on " + dtUpdDate.ToString("dd/MM/yyyy hh:mm") + " \r\n";
                            eMsg.MSGHTML += "<DIV style='font:10pt arial;'><p style='margin: 0 0 10px 0;text-align: left;'><span style='font: bold 10pt arial;'>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Event Notes (" + dr["CC025_NOTE_DESC"].ToString() + ") Updated</span> by " + AMP_Common.GetUserName(dr["CC025_UPD_USER_ID"].ToString()) + " on " + dtUpdDate.ToString("dd/MM/yyyy hh:mm") + " </p><p style='margin: 0 0 10px 0;text-align: left;'>";
                            eMsg.MSGHTML += "<ul style='MARGIN:0 0 10px 40px;LIST-STYLE-TYPE:disc;'><li style=font:10pt arial;'><p style='MARGIN:0 0 10px 0;TEXT-ALIGN:left;'>";
                            if (dr["CC025_Live_NOTE_TEXT"] != DBNull.Value && dr["CC025_Snapshot_NOTE_TEXT"] != DBNull.Value)
                            {
                                if (dr["CC025_Live_NOTE_TEXT"].ToString().Length <= rule.NotesLength)
                                {
                                    eMsg.MSGText += "Update To : " + dr["CC025_Live_NOTE_TEXT"].ToString() + "\r\n";
                                    eMsg.MSGHTML += "Update To : " + dr["CC025_Live_HTML_TEXT"].ToString() + "</p><p style='MARGIN:0 0 10px 0;TEXT-ALIGN:left;'><span style='COLOR:#4f4f4f;BACKGROUND-COLOR:White;'><br/><strike>";
                                }
                                else
                                {
                                    eMsg.MSGText += "Update To:" + dr["CC025_Live_NOTE_TEXT"].ToString().Substring(0, rule.NotesLength - 3) + " ...\r\n";
                                    eMsg.MSGHTML += "Update To:" + dr["CC025_Live_HTML_TEXT"].ToString().Substring(0, rule.NotesLength - 3) + " ...</ p >< p style = 'MARGIN:0 0 10px 0;TEXT-ALIGN:left;' >< span style = 'COLOR:#4f4f4f;BACKGROUND-COLOR:White;' ><br/><strike> ";
                                }

                                if (dr["CC025_Snapshot_NOTE_TEXT"].ToString().Length <= rule.NotesLength)
                                {
                                    eMsg.MSGText += "from:" + dr["CC025_Snapshot_NOTE_TEXT"].ToString() + "\r\n";
                                    eMsg.MSGHTML += "from:" + dr["CC025_Snapshot_HTML_TEXT"].ToString() + "</strike><br></p></li></ul></DIV>";
                                }
                                else
                                {
                                    eMsg.MSGText += "from : " + dr["CC025_Snapshot_NOTE_TEXT"].ToString().Substring(0, rule.NotesLength - 3) + " ...r\n";
                                    eMsg.MSGHTML += "from :" + dr["CC025_Snapshot_HTML_TEXT"].ToString().Substring(0, rule.NotesLength - 3) + " ...</strike><br></p></li></ul></DIV>";
                                }

                                eMsg.MSGHTML += "</p></div>";
                            }
                        }
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
        }

        public static void checkDocumentChange()
        {
            SqlConnection conn = new SqlConnection(strEBMSConn);

            try
            {
                conn.Open();

                //step1, get all deleted function related to the department
                string strSQL = " SELECT LIVE_Doc_SEQ_KEY, Snapshot_Doc_SEQ_KEY, MM446_DOC_DESC, MM446_HEADING_SEQ_1, MM446_HEADING_SEQ_2, MM446_UPD_USER_ID, MM446_UPD_STAMP";
                strSQL += " FROM (SELECT Live_Doc.MM446_DOC_SEQ_KEY AS LIVE_Doc_SEQ_KEY, Snapshot_Doc.MM446_DOC_SEQ_KEY AS Snapshot_Doc_SEQ_KEY,";
                strSQL += " CASE WHEN Live_Doc.MM446_DOC_DESC IS NULL THEN Snapshot_Doc.MM446_DOC_DESC ELSE Live_Doc.MM446_DOC_DESC END AS MM446_DOC_DESC,";
                strSQL += " CASE WHEN Live_Doc.MM446_HEADING_SEQ_1 IS NULL THEN Snapshot_Doc.MM446_HEADING_SEQ_1 ELSE Live_Doc.MM446_HEADING_SEQ_1 END AS MM446_HEADING_SEQ_1,";
                strSQL += " CASE WHEN Live_Doc.MM446_HEADING_SEQ_2 IS NULL THEN Snapshot_Doc.MM446_HEADING_SEQ_2 ELSE Live_Doc.MM446_HEADING_SEQ_2 END AS MM446_HEADING_SEQ_2,";
                strSQL += " CASE WHEN Live_Doc.MM446_UPD_USER_ID IS NULL THEN Snapshot_Doc.MM446_UPD_USER_ID ELSE Live_Doc.MM446_UPD_USER_ID END AS MM446_UPD_USER_ID,";
                strSQL += " CASE WHEN Live_Doc.MM446_UPD_STAMP IS NULL THEN Snapshot_Doc.MM446_UPD_STAMP ELSE Live_Doc.MM446_UPD_STAMP END AS MM446_UPD_STAMP ";
                strSQL += " FROM ";
                strSQL += " (SELECT MM446_DOC_CLASS, MM446_DOC_SEQ_KEY, MM446_DOC_ENTRY_ID,MM446_DOC_STS, MM446_DOC_DESC, MM446_DOC_SUBJ, MM446_EVENT, MM446_EV_FUNC, MM446_UPD_USER_ID, MM446_ENT_STAMP,MM446_UPD_STAMP, MM446_HEADING_SEQ_1, MM446_HEADING_SEQ_2 FROM MM446_DOC_ENTRY_Curr WHERE MM446_EVENT=@eventid) AS Live_Doc";
                strSQL += " FULL OUTER JOIN";
                strSQL += " (SELECT MM446_DOC_CLASS, MM446_DOC_SEQ_KEY, MM446_DOC_ENTRY_ID, MM446_DOC_DESC, MM446_DOC_SUBJ, MM446_EVENT, MM446_EV_FUNC, MM446_UPD_USER_ID, MM446_ENT_STAMP,MM446_UPD_STAMP, MM446_HEADING_SEQ_1, MM446_HEADING_SEQ_2 FROM MM446_DOC_ENTRY_Prev WHERE MM446_EVENT=@eventid) AS Snapshot_Doc";
                strSQL += " ON Live_Doc.MM446_DOC_CLASS=Snapshot_Doc.MM446_DOC_CLASS AND Live_Doc.MM446_DOC_SEQ_KEY=Snapshot_Doc.MM446_DOC_SEQ_KEY AND Live_Doc.MM446_DOC_ENTRY_ID=Snapshot_Doc.MM446_DOC_ENTRY_ID";
                strSQL += " WHERE Live_Doc.MM446_DOC_STS='CI' AND (Live_Doc.MM446_ENT_STAMP between @prevdatetime AND @currdatetime OR Live_Doc.MM446_UPD_STAMP between @prevdatetime AND @currdatetime) or (Live_Doc.MM446_ENT_STAMP is null)";
                strSQL += " ) AS DocDiff";
                strSQL += " INNER JOIN AMP_Noti_DOCHDGDEPT DOCDEP ON DOCDEP.HDG_SEQ_1 = DocDiff.MM446_HEADING_SEQ_1 AND DOCDEP.HDG_SEQ_2 = DocDiff.MM446_HEADING_SEQ_2 and DOCDEP.Noti_Dept_Code=@deptcode ";


                SqlCommand comm = new SqlCommand(strSQL, conn);
                comm.Parameters.Add("@eventid", SqlDbType.Int).Value = evt.EventId;
                comm.Parameters.Add("@deptcode", SqlDbType.VarChar, 20).Value = dep.DepartmentCode;
                comm.Parameters.Add("@prevdatetime", SqlDbType.DateTime).Value = dtSnapshotPrevious;
                comm.Parameters.Add("@currdatetime", SqlDbType.DateTime).Value = dtSnapshotCurrent;
                comm.CommandTimeout = nCommandTimeOut;

                SqlDataAdapter da = new SqlDataAdapter(comm);
                DataTable dt = new DataTable();
                da.Fill(dt);
                da.FillSchema(dt, SchemaType.Source);

                if (dt.Rows.Count > 0)
                {
                    eMsg.EventUpdated = true;

                    //check for each deleted func data
                    foreach (DataRow dr in dt.Rows)
                    {
                        DateTime dtUpdDate = new DateTime();

                        if (dr["MM446_UPD_STAMP"] != DBNull.Value)
                        {
                            if (DateTime.TryParse(dr["MM446_UPD_STAMP"].ToString(), out dtUpdDate))
                            {
                                dtUpdDate = DateTime.Parse(dr["MM446_UPD_STAMP"].ToString());
                            }
                        }

                        int nDocHeadSeq1 = 0, nDocHeadSeq2 = 0;

                        if (dr["MM446_HEADING_SEQ_1"] != DBNull.Value)
                        {
                            if (int.TryParse(dr["MM446_HEADING_SEQ_1"].ToString(), out nDocHeadSeq1))
                            {
                                nDocHeadSeq1 = int.Parse(dr["MM446_HEADING_SEQ_1"].ToString());
                            }
                        }
                        if (dr["MM446_HEADING_SEQ_2"] != DBNull.Value)
                        {
                            if (int.TryParse(dr["MM446_HEADING_SEQ_2"].ToString(), out nDocHeadSeq2))
                            {
                                nDocHeadSeq2 = int.Parse(dr["MM446_HEADING_SEQ_2"].ToString());
                            }
                        }

                        string strDocHeading1 = AMP_Common.GetDocumentHeading(nDocHeadSeq1);
                        string strDocHeading2 = AMP_Common.GetDocumentHeading(nDocHeadSeq2);
                        string strDocHeading = string.Empty;

                        if (!string.IsNullOrEmpty(strDocHeading2))
                        {
                            strDocHeading = strDocHeading1 + " : " + strDocHeading2;
                        }
                        else
                        {
                            strDocHeading = strDocHeading1;
                        }

                        //new document
                        if (dr["Snapshot_Doc_SEQ_KEY"] == DBNull.Value)
                        {
                            eMsg.MSGText += "New document entered/updated by " + AMP_Common.GetUserName(dr["MM446_UPD_USER_ID"].ToString()) + " on " + dtUpdDate.ToString("dd/MM/yyyy hh:mm") + " \r\n";
                            eMsg.MSGHTML += "<DIV style='font:10pt arial;'><p style='margin: 0 0 10px 0;text-align: left;'><span style='font: bold 10pt arial;'>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;New Document</span> entered/updated by " + AMP_Common.GetUserName(dr["MM446_UPD_USER_ID"].ToString()) + " on " + dtUpdDate.ToString("dd/MM/yyyy hh:mm") + " </p>";
                        }
                        //deleted notes
                        else if (dr["LIVE_Doc_SEQ_KEY"] == DBNull.Value)
                        {
                            eMsg.MSGText += "Deleted document \r\n";
                            eMsg.MSGHTML += "<DIV style='font:10pt arial;'><p style='margin: 0 0 10px 0;text-align: left;'><span style='font: bold 10pt arial;'>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; Deleted Document</p>";
                        }
                        else
                        {
                            eMsg.MSGText += "Document Updated by " + AMP_Common.GetUserName(dr["MM446_UPD_USER_ID"].ToString()) + " on " + dtUpdDate.ToString("dd/MM/yyyy hh:mm") + " \r\n";
                            eMsg.MSGHTML += "<DIV style='font:10pt arial;'><p style='margin: 0 0 10px 0;text-align: left;'><span style='font: bold 10pt arial;'>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Document Updated</span> by " + AMP_Common.GetUserName(dr["MM446_UPD_USER_ID"].ToString()) + " on " + dtUpdDate.ToString("dd/MM/yyyy hh:mm") + " </p>";
                        }
                        eMsg.MSGHTML += "<ul style='MARGIN:0 0 10px 40px;LIST-STYLE-TYPE:disc;'><li style=font:10pt arial;'><p style='MARGIN:0 0 10px 0;TEXT-ALIGN:left;'>";
                        eMsg.MSGText += "(" + strDocHeading + ")" + dr["MM446_DOC_DESC"].ToString() + " \r\n";
                        eMsg.MSGHTML += "<span style='font: bold 10pt arial;'>(" + strDocHeading + ")</span>" + dr["MM446_DOC_DESC"].ToString();
                        eMsg.MSGHTML += "<br></p></li></ul></DIV>";
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

        }

        public static void SendMSG()
        {
            AMP_Notification.ReadyMSG = new Notification_MSG();
            AMP_Notification.ReadyMSG.dept = dep;
            AMP_Notification.ReadyMSG.rule = rule;
            AMP_Notification.ReadyMSG.emsg = eMsg;
            AMP_Notification.ReadyMSG.nSnapshotCurr = nSnapshotCurrentID;
            AMP_Notification.ReadyMSG.nSnapshotPrev = nSnapshotPreviousID;
            AMP_Notification.SendMSG();
        }
    }
}
