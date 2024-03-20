# Hybrid Cache specification

Hybrid Cache is a new API designed to build on top of the existing `Microsoft.Extensions.Caching.Distributed.IDistributedCache` API, to fill multiple functional gaps in the usability of the `IDistributedCache` API,
including:

- stampede protection
- simple pass-thru API usage (i.e. a single method replaces multiple discrete steps required with the old API)
- multi-tier (in-process plus backend) caching
- configurable serialization
- tag-based eviction
- metrics

## Overview

The primary API is a new `abstract` class, `HybridCache`, in a **new** `Microsoft.Extensions.Caching.Distributed` package:

``` c#
namespace Microsoft.Extensions.Caching.Distributed;

public abstract class HybridCache
{ /* more detail below */ }
```

This type acts as the primary API that users will interact with for caching using this feature, replacing `IDistributedCache` (which now becomes a backend API); the purpose of `HybridCache`
is to encapsulate the state required to implement new functionality. This required additional state means that the feature cannot be implemented simply as extension methods
on top of `IDistributedCache` - for example for stampede protection we need to track a bucket of in-flight operations so that we can join existing backend operations. Every feature listed
(except perhaps for the pass-thru API usage) requires some state or additional service.

Microsoft will provide a concrete implementation of `HybridCache` via dependency injection, but it is explicitly intended that the API can be implemented independently if desired.

### Why "Hybrid Cache"?

This name seems to capture the multiple roles being fulfilled by the cache implementation. A number of otions have been considered, including "read thru cache",
"advanced cache", "distributed cache 2"; this seems to work, though.

### Why not `IHybridCache`?

1. the primary pass-thru API (discussed below) exists in a dual "stateful"/"stateless" mode, with it being possible to reliably and automatically implement one via the other;
   providing this at the definition level halves this aspect of the API surface for concrete implementations, providing a consistent experince
2. it is anticipated that additional future capabilities will be desired on this API; if we limit this as `IHybridCache` it is harder to extend than with an abstract base class that
can implement features with default implementations that implementors can `override` as desired

It is noted that in both cases, "default interface methods", also serve this function; if provide a mechanism to achieve this same goal with an `IHybridCache` approach.
If we feel that "default interface methods" are now fully greenlit for this scenario, we could indeed use an `IHybridCache` approach.

---

## Registering and configuring `HybridCache`

Registering hybrid cache is performed by `HybridCacheServiceExtensions`:

``` c#
namespace Microsoft.Extensions.DependencyInjection;

public static class HybridCacheServiceExtensions
{
    // adds HybridCache using default options
    public static IHybridCacheBuilder AddHybridCache(this IServiceCollection services);
    // adds HybridCache using custom options
    public static IHybridCacheBuilder AddHybridCache(this IServiceCollection services, Action<HybridCacheOptions> configureOptions);
    // adds TSerializer via DI as the serializer for T
    public static IHybridCacheBuilder WithSerializer<T, TSerializer>(this IHybridCacheBuilder builder);
    // adds a concrete custom serializer for a given type
    public static IHybridCacheBuilder WithSerializer<T>(this IHybridCacheBuilder builder, IHybridCacheSerializer<T> serializer);
    // adds T via DI as a serializer factory
    public static IHybridCacheBuilder WithSerializerFactory<T>(this IHybridCacheBuilder builder);
    // adds a concrete custom serializer factory
    public static IHybridCacheBuilder WithSerializerFactory(this IHybridCacheBuilder builder, IHybridCacheSerializerFactory factory);
}

namespace Microsoft.Extensions.Caching.Distributed;

public interface IHybridCacheBuilder
{
    IServiceCollection Services { get; }
}
public interface IHybridCacheSerializer<T>
{
    T Deserialize(ReadOnlySequence<byte> source);
    void Serialize(T value, IBufferWriter<byte> target);
}
public interface IHybridCacheSerializerFactory
{
    bool TryCreateSerializer<T>([NotNullWhen(true)] out IHybridCacheSerializer<T>? serializer);
}
public class HybridCacheOptions
{
    // default expiration etc configuration, if omitted
    public HybridCacheEntryOptions? DefaultOptions { get; set; }
    // maximum payload quota
    public long MaximumPayloadBytes { get; set; } = 1 << 20; // 1MiB
    // whether compression is enabled
    public bool AllowCompression { get; set; } = true;
}
public class HybridCacheEntryOptions
{ /* more detail below */ }
```

where `IHybridCacheBuilder` here functions purely as a wrapper (via `.Services`) to provide contextual API services to configure related services such as serialization,
for API discoverability, for example making it trivial to configure serialization, rather than having to magically know about the existence of specific services that
can be added to influence behaviour. The return value is the same input services collection, for chaining purposes.

The `HybridCacheOptions` provides additional global options for the cache, including payload max quota and a default cache configuration (primarily: lifetime).

The user will often also wish to register an out-of-process `IDistributedCache` backend (Redis, SQL Server, etc) in the usual manner, as
[discussed here](https://learn.microsoft.com/aspnet/core/performance/caching/distributed). Note that this is not required; it is anticipated that simply having
the L1 cache with stampede protection against the backend *provides compelling value*. Options specific to the chosen `IDistributedCache` backend will
be configured as part of that `IDistributedCache` registration, and are not considered here.

---


## Using `HybridCache`

The `HybridCache` instance will be dependency-injected into code that requires them; from there, the primary API is `GetOrCreateAsync` which provides
a stateless and stateful overload pair:

``` c#
public abstract class HybridCache
{
    protected HybridCache() { }

    public abstract ValueTask<T> GetOrCreateAsync<TState, T>(string key, TState state, Func<TState, CancellationToken, ValueTask<T>> callback,
        HybridCacheEntryOptions? options = null, ReadOnlyMemory<string> tags = default, CancellationToken cancellationToken = default);

    public virtual ValueTask<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, ValueTask<T>> callback,
        HybridCacheEntryOptions? options = null, ReadOnlyMemory<string> tags = default, CancellationToken cancellationToken = default)
        => // default implemention provided automatically via GetOrCreateAsync<TState, T>

    // ...
```

The simplest use-case is the stateless option, typically used with a lambda callback using "captured" state, for example:

``` c#
public MyConsumerCode(HybridCache cache)
{
    public async Task<Customer> GetCustomer(Region region, int id)
        => cache.GetOrCreateAsync($"/customer/{region}/{id}", async _ => await SomeExternalBackend.GetAsync(region, id));
}
```

The `GetOrCreateAsync` name is chosen for parity with [`IMemoryCache`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.caching.memory.cacheextensions.getorcreateasync); it
takes a `string` key, and a callback that is used to fetch the underlying data if it is not available in any other cache. In some high throughput scenarios, it may be
preferable to avoid this capture overhead using a `static` callback and the stateful overload:

``` c#
public MyConsumerCode(HybridCache cache)
{
    public async Task<Customer> GetCustomer(Region region, int id)
        => cache.GetOrCreateAsync($"/customer/{region}/{id}", static (region, id) async (_, state) => await SomeExternalBackend.GetAsync(state.region, state.id));
}
```

Optionally, this API allows:

- `HybridCacheEntryOptions`, controlling the duration of the cache entry (see below)
- zero, one or more "tags", which work similarly to the "tags" feature of "Output Cache"
- cancellation

For the options, timeout is only described in relative terms:

``` c#
public sealed class HybridCacheEntryOptions(TimeSpan expiry, TimeSpan localCacheExpiry, HybridCacheEntryFlags flags = 0)
{
    // convenience .ctor to use same expiry for L1+L2
    public HybridCacheEntryOptions(TimeSpan expiry, HybridCacheEntryFlags flags = 0) : this(expiry, expiry, flags) { }

    public TimeSpan Expiry { get; } = backendExpiry; // overall cache duration

    /// <summary>
    /// Cache duration in local cache; when retrieving a cached value
    /// from an external cache store, this value will be used to calculate the local
    /// cache expiration, not exceeding the remaining overall cache lifetime
    /// </summary>
    public TimeSpan LocalCacheExpiry { get; } = localExpiry; // TTL in L1

    public HybridCacheEntryFlags Flags { get; } = flags;
}
[Flags]
public enum HybridCacheEntryFlags
{
    None = 0,
    DisableLocalCache = 1 << 0,
    DisableDistributedCache = 1 << 1,
    DisableCompression = 1 << 2,
}
```

The `Flags` also allow features such as specific caching tiers or compression to be electively *disabled* on a per-scenario basis. It will directed that
entry options should usually be shared (`static readonly`) and reused on a per-scenario basis. To this end, the type is immutable. If no `options` is supplied,
the default from `HybridCacheOptions` is used; this has an implied "reasonable" default timeout (low minutes, probably) in the eventuality that none is specified.



In many cases, `GetOrCreateAsync` *is the only API needed*, but additionally, `HybridCache` has auxiliary APIs:

``` c#
public abstract ValueTask<(bool Exists, T Value)> GetAsync<T>(string key, HybridCacheEntryOptions? options = null, CancellationToken cancellationToken = default);
public abstract ValueTask SetAsync<T>(string key, T value, HybridCacheEntryOptions? options = null, ReadOnlyMemory<string> tags = default, CancellationToken cancellationToken = default);
public abstract ValueTask RemoveKeyAsync(string key, CancellationToken cancellationToken = default);
public virtual ValueTask RemoveKeysAsync(ReadOnlyMemory<string> keys, CancellationToken cancellationToken = default) // implemented via RemoveKeyAsync
public virtual ValueTask RemoveTagAsync(string tag, CancellationToken cancellationToken = default) // implemented via RemoveTags
public abstract ValueTask RemoveTagsAsync(ReadOnlyMemory<string> tags, CancellationToken cancellationToken = default) 
```

These APIs provide for explicit manual fetch/assignment, and for explicit invalidation at the `key` or `tag` level.

---

## Backend services

To provide the enhanced capabilities, some new additional services are required; `IDistributedCache` has both performance and feature limitations that make it incomplete for this purpose. For
out-of-process caches, the `byte[]` nature of `IDistributedCache` makes for allocation concerns, so a new API is optionally supported; however, the system functions without it and all
pre-existing `IDistributedCache` implementations will continue to work. The system will type-test for the new capability:

``` c#
namespace Microsoft.Extensions.Caching.Distributed;

public interface IBufferDistributedCache : IDistributedCache
{
    ValueTask<bool> TryGetAsync(string key, IBufferWriter<byte> destination, CancellationToken cancellationToken);
    ValueTask SetAsync(string key, ReadOnlySequence<byte> value, DistributedCacheEntryOptions options, CancellationToken cancellationToken);
}
```

If the `IDistributedCache` service injected *also* implements this optional API, these buffer-based overloads will be used in preference to the `byte[]` API. We will absorb the work
to implement this API efficiently in the Redis implementation, and advice on others.

Similarly, invalidation (at the `key` and `tag` level) will be implemented via an optional auxiliary service; however this API is still in design and is not discussed here.

## Serializer configuration

By default, the system will "just work", with defaults:

- `string` will be treated as UTF-8 bytes
- `byte[]` will be treated as raw bytes
- any other type will be serialized with `System.Text.Json`, as a reasonable in-box experience

However, it is important to be able to configure other serializers. Towards this, two serialization APIs are proposed:

``` c#
namespace Microsoft.Extensions.Caching.Distributed;

public interface IHybridCacheSerializerFactory
{
    bool TryCreateSerializer<T>([NotNullWhen(true)] out IHybridCacheSerializer<T>? serializer);
}

public interface IHybridCacheSerializer<T>
{
    T Deserialize(ReadOnlySequence<byte> source);
    void Serialize(T value, IBufferWriter<byte> target);
}
```

With this API, serializers can be configured at both granular and coarse levels using the `WithSerializer` and `WithSerializerFactory` APIs at registration;
for any `T`, if a `IHybridCacheSerializer<T>` is known, it will be used as the serializer. Otherwise, the set of `IHybridCacheSerializerFactory` entries
will be enumerated; the last (i.e. most recently added/overridden) factory that returns `true` and provides a `serializer`: wins (this value may be cached),
with that `serializer` being used. This allows, for example, a protobuf-net serializer implementation to detect types marked `[ProtoContract]`, or
the use of `Newtonsoft.Json` to replace `System.Text.Json`.

## Binary payload implementation

The payload sent to `IDistributedCache` is *not* simply the raw buffer data; it also contains header metadata, to include:

- a version signifier (for safety with future data changes); in the case of `1`:
- the key
- the time (in absolute terms) that the entry was created
- the time (in absolute terms) that the entry expires
- the tags (if any) associated with the entry
- whether the payload is compressed
- payload length (for validation purposes)
- (followed by the payload)

All times are managed via `TimeProvider`. Upon fetching an entry from the cache, the expiration is compared using the current time;
expired entries are discarded as though they had not been received (this avoids a problem with time skew between in-process
and out-of-process stores, although out-of-process stores are still free to actively expire items).
Separately, the system maintains a cache of known tags and their last-invalidation-time (in absolute terms); if a cache entry has any tag that has a
last-invalidation-time *after* the creation time of the cache entry, then it is discarded as though it had not been received. This
effectively implements "tag" expiration *without* requiring that a backend is itself capable of categorized/"tagged" deletes (this feature is
not efficient or effective to implement in Redis, for example).

## Additional implementation notes and assumptions

- Valid keys and tags are always non-`null``, non-empty `string` values
- The header and payload are treated as an opaque BLOB for the purposes of `IDistributedCache`
  - Due the the header and possible compression, it should not be assumed that the value is inspectable in storage
  - The key/value will be inserted/updated/deleted as an atomic operation ("torn" values are not considered, although the payload length in the header will be verified
    with mismatches logged and the entry discarded)
  - There is no "type" metadata associated with a cache entry; the caller must know and specify (via the `<T>`) what they are requesting; if this is incorrect for
    the received data, an error may occur
- The backend store is treated as trusted, and it is assumed that any/all required authentication, encryption, etc requirements are controlled by the `IDistributedCache`
  registration, and the backend store is secure from tampering and exfiltration. Specifically: the data will not be additionally encrypted
- The backend store must be capable of servicing queries, inserts, updates and deletes
- Multi-node concurrency against the same backend store is assumed as a key scenario
- External systems might insert/update/delete against the same backend store; if data outside the expected form is encountered, it will be logged and discarded
- It is assumed that keys and tags cannot be aliased in the backend store; `foo` and `FOO` are separate; `a-b` and `a%2Db` are separate, etc; if the data retrieved
  has a non-matching `key`, it will be logged and discarded
- Keys and tags will be well-formed Unicode
- In the L1 in-process cache, the system will assume control of the `string` comparer and will apply safe logic; it will not be possible to specify a custom comparer
- It is assumed that keys and tags may contain untrusted user-provided tokens; backend implementations will use appropriate mechanisms to handle these values
- It is assumed that the backend storage will be lossless vs the stored data; payload length will be validated with mismatches logged and the entry discarded
- It is assumed that inserting / modifying / retrieving / deleting an entry in the backing store takes, at worst, amortized O((n ln n) + m)​ time,
  where n := number of chars in the key and m := number of bytes in the value
- 
