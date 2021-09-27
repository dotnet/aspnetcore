// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.Testing;
using VerifyCS = Microsoft.AspNetCore.Analyzers.RouteHandlers.CSharpRouteHandlerCodeFixVerifier<
    Microsoft.AspNetCore.Analyzers.RouteHandlers.RouteHandlerAnalyzer,
    Microsoft.AspNetCore.Analyzers.RouteHandlers.Fixers.DetectMismatchedParameterOptionalityFixer>;

namespace Microsoft.AspNetCore.Analyzers.RouteHandlers;

public class DetectInvalidCustomBindingFormat
{
    [Fact]
    public async Task CustomBindingTryParse_ShouldNotReportDiagnostic_ValidFormatOne()
    {
        var source = @"
#nullable enable
using System;
using Microsoft.AspNetCore.Builder;

var app = WebApplication.Create();
app.MapGet(""/map"", (Point point) => $""Point: { point.X}, { point.Y}"");

public class Point
{
    public double X { get; set; }
    public double Y { get; set; }

    public static bool TryParse(string? value, IFormatProvider? provider, out Point? point)
    {
        point = null;
        return false;
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source, Array.Empty<DiagnosticResult>());
    }

    [Fact]
    public async Task CustomBindingTryParse_ShouldNotReportDiagnostic_ValidFormatTwo()
    {
        var source = @"
#nullable enable
using System;
using Microsoft.AspNetCore.Builder;

var app = WebApplication.Create();
app.MapGet(""/map"", (Point point) => $""Point: { point.X}, { point.Y}"");

public class Point
{
    public double X { get; set; }
    public double Y { get; set; }

    public static bool TryParse(string? value, IFormatProvider? provider, out Point? point)
    {
        point = null;
        return false;
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source, Array.Empty<DiagnosticResult>());
    }

    [Fact]
    public async Task CustomBindingTryParse_MustBeOfAValidFormat()
    {
        var source = @"
#nullable enable
using System;
using Microsoft.AspNetCore.Builder;

var app = WebApplication.Create();
app.MapGet(""/map"", (Point {|#0:point|}) => $""Point: { point.X}, { point.Y}"");

public class Point
{
    public double X { get; set; }
    public double Y { get; set; }

    public bool TryParse()
    {
        return false;
    }
}";

        var expectedDiagnostics = new[] {
            new DiagnosticResult(DiagnosticDescriptors.CustomBindingTryParseMustHaveAValidFormat).WithArguments("Point").WithLocation(0)
        };

        await VerifyCS.VerifyAnalyzerAsync(source, expectedDiagnostics);
    }

    [Fact]
    public async Task CustomBindingTryParse_Inherit_MustBeOfAValidFormat()
    {
        var source = @"
#nullable enable
using System;
using Microsoft.AspNetCore.Builder;

var app = WebApplication.Create();
app.MapGet(""/map"", (Point {|#0:point|}) => $""Point: { point.X}, { point.Y}"");

public class BasePoint
{
    public static bool TryParse()
    {
        return false;
    }
}

public class Point : BasePoint
{
    public double X { get; set; }
    public double Y { get; set; }
}";

        var expectedDiagnostics = new[] {
            new DiagnosticResult(DiagnosticDescriptors.CustomBindingTryParseMustHaveAValidFormat).WithArguments("Point").WithLocation(0)
        };

        await VerifyCS.VerifyAnalyzerAsync(source, expectedDiagnostics);
    }


    [Fact]
    public async Task CustomBindingTryParse_MustBeStatic()
    {
        var source = @"
#nullable enable
using System;
using Microsoft.AspNetCore.Builder;

var app = WebApplication.Create();
app.MapGet(""/map"", (Point {|#0:point|}) => $""Point: { point.X}, { point.Y}"");

public class Point
{
    public double X { get; set; }
    public double Y { get; set; }

    public bool {|#1:TryParse|}(string? value, IFormatProvider? provider, out Point? point)
    {
        point = null;
        return false;
    }
}";

        var expectedDiagnostics = new[] {
            new DiagnosticResult(DiagnosticDescriptors.CustomBindingTryParseMustHaveAValidFormat).WithArguments("Point").WithLocation(0),
            new DiagnosticResult(DiagnosticDescriptors.CustomBindingMethodMustBeStatic).WithArguments("TryParse").WithLocation(1)
        };

        await VerifyCS.VerifyAnalyzerAsync(source, expectedDiagnostics);
    }

    [Fact]
    public async Task CustomBindingTryParse_MustBePubli()
    {
        var source = @"
#nullable enable
using System;
using Microsoft.AspNetCore.Builder;

var app = WebApplication.Create();
app.MapGet(""/map"", (Point {|#0:point|}) => $""Point: { point.X}, { point.Y}"");

public class Point
{
    public double X { get; set; }
    public double Y { get; set; }

    private static bool {|#1:TryParse|}(string? value, IFormatProvider? provider, out Point? point)
    {
        point = null;
        return false;
    }
}";

        var expectedDiagnostics = new[] {
            new DiagnosticResult(DiagnosticDescriptors.CustomBindingTryParseMustHaveAValidFormat).WithArguments("Point").WithLocation(0),
            new DiagnosticResult(DiagnosticDescriptors.CustomBindingMethodMustBePublic).WithArguments("TryParse").WithLocation(1)
        };

        await VerifyCS.VerifyAnalyzerAsync(source, expectedDiagnostics);
    }

    [Fact]
    public async Task CustomBindingBindAsync_ShouldNotReportDiagnostic_ValidFormatOne()
    {
        var source = @"
#nullable enable
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Reflection;
using System.Threading.Tasks;

var app = WebApplication.Create();
app.MapGet(""/map"", (Point point) => $""Point: { point.X}, { point.Y}"");

public class Point
{
    public double X { get; set; }
    public double Y { get; set; }

    public static ValueTask<Point> BindAsync(HttpContext context, ParameterInfo parameter)
    {
        return ValueTask.FromResult(new Point());
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source, Array.Empty<DiagnosticResult>());
    }

    [Fact]
    public async Task CustomBindingBindAsync_ShouldNotReportDiagnostic_ValidFormatTwo()
    {
        var source = @"
#nullable enable
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Reflection;
using System.Threading.Tasks;

var app = WebApplication.Create();
app.MapGet(""/map"", (Point point) => $""Point: { point.X}, { point.Y}"");

public class Point
{
    public double X { get; set; }
    public double Y { get; set; }

    public static ValueTask<Point> BindAsync(HttpContext context)
    {
        return ValueTask.FromResult(new Point());
    }
}";

        await VerifyCS.VerifyAnalyzerAsync(source, Array.Empty<DiagnosticResult>());
    }

    [Fact]
    public async Task CustomBindingBindAsync_MustBeOfAValidFormat()
    {
        var source = @"
#nullable enable
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Reflection;
using System.Threading.Tasks;

var app = WebApplication.Create();
app.MapGet(""/map"", (Point {|#0:point|}) => $""Point: { point.X}, { point.Y}"");

public class Point
{
    public double X { get; set; }
    public double Y { get; set; }

    public static ValueTask<Point> BindAsync()
    {
        return ValueTask.FromResult(new Point());
    }
}";

        var expectedDiagnostics = new[] {
            new DiagnosticResult(DiagnosticDescriptors.CustomBindingBindAsyncMustHaveAValidFormat).WithArguments("Point").WithLocation(0)
        };

        await VerifyCS.VerifyAnalyzerAsync(source, expectedDiagnostics);
    }

    [Fact]
    public async Task CustomBindingBindAsync_Inherit_MustBeOfAValidFormat()
    {
        var source = @"
#nullable enable
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Reflection;
using System.Threading.Tasks;

var app = WebApplication.Create();
app.MapGet(""/map"", (Point {|#0:point|}) => $""Point: { point.X}, { point.Y}"");

public class BasePoint
{
    public static ValueTask<Point> BindAsync()
    {
        return ValueTask.FromResult(new Point());
    }
}

public class Point : BasePoint
{
    public double X { get; set; }
    public double Y { get; set; }
}";

        var expectedDiagnostics = new[] {
            new DiagnosticResult(DiagnosticDescriptors.CustomBindingBindAsyncMustHaveAValidFormat).WithArguments("Point").WithLocation(0)
        };

        await VerifyCS.VerifyAnalyzerAsync(source, expectedDiagnostics);
    }


    [Fact]
    public async Task CustomBindingBindAsync_MustBeStatic()
    {
        var source = @"
#nullable enable
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Reflection;
using System.Threading.Tasks;

var app = WebApplication.Create();
app.MapGet(""/map"", (Point {|#0:point|}) => $""Point: { point.X}, { point.Y}"");

public class Point
{
    public double X { get; set; }
    public double Y { get; set; }

    public ValueTask<Point> {|#1:BindAsync|}(HttpContext context)
    {
        return ValueTask.FromResult(new Point());
    }
}";

        var expectedDiagnostics = new[] {
            new DiagnosticResult(DiagnosticDescriptors.CustomBindingBindAsyncMustHaveAValidFormat).WithArguments("Point").WithLocation(0),
            new DiagnosticResult(DiagnosticDescriptors.CustomBindingMethodMustBeStatic).WithArguments("BindAsync").WithLocation(1)
        };

        await VerifyCS.VerifyAnalyzerAsync(source, expectedDiagnostics);
    }

    [Fact]
    public async Task CustomBindingBindAsync_MustBePubli()
    {
        var source = @"
#nullable enable
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Reflection;
using System.Threading.Tasks;

var app = WebApplication.Create();
app.MapGet(""/map"", (Point {|#0:point|}) => $""Point: { point.X}, { point.Y}"");

public class Point
{
    public double X { get; set; }
    public double Y { get; set; }

    private static ValueTask<Point> {|#1:BindAsync|}(HttpContext context)
    {
        return ValueTask.FromResult(new Point());
    }
}";

        var expectedDiagnostics = new[] {
            new DiagnosticResult(DiagnosticDescriptors.CustomBindingBindAsyncMustHaveAValidFormat).WithArguments("Point").WithLocation(0),
            new DiagnosticResult(DiagnosticDescriptors.CustomBindingMethodMustBePublic).WithArguments("BindAsync").WithLocation(1)
        };

        await VerifyCS.VerifyAnalyzerAsync(source, expectedDiagnostics);
    }
}
