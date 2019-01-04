// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// All functional tests in this project require a version of IIS express with an updated schema
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
[assembly: OSSkipCondition(OperatingSystems.MacOSX | OperatingSystems.Linux)]
