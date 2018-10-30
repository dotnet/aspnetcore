// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.AspNetCore.Analyzer.Testing
{
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
            var lines = rawSource.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var markerStartIndex = line.IndexOf(MarkerStart, StringComparison.Ordinal);
                if (markerStartIndex != -1)
                {
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
}
