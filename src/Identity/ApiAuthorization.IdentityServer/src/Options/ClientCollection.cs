// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using Duende.IdentityServer.Models;

namespace Microsoft.AspNetCore.ApiAuthorization.IdentityServer;

/// <summary>
/// A collection of <see cref="Client"/>.
/// </summary>
public class ClientCollection : Collection<Client>
{
    /// <summary>
    /// Initializes a new instance of <see cref="ClientCollection"/>.
    /// </summary>
    public ClientCollection()
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ClientCollection"/> with the given
    /// clients in <paramref name="list"/>.
    /// </summary>
    /// <param name="list">The initial list of <see cref="Client"/>.</param>
    public ClientCollection(IList<Client> list) : base(list)
    {
    }

    /// <summary>
    /// Gets a client given its client id.
    /// </summary>
    /// <param name="key">The name of the <see cref="Client"/>.</param>
    /// <returns>The <see cref="Client"/>.</returns>
    public Client this[string key]
    {
        get
        {
            for (var i = 0; i < Items.Count; i++)
            {
                var candidate = Items[i];
                if (string.Equals(candidate.ClientId, key, StringComparison.Ordinal))
                {
                    return candidate;
                }
            }

            throw new InvalidOperationException($"Client '{key}' not found.");
        }
    }

    /// <summary>
    /// Adds the clients in <paramref name="clients"/> to the collection.
    /// </summary>
    /// <param name="clients">The list of <see cref="Client"/> to add.</param>
    public void AddRange(params Client[] clients)
    {
        foreach (var client in clients)
        {
            Add(client);
        }
    }

    /// <summary>
    /// Adds a single page application that coexists with an authorization server.
    /// </summary>
    /// <param name="clientId">The client id for the single page application.</param>
    /// <param name="configure">The <see cref="Action{ClientBuilder}"/> to configure the default single page application.</param>
    public Client AddIdentityServerSPA(string clientId, Action<ClientBuilder> configure)
    {
        var app = ClientBuilder.IdentityServerSPA(clientId);
        configure(app);
        var client = app.Build();
        Add(client);
        return client;
    }

    /// <summary>
    /// Adds an externally registered single page application.
    /// </summary>
    /// <param name="clientId">The client id for the single page application.</param>
    /// <param name="configure">The <see cref="Action{ClientBuilder}"/> to configure the default single page application.</param>
    public Client AddSPA(string clientId, Action<ClientBuilder> configure)
    {
        var app = ClientBuilder.SPA(clientId);
        configure(app);
        var client = app.Build();
        Add(client);
        return client;
    }

    /// <summary>
    /// Adds an externally registered native application..
    /// </summary>
    /// <param name="clientId">The client id for the single page application.</param>
    /// <param name="configure">The <see cref="Action{ClientBuilder}"/> to configure the native application.</param>
    public Client AddNativeApp(string clientId, Action<ClientBuilder> configure)
    {
        var app = ClientBuilder.NativeApp(clientId);
        configure(app);
        var client = app.Build();
        Add(client);
        return client;
    }
}
