// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace FormatterWebSite;

public class ErrorInfo
{
    public string Source { get; set; }

    public string ActionName { get; set; }

    public string ParameterName { get; set; }

    public List<string> Errors { get; set; }
}
