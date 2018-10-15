// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

// Workaround for DataProtectionProviderTests.System_UsesProvidedDirectoryAndCertificate
// https://github.com/aspnet/DataProtection/issues/160
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly)]