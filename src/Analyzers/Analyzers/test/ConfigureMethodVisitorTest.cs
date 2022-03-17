//Licensed to the .NET Foundation under one or more agreements.
//The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers;

public class ConfigureMethodVisitorTest
{
    [Fact]
    public void FindConfigureMethods_AtDifferentScopes()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;

public class GlobalStartup
{
    public void Configure(IApplicationBuilder app)
    {
    }
}

namespace Another
{
    public class AnotherStartup
    {
        public void Configure(IApplicationBuilder app)
        {
        }
    }
}

namespace ANamespace
{
    public class Startup
    {
        public void ConfigureDevelopment(IApplicationBuilder app)
        {
        }

        public class NestedStartup
        {
            public void ConfigureTest(IApplicationBuilder app)
            {
            }
        }
    }
}

namespace ANamespace.Nested
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
        }

        public class NestedStartup
        {
            public void Configure(IApplicationBuilder app)
            {
            }
        }
    }
}";

        var expected = new string[]
        {
                "global::ANamespace.Nested.Startup.Configure",
                "global::ANamespace.Nested.Startup.NestedStartup.Configure",
                "global::ANamespace.Startup.ConfigureDevelopment",
                "global::ANamespace.Startup.NestedStartup.ConfigureTest",
                "global::Another.AnotherStartup.Configure",
                "global::GlobalStartup.Configure",
        };

        var compilation = TestCompilation.Create(source);
        var symbols = new StartupSymbols(compilation);

        // Act
        var results = ConfigureMethodVisitor.FindConfigureMethods(symbols, compilation.Assembly);

        // Assert
        var actual = results
            .Select(m => m.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + "." + m.Name)
            .OrderBy(s => s)
            .ToArray();
        Assert.Equal(expected, actual);
    }
}
