// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using Xunit;

namespace Microsoft.AspNetCore.Components.Platform.Tests;

public class UrlTest
{
    [Fact]
    public void IterationMethods_ReturnAsyncEnumerables()
    {
        Assert.Equal(typeof(IAsyncEnumerable<KeyValuePair<string, string>>),
            typeof(UrlSearchParams).GetMethod(nameof(UrlSearchParams.EntriesAsync))!.ReturnType);
        Assert.Equal(typeof(IAsyncEnumerable<string>),
            typeof(UrlSearchParams).GetMethod(nameof(UrlSearchParams.KeysAsync))!.ReturnType);
        Assert.Equal(typeof(IAsyncEnumerable<string>),
            typeof(UrlSearchParams).GetMethod(nameof(UrlSearchParams.ValuesAsync))!.ReturnType);
    }

    [Fact]
    public async Task AddBrowserPlatform_ProvidesStableConstructorFacadesWithoutInterop()
    {
        var jsRuntime = new RecordingJSRuntime();
        await using var provider = CreateServiceProvider(jsRuntime);
        var window = provider.GetRequiredService<IBrowserPlatform>().Window;

        Assert.Same(window.Url, window.Url);
        Assert.Same(window.UrlSearchParams, window.UrlSearchParams);
        Assert.Empty(jsRuntime.ConstructorCalls);
        Assert.Empty(jsRuntime.Invocations);
    }

    [Fact]
    public async Task UrlConstructor_CreatesParsesAndChecksUrls()
    {
        var jsRuntime = new RecordingJSRuntime();
        await using var provider = CreateServiceProvider(jsRuntime);
        var constructor = provider.GetRequiredService<IBrowserPlatform>().Window.Url;

        await using var absolute = await constructor.CreateAsync("https://example.test/a");
        await using var relative = await constructor.CreateAsync("../b", "https://example.test/a/");
        await using var parsed = await constructor.ParseAsync("/c", "https://example.test/");
        var invalid = await constructor.ParseAsync("invalid");
        var canParse = await constructor.CanParseAsync("/d", "https://example.test/");

        Assert.NotNull(parsed);
        Assert.Null(invalid);
        Assert.True(canParse);
        Assert.Collection(
            jsRuntime.ConstructorCalls,
            call => AssertInvocation(call, "URL", "https://example.test/a"),
            call => AssertInvocation(call, "URL", "../b", "https://example.test/a/"));
        Assert.Collection(
            jsRuntime.Invocations,
            call => AssertInvocation(call, "URL.parse", "/c", "https://example.test/"),
            call => AssertInvocation(call, "URL.parse", "invalid"),
            call => AssertInvocation(call, "URL.canParse", "/d", "https://example.test/"));
    }

    [Fact]
    public async Task Url_ForwardsAllMembersAndPreservesSearchParamsIdentity()
    {
        var jsRuntime = new RecordingJSRuntime();
        await using var provider = CreateServiceProvider(jsRuntime);
        await using var url = await provider.GetRequiredService<IBrowserPlatform>().Window.Url.CreateAsync("https://example.test/");
        var reference = jsRuntime.UrlReferences.Single();

        await url.SetHrefAsync("https://user:password@example.test:8080/path?query=one#fragment");
        await url.SetProtocolAsync("http:");
        await url.SetUsernameAsync("new-user");
        await url.SetPasswordAsync("new-password");
        await url.SetHostAsync("host.test:90");
        await url.SetHostnameAsync("name.test");
        await url.SetPortAsync("91");
        await url.SetPathnameAsync("/products");
        await url.SetSearchAsync("?page=2");
        await url.SetHashAsync("#details");

        Assert.Equal(reference.Properties["href"], await url.GetHrefAsync());
        Assert.Equal(reference.Properties["origin"], await url.GetOriginAsync());
        Assert.Equal(reference.Properties["protocol"], await url.GetProtocolAsync());
        Assert.Equal(reference.Properties["username"], await url.GetUsernameAsync());
        Assert.Equal(reference.Properties["password"], await url.GetPasswordAsync());
        Assert.Equal(reference.Properties["host"], await url.GetHostAsync());
        Assert.Equal(reference.Properties["hostname"], await url.GetHostnameAsync());
        Assert.Equal(reference.Properties["port"], await url.GetPortAsync());
        Assert.Equal(reference.Properties["pathname"], await url.GetPathnameAsync());
        Assert.Equal(reference.Properties["search"], await url.GetSearchAsync());
        Assert.Equal(reference.Properties["hash"], await url.GetHashAsync());
        Assert.Equal(reference.Properties["href"], await url.ToStringAsync());
        Assert.Equal(reference.Properties["href"], await url.ToJsonAsync());
        Assert.Same(url.SearchParams, url.SearchParams);
        Assert.Equal(0, reference.SearchParamsGetCount);

        await url.SearchParams.GetSizeAsync();

        Assert.Equal(1, reference.SearchParamsGetCount);
    }

    [Fact]
    public async Task UrlSearchParams_ForwardsOperationsAndUsesLiveIterators()
    {
        var jsRuntime = new RecordingJSRuntime();
        await using var provider = CreateServiceProvider(jsRuntime);
        await using var searchParams = await provider.GetRequiredService<IBrowserPlatform>().Window.UrlSearchParams.CreateAsync("tag=one&page=2");

        await searchParams.AppendAsync("tag", "two");
        await searchParams.SetAsync("page", "3");
        Assert.Equal("3", await searchParams.GetAsync("page"));
        Assert.Equal(["one", "two"], await searchParams.GetAllAsync("tag"));
        Assert.True(await searchParams.HasAsync("tag"));
        Assert.True(await searchParams.HasAsync("tag", "two"));
        Assert.Equal(3u, await searchParams.GetSizeAsync());

        var entries = new List<KeyValuePair<string, string>>();
        await foreach (var entry in searchParams.EntriesAsync())
        {
            entries.Add(entry);
            if (entries.Count == 1)
            {
                await searchParams.AppendAsync("after", "iterator");
            }
        }

        Assert.Equal([new("tag", "one"), new("page", "3"), new("tag", "two"), new("after", "iterator")], entries);
        Assert.Equal(["tag", "page", "tag", "after"], await CollectAsync(searchParams.KeysAsync()));
        Assert.Equal(["one", "3", "two", "iterator"], await CollectAsync(searchParams.ValuesAsync()));
        Assert.All(jsRuntime.Iterators, iterator => Assert.True(iterator.Disposed));

        await searchParams.DeleteAsync("tag", "one");
        await searchParams.DeleteAsync("after");
        await searchParams.SortAsync();

        Assert.Equal("page=3&tag=two", await searchParams.ToStringAsync());
    }

    [Fact]
    public async Task UrlSearchParamsConstructor_ForwardsEveryInputShape()
    {
        var jsRuntime = new RecordingJSRuntime();
        await using var provider = CreateServiceProvider(jsRuntime);
        var constructor = provider.GetRequiredService<IBrowserPlatform>().Window.UrlSearchParams;
        IReadOnlyDictionary<string, string> record = new Dictionary<string, string>
        {
            ["name"] = "record",
        };
        IEnumerable<KeyValuePair<string, string>> sequence =
        [
            new("name", "first"),
            new("name", "second"),
        ];

        await using var empty = await constructor.CreateAsync();
        await using var text = await constructor.CreateAsync("name=text");
        await using var fromRecord = await constructor.CreateAsync(record);
        await using var fromSequence = await constructor.CreateAsync(sequence);

        Assert.Collection(
            jsRuntime.ConstructorCalls,
            call => AssertInvocation(call, "URLSearchParams"),
            call => AssertInvocation(call, "URLSearchParams", "name=text"),
            call => AssertInvocation(call, "URLSearchParams", record),
            call =>
            {
                Assert.Equal("URLSearchParams", call.Identifier);
                var entries = Assert.IsType<string[][]>(Assert.Single(call.Arguments));
                Assert.Equal(["name", "first"], entries[0]);
                Assert.Equal(["name", "second"], entries[1]);
            });
    }

    [Fact]
    public async Task Enumeration_IsLazyAndCreatesIndependentIterators()
    {
        var jsRuntime = new RecordingJSRuntime();
        await using var provider = CreateServiceProvider(jsRuntime);
        await using var searchParams = await provider.GetRequiredService<IBrowserPlatform>().Window.UrlSearchParams.CreateAsync("item=first&item=second");
        var sequence = searchParams.ValuesAsync();
        await using var first = sequence.GetAsyncEnumerator();
        await using var second = sequence.GetAsyncEnumerator();

        Assert.Empty(jsRuntime.Iterators);
        Assert.True(await first.MoveNextAsync());
        Assert.Equal("first", first.Current);
        Assert.True(await first.MoveNextAsync());
        Assert.Equal("second", first.Current);
        Assert.True(await second.MoveNextAsync());
        Assert.Equal("first", second.Current);
        Assert.Equal(["first", "second"], await CollectAsync(sequence));
        Assert.Equal(3, jsRuntime.Iterators.Count);
    }

    [Fact]
    public async Task Enumeration_DisposesIteratorOnBreakAndConsumerException()
    {
        var jsRuntime = new RecordingJSRuntime();
        await using var provider = CreateServiceProvider(jsRuntime);
        await using var searchParams = await provider.GetRequiredService<IBrowserPlatform>().Window.UrlSearchParams.CreateAsync("item=first&item=second");

        await foreach (var value in searchParams.ValuesAsync())
        {
            Assert.Equal("first", value);
            break;
        }

        Assert.True(jsRuntime.Iterators.Single().Disposed);
        var exception = new InvalidOperationException("Consumer failed.");
        var actual = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var value in searchParams.ValuesAsync())
            {
                throw exception;
            }
        });

        Assert.Same(exception, actual);
        Assert.All(jsRuntime.Iterators, iterator => Assert.True(iterator.Disposed));
        Assert.Equal(["first", "second"], await CollectAsync(searchParams.ValuesAsync()));
    }

    [Fact]
    public async Task Enumeration_DisposesIteratorOnBrowserException()
    {
        var jsRuntime = new RecordingJSRuntime();
        await using var provider = CreateServiceProvider(jsRuntime);
        await using var searchParams = await provider.GetRequiredService<IBrowserPlatform>().Window.UrlSearchParams.CreateAsync("item=first");
        await using var iterator = searchParams.ValuesAsync().GetAsyncEnumerator();
        Assert.True(await iterator.MoveNextAsync());
        var reference = jsRuntime.Iterators.Single();
        var exception = new JSException("Iterator failed.");
        reference.NextException = exception;

        var actual = await Assert.ThrowsAsync<JSException>(() => iterator.MoveNextAsync().AsTask());

        Assert.Same(exception, actual);
        Assert.True(reference.Disposed);
    }

    [Fact]
    public async Task Enumeration_ObservesCancellationBetweenCallsAndDisposesIterator()
    {
        var jsRuntime = new RecordingJSRuntime();
        await using var provider = CreateServiceProvider(jsRuntime);
        await using var searchParams = await provider.GetRequiredService<IBrowserPlatform>().Window.UrlSearchParams.CreateAsync("item=first&item=second");
        using var cancellation = new CancellationTokenSource();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var value in searchParams.ValuesAsync().WithCancellation(cancellation.Token))
            {
                Assert.Equal("first", value);
                cancellation.Cancel();
            }
        });

        var reference = jsRuntime.Iterators.Single();
        Assert.Equal(1, reference.NextCount);
        Assert.True(reference.Disposed);
    }

    [Fact]
    public async Task Enumeration_DoesNotAbortPendingBrowserCallOnCancellation()
    {
        var jsRuntime = new RecordingJSRuntime();
        await using var provider = CreateServiceProvider(jsRuntime);
        await using var searchParams = await provider.GetRequiredService<IBrowserPlatform>().Window.UrlSearchParams.CreateAsync("item=first");
        using var cancellation = new CancellationTokenSource();
        await using var iterator = searchParams.ValuesAsync().GetAsyncEnumerator(cancellation.Token);
        Assert.True(await iterator.MoveNextAsync());
        var reference = jsRuntime.Iterators.Single();
        reference.PendingNext = new(TaskCreationOptions.RunContinuationsAsynchronously);

        var pendingMove = iterator.MoveNextAsync().AsTask();
        cancellation.Cancel();

        Assert.False(pendingMove.IsCompleted);
        reference.PendingNext.SetResult("""{"Done":true}""");
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pendingMove);
        Assert.True(reference.Disposed);
    }

    [Fact]
    public async Task Enumeration_DoesNotAcquireIteratorWhenAlreadyCanceled()
    {
        var jsRuntime = new RecordingJSRuntime();
        await using var provider = CreateServiceProvider(jsRuntime);
        await using var searchParams = await provider.GetRequiredService<IBrowserPlatform>().Window.UrlSearchParams.CreateAsync();
        await using var iterator = searchParams.ValuesAsync().GetAsyncEnumerator(new CancellationToken(canceled: true));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => iterator.MoveNextAsync().AsTask());

        Assert.Empty(jsRuntime.Iterators);
    }

    [Fact]
    public async Task Enumeration_RejectsParentDisposalAndDisposesIterator()
    {
        var jsRuntime = new RecordingJSRuntime();
        await using var provider = CreateServiceProvider(jsRuntime);
        await using var url = await provider.GetRequiredService<IBrowserPlatform>().Window.Url.CreateAsync("https://example.test/");
        await url.SearchParams.AppendAsync("item", "first");
        await using var iterator = url.SearchParams.ValuesAsync().GetAsyncEnumerator();
        Assert.True(await iterator.MoveNextAsync());

        await url.DisposeAsync();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => iterator.MoveNextAsync().AsTask());
        Assert.True(jsRuntime.Iterators.Single().Disposed);
    }

    [Fact]
    public async Task OwnedObjects_DisposeReferencesAndRejectFurtherOperations()
    {
        var jsRuntime = new RecordingJSRuntime();
        await using var provider = CreateServiceProvider(jsRuntime);
        var window = provider.GetRequiredService<IBrowserPlatform>().Window;
        var url = await window.Url.CreateAsync("https://example.test/");
        var borrowed = url.SearchParams;
        await borrowed.GetSizeAsync();
        var owned = await window.UrlSearchParams.CreateAsync();
        await using var iterator = owned.EntriesAsync().GetAsyncEnumerator();
        Assert.False(await iterator.MoveNextAsync());

        await url.DisposeAsync();
        await url.DisposeAsync();
        await owned.DisposeAsync();
        await owned.DisposeAsync();
        await iterator.DisposeAsync();

        Assert.True(jsRuntime.UrlReferences.Single().Disposed);
        Assert.True(jsRuntime.UrlReferences.Single().SearchParams.Disposed);
        Assert.True(jsRuntime.StandaloneSearchParamsReferences.Single().Disposed);
        Assert.True(jsRuntime.Iterators.Single().Disposed);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => url.GetHrefAsync().AsTask());
        await Assert.ThrowsAsync<ObjectDisposedException>(() => borrowed.GetSizeAsync().AsTask());
        await Assert.ThrowsAsync<ObjectDisposedException>(() => owned.GetSizeAsync().AsTask());
        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await CollectAsync(owned.EntriesAsync()));
    }

    [Fact]
    public async Task DisposingBorrowedSearchParamsDoesNotDisposeUrl()
    {
        var jsRuntime = new RecordingJSRuntime();
        await using var provider = CreateServiceProvider(jsRuntime);
        await using var url = await provider.GetRequiredService<IBrowserPlatform>().Window.Url.CreateAsync("https://example.test/");
        var borrowed = url.SearchParams;

        await borrowed.DisposeAsync();

        Assert.Same(borrowed, url.SearchParams);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => borrowed.GetSizeAsync().AsTask());
        Assert.Equal("https://example.test/", await url.GetHrefAsync());

        await url.DisposeAsync();

        Assert.True(jsRuntime.UrlReferences.Single().Disposed);
    }

    [Fact]
    public async Task BorrowedSearchParams_ReacquiresAfterCanceledAcquisition()
    {
        var jsRuntime = new RecordingJSRuntime();
        await using var provider = CreateServiceProvider(jsRuntime);
        await using var url = await provider.GetRequiredService<IBrowserPlatform>().Window.Url.CreateAsync("https://example.test/");
        var reference = jsRuntime.UrlReferences.Single();
        reference.CancelNextSearchParamsGet = true;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => url.SearchParams.GetSizeAsync().AsTask());
        await url.SearchParams.GetSizeAsync();

        Assert.Equal(2, reference.SearchParamsGetCount);
    }

    [Fact]
    public async Task UrlDisposal_DoesNotWaitForPendingSearchParamsAcquisition()
    {
        var jsRuntime = new RecordingJSRuntime();
        await using var provider = CreateServiceProvider(jsRuntime);
        var url = await provider.GetRequiredService<IBrowserPlatform>().Window.Url.CreateAsync("https://example.test/");
        var reference = jsRuntime.UrlReferences.Single();
        reference.PendingSearchParamsGet = new();

        _ = url.SearchParams.GetSizeAsync();

        await url.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));

        Assert.True(reference.Disposed);
    }

    [Fact]
    public async Task UrlSearchParamsConstructor_SerializesRecordAndSequenceInputs()
    {
        var runtime = new CapturingJSRuntime();
        await using var provider = CreateServiceProvider(runtime);
        var constructor = provider.GetRequiredService<IBrowserPlatform>().Window.UrlSearchParams;
        IReadOnlyDictionary<string, string> record = new Dictionary<string, string>
        {
            ["name"] = "record",
        };
        IEnumerable<KeyValuePair<string, string>> sequence =
        [
            new("name", "first"),
            new("name", "second"),
        ];

        _ = constructor.CreateAsync(record);
        Assert.Equal("[{\"name\":\"record\"}]", runtime.ArgsJson);

        _ = constructor.CreateAsync(sequence);
        Assert.Equal("[[[\"name\",\"first\"],[\"name\",\"second\"]]]", runtime.ArgsJson);
    }

    private static async Task<List<T>> CollectAsync<T>(IAsyncEnumerable<T> source)
    {
        var values = new List<T>();
        await foreach (var value in source)
        {
            values.Add(value);
        }

        return values;
    }

    private static ServiceProvider CreateServiceProvider(IJSRuntime jsRuntime)
    {
        var services = new ServiceCollection();
        services.AddSingleton(jsRuntime);
        services.AddBrowserPlatform();

        return services.BuildServiceProvider();
    }

    private static void AssertInvocation(Invocation invocation, string identifier, params object?[] arguments)
    {
        Assert.Equal(identifier, invocation.Identifier);
        Assert.Equal(arguments, invocation.Arguments);
    }

    private sealed class RecordingJSRuntime : IJSRuntime
    {
        public List<Invocation> ConstructorCalls { get; } = [];

        public List<Invocation> Invocations { get; } = [];

        public List<RecordingUrlReference> UrlReferences { get; } = [];

        public List<RecordingSearchParamsReference> StandaloneSearchParamsReferences { get; } = [];

        public List<RecordingIteratorReference> Iterators { get; } = [];

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            Invocations.Add(new(identifier, args ?? []));

            object? result = identifier switch
            {
                "URL.parse" when Equals(args![0], "invalid") => null,
                "URL.parse" => CreateUrlReference(args![0]!.ToString()!),
                "URL.canParse" => true,
                _ => throw new NotSupportedException(identifier),
            };

            return ValueTask.FromResult((TValue?)result!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args)
            => InvokeAsync<TValue>(identifier, args);

        public ValueTask<IJSObjectReference> InvokeConstructorAsync(string identifier, object?[]? args)
        {
            ConstructorCalls.Add(new(identifier, args ?? []));

            return identifier switch
            {
                "URL" => ValueTask.FromResult<IJSObjectReference>(CreateUrlReference(args![0]!.ToString()!)),
                "URLSearchParams" => ValueTask.FromResult<IJSObjectReference>(CreateSearchParamsReference(args)),
                _ => throw new NotSupportedException(identifier),
            };
        }

        private RecordingUrlReference CreateUrlReference(string href)
        {
            var reference = new RecordingUrlReference(this, href);
            UrlReferences.Add(reference);

            return reference;
        }

        private RecordingSearchParamsReference CreateSearchParamsReference(object?[]? args)
        {
            var reference = new RecordingSearchParamsReference(this);
            StandaloneSearchParamsReferences.Add(reference);

            if (args is [string text])
            {
                foreach (var part in text.Split('&', StringSplitOptions.RemoveEmptyEntries))
                {
                    var pair = part.Split('=', 2);
                    reference.Entries.Add(new(pair[0], pair.Length == 1 ? string.Empty : pair[1]));
                }
            }

            return reference;
        }
    }

    private sealed class RecordingUrlReference : IJSObjectReference
    {
        public RecordingUrlReference(RecordingJSRuntime runtime, string href)
        {
            Runtime = runtime;
            SearchParams = new(runtime);
            Properties["href"] = href;
            Properties["origin"] = "https://example.test";
            Properties["protocol"] = "https:";
            Properties["username"] = string.Empty;
            Properties["password"] = string.Empty;
            Properties["host"] = "example.test";
            Properties["hostname"] = "example.test";
            Properties["port"] = string.Empty;
            Properties["pathname"] = "/";
            Properties["search"] = string.Empty;
            Properties["hash"] = string.Empty;
        }

        public RecordingJSRuntime Runtime { get; }

        public RecordingSearchParamsReference SearchParams { get; }

        public Dictionary<string, string> Properties { get; } = [];

        public bool Disposed { get; private set; }

        public int SearchParamsGetCount { get; private set; }

        public bool CancelNextSearchParamsGet { get; set; }

        public TaskCompletionSource<IJSObjectReference>? PendingSearchParamsGet { get; set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            object result = identifier switch
            {
                "toString" or "toJSON" => Properties["href"],
                _ => throw new NotSupportedException(identifier),
            };

            return ValueTask.FromResult((TValue)result);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args)
            => InvokeAsync<TValue>(identifier, args);

        public ValueTask<TValue> GetValueAsync<TValue>(string identifier)
        {
            if (identifier == "searchParams")
            {
                SearchParamsGetCount++;

                if (CancelNextSearchParamsGet)
                {
                    CancelNextSearchParamsGet = false;

                    return ValueTask.FromCanceled<TValue>(new CancellationToken(canceled: true));
                }

                if (PendingSearchParamsGet is { } pending)
                {
                    PendingSearchParamsGet = null;

                    return new ValueTask<TValue>((Task<TValue>)(object)pending.Task);
                }

                return ValueTask.FromResult((TValue)(object)SearchParams);
            }

            return ValueTask.FromResult((TValue)(object)Properties[identifier]);
        }

        public ValueTask SetValueAsync<TValue>(string identifier, TValue value)
        {
            Properties[identifier] = (string)(object)value!;

            return ValueTask.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            Disposed = true;

            return ValueTask.CompletedTask;
        }
    }

    private sealed class RecordingSearchParamsReference(RecordingJSRuntime runtime) : IJSObjectReference
    {
        public List<KeyValuePair<string, string>> Entries { get; } = [];

        public bool Disposed { get; private set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            object? result = identifier switch
            {
                "append" => Append(args!),
                "delete" => Delete(args!),
                "get" => Entries.FirstOrDefault(pair => pair.Key == (string)args![0]!).Value,
                "getAll" => Entries.Where(pair => pair.Key == (string)args![0]!).Select(pair => pair.Value).ToArray(),
                "has" => Has(args!),
                "set" => Set(args!),
                "sort" => Sort(),
                "entries" => CreateIterator(IteratorKind.Entries),
                "keys" => CreateIterator(IteratorKind.Keys),
                "values" => CreateIterator(IteratorKind.Values),
                "toString" => string.Join("&", Entries.Select(pair => $"{pair.Key}={pair.Value}")),
                _ => throw new NotSupportedException(identifier),
            };

            return ValueTask.FromResult((TValue?)result!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args)
            => InvokeAsync<TValue>(identifier, args);

        public ValueTask<TValue> GetValueAsync<TValue>(string identifier)
        {
            Assert.Equal("size", identifier);

            return ValueTask.FromResult((TValue)(object)(uint)Entries.Count);
        }

        public ValueTask DisposeAsync()
        {
            Disposed = true;

            return ValueTask.CompletedTask;
        }

        private object? Append(object?[] args)
        {
            Entries.Add(new((string)args[0]!, (string)args[1]!));

            return null;
        }

        private object? Delete(object?[] args)
        {
            Entries.RemoveAll(pair =>
                pair.Key == (string)args[0]! &&
                (args.Length == 1 || pair.Value == (string)args[1]!));

            return null;
        }

        private bool Has(object?[] args)
            => Entries.Any(pair =>
                pair.Key == (string)args[0]! &&
                (args.Length == 1 || pair.Value == (string)args[1]!));

        private object? Set(object?[] args)
        {
            var name = (string)args[0]!;
            var index = Entries.FindIndex(pair => pair.Key == name);
            if (index < 0)
            {
                Append(args);
            }
            else
            {
                Entries[index] = new(name, (string)args[1]!);
                for (var i = Entries.Count - 1; i > index; i--)
                {
                    if (Entries[i].Key == name)
                    {
                        Entries.RemoveAt(i);
                    }
                }
            }

            return null;
        }

        private object? Sort()
        {
            Entries.Sort(static (left, right) => StringComparer.Ordinal.Compare(left.Key, right.Key));

            return null;
        }

        private IJSObjectReference CreateIterator(IteratorKind kind)
        {
            var iterator = new RecordingIteratorReference(Entries, kind);
            runtime.Iterators.Add(iterator);

            return iterator;
        }
    }

    private sealed class RecordingIteratorReference(
        List<KeyValuePair<string, string>> entries,
        IteratorKind kind) : IJSObjectReference
    {
        private int _index;

        public bool Disposed { get; private set; }

        public int NextCount { get; private set; }

        public Exception? NextException { get; set; }

        public TaskCompletionSource<string>? PendingNext { get; set; }

        public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            Assert.Equal("next", identifier);
            NextCount++;

            if (NextException is { } exception)
            {
                throw exception;
            }

            var json = PendingNext is { } pending ? await pending.Task : _index >= entries.Count
                ? """{"Done":true}"""
                : kind switch
                {
                    IteratorKind.Entries => JsonSerializer.Serialize(new { Done = false, Value = new[] { entries[_index].Key, entries[_index++].Value } }),
                    IteratorKind.Keys => JsonSerializer.Serialize(new { Done = false, Value = entries[_index++].Key }),
                    IteratorKind.Values => JsonSerializer.Serialize(new { Done = false, Value = entries[_index++].Value }),
                    _ => throw new InvalidOperationException(),
                };

            return JsonSerializer.Deserialize<TValue>(json)!;
        }

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args)
            => throw new InvalidOperationException("Iterator calls must preserve the default interop timeout by omitting a cancellation token.");

        public ValueTask DisposeAsync()
        {
            Disposed = true;

            return ValueTask.CompletedTask;
        }
    }

    private sealed class CapturingJSRuntime : JSRuntime
    {
        public string? ArgsJson { get; private set; }

        protected override void BeginInvokeJS(
            long taskId,
            string identifier,
            [StringSyntax(StringSyntaxAttribute.Json)] string? argsJson,
            JSCallResultType resultType,
            long targetInstanceId)
            => throw new NotSupportedException();

        protected override void BeginInvokeJS(in JSInvocationInfo invocationInfo)
        {
            Assert.Equal("URLSearchParams", invocationInfo.Identifier);
            Assert.Equal(JSCallType.ConstructorCall, invocationInfo.CallType);
            ArgsJson = invocationInfo.ArgsJson;
        }

        protected override void EndInvokeDotNet(
            DotNetInvocationInfo invocationInfo,
            in DotNetInvocationResult invocationResult)
            => throw new NotSupportedException();

        protected override Task TransmitStreamAsync(
            long streamId,
            DotNetStreamReference dotNetStreamReference)
            => Task.CompletedTask;
    }

    private enum IteratorKind
    {
        Entries,
        Keys,
        Values,
    }

    private sealed record Invocation(string Identifier, object?[] Arguments);
}
