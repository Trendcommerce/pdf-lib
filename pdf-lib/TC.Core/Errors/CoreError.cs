using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using TC.Classes;
using TC.Functions;
using TC.Interfaces;

namespace TC.Errors
{
    // Core-Error (14.11.2022, SRM)
    public class CoreError : System.Exception
     {
        #region SHARED

        // SHARED: Get Calling Method (14.11.2022, SRM)
        public static System.Reflection.MethodBase GetCallingMethod()
        {
            var frame = new System.Diagnostics.StackFrame(2);
            return frame.GetMethod();
        }

        #endregion

        #region General

        // Neue leere Instanz
        protected CoreError() : base() { InitStackTrace(); }

        // Neue Instanz mit Inner-Exception
        protected CoreError(Exception innerException) : base(string.Empty, ExceptionDispatchInfo.Capture(innerException).SourceException) { InitStackTrace(); }

        // Neue Instanz mit Message
        public CoreError(string message) : base(message) { InitStackTrace(); }

        // Neue Instanz mit Message und Inner-Exception
        public CoreError(string message, Exception innerException) : base(message, ExceptionDispatchInfo.Capture(innerException).SourceException) { InitStackTrace(); }

        //// New Instance with Inner Exception (14.11.2022, SRM)
        //public CoreError(Exception innerException) : base("Unhandled Error", ExceptionDispatchInfo.Capture(innerException).SourceException)
        //{
        //    Method = GetCallingMethod();
        //}

        //// New Instance with Message (02.01.2024, SME)
        //public CoreError(string message) : base(message)
        //{
        //    Method = GetCallingMethod();
        //}

        //// New Instance with Message + Inner Exception (14.11.2022, SRM)
        //public CoreError(string message, Exception innerException) : base(message, ExceptionDispatchInfo.Capture(innerException).SourceException)
        //{
        //    Method = GetCallingMethod();
        //}

        //// Proctected: New Instance with only calling Method (02.01.2024, SME)
        //protected CoreError(MethodBase method)
        //{
        //    // error-handling
        //    if (method == null) throw new ArgumentNullException(nameof(method));

        //    // set properties
        //    Method = method;
        //}

        //// Proctected: New Instance with calling Method + Message (02.01.2024, SME)
        //protected CoreError(MethodBase method, string message) : base(message)
        //{
        //    // error-handling
        //    if (method == null) throw new ArgumentNullException(nameof(method));

        //    // set properties
        //    Method = method;
        //}

        #endregion

        #region Properties

        // Method (14.11.2022, SRM)
        public readonly MethodBase Method;

        // Stack-Frame von welchem der Fehler ausgelöst wurde (26.04.2023, SME)
        public StackFrame ErrorCalledFrom { get; private set; }

        #endregion

        #region Methods

        // Initialize Stack-Trace
        private void InitStackTrace()
        {
            try
            {
                StackFrame current;

                for (int i = 1; i < 20; i++)
                {
                    current = new(i, true);
                    if (!CoreFC.IsDerivedType(current.GetMethod().DeclaringType, typeof(CoreError)))
                    {
                        ErrorCalledFrom = current;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR while setting ErrorCalledFrom: " + ex.Message);
            }
        }

        #endregion

        #region Message-Handling

        // OVERRIDE: Message
        public override string Message => GetMessage();

        // Get Message with Parameters
        private string GetMessage()
        {
            if (!ParameterList.Any())
            {
                return GetMessageWithoutParameter();
            }
            else
            {
                return GetMessageWithoutParameter() + Environment.NewLine + "- " + string.Join(Environment.NewLine + "- ", ParameterList);
            }
        }

        // Get Message without Parameters
        protected virtual string GetMessageWithoutParameter()
        {
            return base.Message;
        }

        // Get Full-Message
        public string GetFullMessage()
        {
            // Deklarationen
            var sb = new StringBuilder();

            // Fehler-Meldung hinzufügen
            sb.AppendLine(Message);

            // Alle inneren Fehler ebenfalls hinzufügen
            var innerError = InnerException;
            while (innerError != null)
            {
                sb.AppendLine();
                sb.AppendLine(innerError.Message);
                innerError = innerError.InnerException;
            }

            // Rückgabe
            return sb.ToString().Trim();
        }

        // Get Full-Message (14.11.2022, SRM)
        //public string GetFullMessage()
        //{
        //    // create string-builder
        //    var sb = new StringBuilder();

        //    // add error-type and -message
        //    sb.AppendLine(this.GetType().ToString() + ":");
        //    sb.AppendLine(base.Message);

        //    // add method
        //    sb.AppendLine();
        //    sb.AppendLine("Method:");
        //    sb.AppendLine(this.Method.ToString());

        //    // add inner errors
        //    var innerError = this.InnerException;
        //    while (innerError != null)
        //    {
        //        sb.AppendLine();
        //        sb.AppendLine(innerError.GetType().ToString() + ":");
        //        sb.AppendLine(innerError.Message);
        //        innerError = innerError.InnerException;
        //    }

        //    // add stack-trace
        //    sb.AppendLine();
        //    sb.AppendLine("Stack-Trace:");
        //    sb.AppendLine(this.StackTrace);

        //    // return
        //    return sb.ToString().TrimEnd();
        //}

        #endregion

        #region Parameter-Handling

        // Parameters
        private readonly List<ClsNamedParameter> ParameterList = new();
        public ClsNamedParameter[] Parameters => ParameterList.ToArray();

        // Add Parameter
        protected void AddParameter(ClsNamedParameter parameter)
        {
            if (parameter != null)
            {
                ParameterList.Add(parameter);
            }
        }

        // Add Parameter-Range
        protected void AddParameterRange(IEnumerable<ClsNamedParameter> parameterRange)
        {
            if (parameterRange != null && parameterRange.Any())
            {
                foreach (var parameter in parameterRange)
                {
                    AddParameter(parameter);
                }
            }
        }

        #endregion
    }

    #region Core-Error mit Error-Code-Enum

    public class CoreError<TErrorCodeEnum> : CoreError, IErrorWithErrorCode where TErrorCodeEnum : struct
    {
        #region Neue Instanz

        // Neue leere Instanz
        protected CoreError(TErrorCodeEnum errorCode) : base() { SetErrorCode(errorCode); }

        // Neue Instanz mit Inner-Exception
        protected CoreError(TErrorCodeEnum errorCode, Exception innerException) : base(innerException) { SetErrorCode(errorCode); }

        // Neue Instanz mit Message
        public CoreError(TErrorCodeEnum errorCode, string message) : base(message) { SetErrorCode(errorCode); }

        // Neue Instanz mit Message und Inner-Exception
        public CoreError(TErrorCodeEnum errorCode, string message, Exception innerException) : base(message, innerException) { SetErrorCode(errorCode); }

        #endregion

        #region IErrorWithErrorCode-Implementation

        // Error-Code (08.04.2024, SME)
        public object ErrorCode => ErrorCodeEnum;

        #endregion

        #region Error-Code

        // Error-Code
        public TErrorCodeEnum ErrorCodeEnum { get; private set; }

        // Set Error-Code
        private void SetErrorCode(TErrorCodeEnum errorCode)
        {
            this.ErrorCodeEnum = errorCode;
            base.AddParameter(new("Error-Code-Enum", errorCode.ToString()));
        }

        #endregion
    }

    #endregion
}
