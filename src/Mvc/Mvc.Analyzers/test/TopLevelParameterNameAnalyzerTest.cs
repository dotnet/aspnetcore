// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.AspNetCore.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Microsoft.AspNetCore.Mvc.Analyzers;

public class TopLevelParameterNameAnalyzerTest
{
    private static readonly DiagnosticResult Diagnostic = new(DiagnosticDescriptors.MVC1004_ParameterNameCollidesWithTopLevelProperty);

    [Fact]
    public Task DiagnosticsAreReturned_ForControllerActionsWithParametersThatMatchProperties()
    {
        var source = @"
namespace Microsoft.AspNetCore.Mvc.Analyzers.TopLevelParameterNameAnalyzerTestFiles
{
    public class MyController : Controller
    {
        [HttpPost]
        public IActionResult EditPerson(MyModel {|#0:model|}) => null;
    }

    public class MyModel
    {
        public string Model { get; }
    }
}";
        var result = Diagnostic.WithLocation(0)
            .WithArguments("MyModel", "model");

        return VerifyAnalyzerAsync(source, result);
    }

    [Fact]
    public Task DiagnosticsAreReturned_ForModelBoundParameters()
    {
        var source = @"
namespace Microsoft.AspNetCore.Mvc.Analyzers.TopLevelParameterNameAnalyzerTestFiles
{
    public class DiagnosticsAreReturned_ForModelBoundParameters : Controller
    {
        [HttpPost]
        public IActionResult EditPerson(
            [FromBody] DiagnosticsAreReturned_ForModelBoundParametersModel model,
            [FromQuery] DiagnosticsAreReturned_ForModelBoundParametersModel {|#0:value|}) => null;
    }

    public class DiagnosticsAreReturned_ForModelBoundParametersModel
    {
        public string Model { get; }

        public string Value { get; }
    }
}";
        var result = Diagnostic.WithLocation(0)
            .WithArguments("DiagnosticsAreReturned_ForModelBoundParametersModel", "value");

        return VerifyAnalyzerAsync(source, result);
    }

    [Fact]
    public Task DiagnosticsAreReturned_IfModelNameProviderIsUsedToModifyParameterName()
    {
        var source = @"
namespace Microsoft.AspNetCore.Mvc.Analyzers.TopLevelParameterNameAnalyzerTestFiles
{
    public class DiagnosticsAreReturned_IfModelNameProviderIsUsedToModifyParameterName : Controller
    {
        [HttpPost]
        public IActionResult Edit([ModelBinder(Name = ""model"")] TestModel {|#0:parameter|}) => null;
    }

    public class TestModel
    {
        public string Model { get; }

        public string Value { get; }
    }
}
";
        var result = Diagnostic.WithLocation(0)
            .WithArguments("TestModel", "parameter");

        return VerifyAnalyzerAsync(source, result);
    }

    [Fact]
    public Task NoDiagnosticsAreReturnedForApiControllers()
    {
        var source = @"
namespace Microsoft.AspNetCore.Mvc.Analyzers.TopLevelParameterNameAnalyzerTestFiles
{
    [ApiController]
    public class NoDiagnosticsAreReturnedForApiControllers : Controller
    {
        [HttpPost]
        public IActionResult EditPerson(NoDiagnosticsAreReturnedForApiControllersModel model) => null;
    }

    public class NoDiagnosticsAreReturnedForApiControllersModel
    {
        public string Model { get; }
    }
}";
        return VerifyAnalyzerAsync(source, DiagnosticResult.EmptyDiagnosticResults);
    }

    [Fact]
    public Task NoDiagnosticsAreReturnedIfParameterIsRenamedUsingBindingAttribute()
    {
        var source = @"
namespace Microsoft.AspNetCore.Mvc.Analyzers.TopLevelParameterNameAnalyzerTestFiles
{
    public class NoDiagnosticsAreReturnedIfParameterIsRenamedUsingBindingAttribute : Controller
    {
        [HttpPost]
        public IActionResult EditPerson([FromForm(Name = """")] NoDiagnosticsAreReturnedIfParameterIsRenamedUsingBindingAttributeModel model) => null;
    }

    public class NoDiagnosticsAreReturnedIfParameterIsRenamedUsingBindingAttributeModel
    {
        public string Model { get; }
    }
}";

        return VerifyAnalyzerAsync(source, DiagnosticResult.EmptyDiagnosticResults);
    }

    [Fact]
    public Task NoDiagnosticsAreReturnedForNonActions()
    {
        var source = @"
namespace Microsoft.AspNetCore.Mvc.Analyzers.TopLevelParameterNameAnalyzerTestFiles
{
    public class NoDiagnosticsAreReturnedForNonActions : Controller
    {
        [NonAction]
        public IActionResult EditPerson(NoDiagnosticsAreReturnedForNonActionsModel model) => null;
    }

    public class NoDiagnosticsAreReturnedForNonActionsModel
    {
        public string Model { get; }
    }
}";

        return VerifyAnalyzerAsync(source, DiagnosticResult.EmptyDiagnosticResults);
    }

    [Fact]
    public void IsProblematicParameter_ReturnsTrue_IfParameterNameIsTheSameAsModelProperty()
    {
        var source = @"
namespace Microsoft.AspNetCore.Mvc.Analyzers.TopLevelParameterNameAnalyzerTestFiles
{
    public class TestController
    {
        public string Model { get; set; }

        public void ActionMethod(TestController model) { }
    }
}";

        var result = IsProblematicParameterTest(source);
        Assert.True(result);
    }

    [Fact]
    public void IsProblematicParameter_ReturnsTrue_IfParameterNameWithBinderAttributeIsTheSameNameAsModelProperty()
    {
        var source = @"
namespace Microsoft.AspNetCore.Mvc.Analyzers.TopLevelParameterNameAnalyzerTestFiles
{
    public class TestController
    {
        public string Model { get; set; }

        public void ActionMethod([Bind(Prefix = ""model"")] TestController different) { }
    }
}";

        var result = IsProblematicParameterTest(source);
        Assert.True(result);
    }

    [Fact]
    public void IsProblematicParameter_ReturnsTrue_IfPropertyWithModelBindingAttributeHasSameNameAsParameter()
    {
        var source = @"
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace Microsoft.AspNetCore.Mvc.Analyzers.TopLevelParameterNameAnalyzerTestFiles
{
    public class TestController
    {
        [ModelBinder(typeof(ComplexObjectModelBinder), Name = ""model"")]
        public string Different { get; set; }

        public void ActionMethod(TestController model) { }
    }
}";

        var result = IsProblematicParameterTest(source);
        Assert.True(result);
    }

    [Fact]
    public void IsProblematicParameter_ReturnsTrue_IfModelBinderAttributeIsUsedToRenameParameter()
    {
        var source = @"
namespace Microsoft.AspNetCore.Mvc.Analyzers.TopLevelParameterNameAnalyzerTestFiles
{
    public class TestController
    {
        public string Model { get; set; }

        public void ActionMethod([ModelBinder(Name = ""model"")] TestController different) { }
    }
}";
        var result = IsProblematicParameterTest(source);
        Assert.True(result);
    }

    [Fact]
    public void IsProblematicParameter_ReturnsFalse_IfBindingSourceAttributeIsUsedToRenameProperty()
    {
        var source = @"
namespace Microsoft.AspNetCore.Mvc.Analyzers.TopLevelParameterNameAnalyzerTestFiles
{
    public class TestController
    {
        [FromQuery(Name = ""different"")]
        public string Model { get; set; }

        public void ActionMethod(TestController model) { }
    }
}";
        var result = IsProblematicParameterTest(source);
        Assert.False(result);
    }

    [Fact]
    public void IsProblematicParameter_ReturnsFalse_IfBindingSourceAttributeIsUsedToRenameParameter()
    {
        var source = @"
namespace Microsoft.AspNetCore.Mvc.Analyzers.TopLevelParameterNameAnalyzerTestFiles
{
    public class TestController
    {
        public string Model { get; set; }

        public void ActionMethod([FromRoute(Name = ""id"")] TestController model) { }
    }
}";

        var result = IsProblematicParameterTest(source);
        Assert.False(result);
    }

    [Fact]
    public void IsProblematicParameter_ReturnsFalse_ForFromBodyParameter()
    {
        var source = @"
namespace Microsoft.AspNetCore.Mvc.Analyzers.TopLevelParameterNameAnalyzerTestFiles
{
    public class TestController
    {
        public string Model { get; set; }

        public void ActionMethod([FromBody] IsProblematicParameter_ReturnsFalse_ForFromBodyParameter model) { }
    }
}";

        var result = IsProblematicParameterTest(source);
        Assert.False(result);
    }

    [Fact]
    public void IsProblematicParameter_ReturnsFalse_ForParametersWithCustomModelBinder()
    {
        var source = @"
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace Microsoft.AspNetCore.Mvc.Analyzers.TopLevelParameterNameAnalyzerTestFiles
{
    public class TestController
    {
        public string Model { get; set; }

        public void ActionMethod(
            [ModelBinder(typeof(SimpleTypeModelBinder))] TestController model) { }
    }
}";

        var result = IsProblematicParameterTest(source);
        Assert.False(result);
    }

    // Test for https://github.com/dotnet/aspnetcore/issues/6945
    [Fact]
    public void IsProblematicParameter_ReturnsFalse_ForSimpleTypes()
    {
        var source = @"using System;

namespace Microsoft.AspNetCore.Mvc.Analyzers.TopLevelParameterNameAnalyzerTestFiles
{
    public class TestController
    {
        public void ActionMethod(DateTime date, DateTime? day, Uri absoluteUri, Version majorRevision, DayOfWeek sunday) { }
    }
}";
        var compilation = TestCompilation.Create(source);

        var modelType = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.Analyzers.TopLevelParameterNameAnalyzerTestFiles.TestController");
        var method = (IMethodSymbol)modelType.GetMembers("ActionMethod").First();

        Assert.True(TopLevelParameterNameAnalyzer.SymbolCache.TryCreate(compilation, out var symbolCache));

        Assert.Collection(
            method.Parameters,
            p => Assert.False(TopLevelParameterNameAnalyzer.IsProblematicParameter(symbolCache, p)),
            p => Assert.False(TopLevelParameterNameAnalyzer.IsProblematicParameter(symbolCache, p)),
            p => Assert.False(TopLevelParameterNameAnalyzer.IsProblematicParameter(symbolCache, p)),
            p => Assert.False(TopLevelParameterNameAnalyzer.IsProblematicParameter(symbolCache, p)),
            p => Assert.False(TopLevelParameterNameAnalyzer.IsProblematicParameter(symbolCache, p)));
    }

    [Fact]
    public void IsProblematicParameter_IgnoresStaticProperties()
    {
        var source = @"
namespace Microsoft.AspNetCore.Mvc.Analyzers.TopLevelParameterNameAnalyzerTestFiles
{
    public class TestController
    {
        public static string Model { get; set; }

        public void ActionMethod(TestController model) { }
    }
}";

        var result = IsProblematicParameterTest(source);
        Assert.False(result);
    }

    [Fact]
    public void IsProblematicParameter_IgnoresFields()
    {
        var source = @"
namespace Microsoft.AspNetCore.Mvc.Analyzers.TopLevelParameterNameAnalyzerTestFiles
{
    public class TestController
    {
        public string model;

        public void ActionMethod(TestController model) { }
    }
}
";
        var result = IsProblematicParameterTest(source);
        Assert.False(result);
    }

    [Fact]
    public void IsProblematicParameter_IgnoresMethods()
    {
        var source = @"
namespace Microsoft.AspNetCore.Mvc.Analyzers.TopLevelParameterNameAnalyzerTestFiles
{
    public class TestController
    {
        public string Item() => null;

        public void ActionMethod(TestController item) { }
    }
}
";

        var result = IsProblematicParameterTest(source);
        Assert.False(result);
    }

    [Fact]
    public void IsProblematicParameter_IgnoresNonPublicProperties()
    {
        var source = @"
namespace Microsoft.AspNetCore.Mvc.Analyzers.TopLevelParameterNameAnalyzerTestFiles
{
    public class TestController
    {
        protected string Model { get; set; }

        public void ActionMethod(TestController model) { }
    }
}";
        var result = IsProblematicParameterTest(source);
        Assert.False(result);
    }

    private bool IsProblematicParameterTest(string source)
    {
        var compilation = TestCompilation.Create(source);

        var modelType = compilation.Assembly.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.Analyzers.TopLevelParameterNameAnalyzerTestFiles.TestController");
        var method = (IMethodSymbol)modelType.GetMembers("ActionMethod").First();
        var parameter = method.Parameters[0];

        Assert.True(TopLevelParameterNameAnalyzer.SymbolCache.TryCreate(compilation, out var symbolCache));

        return TopLevelParameterNameAnalyzer.IsProblematicParameter(symbolCache, parameter);
    }

    [Fact]
    public void GetName_ReturnsValueFromFirstAttributeWithValue()
    {
        var source = @"
using Microsoft.AspNetCore.Mvc;
namespace TestApp
{
    public class GetNameTests
    {
        public void Action([ModelBinder(Name = ""testModelName"")] int param) { }
    }
}";

        var compilation = TestCompilation.Create(source);
        Assert.True(TopLevelParameterNameAnalyzer.SymbolCache.TryCreate(compilation, out var symbolCache));

        var type = compilation.GetTypeByMetadataName("TestApp.GetNameTests");
        var method = (IMethodSymbol)type.GetMembers("Action").First();

        var parameter = method.Parameters[0];
        var name = TopLevelParameterNameAnalyzer.GetName(symbolCache, parameter);

        Assert.Equal("testModelName", name);
    }

    [Fact]
    public void GetName_ReturnsName_IfNoAttributesAreSpecified()
    {
        var source = @"
using Microsoft.AspNetCore.Mvc;
namespace TestApp
{
    public class GetNameTests
    {
        public void Action(int param) { }
    }
}";

        var compilation = TestCompilation.Create(source);
        Assert.True(TopLevelParameterNameAnalyzer.SymbolCache.TryCreate(compilation, out var symbolCache));

        var type = compilation.GetTypeByMetadataName("TestApp.GetNameTests");
        var method = (IMethodSymbol)type.GetMembers("Action").First();

        var parameter = method.Parameters[0];
        var name = TopLevelParameterNameAnalyzer.GetName(symbolCache, parameter);

        Assert.Equal("param", name);
    }

    [Fact]
    public void GetName_ReturnsName_IfAttributeDoesNotSpecifyName()
    {
        var source = @"
using Microsoft.AspNetCore.Mvc;
namespace TestApp
{
    public class GetNameTests
    {
        public void Action([ModelBinder] int param) { }
    }
}";
        var compilation = TestCompilation.Create(source);
        Assert.True(TopLevelParameterNameAnalyzer.SymbolCache.TryCreate(compilation, out var symbolCache));

        var type = compilation.GetTypeByMetadataName("TestApp.GetNameTests");
        var method = (IMethodSymbol)type.GetMembers("Action").First();

        var parameter = method.Parameters[0];
        var name = TopLevelParameterNameAnalyzer.GetName(symbolCache, parameter);

        Assert.Equal("param", name);
    }

    [Fact]
    public void GetName_ReturnsFirstName_IfMultipleAttributesAreSpecified()
    {
        var source = @"
using Microsoft.AspNetCore.Mvc;
namespace TestApp
{
    public class GetNameTests
    {
        public void Action([ModelBinder(Name = ""name1"")][Bind(Prefix = ""name2"")] int param) { }
    }
}";
        var compilation = TestCompilation.Create(source);
        Assert.True(TopLevelParameterNameAnalyzer.SymbolCache.TryCreate(compilation, out var symbolCache));

        var type = compilation.GetTypeByMetadataName("TestApp.GetNameTests");
        var method = (IMethodSymbol)type.GetMembers("Action").First();

        var parameter = method.Parameters[0];
        var name = TopLevelParameterNameAnalyzer.GetName(symbolCache, parameter);

        Assert.Equal("name1", name);
    }

    [Fact]
    public void SpecifiesModelType_ReturnsFalse_IfModelBinderDoesNotSpecifyType()
    {
        var source = @"
using Microsoft.AspNetCore.Mvc;
namespace TestApp
{
    public class SpecifiesModelTypeTests
    {
        public void Action([ModelBinder(Name = ""Name"")] object model) { }
    }
}";
        var compilation = TestCompilation.Create(source);
        Assert.True(TopLevelParameterNameAnalyzer.SymbolCache.TryCreate(compilation, out var symbolCache));

        var type = compilation.GetTypeByMetadataName("TestApp.SpecifiesModelTypeTests");
        var method = (IMethodSymbol)type.GetMembers("Action").First();

        var parameter = method.Parameters[0];
        var result = TopLevelParameterNameAnalyzer.SpecifiesModelType(symbolCache, parameter);
        Assert.False(result);
    }

    [Fact]
    public void SpecifiesModelType_ReturnsTrue_IfModelBinderSpecifiesTypeFromConstructor()
    {
        var source = @"
using Microsoft.AspNetCore.Mvc;
namespace TestApp
{
    public class SpecifiesModelTypeTests
    {
        public void Action([ModelBinder(typeof(SimpleTypeModelBinder))] object model) { }
    }
}";
        var compilation = TestCompilation.Create(source);
        Assert.True(TopLevelParameterNameAnalyzer.SymbolCache.TryCreate(compilation, out var symbolCache));

        var type = compilation.GetTypeByMetadataName("TestApp.SpecifiesModelTypeTests");
        var method = (IMethodSymbol)type.GetMembers("Action").First();

        var parameter = method.Parameters[0];
        var result = TopLevelParameterNameAnalyzer.SpecifiesModelType(symbolCache, parameter);
        Assert.True(result);
    }

    [Fact]
    public void SpecifiesModelType_ReturnsTrue_IfModelBinderSpecifiesTypeFromProperty()
    {
        var source = @"
using Microsoft.AspNetCore.Mvc;
namespace TestApp
{
    public class SpecifiesModelTypeTests
    {
        public void Action([ModelBinder(BinderType = typeof(SimpleTypeModelBinder))] object model) { }
    }
}";
        var compilation = TestCompilation.Create(source);
        Assert.True(TopLevelParameterNameAnalyzer.SymbolCache.TryCreate(compilation, out var symbolCache));

        var type = compilation.GetTypeByMetadataName("TestApp.SpecifiesModelTypeTests");
        var method = (IMethodSymbol)type.GetMembers("Action").First();

        var parameter = method.Parameters[0];
        var result = TopLevelParameterNameAnalyzer.SpecifiesModelType(symbolCache, parameter);
        Assert.True(result);
    }

    private static Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
    {
        var test = new TopLevelParameterNameCSharpAnalyzerTest(TestReferences.MetadataReferences)
        {
            TestCode = source,
            ReferenceAssemblies = TestReferences.EmptyReferenceAssemblies,
        };

        test.ExpectedDiagnostics.AddRange(expected);
        return test.RunAsync();
    }

    internal sealed class TopLevelParameterNameCSharpAnalyzerTest : CSharpAnalyzerTest<TopLevelParameterNameAnalyzer, DefaultVerifier>
    {
        public TopLevelParameterNameCSharpAnalyzerTest(ImmutableArray<MetadataReference> metadataReferences)
        {
            TestState.AdditionalReferences.AddRange(metadataReferences);
        }

        protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers() => new[] { new TopLevelParameterNameAnalyzer() };
    }
}
