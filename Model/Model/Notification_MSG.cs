using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Model
{
    public class Notification_MSG
    {
        public AMP_Rules rule { get; set; }
        public Notification_Dep_user dept { get; set; }
        public EventAccountMessage emsg { get; set; }
        public int nSnapshotPrev { get; set; }
        public int nSnapshotCurr { get; set; }
    }

    public class Notification_Dep_user
    {
        public string DepartmentCode { get; set; }
        public string DepartmentDesc { get; set; }
        public string UserId { get; set; }
        public string NotifiMethod { get; set; }
        public string EmailAddress { get; set; }
    }

    public class EventAccountMessage
    {
        public int EventId { get; set; }
        public int FuncId { get; set; }
        public string AcctCode { get; set; }
        public string MessageType { get; set; }
        public string MSGText { get; set; }
        public string MSGHTML { get; set; }
        public bool EventUpdated { get; set; }
        public bool FuncUpdated { get; set; }
        public bool OrderUpdated { get; set; }
    }
}
