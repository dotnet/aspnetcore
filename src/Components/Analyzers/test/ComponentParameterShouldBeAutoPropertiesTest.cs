// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;
using Xunit;

namespace Microsoft.AspNetCore.Components.Analyzers.Tests;

public class ComponentParameterShouldBeAutoPropertiesTest : DiagnosticVerifier
{
    [Fact]
    public void IsAutoProperty_NoDiagnostic()
    {
        var source = @"
using Microsoft.AspNetCore.Components

public class C
{
[Parameter]
public string MyProp { get; set; }

[Parameter]
public string MyProp2 { set; get; }
}
" + ComponentsTestDeclarations.Source;
        VerifyCSharpDiagnostic(source);
    }

    [Fact]
    public void HaveSameSemanticAsAutoProperty_NoDiagnostic()
    {
        var source = @"
using Microsoft.AspNetCore.Components

public class C
{
private string _myProp;
private string _myProp2;
private string _myProp3;

[Parameter]
public string MyProp
{
    get => _myProp;
    set => _myProp = value;
}

[Parameter]
public string MyProp2
{
    set => _myProp2 = value;
    get => _myProp2;
}

[Parameter]
public string MyProp3
{
    get
    {
        return _myProp3;
    }
    set
    {
        _myProp3 = value;
    }
}
}
" + ComponentsTestDeclarations.Source;
        VerifyCSharpDiagnostic(source);
    }

    [Fact]
    public void HaveLogicInSetter_Diagnostic()
    {
        var source = @"
using Microsoft.AspNetCore.Components

public class C
{
private string _myProp;

[Parameter]
public string MyProp
{
    get
    {
        return _myProp;
    }
    set
    {
        DoSomething();
        _myProp = value;
    }
}

private void DoSomething() { }
}
" + ComponentsTestDeclarations.Source;
        VerifyCSharpDiagnostic(source, new DiagnosticResult
        {
            Id = DiagnosticDescriptors.ComponentParametersShouldBeAutoProperties.Id,
            Message = "Component parameter 'C.MyProp' should be auto property",
            Locations = new[]
            {
                new DiagnosticResultLocation("Test0.cs", 9, 15),
            },
            Severity = CodeAnalysis.DiagnosticSeverity.Warning,
        });
    }

    [Fact]
    public void HaveLogicInGetter_Diagnostic()
    {
        var source = @"
using Microsoft.AspNetCore.Components

public class C
{
private string _myProp;

[Parameter]
public string MyProp
{
    get
    {
        DoSomething();
        return _myProp;
    }
    set
    {
        _myProp = value;
    }
}

private void DoSomething() { }
}
" + ComponentsTestDeclarations.Source;
        VerifyCSharpDiagnostic(source, new DiagnosticResult
        {
            Id = DiagnosticDescriptors.ComponentParametersShouldBeAutoProperties.Id,
            Message = "Component parameter 'C.MyProp' should be auto property",
            Locations = new[]
            {
                new DiagnosticResultLocation("Test0.cs", 9, 15),
            },
            Severity = CodeAnalysis.DiagnosticSeverity.Warning,
        });
    }

    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new ComponentParameterAnalyzer();
}
