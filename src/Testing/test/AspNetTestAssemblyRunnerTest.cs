// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.InternalTesting;

public class AspNetTestAssemblyRunnerTest
{
    private const int NotCalled = -1;

    [Fact]
    public async Task ForAssemblyHasHigherPriorityThanConstructors()
    {
        var runner = TestableAspNetTestAssemblyRunner.Create(typeof(TestAssemblyFixtureWithAll));

        await runner.AfterTestAssemblyStartingAsync_Public();

        Assert.NotNull(runner.Fixtures);
        var fixtureObject = Assert.Single(runner.Fixtures);
        var fixture = Assert.IsType<TestAssemblyFixtureWithAll>(fixtureObject);
        Assert.False(fixture.ConstructorWithMessageSinkCalled);
        Assert.True(fixture.ForAssemblyCalled);
        Assert.False(fixture.ParameterlessConstructorCalled);
    }

    [Fact]
    public async Task ConstructorWithMessageSinkHasHigherPriorityThanParameterlessConstructor()
    {
        var runner = TestableAspNetTestAssemblyRunner.Create(typeof(TestAssemblyFixtureWithMessageSink));

        await runner.AfterTestAssemblyStartingAsync_Public();

        Assert.NotNull(runner.Fixtures);
        var fixtureObject = Assert.Single(runner.Fixtures);
        var fixture = Assert.IsType<TestAssemblyFixtureWithMessageSink>(fixtureObject);
        Assert.True(fixture.ConstructorWithMessageSinkCalled);
        Assert.False(fixture.ParameterlessConstructorCalled);
    }

    [Fact]
    public async Task CalledInExpectedOrder_SuccessWithDispose()
    {
        var runner = TestableAspNetTestAssemblyRunner.Create(typeof(TextAssemblyFixtureWithDispose));

        var runSummary = await runner.RunAsync();

        Assert.NotNull(runSummary);
        Assert.Equal(0, runSummary.Failed);
        Assert.Equal(0, runSummary.Skipped);
        Assert.Equal(1, runSummary.Total);

        Assert.NotNull(runner.Fixtures);
        var fixtureObject = Assert.Single(runner.Fixtures);
        var fixture = Assert.IsType<TextAssemblyFixtureWithDispose>(fixtureObject);
        Assert.Equal(NotCalled, fixture.ReportTestFailureCalledAt);
        Assert.Equal(0, fixture.DisposeCalledAt);
    }

    [Fact]
    public async Task CalledInExpectedOrder_FailedWithDispose()
    {
        var runner = TestableAspNetTestAssemblyRunner.Create(
            typeof(TextAssemblyFixtureWithDispose),
            failTestCase: true);

        var runSummary = await runner.RunAsync();

        Assert.NotNull(runSummary);
        Assert.Equal(1, runSummary.Failed);
        Assert.Equal(0, runSummary.Skipped);
        Assert.Equal(1, runSummary.Total);

        Assert.NotNull(runner.Fixtures);
        var fixtureObject = Assert.Single(runner.Fixtures);
        var fixture = Assert.IsType<TextAssemblyFixtureWithDispose>(fixtureObject);
        Assert.Equal(0, fixture.ReportTestFailureCalledAt);
        Assert.Equal(1, fixture.DisposeCalledAt);
    }

    [Fact]
    public async Task CalledInExpectedOrder_SuccessWithAsyncDispose()
    {
        var runner = TestableAspNetTestAssemblyRunner.Create(typeof(TestAssemblyFixtureWithAsyncDispose));

        var runSummary = await runner.RunAsync();

        Assert.NotNull(runSummary);
        Assert.Equal(0, runSummary.Failed);
        Assert.Equal(0, runSummary.Skipped);
        Assert.Equal(1, runSummary.Total);

        Assert.NotNull(runner.Fixtures);
        var fixtureObject = Assert.Single(runner.Fixtures);
        var fixture = Assert.IsType<TestAssemblyFixtureWithAsyncDispose>(fixtureObject);
        Assert.Equal(0, fixture.InitializeAsyncCalledAt);
        Assert.Equal(NotCalled, fixture.ReportTestFailureCalledAt);
        Assert.Equal(1, fixture.AsyncDisposeCalledAt);
    }

    [Fact]
    public async Task CalledInExpectedOrder_FailedWithAsyncDispose()
    {
        var runner = TestableAspNetTestAssemblyRunner.Create(
            typeof(TestAssemblyFixtureWithAsyncDispose),
            failTestCase: true);

        var runSummary = await runner.RunAsync();

        Assert.NotNull(runSummary);
        Assert.Equal(1, runSummary.Failed);
        Assert.Equal(0, runSummary.Skipped);
        Assert.Equal(1, runSummary.Total);

        Assert.NotNull(runner.Fixtures);
        var fixtureObject = Assert.Single(runner.Fixtures);
        var fixture = Assert.IsType<TestAssemblyFixtureWithAsyncDispose>(fixtureObject);
        Assert.Equal(0, fixture.InitializeAsyncCalledAt);
        Assert.Equal(1, fixture.ReportTestFailureCalledAt);
        Assert.Equal(2, fixture.AsyncDisposeCalledAt);
    }

    private class TestAssemblyFixtureWithAll
    {
        private TestAssemblyFixtureWithAll(bool forAssemblyCalled)
        {
            ForAssemblyCalled = forAssemblyCalled;
        }

        public TestAssemblyFixtureWithAll()
        {
            ParameterlessConstructorCalled = true;
        }

        public TestAssemblyFixtureWithAll(IMessageSink messageSink)
        {
            ConstructorWithMessageSinkCalled = true;
        }

        public static TestAssemblyFixtureWithAll ForAssembly(Assembly assembly)
        {
            return new TestAssemblyFixtureWithAll(forAssemblyCalled: true);
        }

        public bool ParameterlessConstructorCalled { get; }

        public bool ConstructorWithMessageSinkCalled { get; }

        public bool ForAssemblyCalled { get; }
    }

    private class TestAssemblyFixtureWithMessageSink
    {
        public TestAssemblyFixtureWithMessageSink()
        {
            ParameterlessConstructorCalled = true;
        }

        public TestAssemblyFixtureWithMessageSink(IMessageSink messageSink)
        {
            ConstructorWithMessageSinkCalled = true;
        }

        public bool ParameterlessConstructorCalled { get; }

        public bool ConstructorWithMessageSinkCalled { get; }
    }

    private class TextAssemblyFixtureWithDispose : IAcceptFailureReports, IDisposable
    {
        private int _position;

        public int ReportTestFailureCalledAt { get; private set; } = NotCalled;

        public int DisposeCalledAt { get; private set; } = NotCalled;

        void IAcceptFailureReports.ReportTestFailure()
        {
            ReportTestFailureCalledAt = _position++;
        }

        void IDisposable.Dispose()
        {
            DisposeCalledAt = _position++;
        }
    }

    private class TestAssemblyFixtureWithAsyncDispose : IAcceptFailureReports, IAsyncLifetime
    {
        private int _position;

        public int InitializeAsyncCalledAt { get; private set; } = NotCalled;

        public int ReportTestFailureCalledAt { get; private set; } = NotCalled;

        public int AsyncDisposeCalledAt { get; private set; } = NotCalled;

        Task IAsyncLifetime.InitializeAsync()
        {
            InitializeAsyncCalledAt = _position++;
            return Task.CompletedTask;
        }

        void IAcceptFailureReports.ReportTestFailure()
        {
            ReportTestFailureCalledAt = _position++;
        }

        Task IAsyncLifetime.DisposeAsync()
        {
            AsyncDisposeCalledAt = _position++;
            return Task.CompletedTask;
        }
    }
}
