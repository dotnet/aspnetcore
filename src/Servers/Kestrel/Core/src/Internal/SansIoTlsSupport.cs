// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Security;
using System.Security.Authentication;

namespace Microsoft.AspNetCore.Server.Kestrel.Https.Internal;

/// <summary>
/// Decides whether a connection may use the sans-IO TLS layer (<see cref="TlsBufferSession"/>)
/// instead of <see cref="SslStream"/>.
///
/// Two separate questions, deliberately kept apart:
///
/// <list type="bullet">
/// <item><description><see cref="IsSupported"/> - may this runtime use it? This is both a
/// capability probe and a list of platforms the layer has actually been tested on. The probe
/// alone is not enough: it succeeds on macOS today, where neither this layer nor the runtime
/// implementation has had meaningful coverage.</description></item>
/// <item><description><see cref="IsEnabled"/> - should it be used? On a validated platform
/// this replaces <see cref="SslStream"/> outright; <see cref="DisableSwitch"/> exists only as
/// a kill switch. Note that a sans-IO connection has no <see cref="SslStream"/> to expose, so
/// <c>ISslStreamFeature</c> and <c>Features.Get&lt;SslStream&gt;()</c> cannot be satisfied on
/// those platforms.</description></item>
/// </list>
/// </summary>
internal static class SansIoTlsSupport
{
    /// <summary>
    /// Kill switch, for falling back to <see cref="SslStream"/> without a redeploy.
    /// </summary>
    internal const string DisableSwitch = "Microsoft.AspNetCore.Server.Kestrel.DisableSansIoTls";

    private static readonly bool _isSupported = IsValidatedPlatform() && ProbeSupport();

    /// <summary>
    /// Whether the running framework provides a usable sans-IO TLS implementation *and* this
    /// platform is one the layer has actually been exercised on.
    /// </summary>
    public static bool IsSupported => _isSupported;

    /// <summary>
    /// Whether the sans-IO TLS layer should be used for new connections.
    /// </summary>
    public static bool IsEnabled =>
        _isSupported && !(AppContext.TryGetSwitch(DisableSwitch, out var disabled) && disabled);

    /// <summary>
    /// Platforms this layer has been tested on, in Kestrel and in the runtime.
    ///
    /// This is deliberately *not* the same question as "does the API exist here". The probe
    /// below answers that, and today it answers yes on macOS too - but neither this layer nor
    /// the runtime implementation has had meaningful coverage there yet, and a capability
    /// probe alone would switch it on silently. Extend this list as platforms are validated,
    /// not as they gain an implementation.
    /// </summary>
    private static bool IsValidatedPlatform()
        => OperatingSystem.IsWindows() || OperatingSystem.IsLinux();

    private static bool ProbeSupport()
    {
        try
        {
            // Creating a context is enough to tell whether the platform has an implementation;
            // it does not need a certificate and does not touch the network. This guards against
            // a runtime where the API is absent or trimmed even though the OS is listed above.
            using var context = TlsContext.CreateServer(new SslServerAuthenticationOptions
            {
                EnabledSslProtocols = SslProtocols.None,
            });

            return true;
        }
        catch (PlatformNotSupportedException)
        {
            return false;
        }
        catch (NotSupportedException)
        {
            return false;
        }
        catch
        {
            // Anything else means the implementation is present and rejected these particular
            // options - which still answers the question being asked here. Treating it as
            // unsupported would silently disable the feature on a platform that has it, and
            // letting it escape would fail at static initialisation.
            return true;
        }
    }
}
