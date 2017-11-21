using Microsoft.Owin.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.Services.Protocols;

namespace SP365.AddIn.Services
{
    /// <summary>
    /// Class Logger
    /// </summary>
    public static class Logger
    {
        #region Constructors

        /// <summary>
        /// The lock object
        /// </summary>
        private static object _loggerConstructorLock = new object();
        /// <summary>
        /// Initializes static members of the <see cref="Logger"/> class.
        /// </summary>
        static Logger()
        {
            lock (_loggerConstructorLock)
            {
                try
                {
                    if (EventLog.SourceExists("Application") == false)
                    {
                        try { EventLog.CreateEventSource("Application", "SP365"); }
                        catch
                        {
                            try
                            {
                                //SPSecurity.RunWithElevatedPrivileges(delegate
                                //{
                                    EventLog.CreateEventSource("Application", "SP365");
                                //});
                            }
                            catch (Exception) { }
                        }
                    }
                }
                catch (Exception) { }
            }
        }

        #endregion Constructors

        #region Properties

        private static ILogger _appLogger = null;
        private static ILogger AppLogger { get { if (_appLogger == null) { _appLogger = LoggerFactory.Default.Create("SP365.AddIn.Services"); } return _appLogger; } }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Logs a Verbose message.
        /// </summary>
        public static void Verbose()
        {
            Logger.Verbose((Exception)null, LogCategory.None, null);
        }
        /// <summary>
        /// Logs a Verbose message.
        /// </summary>
        /// <param name="category">The category.</param>
        public static void Verbose(LogCategory category)
        {
            Logger.Verbose((Exception)null, category, string.Format("Currently in method \"{0}\"", getCurrentMethod()));
        }
        /// <summary>
        /// Logs a Verbose message.
        /// </summary>
        /// <param name="message">The message.</param>
        public static void Verbose(string message)
        {
            Logger.Verbose((Exception)null, LogCategory.None, message);
        }
        /// <summary>
        /// Logs a Verbose message.
        /// </summary>
        /// <param name="category">The category.</param>
        /// <param name="message">The message.</param>
        public static void Verbose(LogCategory category, string message)
        {
            Logger.Verbose((Exception)null, category, message);
        }
        /// <summary>
        /// Logs a Verbose message.
        /// </summary>
        /// <param name="exception">The exception.</param>
        public static void Verbose(Exception exception)
        {
            Logger.Verbose(exception, LogCategory.None, null);
        }
        /// <summary>
        /// Logs a Verbose message.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <param name="message">The message.</param>
        public static void Verbose(Exception exception, string message)
        {
            Logger.Verbose(exception, LogCategory.None, message);
        }
        /// <summary>
        /// Logs a Verbose message.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <param name="category">The category.</param>
        public static void Verbose(Exception exception, LogCategory category)
        {
            Logger.Verbose(exception, category, null);
        }
        /// <summary>
        /// Logs a Verbose message.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <param name="category">The category.</param>
        /// <param name="message">The message.</param>
        public static void Verbose(Exception exception, LogCategory category, string message)
        {
            string msg = FormatMessage(exception, message);
            // 
            //DiagnosticsLogger.WriteEvent(category, DiagnosticsLogger.DefaultMessageID, EventSeverity.Verbose, msg, exception);
            //DiagnosticsLogger.WriteTrace(category, DiagnosticsLogger.DefaultMessageID, TraceSeverity.Verbose, msg, exception);
            AppLogger.WriteVerbose(msg);
#if DEBUG
            writeToVisualStudioDebugOutput(msg, "VERBOSE");
#endif
        }
        // 
        /// <summary>
        /// Debugs this instance.
        /// </summary>
        public static void Debug()
        {
            Logger.Debug((Exception)null, LogCategory.None, null);
        }
        /// <summary>
        /// Debugs this instance.
        /// </summary>
        /// <param name="category">The category.</param>
        public static void Debug(LogCategory category)
        {
            string message = string.Format("Currently in method \"{0}\"", getCurrentMethod());
            Logger.Debug((Exception)null, category, message);
        }
        /// <summary>
        /// Debugs the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        public static void Debug(string message)
        {
            Logger.Debug((Exception)null, LogCategory.None, message);
        }
        /// <summary>
        /// Debugs the specified category.
        /// </summary>
        /// <param name="category">The category.</param>
        /// <param name="message">The message.</param>
        public static void Debug(LogCategory category, string message)
        {
            Logger.Debug((Exception)null, category, message);
        }
        /// <summary>
        /// Debugs the specified exception.
        /// </summary>
        /// <param name="exception">The exception.</param>
        public static void Debug(Exception exception)
        {
            Logger.Debug(exception, LogCategory.None, null);
        }
        /// <summary>
        /// Debugs the specified exception.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <param name="message">The message.</param>
        public static void Debug(Exception exception, string message)
        {
            Logger.Debug(exception, LogCategory.None, message);
        }
        /// <summary>
        /// Debugs the specified exception.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <param name="category">The category.</param>
        public static void Debug(Exception exception, LogCategory category)
        {
            Logger.Debug(exception, category, null);
        }
        /// <summary>
        /// Debugs the specified exception.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <param name="category">The category.</param>
        /// <param name="message">The message.</param>
        public static void Debug(Exception exception, LogCategory category, string message)
        {
            string msg = FormatMessage(exception, message);
            // 
            //DiagnosticsLogger.WriteEvent(category, DiagnosticsLogger.DefaultMessageID, EventSeverity.Information, msg, exception);
            //DiagnosticsLogger.WriteTrace(category, DiagnosticsLogger.DefaultMessageID, TraceSeverity.Monitorable, msg, exception);
            AppLogger.WriteInformation(msg);
#if DEBUG
            writeToVisualStudioDebugOutput(msg, "DEBUG");
#endif
        }
        // 
        /// <summary>
        /// Warnings the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        public static void Warning(string message)
        {
            Logger.Warning((Exception)null, LogCategory.None, message);
        }
        /// <summary>
        /// Warnings the specified category.
        /// </summary>
        /// <param name="category">The category.</param>
        /// <param name="message">The message.</param>
        public static void Warning(LogCategory category, string message)
        {
            Logger.Warning((Exception)null, category, message);
        }
        /// <summary>
        /// Warnings the specified exception.
        /// </summary>
        /// <param name="exception">The exception.</param>
        public static void Warning(Exception exception)
        {
            Logger.Warning(exception, LogCategory.None, null);
        }
        /// <summary>
        /// Warnings the specified exception.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <param name="message">The message.</param>
        public static void Warning(Exception exception, string message)
        {
            Logger.Warning(exception, LogCategory.None, message);
        }
        /// <summary>
        /// Warnings the specified exception.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <param name="category">The category.</param>
        public static void Warning(Exception exception, LogCategory category)
        {
            Logger.Warning(exception, category, null);
        }
        /// <summary>
        /// Warnings the specified exception.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <param name="category">The category.</param>
        /// <param name="message">The message.</param>
        public static void Warning(Exception exception, LogCategory category, string message)
        {
            string msg = FormatMessage(exception, message);
            // 
            //DiagnosticsLogger.WriteEvent(category, DiagnosticsLogger.DefaultMessageID, EventSeverity.Warning, msg, exception);
            //DiagnosticsLogger.WriteTrace(category, DiagnosticsLogger.DefaultMessageID, TraceSeverity.Medium, msg, exception);
            AppLogger.WriteWarning(msg, exception);
#if DEBUG
            writeToVisualStudioDebugOutput(msg, "WARNING");
#endif
        }
        // 
        /// <summary>
        /// Errors the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        public static void Error(string message)
        {
            Logger.Error((Exception)null, LogCategory.None, message);
        }
        /// <summary>
        /// Errors the specified category.
        /// </summary>
        /// <param name="category">The category.</param>
        /// <param name="message">The message.</param>
        public static void Error(LogCategory category, string message)
        {
            Logger.Error((Exception)null, category, message);
        }
        /// <summary>
        /// Errors the specified exception.
        /// </summary>
        /// <param name="exception">The exception.</param>
        public static void Error(Exception exception)
        {
            Logger.Error(exception, LogCategory.None, null);
        }
        /// <summary>
        /// Errors the specified exception.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <param name="message">The message.</param>
        public static void Error(Exception exception, string message)
        {
            Logger.Error(exception, LogCategory.None, message);
        }
        /// <summary>
        /// Errors the specified exception.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <param name="category">The category.</param>
        public static void Error(Exception exception, LogCategory category)
        {
            Logger.Error(exception, category, null);
        }
        /// <summary>
        /// Errors the specified exception.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <param name="category">The category.</param>
        /// <param name="message">The message.</param>
        public static void Error(Exception exception, LogCategory category, string message)
        {
            string msg = FormatMessage(exception, message);
            // 
            //DiagnosticsLogger.WriteEvent(category, DiagnosticsLogger.DefaultMessageID, EventSeverity.ErrorCritical, msg, exception);
            //DiagnosticsLogger.WriteTrace(category, DiagnosticsLogger.DefaultMessageID, TraceSeverity.High, msg, exception);
            AppLogger.WriteError(msg, exception);
#if DEBUG
            writeToVisualStudioDebugOutput(msg, "ERROR");
#endif
        }
        // 
        /// <summary>
        /// Errors the and email admin.
        /// </summary>
        /// <param name="web">The web.</param>
        /// <param name="message">The message.</param>
        public static void CriticalError(string message)
        {
            Logger.CriticalError((Exception)null, LogCategory.None, message);
        }
        /// <summary>
        /// Errors the and email admin.
        /// </summary>
        /// <param name="web">The web.</param>
        /// <param name="category">The category.</param>
        /// <param name="message">The message.</param>
        public static void CriticalError(LogCategory category, string message)
        {
            Logger.CriticalError((Exception)null, category, message);
        }
        /// <summary>
        /// Errors the and email admin.
        /// </summary>
        /// <param name="web">The web.</param>
        /// <param name="exception">The exception.</param>
        public static void CriticalError(Exception exception)
        {
            Logger.CriticalError(exception, LogCategory.None, null);
        }
        /// <summary>
        /// Errors the and email admin.
        /// </summary>
        /// <param name="web">The web.</param>
        /// <param name="exception">The exception.</param>
        /// <param name="message">The message.</param>
        public static void CriticalError(Exception exception, string message)
        {
            Logger.CriticalError(exception, LogCategory.None, message);
        }
        /// <summary>
        /// Errors the and email admin.
        /// </summary>
        /// <param name="web">The web.</param>
        /// <param name="exception">The exception.</param>
        /// <param name="category">The category.</param>
        public static void CriticalError(Exception exception, LogCategory category)
        {
            Logger.CriticalError(exception, category, null);
        }
        /// <summary>
        /// Errors the and email admin.
        /// </summary>
        /// <param name="web">The web.</param>
        /// <param name="exception">The exception.</param>
        /// <param name="category">The category.</param>
        /// <param name="message">The message.</param>
        public static void CriticalError(Exception exception, LogCategory category, string message)
        {
            string msg = FormatMessage(exception, message);
            // 
            //DiagnosticsLogger.WriteEvent(category, DiagnosticsLogger.DefaultMessageID, EventSeverity.ErrorCritical, msg, exception);
            //DiagnosticsLogger.WriteTrace(category, DiagnosticsLogger.DefaultMessageID, TraceSeverity.High, msg, exception);
            AppLogger.WriteCritical(msg, exception);
#if DEBUG
            writeToVisualStudioDebugOutput(msg, "ERROR");
#endif
        }

        #endregion Methods

        #region Message Formating Methods

        /// <summary>
        /// Formats the message.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        /// <returns>System.String.</returns>
        public static string FormatMessage(string msg)
        {
            return FormatMessage(null, msg);
        }
        /// <summary>
        /// Formats the message.
        /// </summary>
        /// <param name="environment">The environment.</param>
        /// <param name="ex">The ex.</param>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        /// <returns>System.String.</returns>
        public static string FormatMessage(Exception ex, string msg)
        {
            string ret = msg;
            // 
            if (ex != null) { ret += Environment.NewLine + FormatException(ex); }
            // 
            return ret;
        }
        /// <summary>
        /// Formats the exception.
        /// </summary>
        /// <param name="ex">The ex.</param>
        /// <returns>System.String.</returns>
        public static string FormatException(Exception ex)
        {
            return formatExceptionAux(ex, 1);
        }
        /// <summary>
        /// Formats the exception aux.
        /// </summary>
        /// <param name="ex">The ex.</param>
        /// <param name="indentation">The indentation.</param>
        /// <returns>System.String.</returns>
        private static string formatExceptionAux(Exception ex, int indentation)
        {
            string ind = new string('#', indentation);
            StringBuilder str = new StringBuilder();
            // 
            // Known Exception types
            System.Data.Entity.Validation.DbEntityValidationException dbEx = (ex as System.Data.Entity.Validation.DbEntityValidationException);
            if (dbEx != null)
            {
                string dbErrorMessages = string.Join($@";{Environment.NewLine}", dbEx.EntityValidationErrors.Select(_ => $@"Entity:{_.Entry.Entity}. Errors:{string.Join(", ", _.ValidationErrors.SelectMany(_2 => $@"[{_2.PropertyName}]: {_2.ErrorMessage}"))}"));
                str.AppendLine($@"Validation errors: {dbErrorMessages}");
            }
            // 
            str.AppendLine(string.Format(@"{0}{1}: {2}", ind, ex.GetType().FullName, ex.Message));
            str.AppendLine(string.Format(@"{0}{1}: {2}", ind, ex.GetType().FullName, ex.Message));
            str.AppendLine(string.Format(@"{0}Stack: {1}", ind, ex.StackTrace));
            if (ex.Data != null && ex.Data.Count > 0)
            {
                int i = 0;
                string[] keyDataPairs = new string[ex.Data.Count];
                foreach (object key in ex.Data.Keys)
                {
                    object val = ex.Data[key];
                    keyDataPairs[i++] = string.Format("{0}:{1}", key, val);
                }
                str.AppendLine(string.Format(@"{0}Data: {{{1}}}", ind, string.Join(",", keyDataPairs)));
            }
            SoapException sEx = (ex as SoapException);
            if (sEx != null && sEx.Detail != null) { str.AppendLine(string.Format(@"{0}DetailXml: {1}", ind, sEx.Detail.OuterXml)); }
            if (ex.InnerException != null) { str.AppendLine(string.Format(@"{0}InnerException: {1}", ind, formatExceptionAux(ex.InnerException, indentation + 1))); }
            return str.ToString();
        }

        #endregion Message Formating Methods

        #region Auxiliaries

#if DEBUG
        /// <summary>
        /// Writes to visual studio debug output.
        /// </summary>
        /// <param name="msg">The MSG.</param>
        /// <param name="category">The category.</param>
        private static void writeToVisualStudioDebugOutput(string msg, string category)
        {
            System.Diagnostics.Debug.WriteLine(string.Format("[{0}] {1}", category, msg));
        }
#endif

        /// <summary>
        /// Gets the correlation id.
        /// </summary>
        /// <returns>System.Nullable{Guid}.</returns>
        internal static Guid? getCorrelationId()
        {
            Guid? ret = null;
            // 
            try
            {
                Type ulsType = Type.GetType("Microsoft.SharePoint.Diagnostics.ULS, Microsoft.SharePoint, Version=12.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c");
                MethodInfo miCorrelationGet = ulsType.GetMethod("CorrelationGet");
                Guid tmp = Guid.Empty;
                if (Guid.TryParse((miCorrelationGet.Invoke(null, null) ?? string.Empty).ToString(), out tmp) == true) { ret = tmp; }
            }
            catch (Exception ex) { Logger.Error(ex, LogCategory.Conversion, "Error when running the getCorrelationId()"); }
            if (ret == Guid.Empty) { ret = null; }
            // 
            return ret;
        }
        /// <summary>
        /// Gets the current method.
        /// </summary>
        /// <returns>System.String.</returns>
        private static string getCurrentMethod()
        {
            StackTrace stack = new StackTrace(2, false);
            MethodBase method = stack.GetFrame(0).GetMethod();
            return string.Format("{0}.{1}", method.DeclaringType.Name, method.Name);
        }

        #endregion Auxiliaries
    }
}
