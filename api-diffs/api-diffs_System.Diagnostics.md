# System.Diagnostics

``` diff
 namespace System.Diagnostics {
-    public static class DiagnosticListenerExtensions {
 {
-        public static IDisposable SubscribeWithAdapter(this DiagnosticListener diagnostic, object target);

-        public static IDisposable SubscribeWithAdapter(this DiagnosticListener diagnostic, object target, Func<string, bool> isEnabled);

-        public static IDisposable SubscribeWithAdapter(this DiagnosticListener diagnostic, object target, Func<string, object, object, bool> isEnabled);

-    }
+    public class EntryWrittenEventArgs : EventArgs {
+        public EntryWrittenEventArgs();
+        public EntryWrittenEventArgs(EventLogEntry entry);
+        public EventLogEntry Entry { get; }
+    }
+    public delegate void EntryWrittenEventHandler(object sender, EntryWrittenEventArgs e);
+    public class EventInstance {
+        public EventInstance(long instanceId, int categoryId);
+        public EventInstance(long instanceId, int categoryId, EventLogEntryType entryType);
+        public int CategoryId { get; set; }
+        public EventLogEntryType EntryType { get; set; }
+        public long InstanceId { get; set; }
+    }
+    public class EventLog : Component, ISupportInitialize {
+        public EventLog();
+        public EventLog(string logName);
+        public EventLog(string logName, string machineName);
+        public EventLog(string logName, string machineName, string source);
+        public bool EnableRaisingEvents { get; set; }
+        public EventLogEntryCollection Entries { get; }
+        public string Log { get; set; }
+        public string LogDisplayName { get; }
+        public string MachineName { get; set; }
+        public long MaximumKilobytes { get; set; }
+        public int MinimumRetentionDays { get; }
+        public OverflowAction OverflowAction { get; }
+        public string Source { get; set; }
+        public ISynchronizeInvoke SynchronizingObject { get; set; }
+        public event EntryWrittenEventHandler EntryWritten;
+        public void BeginInit();
+        public void Clear();
+        public void Close();
+        public static void CreateEventSource(EventSourceCreationData sourceData);
+        public static void CreateEventSource(string source, string logName);
+        public static void CreateEventSource(string source, string logName, string machineName);
+        public static void Delete(string logName);
+        public static void Delete(string logName, string machineName);
+        public static void DeleteEventSource(string source);
+        public static void DeleteEventSource(string source, string machineName);
+        protected override void Dispose(bool disposing);
+        public void EndInit();
+        public static bool Exists(string logName);
+        public static bool Exists(string logName, string machineName);
+        public static EventLog[] GetEventLogs();
+        public static EventLog[] GetEventLogs(string machineName);
+        public static string LogNameFromSourceName(string source, string machineName);
+        public void ModifyOverflowPolicy(OverflowAction action, int retentionDays);
+        public void RegisterDisplayName(string resourceFile, long resourceId);
+        public static bool SourceExists(string source);
+        public static bool SourceExists(string source, string machineName);
+        public void WriteEntry(string message);
+        public void WriteEntry(string message, EventLogEntryType type);
+        public void WriteEntry(string message, EventLogEntryType type, int eventID);
+        public void WriteEntry(string message, EventLogEntryType type, int eventID, short category);
+        public void WriteEntry(string message, EventLogEntryType type, int eventID, short category, byte[] rawData);
+        public static void WriteEntry(string source, string message);
+        public static void WriteEntry(string source, string message, EventLogEntryType type);
+        public static void WriteEntry(string source, string message, EventLogEntryType type, int eventID);
+        public static void WriteEntry(string source, string message, EventLogEntryType type, int eventID, short category);
+        public static void WriteEntry(string source, string message, EventLogEntryType type, int eventID, short category, byte[] rawData);
+        public void WriteEvent(EventInstance instance, byte[] data, params object[] values);
+        public void WriteEvent(EventInstance instance, params object[] values);
+        public static void WriteEvent(string source, EventInstance instance, byte[] data, params object[] values);
+        public static void WriteEvent(string source, EventInstance instance, params object[] values);
+    }
+    public sealed class EventLogEntry : Component, ISerializable {
+        public string Category { get; }
+        public short CategoryNumber { get; }
+        public byte[] Data { get; }
+        public EventLogEntryType EntryType { get; }
+        public int EventID { get; }
+        public int Index { get; }
+        public long InstanceId { get; }
+        public string MachineName { get; }
+        public string Message { get; }
+        public string[] ReplacementStrings { get; }
+        public string Source { get; }
+        public DateTime TimeGenerated { get; }
+        public DateTime TimeWritten { get; }
+        public string UserName { get; }
+        public bool Equals(EventLogEntry otherEntry);
+        void System.Runtime.Serialization.ISerializable.GetObjectData(SerializationInfo info, StreamingContext context);
+    }
+    public class EventLogEntryCollection : ICollection, IEnumerable {
+        public int Count { get; }
+        bool System.Collections.ICollection.IsSynchronized { get; }
+        object System.Collections.ICollection.SyncRoot { get; }
+        public virtual EventLogEntry this[int index] { get; }
+        public void CopyTo(EventLogEntry[] entries, int index);
+        public IEnumerator GetEnumerator();
+        void System.Collections.ICollection.CopyTo(Array array, int index);
+    }
+    public enum EventLogEntryType {
+        Error = 1,
+        FailureAudit = 16,
+        Information = 4,
+        SuccessAudit = 8,
+        Warning = 2,
+    }
     public sealed class EventLogPermission : ResourcePermissionBase {
         public EventLogPermission();
         public EventLogPermission(EventLogPermissionAccess permissionAccess, string machineName);
         public EventLogPermission(EventLogPermissionEntry[] permissionAccessEntries);
         public EventLogPermission(PermissionState state);
         public EventLogPermissionEntryCollection PermissionEntries { get; }
     }
     public enum EventLogPermissionAccess {
         Administer = 48,
         Audit = 10,
         Browse = 2,
         Instrument = 6,
         None = 0,
         Write = 16,
     }
     public class EventLogPermissionAttribute : CodeAccessSecurityAttribute {
         public EventLogPermissionAttribute(SecurityAction action);
         public string MachineName { get; set; }
         public EventLogPermissionAccess PermissionAccess { get; set; }
         public override IPermission CreatePermission();
     }
     public class EventLogPermissionEntry {
         public EventLogPermissionEntry(EventLogPermissionAccess permissionAccess, string machineName);
         public string MachineName { get; }
         public EventLogPermissionAccess PermissionAccess { get; }
     }
     public class EventLogPermissionEntryCollection : CollectionBase {
         public EventLogPermissionEntry this[int index] { get; set; }
         public int Add(EventLogPermissionEntry value);
         public void AddRange(EventLogPermissionEntryCollection value);
         public void AddRange(EventLogPermissionEntry[] value);
         public bool Contains(EventLogPermissionEntry value);
         public void CopyTo(EventLogPermissionEntry[] array, int index);
         public int IndexOf(EventLogPermissionEntry value);
         public void Insert(int index, EventLogPermissionEntry value);
         protected override void OnClear();
         protected override void OnInsert(int index, object value);
         protected override void OnRemove(int index, object value);
         protected override void OnSet(int index, object oldValue, object newValue);
         public void Remove(EventLogPermissionEntry value);
     }
+    public sealed class EventLogTraceListener : TraceListener {
+        public EventLogTraceListener();
+        public EventLogTraceListener(EventLog eventLog);
+        public EventLogTraceListener(string source);
+        public EventLog EventLog { get; set; }
+        public override string Name { get; set; }
+        public override void Close();
+        protected override void Dispose(bool disposing);
+        public override void TraceData(TraceEventCache eventCache, string source, TraceEventType severity, int id, object data);
+        public override void TraceData(TraceEventCache eventCache, string source, TraceEventType severity, int id, params object[] data);
+        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType severity, int id, string message);
+        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType severity, int id, string format, params object[] args);
+        public override void Write(string message);
+        public override void WriteLine(string message);
+    }
+    public class EventSourceCreationData {
+        public EventSourceCreationData(string source, string logName);
+        public int CategoryCount { get; set; }
+        public string CategoryResourceFile { get; set; }
+        public string LogName { get; set; }
+        public string MachineName { get; set; }
+        public string MessageResourceFile { get; set; }
+        public string ParameterResourceFile { get; set; }
+        public string Source { get; set; }
+    }
+    public enum OverflowAction {
+        DoNotOverwrite = -1,
+        OverwriteAsNeeded = 0,
+        OverwriteOlder = 1,
+    }
     public sealed class PerformanceCounterPermission : ResourcePermissionBase {
         public PerformanceCounterPermission();
         public PerformanceCounterPermission(PerformanceCounterPermissionAccess permissionAccess, string machineName, string categoryName);
         public PerformanceCounterPermission(PerformanceCounterPermissionEntry[] permissionAccessEntries);
         public PerformanceCounterPermission(PermissionState state);
         public PerformanceCounterPermissionEntryCollection PermissionEntries { get; }
     }
     public enum PerformanceCounterPermissionAccess {
         Administer = 7,
         Browse = 1,
         Instrument = 3,
         None = 0,
         Read = 1,
         Write = 2,
     }
     public class PerformanceCounterPermissionAttribute : CodeAccessSecurityAttribute {
         public PerformanceCounterPermissionAttribute(SecurityAction action);
         public string CategoryName { get; set; }
         public string MachineName { get; set; }
         public PerformanceCounterPermissionAccess PermissionAccess { get; set; }
         public override IPermission CreatePermission();
     }
     public class PerformanceCounterPermissionEntry {
         public PerformanceCounterPermissionEntry(PerformanceCounterPermissionAccess permissionAccess, string machineName, string categoryName);
         public string CategoryName { get; }
         public string MachineName { get; }
         public PerformanceCounterPermissionAccess PermissionAccess { get; }
     }
     public class PerformanceCounterPermissionEntryCollection : CollectionBase {
         public PerformanceCounterPermissionEntry this[int index] { get; set; }
         public int Add(PerformanceCounterPermissionEntry value);
         public void AddRange(PerformanceCounterPermissionEntryCollection value);
         public void AddRange(PerformanceCounterPermissionEntry[] value);
         public bool Contains(PerformanceCounterPermissionEntry value);
         public void CopyTo(PerformanceCounterPermissionEntry[] array, int index);
         public int IndexOf(PerformanceCounterPermissionEntry value);
         public void Insert(int index, PerformanceCounterPermissionEntry value);
         protected override void OnClear();
         protected override void OnInsert(int index, object value);
         protected override void OnRemove(int index, object value);
         protected override void OnSet(int index, object oldValue, object newValue);
         public void Remove(PerformanceCounterPermissionEntry value);
     }
 }
```

