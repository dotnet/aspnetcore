// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.Testing;
using VerifyCS = Microsoft.AspNetCore.Analyzers.Verifiers.CSharpAnalyzerVerifier<Microsoft.AspNetCore.Analyzers.Mvc.MvcAnalyzer>;

namespace Microsoft.AspNetCore.Analyzers.Mvc;

public partial class DetectAmbiguousActionRoutesTest
{
    [Fact]
    public async Task SameRoutes_DifferentAction_HasDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
public class WeatherForecastController : ControllerBase
{
    [Route({|#0:""/a""|})]
    public object Get() => new object();

    [Route({|#1:""/a""|})]
    public object Get1() => new object();
}
internal class Program
{
    static void Main(string[] args)
    {
    }
}
";

        var expectedDiagnostics = new[] {
            new DiagnosticResult(DiagnosticDescriptors.AmbiguousActionRoute).WithArguments("/a").WithLocation(0),
            new DiagnosticResult(DiagnosticDescriptors.AmbiguousActionRoute).WithArguments("/a").WithLocation(1)
        };

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(source, expectedDiagnostics);
    }

    [Fact]
    public async Task MixedRoutes_DifferentAction_HasDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
public class WeatherForecastController : ControllerBase
{
    [Route({|#0:""/a""|})]
    public object Get() => new object();

    [Route({|#1:""/a""|})]
    public object Get1() => new object();

    [Route({|#2:""/a""|})]
    public object Get2() => new object();

    [HttpGet({|#3:""/b""|})]
    public object Get3() => new object();

    [HttpGet({|#4:""/b""|})]
    public object Get4() => new object();
}
internal class Program
{
    static void Main(string[] args)
    {
    }
}
";

        var expectedDiagnostics = new[] {
            new DiagnosticResult(DiagnosticDescriptors.AmbiguousActionRoute).WithArguments("/a").WithLocation(0),
            new DiagnosticResult(DiagnosticDescriptors.AmbiguousActionRoute).WithArguments("/a").WithLocation(1),
            new DiagnosticResult(DiagnosticDescriptors.AmbiguousActionRoute).WithArguments("/a").WithLocation(2),
            new DiagnosticResult(DiagnosticDescriptors.AmbiguousActionRoute).WithArguments("/b").WithLocation(3),
            new DiagnosticResult(DiagnosticDescriptors.AmbiguousActionRoute).WithArguments("/b").WithLocation(4)
        };

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(source, expectedDiagnostics);
    }

    [Fact]
    public async Task SameRoutes_DifferentAction_HostAttribute_NoDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
public class WeatherForecastController : ControllerBase
{
    [Route(""/a"")]
    public object Get() => new object();

    [Route(""/a"")]
    [Host(""consoto.com"")]
    public object Get1() => new object();
}
internal class Program
{
    static void Main(string[] args)
    {
    }
}
";

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task SameRoutes_SameAction_HostAttribute_HasDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
public class WeatherForecastController : ControllerBase
{
    [Route({|#0:""/a""|})]
    [Route({|#1:""/a""|})]
    [Host(""consoto.com"")]
    public object Get1() => new object();
}
internal class Program
{
    static void Main(string[] args)
    {
    }
}
";

        var expectedDiagnostics = new[] {
            new DiagnosticResult(DiagnosticDescriptors.AmbiguousActionRoute).WithArguments("/a").WithLocation(0),
            new DiagnosticResult(DiagnosticDescriptors.AmbiguousActionRoute).WithArguments("/a").WithLocation(1)
        };

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(source, expectedDiagnostics);
    }

    [Fact]
    public async Task SameRoutes_DifferentAction_AuthorizeAttribute_HasDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
public class WeatherForecastController : ControllerBase
{
    [Route({|#0:""/a""|})]
    public object Get() => new object();

    [Route({|#1:""/a""|})]
    [Authorize]
    public object Get1() => new object();
}
internal class Program
{
    static void Main(string[] args)
    {
    }
}
";

        var expectedDiagnostics = new[] {
            new DiagnosticResult(DiagnosticDescriptors.AmbiguousActionRoute).WithArguments("/a").WithLocation(0),
            new DiagnosticResult(DiagnosticDescriptors.AmbiguousActionRoute).WithArguments("/a").WithLocation(1)
        };

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(source, expectedDiagnostics);
    }

    [Fact]
    public async Task SameRoutes_SameAction_AuthorizeAttribute_HasDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
public class WeatherForecastController : ControllerBase
{
    [Route({|#0:""/a""|})]
    [Route({|#1:""/a""|})]
    [Authorize]
    public object Get1() => new object();
}
internal class Program
{
    static void Main(string[] args)
    {
    }
}
";

        var expectedDiagnostics = new[] {
            new DiagnosticResult(DiagnosticDescriptors.AmbiguousActionRoute).WithArguments("/a").WithLocation(0),
            new DiagnosticResult(DiagnosticDescriptors.AmbiguousActionRoute).WithArguments("/a").WithLocation(1)
        };

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(source, expectedDiagnostics);
    }

    [Fact]
    public async Task DifferentRoutes_DifferentAction_NoDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
public class WeatherForecastController : ControllerBase
{
    [Route(""/a"")]
    public object Get() => new object();

    [Route(""/b"")]
    public object Get1() => new object();
}
internal class Program
{
    static void Main(string[] args)
    {
    }
}
";

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task SameRoute_DifferentMethods_NoDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
public class WeatherForecastController : ControllerBase
{
    [HttpGet(""/"")]
    public object Get() => new object();

    [HttpPost(""/"")]
    public object Get1() => new object();
}
internal class Program
{
    static void Main(string[] args)
    {
    }
}
";

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task SameRoute_DifferentMethods_Route_NoDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
public class WeatherForecastController : ControllerBase
{
    [HttpGet(""/"")]
    public object Get() => new object();

    [Route(""/"")]
    public object Get1() => new object();
}
internal class Program
{
    static void Main(string[] args)
    {
    }
}
";

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task DuplicateRoutes_SameAction_HasDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
public class WeatherForecastController : ControllerBase
{
    [Route({|#0:""/""|})]
    [Route({|#1:""/""|})]
    public object Get() => new object();
}
internal class Program
{
    static void Main(string[] args)
    {
    }
}
";

        var expectedDiagnostics = new[] {
            new DiagnosticResult(DiagnosticDescriptors.AmbiguousActionRoute).WithArguments("/").WithLocation(0),
            new DiagnosticResult(DiagnosticDescriptors.AmbiguousActionRoute).WithArguments("/").WithLocation(1)
        };

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(source, expectedDiagnostics);
    }
}

