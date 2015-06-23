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
    public class TagHelperDesignTimeDescriptorFactoryTest
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
            Directory.GetCurrentDirectory() +
            string.Format("{0}TestFiles{0}NotLocalized{0}TagHelperDocumentation.dll", Path.DirectorySeparatorChar);
        public static readonly string LocalizedDocumentedAssemblyLocation =
            Directory.GetCurrentDirectory() +
            string.Format("{0}TestFiles{0}Localized{0}TagHelperDocumentation.dll", Path.DirectorySeparatorChar);
        public static readonly string DocumentedAssemblyCodeBase =
            "file:" + new string(Path.DirectorySeparatorChar, 3) +
            DocumentedAssemblyLocation.TrimStart(Path.DirectorySeparatorChar);

        public static TheoryData OutputElementHintData
        {
            get
            {
                // tagHelperType, expectedDescriptor
                return new TheoryData<Type, TagHelperDesignTimeDescriptor>
                {
                    { typeof(InheritedOutputElementHintTagHelper), null },
                    {
                        typeof(OutputElementHintTagHelper),
                        new TagHelperDesignTimeDescriptor(
                            summary: null,
                            remarks: null,
                            outputElementHint: "hinted-value")
                    },
                    {
                        typeof(OverriddenOutputElementHintTagHelper),
                        new TagHelperDesignTimeDescriptor(
                            summary: null,
                            remarks: null,
                            outputElementHint: "overridden")
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(OutputElementHintData))]
        public void CreateDescriptor_CapturesOutputElementHint(
            Type tagHelperType,
            TagHelperDesignTimeDescriptor expectedDescriptor)
        {
            // Act
            var descriptors = TagHelperDesignTimeDescriptorFactory.CreateDescriptor(tagHelperType);

            // Assert
            Assert.Equal(expectedDescriptor, descriptors, TagHelperDesignTimeDescriptorComparer.Default);
        }

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
                var onlyHint = new TagHelperDesignTimeDescriptor(
                    summary: null,
                    remarks: null,
                    outputElementHint: "p");

                // tagHelperType, expectedDesignTimeDescriptor
                return new TheoryData<Type, TagHelperDesignTimeDescriptor>
                {
                    { CreateDocumentationTagHelperType(location: null, codeBase: null), onlyHint },
                    {
                        CreateDocumentationTagHelperType(defaultLocation, codeBase: null),
                        new TagHelperDesignTimeDescriptor(
                            TypeSummary,
                            TypeRemarks,
                            outputElementHint: "p")
                    },
                    {
                        CreateDocumentationTagHelperType(location: null, codeBase: defaultCodeBase),
                        new TagHelperDesignTimeDescriptor(TypeSummary, TypeRemarks, outputElementHint: "p")
                    },
                    {
                        CreateDocumentationTagHelperType(defaultLocation, defaultCodeBase),
                        new TagHelperDesignTimeDescriptor(TypeSummary, TypeRemarks, outputElementHint: "p")
                    },
                    { CreateType<SingleAttributeTagHelper>(defaultLocation, defaultCodeBase), null },
                    { CreateDocumentationTagHelperType(nonExistentLocation, codeBase: null), onlyHint },
                    { CreateDocumentationTagHelperType(location: null, codeBase: nonExistentCodeBase), onlyHint },
                    { CreateType<SingleAttributeTagHelper>(invalidLocation, codeBase: null), null },
                    { CreateDocumentationTagHelperType(location: null, codeBase: invalidCodeBase), onlyHint },
                };
            }
        }

        [Theory]
        [MemberData(nameof(CreateDescriptor_TypeDocumentationData))]
        public void CreateDescriptor_WithType_ReturnsExpectedDescriptors(
            Type tagHelperType,
            TagHelperDesignTimeDescriptor expectedDesignTimeDescriptor)
        {
            // Act
            var designTimeDescriptor = TagHelperDesignTimeDescriptorFactory.CreateDescriptor(tagHelperType);

            // Assert
            Assert.Equal(expectedDesignTimeDescriptor, designTimeDescriptor, TagHelperDesignTimeDescriptorComparer.Default);
        }

        public static TheoryData CreateDescriptor_LocalizedTypeDocumentationData
        {
            get
            {
                // tagHelperType, expectedDesignTimeDescriptor, culture
                return new TheoryData<Type, TagHelperDesignTimeDescriptor, string>
                {
                    {
                        CreateDocumentationTagHelperType(LocalizedDocumentedAssemblyLocation, codeBase: null),
                        new TagHelperDesignTimeDescriptor(
                            summary: "en-GB: " + TypeSummary,
                            remarks: "en-GB: " + TypeRemarks,
                            outputElementHint: "p"),
                        "en-GB"
                    },
                    {
                        CreateDocumentationTagHelperType(LocalizedDocumentedAssemblyLocation, codeBase: null),
                        new TagHelperDesignTimeDescriptor(
                            summary: "en: " + TypeSummary,
                            remarks: "en: " + TypeRemarks,
                            outputElementHint: "p"),
                        "en-US"
                    },
                    {
                        CreateDocumentationTagHelperType(LocalizedDocumentedAssemblyLocation, codeBase: null),
                        new TagHelperDesignTimeDescriptor(
                            summary: "fr-FR: " + TypeSummary,
                            remarks: "fr-FR: " + TypeRemarks,
                            outputElementHint: "p"),
                        "fr-FR"
                    },
                    {
                        CreateDocumentationTagHelperType(LocalizedDocumentedAssemblyLocation, codeBase: null),
                        new TagHelperDesignTimeDescriptor(
                            summary: "fr: " + TypeSummary,
                            remarks: "fr: " + TypeRemarks,
                            outputElementHint: "p"),
                        "fr-BE"
                    },
                    {
                        CreateDocumentationTagHelperType(LocalizedDocumentedAssemblyLocation, codeBase: null),
                        new TagHelperDesignTimeDescriptor(
                            summary: "nl-BE: " + TypeSummary,
                            remarks: "nl-BE: " + TypeRemarks,
                            outputElementHint: "p"),
                        "nl-BE"
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(CreateDescriptor_LocalizedTypeDocumentationData))]
        public void CreateDescriptor_WithLocalizedType_ReturnsExpectedDescriptors(
            Type tagHelperType,
            TagHelperDesignTimeDescriptor expectedDesignTimeDescriptor,
            string culture)
        {
            // Arrange
            TagHelperDesignTimeDescriptor designTimeDescriptor;

            // Act
            using (new CultureReplacer(culture))
            {
                designTimeDescriptor = TagHelperDesignTimeDescriptorFactory.CreateDescriptor(tagHelperType);
            }

            // Assert
            Assert.Equal(expectedDesignTimeDescriptor, designTimeDescriptor, TagHelperDesignTimeDescriptorComparer.Default);
        }

        public static TheoryData CreateAttributeDescriptor_PropertyDocumentationData
        {
            get
            {
                var defaultLocation = DocumentedAssemblyLocation;
                var defaultCodeBase = DocumentedAssemblyCodeBase;
                var nonExistentLocation = defaultLocation.Replace("TestFiles", "TestFile");
                var nonExistentCodeBase = defaultCodeBase.Replace("TestFiles", "TestFile");
                var invalidLocation = defaultLocation + '\0';
                var invalidCodeBase = defaultCodeBase + '\0';

                // tagHelperType, propertyName, expectedDesignTimeDescriptor
                return new TheoryData<Type, string, TagHelperAttributeDesignTimeDescriptor>
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
                        new TagHelperAttributeDesignTimeDescriptor(PropertySummary, remarks: null)
                    },
                    {
                        CreateDocumentationTagHelperType(defaultLocation, codeBase: null),
                        nameof(DocumentedTagHelper.RemarksProperty),
                        new TagHelperAttributeDesignTimeDescriptor(summary: null, remarks: PropertyRemarks)
                    },
                    {
                        CreateDocumentationTagHelperType(defaultLocation, codeBase: null),
                        nameof(DocumentedTagHelper.RemarksAndSummaryProperty),
                        new TagHelperAttributeDesignTimeDescriptor(
                            PropertyWithSummaryAndRemarks_Summary,
                            PropertyWithSummaryAndRemarks_Remarks)
                    },
                    {
                        CreateDocumentationTagHelperType(location: null, codeBase: defaultCodeBase),
                        nameof(DocumentedTagHelper.SummaryProperty),
                        new TagHelperAttributeDesignTimeDescriptor(PropertySummary, remarks: null)
                    },
                    {
                        CreateDocumentationTagHelperType(location: null, codeBase: defaultCodeBase),
                        nameof(DocumentedTagHelper.RemarksProperty),
                        new TagHelperAttributeDesignTimeDescriptor(summary: null, remarks: PropertyRemarks)
                    },
                    {
                        CreateDocumentationTagHelperType(location: null, codeBase: defaultCodeBase),
                        nameof(DocumentedTagHelper.RemarksAndSummaryProperty),
                        new TagHelperAttributeDesignTimeDescriptor(
                            PropertyWithSummaryAndRemarks_Summary,
                            PropertyWithSummaryAndRemarks_Remarks)
                    },
                    {
                        CreateDocumentationTagHelperType(defaultLocation, defaultCodeBase),
                        nameof(DocumentedTagHelper.SummaryProperty),
                        new TagHelperAttributeDesignTimeDescriptor(PropertySummary, remarks: null)
                    },
                    {
                        CreateDocumentationTagHelperType(defaultLocation, defaultCodeBase),
                        nameof(DocumentedTagHelper.RemarksProperty),
                        new TagHelperAttributeDesignTimeDescriptor(summary: null, remarks: PropertyRemarks)
                    },
                    {
                        CreateDocumentationTagHelperType(defaultLocation, defaultCodeBase),
                        nameof(DocumentedTagHelper.RemarksAndSummaryProperty),
                        new TagHelperAttributeDesignTimeDescriptor(
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
        [MemberData(nameof(CreateAttributeDescriptor_PropertyDocumentationData))]
        public void CreateAttributeDescriptor_ReturnsExpectedDescriptors(
            Type tagHelperType,
            string propertyName,
            TagHelperAttributeDesignTimeDescriptor expectedDesignTimeDescriptor)
        {
            // Arrange
            var mockPropertyInfo = new Mock<PropertyInfo>();
            mockPropertyInfo.Setup(propertyInfo => propertyInfo.DeclaringType).Returns(tagHelperType);
            mockPropertyInfo.Setup(propertyInfo => propertyInfo.Name).Returns(propertyName);

            // Act
            var designTimeDescriptor = TagHelperDesignTimeDescriptorFactory.CreateAttributeDescriptor(
                mockPropertyInfo.Object);

            // Assert
            Assert.Equal(
                expectedDesignTimeDescriptor,
                designTimeDescriptor,
                TagHelperAttributeDesignTimeDescriptorComparer.Default);
        }

        public static TheoryData CreateAttributeDescriptor_LocalizedPropertyData
        {
            get
            {
                // tagHelperType, expectedDesignTimeDescriptor, culture
                return new TheoryData<Type, TagHelperAttributeDesignTimeDescriptor, string>
                {
                    {
                        CreateDocumentationTagHelperType(LocalizedDocumentedAssemblyLocation, codeBase: null),
                        new TagHelperAttributeDesignTimeDescriptor(
                            summary: "en-GB: " + PropertyWithSummaryAndRemarks_Summary,
                            remarks: "en-GB: " + PropertyWithSummaryAndRemarks_Remarks),
                        "en-GB"
                    },
                    {
                        CreateDocumentationTagHelperType(LocalizedDocumentedAssemblyLocation, codeBase: null),
                        new TagHelperAttributeDesignTimeDescriptor(
                            summary: "en: " + PropertyWithSummaryAndRemarks_Summary,
                            remarks: "en: " + PropertyWithSummaryAndRemarks_Remarks),
                        "en-US"
                    },
                    {
                        CreateDocumentationTagHelperType(LocalizedDocumentedAssemblyLocation, codeBase: null),
                        new TagHelperAttributeDesignTimeDescriptor(
                            summary: "fr-FR: " + PropertyWithSummaryAndRemarks_Summary,
                            remarks: "fr-FR: " + PropertyWithSummaryAndRemarks_Remarks),
                        "fr-FR"
                    },
                    {
                        CreateDocumentationTagHelperType(LocalizedDocumentedAssemblyLocation, codeBase: null),
                        new TagHelperAttributeDesignTimeDescriptor(
                            summary: "fr: " + PropertyWithSummaryAndRemarks_Summary,
                            remarks: "fr: " + PropertyWithSummaryAndRemarks_Remarks),
                        "fr-BE"
                    },
                    {
                        CreateDocumentationTagHelperType(LocalizedDocumentedAssemblyLocation, codeBase: null),
                        new TagHelperAttributeDesignTimeDescriptor(
                            summary: "nl-BE: " + PropertyWithSummaryAndRemarks_Summary,
                            remarks: "nl-BE: " + PropertyWithSummaryAndRemarks_Remarks),
                        "nl-BE"
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(CreateAttributeDescriptor_LocalizedPropertyData))]
        public void CreateAttributeDescriptor_WithLocalizedProperty_ReturnsExpectedDescriptors(
            Type tagHelperType,
            TagHelperAttributeDesignTimeDescriptor expectedDesignTimeDescriptor,
            string culture)
        {
            // Arrange
            var mockPropertyInfo = new Mock<PropertyInfo>();
            mockPropertyInfo.Setup(propertyInfo => propertyInfo.DeclaringType).Returns(tagHelperType);
            mockPropertyInfo
                .Setup(propertyInfo => propertyInfo.Name)
                .Returns(nameof(DocumentedTagHelper.RemarksAndSummaryProperty));
            TagHelperAttributeDesignTimeDescriptor designTimeDescriptor;

            // Act
            using (new CultureReplacer(culture))
            {
                designTimeDescriptor = TagHelperDesignTimeDescriptorFactory.CreateAttributeDescriptor(
                    mockPropertyInfo.Object);
            }

            // Assert
            Assert.Equal(
                expectedDesignTimeDescriptor,
                designTimeDescriptor,
                TagHelperAttributeDesignTimeDescriptorComparer.Default);
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
            mockType
                .Setup(type => type.GetCustomAttributes(false))
                .Returns(wrappedType == typeof(DocumentedTagHelper) ?
                    new[] { new OutputElementHintAttribute("p") } :
                    null);

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

        [OutputElementHint("hinted-value")]
        private class OutputElementHintTagHelper : TagHelper
        {
        }

        private class InheritedOutputElementHintTagHelper : OutputElementHintTagHelper
        {
        }

        [OutputElementHint("overridden")]
        private class OverriddenOutputElementHintTagHelper : OutputElementHintTagHelper
        {
        }
    }
}
#endif