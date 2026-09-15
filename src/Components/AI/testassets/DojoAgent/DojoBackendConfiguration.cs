// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace DojoAgent;

/// <summary>
/// Parses the dojo backend selection shared by the AGUIDojoApi and DojoClient hosts.
/// </summary>
public static class DojoBackendConfiguration
{
    /// <summary>
    /// Parses a configured <c>DOJO_BACKEND</c> value into a <see cref="DojoBackendKind"/>.
    /// </summary>
    /// <param name="value">
    /// The configured value. <see langword="null"/> selects
    /// <see cref="DojoBackendKind.AGUI"/>; otherwise the value must exactly match the name of a
    /// <see cref="DojoBackendKind"/> member.
    /// </param>
    /// <returns>The selected backend.</returns>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="value"/> is not <see langword="null"/> or an exact
    /// <see cref="DojoBackendKind"/> member name.
    /// </exception>
    public static DojoBackendKind Parse(string? value) => value switch
    {
        null => DojoBackendKind.AGUI,
        nameof(DojoBackendKind.AGUI) => DojoBackendKind.AGUI,
        nameof(DojoBackendKind.Direct) => DojoBackendKind.Direct,
        _ => throw new InvalidOperationException(
            $"Invalid DOJO_BACKEND '{value}'. Expected '{nameof(DojoBackendKind.AGUI)}' or " +
            $"'{nameof(DojoBackendKind.Direct)}'."),
    };
}
