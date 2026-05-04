// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Xunit;

namespace TestApp.E2E.Tests.Fixtures;

[CollectionDefinition(nameof(E2ECollection))]
public class E2ECollection : ICollectionFixture<ServerFixture<E2ETestAssembly>>;
