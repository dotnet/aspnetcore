// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if !DNXCORE50
using System;
using System.IO;
using System.Reflection;
using Microsoft.AspNet.Razor.TagHelpers;
using Microsoft.AspNet.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    public class TagHelperUsageDescriptorFactoryTest
    {
        private const string TypeSummary = "The summary for <see cref=\"T:Microsoft.AspNet.Razor.Runtime." +
            "TagHelpers.DocumentedTagHelper\" />.";
        private const string TypeRemarks = "Inherits from <see cref=\"T:Microsoft.AspNet.Razor.Runtime." +
            "TagHelpers.TagHelper\" />.";
        private const string PropertySummary = "This <see cref=\"P:Microsoft.AspNet.Razor.Runtime." +
            "TagHelpers.DocumentedTagHelper.SummaryProperty\" /> is of type <see cref=\"T:System.String\" />.";
        private const string PropertyRemarks = "The <see cref=\"P:Microsoft.AspNet.Razor.Runtime." +
            "TagHelpers.DocumentedTagHelper.SummaryProperty\" /> may be <c>null</c>.";
        private const string PropertyWithSummaryAndRemarks_Summary = "This is a complex <see cref=\"T:System." +
            "Collections.Generic.IDictionary`2\" />.";
        private const string PropertyWithSummaryAndRemarks_Remarks = "<see cref=\"P:Microsoft.AspNet.Razor." +
            "Runtime.TagHelpers.DocumentedTagHelper.SummaryProperty\" /><see cref=\"P:Microsoft.AspNet.Razor.Runtime" +
            ".TagHelpers.DocumentedTagHelper.RemarksProperty\" />";

        // These test assemblies don't really exist. They are used to look up corresponding XML for a fake assembly
        // which is based on the DocumentedTagHelper type.
        public static readonly string DocumentedAssemblyLocation =
            Directory.GetCurrentDirectory() + "/TestFiles/NotLocalized/TagHelperDocumentation.dll";
        public static readonly string LocalizedDocumentedAssemblyLocation =
            Directory.GetCurrentDirectory() + "/TestFiles/Localized/TagHelperDocumentation.dll";
        public static readonly string DocumentedAssemblyCodeBase =
            "file:///" + DocumentedAssemblyLocation;

        public static TheoryData CreateDescriptor_TypeDocumentationData
        {
            get
            {
                var defaultLocation = DocumentedAssemblyLocation;
                var defaultCodeBase = DocumentedAssemblyCodeBase;
                var nonExistentLocation = defaultLocation.Replace("TestFiles", "TestFile");
                var nonExistentCodeBase = defaultCodeBase.Replace("TestFiles", "TestFile");
                var invalidLocation = defaultLocation + '\0';
                var invalidCodeBase = defaultCodeBase + '\0';

                // tagHelperType, expectedUsageDescriptor
                return new TheoryData<Type, TagHelperUsageDescriptor>
                {
                    { CreateDocumentationTagHelperType(location: null, codeBase: null), null },
                    {
                        CreateDocumentationTagHelperType(defaultLocation, codeBase: null),
                        new TagHelperUsageDescriptor(TypeSummary, TypeRemarks)
                    },
                    {
                        CreateDocumentationTagHelperType(location: null, codeBase: defaultCodeBase),
                        new TagHelperUsageDescriptor(TypeSummary, TypeRemarks)
                    },
                    {
                        CreateDocumentationTagHelperType(defaultLocation, defaultCodeBase),
                        new TagHelperUsageDescriptor(TypeSummary, TypeRemarks)
                    },
                    { CreateType<SingleAttributeTagHelper>(defaultLocation, defaultCodeBase), null },
                    { CreateDocumentationTagHelperType(nonExistentLocation, codeBase: null), null },
                    { CreateDocumentationTagHelperType(location: null, codeBase: nonExistentCodeBase), null },
                    { CreateType<SingleAttributeTagHelper>(invalidLocation, codeBase: null), null },
                    { CreateDocumentationTagHelperType(location: null, codeBase: invalidCodeBase), null },
                };
            }
        }

        [Theory]
        [MemberData(nameof(CreateDescriptor_TypeDocumentationData))]
        public void CreateDescriptor_WithType_ReturnsExpectedDescriptors(
            Type tagHelperType,
            TagHelperUsageDescriptor expectedUsageDescriptor)
        {
            // Act
            var usageDescriptor = TagHelperUsageDescriptorFactory.CreateDescriptor(tagHelperType);

            // Assert
            Assert.Equal(expectedUsageDescriptor, usageDescriptor, TagHelperUsageDescriptorComparer.Default);
        }

        public static TheoryData CreateDescriptor_LocalizedTypeDocumentationData
        {
            get
            {
                // tagHelperType, expectedUsageDescriptor, culture
                return new TheoryData<Type, TagHelperUsageDescriptor, string>
                {
                    {
                        CreateDocumentationTagHelperType(LocalizedDocumentedAssemblyLocation, codeBase: null),
                        new TagHelperUsageDescriptor(
                            summary: "en-GB: " + TypeSummary,
                            remarks: "en-GB: " + TypeRemarks),
                        "en-GB"
                    },
                    {
                        CreateDocumentationTagHelperType(LocalizedDocumentedAssemblyLocation, codeBase: null),
                        new TagHelperUsageDescriptor(
                            summary: "en: " + TypeSummary,
                            remarks: "en: " + TypeRemarks),
                        "en-US"
                    },
                    {
                        CreateDocumentationTagHelperType(LocalizedDocumentedAssemblyLocation, codeBase: null),
                        new TagHelperUsageDescriptor(
                            summary: "fr-FR: " + TypeSummary,
                            remarks: "fr-FR: " + TypeRemarks),
                        "fr-FR"
                    },
                    {
                        CreateDocumentationTagHelperType(LocalizedDocumentedAssemblyLocation, codeBase: null),
                        new TagHelperUsageDescriptor(
                            summary: "fr: " + TypeSummary,
                            remarks: "fr: " + TypeRemarks),
                        "fr-BE"
                    },
                    {
                        CreateDocumentationTagHelperType(LocalizedDocumentedAssemblyLocation, codeBase: null),
                        new TagHelperUsageDescriptor(
                            summary: "nl-BE: " + TypeSummary,
                            remarks: "nl-BE: " + TypeRemarks),
                        "nl-BE"
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(CreateDescriptor_LocalizedTypeDocumentationData))]
        public void CreateDescriptor_WithLocalizedType_ReturnsExpectedDescriptors(
            Type tagHelperType,
            TagHelperUsageDescriptor expectedUsageDescriptor,
            string culture)
        {
            // Arrange
            TagHelperUsageDescriptor usageDescriptor;

            // Act
            using (new CultureReplacer(culture))
            {
                usageDescriptor = TagHelperUsageDescriptorFactory.CreateDescriptor(tagHelperType);
            }

            // Assert
            Assert.Equal(expectedUsageDescriptor, usageDescriptor, TagHelperUsageDescriptorComparer.Default);
        }

        public static TheoryData CreateDescriptor_PropertyDocumentationData
        {
            get
            {
                var defaultLocation = DocumentedAssemblyLocation;
                var defaultCodeBase = DocumentedAssemblyCodeBase;
                var nonExistentLocation = defaultLocation.Replace("TestFiles", "TestFile");
                var nonExistentCodeBase = defaultCodeBase.Replace("TestFiles", "TestFile");
                var invalidLocation = defaultLocation + '\0';
                var invalidCodeBase = defaultCodeBase + '\0';

                // tagHelperType, propertyName, expectedUsageDescriptor
                return new TheoryData<Type, string, TagHelperUsageDescriptor>
                {
                    {
                        CreateDocumentationTagHelperType(location: null, codeBase: null),
                        nameof(DocumentedTagHelper.SummaryProperty),
                        null
                    },
                    {
                        CreateDocumentationTagHelperType(location: null, codeBase: null),
                        nameof(DocumentedTagHelper.RemarksProperty),
                        null
                    },
                    {
                        CreateDocumentationTagHelperType(location: null, codeBase: null),
                        nameof(DocumentedTagHelper.RemarksAndSummaryProperty),
                        null
                    },
                    {
                        CreateDocumentationTagHelperType(defaultLocation, defaultCodeBase),
                        nameof(DocumentedTagHelper.UndocumentedProperty),
                        null
                    },
                    {
                        CreateDocumentationTagHelperType(defaultLocation, codeBase: null),
                        nameof(DocumentedTagHelper.SummaryProperty),
                        new TagHelperUsageDescriptor(PropertySummary, remarks: null)
                    },
                    {
                        CreateDocumentationTagHelperType(defaultLocation, codeBase: null),
                        nameof(DocumentedTagHelper.RemarksProperty),
                        new TagHelperUsageDescriptor(summary: null, remarks: PropertyRemarks)
                    },
                    {
                        CreateDocumentationTagHelperType(defaultLocation, codeBase: null),
                        nameof(DocumentedTagHelper.RemarksAndSummaryProperty),
                        new TagHelperUsageDescriptor(
                            PropertyWithSummaryAndRemarks_Summary,
                            PropertyWithSummaryAndRemarks_Remarks)
                    },
                    {
                        CreateDocumentationTagHelperType(location: null, codeBase: defaultCodeBase),
                        nameof(DocumentedTagHelper.SummaryProperty),
                        new TagHelperUsageDescriptor(PropertySummary, remarks: null)
                    },
                    {
                        CreateDocumentationTagHelperType(location: null, codeBase: defaultCodeBase),
                        nameof(DocumentedTagHelper.RemarksProperty),
                        new TagHelperUsageDescriptor(summary: null, remarks: PropertyRemarks)
                    },
                    {
                        CreateDocumentationTagHelperType(location: null, codeBase: defaultCodeBase),
                        nameof(DocumentedTagHelper.RemarksAndSummaryProperty),
                        new TagHelperUsageDescriptor(
                            PropertyWithSummaryAndRemarks_Summary,
                            PropertyWithSummaryAndRemarks_Remarks)
                    },
                    {
                        CreateDocumentationTagHelperType(defaultLocation, defaultCodeBase),
                        nameof(DocumentedTagHelper.SummaryProperty),
                        new TagHelperUsageDescriptor(PropertySummary, remarks: null)
                    },
                    {
                        CreateDocumentationTagHelperType(defaultLocation, defaultCodeBase),
                        nameof(DocumentedTagHelper.RemarksProperty),
                        new TagHelperUsageDescriptor(summary: null, remarks: PropertyRemarks)
                    },
                    {
                        CreateDocumentationTagHelperType(defaultLocation, defaultCodeBase),
                        nameof(DocumentedTagHelper.RemarksAndSummaryProperty),
                        new TagHelperUsageDescriptor(
                            PropertyWithSummaryAndRemarks_Summary,
                            PropertyWithSummaryAndRemarks_Remarks)
                    },
                    {
                        CreateDocumentationTagHelperType(nonExistentLocation, codeBase: null),
                        nameof(DocumentedTagHelper.RemarksAndSummaryProperty),
                        null
                    },
                    {
                        CreateDocumentationTagHelperType(location: null, codeBase: nonExistentCodeBase),
                        nameof(DocumentedTagHelper.RemarksAndSummaryProperty),
                        null
                    },
                    {
                        CreateDocumentationTagHelperType(invalidLocation, codeBase: null),
                        nameof(DocumentedTagHelper.RemarksAndSummaryProperty),
                        null
                    },
                    {
                        CreateDocumentationTagHelperType(location: null, codeBase: invalidCodeBase),
                        nameof(DocumentedTagHelper.RemarksAndSummaryProperty),
                        null
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(CreateDescriptor_PropertyDocumentationData))]
        public void CreateDescriptor_WithProperty_ReturnsExpectedDescriptors(
            Type tagHelperType,
            string propertyName,
            TagHelperUsageDescriptor expectedUsageDescriptor)
        {
            // Arrange
            var mockPropertyInfo = new Mock<PropertyInfo>();
            mockPropertyInfo.Setup(propertyInfo => propertyInfo.DeclaringType).Returns(tagHelperType);
            mockPropertyInfo.Setup(propertyInfo => propertyInfo.Name).Returns(propertyName);

            // Act
            var usageDescriptor = TagHelperUsageDescriptorFactory.CreateDescriptor(mockPropertyInfo.Object);

            // Assert
            Assert.Equal(expectedUsageDescriptor, usageDescriptor, TagHelperUsageDescriptorComparer.Default);
        }

        public static TheoryData CreateDescriptor_LocalizedPropertyData
        {
            get
            {
                // tagHelperType, expectedUsageDescriptor, culture
                return new TheoryData<Type, TagHelperUsageDescriptor, string>
                {
                    {
                        CreateDocumentationTagHelperType(LocalizedDocumentedAssemblyLocation, codeBase: null),
                        new TagHelperUsageDescriptor(
                            summary: "en-GB: " + PropertyWithSummaryAndRemarks_Summary,
                            remarks: "en-GB: " + PropertyWithSummaryAndRemarks_Remarks),
                        "en-GB"
                    },
                    {
                        CreateDocumentationTagHelperType(LocalizedDocumentedAssemblyLocation, codeBase: null),
                        new TagHelperUsageDescriptor(
                            summary: "en: " + PropertyWithSummaryAndRemarks_Summary,
                            remarks: "en: " + PropertyWithSummaryAndRemarks_Remarks),
                        "en-US"
                    },
                    {
                        CreateDocumentationTagHelperType(LocalizedDocumentedAssemblyLocation, codeBase: null),
                        new TagHelperUsageDescriptor(
                            summary: "fr-FR: " + PropertyWithSummaryAndRemarks_Summary,
                            remarks: "fr-FR: " + PropertyWithSummaryAndRemarks_Remarks),
                        "fr-FR"
                    },
                    {
                        CreateDocumentationTagHelperType(LocalizedDocumentedAssemblyLocation, codeBase: null),
                        new TagHelperUsageDescriptor(
                            summary: "fr: " + PropertyWithSummaryAndRemarks_Summary,
                            remarks: "fr: " + PropertyWithSummaryAndRemarks_Remarks),
                        "fr-BE"
                    },
                    {
                        CreateDocumentationTagHelperType(LocalizedDocumentedAssemblyLocation, codeBase: null),
                        new TagHelperUsageDescriptor(
                            summary: "nl-BE: " + PropertyWithSummaryAndRemarks_Summary,
                            remarks: "nl-BE: " + PropertyWithSummaryAndRemarks_Remarks),
                        "nl-BE"
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(CreateDescriptor_LocalizedPropertyData))]
        public void CreateDescriptor_WithLocalizedProperty_ReturnsExpectedDescriptors(
            Type tagHelperType,
            TagHelperUsageDescriptor expectedUsageDescriptor,
            string culture)
        {
            // Arrange
            var mockPropertyInfo = new Mock<PropertyInfo>();
            mockPropertyInfo.Setup(propertyInfo => propertyInfo.DeclaringType).Returns(tagHelperType);
            mockPropertyInfo
                .Setup(propertyInfo => propertyInfo.Name)
                .Returns(nameof(DocumentedTagHelper.RemarksAndSummaryProperty));
            TagHelperUsageDescriptor usageDescriptor;

            // Act
            using (new CultureReplacer(culture))
            {
                usageDescriptor = TagHelperUsageDescriptorFactory.CreateDescriptor(mockPropertyInfo.Object);
            }

            // Assert
            Assert.Equal(expectedUsageDescriptor, usageDescriptor, TagHelperUsageDescriptorComparer.Default);
        }

        private static Type CreateDocumentationTagHelperType(string location, string codeBase)
        {
            return CreateType<DocumentedTagHelper>(location, codeBase);
        }

        private static Type CreateType<TWrappedType>(string location, string codeBase)
        {
            var testAssembly = new TestAssembly(location, codeBase);
            var wrappedType = typeof(TWrappedType);
            var mockType = new Mock<Type>();
            mockType.Setup(type => type.Assembly).Returns(testAssembly);
            mockType.Setup(type => type.FullName).Returns(wrappedType.FullName);
            mockType.Setup(type => type.DeclaringType).Returns(wrappedType.DeclaringType);

            return mockType.Object;
        }

        private class TestAssembly : Assembly
        {
            public TestAssembly(string location, string codeBase)
            {
                Location = location;
                CodeBase = codeBase;
            }

            public override string Location { get; }

            public override string CodeBase { get; }
        }
    }
}
#endif