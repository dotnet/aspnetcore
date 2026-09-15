// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Describes the microphone permission feedback displayed by a <see cref="MessageInput"/>.
/// </summary>
public enum MicrophonePermissionStatus
{
    /// <summary>
    /// No microphone permission feedback is displayed.
    /// </summary>
    None,

    /// <summary>
    /// The browser is waiting for the user to respond to a microphone permission request.
    /// </summary>
    Requesting,

    /// <summary>
    /// The browser denied or revoked microphone access.
    /// </summary>
    Denied,

    /// <summary>
    /// No microphone is available.
    /// </summary>
    Unavailable,
}
