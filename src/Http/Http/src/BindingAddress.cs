// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// An address that a HTTP server may bind to.
/// </summary>
public class BindingAddress
{
    private const string UnixPipeHostPrefix = "unix:/";
    private const string NamedPipeHostPrefix = "pipe:/";

    private BindingAddress(string host, string pathBase, int port, string scheme)
    {
        Host = host;
        PathBase = pathBase;
        Port = port;
        Scheme = scheme;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="BindingAddress"/>.
    /// </summary>
    [Obsolete("This constructor is obsolete and will be removed in a future version. Use BindingAddress.Parse(address) to create a BindingAddress instance.")]
    public BindingAddress()
    {
        throw new InvalidOperationException("This constructor is obsolete and will be removed in a future version. Use BindingAddress.Parse(address) to create a BindingAddress instance.");
    }

    /// <summary>
    /// Gets the host component.
    /// </summary>
    public string Host { get; }

    /// <summary>
    /// Gets the path component.
    /// </summary>
    public string PathBase { get; }

    /// <summary>
    /// Gets the port.
    /// </summary>
    public int Port { get; }

    /// <summary>
    /// Gets the scheme component.
    /// </summary>
    public string Scheme { get; }

    /// <summary>
    /// Gets a value that determines if this instance represents a Unix pipe.
    /// <para>
    /// Returns <see langword="true"/> if <see cref="Host"/> starts with <c>unix://</c> prefix.
    /// </para>
    /// </summary>
    public bool IsUnixPipe => Host.StartsWith(UnixPipeHostPrefix, StringComparison.Ordinal);

    /// <summary>
    /// Gets a value that determines if this instance represents a named pipe.
    /// <para>
    /// Returns <see langword="true"/> if <see cref="Host"/> starts with <c>pipe:/</c> prefix.
    /// </para>
    /// </summary>
    public bool IsNamedPipe => Host.StartsWith(NamedPipeHostPrefix, StringComparison.Ordinal);

    /// <summary>
    /// Gets the unix pipe path if this instance represents a Unix pipe.
    /// </summary>
    public string UnixPipePath
    {
        get
        {
            if (!IsUnixPipe)
            {
                throw new InvalidOperationException("Binding address is not a unix pipe.");
            }

            return GetUnixPipePath(Host);
        }
    }

    /// <summary>
    /// Gets the named pipe name if this instance represents a named pipe.
    /// </summary>
    public string NamedPipeName
    {
        get
        {
            if (!IsNamedPipe)
            {
                throw new InvalidOperationException("Binding address is not a named pipe.");
            }

            return GetNamedPipeName(Host);
        }
    }

    private static string GetUnixPipePath(string host)
    {
        var unixPipeHostPrefixLength = UnixPipeHostPrefix.Length;
        if (!OperatingSystem.IsWindows())
        {
            // "/" character in unix refers to root. Windows has drive letters and volume separator (c:)
            unixPipeHostPrefixLength--;
        }
        return host.Substring(unixPipeHostPrefixLength);
    }

    private static string GetNamedPipeName(string host) => host.Substring(NamedPipeHostPrefix.Length);

    /// <inheritdoc />
    public override string ToString()
    {
        if (IsUnixPipe || IsNamedPipe)
        {
            return Scheme.ToLowerInvariant() + Uri.SchemeDelimiter + Host.ToLowerInvariant();
        }
        else
        {
            return Scheme.ToLowerInvariant() + Uri.SchemeDelimiter + Host.ToLowerInvariant() + ":" + Port.ToString(CultureInfo.InvariantCulture) + PathBase;
        }
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return ToString().GetHashCode();
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        var other = obj as BindingAddress;
        if (other == null)
        {
            return false;
        }
        return string.Equals(Scheme, other.Scheme, StringComparison.OrdinalIgnoreCase)
            && string.Equals(Host, other.Host, StringComparison.OrdinalIgnoreCase)
            && Port == other.Port
            && PathBase == other.PathBase;
    }

    /// <summary>
    /// Parses the specified <paramref name="address"/> as a <see cref="BindingAddress"/>.
    /// </summary>
    /// <param name="address">The address to parse.</param>
    /// <returns>The parsed address.</returns>
    public static BindingAddress Parse(string address)
    {
        // A null/empty address will throw FormatException
        address = address ?? string.Empty;

        var schemeDelimiterStart = address.IndexOf(Uri.SchemeDelimiter, StringComparison.Ordinal);
        if (schemeDelimiterStart < 0)
        {
            throw new FormatException($"Invalid url: '{address}'");
        }
        var schemeDelimiterEnd = schemeDelimiterStart + Uri.SchemeDelimiter.Length;

        var isUnixPipe = address.IndexOf(UnixPipeHostPrefix, schemeDelimiterEnd, StringComparison.Ordinal) == schemeDelimiterEnd;
        var isNamedPipe = address.IndexOf(NamedPipeHostPrefix, schemeDelimiterEnd, StringComparison.Ordinal) == schemeDelimiterEnd;

        int pathDelimiterStart;
        int pathDelimiterEnd;
        if (isUnixPipe)
        {
            var unixPipeHostPrefixLength = UnixPipeHostPrefix.Length;
            if (OperatingSystem.IsWindows())
            {
                // Windows has drive letters and volume separator (c:)
                unixPipeHostPrefixLength += 2;
                if (schemeDelimiterEnd + unixPipeHostPrefixLength > address.Length)
                {
                    throw new FormatException($"Invalid url: '{address}'");
                }
            }

            pathDelimiterStart = address.IndexOf(':', schemeDelimiterEnd + unixPipeHostPrefixLength);
            pathDelimiterEnd = pathDelimiterStart + ":".Length;
        }
        else if (isNamedPipe)
        {
            pathDelimiterStart = address.IndexOf(':', schemeDelimiterEnd + NamedPipeHostPrefix.Length);
            pathDelimiterEnd = pathDelimiterStart + ":".Length;
        }
        else
        {
            pathDelimiterStart = address.IndexOf('/', schemeDelimiterEnd);
            pathDelimiterEnd = pathDelimiterStart;
        }

        if (pathDelimiterStart < 0)
        {
            pathDelimiterStart = pathDelimiterEnd = address.Length;
        }

        var scheme = address.Substring(0, schemeDelimiterStart);
        string? host = null;
        var port = 0;

        var hasSpecifiedPort = false;
        if (!isUnixPipe)
        {
            var portDelimiterStart = address.LastIndexOf(':', pathDelimiterStart - 1, pathDelimiterStart - schemeDelimiterEnd);
            if (portDelimiterStart >= 0)
            {
                var portDelimiterEnd = portDelimiterStart + ":".Length;

                var portString = address.Substring(portDelimiterEnd, pathDelimiterStart - portDelimiterEnd);
                int portNumber;
                if (int.TryParse(portString, NumberStyles.Integer, CultureInfo.InvariantCulture, out portNumber))
                {
                    hasSpecifiedPort = true;
                    host = address.Substring(schemeDelimiterEnd, portDelimiterStart - schemeDelimiterEnd);
                    port = portNumber;
                }
            }

            if (!hasSpecifiedPort)
            {
                if (string.Equals(scheme, "http", StringComparison.OrdinalIgnoreCase))
                {
                    port = 80;
                }
                else if (string.Equals(scheme, "https", StringComparison.OrdinalIgnoreCase))
                {
                    port = 443;
                }
            }
        }

        if (!hasSpecifiedPort)
        {
            host = address.Substring(schemeDelimiterEnd, pathDelimiterStart - schemeDelimiterEnd);
        }

        if (string.IsNullOrEmpty(host))
        {
            throw new FormatException($"Invalid url: '{address}'");
        }

        if (isUnixPipe && !Path.IsPathRooted(GetUnixPipePath(host)))
        {
            throw new FormatException($"Invalid url, unix socket path must be absolute: '{address}'");
        }

        string pathBase;
        if (address[address.Length - 1] == '/')
        {
            pathBase = address.Substring(pathDelimiterEnd, address.Length - pathDelimiterEnd - 1);
        }
        else
        {
            pathBase = address.Substring(pathDelimiterEnd);
        }

        return new BindingAddress(host: host, pathBase: pathBase, port: port, scheme: scheme);
    }
}
