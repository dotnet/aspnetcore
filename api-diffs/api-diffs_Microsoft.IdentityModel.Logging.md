# Microsoft.IdentityModel.Logging

``` diff
-namespace Microsoft.IdentityModel.Logging {
 {
-    public class IdentityModelEventSource : EventSource {
 {
-        public static bool HeaderWritten { get; set; }

-        public static string HiddenPIIString { get; }

-        public static IdentityModelEventSource Logger { get; }

-        public EventLevel LogLevel { get; set; }

-        public static bool ShowPII { get; set; }

-        public void Write(EventLevel level, Exception innerException, string message);

-        public void Write(EventLevel level, Exception innerException, string message, params object[] args);

-        public void WriteAlways(string message);

-        public void WriteAlways(string message, params object[] args);

-        public void WriteCritical(string message);

-        public void WriteCritical(string message, params object[] args);

-        public void WriteError(string message);

-        public void WriteError(string message, params object[] args);

-        public void WriteInformation(string message);

-        public void WriteInformation(string message, params object[] args);

-        public void WriteVerbose(string message);

-        public void WriteVerbose(string message, params object[] args);

-        public void WriteWarning(string message);

-        public void WriteWarning(string message, params object[] args);

-    }
-    public class LogHelper {
 {
-        public LogHelper();

-        public static string FormatInvariant(string format, params object[] args);

-        public static T LogArgumentException<T>(EventLevel eventLevel, string argumentName, Exception innerException, string message) where T : ArgumentException;

-        public static T LogArgumentException<T>(EventLevel eventLevel, string argumentName, Exception innerException, string format, params object[] args) where T : ArgumentException;

-        public static T LogArgumentException<T>(EventLevel eventLevel, string argumentName, string message) where T : ArgumentException;

-        public static T LogArgumentException<T>(EventLevel eventLevel, string argumentName, string format, params object[] args) where T : ArgumentException;

-        public static T LogArgumentException<T>(string argumentName, Exception innerException, string message) where T : ArgumentException;

-        public static T LogArgumentException<T>(string argumentName, Exception innerException, string format, params object[] args) where T : ArgumentException;

-        public static T LogArgumentException<T>(string argumentName, string message) where T : ArgumentException;

-        public static T LogArgumentException<T>(string argumentName, string format, params object[] args) where T : ArgumentException;

-        public static ArgumentNullException LogArgumentNullException(string argument);

-        public static T LogException<T>(EventLevel eventLevel, Exception innerException, string message) where T : Exception;

-        public static T LogException<T>(EventLevel eventLevel, Exception innerException, string format, params object[] args) where T : Exception;

-        public static T LogException<T>(EventLevel eventLevel, string message) where T : Exception;

-        public static T LogException<T>(EventLevel eventLevel, string format, params object[] args) where T : Exception;

-        public static T LogException<T>(Exception innerException, string message) where T : Exception;

-        public static T LogException<T>(Exception innerException, string format, params object[] args) where T : Exception;

-        public static T LogException<T>(string message) where T : Exception;

-        public static T LogException<T>(string format, params object[] args) where T : Exception;

-        public static Exception LogExceptionMessage(EventLevel eventLevel, Exception exception);

-        public static Exception LogExceptionMessage(Exception exception);

-        public static void LogInformation(string message, params object[] args);

-        public static void LogVerbose(string message, params object[] args);

-        public static void LogWarning(string message, params object[] args);

-    }
-    public class TextWriterEventListener : EventListener {
 {
-        public static readonly string DefaultLogFileName;

-        public TextWriterEventListener();

-        public TextWriterEventListener(StreamWriter streamWriter);

-        public TextWriterEventListener(string filePath);

-        public override void Dispose();

-        protected override void OnEventWritten(EventWrittenEventArgs eventData);

-    }
-}
```

