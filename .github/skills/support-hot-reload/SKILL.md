---
name: support-hot-reload
description: "Make an ASP.NET Core feature work correctly under .NET Hot Reload, and decide whether a feature needs hot-reload work at all. USE FOR: reviewing or writing a feature that caches type/member metadata (reflection results, compiled accessors, attribute lookups, route/endpoint tables, model metadata, DI activators) and must stay correct when source is edited live; adding a MetadataUpdateHandler or integrating with an existing subsystem refresh mechanism; auditing a PR for missing cache invalidation or derived-state refresh; understanding supported versus rude edits and runtime capability differences. Triggers on \"does this need hot reload support\", \"add hot reload support\", \"MetadataUpdateHandler\", \"cache not cleared on hot reload\", \"why doesn't my edit apply\". DO NOT USE FOR: writing end-to-end tests as the primary goal, general application authoring, or diagnosing a build failure unrelated to live edits."
---

# Support hot reload

Decide whether a feature needs hot-reload work, and if so, make it correct: when a developer edits source in a running app, any process-wide state the feature derived from the old metadata must be invalidated so the edit takes effect without a restart. Getting this wrong is silent — the app keeps running with stale cached metadata, and the edit only appears after a manual restart, so it is easy to ship a feature that quietly does not hot-reload.

## Does this feature need hot-reload work?

A feature needs hot-reload handling when it holds **process-wide state derived from type or member metadata that a source edit can change**. Ask, in order:

1. Does it keep a `static` (or singleton) cache keyed by `Type`, `MethodInfo`, `PropertyInfo`, or an assembly — holding reflection results, compiled getters/setters/factories, attribute lookups, a route/endpoint table, or model metadata?
2. Is that cache populated once and reused for the life of the process?
3. Would a plausible source edit change what the cache should contain — adding or changing a member, controller action, endpoint, route, model property, validation attribute, component parameter, render mode, or invokable method?

If all three are yes, the feature needs hot-reload handling: the cache must be invalidated on a metadata delta, and any derived runtime structure (a rendered tree, a route/endpoint table, a change token) must be refreshed. If the feature only reads metadata on demand without caching, or its state is per-request, it usually needs nothing.

The trap is a cache added for performance with no hot-reload wiring. It works in every test that starts fresh and only fails when someone edits the relevant declaration live — see [references/detection.md](references/detection.md) for the full signal list and how to audit a change for a missing invalidation.

## Adding hot-reload support

Use the runtime metadata-update handler by default. Reuse an existing subsystem refresh mechanism when the owning subsystem already centralizes Hot Reload notifications and derived-state refresh.

### The metadata update handler (the .NET primitive)
Mark an assembly with a handler type and implement the static methods the runtime calls after applying a delta:

```csharp
[assembly: System.Reflection.Metadata.MetadataUpdateHandler(typeof(MyFeatureHotReload))]

internal static class MyFeatureHotReload
{
    // Clear caches first. `updatedTypes` is the changed types, or null = clear everything.
    public static void ClearCache(Type[]? updatedTypes) => MyCache.Clear();

    // Then refresh anything derived from the cache (rebuild a table, fire a change token, re-render).
    public static void UpdateApplication(Type[]? updatedTypes) => MyChangeSource.NotifyChanged();
}
```

`ClearCache` runs before `UpdateApplication`; implement whichever you need. Keep the handler type discoverable (a public or internal static type in the same assembly) and the methods exactly `ClearCache(Type[]?)` / `UpdateApplication(Type[]?)`.

### Subsystem-specific refresh mechanisms

Some ASP.NET Core subsystems already receive the runtime delta and coordinate cache clearing with a broader refresh. Reuse that mechanism instead of adding a second independent handler.

For example, the Components subsystem fans deltas out through `HotReloadManager`: its `UpdateApplication` fires `OnDeltaApplied`, and component-layer caches subscribe and clear themselves:

```csharp
static MyComponentCache()
{
    if (HotReloadManager.IsSupported)
    {
        HotReloadManager.Default.OnDeltaApplied += ClearCache;
    }
}

public static void ClearCache() => _cache.Clear();
```

This is the pattern the framework's own per-type component caches use (`ComponentProperties`, `DefaultComponentPropertyActivator`, `ComponentFactory`, `DefaultComponentActivator`), and the renderer additionally force-renders roots on a delta. MVC uses its Hot Reload service to clear model/controller/Razor caches and signal action-descriptor changes. Details and the ASP.NET Core cache catalog: [references/mechanisms.md](references/mechanisms.md).

### Refresh retained runtime state

Clearing the process-wide cache is insufficient when existing runtime objects copied or derived state from it. Identify where the cached result was materialized and make the Hot Reload refresh recompute that state for retained instances.

If recomputation rebuilds subscriptions, distinguish initial subscription from metadata refresh on the supplier abstraction. The supplier owns replay, destructive reads, and preservation of current values; the generic renderer or coordinator must not branch on concrete supplier types.

### Completion check
Prove the invalidation works by editing the relevant declaration in a running app under `dotnet watch` and confirming the new behavior appears **without a restart** — asserted on the watch log ("changes applied", no restart), not just on the eventual output, because a rude edit restarts the app and makes a stale cache look fixed. How to validate: [references/validation.md](references/validation.md).

Permanent regression coverage and live-edit validation serve different purposes:

- Use focused unit or integration tests to deterministically cover cache clearing, derived-state recomputation, and lifecycle behavior.
- Use `dotnet watch` plus the appropriate observable boundary—HTTP response, endpoint/action discovery, rendered output, logs, or browser UI—to prove the actual metadata delta applies without restart.
- Add E2E coverage only when the existing harness applies the real delta. Do not seed private caches or add test-only endpoints to imitate it.

## What edits are even possible

Not every edit can hot-reload; some are "rude edits" that force a restart regardless of cache handling. Design and test against the target runtime's real capability set. Added methods, fields, types, and custom attributes are commonly supported; renaming or deleting fields, changing field types, changing constants, and editing startup behavior generally require a restart. Added fields are not initialized on existing instances, and runtime capability sets can differ. See [references/edit-limits.md](references/edit-limits.md) for the detailed tables.

## References

- [references/detection.md](references/detection.md) — the full signal list for "does this need hot-reload work", and how to audit a diff for a missing cache invalidation.
- [references/mechanisms.md](references/mechanisms.md) — the metadata update handler, subsystem refresh mechanisms, the refresh step (change tokens, re-render, endpoint rebuild), and the ASP.NET Core cache catalog with the edit that exercises each.
- [references/edit-limits.md](references/edit-limits.md) — supported vs rude edits with examples, added-field initialization semantics, and CoreCLR vs Blazor WebAssembly capability differences.
- [references/validation.md](references/validation.md) — validating an invalidation works: driving `dotnet watch`, asserting on the watch log, and proving no restart occurred.
