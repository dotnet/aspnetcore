//------------------------------------------------------------------------------
// <copyright file="Internal.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography.X509Certificates;
using System.Security.Permissions;

namespace Microsoft.AspNet.Security.Windows
{
    internal enum SecurityStatus
    {
        // Success / Informational
        OK = 0x00000000,
        ContinueNeeded = unchecked((int)0x00090312),
        CompleteNeeded = unchecked((int)0x00090313),
        CompAndContinue = unchecked((int)0x00090314),
        ContextExpired = unchecked((int)0x00090317),
        CredentialsNeeded = unchecked((int)0x00090320),
        Renegotiate = unchecked((int)0x00090321),

        // Errors
        OutOfMemory = unchecked((int)0x80090300),
        InvalidHandle = unchecked((int)0x80090301),
        Unsupported = unchecked((int)0x80090302),
        TargetUnknown = unchecked((int)0x80090303),
        InternalError = unchecked((int)0x80090304),
        PackageNotFound = unchecked((int)0x80090305),
        NotOwner = unchecked((int)0x80090306),
        CannotInstall = unchecked((int)0x80090307),
        InvalidToken = unchecked((int)0x80090308),
        CannotPack = unchecked((int)0x80090309),
        QopNotSupported = unchecked((int)0x8009030A),
        NoImpersonation = unchecked((int)0x8009030B),
        LogonDenied = unchecked((int)0x8009030C),
        UnknownCredentials = unchecked((int)0x8009030D),
        NoCredentials = unchecked((int)0x8009030E),
        MessageAltered = unchecked((int)0x8009030F),
        OutOfSequence = unchecked((int)0x80090310),
        NoAuthenticatingAuthority = unchecked((int)0x80090311),
        IncompleteMessage = unchecked((int)0x80090318),
        IncompleteCredentials = unchecked((int)0x80090320),
        BufferNotEnough = unchecked((int)0x80090321),
        WrongPrincipal = unchecked((int)0x80090322),
        TimeSkew = unchecked((int)0x80090324),
        UntrustedRoot = unchecked((int)0x80090325),
        IllegalMessage = unchecked((int)0x80090326),
        CertUnknown = unchecked((int)0x80090327),
        CertExpired = unchecked((int)0x80090328),
        AlgorithmMismatch = unchecked((int)0x80090331),
        SecurityQosFailed = unchecked((int)0x80090332),
        SmartcardLogonRequired = unchecked((int)0x8009033E),
        UnsupportedPreauth = unchecked((int)0x80090343),
        BadBinding = unchecked((int)0x80090346)
    }

    internal enum ContextAttribute
    {
        // look into <sspi.h> and <schannel.h>
        Sizes = 0x00,
        Names = 0x01,
        Lifespan = 0x02,
        DceInfo = 0x03,
        StreamSizes = 0x04,
        // KeyInfo             = 0x05, must not be used, see ConnectionInfo instead
        Authority = 0x06,
        // SECPKG_ATTR_PROTO_INFO          = 7,
        // SECPKG_ATTR_PASSWORD_EXPIRY     = 8,
        // SECPKG_ATTR_SESSION_KEY         = 9,
        PackageInfo = 0x0A,
        // SECPKG_ATTR_USER_FLAGS          = 11,
        NegotiationInfo = 0x0C,
        // SECPKG_ATTR_NATIVE_NAMES        = 13,
        // SECPKG_ATTR_FLAGS               = 14,
        // SECPKG_ATTR_USE_VALIDATED       = 15,
        // SECPKG_ATTR_CREDENTIAL_NAME     = 16,
        // SECPKG_ATTR_TARGET_INFORMATION  = 17,
        // SECPKG_ATTR_ACCESS_TOKEN        = 18,
        // SECPKG_ATTR_TARGET              = 19,
        // SECPKG_ATTR_AUTHENTICATION_ID   = 20,
        UniqueBindings = 0x19,
        EndpointBindings = 0x1A,
        ClientSpecifiedSpn = 0x1B, // SECPKG_ATTR_CLIENT_SPECIFIED_TARGET = 27
        RemoteCertificate = 0x53,
        LocalCertificate = 0x54,
        RootStore = 0x55,
        IssuerListInfoEx = 0x59,
        ConnectionInfo = 0x5A,
        // SECPKG_ATTR_EAP_KEY_BLOCK        0x5b   // returns SecPkgContext_EapKeyBlock  
        // SECPKG_ATTR_MAPPED_CRED_ATTR     0x5c   // returns SecPkgContext_MappedCredAttr  
        // SECPKG_ATTR_SESSION_INFO         0x5d   // returns SecPkgContext_SessionInfo  
        // SECPKG_ATTR_APP_DATA             0x5e   // sets/returns SecPkgContext_SessionAppData  
        // SECPKG_ATTR_REMOTE_CERTIFICATES  0x5F   // returns SecPkgContext_Certificates  
        // SECPKG_ATTR_CLIENT_CERT_POLICY   0x60   // sets    SecPkgCred_ClientCertCtlPolicy  
        // SECPKG_ATTR_CC_POLICY_RESULT     0x61   // returns SecPkgContext_ClientCertPolicyResult  
        // SECPKG_ATTR_USE_NCRYPT           0x62   // Sets the CRED_FLAG_USE_NCRYPT_PROVIDER FLAG on cred group  
        // SECPKG_ATTR_LOCAL_CERT_INFO      0x63   // returns SecPkgContext_CertInfo  
        // SECPKG_ATTR_CIPHER_INFO          0x64   // returns new CNG SecPkgContext_CipherInfo  
        // SECPKG_ATTR_EAP_PRF_INFO         0x65   // sets    SecPkgContext_EapPrfInfo  
        // SECPKG_ATTR_SUPPORTED_SIGNATURES 0x66   // returns SecPkgContext_SupportedSignatures  
        // SECPKG_ATTR_REMOTE_CERT_CHAIN    0x67   // returns PCCERT_CONTEXT  
        UiInfo = 0x68, // sets SEcPkgContext_UiInfo  
    }

    internal enum Endianness
    {
        Network = 0x00,
        Native = 0x10,
    }

    internal enum CredentialUse
    {
        Inbound = 0x1,
        Outbound = 0x2,
        Both = 0x3,
    }

    internal enum BufferType
    {
        Empty = 0x00,
        Data = 0x01,
        Token = 0x02,
        Parameters = 0x03,
        Missing = 0x04,
        Extra = 0x05,
        Trailer = 0x06,
        Header = 0x07,
        Padding = 0x09,    // non-data padding
        Stream = 0x0A,
        ChannelBindings = 0x0E,
        TargetHost = 0x10,
        ReadOnlyFlag = unchecked((int)0x80000000),
        ReadOnlyWithChecksum = 0x10000000
    }

    // SecPkgContext_IssuerListInfoEx
    [StructLayout(LayoutKind.Sequential)]
    internal struct IssuerListInfoEx
    {
        public SafeHandle aIssuers;
        public uint cIssuers;

        public unsafe IssuerListInfoEx(SafeHandle handle, byte[] nativeBuffer)
        {
            aIssuers = handle;
            fixed (byte* voidPtr = nativeBuffer)
            {
                // if this breaks on 64 bit, do the sizeof(IntPtr) trick
                cIssuers = *((uint*)(voidPtr + IntPtr.Size));
            }
        }
    }
    
    [StructLayout(LayoutKind.Sequential)]
    internal struct SecureCredential
    {
        /*
        typedef struct _SCHANNEL_CRED
        {
            DWORD           dwVersion;      // always SCHANNEL_CRED_VERSION
            DWORD           cCreds;
            PCCERT_CONTEXT *paCred;
            HCERTSTORE      hRootStore;

            DWORD           cMappers;
            struct _HMAPPER **aphMappers;

            DWORD           cSupportedAlgs;
            ALG_ID *        palgSupportedAlgs;

            DWORD           grbitEnabledProtocols;
            DWORD           dwMinimumCipherStrength;
            DWORD           dwMaximumCipherStrength;
            DWORD           dwSessionLifespan;
            DWORD           dwFlags;
            DWORD           reserved;
        } SCHANNEL_CRED, *PSCHANNEL_CRED;
        */

        public const int CurrentVersion = 0x4;

        public int version;
        public int cCreds;

        // ptr to an array of pointers
        // There is a hack done with this field.  AcquireCredentialsHandle requires an array of
        // certificate handles; we only ever use one.  In order to avoid pinning a one element array,
        // we copy this value onto the stack, create a pointer on the stack to the copied value,
        // and replace this field with the pointer, during the call to AcquireCredentialsHandle.
        // Then we fix it up afterwards.  Fine as long as all the SSPI credentials are not
        // supposed to be threadsafe.
        public IntPtr certContextArray;

        private readonly IntPtr rootStore;               // == always null, OTHERWISE NOT RELIABLE
        public int cMappers;
        private readonly IntPtr phMappers;               // == always null, OTHERWISE NOT RELIABLE
        public int cSupportedAlgs;
        private readonly IntPtr palgSupportedAlgs;       // == always null, OTHERWISE NOT RELIABLE
        public SchProtocols grbitEnabledProtocols;
        public int dwMinimumCipherStrength;
        public int dwMaximumCipherStrength;
        public int dwSessionLifespan;
        public SecureCredential.Flags dwFlags;
        public int reserved;

        public SecureCredential(int version, X509Certificate certificate, SecureCredential.Flags flags, SchProtocols protocols, EncryptionPolicy policy)
        {
            // default values required for a struct
            rootStore = phMappers = palgSupportedAlgs = certContextArray = IntPtr.Zero;
            cCreds = cMappers = cSupportedAlgs = 0;

            if (policy == EncryptionPolicy.RequireEncryption)
            {
                // Prohibit null encryption cipher
                dwMinimumCipherStrength = 0;
                dwMaximumCipherStrength = 0;
            }
            else if (policy == EncryptionPolicy.AllowNoEncryption)
            {
                // Allow null encryption cipher in addition to other ciphers
                dwMinimumCipherStrength = -1;
                dwMaximumCipherStrength = 0;
            }
            else if (policy == EncryptionPolicy.NoEncryption)
            {
                // Suppress all encryption and require null encryption cipher only
                dwMinimumCipherStrength = -1;
                dwMaximumCipherStrength = -1;
            }
            else
            {
                throw new ArgumentException(SR.GetString(SR.net_invalid_enum, "EncryptionPolicy"), "policy");
            }

            dwSessionLifespan = reserved = 0;
            this.version = version;
            dwFlags = flags;
            grbitEnabledProtocols = protocols;
            if (certificate != null)
            {
                certContextArray = certificate.Handle;
                cCreds = 1;
            }
        }
        
        [Flags]
        public enum Flags
        {
            Zero = 0,
            NoSystemMapper = 0x02,
            NoNameCheck = 0x04,
            ValidateManual = 0x08,
            NoDefaultCred = 0x10,
            ValidateAuto = 0x20
        }
    } // SecureCredential

    [SuppressMessage("Microsoft.Design", "CA1049:TypesThatOwnNativeResourcesShouldBeDisposable",
        Justification = "This structure does not own the native resource.")]
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct SecurityBufferStruct
    {
        public int count;
        public BufferType type;
        public IntPtr token;

        public static readonly int Size = sizeof(SecurityBufferStruct);
    }
    
    internal static class IntPtrHelper
    {
        internal static IntPtr Add(IntPtr a, int b)
        {
            return (IntPtr)((long)a + (long)b);
        }
    }
}
