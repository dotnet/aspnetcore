// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components.Forms;

namespace Microsoft.AspNetCore.Components.Web.Internal;

[JsonSerializable(typeof(AntiforgeryRequestToken))]
internal sealed partial class WebPersistentStateJsonContext : JsonSerializerContext;
