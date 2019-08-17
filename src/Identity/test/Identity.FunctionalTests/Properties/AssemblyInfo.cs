// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Testing;
using Xunit;

// Caused OOM test issues with file watcher. See https://github.com/aspnet/Identity/issues/1926
[assembly: CollectionBehavior(DisableTestParallelization = true)]
