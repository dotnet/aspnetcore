// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Analyzer.Testing;

public class TestSource
{
    private const string MarkerStart = "/*MM";
    private const string MarkerEnd = "*/";

    public IDictionary<string, DiagnosticLocation> MarkerLocations { get; }
        = new Dictionary<string, DiagnosticLocation>(StringComparer.Ordinal);

    public DiagnosticLocation DefaultMarkerLocation { get; private set; }

    public string Source { get; private set; }

    public static TestSource Read(string rawSource)
    {
        var testInput = new TestSource();
        var lines = rawSource.Split(new[] { "\n", "\r\n" }, StringSplitOptions.None);
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            while (true)
            {
                var markerStartIndex = line.IndexOf(MarkerStart, StringComparison.Ordinal);
                if (markerStartIndex == -1)
                {
                    break;
                }

                var markerEndIndex = line.IndexOf(MarkerEnd, markerStartIndex, StringComparison.Ordinal);
                var markerName = line.Substring(markerStartIndex + 2, markerEndIndex - markerStartIndex - 2);
                var markerLocation = new DiagnosticLocation(i + 1, markerStartIndex + 1);
                if (testInput.DefaultMarkerLocation == null)
                {
                    testInput.DefaultMarkerLocation = markerLocation;
                }

                testInput.MarkerLocations.Add(markerName, markerLocation);
                line = line.Substring(0, markerStartIndex) + line.Substring(markerEndIndex + MarkerEnd.Length);
            }

            lines[i] = line;
        }

        testInput.Source = string.Join(Environment.NewLine, lines);
        return testInput;
    }
}
