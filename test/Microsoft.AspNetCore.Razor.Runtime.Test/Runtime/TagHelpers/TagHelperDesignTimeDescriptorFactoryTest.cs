// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Razor.Compilation.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers.Testing;
using Microsoft.AspNetCore.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Runtime.TagHelpers
{
    public class TagHelperDesignTimeDescriptorFactoryTest
    {
        private const string TypeSummary = "The summary for <see cref=\"T:Microsoft.AspNetCore.Razor." +
            "TagHelpers.DocumentedTagHelper\" />.";
        private const string TypeRemarks = "Inherits from <see cref=\"T:Microsoft.AspNetCore.Razor." +
            "TagHelpers.TagHelper\" />.";
        private const string PropertySummary = "This <see cref=\"P:Microsoft.AspNetCore.Razor." +
            "TagHelpers.DocumentedTagHelper.SummaryProperty\" /> is of type <see cref=\"T:System.String\" />.";
        private const string PropertyRemarks = "The <see cref=\"P:Microsoft.AspNetCore.Razor." +
            "TagHelpers.DocumentedTagHelper.SummaryProperty\" /> may be <c>null</c>.";
        private const string PropertyWithSummaryAndRemarks_Summary = "This is a complex <see cref=\"T:System." +
            "Collections.Generic.IDictionary`2\" />.";
        private const string PropertyWithSummaryAndRemarks_Remarks = "<see cref=\"P:Microsoft.AspNetCore.Razor." +
            "TagHelpers.DocumentedTagHelper.SummaryProperty\" /><see cref=\"P:Microsoft.AspNetCore.Razor" +
            ".TagHelpers.DocumentedTagHelper.RemarksProperty\" />";

        // These test assemblies don't really exist. They are used to look up corresponding XML for a fake assembly
        // which is based on the DocumentedTagHelper type.
        public static readonly string TestRoot =
#if NET451
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", ".."));
#else
            Directory.GetCurrentDirectory();
#endif
        public static readonly string DocumentedAssemblyLocation =
            Path.Combine(TestRoot, "TestFiles", "NotLocalized", "TagHelperDocumentation.dll");
        public static readonly string LocalizedDocumentedAssemblyLocation =
            Path.Combine(TestRoot, "TestFiles", "Localized", "TagHelperDocumentation.dll");
        public static readonly string DocumentedAssemblyCodeBase =
            "file:" +
            new string(Path.DirectorySeparatorChar, 3) +
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
                        new TagHelperDesignTimeDescriptor
                        {
                            OutputElementHint = "hinted-value"
                        }
                    },
                    {
                        typeof(OverriddenOutputElementHintTagHelper),
                        new TagHelperDesignTimeDescriptor
                        {
                            OutputElementHint = "overridden"
                        }
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
            // Arrange
            var factory = new TagHelperDesignTimeDescriptorFactory();

            // Act
            var descriptors = factory.CreateDescriptor(tagHelperType);

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
                var onlyHint = new TagHelperDesignTimeDescriptor
                {
                    OutputElementHint = "p"
                };

                // tagHelperType, assemblyLocation, expectedDesignTimeDescriptor
                return new TheoryData<Type, string, TagHelperDesignTimeDescriptor>
                {
                    { typeof(DocumentedTagHelper), null, onlyHint },
                    {
                        typeof(DocumentedTagHelper),
                        defaultLocation,
                        new TagHelperDesignTimeDescriptor
                        {
                            Summary = TypeSummary,
                            Remarks = TypeRemarks,
                            OutputElementHint = "p"
                        }
                    },
                    { typeof(SingleAttributeTagHelper), defaultLocation, null },
                    { typeof(DocumentedTagHelper), nonExistentLocation, onlyHint },
                    { typeof(SingleAttributeTagHelper), invalidLocation, null },
                };
            }
        }

        [Theory]
        [MemberData(nameof(CreateDescriptor_TypeDocumentationData))]
        public void CreateDescriptor_WithType_ReturnsExpectedDescriptors(
            Type tagHelperType,
            string assemblyLocation,
            TagHelperDesignTimeDescriptor expectedDesignTimeDescriptor)
        {
            // Arrange
            var factory = new TestTagHelperDesignTimeDescriptorFactory(assemblyLocation);

            // Act
            var designTimeDescriptor = factory.CreateDescriptor(tagHelperType);

            // Assert
            Assert.Equal(expectedDesignTimeDescriptor, designTimeDescriptor, TagHelperDesignTimeDescriptorComparer.Default);
        }

        public static TheoryData CreateDescriptor_LocalizedTypeDocumentationData
        {
            get
            {
                // tagHelperType, assemblyLocation, expectedDesignTimeDescriptor, culture
                return new TheoryData<Type, string, TagHelperDesignTimeDescriptor, string>
                {
                    {
                        typeof(DocumentedTagHelper),
                        LocalizedDocumentedAssemblyLocation,
                        new TagHelperDesignTimeDescriptor
                        {
                            Summary = "en-GB: " + TypeSummary,
                            Remarks = "en-GB: " + TypeRemarks,
                            OutputElementHint = "p"
                        },
                        "en-GB"
                    },
                    {
                        typeof(DocumentedTagHelper),
                        LocalizedDocumentedAssemblyLocation,
                        new TagHelperDesignTimeDescriptor
                        {
                            Summary = "en: " + TypeSummary,
                            Remarks = "en: " + TypeRemarks,
                            OutputElementHint = "p"
                        },
                        "en-US"
                    },
                    {
                        typeof(DocumentedTagHelper),
                        LocalizedDocumentedAssemblyLocation,
                        new TagHelperDesignTimeDescriptor
                        {
                            Summary = "fr-FR: " + TypeSummary,
                            Remarks = "fr-FR: " + TypeRemarks,
                            OutputElementHint = "p"
                        },
                        "fr-FR"
                    },
                    {
                        typeof(DocumentedTagHelper),
                        LocalizedDocumentedAssemblyLocation,
                        new TagHelperDesignTimeDescriptor
                        {
                            Summary = "fr: " + TypeSummary,
                            Remarks = "fr: " + TypeRemarks,
                            OutputElementHint = "p"
                        },
                        "fr-BE"
                    },
                    {
                        typeof(DocumentedTagHelper),
                        LocalizedDocumentedAssemblyLocation,
                        new TagHelperDesignTimeDescriptor
                        {
                            Summary = "nl-BE: " + TypeSummary,
                            Remarks = "nl-BE: " + TypeRemarks,
                            OutputElementHint = "p"
                        },
                        "nl-BE"
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(CreateDescriptor_LocalizedTypeDocumentationData))]
        public void CreateDescriptor_WithLocalizedType_ReturnsExpectedDescriptors(
            Type tagHelperType,
            string assemblyLocation,
            TagHelperDesignTimeDescriptor expectedDesignTimeDescriptor,
            string culture)
        {
            // Arrange
            TagHelperDesignTimeDescriptor designTimeDescriptor;
            var factory = new TestTagHelperDesignTimeDescriptorFactory(assemblyLocation);

            // Act
            using (new CultureReplacer(culture))
            {
                designTimeDescriptor = factory.CreateDescriptor(tagHelperType);
            }

            // Assert
            Assert.Equal(expectedDesignTimeDescriptor, designTimeDescriptor, TagHelperDesignTimeDescriptorComparer.Default);
        }

        [Fact]
        public void CreateDescriptor_WithLocalizedType_CachesBasedOnCulture()
        {
            // Arrange
            var factory = new TestTagHelperDesignTimeDescriptorFactory(LocalizedDocumentedAssemblyLocation);
            var expectedFRDesignTimeDescriptor = new TagHelperDesignTimeDescriptor
            {
                Summary = "fr-FR: " + TypeSummary,
                Remarks = "fr-FR: " + TypeRemarks,
                OutputElementHint = "p",
            };
            var expectedNLBEDesignTimeDescriptor = new TagHelperDesignTimeDescriptor
            {
                Summary = "nl-BE: " + TypeSummary,
                Remarks = "nl-BE: " + TypeRemarks,
                OutputElementHint = "p",
            };
            var tagHelperType = typeof(DocumentedTagHelper);
            TagHelperDesignTimeDescriptor frDesignTimeDescriptor, nlBEDesignTimeDescriptor;

            // Act
            using (new CultureReplacer("fr-FR"))
            {
                frDesignTimeDescriptor = factory.CreateDescriptor(tagHelperType);
            }
            using (new CultureReplacer("nl-BE"))
            {
                nlBEDesignTimeDescriptor = factory.CreateDescriptor(tagHelperType);
            }

            // Assert
            Assert.Equal(
                expectedFRDesignTimeDescriptor,
                frDesignTimeDescriptor,
                TagHelperDesignTimeDescriptorComparer.Default);
            Assert.Equal(
                expectedNLBEDesignTimeDescriptor,
                nlBEDesignTimeDescriptor,
                TagHelperDesignTimeDescriptorComparer.Default);
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

                // tagHelperType, propertyName, assemblyLocation, expectedDesignTimeDescriptor
                return new TheoryData<Type, string, string, TagHelperAttributeDesignTimeDescriptor>
                {
                    {
                        typeof(DocumentedTagHelper),
                        nameof(DocumentedTagHelper.SummaryProperty),
                        null,
                        null
                    },
                    {
                        typeof(DocumentedTagHelper),
                        nameof(DocumentedTagHelper.RemarksProperty),
                        null,
                        null
                    },
                    {
                        typeof(DocumentedTagHelper),
                        nameof(DocumentedTagHelper.RemarksAndSummaryProperty),
                        null,
                        null
                    },
                    {
                        typeof(DocumentedTagHelper),
                        nameof(DocumentedTagHelper.UndocumentedProperty),
                        defaultLocation,
                        null
                    },
                    {
                        typeof(DocumentedTagHelper),
                        nameof(DocumentedTagHelper.SummaryProperty),
                        defaultLocation,
                        new TagHelperAttributeDesignTimeDescriptor
                        {
                            Summary = PropertySummary
                        }
                    },
                    {
                        typeof(DocumentedTagHelper),
                        nameof(DocumentedTagHelper.RemarksProperty),
                        defaultLocation,
                        new TagHelperAttributeDesignTimeDescriptor
                        {
                            Remarks = PropertyRemarks
                        }
                    },
                    {
                        typeof(DocumentedTagHelper),
                        nameof(DocumentedTagHelper.RemarksAndSummaryProperty),
                        defaultLocation,
                        new TagHelperAttributeDesignTimeDescriptor
                        {
                            Summary = PropertyWithSummaryAndRemarks_Summary,
                            Remarks = PropertyWithSummaryAndRemarks_Remarks
                        }
                    },
                    {
                        typeof(DocumentedTagHelper),
                        nameof(DocumentedTagHelper.SummaryProperty),
                        defaultLocation,
                        new TagHelperAttributeDesignTimeDescriptor
                        {
                            Summary = PropertySummary
                        }
                    },
                    {
                        typeof(DocumentedTagHelper),
                        nameof(DocumentedTagHelper.RemarksProperty),
                        defaultLocation,
                        new TagHelperAttributeDesignTimeDescriptor
                        {
                            Remarks = PropertyRemarks
                        }
                    },
                    {
                        typeof(DocumentedTagHelper),
                        nameof(DocumentedTagHelper.RemarksAndSummaryProperty),
                        nonExistentLocation,
                        null
                    },
                    {
                        typeof(DocumentedTagHelper),
                        nameof(DocumentedTagHelper.RemarksAndSummaryProperty),
                        invalidLocation,
                        null
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(CreateAttributeDescriptor_PropertyDocumentationData))]
        public void CreateAttributeDescriptor_ReturnsExpectedDescriptors(
            Type tagHelperType,
            string propertyName,
            string assemblyLocation,
            TagHelperAttributeDesignTimeDescriptor expectedDesignTimeDescriptor)
        {
            // Arrange
            var mockPropertyInfo = new Mock<PropertyInfo>();
            mockPropertyInfo.Setup(propertyInfo => propertyInfo.DeclaringType).Returns(tagHelperType);
            mockPropertyInfo.Setup(propertyInfo => propertyInfo.Name).Returns(propertyName);
            var factory = new TestTagHelperDesignTimeDescriptorFactory(assemblyLocation);

            // Act
            var designTimeDescriptor = factory.CreateAttributeDescriptor(
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
                // tagHelperType, assemblyLocation, expectedDesignTimeDescriptor, culture
                return new TheoryData<Type, string, TagHelperAttributeDesignTimeDescriptor, string>
                {
                    {
                        typeof(DocumentedTagHelper),
                        LocalizedDocumentedAssemblyLocation,
                        new TagHelperAttributeDesignTimeDescriptor
                        {
                            Summary = "en-GB: " + PropertyWithSummaryAndRemarks_Summary,
                            Remarks = "en-GB: " + PropertyWithSummaryAndRemarks_Remarks
                        },
                        "en-GB"
                    },
                    {
                        typeof(DocumentedTagHelper),
                        LocalizedDocumentedAssemblyLocation,
                        new TagHelperAttributeDesignTimeDescriptor
                        {
                            Summary = "en: " + PropertyWithSummaryAndRemarks_Summary,
                            Remarks = "en: " + PropertyWithSummaryAndRemarks_Remarks
                        },
                        "en-US"
                    },
                    {
                        typeof(DocumentedTagHelper),
                        LocalizedDocumentedAssemblyLocation,
                        new TagHelperAttributeDesignTimeDescriptor
                        {
                            Summary = "fr-FR: " + PropertyWithSummaryAndRemarks_Summary,
                            Remarks = "fr-FR: " + PropertyWithSummaryAndRemarks_Remarks
                        },
                        "fr-FR"
                    },
                    {
                        typeof(DocumentedTagHelper),
                        LocalizedDocumentedAssemblyLocation,
                        new TagHelperAttributeDesignTimeDescriptor
                        {
                            Summary = "fr: " + PropertyWithSummaryAndRemarks_Summary,
                            Remarks = "fr: " + PropertyWithSummaryAndRemarks_Remarks
                        },
                        "fr-BE"
                    },
                    {
                        typeof(DocumentedTagHelper),
                        LocalizedDocumentedAssemblyLocation,
                        new TagHelperAttributeDesignTimeDescriptor
                        {
                            Summary = "nl-BE: " + PropertyWithSummaryAndRemarks_Summary,
                            Remarks = "nl-BE: " + PropertyWithSummaryAndRemarks_Remarks
                        },
                        "nl-BE"
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(CreateAttributeDescriptor_LocalizedPropertyData))]
        public void CreateAttributeDescriptor_WithLocalizedProperty_ReturnsExpectedDescriptors(
            Type tagHelperType,
            string assemblyLocation,
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
            var factory = new TestTagHelperDesignTimeDescriptorFactory(assemblyLocation);

            // Act
            using (new CultureReplacer(culture))
            {
                designTimeDescriptor = factory.CreateAttributeDescriptor(
                    mockPropertyInfo.Object);
            }

            // Assert
            Assert.Equal(
                expectedDesignTimeDescriptor,
                designTimeDescriptor,
                TagHelperAttributeDesignTimeDescriptorComparer.Default);
        }

        [Fact]
        public void CreateDescriptor_WithLocalizedProperty_CachesBasedOnCulture()
        {
            // Arrange
            var tagHelperType = typeof(DocumentedTagHelper);
            var mockPropertyInfo = new Mock<PropertyInfo>();
            mockPropertyInfo.Setup(propertyInfo => propertyInfo.DeclaringType).Returns(tagHelperType);
            mockPropertyInfo
                .Setup(propertyInfo => propertyInfo.Name)
                .Returns(nameof(DocumentedTagHelper.RemarksAndSummaryProperty));
            var factory = new TestTagHelperDesignTimeDescriptorFactory(LocalizedDocumentedAssemblyLocation);
            var expectedFRDesignTimeDescriptor = new TagHelperAttributeDesignTimeDescriptor
            {
                Summary = "fr-FR: " + PropertyWithSummaryAndRemarks_Summary,
                Remarks = "fr-FR: " + PropertyWithSummaryAndRemarks_Remarks
            };
            var expectedNLBEDesignTimeDescriptor = new TagHelperAttributeDesignTimeDescriptor
            {
                Summary = "nl-BE: " + PropertyWithSummaryAndRemarks_Summary,
                Remarks = "nl-BE: " + PropertyWithSummaryAndRemarks_Remarks
            };
            TagHelperAttributeDesignTimeDescriptor frDesignTimeDescriptor, nlBEDesignTimeDescriptor;

            // Act
            using (new CultureReplacer("fr-FR"))
            {
                frDesignTimeDescriptor = factory.CreateAttributeDescriptor(mockPropertyInfo.Object);
            }
            using (new CultureReplacer("nl-BE"))
            {
                nlBEDesignTimeDescriptor = factory.CreateAttributeDescriptor(mockPropertyInfo.Object);
            }

            // Assert
            Assert.Equal(
                expectedFRDesignTimeDescriptor,
                frDesignTimeDescriptor,
                TagHelperAttributeDesignTimeDescriptorComparer.Default);
            Assert.Equal(
                expectedNLBEDesignTimeDescriptor,
                nlBEDesignTimeDescriptor,
                TagHelperAttributeDesignTimeDescriptorComparer.Default);
        }

        private class TestTagHelperDesignTimeDescriptorFactory : TagHelperDesignTimeDescriptorFactory
        {
            private readonly string _assemblyLocation;

            public TestTagHelperDesignTimeDescriptorFactory(string assemblyLocation)
                : base()
            {
                _assemblyLocation = assemblyLocation;
            }

            public override string GetAssemblyLocation(Assembly assembly)
            {
                return _assemblyLocation;
            }
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