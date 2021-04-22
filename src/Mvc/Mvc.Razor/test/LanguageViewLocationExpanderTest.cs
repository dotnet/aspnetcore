// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor
{
    public class LanguageViewLocationExpanderTest
    {
        public static IEnumerable<object[]> ViewLocationExpanderTestDataWithExpectedValues
        {
            get
            {
                yield return new object[]
                {
                    LanguageViewLocationExpanderFormat.Suffix,
                    new[]
                    {
                        "/Views/{1}/{0}.cshtml",
                        "/Views/Shared/{0}.cshtml"
                    },
                    new[]
                    {
                        "/Views/{1}/{0}.en-GB.cshtml",
                        "/Views/{1}/{0}.en.cshtml",
                        "/Views/{1}/{0}.cshtml",
                        "/Views/Shared/{0}.en-GB.cshtml",
                        "/Views/Shared/{0}.en.cshtml",
                        "/Views/Shared/{0}.cshtml"
                    }
                };

                yield return new object[]
                {
                    LanguageViewLocationExpanderFormat.SubFolder,
                    new[]
                    {
                        "/Views/{1}/{0}.cshtml",
                        "/Views/Shared/{0}.cshtml"
                    },
                    new[]
                    {
                        "/Views/{1}/en-GB/{0}.cshtml",
                        "/Views/{1}/en/{0}.cshtml",
                        "/Views/{1}/{0}.cshtml",
                        "/Views/Shared/en-GB/{0}.cshtml",
                        "/Views/Shared/en/{0}.cshtml",
                        "/Views/Shared/{0}.cshtml"
                    }
                };

                yield return new object[]
                {
                    LanguageViewLocationExpanderFormat.Suffix,
                    new[]
                    {
                        "/Areas/{2}/Views/{1}/{0}.cshtml",
                        "/Areas/{2}/Views/Shared/{0}.cshtml",
                        "/Views/Shared/{0}.cshtml"
                    },
                    new[]
                    {
                        "/Areas/{2}/Views/{1}/{0}.en-GB.cshtml",
                        "/Areas/{2}/Views/{1}/{0}.en.cshtml",
                        "/Areas/{2}/Views/{1}/{0}.cshtml",
                        "/Areas/{2}/Views/Shared/{0}.en-GB.cshtml",
                        "/Areas/{2}/Views/Shared/{0}.en.cshtml",
                        "/Areas/{2}/Views/Shared/{0}.cshtml",
                        "/Views/Shared/{0}.en-GB.cshtml",
                        "/Views/Shared/{0}.en.cshtml",
                        "/Views/Shared/{0}.cshtml"
                    }
                };

                yield return new object[]
                {
                    LanguageViewLocationExpanderFormat.SubFolder,
                    new[]
                    {
                        "/Areas/{2}/Views/{1}/{0}.cshtml",
                        "/Areas/{2}/Views/Shared/{0}.cshtml",
                        "/Views/Shared/{0}.cshtml"
                    },
                    new[]
                    {
                        "/Areas/{2}/Views/{1}/en-GB/{0}.cshtml",
                        "/Areas/{2}/Views/{1}/en/{0}.cshtml",
                        "/Areas/{2}/Views/{1}/{0}.cshtml",
                        "/Areas/{2}/Views/Shared/en-GB/{0}.cshtml",
                        "/Areas/{2}/Views/Shared/en/{0}.cshtml",
                        "/Areas/{2}/Views/Shared/{0}.cshtml",
                        "/Views/Shared/en-GB/{0}.cshtml",
                        "/Views/Shared/en/{0}.cshtml",
                        "/Views/Shared/{0}.cshtml"
                    }
                };
            }
        }

        public static IEnumerable<object[]> ViewLocationExpanderTestData
        {
            get
            {
                yield return new object[]
                {
                    new[]
                    {
                        "/Views/{1}/{0}.cshtml",
                        "/Views/Shared/{0}.cshtml"
                    }
                };

                yield return new object[]
                {
                    new[]
                    {
                        "/Areas/{2}/Views/{1}/{0}.cshtml",
                        "/Areas/{2}/Views/Shared/{0}.cshtml",
                        "/Views/Shared/{0}.cshtml"
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(ViewLocationExpanderTestDataWithExpectedValues))]
        public void ExpandViewLocations_SpecificLocale(
            LanguageViewLocationExpanderFormat format,
            IEnumerable<string> viewLocations,
            IEnumerable<string> expectedViewLocations)
        {
            // Arrange
            var viewLocationExpanderContext = new ViewLocationExpanderContext(
                new ActionContext(),
                "testView",
                "test-controller",
                "",
                null,
                false);
            var languageViewLocationExpander = new LanguageViewLocationExpander(format);
            viewLocationExpanderContext.Values = new Dictionary<string, string>();
            viewLocationExpanderContext.Values["language"] = "en-GB";

            // Act
            var expandedViewLocations = languageViewLocationExpander.ExpandViewLocations(
                viewLocationExpanderContext,
                viewLocations);

            // Assert
            Assert.Equal(expectedViewLocations, expandedViewLocations);
        }

        [Theory]
        [MemberData(nameof(ViewLocationExpanderTestData))]
        public void ExpandViewLocations_NullContextValue(IEnumerable<string> viewLocations)
        {
            // Arrange
            var viewLocationExpanderContext = new ViewLocationExpanderContext(
                new ActionContext(),
                "testView",
                "test-controller",
                "test-area",
                null,
                false);
            var languageViewLocationExpander = new LanguageViewLocationExpander();
            viewLocationExpanderContext.Values = new Dictionary<string, string>();

            // Act
            var expandedViewLocations = languageViewLocationExpander.ExpandViewLocations(
                viewLocationExpanderContext,
                viewLocations);

            // Assert
            Assert.Equal(viewLocations, expandedViewLocations);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux,
            SkipReason = "Invalid culture detection is OS-specific")]
        [OSSkipCondition(OperatingSystems.MacOSX,
            SkipReason = "Invalid culture detection is OS-specific")]
        [MemberData(nameof(ViewLocationExpanderTestData))]
        public void ExpandViewLocations_IncorrectLocaleContextValue(IEnumerable<string> viewLocations)
        {
            // Arrange
            var viewLocationExpanderContext = new ViewLocationExpanderContext(
                new ActionContext(),
                "testView",
                "test-controller",
                "test-area",
                null,
                false);
            var languageViewLocationExpander = new LanguageViewLocationExpander();
            viewLocationExpanderContext.Values = new Dictionary<string, string>();
            viewLocationExpanderContext.Values["language"] = "!-invalid-culture-!";

            // Act
            var expandedViewLocations = languageViewLocationExpander.ExpandViewLocations(
                viewLocationExpanderContext,
                viewLocations);

            // Assert
            Assert.Equal(viewLocations, expandedViewLocations);
        }
    }
}
