// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Testing;
using Xunit;

[assembly: OSSkipCondition(OperatingSystems.MacOSX)]
[assembly: OSSkipCondition(OperatingSystems.Linux)]
