// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components.Reflection;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.DotNet.RemoteExecutor;

namespace Microsoft.AspNetCore.Components;

public class ComponentBindingTest
{
    [Theory]
    [InlineData(0, "component")]
    [InlineData(1, "component")]
    [InlineData(2, "component")]
    [InlineData(3, "component")]
    [InlineData(4, "component")]
    [InlineData(5, "component")]
    [InlineData(6, "component")]
    [InlineData(7, "component")]
    [InlineData(0, "typed")]
    [InlineData(1, "typed")]
    [InlineData(2, "typed")]
    [InlineData(3, "typed")]
    [InlineData(4, "typed")]
    [InlineData(5, "typed")]
    [InlineData(6, "typed")]
    [InlineData(7, "typed")]
    [InlineData(0, "boxed")]
    [InlineData(1, "boxed")]
    [InlineData(2, "boxed")]
    [InlineData(3, "boxed")]
    [InlineData(4, "boxed")]
    [InlineData(5, "boxed")]
    [InlineData(6, "boxed")]
    [InlineData(7, "boxed")]
    public void MissingBoundCallbackWithCapture_Throws(int callbackKind, string route)
    {
        var target = new CaptureComponent();
        var callback = CreateBinding(new EventReceiver(), callbackKind);
        using var builder = BuildParameters(typeof(CaptureComponent), callback, route);
        var parameters = new ParameterView(ParameterViewLifetime.Unbound, builder.GetFrames().Array, 0);

        var exception = Assert.Throws<InvalidOperationException>(() => parameters.SetParameterProperties(target));

        Assert.Equal(
            $"Object of type '{typeof(CaptureComponent).FullName}' does not have a property matching the name 'ValueChanged'.",
            exception.Message);
        Assert.Empty(target.Attributes);
    }

    [Theory]
    [InlineData("ValueChanged")]
    [InlineData("OnUpdate")]
    public void MissingBoundCallbackWithoutCapture_Throws(string callbackName)
    {
        var target = new NoCaptureComponent();
        using var builder = BuildParameters(typeof(NoCaptureComponent), CreateBinding(new EventReceiver(), 0), "component", callbackName, includeOrdinaryAttribute: false);
        var parameters = new ParameterView(ParameterViewLifetime.Unbound, builder.GetFrames().Array, 0);

        var exception = Assert.Throws<InvalidOperationException>(() => parameters.SetParameterProperties(target));

        Assert.Equal(
            $"Object of type '{typeof(NoCaptureComponent).FullName}' does not have a property matching the name '{callbackName}'.",
            exception.Message);
    }

    [Fact]
    public void MissingCustomBoundCallbackWithCapture_Throws()
    {
        using var builder = BuildParameters(typeof(CaptureComponent), CreateBinding(new EventReceiver(), 0), "component", "OnUpdate");
        var parameters = new ParameterView(ParameterViewLifetime.Unbound, builder.GetFrames().Array, 0);

        var exception = Assert.Throws<InvalidOperationException>(() => parameters.SetParameterProperties(new CaptureComponent()));

        Assert.Contains("matching the name 'OnUpdate'", exception.Message);
    }

    [Fact]
    public void BoundCallbackMatchingCascadingOnlyParameter_Throws()
    {
        using var builder = BuildParameters(typeof(CascadingCallbackComponent), CreateBinding(new EventReceiver(), 0), "component");
        var parameters = new ParameterView(ParameterViewLifetime.Unbound, builder.GetFrames().Array, 0);

        var exception = Assert.Throws<InvalidOperationException>(() => parameters.SetParameterProperties(new CascadingCallbackComponent()));

        Assert.Equal(
            $"The property 'ValueChanged' on component type '{typeof(CascadingCallbackComponent).FullName}' cannot be set explicitly because it only accepts cascading values.",
            exception.Message);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeclaredBoundCallback_InvokesOriginalReceiver(bool asyncCallback)
    {
        var receiver = new EventReceiver();
        var parentValue = false;
        var callback = asyncCallback
            ? CompilerServices.RuntimeHelpers.CreateInferredEventCallback(receiver, (bool value) =>
            {
                parentValue = value;
                return Task.CompletedTask;
            }, parentValue)
            : CompilerServices.RuntimeHelpers.CreateInferredEventCallback(receiver, (bool value) => parentValue = value, parentValue);
        var target = new DeclaredCallbackComponent();
        using var builder = BuildParameters(typeof(DeclaredCallbackComponent), callback, "component");
        var parameters = new ParameterView(ParameterViewLifetime.Unbound, builder.GetFrames().Array, 0);

        parameters.SetParameterProperties(target);
        await target.ValueChanged.InvokeAsync(true);

        Assert.True(parentValue);
        Assert.Equal(1, receiver.Count);
        Assert.Same(receiver, callback.Receiver);
        Assert.Equal("ordinary-attribute", Assert.Single(target.Attributes).Value);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExplicitCallbackWithSameDelegateAndReceiver_IsStillCaptured(bool cascadingCollision)
    {
        var receiver = new EventReceiver();
        var parentValue = false;
        Action<bool> setter = value => parentValue = value;
        var boundCallback = CompilerServices.RuntimeHelpers.CreateInferredEventCallback(receiver, setter, parentValue);
        var explicitCallback = EventCallback.Factory.Create(receiver, setter);
        CaptureComponent target = cascadingCollision ? new CascadingCallbackComponent() : new CaptureComponent();
        using var builder = BuildParameters(target.GetType(), explicitCallback, "component");
        var parameters = new ParameterView(ParameterViewLifetime.Unbound, builder.GetFrames().Array, 0);

        parameters.SetParameterProperties(target);
        var capturedCallback = Assert.IsType<EventCallback<bool>>(target.Attributes["ValueChanged"]);
        await capturedCallback.InvokeAsync(true);

        Assert.True(parentValue);
        Assert.Equal(1, receiver.Count);
        Assert.Equal(boundCallback, explicitCallback);
        Assert.Equal(boundCallback.GetHashCode(), explicitCallback.GetHashCode());
        Assert.Equal("ordinary-attribute", target.Attributes["class"]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeclaredBindingCallback_CanBeForwardedToUnmatchedParameter(bool cascadingCollision)
    {
        var receiver = new EventReceiver();
        var parentValue = false;
        var callback = CompilerServices.RuntimeHelpers.CreateInferredEventCallback(receiver, (bool value) => parentValue = value, parentValue);
        var wrapper = new DeclaredCallbackComponent();
        using var wrapperBuilder = BuildParameters(typeof(DeclaredCallbackComponent), callback, "component");
        var wrapperParameters = new ParameterView(ParameterViewLifetime.Unbound, wrapperBuilder.GetFrames().Array, 0);
        wrapperParameters.SetParameterProperties(wrapper);
        CaptureComponent target = cascadingCollision ? new CascadingCallbackComponent() : new CaptureComponent();
        var callbackName = cascadingCollision ? "ValueChanged" : "OnInput";
        using var builder = BuildParameters(target.GetType(), wrapper.ValueChanged, "component", callbackName);
        var parameters = new ParameterView(ParameterViewLifetime.Unbound, builder.GetFrames().Array, 0);

        parameters.SetParameterProperties(target);
        await Assert.IsType<EventCallback<bool>>(target.Attributes[callbackName]).InvokeAsync(true);

        Assert.True(parentValue);
        Assert.Equal(1, receiver.Count);
    }

    [Fact]
    public void ExplicitCaptureDictionary_IsNotInterpretedAsBindingParameters()
    {
        var attributes = new Dictionary<string, object>
        {
            ["ValueChanged"] = CreateBinding(new EventReceiver(), 0),
        };
        var parameters = ParameterView.FromDictionary(new Dictionary<string, object>
        {
            [nameof(CaptureComponent.Attributes)] = attributes,
        });
        var target = new CaptureComponent();

        parameters.SetParameterProperties(target);

        Assert.Same(attributes, target.Attributes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BindingProvenanceChange_IsNotSkippedByParameterEquality(bool untyped)
    {
        var receiver = new EventReceiver();
        Action<bool> setter = _ => { };
        var boundCallback = CompilerServices.RuntimeHelpers.CreateInferredEventCallback(receiver, setter, false);
        var explicitCallback = EventCallback.Factory.Create(receiver, setter);
        using var oldBuilder = BuildParameters(typeof(CaptureComponent), untyped ? explicitCallback.AsUntyped() : explicitCallback, "component");
        using var newBuilder = BuildParameters(typeof(CaptureComponent), untyped ? boundCallback.AsUntyped() : boundCallback, "component");
        var oldParameters = new ParameterView(ParameterViewLifetime.Unbound, oldBuilder.GetFrames().Array, 0);
        var newParameters = new ParameterView(ParameterViewLifetime.Unbound, newBuilder.GetFrames().Array, 0);

        Assert.Equal(explicitCallback, boundCallback);
        Assert.False(oldParameters.DefinitelyEquals(newParameters));
    }

    [Fact]
    public async Task BindingWithoutReceiver_PreservesInvocationAndEquality()
    {
        var value = false;
        Action<bool> setter = next => value = next;
        var receiver = new object();
        var boundCallback = CompilerServices.RuntimeHelpers.CreateInferredEventCallback(receiver, setter, value);
        var explicitCallback = EventCallback.Factory.Create(receiver, setter);

        await boundCallback.InvokeAsync(true);

        Assert.True(value);
        Assert.Null(boundCallback.Receiver);
        Assert.Equal(explicitCallback, boundCallback);
        Assert.Equal(explicitCallback.GetHashCode(), boundCallback.GetHashCode());
    }

    [Fact]
    public async Task EmptyBindingCallback_PreservesEmptyInvocation()
    {
        var callback = CompilerServices.RuntimeHelpers.CreateInferredEventCallback(new EventReceiver(), default(EventCallback<bool>), false);

        Assert.False(callback.HasDelegate);
        Assert.Null(callback.Receiver);
        await callback.InvokeAsync(true);
    }

    [Fact]
    public void CallbackStorage_RemainsTwoReferences()
    {
        Assert.Equal(2 * IntPtr.Size, Unsafe.SizeOf<EventCallback>());
        Assert.Equal(2 * IntPtr.Size, Unsafe.SizeOf<EventCallback<bool>>());
    }

    [Fact]
    public void RepeatedBindingConstruction_DoesNotAllocateAfterWarmup()
    {
        var receiver = new EventReceiver();
        Action<bool> setter = _ => { };
        var callbacks = new EventCallback<bool>[1024];
        _ = CompilerServices.RuntimeHelpers.CreateInferredEventCallback(receiver, setter, false);
        var before = GC.GetAllocatedBytesForCurrentThread();

        for (var index = 0; index < callbacks.Length; index++)
        {
            callbacks[index] = CompilerServices.RuntimeHelpers.CreateInferredEventCallback(receiver, setter, false);
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(0, allocated);
        GC.KeepAlive(callbacks);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void RepeatedBindingAssignment_DoesNotAllocateAfterWarmup(bool untyped, bool captureUnmatchedValues)
    {
        const int iterations = 100_000;
        var receiver = new EventReceiver();
        Action<bool> setter = _ => { };
        var boundCallback = CompilerServices.RuntimeHelpers.CreateInferredEventCallback(receiver, setter, false);
        var explicitCallback = EventCallback.Factory.Create(receiver, setter);
        var target = CreateCallbackComponent(untyped, captureUnmatchedValues);
        var boundParameters = ParameterView.FromDictionary(new Dictionary<string, object>
        {
            [nameof(CallbackComponent<EventCallback>.ValueChanged)] = untyped ? boundCallback.AsUntyped() : boundCallback,
        });
        var explicitParameters = ParameterView.FromDictionary(new Dictionary<string, object>
        {
            [nameof(CallbackComponent<EventCallback>.ValueChanged)] = untyped ? explicitCallback.AsUntyped() : explicitCallback,
        });

        for (var index = 0; index < 1000; index++)
        {
            boundParameters.SetParameterProperties(target);
            explicitParameters.SetParameterProperties(target);
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < iterations; index++)
        {
            explicitParameters.SetParameterProperties(target);
        }
        var explicitAllocated = GC.GetAllocatedBytesForCurrentThread() - before;

        before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < iterations; index++)
        {
            boundParameters.SetParameterProperties(target);
        }
        var boundAllocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, explicitAllocated);
        Assert.Equal(0, boundAllocated);
        Assert.False(GetCallback(target).IsBinding);
        Assert.True(boundCallback.IsBinding);
        GC.KeepAlive(target);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task DeclaredBindingAssignment_PreservesCallbackAndOriginalProvenance(bool untyped, bool captureUnmatchedValues)
    {
        var receiver = new EventReceiver();
        var value = false;
        var boundCallback = CompilerServices.RuntimeHelpers.CreateInferredEventCallback(receiver, (bool next) => value = next, value);
        object originalCallback = untyped ? boundCallback.AsUntyped() : boundCallback;
        var parameters = ParameterView.FromDictionary(new Dictionary<string, object>
        {
            [nameof(CallbackComponent<EventCallback>.ValueChanged)] = originalCallback,
        });
        var target = CreateCallbackComponent(untyped, captureUnmatchedValues);

        parameters.SetParameterProperties(target);

        var assignedCallback = GetCallback(target);
        Assert.False(assignedCallback.IsBinding);
        Assert.True(((IEventCallback)originalCallback).IsBinding);
        using var forwardedBuilder = BuildParameters(typeof(CaptureComponent), assignedCallback, "component");
        var assignedParameters = new ParameterView(ParameterViewLifetime.Unbound, forwardedBuilder.GetFrames().Array, 0);
        var forwardedTarget = new CaptureComponent();
        assignedParameters.SetParameterProperties(forwardedTarget);
        Assert.Same(assignedCallback, forwardedTarget.Attributes[nameof(CallbackComponent<EventCallback>.ValueChanged)]);
        if (untyped)
        {
            var callback = Assert.IsType<EventCallback>(forwardedTarget.Attributes[nameof(CallbackComponent<EventCallback>.ValueChanged)]);
            Assert.Same(boundCallback.Receiver, callback.Receiver);
            Assert.Same(boundCallback.Delegate, callback.Delegate);
            await callback.InvokeAsync(true);
        }
        else
        {
            var callback = Assert.IsType<EventCallback<bool>>(forwardedTarget.Attributes[nameof(CallbackComponent<EventCallback>.ValueChanged)]);
            Assert.Same(boundCallback.Receiver, callback.Receiver);
            Assert.Same(boundCallback.Delegate, callback.Delegate);
            await callback.InvokeAsync(true);
        }

        Assert.True(value);
        Assert.Equal(1, receiver.Count);
        using var builder = BuildParameters(typeof(CaptureComponent), originalCallback, "component");
        var forwardedParameters = new ParameterView(ParameterViewLifetime.Unbound, builder.GetFrames().Array, 0);
        Assert.Throws<InvalidOperationException>(() => forwardedParameters.SetParameterProperties(new CaptureComponent()));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DeclaredCallbackAssignment_NullWritesDefault(bool untyped)
    {
        var target = CreateCallbackComponent(untyped, captureUnmatchedValues: false);
        var callbackParameters = ParameterView.FromDictionary(new Dictionary<string, object>
        {
            [nameof(CallbackComponent<EventCallback>.ValueChanged)] = CreateBinding(new EventReceiver(), untyped ? 6 : 0),
        });
        callbackParameters.SetParameterProperties(target);
        Assert.True(GetCallback(target).HasDelegate);
        var nullParameters = ParameterView.FromDictionary(new Dictionary<string, object>
        {
            [nameof(CallbackComponent<EventCallback>.ValueChanged)] = null!,
        });

        nullParameters.SetParameterProperties(target);

        Assert.Equal(untyped ? (object)default(EventCallback) : default(EventCallback<bool>), GetCallback(target));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DeclaredCallbackAssignment_WrapsInvalidValueError(bool untyped)
    {
        var target = CreateCallbackComponent(untyped, captureUnmatchedValues: false);
        var parameters = ParameterView.FromDictionary(new Dictionary<string, object>
        {
            [nameof(CallbackComponent<EventCallback>.ValueChanged)] = "not a callback",
        });

        var exception = Assert.Throws<InvalidOperationException>(() => parameters.SetParameterProperties(target));

        Assert.IsType<InvalidCastException>(exception.InnerException);
        Assert.StartsWith($"Unable to set property '{nameof(CallbackComponent<EventCallback>.ValueChanged)}' on object of type '{target.GetType().FullName}'.", exception.Message);
        Assert.False(GetCallback(target).HasDelegate);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BoxedCallbackAssignment_ClearsOnlyAssignedProvenance(bool untyped)
    {
        var originalCallback = CreateBinding(new EventReceiver(), untyped ? 6 : 0);
        var target = new CallbackComponent<object>();
        var parameters = ParameterView.FromDictionary(new Dictionary<string, object>
        {
            [nameof(target.ValueChanged)] = originalCallback,
        });

        parameters.SetParameterProperties(target);

        Assert.NotSame(originalCallback, target.ValueChanged);
        Assert.Equal(originalCallback, target.ValueChanged);
        Assert.True(((IEventCallback)originalCallback).IsBinding);
        Assert.False(Assert.IsAssignableFrom<IEventCallback>(target.ValueChanged).IsBinding);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DeclaredBindingAssignment_RecreatesWriterAfterCacheClear(bool untyped)
    {
        var parameters = ParameterView.FromDictionary(new Dictionary<string, object>
        {
            [nameof(CallbackComponent<EventCallback>.ValueChanged)] = CreateBinding(new EventReceiver(), untyped ? 6 : 0),
        });
        var target = CreateCallbackComponent(untyped, captureUnmatchedValues: false);
        parameters.SetParameterProperties(target);

        ComponentProperties.ClearCache();
        var newTarget = CreateCallbackComponent(untyped, captureUnmatchedValues: false);
        parameters.SetParameterProperties(newTarget);

        Assert.True(GetCallback(newTarget).HasDelegate);
        Assert.False(GetCallback(newTarget).IsBinding);
        Assert.Equal(GetCallback(target), GetCallback(newTarget));
    }

    [ConditionalFact]
    [RemoteExecutionSupported]
    public void DeclaredBindingAssignment_WorksWithoutDynamicCode()
    {
        var options = new RemoteInvokeOptions();
        options.RuntimeConfigurationOptions.Add("System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported", "false");

        using var remoteHandle = RemoteExecutor.Invoke(static () =>
        {
            Assert.False(RuntimeFeature.IsDynamicCodeSupported);
            var tests = new ComponentBindingTest();
            tests.DeclaredBindingAssignment_PreservesCallbackAndOriginalProvenance(untyped: false, captureUnmatchedValues: false).GetAwaiter().GetResult();
            tests.DeclaredBindingAssignment_PreservesCallbackAndOriginalProvenance(untyped: false, captureUnmatchedValues: true).GetAwaiter().GetResult();
            tests.DeclaredBindingAssignment_PreservesCallbackAndOriginalProvenance(untyped: true, captureUnmatchedValues: false).GetAwaiter().GetResult();
            tests.DeclaredBindingAssignment_PreservesCallbackAndOriginalProvenance(untyped: true, captureUnmatchedValues: true).GetAwaiter().GetResult();
            var receiver = new EventReceiver();
            var value = new CallbackValue(1);
            var callback = CompilerServices.RuntimeHelpers.CreateInferredEventCallback(receiver, (CallbackValue next) => value = next, value);
            var target = new CallbackComponent<EventCallback<CallbackValue>>();
            var parameters = ParameterView.FromDictionary(new Dictionary<string, object>
            {
                [nameof(target.ValueChanged)] = callback,
            });

            parameters.SetParameterProperties(target);
            target.ValueChanged.InvokeAsync(new CallbackValue(2)).GetAwaiter().GetResult();

            Assert.Equal(new CallbackValue(2), value);
            Assert.Equal(1, receiver.Count);
            Assert.False(target.ValueChanged.IsBinding);
            Assert.True(callback.IsBinding);
            Assert.Same(callback.Receiver, target.ValueChanged.Receiver);
            Assert.Same(callback.Delegate, target.ValueChanged.Delegate);
        }, options);
    }

    private static object CreateCallbackComponent(bool untyped, bool captureUnmatchedValues)
        => (untyped, captureUnmatchedValues) switch
        {
            (false, false) => new CallbackComponent<EventCallback<bool>>(),
            (false, true) => new CallbackWithCaptureComponent<EventCallback<bool>>(),
            (true, false) => new CallbackComponent<EventCallback>(),
            (true, true) => new CallbackWithCaptureComponent<EventCallback>(),
        };

    private static IEventCallback GetCallback(object target)
        => target switch
        {
            CallbackComponent<EventCallback<bool>> component => component.ValueChanged,
            CallbackComponent<EventCallback> component => component.ValueChanged,
            _ => throw new ArgumentException("Expected a callback component.", nameof(target)),
        };

    private static object CreateBinding(object receiver, int callbackKind)
    {
        Action<bool> setter = _ => { };
        Func<bool, Task> asyncSetter = _ => Task.CompletedTask;
        return callbackKind switch
        {
            0 => CompilerServices.RuntimeHelpers.CreateInferredEventCallback(receiver, setter, false),
            1 => CompilerServices.RuntimeHelpers.CreateInferredEventCallback(receiver, asyncSetter, false),
            2 => CompilerServices.RuntimeHelpers.CreateInferredEventCallback(receiver, EventCallback.Factory.Create(receiver, setter), false),
            3 => CompilerServices.RuntimeHelpers.CreateInferredEventCallback(receiver, default(EventCallback<bool>), false),
            4 => EventCallback.Factory.CreateInferred(receiver, setter, false),
            5 => EventCallback.Factory.CreateInferred(receiver, asyncSetter, false),
            6 => CompilerServices.RuntimeHelpers.CreateInferredEventCallback(receiver, setter, false).AsUntyped(),
            7 => EventCallback.Factory.Create<bool>(receiver, CompilerServices.RuntimeHelpers.CreateInferredEventCallback(receiver, setter, false).AsUntyped()),
            _ => throw new ArgumentOutOfRangeException(nameof(callbackKind)),
        };
    }

    private static RenderTreeBuilder BuildParameters(Type componentType, object callback, string route, string callbackName = "ValueChanged", bool includeOrdinaryAttribute = true)
    {
        var builder = new RenderTreeBuilder();
        builder.OpenComponent(0, componentType);
        builder.AddComponentParameter(1, nameof(CaptureComponent.Value), false);
        if (includeOrdinaryAttribute)
        {
            builder.AddComponentParameter(2, "class", "ordinary-attribute");
        }
        if (route == "component")
        {
            builder.AddComponentParameter(3, callbackName, callback);
        }
        else if (route == "typed" && callback is EventCallback<bool> typedCallback)
        {
            builder.AddAttribute(3, callbackName, typedCallback);
        }
        else if (route == "typed" && callback is EventCallback untypedCallback)
        {
            builder.AddAttribute(3, callbackName, untypedCallback);
        }
        else
        {
            builder.AddAttribute(3, callbackName, callback);
        }
        builder.CloseComponent();

        return builder;
    }

    private class CaptureComponent : ComponentBase
    {
        [Parameter]
        public bool Value { get; set; }

        [Parameter(CaptureUnmatchedValues = true)]
        public Dictionary<string, object> Attributes { get; set; } = new();
    }

    private sealed class DeclaredCallbackComponent : CaptureComponent
    {
        [Parameter]
        public EventCallback<bool> ValueChanged { get; set; }
    }

    private sealed class CascadingCallbackComponent : CaptureComponent
    {
        [CascadingParameter]
        public EventCallback<bool> ValueChanged { get; set; }
    }

    private sealed class NoCaptureComponent : ComponentBase
    {
        [Parameter]
        public bool Value { get; set; }
    }

    private class CallbackComponent<TCallback> : ComponentBase
    {
        [Parameter]
        public TCallback ValueChanged { get; set; } = default!;
    }

    private sealed class CallbackWithCaptureComponent<TCallback> : CallbackComponent<TCallback>
    {
        [Parameter(CaptureUnmatchedValues = true)]
        public Dictionary<string, object> Attributes { get; set; } = new();
    }

    private readonly record struct CallbackValue(int Value);

    private sealed class EventReceiver : IHandleEvent
    {
        public int Count { get; private set; }

        public Task HandleEventAsync(EventCallbackWorkItem item, object arg)
        {
            Count++;
            return item.InvokeAsync(arg);
        }
    }
}
