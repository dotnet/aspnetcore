// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if !DNXCORE50
using System;
using System.IO;
using System.Reflection;
using Microsoft.AspNet.Razor.Compilation.TagHelpers;
using Microsoft.AspNet.Razor.TagHelpers;
using Microsoft.AspNet.Razor.Test.Internal;
using Microsoft.AspNet.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    public class TagHelperDesignTimeDescriptorFactoryTest
    {
        private const string TypeSummary = "The summary for <see cref=\"T:Microsoft.AspNet.Razor." +
            "TagHelpers.DocumentedTagHelper\" />.";
        private const string TypeRemarks = "Inherits from <see cref=\"T:Microsoft.AspNet.Razor." +
            "TagHelpers.TagHelper\" />.";
        private const string PropertySummary = "This <see cref=\"P:Microsoft.AspNet.Razor." +
            "TagHelpers.DocumentedTagHelper.SummaryProperty\" /> is of type <see cref=\"T:System.String\" />.";
        private const string PropertyRemarks = "The <see cref=\"P:Microsoft.AspNet.Razor." +
            "TagHelpers.DocumentedTagHelper.SummaryProperty\" /> may be <c>null</c>.";
        private const string PropertyWithSummaryAndRemarks_Summary = "This is a complex <see cref=\"T:System." +
            "Collections.Generic.IDictionary`2\" />.";
        private const string PropertyWithSummaryAndRemarks_Remarks = "<see cref=\"P:Microsoft.AspNet.Razor." +
            "TagHelpers.DocumentedTagHelper.SummaryProperty\" /><see cref=\"P:Microsoft.AspNet.Razor" +
            ".TagHelpers.DocumentedTagHelper.RemarksProperty\" />";

        // These test assemblies don't really exist. They are used to look up corresponding XML for a fake assembly
        // which is based on the DocumentedTagHelper type.
        public static readonly string TestRoot =
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", ".."));
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

                // tagHelperType, expectedDesignTimeDescriptor
                return new TheoryData<Type, TagHelperDesignTimeDescriptor>
                {
                    { CreateDocumentationTagHelperType(location: null, codeBase: null), onlyHint },
                    {
                        CreateDocumentationTagHelperType(defaultLocation, codeBase: null),
                        new TagHelperDesignTimeDescriptor
                        {
                            Summary = TypeSummary,
                            Remarks = TypeRemarks,
                            OutputElementHint = "p"
                        }
                    },
                    {
                        CreateDocumentationTagHelperType(location: null, codeBase: defaultCodeBase),
                        new TagHelperDesignTimeDescriptor
                        {
                            Summary = TypeSummary,
                            Remarks = TypeRemarks,
                            OutputElementHint = "p"
                        }
                    },
                    {
                        CreateDocumentationTagHelperType(defaultLocation, defaultCodeBase),
                        new TagHelperDesignTimeDescriptor
                        {
                            Summary = TypeSummary,
                            Remarks = TypeRemarks,
                            OutputElementHint = "p"
                        }
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
            // Arrange
            var factory = new TagHelperDesignTimeDescriptorFactory();

            // Act
            var designTimeDescriptor = factory.CreateDescriptor(tagHelperType);

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
                        new TagHelperDesignTimeDescriptor
                        {
                            Summary = "en-GB: " + TypeSummary,
                            Remarks = "en-GB: " + TypeRemarks,
                            OutputElementHint = "p"
                        },
                        "en-GB"
                    },
                    {
                        CreateDocumentationTagHelperType(LocalizedDocumentedAssemblyLocation, codeBase: null),
                        new TagHelperDesignTimeDescriptor
                        {
                            Summary = "en: " + TypeSummary,
                            Remarks = "en: " + TypeRemarks,
                            OutputElementHint = "p"
                        },
                        "en-US"
                    },
                    {
                        CreateDocumentationTagHelperType(LocalizedDocumentedAssemblyLocation, codeBase: null),
                        new TagHelperDesignTimeDescriptor
                        {
                            Summary = "fr-FR: " + TypeSummary,
                            Remarks = "fr-FR: " + TypeRemarks,
                            OutputElementHint = "p"
                        },
                        "fr-FR"
                    },
                    {
                        CreateDocumentationTagHelperType(LocalizedDocumentedAssemblyLocation, codeBase: null),
                        new TagHelperDesignTimeDescriptor
                        {
                            Summary = "fr: " + TypeSummary,
                            Remarks = "fr: " + TypeRemarks,
                            OutputElementHint = "p"
                        },
                        "fr-BE"
                    },
                    {
                        CreateDocumentationTagHelperType(LocalizedDocumentedAssemblyLocation, codeBase: null),
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
            TagHelperDesignTimeDescriptor expectedDesignTimeDescriptor,
            string culture)
        {
            // Arrange
            TagHelperDesignTimeDescriptor designTimeDescriptor;
            var factory = new TagHelperDesignTimeDescriptorFactory();

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
            var factory = new TagHelperDesignTimeDescriptorFactory();
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
            var tagHelperType = CreateDocumentationTagHelperType(LocalizedDocumentedAssemblyLocation, codeBase: null);
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
                        new TagHelperAttributeDesignTimeDescriptor
                        {
                            Summary = PropertySummary
                        }
                    },
                    {
                        CreateDocumentationTagHelperType(defaultLocation, codeBase: null),
                        nameof(DocumentedTagHelper.RemarksProperty),
                        new TagHelperAttributeDesignTimeDescriptor
                        {
                            Remarks = PropertyRemarks
                        }
                    },
                    {
                        CreateDocumentationTagHelperType(defaultLocation, codeBase: null),
                        nameof(DocumentedTagHelper.RemarksAndSummaryProperty),
                        new TagHelperAttributeDesignTimeDescriptor
                        {
                            Summary = PropertyWithSummaryAndRemarks_Summary,
                            Remarks = PropertyWithSummaryAndRemarks_Remarks
                        }
                    },
                    {
                        CreateDocumentationTagHelperType(location: null, codeBase: defaultCodeBase),
                        nameof(DocumentedTagHelper.SummaryProperty),
                        new TagHelperAttributeDesignTimeDescriptor
                        {
                            Summary = PropertySummary
                        }
                    },
                    {
                        CreateDocumentationTagHelperType(location: null, codeBase: defaultCodeBase),
                        nameof(DocumentedTagHelper.RemarksProperty),
                        new TagHelperAttributeDesignTimeDescriptor
                        {
                            Remarks = PropertyRemarks
                        }
                    },
                    {
                        CreateDocumentationTagHelperType(location: null, codeBase: defaultCodeBase),
                        nameof(DocumentedTagHelper.RemarksAndSummaryProperty),
                        new TagHelperAttributeDesignTimeDescriptor
                        {
                            Summary = PropertyWithSummaryAndRemarks_Summary,
                            Remarks = PropertyWithSummaryAndRemarks_Remarks
                        }
                    },
                    {
                        CreateDocumentationTagHelperType(defaultLocation, defaultCodeBase),
                        nameof(DocumentedTagHelper.SummaryProperty),
                        new TagHelperAttributeDesignTimeDescriptor
                        {
                            Summary = PropertySummary
                        }
                    },
                    {
                        CreateDocumentationTagHelperType(defaultLocation, defaultCodeBase),
                        nameof(DocumentedTagHelper.RemarksProperty),
                        new TagHelperAttributeDesignTimeDescriptor
                        {
                            Remarks = PropertyRemarks
                        }
                    },
                    {
                        CreateDocumentationTagHelperType(defaultLocation, defaultCodeBase),
                        nameof(DocumentedTagHelper.RemarksAndSummaryProperty),
                        new TagHelperAttributeDesignTimeDescriptor
                        {
                            Summary = PropertyWithSummaryAndRemarks_Summary,
                            Remarks = PropertyWithSummaryAndRemarks_Remarks
                        }
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
            var factory = new TagHelperDesignTimeDescriptorFactory();

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
                // tagHelperType, expectedDesignTimeDescriptor, culture
                return new TheoryData<Type, TagHelperAttributeDesignTimeDescriptor, string>
                {
                    {
                        CreateDocumentationTagHelperType(LocalizedDocumentedAssemblyLocation, codeBase: null),
                        new TagHelperAttributeDesignTimeDescriptor
                        {
                            Summary = "en-GB: " + PropertyWithSummaryAndRemarks_Summary,
                            Remarks = "en-GB: " + PropertyWithSummaryAndRemarks_Remarks
                        },
                        "en-GB"
                    },
                    {
                        CreateDocumentationTagHelperType(LocalizedDocumentedAssemblyLocation, codeBase: null),
                        new TagHelperAttributeDesignTimeDescriptor
                        {
                            Summary = "en: " + PropertyWithSummaryAndRemarks_Summary,
                            Remarks = "en: " + PropertyWithSummaryAndRemarks_Remarks
                        },
                        "en-US"
                    },
                    {
                        CreateDocumentationTagHelperType(LocalizedDocumentedAssemblyLocation, codeBase: null),
                        new TagHelperAttributeDesignTimeDescriptor
                        {
                            Summary = "fr-FR: " + PropertyWithSummaryAndRemarks_Summary,
                            Remarks = "fr-FR: " + PropertyWithSummaryAndRemarks_Remarks
                        },
                        "fr-FR"
                    },
                    {
                        CreateDocumentationTagHelperType(LocalizedDocumentedAssemblyLocation, codeBase: null),
                        new TagHelperAttributeDesignTimeDescriptor
                        {
                            Summary = "fr: " + PropertyWithSummaryAndRemarks_Summary,
                            Remarks = "fr: " + PropertyWithSummaryAndRemarks_Remarks
                        },
                        "fr-BE"
                    },
                    {
                        CreateDocumentationTagHelperType(LocalizedDocumentedAssemblyLocation, codeBase: null),
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
            var factory = new TagHelperDesignTimeDescriptorFactory();

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
            var tagHelperType = CreateDocumentationTagHelperType(LocalizedDocumentedAssemblyLocation, codeBase: null);
            var mockPropertyInfo = new Mock<PropertyInfo>();
            mockPropertyInfo.Setup(propertyInfo => propertyInfo.DeclaringType).Returns(tagHelperType);
            mockPropertyInfo
                .Setup(propertyInfo => propertyInfo.Name)
                .Returns(nameof(DocumentedTagHelper.RemarksAndSummaryProperty));
            var factory = new TagHelperDesignTimeDescriptorFactory();
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

        private static Type CreateDocumentationTagHelperType(string location, string codeBase)
        {
            return CreateType<DocumentedTagHelper>(location, codeBase);
        }

        private static Type CreateType<TWrappedType>(string location, string codeBase)
        {
            var testAssembly = new TestAssembly(location, codeBase);
            var wrappedType = typeof(TWrappedType);

            var mockType = new Mock<Type>();
            var mockReflectedType = mockType.As<IReflectableType>();

            // TypeDelegator inherits from abstract TypeInfo class and has a constructor Moq can use.
            var mockTypeInfo = new Mock<TypeDelegator>(mockType.Object);
            mockReflectedType.Setup(type => type.GetTypeInfo()).Returns(mockTypeInfo.Object);
            mockTypeInfo.Setup(typeInfo => typeInfo.Assembly).Returns(testAssembly);
            mockTypeInfo.Setup(typeInfo => typeInfo.FullName).Returns(wrappedType.FullName);

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

            public override AssemblyName GetName() { return new AssemblyName("TestAssembly"); }
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