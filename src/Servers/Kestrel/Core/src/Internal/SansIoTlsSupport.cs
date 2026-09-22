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
/// <item><description><see cref="IsSupported"/> - can this runtime do it at all? Determined by
/// probing rather than by testing the OS, because the set of supported platforms is moving
/// (Windows, Linux and macOS in .NET 11; Android is expected in .NET 12) and a hard-coded list
/// would silently exclude platforms as they land.</description></item>
/// <item><description><see cref="IsEnabled"/> - is it turned on? Opt-in, because a sans-IO
/// connection has no <see cref="SslStream"/> to expose, so
/// <c>ISslStreamFeature</c> and <c>Features.Get&lt;SslStream&gt;()</c> cannot be satisfied.
/// That is observable to applications, so it must not change by default.</description></item>
/// </list>
/// </summary>
internal static class SansIoTlsSupport
{
    /// <summary>
    /// Opt-in switch. Off by default; see the remarks on <see cref="SansIoTlsSupport"/> for why
    /// this cannot simply be enabled everywhere it is supported.
    /// </summary>
    internal const string EnableSwitch = "Microsoft.AspNetCore.Server.Kestrel.EnableSansIoTls";

    private static readonly bool _isSupported = ProbeSupport();

    /// <summary>
    /// Whether the running framework provides a usable sans-IO TLS implementation.
    /// </summary>
    public static bool IsSupported => _isSupported;

    /// <summary>
    /// Whether the sans-IO TLS layer should be used. Requires both platform support and opt-in.
    /// </summary>
    public static bool IsEnabled =>
        _isSupported && AppContext.TryGetSwitch(EnableSwitch, out var enabled) && enabled;

    private static bool ProbeSupport()
    {
        try
        {
            // Creating a context is enough to tell whether the platform has an implementation;
            // it does not need a certificate and does not touch the network.
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
