using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BLL;

namespace EBMS_Amendments
{
    class Program
    {
        static void Main(string[] args)
        {
            EventAmendmentBLL.checkAmendmentbyDepartment_AllofEvent();
        }
    }
}
