// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.Testing;
using VerifyCS = Microsoft.AspNetCore.Analyzers.Verifiers.CSharpCodeFixVerifier<
    Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.RoutePatternAnalyzer,
    Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Fixers.RouteParameterUnusedParameterFixer>;

namespace Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage;

public class RouteParameterUnusedParameterFixerTest
{
    [Fact]
    public async Task Controller_UnusedParameter_AddToAction()
    {
        // Arrange
        var source = @"
using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;

class Program
{
    static void Main()
    {
    }
}

public class TestController
{
    [HttpGet(""{|#0:{id}|}"")]
    public object TestAction()
    {
        return null;
    }
}";

        var fixedSource = @"
using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;

class Program
{
    static void Main()
    {
    }
}

public class TestController
{
    [HttpGet(""{id}"")]
    public object TestAction(string id)
    {
        return null;
    }
}";

        var expectedDiagnostics = new DiagnosticResult(DiagnosticDescriptors.RoutePatternUnusedParameter).WithArguments("id").WithLocation(0);

        // Act & Assert
        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnostics, fixedSource);
    }

    [Fact]
    public async Task Controller_UnusedParameter_HasCancellationToken_AddToActionBeforeToken()
    {
        // Arrange
        var source = @"
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;

class Program
{
    static void Main()
    {
    }
}

public class TestController
{
    [HttpGet(""{|#0:{id}|}"")]
    public object TestAction(CancellationToken cancellationToken)
    {
        return null;
    }
}";

        var fixedSource = @"
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;

class Program
{
    static void Main()
    {
    }
}

public class TestController
{
    [HttpGet(""{id}"")]
    public object TestAction(string id, CancellationToken cancellationToken)
    {
        return null;
    }
}";

        var expectedDiagnostics = new DiagnosticResult(DiagnosticDescriptors.RoutePatternUnusedParameter).WithArguments("id").WithLocation(0);

        // Act & Assert
        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnostics, fixedSource);
    }

    [Fact]
    public async Task Controller_UnusedParameter_BeforeExistingParameter_AddToActionBeforeExisting()
    {
        // Arrange
        var source = @"
using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;

class Program
{
    static void Main()
    {
    }
}

public class TestController
{
    [HttpGet(""{|#0:{id}|}/books/{bookId}"")]
    public object TestAction(string bookId)
    {
        return null;
    }
}";

        var fixedSource = @"
using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;

class Program
{
    static void Main()
    {
    }
}

public class TestController
{
    [HttpGet(""{id}/books/{bookId}"")]
    public object TestAction(string id, string bookId)
    {
        return null;
    }
}";

        var expectedDiagnostics = new DiagnosticResult(DiagnosticDescriptors.RoutePatternUnusedParameter).WithArguments("id").WithLocation(0);

        // Act & Assert
        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnostics, fixedSource);
    }

    [Fact]
    public async Task Controller_MultipleUnusedParameters_AddToAction()
    {
        // Arrange
        var source = @"
using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;

class Program
{
    static void Main()
    {
    }
}

public class TestController
{
    [HttpGet(""{|#0:{id}|}/books/{|#1:{bookId}|}"")]
    public object TestAction()
    {
        return null;
    }
}";

        var fixedSource = @"
using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;

class Program
{
    static void Main()
    {
    }
}

public class TestController
{
    [HttpGet(""{id}/books/{bookId}"")]
    public object TestAction(string id, string bookId)
    {
        return null;
    }
}";

        var expectedDiagnostics = new[]
        {
            new DiagnosticResult(DiagnosticDescriptors.RoutePatternUnusedParameter).WithArguments("id").WithLocation(0),
            new DiagnosticResult(DiagnosticDescriptors.RoutePatternUnusedParameter).WithArguments("bookId").WithLocation(1)
        };

        // Act & Assert
        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnostics, fixedSource, expectedIterations: 2);
    }

    [Fact]
    public async Task Controller_MultipleUnusedParameters_WithConstraints_AddToAction()
    {
        // Arrange
        var source = @"
using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;

class Program
{
    static void Main()
    {
    }
}

public class TestController
{
    [HttpGet(""{|#0:{id:int}|}/books/{|#1:{bookId:guid}|}"")]
    public object TestAction()
    {
        return null;
    }
}";

        var fixedSource = @"
using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;

class Program
{
    static void Main()
    {
    }
}

public class TestController
{
    [HttpGet(""{id:int}/books/{bookId:guid}"")]
    public object TestAction(int id, System.Guid bookId)
    {
        return null;
    }
}";

        var expectedDiagnostics = new[]
        {
            new DiagnosticResult(DiagnosticDescriptors.RoutePatternUnusedParameter).WithArguments("id").WithLocation(0),
            new DiagnosticResult(DiagnosticDescriptors.RoutePatternUnusedParameter).WithArguments("bookId").WithLocation(1)
        };

        // Act & Assert
        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnostics, fixedSource, expectedIterations: 2);
    }

    [Fact]
    public async Task Controller_DuplicateUnusedParameters_AddToAction()
    {
        // Arrange
        var source = @"
using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;

class Program
{
    static void Main()
    {
    }
}

public class TestController
{
    [HttpGet(""{|#0:{id}|}/books/{|#1:{id}|}"")]
    public object TestAction()
    {
        return null;
    }
}";

        var fixedSource = @"
using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;

class Program
{
    static void Main()
    {
    }
}

public class TestController
{
    [HttpGet(""{id}/books/{|#1:{id}|}"")]
    public object TestAction(string id)
    {
        return null;
    }
}";

        var expectedDiagnostics = new[]
        {
            new DiagnosticResult(DiagnosticDescriptors.RoutePatternUnusedParameter).WithArguments("id").WithLocation(0),
            new DiagnosticResult(DiagnosticDescriptors.RoutePatternIssue).WithArguments("The route parameter name 'id' appears more than one time in the route template.").WithLocation(1),
        };

        // Act & Assert
        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnostics, fixedSource, expectedIterations: 1);
    }

    [Fact]
    public async Task MapGet_UnusedParameter_AddToLambda()
    {
        // Arrange
        var source = @"
using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;

class Program
{
    static void Main()
    {
        EndpointRouteBuilderExtensions.MapGet(null, @""{|#0:{id}|}"", () => ""test"");
    }
}
";

        var fixedSource = @"
using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;

class Program
{
    static void Main()
    {
        EndpointRouteBuilderExtensions.MapGet(null, @""{id}"", (string id) => ""test"");
    }
}
";

        var expectedDiagnostics = new[]
        {
            new DiagnosticResult(DiagnosticDescriptors.RoutePatternUnusedParameter).WithArguments("id").WithLocation(0)
        };

        // Act & Assert
        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnostics, fixedSource, expectedIterations: 1);
    }

    [Fact]
    public async Task MapGet_UnusedParameter_AddToRequestDelegateLambda()
    {
        // Arrange
        var source = @"
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

class Program
{
    static void Main()
    {
        EndpointRouteBuilderExtensions.MapGet(null, @""{|#0:{id}|}"", (HttpContext context) => Task.CompletedTask);
    }
}
";

        var fixedSource = @"
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

class Program
{
    static void Main()
    {
        EndpointRouteBuilderExtensions.MapGet(null, @""{id}"", (string id, HttpContext context) => Task.CompletedTask);
    }
}
";

        var expectedDiagnostics = new[]
        {
            new DiagnosticResult(DiagnosticDescriptors.RoutePatternUnusedParameter).WithArguments("id").WithLocation(0)
        };

        // Act & Assert
        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnostics, fixedSource, expectedIterations: 1);
    }

    [Fact]
    public async Task MapGet_UnusedParameter_IntPolicy_AddIntToLambda()
    {
        // Arrange
        var source = @"
using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;

class Program
{
    static void Main()
    {
        EndpointRouteBuilderExtensions.MapGet(null, @""{|#0:{id:int}|}"", () => ""test"");
    }
}
";

        var fixedSource = @"
using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;

class Program
{
    static void Main()
    {
        EndpointRouteBuilderExtensions.MapGet(null, @""{id:int}"", (int id) => ""test"");
    }
}
";

        var expectedDiagnostics = new[]
        {
            new DiagnosticResult(DiagnosticDescriptors.RoutePatternUnusedParameter).WithArguments("id").WithLocation(0)
        };

        // Act & Assert
        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnostics, fixedSource, expectedIterations: 1);
    }

    [Fact]
    public async Task MapGet_UnusedParameter_IntPolicy_IsOptional_AddNullableIntToLambda()
    {
        // Arrange
        var source = @"
using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;

class Program
{
    static void Main()
    {
        EndpointRouteBuilderExtensions.MapGet(null, @""{|#0:{id:int?}|}"", () => ""test"");
    }
}
";

        var fixedSource = @"
using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;

class Program
{
    static void Main()
    {
        EndpointRouteBuilderExtensions.MapGet(null, @""{id:int?}"", (int? id) => ""test"");
    }
}
";

        var expectedDiagnostics = new[]
        {
            new DiagnosticResult(DiagnosticDescriptors.RoutePatternUnusedParameter).WithArguments("id").WithLocation(0)
        };

        // Act & Assert
        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnostics, fixedSource, expectedIterations: 1);
    }

    [Fact]
    public async Task MapGet_UnusedParameter_IsOptional_AddNullableStringToLambda()
    {
        // Arrange
        var source = @"
using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;

class Program
{
    static void Main()
    {
        EndpointRouteBuilderExtensions.MapGet(null, @""{|#0:{id?}|}"", () => ""test"");
    }
}
";

        var fixedSource = @"
using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;

class Program
{
    static void Main()
    {
        EndpointRouteBuilderExtensions.MapGet(null, @""{id?}"", (string? id) => ""test"");
    }
}
";

        var expectedDiagnostics = new[]
        {
            new DiagnosticResult(DiagnosticDescriptors.RoutePatternUnusedParameter).WithArguments("id").WithLocation(0)
        };

        // Act & Assert
        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnostics, fixedSource, expectedIterations: 1);
    }

    [Fact]
    public async Task MapGet_UnusedParameter_IntAndDecimalPolicy_AddStringToLambda()
    {
        // Arrange
        var source = @"
using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;

class Program
{
    static void Main()
    {
        EndpointRouteBuilderExtensions.MapGet(null, @""{|#0:{id:int:decimal}|}"", () => ""test"");
    }
}
";

        var fixedSource = @"
using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;

class Program
{
    static void Main()
    {
        EndpointRouteBuilderExtensions.MapGet(null, @""{id:int:decimal}"", (string id) => ""test"");
    }
}
";

        var expectedDiagnostics = new[]
        {
            new DiagnosticResult(DiagnosticDescriptors.RoutePatternUnusedParameter).WithArguments("id").WithLocation(0)
        };

        // Act & Assert
        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnostics, fixedSource, expectedIterations: 1);
    }

    [Fact]
    public async Task MapGet_UnusedParameter_IntAndMinPolicy_AddStringToLambda()
    {
        // Arrange
        var source = @"
using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;

class Program
{
    static void Main()
    {
        EndpointRouteBuilderExtensions.MapGet(null, @""{|#0:{id:int:min(10)}|}"", () => ""test"");
    }
}
";

        var fixedSource = @"
using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;

class Program
{
    static void Main()
    {
        EndpointRouteBuilderExtensions.MapGet(null, @""{id:int:min(10)}"", (int id) => ""test"");
    }
}
";

        var expectedDiagnostics = new[]
        {
            new DiagnosticResult(DiagnosticDescriptors.RoutePatternUnusedParameter).WithArguments("id").WithLocation(0)
        };

        // Act & Assert
        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnostics, fixedSource, expectedIterations: 1);
    }

    [Fact]
    public async Task MapGet_UnusedParameter_BeforeExistingParameter_AddToLambdaBefore()
    {
        // Arrange
        var source = @"
using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;

class Program
{
    static void Main()
    {
        EndpointRouteBuilderExtensions.MapGet(null, @""{|#0:{id}|}/book/{bookId}"", (string bookId) => ""test"");
    }
}
";

        var fixedSource = @"
using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;

class Program
{
    static void Main()
    {
        EndpointRouteBuilderExtensions.MapGet(null, @""{id}/book/{bookId}"", (string id, string bookId) => ""test"");
    }
}
";

        var expectedDiagnostics = new[]
        {
            new DiagnosticResult(DiagnosticDescriptors.RoutePatternUnusedParameter).WithArguments("id").WithLocation(0)
        };

        // Act & Assert
        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnostics, fixedSource, expectedIterations: 1);
    }

    [Fact]
    public async Task MapGet_UnusedParameter_HasCancellationToken_AddToLambdaBeforeToken()
    {
        // Arrange
        var source = @"
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.AspNetCore.Builder;

class Program
{
    static void Main()
    {
        EndpointRouteBuilderExtensions.MapGet(null, @""{id}/book/{|#0:{bookId}|}"", (string id, CancellationToken cancellationToken) => ""test"");
    }
}
";

        var fixedSource = @"
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.AspNetCore.Builder;

class Program
{
    static void Main()
    {
        EndpointRouteBuilderExtensions.MapGet(null, @""{id}/book/{bookId}"", (string id, string bookId, CancellationToken cancellationToken) => ""test"");
    }
}
";

        var expectedDiagnostics = new[]
        {
            new DiagnosticResult(DiagnosticDescriptors.RoutePatternUnusedParameter).WithArguments("bookId").WithLocation(0)
        };

        // Act & Assert
        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnostics, fixedSource, expectedIterations: 1);
    }

    [Fact]
    public async Task MapGet_UnusedParameter_Multiple_HasCancellationToken_AddToLambdaBeforeToken()
    {
        // Arrange
        var source = @"
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.AspNetCore.Builder;

class Program
{
    static void Main()
    {
        EndpointRouteBuilderExtensions.MapGet(null, @""/{|#0:{id}|}/{|#1:{id2}|}/book/{|#2:{bookId}|}/{|#3:{after}|}"", (CancellationToken cancellationToken) => ""test"");
    }
}
";

        var fixedSource = @"
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.AspNetCore.Builder;

class Program
{
    static void Main()
    {
        EndpointRouteBuilderExtensions.MapGet(null, @""/{id}/{id2}/book/{bookId}/{after}"", (string id, string id2, string bookId, string after, CancellationToken cancellationToken) => ""test"");
    }
}
";

        var expectedDiagnostics = new[]
        {
            new DiagnosticResult(DiagnosticDescriptors.RoutePatternUnusedParameter).WithArguments("id").WithLocation(0),
            new DiagnosticResult(DiagnosticDescriptors.RoutePatternUnusedParameter).WithArguments("id2").WithLocation(1),
            new DiagnosticResult(DiagnosticDescriptors.RoutePatternUnusedParameter).WithArguments("bookId").WithLocation(2),
            new DiagnosticResult(DiagnosticDescriptors.RoutePatternUnusedParameter).WithArguments("after").WithLocation(3),
        };

        // Act & Assert
        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnostics, fixedSource, expectedIterations: 4);
    }

    [Fact]
    public async Task MapGet_UnusedParameter_AsParameters_HasCancellationToken_AddToLambdaBeforeToken()
    {
        // Arrange
        var source = @"
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

class Program
{
    static void Main()
    {
        EndpointRouteBuilderExtensions.MapGet(null, @""{PageNumber}/{PageIndex}/{|#0:{id}|}"", ([AsParameters] PageData pageData, CancellationToken cancellationToken) => """");
    }

    int OtherMethod(PageData pageData)
    {
        return pageData.PageIndex;
    }
}

public class PageData
{
    public int PageNumber { get; set; }
    public int PageIndex { get; set; }
}
";

        var fixedSource = @"
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

class Program
{
    static void Main()
    {
        EndpointRouteBuilderExtensions.MapGet(null, @""{PageNumber}/{PageIndex}/{id}"", ([AsParameters] PageData pageData, string id, CancellationToken cancellationToken) => """");
    }

    int OtherMethod(PageData pageData)
    {
        return pageData.PageIndex;
    }
}

public class PageData
{
    public int PageNumber { get; set; }
    public int PageIndex { get; set; }
}
";

        var expectedDiagnostics = new[]
        {
            new DiagnosticResult(DiagnosticDescriptors.RoutePatternUnusedParameter).WithArguments("id").WithLocation(0),
        };

        // Act & Assert
        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnostics, fixedSource, expectedIterations: 1);
    }
}
