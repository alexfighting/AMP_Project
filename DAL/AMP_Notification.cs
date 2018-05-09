using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using Model;
using System.Data.SqlClient;
using System.Data;

namespace DAL
{
    public class AMP_Notification
    {
        public static string strEBMSConn = Properties.Settings.Default.strEBMSConn;

        public static string strCompDatabase = Properties.Settings.Default.strCompDatabase;
        public static int nCommandTimeOut = Properties.Settings.Default.nCommandTimeOut;

        public static Notification_MSG ReadyMSG { get; set; }

        public AMP_Notification()
        {
            //
            // TODO: Add constructor logic here
            //
        }


        public static void SendMSG()
        {           
            if (ReadyMSG.dept.NotifiMethod == "Email" && !string.IsNullOrEmpty(ReadyMSG.dept.EmailAddress))
            {                
                SaveHistory();
                sendMsg_via_Email();
            }
            if (ReadyMSG.dept.NotifiMethod == "Activity" && !string.IsNullOrEmpty(ReadyMSG.dept.UserId))
            {
                saveToActivity();
                SaveHistory();
            }
            if (ReadyMSG.dept.NotifiMethod == "Both" && !string.IsNullOrEmpty(ReadyMSG.dept.EmailAddress) && !string.IsNullOrEmpty(ReadyMSG.dept.UserId))
            {                
                saveToActivity();
                SaveHistory();
                sendMsg_via_Email();
            }
        }


        public static void sendEmail(string strFrom, string strTo, string strSubject, string strBody)
        {
            MailMessage mail = new MailMessage(strFrom, strTo);
            mail.Body = strBody;
            mail.Subject = strSubject;
            mail.IsBodyHtml = true;
            SmtpClient client = new SmtpClient("smtp.mecc.com.au");
            client.Credentials = System.Net.CredentialCache.DefaultNetworkCredentials;
            client.Send(mail);

        }//end sendEmail

        public void sendEmailAttachment(string strFrom, string strTo, string strSubject, string strBody, string strAttachmentFile)
        {
            MailMessage mail = new MailMessage(strFrom, strTo);
            mail.Body = strBody;
            mail.Subject = strSubject;
            mail.IsBodyHtml = true;
            mail.Attachments.Add(new Attachment(strAttachmentFile));
            SmtpClient client = new SmtpClient("smtp.mecc.com.au");
            client.Credentials = System.Net.CredentialCache.DefaultNetworkCredentials;
            client.Send(mail);
        }
     

        public static void saveToActivity()
        {
            SqlConnection conn = new SqlConnection(strEBMSConn);

            int nMaxSEQ = 1;

            if (!string.IsNullOrEmpty(ReadyMSG.dept.UserId) && !string.IsNullOrEmpty(ReadyMSG.emsg.AcctCode))
            {
                try
                {
                    conn.Open();

                    string strSQL0 = "SELECT COALESCE(MAX([CR031_DIARY_SEQ]),0) + 1 as MAXSEQ FROM [" + strCompDatabase + "].[dbo].[CR031_DIARY] WHERE CR031_EXT_ACCT_CODE=@EventAcctCode";
                    SqlCommand comm0 = new SqlCommand(strSQL0, conn);
                    comm0.Parameters.Add("@EventAcctCode", SqlDbType.VarChar, 8).Value = ReadyMSG.emsg.AcctCode;
                    SqlDataReader dr = comm0.ExecuteReader();

                    if (dr.Read() && dr.HasRows)
                    {
                        nMaxSEQ = int.Parse(dr["MAXSEQ"].ToString());
                    }

                    dr.Close();

                    string strSQL = " INSERT INTO [" + strCompDatabase + "].[dbo].[CR031_DIARY] ";
                    strSQL += " ([CR031_ORG_CODE] ,[CR031_EXT_ACCT_CODE], [CR031_DIARY_SEQ], [CR031_DIARY_SEQ_CONT], [CR031_OCCURENCE], [CR031_DIARY_TEXT], [CR031_DIARY_DATE], [CR031_DIARY_TIME], ";
                    strSQL += " [CR031_DIARY_REP_CODE], [CR031_DIARY_TYPE] , [CR031_DIARY_ENTRY_TYPE], [CR031_TRACE_PRIORITY], [CR031_PRIVILEGED] , [CR031_ENT_DATE], [CR031_ENT_USER_ID],  ";
                    strSQL += " [CR031_TRACE_STS] , [CR031_DESIGNATION], [CR031_EVT_ID], [CR031_FUNC_ID] , [CR031_HTML_TEXT] , [CR031_ACT_STATUS], [CR031_ACT_CLASS], [CR031_SOURCE]) ";
                    strSQL += " VALUES ";
                    strSQL += " ('10', @DiaryExtAcctCode, @DiarySeq,0 ,0 ,@DiaryText, GETDATE(), GETDATE(), @UserId, 'A', @amendmenttype, 'R', 'N', GETDATE(), 'AZHENG', 'N', 'C', @DiaryEventId, @DiaryFuncId, @DiaryHTMLText, 'N', 'T', 'SYS') ";


                    SqlCommand comm = new SqlCommand(strSQL, conn);
                    comm.Parameters.Add("@DiaryExtAcctCode", SqlDbType.VarChar, 8).Value = ReadyMSG.emsg.AcctCode;
                    comm.Parameters.Add("@DiarySeq", SqlDbType.Int).Value = nMaxSEQ;
                    comm.Parameters.Add("@DiaryText", SqlDbType.VarChar).Value = ReadyMSG.emsg.MSGText;
                    //comm.Parameters.Add("@DiaryText", SqlDbType.Text).Value = "Event Amendments";                                     
                    comm.Parameters.Add("@UserId", SqlDbType.VarChar, 8).Value = ReadyMSG.dept.UserId;
                    comm.Parameters.Add("@amendmenttype", SqlDbType.VarChar, 3).Value = ReadyMSG.emsg.MessageType;                    
                    comm.Parameters.Add("@DiaryEventId", SqlDbType.Int).Value = ReadyMSG.emsg.EventId;
                    comm.Parameters.Add("@DiaryFuncId", SqlDbType.Int).Value = ReadyMSG.emsg.FuncId;
                    comm.Parameters.Add("@DiaryHTMLText", SqlDbType.VarChar).Value = ReadyMSG.emsg.MSGHTML;
                    comm.CommandTimeout = nCommandTimeOut;

                    comm.ExecuteNonQuery();                    
                }
                catch (Exception ex)
                {
                    AMP_Common.sendException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name, "amendmenterror@mcec.com.au", "azheng@mcec.com.au", ex.Message);
                }
                finally
                {
                    conn.Close();
                }
            }        
        }

        public static void SaveHistory()
        {
            SqlConnection conn = new SqlConnection(strEBMSConn);

            try
            {
                conn.Open();

                string strSQL2 = "INSERT INTO AMP_Logs (RuleCode ,RuleName ,nSnapshotPrevious ,nSnapshotCurrent ,DepartmentCode ,UserId ,EmailAddress ,AccountCode, EventId, DiarySeq ,DiaryText, DiaryHTML ,RunDate) VALUES ";
                strSQL2 += " (@rulecode, @rulename, @nSnapshotPrevId, @nSnapshotCurrId, @deptcode, @userid, @emailaddress, @accountcode,@eventid, @diaryseq, @diarytext, @diaryhtml, @rundate)";

                SqlCommand comm2 = new SqlCommand(strSQL2, conn);
                comm2.Parameters.Add("@rulecode", SqlDbType.VarChar, 20).Value = ReadyMSG.rule.RuleId;
                comm2.Parameters.Add("@rulename", SqlDbType.VarChar, 255).Value = ReadyMSG.rule.Rule_Name;
                comm2.Parameters.Add("@nSnapshotPrevId", SqlDbType.Int).Value = ReadyMSG.nSnapshotPrev;
                comm2.Parameters.Add("@nSnapshotCurrId", SqlDbType.Int).Value = ReadyMSG.nSnapshotCurr;
                comm2.Parameters.Add("@deptcode", SqlDbType.VarChar, 20).Value = ReadyMSG.dept.DepartmentCode;
                comm2.Parameters.Add("@userid", SqlDbType.VarChar, 50).Value = ReadyMSG.dept.UserId;
                comm2.Parameters.Add("@emailaddress", SqlDbType.VarChar, 200).Value = ReadyMSG.dept.EmailAddress;
                comm2.Parameters.Add("@accountcode", SqlDbType.VarChar, 8).Value = ReadyMSG.emsg.AcctCode;
                comm2.Parameters.Add("@eventid", SqlDbType.Int).Value = ReadyMSG.emsg.EventId;
                comm2.Parameters.Add("@diaryseq", SqlDbType.Int).Value = 0;
                comm2.Parameters.Add("@diarytext", SqlDbType.NVarChar, -1).Value = ReadyMSG.emsg.MSGText;
                comm2.Parameters.Add("@diaryhtml", SqlDbType.NVarChar, -1).Value = ReadyMSG.emsg.MSGHTML;
                comm2.Parameters.Add("@rundate", SqlDbType.DateTime).Value = DateTime.Now;
                comm2.CommandTimeout = nCommandTimeOut;

                comm2.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                AMP_Common.sendException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name, "amendmenterror@mcec.com.au", "azheng@mcec.com.au", ex.Message);
            }
            finally
            {
                conn.Close();
            }
        }

        public static void sendMsg_via_Email()
        {
            if (!string.IsNullOrEmpty(ReadyMSG.dept.EmailAddress))
            {
                try
                {
                    string strFrom = "Event_Amendment@mcec.com.au";
                    string strTo = ReadyMSG.dept.EmailAddress;
                    string strSubject = ReadyMSG.rule.EmailSubject;
                    string strBody = ReadyMSG.emsg.MSGHTML;
                    sendEmail(strFrom, strTo, strSubject, strBody);
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message, ex);
                }                
            }
        }
    }
}
