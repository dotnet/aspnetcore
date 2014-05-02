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
// <copyright file="SecurityStatus.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Net.Server
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
}
