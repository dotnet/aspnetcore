// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Authentication.Negotiate;

internal enum SecurityStatusPalErrorCode
{
    NotSet = 0,
    OK,
    ContinueNeeded,
    CompleteNeeded,
    CompAndContinue,
    ContextExpired,
    CredentialsNeeded,
    Renegotiate,

    // Errors
    OutOfMemory,
    InvalidHandle,
    Unsupported,
    TargetUnknown,
    InternalError,
    PackageNotFound,
    NotOwner,
    CannotInstall,
    InvalidToken,
    CannotPack,
    QopNotSupported,
    NoImpersonation,
    LogonDenied,
    UnknownCredentials,
    NoCredentials,
    MessageAltered,
    OutOfSequence,
    NoAuthenticatingAuthority,
    IncompleteMessage,
    IncompleteCredentials,
    BufferNotEnough,
    WrongPrincipal,
    TimeSkew,
    UntrustedRoot,
    IllegalMessage,
    CertUnknown,
    CertExpired,
    DecryptFailure,
    AlgorithmMismatch,
    SecurityQosFailed,
    SmartcardLogonRequired,
    UnsupportedPreauth,
    BadBinding,
    DowngradeDetected,
    ApplicationProtocolMismatch
}
