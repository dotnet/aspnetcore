// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Authentication;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Configuration;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal;

internal sealed class ConfigurationReader
{
    private const string ProtocolsKey = "Protocols";
    private const string CertificatesKey = "Certificates";
    private const string CertificateKey = "Certificate";
    private const string SslProtocolsKey = "SslProtocols";
    private const string EndpointDefaultsKey = "EndpointDefaults";
    private const string EndpointsKey = "Endpoints";
    private const string UrlKey = "Url";
    private const string ClientCertificateModeKey = "ClientCertificateMode";
    private const string SniKey = "Sni";

    private readonly IConfiguration _configuration;

    private IDictionary<string, CertificateConfig>? _certificates;
    private EndpointDefaults? _endpointDefaults;
    private IEnumerable<EndpointConfig>? _endpoints;

    public ConfigurationReader(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public IDictionary<string, CertificateConfig> Certificates => _certificates ??= ReadCertificates();
    public EndpointDefaults EndpointDefaults => _endpointDefaults ??= ReadEndpointDefaults();
    public IEnumerable<EndpointConfig> Endpoints => _endpoints ??= ReadEndpoints();

    private IDictionary<string, CertificateConfig> ReadCertificates()
    {
        var certificates = new Dictionary<string, CertificateConfig>(0, StringComparer.OrdinalIgnoreCase);

        var certificatesConfig = _configuration.GetSection(CertificatesKey).GetChildren();
        foreach (var certificateConfig in certificatesConfig)
        {
            certificates.Add(certificateConfig.Key, new CertificateConfig(certificateConfig));
        }

        return certificates;
    }

    // "EndpointDefaults": {
    //     "Protocols": "Http1AndHttp2",
    //     "SslProtocols": [ "Tls11", "Tls12", "Tls13"],
    //     "ClientCertificateMode" : "NoCertificate"
    // }
    private EndpointDefaults ReadEndpointDefaults()
    {
        var configSection = _configuration.GetSection(EndpointDefaultsKey);
        return new EndpointDefaults
        {
            Protocols = ParseProtocols(configSection[ProtocolsKey]),
            SslProtocols = ParseSslProcotols(configSection.GetSection(SslProtocolsKey)),
            ClientCertificateMode = ParseClientCertificateMode(configSection[ClientCertificateModeKey]),
        };
    }

    private IEnumerable<EndpointConfig> ReadEndpoints()
    {
        var endpoints = new List<EndpointConfig>();

        var endpointsConfig = _configuration.GetSection(EndpointsKey).GetChildren();
        foreach (var endpointConfig in endpointsConfig)
        {
            // "EndpointName": {
            //     "Url": "https://*:5463",
            //     "Protocols": "Http1AndHttp2",
            //     "SslProtocols": [ "Tls11", "Tls12", "Tls13"],
            //     "Certificate": {
            //         "Path": "testCert.pfx",
            //         "Password": "testPassword"
            //     },
            //     "ClientCertificateMode" : "NoCertificate",
            //     "Sni": {
            //         "a.example.org": {
            //             "Certificate": {
            //                 "Path": "testCertA.pfx",
            //                 "Password": "testPassword"
            //             }
            //         },
            //         "*.example.org": {
            //             "Protocols": "Http1",
            //         }
            //     }
            // }

            var url = endpointConfig[UrlKey];
            if (string.IsNullOrEmpty(url))
            {
                throw new InvalidOperationException(CoreStrings.FormatEndpointMissingUrl(endpointConfig.Key));
            }

            var endpoint = new EndpointConfig(
                endpointConfig.Key,
                url,
                ReadSni(endpointConfig.GetSection(SniKey), endpointConfig.Key),
                endpointConfig)
            {
                Protocols = ParseProtocols(endpointConfig[ProtocolsKey]),
                SslProtocols = ParseSslProcotols(endpointConfig.GetSection(SslProtocolsKey)),
                ClientCertificateMode = ParseClientCertificateMode(endpointConfig[ClientCertificateModeKey]),
                Certificate = new CertificateConfig(endpointConfig.GetSection(CertificateKey))
            };

            endpoints.Add(endpoint);
        }

        return endpoints;
    }

    private static Dictionary<string, SniConfig> ReadSni(IConfigurationSection sniConfig, string endpointName)
    {
        var sniDictionary = new Dictionary<string, SniConfig>(0, StringComparer.OrdinalIgnoreCase);

        foreach (var sniChild in sniConfig.GetChildren())
        {
            // "Sni": {
            //     "a.example.org": {
            //         "Protocols": "Http1",
            //         "SslProtocols": [ "Tls11", "Tls12", "Tls13"],
            //         "Certificate": {
            //             "Path": "testCertA.pfx",
            //             "Password": "testPassword"
            //         },
            //         "ClientCertificateMode" : "NoCertificate"
            //     },
            //     "*.example.org": {
            //         "Certificate": {
            //             "Path": "testCertWildcard.pfx",
            //             "Password": "testPassword"
            //         }
            //     }
            //     // The following should work once https://github.com/dotnet/runtime/issues/40218 is resolved
            //     "*": {}
            // }

            if (string.IsNullOrEmpty(sniChild.Key))
            {
                throw new InvalidOperationException(CoreStrings.FormatSniNameCannotBeEmpty(endpointName));
            }

            var sni = new SniConfig
            {
                Certificate = new CertificateConfig(sniChild.GetSection(CertificateKey)),
                Protocols = ParseProtocols(sniChild[ProtocolsKey]),
                SslProtocols = ParseSslProcotols(sniChild.GetSection(SslProtocolsKey)),
                ClientCertificateMode = ParseClientCertificateMode(sniChild[ClientCertificateModeKey])
            };

            sniDictionary.Add(sniChild.Key, sni);
        }

        return sniDictionary;
    }

    private static ClientCertificateMode? ParseClientCertificateMode(string? clientCertificateMode)
    {
        if (Enum.TryParse<ClientCertificateMode>(clientCertificateMode, ignoreCase: true, out var result))
        {
            return result;
        }

        return null;
    }

    private static HttpProtocols? ParseProtocols(string? protocols)
    {
        if (Enum.TryParse<HttpProtocols>(protocols, ignoreCase: true, out var result))
        {
            return result;
        }

        return null;
    }

    private static SslProtocols? ParseSslProcotols(IConfigurationSection sslProtocols)
    {
        // Avoid trimming warning from IConfigurationSection.Get<string[]>()
        string[]? stringProtocols = null;
        var childrenSections = sslProtocols.GetChildren().ToArray();
        if (childrenSections.Length > 0)
        {
            stringProtocols = new string[childrenSections.Length];
            for (var i = 0; i < childrenSections.Length; i++)
            {
                stringProtocols[i] = childrenSections[i].Value!;
            }
        }

        return stringProtocols?.Aggregate(SslProtocols.None, (acc, current) =>
        {
            if (Enum.TryParse(current, ignoreCase: true, out SslProtocols parsed))
            {
                return acc | parsed;
            }

            return acc;
        });
    }

    internal static void ThrowIfContainsHttpsOnlyConfiguration(EndpointConfig endpoint)
    {
        if (endpoint.Certificate != null && (endpoint.Certificate.IsFileCert || endpoint.Certificate.IsStoreCert))
        {
            throw new InvalidOperationException(CoreStrings.FormatEndpointHasUnusedHttpsConfig(endpoint.Name, CertificateKey));
        }

        if (endpoint.ClientCertificateMode.HasValue)
        {
            throw new InvalidOperationException(CoreStrings.FormatEndpointHasUnusedHttpsConfig(endpoint.Name, ClientCertificateModeKey));
        }

        if (endpoint.SslProtocols.HasValue)
        {
            throw new InvalidOperationException(CoreStrings.FormatEndpointHasUnusedHttpsConfig(endpoint.Name, SslProtocolsKey));
        }

        if (endpoint.Sni.Count > 0)
        {
            throw new InvalidOperationException(CoreStrings.FormatEndpointHasUnusedHttpsConfig(endpoint.Name, SniKey));
        }
    }
}

// "EndpointDefaults": {
//     "Protocols": "Http1AndHttp2",
//     "SslProtocols": [ "Tls11", "Tls12", "Tls13"],
//     "ClientCertificateMode" : "NoCertificate"
// }
internal sealed class EndpointDefaults
{
    public HttpProtocols? Protocols { get; set; }
    public SslProtocols? SslProtocols { get; set; }
    public ClientCertificateMode? ClientCertificateMode { get; set; }
}

// "EndpointName": {
//     "Url": "https://*:5463",
//     "Protocols": "Http1AndHttp2",
//     "SslProtocols": [ "Tls11", "Tls12", "Tls13"],
//     "Certificate": {
//         "Path": "testCert.pfx",
//         "Password": "testPassword"
//     },
//     "ClientCertificateMode" : "NoCertificate",
//     "Sni": {
//         "a.example.org": {
//             "Certificate": {
//                 "Path": "testCertA.pfx",
//                 "Password": "testPasswordA"
//             }
//         },
//         "*.example.org": {
//             "Protocols": "Http1",
//         }
//     }
// }
internal sealed class EndpointConfig
{
    private readonly ConfigSectionClone _configSectionClone;

    public EndpointConfig(
        string name,
        string url,
        Dictionary<string, SniConfig> sni,
        IConfigurationSection configSection)
    {
        Name = name;
        Url = url;
        Sni = sni;

        // Compare config sections because it's accessible to app developers via an Action<EndpointConfiguration> callback.
        // We cannot rely entirely on comparing config sections for equality, because KestrelConfigurationLoader.Reload() sets
        // EndpointConfig properties to their default values. If a default value changes, the properties would no longer be equal,
        // but the config sections could still be equal.
        ConfigSection = configSection;
        // The IConfigrationSection will mutate, so we need to take a snapshot to compare against later and check for changes.
        _configSectionClone = new ConfigSectionClone(configSection);
    }

    public string Name { get; }
    public string Url { get; }
    public Dictionary<string, SniConfig> Sni { get; }
    public IConfigurationSection ConfigSection { get; }

    public HttpProtocols? Protocols { get; set; }
    public SslProtocols? SslProtocols { get; set; }
    public CertificateConfig? Certificate { get; set; }
    public ClientCertificateMode? ClientCertificateMode { get; set; }

    public override bool Equals(object? obj) =>
        obj is EndpointConfig other &&
        Name == other.Name &&
        Url == other.Url &&
        (Protocols ?? ListenOptions.DefaultHttpProtocols) == (other.Protocols ?? ListenOptions.DefaultHttpProtocols) &&
        (SslProtocols ?? System.Security.Authentication.SslProtocols.None) == (other.SslProtocols ?? System.Security.Authentication.SslProtocols.None) &&
        Certificate == other.Certificate &&
        (ClientCertificateMode ?? Https.ClientCertificateMode.NoCertificate) == (other.ClientCertificateMode ?? Https.ClientCertificateMode.NoCertificate) &&
        CompareSniDictionaries(Sni, other.Sni) &&
        _configSectionClone == other._configSectionClone;

    public override int GetHashCode() => HashCode.Combine(Name, Url,
        Protocols ?? ListenOptions.DefaultHttpProtocols, SslProtocols ?? System.Security.Authentication.SslProtocols.None,
        Certificate, ClientCertificateMode ?? Https.ClientCertificateMode.NoCertificate, Sni.Count, _configSectionClone);

    public static bool operator ==(EndpointConfig? lhs, EndpointConfig? rhs) => lhs is null ? rhs is null : lhs.Equals(rhs);
    public static bool operator !=(EndpointConfig? lhs, EndpointConfig? rhs) => !(lhs == rhs);

    private static bool CompareSniDictionaries(Dictionary<string, SniConfig> lhs, Dictionary<string, SniConfig> rhs)
    {
        if (lhs.Count != rhs.Count)
        {
            return false;
        }

        foreach (var (lhsName, lhsSniConfig) in lhs)
        {
            if (!rhs.TryGetValue(lhsName, out var rhsSniConfig) || lhsSniConfig != rhsSniConfig)
            {
                return false;
            }
        }

        return true;
    }
}

internal sealed class SniConfig
{
    public HttpProtocols? Protocols { get; set; }
    public SslProtocols? SslProtocols { get; set; }
    public CertificateConfig? Certificate { get; set; }
    public ClientCertificateMode? ClientCertificateMode { get; set; }

    public override bool Equals(object? obj) =>
        obj is SniConfig other &&
        (Protocols ?? ListenOptions.DefaultHttpProtocols) == (other.Protocols ?? ListenOptions.DefaultHttpProtocols) &&
        (SslProtocols ?? System.Security.Authentication.SslProtocols.None) == (other.SslProtocols ?? System.Security.Authentication.SslProtocols.None) &&
        Certificate == other.Certificate &&
        (ClientCertificateMode ?? Https.ClientCertificateMode.NoCertificate) == (other.ClientCertificateMode ?? Https.ClientCertificateMode.NoCertificate);

    public override int GetHashCode() => HashCode.Combine(
        Protocols ?? ListenOptions.DefaultHttpProtocols, SslProtocols ?? System.Security.Authentication.SslProtocols.None,
        Certificate, ClientCertificateMode ?? Https.ClientCertificateMode.NoCertificate);

    public static bool operator ==(SniConfig lhs, SniConfig rhs) => lhs is null ? rhs is null : lhs.Equals(rhs);
    public static bool operator !=(SniConfig lhs, SniConfig rhs) => !(lhs == rhs);
}

// "CertificateName": {
//     "Path": "testCert.pfx",
//     "Password": "testPassword"
// }
internal sealed class CertificateConfig
{
    public CertificateConfig(IConfigurationSection configSection)
    {
        ConfigSection = configSection;

        // Bind explictly to preserve linkability
        Path = configSection[nameof(Path)];
        KeyPath = configSection[nameof(KeyPath)];
        Password = configSection[nameof(Password)];
        Subject = configSection[nameof(Subject)];
        Store = configSection[nameof(Store)];
        Location = configSection[nameof(Location)];

        if (bool.TryParse(configSection[nameof(AllowInvalid)], out var value))
        {
            AllowInvalid = value;
        }
    }

    // For testing
    internal CertificateConfig()
    {
    }

    public IConfigurationSection? ConfigSection { get; }

    // File

    [MemberNotNullWhen(true, nameof(Path))]
    public bool IsFileCert => !string.IsNullOrEmpty(Path);

    public string? Path { get; init; }

    public string? KeyPath { get; init; }

    public string? Password { get; init; }

    /// <remarks>
    /// Vacuously false if this isn't a file cert.
    /// Used for change tracking - not actually part of configuring the certificate.
    /// </remarks>
    public bool FileHasChanged { get; internal set; }

    // Cert store

    [MemberNotNullWhen(true, nameof(Subject))]
    public bool IsStoreCert => !string.IsNullOrEmpty(Subject);

    public string? Subject { get; init; }

    public string? Store { get; init; }

    public string? Location { get; init; }

    public bool? AllowInvalid { get; init; }

    public override bool Equals(object? obj) =>
        obj is CertificateConfig other &&
        Path == other.Path &&
        KeyPath == other.KeyPath &&
        Password == other.Password &&
        FileHasChanged == other.FileHasChanged &&
        Subject == other.Subject &&
        Store == other.Store &&
        Location == other.Location &&
        (AllowInvalid ?? false) == (other.AllowInvalid ?? false);

    public override int GetHashCode() => HashCode.Combine(Path, KeyPath, Password, FileHasChanged, Subject, Store, Location, AllowInvalid ?? false);

    public static bool operator ==(CertificateConfig? lhs, CertificateConfig? rhs) => lhs is null ? rhs is null : lhs.Equals(rhs);
    public static bool operator !=(CertificateConfig? lhs, CertificateConfig? rhs) => !(lhs == rhs);
}
