# System.ServiceProcess

``` diff
 namespace System.ServiceProcess {
     public sealed class ServiceControllerPermission : ResourcePermissionBase {
         public ServiceControllerPermission();
         public ServiceControllerPermission(PermissionState state);
         public ServiceControllerPermission(ServiceControllerPermissionAccess permissionAccess, string machineName, string serviceName);
         public ServiceControllerPermission(ServiceControllerPermissionEntry[] permissionAccessEntries);
         public ServiceControllerPermissionEntryCollection PermissionEntries { get; }
     }
     public enum ServiceControllerPermissionAccess {
         Browse = 2,
         Control = 6,
         None = 0,
     }
     public class ServiceControllerPermissionAttribute : CodeAccessSecurityAttribute {
         public ServiceControllerPermissionAttribute(SecurityAction action);
         public string MachineName { get; set; }
         public ServiceControllerPermissionAccess PermissionAccess { get; set; }
         public string ServiceName { get; set; }
         public override IPermission CreatePermission();
     }
     public class ServiceControllerPermissionEntry {
         public ServiceControllerPermissionEntry();
         public ServiceControllerPermissionEntry(ServiceControllerPermissionAccess permissionAccess, string machineName, string serviceName);
         public string MachineName { get; }
         public ServiceControllerPermissionAccess PermissionAccess { get; }
         public string ServiceName { get; }
     }
-    public class ServiceControllerPermissionEntryCollection : CollectionBase {
+    public sealed class ServiceControllerPermissionEntryCollection : CollectionBase {
         public ServiceControllerPermissionEntry this[int index] { get; set; }
         public int Add(ServiceControllerPermissionEntry value);
         public void AddRange(ServiceControllerPermissionEntryCollection value);
         public void AddRange(ServiceControllerPermissionEntry[] value);
         public bool Contains(ServiceControllerPermissionEntry value);
         public void CopyTo(ServiceControllerPermissionEntry[] array, int index);
         public int IndexOf(ServiceControllerPermissionEntry value);
         public void Insert(int index, ServiceControllerPermissionEntry value);
         protected override void OnClear();
         protected override void OnInsert(int index, object value);
         protected override void OnRemove(int index, object value);
         protected override void OnSet(int index, object oldValue, object newValue);
         public void Remove(ServiceControllerPermissionEntry value);
     }
 }
```

