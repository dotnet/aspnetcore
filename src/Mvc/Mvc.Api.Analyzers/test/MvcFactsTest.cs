// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Shared;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers;

public class MvcFactsTest
{
    private static readonly string ControllerAttribute = typeof(ControllerAttribute).FullName;
    private static readonly string NonControllerAttribute = typeof(NonControllerAttribute).FullName;
    private static readonly string NonActionAttribute = typeof(NonActionAttribute).FullName;
    private static readonly Type TestIsControllerActionType = typeof(TestIsControllerAction);

    #region IsController
    [Fact]
    public Task IsController_ReturnsFalseForInterfaces() => IsControllerReturnsFalse(typeof(ITestController));

    [Fact]
    public Task IsController_ReturnsFalseForAbstractTypes() => IsControllerReturnsFalse(typeof(AbstractController));

    [Fact]
    public Task IsController_ReturnsFalseForValueType() => IsControllerReturnsFalse(typeof(ValueTypeController));

    [Fact]
    public Task IsController_ReturnsFalseForGenericType() => IsControllerReturnsFalse(typeof(OpenGenericController<>));

    [Fact]
    public Task IsController_ReturnsFalseForPocoType() => IsControllerReturnsFalse(typeof(PocoType));

    [Fact]
    public Task IsController_ReturnsFalseForTypeDerivedFromPocoType() => IsControllerReturnsFalse(typeof(DerivedPocoType));

    [Fact]
    public Task IsController_ReturnsTrueForTypeDerivingFromController() => IsControllerReturnsTrue(typeof(TypeDerivingFromController));

    [Fact]
    public Task IsController_ReturnsTrueForTypeDerivingFromControllerBase() => IsControllerReturnsTrue(typeof(TypeDerivingFromControllerBase));

    [Fact]
    public Task IsController_ReturnsTrueForTypeDerivingFromController_WithoutSuffix() => IsControllerReturnsTrue(typeof(NoSuffix));

    [Fact]
    public Task IsController_ReturnsTrueForTypeWithSuffix_ThatIsNotDerivedFromController() => IsControllerReturnsTrue(typeof(PocoController));

    [Fact]
    public Task IsController_ReturnsTrueForTypeWithoutSuffix_WithControllerAttribute() => IsControllerReturnsTrue(typeof(CustomBase));

    [Fact]
    public Task IsController_ReturnsTrueForTypeDerivingFromCustomBaseThatHasControllerAttribute() => IsControllerReturnsTrue(typeof(ChildOfCustomBase));

    [Fact]
    public Task IsController_ReturnsFalseForTypeWithNonControllerAttribute() => IsControllerReturnsFalse(typeof(BaseNonController));

    [Fact]
    public Task IsController_ReturnsFalseForTypesDerivingFromTypeWithNonControllerAttribute() => IsControllerReturnsFalse(typeof(BasePocoNonControllerChildController));

    [Fact]
    public Task IsController_ReturnsFalseForTypesDerivingFromTypeWithNonControllerAttributeWithControllerAttribute() =>
        IsControllerReturnsFalse(typeof(ControllerAttributeDerivingFromNonController));

    private async Task IsControllerReturnsFalse(Type type)
    {
        var compilation = await GetIsControllerCompilation();
        var controllerAttribute = compilation.GetTypeByMetadataName(ControllerAttribute);
        var nonControllerAttribute = compilation.GetTypeByMetadataName(NonControllerAttribute);
        var typeSymbol = compilation.GetTypeByMetadataName(type.FullName);

        // Act
        var isController = MvcFacts.IsController(typeSymbol, controllerAttribute, nonControllerAttribute);

        // Assert
        Assert.False(isController);
    }

    private async Task IsControllerReturnsTrue(Type type)
    {
        var compilation = await GetIsControllerCompilation();
        var controllerAttribute = compilation.GetTypeByMetadataName(ControllerAttribute);
        var nonControllerAttribute = compilation.GetTypeByMetadataName(NonControllerAttribute);
        var typeSymbol = compilation.GetTypeByMetadataName(type.FullName);

        // Act
        var isController = MvcFacts.IsController(typeSymbol, controllerAttribute, nonControllerAttribute);

        // Assert
        Assert.True(isController);
    }

    #endregion

    #region IsControllerAction
    [Fact]
    public Task IsAction_ReturnsFalseForConstructor() => IsActionReturnsFalse(TestIsControllerActionType, ".ctor");

    [Fact]
    public Task IsAction_ReturnsFalseForStaticConstructor() => IsActionReturnsFalse(TestIsControllerActionType, ".cctor");

    [Fact]
    public Task IsAction_ReturnsFalseForPrivateMethod() => IsActionReturnsFalse(TestIsControllerActionType, "PrivateMethod");

    [Fact]
    public Task IsAction_ReturnsFalseForProtectedMethod() => IsActionReturnsFalse(TestIsControllerActionType, "ProtectedMethod");

    [Fact]
    public Task IsAction_ReturnsFalseForInternalMethod() => IsActionReturnsFalse(TestIsControllerActionType, nameof(TestIsControllerAction.InternalMethod));

    [Fact]
    public Task IsAction_ReturnsFalseForGenericMethod() => IsActionReturnsFalse(TestIsControllerActionType, nameof(TestIsControllerAction.GenericMethod));

    [Fact]
    public Task IsAction_ReturnsFalseForStaticMethod() => IsActionReturnsFalse(TestIsControllerActionType, nameof(TestIsControllerAction.StaticMethod));

    [Fact]
    public Task IsAction_ReturnsFalseForNonActionMethod() => IsActionReturnsFalse(TestIsControllerActionType, nameof(TestIsControllerAction.NonAction));

    [Fact]
    public Task IsAction_ReturnsFalseForOverriddenNonActionMethod() => IsActionReturnsFalse(TestIsControllerActionType, nameof(TestIsControllerAction.NonActionBase));

    [Fact]
    public Task IsAction_ReturnsFalseForDisposableDispose() => IsActionReturnsFalse(TestIsControllerActionType, nameof(TestIsControllerAction.Dispose));

    [Fact]
    public Task IsAction_ReturnsFalseForExplicitDisposableDispose() => IsActionReturnsFalse(typeof(ExplicitIDisposable), "System.IDisposable.Dispose");

    [Fact]
    public Task IsAction_ReturnsFalseForAbstractMethods() => IsActionReturnsFalse(typeof(TestIsControllerActionBase), nameof(TestIsControllerActionBase.AbstractMethod));

    [Fact]
    public Task IsAction_ReturnsFalseForObjectEquals() => IsActionReturnsFalse(typeof(object), nameof(object.Equals));

    [Fact]
    public Task IsAction_ReturnsFalseForObjectHashCode() => IsActionReturnsFalse(typeof(object), nameof(object.GetHashCode));

    [Fact]
    public Task IsAction_ReturnsFalseForObjectToString() => IsActionReturnsFalse(typeof(object), nameof(object.ToString));

    [Fact]
    public Task IsAction_ReturnsFalseForOverriddenObjectEquals() =>
        IsActionReturnsFalse(typeof(OverridesObjectMethods), nameof(OverridesObjectMethods.Equals));

    [Fact]
    public Task IsAction_ReturnsFalseForOverriddenObjectHashCode() =>
        IsActionReturnsFalse(typeof(OverridesObjectMethods), nameof(OverridesObjectMethods.GetHashCode));

    private async Task IsActionReturnsFalse(Type type, string methodName)
    {
        var compilation = await GetIsControllerActionCompilation();
        var nonActionAttribute = compilation.GetTypeByMetadataName(NonActionAttribute);
        var disposableDispose = GetDisposableDispose(compilation);
        var typeSymbol = compilation.GetTypeByMetadataName(type.FullName);
        var method = (IMethodSymbol)typeSymbol.GetMembers(methodName).First();

        // Act
        var isControllerAction = MvcFacts.IsControllerAction(method, nonActionAttribute, disposableDispose);

        // Assert
        Assert.False(isControllerAction);
    }

    [Fact]
    public Task IsAction_ReturnsTrueForNewMethodsOfObject() => IsActionReturnsTrue(typeof(OverridesObjectMethods), nameof(OverridesObjectMethods.ToString));

    [Fact]
    public Task IsAction_ReturnsTrueForNotDisposableDispose() => IsActionReturnsTrue(typeof(NotDisposable), nameof(NotDisposable.Dispose));

    [Fact]
    public Task IsAction_ReturnsTrueForNotDisposableDisposeOnTypeWithExplicitImplementation() =>
        IsActionReturnsTrue(typeof(NotDisposableWithExplicitImplementation), nameof(NotDisposableWithExplicitImplementation.Dispose));

    [Fact]
    public Task IsAction_ReturnsTrueForOrdinaryAction() => IsActionReturnsTrue(TestIsControllerActionType, nameof(TestIsControllerAction.Ordinary));

    [Fact]
    public Task IsAction_ReturnsTrueForOverriddenMethod() => IsActionReturnsTrue(TestIsControllerActionType, nameof(TestIsControllerAction.AbstractMethod));

    [Fact]
    public async Task IsAction_ReturnsTrueForNotDisposableDisposeOnTypeWithImplicitImplementation()
    {
        var compilation = await GetIsControllerActionCompilation();
        var nonActionAttribute = compilation.GetTypeByMetadataName(NonActionAttribute);
        var disposableDispose = GetDisposableDispose(compilation);
        var typeSymbol = compilation.GetTypeByMetadataName(typeof(NotDisposableWithDisposeThatIsNotInterfaceContract).FullName);
        var method = typeSymbol.GetMembers(nameof(IDisposable.Dispose)).OfType<IMethodSymbol>().First(f => !f.ReturnsVoid);

        // Act
        var isControllerAction = MvcFacts.IsControllerAction(method, nonActionAttribute, disposableDispose);

        // Assert
        Assert.True(isControllerAction);
    }

    private async Task IsActionReturnsTrue(Type type, string methodName)
    {
        var compilation = await GetIsControllerActionCompilation();
        var nonActionAttribute = compilation.GetTypeByMetadataName(NonActionAttribute);
        var disposableDispose = GetDisposableDispose(compilation);
        var typeSymbol = compilation.GetTypeByMetadataName(type.FullName);
        var method = (IMethodSymbol)typeSymbol.GetMembers(methodName).First();

        // Act
        var isControllerAction = MvcFacts.IsControllerAction(method, nonActionAttribute, disposableDispose);

        // Assert
        Assert.True(isControllerAction);
    }

    private IMethodSymbol GetDisposableDispose(Compilation compilation)
    {
        var type = compilation.GetSpecialType(SpecialType.System_IDisposable);
        return (IMethodSymbol)type.GetMembers(nameof(IDisposable.Dispose)).First();
    }
    #endregion

    private Task<Compilation> GetIsControllerCompilation() => GetCompilation("IsControllerTests");

    private Task<Compilation> GetIsControllerActionCompilation() => GetCompilation("IsControllerActionTests");

    private Task<Compilation> GetCompilation(string test)
    {
        var testSource = MvcTestSource.Read(GetType().Name, test);
        var project = MvcDiagnosticAnalyzerRunner.CreateProjectWithReferencesInBinDir(GetType().Assembly, new[] { testSource.Source });

        return project.GetCompilationAsync();
    }
}
