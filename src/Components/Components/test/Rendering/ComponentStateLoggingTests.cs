// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Microsoft.AspNetCore.Components.Tests.Rendering;

public class ComponentStateLoggingTests
{
    [Fact]
    public void RenderIntoBatch_LogsWhenComponentDisposed()
    {
        var mockLogger = new Mock<ILogger>();
        var mockRenderer = new MockRenderer(mockLogger.Object);
        var component = new TestComponent();
        var componentState = new ComponentState(mockRenderer, 1, component, null);

        _ = componentState.DisposeAsync();

        mockLogger.Reset();
        mockLogger
            .Setup(l => l.IsEnabled(LogLevel.Debug))
            .Returns(true);

        var batchBuilder = new RenderBatchBuilder();
        RenderFragment renderFragment = builder => { };

        componentState.RenderIntoBatch(batchBuilder, renderFragment, out var exception);

        mockLogger.Verify(
            l => l.Log(
                LogLevel.Debug,
                It.Is<EventId>(e => e.Id == 9),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once,
            "Expected SkippingRenderOnDisposedComponent (EventId 9) to be logged when rendering a disposed component");
    }

    [Fact]
    public void NotifyCascadingValueChanged_LogsWhenComponentDisposed()
    {
        var mockLogger = new Mock<ILogger>();
        var mockRenderer = new MockRenderer(mockLogger.Object);
        var component = new TestComponent();
        var componentState = new ComponentState(mockRenderer, 1, component, null);

        _ = componentState.DisposeAsync();

        mockLogger.Reset();
        mockLogger
            .Setup(l => l.IsEnabled(LogLevel.Debug))
            .Returns(true);

        var lifetime = new ParameterViewLifetime();
        componentState.NotifyCascadingValueChanged(lifetime);

        mockLogger.Verify(
            l => l.Log(
                LogLevel.Debug,
                It.Is<EventId>(e => e.Id == 7),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once,
            "Expected SkippingCascadingUpdateOnDisposedComponent (EventId 7) to be logged when updating cascading values on disposed component");
    }

    [Fact]
    public void SetDirectParameters_LogsSupplyingCombinedParametersAtTrace()
    {
        var mockLogger = new Mock<ILogger>();
        var mockRenderer = new MockRenderer(mockLogger.Object);
        var component = new TestComponent();
        var componentState = new ComponentState(mockRenderer, 1, component, null);

        mockLogger.Reset();
        mockLogger
            .Setup(l => l.IsEnabled(LogLevel.Trace))
            .Returns(true);

        componentState.SetDirectParameters(ParameterView.Empty);

        mockLogger.Verify(
            l => l.Log(
                LogLevel.Trace,
                It.Is<EventId>(e => e.Id == 10),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.AtLeastOnce,
            "Expected SupplyingCombinedParameters (EventId 10) to be logged at Trace level when parameters are supplied");
    }

    [Fact]
    public void SetDirectParameters_WithSingleDeliveryParam_LogsStoppedSingleDeliveryAndSupplying()
    {
        var mockLogger = new Mock<ILogger>();
        var mockRenderer = new MockRenderer(mockLogger.Object);

        var supplier = new SingleDeliverySupplierComponent(isFixed: true);
        var supplierState = new ComponentState(mockRenderer, 1, supplier, null);

        var consumer = new SingleDeliveryConsumerComponent();
        var consumerState = new ComponentState(mockRenderer, 2, consumer, supplierState);

        mockLogger.Reset();
        mockLogger
            .Setup(l => l.IsEnabled(It.IsAny<LogLevel>()))
            .Returns(true);

        consumerState.SetDirectParameters(ParameterView.Empty);

        mockLogger.Verify(
            l => l.Log(
                LogLevel.Debug,
                It.Is<EventId>(e => e.Id == 8),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once,
            "Expected StoppedSingleDeliveryCascadingParameters (EventId 8) to be logged when a single-delivery cascading parameter is consumed");

        mockLogger.Verify(
            l => l.Log(
                LogLevel.Trace,
                It.Is<EventId>(e => e.Id == 10),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.AtLeastOnce,
            "Expected SupplyingCombinedParameters (EventId 10) to be logged after single-delivery teardown");
    }

    [Fact]
    public async Task FullLifecycle_LogsAllFourStateTransitionsInOrder()
    {
        var mockLogger = new Mock<ILogger>();
        var mockRenderer = new MockRenderer(mockLogger.Object);

        var supplier = new SingleDeliverySupplierComponent(isFixed: true);
        var supplierState = new ComponentState(mockRenderer, 1, supplier, null);

        var consumer = new SingleDeliveryConsumerComponent();
        var consumerState = new ComponentState(mockRenderer, 2, consumer, supplierState);

        var batchBuilder = new RenderBatchBuilder();
        var parameterPhaseEventIds = new List<int>();
        var postDisposalEventIds = new List<int>();

        mockLogger
            .Setup(l => l.IsEnabled(It.IsAny<LogLevel>()))
            .Returns(true);
        mockLogger
            .Setup(l => l.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()))
            .Callback((LogLevel level, EventId eventId, object state, Exception exception, Delegate formatter) =>
            {
                parameterPhaseEventIds.Add(eventId.Id);
            });

        consumerState.SetDirectParameters(ParameterView.Empty);

        // Re-route the callback for the post-disposal phase so we can verify ordering
        // independently across the two phases.
        mockLogger
            .Setup(l => l.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()))
            .Callback((LogLevel level, EventId eventId, object state, Exception exception, Delegate formatter) =>
            {
                postDisposalEventIds.Add(eventId.Id);
            });

        await consumerState.DisposeAsync();
        consumerState.NotifyCascadingValueChanged(ParameterViewLifetime.Unbound);
        consumerState.RenderIntoBatch(batchBuilder, builder => { }, out _);

        // Phase 1 ordering: 8 (stopped single-delivery) before 10 (supplying).
        Assert.Contains(8, parameterPhaseEventIds);
        Assert.Contains(10, parameterPhaseEventIds);
        Assert.True(
            parameterPhaseEventIds.IndexOf(8) < parameterPhaseEventIds.IndexOf(10),
            $"Expected EventId 8 (StoppedSingleDeliveryCascadingParameters) to be logged before EventId 10 (SupplyingCombinedParameters). Actual order: [{string.Join(", ", parameterPhaseEventIds)}]");

        // Phase 2 ordering: 7 (skipping cascading update) before 9 (skipping render).
        Assert.Contains(7, postDisposalEventIds);
        Assert.Contains(9, postDisposalEventIds);
        Assert.True(
            postDisposalEventIds.IndexOf(7) < postDisposalEventIds.IndexOf(9),
            $"Expected EventId 7 (SkippingCascadingUpdateOnDisposedComponent) to be logged before EventId 9 (SkippingRenderOnDisposedComponent). Actual order: [{string.Join(", ", postDisposalEventIds)}]");

        // Disposal-only events must not appear during the parameter phase.
        Assert.DoesNotContain(7, parameterPhaseEventIds);
        Assert.DoesNotContain(9, parameterPhaseEventIds);

        // All four event IDs must appear across the lifecycle.
        var allEventIds = parameterPhaseEventIds.Concat(postDisposalEventIds).ToList();
        Assert.Equal(new[] { 7, 8, 9, 10 }, allEventIds.Distinct().OrderBy(id => id).ToArray());
    }

    private class TestComponent : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddMarkupContent(0, "<div>Test</div>");
        }
    }

    private class MockRenderer : Renderer
    {
        public MockRenderer(ILogger logger) : base(
            new ServiceCollection()
                .AddSingleton(logger)
                .BuildServiceProvider(),
            new MockLoggerFactory(logger))
        {
        }

        public override Dispatcher Dispatcher => new TestDispatcher();

        protected override void HandleException(Exception exception)
        {
        }

        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch)
        {
            return Task.CompletedTask;
        }
    }

    private class MockLoggerFactory : ILoggerFactory
    {
        private readonly ILogger _logger;

        public MockLoggerFactory(ILogger logger)
        {
            _logger = logger;
        }

        public ILogger CreateLogger(string categoryName) => _logger;

        public void AddProvider(ILoggerProvider provider) { }

        public void Dispose() { }
    }

    private class TestDispatcher : Dispatcher
    {
        public override bool CheckAccess() => true;

        public override Task InvokeAsync(Action workItem)
        {
            workItem();
            return Task.CompletedTask;
        }

        public override Task InvokeAsync(Func<Task> workItem)
        {
            return workItem();
        }

        public override Task<TResult> InvokeAsync<TResult>(Func<TResult> workItem)
        {
            return Task.FromResult(workItem());
        }

        public override Task<TResult> InvokeAsync<TResult>(Func<Task<TResult>> workItem)
        {
            return workItem();
        }
    }

    private sealed class TestSingleDeliveryAttribute : CascadingParameterAttributeBase
    {
        internal override bool SingleDelivery => true;
    }

    private class SingleDeliverySupplierComponent(bool isFixed) : ComponentBase, ICascadingValueSupplier
    {
        public bool IsFixed => isFixed;

        public bool CanSupplyValue(in CascadingParameterInfo parameterInfo)
            => parameterInfo.Attribute is TestSingleDeliveryAttribute;

        public object GetCurrentValue(object key, in CascadingParameterInfo parameterInfo)
            => null;

        public void Subscribe(ComponentState subscriber, in CascadingParameterInfo parameterInfo)
        {
        }

        public void Unsubscribe(ComponentState subscriber, in CascadingParameterInfo parameterInfo)
        {
        }
    }

    private class SingleDeliveryConsumerComponent : IComponent
    {
        public RenderHandle RenderHandle { get; private set; }

        [TestSingleDelivery]
        public string CascadingValue { get; set; }

        public void Attach(RenderHandle renderHandle) => RenderHandle = renderHandle;
        public Task SetParametersAsync(ParameterView parameters) => Task.CompletedTask;
    }
}
