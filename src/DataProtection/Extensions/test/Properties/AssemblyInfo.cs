// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

// Workaround for DataProtectionProviderTests.System_UsesProvidedDirectoryAndCertificate
// https://github.com/aspnet/DataProtection/issues/160
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly)]
