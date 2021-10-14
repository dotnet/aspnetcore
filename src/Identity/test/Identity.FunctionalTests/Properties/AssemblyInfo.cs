// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Testing;
using Xunit;

// Caused OOM test issues with file watcher. See https://github.com/aspnet/Identity/issues/1926
[assembly: CollectionBehavior(DisableTestParallelization = true)]
