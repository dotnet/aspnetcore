# System.Security.Policy

``` diff
 namespace System.Security.Policy {
     public sealed class AllMembershipCondition : IMembershipCondition, ISecurityEncodable, ISecurityPolicyEncodable {
         public AllMembershipCondition();
         public bool Check(Evidence evidence);
         public IMembershipCondition Copy();
         public override bool Equals(object o);
         public void FromXml(SecurityElement e);
         public void FromXml(SecurityElement e, PolicyLevel level);
         public override int GetHashCode();
         public override string ToString();
         public SecurityElement ToXml();
         public SecurityElement ToXml(PolicyLevel level);
     }
     public sealed class ApplicationDirectory : EvidenceBase {
         public ApplicationDirectory(string name);
         public string Directory { get; }
         public object Copy();
         public override bool Equals(object o);
         public override int GetHashCode();
         public override string ToString();
     }
     public sealed class ApplicationDirectoryMembershipCondition : IMembershipCondition, ISecurityEncodable, ISecurityPolicyEncodable {
         public ApplicationDirectoryMembershipCondition();
         public bool Check(Evidence evidence);
         public IMembershipCondition Copy();
         public override bool Equals(object o);
         public void FromXml(SecurityElement e);
         public void FromXml(SecurityElement e, PolicyLevel level);
         public override int GetHashCode();
         public override string ToString();
         public SecurityElement ToXml();
         public SecurityElement ToXml(PolicyLevel level);
     }
     public sealed class ApplicationTrust : EvidenceBase, ISecurityEncodable {
         public ApplicationTrust();
         public ApplicationTrust(ApplicationIdentity identity);
         public ApplicationTrust(PermissionSet defaultGrantSet, IEnumerable<StrongName> fullTrustAssemblies);
         public ApplicationIdentity ApplicationIdentity { get; set; }
         public PolicyStatement DefaultGrantSet { get; set; }
         public object ExtraInfo { get; set; }
         public IList<StrongName> FullTrustAssemblies { get; }
         public bool IsApplicationTrustedToRun { get; set; }
         public bool Persist { get; set; }
         public void FromXml(SecurityElement element);
         public SecurityElement ToXml();
     }
     public sealed class ApplicationTrustCollection : ICollection, IEnumerable {
         public int Count { get; }
         public bool IsSynchronized { get; }
         public object SyncRoot { get; }
         public ApplicationTrust this[int index] { get; }
         public ApplicationTrust this[string appFullName] { get; }
         public int Add(ApplicationTrust trust);
         public void AddRange(ApplicationTrustCollection trusts);
         public void AddRange(ApplicationTrust[] trusts);
         public void Clear();
         public void CopyTo(ApplicationTrust[] array, int index);
         public ApplicationTrustCollection Find(ApplicationIdentity applicationIdentity, ApplicationVersionMatch versionMatch);
         public ApplicationTrustEnumerator GetEnumerator();
         public void Remove(ApplicationIdentity applicationIdentity, ApplicationVersionMatch versionMatch);
         public void Remove(ApplicationTrust trust);
         public void RemoveRange(ApplicationTrustCollection trusts);
         public void RemoveRange(ApplicationTrust[] trusts);
         void System.Collections.ICollection.CopyTo(Array array, int index);
         IEnumerator System.Collections.IEnumerable.GetEnumerator();
     }
     public sealed class ApplicationTrustEnumerator : IEnumerator {
         public ApplicationTrust Current { get; }
         object System.Collections.IEnumerator.Current { get; }
         public bool MoveNext();
         public void Reset();
     }
     public enum ApplicationVersionMatch {
         MatchAllVersions = 1,
         MatchExactVersion = 0,
     }
     public class CodeConnectAccess {
         public static readonly int DefaultPort;
         public static readonly int OriginPort;
         public static readonly string AnyScheme;
         public static readonly string OriginScheme;
         public CodeConnectAccess(string allowScheme, int allowPort);
         public int Port { get; }
         public string Scheme { get; }
         public static CodeConnectAccess CreateAnySchemeAccess(int allowPort);
         public static CodeConnectAccess CreateOriginSchemeAccess(int allowPort);
         public override bool Equals(object o);
         public override int GetHashCode();
     }
     public abstract class CodeGroup {
         protected CodeGroup(IMembershipCondition membershipCondition, PolicyStatement policy);
         public virtual string AttributeString { get; }
         public IList Children { get; set; }
         public string Description { get; set; }
         public IMembershipCondition MembershipCondition { get; set; }
         public abstract string MergeLogic { get; }
         public string Name { get; set; }
         public virtual string PermissionSetName { get; }
         public PolicyStatement PolicyStatement { get; set; }
         public void AddChild(CodeGroup group);
         public abstract CodeGroup Copy();
         protected virtual void CreateXml(SecurityElement element, PolicyLevel level);
         public override bool Equals(object o);
         public bool Equals(CodeGroup cg, bool compareChildren);
         public void FromXml(SecurityElement e);
         public void FromXml(SecurityElement e, PolicyLevel level);
         public override int GetHashCode();
         protected virtual void ParseXml(SecurityElement e, PolicyLevel level);
         public void RemoveChild(CodeGroup group);
         public abstract PolicyStatement Resolve(Evidence evidence);
         public abstract CodeGroup ResolveMatchingCodeGroups(Evidence evidence);
         public SecurityElement ToXml();
         public SecurityElement ToXml(PolicyLevel level);
     }
     public sealed class Evidence : ICollection, IEnumerable {
         public Evidence();
         public Evidence(object[] hostEvidence, object[] assemblyEvidence);
         public Evidence(Evidence evidence);
         public Evidence(EvidenceBase[] hostEvidence, EvidenceBase[] assemblyEvidence);
         public int Count { get; }
         public bool IsReadOnly { get; }
         public bool IsSynchronized { get; }
         public bool Locked { get; set; }
         public object SyncRoot { get; }
         public void AddAssembly(object id);
         public void AddAssemblyEvidence<T>(T evidence) where T : EvidenceBase;
         public void AddHost(object id);
         public void AddHostEvidence<T>(T evidence) where T : EvidenceBase;
         public void Clear();
         public Evidence Clone();
         public void CopyTo(Array array, int index);
         public IEnumerator GetAssemblyEnumerator();
         public T GetAssemblyEvidence<T>() where T : EvidenceBase;
         public IEnumerator GetEnumerator();
         public IEnumerator GetHostEnumerator();
         public T GetHostEvidence<T>() where T : EvidenceBase;
         public void Merge(Evidence evidence);
         public void RemoveType(Type t);
     }
     public abstract class EvidenceBase {
         protected EvidenceBase();
         public virtual EvidenceBase Clone();
     }
     public sealed class FileCodeGroup : CodeGroup {
         public FileCodeGroup(IMembershipCondition membershipCondition, FileIOPermissionAccess access);
         public override string AttributeString { get; }
         public override string MergeLogic { get; }
         public override string PermissionSetName { get; }
         public override CodeGroup Copy();
         protected override void CreateXml(SecurityElement element, PolicyLevel level);
         public override bool Equals(object o);
         public override int GetHashCode();
         protected override void ParseXml(SecurityElement e, PolicyLevel level);
         public override PolicyStatement Resolve(Evidence evidence);
         public override CodeGroup ResolveMatchingCodeGroups(Evidence evidence);
     }
     public sealed class FirstMatchCodeGroup : CodeGroup {
         public FirstMatchCodeGroup(IMembershipCondition membershipCondition, PolicyStatement policy);
         public override string MergeLogic { get; }
         public override CodeGroup Copy();
         public override PolicyStatement Resolve(Evidence evidence);
         public override CodeGroup ResolveMatchingCodeGroups(Evidence evidence);
     }
     public sealed class GacInstalled : EvidenceBase, IIdentityPermissionFactory {
         public GacInstalled();
         public object Copy();
         public IPermission CreateIdentityPermission(Evidence evidence);
         public override bool Equals(object o);
         public override int GetHashCode();
         public override string ToString();
     }
     public sealed class GacMembershipCondition : IMembershipCondition, ISecurityEncodable, ISecurityPolicyEncodable {
         public GacMembershipCondition();
         public bool Check(Evidence evidence);
         public IMembershipCondition Copy();
         public override bool Equals(object o);
         public void FromXml(SecurityElement e);
         public void FromXml(SecurityElement e, PolicyLevel level);
         public override int GetHashCode();
         public override string ToString();
         public SecurityElement ToXml();
         public SecurityElement ToXml(PolicyLevel level);
     }
     public sealed class Hash : EvidenceBase, ISerializable {
         public Hash(Assembly assembly);
         public byte[] MD5 { get; }
         public byte[] SHA1 { get; }
         public byte[] SHA256 { get; }
         public static Hash CreateMD5(byte[] md5);
         public static Hash CreateSHA1(byte[] sha1);
         public static Hash CreateSHA256(byte[] sha256);
         public byte[] GenerateHash(HashAlgorithm hashAlg);
         public void GetObjectData(SerializationInfo info, StreamingContext context);
         public override string ToString();
     }
     public sealed class HashMembershipCondition : IDeserializationCallback, IMembershipCondition, ISecurityEncodable, ISecurityPolicyEncodable, ISerializable {
         public HashMembershipCondition(HashAlgorithm hashAlg, byte[] value);
         public HashAlgorithm HashAlgorithm { get; set; }
         public byte[] HashValue { get; set; }
         public bool Check(Evidence evidence);
         public IMembershipCondition Copy();
         public override bool Equals(object o);
         public void FromXml(SecurityElement e);
         public void FromXml(SecurityElement e, PolicyLevel level);
         public override int GetHashCode();
         void System.Runtime.Serialization.IDeserializationCallback.OnDeserialization(object sender);
         void System.Runtime.Serialization.ISerializable.GetObjectData(SerializationInfo info, StreamingContext context);
         public override string ToString();
         public SecurityElement ToXml();
         public SecurityElement ToXml(PolicyLevel level);
     }
     public interface IIdentityPermissionFactory {
         IPermission CreateIdentityPermission(Evidence evidence);
     }
     public interface IMembershipCondition : ISecurityEncodable, ISecurityPolicyEncodable {
         bool Check(Evidence evidence);
         IMembershipCondition Copy();
         bool Equals(object obj);
         string ToString();
     }
     public sealed class NetCodeGroup : CodeGroup {
         public static readonly string AbsentOriginScheme;
         public static readonly string AnyOtherOriginScheme;
         public NetCodeGroup(IMembershipCondition membershipCondition);
         public override string AttributeString { get; }
         public override string MergeLogic { get; }
         public override string PermissionSetName { get; }
         public void AddConnectAccess(string originScheme, CodeConnectAccess connectAccess);
         public override CodeGroup Copy();
         protected override void CreateXml(SecurityElement element, PolicyLevel level);
         public override bool Equals(object o);
         public DictionaryEntry[] GetConnectAccessRules();
         public override int GetHashCode();
         protected override void ParseXml(SecurityElement e, PolicyLevel level);
         public void ResetConnectAccess();
         public override PolicyStatement Resolve(Evidence evidence);
         public override CodeGroup ResolveMatchingCodeGroups(Evidence evidence);
     }
     public sealed class PermissionRequestEvidence : EvidenceBase {
         public PermissionRequestEvidence(PermissionSet request, PermissionSet optional, PermissionSet denied);
         public PermissionSet DeniedPermissions { get; }
         public PermissionSet OptionalPermissions { get; }
         public PermissionSet RequestedPermissions { get; }
         public PermissionRequestEvidence Copy();
         public override string ToString();
     }
     public class PolicyException : SystemException {
         public PolicyException();
         protected PolicyException(SerializationInfo info, StreamingContext context);
         public PolicyException(string message);
         public PolicyException(string message, Exception exception);
     }
     public sealed class PolicyLevel {
         public IList FullTrustAssemblies { get; }
         public string Label { get; }
         public IList NamedPermissionSets { get; }
         public CodeGroup RootCodeGroup { get; set; }
         public string StoreLocation { get; }
         public PolicyLevelType Type { get; }
         public void AddFullTrustAssembly(StrongName sn);
         public void AddFullTrustAssembly(StrongNameMembershipCondition snMC);
         public void AddNamedPermissionSet(NamedPermissionSet permSet);
         public NamedPermissionSet ChangeNamedPermissionSet(string name, PermissionSet pSet);
         public static PolicyLevel CreateAppDomainLevel();
         public void FromXml(SecurityElement e);
         public NamedPermissionSet GetNamedPermissionSet(string name);
         public void Recover();
         public void RemoveFullTrustAssembly(StrongName sn);
         public void RemoveFullTrustAssembly(StrongNameMembershipCondition snMC);
         public NamedPermissionSet RemoveNamedPermissionSet(NamedPermissionSet permSet);
         public NamedPermissionSet RemoveNamedPermissionSet(string name);
         public void Reset();
         public PolicyStatement Resolve(Evidence evidence);
         public CodeGroup ResolveMatchingCodeGroups(Evidence evidence);
         public SecurityElement ToXml();
     }
     public sealed class PolicyStatement : ISecurityEncodable, ISecurityPolicyEncodable {
         public PolicyStatement(PermissionSet permSet);
         public PolicyStatement(PermissionSet permSet, PolicyStatementAttribute attributes);
         public PolicyStatementAttribute Attributes { get; set; }
         public string AttributeString { get; }
         public PermissionSet PermissionSet { get; set; }
         public PolicyStatement Copy();
         public override bool Equals(object o);
         public void FromXml(SecurityElement et);
         public void FromXml(SecurityElement et, PolicyLevel level);
         public override int GetHashCode();
         public SecurityElement ToXml();
         public SecurityElement ToXml(PolicyLevel level);
     }
     public enum PolicyStatementAttribute {
         All = 3,
         Exclusive = 1,
         LevelFinal = 2,
         Nothing = 0,
     }
     public sealed class Publisher : EvidenceBase, IIdentityPermissionFactory {
         public Publisher(X509Certificate cert);
         public X509Certificate Certificate { get; }
         public object Copy();
         public IPermission CreateIdentityPermission(Evidence evidence);
         public override bool Equals(object o);
         public override int GetHashCode();
         public override string ToString();
     }
     public sealed class PublisherMembershipCondition : IMembershipCondition, ISecurityEncodable, ISecurityPolicyEncodable {
         public PublisherMembershipCondition(X509Certificate certificate);
         public X509Certificate Certificate { get; set; }
         public bool Check(Evidence evidence);
         public IMembershipCondition Copy();
         public override bool Equals(object o);
         public void FromXml(SecurityElement e);
         public void FromXml(SecurityElement e, PolicyLevel level);
         public override int GetHashCode();
         public override string ToString();
         public SecurityElement ToXml();
         public SecurityElement ToXml(PolicyLevel level);
     }
     public sealed class Site : EvidenceBase, IIdentityPermissionFactory {
         public Site(string name);
         public string Name { get; }
         public object Copy();
         public static Site CreateFromUrl(string url);
         public IPermission CreateIdentityPermission(Evidence evidence);
         public override bool Equals(object o);
         public override int GetHashCode();
         public override string ToString();
     }
     public sealed class SiteMembershipCondition : IMembershipCondition, ISecurityEncodable, ISecurityPolicyEncodable {
         public SiteMembershipCondition(string site);
         public string Site { get; set; }
         public bool Check(Evidence evidence);
         public IMembershipCondition Copy();
         public override bool Equals(object o);
         public void FromXml(SecurityElement e);
         public void FromXml(SecurityElement e, PolicyLevel level);
         public override int GetHashCode();
         public override string ToString();
         public SecurityElement ToXml();
         public SecurityElement ToXml(PolicyLevel level);
     }
     public sealed class StrongName : EvidenceBase, IIdentityPermissionFactory {
         public StrongName(StrongNamePublicKeyBlob blob, string name, Version version);
         public string Name { get; }
         public StrongNamePublicKeyBlob PublicKey { get; }
         public Version Version { get; }
         public object Copy();
         public IPermission CreateIdentityPermission(Evidence evidence);
         public override bool Equals(object o);
         public override int GetHashCode();
         public override string ToString();
     }
     public sealed class StrongNameMembershipCondition : IMembershipCondition, ISecurityEncodable, ISecurityPolicyEncodable {
         public StrongNameMembershipCondition(StrongNamePublicKeyBlob blob, string name, Version version);
         public string Name { get; set; }
         public StrongNamePublicKeyBlob PublicKey { get; set; }
         public Version Version { get; set; }
         public bool Check(Evidence evidence);
         public IMembershipCondition Copy();
         public override bool Equals(object o);
         public void FromXml(SecurityElement e);
         public void FromXml(SecurityElement e, PolicyLevel level);
         public override int GetHashCode();
         public override string ToString();
         public SecurityElement ToXml();
         public SecurityElement ToXml(PolicyLevel level);
     }
     public class TrustManagerContext {
         public TrustManagerContext();
         public TrustManagerContext(TrustManagerUIContext uiContext);
         public virtual bool IgnorePersistedDecision { get; set; }
         public virtual bool KeepAlive { get; set; }
         public virtual bool NoPrompt { get; set; }
         public virtual bool Persist { get; set; }
         public virtual ApplicationIdentity PreviousApplicationIdentity { get; set; }
         public virtual TrustManagerUIContext UIContext { get; set; }
     }
     public enum TrustManagerUIContext {
         Install = 0,
         Run = 2,
         Upgrade = 1,
     }
     public sealed class UnionCodeGroup : CodeGroup {
         public UnionCodeGroup(IMembershipCondition membershipCondition, PolicyStatement policy);
         public override string MergeLogic { get; }
         public override CodeGroup Copy();
         public override PolicyStatement Resolve(Evidence evidence);
         public override CodeGroup ResolveMatchingCodeGroups(Evidence evidence);
     }
     public sealed class Url : EvidenceBase, IIdentityPermissionFactory {
         public Url(string name);
         public string Value { get; }
         public object Copy();
         public IPermission CreateIdentityPermission(Evidence evidence);
         public override bool Equals(object o);
         public override int GetHashCode();
         public override string ToString();
     }
     public sealed class UrlMembershipCondition : IMembershipCondition, ISecurityEncodable, ISecurityPolicyEncodable {
         public UrlMembershipCondition(string url);
         public string Url { get; set; }
         public bool Check(Evidence evidence);
         public IMembershipCondition Copy();
-        public override bool Equals(object obj);
+        public override bool Equals(object o);
         public void FromXml(SecurityElement e);
         public void FromXml(SecurityElement e, PolicyLevel level);
         public override int GetHashCode();
         public override string ToString();
         public SecurityElement ToXml();
         public SecurityElement ToXml(PolicyLevel level);
     }
     public sealed class Zone : EvidenceBase, IIdentityPermissionFactory {
         public Zone(SecurityZone zone);
         public SecurityZone SecurityZone { get; }
         public object Copy();
         public static Zone CreateFromUrl(string url);
         public IPermission CreateIdentityPermission(Evidence evidence);
         public override bool Equals(object o);
         public override int GetHashCode();
         public override string ToString();
     }
     public sealed class ZoneMembershipCondition : IMembershipCondition, ISecurityEncodable, ISecurityPolicyEncodable {
         public ZoneMembershipCondition(SecurityZone zone);
         public SecurityZone SecurityZone { get; set; }
         public bool Check(Evidence evidence);
         public IMembershipCondition Copy();
         public override bool Equals(object o);
         public void FromXml(SecurityElement e);
         public void FromXml(SecurityElement e, PolicyLevel level);
         public override int GetHashCode();
         public override string ToString();
         public SecurityElement ToXml();
         public SecurityElement ToXml(PolicyLevel level);
     }
 }
```

