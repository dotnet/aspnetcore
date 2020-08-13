// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Extensions.ApiDescription.Client
{
    public class GetCurrentOpenApiReferenceTest
    {
        [Fact]
        public void Execute_ReturnsExpectedItem()
        {
            // Arrange
            var input = "Identity=../files/azureMonitor.json|ClassName=azureMonitorClient|" +
                "CodeGenerator=NSwagCSharp|Namespace=ConsoleClient|Options=|OutputPath=" +
                "C:\\dd\\dnx\\AspNetCore\\artifacts\\obj\\ConsoleClient\\azureMonitorClient.cs|" +
                "OriginalItemSpec=../files/azureMonitor.json|FirstForGenerator=true";
            var task = new GetCurrentOpenApiReference
            {
                Input = input,
            };

            var expectedIdentity = "../files/azureMonitor.json";
            var expectedMetadata = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                { "ClassName", "azureMonitorClient" },
                { "CodeGenerator", "NSwagCSharp" },
                { "FirstForGenerator", "true" },
                { "Namespace", "ConsoleClient" },
                { "Options", "" },
                { "OriginalItemSpec", expectedIdentity },
                { "OutputPath", "C:\\dd\\dnx\\AspNetCore\\artifacts\\obj\\ConsoleClient\\azureMonitorClient.cs" },
            };

            // Act
            var result = task.Execute();

            // Assert
            Assert.True(result);
            Assert.False(task.Log.HasLoggedErrors);
            var output = Assert.Single(task.Outputs);
            Assert.Equal(expectedIdentity, output.ItemSpec);
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
