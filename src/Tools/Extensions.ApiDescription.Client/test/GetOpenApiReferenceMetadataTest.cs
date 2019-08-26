// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Utilities;
using Xunit;

namespace Microsoft.Extensions.ApiDescription.Client
{
    public class GetOpenApiReferenceMetadataTest
    {
        private static readonly Uri AssemblyUri = new Uri(typeof(GetOpenApiReferenceMetadataTest).Assembly.CodeBase);
        private static readonly string AssemblyLocation = Path.GetDirectoryName(AssemblyUri.LocalPath);

        [Fact]
        public void Execute_AddsExpectedMetadata()
        {
            // Arrange
            var identity = Path.Combine("TestProjects", "files", "NSwag.json");
            var @namespace = "Console.Client";
            var outputPath = Path.Combine("obj", "NSwagClient.cs");
            var inputMetadata = new Dictionary<string, string> { { "CodeGenerator", "NSwagCSharp" } };
            var task = new GetOpenApiReferenceMetadata
            {
                Extension = ".cs",
                Inputs = new[] { new TaskItem(identity, inputMetadata) },
                Namespace = @namespace,
                OutputDirectory = "obj",
            };

            IDictionary<string, string> expectedMetadata = new Dictionary<string, string>
            {
                { "ClassName", "NSwagClient" },
                { "CodeGenerator", "NSwagCSharp" },
                { "FirstForGenerator", "true" },
                { "Namespace", "Console.Client" },
                { "OriginalItemSpec", identity },
                { "OutputPath", outputPath },
                {
                    "SerializedMetadata",
                    $"Identity={identity}|CodeGenerator=NSwagCSharp|" +
                    $"OriginalItemSpec={identity}|FirstForGenerator=true|" +
                    $"OutputPath={outputPath}|ClassName=NSwagClient|Namespace={@namespace}"
                },
            };

            // Act
            var result = task.Execute();

            // Assert
            Assert.True(result);
            Assert.False(task.Log.HasLoggedErrors);
            var output = Assert.Single(task.Outputs);
            Assert.Equal(identity, output.ItemSpec);
            var metadata = Assert.IsAssignableFrom<IDictionary<string, string>>(output.CloneCustomMetadata());

            // The dictionary CloneCustomMetadata returns doesn't provide a useful KeyValuePair enumerator.
            var orderedMetadata = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var key in metadata.Keys)
            {
                orderedMetadata.Add(key, metadata[key]);
            }

            Assert.Equal(expectedMetadata, orderedMetadata);
        }
    }
}
