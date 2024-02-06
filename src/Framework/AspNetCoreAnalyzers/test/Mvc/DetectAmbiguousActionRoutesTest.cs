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
    public async Task ActionReplacementToken_DifferentActionNames_NoDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
public class WeatherForecastController : ControllerBase
{
    [Route(""[action]"")]
    public object Get() => new object();

    [Route(""[action]"")]
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
    public async Task ActionReplacementToken_SameActionName_HasDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
public class WeatherForecastController : ControllerBase
{
    [Route({|#0:""[action]""|})]
    public object Get() => new object();

    [Route({|#1:""[action]""|})]
    public object Get(int i) => new object();
}
internal class Program
{
    static void Main(string[] args)
    {
    }
}
";

        var expectedDiagnostics = new[] {
            new DiagnosticResult(DiagnosticDescriptors.AmbiguousActionRoute).WithArguments("[action]").WithLocation(0),
            new DiagnosticResult(DiagnosticDescriptors.AmbiguousActionRoute).WithArguments("[action]").WithLocation(1)
        };

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(source, expectedDiagnostics);
    }

    [Fact]
    public async Task ActionReplacementToken_ActionNameAttribute_HasDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
public class WeatherForecastController : ControllerBase
{
    [Route({|#0:""[action]""|})]
    public object Get() => new object();

    [Route({|#1:""[action]""|})]
    [ActionName(""get"")]
    public object Get1(int i) => new object();
}
internal class Program
{
    static void Main(string[] args)
    {
    }
}
";

        var expectedDiagnostics = new[] {
            new DiagnosticResult(DiagnosticDescriptors.AmbiguousActionRoute).WithArguments("[action]").WithLocation(0),
            new DiagnosticResult(DiagnosticDescriptors.AmbiguousActionRoute).WithArguments("[action]").WithLocation(1)
        };

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(source, expectedDiagnostics);
    }

    [Fact]
    public async Task ActionReplacementToken_ActionNameAttributeNullValue_NoDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
public class WeatherForecastController : ControllerBase
{
    [Route({|#0:""[action]""|})]
    public object Get() => new object();

    [Route({|#1:""[action]""|})]
    [ActionName(null)]
    public object Get1(int i) => new object();
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
    public async Task ActionReplacementToken_OnController_NoDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
[Route(""[controller]/[action]"")]
public class WeatherForecastController : ControllerBase
{
    [Route(""{i}"")]
    public object Get(int i) => new object();

    [Route(""{i}"")]
    public object Get1(int i) => new object();
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
    public async Task ActionReplacementToken_OnBaseController_NoDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
[Route(""[controller]/[action]"")]
public class MyControllerBase : ControllerBase
{
}
public class WeatherForecastController : MyControllerBase
{
    [Route(""{i}"")]
    public object Get(int i) => new object();

    [Route(""{i}"")]
    public object Get1(int i) => new object();
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
    public async Task ActionReplacementToken_OnBaseControllerButOverridden_HasDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
[Route(""[controller]/[action]"")]
public class MyControllerBase : ControllerBase
{
}
[Route(""api"")]
public class WeatherForecastController : MyControllerBase
{
    [Route({|#0:""{i}""|})]
    public object Get(int i) => new object();

    [Route({|#1:""{i}""|})]
    public object Get1(int i) => new object();
}
internal class Program
{
    static void Main(string[] args)
    {
    }
}
";

        var expectedDiagnostics = new[] {
            new DiagnosticResult(DiagnosticDescriptors.AmbiguousActionRoute).WithArguments("{i}").WithLocation(0),
            new DiagnosticResult(DiagnosticDescriptors.AmbiguousActionRoute).WithArguments("{i}").WithLocation(1)
        };

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(source, expectedDiagnostics);
    }

    [Fact]
    public async Task ActionReplacementToken_OnController_ActionName_NoDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
[Route(""[controller]/[action]"")]
public class WeatherForecastController : ControllerBase
{
    [Route(""{i}"")]
    public object Get(int i) => new object();

    [Route(""{s}"")]
    [ActionName(name: ""getWithString"")]
    public object Get(string s) => new object();
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
    public async Task ActionReplacementToken_OnController_ActionNameOnBase_NoDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
public abstract class MyControllerBase : ControllerBase
{
    [ActionName(name: ""getWithString"")]
    public abstract object Get(string s);
}
[Route(""[controller]/[action]"")]
public class WeatherForecastController : MyControllerBase
{
    [Route(""{i}"")]
    public object Get(int i) => new object();

    [Route(""{s}"")]
    public override object Get(string s) => new object();
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
    public async Task ActionRouteToken_DifferentActionNames_NoDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
public class WeatherForecastController : ControllerBase
{
    [Route(""{action}"")]
    public object Get() => new object();

    [Route(""{action}"")]
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
    public async Task ActionRouteToken_SameActionName_HasDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
public class WeatherForecastController : ControllerBase
{
    [Route({|#0:""{action}""|})]
    public object Get() => new object();

    [Route({|#1:""{action}""|})]
    public object Get(int i) => new object();
}
internal class Program
{
    static void Main(string[] args)
    {
    }
}
";

        var expectedDiagnostics = new[] {
            new DiagnosticResult(DiagnosticDescriptors.AmbiguousActionRoute).WithArguments("{action}").WithLocation(0),
            new DiagnosticResult(DiagnosticDescriptors.AmbiguousActionRoute).WithArguments("{action}").WithLocation(1)
        };

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(source, expectedDiagnostics);
    }

    [Fact]
    public async Task ActionRouteToken_ActionNameAttribute_HasDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
public class WeatherForecastController : ControllerBase
{
    [Route({|#0:""{action}""|})]
    public object Get() => new object();

    [Route({|#1:""{action}""|})]
    [ActionName(""get"")]
    public object Get1(int i) => new object();
}
internal class Program
{
    static void Main(string[] args)
    {
    }
}
";

        var expectedDiagnostics = new[] {
            new DiagnosticResult(DiagnosticDescriptors.AmbiguousActionRoute).WithArguments("{action}").WithLocation(0),
            new DiagnosticResult(DiagnosticDescriptors.AmbiguousActionRoute).WithArguments("{action}").WithLocation(1)
        };

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(source, expectedDiagnostics);
    }

    [Fact]
    public async Task ActionRouteToken_ActionNameAttributeNullValue_NoDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
public class WeatherForecastController : ControllerBase
{
    [Route({|#0:""{action}""|})]
    public object Get() => new object();

    [Route({|#1:""{action}""|})]
    [ActionName(null)]
    public object Get1(int i) => new object();
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
    public async Task ActionRouteToken_OnController_NoDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
[Route(""{controller}/{action}"")]
public class WeatherForecastController : ControllerBase
{
    [Route(""{i}"")]
    public object Get(int i) => new object();

    [Route(""{i}"")]
    public object Get1(int i) => new object();
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
    public async Task ActionRouteToken_OnBaseController_NoDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
[Route(""{controller}/{action}"")]
public class MyControllerBase : ControllerBase
{
}
public class WeatherForecastController : MyControllerBase
{
    [Route(""{i}"")]
    public object Get(int i) => new object();

    [Route(""{i}"")]
    public object Get1(int i) => new object();
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
    public async Task ActionRouteToken_OnBaseControllerButOverridden_HasDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
[Route(""{controller}/{action}"")]
public class MyControllerBase : ControllerBase
{
}
[Route(""api"")]
public class WeatherForecastController : MyControllerBase
{
    [Route({|#0:""{i}""|})]
    public object Get(int i) => new object();

    [Route({|#1:""{i}""|})]
    public object Get1(int i) => new object();
}
internal class Program
{
    static void Main(string[] args)
    {
    }
}
";

        var expectedDiagnostics = new[] {
            new DiagnosticResult(DiagnosticDescriptors.AmbiguousActionRoute).WithArguments("{i}").WithLocation(0),
            new DiagnosticResult(DiagnosticDescriptors.AmbiguousActionRoute).WithArguments("{i}").WithLocation(1)
        };

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(source, expectedDiagnostics);
    }

    [Fact]
    public async Task ActionRouteToken_OnController_ActionName_NoDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
[Route(""{controller}/{action}"")]
public class WeatherForecastController : ControllerBase
{
    [Route(""{i}"")]
    public object Get(int i) => new object();

    [Route(""{s}"")]
    [ActionName(name: ""getWithString"")]
    public object Get(string s) => new object();
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
    public async Task ActionRouteToken_OnController_ActionNameOnBase_NoDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
public abstract class MyControllerBase : ControllerBase
{
    [ActionName(name: ""getWithString"")]
    public abstract object Get(string s);
}
[Route(""{controller}/{action}"")]
public class WeatherForecastController : MyControllerBase
{
    [Route(""{i}"")]
    public object Get(int i) => new object();

    [Route(""{s}"")]
    public override object Get(string s) => new object();
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

    [Fact]
    public async Task DuplicateRoutes_HasHttpAttributes_NoDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
public class MyController : Controller
{
    [HttpGet]
    [Route(""Person"")]
    public IActionResult Get() => Ok(""You GET me"");
    
    [HttpGet(""PersonGet"")]
    [HttpPut]
    [HttpPost]
    [Route(""Person"")]
    public IActionResult Put() => Ok(""You PUT me"");
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
    public async Task DuplicateRoutes_HasDuplicateHttpAttributes_HasDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
public class MyController : Controller
{
    [HttpGet]
    [Route({|#0:""Person""|})]
    public IActionResult Get() => Ok(""You GET me"");
    
    [HttpGet]
    [Route({|#1:""Person""|})]
    public IActionResult Put() => Ok(""You PUT me"");
}
internal class Program
{
    static void Main(string[] args)
    {
    }
}
";

        var expectedDiagnostics = new[] {
            new DiagnosticResult(DiagnosticDescriptors.AmbiguousActionRoute).WithArguments("Person").WithLocation(0),
            new DiagnosticResult(DiagnosticDescriptors.AmbiguousActionRoute).WithArguments("Person").WithLocation(1)
        };

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(source, expectedDiagnostics);
    }

    [Fact]
    public async Task DuplicateRoutes_RouteAndGetVsGet_HasDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
public class MyController : Controller
{
    [HttpGet({|#0:""Person""|})]
    public IActionResult Get() => Ok(""You GET me"");
    
    [HttpGet]
    [Route({|#1:""Person""|})]
    public IActionResult Put() => Ok(""You PUT me"");
}
internal class Program
{
    static void Main(string[] args)
    {
    }
}
";

        var expectedDiagnostics = new[] {
            new DiagnosticResult(DiagnosticDescriptors.AmbiguousActionRoute).WithArguments("Person").WithLocation(0),
            new DiagnosticResult(DiagnosticDescriptors.AmbiguousActionRoute).WithArguments("Person").WithLocation(1)
        };

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(source, expectedDiagnostics);
    }
}

