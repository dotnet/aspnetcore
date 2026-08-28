---
name: support-hot-reload
description: "Make an ASP.NET Core feature work correctly under .NET Hot Reload, and decide whether a feature needs hot-reload work at all. USE FOR: reviewing or writing a feature that caches type/member metadata (reflection results, compiled accessors, attribute lookups, route/endpoint tables, DI activators) and must stay correct when source is edited live; adding a MetadataUpdateHandler / wiring into HotReloadManager to clear caches and refresh on a delta; auditing a PR for a missing hot-reload cache invalidation; understanding what edits Hot Reload supports vs rude edits, and CoreCLR-vs-WebAssembly differences. Triggers on \"does this need hot reload support\", \"add hot reload support\", \"MetadataUpdateHandler\", \"cache not cleared on hot reload\", \"why doesn't my edit apply\". DO NOT USE FOR: writing hot-reload END-TO-END tests as the primary goal, general Blazor authoring, or diagnosing a build failure unrelated to live edits."
---

# Support hot reload

Decide whether a feature needs hot-reload work, and if so, make it correct: when a developer edits source in a running app, any process-wide state the feature derived from the old metadata must be invalidated so the edit takes effect without a restart. Getting this wrong is silent — the app keeps running with stale cached metadata, and the edit only appears after a manual restart, so it is easy to ship a feature that quietly does not hot-reload.

## Does this feature need hot-reload work?

A feature needs hot-reload handling when it holds **process-wide state derived from type or member metadata that a source edit can change**. Ask, in order:

1. Does it keep a `static` (or singleton) cache keyed by `Type`, `MethodInfo`, `PropertyInfo`, or an assembly — holding reflection results, compiled getters/setters/factories, attribute lookups, a route/endpoint table, or model metadata?
2. Is that cache populated once and reused for the life of the process?
3. Would a plausible source edit change what the cache should contain — adding/removing a `[Parameter]`/`[Inject]`/`[Route]`/validation attribute, a member, a route, a render mode, a `[JSInvokable]` method?

If all three are yes, the feature needs hot-reload handling: the cache must be invalidated on a metadata delta, and any derived runtime structure (a rendered tree, a route/endpoint table, a change token) must be refreshed. If the feature only reads metadata on demand without caching, or its state is per-request, it usually needs nothing.

The trap is a cache added for performance with no hot-reload wiring. It works in every test that starts fresh and only fails when someone edits the relevant declaration live — see [references/detection.md](references/detection.md) for the full signal list and how to audit a change for a missing invalidation.

## Adding hot-reload support

Two mechanisms; pick by whether the runtime hands you the change directly or you piggyback on a framework hub.

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

### The ASP.NET Core component hub
Inside the Blazor components stack, the framework already receives the delta and fans it out through `HotReloadManager`: its `UpdateApplication` fires `OnDeltaApplied`. A component-layer cache subscribes and clears itself:

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

This is the pattern the framework's own per-type component caches use (`ComponentProperties`, `DefaultComponentPropertyActivator`, `ComponentFactory`, `DefaultComponentActivator`), and the renderer additionally force-re-renders the root on a delta. Use the hub inside that stack; use the assembly attribute for standalone libraries and non-component features. Details and the ASP.NET Core cache catalog: [references/mechanisms.md](references/mechanisms.md).

### Refresh retained runtime state

Clearing the process-wide cache is insufficient when existing runtime objects copied or derived state from it. Identify where the cached result was materialized and make the Hot Reload refresh recompute that state for retained instances.

If recomputation rebuilds subscriptions, distinguish initial subscription from metadata refresh on the supplier abstraction. The supplier owns replay, destructive reads, and preservation of current values; the generic renderer or coordinator must not branch on concrete supplier types.

### Completion check
Prove the invalidation works by editing the relevant declaration in a running app under `dotnet watch` and confirming the new behavior appears **without a restart** — asserted on the watch log ("changes applied", no restart), not just on the eventual output, because a rude edit restarts the app and makes a stale cache look fixed. How to validate: [references/validation.md](references/validation.md).

Permanent regression coverage and live-edit validation serve different purposes:

- Use focused component or unit tests to deterministically cover cache clearing, retained-state recomputation, and subscription lifecycle.
- Use `dotnet watch` plus a browser to prove the actual metadata delta applies without restart.
- Add E2E coverage only when the existing harness applies the real delta. Do not seed private caches or add test-only endpoints to imitate it.

## What edits are even possible

Not every edit can hot-reload; some are "rude edits" that force a restart regardless of your cache handling. Design and test against the real capability set: method/lambda/Razor bodies, adding methods/fields/types/routes/attributes are supported; renaming or deleting a field, changing a field's type, changing a signature in unsupported ways, changing a `const`, and editing startup are rude. Added fields are not re-initialized on existing instances. WebAssembly/Mono supports fewer edits than CoreCLR. The full supported-vs-rude tables, the field-initialization semantics, and the runtime differences: [references/edit-limits.md](references/edit-limits.md).

## References

- [references/detection.md](references/detection.md) — the full signal list for "does this need hot-reload work", and how to audit a diff for a missing cache invalidation.
- [references/mechanisms.md](references/mechanisms.md) — the metadata update handler and the `HotReloadManager` hub in depth, the refresh step (change tokens, re-render, endpoint rebuild), and the ASP.NET Core cache catalog with the edit that exercises each.
- [references/edit-limits.md](references/edit-limits.md) — supported vs rude edits with examples, added-field initialization semantics, and CoreCLR vs Blazor WebAssembly capability differences.
- [references/validation.md](references/validation.md) — validating an invalidation works: driving `dotnet watch`, asserting on the watch log, and proving no restart occurred.
