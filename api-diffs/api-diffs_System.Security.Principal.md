# System.Security.Principal

``` diff
+namespace System.Security.Principal {
+    public sealed class IdentityNotMappedException : SystemException {
+        public IdentityNotMappedException();
+        public IdentityNotMappedException(string message);
+        public IdentityNotMappedException(string message, Exception inner);
+        public IdentityReferenceCollection UnmappedIdentities { get; }
+        public override void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext);
+    }
+    public abstract class IdentityReference {
+        public abstract string Value { get; }
+        public abstract override bool Equals(object o);
+        public abstract override int GetHashCode();
+        public abstract bool IsValidTargetType(Type targetType);
+        public static bool operator ==(IdentityReference left, IdentityReference right);
+        public static bool operator !=(IdentityReference left, IdentityReference right);
+        public abstract override string ToString();
+        public abstract IdentityReference Translate(Type targetType);
+    }
+    public class IdentityReferenceCollection : ICollection<IdentityReference>, IEnumerable, IEnumerable<IdentityReference> {
+        public IdentityReferenceCollection();
+        public IdentityReferenceCollection(int capacity);
+        public int Count { get; }
+        bool System.Collections.Generic.ICollection<System.Security.Principal.IdentityReference>.IsReadOnly { get; }
+        public IdentityReference this[int index] { get; set; }
+        public void Add(IdentityReference identity);
+        public void Clear();
+        public bool Contains(IdentityReference identity);
+        public void CopyTo(IdentityReference[] array, int offset);
+        public IEnumerator<IdentityReference> GetEnumerator();
+        public bool Remove(IdentityReference identity);
+        IEnumerator System.Collections.IEnumerable.GetEnumerator();
+        public IdentityReferenceCollection Translate(Type targetType);
+        public IdentityReferenceCollection Translate(Type targetType, bool forceSuccess);
+    }
+    public sealed class NTAccount : IdentityReference {
+        public NTAccount(string name);
+        public NTAccount(string domainName, string accountName);
+        public override string Value { get; }
+        public override bool Equals(object o);
+        public override int GetHashCode();
+        public override bool IsValidTargetType(Type targetType);
+        public static bool operator ==(NTAccount left, NTAccount right);
+        public static bool operator !=(NTAccount left, NTAccount right);
+        public override string ToString();
+        public override IdentityReference Translate(Type targetType);
+    }
+    public sealed class SecurityIdentifier : IdentityReference, IComparable<SecurityIdentifier> {
+        public static readonly int MaxBinaryLength;
+        public static readonly int MinBinaryLength;
+        public SecurityIdentifier(byte[] binaryForm, int offset);
+        public SecurityIdentifier(IntPtr binaryForm);
+        public SecurityIdentifier(WellKnownSidType sidType, SecurityIdentifier domainSid);
+        public SecurityIdentifier(string sddlForm);
+        public SecurityIdentifier AccountDomainSid { get; }
+        public int BinaryLength { get; }
+        public override string Value { get; }
+        public int CompareTo(SecurityIdentifier sid);
+        public override bool Equals(object o);
+        public bool Equals(SecurityIdentifier sid);
+        public void GetBinaryForm(byte[] binaryForm, int offset);
+        public override int GetHashCode();
+        public bool IsAccountSid();
+        public bool IsEqualDomainSid(SecurityIdentifier sid);
+        public override bool IsValidTargetType(Type targetType);
+        public bool IsWellKnown(WellKnownSidType type);
+        public static bool operator ==(SecurityIdentifier left, SecurityIdentifier right);
+        public static bool operator !=(SecurityIdentifier left, SecurityIdentifier right);
+        public override string ToString();
+        public override IdentityReference Translate(Type targetType);
+    }
+    public enum TokenAccessLevels {
+        AdjustDefault = 128,
+        AdjustGroups = 64,
+        AdjustPrivileges = 32,
+        AdjustSessionId = 256,
+        AllAccess = 983551,
+        AssignPrimary = 1,
+        Duplicate = 2,
+        Impersonate = 4,
+        MaximumAllowed = 33554432,
+        Query = 8,
+        QuerySource = 16,
+        Read = 131080,
+        Write = 131296,
+    }
+    public enum WellKnownSidType {
+        AccountAdministratorSid = 38,
+        AccountCertAdminsSid = 46,
+        AccountComputersSid = 44,
+        AccountControllersSid = 45,
+        AccountDomainAdminsSid = 41,
+        AccountDomainGuestsSid = 43,
+        AccountDomainUsersSid = 42,
+        AccountEnterpriseAdminsSid = 48,
+        AccountGuestSid = 39,
+        AccountKrbtgtSid = 40,
+        AccountPolicyAdminsSid = 49,
+        AccountRasAndIasServersSid = 50,
+        AccountSchemaAdminsSid = 47,
+        AnonymousSid = 13,
+        AuthenticatedUserSid = 17,
+        BatchSid = 10,
+        BuiltinAccountOperatorsSid = 30,
+        BuiltinAdministratorsSid = 26,
+        BuiltinAuthorizationAccessSid = 59,
+        BuiltinBackupOperatorsSid = 33,
+        BuiltinDomainSid = 25,
+        BuiltinGuestsSid = 28,
+        BuiltinIncomingForestTrustBuildersSid = 56,
+        BuiltinNetworkConfigurationOperatorsSid = 37,
+        BuiltinPerformanceLoggingUsersSid = 58,
+        BuiltinPerformanceMonitoringUsersSid = 57,
+        BuiltinPowerUsersSid = 29,
+        BuiltinPreWindows2000CompatibleAccessSid = 35,
+        BuiltinPrintOperatorsSid = 32,
+        BuiltinRemoteDesktopUsersSid = 36,
+        BuiltinReplicatorSid = 34,
+        BuiltinSystemOperatorsSid = 31,
+        BuiltinUsersSid = 27,
+        CreatorGroupServerSid = 6,
+        CreatorGroupSid = 4,
+        CreatorOwnerServerSid = 5,
+        CreatorOwnerSid = 3,
+        DialupSid = 8,
+        DigestAuthenticationSid = 52,
+        EnterpriseControllersSid = 15,
+        InteractiveSid = 11,
+        LocalServiceSid = 23,
+        LocalSid = 2,
+        LocalSystemSid = 22,
+        LogonIdsSid = 21,
+        MaxDefined = 60,
+        NetworkServiceSid = 24,
+        NetworkSid = 9,
+        NTAuthoritySid = 7,
+        NtlmAuthenticationSid = 51,
+        NullSid = 0,
+        OtherOrganizationSid = 55,
+        ProxySid = 14,
+        RemoteLogonIdSid = 20,
+        RestrictedCodeSid = 18,
+        SChannelAuthenticationSid = 53,
+        SelfSid = 16,
+        ServiceSid = 12,
+        TerminalServerSid = 19,
+        ThisOrganizationSid = 54,
+        WinAccountReadonlyControllersSid = 75,
+        WinApplicationPackageAuthoritySid = 83,
+        WinBuiltinAnyPackageSid = 84,
+        WinBuiltinCertSvcDComAccessGroup = 78,
+        WinBuiltinCryptoOperatorsSid = 64,
+        WinBuiltinDCOMUsersSid = 61,
+        WinBuiltinEventLogReadersGroup = 76,
+        WinBuiltinIUsersSid = 62,
+        WinBuiltinTerminalServerLicenseServersSid = 60,
+        WinCacheablePrincipalsGroupSid = 72,
+        WinCapabilityDocumentsLibrarySid = 91,
+        WinCapabilityEnterpriseAuthenticationSid = 93,
+        WinCapabilityInternetClientServerSid = 86,
+        WinCapabilityInternetClientSid = 85,
+        WinCapabilityMusicLibrarySid = 90,
+        WinCapabilityPicturesLibrarySid = 88,
+        WinCapabilityPrivateNetworkClientServerSid = 87,
+        WinCapabilityRemovableStorageSid = 94,
+        WinCapabilitySharedUserCertificatesSid = 92,
+        WinCapabilityVideosLibrarySid = 89,
+        WinConsoleLogonSid = 81,
+        WinCreatorOwnerRightsSid = 71,
+        WinEnterpriseReadonlyControllersSid = 74,
+        WinHighLabelSid = 68,
+        WinIUserSid = 63,
+        WinLocalLogonSid = 80,
+        WinLowLabelSid = 66,
+        WinMediumLabelSid = 67,
+        WinMediumPlusLabelSid = 79,
+        WinNewEnterpriseReadonlyControllersSid = 77,
+        WinNonCacheablePrincipalsGroupSid = 73,
+        WinSystemLabelSid = 69,
+        WinThisOrganizationCertificateSid = 82,
+        WinUntrustedLabelSid = 65,
+        WinWriteRestrictedCodeSid = 70,
+        WorldSid = 1,
+    }
+    public enum WindowsAccountType {
+        Anonymous = 3,
+        Guest = 1,
+        Normal = 0,
+        System = 2,
+    }
+    public enum WindowsBuiltInRole {
+        AccountOperator = 548,
+        Administrator = 544,
+        BackupOperator = 551,
+        Guest = 546,
+        PowerUser = 547,
+        PrintOperator = 550,
+        Replicator = 552,
+        SystemOperator = 549,
+        User = 545,
+    }
+    public class WindowsIdentity : ClaimsIdentity, IDeserializationCallback, IDisposable, ISerializable {
+        public const string DefaultIssuer = "AD AUTHORITY";
+        public WindowsIdentity(IntPtr userToken);
+        public WindowsIdentity(IntPtr userToken, string type);
+        public WindowsIdentity(IntPtr userToken, string type, WindowsAccountType acctType);
+        public WindowsIdentity(IntPtr userToken, string type, WindowsAccountType acctType, bool isAuthenticated);
+        public WindowsIdentity(SerializationInfo info, StreamingContext context);
+        protected WindowsIdentity(WindowsIdentity identity);
+        public WindowsIdentity(string sUserPrincipalName);
+        public SafeAccessTokenHandle AccessToken { get; }
+        public sealed override string AuthenticationType { get; }
+        public override IEnumerable<Claim> Claims { get; }
+        public virtual IEnumerable<Claim> DeviceClaims { get; }
+        public IdentityReferenceCollection Groups { get; }
+        public TokenImpersonationLevel ImpersonationLevel { get; }
+        public virtual bool IsAnonymous { get; }
+        public override bool IsAuthenticated { get; }
+        public virtual bool IsGuest { get; }
+        public virtual bool IsSystem { get; }
+        public override string Name { get; }
+        public SecurityIdentifier Owner { get; }
+        public virtual IntPtr Token { get; }
+        public SecurityIdentifier User { get; }
+        public virtual IEnumerable<Claim> UserClaims { get; }
+        public override ClaimsIdentity Clone();
+        public void Dispose();
+        protected virtual void Dispose(bool disposing);
+        public static WindowsIdentity GetAnonymous();
+        public static WindowsIdentity GetCurrent();
+        public static WindowsIdentity GetCurrent(bool ifImpersonating);
+        public static WindowsIdentity GetCurrent(TokenAccessLevels desiredAccess);
+        public static void RunImpersonated(SafeAccessTokenHandle safeAccessTokenHandle, Action action);
+        public static T RunImpersonated<T>(SafeAccessTokenHandle safeAccessTokenHandle, Func<T> func);
+        void System.Runtime.Serialization.IDeserializationCallback.OnDeserialization(object sender);
+        void System.Runtime.Serialization.ISerializable.GetObjectData(SerializationInfo info, StreamingContext context);
+    }
+    public class WindowsPrincipal : ClaimsPrincipal {
+        public WindowsPrincipal(WindowsIdentity ntIdentity);
+        public virtual IEnumerable<Claim> DeviceClaims { get; }
+        public override IIdentity Identity { get; }
+        public virtual IEnumerable<Claim> UserClaims { get; }
+        public virtual bool IsInRole(int rid);
+        public virtual bool IsInRole(SecurityIdentifier sid);
+        public virtual bool IsInRole(WindowsBuiltInRole role);
+        public override bool IsInRole(string role);
+    }
+}
```

