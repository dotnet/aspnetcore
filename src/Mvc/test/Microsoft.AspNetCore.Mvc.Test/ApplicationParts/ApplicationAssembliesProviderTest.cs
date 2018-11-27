// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyModel;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ApplicationParts
{
    public class ApplicationAssembliesProviderTest
    {
        private static readonly Assembly ThisAssembly = typeof(ApplicationAssembliesProviderTest).Assembly;

        // This test verifies ApplicationAssembliesProviderTest.ReferenceAssemblies reflects the actual loadable assemblies
        // of the libraries that Microsoft.AspNetCore.Mvc depends on.
        // If we add or remove dependencies, this test should be changed together.
        [Fact]
        public void ReferenceAssemblies_ReturnsLoadableReferenceAssemblies()
        {
            // Arrange
            var excludeAssemblies = new string[]
            {
                "Microsoft.AspNetCore.Mvc.Analyzers",
                "Microsoft.AspNetCore.Mvc.Test",
                "Microsoft.AspNetCore.Mvc.Core.TestCommon",
            };

            var additionalAssemblies = new[]
            {
                // The following assemblies are not reachable from Microsoft.AspNetCore.Mvc
                "Microsoft.AspNetCore.App",
                "Microsoft.AspNetCore.Mvc.Formatters.Xml",
            };

            var dependencyContextLibraries = DependencyContext.Load(ThisAssembly)
                .CompileLibraries
                .Where(r => r.Name.StartsWith("Microsoft.AspNetCore.Mvc", StringComparison.OrdinalIgnoreCase) &&
                    !excludeAssemblies.Contains(r.Name, StringComparer.OrdinalIgnoreCase))
                .Select(r => r.Name);

            var expected = dependencyContextLibraries
                .Concat(additionalAssemblies)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase);

            // Act
            var referenceAssemblies = ApplicationAssembliesProvider
                .ReferenceAssemblies
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase);

            // Assert
            Assert.Equal(expected, referenceAssemblies, StringComparer.OrdinalIgnoreCase);
        }
    }
}
