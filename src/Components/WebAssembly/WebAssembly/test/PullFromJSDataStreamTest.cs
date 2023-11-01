// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.JSInterop;
using Moq;

namespace Microsoft.AspNetCore.Components;

public class PullFromJSDataStreamTest
{
    private static readonly TestJSRuntime _jsRuntime = new();
    private static readonly byte[] Data = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };

    [Fact]
    public void CreateJSDataStream_CreatesStream()
    {
        // Arrange
        var jsStreamReference = Mock.Of<IJSStreamReference>();

        // Act
        var pullFromJSDataStream = PullFromJSDataStream.CreateJSDataStream(_jsRuntime, jsStreamReference, totalLength: 100, cancellationToken: CancellationToken.None);

        // Assert
        Assert.NotNull(pullFromJSDataStream);
    }

    [Fact]
    public async Task ReceiveData_SuccessReadsBackStream_UsingByteArrayBuffer()
    {
        // Arrange
        var expectedChunks = new byte[][]
        {
                new byte[] { 1, 2, 3 },
                new byte[] { 4, 5, 6 },
                new byte[] { 7, 8, 9 },
        };
        var pullFromJSDataStream = CreateJSDataStream(Data);

        // Act & Assert
        for (byte i = 0; i < 3; i++)
        {
            var buffer = new byte[3];
            Assert.Equal(3, await pullFromJSDataStream.ReadAsync(buffer));
            Assert.Equal(expectedChunks[i], buffer);
        }
        Assert.Equal(pullFromJSDataStream.Position, Data.Length);
    }

    [Fact]
    public async Task ReceiveData_SuccessReadsBackStream_UsingMemoryBuffer()
    {
        // Arrange
        var pullFromJSDataStream = CreateJSDataStream(Data);

        // Act & Assert
        using var mem = new MemoryStream();
        await pullFromJSDataStream.CopyToAsync(mem).DefaultTimeout();
        Assert.Equal(Data, mem.ToArray());
    }

    [Fact]
    public async Task ReceiveData_JSProvidesInsufficientData_Throws()
    {
        // Arrange
        var insufficientDataJSRuntime = new TestJSRuntime_ProvidesInsufficientData(Data);
        var pullFromJSDataStream = CreateJSDataStream(Data, insufficientDataJSRuntime);

        // Act & Assert
        using var mem = new MemoryStream();
        var ex = await Assert.ThrowsAsync<EndOfStreamException>(async () => await pullFromJSDataStream.CopyToAsync(mem).DefaultTimeout());
        Assert.Equal("Failed to read the requested number of bytes from the stream.", ex.Message);
    }

    [Fact]
    public async Task ReceiveData_JSProvidesExcessData_Throws()
    {
        // Arrange
        var data = new byte[50000];
        var excessDataJSRuntime = new TestJSRuntime_ProvidesExcessData(data);
        var pullFromJSDataStream = CreateJSDataStream(Data, excessDataJSRuntime);

        // Act & Assert
        using var mem = new MemoryStream();
        var ex = await Assert.ThrowsAsync<EndOfStreamException>(async () => await pullFromJSDataStream.CopyToAsync(mem).DefaultTimeout());
        Assert.Equal("Failed to read the requested number of bytes from the stream.", ex.Message);
    }

    [Fact]
    public async Task ReceiveData_JSProvidesExcessData_Throws2()
    {
        // Arrange
        var data = new byte[50000];
        var excessDataJSRuntime = new TestJSRuntime_ProvidesExcessData(data);
        var pullFromJSDataStream = CreateJSDataStream(Data, excessDataJSRuntime);

        // Act & Assert
        using var mem = new MemoryStream();
        var ex = await Assert.ThrowsAsync<EndOfStreamException>(async () => await pullFromJSDataStream.CopyToAsync(mem).DefaultTimeout());
        Assert.Equal("Failed to read the requested number of bytes from the stream.", ex.Message);
    }

    private static PullFromJSDataStream CreateJSDataStream(byte[] data, IJSRuntime runtime = null)
    {
        runtime ??= new TestJSRuntime(data);
        var jsStreamReference = Mock.Of<IJSStreamReference>();
        var pullFromJSDataStream = PullFromJSDataStream.CreateJSDataStream(runtime, jsStreamReference, totalLength: data.Length, cancellationToken: CancellationToken.None);
        return pullFromJSDataStream;
    }

    class TestJSRuntime : IJSRuntime
    {
        protected readonly byte[] _data;

        public TestJSRuntime(byte[] data = default)
        {
            _data = data;
        }

        public virtual ValueTask<TValue> InvokeAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] TValue>(string identifier, CancellationToken cancellationToken, object[] args)
        {
            Assert.Equal("Blazor._internal.getJSDataStreamChunk", identifier);
            if (typeof(TValue) != typeof(byte[]))
            {
                throw new ArgumentException($"Unexpected call to InvokeAsync, expected byte[] got {typeof(TValue)}");
            }
            var offset = (long)args[1];
            var bytesToRead = (int)args[2];
            return ValueTask.FromResult((TValue)(object)_data.Skip((int)offset).Take(bytesToRead).ToArray());
        }

        public async ValueTask<TValue> InvokeAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] TValue>(string identifier, object[] args)
        {
            return await InvokeAsync<TValue>(identifier, CancellationToken.None, args);
        }
    }

    class TestJSRuntime_ProvidesInsufficientData : TestJSRuntime
    {
        public TestJSRuntime_ProvidesInsufficientData(byte[] data) : base(data)
        {
        }

        public override ValueTask<TValue> InvokeAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] TValue>(string identifier, CancellationToken cancellationToken, object[] args)
        {
            var offset = (long)args[1];
            var bytesToRead = (int)args[2];
            return ValueTask.FromResult((TValue)(object)_data.Skip((int)offset).Take(bytesToRead - 1).ToArray());
        }
    }

    class TestJSRuntime_ProvidesExcessData : TestJSRuntime
    {
        public TestJSRuntime_ProvidesExcessData(byte[] data) : base(data)
        {
        }

        public override ValueTask<TValue> InvokeAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] TValue>(string identifier, CancellationToken cancellationToken, object[] args)
        {
            var offset = (long)args[1];
            var bytesToRead = (int)args[2];
            return ValueTask.FromResult((TValue)(object)_data.Skip((int)offset).Take(bytesToRead + 1).ToArray());
        }
    }
}
