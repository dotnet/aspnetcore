// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components;

public class ComponentTypeInfoResolverTest
{
    [Fact]
    public void SourceGeneratedResolver_MergesDescriptorsForTypeNameAndAssemblyEnumeration()
    {
        var generatedShared = new ComponentParameterDescriptor
        {
            Name = "Shared",
            ParameterType = typeof(string),
            Attribute = new ParameterAttribute(),
            SetValue = static (_, _) => { },
            GetValue = static _ => null,
        };
        var builtInShared = new ComponentParameterDescriptor
        {
            Name = "Shared",
            ParameterType = typeof(int),
            Attribute = new CascadingParameterAttribute(),
            SetValue = static (_, _) => { },
            GetValue = static _ => null,
        };
        var builtInOnly = new ComponentParameterDescriptor
        {
            Name = "BuiltInOnly",
            ParameterType = typeof(object),
            Attribute = new CascadingParameterAttribute(),
            SetValue = static (_, _) => { },
            GetValue = static _ => null,
        };
        var first = Describe(
            typeof(PublicRoutableComponent),
            parameters: [generatedShared],
            metadata: ["shared", "first"]);
        var second = Describe(
            typeof(PublicRoutableComponent),
            createInstance: static _ => new PublicRoutableComponent(),
            parameters: [builtInShared, builtInOnly],
            metadata: ["shared", "second"]);
        var resolver = new SourceGeneratedComponentTypeInfoResolver(new StubMetadataResolver(first, second));

        var byType = resolver.GetRequiredTypeInfo(typeof(PublicRoutableComponent));
        var byName = resolver.GetRequiredTypeInfo(typeof(PublicRoutableComponent).Assembly.GetName().Name!, typeof(PublicRoutableComponent).FullName!);
        var byAssembly = resolver.GetTypeInfos(typeof(PublicRoutableComponent).Assembly);

        Assert.Same(byType, byName);
        Assert.Single(byAssembly);
        Assert.Same(byType, byAssembly[0]);
        Assert.Equal(["shared", "first", "second"], byType.Metadata);
        Assert.NotNull(byType.CreateInstance);
        Assert.Same(generatedShared, Assert.Single(byType.Parameters, parameter => parameter.Name == "Shared"));
        Assert.Same(builtInOnly, Assert.Single(byType.Parameters, parameter => parameter.Name == "BuiltInOnly"));
    }

    [Fact]
    public void CompositeResolver_PrefersGeneratedMetadataAndFillsMissingFactoryFromReflection()
    {
        var generated = Describe(
            typeof(PublicRoutableComponent),
            createInstance: null,
            parameters:
            [
                new ComponentParameterDescriptor
                {
                    Name = nameof(PublicRoutableComponent.Title),
                    ParameterType = typeof(string),
                    Attribute = new ParameterAttribute(),
                    SetValue = static (target, value) => ((PublicRoutableComponent)target).Title = (string?)value,
                    GetValue = static target => ((PublicRoutableComponent)target).Title,
                },
            ],
            metadata: ["generated"]);
        using var resolver = new CompositeComponentTypeInfoResolver(
        [
            new SourceGeneratedComponentTypeInfoResolver(new StubMetadataResolver(generated)),
            new ReflectionComponentTypeInfoResolver(),
        ]);

        var typeInfo = resolver.GetRequiredTypeInfo(typeof(PublicRoutableComponent));
        var instance = Assert.IsType<PublicRoutableComponent>(typeInfo.CreateInstance!(new ServiceCollection().BuildServiceProvider()));

        Assert.Equal(["generated"], typeInfo.Metadata);
        Assert.Single(typeInfo.Parameters);
        Assert.Equal(nameof(PublicRoutableComponent.Title), typeInfo.Parameters[0].Name);
        Assert.Null(instance.Title);
    }

    [Fact]
    public void CompositeResolver_FallsBackToReflectionForTypeNameAndAssemblyEnumeration()
    {
        using var resolver = new CompositeComponentTypeInfoResolver(
        [
            new SourceGeneratedComponentTypeInfoResolver(new StubMetadataResolver(Describe(typeof(DescriptorOnlyComponent)))),
            new ReflectionComponentTypeInfoResolver(),
        ]);

        var byType = resolver.GetRequiredTypeInfo(typeof(PublicRoutableComponent));
        var byName = resolver.GetRequiredTypeInfo(typeof(PublicRoutableComponent).Assembly.GetName().Name!, typeof(PublicRoutableComponent).FullName!);
        var byAssembly = resolver.GetTypeInfos(typeof(PublicRoutableComponent).Assembly);

        Assert.Same(byType, byName);
        Assert.Contains(byType, byAssembly);
        Assert.Contains(byAssembly, info => info.Type == typeof(DescriptorOnlyComponent));
        Assert.Contains(byAssembly, info => info.Type == typeof(PublicRoutableComponent));
        Assert.Single(byAssembly.Where(info => info.Type == typeof(PublicRoutableComponent)));
        Assert.Contains(byType.Metadata, item => item is RouteAttribute { Template: "/routable" });
    }

    [Fact]
    public void CompositeResolver_ConsultsResolversInOrderForEveryOperation()
    {
        var calls = new List<string>();
        var typeInfo = new ComponentTypeInfo(Describe(
            typeof(PublicRoutableComponent),
            createInstance: static _ => new PublicRoutableComponent()));
        var first = new RecordingResolver("first", calls);
        var second = new RecordingResolver("second", calls, typeInfo);
        var third = new RecordingResolver("third", calls, typeInfo);
        using var resolver = new CompositeComponentTypeInfoResolver([first, second, third]);
        var assembly = typeof(PublicRoutableComponent).Assembly;
        var assemblyName = assembly.GetName().Name!;
        var typeName = typeof(PublicRoutableComponent).FullName!;

        Assert.Same(typeInfo, resolver.GetTypeInfo(typeof(PublicRoutableComponent)));
        Assert.Equal(["first:type", "second:type"], calls);

        calls.Clear();
        Assert.Same(typeInfo, resolver.GetTypeInfo(assemblyName, typeName));
        Assert.Equal(["first:name", "second:name"], calls);

        calls.Clear();
        Assert.Same(typeInfo, Assert.Single(resolver.GetTypeInfos(assembly)));
        Assert.Equal(["first:assembly", "second:assembly", "third:assembly"], calls);
    }

    [Fact]
    public void Factory_UsesReflectionByDefault()
    {
        var resolver = ComponentTypeInfoResolverFactory.Create(new ServiceCollection().BuildServiceProvider());
        using var disposable = resolver as IDisposable;

        Assert.True(ComponentMetadataFeature.IsReflectionEnabledByDefault);
        Assert.NotNull(resolver.GetTypeInfo(typeof(PublicRoutableComponent)));
    }

    [ConditionalFact]
    [RemoteExecutionSupported]
    public void Factory_CanDisableReflectionThroughRuntimeConfiguration()
    {
        var options = new RemoteInvokeOptions();
        options.RuntimeConfigurationOptions.Add(ComponentMetadataFeature.SwitchName, false.ToString());

        using var remoteHandle = RemoteExecutor.Invoke(static () =>
        {
            var resolver = ComponentTypeInfoResolverFactory.Create(new ServiceCollection().BuildServiceProvider());
            using var disposable = resolver as IDisposable;

            Assert.False(ComponentMetadataFeature.IsReflectionEnabledByDefault);
            Assert.Null(resolver.GetTypeInfo(typeof(PublicRoutableComponent)));
            Assert.Null(resolver.GetTypeInfo(typeof(PublicRoutableComponent).Assembly.GetName().Name!, typeof(PublicRoutableComponent).FullName!));
            Assert.Empty(resolver.GetTypeInfos(typeof(PublicRoutableComponent).Assembly));
            Assert.Throws<NotSupportedException>(() => resolver.GetRequiredTypeInfo(typeof(PublicRoutableComponent)));
            var exception = Assert.Throws<NotSupportedException>(
                () => resolver.GetRequiredTypeInfos(typeof(PublicRoutableComponent).Assembly));
            Assert.Contains("Register generated component metadata", exception.Message);
        }, options);
    }

    [Fact]
    public void SourceGeneratedResolvers_AreProviderIsolated()
    {
        var first = new SourceGeneratedComponentTypeInfoResolver(new StubMetadataResolver(
            Describe(typeof(PublicRoutableComponent), metadata: ["first"])));
        var second = new SourceGeneratedComponentTypeInfoResolver(new StubMetadataResolver(
            Describe(typeof(PublicRoutableComponent), metadata: ["second"])));

        var firstInfo = first.GetRequiredTypeInfo(typeof(PublicRoutableComponent));
        var secondInfo = second.GetRequiredTypeInfo(typeof(PublicRoutableComponent));

        Assert.NotSame(firstInfo, secondInfo);
        Assert.Equal(["first"], firstInfo.Metadata);
        Assert.Equal(["second"], secondInfo.Metadata);
    }

    [Fact]
    public void ReflectionResolver_DoesNotCacheNegativeNameLookups()
    {
        using var reflectionResolver = new ReflectionComponentTypeInfoResolver();
        IComponentTypeInfoResolver resolver = reflectionResolver;
        var assemblyName = "DynamicComponentAssembly_" + Guid.NewGuid().ToString("N");
        var typeName = "DynamicComponent_" + Guid.NewGuid().ToString("N");

        Assert.Null(resolver.GetTypeInfo(assemblyName, typeName));

        CreateDynamicComponentAssembly(assemblyName, typeName);

        var typeInfo = resolver.GetRequiredTypeInfo(assemblyName, typeName);

        Assert.Equal(typeName, typeInfo.Type.Name);
    }

    [Fact]
    public void CompositeResolver_CachesPositiveResultsAcrossConcurrentLookups()
    {
        using var resolver = new CompositeComponentTypeInfoResolver(
        [
            new SourceGeneratedComponentTypeInfoResolver(new StubMetadataResolver(
                Describe(typeof(PublicRoutableComponent), createInstance: null, metadata: ["generated"]))),
            new ReflectionComponentTypeInfoResolver(),
        ]);
        var typeInfos = new ConcurrentBag<ComponentTypeInfo>();
        var assemblyResults = new ConcurrentBag<IReadOnlyList<ComponentTypeInfo>>();
        var assemblyName = typeof(PublicRoutableComponent).Assembly.GetName().Name!;
        var typeName = typeof(PublicRoutableComponent).FullName!;

        Parallel.For(0, 64, _ =>
        {
            typeInfos.Add(resolver.GetRequiredTypeInfo(typeof(PublicRoutableComponent)));
            typeInfos.Add(resolver.GetRequiredTypeInfo(assemblyName, typeName));
            assemblyResults.Add(resolver.GetTypeInfos(typeof(PublicRoutableComponent).Assembly));
        });

        var firstTypeInfo = typeInfos.First();
        Assert.All(typeInfos, info =>
        {
            Assert.Equal(firstTypeInfo.Type, info.Type);
            Assert.Equal(firstTypeInfo.Metadata, info.Metadata);
            Assert.Equal(firstTypeInfo.Parameters.Count, info.Parameters.Count);
            Assert.Equal(firstTypeInfo.CreateInstance is not null, info.CreateInstance is not null);
        });

        var cachedByType = resolver.GetRequiredTypeInfo(typeof(PublicRoutableComponent));
        var cachedByName = resolver.GetRequiredTypeInfo(assemblyName, typeName);
        Assert.Same(cachedByType, cachedByName);

        var firstAssembly = assemblyResults.First();
        Assert.All(assemblyResults, result => Assert.Equal(firstAssembly.Select(info => info.Type), result.Select(info => info.Type)));
        var cachedAssembly = resolver.GetTypeInfos(typeof(PublicRoutableComponent).Assembly);
        Assert.Same(cachedAssembly, resolver.GetTypeInfos(typeof(PublicRoutableComponent).Assembly));
    }

    private static ComponentDescriptor Describe(
        Type type,
        Func<IServiceProvider, IComponent>? createInstance = null,
        IReadOnlyList<ComponentParameterDescriptor>? parameters = null,
        IReadOnlyList<object>? metadata = null)
        => new()
        {
            Type = type,
            CreateInstance = createInstance,
            Parameters = parameters ?? [],
            Metadata = metadata ?? [],
        };

    private static void CreateDynamicComponentAssembly(string assemblyName, string typeName)
    {
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName(assemblyName),
            AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule("Main");
        var typeBuilder = moduleBuilder.DefineType(
            typeName,
            TypeAttributes.Public | TypeAttributes.Class);

        typeBuilder.AddInterfaceImplementation(typeof(IComponent));
        typeBuilder.DefineDefaultConstructor(MethodAttributes.Public);

        var attachMethod = typeBuilder.DefineMethod(
            nameof(IComponent.Attach),
            MethodAttributes.Public | MethodAttributes.Virtual,
            typeof(void),
            [typeof(RenderHandle)]);
        attachMethod.GetILGenerator().Emit(OpCodes.Ret);
        typeBuilder.DefineMethodOverride(attachMethod, typeof(IComponent).GetMethod(nameof(IComponent.Attach))!);

        var setParametersMethod = typeBuilder.DefineMethod(
            nameof(IComponent.SetParametersAsync),
            MethodAttributes.Public | MethodAttributes.Virtual,
            typeof(Task),
            [typeof(ParameterView)]);
        var setParametersIl = setParametersMethod.GetILGenerator();
        setParametersIl.Emit(OpCodes.Call, typeof(Task).GetProperty(nameof(Task.CompletedTask))!.GetGetMethod()!);
        setParametersIl.Emit(OpCodes.Ret);
        typeBuilder.DefineMethodOverride(setParametersMethod, typeof(IComponent).GetMethod(nameof(IComponent.SetParametersAsync))!);

        _ = typeBuilder.CreateType();
    }

    private sealed class StubMetadataResolver(params ComponentDescriptor[] descriptors) : IComponentMetadataResolver
    {
        private readonly Dictionary<Type, ComponentDescriptor> _descriptors = CreateDescriptorsByType(descriptors);

        public IReadOnlyList<ComponentDescriptor> Components => descriptors;

        public bool TryGetComponentDescriptor(Type type, [NotNullWhen(true)] out ComponentDescriptor? descriptor)
            => _descriptors.TryGetValue(type, out descriptor);

        private static Dictionary<Type, ComponentDescriptor> CreateDescriptorsByType(ComponentDescriptor[] descriptors)
        {
            var results = new Dictionary<Type, ComponentDescriptor>();
            foreach (var descriptor in descriptors)
            {
                results[descriptor.Type] = descriptor;
            }

            return results;
        }
    }

    private sealed class RecordingResolver(
        string name,
        List<string> calls,
        ComponentTypeInfo? typeInfo = null) : IComponentTypeInfoResolver
    {
        public ComponentTypeInfo? GetTypeInfo(Type componentType)
        {
            calls.Add($"{name}:type");
            return typeInfo?.Type == componentType ? typeInfo : null;
        }

        public ComponentTypeInfo? GetTypeInfo(string assemblyName, string typeName)
        {
            calls.Add($"{name}:name");
            return typeInfo is not null &&
                typeInfo.Type.Assembly.GetName().Name == assemblyName &&
                typeInfo.Type.FullName == typeName
                    ? typeInfo
                    : null;
        }

        public IReadOnlyList<ComponentTypeInfo> GetTypeInfos(Assembly assembly)
        {
            calls.Add($"{name}:assembly");
            return typeInfo?.Type.Assembly == assembly ? [typeInfo] : [];
        }
    }

    [Route("/routable")]
    public sealed class PublicRoutableComponent : IComponent
    {
        [Parameter]
        public string? Title { get; set; }

        public void Attach(RenderHandle renderHandle)
            => throw new NotImplementedException();

        public Task SetParametersAsync(ParameterView parameters)
            => throw new NotImplementedException();
    }

    public sealed class DescriptorOnlyComponent : IComponent
    {
        public void Attach(RenderHandle renderHandle)
            => throw new NotImplementedException();

        public Task SetParametersAsync(ParameterView parameters)
            => throw new NotImplementedException();
    }
}
