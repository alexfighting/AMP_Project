using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DAL;
using Model;
using BLL;

namespace AmendmentManually
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void btnRun_Click(object sender, EventArgs e)
        {
             if (checkValues())
             {
                 foreach (object item in checkedListBox1.CheckedItems)
                 {
                     Notification_Dep_user dep = (Notification_Dep_user)item;
            
                     if (!string.IsNullOrEmpty(dep.DepartmentCode))
                     {
                         int nStartId = AMP_Common.getRecentSnapshotID(dtPickerStart.Value);
                         int nEndId = AMP_Common.getRecentSnapshotID(dtPickerEnd.Value);
                         DateTime dtStart = AMP_Common.getRecentSnapshotDateTime(dtPickerStart.Value);
                         DateTime dtEnd = AMP_Common.getRecentSnapshotDateTime(dtPickerEnd.Value);
            
                         string strEventID = txtEventID.Text.ToString();
            
                         if (!string.IsNullOrEmpty(strEventID))
                         {
                             EventAmendmentBLL.checkAmendmentManually(dep.DepartmentCode, nStartId, nEndId, strEventID);
                         }
                         else
                         {
                             EventAmendmentBLL.checkAmendmentManually(dep.DepartmentCode, nStartId, nEndId);
                         }
                     }
                 }
            
             }                       
        }

        private bool checkValues()
        {
            bool isSuccess = true;

            if (checkedListBox1.CheckedItems.Count==0)
            {
                isSuccess = false;

                MessageBox.Show("Please select at least one department to run.");
                return false;
            }


            if (dtPickerStart.Value>=dtPickerEnd.Value)
            {
                isSuccess = false;

                MessageBox.Show("Start time must early than end time.");
                return false;
            }
            else
            {
                DateTime dtStart = AMP_Common.getRecentSnapshotDateTime(dtPickerStart.Value);
                DateTime dtEnd = AMP_Common.getRecentSnapshotDateTime(dtPickerEnd.Value);

                if (dtStart >= dtEnd)
                {
                    isSuccess = false;

                    MessageBox.Show("Start time and end time have the same snapshot value in the database.");
                    return false;
                }
            }

           return isSuccess;
        }


        private void txtEventID_TextChanged(object sender, EventArgs e)
        {
            string strEventID = txtEventID.Text.ToString();

            int nEventId = int.MinValue;
            bool isSuccess = int.TryParse(strEventID, out nEventId);

            if (!isSuccess && !string.IsNullOrEmpty(strEventID))
            {
                MessageBox.Show("Please only enter numbers in event id field.");
                txtEventID.Text = txtEventID.Text.Substring(0, txtEventID.Text.Length - 1);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LoadDepartments();
        }

        private void LoadDepartments()
        {
            List<Notification_Dep_user> lstDepartments = AMP_DepartmentDAL.getDepartments();

            if (lstDepartments.Count >0)
            {
                ((ListBox)checkedListBox1).DataSource = lstDepartments;
                ((ListBox)checkedListBox1).DisplayMember = "DepartmentDesc";
                ((ListBox)checkedListBox1).ValueMember = "DepartmentCode";
            }
        }

       
    }
}
