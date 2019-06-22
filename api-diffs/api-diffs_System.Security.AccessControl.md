# System.Security.AccessControl

``` diff
+namespace System.Security.AccessControl {
+    public enum AccessControlActions {
+        Change = 2,
+        None = 0,
+        View = 1,
+    }
+    public enum AccessControlModification {
+        Add = 0,
+        Remove = 3,
+        RemoveAll = 4,
+        RemoveSpecific = 5,
+        Reset = 2,
+        Set = 1,
+    }
+    public enum AccessControlSections {
+        Access = 2,
+        All = 15,
+        Audit = 1,
+        Group = 8,
+        None = 0,
+        Owner = 4,
+    }
+    public enum AccessControlType {
+        Allow = 0,
+        Deny = 1,
+    }
+    public abstract class AccessRule : AuthorizationRule {
+        protected AccessRule(IdentityReference identity, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type);
+        public AccessControlType AccessControlType { get; }
+    }
+    public class AccessRule<T> : AccessRule where T : struct, ValueType {
+        public AccessRule(IdentityReference identity, T rights, AccessControlType type);
+        public AccessRule(IdentityReference identity, T rights, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type);
+        public AccessRule(string identity, T rights, AccessControlType type);
+        public AccessRule(string identity, T rights, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type);
+        public T Rights { get; }
+    }
+    public sealed class AceEnumerator : IEnumerator {
+        public GenericAce Current { get; }
+        object System.Collections.IEnumerator.Current { get; }
+        public bool MoveNext();
+        public void Reset();
+    }
+    public enum AceFlags : byte {
+        AuditFlags = (byte)192,
+        ContainerInherit = (byte)2,
+        FailedAccess = (byte)128,
+        InheritanceFlags = (byte)15,
+        Inherited = (byte)16,
+        InheritOnly = (byte)8,
+        None = (byte)0,
+        NoPropagateInherit = (byte)4,
+        ObjectInherit = (byte)1,
+        SuccessfulAccess = (byte)64,
+    }
+    public enum AceQualifier {
+        AccessAllowed = 0,
+        AccessDenied = 1,
+        SystemAlarm = 3,
+        SystemAudit = 2,
+    }
+    public enum AceType : byte {
+        AccessAllowed = (byte)0,
+        AccessAllowedCallback = (byte)9,
+        AccessAllowedCallbackObject = (byte)11,
+        AccessAllowedCompound = (byte)4,
+        AccessAllowedObject = (byte)5,
+        AccessDenied = (byte)1,
+        AccessDeniedCallback = (byte)10,
+        AccessDeniedCallbackObject = (byte)12,
+        AccessDeniedObject = (byte)6,
+        MaxDefinedAceType = (byte)16,
+        SystemAlarm = (byte)3,
+        SystemAlarmCallback = (byte)14,
+        SystemAlarmCallbackObject = (byte)16,
+        SystemAlarmObject = (byte)8,
+        SystemAudit = (byte)2,
+        SystemAuditCallback = (byte)13,
+        SystemAuditCallbackObject = (byte)15,
+        SystemAuditObject = (byte)7,
+    }
+    public enum AuditFlags {
+        Failure = 2,
+        None = 0,
+        Success = 1,
+    }
+    public abstract class AuditRule : AuthorizationRule {
+        protected AuditRule(IdentityReference identity, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags auditFlags);
+        public AuditFlags AuditFlags { get; }
+    }
+    public class AuditRule<T> : AuditRule where T : struct, ValueType {
+        public AuditRule(IdentityReference identity, T rights, AuditFlags flags);
+        public AuditRule(IdentityReference identity, T rights, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags flags);
+        public AuditRule(string identity, T rights, AuditFlags flags);
+        public AuditRule(string identity, T rights, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags flags);
+        public T Rights { get; }
+    }
+    public abstract class AuthorizationRule {
+        protected internal AuthorizationRule(IdentityReference identity, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags);
+        protected internal int AccessMask { get; }
+        public IdentityReference IdentityReference { get; }
+        public InheritanceFlags InheritanceFlags { get; }
+        public bool IsInherited { get; }
+        public PropagationFlags PropagationFlags { get; }
+    }
+    public sealed class AuthorizationRuleCollection : ReadOnlyCollectionBase {
+        public AuthorizationRuleCollection();
+        public AuthorizationRule this[int index] { get; }
+        public void AddRule(AuthorizationRule rule);
+        public void CopyTo(AuthorizationRule[] rules, int index);
+    }
+    public sealed class CommonAce : QualifiedAce {
+        public CommonAce(AceFlags flags, AceQualifier qualifier, int accessMask, SecurityIdentifier sid, bool isCallback, byte[] opaque);
+        public override int BinaryLength { get; }
+        public override void GetBinaryForm(byte[] binaryForm, int offset);
+        public static int MaxOpaqueLength(bool isCallback);
+    }
+    public abstract class CommonAcl : GenericAcl {
+        public sealed override int BinaryLength { get; }
+        public sealed override int Count { get; }
+        public bool IsCanonical { get; }
+        public bool IsContainer { get; }
+        public bool IsDS { get; }
+        public sealed override byte Revision { get; }
+        public sealed override GenericAce this[int index] { get; set; }
+        public sealed override void GetBinaryForm(byte[] binaryForm, int offset);
+        public void Purge(SecurityIdentifier sid);
+        public void RemoveInheritedAces();
+    }
+    public abstract class CommonObjectSecurity : ObjectSecurity {
+        protected CommonObjectSecurity(bool isContainer);
+        protected void AddAccessRule(AccessRule rule);
+        protected void AddAuditRule(AuditRule rule);
+        public AuthorizationRuleCollection GetAccessRules(bool includeExplicit, bool includeInherited, Type targetType);
+        public AuthorizationRuleCollection GetAuditRules(bool includeExplicit, bool includeInherited, Type targetType);
+        protected override bool ModifyAccess(AccessControlModification modification, AccessRule rule, out bool modified);
+        protected override bool ModifyAudit(AccessControlModification modification, AuditRule rule, out bool modified);
+        protected bool RemoveAccessRule(AccessRule rule);
+        protected void RemoveAccessRuleAll(AccessRule rule);
+        protected void RemoveAccessRuleSpecific(AccessRule rule);
+        protected bool RemoveAuditRule(AuditRule rule);
+        protected void RemoveAuditRuleAll(AuditRule rule);
+        protected void RemoveAuditRuleSpecific(AuditRule rule);
+        protected void ResetAccessRule(AccessRule rule);
+        protected void SetAccessRule(AccessRule rule);
+        protected void SetAuditRule(AuditRule rule);
+    }
+    public sealed class CommonSecurityDescriptor : GenericSecurityDescriptor {
+        public CommonSecurityDescriptor(bool isContainer, bool isDS, byte[] binaryForm, int offset);
+        public CommonSecurityDescriptor(bool isContainer, bool isDS, ControlFlags flags, SecurityIdentifier owner, SecurityIdentifier group, SystemAcl systemAcl, DiscretionaryAcl discretionaryAcl);
+        public CommonSecurityDescriptor(bool isContainer, bool isDS, RawSecurityDescriptor rawSecurityDescriptor);
+        public CommonSecurityDescriptor(bool isContainer, bool isDS, string sddlForm);
+        public override ControlFlags ControlFlags { get; }
+        public DiscretionaryAcl DiscretionaryAcl { get; set; }
+        public override SecurityIdentifier Group { get; set; }
+        public bool IsContainer { get; }
+        public bool IsDiscretionaryAclCanonical { get; }
+        public bool IsDS { get; }
+        public bool IsSystemAclCanonical { get; }
+        public override SecurityIdentifier Owner { get; set; }
+        public SystemAcl SystemAcl { get; set; }
+        public void AddDiscretionaryAcl(byte revision, int trusted);
+        public void AddSystemAcl(byte revision, int trusted);
+        public void PurgeAccessControl(SecurityIdentifier sid);
+        public void PurgeAudit(SecurityIdentifier sid);
+        public void SetDiscretionaryAclProtection(bool isProtected, bool preserveInheritance);
+        public void SetSystemAclProtection(bool isProtected, bool preserveInheritance);
+    }
+    public sealed class CompoundAce : KnownAce {
+        public CompoundAce(AceFlags flags, int accessMask, CompoundAceType compoundAceType, SecurityIdentifier sid);
+        public override int BinaryLength { get; }
+        public CompoundAceType CompoundAceType { get; set; }
+        public override void GetBinaryForm(byte[] binaryForm, int offset);
+    }
+    public enum CompoundAceType {
+        Impersonation = 1,
+    }
+    public enum ControlFlags {
+        DiscretionaryAclAutoInherited = 1024,
+        DiscretionaryAclAutoInheritRequired = 256,
+        DiscretionaryAclDefaulted = 8,
+        DiscretionaryAclPresent = 4,
+        DiscretionaryAclProtected = 4096,
+        DiscretionaryAclUntrusted = 64,
+        GroupDefaulted = 2,
+        None = 0,
+        OwnerDefaulted = 1,
+        RMControlValid = 16384,
+        SelfRelative = 32768,
+        ServerSecurity = 128,
+        SystemAclAutoInherited = 2048,
+        SystemAclAutoInheritRequired = 512,
+        SystemAclDefaulted = 32,
+        SystemAclPresent = 16,
+        SystemAclProtected = 8192,
+    }
+    public sealed class CustomAce : GenericAce {
+        public static readonly int MaxOpaqueLength;
+        public CustomAce(AceType type, AceFlags flags, byte[] opaque);
+        public override int BinaryLength { get; }
+        public int OpaqueLength { get; }
+        public override void GetBinaryForm(byte[] binaryForm, int offset);
+        public byte[] GetOpaque();
+        public void SetOpaque(byte[] opaque);
+    }
+    public sealed class DiscretionaryAcl : CommonAcl {
+        public DiscretionaryAcl(bool isContainer, bool isDS, byte revision, int capacity);
+        public DiscretionaryAcl(bool isContainer, bool isDS, int capacity);
+        public DiscretionaryAcl(bool isContainer, bool isDS, RawAcl rawAcl);
+        public void AddAccess(AccessControlType accessType, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags);
+        public void AddAccess(AccessControlType accessType, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, ObjectAceFlags objectFlags, Guid objectType, Guid inheritedObjectType);
+        public void AddAccess(AccessControlType accessType, SecurityIdentifier sid, ObjectAccessRule rule);
+        public bool RemoveAccess(AccessControlType accessType, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags);
+        public bool RemoveAccess(AccessControlType accessType, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, ObjectAceFlags objectFlags, Guid objectType, Guid inheritedObjectType);
+        public bool RemoveAccess(AccessControlType accessType, SecurityIdentifier sid, ObjectAccessRule rule);
+        public void RemoveAccessSpecific(AccessControlType accessType, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags);
+        public void RemoveAccessSpecific(AccessControlType accessType, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, ObjectAceFlags objectFlags, Guid objectType, Guid inheritedObjectType);
+        public void RemoveAccessSpecific(AccessControlType accessType, SecurityIdentifier sid, ObjectAccessRule rule);
+        public void SetAccess(AccessControlType accessType, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags);
+        public void SetAccess(AccessControlType accessType, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, ObjectAceFlags objectFlags, Guid objectType, Guid inheritedObjectType);
+        public void SetAccess(AccessControlType accessType, SecurityIdentifier sid, ObjectAccessRule rule);
+    }
+    public abstract class GenericAce {
+        public AceFlags AceFlags { get; set; }
+        public AceType AceType { get; }
+        public AuditFlags AuditFlags { get; }
+        public abstract int BinaryLength { get; }
+        public InheritanceFlags InheritanceFlags { get; }
+        public bool IsInherited { get; }
+        public PropagationFlags PropagationFlags { get; }
+        public GenericAce Copy();
+        public static GenericAce CreateFromBinaryForm(byte[] binaryForm, int offset);
+        public sealed override bool Equals(object o);
+        public abstract void GetBinaryForm(byte[] binaryForm, int offset);
+        public sealed override int GetHashCode();
+        public static bool operator ==(GenericAce left, GenericAce right);
+        public static bool operator !=(GenericAce left, GenericAce right);
+    }
+    public abstract class GenericAcl : ICollection, IEnumerable {
+        public static readonly byte AclRevision;
+        public static readonly byte AclRevisionDS;
+        public static readonly int MaxBinaryLength;
+        protected GenericAcl();
+        public abstract int BinaryLength { get; }
+        public abstract int Count { get; }
+        public bool IsSynchronized { get; }
+        public abstract byte Revision { get; }
+        public virtual object SyncRoot { get; }
+        public abstract GenericAce this[int index] { get; set; }
+        public void CopyTo(GenericAce[] array, int index);
+        public abstract void GetBinaryForm(byte[] binaryForm, int offset);
+        public AceEnumerator GetEnumerator();
+        void System.Collections.ICollection.CopyTo(Array array, int index);
+        IEnumerator System.Collections.IEnumerable.GetEnumerator();
+    }
+    public abstract class GenericSecurityDescriptor {
+        public int BinaryLength { get; }
+        public abstract ControlFlags ControlFlags { get; }
+        public abstract SecurityIdentifier Group { get; set; }
+        public abstract SecurityIdentifier Owner { get; set; }
+        public static byte Revision { get; }
+        public void GetBinaryForm(byte[] binaryForm, int offset);
+        public string GetSddlForm(AccessControlSections includeSections);
+        public static bool IsSddlConversionSupported();
+    }
+    public enum InheritanceFlags {
+        ContainerInherit = 1,
+        None = 0,
+        ObjectInherit = 2,
+    }
+    public abstract class KnownAce : GenericAce {
+        public int AccessMask { get; set; }
+        public SecurityIdentifier SecurityIdentifier { get; set; }
+    }
+    public abstract class NativeObjectSecurity : CommonObjectSecurity {
+        protected NativeObjectSecurity(bool isContainer, ResourceType resourceType);
+        protected NativeObjectSecurity(bool isContainer, ResourceType resourceType, NativeObjectSecurity.ExceptionFromErrorCode exceptionFromErrorCode, object exceptionContext);
+        protected NativeObjectSecurity(bool isContainer, ResourceType resourceType, SafeHandle handle, AccessControlSections includeSections);
+        protected NativeObjectSecurity(bool isContainer, ResourceType resourceType, SafeHandle handle, AccessControlSections includeSections, NativeObjectSecurity.ExceptionFromErrorCode exceptionFromErrorCode, object exceptionContext);
+        protected NativeObjectSecurity(bool isContainer, ResourceType resourceType, string name, AccessControlSections includeSections);
+        protected NativeObjectSecurity(bool isContainer, ResourceType resourceType, string name, AccessControlSections includeSections, NativeObjectSecurity.ExceptionFromErrorCode exceptionFromErrorCode, object exceptionContext);
+        protected sealed override void Persist(SafeHandle handle, AccessControlSections includeSections);
+        protected void Persist(SafeHandle handle, AccessControlSections includeSections, object exceptionContext);
+        protected sealed override void Persist(string name, AccessControlSections includeSections);
+        protected void Persist(string name, AccessControlSections includeSections, object exceptionContext);
+        protected internal delegate Exception ExceptionFromErrorCode(int errorCode, string name, SafeHandle handle, object context);
+    }
+    public abstract class ObjectAccessRule : AccessRule {
+        protected ObjectAccessRule(IdentityReference identity, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, Guid objectType, Guid inheritedObjectType, AccessControlType type);
+        public Guid InheritedObjectType { get; }
+        public ObjectAceFlags ObjectFlags { get; }
+        public Guid ObjectType { get; }
+    }
+    public sealed class ObjectAce : QualifiedAce {
+        public ObjectAce(AceFlags aceFlags, AceQualifier qualifier, int accessMask, SecurityIdentifier sid, ObjectAceFlags flags, Guid type, Guid inheritedType, bool isCallback, byte[] opaque);
+        public override int BinaryLength { get; }
+        public Guid InheritedObjectAceType { get; set; }
+        public ObjectAceFlags ObjectAceFlags { get; set; }
+        public Guid ObjectAceType { get; set; }
+        public override void GetBinaryForm(byte[] binaryForm, int offset);
+        public static int MaxOpaqueLength(bool isCallback);
+    }
+    public enum ObjectAceFlags {
+        InheritedObjectAceTypePresent = 2,
+        None = 0,
+        ObjectAceTypePresent = 1,
+    }
+    public abstract class ObjectAuditRule : AuditRule {
+        protected ObjectAuditRule(IdentityReference identity, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, Guid objectType, Guid inheritedObjectType, AuditFlags auditFlags);
+        public Guid InheritedObjectType { get; }
+        public ObjectAceFlags ObjectFlags { get; }
+        public Guid ObjectType { get; }
+    }
+    public abstract class ObjectSecurity {
+        protected ObjectSecurity();
+        protected ObjectSecurity(bool isContainer, bool isDS);
+        protected ObjectSecurity(CommonSecurityDescriptor securityDescriptor);
+        public abstract Type AccessRightType { get; }
+        protected bool AccessRulesModified { get; set; }
+        public abstract Type AccessRuleType { get; }
+        public bool AreAccessRulesCanonical { get; }
+        public bool AreAccessRulesProtected { get; }
+        public bool AreAuditRulesCanonical { get; }
+        public bool AreAuditRulesProtected { get; }
+        protected bool AuditRulesModified { get; set; }
+        public abstract Type AuditRuleType { get; }
+        protected bool GroupModified { get; set; }
+        protected bool IsContainer { get; }
+        protected bool IsDS { get; }
+        protected bool OwnerModified { get; set; }
+        protected CommonSecurityDescriptor SecurityDescriptor { get; }
+        public abstract AccessRule AccessRuleFactory(IdentityReference identityReference, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type);
+        public abstract AuditRule AuditRuleFactory(IdentityReference identityReference, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags flags);
+        public IdentityReference GetGroup(Type targetType);
+        public IdentityReference GetOwner(Type targetType);
+        public byte[] GetSecurityDescriptorBinaryForm();
+        public string GetSecurityDescriptorSddlForm(AccessControlSections includeSections);
+        public static bool IsSddlConversionSupported();
+        protected abstract bool ModifyAccess(AccessControlModification modification, AccessRule rule, out bool modified);
+        public virtual bool ModifyAccessRule(AccessControlModification modification, AccessRule rule, out bool modified);
+        protected abstract bool ModifyAudit(AccessControlModification modification, AuditRule rule, out bool modified);
+        public virtual bool ModifyAuditRule(AccessControlModification modification, AuditRule rule, out bool modified);
+        protected virtual void Persist(bool enableOwnershipPrivilege, string name, AccessControlSections includeSections);
+        protected virtual void Persist(SafeHandle handle, AccessControlSections includeSections);
+        protected virtual void Persist(string name, AccessControlSections includeSections);
+        public virtual void PurgeAccessRules(IdentityReference identity);
+        public virtual void PurgeAuditRules(IdentityReference identity);
+        protected void ReadLock();
+        protected void ReadUnlock();
+        public void SetAccessRuleProtection(bool isProtected, bool preserveInheritance);
+        public void SetAuditRuleProtection(bool isProtected, bool preserveInheritance);
+        public void SetGroup(IdentityReference identity);
+        public void SetOwner(IdentityReference identity);
+        public void SetSecurityDescriptorBinaryForm(byte[] binaryForm);
+        public void SetSecurityDescriptorBinaryForm(byte[] binaryForm, AccessControlSections includeSections);
+        public void SetSecurityDescriptorSddlForm(string sddlForm);
+        public void SetSecurityDescriptorSddlForm(string sddlForm, AccessControlSections includeSections);
+        protected void WriteLock();
+        protected void WriteUnlock();
+    }
+    public abstract class ObjectSecurity<T> : NativeObjectSecurity where T : struct, ValueType {
+        protected ObjectSecurity(bool isContainer, ResourceType resourceType);
+        protected ObjectSecurity(bool isContainer, ResourceType resourceType, SafeHandle safeHandle, AccessControlSections includeSections);
+        protected ObjectSecurity(bool isContainer, ResourceType resourceType, SafeHandle safeHandle, AccessControlSections includeSections, NativeObjectSecurity.ExceptionFromErrorCode exceptionFromErrorCode, object exceptionContext);
+        protected ObjectSecurity(bool isContainer, ResourceType resourceType, string name, AccessControlSections includeSections);
+        protected ObjectSecurity(bool isContainer, ResourceType resourceType, string name, AccessControlSections includeSections, NativeObjectSecurity.ExceptionFromErrorCode exceptionFromErrorCode, object exceptionContext);
+        public override Type AccessRightType { get; }
+        public override Type AccessRuleType { get; }
+        public override Type AuditRuleType { get; }
+        public override AccessRule AccessRuleFactory(IdentityReference identityReference, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type);
+        public virtual void AddAccessRule(AccessRule<T> rule);
+        public virtual void AddAuditRule(AuditRule<T> rule);
+        public override AuditRule AuditRuleFactory(IdentityReference identityReference, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags flags);
+        protected internal void Persist(SafeHandle handle);
+        protected internal void Persist(string name);
+        public virtual bool RemoveAccessRule(AccessRule<T> rule);
+        public virtual void RemoveAccessRuleAll(AccessRule<T> rule);
+        public virtual void RemoveAccessRuleSpecific(AccessRule<T> rule);
+        public virtual bool RemoveAuditRule(AuditRule<T> rule);
+        public virtual void RemoveAuditRuleAll(AuditRule<T> rule);
+        public virtual void RemoveAuditRuleSpecific(AuditRule<T> rule);
+        public virtual void ResetAccessRule(AccessRule<T> rule);
+        public virtual void SetAccessRule(AccessRule<T> rule);
+        public virtual void SetAuditRule(AuditRule<T> rule);
+    }
+    public sealed class PrivilegeNotHeldException : UnauthorizedAccessException, ISerializable {
+        public PrivilegeNotHeldException();
+        public PrivilegeNotHeldException(string privilege);
+        public PrivilegeNotHeldException(string privilege, Exception inner);
+        public string PrivilegeName { get; }
+        public override void GetObjectData(SerializationInfo info, StreamingContext context);
+    }
+    public enum PropagationFlags {
+        InheritOnly = 2,
+        None = 0,
+        NoPropagateInherit = 1,
+    }
+    public abstract class QualifiedAce : KnownAce {
+        public AceQualifier AceQualifier { get; }
+        public bool IsCallback { get; }
+        public int OpaqueLength { get; }
+        public byte[] GetOpaque();
+        public void SetOpaque(byte[] opaque);
+    }
+    public sealed class RawAcl : GenericAcl {
+        public RawAcl(byte revision, int capacity);
+        public RawAcl(byte[] binaryForm, int offset);
+        public override int BinaryLength { get; }
+        public override int Count { get; }
+        public override byte Revision { get; }
+        public override GenericAce this[int index] { get; set; }
+        public override void GetBinaryForm(byte[] binaryForm, int offset);
+        public void InsertAce(int index, GenericAce ace);
+        public void RemoveAce(int index);
+    }
+    public sealed class RawSecurityDescriptor : GenericSecurityDescriptor {
+        public RawSecurityDescriptor(byte[] binaryForm, int offset);
+        public RawSecurityDescriptor(ControlFlags flags, SecurityIdentifier owner, SecurityIdentifier group, RawAcl systemAcl, RawAcl discretionaryAcl);
+        public RawSecurityDescriptor(string sddlForm);
+        public override ControlFlags ControlFlags { get; }
+        public RawAcl DiscretionaryAcl { get; set; }
+        public override SecurityIdentifier Group { get; set; }
+        public override SecurityIdentifier Owner { get; set; }
+        public byte ResourceManagerControl { get; set; }
+        public RawAcl SystemAcl { get; set; }
+        public void SetFlags(ControlFlags flags);
+    }
+    public sealed class RegistryAccessRule : AccessRule {
+        public RegistryAccessRule(IdentityReference identity, RegistryRights registryRights, AccessControlType type);
+        public RegistryAccessRule(IdentityReference identity, RegistryRights registryRights, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type);
+        public RegistryAccessRule(string identity, RegistryRights registryRights, AccessControlType type);
+        public RegistryAccessRule(string identity, RegistryRights registryRights, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type);
+        public RegistryRights RegistryRights { get; }
+    }
+    public sealed class RegistryAuditRule : AuditRule {
+        public RegistryAuditRule(IdentityReference identity, RegistryRights registryRights, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags flags);
+        public RegistryAuditRule(string identity, RegistryRights registryRights, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags flags);
+        public RegistryRights RegistryRights { get; }
+    }
+    public enum RegistryRights {
+        ChangePermissions = 262144,
+        CreateLink = 32,
+        CreateSubKey = 4,
+        Delete = 65536,
+        EnumerateSubKeys = 8,
+        ExecuteKey = 131097,
+        FullControl = 983103,
+        Notify = 16,
+        QueryValues = 1,
+        ReadKey = 131097,
+        ReadPermissions = 131072,
+        SetValue = 2,
+        TakeOwnership = 524288,
+        WriteKey = 131078,
+    }
+    public sealed class RegistrySecurity : NativeObjectSecurity {
+        public RegistrySecurity();
+        public override Type AccessRightType { get; }
+        public override Type AccessRuleType { get; }
+        public override Type AuditRuleType { get; }
+        public override AccessRule AccessRuleFactory(IdentityReference identityReference, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type);
+        public void AddAccessRule(RegistryAccessRule rule);
+        public void AddAuditRule(RegistryAuditRule rule);
+        public override AuditRule AuditRuleFactory(IdentityReference identityReference, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags flags);
+        public bool RemoveAccessRule(RegistryAccessRule rule);
+        public void RemoveAccessRuleAll(RegistryAccessRule rule);
+        public void RemoveAccessRuleSpecific(RegistryAccessRule rule);
+        public bool RemoveAuditRule(RegistryAuditRule rule);
+        public void RemoveAuditRuleAll(RegistryAuditRule rule);
+        public void RemoveAuditRuleSpecific(RegistryAuditRule rule);
+        public void ResetAccessRule(RegistryAccessRule rule);
+        public void SetAccessRule(RegistryAccessRule rule);
+        public void SetAuditRule(RegistryAuditRule rule);
+    }
+    public enum ResourceType {
+        DSObject = 8,
+        DSObjectAll = 9,
+        FileObject = 1,
+        KernelObject = 6,
+        LMShare = 5,
+        Printer = 3,
+        ProviderDefined = 10,
+        RegistryKey = 4,
+        RegistryWow6432Key = 12,
+        Service = 2,
+        Unknown = 0,
+        WindowObject = 7,
+        WmiGuidObject = 11,
+    }
+    public enum SecurityInfos {
+        DiscretionaryAcl = 4,
+        Group = 2,
+        Owner = 1,
+        SystemAcl = 8,
+    }
+    public sealed class SystemAcl : CommonAcl {
+        public SystemAcl(bool isContainer, bool isDS, byte revision, int capacity);
+        public SystemAcl(bool isContainer, bool isDS, int capacity);
+        public SystemAcl(bool isContainer, bool isDS, RawAcl rawAcl);
+        public void AddAudit(AuditFlags auditFlags, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags);
+        public void AddAudit(AuditFlags auditFlags, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, ObjectAceFlags objectFlags, Guid objectType, Guid inheritedObjectType);
+        public void AddAudit(SecurityIdentifier sid, ObjectAuditRule rule);
+        public bool RemoveAudit(AuditFlags auditFlags, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags);
+        public bool RemoveAudit(AuditFlags auditFlags, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, ObjectAceFlags objectFlags, Guid objectType, Guid inheritedObjectType);
+        public bool RemoveAudit(SecurityIdentifier sid, ObjectAuditRule rule);
+        public void RemoveAuditSpecific(AuditFlags auditFlags, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags);
+        public void RemoveAuditSpecific(AuditFlags auditFlags, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, ObjectAceFlags objectFlags, Guid objectType, Guid inheritedObjectType);
+        public void RemoveAuditSpecific(SecurityIdentifier sid, ObjectAuditRule rule);
+        public void SetAudit(AuditFlags auditFlags, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags);
+        public void SetAudit(AuditFlags auditFlags, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, ObjectAceFlags objectFlags, Guid objectType, Guid inheritedObjectType);
+        public void SetAudit(SecurityIdentifier sid, ObjectAuditRule rule);
+    }
+}
```

