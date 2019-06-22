# System.Security

``` diff
 namespace System.Security {
     public abstract class CodeAccessPermission : IPermission, ISecurityEncodable, IStackWalk {
         protected CodeAccessPermission();
         public void Assert();
         public abstract IPermission Copy();
         public void Demand();
         public void Deny();
         public override bool Equals(object obj);
         public abstract void FromXml(SecurityElement elem);
         public override int GetHashCode();
         public abstract IPermission Intersect(IPermission target);
         public abstract bool IsSubsetOf(IPermission target);
         public void PermitOnly();
         public static void RevertAll();
         public static void RevertAssert();
         public static void RevertDeny();
         public static void RevertPermitOnly();
         public override string ToString();
         public abstract SecurityElement ToXml();
         public virtual IPermission Union(IPermission other);
     }
     public class HostProtectionException : SystemException {
         public HostProtectionException();
         protected HostProtectionException(SerializationInfo info, StreamingContext context);
         public HostProtectionException(string message);
         public HostProtectionException(string message, Exception e);
         public HostProtectionException(string message, HostProtectionResource protectedResources, HostProtectionResource demandedResources);
         public HostProtectionResource DemandedResources { get; }
         public HostProtectionResource ProtectedResources { get; }
-        public override void GetObjectData(SerializationInfo info, StreamingContext context);

         public override string ToString();
     }
     public class HostSecurityManager {
         public HostSecurityManager();
         public virtual PolicyLevel DomainPolicy { get; }
         public virtual HostSecurityManagerOptions Flags { get; }
         public virtual ApplicationTrust DetermineApplicationTrust(Evidence applicationEvidence, Evidence activatorEvidence, TrustManagerContext context);
         public virtual EvidenceBase GenerateAppDomainEvidence(Type evidenceType);
         public virtual EvidenceBase GenerateAssemblyEvidence(Type evidenceType, Assembly assembly);
         public virtual Type[] GetHostSuppliedAppDomainEvidenceTypes();
         public virtual Type[] GetHostSuppliedAssemblyEvidenceTypes(Assembly assembly);
         public virtual Evidence ProvideAppDomainEvidence(Evidence inputEvidence);
         public virtual Evidence ProvideAssemblyEvidence(Assembly loadedAssembly, Evidence inputEvidence);
         public virtual PermissionSet ResolvePolicy(Evidence evidence);
     }
     public enum HostSecurityManagerOptions {
         AllFlags = 31,
         HostAppDomainEvidence = 1,
         HostAssemblyEvidence = 4,
         HostDetermineApplicationTrust = 8,
         HostPolicyLevel = 2,
         HostResolvePolicy = 16,
         None = 0,
     }
     public interface IEvidenceFactory {
         Evidence Evidence { get; }
     }
     public interface ISecurityPolicyEncodable {
         void FromXml(SecurityElement e, PolicyLevel level);
         SecurityElement ToXml(PolicyLevel level);
     }
-    public interface IStackWalk {
 {
-        void Assert();

-        void Demand();

-        void Deny();

-        void PermitOnly();

-    }
     public sealed class NamedPermissionSet : PermissionSet {
         public NamedPermissionSet(NamedPermissionSet permSet);
         public NamedPermissionSet(string name);
         public NamedPermissionSet(string name, PermissionState state);
         public NamedPermissionSet(string name, PermissionSet permSet);
         public string Description { get; set; }
         public string Name { get; set; }
         public override PermissionSet Copy();
         public NamedPermissionSet Copy(string name);
         public override bool Equals(object o);
         public override void FromXml(SecurityElement et);
         public override int GetHashCode();
         public override SecurityElement ToXml();
     }
-    public class PermissionSet : ICollection, IDeserializationCallback, IEnumerable, ISecurityEncodable, IStackWalk {
 {
-        public PermissionSet(PermissionState state);

-        public PermissionSet(PermissionSet permSet);

-        public virtual int Count { get; }

-        public virtual bool IsReadOnly { get; }

-        public virtual bool IsSynchronized { get; }

-        public virtual object SyncRoot { get; }

-        public IPermission AddPermission(IPermission perm);

-        protected virtual IPermission AddPermissionImpl(IPermission perm);

-        public void Assert();

-        public bool ContainsNonCodeAccessPermissions();

-        public static byte[] ConvertPermissionSet(string inFormat, byte[] inData, string outFormat);

-        public virtual PermissionSet Copy();

-        public virtual void CopyTo(Array array, int index);

-        public void Demand();

-        public void Deny();

-        public override bool Equals(object o);

-        public virtual void FromXml(SecurityElement et);

-        public IEnumerator GetEnumerator();

-        protected virtual IEnumerator GetEnumeratorImpl();

-        public override int GetHashCode();

-        public IPermission GetPermission(Type permClass);

-        protected virtual IPermission GetPermissionImpl(Type permClass);

-        public PermissionSet Intersect(PermissionSet other);

-        public bool IsEmpty();

-        public bool IsSubsetOf(PermissionSet target);

-        public bool IsUnrestricted();

-        public void PermitOnly();

-        public IPermission RemovePermission(Type permClass);

-        protected virtual IPermission RemovePermissionImpl(Type permClass);

-        public static void RevertAssert();

-        public IPermission SetPermission(IPermission perm);

-        protected virtual IPermission SetPermissionImpl(IPermission perm);

-        void System.Runtime.Serialization.IDeserializationCallback.OnDeserialization(object sender);

-        public override string ToString();

-        public virtual SecurityElement ToXml();

-        public PermissionSet Union(PermissionSet other);

-    }
     public enum PolicyLevelType {
         AppDomain = 3,
         Enterprise = 2,
         Machine = 1,
         User = 0,
     }
     public sealed class SecurityContext : IDisposable {
         public static SecurityContext Capture();
         public SecurityContext CreateCopy();
         public void Dispose();
         public static bool IsFlowSuppressed();
         public static bool IsWindowsIdentityFlowSuppressed();
         public static void RestoreFlow();
         public static void Run(SecurityContext securityContext, ContextCallback callback, object state);
         public static AsyncFlowControl SuppressFlow();
         public static AsyncFlowControl SuppressFlowWindowsIdentity();
     }
     public enum SecurityContextSource {
         CurrentAppDomain = 0,
         CurrentAssembly = 1,
     }
     public static class SecurityManager {
         public static bool CheckExecutionRights { get; set; }
         public static bool SecurityEnabled { get; set; }
         public static bool CurrentThreadRequiresSecurityContextCapture();
         public static PermissionSet GetStandardSandbox(Evidence evidence);
         public static void GetZoneAndOrigin(out ArrayList zone, out ArrayList origin);
         public static bool IsGranted(IPermission perm);
         public static PolicyLevel LoadPolicyLevelFromFile(string path, PolicyLevelType type);
         public static PolicyLevel LoadPolicyLevelFromString(string str, PolicyLevelType type);
         public static IEnumerator PolicyHierarchy();
         public static PermissionSet ResolvePolicy(Evidence evidence);
         public static PermissionSet ResolvePolicy(Evidence evidence, PermissionSet reqdPset, PermissionSet optPset, PermissionSet denyPset, out PermissionSet denied);
         public static PermissionSet ResolvePolicy(Evidence[] evidences);
         public static IEnumerator ResolvePolicyGroups(Evidence evidence);
         public static PermissionSet ResolveSystemPolicy(Evidence evidence);
         public static void SavePolicy();
         public static void SavePolicyLevel(PolicyLevel level);
     }
     public abstract class SecurityState {
         protected SecurityState();
         public abstract void EnsureState();
         public bool IsStateAvailable();
     }
     public enum SecurityZone {
         Internet = 3,
         Intranet = 1,
         MyComputer = 0,
         NoZone = -1,
         Trusted = 2,
         Untrusted = 4,
     }
     public sealed class XmlSyntaxException : SystemException {
         public XmlSyntaxException();
         public XmlSyntaxException(int lineNumber);
         public XmlSyntaxException(int lineNumber, string message);
         public XmlSyntaxException(string message);
         public XmlSyntaxException(string message, Exception inner);
     }
 }
```

