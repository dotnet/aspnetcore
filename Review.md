###

src/Components/Components/src/PersistentComponentState.cs

    public RestoringComponentStateSubscription RegisterOnRestoring(IPersistentStateFilter? filter, Action callback)

Reorder parameters, action first, filter later.

Pass in multiple filters? (and create a composite filter)

params Span<IPersistentComponentStateFilter>

###

src/Components/Components/src/PersistentState/ComponentStatePersistenceManager.cs

    public async Task RestoreStateAsync(IPersistentComponentStateStore store, IPersistentComponentStateScenario? scenario)

Make scenario required (can't pass null)

Remove extra line at the end. (307)

###

src/Components/Components/src/PersistentState/RestoreStateOnPrerenderingAttribute.cs
src/Components/Components/src/PersistentState/RestoreStateOnReconnectionAttribute.cs
src/Components/Components/src/PersistentState/UpdateStateOnEnhancedNavigation.cs
src/Components/Components/src/PersistentState/WebPersistenceFilter.cs
src/Components/Components/src/PersistentState/WebPersistenceScenario.cs

Move these types to src/Components/Web/src/Restore/.

###

src/Components/Components/src/PersistentStateValueProvider.cs

Move CompositeScenarioFilter to its own file.

Move CreateFilter inside ComponentSubscription

Move the Log class to ComponentSubscription.

Move ComponentSubscription into its own file.

Move ResolvePropertyGetter SerializerFactory and PropertyGetterFactory into ComponentSubscription.

Move _serializerCache, _propertyGetterCache and _keyCache into ComponentSubscription.

Update the `ComponentSubscription` constructor to take in the "source" values and compute the property getter, the custom serializer, and so on inside the constructor.

