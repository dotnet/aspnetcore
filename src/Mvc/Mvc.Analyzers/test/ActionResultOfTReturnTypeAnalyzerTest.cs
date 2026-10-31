// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.AspNetCore.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Microsoft.AspNetCore.Mvc.Analyzers;

public class ActionResultOfTReturnTypeAnalyzerTest
{
    private static readonly DiagnosticResult Diagnostic = new(DiagnosticDescriptors.MVC1007_ActionResultOfTTypeMismatch);

    private const string Usings = @"using Microsoft.AspNetCore.Mvc;
";

    [Fact]
    public Task DiagnosticIsReturned_WhenOkReturnsWrongType()
    {
        var source = Usings + @"
namespace TestApp
{
    public class Customer
    {
        public string Name { get; set; }
    }

    public class TestController : Controller
    {
        [HttpGet]
        public ActionResult<Customer> GetCustomer()
        {
            return {|#0:Ok(42)|};
        }
    }
}";
        var expected = Diagnostic.WithLocation(0)
            .WithArguments("int", "Customer");

        return VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public Task DiagnosticIsReturned_WhenOkReturnsWrongReferenceType()
    {
        var source = Usings + @"
namespace TestApp
{
    public class Customer
    {
        public string Name { get; set; }
    }

    public class Order
    {
        public int Id { get; set; }
    }

    public class TestController : Controller
    {
        [HttpGet]
        public ActionResult<Customer> GetCustomer()
        {
            return {|#0:Ok(new Order())|};
        }
    }
}";
        var expected = Diagnostic.WithLocation(0)
            .WithArguments("Order", "Customer");

        return VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public Task DiagnosticIsReturned_WhenCreatedReturnsWrongType()
    {
        var source = Usings + @"
namespace TestApp
{
    public class Customer
    {
        public string Name { get; set; }
    }

    public class TestController : Controller
    {
        [HttpPost]
        public ActionResult<Customer> Create()
        {
            return {|#0:Created(""https://example.com"", 42)|};
        }
    }
}";
        var expected = Diagnostic.WithLocation(0)
            .WithArguments("int", "Customer");

        return VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public Task DiagnosticIsReturned_WhenAcceptedReturnsWrongType()
    {
        var source = Usings + @"
namespace TestApp
{
    public class Customer
    {
        public string Name { get; set; }
    }

    public class TestController : Controller
    {
        [HttpPost]
        public ActionResult<Customer> Create()
        {
            return {|#0:Accepted(new object())|};
        }
    }
}";
        var expected = Diagnostic.WithLocation(0)
            .WithArguments("object", "Customer");

        return VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public Task NoDiagnostic_WhenOkReturnsCorrectType()
    {
        var source = Usings + @"
namespace TestApp
{
    public class Customer
    {
        public string Name { get; set; }
    }

    public class TestController : Controller
    {
        [HttpGet]
        public ActionResult<Customer> GetCustomer()
        {
            return Ok(new Customer());
        }
    }
}";
        return VerifyAnalyzerAsync(source);
    }

    [Fact]
    public Task NoDiagnostic_WhenOkReturnsDerivedType()
    {
        var source = Usings + @"
namespace TestApp
{
    public class Animal { }
    public class Dog : Animal { }

    public class TestController : Controller
    {
        [HttpGet]
        public ActionResult<Animal> GetAnimal()
        {
            return Ok(new Dog());
        }
    }
}";
        return VerifyAnalyzerAsync(source);
    }

    [Fact]
    public Task NoDiagnostic_WhenReturningNon2xxResult()
    {
        var source = Usings + @"
namespace TestApp
{
    public class Customer
    {
        public string Name { get; set; }
    }

    public class TestController : Controller
    {
        [HttpGet]
        public ActionResult<Customer> GetCustomer()
        {
            return NotFound();
        }
    }
}";
        return VerifyAnalyzerAsync(source);
    }

    [Fact]
    public Task NoDiagnostic_WhenReturningBadRequest()
    {
        var source = Usings + @"
namespace TestApp
{
    public class Customer
    {
        public string Name { get; set; }
    }

    public class TestController : Controller
    {
        [HttpGet]
        public ActionResult<Customer> GetCustomer()
        {
            return BadRequest(""error"");
        }
    }
}";
        return VerifyAnalyzerAsync(source);
    }

    [Fact]
    public Task NoDiagnostic_WhenMethodReturnsIActionResult()
    {
        var source = Usings + @"
namespace TestApp
{
    public class TestController : Controller
    {
        [HttpGet]
        public IActionResult GetCustomer()
        {
            return Ok(42);
        }
    }
}";
        return VerifyAnalyzerAsync(source);
    }

    [Fact]
    public Task NoDiagnostic_WhenOkReturnsNull()
    {
        var source = Usings + @"
namespace TestApp
{
    public class Customer
    {
        public string Name { get; set; }
    }

    public class TestController : Controller
    {
        [HttpGet]
        public ActionResult<Customer> GetCustomer()
        {
            return Ok(null);
        }
    }
}";
        return VerifyAnalyzerAsync(source);
    }

    [Fact]
    public Task NoDiagnostic_WhenOkCalledWithNoArgument()
    {
        var source = Usings + @"
namespace TestApp
{
    public class Customer
    {
        public string Name { get; set; }
    }

    public class TestController : Controller
    {
        [HttpGet]
        public ActionResult<Customer> GetCustomer()
        {
            return Ok();
        }
    }
}";
        return VerifyAnalyzerAsync(source);
    }

    [Fact]
    public Task DiagnosticIsReturned_ForAsyncMethod()
    {
        var source = @"using System.Threading.Tasks;
" + Usings + @"
namespace TestApp
{
    public class Customer
    {
        public string Name { get; set; }
    }

    public class TestController : Controller
    {
        [HttpGet]
        public async Task<ActionResult<Customer>> GetCustomer()
        {
            await Task.CompletedTask;
            return {|#0:Ok(42)|};
        }
    }
}";
        var expected = Diagnostic.WithLocation(0)
            .WithArguments("int", "Customer");

        return VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public Task NoDiagnostic_WhenOkReturnsImplementedInterface()
    {
        var source = @"using System.Collections.Generic;
" + Usings + @"
namespace TestApp
{
    public class TestController : Controller
    {
        [HttpGet]
        public ActionResult<IEnumerable<string>> GetItems()
        {
            return Ok(new List<string>());
        }
    }
}";
        return VerifyAnalyzerAsync(source);
    }

    private static Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
    {
        var test = new ActionResultOfTCSharpAnalyzerTest(TestReferences.MetadataReferences)
        {
            TestCode = source,
            ReferenceAssemblies = TestReferences.EmptyReferenceAssemblies,
        };

        test.ExpectedDiagnostics.AddRange(expected);
        return test.RunAsync();
    }

    internal sealed class ActionResultOfTCSharpAnalyzerTest : CSharpAnalyzerTest<ActionResultOfTReturnTypeAnalyzer, DefaultVerifier>
    {
        public ActionResultOfTCSharpAnalyzerTest(ImmutableArray<MetadataReference> metadataReferences)
        {
            TestState.AdditionalReferences.AddRange(metadataReferences);
        }

        protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers() => new[] { new ActionResultOfTReturnTypeAnalyzer() };
    }
}
