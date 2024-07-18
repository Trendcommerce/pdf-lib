using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TC.Functions;

namespace TC.Classes
{
    // Template for Class with Logging-Functionality (11.11.2022, SME)
    public abstract class ClassWithLogging
    {
        // Log Text (11.11.2022, SME)
        protected void Log(string text)
        {
            CoreFC.WriteDebugInfo(text);
        }

        // Log Error (11.11.2022, SME)
        protected void LogError(System.Exception error, MethodBase method)
        {
            CoreFC.WriteError(error, method);
        }

        // Log Event (11.11.2022, SME)
        protected void LogEvent(MethodBase method, object sender, EventArgs e)
        {
            CoreFC.WriteEventInfo(method, sender, e);
        }
    }
}
