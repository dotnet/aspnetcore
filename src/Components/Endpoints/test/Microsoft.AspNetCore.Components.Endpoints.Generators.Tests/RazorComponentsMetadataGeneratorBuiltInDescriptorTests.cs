// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;

namespace Microsoft.AspNetCore.Components.Endpoints.Generators.Tests;

#nullable enable

public class RazorComponentsMetadataGeneratorBuiltInDescriptorTests : RazorComponentsMetadataGeneratorTestBase
{
    private const string BuiltInDescriptorProviderType =
        "Microsoft.AspNetCore.Components.Infrastructure.BuiltInComponentDescriptors";

    [Fact]
    public void ReferencedKnownAssemblies_EmitOnlyMatchingProviderAccessors()
    {
        string[] directFrameworkReferences =
        [
            "Microsoft.AspNetCore.Components",
            "Microsoft.AspNetCore.Components.Forms",
            "Microsoft.AspNetCore.Components.Web",
            "Microsoft.JSInterop",
        ];
        string[] expectedProviderAssemblies =
        [
            "Microsoft.AspNetCore.Components",
            "Microsoft.AspNetCore.Components.Forms",
            "Microsoft.AspNetCore.Components.Web",
        ];

        var result = RunGenerator(
            "namespace TestComponents;",
            hostFrameworkAssemblyNames: directFrameworkReferences);

        var source = GetGeneratedSource(result);
        var providerAssemblies = Regex.Matches(
                source,
                $"""UnsafeAccessorType\("{Regex.Escape(BuiltInDescriptorProviderType)}, ([^"]+)"\)""")
            .Select(match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Equal(expectedProviderAssemblies.Order(), providerAssemblies.Order());
        Assert.DoesNotContain("Microsoft.AspNetCore.Components.Endpoints", providerAssemblies);
        Assert.DoesNotContain("Microsoft.AspNetCore.Components.Authorization", providerAssemblies);
        Assert.DoesNotContain("Microsoft.JSInterop", providerAssemblies);
    }

    [Theory]
    [InlineData(
        "Microsoft.AspNetCore.Components.Forms.ValidationMessage<string>",
        "CreateValidationMessageDescriptors")]
    [InlineData(
        "Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize<string>",
        "CreateVirtualizeDescriptors")]
    public void ExplicitKnownGenericComponent_EmitsBuiltInFactory(
        string componentType,
        string factoryName)
    {
        var result = RunGenerator(
            "namespace TestComponents;",
            $$"""
            namespace TestHost;

            [Microsoft.AspNetCore.Components.Web.ComponentTypeInfo(
                typeof({{componentType}}))]
            public sealed partial class TestMetadata : Microsoft.AspNetCore.Components.Web.RazorComponentsMetadataContext
            {
            }
            """);

        var source = GetGeneratedSource(result);
        var factoryAccessor = Assert.Single(
            Regex.Matches(
                source,
                $$"""UnsafeAccessorKind\.StaticMethod, Name = "{{factoryName}}"\)\]\s*private static extern [^\r\n]+ GetBuiltInComponentDescriptorFactory_(\d+)<T0>\(\s*\[global::System\.Runtime\.CompilerServices\.UnsafeAccessorType\("{{Regex.Escape(BuiltInDescriptorProviderType)}}, Microsoft\.AspNetCore\.Components\.Web"\)\] object\? target\);""")
                .Cast<Match>());
        var factoryIndex = factoryAccessor.Groups[1].Value;

        Assert.Contains($".. GetBuiltInComponentDescriptorFactory_{factoryIndex}<string>(null),", source);
    }

    [Theory]
    [InlineData(
        "Microsoft.AspNetCore.Components.CascadingValue<TestComponents.Marker>",
        "Microsoft.AspNetCore.Components.CascadingValue<global::TestComponents.Marker>")]
    [InlineData(
        "TestComponents.GenericComponent<TestComponents.Marker>",
        "TestComponents.GenericComponent<global::TestComponents.Marker>")]
    public void ExplicitOtherClosedGenericComponent_EmitsOnlyDirectDescriptor(
        string componentType,
        string emittedComponentType)
    {
        var result = RunGenerator(
            """
            namespace TestComponents;

            public sealed class Marker;

            public sealed class GenericComponent<T> : Microsoft.AspNetCore.Components.ComponentBase
            {
                [Microsoft.AspNetCore.Components.Parameter]
                public T? Value { get; set; }
            }
            """,
            $$"""
            namespace TestHost;

            [Microsoft.AspNetCore.Components.Web.ComponentTypeInfo(
                typeof({{componentType}}))]
            public sealed partial class TestMetadata : Microsoft.AspNetCore.Components.Web.RazorComponentsMetadataContext
            {
            }
            """);

        var source = GetGeneratedSource(result);

        Assert.Contains($"Type = typeof(global::{emittedComponentType}),", source);
        Assert.Empty(Regex.Matches(
            source,
            @"GetBuiltInComponentDescriptorFactory_\d+<global::TestComponents\.Marker>"));
    }

    [Fact]
    public void ComponentsProvider_ExposesWorkingRouterDescriptor()
    {
        var result = RunGeneratorForProviders("Microsoft.AspNetCore.Components");
        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            var descriptor = Assert.Single(
                context.Components,
                item => item.Type.FullName == "Microsoft.AspNetCore.Components.Routing.Router" &&
                    IsBuiltInProviderDescriptor(item));
            var router = Assert.IsType<Microsoft.AspNetCore.Components.Routing.Router>(
                descriptor.CreateInstance!(new TestServiceProvider()));

            var expectedParameters = new Dictionary<string, Type>
            {
                ["AppAssembly"] = typeof(System.Reflection.Assembly),
                ["AdditionalAssemblies"] = typeof(IEnumerable<System.Reflection.Assembly>),
                ["NotFound"] = typeof(Microsoft.AspNetCore.Components.RenderFragment),
                ["NotFoundPage"] = typeof(Type),
                ["Found"] = typeof(Microsoft.AspNetCore.Components.RenderFragment<Microsoft.AspNetCore.Components.RouteData>),
                ["Navigating"] = typeof(Microsoft.AspNetCore.Components.RenderFragment),
                ["OnNavigateAsync"] = typeof(Microsoft.AspNetCore.Components.EventCallback<Microsoft.AspNetCore.Components.Routing.NavigationContext>),
            };
            Assert.Equal(expectedParameters.Count, descriptor.Parameters.Count);
            foreach (var parameter in descriptor.Parameters)
            {
                Assert.Equal(expectedParameters[parameter.Name], parameter.ParameterType);
                Assert.IsType<Microsoft.AspNetCore.Components.ParameterAttribute>(parameter.Attribute);
            }

            var appAssembly = typeof(RazorComponentsMetadataGeneratorBuiltInDescriptorTests).Assembly;
            var appAssemblyParameter = Assert.Single(descriptor.Parameters, item => item.Name == "AppAssembly");
            appAssemblyParameter.SetValue(router, appAssembly);
            Assert.Same(appAssembly, appAssemblyParameter.GetValue(router));

            var notFoundPageParameter = Assert.Single(descriptor.Parameters, item => item.Name == "NotFoundPage");
            notFoundPageParameter.SetValue(router, typeof(TestServiceProvider));
            Assert.Same(typeof(TestServiceProvider), notFoundPageParameter.GetValue(router));
            notFoundPageParameter.SetValue(router, null);
            Assert.Null(notFoundPageParameter.GetValue(router));

            Microsoft.AspNetCore.Components.RenderFragment navigating = _ => { };
            var navigatingParameter = Assert.Single(descriptor.Parameters, item => item.Name == "Navigating");
            navigatingParameter.SetValue(router, navigating);
            Assert.Same(navigating, navigatingParameter.GetValue(router));

            var callback = Microsoft.AspNetCore.Components.EventCallback.Factory.Create<
                Microsoft.AspNetCore.Components.Routing.NavigationContext>(
                    new object(),
                    static _ => Task.CompletedTask);
            var callbackParameter = Assert.Single(descriptor.Parameters, item => item.Name == "OnNavigateAsync");
            callbackParameter.SetValue(router, callback);
            Assert.Equal(callback, callbackParameter.GetValue(router));

            var navigationManager = new TestNavigationManager();
            var navigationInterception = new TestNavigationInterception();
            var scrollToLocationHash = new TestScrollToLocationHash();
            var loggerFactory = Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;
            var serviceProvider = new TestServiceProvider();
            var injectableValues = new Dictionary<string, (Type ServiceType, object Value)>
            {
                ["NavigationManager"] = (typeof(Microsoft.AspNetCore.Components.NavigationManager), navigationManager),
                ["NavigationInterception"] = (typeof(Microsoft.AspNetCore.Components.Routing.INavigationInterception), navigationInterception),
                ["ScrollToLocationHash"] = (typeof(Microsoft.AspNetCore.Components.Routing.IScrollToLocationHash), scrollToLocationHash),
                ["LoggerFactory"] = (typeof(Microsoft.Extensions.Logging.ILoggerFactory), loggerFactory),
                ["ServiceProvider"] = (typeof(IServiceProvider), serviceProvider),
            };
            Assert.Equal(injectableValues.Count, descriptor.Injectables.Count);
            foreach (var injectable in descriptor.Injectables)
            {
                var expected = injectableValues[injectable.Name];
                Assert.Equal(expected.ServiceType, injectable.ServiceType);
                Assert.IsType<Microsoft.AspNetCore.Components.InjectAttribute>(injectable.Attribute);
                injectable.SetValue(router, expected.Value);
                Assert.Same(expected.Value, GetNonPublicProperty(router, injectable.Name));
            }
        }
    }

    [Fact]
    public void ComponentsProvider_ExposesConstructibleSectionContentRenderer()
    {
        var result = RunGeneratorForProviders("Microsoft.AspNetCore.Components");
        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            var descriptor = Assert.Single(
                context.Components,
                item => item.Type.FullName ==
                    "Microsoft.AspNetCore.Components.Sections.SectionOutlet+SectionOutletContentRenderer" &&
                    IsBuiltInProviderDescriptor(item));

            var instance = descriptor.CreateInstance!(new TestServiceProvider());

            Assert.Equal(descriptor.Type, instance.GetType());
            Assert.Empty(descriptor.Parameters);
            Assert.Empty(descriptor.Injectables);
        }
    }

    [Fact]
    public void FormsProvider_ExposesWorkingDataAnnotationsValidatorDescriptor()
    {
        var result = RunGeneratorForProviders(
            "Microsoft.AspNetCore.Components",
            "Microsoft.AspNetCore.Components.Forms");
        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            var descriptor = Assert.Single(
                context.Components,
                item => item.Type.FullName ==
                    "Microsoft.AspNetCore.Components.Forms.DataAnnotationsValidator" &&
                    IsBuiltInProviderDescriptor(item));
            var validator = Assert.IsType<Microsoft.AspNetCore.Components.Forms.DataAnnotationsValidator>(
                descriptor.CreateInstance!(new TestServiceProvider()));

            var parameter = Assert.Single(descriptor.Parameters);
            Assert.Equal("CurrentEditContext", parameter.Name);
            Assert.Equal(typeof(Microsoft.AspNetCore.Components.Forms.EditContext), parameter.ParameterType);
            Assert.IsType<Microsoft.AspNetCore.Components.CascadingParameterAttribute>(parameter.Attribute);

            var editContext = new Microsoft.AspNetCore.Components.Forms.EditContext(new object());
            parameter.SetValue(validator, editContext);
            Assert.Same(editContext, parameter.GetValue(validator));
            parameter.SetValue(validator, null);
            Assert.Null(parameter.GetValue(validator));

            var injectable = Assert.Single(descriptor.Injectables);
            Assert.Equal("ServiceProvider", injectable.Name);
            Assert.Equal(typeof(IServiceProvider), injectable.ServiceType);
            Assert.IsType<Microsoft.AspNetCore.Components.InjectAttribute>(injectable.Attribute);

            var serviceProvider = new TestServiceProvider();
            injectable.SetValue(validator, serviceProvider);
            Assert.Same(serviceProvider, GetNonPublicProperty(validator, "ServiceProvider"));
        }
    }

    [Fact]
    public void EndpointsProvider_ExposesTypeOnlySsrRenderModeBoundaryDescriptor()
    {
        var result = RunGeneratorForProviders(
            "Microsoft.AspNetCore.Components",
            "Microsoft.AspNetCore.Components.Endpoints",
            "Microsoft.AspNetCore.Http.Abstractions");
        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            var descriptor = Assert.Single(
                context.Components,
                item => item.Type.FullName ==
                    "Microsoft.AspNetCore.Components.Endpoints.SSRRenderModeBoundary");

            Assert.Null(descriptor.CreateInstance);
            Assert.Empty(descriptor.Parameters);
            Assert.Empty(descriptor.Injectables);
        }
    }

    [Fact]
    public void WebProvider_ExposesWorkingClientValidationDataDescriptor()
    {
        var result = RunGeneratorForProviders(
            "Microsoft.AspNetCore.Components",
            "Microsoft.AspNetCore.Components.Forms",
            "Microsoft.AspNetCore.Components.Web");
        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            var descriptor = Assert.Single(
                context.Components,
                item => item.Type.FullName ==
                    "Microsoft.AspNetCore.Components.Forms.ClientValidationData" &&
                    IsBuiltInProviderDescriptor(item));
            var instance = descriptor.CreateInstance!(new TestServiceProvider());

            Assert.Equal(descriptor.Type, instance.GetType());

            var parameter = Assert.Single(descriptor.Parameters);
            Assert.Equal("CurrentEditContext", parameter.Name);
            Assert.Equal(typeof(Microsoft.AspNetCore.Components.Forms.EditContext), parameter.ParameterType);
            Assert.IsType<Microsoft.AspNetCore.Components.CascadingParameterAttribute>(parameter.Attribute);

            var editContext = new Microsoft.AspNetCore.Components.Forms.EditContext(new object());
            parameter.SetValue(instance, editContext);
            Assert.Same(editContext, parameter.GetValue(instance));
            parameter.SetValue(instance, null);
            Assert.Null(parameter.GetValue(instance));

            var injectable = Assert.Single(descriptor.Injectables);
            Assert.Equal("Services", injectable.Name);
            Assert.Equal(typeof(IServiceProvider), injectable.ServiceType);
            Assert.IsType<Microsoft.AspNetCore.Components.InjectAttribute>(injectable.Attribute);

            var services = new TestServiceProvider();
            injectable.SetValue(instance, services);
            Assert.Same(services, GetNonPublicProperty(instance, "Services"));
        }
    }

    [Fact]
    public void ValidationMessageFactory_ExposesClosedWorkingDescriptor()
    {
        var result = RunGeneratorForComponent(
            "Microsoft.AspNetCore.Components.Forms.ValidationMessage<string>");
        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            var descriptor = Assert.Single(
                context.Components,
                item => item.Type == typeof(Microsoft.AspNetCore.Components.Forms.ValidationMessage<string>) &&
                    IsBuiltInProviderDescriptor(item));
            var validationMessage =
                Assert.IsType<Microsoft.AspNetCore.Components.Forms.ValidationMessage<string>>(
                    descriptor.CreateInstance!(new TestServiceProvider()));
            var fieldPrefixType = typeof(Microsoft.AspNetCore.Components.Forms.ValidationMessage<string>)
                .Assembly
                .GetType("Microsoft.AspNetCore.Components.Forms.HtmlFieldPrefix", throwOnError: true)!;

            var expectedParameterTypes = new Dictionary<string, Type>
            {
                ["AdditionalAttributes"] = typeof(IReadOnlyDictionary<string, object>),
                ["CurrentEditContext"] = typeof(Microsoft.AspNetCore.Components.Forms.EditContext),
                ["FieldPrefix"] = fieldPrefixType,
                ["For"] = typeof(System.Linq.Expressions.Expression<Func<string>>),
            };
            Assert.Equal(expectedParameterTypes.Count, descriptor.Parameters.Count);
            foreach (var parameter in descriptor.Parameters)
            {
                Assert.Equal(expectedParameterTypes[parameter.Name], parameter.ParameterType);
            }

            var additionalAttributes = Assert.Single(
                descriptor.Parameters,
                item => item.Name == "AdditionalAttributes");
            var additionalAttributesAttribute =
                Assert.IsType<Microsoft.AspNetCore.Components.ParameterAttribute>(
                    additionalAttributes.Attribute);
            Assert.True(additionalAttributesAttribute.CaptureUnmatchedValues);
            IReadOnlyDictionary<string, object> attributes =
                new Dictionary<string, object> { ["class"] = "validation" };
            additionalAttributes.SetValue(validationMessage, attributes);
            Assert.Same(attributes, additionalAttributes.GetValue(validationMessage));

            var currentEditContext = Assert.Single(
                descriptor.Parameters,
                item => item.Name == "CurrentEditContext");
            Assert.IsType<Microsoft.AspNetCore.Components.CascadingParameterAttribute>(
                currentEditContext.Attribute);
            var editContext = new Microsoft.AspNetCore.Components.Forms.EditContext(new object());
            currentEditContext.SetValue(validationMessage, editContext);
            Assert.Same(editContext, currentEditContext.GetValue(validationMessage));

            var fieldPrefix = Assert.Single(descriptor.Parameters, item => item.Name == "FieldPrefix");
            Assert.IsType<Microsoft.AspNetCore.Components.CascadingParameterAttribute>(
                fieldPrefix.Attribute);
            string fieldValue = "value";
            System.Linq.Expressions.Expression<Func<string>> fieldExpression = () => fieldValue;
            var prefix = Activator.CreateInstance(
                fieldPrefix.ParameterType,
                System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic,
                binder: null,
                args: [fieldExpression],
                culture: null)!;
            fieldPrefix.SetValue(validationMessage, prefix);
            Assert.Same(prefix, fieldPrefix.GetValue(validationMessage));
            fieldPrefix.SetValue(validationMessage, null);
            Assert.Null(fieldPrefix.GetValue(validationMessage));

            var forParameter = Assert.Single(descriptor.Parameters, item => item.Name == "For");
            var forAttribute = Assert.IsType<Microsoft.AspNetCore.Components.ParameterAttribute>(
                forParameter.Attribute);
            Assert.False(forAttribute.CaptureUnmatchedValues);
            System.Linq.Expressions.Expression<Func<string>> forExpression = () => fieldValue;
            forParameter.SetValue(validationMessage, forExpression);
            Assert.Same(forExpression, forParameter.GetValue(validationMessage));
        }
    }

    [Fact]
    public void VirtualizeFactory_ExposesClosedWorkingDescriptor()
    {
        var result = RunGeneratorForComponent(
            "Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize<string>");
        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            var descriptor = Assert.Single(
                context.Components,
                item => item.Type ==
                    typeof(Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize<string>) &&
                    IsBuiltInProviderDescriptor(item));
            var virtualize =
                Assert.IsType<Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize<string>>(
                    descriptor.CreateInstance!(new TestServiceProvider()));

            var expectedParameterTypes = new Dictionary<string, Type>
            {
                ["ChildContent"] = typeof(Microsoft.AspNetCore.Components.RenderFragment<string>),
                ["ItemContent"] = typeof(Microsoft.AspNetCore.Components.RenderFragment<string>),
                ["Placeholder"] = typeof(Microsoft.AspNetCore.Components.RenderFragment<
                    Microsoft.AspNetCore.Components.Web.Virtualization.PlaceholderContext>),
                ["EmptyContent"] = typeof(Microsoft.AspNetCore.Components.RenderFragment),
                ["ItemSize"] = typeof(float),
                ["ItemsProvider"] = typeof(Microsoft.AspNetCore.Components.Web.Virtualization.ItemsProviderDelegate<string>),
                ["Items"] = typeof(ICollection<string>),
                ["OverscanCount"] = typeof(int),
                ["SpacerElement"] = typeof(string),
                ["MaxItemCount"] = typeof(int),
                ["AnchorMode"] = typeof(Microsoft.AspNetCore.Components.Web.Virtualization.VirtualizeAnchorMode),
                ["ItemComparer"] = typeof(IEqualityComparer<string>),
                ["InitialItemIndex"] = typeof(int),
            };
            Assert.Equal(expectedParameterTypes.Count, descriptor.Parameters.Count);
            foreach (var parameter in descriptor.Parameters)
            {
                Assert.Equal(expectedParameterTypes[parameter.Name], parameter.ParameterType);
                var attribute = Assert.IsType<Microsoft.AspNetCore.Components.ParameterAttribute>(
                    parameter.Attribute);
                Assert.False(attribute.CaptureUnmatchedValues);
            }

            Microsoft.AspNetCore.Components.RenderFragment<string> childContent =
                static _ => static _ => { };
            Microsoft.AspNetCore.Components.RenderFragment<string> itemContent =
                static _ => static _ => { };
            Microsoft.AspNetCore.Components.RenderFragment<
                Microsoft.AspNetCore.Components.Web.Virtualization.PlaceholderContext> placeholder =
                    static _ => static _ => { };
            Microsoft.AspNetCore.Components.RenderFragment emptyContent = static _ => { };
            Microsoft.AspNetCore.Components.Web.Virtualization.ItemsProviderDelegate<string>
                itemsProvider = static _ => ValueTask.FromResult(
                    new Microsoft.AspNetCore.Components.Web.Virtualization.ItemsProviderResult<string>(
                        ["provided"],
                        1));
            ICollection<string> items = new List<string> { "one", "two" };
            var comparer = StringComparer.OrdinalIgnoreCase;
            var parameterValues = new Dictionary<string, object>
            {
                ["ChildContent"] = childContent,
                ["ItemContent"] = itemContent,
                ["Placeholder"] = placeholder,
                ["EmptyContent"] = emptyContent,
                ["ItemSize"] = 37.5f,
                ["ItemsProvider"] = itemsProvider,
                ["Items"] = items,
                ["OverscanCount"] = 7,
                ["SpacerElement"] = "tr",
                ["MaxItemCount"] = 25,
                ["AnchorMode"] =
                    Microsoft.AspNetCore.Components.Web.Virtualization.VirtualizeAnchorMode.End,
                ["ItemComparer"] = comparer,
                ["InitialItemIndex"] = 12,
            };
            foreach (var parameter in descriptor.Parameters)
            {
                var expected = parameterValues[parameter.Name];
                parameter.SetValue(virtualize, expected);
                Assert.Equal(expected, parameter.GetValue(virtualize));
            }

            var itemsProviderParameter = Assert.Single(
                descriptor.Parameters,
                item => item.Name == "ItemsProvider");
            itemsProviderParameter.SetValue(virtualize, null);
            Assert.Null(itemsProviderParameter.GetValue(virtualize));

            var injectable = Assert.Single(descriptor.Injectables);
            Assert.Equal("JSRuntime", injectable.Name);
            Assert.Equal(typeof(Microsoft.JSInterop.IJSRuntime), injectable.ServiceType);
            Assert.IsType<Microsoft.AspNetCore.Components.InjectAttribute>(injectable.Attribute);
            var jsRuntime = new TestJSRuntime();
            injectable.SetValue(virtualize, jsRuntime);
            Assert.Same(jsRuntime, GetNonPublicProperty(virtualize, "JSRuntime"));
        }
    }

    private static GeneratorTestResult RunGeneratorForComponent(string componentType)
        => RunGenerator(
            "namespace TestComponents;",
            $$"""
            namespace TestHost;

            [Microsoft.AspNetCore.Components.Web.ComponentTypeInfo(
                typeof({{componentType}}))]
            public sealed partial class TestMetadata : Microsoft.AspNetCore.Components.Web.RazorComponentsMetadataContext
            {
            }
            """,
            hostFrameworkAssemblyNames:
            [
                "Microsoft.AspNetCore.Components",
                "Microsoft.AspNetCore.Components.Forms",
                "Microsoft.AspNetCore.Components.Web",
            ]);

    private static GeneratorTestResult RunGeneratorForProviders(params string[] assemblyNames)
        => RunGenerator(
            "namespace TestComponents;",
            hostFrameworkAssemblyNames:
            [
                .. assemblyNames,
                "Microsoft.AspNetCore.Components.Forms",
                "Microsoft.AspNetCore.Components.Web",
            ]);

    private static object? GetNonPublicProperty(object target, string name)
        => target.GetType()
            .GetProperty(
                name,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(target);

    private static bool IsBuiltInProviderDescriptor(
        Microsoft.AspNetCore.Components.Infrastructure.ComponentDescriptor descriptor)
        => descriptor.CreateInstance?.Method.DeclaringType?.Assembly == descriptor.Type.Assembly;

    private sealed class TestNavigationManager : Microsoft.AspNetCore.Components.NavigationManager
    {
        public TestNavigationManager()
            => Initialize("https://example.com/", "https://example.com/");
    }

    private sealed class TestNavigationInterception :
        Microsoft.AspNetCore.Components.Routing.INavigationInterception
    {
        public Task EnableNavigationInterceptionAsync() => Task.CompletedTask;
    }

    private sealed class TestScrollToLocationHash :
        Microsoft.AspNetCore.Components.Routing.IScrollToLocationHash
    {
        public Task RefreshScrollPositionForHash(string locationAbsolute) => Task.CompletedTask;
    }

    private sealed class TestJSRuntime : Microsoft.JSInterop.IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => throw new NotSupportedException();

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args)
            => throw new NotSupportedException();
    }

    private sealed class TestServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    [Theory]
    [InlineData("Microsoft.AspNetCore.Components")]
    [InlineData("Microsoft.AspNetCore.Components.Web")]
    [InlineData("Microsoft.AspNetCore.Components.Forms")]
    [InlineData("Microsoft.AspNetCore.Components.Authorization")]
    [InlineData("Microsoft.AspNetCore.Components.Endpoints")]
    [InlineData("Microsoft.AspNetCore.Components.QuickGrid")]
    [InlineData("Microsoft.AspNetCore.Components.Media")]
    [InlineData("Microsoft.AspNetCore.Components.WebAssembly.Authentication")]
    [InlineData(null)]
    public void KnownAssemblies_EmitOnlyReferencedProviderAccessors(string? providerAssembly)
    {
        string[] knownProviderAssemblies =
        [
            "Microsoft.AspNetCore.Components",
            "Microsoft.AspNetCore.Components.Web",
            "Microsoft.AspNetCore.Components.Forms",
            "Microsoft.AspNetCore.Components.Authorization",
            "Microsoft.AspNetCore.Components.Endpoints",
            "Microsoft.AspNetCore.Components.QuickGrid",
            "Microsoft.AspNetCore.Components.Media",
            "Microsoft.AspNetCore.Components.WebAssembly.Authentication",
        ];
        var expectedProviderAssemblies = providerAssembly is null
            ? knownProviderAssemblies.ToHashSet(StringComparer.Ordinal)
            : new HashSet<string>(
                [
                    "Microsoft.AspNetCore.Components",
                    "Microsoft.AspNetCore.Components.Web",
                    providerAssembly,
                ],
                StringComparer.Ordinal);
        var frameworkAssemblyNames = expectedProviderAssemblies
            .Append("Microsoft.JSInterop")
            .ToArray();

        var result = RunGenerator(
            "namespace TestComponents;",
            hostFrameworkAssemblyNames: frameworkAssemblyNames);

        var providerAssemblies = GetProviderAssemblies(GetGeneratedSource(result));

        foreach (var knownProviderAssembly in knownProviderAssemblies)
        {
            Assert.Equal(
                expectedProviderAssemblies.Contains(knownProviderAssembly) ? 1 : 0,
                providerAssemblies.Count(assembly => assembly == knownProviderAssembly));
        }

        Assert.DoesNotContain("Microsoft.JSInterop", providerAssemblies);
        Assert.Equal(
            expectedProviderAssemblies.Order(),
            providerAssemblies.Distinct(StringComparer.Ordinal).Order());
    }

    private static string[] GetProviderAssemblies(string generatedSource)
        => Regex.Matches(
                generatedSource,
                $"""GetBuiltInComponentDescriptors_\d+\(\s*\[global::System\.Runtime\.CompilerServices\.UnsafeAccessorType\("{Regex.Escape(BuiltInDescriptorProviderType)}, ([^"]+)"\)\]""")
            .Select(match => match.Groups[1].Value)
            .ToArray();

    [Theory]
    [InlineData(
        "Microsoft.AspNetCore.Components.Forms.InputDate<int>",
        "Microsoft.AspNetCore.Components.Web",
        "CreateInputDateDescriptors",
        "Microsoft.AspNetCore.Components.Forms.InputDate`1",
        "System.Int32",
        "int",
        "-1",
        null)]
    [InlineData(
        "Microsoft.AspNetCore.Components.Forms.InputNumber<int>",
        "Microsoft.AspNetCore.Components.Web",
        "CreateInputNumberDescriptors",
        "Microsoft.AspNetCore.Components.Forms.InputNumber`1",
        "System.Int32",
        "int",
        "-1",
        null)]
    [InlineData(
        "Microsoft.AspNetCore.Components.Forms.InputRadio<int>",
        "Microsoft.AspNetCore.Components.Web",
        "CreateInputRadioDescriptors",
        "Microsoft.AspNetCore.Components.Forms.InputRadio`1",
        "System.Int32",
        "int",
        "-1",
        null)]
    [InlineData(
        "Microsoft.AspNetCore.Components.Forms.InputRadioGroup<int>",
        "Microsoft.AspNetCore.Components.Web",
        "CreateInputRadioGroupDescriptors",
        "Microsoft.AspNetCore.Components.Forms.InputRadioGroup`1",
        "System.Int32",
        "int",
        "-1",
        null)]
    [InlineData(
        "Microsoft.AspNetCore.Components.Forms.InputSelect<int>",
        "Microsoft.AspNetCore.Components.Web",
        "CreateInputSelectDescriptors",
        "Microsoft.AspNetCore.Components.Forms.InputSelect`1",
        "System.Int32",
        "int",
        "-1",
        null)]
    [InlineData(
        "Microsoft.AspNetCore.Components.Forms.Label<int>",
        "Microsoft.AspNetCore.Components.Web",
        "CreateLabelDescriptors",
        "Microsoft.AspNetCore.Components.Forms.Label`1",
        "System.Int32",
        "int",
        "0",
        null)]
    [InlineData(
        "Microsoft.AspNetCore.Components.Forms.ValidationMessage<int>",
        "Microsoft.AspNetCore.Components.Web",
        "CreateValidationMessageDescriptors",
        "Microsoft.AspNetCore.Components.Forms.ValidationMessage`1",
        "System.Int32",
        "int",
        "0",
        null)]
    [InlineData(
        "Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize<int>",
        "Microsoft.AspNetCore.Components.Web",
        "CreateVirtualizeDescriptors",
        "Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize`1",
        "System.Int32",
        "int",
        "0",
        null)]
    [InlineData(
        "Microsoft.AspNetCore.Components.QuickGrid.QuickGrid<int>",
        "Microsoft.AspNetCore.Components.QuickGrid",
        "CreateQuickGridDescriptors",
        "Microsoft.AspNetCore.Components.QuickGrid.QuickGrid`1",
        "System.Int32",
        "int",
        "0",
        null)]
    [InlineData(
        "Microsoft.AspNetCore.Components.QuickGrid.PropertyColumn<string, int>",
        "Microsoft.AspNetCore.Components.QuickGrid",
        "CreatePropertyColumnDescriptors",
        "Microsoft.AspNetCore.Components.QuickGrid.PropertyColumn`2",
        "System.String|System.Int32",
        "string, int",
        "0,0",
        null)]
    [InlineData(
        "Microsoft.AspNetCore.Components.QuickGrid.TemplateColumn<string>",
        "Microsoft.AspNetCore.Components.QuickGrid",
        "CreateTemplateColumnDescriptors",
        "Microsoft.AspNetCore.Components.QuickGrid.TemplateColumn`1",
        "System.String",
        "string",
        "0",
        null)]
    [InlineData(
        "Microsoft.AspNetCore.Components.QuickGrid.Infrastructure.ColumnsCollectedNotifier<string>",
        "Microsoft.AspNetCore.Components.QuickGrid",
        "CreateColumnsCollectedNotifierDescriptors",
        "Microsoft.AspNetCore.Components.QuickGrid.Infrastructure.ColumnsCollectedNotifier`1",
        "System.String",
        "string",
        "0",
        null)]
    [InlineData(
        "Microsoft.AspNetCore.Components.WebAssembly.Authentication.RemoteAuthenticatorViewCore<Microsoft.AspNetCore.Components.WebAssembly.Authentication.RemoteAuthenticationState>",
        "Microsoft.AspNetCore.Components.WebAssembly.Authentication",
        "CreateRemoteAuthenticatorViewCoreDescriptors",
        "Microsoft.AspNetCore.Components.WebAssembly.Authentication.RemoteAuthenticatorViewCore`1",
        "Microsoft.AspNetCore.Components.WebAssembly.Authentication.RemoteAuthenticationState",
        "global::Microsoft.AspNetCore.Components.WebAssembly.Authentication.RemoteAuthenticationState",
        "547",
        "where T0 : global::Microsoft.AspNetCore.Components.WebAssembly.Authentication.RemoteAuthenticationState")]
    public void ExplicitKnownGenericComponent_EmitsExpectedBuiltInFactory(
        string componentType,
        string providerAssembly,
        string factoryName,
        string expectedGenericDefinition,
        string expectedTypeArguments,
        string expectedInvocationTypeArguments,
        string expectedDamValues,
        string? expectedConstraint)
    {
        var result = RunGeneratorForGenericMapping("namespace TestComponents;", componentType);
        var source = GetGeneratedSource(result);

        AssertFactoryAccessor(
            source,
            providerAssembly,
            factoryName,
            expectedInvocationTypeArguments,
            expectedDamValues,
            expectedConstraint);

        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            var expectedArguments = expectedTypeArguments.Split('|');
            var descriptor = Assert.Single(
                context.Components,
                item => IsClosedType(item.Type, expectedGenericDefinition, expectedArguments) &&
                    IsExpectedMappingDescriptor(item, factoryName));

            Assert.Equal(expectedGenericDefinition, descriptor.Type.GetGenericTypeDefinition().FullName);
            Assert.Equal(expectedArguments, descriptor.Type.GetGenericArguments().Select(type => type.FullName));
            if (descriptor.CreateInstance is not null)
            {
                Assert.Equal(
                    descriptor.Type,
                    descriptor.CreateInstance(new TestServiceProvider()).GetType());
            }
        }
    }

    public static TheoryData<
        string,
        string,
        string,
        string,
        string,
        string?,
        string,
        string> InheritedGenericMappings => new()
        {
            {
                """
                namespace TestComponents;

                public sealed class CustomOwning :
                    Microsoft.AspNetCore.Components.OwningComponentBase<object>
                {
                }
                """,
                "Microsoft.AspNetCore.Components",
                "CreateOwningComponentBaseDescriptors",
                "global::TestComponents.CustomOwning",
                "-1",
                null,
                "TestComponents.CustomOwning",
                "ScopeFactory"
            },
            {
                """
                namespace TestComponents;

                public sealed class CustomInput :
                    Microsoft.AspNetCore.Components.Forms.InputBase<string>
                {
                    protected override bool TryParseValueFromString(
                        string? value,
                        out string result,
                        out string? validationErrorMessage)
                    {
                        result = value ?? string.Empty;
                        validationErrorMessage = null;
                        return true;
                    }
                }
                """,
                "Microsoft.AspNetCore.Components.Web",
                "CreateInputBaseDescriptors",
                "global::TestComponents.CustomInput, string",
                "-1,0",
                "where T0 : global::Microsoft.AspNetCore.Components.Forms.InputBase<T1>",
                "TestComponents.CustomInput",
                "CascadedEditContext|FieldPrefix"
            },
            {
                """
                namespace TestComponents;

                public sealed class CustomEditor :
                    Microsoft.AspNetCore.Components.Forms.Editor<string>
                {
                }
                """,
                "Microsoft.AspNetCore.Components.Web",
                "CreateEditorDescriptors",
                "global::TestComponents.CustomEditor, string",
                "-1,0",
                "where T0 : global::Microsoft.AspNetCore.Components.Forms.Editor<T1>",
                "TestComponents.CustomEditor",
                "FieldPrefix"
            },
            {
                """
                namespace TestComponents;

                public sealed class CustomColumn :
                    Microsoft.AspNetCore.Components.QuickGrid.ColumnBase<string>
                {
                    public override Microsoft.AspNetCore.Components.QuickGrid.GridSort<string>? SortBy
                    {
                        get;
                        set;
                    }

                    protected override void CellContent(
                        Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder,
                        string item)
                    {
                    }
                }
                """,
                "Microsoft.AspNetCore.Components.QuickGrid",
                "CreateColumnBaseDescriptors",
                "string, global::TestComponents.CustomColumn",
                "0,-1",
                "where T1 : global::Microsoft.AspNetCore.Components.QuickGrid.ColumnBase<T0>",
                "TestComponents.CustomColumn",
                "InternalGridContext"
            },
        };

    [Theory]
    [MemberData(nameof(InheritedGenericMappings))]
    public void InheritedComponent_EmitsExpectedBuiltInFactory(
        string referencedSource,
        string providerAssembly,
        string factoryName,
        string expectedInvocationTypeArguments,
        string expectedDamValues,
        string? expectedConstraint,
        string expectedTypeName,
        string expectedSupplementalMembers)
    {
        var result = RunGeneratorForGenericMapping(referencedSource);
        var source = GetGeneratedSource(result);

        AssertFactoryAccessor(
            source,
            providerAssembly,
            factoryName,
            expectedInvocationTypeArguments,
            expectedDamValues,
            expectedConstraint);

        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            var expectedMembers = expectedSupplementalMembers.Split('|');
            var descriptor = Assert.Single(
                context.Components,
                item => item.Type.FullName == expectedTypeName &&
                    item.CreateInstance is null &&
                    expectedMembers.All(member =>
                        item.Parameters.Any(parameter => parameter.Name == member) ||
                        item.Injectables.Any(injectable => injectable.Name == member)));

            foreach (var member in expectedMembers)
            {
                var parameter = descriptor.Parameters.SingleOrDefault(item => item.Name == member);
                if (parameter is not null)
                {
                    Assert.IsType<Microsoft.AspNetCore.Components.CascadingParameterAttribute>(
                        parameter.Attribute);
                }
                else
                {
                    var injectable = Assert.Single(
                        descriptor.Injectables,
                        item => item.Name == member);
                    Assert.IsType<Microsoft.AspNetCore.Components.InjectAttribute>(
                        injectable.Attribute);
                }
            }

            Assert.Null(descriptor.CreateInstance);
        }
    }

    private static GeneratorTestResult RunGeneratorForGenericMapping(
        string referencedSource,
        string? componentType = null)
        => RunGenerator(
            referencedSource,
            componentType is null
                ? DefaultHostSource
                : $$"""
                  namespace TestHost;

                  [Microsoft.AspNetCore.Components.Web.ComponentTypeInfo(
                      typeof({{componentType}}))]
                  public sealed partial class TestMetadata :
                      Microsoft.AspNetCore.Components.Web.RazorComponentsMetadataContext
                  {
                  }
                  """,
            hostFrameworkAssemblyNames:
            [
                "Microsoft.AspNetCore.Components",
                "Microsoft.AspNetCore.Components.Forms",
                "Microsoft.AspNetCore.Components.Web",
                "Microsoft.AspNetCore.Components.QuickGrid",
                "Microsoft.AspNetCore.Components.WebAssembly.Authentication",
            ]);

    private static void AssertFactoryAccessor(
        string source,
        string providerAssembly,
        string factoryName,
        string expectedInvocationTypeArguments,
        string expectedDamValues,
        string? expectedConstraint)
    {
        var accessor = Assert.Single(
            Regex.Matches(
                    source,
                    $$"""
                    \[global::System\.Runtime\.CompilerServices\.UnsafeAccessor\(
                    global::System\.Runtime\.CompilerServices\.UnsafeAccessorKind\.StaticMethod,\ Name\ =\ "{{Regex.Escape(factoryName)}}"\)\]
                    \s*(?<declaration>private\ static\ extern\ [^;]+;)
                    """,
                    RegexOptions.IgnorePatternWhitespace)
                .Cast<Match>());
        var declaration = accessor.Groups["declaration"].Value;
        var factoryIndex = Assert.Single(
            Regex.Matches(declaration, @"GetBuiltInComponentDescriptorFactory_(\d+)")
                .Cast<Match>())
            .Groups[1]
            .Value;

        Assert.Contains(
            $"UnsafeAccessorType(\"{BuiltInDescriptorProviderType}, {providerAssembly}\")",
            declaration);
        Assert.Contains(
            $"GetBuiltInComponentDescriptorFactory_{factoryIndex}<{expectedInvocationTypeArguments}>(null)",
            source);

        var damValues = expectedDamValues.Split(',').Select(int.Parse).ToArray();
        for (var i = 0; i < damValues.Length; i++)
        {
            var damFragment =
                $"[global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(" +
                $"(global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes)" +
                $"({damValues[i]}))] T{i}";
            if (damValues[i] == 0)
            {
                Assert.DoesNotMatch(
                    $@"DynamicallyAccessedMembers\([^\]]+\)\]\s*T{i}\b",
                    declaration);
                Assert.Matches($@"\bT{i}\b", declaration);
            }
            else
            {
                Assert.Contains(damFragment, declaration);
            }
        }

        if (expectedConstraint is null)
        {
            Assert.DoesNotContain(" where T", declaration);
        }
        else
        {
            Assert.Contains(expectedConstraint, declaration);
        }
    }

    private static bool IsClosedType(
        Type type,
        string expectedGenericDefinition,
        string[] expectedTypeArguments)
        => type.IsGenericType &&
            type.GetGenericTypeDefinition().FullName == expectedGenericDefinition &&
            type.GetGenericArguments().Select(argument => argument.FullName)
                .SequenceEqual(expectedTypeArguments);

    private static bool IsExpectedMappingDescriptor(
        Microsoft.AspNetCore.Components.Infrastructure.ComponentDescriptor descriptor,
        string factoryName)
        => factoryName switch
        {
            "CreateInputDateDescriptors" or
            "CreateInputNumberDescriptors" or
            "CreateInputSelectDescriptors" =>
                descriptor.Parameters.Select(parameter => parameter.Name)
                    .Order()
                    .SequenceEqual(new[] { "CascadedEditContext", "FieldPrefix" }),
            "CreateInputRadioDescriptors" =>
                descriptor.Parameters.Select(parameter => parameter.Name)
                    .SequenceEqual(new[] { "CascadedContext" }),
            "CreateInputRadioGroupDescriptors" =>
                descriptor.Parameters.Select(parameter => parameter.Name)
                    .Order()
                    .SequenceEqual(new[] { "CascadedContext", "CascadedEditContext", "FieldPrefix" }),
            "CreateLabelDescriptors" =>
                descriptor.Parameters.Select(parameter => parameter.Name)
                    .SequenceEqual(new[] { "FieldPrefix" }),
            "CreatePropertyColumnDescriptors" or
            "CreateTemplateColumnDescriptors" or
            "CreateColumnsCollectedNotifierDescriptors" =>
                descriptor.Parameters.Any(parameter => parameter.Name == "InternalGridContext"),
            "CreateRemoteAuthenticatorViewCoreDescriptors" =>
                descriptor.Injectables.Count == 5 &&
                descriptor.Injectables.Any(injectable => injectable.Name == "AuthenticationService"),
            _ => IsBuiltInProviderDescriptor(descriptor),
        };

    [Fact]
    public void AuthorizationProvider_ExposesExactDescriptorSet()
    {
        var result = RunGeneratorForProviders(
            "Microsoft.AspNetCore.Components",
            "Microsoft.AspNetCore.Components.Authorization");
        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            var descriptors = GetAuthorizationProviderDescriptors(context.Components);
            Assert.Equal(
                [
                    "Microsoft.AspNetCore.Components.Authorization.AuthorizeRouteView",
                    "Microsoft.AspNetCore.Components.Authorization.AuthorizeRouteView+AuthorizeRouteViewCore",
                    "Microsoft.AspNetCore.Components.Authorization.AuthorizeView",
                    "Microsoft.AspNetCore.Components.Authorization.CascadingAuthenticationState",
                ],
                descriptors.Select(descriptor => descriptor.Type.FullName).Order());

            AssertDescriptorShape(
                FindDescriptor(descriptors, "Microsoft.AspNetCore.Components.Authorization.AuthorizeView"),
                false,
                [("AuthenticationState", typeof(Task<Microsoft.AspNetCore.Components.Authorization.AuthenticationState>), typeof(Microsoft.AspNetCore.Components.CascadingParameterAttribute))],
                [
                    ("AuthorizationPolicyProvider", typeof(Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider)),
                    ("AuthorizationService", typeof(Microsoft.AspNetCore.Authorization.IAuthorizationService)),
                ]);
            AssertDescriptorShape(
                FindDescriptor(descriptors, "Microsoft.AspNetCore.Components.Authorization.AuthorizeRouteView"),
                false,
                [("ExistingCascadedAuthenticationState", typeof(Task<Microsoft.AspNetCore.Components.Authorization.AuthenticationState>), typeof(Microsoft.AspNetCore.Components.CascadingParameterAttribute))],
                []);
            AssertDescriptorShape(
                FindDescriptor(descriptors, "Microsoft.AspNetCore.Components.Authorization.CascadingAuthenticationState"),
                false,
                [],
                [("AuthenticationStateProvider", typeof(Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider))]);
            AssertDescriptorShape(
                FindDescriptor(descriptors, "Microsoft.AspNetCore.Components.Authorization.AuthorizeRouteView+AuthorizeRouteViewCore"),
                true,
                [
                    ("ChildContent", typeof(Microsoft.AspNetCore.Components.RenderFragment<Microsoft.AspNetCore.Components.Authorization.AuthenticationState>), typeof(Microsoft.AspNetCore.Components.ParameterAttribute)),
                    ("NotAuthorized", typeof(Microsoft.AspNetCore.Components.RenderFragment<Microsoft.AspNetCore.Components.Authorization.AuthenticationState>), typeof(Microsoft.AspNetCore.Components.ParameterAttribute)),
                    ("Authorized", typeof(Microsoft.AspNetCore.Components.RenderFragment<Microsoft.AspNetCore.Components.Authorization.AuthenticationState>), typeof(Microsoft.AspNetCore.Components.ParameterAttribute)),
                    ("Authorizing", typeof(Microsoft.AspNetCore.Components.RenderFragment), typeof(Microsoft.AspNetCore.Components.ParameterAttribute)),
                    ("Resource", typeof(object), typeof(Microsoft.AspNetCore.Components.ParameterAttribute)),
                    ("RouteData", typeof(Microsoft.AspNetCore.Components.RouteData), typeof(Microsoft.AspNetCore.Components.ParameterAttribute)),
                    ("AuthenticationState", typeof(Task<Microsoft.AspNetCore.Components.Authorization.AuthenticationState>), typeof(Microsoft.AspNetCore.Components.CascadingParameterAttribute)),
                ],
                [
                    ("AuthorizationPolicyProvider", typeof(Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider)),
                    ("AuthorizationService", typeof(Microsoft.AspNetCore.Authorization.IAuthorizationService)),
                ]);
        }
    }

    [Fact]
    public void AuthorizationProvider_HiddenMembersAndPrivateCoreRoundTrip()
    {
        var result = RunGeneratorForProviders(
            "Microsoft.AspNetCore.Components",
            "Microsoft.AspNetCore.Components.Authorization");
        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            var descriptors = GetAuthorizationProviderDescriptors(context.Components);
            var authenticationState = Task.FromResult(
                new Microsoft.AspNetCore.Components.Authorization.AuthenticationState(
                    new System.Security.Claims.ClaimsPrincipal()));
            var policyProvider = new TestAuthorizationPolicyProvider();
            var authorizationService = new TestAuthorizationService();

            var authorizeViewDescriptor = FindDescriptor(
                descriptors,
                "Microsoft.AspNetCore.Components.Authorization.AuthorizeView");
            var authorizeView = new Microsoft.AspNetCore.Components.Authorization.AuthorizeView();
            SetAndAssertParameter(authorizeViewDescriptor, authorizeView, "AuthenticationState", authenticationState);
            SetAndAssertInjectable(authorizeViewDescriptor, authorizeView, "AuthorizationPolicyProvider", policyProvider);
            SetAndAssertInjectable(authorizeViewDescriptor, authorizeView, "AuthorizationService", authorizationService);

            var routeViewDescriptor = FindDescriptor(
                descriptors,
                "Microsoft.AspNetCore.Components.Authorization.AuthorizeRouteView");
            var routeView = new Microsoft.AspNetCore.Components.Authorization.AuthorizeRouteView();
            SetAndAssertParameter(
                routeViewDescriptor,
                routeView,
                "ExistingCascadedAuthenticationState",
                authenticationState);
            SetAndAssertParameter(
                routeViewDescriptor,
                routeView,
                "ExistingCascadedAuthenticationState",
                null);

            var cascadingDescriptor = FindDescriptor(
                descriptors,
                "Microsoft.AspNetCore.Components.Authorization.CascadingAuthenticationState");
            var cascading = Activator.CreateInstance(cascadingDescriptor.Type)!;
            var authenticationStateProvider = new TestAuthenticationStateProvider();
            SetAndAssertInjectable(
                cascadingDescriptor,
                cascading,
                "AuthenticationStateProvider",
                authenticationStateProvider);

            var coreDescriptor = FindDescriptor(
                descriptors,
                "Microsoft.AspNetCore.Components.Authorization.AuthorizeRouteView+AuthorizeRouteViewCore");
            var core = coreDescriptor.CreateInstance!(new TestServiceProvider());
            Assert.Equal(coreDescriptor.Type, core.GetType());

            Microsoft.AspNetCore.Components.RenderFragment<
                Microsoft.AspNetCore.Components.Authorization.AuthenticationState> childContent =
                    static _ => static _ => { };
            Microsoft.AspNetCore.Components.RenderFragment<
                Microsoft.AspNetCore.Components.Authorization.AuthenticationState> notAuthorized =
                    static _ => static _ => { };
            Microsoft.AspNetCore.Components.RenderFragment<
                Microsoft.AspNetCore.Components.Authorization.AuthenticationState> authorized =
                    static _ => static _ => { };
            Microsoft.AspNetCore.Components.RenderFragment authorizing = static _ => { };
            var resource = new object();
            var routeData = new Microsoft.AspNetCore.Components.RouteData(
                typeof(Microsoft.AspNetCore.Components.Authorization.AuthorizeView),
                new Dictionary<string, object?> { ["id"] = 17 });
            var values = new Dictionary<string, object?>
            {
                ["ChildContent"] = childContent,
                ["NotAuthorized"] = notAuthorized,
                ["Authorized"] = authorized,
                ["Authorizing"] = authorizing,
                ["Resource"] = resource,
                ["RouteData"] = routeData,
                ["AuthenticationState"] = authenticationState,
            };
            foreach (var parameter in coreDescriptor.Parameters)
            {
                parameter.SetValue(core, values[parameter.Name]);
                Assert.Same(values[parameter.Name], parameter.GetValue(core));
            }

            foreach (var nullableName in new[]
                     {
                         "ChildContent",
                         "NotAuthorized",
                         "Authorized",
                         "Authorizing",
                         "Resource",
                         "AuthenticationState",
                     })
            {
                SetAndAssertParameter(coreDescriptor, core, nullableName, null);
            }

            SetAndAssertParameter(coreDescriptor, core, "RouteData", routeData);
            SetAndAssertInjectable(coreDescriptor, core, "AuthorizationPolicyProvider", policyProvider);
            SetAndAssertInjectable(coreDescriptor, core, "AuthorizationService", authorizationService);
        }
    }

    [Theory]
    [InlineData(
        "Microsoft.AspNetCore.Components.ConfigureBrowser",
        "HttpContext:Microsoft.AspNetCore.Http.HttpContext:CascadingParameterAttribute",
        "")]
    [InlineData(
        "Microsoft.AspNetCore.Components.Endpoints.BasePath",
        "",
        "NavigationManager:Microsoft.AspNetCore.Components.NavigationManager")]
    [InlineData(
        "Microsoft.AspNetCore.Components.ResourcePreloader",
        "",
        "Service:Microsoft.AspNetCore.Components.Endpoints.ResourcePreloadService")]
    [InlineData(
        "Microsoft.AspNetCore.Components.CacheView",
        "CacheKey:System.String:ParameterAttribute|ChildContent:Microsoft.AspNetCore.Components.RenderFragment:ParameterAttribute|Enabled:System.Boolean:ParameterAttribute|ExpiresAfter:System.Nullable`1[[System.TimeSpan]]:ParameterAttribute|ExpiresOn:System.Nullable`1[[System.DateTimeOffset]]:ParameterAttribute|ExpiresSliding:System.Nullable`1[[System.TimeSpan]]:ParameterAttribute|HttpContext:Microsoft.AspNetCore.Http.HttpContext:CascadingParameterAttribute|VaryBy:System.String:ParameterAttribute|VaryByCookie:System.String:ParameterAttribute|VaryByCulture:System.Boolean:ParameterAttribute|VaryByHeader:System.String:ParameterAttribute|VaryByQuery:System.String:ParameterAttribute|VaryByRoute:System.String:ParameterAttribute|VaryByUser:System.Boolean:ParameterAttribute",
        "CacheService:Microsoft.AspNetCore.Components.Endpoints.CacheViewService")]
    [InlineData(
        "Microsoft.AspNetCore.Components.Endpoints.RazorComponentEndpointHost",
        "ComponentParameters:System.Collections.Generic.IReadOnlyDictionary`2[[System.String],[System.Object]]:ParameterAttribute|ComponentType:System.Type:ParameterAttribute",
        "")]
    public void EndpointsProvider_ExposesCompletedDescriptorShape(
        string typeName,
        string expectedParameters,
        string expectedInjectables)
    {
        var result = RunGeneratorForProviders(
            "Microsoft.AspNetCore.Components",
            "Microsoft.AspNetCore.Components.Endpoints",
            "Microsoft.AspNetCore.Http.Abstractions");
        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            var descriptor = Assert.Single(
                context.Components,
                item => item.Type.FullName == typeName && IsBuiltInProviderDescriptor(item));

            Assert.NotNull(descriptor.CreateInstance);
            Assert.Equal(
                ParseExpectedMembers(expectedParameters),
                descriptor.Parameters
                    .Select(parameter =>
                        $"{parameter.Name}:{GetStableTypeName(parameter.ParameterType)}:{parameter.Attribute.GetType().Name}")
                    .Order());
            Assert.Equal(
                ParseExpectedMembers(expectedInjectables),
                descriptor.Injectables
                    .Select(injectable =>
                        $"{injectable.Name}:{GetStableTypeName(injectable.ServiceType)}")
                    .Order());
            Assert.All(
                descriptor.Injectables,
                injectable => Assert.IsType<Microsoft.AspNetCore.Components.InjectAttribute>(
                    injectable.Attribute));
        }
    }

    [Fact]
    public void EndpointsProvider_HiddenMembersRoundTrip()
    {
        var result = RunGeneratorForProviders(
            "Microsoft.AspNetCore.Components",
            "Microsoft.AspNetCore.Components.Endpoints",
            "Microsoft.AspNetCore.Http.Abstractions");
        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            var configureBrowser = FindBuiltInDescriptor(
                context.Components,
                "Microsoft.AspNetCore.Components.ConfigureBrowser");
            var configureBrowserInstance = configureBrowser.CreateInstance!(new TestServiceProvider());
            var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
            SetAndAssertParameter(configureBrowser, configureBrowserInstance, "HttpContext", httpContext);
            SetAndAssertParameter(configureBrowser, configureBrowserInstance, "HttpContext", null);
            Assert.DoesNotContain(configureBrowser.Parameters, parameter => parameter.Name == "Options");

            var basePath = FindBuiltInDescriptor(
                context.Components,
                "Microsoft.AspNetCore.Components.Endpoints.BasePath");
            var basePathInstance = basePath.CreateInstance!(new TestServiceProvider());
            SetAndAssertInjectable(
                basePath,
                basePathInstance,
                "NavigationManager",
                new TestNavigationManager());

            var resourcePreloader = FindBuiltInDescriptor(
                context.Components,
                "Microsoft.AspNetCore.Components.ResourcePreloader");
            var resourcePreloaderInstance =
                resourcePreloader.CreateInstance!(new TestServiceProvider());
            var service = System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(
                Assert.Single(resourcePreloader.Injectables).ServiceType);
            SetAndAssertInjectable(resourcePreloader, resourcePreloaderInstance, "Service", service);
        }
    }

    [Fact]
    public void CacheViewDescriptor_AllParametersAndHiddenServiceRoundTrip()
    {
        var result = RunGeneratorForProviders(
            "Microsoft.AspNetCore.Components",
            "Microsoft.AspNetCore.Components.Endpoints",
            "Microsoft.AspNetCore.Http.Abstractions");
        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            var descriptor = FindBuiltInDescriptor(
                context.Components,
                "Microsoft.AspNetCore.Components.CacheView");
            var instance = descriptor.CreateInstance!(new TestServiceProvider());
            Microsoft.AspNetCore.Components.RenderFragment childContent = static _ => { };
            var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
            var values = new Dictionary<string, object?>
            {
                ["ChildContent"] = childContent,
                ["CacheKey"] = "phase-three",
                ["Enabled"] = false,
                ["ExpiresAfter"] = TimeSpan.FromMinutes(2),
                ["ExpiresOn"] = new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero),
                ["ExpiresSliding"] = TimeSpan.FromSeconds(30),
                ["VaryByQuery"] = "page",
                ["VaryByRoute"] = "id",
                ["VaryByHeader"] = "accept-language",
                ["VaryByCookie"] = "session",
                ["VaryByUser"] = true,
                ["VaryByCulture"] = true,
                ["VaryBy"] = "custom",
                ["HttpContext"] = httpContext,
            };
            Assert.Equal(14, descriptor.Parameters.Count);
            foreach (var parameter in descriptor.Parameters)
            {
                parameter.SetValue(instance, values[parameter.Name]);
                Assert.Equal(values[parameter.Name], parameter.GetValue(instance));
            }

            foreach (var nullableName in new[]
                     {
                         "ExpiresAfter",
                         "ExpiresOn",
                         "ExpiresSliding",
                         "VaryBy",
                         "HttpContext",
                     })
            {
                SetAndAssertParameter(descriptor, instance, nullableName, null);
            }

            var cacheService = System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(
                Assert.Single(descriptor.Injectables).ServiceType);
            SetAndAssertInjectable(descriptor, instance, "CacheService", cacheService);
        }
    }

    [Fact]
    public void RazorComponentEndpointHostDescriptor_ParametersRoundTrip()
    {
        var result = RunGeneratorForProviders(
            "Microsoft.AspNetCore.Components",
            "Microsoft.AspNetCore.Components.Endpoints");
        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            var descriptor = FindBuiltInDescriptor(
                context.Components,
                "Microsoft.AspNetCore.Components.Endpoints.RazorComponentEndpointHost");
            var instance = descriptor.CreateInstance!(new TestServiceProvider());
            IReadOnlyDictionary<string, object?> parameters =
                new Dictionary<string, object?> { ["Count"] = 3, ["Optional"] = null };

            SetAndAssertParameter(descriptor, instance, "ComponentType", typeof(TestServiceProvider));
            SetAndAssertParameter(descriptor, instance, "ComponentParameters", parameters);
            SetAndAssertParameter(descriptor, instance, "ComponentParameters", null);
        }
    }

    [Theory]
    [InlineData("Microsoft.AspNetCore.Components.Media.Image")]
    [InlineData("Microsoft.AspNetCore.Components.Media.Video")]
    [InlineData("Microsoft.AspNetCore.Components.Media.FileDownload")]
    public void MediaProvider_ExposesWorkingDescriptors(string typeName)
    {
        var result = RunGeneratorForProviders(
            "Microsoft.AspNetCore.Components",
            "Microsoft.AspNetCore.Components.Media");
        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            var descriptor = Assert.Single(
                context.Components,
                item => item.Type.FullName == typeName && IsBuiltInProviderDescriptor(item));
            var instance = descriptor.CreateInstance!(new TestServiceProvider());

            Assert.Equal(descriptor.Type, instance.GetType());
            Assert.Empty(descriptor.Parameters);
            Assert.Equal(
                [
                    ("JSRuntime", typeof(Microsoft.JSInterop.IJSRuntime)),
                    ("LoggerFactory", typeof(Microsoft.Extensions.Logging.ILoggerFactory)),
                ],
                descriptor.Injectables
                    .Select(injectable => (injectable.Name, injectable.ServiceType))
                    .OrderBy(item => item.Name));

            var jsRuntime = new TestJSRuntime();
            var loggerFactory = Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;
            SetAndAssertInjectable(descriptor, instance, "JSRuntime", jsRuntime);
            SetAndAssertInjectable(descriptor, instance, "LoggerFactory", loggerFactory);
        }
    }

    private static Microsoft.AspNetCore.Components.Infrastructure.ComponentDescriptor[]
        GetAuthorizationProviderDescriptors(
            IReadOnlyList<Microsoft.AspNetCore.Components.Infrastructure.ComponentDescriptor> descriptors)
        => descriptors
            .Where(descriptor =>
                descriptor.Parameters.Any(parameter => parameter.Name is
                    "AuthenticationState" or "ExistingCascadedAuthenticationState") ||
                descriptor.Injectables.Any(injectable => injectable.Name is
                    "AuthorizationPolicyProvider" or "AuthorizationService" or
                    "AuthenticationStateProvider"))
            .Where(descriptor =>
                descriptor.Type.Assembly == typeof(
                    Microsoft.AspNetCore.Components.Authorization.AuthorizeView).Assembly)
            .ToArray();

    private static Microsoft.AspNetCore.Components.Infrastructure.ComponentDescriptor FindDescriptor(
        IEnumerable<Microsoft.AspNetCore.Components.Infrastructure.ComponentDescriptor> descriptors,
        string typeName)
        => Assert.Single(descriptors, descriptor => descriptor.Type.FullName == typeName);

    private static Microsoft.AspNetCore.Components.Infrastructure.ComponentDescriptor
        FindBuiltInDescriptor(
            IReadOnlyList<Microsoft.AspNetCore.Components.Infrastructure.ComponentDescriptor> descriptors,
            string typeName)
        => Assert.Single(
            descriptors,
            descriptor => descriptor.Type.FullName == typeName &&
                IsBuiltInProviderDescriptor(descriptor));

    private static void AssertDescriptorShape(
        Microsoft.AspNetCore.Components.Infrastructure.ComponentDescriptor descriptor,
        bool hasFactory,
        (string Name, Type Type, Type AttributeType)[] parameters,
        (string Name, Type Type)[] injectables)
    {
        Assert.Equal(hasFactory, descriptor.CreateInstance is not null);
        Assert.Equal(
            parameters.OrderBy(item => item.Name),
            descriptor.Parameters
                .Select(parameter =>
                    (parameter.Name, parameter.ParameterType, parameter.Attribute.GetType()))
                .OrderBy(item => item.Name));
        Assert.Equal(
            injectables.OrderBy(item => item.Name),
            descriptor.Injectables
                .Select(injectable => (injectable.Name, injectable.ServiceType))
                .OrderBy(item => item.Name));
        Assert.All(
            descriptor.Injectables,
            injectable => Assert.IsType<Microsoft.AspNetCore.Components.InjectAttribute>(
                injectable.Attribute));
    }

    private static void SetAndAssertParameter(
        Microsoft.AspNetCore.Components.Infrastructure.ComponentDescriptor descriptor,
        object instance,
        string name,
        object? value)
    {
        var parameter = Assert.Single(descriptor.Parameters, item => item.Name == name);
        parameter.SetValue(instance, value);
        Assert.Same(value, parameter.GetValue(instance));
        Assert.Same(value, GetHiddenProperty(instance, name));
    }

    private static void SetAndAssertInjectable(
        Microsoft.AspNetCore.Components.Infrastructure.ComponentDescriptor descriptor,
        object instance,
        string name,
        object value)
    {
        var injectable = Assert.Single(descriptor.Injectables, item => item.Name == name);
        Assert.IsType<Microsoft.AspNetCore.Components.InjectAttribute>(injectable.Attribute);
        injectable.SetValue(instance, value);
        Assert.Same(value, GetHiddenProperty(instance, name));
    }

    private static object? GetHiddenProperty(object target, string name)
    {
        for (var type = target.GetType(); type is not null; type = type.BaseType)
        {
            var property = type.GetProperty(
                name,
                System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.DeclaredOnly);
            if (property is not null)
            {
                return property.GetValue(target);
            }
        }

        throw new InvalidOperationException(
            $"Could not find property '{name}' on '{target.GetType()}'.");
    }

    private static string[] ParseExpectedMembers(string value)
        => value.Length == 0
            ? []
            : value.Split('|').Order().ToArray();

    private static string GetStableTypeName(Type type)
    {
        if (!type.IsGenericType)
        {
            return type.FullName!;
        }

        return $"{type.GetGenericTypeDefinition().FullName}[[" +
            string.Join("],[", type.GetGenericArguments().Select(GetStableTypeName)) +
            "]]";
    }

    private sealed class TestAuthenticationStateProvider :
        Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider
    {
        public override Task<Microsoft.AspNetCore.Components.Authorization.AuthenticationState>
            GetAuthenticationStateAsync()
            => Task.FromResult(
                new Microsoft.AspNetCore.Components.Authorization.AuthenticationState(
                    new System.Security.Claims.ClaimsPrincipal()));
    }

    private sealed class TestAuthorizationPolicyProvider :
        Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider
    {
        public Task<Microsoft.AspNetCore.Authorization.AuthorizationPolicy?>
            GetPolicyAsync(string policyName)
            => Task.FromResult<Microsoft.AspNetCore.Authorization.AuthorizationPolicy?>(null);

        public Task<Microsoft.AspNetCore.Authorization.AuthorizationPolicy>
            GetDefaultPolicyAsync()
            => throw new NotSupportedException();

        public Task<Microsoft.AspNetCore.Authorization.AuthorizationPolicy?>
            GetFallbackPolicyAsync()
            => Task.FromResult<Microsoft.AspNetCore.Authorization.AuthorizationPolicy?>(null);
    }

    private sealed class TestAuthorizationService :
        Microsoft.AspNetCore.Authorization.IAuthorizationService
    {
        public Task<Microsoft.AspNetCore.Authorization.AuthorizationResult> AuthorizeAsync(
            System.Security.Claims.ClaimsPrincipal user,
            object? resource,
            IEnumerable<Microsoft.AspNetCore.Authorization.IAuthorizationRequirement> requirements)
            => Task.FromResult(Microsoft.AspNetCore.Authorization.AuthorizationResult.Success());

        public Task<Microsoft.AspNetCore.Authorization.AuthorizationResult> AuthorizeAsync(
            System.Security.Claims.ClaimsPrincipal user,
            object? resource,
            string policyName)
            => Task.FromResult(Microsoft.AspNetCore.Authorization.AuthorizationResult.Success());
    }

    [Fact]
    public void WebProvider_ExposesExactNonGenericDescriptorSet()
    {
        var result = RunGeneratorForWebProvider();
        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            Assert.Equal(
                [
                    "Microsoft.AspNetCore.Components.Forms.AntiforgeryToken",
                    "Microsoft.AspNetCore.Components.Forms.ClientValidationData",
                    "Microsoft.AspNetCore.Components.Forms.EditForm",
                    "Microsoft.AspNetCore.Components.Forms.FormMappingScope",
                    "Microsoft.AspNetCore.Components.Forms.InputCheckbox",
                    "Microsoft.AspNetCore.Components.Forms.InputFile",
                    "Microsoft.AspNetCore.Components.Forms.InputHidden",
                    "Microsoft.AspNetCore.Components.Forms.InputText",
                    "Microsoft.AspNetCore.Components.Forms.InputTextArea",
                    "Microsoft.AspNetCore.Components.Forms.Mapping.FormMappingValidator",
                    "Microsoft.AspNetCore.Components.Forms.ValidationSummary",
                    "Microsoft.AspNetCore.Components.Routing.FocusOnNavigate",
                    "Microsoft.AspNetCore.Components.Routing.NavigationLock",
                    "Microsoft.AspNetCore.Components.Routing.NavLink",
                    "Microsoft.AspNetCore.Components.Web.EnvironmentView",
                    "Microsoft.AspNetCore.Components.Web.ErrorBoundary",
                    "Microsoft.AspNetCore.Components.Web.HeadOutlet",
                ],
                GetWebProviderDescriptors(context.Components)
                    .Select(descriptor => descriptor.Type.FullName)
                    .Order());
        }
    }

    [Fact]
    public async Task WebProvider_ExposesFrameworkJSInvokableDescriptors()
    {
        var result = RunGeneratorForProviders(
            "Microsoft.AspNetCore.Components",
            "Microsoft.AspNetCore.Components.Forms",
            "Microsoft.AspNetCore.Components.Web");
        var source = GetGeneratedSource(result);
        Assert.Contains(
            "UnsafeAccessorType(\"Microsoft.JSInterop.Infrastructure.BuiltInJSInvokableMethodDescriptors, Microsoft.AspNetCore.Components.Web\")",
            source);

        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            var descriptors = context.JSInvokableMethods
                .Where(descriptor => descriptor.AssemblyName == "Microsoft.AspNetCore.Components.Web")
                .Select(descriptor => $"{descriptor.TargetType.FullName}:{descriptor.Identifier}")
                .Order()
                .ToArray();

            Assert.Equal(
            [
                "Microsoft.AspNetCore.Components.Forms.InputFileJsCallbacksRelay:NotifyChange",
                "Microsoft.AspNetCore.Components.RenderTree.WebRenderer+WebRendererInteropMethods:AddRootComponent",
                "Microsoft.AspNetCore.Components.RenderTree.WebRenderer+WebRendererInteropMethods:DispatchEventAsync",
                "Microsoft.AspNetCore.Components.RenderTree.WebRenderer+WebRendererInteropMethods:RemoveRootComponent",
                "Microsoft.AspNetCore.Components.RenderTree.WebRenderer+WebRendererInteropMethods:SetRootComponentParameters",
                "Microsoft.AspNetCore.Components.Web.Virtualization.VirtualizeJsInterop:OnSpacerAfterVisible",
                "Microsoft.AspNetCore.Components.Web.Virtualization.VirtualizeJsInterop:OnSpacerBeforeVisible",
            ],
                descriptors);

            var removeRootComponent = Assert.Single(
                context.JSInvokableMethods,
                descriptor => descriptor.AssemblyName == "Microsoft.AspNetCore.Components.Web" &&
                    descriptor.Identifier == "RemoveRootComponent");
            var options = new System.Text.Json.JsonSerializerOptions();
            await Assert.ThrowsAsync<ArgumentException>(
                async () => await removeRootComponent.Invoke(null, "[]", options));
            await Assert.ThrowsAsync<System.Text.Json.JsonException>(
                async () => await removeRootComponent.Invoke(null, "[1,2]", options));
        }
    }

    [Fact]
    public void WebProvider_SupplementalMembersRoundTrip()
    {
        var result = RunGeneratorForWebProvider();
        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            var cases = new[]
            {
                new WebDescriptorCase(
                    "Microsoft.AspNetCore.Components.Web.EnvironmentView",
                    false,
                    "",
                    "HostEnvironment:Microsoft.Extensions.Hosting.IHostEnvironment"),
                new WebDescriptorCase(
                    "Microsoft.AspNetCore.Components.Web.ErrorBoundary",
                    false,
                    "",
                    "ErrorBoundaryLogger:Microsoft.AspNetCore.Components.Web.IErrorBoundaryLogger"),
                new WebDescriptorCase(
                    "Microsoft.AspNetCore.Components.Web.HeadOutlet",
                    false,
                    "",
                    "JSRuntime:Microsoft.JSInterop.IJSRuntime"),
                new WebDescriptorCase(
                    "Microsoft.AspNetCore.Components.Routing.FocusOnNavigate",
                    false,
                    "",
                    "JSRuntime:Microsoft.JSInterop.IJSRuntime"),
                new WebDescriptorCase(
                    "Microsoft.AspNetCore.Components.Routing.NavigationLock",
                    false,
                    "",
                    "JSRuntime:Microsoft.JSInterop.IJSRuntime|NavigationManager:Microsoft.AspNetCore.Components.NavigationManager"),
                new WebDescriptorCase(
                    "Microsoft.AspNetCore.Components.Routing.NavLink",
                    false,
                    "",
                    "NavigationManager:Microsoft.AspNetCore.Components.NavigationManager"),
                new WebDescriptorCase(
                    "Microsoft.AspNetCore.Components.Forms.AntiforgeryToken",
                    false,
                    "",
                    "Services:System.IServiceProvider"),
                new WebDescriptorCase(
                    "Microsoft.AspNetCore.Components.Forms.EditForm",
                    false,
                    "MappingContext:Microsoft.AspNetCore.Components.Forms.FormMappingContext:CascadingParameterAttribute",
                    ""),
                new WebDescriptorCase(
                    "Microsoft.AspNetCore.Components.Forms.FormMappingScope",
                    false,
                    "",
                    "FormValueModelBinder:Microsoft.AspNetCore.Components.Forms.Mapping.IFormValueMapper"),
                new WebDescriptorCase(
                    "Microsoft.AspNetCore.Components.Forms.InputCheckbox",
                    false,
                    InputBaseParameterMap,
                    ""),
                new WebDescriptorCase(
                    "Microsoft.AspNetCore.Components.Forms.InputHidden",
                    false,
                    InputBaseParameterMap,
                    ""),
                new WebDescriptorCase(
                    "Microsoft.AspNetCore.Components.Forms.InputText",
                    false,
                    InputBaseParameterMap,
                    ""),
                new WebDescriptorCase(
                    "Microsoft.AspNetCore.Components.Forms.InputTextArea",
                    false,
                    InputBaseParameterMap,
                    ""),
                new WebDescriptorCase(
                    "Microsoft.AspNetCore.Components.Forms.InputFile",
                    false,
                    "",
                    "JSRuntime:Microsoft.JSInterop.IJSRuntime"),
                new WebDescriptorCase(
                    "Microsoft.AspNetCore.Components.Forms.ValidationSummary",
                    false,
                    "CurrentEditContext:Microsoft.AspNetCore.Components.Forms.EditContext:CascadingParameterAttribute",
                    ""),
                new WebDescriptorCase(
                    "Microsoft.AspNetCore.Components.Forms.Mapping.FormMappingValidator",
                    true,
                    "CurrentEditContext:Microsoft.AspNetCore.Components.Forms.EditContext:ParameterAttribute|MappingContext:Microsoft.AspNetCore.Components.Forms.FormMappingContext:CascadingParameterAttribute",
                    ""),
                new WebDescriptorCase(
                    "Microsoft.AspNetCore.Components.Forms.ClientValidationData",
                    true,
                    "CurrentEditContext:Microsoft.AspNetCore.Components.Forms.EditContext:CascadingParameterAttribute",
                    "Services:System.IServiceProvider"),
            };

            var descriptors = GetWebProviderDescriptors(context.Components);
            foreach (var testCase in cases)
            {
                var descriptor = FindDescriptor(descriptors, testCase.TypeName);
                Assert.Equal(testCase.HasFactory, descriptor.CreateInstance is not null);
                Assert.Equal(
                    ParseExpectedMembers(testCase.Parameters),
                    descriptor.Parameters
                        .Select(parameter =>
                            $"{parameter.Name}:{GetStableTypeName(parameter.ParameterType)}:{parameter.Attribute.GetType().Name}")
                        .Order());
                Assert.Equal(
                    ParseExpectedMembers(testCase.Injectables),
                    descriptor.Injectables
                        .Select(injectable =>
                            $"{injectable.Name}:{GetStableTypeName(injectable.ServiceType)}")
                        .Order());

                var instance = descriptor.CreateInstance?.Invoke(new TestServiceProvider()) ??
                    Activator.CreateInstance(descriptor.Type)!;
                Assert.Equal(descriptor.Type, instance.GetType());

                foreach (var parameter in descriptor.Parameters)
                {
                    var value = CreateWebParameterValue(parameter);
                    parameter.SetValue(instance, value);
                    Assert.Same(value, parameter.GetValue(instance));
                    Assert.Same(value, GetHiddenProperty(instance, parameter.Name));

                    if (!parameter.ParameterType.IsValueType)
                    {
                        parameter.SetValue(instance, null);
                        Assert.Null(parameter.GetValue(instance));
                        Assert.Null(GetHiddenProperty(instance, parameter.Name));
                    }
                }

                foreach (var injectable in descriptor.Injectables)
                {
                    Assert.IsType<Microsoft.AspNetCore.Components.InjectAttribute>(
                        injectable.Attribute);
                    var value = CreateWebInjectableValue(injectable.ServiceType);
                    injectable.SetValue(instance, value);
                    Assert.Same(value, GetHiddenProperty(instance, injectable.Name));
                }
            }
        }
    }

    [Theory]
    [InlineData(
        "Microsoft.AspNetCore.Components.Forms.InputDate<System.DateTime>",
        "Microsoft.AspNetCore.Components.Forms.InputDate`1",
        "CascadedEditContext|FieldPrefix")]
    [InlineData(
        "Microsoft.AspNetCore.Components.Forms.InputNumber<int>",
        "Microsoft.AspNetCore.Components.Forms.InputNumber`1",
        "CascadedEditContext|FieldPrefix")]
    [InlineData(
        "Microsoft.AspNetCore.Components.Forms.InputRadio<int>",
        "Microsoft.AspNetCore.Components.Forms.InputRadio`1",
        "CascadedContext")]
    [InlineData(
        "Microsoft.AspNetCore.Components.Forms.InputRadioGroup<int>",
        "Microsoft.AspNetCore.Components.Forms.InputRadioGroup`1",
        "CascadedContext|CascadedEditContext|FieldPrefix")]
    [InlineData(
        "Microsoft.AspNetCore.Components.Forms.InputSelect<int>",
        "Microsoft.AspNetCore.Components.Forms.InputSelect`1",
        "CascadedEditContext|FieldPrefix")]
    [InlineData(
        "Microsoft.AspNetCore.Components.Forms.Label<int>",
        "Microsoft.AspNetCore.Components.Forms.Label`1",
        "FieldPrefix")]
    public void WebGenericFactory_ExposesClosedWorkingDescriptor(
        string componentType,
        string expectedGenericDefinition,
        string expectedParameters)
    {
        var result = RunGeneratorForComponent(componentType);
        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            var expectedParameterNames = expectedParameters.Split('|').Order().ToArray();
            var expectedTypeArgument = expectedGenericDefinition.EndsWith(
                ".InputDate`1",
                StringComparison.Ordinal)
                    ? "System.DateTime"
                    : "System.Int32";
            var descriptor = Assert.Single(
                context.Components,
                item => IsClosedType(item.Type, expectedGenericDefinition, [expectedTypeArgument]) &&
                    item.Parameters.Select(parameter => parameter.Name).Order()
                        .SequenceEqual(expectedParameterNames));

            Assert.Null(descriptor.CreateInstance);
            Assert.Empty(descriptor.Injectables);
            Assert.Equal(
                expectedParameterNames,
                descriptor.Parameters.Select(parameter => parameter.Name).Order());
            Assert.All(
                descriptor.Parameters,
                parameter => Assert.IsType<Microsoft.AspNetCore.Components.CascadingParameterAttribute>(
                    parameter.Attribute));

            var instance = Activator.CreateInstance(descriptor.Type)!;
            Assert.Equal(descriptor.Type, instance.GetType());
            foreach (var parameter in descriptor.Parameters)
            {
                var value = CreateWebParameterValue(parameter);
                parameter.SetValue(instance, value);
                Assert.Same(value, parameter.GetValue(instance));
                Assert.Same(value, GetHiddenProperty(instance, parameter.Name));
            }
        }
    }

    private const string InputBaseParameterMap =
        "CascadedEditContext:Microsoft.AspNetCore.Components.Forms.EditContext:CascadingParameterAttribute|" +
        "FieldPrefix:Microsoft.AspNetCore.Components.Forms.HtmlFieldPrefix:CascadingParameterAttribute";

    private static GeneratorTestResult RunGeneratorForWebProvider()
        => RunGenerator(
            "namespace TestComponents;",
            hostFrameworkAssemblyNames:
            [
                "Microsoft.AspNetCore.Components",
                "Microsoft.AspNetCore.Components.Web",
            ]);

    private static Microsoft.AspNetCore.Components.Infrastructure.ComponentDescriptor[]
        GetWebProviderDescriptors(
            IReadOnlyList<Microsoft.AspNetCore.Components.Infrastructure.ComponentDescriptor> descriptors)
    {
        var providerDescriptors = descriptors
            .Where(descriptor => descriptor.Type.Assembly ==
                typeof(Microsoft.AspNetCore.Components.Forms.EditForm).Assembly)
            .Where(descriptor =>
                descriptor.CreateInstance is null ||
                IsBuiltInProviderDescriptor(descriptor))
            .Where(descriptor =>
                descriptor.Injectables.Count > 0 ||
                descriptor.Parameters.Any(parameter => parameter.Name is
                    "MappingContext" or
                    "CascadedEditContext" or
                    "FieldPrefix" or
                    "CurrentEditContext"))
            .ToArray();

        var duplicates = providerDescriptors
            .GroupBy(descriptor => descriptor.Type)
            .Where(group => group.Count() > 1)
            .Select(group => $"{group.Key.FullName} ({group.Count()})")
            .Order()
            .ToArray();
        Assert.Empty(duplicates);
        return providerDescriptors;
    }

    private static object CreateWebParameterValue(
        Microsoft.AspNetCore.Components.Infrastructure.ComponentParameterDescriptor parameter)
        => parameter.ParameterType == typeof(Microsoft.AspNetCore.Components.Forms.EditContext)
            ? new Microsoft.AspNetCore.Components.Forms.EditContext(new object())
            : System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(
                parameter.ParameterType);

    private static object CreateWebInjectableValue(Type serviceType)
    {
        if (serviceType == typeof(Microsoft.JSInterop.IJSRuntime))
        {
            return new TestJSRuntime();
        }

        if (serviceType == typeof(Microsoft.AspNetCore.Components.NavigationManager))
        {
            return new TestNavigationManager();
        }

        if (serviceType == typeof(IServiceProvider))
        {
            return new TestServiceProvider();
        }

        return serviceType.IsInterface
            ? System.Reflection.DispatchProxy.Create(serviceType, typeof(TestDispatchProxy))
            : System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(serviceType);
    }

    private class TestDispatchProxy : System.Reflection.DispatchProxy
    {
        protected override object? Invoke(
            System.Reflection.MethodInfo? targetMethod,
            object?[]? args)
            => targetMethod?.ReturnType == typeof(void)
                ? null
                : targetMethod?.ReturnType.IsValueType == true
                    ? Activator.CreateInstance(targetMethod.ReturnType)
                    : null;
    }

    private sealed record WebDescriptorCase(
        string TypeName,
        bool HasFactory,
        string Parameters,
        string Injectables);

    [Fact]
    public void QuickGridProvider_PaginatorDescriptorIsConstructible()
    {
        var result = RunGeneratorForProviders(
            "Microsoft.AspNetCore.Components",
            "Microsoft.AspNetCore.Components.QuickGrid");
        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            var descriptor = FindBuiltInDescriptor(
                context.Components,
                "Microsoft.AspNetCore.Components.QuickGrid.Paginator");
            var instance = descriptor.CreateInstance!(new TestServiceProvider());

            Assert.Equal(descriptor.Type, instance.GetType());
            Assert.Empty(descriptor.Parameters);
            AssertDescriptorInjectables(
                descriptor,
                [("NavigationManager", typeof(Microsoft.AspNetCore.Components.NavigationManager))]);

            var navigationManager = new TestNavigationManager();
            SetAndAssertInjectable(
                descriptor,
                instance,
                "NavigationManager",
                navigationManager);
        }
    }

    [Fact]
    public void QuickGridFactory_ReturnsGridCascadingValueAndTupleVirtualize()
    {
        var result = RunGeneratorForGenericMapping(
            "namespace TestComponents;",
            "Microsoft.AspNetCore.Components.QuickGrid.QuickGrid<string>");
        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            var quickGridType =
                typeof(Microsoft.AspNetCore.Components.QuickGrid.QuickGrid<string>);
            var tupleVirtualizeType =
                typeof(Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize<
                    (int RowIndex, string Data)>);
            var descriptors = context.Components
                .Where(descriptor =>
                    descriptor.Type == quickGridType && IsBuiltInProviderDescriptor(descriptor) ||
                    descriptor.Type == tupleVirtualizeType &&
                        IsBuiltInProviderDescriptor(descriptor) ||
                    descriptor.Type.IsGenericType &&
                    descriptor.Type.GetGenericTypeDefinition() ==
                        typeof(Microsoft.AspNetCore.Components.CascadingValue<>) &&
                    descriptor.Type.GetGenericArguments()[0].IsGenericType &&
                    descriptor.Type.GetGenericArguments()[0].GetGenericTypeDefinition().FullName ==
                        "Microsoft.AspNetCore.Components.QuickGrid.Infrastructure.InternalGridContext`1" &&
                    descriptor.Type.GetGenericArguments()[0].GetGenericArguments()
                        .SequenceEqual([typeof(string)]))
                .ToArray();

            Assert.Equal(3, descriptors.Length);

            var cascadingDescriptor = Assert.Single(
                descriptors,
                descriptor => descriptor.Type.IsGenericType &&
                    descriptor.Type.GetGenericTypeDefinition() ==
                        typeof(Microsoft.AspNetCore.Components.CascadingValue<>));
            var internalContextType = cascadingDescriptor.Type.GetGenericArguments()[0];
            Assert.Equal(
                "Microsoft.AspNetCore.Components.QuickGrid.Infrastructure.InternalGridContext`1",
                internalContextType.GetGenericTypeDefinition().FullName);
            Assert.Equal(typeof(string), Assert.Single(internalContextType.GetGenericArguments()));
            Assert.Equal(
                new[]
                {
                    quickGridType,
                    typeof(Microsoft.AspNetCore.Components.CascadingValue<>)
                        .MakeGenericType(internalContextType),
                    tupleVirtualizeType,
                }.OrderBy(type => type.FullName),
                descriptors.Select(descriptor => descriptor.Type)
                    .OrderBy(type => type.FullName));
            AssertDescriptorParameters(
                cascadingDescriptor,
                [
                    ("ChildContent", typeof(Microsoft.AspNetCore.Components.RenderFragment)),
                    ("Value", internalContextType),
                    ("Name", typeof(string)),
                    ("IsFixed", typeof(bool)),
                ]);
            Assert.Empty(cascadingDescriptor.Injectables);

            var cascadingValue =
                cascadingDescriptor.CreateInstance!(new TestServiceProvider());
            Microsoft.AspNetCore.Components.RenderFragment childContent = static _ => { };
            var internalContext =
                System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(
                    internalContextType);
            SetAndAssertDescriptorValue(
                cascadingDescriptor,
                cascadingValue,
                "ChildContent",
                childContent);
            SetAndAssertDescriptorValue(
                cascadingDescriptor,
                cascadingValue,
                "Value",
                internalContext);
            SetAndAssertDescriptorValue(cascadingDescriptor, cascadingValue, "Name", "phase-five");
            SetAndAssertDescriptorValue(cascadingDescriptor, cascadingValue, "IsFixed", true);

            var virtualizeDescriptor = Assert.Single(
                descriptors,
                descriptor => descriptor.Type == tupleVirtualizeType);
            AssertVirtualizeDescriptorShape(
                virtualizeDescriptor,
                typeof((int RowIndex, string Data)));
            var virtualize = virtualizeDescriptor.CreateInstance!(new TestServiceProvider());
            var jsRuntime = new TestJSRuntime();
            SetAndAssertInjectable(
                virtualizeDescriptor,
                virtualize,
                "JSRuntime",
                jsRuntime);
        }
    }

    [Fact]
    public void QuickGridDescriptor_HiddenInjectablesRoundTrip()
    {
        var result = RunGeneratorForGenericMapping(
            "namespace TestComponents;",
            "Microsoft.AspNetCore.Components.QuickGrid.QuickGrid<string>");
        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            var descriptor = Assert.Single(
                context.Components,
                item => item.Type ==
                        typeof(Microsoft.AspNetCore.Components.QuickGrid.QuickGrid<string>) &&
                    IsBuiltInProviderDescriptor(item));
            var instance = descriptor.CreateInstance!(new TestServiceProvider());
            AssertDescriptorInjectables(
                descriptor,
                [
                    ("Services", typeof(IServiceProvider)),
                    ("JS", typeof(Microsoft.JSInterop.IJSRuntime)),
                    ("NavigationManager", typeof(Microsoft.AspNetCore.Components.NavigationManager)),
                ]);

            SetAndAssertInjectable(
                descriptor,
                instance,
                "Services",
                new TestServiceProvider());
            SetAndAssertInjectable(
                descriptor,
                instance,
                "JS",
                new TestJSRuntime());
            SetAndAssertInjectable(
                descriptor,
                instance,
                "NavigationManager",
                new TestNavigationManager());
        }
    }

    [Theory]
    [InlineData(
        "Microsoft.AspNetCore.Components.QuickGrid.PropertyColumn<string, int>",
        "Microsoft.AspNetCore.Components.QuickGrid.PropertyColumn`2",
        "System.String|System.Int32")]
    [InlineData(
        "Microsoft.AspNetCore.Components.QuickGrid.TemplateColumn<string>",
        "Microsoft.AspNetCore.Components.QuickGrid.TemplateColumn`1",
        "System.String")]
    [InlineData(
        "Microsoft.AspNetCore.Components.QuickGrid.Infrastructure.ColumnsCollectedNotifier<string>",
        "Microsoft.AspNetCore.Components.QuickGrid.Infrastructure.ColumnsCollectedNotifier`1",
        "System.String")]
    public void QuickGridColumnFactory_IsConstructibleAndExposesCascadingContext(
        string componentType,
        string expectedGenericDefinition,
        string expectedTypeArguments)
    {
        var result = RunGeneratorForGenericMapping("namespace TestComponents;", componentType);
        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            var descriptor = Assert.Single(
                context.Components,
                item => IsClosedType(
                        item.Type,
                        expectedGenericDefinition,
                        expectedTypeArguments.Split('|')) &&
                    item.Parameters.Select(parameter => parameter.Name)
                        .SequenceEqual(["InternalGridContext"]) &&
                    IsBuiltInProviderDescriptor(item));
            var instance = descriptor.CreateInstance!(new TestServiceProvider());
            var parameter = Assert.Single(descriptor.Parameters);

            Assert.Equal(descriptor.Type, instance.GetType());
            Assert.Equal("InternalGridContext", parameter.Name);
            Assert.IsType<Microsoft.AspNetCore.Components.CascadingParameterAttribute>(
                parameter.Attribute);
            Assert.Empty(descriptor.Injectables);

            var internalContext =
                System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(
                    parameter.ParameterType);
            SetAndAssertDescriptorValue(
                descriptor,
                instance,
                "InternalGridContext",
                internalContext);
            Assert.Same(
                internalContext,
                GetHiddenProperty(instance, "InternalGridContext"));
        }
    }

    [Fact]
    public void CustomColumnBaseFactory_EmitsConstraintAndExposesCascadingContext()
    {
        const string referencedSource = """
            namespace TestComponents;

            public sealed class PhaseFiveColumn :
                Microsoft.AspNetCore.Components.QuickGrid.ColumnBase<string>
            {
                public override Microsoft.AspNetCore.Components.QuickGrid.GridSort<string>? SortBy
                {
                    get;
                    set;
                }

                protected override void CellContent(
                    Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder,
                    string item)
                {
                }
            }
            """;
        var result = RunGeneratorForGenericMapping(referencedSource);
        AssertFactoryAccessor(
            GetGeneratedSource(result),
            "Microsoft.AspNetCore.Components.QuickGrid",
            "CreateColumnBaseDescriptors",
            "string, global::TestComponents.PhaseFiveColumn",
            "0,-1",
            "where T1 : global::Microsoft.AspNetCore.Components.QuickGrid.ColumnBase<T0>");

        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            var descriptor = Assert.Single(
                context.Components,
                item => item.Type.FullName == "TestComponents.PhaseFiveColumn" &&
                    item.CreateInstance is null &&
                    item.Parameters.Select(parameter => parameter.Name)
                        .SequenceEqual(["InternalGridContext"]));
            var parameter = Assert.Single(descriptor.Parameters);
            var instance = Activator.CreateInstance(descriptor.Type)!;
            var internalContext =
                System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(
                    parameter.ParameterType);

            Assert.IsType<Microsoft.AspNetCore.Components.CascadingParameterAttribute>(
                parameter.Attribute);
            Assert.Empty(descriptor.Injectables);
            SetAndAssertDescriptorValue(
                descriptor,
                instance,
                "InternalGridContext",
                internalContext);
            Assert.Same(
                internalContext,
                GetHiddenProperty(instance, "InternalGridContext"));
        }
    }

    [Fact]
    public void WebAssemblyAuthenticationProvider_ExposesRemoteAuthenticatorView()
    {
        var result = RunGeneratorForProviders(
            "Microsoft.AspNetCore.Components",
            "Microsoft.AspNetCore.Components.Authorization",
            "Microsoft.AspNetCore.Components.WebAssembly.Authentication");
        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            var descriptors = context.Components
                .Where(descriptor => descriptor.Type.Assembly ==
                    typeof(Microsoft.AspNetCore.Components.WebAssembly.Authentication
                        .RemoteAuthenticatorView).Assembly)
                .Where(descriptor => descriptor.Injectables.Count > 0)
                .ToArray();
            var descriptor = Assert.Single(descriptors);

            Assert.Equal(
                typeof(Microsoft.AspNetCore.Components.WebAssembly.Authentication
                    .RemoteAuthenticatorView),
                descriptor.Type);
            Assert.Null(descriptor.CreateInstance);
            Assert.Empty(descriptor.Parameters);
            AssertRemoteAuthenticatorInjectableShape(
                descriptor,
                typeof(Microsoft.AspNetCore.Components.WebAssembly.Authentication
                    .RemoteAuthenticationState));

            var instance = new Microsoft.AspNetCore.Components.WebAssembly.Authentication
                .RemoteAuthenticatorView();
            SetAndAssertRemoteAuthenticatorInjectables(descriptor, instance);
        }
    }

    [Fact]
    public void RemoteAuthenticatorViewCoreFactory_UsesCustomAuthenticationState()
    {
        const string referencedSource = """
            namespace TestComponents;

            public sealed class CustomRemoteAuthenticationState :
                Microsoft.AspNetCore.Components.WebAssembly.Authentication.RemoteAuthenticationState
            {
                public string? Tenant { get; set; }
            }

            public sealed class CustomAuthenticationService :
                Microsoft.AspNetCore.Components.WebAssembly.Authentication.IRemoteAuthenticationService<
                    CustomRemoteAuthenticationState>
            {
                public System.Threading.Tasks.Task<
                    Microsoft.AspNetCore.Components.WebAssembly.Authentication.RemoteAuthenticationResult<
                        CustomRemoteAuthenticationState>> SignInAsync(
                            Microsoft.AspNetCore.Components.WebAssembly.Authentication.RemoteAuthenticationContext<
                                CustomRemoteAuthenticationState> context)
                    => throw new System.NotSupportedException();

                public System.Threading.Tasks.Task<
                    Microsoft.AspNetCore.Components.WebAssembly.Authentication.RemoteAuthenticationResult<
                        CustomRemoteAuthenticationState>> CompleteSignInAsync(
                            Microsoft.AspNetCore.Components.WebAssembly.Authentication.RemoteAuthenticationContext<
                                CustomRemoteAuthenticationState> context)
                    => throw new System.NotSupportedException();

                public System.Threading.Tasks.Task<
                    Microsoft.AspNetCore.Components.WebAssembly.Authentication.RemoteAuthenticationResult<
                        CustomRemoteAuthenticationState>> SignOutAsync(
                            Microsoft.AspNetCore.Components.WebAssembly.Authentication.RemoteAuthenticationContext<
                                CustomRemoteAuthenticationState> context)
                    => throw new System.NotSupportedException();

                public System.Threading.Tasks.Task<
                    Microsoft.AspNetCore.Components.WebAssembly.Authentication.RemoteAuthenticationResult<
                        CustomRemoteAuthenticationState>> CompleteSignOutAsync(
                            Microsoft.AspNetCore.Components.WebAssembly.Authentication.RemoteAuthenticationContext<
                                CustomRemoteAuthenticationState> context)
                    => throw new System.NotSupportedException();
            }

            public sealed class CustomLogger :
                Microsoft.Extensions.Logging.ILogger<
                    Microsoft.AspNetCore.Components.WebAssembly.Authentication.RemoteAuthenticatorViewCore<
                        CustomRemoteAuthenticationState>>
            {
                public System.IDisposable? BeginScope<TState>(TState state)
                    where TState : notnull
                    => null;

                public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => false;

                public void Log<TState>(
                    Microsoft.Extensions.Logging.LogLevel logLevel,
                    Microsoft.Extensions.Logging.EventId eventId,
                    TState state,
                    System.Exception? exception,
                    System.Func<TState, System.Exception?, string> formatter)
                {
                }
            }
            """;
        var result = RunGeneratorForGenericMapping(
            referencedSource,
            "Microsoft.AspNetCore.Components.WebAssembly.Authentication.RemoteAuthenticatorViewCore<TestComponents.CustomRemoteAuthenticationState>");
        AssertFactoryAccessor(
            GetGeneratedSource(result),
            "Microsoft.AspNetCore.Components.WebAssembly.Authentication",
            "CreateRemoteAuthenticatorViewCoreDescriptors",
            "global::TestComponents.CustomRemoteAuthenticationState",
            "547",
            "where T0 : global::Microsoft.AspNetCore.Components.WebAssembly.Authentication.RemoteAuthenticationState");

        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            var descriptor = Assert.Single(
                context.Components,
                item => item.Type.IsGenericType &&
                    item.Type.GetGenericTypeDefinition() ==
                        typeof(Microsoft.AspNetCore.Components.WebAssembly.Authentication
                            .RemoteAuthenticatorViewCore<>) &&
                    item.Type.GetGenericArguments()[0].FullName ==
                        "TestComponents.CustomRemoteAuthenticationState" &&
                    item.Injectables.Count == 5);
            var stateType = Assert.Single(descriptor.Type.GetGenericArguments());

            Assert.Null(descriptor.CreateInstance);
            Assert.Empty(descriptor.Parameters);
            AssertRemoteAuthenticatorInjectableShape(descriptor, stateType);

            var instance = Activator.CreateInstance(descriptor.Type)!;
            var authenticationService = Activator.CreateInstance(
                loaded.ReferencedAssembly.GetType(
                    "TestComponents.CustomAuthenticationService",
                    throwOnError: true)!)!;
            var logger = Activator.CreateInstance(
                loaded.ReferencedAssembly.GetType(
                    "TestComponents.CustomLogger",
                    throwOnError: true)!)!;
            SetAndAssertRemoteAuthenticatorInjectables(
                descriptor,
                instance,
                authenticationService,
                logger);
        }
    }

    private static void AssertDescriptorParameters(
        Microsoft.AspNetCore.Components.Infrastructure.ComponentDescriptor descriptor,
        (string Name, Type Type)[] expected)
    {
        Assert.Equal(
            expected.OrderBy(item => item.Name),
            descriptor.Parameters
                .Select(parameter => (parameter.Name, parameter.ParameterType))
                .OrderBy(item => item.Name));
        Assert.All(
            descriptor.Parameters,
            parameter => Assert.IsType<Microsoft.AspNetCore.Components.ParameterAttribute>(
                parameter.Attribute));
    }

    private static void AssertDescriptorInjectables(
        Microsoft.AspNetCore.Components.Infrastructure.ComponentDescriptor descriptor,
        (string Name, Type Type)[] expected)
    {
        Assert.Equal(
            expected.OrderBy(item => item.Name),
            descriptor.Injectables
                .Select(injectable => (injectable.Name, injectable.ServiceType))
                .OrderBy(item => item.Name));
        Assert.All(
            descriptor.Injectables,
            injectable => Assert.IsType<Microsoft.AspNetCore.Components.InjectAttribute>(
                injectable.Attribute));
    }

    private static void SetAndAssertDescriptorValue(
        Microsoft.AspNetCore.Components.Infrastructure.ComponentDescriptor descriptor,
        object instance,
        string name,
        object? value)
    {
        var parameter = Assert.Single(descriptor.Parameters, item => item.Name == name);
        parameter.SetValue(instance, value);
        Assert.Equal(value, parameter.GetValue(instance));
    }

    private static void AssertVirtualizeDescriptorShape(
        Microsoft.AspNetCore.Components.Infrastructure.ComponentDescriptor descriptor,
        Type itemType)
    {
        var renderFragmentType = typeof(Microsoft.AspNetCore.Components.RenderFragment<>)
            .MakeGenericType(itemType);
        var itemsProviderType = typeof(
                Microsoft.AspNetCore.Components.Web.Virtualization.ItemsProviderDelegate<>)
            .MakeGenericType(itemType);
        var itemsType = typeof(ICollection<>).MakeGenericType(itemType);
        var comparerType = typeof(IEqualityComparer<>).MakeGenericType(itemType);

        AssertDescriptorParameters(
            descriptor,
            [
                ("ChildContent", renderFragmentType),
                ("ItemContent", renderFragmentType),
                ("Placeholder", typeof(Microsoft.AspNetCore.Components.RenderFragment<
                    Microsoft.AspNetCore.Components.Web.Virtualization.PlaceholderContext>)),
                ("EmptyContent", typeof(Microsoft.AspNetCore.Components.RenderFragment)),
                ("ItemSize", typeof(float)),
                ("ItemsProvider", itemsProviderType),
                ("Items", itemsType),
                ("OverscanCount", typeof(int)),
                ("SpacerElement", typeof(string)),
                ("MaxItemCount", typeof(int)),
                ("AnchorMode", typeof(Microsoft.AspNetCore.Components.Web.Virtualization
                    .VirtualizeAnchorMode)),
                ("ItemComparer", comparerType),
                ("InitialItemIndex", typeof(int)),
            ]);
        AssertDescriptorInjectables(
            descriptor,
            [("JSRuntime", typeof(Microsoft.JSInterop.IJSRuntime))]);
    }

    private static void AssertRemoteAuthenticatorInjectableShape(
        Microsoft.AspNetCore.Components.Infrastructure.ComponentDescriptor descriptor,
        Type stateType)
    {
        var coreType = typeof(Microsoft.AspNetCore.Components.WebAssembly.Authentication
                .RemoteAuthenticatorViewCore<>)
            .MakeGenericType(stateType);
        AssertDescriptorInjectables(
            descriptor,
            [
                ("Navigation", typeof(Microsoft.AspNetCore.Components.NavigationManager)),
                ("AuthenticationService", typeof(Microsoft.AspNetCore.Components.WebAssembly
                    .Authentication.IRemoteAuthenticationService<>).MakeGenericType(stateType)),
                ("RemoteApplicationPathsProvider", descriptor.Injectables.Single(
                    injectable => injectable.Name == "RemoteApplicationPathsProvider").ServiceType),
                ("AuthenticationProvider", typeof(Microsoft.AspNetCore.Components.Authorization
                    .AuthenticationStateProvider)),
                ("Logger", typeof(Microsoft.Extensions.Logging.ILogger<>).MakeGenericType(coreType)),
            ]);
        Assert.Equal(
            "Microsoft.AspNetCore.Components.WebAssembly.Authentication.IRemoteAuthenticationPathsProvider",
            descriptor.Injectables.Single(
                injectable => injectable.Name == "RemoteApplicationPathsProvider")
                .ServiceType.FullName);
    }

    private static void SetAndAssertRemoteAuthenticatorInjectables(
        Microsoft.AspNetCore.Components.Infrastructure.ComponentDescriptor descriptor,
        object instance,
        object? authenticationService = null,
        object? logger = null)
    {
        foreach (var injectable in descriptor.Injectables)
        {
            object? value = injectable.Name switch
            {
                "Navigation" => new TestNavigationManager(),
                "AuthenticationProvider" => new TestAuthenticationStateProvider(),
                "AuthenticationService" when authenticationService is not null =>
                    authenticationService,
                "Logger" when logger is not null => logger,
                _ => System.Reflection.DispatchProxy.Create(
                    injectable.ServiceType,
                    typeof(TestDispatchProxy)),
            };
            injectable.SetValue(instance, value);
            Assert.Same(value, GetHiddenProperty(instance, injectable.Name));
        }
    }
}
