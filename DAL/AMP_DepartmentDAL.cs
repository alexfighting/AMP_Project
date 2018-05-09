using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Model;
using System.Data.SqlClient;
using System.Data;

namespace DAL
{
    public class AMP_DepartmentDAL
    {
        public static string strEBMSConn = Properties.Settings.Default.strEBMSConn;
        public static string strCompDatabase = Properties.Settings.Default.strCompDatabase;
        public static string strEmailFrom = Properties.Settings.Default.Amendment_Error_EmailAddressFrom;
        public static string strEmailTo = Properties.Settings.Default.Amendment_Error_EmailAddressTo;
        public static int nCommandTimeOut = Properties.Settings.Default.nCommandTimeOut;


        public static List<Notification_Dep_user> getDepartments()
        {
            List<Notification_Dep_user> depts = new List<Notification_Dep_user>();

            SqlConnection conn = new SqlConnection(strEBMSConn);

            try
            {
                conn.Open();

                string strSQL = "SELECT  Noti_Dep_Code,Noti_Dep_Desc, Dep_User_Id, Noti_Method, EmailAddress  FROM  Noti_Dept dep  where Status='1' and (Dep_User_Id<>'' or EmailAddress<>'')";

                SqlCommand comm = new SqlCommand(strSQL, conn);
                comm.CommandTimeout = nCommandTimeOut;

                SqlDataAdapter da = new SqlDataAdapter(comm);
                DataTable dt = new DataTable();

                da.Fill(dt);
                da.FillSchema(dt, SchemaType.Source);

                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        if (dr["Noti_Dep_Code"] != DBNull.Value && (dr["Dep_User_Id"] != DBNull.Value || dr["EmailAddress"] != DBNull.Value))
                        {
                            Notification_Dep_user Noti = new Notification_Dep_user();
                            Noti.DepartmentCode = dr["Noti_Dep_Code"].ToString();
                            Noti.DepartmentDesc = dr["Noti_Dep_Desc"].ToString();
                            Noti.NotifiMethod = dr["Noti_Method"].ToString();
                            Noti.UserId = dr["Dep_User_Id"].ToString();
                            Noti.EmailAddress = dr["EmailAddress"].ToString();
                            depts.Add(Noti);
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

            return depts;
        }

        public static Notification_Dep_user getDepartment(string strDeptCode)
        {
            Notification_Dep_user dept = new Notification_Dep_user();

            SqlConnection conn = new SqlConnection(strEBMSConn);

            try
            {
                conn.Open();

                string strSQL = "SELECT  Noti_Dep_Code,Noti_Dep_Desc, Dep_User_Id, Noti_Method, EmailAddress  FROM  Noti_Dept  where (Dep_User_Id<>'' or EmailAddress<>'') and Noti_Dep_Code=@departmentcode";

                SqlCommand comm = new SqlCommand(strSQL, conn);
                comm.Parameters.Add("@departmentcode", SqlDbType.VarChar, 20).Value = strDeptCode;
                comm.CommandTimeout = nCommandTimeOut;

                SqlDataReader dr = comm.ExecuteReader();

                if (dr.Read())
                {
                    if (dr["Noti_Dep_Code"] != DBNull.Value && (dr["Dep_User_Id"] != DBNull.Value || dr["EmailAddress"] != DBNull.Value))
                    {
                        dept.DepartmentCode = dr["Noti_Dep_Code"].ToString();
                        dept.DepartmentDesc = dr["Noti_Dep_Desc"] ==DBNull.Value?"": dr["Noti_Dep_Desc"].ToString();
                        dept.NotifiMethod = dr["Noti_Method"].ToString();
                        dept.UserId = dr["Dep_User_Id"].ToString();
                        dept.EmailAddress = dr["EmailAddress"].ToString();
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

            return dept;
        }

    }
}
