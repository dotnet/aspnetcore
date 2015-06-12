// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if !DNXCORE50
using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Xunit;

namespace Microsoft.AspNet.Razor.Runtime
{
    public class XmlDocumentationProviderTest
    {
        public static readonly string XmlTestFileLocation =
            Directory.GetCurrentDirectory() + "/TestFiles/NotLocalized/TagHelperDocumentation.xml";
        private static readonly TypeInfo DocumentedTagHelperTypeInfo = typeof(DocumentedTagHelper).GetTypeInfo();
        private static readonly PropertyInfo DocumentedTagHelperSummaryPropertyInfo =
            DocumentedTagHelperTypeInfo.GetProperty(nameof(DocumentedTagHelper.SummaryProperty));
        private static readonly PropertyInfo DocumentedTagHelperRemarksPropertyInfo =
            DocumentedTagHelperTypeInfo.GetProperty(nameof(DocumentedTagHelper.RemarksProperty));
        private static readonly PropertyInfo DocumentedTagHelperRemarksSummaryPropertyInfo =
            DocumentedTagHelperTypeInfo.GetProperty(nameof(DocumentedTagHelper.RemarksAndSummaryProperty));

        [Fact]
        public void CanReadXml()
        {
            // Act. Ensuring that reading the Xml file doesn't throw.
            new XmlDocumentationProvider(XmlTestFileLocation);
        }

        public static TheoryData SummaryDocumentationData
        {
            get
            {
                var fullTypeName = DocumentedTagHelperTypeInfo.FullName;

                // id, expectedSummary
                return new TheoryData<string, string>
                {
                    {
                        $"T:{fullTypeName}",
                        "The summary for <see cref=\"T:Microsoft.AspNet.Razor.Runtime.TagHelpers." +
                        "DocumentedTagHelper\" />."
                    },
                    {
                        $"P:{fullTypeName}.{DocumentedTagHelperSummaryPropertyInfo.Name}",
                        "This <see cref=\"P:Microsoft.AspNet.Razor.Runtime.TagHelpers.DocumentedTagHelper." +
                        "SummaryProperty\" /> is of type <see cref=\"T:System.String\" />."
                    },
                    {
                        $"P:{fullTypeName}.{DocumentedTagHelperRemarksPropertyInfo.Name}",
                        null
                    },
                    {
                        $"P:{fullTypeName}.{DocumentedTagHelperRemarksSummaryPropertyInfo.Name}",
                        "This is a complex <see cref=\"T:System.Collections.Generic.IDictionary`2\" />."
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(SummaryDocumentationData))]
        public void GetSummary_ReturnsExpectedSummarys(string id, string expectedSummary)
        {
            // Arrange
            var xmlDocumentationProvider = new XmlDocumentationProvider(XmlTestFileLocation);

            // Act
            var summary = xmlDocumentationProvider.GetSummary(id);

            // Assert
            Assert.Equal(expectedSummary, summary, StringComparer.Ordinal);
        }

        public static TheoryData RemarksDocumentationData
        {
            get
            {
                var fullTypeName = DocumentedTagHelperTypeInfo.FullName;

                // id, expectedRemarks
                return new TheoryData<string, string>
                {
                    {
                        $"T:{fullTypeName}",
                        "Inherits from <see cref=\"T:Microsoft.AspNet.Razor.Runtime.TagHelpers.TagHelper\" />."
                    },
                    {
                        $"P:{fullTypeName}.{DocumentedTagHelperSummaryPropertyInfo.Name}",
                        null
                    },
                    {
                        $"P:{fullTypeName}.{DocumentedTagHelperRemarksPropertyInfo.Name}",
                        "The <see cref=\"P:Microsoft.AspNet.Razor.Runtime.TagHelpers.DocumentedTagHelper." +
                        "SummaryProperty\" /> may be <c>null</c>."
                    },
                    {
                        $"P:{fullTypeName}.{DocumentedTagHelperRemarksSummaryPropertyInfo.Name}",
                        "<see cref=\"P:Microsoft.AspNet.Razor.Runtime.TagHelpers.DocumentedTagHelper." +
                        "SummaryProperty\" /><see cref=\"P:Microsoft.AspNet.Razor.Runtime.TagHelpers." +
                        "DocumentedTagHelper.RemarksProperty\" />"
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(RemarksDocumentationData))]
        public void GetRemarks_ReturnsExpectedRemarks(string id, string expectedRemarks)
        {
            // Arrange
            var xmlDocumentationProvider = new XmlDocumentationProvider(XmlTestFileLocation);

            // Act
            var remarks = xmlDocumentationProvider.GetRemarks(id);

            // Assert
            Assert.Equal(expectedRemarks, remarks, StringComparer.Ordinal);
        }

        [Fact]
        public void GetId_UnderstandsTypeInfo()
        {
            // Arrange
            var expectedId = "T:" + typeof(DocumentedTagHelper).FullName;

            // Act
            var id = XmlDocumentationProvider.GetId(DocumentedTagHelperTypeInfo);

            // Assert
            Assert.Equal(expectedId, id, StringComparer.Ordinal);
        }

        [Fact]
        public void GetId_UnderstandsPropertyInfo()
        {
            // Arrange
            var expectedId = string.Format(
                CultureInfo.InvariantCulture,
                "P:{0}.{1}",
                typeof(DocumentedTagHelper).FullName,
                nameof(DocumentedTagHelper.RemarksAndSummaryProperty));

            // Act
            var id = XmlDocumentationProvider.GetId(DocumentedTagHelperRemarksSummaryPropertyInfo);

            // Assert
            Assert.Equal(expectedId, id, StringComparer.Ordinal);
        }
    }
}
#endif