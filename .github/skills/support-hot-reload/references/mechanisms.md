# Mechanisms for clearing and refreshing on a delta

## Table of contents
- [The metadata update handler](#the-metadata-update-handler)
- [The ASP.NET Core component hub](#the-aspnet-core-component-hub)
- [Clear, then refresh](#clear-then-refresh)
- [ASP.NET Core cache catalog](#aspnet-core-cache-catalog)

## The metadata update handler

`System.Reflection.Metadata.MetadataUpdateHandlerAttribute` is the .NET primitive. Mark an assembly with a handler type; after the runtime applies a delta it invokes the handler's static methods by convention:

```csharp
[assembly: System.Reflection.Metadata.MetadataUpdateHandler(typeof(MyFeatureHotReload))]

internal static class MyFeatureHotReload
{
    public static void ClearCache(Type[]? updatedTypes) => MyCache.Clear();
    public static void UpdateApplication(Type[]? updatedTypes) => MyChangeSource.NotifyChanged();
}
```

Rules that make it actually fire:
- The methods must be named exactly `ClearCache(Type[]?)` and/or `UpdateApplication(Type[]?)`; implement either or both. `ClearCache` is invoked before `UpdateApplication`.
- `updatedTypes` is the set of changed types, or `null` meaning "a change happened but the specific types are unknown — invalidate everything". Handle the `null` case by clearing fully.
- The handler type is referenced only by the attribute, so keep it from being trimmed; a static type in the same assembly as the cache is the normal placement.
- Guard against doing work when hot reload is not active if the clear is expensive; the handler is only called under a delta, but the registration itself should be cheap.

Use this for standalone libraries and any non-component feature that owns a metadata-derived cache.

## The ASP.NET Core component hub

Inside the Blazor components stack the framework already receives the delta and re-broadcasts it, so component-layer caches do not each need an assembly attribute. `HotReloadManager` is itself the `[MetadataUpdateHandler]`; its `UpdateApplication` fires an `OnDeltaApplied` event. A cache subscribes in its static constructor:

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

`HotReloadManager.IsSupported` gates the subscription so nothing is wired when hot reload is unavailable. The renderer subscribes too and, on a delta, clears the core component caches and force-re-renders every root component so the whole tree reflects the edit. Prefer the hub for anything in the components layer; it keeps all component caches on one coherent signal.

## Clear, then refresh

Clearing a cache is only half the job. The edit becomes visible only when something re-reads the metadata, so pair the clear with the refresh of whatever was derived from it:

- A route/endpoint table: rebuild it (Blazor's router clears `RouteTableFactory` and rebuilds on the next refresh; SSR endpoints refresh by cancelling a `CancellationTokenSource` that backs an `IChangeToken` the endpoint data source listens to).
- MVC action/model state: the MVC hot-reload service cancels a token that drives `ActionDescriptorCollection` to rebuild, and clears model-metadata/view/tag-helper caches.
- Rendered UI: after clearing component caches, re-render (the renderer sets a flag that bypasses `ShouldRender` once so every component refreshes).

If nothing re-reads the cleared cache until an unrelated later trigger, the visible state stays stale even though the cache is technically empty — so make the refresh explicit.

## ASP.NET Core cache catalog

The framework's own hot-reload-aware caches, as concrete models. Each lists the metadata it holds and the source edit that must invalidate it.

Blazor / components (cleared via the hub or the renderer):
- `ComponentProperties._cachedWritersByType` — `[Parameter]`/`[CascadingParameter]` setter writers per component type. Edit: add a `[Parameter]`.
- `CascadingParameterState._cachedInfos` — `[CascadingParameterAttributeBase]` metadata per component type. It clears through `HotReloadManager`; the renderer then refreshes retained `ComponentState` matches and supplier subscriptions. Edit: add or change `[CascadingParameter]` or `[PersistentState]`.
- `DefaultComponentPropertyActivator._cachedPropertyActivators` — compiled `[Inject]` property injectors. Edit: add an `[Inject]` property.
- `ComponentFactory._cachedComponentTypeRenderModes` — the declared `@rendermode` per component. Edit: add/change `@rendermode`.
- `DefaultComponentActivator._cachedComponentTypeInfo` — constructor `ObjectFactory` per component. Edit: change a constructor.
- `RouteTableFactory._cache` (+ `Router`) — the route table keyed by the assembly set. Edit: add/change an `@page` route.
- `EndpointComponentState._streamRenderingAttributeByComponentType` — `[StreamRendering]` per component (also an assembly `[MetadataUpdateHandler]`). Edit: add `[StreamRendering]`.
- `RazorComponentsEndpointHttpContextExtensions` `AcceptsInteractiveRoutingCache` — `[ExcludeFromInteractiveRouting]` per page. Edit: add that attribute.
- `PersistentValueProviderComponentSubscription` — `[PersistentState]` property getters and serializers. Edit: add `[PersistentState]`.
- `EditContextDataAnnotationsExtensions` `_propertyInfoCache` — validated model `PropertyInfo` per `(modelType, field)`. Edit: add a validation attribute to an `EditForm` model.
- `DotNetDispatcher` method caches — `[JSInvokable]` methods per type/assembly. Edit: add a `[JSInvokable]` method.
- Endpoints `HotReloadService` — an `IChangeToken` source that rebuilds the SSR endpoint list. Edit: add an `@page` or change `@rendermode` on an SSR component.

Shared reflection (assembly `[MetadataUpdateHandler]`):
- `PropertyHelper` `PropertiesCache`/`VisiblePropertiesCache` — public/visible properties per type, the base used by model binding and metadata. Edit: add a public property to a bound model.
