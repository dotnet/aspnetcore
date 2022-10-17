// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.Testing;
using VerifyCS = Microsoft.AspNetCore.Analyzers.Verifiers.CSharpCodeFixVerifier<
    Microsoft.AspNetCore.Analyzers.Http.HeaderDictionaryAddAnalyzer,
    Microsoft.AspNetCore.Analyzers.Http.Fixers.HeaderDictionaryAddFixer>;

namespace Microsoft.AspNetCore.Analyzers.Http;

public class HeaderDictionaryAddTest
{
    private const string AppendCodeActionEquivalenceKey = "Use 'IHeaderDictionary.Append'";
    private const string IndexerCodeActionEquivalenceKey = "Use indexer";

    [Fact]
    public async Task IHeaderDictionary_WithAdd_FixedWithAppend()
    {
        // Arrange & Act & Assert
        await VerifyCS.VerifyCodeFixAsync(@"
using Microsoft.AspNetCore.Http;
namespace HeaderDictionaryAddFixerTests;
public class Program
{
    public static void Main()
    {
        var context = new DefaultHttpContext();
        {|#0:context.Request.Headers.Add(""Accept"", ""text/html"")|};
    }
}",
        new[]
        {
            new DiagnosticResult(DiagnosticDescriptors.DoNotUseIHeaderDictionaryAdd)
                .WithLocation(0)
                .WithMessage(Resources.Analyzer_HeaderDictionaryAdd_Message)
        },
        @"
using Microsoft.AspNetCore.Http;
namespace HeaderDictionaryAddFixerTests;
public class Program
{
    public static void Main()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Append(""Accept"", ""text/html"");
    }
}",
        codeActionEquivalenceKey: AppendCodeActionEquivalenceKey);
    }

    public static IEnumerable<object[]> FixedWithAppendAddsUsingDirectiveTestData()
    {
        yield return new[]
        {
            @"
using Microsoft.AspNetCore.Mvc;

namespace HeaderDictionaryAddFixerTests;
public class TestController : ControllerBase
{
    public IActionResult Endpoint()
    {
        {|#0:Response.Headers.Add(""Content-Type"", ""text/html"")|};
        return Ok();
    }
}

public class Program
{
    public static void Main()
    {
    }
}",
            @"
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HeaderDictionaryAddFixerTests;
public class TestController : ControllerBase
{
    public IActionResult Endpoint()
    {
        Response.Headers.Append(""Content-Type"", ""text/html"");
        return Ok();
    }
}

public class Program
{
    public static void Main()
    {
    }
}"
        };

        yield return new[]
        {
            @"
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace HeaderDictionaryAddFixerTests;
public class TestController : ControllerBase
{
    public IActionResult Endpoint()
    {
        {|#0:Response.Headers.Add(""Content-Type"", ""text/html"")|};
        return Ok(new List<string>());
    }
}

public class Program
{
    public static void Main()
    {
    }
}",
            @"
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HeaderDictionaryAddFixerTests;
public class TestController : ControllerBase
{
    public IActionResult Endpoint()
    {
        Response.Headers.Append(""Content-Type"", ""text/html"");
        return Ok(new List<string>());
    }
}

public class Program
{
    public static void Main()
    {
    }
}"
        };

        yield return new[]
        {
            @"
namespace HeaderDictionaryAddFixerTests;
public class TestController : Microsoft.AspNetCore.Mvc.ControllerBase
{
    public Microsoft.AspNetCore.Mvc.IActionResult Endpoint()
    {
        {|#0:Response.Headers.Add(""Content-Type"", ""text/html"")|};
        return Ok();
    }
}

public class Program
{
    public static void Main()
    {
    }
}",
            @"
using Microsoft.AspNetCore.Http;

namespace HeaderDictionaryAddFixerTests;
public class TestController : Microsoft.AspNetCore.Mvc.ControllerBase
{
    public Microsoft.AspNetCore.Mvc.IActionResult Endpoint()
    {
        Response.Headers.Append(""Content-Type"", ""text/html"");
        return Ok();
    }
}

public class Program
{
    public static void Main()
    {
    }
}"
        };

        yield return new[]
        {
            @"
using System.Collections.Generic;

namespace HeaderDictionaryAddFixerTests;
public class TestController : Microsoft.AspNetCore.Mvc.ControllerBase
{
    public Microsoft.AspNetCore.Mvc.IActionResult Endpoint()
    {
        {|#0:Response.Headers.Add(""Content-Type"", ""text/html"")|};
        return Ok(new List<string>());
    }
}

public class Program
{
    public static void Main()
    {
    }
}",
            @"
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace HeaderDictionaryAddFixerTests;
public class TestController : Microsoft.AspNetCore.Mvc.ControllerBase
{
    public Microsoft.AspNetCore.Mvc.IActionResult Endpoint()
    {
        Response.Headers.Append(""Content-Type"", ""text/html"");
        return Ok(new List<string>());
    }
}

public class Program
{
    public static void Main()
    {
    }
}"
        };
    }

    [Theory]
    [MemberData(nameof(FixedWithAppendAddsUsingDirectiveTestData))]
    public async Task IHeaderDictionary_WithAdd_FixedWithAppend_AddsUsingDirective(string source, string fixedSource)
    {
        // Arrange & Act & Assert
        await VerifyCS.VerifyCodeFixAsync(
            source.TrimStart(),
            new[]
            {
                new DiagnosticResult(DiagnosticDescriptors.DoNotUseIHeaderDictionaryAdd)
                    .WithLocation(0)
                    .WithMessage(Resources.Analyzer_HeaderDictionaryAdd_Message)
            },
            fixedSource.TrimStart(),
            codeActionEquivalenceKey: AppendCodeActionEquivalenceKey);
    }

    [Fact]
    public async Task IHeaderDictionary_WithAdd_FixedWithIndexer()
    {
        // Arrange & Act & Assert
        await VerifyCS.VerifyCodeFixAsync(@"
using Microsoft.AspNetCore.Http;
namespace HeaderDictionaryAddFixerTests;
public class Program
{
    public static void Main()
    {
        var context = new DefaultHttpContext();
        {|#0:context.Request.Headers.Add(""Accept"", ""text/html"")|};
    }
}",
        new[]
        {
            new DiagnosticResult(DiagnosticDescriptors.DoNotUseIHeaderDictionaryAdd)
                .WithLocation(0)
                .WithMessage(Resources.Analyzer_HeaderDictionaryAdd_Message)
        },
        @"
using Microsoft.AspNetCore.Http;
namespace HeaderDictionaryAddFixerTests;
public class Program
{
    public static void Main()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[""Accept""] = ""text/html"";
    }
}",
        codeActionEquivalenceKey: IndexerCodeActionEquivalenceKey);
    }

    [Fact]
    public async Task IHeaderDictionary_WithAppend_DoesNotProduceDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Http;
namespace HeaderDictionaryAddAnalyzerTests;
public class Program
{
    public static void Main()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Append(""Accept"", ""text/html"");
    }
}";

        // Act & Assert
        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task IHeaderDictionary_WithIndexer_DoesNotProduceDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Http;
namespace HeaderDictionaryAddAnalyzerTests;
public class Program
{
    public static void Main()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[""Accept""] = ""text/html"";
    }
}";

        // Act & Assert
        await VerifyCS.VerifyCodeFixAsync(source, source);
    }
}
