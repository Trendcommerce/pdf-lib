using System;
using System.Collections.Generic;

namespace TC.Classes
{
    public class clsMailEntry
    {
        public DateTime ErrorDate;
        public string Customer = string.Empty;
        public string ApplicationName = string.Empty;
        public string MachineName = string.Empty;
        public string DBEnvironment = string.Empty;
        public string MailMessage = string.Empty;
        public string MailMessageForExternal = string.Empty;
        public string MailSubjectForExternal = string.Empty;
        public string XMLGUID = string.Empty;
        public string MailType = string.Empty;
        public string AppPath = string.Empty;
        public List<ErrorDetail> ErrorList = new List<ErrorDetail>();
        public string CurrentUser = string.Empty;
    }

    public class ErrorDetail
    {
        public string exLine = string.Empty;
        public string exMessage = string.Empty;
    }
}
