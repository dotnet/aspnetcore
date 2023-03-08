// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Routing;

public class DefaultParameterPolicyFactoryTest
{
    [Fact]
    public void Create_ThrowsException_IfNoConstraintOrParameterPolicy_FoundInMap()
    {
        // Arrange
        var factory = GetParameterPolicyFactory();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => factory.Create(RoutePatternFactory.ParameterPart("id", @default: null, RoutePatternParameterKind.Optional), @"notpresent(\d+)"));

        // Assert
        Assert.Equal(
            "The constraint reference 'notpresent' could not be resolved to a type. " +
            $"Register the constraint type with '{typeof(RouteOptions)}.{nameof(RouteOptions.ConstraintMap)}'.",
            exception.Message);
    }

    [Fact]
    public void Create_ThrowsException_OnInvalidType()
    {
        // Arrange
        var options = new RouteOptions();
        options.ConstraintMap.Add("bad", typeof(string));

        var services = new ServiceCollection();

        var factory = GetParameterPolicyFactory(options, services);

        // Act
        var exception = Assert.Throws<RouteCreationException>(
            () => factory.Create(RoutePatternFactory.ParameterPart("id"), @"bad"));

        // Assert
        Assert.Equal(
            $"The constraint type '{typeof(string)}' which is mapped to constraint key 'bad' must implement the '{nameof(IParameterPolicy)}' interface.",
            exception.Message);
    }

    [Fact]
    public void Create_CreatesParameterPolicy_FromRoutePattern_String()
    {
        // Arrange
        var factory = GetParameterPolicyFactory();

        var parameter = RoutePatternFactory.ParameterPart(
            "id",
            @default: null,
            parameterKind: RoutePatternParameterKind.Standard,
            parameterPolicies: new[] { RoutePatternFactory.Constraint("int"), });

        // Act
        var parameterPolicy = factory.Create(parameter, parameter.ParameterPolicies[0]);

        // Assert
        Assert.IsType<IntRouteConstraint>(parameterPolicy);
    }

    [Fact]
    public void Create_CreatesParameterPolicy_FromRoutePattern_String_Optional()
    {
        // Arrange
        var factory = GetParameterPolicyFactory();

        var parameter = RoutePatternFactory.ParameterPart(
            "id",
            @default: null,
            parameterKind: RoutePatternParameterKind.Optional,
            parameterPolicies: new[] { RoutePatternFactory.Constraint("int"), });

        // Act
        var parameterPolicy = factory.Create(parameter, parameter.ParameterPolicies[0]);

        // Assert
        var optionalConstraint = Assert.IsType<OptionalRouteConstraint>(parameterPolicy);
        Assert.IsType<IntRouteConstraint>(optionalConstraint.InnerConstraint);
    }

    [Fact]
    public void Create_CreatesParameterPolicy_FromRoutePattern_Constraint()
    {
        // Arrange
        var factory = GetParameterPolicyFactory();

        var parameter = RoutePatternFactory.ParameterPart(
            "id",
            @default: null,
            parameterKind: RoutePatternParameterKind.Standard,
            parameterPolicies: new[] { RoutePatternFactory.ParameterPolicy(new IntRouteConstraint()), });

        // Act
        var parameterPolicy = factory.Create(parameter, parameter.ParameterPolicies[0]);

        // Assert
        Assert.IsType<IntRouteConstraint>(parameterPolicy);
    }

    [Fact]
    public void Create_CreatesParameterPolicy_FromRoutePattern_Constraint_Optional()
    {
        // Arrange
        var factory = GetParameterPolicyFactory();

        var parameter = RoutePatternFactory.ParameterPart(
            "id",
            @default: null,
            parameterKind: RoutePatternParameterKind.Optional,
            parameterPolicies: new[] { RoutePatternFactory.ParameterPolicy(new IntRouteConstraint()), });

        // Act
        var parameterPolicy = factory.Create(parameter, parameter.ParameterPolicies[0]);

        // Assert
        var optionalConstraint = Assert.IsType<OptionalRouteConstraint>(parameterPolicy);
        Assert.IsType<IntRouteConstraint>(optionalConstraint.InnerConstraint);
    }

    [Fact]
    public void Create_CreatesParameterPolicy_FromRoutePattern_ParameterPolicy()
    {
        // Arrange
        var factory = GetParameterPolicyFactory();

        var parameter = RoutePatternFactory.ParameterPart(
            "id",
            @default: null,
            parameterKind: RoutePatternParameterKind.Standard,
            parameterPolicies: new[] { RoutePatternFactory.ParameterPolicy(new CustomParameterPolicy()), });

        // Act
        var parameterPolicy = factory.Create(parameter, parameter.ParameterPolicies[0]);

        // Assert
        Assert.IsType<CustomParameterPolicy>(parameterPolicy);
    }

    [Fact]
    public void Create_CreatesParameterPolicy_FromConstraintText_AndRouteConstraint()
    {
        // Arrange
        var factory = GetParameterPolicyFactory();

        // Act
        var parameterPolicy = factory.Create(RoutePatternFactory.ParameterPart("id"), "int");

        // Assert
        Assert.IsType<IntRouteConstraint>(parameterPolicy);
    }

    [Fact]
    public void Create_CreatesParameterPolicy_FromConstraintText_AndRouteConstraintWithArgument()
    {
        // Arrange
        var factory = GetParameterPolicyFactory();

        // Act
        var parameterPolicy = factory.Create(RoutePatternFactory.ParameterPart("id"), "range(1,20)");

        // Assert
        var constraint = Assert.IsType<RangeRouteConstraint>(parameterPolicy);
        Assert.Equal(1, constraint.Min);
        Assert.Equal(20, constraint.Max);
    }

    [Fact]
    public void Create_CreatesParameterPolicy_FromConstraintText_AndRouteConstraint_Optional()
    {
        // Arrange
        var factory = GetParameterPolicyFactory();

        // Act
        var parameterPolicy = factory.Create(RoutePatternFactory.ParameterPart("id", @default: null, RoutePatternParameterKind.Optional), "int");

        // Assert
        var optionalConstraint = Assert.IsType<OptionalRouteConstraint>(parameterPolicy);
        Assert.IsType<IntRouteConstraint>(optionalConstraint.InnerConstraint);
    }

    [Fact]
    public void Create_CreatesParameterPolicy_FromConstraintText_AndParameterPolicy()
    {
        // Arrange
        var options = new RouteOptions();
        options.ConstraintMap.Add("customParameterPolicy", typeof(CustomParameterPolicy));

        var services = new ServiceCollection();
        services.AddTransient<CustomParameterPolicy>();

        var factory = GetParameterPolicyFactory(options, services);

        // Act
        var parameterPolicy = factory.Create(RoutePatternFactory.ParameterPart("id", @default: null, RoutePatternParameterKind.Optional), "customParameterPolicy");

        // Assert
        Assert.IsType<CustomParameterPolicy>(parameterPolicy);
    }

    [Fact]
    public void Create_CreatesParameterPolicy_FromConstraintText_AndParameterPolicyWithArgumentAndServices()
    {
        // Arrange
        var options = new RouteOptions();
        options.ConstraintMap.Add("customConstraintPolicy", typeof(CustomParameterPolicyWithArguments));

        var services = new ServiceCollection();
        services.AddTransient<ITestService, TestService>();

        var factory = GetParameterPolicyFactory(options, services);

        // Act
        var parameterPolicy = factory.Create(RoutePatternFactory.ParameterPart("id"), "customConstraintPolicy(20)");

        // Assert
        var constraint = Assert.IsType<CustomParameterPolicyWithArguments>(parameterPolicy);
        Assert.Equal(20, constraint.Count);
        Assert.NotNull(constraint.TestService);
    }

    [Fact]
    public void Create_CreatesParameterPolicy_FromConstraintText_AndParameterPolicyWithArgumentAndMultipleServices()
    {
        // Arrange
        var options = new RouteOptions();
        options.ConstraintMap.Add("customConstraintPolicy", typeof(CustomParameterPolicyWithMultipleArguments));

        var services = new ServiceCollection();
        services.AddTransient<ITestService, TestService>();

        var factory = GetParameterPolicyFactory(options, services);

        // Act
        var parameterPolicy = factory.Create(RoutePatternFactory.ParameterPart("id"), "customConstraintPolicy(20,-1)");

        // Assert
        var constraint = Assert.IsType<CustomParameterPolicyWithMultipleArguments>(parameterPolicy);
        Assert.Equal(20, constraint.First);
        Assert.Equal(-1, constraint.Second);
        Assert.NotNull(constraint.TestService1);
        Assert.NotNull(constraint.TestService2);
    }

    [Fact]
    public void Create_CreatesParameterPolicy_FromConstraintText_AndParameterPolicyWithOnlyServiceArguments()
    {
        // Arrange
        var options = new RouteOptions();
        options.ConstraintMap.Add("customConstraintPolicy", typeof(CustomParameterPolicyWithOnlyServiceArguments));

        var services = new ServiceCollection();
        services.AddTransient<ITestService, TestService>();

        var factory = GetParameterPolicyFactory(options, services);

        // Act
        var parameterPolicy = factory.Create(RoutePatternFactory.ParameterPart("id"), "customConstraintPolicy");

        // Assert
        var constraint = Assert.IsType<CustomParameterPolicyWithOnlyServiceArguments>(parameterPolicy);
        Assert.NotNull(constraint.TestService1);
        Assert.NotNull(constraint.TestService2);
    }

    [Fact]
    public void Create_CreatesParameterPolicy_FromConstraintText_AndParameterPolicyWithMultipleMatchingCtors()
    {
        // Arrange
        var options = new RouteOptions();
        options.ConstraintMap.Add("customConstraintPolicy", typeof(CustomParameterPolicyWithMultipleCtors));

        var services = new ServiceCollection();
        services.AddTransient<ITestService, TestService>();

        var factory = GetParameterPolicyFactory(options, services);

        // Act
        var parameterPolicy = factory.Create(RoutePatternFactory.ParameterPart("id"), "customConstraintPolicy(1)");

        // Assert
        var constraint = Assert.IsType<CustomParameterPolicyWithMultipleCtors>(parameterPolicy);
        Assert.NotNull(constraint.TestService);
        Assert.Equal(1, constraint.Count);
    }

    [Fact]
    public void Create_CreatesParameterPolicy_FromConstraintText_AndParameterPolicyWithMultipleMatchingCtorsInAscendingOrder()
    {
        // Arrange
        var options = new RouteOptions();
        options.ConstraintMap.Add("customConstraintPolicy", typeof(CustomParameterPolicyWithMultipleCtorsInAscendingOrder));

        var services = new ServiceCollection();
        services.AddTransient<ITestService, TestService>();

        var factory = GetParameterPolicyFactory(options, services);

        // Act
        var parameterPolicy = factory.Create(RoutePatternFactory.ParameterPart("id"), "customConstraintPolicy(1)");

        // Assert
        var constraint = Assert.IsType<CustomParameterPolicyWithMultipleCtorsInAscendingOrder>(parameterPolicy);
        Assert.NotNull(constraint.TestService1);
        Assert.NotNull(constraint.TestService2);
        Assert.Equal(1, constraint.Count);
    }

    [Fact]
    public void Create_CreatesParameterPolicy_FromConstraintText_AndParameterPolicyWithAmbigiousMatchingCtors()
    {
        // Arrange
        var options = new RouteOptions();
        options.ConstraintMap.Add("customConstraintPolicy", typeof(CustomParameterPolicyWithAmbiguousMultipleCtors));

        var services = new ServiceCollection();
        services.AddTransient<ITestService, TestService>();

        var factory = GetParameterPolicyFactory(options, services);

        // Act
        var exception = Assert.Throws<RouteCreationException>(
            () => factory.Create(RoutePatternFactory.ParameterPart("id"), "customConstraintPolicy(1)"));

        // Assert
        Assert.Equal($"The constructor to use for activating the constraint type '{nameof(CustomParameterPolicyWithAmbiguousMultipleCtors)}' is ambiguous. "
            + "Multiple constructors were found with the following number of parameters: 2.", exception.Message);
    }

    [Fact]
    public void Create_CreatesParameterPolicy_FromConstraintText_AndParameterPolicyWithSingleArgumentAndServiceArgument()
    {
        // Arrange
        var options = new RouteOptions();
        options.ConstraintMap.Add("regex-service", typeof(RegexInlineRouteConstraintWithService));

        var services = new ServiceCollection();
        services.AddTransient<ITestService, TestService>();

        var factory = GetParameterPolicyFactory(options, services);

        // Act
        var parameterPolicy = factory.Create(RoutePatternFactory.ParameterPart("id"), @"regex-service(\\d{1,2})");

        // Assert
        var constraint = Assert.IsType<RegexInlineRouteConstraintWithService>(parameterPolicy);
        Assert.NotNull(constraint.TestService);
        Assert.Equal("\\\\d{1,2}", constraint.Constraint.ToString());
    }

    [Fact]
    public void Create_CreatesParameterPolicy_FromConstraintText_AndParameterPolicyWithArgumentAndUnresolvedServices_Throw()
    {
        // Arrange
        var options = new RouteOptions();
        options.ConstraintMap.Add("customConstraintPolicy", typeof(CustomParameterPolicyWithArguments));

        var services = new ServiceCollection();

        var factory = GetParameterPolicyFactory(options, services);

        // Act
        var exception = Assert.Throws<RouteCreationException>(
            () => factory.Create(RoutePatternFactory.ParameterPart("id"), "customConstraintPolicy(20)"));

        // Assert
        var inner = Assert.IsType<InvalidOperationException>(exception.InnerException);
        Assert.Equal($"No service for type '{typeof(ITestService).FullName}' has been registered.", inner.Message);
    }

    [Fact]
    public void Create_CreatesParameterPolicy_FromConstraintText_AndParameterPolicy_Optional()
    {
        // Arrange
        var options = new RouteOptions();
        options.ConstraintMap.Add("customParameterPolicy", typeof(CustomParameterPolicy));

        var services = new ServiceCollection();
        services.AddTransient<CustomParameterPolicy>();

        var factory = GetParameterPolicyFactory(options, services);

        // Act
        var parameterPolicy = factory.Create(RoutePatternFactory.ParameterPart("id", @default: null, RoutePatternParameterKind.Optional), "customParameterPolicy");

        // Assert
        Assert.IsType<CustomParameterPolicy>(parameterPolicy);
    }

    private DefaultParameterPolicyFactory GetParameterPolicyFactory(
        RouteOptions options = null,
        ServiceCollection services = null)
    {
        if (options == null)
        {
            options = new RouteOptions();
        }

        if (services == null)
        {
            services = new ServiceCollection();
        }

        return new DefaultParameterPolicyFactory(
            Options.Create(options),
            services.BuildServiceProvider());
    }

    private class TestRouteConstraint : IRouteConstraint
    {
        private TestRouteConstraint() { }

        public HttpContext HttpContext { get; private set; }
        public IRouter Route { get; private set; }
        public string RouteKey { get; private set; }
        public RouteValueDictionary Values { get; private set; }
        public RouteDirection RouteDirection { get; private set; }

        public static TestRouteConstraint Create()
        {
            return new TestRouteConstraint();
        }

        public bool Match(
            HttpContext httpContext,
            IRouter route,
            string routeKey,
            RouteValueDictionary values,
            RouteDirection routeDirection)
        {
            HttpContext = httpContext;
            Route = route;
            RouteKey = routeKey;
            Values = values;
            RouteDirection = routeDirection;
            return false;
        }
    }
}

public class CustomParameterPolicy : IParameterPolicy
{
}

public class CustomParameterPolicyWithArguments : IParameterPolicy
{
    public CustomParameterPolicyWithArguments(ITestService testService, int count)
    {
        TestService = testService;
        Count = count;
    }

    public ITestService TestService { get; }
    public int Count { get; }
}

public class CustomParameterPolicyWithMultipleCtors : IParameterPolicy
{
    public CustomParameterPolicyWithMultipleCtors(ITestService testService, int count)
    {
        TestService = testService;
        Count = count;
    }

    public CustomParameterPolicyWithMultipleCtors(int count)
        : this(testService: null, count)
    {
    }

    public ITestService TestService { get; }
    public int Count { get; }
}

public class CustomParameterPolicyWithMultipleCtorsInAscendingOrder : IParameterPolicy
{
    public CustomParameterPolicyWithMultipleCtorsInAscendingOrder(int count)
        : this(testService1: null, count)
    {
    }

    public CustomParameterPolicyWithMultipleCtorsInAscendingOrder(ITestService testService1, int count)
    {
        TestService1 = testService1;
        Count = count;
    }

    public CustomParameterPolicyWithMultipleCtorsInAscendingOrder(ITestService testService1, ITestService testService2, int count)
    {
        TestService1 = testService1;
        TestService2 = testService2;
        Count = count;
    }

    public ITestService TestService1 { get; }
    public ITestService TestService2 { get; }
    public int Count { get; }
}

public class CustomParameterPolicyWithAmbiguousMultipleCtors : IParameterPolicy
{
    public CustomParameterPolicyWithAmbiguousMultipleCtors(ITestService testService, int count)
    {
        TestService = testService;
        Count = count;
    }

    public CustomParameterPolicyWithAmbiguousMultipleCtors(object testService, int count)
        : this(testService: null, count)
    {
    }

    public CustomParameterPolicyWithAmbiguousMultipleCtors(int count)
        : this(testService: null, count)
    {
    }

    public ITestService TestService { get; }
    public int Count { get; }
}

public class CustomParameterPolicyWithMultipleArguments : IParameterPolicy
{
    public CustomParameterPolicyWithMultipleArguments(int first, ITestService testService1, int second, ITestService testService2)
    {
        First = first;
        TestService1 = testService1;
        Second = second;
        TestService2 = testService2;
    }

    public int First { get; }
    public ITestService TestService1 { get; }
    public int Second { get; }
    public ITestService TestService2 { get; }
}

public class CustomParameterPolicyWithOnlyServiceArguments : IParameterPolicy
{
    public CustomParameterPolicyWithOnlyServiceArguments(ITestService testService1, ITestService testService2)
    {
        TestService1 = testService1;
        TestService2 = testService2;
    }

    public ITestService TestService1 { get; }
    public ITestService TestService2 { get; }
}

public interface ITestService
{
}

public class TestService : ITestService
{

}

public class RegexInlineRouteConstraintWithService : RegexRouteConstraint
{
    public RegexInlineRouteConstraintWithService(string regexPattern, ITestService testService)
        : base(regexPattern)
    {
        TestService = testService;
    }

    public ITestService TestService { get; }
}
