// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Microsoft.JSInterop.Infrastructure;

public class DotNetDispatcherDescriptorTest
{
    private const string DescribedAssemblyName = "TestAssembly";

    private static readonly string ThisAssemblyName = typeof(DotNetDispatcherDescriptorTest).Assembly.GetName().Name;
    private static TaskCompletionSource<string> _pendingReflectionResult = new();

    [Fact]
    public void InvokeUsesTheDescriptorForAStaticCall()
    {
        var runtime = new DescriptorJSRuntime(StaticDescriptor("Greet", (_, args, _) => $"\"hello {ReadFirstString(args)}\""));

        var result = DotNetDispatcher.Invoke(runtime, new DotNetInvocationInfo(DescribedAssemblyName, "Greet", default, default), "[\"world\"]");

        Assert.Equal("\"hello world\"", result);
    }

    [Fact]
    public void InvokeUsesTheDescriptorForAnInstanceCall()
    {
        var runtime = new DescriptorJSRuntime(InstanceDescriptor(typeof(Receiver), "Read", (instance, _, _) => $"\"{((Receiver)instance).Value}\""));
        var objectId = runtime.TrackObjectReference(DotNetObjectReference.Create(new Receiver()));

        var result = DotNetDispatcher.Invoke(runtime, new DotNetInvocationInfo(null, "Read", objectId, default), "[]");

        Assert.Equal("\"receiver\"", result);
    }

    [Fact]
    public void InvokeFindsADescriptorDeclaredOnABaseType()
    {
        var runtime = new DescriptorJSRuntime(
            InstanceDescriptor(typeof(Receiver), "Read", (instance, _, _) => $"\"{((Receiver)instance).Value}\""),
            TypeCoverageDescriptor(typeof(DerivedReceiver)));
        var objectId = runtime.TrackObjectReference(DotNetObjectReference.Create(new DerivedReceiver()));

        var result = DotNetDispatcher.Invoke(runtime, new DotNetInvocationInfo(null, "Read", objectId, default), "[]");

        Assert.Equal("\"derived\"", result);
    }

    [Fact]
    public void StaticAndInstanceDescriptorsUseSeparateKeys()
    {
        var runtime = new DescriptorJSRuntime(
            StaticDescriptor("Same", (_, _, _) => "\"static\""),
            InstanceDescriptor(typeof(Receiver), "Same", (_, _, _) => "\"instance\""));
        var objectId = runtime.TrackObjectReference(DotNetObjectReference.Create(new Receiver()));

        var staticResult = DotNetDispatcher.Invoke(
            runtime,
            new DotNetInvocationInfo(DescribedAssemblyName, "Same", default, default),
            "[]");
        var instanceResult = DotNetDispatcher.Invoke(
            runtime,
            new DotNetInvocationInfo(null, "Same", objectId, default),
            "[]");

        Assert.Equal("\"static\"", staticResult);
        Assert.Equal("\"instance\"", instanceResult);
    }

    [Fact]
    public void DuplicateStaticDescriptorsFailInRegistrationOrder()
    {
        var runtime = new DescriptorJSRuntime(
            StaticDescriptor("Duplicate", (_, _, _) => "\"first\""),
            StaticDescriptor("Duplicate", (_, _, _) => "\"second\""));

        var exception = Assert.Throws<InvalidOperationException>(() => runtime.InvokableMethodResolver);

        Assert.Contains("assembly 'TestAssembly'", exception.Message);
        Assert.Contains("identifier 'Duplicate'", exception.Message);
    }

    [Fact]
    public void DuplicateInstanceDescriptorsFailInRegistrationOrder()
    {
        var runtime = new DescriptorJSRuntime(
            InstanceDescriptor(typeof(Receiver), "Duplicate", (_, _, _) => "\"first\""),
            InstanceDescriptor(typeof(Receiver), "Duplicate", (_, _, _) => "\"second\""));

        var exception = Assert.Throws<InvalidOperationException>(() => runtime.InvokableMethodResolver);

        Assert.Contains("type Receiver", exception.Message);
        Assert.Contains("identifier 'Duplicate'", exception.Message);
    }

    [Fact]
    public void DuplicateGeneratedContributionsKeepTheFirstRegistration()
    {
        var first = CloneWithMethodKey(
            StaticDescriptor("Same", (_, _, _) => "\"first\""),
            "M:Test.Same#0");
        var second = CloneWithMethodKey(
            StaticDescriptor("Same", (_, _, _) => "\"second\""),
            "M:Test.Same#0");
        var runtime = new DescriptorJSRuntime(first, second);

        var result = DotNetDispatcher.Invoke(
            runtime,
            new DotNetInvocationInfo(DescribedAssemblyName, "Same", default, default),
            "[]");

        Assert.Equal("\"first\"", result);
    }

    [Fact]
    public void DifferentGeneratedContributionsWithTheSameLookupKeyConflict()
    {
        var runtime = new DescriptorJSRuntime(
            CloneWithMethodKey(StaticDescriptor("Same", (_, _, _) => "\"first\""), "M:Test.First#0"),
            CloneWithMethodKey(StaticDescriptor("Same", (_, _, _) => "\"second\""), "M:Test.Second#0"));

        Assert.Throws<InvalidOperationException>(() => runtime.InvokableMethodResolver);
    }

    [Fact]
    public void GeneratedOverrideBlockerHidesBaseDescriptor()
    {
        var resolver = new SourceGeneratedJSInvokableMethodResolver(
        [
            InstanceDescriptor(typeof(ReflectionBase), "virtual", (_, _, _) => "\"base\"", JSInvokableMethodKind.Override),
            InstanceDescriptor(
                typeof(UnannotatedOverride),
                "virtual",
                (_, _, _) => throw new InvalidOperationException(),
                JSInvokableMethodKind.OverrideBlocker),
        ]);

        Assert.False(resolver.TryResolve(
            new JSInvokableMethodInfo(null, typeof(UnannotatedOverride), "virtual"),
            out _));
    }

    [Fact]
    public void GeneratedAnnotatedOverrideHidesBaseDescriptor()
    {
        var derived = InstanceDescriptor(
            typeof(AnnotatedOverride),
            "virtual",
            (_, _, _) => "\"derived\"",
            JSInvokableMethodKind.Override);
        var resolver = new SourceGeneratedJSInvokableMethodResolver(
        [
            InstanceDescriptor(typeof(ReflectionBase), "virtual", (_, _, _) => "\"base\"", JSInvokableMethodKind.Override),
            derived,
        ]);

        Assert.True(resolver.TryResolve(
            new JSInvokableMethodInfo(null, typeof(AnnotatedOverride), "virtual"),
            out var descriptor));
        Assert.Same(derived, descriptor);
    }

    [Fact]
    public void GeneratedNewSlotWithSameAliasConflictsWithBaseDescriptor()
    {
        var resolver = new SourceGeneratedJSInvokableMethodResolver(
        [
            InstanceDescriptor(typeof(ReflectionBase), "virtual", (_, _, _) => "\"base\"", JSInvokableMethodKind.Override),
            InstanceDescriptor(typeof(AnnotatedNewSlot), "virtual", (_, _, _) => "\"new\""),
            TypeCoverageDescriptor(typeof(AnnotatedNewSlot)),
        ]);

        Assert.Throws<InvalidOperationException>(() => resolver.TryResolve(
            new JSInvokableMethodInfo(null, typeof(AnnotatedNewSlot), "virtual"),
            out _));
    }

    [Fact]
    public void GeneratedVirtualBaseRequiresCoverageForDerivedReceiver()
    {
        var resolver = new SourceGeneratedJSInvokableMethodResolver(
        [
            InstanceDescriptor(typeof(ReflectionBase), "virtual", (_, _, _) => "\"base\"", JSInvokableMethodKind.Override),
        ]);

        Assert.False(resolver.TryResolve(
            new JSInvokableMethodInfo(null, typeof(UnannotatedOverride), "virtual"),
            out _));
    }

    [Fact]
    public void GeneratedVirtualBaseAllowsCoveredDerivedReceiver()
    {
        var descriptor = InstanceDescriptor(
            typeof(ReflectionBase),
            "virtual",
            (_, _, _) => "\"base\"",
            JSInvokableMethodKind.Override);
        var resolver = new SourceGeneratedJSInvokableMethodResolver(
        [
            descriptor,
            TypeCoverageDescriptor(typeof(CoveredReflectionDerived)),
        ]);

        Assert.True(resolver.TryResolve(
            new JSInvokableMethodInfo(null, typeof(CoveredReflectionDerived), "virtual"),
            out var resolved));
        Assert.Same(descriptor, resolved);
    }

    [Fact]
    public void GeneratedGenericOverrideBlockerMatchesConstructedReceiver()
    {
        var resolver = new SourceGeneratedJSInvokableMethodResolver(
        [
            InstanceDescriptor(typeof(ReflectionBase), "virtual", (_, _, _) => "\"base\"", JSInvokableMethodKind.Override),
            TypeCoverageDescriptor(typeof(GenericUnannotatedOverride<>)),
            InstanceDescriptor(
                typeof(GenericUnannotatedOverride<>),
                "virtual",
                (_, _, _) => throw new InvalidOperationException(),
                JSInvokableMethodKind.OverrideBlocker),
        ]);

        Assert.False(resolver.TryResolve(
            new JSInvokableMethodInfo(null, typeof(GenericUnannotatedOverride<int>), "virtual"),
            out _));
    }

    [Fact]
    public void GeneratedNonVirtualBaseRequiresDerivedCoverage()
    {
        var descriptor = InstanceDescriptor(typeof(ReflectionBase), "inherited", (_, _, _) => "\"inherited\"");
        var resolver = new SourceGeneratedJSInvokableMethodResolver([descriptor]);

        Assert.False(resolver.TryResolve(
            new JSInvokableMethodInfo(null, typeof(UnannotatedOverride), "inherited"),
            out _));
    }

    [Fact]
    public void GeneratedNonVirtualBaseAllowsCoveredDerivedReceiver()
    {
        var descriptor = InstanceDescriptor(typeof(ReflectionBase), "inherited", (_, _, _) => "\"inherited\"");
        var resolver = new SourceGeneratedJSInvokableMethodResolver(
        [
            descriptor,
            TypeCoverageDescriptor(typeof(CoveredReflectionDerived)),
        ]);

        Assert.True(resolver.TryResolve(
            new JSInvokableMethodInfo(null, typeof(CoveredReflectionDerived), "inherited"),
            out var resolved));
        Assert.Same(descriptor, resolved);
    }

    [Fact]
    public void GeneratedNonVirtualBaseMissesForUncoveredGenericNewSlot()
    {
        var descriptor = InstanceDescriptor(typeof(ReflectionBase), "inherited", (_, _, _) => "\"inherited\"");
        var resolver = new SourceGeneratedJSInvokableMethodResolver([descriptor]);

        Assert.False(resolver.TryResolve(
            new JSInvokableMethodInfo(null, typeof(GenericAnnotatedNewSlot<int>), "inherited"),
            out _));
    }

    [Fact]
    public void ReflectionCompatibilityDetectsDuplicateForUncoveredGenericNewSlot()
    {
        var runtime = new DescriptorJSRuntime(
            InstanceDescriptor(typeof(ReflectionBase), "inherited", (_, _, _) => "\"generated-base\""));
        var objectId = runtime.TrackObjectReference(
            DotNetObjectReference.Create<ReflectionBase>(new GenericAnnotatedNewSlot<int>()));

        Assert.Throws<InvalidOperationException>(() => DotNetDispatcher.Invoke(
            runtime,
            new DotNetInvocationInfo(null, "inherited", objectId, default),
            "[]"));
    }

    [Fact]
    public void ReflectionCompatibilityHandlesUncoveredAnnotatedOverride()
    {
        var runtime = new DescriptorJSRuntime(
            InstanceDescriptor(
                typeof(ReflectionBase),
                "virtual",
                (_, _, _) => "\"generated-base\"",
                JSInvokableMethodKind.Override));
        var objectId = runtime.TrackObjectReference(
            DotNetObjectReference.Create<ReflectionBase>(new PrivateAnnotatedOverride()));

        var result = DotNetDispatcher.Invoke(
            runtime,
            new DotNetInvocationInfo(null, "virtual", objectId, default),
            "[]");

        Assert.Equal("\"private-derived\"", result);
    }

    [Fact]
    public void ReflectionCompatibilityDoesNotInheritAttributeThroughGenericOverride()
    {
        var runtime = new DescriptorJSRuntime(
            InstanceDescriptor(
                typeof(ReflectionBase),
                "virtual",
                (target, _, _) => $"\"{((ReflectionBase)target).VirtualMethod()}\"",
                JSInvokableMethodKind.Override),
            TypeCoverageDescriptor(typeof(GenericUnannotatedOverride<>)),
            InstanceDescriptor(
                typeof(GenericUnannotatedOverride<>),
                "virtual",
                (_, _, _) => throw new InvalidOperationException(),
                JSInvokableMethodKind.OverrideBlocker));
        var objectId = runtime.TrackObjectReference(
            DotNetObjectReference.Create<ReflectionBase>(new GenericUnannotatedOverride<int>()));

        Assert.Throws<ArgumentException>(() => DotNetDispatcher.Invoke(
            runtime,
            new DotNetInvocationInfo(null, "virtual", objectId, default),
            "[]"));
    }

    [Fact]
    public void InvokeFallsBackToReflectionWhenNoDescriptorMatches()
    {
        var runtime = new DescriptorJSRuntime(StaticDescriptor("SomethingElse", (_, _, _) => "\"unused\""));

        var result = DotNetDispatcher.Invoke(runtime, new DotNetInvocationInfo(ThisAssemblyName, nameof(ReflectedMethod), default, default), "[]");

        Assert.Equal("\"reflected\"", result);
    }

    [Fact]
    public void InvokePrefersTheDescriptorOverReflection()
    {
        var runtime = new DescriptorJSRuntime(new JSInvokableMethodDescriptor
        {
            AssemblyName = ThisAssemblyName,
            TargetType = typeof(DotNetDispatcherDescriptorTest),
            Identifier = nameof(ReflectedMethod),
            IsStatic = true,
            Invoke = (_, _, _) => new ValueTask<string>("\"described\""),
        });

        var result = DotNetDispatcher.Invoke(runtime, new DotNetInvocationInfo(ThisAssemblyName, nameof(ReflectedMethod), default, default), "[]");

        Assert.Equal("\"described\"", result);
    }

    [Fact]
    public void DisposeIsStillHandledWhenDescriptorsArePresent()
    {
        var runtime = new DescriptorJSRuntime(InstanceDescriptor(typeof(Receiver), "__Dispose", (_, _, _) => "\"should not run\""));
        var objectId = runtime.TrackObjectReference(DotNetObjectReference.Create(new Receiver()));

        DotNetDispatcher.Invoke(runtime, new DotNetInvocationInfo(null, "__Dispose", objectId, default), "[]");

        Assert.Throws<ArgumentException>(() => runtime.GetObjectReference(objectId));
    }

    [Fact]
    public void InvokeThrowsWhenTheDescriptorResultHasNotCompleted()
    {
        var completion = new TaskCompletionSource<string>();
        var runtime = new DescriptorJSRuntime(StaticDescriptor("Pending", (_, _, _) => new ValueTask<string>(completion.Task)));

        var exception = Assert.Throws<InvalidOperationException>(
            () => DotNetDispatcher.Invoke(runtime, new DotNetInvocationInfo(DescribedAssemblyName, "Pending", default, default), "[]"));

        Assert.Contains("'Pending'", exception.Message);
    }

    [Fact]
    public void BeginInvokeDotNetCompletesSynchronousDescriptorResults()
    {
        var runtime = new DescriptorJSRuntime(StaticDescriptor("Greet", (_, _, _) => "\"hi\""));

        DotNetDispatcher.BeginInvokeDotNet(runtime, new DotNetInvocationInfo(DescribedAssemblyName, "Greet", default, "call-1"), "[]");

        Assert.Equal("call-1", runtime.LastCompletionCallId);
        Assert.True(runtime.LastCompletionResult.Success);
        Assert.Equal("\"hi\"", runtime.LastCompletionResult.ResultJson);
    }

    [Fact]
    public void BeginInvokeDotNetReportsNullForADescriptorWithNoResult()
    {
        var runtime = new DescriptorJSRuntime(StaticDescriptor("DoWork", (_, _, _) => (string)null));

        DotNetDispatcher.BeginInvokeDotNet(runtime, new DotNetInvocationInfo(DescribedAssemblyName, "DoWork", default, "call-1"), "[]");

        Assert.True(runtime.LastCompletionResult.Success);
        Assert.Equal("null", runtime.LastCompletionResult.ResultJson);
    }

    [Fact]
    public async Task BeginInvokeDotNetCompletesAsynchronousDescriptorResults()
    {
        var completion = new TaskCompletionSource<string>();
        var runtime = new DescriptorJSRuntime(StaticDescriptor("Later", (_, _, _) => new ValueTask<string>(completion.Task)));
        var nextCompletion = runtime.NextCompletion;

        DotNetDispatcher.BeginInvokeDotNet(runtime, new DotNetInvocationInfo(DescribedAssemblyName, "Later", default, "call-1"), "[]");

        Assert.Null(runtime.LastCompletionCallId);

        completion.SetResult("\"eventually\"");
        await nextCompletion;

        Assert.Equal("call-1", runtime.LastCompletionCallId);
        Assert.Equal("\"eventually\"", runtime.LastCompletionResult.ResultJson);
    }

    [Fact]
    public async Task BeginInvokeDotNetReportsAsynchronousDescriptorFailures()
    {
        var completion = new TaskCompletionSource<string>();
        var runtime = new DescriptorJSRuntime(StaticDescriptor("Later", (_, _, _) => new ValueTask<string>(completion.Task)));
        var nextCompletion = runtime.NextCompletion;

        DotNetDispatcher.BeginInvokeDotNet(runtime, new DotNetInvocationInfo(DescribedAssemblyName, "Later", default, "call-1"), "[]");

        completion.SetException(new InvalidTimeZoneException("boom"));
        await nextCompletion;

        Assert.False(runtime.LastCompletionResult.Success);
        Assert.Equal("InvocationFailure", runtime.LastCompletionResult.ErrorKind);
        Assert.IsType<InvalidTimeZoneException>(runtime.LastCompletionResult.Exception);
    }

    [Fact]
    public void BeginInvokeDotNetReportsSynchronousDescriptorFailures()
    {
        var runtime = new DescriptorJSRuntime(StaticDescriptor("Throws", (Func<object, string, JsonSerializerOptions, ValueTask<string>>)((_, _, _) => throw new InvalidTimeZoneException("boom"))));

        DotNetDispatcher.BeginInvokeDotNet(runtime, new DotNetInvocationInfo(DescribedAssemblyName, "Throws", default, "call-1"), "[]");

        Assert.False(runtime.LastCompletionResult.Success);
        Assert.Equal("InvocationFailure", runtime.LastCompletionResult.ErrorKind);
        Assert.IsType<InvalidTimeZoneException>(runtime.LastCompletionResult.Exception);
    }

    [Fact]
    public void RuntimeWithoutDescriptorsBuildsReflectionOnlyResolver()
    {
        Assert.IsType<ReflectionJSInvokableMethodResolver>(
            Assert.Single(new TestJSRuntime().InvokableMethodResolver.Resolvers));
        Assert.IsType<ReflectionJSInvokableMethodResolver>(
            Assert.Single(new DescriptorJSRuntime().InvokableMethodResolver.Resolvers));
    }

    [Fact]
    public void ResolverChainIsGeneratedFirstAndReflectionLast()
    {
        var runtime = new DescriptorJSRuntime(StaticDescriptor("Generated", (_, _, _) => "\"generated\""));

        Assert.Collection(
            runtime.InvokableMethodResolver.Resolvers,
            resolver => Assert.IsType<SourceGeneratedJSInvokableMethodResolver>(resolver),
            resolver => Assert.IsType<ReflectionJSInvokableMethodResolver>(resolver));
    }

    [Fact]
    public void ResolverChainIsCreatedLazilyOncePerRuntime()
    {
        var runtime = new CountingDescriptorJSRuntime(StaticDescriptor("Generated", (_, _, _) => "\"generated\""));

        Assert.Equal(0, runtime.InvokableMethodsReadCount);
        Assert.Same(runtime.InvokableMethodResolver, runtime.InvokableMethodResolver);
        Assert.Equal(1, runtime.InvokableMethodsReadCount);
    }

    [Fact]
    public void ReflectionResolverCreatesTheSameDescriptorShape()
    {
        var resolver = new ReflectionJSInvokableMethodResolver();

        Assert.True(resolver.TryResolve(
            new JSInvokableMethodInfo(ThisAssemblyName, null, nameof(ReflectedMethod)),
            out var staticDescriptor));
        Assert.True(staticDescriptor.IsStatic);
        Assert.Equal(ThisAssemblyName, staticDescriptor.AssemblyName);
        Assert.Equal(typeof(DotNetDispatcherDescriptorTest), staticDescriptor.TargetType);

        Assert.True(resolver.TryResolve(
            new JSInvokableMethodInfo(null, typeof(Receiver), nameof(Receiver.ReflectedInstance)),
            out var instanceDescriptor));
        Assert.False(instanceDescriptor.IsStatic);
        Assert.Equal(typeof(Receiver), instanceDescriptor.TargetType);
    }

    [Fact]
    public void InvokeAcceptsACompletedReflectedAwaitable()
    {
        var result = DotNetDispatcher.Invoke(
            new TestJSRuntime(),
            new DotNetInvocationInfo(ThisAssemblyName, nameof(CompletedReflectionResult), default, default),
            "[]");

        Assert.Equal("\"completed\"", result);
    }

    [Fact]
    public void InvokeRejectsAPendingReflectedAwaitable()
    {
        _pendingReflectionResult = new TaskCompletionSource<string>();

        var exception = Assert.Throws<InvalidOperationException>(() => DotNetDispatcher.Invoke(
            new TestJSRuntime(),
            new DotNetInvocationInfo(ThisAssemblyName, nameof(PendingReflectionResult), default, default),
            "[]"));

        _pendingReflectionResult.SetResult("completed");
        Assert.Contains(nameof(PendingReflectionResult), exception.Message);
    }

    [Fact]
    public void ReflectionDescriptorConsumesAndClearsPendingByteArrays()
    {
        var runtime = new TestJSRuntime();
        DotNetDispatcher.ReceiveByteArray(runtime, 0, [1, 2, 3]);

        var result = DotNetDispatcher.Invoke(
            runtime,
            new DotNetInvocationInfo(ThisAssemblyName, nameof(GetByteCount), default, default),
            """[{"__byte[]":0}]""");

        Assert.Equal("3", result);
        Assert.Equal(0, runtime.ByteArraysToBeRevived.Count);
    }

    [Fact]
    public void ReflectionDoesNotInheritAttributeThroughUnannotatedOverride()
    {
        var runtime = new TestJSRuntime();
        var objectId = runtime.TrackObjectReference(
            DotNetObjectReference.Create<ReflectionBase>(new UnannotatedOverride()));

        Assert.Throws<ArgumentException>(() => DotNetDispatcher.Invoke(
            runtime,
            new DotNetInvocationInfo(null, "virtual", objectId, default),
            "[]"));
    }

    [Fact]
    public void ReflectionUsesAnnotatedOverrideAndInheritedNonVirtualMethod()
    {
        var runtime = new TestJSRuntime();
        var objectId = runtime.TrackObjectReference(
            DotNetObjectReference.Create<ReflectionBase>(new AnnotatedOverride()));

        Assert.Equal("\"derived\"", DotNetDispatcher.Invoke(
            runtime,
            new DotNetInvocationInfo(null, "virtual", objectId, default),
            "[]"));
        Assert.Equal("\"inherited\"", DotNetDispatcher.Invoke(
            runtime,
            new DotNetInvocationInfo(null, "inherited", objectId, default),
            "[]"));
    }

    [Fact]
    public void ReflectionUnannotatedNewSlotDoesNotHideBaseMethod()
    {
        var runtime = new TestJSRuntime();
        var objectId = runtime.TrackObjectReference(
            DotNetObjectReference.Create<ReflectionBase>(new UnannotatedNewSlot()));

        Assert.Equal("\"base-new-slot\"", DotNetDispatcher.Invoke(
            runtime,
            new DotNetInvocationInfo(null, "new-slot", objectId, default),
            "[]"));
    }

    [Fact]
    public void ReflectionAnnotatedNewSlotWithSameAliasConflicts()
    {
        var runtime = new TestJSRuntime();
        var objectId = runtime.TrackObjectReference(
            DotNetObjectReference.Create<ReflectionBase>(new AnnotatedNewSlot()));

        Assert.Throws<InvalidOperationException>(() => DotNetDispatcher.Invoke(
            runtime,
            new DotNetInvocationInfo(null, "virtual", objectId, default),
            "[]"));
    }

    [Theory]
    [InlineData(nameof(ObjectReturningVoidTask), "{}")]
    [InlineData(nameof(TaskDerivedResult), "\"derived-task\"")]
    [InlineData(nameof(ObjectReturningTask), "\"object-task\"")]
    [InlineData(nameof(PolymorphicTaskResult), """{"derivedValue":"derived","baseValue":"base"}""")]
    [InlineData(nameof(PolymorphicResult), """{"derivedValue":"derived","baseValue":"base"}""")]
    public void ReflectionUsesRuntimeReturnShapeAndResultType(string identifier, string expected)
    {
        var result = DotNetDispatcher.Invoke(
            new TestJSRuntime(),
            new DotNetInvocationInfo(ThisAssemblyName, identifier, default, default),
            "[]");

        Assert.Equal(expected, result);
    }

    [JSInvokable]
    public static string ReflectedMethod() => "reflected";

    [JSInvokable]
    public static Task<string> CompletedReflectionResult() => Task.FromResult("completed");

    [JSInvokable]
    public static Task<string> PendingReflectionResult() => _pendingReflectionResult.Task;

    [JSInvokable]
    public static int GetByteCount(byte[] value) => value.Length;

    [JSInvokable]
    public static object ObjectReturningVoidTask() => Task.CompletedTask;

    [JSInvokable]
    public static StringTask TaskDerivedResult() => new("derived-task");

    [JSInvokable]
    public static object ObjectReturningTask() => Task.FromResult("object-task");

    [JSInvokable]
    public static Task<BaseResult> PolymorphicTaskResult()
        => Task.FromResult<BaseResult>(new DerivedResult());

    [JSInvokable]
    public static BaseResult PolymorphicResult() => new DerivedResult();

    private static string ReadFirstString(string argsJson)
        => JsonSerializer.Deserialize<string[]>(argsJson)[0];

    private static JSInvokableMethodDescriptor StaticDescriptor(string identifier, Func<object, string, JsonSerializerOptions, ValueTask<string>> invoke)
        => new()
        {
            AssemblyName = DescribedAssemblyName,
            TargetType = typeof(DotNetDispatcherDescriptorTest),
            Identifier = identifier,
            IsStatic = true,
            Invoke = invoke,
        };

    private static JSInvokableMethodDescriptor StaticDescriptor(string identifier, Func<object, string, JsonSerializerOptions, string> invoke)
        => StaticDescriptor(identifier, (target, args, options) => new ValueTask<string>(invoke(target, args, options)));

    private static JSInvokableMethodDescriptor InstanceDescriptor(
        Type targetType,
        string identifier,
        Func<object, string, JsonSerializerOptions, string> invoke,
        JSInvokableMethodKind kind = JSInvokableMethodKind.Method)
        => new()
        {
            AssemblyName = DescribedAssemblyName,
            TargetType = targetType,
            Identifier = identifier,
            IsStatic = false,
            Kind = kind,
            Invoke = (target, args, options) => new ValueTask<string>(invoke(target, args, options)),
        };

    private static JSInvokableMethodDescriptor CloneWithMethodKey(
        JSInvokableMethodDescriptor descriptor,
        string methodKey)
        => new()
        {
            AssemblyName = descriptor.AssemblyName,
            TargetType = descriptor.TargetType,
            Identifier = descriptor.Identifier,
            IsStatic = descriptor.IsStatic,
            MethodKey = methodKey,
            Kind = descriptor.Kind,
            Invoke = descriptor.Invoke,
        };

    private static JSInvokableMethodDescriptor TypeCoverageDescriptor(Type targetType)
        => new()
        {
            AssemblyName = DescribedAssemblyName,
            TargetType = targetType,
            Identifier = string.Empty,
            IsStatic = false,
            Kind = JSInvokableMethodKind.OverrideBlocker,
            Invoke = (_, _, _) => throw new InvalidOperationException(),
        };

    public class Receiver
    {
        public virtual string Value => "receiver";

        [JSInvokable]
        public string ReflectedInstance() => Value;
    }

    public sealed class DerivedReceiver : Receiver
    {
        public override string Value => "derived";
    }

    public class ReflectionBase
    {
        [JSInvokable("virtual")]
        public virtual string VirtualMethod() => "base";

        [JSInvokable("inherited")]
        public string InheritedMethod() => "inherited";

        [JSInvokable("new-slot")]
        public string NewSlotMethod() => "base-new-slot";
    }

    public sealed class UnannotatedOverride : ReflectionBase
    {
        public override string VirtualMethod() => "unannotated";
    }

    public sealed class AnnotatedOverride : ReflectionBase
    {
        [JSInvokable("virtual")]
        public override string VirtualMethod() => "derived";
    }

    public sealed class GenericUnannotatedOverride<T> : ReflectionBase
    {
        public override string VirtualMethod() => "generic-unannotated";
    }

    public sealed class GenericAnnotatedNewSlot<T> : ReflectionBase
    {
        [JSInvokable("inherited")]
        public new string InheritedMethod() => "generic-new-slot";
    }

    public sealed class CoveredReflectionDerived : ReflectionBase
    {
    }

    private sealed class PrivateAnnotatedOverride : ReflectionBase
    {
        [JSInvokable("virtual")]
        public override string VirtualMethod() => "private-derived";
    }

    public sealed class UnannotatedNewSlot : ReflectionBase
    {
        public new string NewSlotMethod() => "new";
    }

    public sealed class AnnotatedNewSlot : ReflectionBase
    {
        [JSInvokable("virtual")]
        public new string VirtualMethod() => "new";
    }

    public class BaseResult
    {
        public string BaseValue => "base";
    }

    public sealed class DerivedResult : BaseResult
    {
        public string DerivedValue => "derived";
    }

    public sealed class StringTask : Task<string>
    {
        public StringTask(string value)
            : base(() => value)
        {
            RunSynchronously();
        }
    }

    private class DescriptorJSRuntime : JSRuntime
    {
        private readonly JSInvokableMethodDescriptor[] _descriptors;
        private TaskCompletionSource _nextCompletion = new();

        public DescriptorJSRuntime(params JSInvokableMethodDescriptor[] descriptors)
        {
            _descriptors = descriptors;
        }

        public Task NextCompletion => _nextCompletion.Task;

        public string LastCompletionCallId { get; private set; }

        public DotNetInvocationResult LastCompletionResult { get; private set; }

        protected internal override IReadOnlyList<JSInvokableMethodDescriptor> InvokableMethods
            => _descriptors.Length == 0 ? null : _descriptors;

        protected override void BeginInvokeJS(long taskId, string identifier, [StringSyntax("Json")] string argsJson, JSCallResultType resultType, long targetInstanceId)
            => throw new NotImplementedException();

        protected internal override void EndInvokeDotNet(DotNetInvocationInfo invocationInfo, in DotNetInvocationResult invocationResult)
        {
            LastCompletionCallId = invocationInfo.CallId;
            LastCompletionResult = invocationResult;

            var completion = _nextCompletion;
            _nextCompletion = new TaskCompletionSource();
            completion.TrySetResult();
        }

    }

    private sealed class CountingDescriptorJSRuntime(params JSInvokableMethodDescriptor[] descriptors)
        : DescriptorJSRuntime(descriptors)
    {
        public int InvokableMethodsReadCount { get; private set; }

        protected internal override IReadOnlyList<JSInvokableMethodDescriptor> InvokableMethods
        {
            get
            {
                InvokableMethodsReadCount++;
                return base.InvokableMethods;
            }
        }
    }
}
