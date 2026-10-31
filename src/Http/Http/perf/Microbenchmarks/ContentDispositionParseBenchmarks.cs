// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http;

public class ContentDispositionParseBenchmarks
{
    private const string FormField = "form-data; name=\"description\"";
    private const string FileField = "form-data; name=\"attachment\"; filename=\"holiday-photo.jpg\"";

    // Mirrors what FormFeature does for each multipart section.
    [Benchmark]
    public string FormDataSection()
    {
        ContentDispositionHeaderValue.TryParse(FormField, out var cd);
        if (cd!.IsFileDisposition())
        {
            return null!;
        }
        if (cd.IsFormDisposition())
        {
            return HeaderUtilities.RemoveQuotes(cd.Name).ToString();
        }
        return null!;
    }

    [Benchmark]
    public string FileSection()
    {
        ContentDispositionHeaderValue.TryParse(FileField, out var cd);
        if (cd!.IsFileDisposition())
        {
            var name = HeaderUtilities.RemoveQuotes(cd.Name).ToString();
            var fileName = HeaderUtilities.RemoveQuotes(cd.FileNameStar.HasValue ? cd.FileNameStar : cd.FileName).ToString();
            return name + fileName;
        }
        return null!;
    }
}
