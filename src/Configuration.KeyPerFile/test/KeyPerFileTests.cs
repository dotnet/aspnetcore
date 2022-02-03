// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.Extensions.Configuration.KeyPerFile.Test;

public class KeyPerFileTests
{
    [Fact]
    public void DoesNotThrowWhenOptionalAndNoSecrets()
    {
        new ConfigurationBuilder().AddKeyPerFile(o => o.Optional = true).Build();
    }

    [Fact]
    public void DoesNotThrowWhenOptionalAndDirectoryDoesntExist()
    {
        new ConfigurationBuilder().AddKeyPerFile("nonexistent", true).Build();
    }

    [Fact]
    public void ThrowsWhenNotOptionalAndDirectoryDoesntExist()
    {
        var e = Assert.Throws<ArgumentException>(() => new ConfigurationBuilder().AddKeyPerFile("nonexistent", false).Build());
        Assert.Contains("The path must be absolute.", e.Message);
    }

    [Fact]
    public void CanLoadMultipleSecrets()
    {
        var testFileProvider = new TestFileProvider(
            new TestFile("Secret1", "SecretValue1"),
            new TestFile("Secret2", "SecretValue2"));

        var config = new ConfigurationBuilder()
            .AddKeyPerFile(o => o.FileProvider = testFileProvider)
            .Build();

        Assert.Equal("SecretValue1", config["Secret1"]);
        Assert.Equal("SecretValue2", config["Secret2"]);
    }

    [Fact]
    public void CanLoadMultipleSecretsWithDirectory()
    {
        var testFileProvider = new TestFileProvider(
            new TestFile("Secret1", "SecretValue1"),
            new TestFile("Secret2", "SecretValue2"),
            new TestFile("directory"));

        var config = new ConfigurationBuilder()
            .AddKeyPerFile(o => o.FileProvider = testFileProvider)
            .Build();

        Assert.Equal("SecretValue1", config["Secret1"]);
        Assert.Equal("SecretValue2", config["Secret2"]);
    }

    [Fact]
    public void CanLoadNestedKeys()
    {
        var testFileProvider = new TestFileProvider(
            new TestFile("Secret0__Secret1__Secret2__Key", "SecretValue2"),
            new TestFile("Secret0__Secret1__Key", "SecretValue1"),
            new TestFile("Secret0__Key", "SecretValue0"));

        var config = new ConfigurationBuilder()
            .AddKeyPerFile(o => o.FileProvider = testFileProvider)
            .Build();

        Assert.Equal("SecretValue0", config["Secret0:Key"]);
        Assert.Equal("SecretValue1", config["Secret0:Secret1:Key"]);
        Assert.Equal("SecretValue2", config["Secret0:Secret1:Secret2:Key"]);
    }

    [Fact]
    public void LoadWithCustomSectionDelimiter()
    {
        var testFileProvider = new TestFileProvider(
            new TestFile("Secret0--Secret1--Secret2--Key", "SecretValue2"),
            new TestFile("Secret0--Secret1--Key", "SecretValue1"),
            new TestFile("Secret0--Key", "SecretValue0"));

        var config = new ConfigurationBuilder()
            .AddKeyPerFile(o =>
            {
                o.FileProvider = testFileProvider;
                o.SectionDelimiter = "--";
            })
            .Build();

        Assert.Equal("SecretValue0", config["Secret0:Key"]);
        Assert.Equal("SecretValue1", config["Secret0:Secret1:Key"]);
        Assert.Equal("SecretValue2", config["Secret0:Secret1:Secret2:Key"]);
    }

    [Fact]
    public void CanIgnoreFilesWithDefault()
    {
        var testFileProvider = new TestFileProvider(
            new TestFile("ignore.Secret0", "SecretValue0"),
            new TestFile("ignore.Secret1", "SecretValue1"),
            new TestFile("Secret2", "SecretValue2"));

        var config = new ConfigurationBuilder()
            .AddKeyPerFile(o => o.FileProvider = testFileProvider)
            .Build();

        Assert.Null(config["ignore.Secret0"]);
        Assert.Null(config["ignore.Secret1"]);
        Assert.Equal("SecretValue2", config["Secret2"]);
    }

    [Fact]
    public void CanTurnOffDefaultIgnorePrefixWithCondition()
    {
        var testFileProvider = new TestFileProvider(
            new TestFile("ignore.Secret0", "SecretValue0"),
            new TestFile("ignore.Secret1", "SecretValue1"),
            new TestFile("Secret2", "SecretValue2"));

        var config = new ConfigurationBuilder()
            .AddKeyPerFile(o =>
            {
                o.FileProvider = testFileProvider;
                o.IgnoreCondition = null;
            })
            .Build();

        Assert.Equal("SecretValue0", config["ignore.Secret0"]);
        Assert.Equal("SecretValue1", config["ignore.Secret1"]);
        Assert.Equal("SecretValue2", config["Secret2"]);
    }

    [Fact]
    public void CanIgnoreAllWithCondition()
    {
        var testFileProvider = new TestFileProvider(
            new TestFile("Secret0", "SecretValue0"),
            new TestFile("Secret1", "SecretValue1"),
            new TestFile("Secret2", "SecretValue2"));

        var config = new ConfigurationBuilder()
            .AddKeyPerFile(o =>
            {
                o.FileProvider = testFileProvider;
                o.IgnoreCondition = s => true;
            })
            .Build();

        Assert.Empty(config.AsEnumerable());
    }

    [Fact]
    public void CanIgnoreFilesWithCustomIgnore()
    {
        var testFileProvider = new TestFileProvider(
            new TestFile("meSecret0", "SecretValue0"),
            new TestFile("meSecret1", "SecretValue1"),
            new TestFile("Secret2", "SecretValue2"));

        var config = new ConfigurationBuilder()
            .AddKeyPerFile(o =>
            {
                o.FileProvider = testFileProvider;
                o.IgnorePrefix = "me";
            })
            .Build();

        Assert.Null(config["meSecret0"]);
        Assert.Null(config["meSecret1"]);
        Assert.Equal("SecretValue2", config["Secret2"]);
    }

    [Fact]
    public void CanUnIgnoreDefaultFiles()
    {
        var testFileProvider = new TestFileProvider(
            new TestFile("ignore.Secret0", "SecretValue0"),
            new TestFile("ignore.Secret1", "SecretValue1"),
            new TestFile("Secret2", "SecretValue2"));

        var config = new ConfigurationBuilder()
            .AddKeyPerFile(o =>
            {
                o.FileProvider = testFileProvider;
                o.IgnorePrefix = null;
            })
            .Build();

        Assert.Equal("SecretValue0", config["ignore.Secret0"]);
        Assert.Equal("SecretValue1", config["ignore.Secret1"]);
        Assert.Equal("SecretValue2", config["Secret2"]);
    }

    [Fact]
    public void BindingDoesNotThrowIfReloadedDuringBinding()
    {
        var testFileProvider = new TestFileProvider(
            new TestFile("Number", "-2"),
            new TestFile("Text", "Foo"));

        var config = new ConfigurationBuilder()
            .AddKeyPerFile(o => o.FileProvider = testFileProvider)
            .Build();

        MyOptions options = null;

        using (var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(250)))
        {
            void ReloadLoop()
            {
                while (!cts.IsCancellationRequested)
                {
                    config.Reload();
                }
            }

            _ = Task.Run(ReloadLoop);

            while (!cts.IsCancellationRequested)
            {
                options = config.Get<MyOptions>();
            }
        }

        Assert.Equal(-2, options.Number);
        Assert.Equal("Foo", options.Text);
    }

    [Fact]
    public void ReloadConfigWhenReloadOnChangeIsTrue()
    {
        var testFileProvider = new TestFileProvider(
           new TestFile("Secret1", "SecretValue1"),
           new TestFile("Secret2", "SecretValue2"));

        var config = new ConfigurationBuilder()
            .AddKeyPerFile(o =>
            {
                o.FileProvider = testFileProvider;
                o.ReloadOnChange = true;
            }).Build();

        Assert.Equal("SecretValue1", config["Secret1"]);
        Assert.Equal("SecretValue2", config["Secret2"]);

        testFileProvider.ChangeFiles(
            new TestFile("Secret1", "NewSecretValue1"),
            new TestFile("Secret3", "NewSecretValue3"));

        Assert.Equal("NewSecretValue1", config["Secret1"]);
        Assert.Null(config["NewSecret2"]);
        Assert.Equal("NewSecretValue3", config["Secret3"]);
    }

    [Fact]
    public void SameConfigWhenReloadOnChangeIsFalse()
    {
        var testFileProvider = new TestFileProvider(
           new TestFile("Secret1", "SecretValue1"),
           new TestFile("Secret2", "SecretValue2"));

        var config = new ConfigurationBuilder()
            .AddKeyPerFile(o =>
            {
                o.FileProvider = testFileProvider;
                o.ReloadOnChange = false;
            }).Build();

        Assert.Equal("SecretValue1", config["Secret1"]);
        Assert.Equal("SecretValue2", config["Secret2"]);

        testFileProvider.ChangeFiles(
            new TestFile("Secret1", "NewSecretValue1"),
            new TestFile("Secret3", "NewSecretValue3"));

        Assert.Equal("SecretValue1", config["Secret1"]);
        Assert.Equal("SecretValue2", config["Secret2"]);
    }

    [Fact]
    public void NoFilesReloadWhenAddedFiles()
    {
        var testFileProvider = new TestFileProvider();

        var config = new ConfigurationBuilder()
            .AddKeyPerFile(o =>
            {
                o.FileProvider = testFileProvider;
                o.ReloadOnChange = true;
                o.Optional = true;
            }).Build();

        Assert.Empty(config.AsEnumerable());

        testFileProvider.ChangeFiles(
            new TestFile("Secret1", "SecretValue1"),
            new TestFile("Secret2", "SecretValue2"));

        Assert.Equal("SecretValue1", config["Secret1"]);
        Assert.Equal("SecretValue2", config["Secret2"]);
    }

    [Fact(Timeout = 2000)]
    public async Task RaiseChangeEventWhenReloadOnChangeIsTrue()
    {
        var testFileProvider = new TestFileProvider(
           new TestFile("Secret1", "SecretValue1"),
           new TestFile("Secret2", "SecretValue2"));

        var config = new ConfigurationBuilder()
            .AddKeyPerFile(o =>
            {
                o.FileProvider = testFileProvider;
                o.ReloadOnChange = true;
            }).Build();

        var changeToken = config.GetReloadToken();
        var changeTaskCompletion = new TaskCompletionSource<object>();
        changeToken.RegisterChangeCallback(state =>
            ((TaskCompletionSource<object>)state).TrySetResult(null), changeTaskCompletion);

        testFileProvider.ChangeFiles(
            new TestFile("Secret1", "NewSecretValue1"),
            new TestFile("Secret3", "NewSecretValue3"));

        await changeTaskCompletion.Task;

        Assert.Equal("NewSecretValue1", config["Secret1"]);
        Assert.Null(config["NewSecret2"]);
        Assert.Equal("NewSecretValue3", config["Secret3"]);
    }

    [Fact(Timeout = 2000)]
    public async Task RaiseChangeEventWhenDirectoryClearsReloadOnChangeIsTrue()
    {
        var testFileProvider = new TestFileProvider(
           new TestFile("Secret1", "SecretValue1"),
           new TestFile("Secret2", "SecretValue2"));

        var config = new ConfigurationBuilder()
            .AddKeyPerFile(o =>
            {
                o.FileProvider = testFileProvider;
                o.ReloadOnChange = true;
            }).Build();

        var changeToken = config.GetReloadToken();
        var changeTaskCompletion = new TaskCompletionSource<object>();
        changeToken.RegisterChangeCallback(state =>
            ((TaskCompletionSource<object>)state).TrySetResult(null), changeTaskCompletion);

        testFileProvider.ChangeFiles();

        await changeTaskCompletion.Task;

        Assert.Empty(config.AsEnumerable());
    }

    [Fact(Timeout = 2000)]
    public async Task RaiseChangeEventAfterStartingWithEmptyDirectoryReloadOnChangeIsTrue()
    {
        var testFileProvider = new TestFileProvider();

        var config = new ConfigurationBuilder()
            .AddKeyPerFile(o =>
            {
                o.FileProvider = testFileProvider;
                o.ReloadOnChange = true;
                o.Optional = true;
            }).Build();

        var changeToken = config.GetReloadToken();
        var changeTaskCompletion = new TaskCompletionSource<object>();
        changeToken.RegisterChangeCallback(state =>
            ((TaskCompletionSource<object>)state).TrySetResult(null), changeTaskCompletion);

        testFileProvider.ChangeFiles(new TestFile("Secret1", "SecretValue1"));

        await changeTaskCompletion.Task;

        Assert.Equal("SecretValue1", config["Secret1"]);
    }

    [Fact(Timeout = 2000)]
    public async Task RaiseChangeEventAfterProviderSetToNull()
    {
        var testFileProvider = new TestFileProvider(
           new TestFile("Secret1", "SecretValue1"),
           new TestFile("Secret2", "SecretValue2"));
        var configurationSource = new KeyPerFileConfigurationSource
        {
            FileProvider = testFileProvider,
            Optional = true,
        };
        var keyPerFileProvider = new KeyPerFileConfigurationProvider(configurationSource);
        var config = new ConfigurationRoot(new[] { keyPerFileProvider });

        var changeToken = config.GetReloadToken();
        var changeTaskCompletion = new TaskCompletionSource<object>();
        changeToken.RegisterChangeCallback(state =>
            ((TaskCompletionSource<object>)state).TrySetResult(null), changeTaskCompletion);

        configurationSource.FileProvider = null;
        config.Reload();

        await changeTaskCompletion.Task;

        Assert.Empty(config.AsEnumerable());
    }

    private sealed class MyOptions
    {
        public int Number { get; set; }
        public string Text { get; set; }
    }
}

class TestFileProvider : IFileProvider
{
    IDirectoryContents _contents;
    MockChangeToken _changeToken;

    public TestFileProvider(params IFileInfo[] files)
    {
        _contents = new TestDirectoryContents(files);
        _changeToken = new MockChangeToken();
    }

    public IDirectoryContents GetDirectoryContents(string subpath) => _contents;

    public IFileInfo GetFileInfo(string subpath) => new TestFile("TestDirectory");

    public IChangeToken Watch(string filter) => _changeToken;

    internal void ChangeFiles(params IFileInfo[] files)
    {
        _contents = new TestDirectoryContents(files);
        _changeToken.RaiseCallback();
    }
}

class MockChangeToken : IChangeToken
{
    private Action _callback;

    public bool ActiveChangeCallbacks => true;

    public bool HasChanged => true;

    public IDisposable RegisterChangeCallback(Action<object> callback, object state)
    {
        var disposable = new MockDisposable();
        _callback = () => callback(state);
        return disposable;
    }

    internal void RaiseCallback()
    {
        _callback?.Invoke();
    }
}

class MockDisposable : IDisposable
{
    public bool Disposed { get; set; }

    public void Dispose()
    {
        Disposed = true;
    }
}

class TestDirectoryContents : IDirectoryContents
{
    List<IFileInfo> _list;

    public TestDirectoryContents(params IFileInfo[] files)
    {
        _list = new List<IFileInfo>(files);
    }

    public bool Exists => _list.Any();

    public IEnumerator<IFileInfo> GetEnumerator() => _list.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

//TODO: Probably need a directory and file type.
class TestFile : IFileInfo
{
    private readonly string _name;
    private readonly string _contents;

    public bool Exists => true;

    public bool IsDirectory
    {
        get;
    }

    public DateTimeOffset LastModified => throw new NotImplementedException();

    public long Length => throw new NotImplementedException();

    public string Name => _name;

    public string PhysicalPath => "Root/" + Name;

    public TestFile(string name)
    {
        _name = name;
        IsDirectory = true;
    }

    public TestFile(string name, string contents)
    {
        _name = name;
        _contents = contents;
    }

    public Stream CreateReadStream()
    {
        if (IsDirectory)
        {
            throw new InvalidOperationException("Cannot create stream from directory");
        }

        return _contents == null
            ? new MemoryStream()
            : new MemoryStream(Encoding.UTF8.GetBytes(_contents));
    }
}
