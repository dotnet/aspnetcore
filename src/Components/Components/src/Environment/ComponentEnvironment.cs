// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components.Services;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Environment
{
    /// <summary>
    /// Provides information an environment specific features about the environment the components are running on.
    /// </summary>
    public class ComponentEnvironment
    {
        /// <summary>
        /// Name of associated with the <see cref="ComponentEnvironment"/> when components are being remotely rendered on the server.
        /// </summary>
        public const string Remote = nameof(Remote);

        /// <summary>
        /// Name of associated with the <see cref="ComponentEnvironment"/> when components are being prerrendered on the server.
        /// </summary>
        public const string Prerrender = nameof(Prerrender);

        /// <summary>
        /// Name of associated with the <see cref="ComponentEnvironment"/> when components are being locally rendered on the client.
        /// </summary>
        public const string Local = nameof(Local);

        /// <summary>
        /// Gets or sets identifier associated with the <see cref="ComponentEnvironment"/> the components are running on.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="IJSRuntime"/> for this <see cref="ComponentEnvironment"/>.
        /// </summary>
        public IJSRuntime JSRuntime { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="IUriHelper"/> for this <see cref="ComponentEnvironment"/>.
        /// </summary>
        public IUriHelper UriHelper { get; set; }
    }
}
