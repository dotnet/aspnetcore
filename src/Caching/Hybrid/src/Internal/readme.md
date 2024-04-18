# HybridCache internal design

`HybridCache` encapsulates serialization, caching and stampede protection.

The `DefaultHybridCache` implementation keeps a collection of `StampedeState` entries
that represent the current in-flight operations (keyed by `StampedeKey`); if a duplicate
operation occurs during the execution, the second operation will be joined with that
same flow, rather than executing independently. When attempting to merge with an
existing flow, interlocked counting is used: we can only join if we can successfully
increment the value from a non-zero value (zero meaning all existing consumers have
canceled, and the shared token is therefore canceled)

The `StampedeState<>` performs back-end fetch operations, resulting not in a `T` (of the final
value), but instead a `CacheItem<T>`; this is the object that gets put into L1 cache,
and can describe both mutable and immutable types; the significance here is that for
mutable types, we need a defensive copy per-call to prevent callers impacting each-other.

`StampedeState<>` combines cancellation (so that operations proceed as long as *a* caller
is still active); this covers all L2 access and serialization operations, releasing all pending
shared callers for the same operation. Note that L2 storage can occur *after* callers
have been released.

To ensure correct buffer recycling, when dealing with cache entries that need defensive copies
we use more ref-counting while reading the buffer, combined with an eviction callback which
decrements that counter. This means that we recycle things when evicted, without impacting
in-progress deserialize operations. To simplify tracking, `BufferChunk` acts like a `byte[]`+`int`
(we don't need non-zero offset), but also tracking "should this be returned to the pool?".
