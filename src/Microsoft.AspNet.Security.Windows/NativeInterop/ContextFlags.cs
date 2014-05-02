// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

// -----------------------------------------------------------------------
// <copyright file="ContextFlags.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace Microsoft.AspNet.Security.Windows
{
    // #define ISC_REQ_DELEGATE                0x00000001
    // #define ISC_REQ_MUTUAL_AUTH             0x00000002
    // #define ISC_REQ_REPLAY_DETECT           0x00000004
    // #define ISC_REQ_SEQUENCE_DETECT         0x00000008
    // #define ISC_REQ_CONFIDENTIALITY         0x00000010
    // #define ISC_REQ_USE_SESSION_KEY         0x00000020
    // #define ISC_REQ_PROMPT_FOR_CREDS        0x00000040
    // #define ISC_REQ_USE_SUPPLIED_CREDS      0x00000080
    // #define ISC_REQ_ALLOCATE_MEMORY         0x00000100
    // #define ISC_REQ_USE_DCE_STYLE           0x00000200
    // #define ISC_REQ_DATAGRAM                0x00000400
    // #define ISC_REQ_CONNECTION              0x00000800
    // #define ISC_REQ_CALL_LEVEL              0x00001000
    // #define ISC_REQ_FRAGMENT_SUPPLIED       0x00002000
    // #define ISC_REQ_EXTENDED_ERROR          0x00004000
    // #define ISC_REQ_STREAM                  0x00008000
    // #define ISC_REQ_INTEGRITY               0x00010000
    // #define ISC_REQ_IDENTIFY                0x00020000
    // #define ISC_REQ_NULL_SESSION            0x00040000
    // #define ISC_REQ_MANUAL_CRED_VALIDATION  0x00080000
    // #define ISC_REQ_RESERVED1               0x00100000
    // #define ISC_REQ_FRAGMENT_TO_FIT         0x00200000
    // #define ISC_REQ_HTTP                    0x10000000
    // Win7 SP1 +
    // #define ISC_REQ_UNVERIFIED_TARGET_NAME  0x20000000  

    // #define ASC_REQ_DELEGATE                0x00000001
    // #define ASC_REQ_MUTUAL_AUTH             0x00000002
    // #define ASC_REQ_REPLAY_DETECT           0x00000004
    // #define ASC_REQ_SEQUENCE_DETECT         0x00000008
    // #define ASC_REQ_CONFIDENTIALITY         0x00000010
    // #define ASC_REQ_USE_SESSION_KEY         0x00000020
    // #define ASC_REQ_ALLOCATE_MEMORY         0x00000100
    // #define ASC_REQ_USE_DCE_STYLE           0x00000200
    // #define ASC_REQ_DATAGRAM                0x00000400
    // #define ASC_REQ_CONNECTION              0x00000800
    // #define ASC_REQ_CALL_LEVEL              0x00001000
    // #define ASC_REQ_EXTENDED_ERROR          0x00008000
    // #define ASC_REQ_STREAM                  0x00010000
    // #define ASC_REQ_INTEGRITY               0x00020000
    // #define ASC_REQ_LICENSING               0x00040000
    // #define ASC_REQ_IDENTIFY                0x00080000
    // #define ASC_REQ_ALLOW_NULL_SESSION      0x00100000
    // #define ASC_REQ_ALLOW_NON_USER_LOGONS   0x00200000
    // #define ASC_REQ_ALLOW_CONTEXT_REPLAY    0x00400000
    // #define ASC_REQ_FRAGMENT_TO_FIT         0x00800000
    // #define ASC_REQ_FRAGMENT_SUPPLIED       0x00002000
    // #define ASC_REQ_NO_TOKEN                0x01000000
    // #define ASC_REQ_HTTP                    0x10000000

    [Flags]
    internal enum ContextFlags
    {
        Zero = 0,
        // The server in the transport application can
        // build new security contexts impersonating the
        // client that will be accepted by other servers
        // as the client's contexts.
        Delegate = 0x00000001,
        // The communicating parties must authenticate
        // their identities to each other. Without MutualAuth,
        // the client authenticates its identity to the server.
        // With MutualAuth, the server also must authenticate
        // its identity to the client.
        MutualAuth = 0x00000002,
        // The security package detects replayed packets and
        // notifies the caller if a packet has been replayed.
        // The use of this flag implies all of the conditions
        // specified by the Integrity flag.
        ReplayDetect = 0x00000004,
        // The context must be allowed to detect out-of-order
        // delivery of packets later through the message support
        // functions. Use of this flag implies all of the
        // conditions specified by the Integrity flag.
        SequenceDetect = 0x00000008,
        // The context must protect data while in transit.
        // Confidentiality is supported for NTLM with Microsoft
        // Windows NT version 4.0, SP4 and later and with the
        // Kerberos protocol in Microsoft Windows 2000 and later.
        Confidentiality = 0x00000010,
        UseSessionKey = 0x00000020,
        AllocateMemory = 0x00000100,

        // Connection semantics must be used.
        Connection = 0x00000800,

        // Client applications requiring extended error messages specify the
        // ISC_REQ_EXTENDED_ERROR flag when calling the InitializeSecurityContext
        // Server applications requiring extended error messages set
        // the ASC_REQ_EXTENDED_ERROR flag when calling AcceptSecurityContext.
        InitExtendedError = 0x00004000,
        AcceptExtendedError = 0x00008000,
        // A transport application requests stream semantics
        // by setting the ISC_REQ_STREAM and ASC_REQ_STREAM
        // flags in the calls to the InitializeSecurityContext
        // and AcceptSecurityContext functions
        InitStream = 0x00008000,
        AcceptStream = 0x00010000,
        // Buffer integrity can be verified; however, replayed
        // and out-of-sequence messages will not be detected
        InitIntegrity = 0x00010000,       // ISC_REQ_INTEGRITY
        AcceptIntegrity = 0x00020000,       // ASC_REQ_INTEGRITY

        InitManualCredValidation = 0x00080000,   // ISC_REQ_MANUAL_CRED_VALIDATION
        InitUseSuppliedCreds = 0x00000080,   // ISC_REQ_USE_SUPPLIED_CREDS
        InitIdentify = 0x00020000,   // ISC_REQ_IDENTIFY
        AcceptIdentify = 0x00080000,   // ASC_REQ_IDENTIFY

        ProxyBindings = 0x04000000,   // ASC_REQ_PROXY_BINDINGS
        AllowMissingBindings = 0x10000000,   // ASC_REQ_ALLOW_MISSING_BINDINGS

        UnverifiedTargetName = 0x20000000,   // ISC_REQ_UNVERIFIED_TARGET_NAME
    }
}
