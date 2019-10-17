// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Moq;
using Xunit;

namespace Microsoft.Extensions.ApiDescription.Client
{
    // ItemSpec values always have '\\' converted to '/' on input when running on non-Windows. It is not possible to
    // retrieve the original (unconverted) item spec value. In other respects, item spec values are treated identically
    // to custom metadata values.
    //
    // ITaskItem members aka the implicitly-implemented methods and properties in TaskItem expect _escaped_ values on
    // input and return _literal_ values. This includes TaskItem constructors and CloneCustomMetadata() (which returns
    // a new dictionary containing literal values). TaskItem stores all values in their escaped form.
    //
    // Added ITaskItem2 members e.g. CloneCustomMetadataEscaped(), GetMetadataValueEscaped(...) and
    // EvaluatedIncludeEscaped return escaped values. Of all TaskItem methods, only SetMetadataValueLiteral(...)
    // accepts a literal input value.
    //
    // Metadata names are never escaped.
    //
    // MetadataSerializer expects literal values on input.
    public class MetadataSerializerTest
    {
        // Maps literal to escaped values.
        public static TheoryData<string, string> EscapedValuesMapping { get; } = new TheoryData<string, string>
        {
          { "No escaping necessary for =.", "No escaping necessary for =." },
          { "Value needs escaping? (yes)", "Value needs escaping%3f %28yes%29" },
          { "$ comes earlier; @ comes later.", "%24 comes earlier%3b %40 comes later." },
          {
            "A '%' *character* needs escaping %-escaping.",
            "A %27%25%27 %2acharacter%2a needs escaping %25-escaping."
          },
        };

        public static TheoryData<string> EscapedValues
        {
            get
            {
                var result = new TheoryData<string>();
                foreach (var entry in EscapedValuesMapping)
                {
                    result.Add((string)entry[1]);
                }

                return result;
            }
        }

        public static TheoryData<string> LiteralValues
        {
            get
            {
                var result = new TheoryData<string>();
                foreach (var entry in EscapedValuesMapping)
                {
                    result.Add((string)entry[0]);
                }

                return result;
            }
        }

        [Theory]
        [MemberData(nameof(LiteralValues))]
        public void SetMetadata_UpdatesTaskAsExpected(string value)
        {
            // Arrange
            var item = new TaskItem("My Identity");
            var key = "My key";

            // Act
            MetadataSerializer.SetMetadata(item, key, value);

            // Assert
            Assert.Equal(value, item.GetMetadata(key));
        }

        [Theory]
        [MemberData(nameof(EscapedValuesMapping))]
        public void SetMetadata_UpdatesTaskAsExpected_WithLegacyItem(string value, string escapedValue)
        {
            // Arrange
            var item = new Mock<ITaskItem>(MockBehavior.Strict);
            var key = "My key";
            item.Setup(i => i.SetMetadata(key, escapedValue)).Verifiable();

            // Act
            MetadataSerializer.SetMetadata(item.Object, key, value);

            // Assert
            item.Verify(i => i.SetMetadata(key, escapedValue), Times.Once);
        }

        [Fact]
        public void DeserializeMetadata_ReturnsExpectedTask()
        {
            // Arrange
            var identity = "../files/azureMonitor.json";
            var input = $"Identity={identity}|ClassName=azureMonitorClient|" +
                "CodeGenerator=NSwagCSharp|FirstForGenerator=true|Namespace=ConsoleClient|" +
                "Options=|OriginalItemSpec=../files/azureMonitor.json|" +
                "OutputPath=C:\\dd\\dnx\\AspNetCore\\artifacts\\obj\\ConsoleClient\\azureMonitorClient.cs";

            var expectedMetadata = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                { "ClassName", "azureMonitorClient" },
                { "CodeGenerator", "NSwagCSharp" },
                { "FirstForGenerator", "true" },
                { "Namespace", "ConsoleClient" },
                { "Options", "" },
                { "OriginalItemSpec", identity },
                { "OutputPath", "C:\\dd\\dnx\\AspNetCore\\artifacts\\obj\\ConsoleClient\\azureMonitorClient.cs" },
            };

            // Act
            var item = MetadataSerializer.DeserializeMetadata(input);

            // Assert
            Assert.Equal(identity, item.ItemSpec);
            var metadata = Assert.IsAssignableFrom<IDictionary<string, string>>(item.CloneCustomMetadata());

            // The dictionary CloneCustomMetadata returns doesn't provide a useful KeyValuePair enumerator.
            var orderedMetadata = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var key in metadata.Keys)
            {
                orderedMetadata.Add(key, metadata[key]);
            }

            Assert.Equal(expectedMetadata, orderedMetadata);

        }

        [Theory]
        [MemberData(nameof(EscapedValuesMapping))]
        public void DeserializeMetadata_ReturnsExpectedTask_WhenEscaping(string value, string escapedValue)
        {
            // Arrange
            var identity = "../files/azureMonitor.json";
            var input = $"Identity={identity}|Value={escapedValue}";

            // Act
            var item = MetadataSerializer.DeserializeMetadata(input);

            // Assert
            Assert.Equal(identity, item.ItemSpec);
            Assert.Equal(value, item.GetMetadata("Value"));
        }

        [Theory]
        [MemberData(nameof(EscapedValuesMapping))]
        public void DeserializeMetadata_ReturnsExpectedTask_WhenEscapingIdentity(string value, string escapedValue)
        {
            // Arrange
            var input = $"Identity={escapedValue}|Value=a value";

            // Act
            var item = MetadataSerializer.DeserializeMetadata(input);

            // Assert
            Assert.Equal(value, item.ItemSpec);
            Assert.Equal("a value", item.GetMetadata("Value"));
        }

        [Fact]
        public void SerializeMetadata_ReturnsExpectedString()
        {
            // Arrange
            var identity = "../files/azureMonitor.json";
            var metadata = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                { "ClassName", "azureMonitorClient" },
                { "CodeGenerator", "NSwagCSharp" },
                { "FirstForGenerator", "true" },
                { "Namespace", "ConsoleClient" },
                { "Options", "" },
                { "OriginalItemSpec", identity },
                { "OutputPath", "C:\\dd\\dnx\\AspNetCore\\artifacts\\obj\\ConsoleClient\\azureMonitorClient.cs" },
            };

            var input = new TaskItem(identity, metadata);
            var expectedResult = $"Identity={identity}|ClassName=azureMonitorClient|" +
                "CodeGenerator=NSwagCSharp|FirstForGenerator=true|Namespace=ConsoleClient|" +
                "Options=|OriginalItemSpec=../files/azureMonitor.json|" +
                "OutputPath=C:\\dd\\dnx\\AspNetCore\\artifacts\\obj\\ConsoleClient\\azureMonitorClient.cs";

            // Act
            var result = MetadataSerializer.SerializeMetadata(input);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [MemberData(nameof(EscapedValues))]
        public void SerializeMetadata_ReturnsExpectedString_WhenEscaping(string escapedValue)
        {
            // Arrange
            var identity = "../files/azureMonitor.json";
            var expectedResult = $"Identity={identity}|Value={escapedValue}";
            var metadata = new SortedDictionary<string, string>(StringComparer.Ordinal) { { "Value", escapedValue } };
            var input = new TaskItem(identity, metadata);

            // Act
            var result = MetadataSerializer.SerializeMetadata(input);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [MemberData(nameof(EscapedValues))]
        public void SerializeMetadata_ReturnsExpectedString_WhenEscapingIdentity(string escapedValue)
        {
            // Arrange
            var metadata = new SortedDictionary<string, string>(StringComparer.Ordinal) { { "Value", "a value" } };
            var expectedResult = $"Identity={escapedValue}|Value=a value";
            var input = new TaskItem(escapedValue, metadata);

            // Act
            var result = MetadataSerializer.SerializeMetadata(input);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void SerializeMetadata_ReturnsExpectedString_WithLegacyItem()
        {
            // Arrange
            var identity = "../files/azureMonitor.json";
            var metadata = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                { "ClassName", "azureMonitorClient" },
                { "CodeGenerator", "NSwagCSharp" },
                { "FirstForGenerator", "true" },
                { "Namespace", "ConsoleClient" },
                { "Options", "" },
                { "OriginalItemSpec", identity },
                { "OutputPath", "C:\\dd\\dnx\\AspNetCore\\artifacts\\obj\\ConsoleClient\\azureMonitorClient.cs" },
            };

            var input = new Mock<ITaskItem>(MockBehavior.Strict);
            input.SetupGet(i => i.ItemSpec).Returns(identity).Verifiable();
            input.Setup(i => i.CloneCustomMetadata()).Returns(metadata).Verifiable();

            var expectedResult = $"Identity={identity}|ClassName=azureMonitorClient|" +
                "CodeGenerator=NSwagCSharp|FirstForGenerator=true|Namespace=ConsoleClient|" +
                "Options=|OriginalItemSpec=../files/azureMonitor.json|" +
                "OutputPath=C:\\dd\\dnx\\AspNetCore\\artifacts\\obj\\ConsoleClient\\azureMonitorClient.cs";

            // Act
            var result = MetadataSerializer.SerializeMetadata(input.Object);

            // Assert
            Assert.Equal(expectedResult, result);
            input.VerifyGet(i => i.ItemSpec, Times.Once);
            input.Verify(i => i.CloneCustomMetadata(), Times.Once);
        }

        [Theory]
        [MemberData(nameof(EscapedValuesMapping))]
        public void SerializeMetadata_ReturnsExpectedString_WithLegacyItem_WhenEscaping(
            string value,
            string escapedValue)
        {
            // Arrange
            var identity = "../files/azureMonitor.json";
            var metadata = new SortedDictionary<string, string>(StringComparer.Ordinal) { { "Value", value } };
            var input = new Mock<ITaskItem>(MockBehavior.Strict);
            input.SetupGet(i => i.ItemSpec).Returns(identity).Verifiable();
            input.Setup(i => i.CloneCustomMetadata()).Returns(metadata).Verifiable();

            var expectedResult = $"Identity={identity}|Value={escapedValue}";

            // Act
            var result = MetadataSerializer.SerializeMetadata(input.Object);

            // Assert
            Assert.Equal(expectedResult, result);
            input.VerifyGet(i => i.ItemSpec, Times.Once);
            input.Verify(i => i.CloneCustomMetadata(), Times.Once);
        }

        [Theory]
        [MemberData(nameof(EscapedValuesMapping))]
        public void SerializeMetadata_ReturnsExpectedString_WithLegacyItem_WhenEscapingIdentity(
            string value,
            string escapedValue)
        {
            // Arrange
            var metadata = new SortedDictionary<string, string>(StringComparer.Ordinal) { { "Value", "a value" } };
            var input = new Mock<ITaskItem>(MockBehavior.Strict);
            input.SetupGet(i => i.ItemSpec).Returns(value).Verifiable();
            input.Setup(i => i.CloneCustomMetadata()).Returns(metadata).Verifiable();

            var expectedResult = $"Identity={escapedValue}|Value=a value";

            // Act
            var result = MetadataSerializer.SerializeMetadata(input.Object);

            // Assert
            Assert.Equal(expectedResult, result);
            input.VerifyGet(i => i.ItemSpec, Times.Once);
            input.Verify(i => i.CloneCustomMetadata(), Times.Once);
        }
    }
}
