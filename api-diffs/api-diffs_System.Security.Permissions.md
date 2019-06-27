# System.Security.Permissions

``` diff
 namespace System.Security.Permissions {
+    public sealed class DataProtectionPermission : CodeAccessPermission, IUnrestrictedPermission {
+        public DataProtectionPermission(DataProtectionPermissionFlags flag);
+        public DataProtectionPermission(PermissionState state);
+        public DataProtectionPermissionFlags Flags { get; set; }
+        public override IPermission Copy();
+        public override void FromXml(SecurityElement securityElement);
+        public override IPermission Intersect(IPermission target);
+        public override bool IsSubsetOf(IPermission target);
+        public bool IsUnrestricted();
+        public override SecurityElement ToXml();
+        public override IPermission Union(IPermission target);
+    }
+    public sealed class DataProtectionPermissionAttribute : CodeAccessSecurityAttribute {
+        public DataProtectionPermissionAttribute(SecurityAction action);
+        public DataProtectionPermissionFlags Flags { get; set; }
+        public bool ProtectData { get; set; }
+        public bool ProtectMemory { get; set; }
+        public bool UnprotectData { get; set; }
+        public bool UnprotectMemory { get; set; }
+        public override IPermission CreatePermission();
+    }
+    public enum DataProtectionPermissionFlags {
+        AllFlags = 15,
+        NoFlags = 0,
+        ProtectData = 1,
+        ProtectMemory = 4,
+        UnprotectData = 2,
+        UnprotectMemory = 8,
+    }
     public sealed class EnvironmentPermission : CodeAccessPermission, IUnrestrictedPermission {
         public EnvironmentPermission(EnvironmentPermissionAccess flag, string pathList);
         public EnvironmentPermission(PermissionState state);
         public void AddPathList(EnvironmentPermissionAccess flag, string pathList);
         public override IPermission Copy();
         public override void FromXml(SecurityElement esd);
         public string GetPathList(EnvironmentPermissionAccess flag);
         public override IPermission Intersect(IPermission target);
         public override bool IsSubsetOf(IPermission target);
         public bool IsUnrestricted();
         public void SetPathList(EnvironmentPermissionAccess flag, string pathList);
         public override SecurityElement ToXml();
         public override IPermission Union(IPermission other);
     }
     public enum EnvironmentPermissionAccess {
         AllAccess = 3,
         NoAccess = 0,
         Read = 1,
         Write = 2,
     }
     public sealed class EnvironmentPermissionAttribute : CodeAccessSecurityAttribute {
         public EnvironmentPermissionAttribute(SecurityAction action);
         public string All { get; set; }
         public string Read { get; set; }
         public string Write { get; set; }
         public override IPermission CreatePermission();
     }
     public sealed class FileDialogPermission : CodeAccessPermission, IUnrestrictedPermission {
         public FileDialogPermission(FileDialogPermissionAccess access);
         public FileDialogPermission(PermissionState state);
         public FileDialogPermissionAccess Access { get; set; }
         public override IPermission Copy();
         public override void FromXml(SecurityElement esd);
         public override IPermission Intersect(IPermission target);
         public override bool IsSubsetOf(IPermission target);
         public bool IsUnrestricted();
         public override SecurityElement ToXml();
         public override IPermission Union(IPermission target);
     }
     public enum FileDialogPermissionAccess {
         None = 0,
         Open = 1,
         OpenSave = 3,
         Save = 2,
     }
     public sealed class FileDialogPermissionAttribute : CodeAccessSecurityAttribute {
         public FileDialogPermissionAttribute(SecurityAction action);
         public bool Open { get; set; }
         public bool Save { get; set; }
         public override IPermission CreatePermission();
     }
     public sealed class FileIOPermission : CodeAccessPermission, IUnrestrictedPermission {
         public FileIOPermission(FileIOPermissionAccess access, AccessControlActions actions, string path);
         public FileIOPermission(FileIOPermissionAccess access, AccessControlActions actions, string[] pathList);
         public FileIOPermission(FileIOPermissionAccess access, string path);
         public FileIOPermission(FileIOPermissionAccess access, string[] pathList);
         public FileIOPermission(PermissionState state);
         public FileIOPermissionAccess AllFiles { get; set; }
         public FileIOPermissionAccess AllLocalFiles { get; set; }
         public void AddPathList(FileIOPermissionAccess access, string path);
         public void AddPathList(FileIOPermissionAccess access, string[] pathList);
         public override IPermission Copy();
         public override bool Equals(object o);
         public override void FromXml(SecurityElement esd);
         public override int GetHashCode();
         public string[] GetPathList(FileIOPermissionAccess access);
         public override IPermission Intersect(IPermission target);
         public override bool IsSubsetOf(IPermission target);
         public bool IsUnrestricted();
         public void SetPathList(FileIOPermissionAccess access, string path);
         public void SetPathList(FileIOPermissionAccess access, string[] pathList);
         public override SecurityElement ToXml();
         public override IPermission Union(IPermission other);
     }
     public enum FileIOPermissionAccess {
         AllAccess = 15,
         Append = 4,
         NoAccess = 0,
         PathDiscovery = 8,
         Read = 1,
         Write = 2,
     }
     public sealed class FileIOPermissionAttribute : CodeAccessSecurityAttribute {
         public FileIOPermissionAttribute(SecurityAction action);
         public string All { get; set; }
         public FileIOPermissionAccess AllFiles { get; set; }
         public FileIOPermissionAccess AllLocalFiles { get; set; }
         public string Append { get; set; }
         public string ChangeAccessControl { get; set; }
         public string PathDiscovery { get; set; }
         public string Read { get; set; }
         public string ViewAccessControl { get; set; }
         public string ViewAndModify { get; set; }
         public string Write { get; set; }
         public override IPermission CreatePermission();
     }
     public sealed class GacIdentityPermission : CodeAccessPermission {
         public GacIdentityPermission();
         public GacIdentityPermission(PermissionState state);
         public override IPermission Copy();
         public override void FromXml(SecurityElement securityElement);
         public override IPermission Intersect(IPermission target);
         public override bool IsSubsetOf(IPermission target);
         public override SecurityElement ToXml();
         public override IPermission Union(IPermission target);
     }
     public sealed class GacIdentityPermissionAttribute : CodeAccessSecurityAttribute {
         public GacIdentityPermissionAttribute(SecurityAction action);
         public override IPermission CreatePermission();
     }
     public sealed class HostProtectionAttribute : CodeAccessSecurityAttribute {
         public HostProtectionAttribute();
         public HostProtectionAttribute(SecurityAction action);
         public bool ExternalProcessMgmt { get; set; }
         public bool ExternalThreading { get; set; }
         public bool MayLeakOnAbort { get; set; }
         public HostProtectionResource Resources { get; set; }
         public bool SecurityInfrastructure { get; set; }
         public bool SelfAffectingProcessMgmt { get; set; }
         public bool SelfAffectingThreading { get; set; }
         public bool SharedState { get; set; }
         public bool Synchronization { get; set; }
         public bool UI { get; set; }
         public override IPermission CreatePermission();
     }
     public enum HostProtectionResource {
         All = 511,
         ExternalProcessMgmt = 4,
         ExternalThreading = 16,
         MayLeakOnAbort = 256,
         None = 0,
         SecurityInfrastructure = 64,
         SelfAffectingProcessMgmt = 8,
         SelfAffectingThreading = 32,
         SharedState = 2,
         Synchronization = 1,
         UI = 128,
     }
     public enum IsolatedStorageContainment {
         AdministerIsolatedStorageByUser = 112,
         ApplicationIsolationByMachine = 69,
         ApplicationIsolationByRoamingUser = 101,
         ApplicationIsolationByUser = 21,
         AssemblyIsolationByMachine = 64,
         AssemblyIsolationByRoamingUser = 96,
         AssemblyIsolationByUser = 32,
         DomainIsolationByMachine = 48,
         DomainIsolationByRoamingUser = 80,
         DomainIsolationByUser = 16,
         None = 0,
         UnrestrictedIsolatedStorage = 240,
     }
     public sealed class IsolatedStorageFilePermission : IsolatedStoragePermission {
         public IsolatedStorageFilePermission(PermissionState state);
         public override IPermission Copy();
         public override IPermission Intersect(IPermission target);
         public override bool IsSubsetOf(IPermission target);
         public override SecurityElement ToXml();
         public override IPermission Union(IPermission target);
     }
     public sealed class IsolatedStorageFilePermissionAttribute : IsolatedStoragePermissionAttribute {
         public IsolatedStorageFilePermissionAttribute(SecurityAction action);
         public override IPermission CreatePermission();
     }
     public abstract class IsolatedStoragePermission : CodeAccessPermission, IUnrestrictedPermission {
         protected IsolatedStoragePermission(PermissionState state);
         public IsolatedStorageContainment UsageAllowed { get; set; }
         public long UserQuota { get; set; }
         public override void FromXml(SecurityElement esd);
         public bool IsUnrestricted();
         public override SecurityElement ToXml();
     }
     public abstract class IsolatedStoragePermissionAttribute : CodeAccessSecurityAttribute {
         protected IsolatedStoragePermissionAttribute(SecurityAction action);
         public IsolatedStorageContainment UsageAllowed { get; set; }
         public long UserQuota { get; set; }
     }
     public interface IUnrestrictedPermission {
         bool IsUnrestricted();
     }
     public sealed class KeyContainerPermission : CodeAccessPermission, IUnrestrictedPermission {
         public KeyContainerPermission(KeyContainerPermissionFlags flags);
         public KeyContainerPermission(KeyContainerPermissionFlags flags, KeyContainerPermissionAccessEntry[] accessList);
         public KeyContainerPermission(PermissionState state);
         public KeyContainerPermissionAccessEntryCollection AccessEntries { get; }
         public KeyContainerPermissionFlags Flags { get; }
         public override IPermission Copy();
         public override void FromXml(SecurityElement securityElement);
         public override IPermission Intersect(IPermission target);
         public override bool IsSubsetOf(IPermission target);
         public bool IsUnrestricted();
         public override SecurityElement ToXml();
         public override IPermission Union(IPermission target);
     }
     public sealed class KeyContainerPermissionAccessEntry {
         public KeyContainerPermissionAccessEntry(CspParameters parameters, KeyContainerPermissionFlags flags);
         public KeyContainerPermissionAccessEntry(string keyContainerName, KeyContainerPermissionFlags flags);
         public KeyContainerPermissionAccessEntry(string keyStore, string providerName, int providerType, string keyContainerName, int keySpec, KeyContainerPermissionFlags flags);
         public KeyContainerPermissionFlags Flags { get; set; }
         public string KeyContainerName { get; set; }
         public int KeySpec { get; set; }
         public string KeyStore { get; set; }
         public string ProviderName { get; set; }
         public int ProviderType { get; set; }
         public override bool Equals(object o);
         public override int GetHashCode();
     }
     public sealed class KeyContainerPermissionAccessEntryCollection : ICollection, IEnumerable {
         public KeyContainerPermissionAccessEntryCollection();
         public int Count { get; }
         public bool IsSynchronized { get; }
         public object SyncRoot { get; }
         public KeyContainerPermissionAccessEntry this[int index] { get; }
         public int Add(KeyContainerPermissionAccessEntry accessEntry);
         public void Clear();
         public void CopyTo(Array array, int index);
         public void CopyTo(KeyContainerPermissionAccessEntry[] array, int index);
         public KeyContainerPermissionAccessEntryEnumerator GetEnumerator();
         public int IndexOf(KeyContainerPermissionAccessEntry accessEntry);
         public void Remove(KeyContainerPermissionAccessEntry accessEntry);
         IEnumerator System.Collections.IEnumerable.GetEnumerator();
     }
     public sealed class KeyContainerPermissionAccessEntryEnumerator : IEnumerator {
         public KeyContainerPermissionAccessEntryEnumerator();
         public KeyContainerPermissionAccessEntry Current { get; }
         object System.Collections.IEnumerator.Current { get; }
         public bool MoveNext();
         public void Reset();
     }
     public sealed class KeyContainerPermissionAttribute : CodeAccessSecurityAttribute {
         public KeyContainerPermissionAttribute(SecurityAction action);
         public KeyContainerPermissionFlags Flags { get; set; }
         public string KeyContainerName { get; set; }
         public int KeySpec { get; set; }
         public string KeyStore { get; set; }
         public string ProviderName { get; set; }
         public int ProviderType { get; set; }
         public override IPermission CreatePermission();
     }
     public enum KeyContainerPermissionFlags {
         AllFlags = 13111,
         ChangeAcl = 8192,
         Create = 1,
         Decrypt = 512,
         Delete = 4,
         Export = 32,
         Import = 16,
         NoFlags = 0,
         Open = 2,
         Sign = 256,
         ViewAcl = 4096,
     }
+    public sealed class MediaPermission : CodeAccessPermission, IUnrestrictedPermission {
+        public MediaPermission();
+        public MediaPermission(MediaPermissionAudio permissionAudio);
+        public MediaPermission(MediaPermissionAudio permissionAudio, MediaPermissionVideo permissionVideo, MediaPermissionImage permissionImage);
+        public MediaPermission(MediaPermissionImage permissionImage);
+        public MediaPermission(MediaPermissionVideo permissionVideo);
+        public MediaPermission(PermissionState state);
+        public MediaPermissionAudio Audio { get; }
+        public MediaPermissionImage Image { get; }
+        public MediaPermissionVideo Video { get; }
+        public override IPermission Copy();
+        public override void FromXml(SecurityElement securityElement);
+        public override IPermission Intersect(IPermission target);
+        public override bool IsSubsetOf(IPermission target);
+        public bool IsUnrestricted();
+        public override SecurityElement ToXml();
+        public override IPermission Union(IPermission target);
+    }
+    public sealed class MediaPermissionAttribute : CodeAccessSecurityAttribute {
+        public MediaPermissionAttribute(SecurityAction action);
+        public MediaPermissionAudio Audio { get; set; }
+        public MediaPermissionImage Image { get; set; }
+        public MediaPermissionVideo Video { get; set; }
+        public override IPermission CreatePermission();
+    }
+    public enum MediaPermissionAudio {
+        AllAudio = 3,
+        NoAudio = 0,
+        SafeAudio = 2,
+        SiteOfOriginAudio = 1,
+    }
+    public enum MediaPermissionImage {
+        AllImage = 3,
+        NoImage = 0,
+        SafeImage = 2,
+        SiteOfOriginImage = 1,
+    }
+    public enum MediaPermissionVideo {
+        AllVideo = 3,
+        NoVideo = 0,
+        SafeVideo = 2,
+        SiteOfOriginVideo = 1,
+    }
     public sealed class PermissionSetAttribute : CodeAccessSecurityAttribute {
         public PermissionSetAttribute(SecurityAction action);
         public string File { get; set; }
         public string Hex { get; set; }
         public string Name { get; set; }
         public bool UnicodeEncoded { get; set; }
         public string XML { get; set; }
         public override IPermission CreatePermission();
         public PermissionSet CreatePermissionSet();
     }
-    public enum PermissionState {
 {
-        None = 0,

-        Unrestricted = 1,

-    }
     public sealed class PrincipalPermission : IPermission, ISecurityEncodable, IUnrestrictedPermission {
         public PrincipalPermission(PermissionState state);
         public PrincipalPermission(string name, string role);
         public PrincipalPermission(string name, string role, bool isAuthenticated);
         public IPermission Copy();
         public void Demand();
-        public override bool Equals(object obj);
+        public override bool Equals(object o);
         public void FromXml(SecurityElement elem);
         public override int GetHashCode();
         public IPermission Intersect(IPermission target);
         public bool IsSubsetOf(IPermission target);
         public bool IsUnrestricted();
         public override string ToString();
         public SecurityElement ToXml();
         public IPermission Union(IPermission other);
     }
     public sealed class PrincipalPermissionAttribute : CodeAccessSecurityAttribute {
         public PrincipalPermissionAttribute(SecurityAction action);
         public bool Authenticated { get; set; }
         public string Name { get; set; }
         public string Role { get; set; }
         public override IPermission CreatePermission();
     }
     public sealed class PublisherIdentityPermission : CodeAccessPermission {
         public PublisherIdentityPermission(X509Certificate certificate);
         public PublisherIdentityPermission(PermissionState state);
         public X509Certificate Certificate { get; set; }
         public override IPermission Copy();
         public override void FromXml(SecurityElement esd);
         public override IPermission Intersect(IPermission target);
         public override bool IsSubsetOf(IPermission target);
         public override SecurityElement ToXml();
         public override IPermission Union(IPermission target);
     }
     public sealed class PublisherIdentityPermissionAttribute : CodeAccessSecurityAttribute {
         public PublisherIdentityPermissionAttribute(SecurityAction action);
         public string CertFile { get; set; }
         public string SignedFile { get; set; }
         public string X509Certificate { get; set; }
         public override IPermission CreatePermission();
     }
     public sealed class ReflectionPermission : CodeAccessPermission, IUnrestrictedPermission {
         public ReflectionPermission(PermissionState state);
         public ReflectionPermission(ReflectionPermissionFlag flag);
         public ReflectionPermissionFlag Flags { get; set; }
         public override IPermission Copy();
         public override void FromXml(SecurityElement esd);
         public override IPermission Intersect(IPermission target);
         public override bool IsSubsetOf(IPermission target);
         public bool IsUnrestricted();
         public override SecurityElement ToXml();
         public override IPermission Union(IPermission other);
     }
     public sealed class ReflectionPermissionAttribute : CodeAccessSecurityAttribute {
         public ReflectionPermissionAttribute(SecurityAction action);
         public ReflectionPermissionFlag Flags { get; set; }
         public bool MemberAccess { get; set; }
         public bool ReflectionEmit { get; set; }
         public bool RestrictedMemberAccess { get; set; }
         public bool TypeInformation { get; set; }
         public override IPermission CreatePermission();
     }
     public enum ReflectionPermissionFlag {
         AllFlags = 7,
         MemberAccess = 2,
         NoFlags = 0,
         ReflectionEmit = 4,
         RestrictedMemberAccess = 8,
         TypeInformation = 1,
     }
     public sealed class RegistryPermission : CodeAccessPermission, IUnrestrictedPermission {
         public RegistryPermission(PermissionState state);
         public RegistryPermission(RegistryPermissionAccess access, AccessControlActions control, string pathList);
         public RegistryPermission(RegistryPermissionAccess access, string pathList);
         public void AddPathList(RegistryPermissionAccess access, AccessControlActions actions, string pathList);
         public void AddPathList(RegistryPermissionAccess access, string pathList);
         public override IPermission Copy();
         public override void FromXml(SecurityElement elem);
         public string GetPathList(RegistryPermissionAccess access);
         public override IPermission Intersect(IPermission target);
         public override bool IsSubsetOf(IPermission target);
         public bool IsUnrestricted();
         public void SetPathList(RegistryPermissionAccess access, string pathList);
         public override SecurityElement ToXml();
         public override IPermission Union(IPermission other);
     }
     public enum RegistryPermissionAccess {
         AllAccess = 7,
         Create = 4,
         NoAccess = 0,
         Read = 1,
         Write = 2,
     }
     public sealed class RegistryPermissionAttribute : CodeAccessSecurityAttribute {
         public RegistryPermissionAttribute(SecurityAction action);
         public string All { get; set; }
         public string ChangeAccessControl { get; set; }
         public string Create { get; set; }
         public string Read { get; set; }
         public string ViewAccessControl { get; set; }
         public string ViewAndModify { get; set; }
         public string Write { get; set; }
         public override IPermission CreatePermission();
     }
     public abstract class ResourcePermissionBase : CodeAccessPermission, IUnrestrictedPermission {
         public const string Any = "*";
         public const string Local = ".";
         protected ResourcePermissionBase();
         protected ResourcePermissionBase(PermissionState state);
         protected Type PermissionAccessType { get; set; }
         protected string[] TagNames { get; set; }
         protected void AddPermissionAccess(ResourcePermissionBaseEntry entry);
         protected void Clear();
         public override IPermission Copy();
         public override void FromXml(SecurityElement securityElement);
         protected ResourcePermissionBaseEntry[] GetPermissionEntries();
         public override IPermission Intersect(IPermission target);
         public override bool IsSubsetOf(IPermission target);
         public bool IsUnrestricted();
         protected void RemovePermissionAccess(ResourcePermissionBaseEntry entry);
         public override SecurityElement ToXml();
         public override IPermission Union(IPermission target);
     }
     public class ResourcePermissionBaseEntry {
         public ResourcePermissionBaseEntry();
         public ResourcePermissionBaseEntry(int permissionAccess, string[] permissionAccessPath);
         public int PermissionAccess { get; }
         public string[] PermissionAccessPath { get; }
     }
     public sealed class SecurityPermission : CodeAccessPermission, IUnrestrictedPermission {
         public SecurityPermission(PermissionState state);
         public SecurityPermission(SecurityPermissionFlag flag);
         public SecurityPermissionFlag Flags { get; set; }
         public override IPermission Copy();
         public override void FromXml(SecurityElement esd);
         public override IPermission Intersect(IPermission target);
         public override bool IsSubsetOf(IPermission target);
         public bool IsUnrestricted();
         public override SecurityElement ToXml();
         public override IPermission Union(IPermission target);
     }
     public sealed class SiteIdentityPermission : CodeAccessPermission {
         public SiteIdentityPermission(PermissionState state);
         public SiteIdentityPermission(string site);
         public string Site { get; set; }
         public override IPermission Copy();
         public override void FromXml(SecurityElement esd);
         public override IPermission Intersect(IPermission target);
         public override bool IsSubsetOf(IPermission target);
         public override SecurityElement ToXml();
         public override IPermission Union(IPermission target);
     }
     public sealed class SiteIdentityPermissionAttribute : CodeAccessSecurityAttribute {
         public SiteIdentityPermissionAttribute(SecurityAction action);
         public string Site { get; set; }
         public override IPermission CreatePermission();
     }
     public sealed class StorePermission : CodeAccessPermission, IUnrestrictedPermission {
         public StorePermission(PermissionState state);
         public StorePermission(StorePermissionFlags flag);
         public StorePermissionFlags Flags { get; set; }
         public override IPermission Copy();
         public override void FromXml(SecurityElement securityElement);
         public override IPermission Intersect(IPermission target);
         public override bool IsSubsetOf(IPermission target);
         public bool IsUnrestricted();
         public override SecurityElement ToXml();
         public override IPermission Union(IPermission target);
     }
     public sealed class StorePermissionAttribute : CodeAccessSecurityAttribute {
         public StorePermissionAttribute(SecurityAction action);
         public bool AddToStore { get; set; }
         public bool CreateStore { get; set; }
         public bool DeleteStore { get; set; }
         public bool EnumerateCertificates { get; set; }
         public bool EnumerateStores { get; set; }
         public StorePermissionFlags Flags { get; set; }
         public bool OpenStore { get; set; }
         public bool RemoveFromStore { get; set; }
         public override IPermission CreatePermission();
     }
     public enum StorePermissionFlags {
         AddToStore = 32,
         AllFlags = 247,
         CreateStore = 1,
         DeleteStore = 2,
         EnumerateCertificates = 128,
         EnumerateStores = 4,
         NoFlags = 0,
         OpenStore = 16,
         RemoveFromStore = 64,
     }
     public sealed class StrongNameIdentityPermission : CodeAccessPermission {
         public StrongNameIdentityPermission(PermissionState state);
         public StrongNameIdentityPermission(StrongNamePublicKeyBlob blob, string name, Version version);
         public string Name { get; set; }
         public StrongNamePublicKeyBlob PublicKey { get; set; }
         public Version Version { get; set; }
         public override IPermission Copy();
         public override void FromXml(SecurityElement e);
         public override IPermission Intersect(IPermission target);
         public override bool IsSubsetOf(IPermission target);
         public override SecurityElement ToXml();
         public override IPermission Union(IPermission target);
     }
     public sealed class StrongNameIdentityPermissionAttribute : CodeAccessSecurityAttribute {
         public StrongNameIdentityPermissionAttribute(SecurityAction action);
         public string Name { get; set; }
         public string PublicKey { get; set; }
         public string Version { get; set; }
         public override IPermission CreatePermission();
     }
     public sealed class StrongNamePublicKeyBlob {
         public StrongNamePublicKeyBlob(byte[] publicKey);
         public override bool Equals(object o);
         public override int GetHashCode();
         public override string ToString();
     }
     public sealed class TypeDescriptorPermission : CodeAccessPermission, IUnrestrictedPermission {
         public TypeDescriptorPermission(PermissionState state);
         public TypeDescriptorPermission(TypeDescriptorPermissionFlags flag);
         public TypeDescriptorPermissionFlags Flags { get; set; }
         public override IPermission Copy();
         public override void FromXml(SecurityElement securityElement);
         public override IPermission Intersect(IPermission target);
         public override bool IsSubsetOf(IPermission target);
         public bool IsUnrestricted();
         public override SecurityElement ToXml();
         public override IPermission Union(IPermission target);
     }
     public sealed class TypeDescriptorPermissionAttribute : CodeAccessSecurityAttribute {
         public TypeDescriptorPermissionAttribute(SecurityAction action);
         public TypeDescriptorPermissionFlags Flags { get; set; }
         public bool RestrictedRegistrationAccess { get; set; }
         public override IPermission CreatePermission();
     }
     public enum TypeDescriptorPermissionFlags {
         NoFlags = 0,
         RestrictedRegistrationAccess = 1,
     }
     public sealed class UIPermission : CodeAccessPermission, IUnrestrictedPermission {
         public UIPermission(PermissionState state);
         public UIPermission(UIPermissionClipboard clipboardFlag);
         public UIPermission(UIPermissionWindow windowFlag);
         public UIPermission(UIPermissionWindow windowFlag, UIPermissionClipboard clipboardFlag);
         public UIPermissionClipboard Clipboard { get; set; }
         public UIPermissionWindow Window { get; set; }
         public override IPermission Copy();
         public override void FromXml(SecurityElement esd);
         public override IPermission Intersect(IPermission target);
         public override bool IsSubsetOf(IPermission target);
         public bool IsUnrestricted();
         public override SecurityElement ToXml();
         public override IPermission Union(IPermission target);
     }
     public sealed class UIPermissionAttribute : CodeAccessSecurityAttribute {
         public UIPermissionAttribute(SecurityAction action);
         public UIPermissionClipboard Clipboard { get; set; }
         public UIPermissionWindow Window { get; set; }
         public override IPermission CreatePermission();
     }
     public enum UIPermissionClipboard {
         AllClipboard = 2,
         NoClipboard = 0,
         OwnClipboard = 1,
     }
     public enum UIPermissionWindow {
         AllWindows = 3,
         NoWindows = 0,
         SafeSubWindows = 1,
         SafeTopLevelWindows = 2,
     }
     public sealed class UrlIdentityPermission : CodeAccessPermission {
         public UrlIdentityPermission(PermissionState state);
         public UrlIdentityPermission(string site);
         public string Url { get; set; }
         public override IPermission Copy();
         public override void FromXml(SecurityElement esd);
         public override IPermission Intersect(IPermission target);
         public override bool IsSubsetOf(IPermission target);
         public override SecurityElement ToXml();
         public override IPermission Union(IPermission target);
     }
     public sealed class UrlIdentityPermissionAttribute : CodeAccessSecurityAttribute {
         public UrlIdentityPermissionAttribute(SecurityAction action);
         public string Url { get; set; }
         public override IPermission CreatePermission();
     }
+    public sealed class WebBrowserPermission : CodeAccessPermission, IUnrestrictedPermission {
+        public WebBrowserPermission();
+        public WebBrowserPermission(PermissionState state);
+        public WebBrowserPermission(WebBrowserPermissionLevel webBrowserPermissionLevel);
+        public WebBrowserPermissionLevel Level { get; set; }
+        public override IPermission Copy();
+        public override void FromXml(SecurityElement securityElement);
+        public override IPermission Intersect(IPermission target);
+        public override bool IsSubsetOf(IPermission target);
+        public bool IsUnrestricted();
+        public override SecurityElement ToXml();
+        public override IPermission Union(IPermission target);
+    }
+    public sealed class WebBrowserPermissionAttribute : CodeAccessSecurityAttribute {
+        public WebBrowserPermissionAttribute(SecurityAction action);
+        public WebBrowserPermissionLevel Level { get; set; }
+        public override IPermission CreatePermission();
+    }
+    public enum WebBrowserPermissionLevel {
+        None = 0,
+        Safe = 1,
+        Unrestricted = 2,
+    }
     public sealed class ZoneIdentityPermission : CodeAccessPermission {
         public ZoneIdentityPermission(PermissionState state);
         public ZoneIdentityPermission(SecurityZone zone);
         public SecurityZone SecurityZone { get; set; }
         public override IPermission Copy();
         public override void FromXml(SecurityElement esd);
         public override IPermission Intersect(IPermission target);
         public override bool IsSubsetOf(IPermission target);
         public override SecurityElement ToXml();
         public override IPermission Union(IPermission target);
     }
     public sealed class ZoneIdentityPermissionAttribute : CodeAccessSecurityAttribute {
         public ZoneIdentityPermissionAttribute(SecurityAction action);
         public SecurityZone Zone { get; set; }
         public override IPermission CreatePermission();
     }
 }
```

