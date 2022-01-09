// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Mvc.NewtonsoftJson;

internal class AnnotatedProblemDetails
{
    /// <remarks>
    /// Required for JSON.NET deserialization.
    /// </remarks>
    public AnnotatedProblemDetails() { }

    public AnnotatedProblemDetails(ProblemDetails problemDetails)
    {
        Detail = problemDetails.Detail;
        Instance = problemDetails.Instance;
        Status = problemDetails.Status;
        Title = problemDetails.Title;
        Type = problemDetails.Type;

        foreach (var kvp in problemDetails.Extensions)
        {
            Extensions[kvp.Key] = kvp.Value;
        }
    }

    [JsonProperty(PropertyName = "type", NullValueHandling = NullValueHandling.Ignore)]
    public string? Type { get; set; }

    [JsonProperty(PropertyName = "title", NullValueHandling = NullValueHandling.Ignore)]
    public string? Title { get; set; }

    [JsonProperty(PropertyName = "status", NullValueHandling = NullValueHandling.Ignore)]
    public int? Status { get; set; }

    [JsonProperty(PropertyName = "detail", NullValueHandling = NullValueHandling.Ignore)]
    public string? Detail { get; set; }

    [JsonProperty(PropertyName = "instance", NullValueHandling = NullValueHandling.Ignore)]
    public string? Instance { get; set; }

    [JsonExtensionData]
    public IDictionary<string, object?> Extensions { get; } = new Dictionary<string, object?>(StringComparer.Ordinal);

    public void CopyTo(ProblemDetails problemDetails)
    {
        problemDetails.Type = Type;
        problemDetails.Title = Title;
        problemDetails.Status = Status;
        problemDetails.Instance = Instance;
        problemDetails.Detail = Detail;

        foreach (var kvp in Extensions)
        {
            problemDetails.Extensions[kvp.Key] = kvp.Value;
        }
    }
}
