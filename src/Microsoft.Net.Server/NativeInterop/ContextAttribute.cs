// -----------------------------------------------------------------------
// <copyright file="ContextAttribute.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Net.Server
{
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
}
