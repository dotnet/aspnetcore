// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Components;

[JsonSerializable(typeof(RootComponentOperationBatch))]
[JsonSerializable(typeof(RootComponentOperation))]
internal sealed partial class ServerComponentJsonContext;
