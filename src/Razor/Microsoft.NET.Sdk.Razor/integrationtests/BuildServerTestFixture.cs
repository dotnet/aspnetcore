// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Tools;

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    /// <summary>
    /// A test fixture that initializes a server as part of it's ctor.
    /// Note that this fixture will always initialize a server of the current version since it
    /// invokes the ServerConnection API from the referenced rzc.
    /// </summary>
    public class BuildServerTestFixture : BuildServerTestFixtureBase, IDisposable
    {
        public BuildServerTestFixture() : this(Guid.NewGuid().ToString())
        {
        }

        internal BuildServerTestFixture(string pipeName)
            : base(pipeName)
        {
            if (!ServerConnection.TryCreateServerCore(Environment.CurrentDirectory, pipeName, out _))
            {
                throw new InvalidOperationException($"Failed to start the build server at pipe {pipeName}.");
            }
        }
    }
}
