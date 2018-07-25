// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
[assembly: RequiresIIS]
[assembly: OSSkipCondition(OperatingSystems.Windows, WindowsVersions.Win7, SkipReason = "https://github.com/aspnet/IISIntegration/issues/1069")]

