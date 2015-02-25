// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNet.Cors.Core;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Cors
{
    /// <summary>
    /// Exposes methods to build a policy.
    /// </summary>
    public class CorsPolicyBuilder
    {
        private readonly CorsPolicy _policy = new CorsPolicy();

        /// <summary>
        /// Creates a new instance of the <see cref="CorsPolicyBuilder"/>.
        /// </summary>
        /// <param name="origins">list of origins which can be added.</param>
        public CorsPolicyBuilder(params string[] origins)
        {
            WithOrigins(origins);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="CorsPolicyBuilder"/>.
        /// </summary>
        /// <param name="policy">The policy which will be used to intialize the builder.</param>
        public CorsPolicyBuilder(CorsPolicy policy)
        {
            Combine(policy);
        }

        /// <summary>
        /// Adds the specified <paramref name="origins"/> to the policy.
        /// </summary>
        /// <param name="origins">The origins that are allowed.</param>
        /// <returns>The current policy builder</returns>
        public CorsPolicyBuilder WithOrigins(params string[] origins)
        {
            foreach (var req in origins)
            {
                _policy.Origins.Add(req);
            }

            return this;
        }

        /// <summary>
        /// Adds the specified <paramref name="headers"/> to the policy.
        /// </summary>
        /// <param name="headers">The headers which need to be allowed in the request.</param>
        /// <returns>The current policy builder</returns>
        public CorsPolicyBuilder WithHeaders(params string[] headers)
        {
            foreach (var req in headers)
            {
                _policy.Headers.Add(req);
            }
            return this;
        }

        /// <summary>
        /// Adds the specified <paramref name="exposedHeaders"/> to the policy.
        /// </summary>
        /// <param name="exposedHeaders">The headers which need to be exposed to the client.</param>
        /// <returns>The current policy builder</returns>
        public CorsPolicyBuilder WithExposedHeaders(params string[] exposedHeaders)
        {
            foreach (var req in exposedHeaders)
            {
                _policy.ExposedHeaders.Add(req);
            }

            return this;
        }

        /// <summary>
        /// Adds the specified <paramref name="methods"/> to the policy.
        /// </summary>
        /// <param name="methods">The methods which need to be added to the policy.</param>
        /// <returns>The current policy builder</returns>
        public CorsPolicyBuilder WithMethods(params string[] methods)
        {
            foreach (var req in methods)
            {
                _policy.Methods.Add(req);
            }

            return this;
        }

        /// <summary>
        /// Sets the policy to allow credentials.
        /// </summary>
        /// <returns>The current policy builder</returns>
        public CorsPolicyBuilder AllowCredentials()
        {
            _policy.SupportsCredentials = true;
            return this;
        }

        /// <summary>
        /// Sets the policy to not allow credentials.
        /// </summary>
        /// <returns>The current policy builder</returns>
        public CorsPolicyBuilder DisallowCredentials()
        {
            _policy.SupportsCredentials = false;
            return this;
        }

        /// <summary>
        /// Ensures that the policy allows any origin.
        /// </summary>
        /// <returns>The current policy builder</returns>
        public CorsPolicyBuilder AllowAnyOrigin()
        {
            _policy.Origins.Clear();
            _policy.Origins.Add(CorsConstants.AnyOrigin);
            return this;
        }

        /// <summary>
        /// Ensures that the policy allows any method.
        /// </summary>
        /// <returns>The current policy builder</returns>
        public CorsPolicyBuilder AllowAnyMethod()
        {
            _policy.Methods.Clear();
            _policy.Methods.Add("*");
            return this;
        }

        /// <summary>
        /// Ensures that the policy allows any header.
        /// </summary>
        /// <returns>The current policy builder</returns>
        public CorsPolicyBuilder AllowAnyHeader()
        {
            _policy.Headers.Clear();
            _policy.Headers.Add("*");
            return this;
        }

        /// <summary>
        /// Sets the preflightMaxAge for the underlying policy.
        /// </summary>
        /// <param name="preflightMaxAge">A positive <see cref="TimeSpan"/> indicating the time a preflight
        /// request can be cached.</param>
        /// <returns></returns>
        public CorsPolicyBuilder SetPreflightMaxAge(TimeSpan preflightMaxAge)
        {
            _policy.PreflightMaxAge = preflightMaxAge;
            return this;
        }

        /// <summary>
        /// Builds a new <see cref="CorsPolicy"/> using the entries added.
        /// </summary>
        /// <returns>The constructed <see cref="CorsPolicy"/>.</returns>
        public CorsPolicy Build()
        {
            return _policy;
        }

        /// <summary>
        /// Combines the given <paramref name="policy"/> to the existing properties in the builder.
        /// </summary>
        /// <param name="policy">The policy which needs to be combined.</param>
        /// <returns>The current policy builder</returns>
        private CorsPolicyBuilder Combine([NotNull] CorsPolicy policy)
        {
            WithOrigins(policy.Origins.ToArray());
            WithHeaders(policy.Headers.ToArray());
            WithExposedHeaders(policy.ExposedHeaders.ToArray());
            WithMethods(policy.Methods.ToArray());
            SetPreflightMaxAge(policy.PreflightMaxAge.Value);

            if (policy.SupportsCredentials)
            {
                AllowCredentials();
            }
            else
            {
                DisallowCredentials();
            }

            return this;
        }
    }
}