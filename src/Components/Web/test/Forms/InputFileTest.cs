// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Forms;

public class InputFileTest
{
    [Fact]
    public void DerivedClass_CanOverrideDisposeMethod()
    {
        // Arrange
        var disposed = false;
        var derivedInputFile = new DerivedInputFile(() => disposed = true);

        // Act
        ((IDisposable)derivedInputFile).Dispose();

        // Assert
        Assert.True(disposed, "Derived class Dispose(bool) method should be called");
    }

    private class DerivedInputFile : InputFile
    {
        private readonly Action _onDispose;

        public DerivedInputFile(Action onDispose)
        {
            _onDispose = onDispose;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _onDispose();
            }
            base.Dispose(disposing);
        }
    }

    [Fact]
    public async Task OnAfterRenderAsync_InvokesInitJsFunction()
    {
        // Arrange
        var inputFile = new TestInputFile();
        var jsRuntime = new TestJSRuntime();
        inputFile.JSRuntime = jsRuntime;

        // Act
        await inputFile.CallOnAfterRenderAsync(firstRender: true);

        // Assert
        Assert.Contains(jsRuntime.Invocations, i => i.identifier == InputFileInterop.Init);
    }

    [Fact]
    public async Task ConvertToImageFileAsync_SetsOwnerAndReturnsFile()
    {
        // Arrange
        var inputFile = new InputFile();
        var browserFile = new BrowserFile { Id = 123, Name = "f", Size = 10 };
        var jsRuntime = new TestJSRuntime((identifier, args) =>
        {
            if (identifier == InputFileInterop.ToImageFile)
            {
                return browserFile;
            }

            return null;
        });

        inputFile.JSRuntime = jsRuntime;

        // Act
        var result = await inputFile.ConvertToImageFileAsync(browserFile, format: "jpeg", maxWidth: 100, maxHeight: 100);

        // Assert
        Assert.Same(browserFile, result);
        Assert.Same(inputFile, ((BrowserFile)result).Owner);
    }

    [Fact]
    public async Task ConvertToImageFileAsync_ThrowsIfJsReturnsNull()
    {
        // Arrange
        var inputFile = new InputFile();
        var browserFile = new BrowserFile { Id = 1 };
        var jsRuntime = new TestJSRuntime((identifier, args) => null);
        inputFile.JSRuntime = jsRuntime;

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => inputFile.ConvertToImageFileAsync(browserFile, "jpeg", 1, 1).AsTask());
    }

    [Fact]
    public async Task NotifyChange_SetsOwnerAndInvokesOnChange()
    {
        // Arrange
        var inputFile = new InputFile();
        var file1 = new BrowserFile();
        var file2 = new BrowserFile();
        var invoked = false;
        InputFileChangeEventArgs? receivedArgs = null;

        inputFile.OnChange = new EventCallback<InputFileChangeEventArgs>((IHandleEvent?)null, (Func<InputFileChangeEventArgs, Task>)(args => { invoked = true; receivedArgs = args; return Task.CompletedTask; }));

        // Act
        await ((IInputFileJsCallbacks)inputFile).NotifyChange(new[] { file1, file2 });

        // Assert
        Assert.True(invoked);
        Assert.NotNull(receivedArgs);
        Assert.Equal(2, receivedArgs!.FileCount);
        Assert.Same(inputFile, ((BrowserFile)receivedArgs.GetMultipleFiles().First()).Owner);
    }

    private class TestJSRuntime : IJSRuntime
    {
        public List<(string identifier, object?[]? args)> Invocations { get; } = new();

        private readonly Func<string, object?[]?, object?>? _onInvoke;

        public TestJSRuntime(Func<string, object?[]?, object?>? onInvoke = null)
        {
            _onInvoke = onInvoke;
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            Invocations.Add((identifier, args));
            var result = _onInvoke?.Invoke(identifier, args);
            return new ValueTask<TValue>((TValue)result!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            Invocations.Add((identifier, args));
            var result = _onInvoke?.Invoke(identifier, args);
            return new ValueTask<TValue>((TValue)result!);
        }

        public ValueTask InvokeVoidAsync(string identifier, object?[]? args)
        {
            Invocations.Add((identifier, args));
            return new ValueTask();
        }
    }

    private class TestInputFile : InputFile
    {
        public Task CallOnAfterRenderAsync(bool firstRender) => base.OnAfterRenderAsync(firstRender);

        public void CallBuildRenderTree(RenderTreeBuilder builder) => base.BuildRenderTree(builder);
    }

    [Fact]
    public async Task OnAfterRenderAsync_DoesNotInvokeInit_OnSubsequentRenders()
    {
        var inputFile = new TestInputFile();
        var jsRuntime = new TestJSRuntime();
        inputFile.JSRuntime = jsRuntime;

        await inputFile.CallOnAfterRenderAsync(firstRender: true);

        jsRuntime.Invocations.Clear();

        await inputFile.CallOnAfterRenderAsync(firstRender: false);

        Assert.DoesNotContain(jsRuntime.Invocations, i => i.identifier == InputFileInterop.Init);
    }

    [Fact]
    public async Task ConvertToImageFileAsync_PassesCorrectArgumentsToJs()
    {
        var inputFile = new TestInputFile();
        var file = new BrowserFile { Id = 99 };
        var jsRuntime = new TestJSRuntime((identifier, args) =>
        {
            if (identifier == InputFileInterop.ToImageFile)
            {
                return new BrowserFile { Id = 42 };
            }

            return null;
        });

        inputFile.JSRuntime = jsRuntime;

        var builder = new RenderTreeBuilder();
        inputFile.CallBuildRenderTree(builder);

        var result = await inputFile.ConvertToImageFileAsync(file, format: "png", maxWidth: 200, maxHeight: 300);

        Assert.Contains(jsRuntime.Invocations, i => i.identifier == InputFileInterop.ToImageFile);

        var invocation = jsRuntime.Invocations.First(i => i.identifier == InputFileInterop.ToImageFile);
        Assert.NotNull(invocation.args);
        Assert.Equal(5, invocation.args!.Length);
        Assert.IsType<ElementReference>(invocation.args[0]);
        Assert.Equal(file.Id, invocation.args[1]);
        Assert.Equal("png", invocation.args[2]);
        Assert.Equal(200, invocation.args[3]);
        Assert.Equal(300, invocation.args[4]);
    }

    [Fact]
    public void BuildRenderTree_DoesNotSetElementReference_WhenCalledDirectly()
    {
        var inputFile = new TestInputFile();

        Assert.Equal(default(ElementReference), inputFile.Element);

        var builder = new RenderTreeBuilder();
        inputFile.CallBuildRenderTree(builder);

        // When BuildRenderTree is invoked directly (without a renderer), the
        // element reference capture action isn't executed so the Element stays
        // at its default value.
        Assert.Equal(default(ElementReference), inputFile.Element);
    }

    [Fact]
    public async Task OnAfterRenderAsync_PassesCorrectArgumentsToJs()
    {
        var inputFile = new TestInputFile();
        var jsRuntime = new TestJSRuntime();
        inputFile.JSRuntime = jsRuntime;

        await inputFile.CallOnAfterRenderAsync(firstRender: true);

        var invocation = jsRuntime.Invocations.First(i => i.identifier == InputFileInterop.Init);
        Assert.NotNull(invocation.args);
        Assert.Equal(2, invocation.args!.Length);
        Assert.NotNull(invocation.args[0]);
        Assert.IsType<ElementReference>(invocation.args[1]);
    }

    [Fact]
    public async Task NotifyChange_WithNoFiles_InvokesOnChangeWithZeroCount()
    {
        var inputFile = new InputFile();
        var invoked = false;
        InputFileChangeEventArgs? receivedArgs = null;

        inputFile.OnChange = new EventCallback<InputFileChangeEventArgs>((IHandleEvent?)null, (Func<InputFileChangeEventArgs, Task>)(args => { invoked = true; receivedArgs = args; return Task.CompletedTask; }));

        await ((IInputFileJsCallbacks)inputFile).NotifyChange(Array.Empty<BrowserFile>());

        Assert.True(invoked);
        Assert.NotNull(receivedArgs);
        Assert.Equal(0, receivedArgs!.FileCount);
    }

    [Fact]
    public void BrowserFile_Size_ThrowsOnNegative()
    {
        var file = new BrowserFile();

        Assert.Throws<ArgumentOutOfRangeException>(() => file.Size = -1);
    }

    [Fact]
    public void BrowserFile_OpenReadStream_ThrowsIfTooLarge()
    {
        var file = new BrowserFile { Size = 1000 };

        Assert.Throws<IOException>(() => file.OpenReadStream(maxAllowedSize: 10));
    }

    [Fact]
    public async Task BrowserFile_OpenReadStream_ReadsDataFromJsStream()
    {
        var inputFile = new InputFile();
        var file = new BrowserFile { Id = 1, Size = 3 };
        var bytes = new byte[] { 1, 2, 3 };

        var jsRuntime = new TestJSRuntime((identifier, args) =>
        {
            if (identifier == InputFileInterop.ReadFileData)
            {
                return new TestJSStreamReference(new MemoryStream(bytes));
            }

            return null;
        });

        inputFile.JSRuntime = jsRuntime;
        file.Owner = inputFile;

        using var stream = file.OpenReadStream(maxAllowedSize: 1000);
        var buffer = new byte[3];
        var read = await stream.ReadAsync(buffer, 0, buffer.Length);

        Assert.Equal(3, read);
        Assert.Equal(bytes, buffer);
    }

    [Fact]
    public async Task BrowserFile_OpenReadStream_PassesCorrectArgumentsToJs()
    {
        var inputFile = new InputFile();
        var file = new BrowserFile { Id = 7, Size = 3 };
        var bytes = new byte[] { 9, 8, 7 };

        var jsRuntime = new TestJSRuntime((identifier, args) =>
        {
            if (identifier == InputFileInterop.ReadFileData)
            {
                return new TestJSStreamReference(new MemoryStream(bytes));
            }

            return null;
        });

        inputFile.JSRuntime = jsRuntime;
        file.Owner = inputFile;

        using var stream = file.OpenReadStream(maxAllowedSize: 1000);
        var buffer = new byte[3];
        var read = await stream.ReadAsync(buffer, 0, buffer.Length);

        Assert.Equal(3, read);
        Assert.Equal(bytes, buffer);

        var invocation = jsRuntime.Invocations.First(i => i.identifier == InputFileInterop.ReadFileData);
        Assert.NotNull(invocation.args);
        Assert.Equal(2, invocation.args!.Length);
        Assert.IsType<ElementReference>(invocation.args[0]);
        Assert.Equal(file.Id, invocation.args[1]);
    }

    [Fact]
    public void BrowserFile_DefaultPropertyValues()
    {
        var file = new BrowserFile();

        Assert.Equal(string.Empty, file.Name);
        Assert.Equal(string.Empty, file.ContentType);
        Assert.Equal(default(DateTimeOffset), file.LastModified);
        Assert.Null(file.RelativePath);
    }

    [Fact]
    public void RequestImageFileAsync_ThrowsIfCustomIBrowserFile()
    {
        var custom = new CustomBrowserFile();

        var ex = Assert.Throws<InvalidOperationException>(() => BrowserFileExtensions.RequestImageFileAsync(custom, "jpeg", 100, 100));
        Assert.Contains("custom", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RequestImageFileAsync_DelegatesToOwner()
    {
        var inputFile = new InputFile();
        var browserFile = new BrowserFile { Id = 5, Name = "f", Size = 1 };
        var returned = new BrowserFile { Id = 6 };

        var jsRuntime = new TestJSRuntime((identifier, args) =>
        {
            if (identifier == InputFileInterop.ToImageFile)
            {
                return returned;
            }

            return null;
        });

        inputFile.JSRuntime = jsRuntime;
        browserFile.Owner = inputFile;

        var result = await BrowserFileExtensions.RequestImageFileAsync(browserFile, "png", 10, 10);

        Assert.Same(returned, result);
        Assert.Same(inputFile, ((BrowserFile)result).Owner);
    }

    private class CustomBrowserFile : IBrowserFile
    {
        public string Name => "custom";
        public DateTimeOffset LastModified => DateTimeOffset.Now;
        public long Size => 10;
        public string ContentType => "image/png";
        public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }

    private class TestJSStreamReference : IJSStreamReference
    {
        private readonly Stream _stream;

        public TestJSStreamReference(Stream stream)
        {
            _stream = stream;
        }

        public ValueTask<Stream> OpenReadStreamAsync(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
            => new ValueTask<Stream>(_stream);

        public ValueTask DisposeAsync()
        {
            try
            {
                _stream.Dispose();
            }
            catch { }

            return new ValueTask();
        }
        public long Length => _stream.CanSeek ? _stream.Length : 0L;
    }

    [Fact]
    public async Task BrowserFileStream_MembersThrowOrReturnExpectedValues()
    {
        var inputFile = new InputFile();
        var file = new BrowserFile { Id = 11, Size = 3 };
        var bytes = new byte[] { 1, 2, 3 };

        var jsRuntime = new TestJSRuntime((identifier, args) =>
        {
            if (identifier == InputFileInterop.ReadFileData)
            {
                return new TestJSStreamReference(new MemoryStream(bytes));
            }

            return null;
        });

        inputFile.JSRuntime = jsRuntime;
        file.Owner = inputFile;

        using var stream = file.OpenReadStream(maxAllowedSize: 1000);

        Assert.True(stream.CanRead);
        Assert.False(stream.CanSeek);
        Assert.False(stream.CanWrite);
        Assert.Equal(file.Size, stream.Length);

        // Synchronous read not supported
        Assert.Throws<NotSupportedException>(() => stream.Read(new byte[1], 0, 1));

        // Seek/Write/Flush/SetLength not supported
        Assert.Throws<NotSupportedException>(() => stream.Seek(0, SeekOrigin.Begin));
        Assert.Throws<NotSupportedException>(() => stream.Write(new byte[1], 0, 1));
        Assert.Throws<NotSupportedException>(() => stream.Flush());
        Assert.Throws<NotSupportedException>(() => stream.SetLength(10));

        // Position setter not supported
        Assert.Throws<NotSupportedException>(() => stream.Position = 1);

        // Read to end then additional read returns 0
        var buffer = new byte[3];
        var read = await stream.ReadAsync(buffer, 0, buffer.Length);
        Assert.Equal(3, read);
        var read2 = await stream.ReadAsync(buffer, 0, buffer.Length);
        Assert.Equal(0, read2);
    }
}
