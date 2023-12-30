// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Authentication.Negotiate;

/// <summary>
/// Options class provides information needed to control Negotiate Authentication handler behavior
/// </summary>
public class NegotiateOptions : AuthenticationSchemeOptions
{
    /// <summary>
    /// The object provided by the application to process events raised by the negotiate authentication handler.
    /// The application may use the existing NegotiateEvents instance and assign delegates only to the events it
    /// wants to process. The application may also replace it with its own derived instance.
    /// </summary>
    public new NegotiateEvents? Events
    {
        get { return (NegotiateEvents?)base.Events; }
        set { base.Events = value; }
    }

    /// <summary>
    /// Indicates if Kerberos credentials should be persisted and re-used for subsquent anonymous requests.
    /// This option must not be used if connections may be shared by requests from different users.
    /// </summary>
    /// <value>Defaults to <see langword="false"/>.</value>
    public bool PersistKerberosCredentials { get; set; }

    /// <summary>
    /// Indicates if NTLM credentials should be persisted and re-used for subsquent anonymous requests.
    /// This option must not be used if connections may be shared by requests from different users.
    /// </summary>
    /// <value>Defaults to <see langword="true"/>.</value>
    public bool PersistNtlmCredentials { get; set; } = true;

    /// <summary>
    /// Configuration settings for LDAP connections used to retrieve claims.
    /// This should only be used on Linux systems.
    /// </summary>
    internal LdapSettings LdapSettings { get; } = new LdapSettings();

    /// <summary>
    /// Use LDAP connections used to retrieve claims for the given domain.
    /// This should only be used on Linux systems.
    /// </summary>
    public void EnableLdap(string domain)
    {
        ArgumentException.ThrowIfNullOrEmpty(domain);

        LdapSettings.EnableLdapClaimResolution = true;
        LdapSettings.Domain = domain;
    }

    /// <summary>
    /// Use LDAP connections used to retrieve claims using the configured settings.
    /// This should only be used on Linux systems.
    /// </summary>
    public void EnableLdap(Action<LdapSettings> configureSettings)
    {
        ArgumentNullException.ThrowIfNull(configureSettings);

        LdapSettings.EnableLdapClaimResolution = true;
        configureSettings(LdapSettings);
    }

    /// <summary>
    /// Indicates if integrated server Windows Auth is being used instead of this handler.
    /// See <see cref="PostConfigureNegotiateOptions"/>.
    /// </summary>
    internal bool DeferToServer { get; set; }

    // For testing
    internal INegotiateStateFactory StateFactory { get; set; } = new NegotiateStateFactory();
}
