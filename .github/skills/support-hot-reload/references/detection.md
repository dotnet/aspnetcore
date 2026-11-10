# Detecting whether a feature needs hot-reload work

## Table of contents
- [The core question](#the-core-question)
- [Signals that a feature needs handling](#signals-that-a-feature-needs-handling)
- [Signals that it does not](#signals-that-it-does-not)
- [Auditing a change](#auditing-a-change)
- [Worked examples](#worked-examples)

## The core question

A feature needs hot-reload handling when it holds process-wide state derived from type or member metadata that a live source edit can change, and it reuses that state instead of recomputing it. The failure is silent: nothing throws, the app keeps serving, and the edit simply does not take effect until a restart. So the judgment has to be made by inspecting the feature's caching, not by waiting for a test to fail.

## Signals that a feature needs handling

Any one of these, combined with reuse across requests, is enough:

- A `static` or singleton cache keyed by `Type`, `MethodInfo`, `PropertyInfo`, `ParameterInfo`, `ConstructorInfo`, or `Assembly`.
- Cached reflection results: the set of properties/fields/methods of a type, custom-attribute lookups, or `[SomeAttribute]` presence/values.
- Cached compiled accessors built from metadata: expression-tree or delegate getters/setters, an `ObjectFactory`/activator, a parameter-writer, a property-injector.
- A table or map built by scanning types for an attribute: a route table, an endpoint list, a discovered-handler registry, a `[JSInvokable]` method map.
- Model or validation metadata derived from a type's members and their attributes.
- A resolved per-type decision cached as a value: "does this type have attribute X", "what render mode does this component declare", "does this participate in routing".

The common shape is `ConcurrentDictionary<Type, something-derived-from-metadata>` populated lazily and never cleared.

## Signals that it does not

- The feature reads metadata on demand and does not cache it across uses.
- Its state is per-request or per-scope, rebuilt naturally on the next request.
- It caches data that no source edit can change (configuration values, environment, external inputs).
- It is pure runtime state unrelated to type/member shape.

Startup-only logic is a special case: middleware pipeline, service registration, and route configuration run once at startup and are not re-executed by hot reload. That is not a cache to invalidate — it is an inherent limit (the edit is a rude edit or simply does not re-run), and the right response is to document it, not to wire a handler. The exception is an inline middleware delegate body, whose body edit is a normal method-body delta.

## Auditing a change

When reviewing a diff for hot-reload correctness:

1. Find every new or modified `static`/singleton field that caches something keyed by a metadata identity (the signals above).
2. For each, ask whether a source edit changes what it should hold. If yes, confirm the change also invalidates it — an assembly `[MetadataUpdateHandler]` with a `ClearCache`, or a subscription to the framework hub that clears it. If the invalidation is missing, that is the finding.
3. Confirm the derived runtime structure is also refreshed, not just the cache: a cleared route table must be rebuilt, a cleared metadata cache backing an endpoint list must fire its change token, a component cache clear must be followed by a re-render. Clearing a cache that nothing re-reads until the next natural trigger can still leave stale visible state.
4. Confirm consistency with sibling caches: if analogous caches in the same area subscribe to hot reload and this one does not, that asymmetry is almost always a bug.

## Worked examples

### Components retained state

`CascadingParameterState` in the Blazor components stack caches, per component type, the set of `[CascadingParameterAttributeBase]` properties in a static `ConcurrentDictionary<Type, CascadingParameterInfo[]>`, populated lazily and reused. It subscribes to the component Hot Reload hub and clears on a delta, matching sibling caches such as `ComponentProperties` and `DefaultComponentPropertyActivator`.

Clearing is only half of this example. Existing `ComponentState` instances retain the matches and subscriptions derived from the cache, so the renderer also refreshes those retained instances during its forced Hot Reload render. This is the pattern to look for when auditing a metadata cache: invalidate the process-wide source and refresh every longer-lived structure that copied its result.

### MVC action and model discovery

MVC's `HotReloadService` coordinates several forms of derived metadata. During the clear phase it invalidates model metadata, controller property activators, and Razor caches. During the update phase it cancels the action-descriptor change token so actions are rediscovered. This demonstrates why cache invalidation and application refresh are separate responsibilities: clearing model metadata alone would not make a newly added action reachable.
