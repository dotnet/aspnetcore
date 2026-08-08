// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Components;

// Contracts for the DTOs that only the server hub exchanges. They extend the shared context so that
// they resolve from the same options, and they live here because RootComponentOperationBatch is only
// compiled into this assembly.
[JsonSerializable(typeof(RootComponentOperationBatch))]
[JsonSerializable(typeof(RootComponentOperation))]
internal sealed partial class ServerComponentJsonContext;
