// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.InternalTesting;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class TransportConnectionFeatureCollectionTests
{
    [Fact]
    public void IConnectionEndPointFeature_IsAvailableInFeatureCollection()
    {
        var serviceContext = new TestServiceContext();
        var connection = new Mock<DefaultConnectionContext> { CallBase = true }.Object;
        var transportConnectionManager = new TransportConnectionManager(serviceContext.ConnectionManager);
        var kestrelConnection = CreateKestrelConnection(serviceContext, connection, transportConnectionManager);
        
        var endpointFeature = kestrelConnection.TransportConnection.Features.Get<IConnectionEndPointFeature>();
        
        Assert.NotNull(endpointFeature);
    }

    [Fact]
    public void IConnectionEndPointFeature_ReturnsCorrectLocalEndPoint()
    {
        var serviceContext = new TestServiceContext();
        var connection = new Mock<DefaultConnectionContext> { CallBase = true }.Object;
        var expectedLocalEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8080);
        connection.LocalEndPoint = expectedLocalEndPoint;
        var transportConnectionManager = new TransportConnectionManager(serviceContext.ConnectionManager);
        var kestrelConnection = CreateKestrelConnection(serviceContext, connection, transportConnectionManager);
        
        var endpointFeature = kestrelConnection.TransportConnection.Features.Get<IConnectionEndPointFeature>();
        
        Assert.NotNull(endpointFeature);
        Assert.Equal(expectedLocalEndPoint, endpointFeature.LocalEndPoint);
    }

    [Fact]
    public void IConnectionEndPointFeature_ReturnsCorrectRemoteEndPoint()
    {
        var serviceContext = new TestServiceContext();
        var connection = new Mock<DefaultConnectionContext> { CallBase = true }.Object;
        var expectedRemoteEndPoint = new IPEndPoint(IPAddress.Parse("192.168.1.100"), 54321);
        connection.RemoteEndPoint = expectedRemoteEndPoint;
        var transportConnectionManager = new TransportConnectionManager(serviceContext.ConnectionManager);
        var kestrelConnection = CreateKestrelConnection(serviceContext, connection, transportConnectionManager);
        
        var endpointFeature = kestrelConnection.TransportConnection.Features.Get<IConnectionEndPointFeature>();
        
        Assert.NotNull(endpointFeature);
        Assert.Equal(expectedRemoteEndPoint, endpointFeature.RemoteEndPoint);
    }

    [Fact]
    public void IConnectionEndPointFeature_AllowsSettingLocalEndPoint()
    {
        var serviceContext = new TestServiceContext();
        var connection = new Mock<DefaultConnectionContext> { CallBase = true }.Object;
        var transportConnectionManager = new TransportConnectionManager(serviceContext.ConnectionManager);
        var kestrelConnection = CreateKestrelConnection(serviceContext, connection, transportConnectionManager);
        var newLocalEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9090);
        
        var endpointFeature = kestrelConnection.TransportConnection.Features.Get<IConnectionEndPointFeature>();
        endpointFeature.LocalEndPoint = newLocalEndPoint;
        
        Assert.Equal(newLocalEndPoint, kestrelConnection.TransportConnection.LocalEndPoint);
        Assert.Equal(newLocalEndPoint, endpointFeature.LocalEndPoint);
    }

    [Fact]
    public void IConnectionEndPointFeature_AllowsSettingRemoteEndPoint()
    {
        var serviceContext = new TestServiceContext();
        var connection = new Mock<DefaultConnectionContext> { CallBase = true }.Object;
        var transportConnectionManager = new TransportConnectionManager(serviceContext.ConnectionManager);
        var kestrelConnection = CreateKestrelConnection(serviceContext, connection, transportConnectionManager);
        var newRemoteEndPoint = new IPEndPoint(IPAddress.Parse("10.0.0.1"), 12345);
        
        var endpointFeature = kestrelConnection.TransportConnection.Features.Get<IConnectionEndPointFeature>();
        endpointFeature.RemoteEndPoint = newRemoteEndPoint;
        
        Assert.Equal(newRemoteEndPoint, kestrelConnection.TransportConnection.RemoteEndPoint);
        Assert.Equal(newRemoteEndPoint, endpointFeature.RemoteEndPoint);
    }

    private static KestrelConnection<ConnectionContext> CreateKestrelConnection(TestServiceContext serviceContext, DefaultConnectionContext connection, TransportConnectionManager transportConnectionManager, Func<ConnectionContext, Task> connectionDelegate = null)
    {
        connectionDelegate ??= _ => Task.CompletedTask;

        return new KestrelConnection<ConnectionContext>(
            id: 0, serviceContext, transportConnectionManager, connectionDelegate, connection, serviceContext.Log, TestContextFactory.CreateMetricsContext(connection));
    }
}