# System.Diagnostics.Eventing.Reader

``` diff
+namespace System.Diagnostics.Eventing.Reader {
+    public class EventBookmark
+    public sealed class EventKeyword {
+        public string DisplayName { get; }
+        public string Name { get; }
+        public long Value { get; }
+    }
+    public sealed class EventLevel {
+        public string DisplayName { get; }
+        public string Name { get; }
+        public int Value { get; }
+    }
+    public class EventLogConfiguration : IDisposable {
+        public EventLogConfiguration(string logName);
+        public EventLogConfiguration(string logName, EventLogSession session);
+        public bool IsClassicLog { get; }
+        public bool IsEnabled { get; set; }
+        public string LogFilePath { get; set; }
+        public EventLogIsolation LogIsolation { get; }
+        public EventLogMode LogMode { get; set; }
+        public string LogName { get; }
+        public EventLogType LogType { get; }
+        public long MaximumSizeInBytes { get; set; }
+        public string OwningProviderName { get; }
+        public Nullable<int> ProviderBufferSize { get; }
+        public Nullable<Guid> ProviderControlGuid { get; }
+        public Nullable<long> ProviderKeywords { get; set; }
+        public Nullable<int> ProviderLatency { get; }
+        public Nullable<int> ProviderLevel { get; set; }
+        public Nullable<int> ProviderMaximumNumberOfBuffers { get; }
+        public Nullable<int> ProviderMinimumNumberOfBuffers { get; }
+        public IEnumerable<string> ProviderNames { get; }
+        public string SecurityDescriptor { get; set; }
+        public void Dispose();
+        protected virtual void Dispose(bool disposing);
+        public void SaveChanges();
+    }
+    public class EventLogException : Exception {
+        public EventLogException();
+        protected EventLogException(int errorCode);
+        public EventLogException(string message);
+        public EventLogException(string message, Exception innerException);
+        public override string Message { get; }
+    }
+    public sealed class EventLogInformation {
+        public Nullable<int> Attributes { get; }
+        public Nullable<DateTime> CreationTime { get; }
+        public Nullable<long> FileSize { get; }
+        public Nullable<bool> IsLogFull { get; }
+        public Nullable<DateTime> LastAccessTime { get; }
+        public Nullable<DateTime> LastWriteTime { get; }
+        public Nullable<long> OldestRecordNumber { get; }
+        public Nullable<long> RecordCount { get; }
+    }
+    public class EventLogInvalidDataException : EventLogException {
+        public EventLogInvalidDataException();
+        public EventLogInvalidDataException(string message);
+        public EventLogInvalidDataException(string message, Exception innerException);
+    }
+    public enum EventLogIsolation {
+        Application = 0,
+        Custom = 2,
+        System = 1,
+    }
+    public sealed class EventLogLink {
+        public string DisplayName { get; }
+        public bool IsImported { get; }
+        public string LogName { get; }
+    }
+    public enum EventLogMode {
+        AutoBackup = 1,
+        Circular = 0,
+        Retain = 2,
+    }
+    public class EventLogNotFoundException : EventLogException {
+        public EventLogNotFoundException();
+        public EventLogNotFoundException(string message);
+        public EventLogNotFoundException(string message, Exception innerException);
+    }
+    public class EventLogPropertySelector : IDisposable {
+        public EventLogPropertySelector(IEnumerable<string> propertyQueries);
+        public void Dispose();
+        protected virtual void Dispose(bool disposing);
+    }
+    public class EventLogProviderDisabledException : EventLogException {
+        public EventLogProviderDisabledException();
+        public EventLogProviderDisabledException(string message);
+        public EventLogProviderDisabledException(string message, Exception innerException);
+    }
+    public class EventLogQuery {
+        public EventLogQuery(string path, PathType pathType);
+        public EventLogQuery(string path, PathType pathType, string query);
+        public bool ReverseDirection { get; set; }
+        public EventLogSession Session { get; set; }
+        public bool TolerateQueryErrors { get; set; }
+    }
+    public class EventLogReader : IDisposable {
+        public EventLogReader(EventLogQuery eventQuery);
+        public EventLogReader(EventLogQuery eventQuery, EventBookmark bookmark);
+        public EventLogReader(string path);
+        public EventLogReader(string path, PathType pathType);
+        public int BatchSize { get; set; }
+        public IList<EventLogStatus> LogStatus { get; }
+        public void CancelReading();
+        public void Dispose();
+        protected virtual void Dispose(bool disposing);
+        public EventRecord ReadEvent();
+        public EventRecord ReadEvent(TimeSpan timeout);
+        public void Seek(EventBookmark bookmark);
+        public void Seek(EventBookmark bookmark, long offset);
+        public void Seek(SeekOrigin origin, long offset);
+    }
+    public class EventLogReadingException : EventLogException {
+        public EventLogReadingException();
+        public EventLogReadingException(string message);
+        public EventLogReadingException(string message, Exception innerException);
+    }
+    public class EventLogRecord : EventRecord {
+        public override Nullable<Guid> ActivityId { get; }
+        public override EventBookmark Bookmark { get; }
+        public string ContainerLog { get; }
+        public override int Id { get; }
+        public override Nullable<long> Keywords { get; }
+        public override IEnumerable<string> KeywordsDisplayNames { get; }
+        public override Nullable<byte> Level { get; }
+        public override string LevelDisplayName { get; }
+        public override string LogName { get; }
+        public override string MachineName { get; }
+        public IEnumerable<int> MatchedQueryIds { get; }
+        public override Nullable<short> Opcode { get; }
+        public override string OpcodeDisplayName { get; }
+        public override Nullable<int> ProcessId { get; }
+        public override IList<EventProperty> Properties { get; }
+        public override Nullable<Guid> ProviderId { get; }
+        public override string ProviderName { get; }
+        public override Nullable<int> Qualifiers { get; }
+        public override Nullable<long> RecordId { get; }
+        public override Nullable<Guid> RelatedActivityId { get; }
+        public override Nullable<int> Task { get; }
+        public override string TaskDisplayName { get; }
+        public override Nullable<int> ThreadId { get; }
+        public override Nullable<DateTime> TimeCreated { get; }
+        public override SecurityIdentifier UserId { get; }
+        public override Nullable<byte> Version { get; }
+        protected override void Dispose(bool disposing);
+        public override string FormatDescription();
+        public override string FormatDescription(IEnumerable<object> values);
+        public IList<object> GetPropertyValues(EventLogPropertySelector propertySelector);
+        public override string ToXml();
+    }
+    public class EventLogSession : IDisposable {
+        public EventLogSession();
+        public EventLogSession(string server);
+        public EventLogSession(string server, string domain, string user, SecureString password, SessionAuthentication logOnType);
+        public static EventLogSession GlobalSession { get; }
+        public void CancelCurrentOperations();
+        public void ClearLog(string logName);
+        public void ClearLog(string logName, string backupPath);
+        public void Dispose();
+        protected virtual void Dispose(bool disposing);
+        public void ExportLog(string path, PathType pathType, string query, string targetFilePath);
+        public void ExportLog(string path, PathType pathType, string query, string targetFilePath, bool tolerateQueryErrors);
+        public void ExportLogAndMessages(string path, PathType pathType, string query, string targetFilePath);
+        public void ExportLogAndMessages(string path, PathType pathType, string query, string targetFilePath, bool tolerateQueryErrors, CultureInfo targetCultureInfo);
+        public EventLogInformation GetLogInformation(string logName, PathType pathType);
+        public IEnumerable<string> GetLogNames();
+        public IEnumerable<string> GetProviderNames();
+    }
+    public sealed class EventLogStatus {
+        public string LogName { get; }
+        public int StatusCode { get; }
+    }
+    public enum EventLogType {
+        Administrative = 0,
+        Analytical = 2,
+        Debug = 3,
+        Operational = 1,
+    }
+    public class EventLogWatcher : IDisposable {
+        public EventLogWatcher(EventLogQuery eventQuery);
+        public EventLogWatcher(EventLogQuery eventQuery, EventBookmark bookmark);
+        public EventLogWatcher(EventLogQuery eventQuery, EventBookmark bookmark, bool readExistingEvents);
+        public EventLogWatcher(string path);
+        public bool Enabled { get; set; }
+        public event EventHandler<EventRecordWrittenEventArgs> EventRecordWritten;
+        public void Dispose();
+        protected virtual void Dispose(bool disposing);
+    }
+    public sealed class EventMetadata {
+        public string Description { get; }
+        public long Id { get; }
+        public IEnumerable<EventKeyword> Keywords { get; }
+        public EventLevel Level { get; }
+        public EventLogLink LogLink { get; }
+        public EventOpcode Opcode { get; }
+        public EventTask Task { get; }
+        public string Template { get; }
+        public byte Version { get; }
+    }
+    public sealed class EventOpcode {
+        public string DisplayName { get; }
+        public string Name { get; }
+        public int Value { get; }
+    }
+    public sealed class EventProperty {
+        public object Value { get; }
+    }
+    public abstract class EventRecord : IDisposable {
+        protected EventRecord();
+        public abstract Nullable<Guid> ActivityId { get; }
+        public abstract EventBookmark Bookmark { get; }
+        public abstract int Id { get; }
+        public abstract Nullable<long> Keywords { get; }
+        public abstract IEnumerable<string> KeywordsDisplayNames { get; }
+        public abstract Nullable<byte> Level { get; }
+        public abstract string LevelDisplayName { get; }
+        public abstract string LogName { get; }
+        public abstract string MachineName { get; }
+        public abstract Nullable<short> Opcode { get; }
+        public abstract string OpcodeDisplayName { get; }
+        public abstract Nullable<int> ProcessId { get; }
+        public abstract IList<EventProperty> Properties { get; }
+        public abstract Nullable<Guid> ProviderId { get; }
+        public abstract string ProviderName { get; }
+        public abstract Nullable<int> Qualifiers { get; }
+        public abstract Nullable<long> RecordId { get; }
+        public abstract Nullable<Guid> RelatedActivityId { get; }
+        public abstract Nullable<int> Task { get; }
+        public abstract string TaskDisplayName { get; }
+        public abstract Nullable<int> ThreadId { get; }
+        public abstract Nullable<DateTime> TimeCreated { get; }
+        public abstract SecurityIdentifier UserId { get; }
+        public abstract Nullable<byte> Version { get; }
+        public void Dispose();
+        protected virtual void Dispose(bool disposing);
+        public abstract string FormatDescription();
+        public abstract string FormatDescription(IEnumerable<object> values);
+        public abstract string ToXml();
+    }
+    public sealed class EventRecordWrittenEventArgs : EventArgs {
+        public Exception EventException { get; }
+        public EventRecord EventRecord { get; }
+    }
+    public sealed class EventTask {
+        public string DisplayName { get; }
+        public Guid EventGuid { get; }
+        public string Name { get; }
+        public int Value { get; }
+    }
+    public enum PathType {
+        FilePath = 2,
+        LogName = 1,
+    }
+    public class ProviderMetadata : IDisposable {
+        public ProviderMetadata(string providerName);
+        public ProviderMetadata(string providerName, EventLogSession session, CultureInfo targetCultureInfo);
+        public string DisplayName { get; }
+        public IEnumerable<EventMetadata> Events { get; }
+        public Uri HelpLink { get; }
+        public Guid Id { get; }
+        public IList<EventKeyword> Keywords { get; }
+        public IList<EventLevel> Levels { get; }
+        public IList<EventLogLink> LogLinks { get; }
+        public string MessageFilePath { get; }
+        public string Name { get; }
+        public IList<EventOpcode> Opcodes { get; }
+        public string ParameterFilePath { get; }
+        public string ResourceFilePath { get; }
+        public IList<EventTask> Tasks { get; }
+        public void Dispose();
+        protected virtual void Dispose(bool disposing);
+    }
+    public enum SessionAuthentication {
+        Default = 0,
+        Kerberos = 2,
+        Negotiate = 1,
+        Ntlm = 3,
+    }
+    public enum StandardEventKeywords : long {
+        AuditFailure = (long)4503599627370496,
+        AuditSuccess = (long)9007199254740992,
+        CorrelationHint = (long)4503599627370496,
+        CorrelationHint2 = (long)18014398509481984,
+        EventLogClassic = (long)36028797018963968,
+        None = (long)0,
+        ResponseTime = (long)281474976710656,
+        Sqm = (long)2251799813685248,
+        WdiContext = (long)562949953421312,
+        WdiDiagnostic = (long)1125899906842624,
+    }
+    public enum StandardEventLevel {
+        Critical = 1,
+        Error = 2,
+        Informational = 4,
+        LogAlways = 0,
+        Verbose = 5,
+        Warning = 3,
+    }
+    public enum StandardEventOpcode {
+        DataCollectionStart = 3,
+        DataCollectionStop = 4,
+        Extension = 5,
+        Info = 0,
+        Receive = 240,
+        Reply = 6,
+        Resume = 7,
+        Send = 9,
+        Start = 1,
+        Stop = 2,
+        Suspend = 8,
+    }
+    public enum StandardEventTask {
+        None = 0,
+    }
+}
```

