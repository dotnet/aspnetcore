// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace AGUIDojoApi.AgenticGenerativeUI;

[JsonConverter(typeof(JsonStringEnumConverter<PlanStepStatus>))]
internal enum PlanStepStatus
{
    [JsonStringEnumMemberName("pending")]
    Pending,

    [JsonStringEnumMemberName("completed")]
    Completed,
}
