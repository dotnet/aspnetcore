// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures
{
    public class ToggleExecutionModeServerFixture<TClientProgram>
        : ServerFixture
    {
        public string PathBase { get; set; }

        public ExecutionMode ExecutionMode { get; set; } = ExecutionMode.Client;

        private AspNetSiteServerFixture.BuildWebHost _buildWebHostMethod;
        private IDisposable _serverToDispose;

        public List<string> AspNetFixtureAdditionalArguments { get; set; } = new List<string>();

        public void UseAspNetHost(AspNetSiteServerFixture.BuildWebHost buildWebHostMethod)
        {
            _buildWebHostMethod = buildWebHostMethod
                ?? throw new ArgumentNullException(nameof(buildWebHostMethod));
        }

        protected override string StartAndGetRootUri()
        {
            if (_buildWebHostMethod == null)
            {
                // Use Blazor's dev host server
                var underlying = new DevHostServerFixture<TClientProgram>();
                underlying.PathBase = PathBase;
                _serverToDispose = underlying;
                return underlying.RootUri.AbsoluteUri;
            }
            else
            {
                // Use specified ASP.NET host server
                var underlying = new AspNetSiteServerFixture();
                underlying.AdditionalArguments.AddRange(AspNetFixtureAdditionalArguments);
                underlying.BuildWebHostMethod = _buildWebHostMethod;
                _serverToDispose = underlying;
                return underlying.RootUri.AbsoluteUri;
            }
        }

        public override void Dispose()
        {
            _serverToDispose?.Dispose();
        }

        internal ToggleExecutionModeServerFixture<TClientProgram> WithAdditionalArguments(string [] additionalArguments)
        {
            AspNetFixtureAdditionalArguments.AddRange(additionalArguments);
            return this;
        }
    }

    public enum ExecutionMode { Client, Server }
}
