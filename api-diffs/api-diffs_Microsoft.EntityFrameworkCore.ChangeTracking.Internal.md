# Microsoft.EntityFrameworkCore.ChangeTracking.Internal

``` diff
-namespace Microsoft.EntityFrameworkCore.ChangeTracking.Internal {
 {
-    public class ArrayPropertyValues : PropertyValues {
 {
-        public ArrayPropertyValues(InternalEntityEntry internalEntry, object[] values);

-        public override IReadOnlyList<IProperty> Properties { get; }

-        public override object this[IProperty property] { get; set; }

-        public override object this[string propertyName] { get; set; }

-        public override PropertyValues Clone();

-        public override TValue GetValue<TValue>(IProperty property);

-        public override TValue GetValue<TValue>(string propertyName);

-        public override void SetValues(PropertyValues propertyValues);

-        public override void SetValues(object obj);

-        public override object ToObject();

-    }
-    public class ChangeDetector : IChangeDetector, IPropertyListener {
 {
-        public const string SkipDetectChangesAnnotation = "ChangeDetector.SkipDetectChanges";

-        public ChangeDetector(IDiagnosticsLogger<DbLoggerCategory.ChangeTracking> logger, ILoggingOptions loggingOptions);

-        public virtual void DetectChanges(InternalEntityEntry entry);

-        public virtual void DetectChanges(IStateManager stateManager);

-        public virtual void PropertyChanged(InternalEntityEntry entry, IPropertyBase propertyBase, bool setModified);

-        public virtual void PropertyChanging(InternalEntityEntry entry, IPropertyBase propertyBase);

-        public virtual void Resume();

-        public virtual void Suspend();

-    }
-    public class ChangeTrackerFactory : IChangeTrackerFactory {
 {
-        public ChangeTrackerFactory(ICurrentDbContext currentContext, IStateManager stateManager, IChangeDetector changeDetector, IModel model, IEntityEntryGraphIterator graphIterator);

-        public virtual ChangeTracker Create();

-    }
-    public class CompositeNullableValueFactory : CompositeValueFactory, IDependentKeyValueFactory<object[]>, INullableValueFactory<object[]> {
 {
-        public CompositeNullableValueFactory(IReadOnlyList<IProperty> properties);

-        public virtual IEqualityComparer<object[]> EqualityComparer { get; }

-    }
-    public class CompositePrincipalKeyValueFactory : CompositeValueFactory, IPrincipalKeyValueFactory<object[]> {
 {
-        public CompositePrincipalKeyValueFactory(IKey key);

-        public virtual IEqualityComparer<object[]> EqualityComparer { get; }

-        public virtual object CreateFromBuffer(ValueBuffer valueBuffer);

-        public virtual object[] CreateFromCurrentValues(InternalEntityEntry entry);

-        public virtual object CreateFromKeyValues(object[] keyValues);

-        public virtual object[] CreateFromOriginalValues(InternalEntityEntry entry);

-        public virtual object[] CreateFromRelationshipSnapshot(InternalEntityEntry entry);

-        public virtual IProperty FindNullPropertyInCurrentValues(InternalEntityEntry entry);

-        public virtual IProperty FindNullPropertyInValueBuffer(ValueBuffer valueBuffer);

-    }
-    public class CompositeValueFactory : IDependentKeyValueFactory<object[]> {
 {
-        public CompositeValueFactory(IReadOnlyList<IProperty> properties);

-        protected virtual IReadOnlyList<IProperty> Properties { get; }

-        protected static IEqualityComparer<object[]> CreateEqualityComparer(IReadOnlyList<IProperty> properties);

-        public virtual bool TryCreateFromBuffer(in ValueBuffer valueBuffer, out object[] key);

-        public virtual bool TryCreateFromCurrentValues(InternalEntityEntry entry, out object[] key);

-        protected virtual bool TryCreateFromEntry(InternalEntityEntry entry, Func<InternalEntityEntry, IProperty, object> getValue, out object[] key);

-        public virtual bool TryCreateFromOriginalValues(InternalEntityEntry entry, out object[] key);

-        public virtual bool TryCreateFromPreStoreGeneratedCurrentValues(InternalEntityEntry entry, out object[] key);

-        public virtual bool TryCreateFromRelationshipSnapshot(InternalEntityEntry entry, out object[] key);

-    }
-    public class CurrentPropertyValues : EntryPropertyValues {
 {
-        public CurrentPropertyValues(InternalEntityEntry internalEntry);

-        public override TValue GetValue<TValue>(IProperty property);

-        public override TValue GetValue<TValue>(string propertyName);

-        protected override object GetValueInternal(IProperty property);

-        protected override void SetValueInternal(IProperty property, object value);

-    }
-    public class DependentKeyValueFactoryFactory {
 {
-        public DependentKeyValueFactoryFactory();

-        public virtual IDependentKeyValueFactory<TKey> Create<TKey>(IForeignKey foreignKey);

-        public virtual IDependentKeyValueFactory<object[]> CreateComposite(IForeignKey foreignKey);

-        public virtual IDependentKeyValueFactory<TKey> CreateSimple<TKey>(IForeignKey foreignKey);

-    }
-    public class DependentsMap<TKey> : IDependentsMap {
 {
-        public DependentsMap(IForeignKey foreignKey, IPrincipalKeyValueFactory<TKey> principalKeyValueFactory, IDependentKeyValueFactory<TKey> dependentKeyValueFactory);

-        public virtual void Add(InternalEntityEntry entry);

-        public virtual IEnumerable<InternalEntityEntry> GetDependents(InternalEntityEntry principalEntry);

-        public virtual IEnumerable<InternalEntityEntry> GetDependentsUsingRelationshipSnapshot(InternalEntityEntry principalEntry);

-        public virtual void Remove(InternalEntityEntry entry);

-        public virtual void Update(InternalEntityEntry entry);

-    }
-    public class DependentsMapFactoryFactory : IdentityMapFactoryFactoryBase {
 {
-        public DependentsMapFactoryFactory();

-        public virtual Func<IDependentsMap> Create(IForeignKey foreignKey);

-    }
-    public class EmptyShadowValuesFactoryFactory : SnapshotFactoryFactory {
 {
-        public EmptyShadowValuesFactoryFactory();

-        protected override bool UseEntityVariable { get; }

-        protected override Expression CreateReadShadowValueExpression(ParameterExpression parameter, IPropertyBase property);

-        protected override int GetPropertyCount(IEntityType entityType);

-        protected override int GetPropertyIndex(IPropertyBase propertyBase);

-        protected override ValueComparer GetValueComparer(IProperty property);

-    }
-    public class EntityEntryGraphIterator : IEntityEntryGraphIterator {
 {
-        public EntityEntryGraphIterator();

-        public virtual void TraverseGraph<TState>(EntityEntryGraphNode node, TState state, Func<EntityEntryGraphNode, TState, bool> handleNode);

-        public virtual Task TraverseGraphAsync<TState>(EntityEntryGraphNode node, TState state, Func<EntityEntryGraphNode, TState, CancellationToken, Task<bool>> handleNode, CancellationToken cancellationToken = default(CancellationToken));

-    }
-    public class EntityGraphAttacher : IEntityGraphAttacher {
 {
-        public EntityGraphAttacher(IEntityEntryGraphIterator graphIterator);

-        public virtual void AttachGraph(InternalEntityEntry rootEntry, EntityState entityState, bool forceStateWhenUnknownKey);

-        public virtual Task AttachGraphAsync(InternalEntityEntry rootEntry, EntityState entityState, bool forceStateWhenUnknownKey, CancellationToken cancellationToken = default(CancellationToken));

-    }
-    public abstract class EntryPropertyValues : PropertyValues {
 {
-        protected EntryPropertyValues(InternalEntityEntry internalEntry);

-        public override IReadOnlyList<IProperty> Properties { get; }

-        public override object this[IProperty property] { get; set; }

-        public override object this[string propertyName] { get; set; }

-        public override PropertyValues Clone();

-        protected abstract object GetValueInternal(IProperty property);

-        protected abstract void SetValueInternal(IProperty property, object value);

-        public override void SetValues(PropertyValues propertyValues);

-        public override void SetValues(object obj);

-        public override object ToObject();

-    }
-    public interface IChangeDetector : IPropertyListener {
 {
-        void DetectChanges(InternalEntityEntry entry);

-        void DetectChanges(IStateManager stateManager);

-        void Resume();

-        void Suspend();

-    }
-    public interface IChangeTrackerFactory {
 {
-        ChangeTracker Create();

-    }
-    public class IdentityMap<TKey> : IIdentityMap {
 {
-        public IdentityMap(IKey key, IPrincipalKeyValueFactory<TKey> principalKeyValueFactory, bool sensitiveLoggingEnabled);

-        public virtual IKey Key { get; }

-        protected virtual IPrincipalKeyValueFactory<TKey> PrincipalKeyValueFactory { get; }

-        public virtual void Add(InternalEntityEntry entry);

-        public virtual void Add(object[] keyValues, InternalEntityEntry entry);

-        protected virtual void Add(TKey key, InternalEntityEntry entry);

-        public virtual void AddOrUpdate(InternalEntityEntry entry);

-        public virtual void Clear();

-        public virtual bool Contains(IForeignKey foreignKey, in ValueBuffer valueBuffer);

-        public virtual bool Contains(in ValueBuffer valueBuffer);

-        public virtual IDependentsMap FindDependentsMap(IForeignKey foreignKey);

-        public virtual IDependentsMap GetDependentsMap(IForeignKey foreignKey);

-        public virtual void Remove(InternalEntityEntry entry);

-        protected virtual void Remove(TKey key, InternalEntityEntry entry);

-        public virtual void RemoveUsingRelationshipSnapshot(InternalEntityEntry entry);

-        public virtual InternalEntityEntry TryGetEntry(IForeignKey foreignKey, InternalEntityEntry dependentEntry);

-        public virtual InternalEntityEntry TryGetEntry(in ValueBuffer valueBuffer, bool throwOnNullKey);

-        public virtual InternalEntityEntry TryGetEntry(object[] keyValues);

-        public virtual InternalEntityEntry TryGetEntryUsingPreStoreGeneratedValues(IForeignKey foreignKey, InternalEntityEntry dependentEntry);

-        public virtual InternalEntityEntry TryGetEntryUsingRelationshipSnapshot(IForeignKey foreignKey, InternalEntityEntry dependentEntry);

-    }
-    public class IdentityMapFactoryFactory : IdentityMapFactoryFactoryBase {
 {
-        public IdentityMapFactoryFactory();

-        public virtual Func<bool, IIdentityMap> Create(IKey key);

-    }
-    public abstract class IdentityMapFactoryFactoryBase {
 {
-        protected IdentityMapFactoryFactoryBase();

-        protected virtual Type GetKeyType(IKey key);

-    }
-    public interface IDependentKeyValueFactory<TKey> {
 {
-        bool TryCreateFromBuffer(in ValueBuffer valueBuffer, out TKey key);

-        bool TryCreateFromCurrentValues(InternalEntityEntry entry, out TKey key);

-        bool TryCreateFromOriginalValues(InternalEntityEntry entry, out TKey key);

-        bool TryCreateFromPreStoreGeneratedCurrentValues(InternalEntityEntry entry, out TKey key);

-        bool TryCreateFromRelationshipSnapshot(InternalEntityEntry entry, out TKey key);

-    }
-    public interface IDependentsMap {
 {
-        void Add(InternalEntityEntry entry);

-        IEnumerable<InternalEntityEntry> GetDependents(InternalEntityEntry principalEntry);

-        IEnumerable<InternalEntityEntry> GetDependentsUsingRelationshipSnapshot(InternalEntityEntry principalEntry);

-        void Remove(InternalEntityEntry entry);

-        void Update(InternalEntityEntry entry);

-    }
-    public interface IEntityGraphAttacher {
 {
-        void AttachGraph(InternalEntityEntry rootEntry, EntityState entityState, bool forceStateWhenUnknownKey);

-        Task AttachGraphAsync(InternalEntityEntry rootEntry, EntityState entityState, bool forceStateWhenUnknownKey, CancellationToken cancellationToken = default(CancellationToken));

-    }
-    public interface IEntityStateListener {
 {
-        void StateChanged(InternalEntityEntry entry, EntityState oldState, bool fromQuery);

-        void StateChanging(InternalEntityEntry entry, EntityState newState);

-    }
-    public interface IIdentityMap {
 {
-        IKey Key { get; }

-        void Add(InternalEntityEntry entry);

-        void Add(object[] keyValues, InternalEntityEntry entry);

-        void AddOrUpdate(InternalEntityEntry entry);

-        void Clear();

-        bool Contains(IForeignKey foreignKey, in ValueBuffer valueBuffer);

-        bool Contains(in ValueBuffer valueBuffer);

-        IDependentsMap FindDependentsMap(IForeignKey foreignKey);

-        IDependentsMap GetDependentsMap(IForeignKey foreignKey);

-        void Remove(InternalEntityEntry entry);

-        void RemoveUsingRelationshipSnapshot(InternalEntityEntry entry);

-        InternalEntityEntry TryGetEntry(IForeignKey foreignKey, InternalEntityEntry dependentEntry);

-        InternalEntityEntry TryGetEntry(in ValueBuffer valueBuffer, bool throwOnNullKey);

-        InternalEntityEntry TryGetEntry(object[] keyValues);

-        InternalEntityEntry TryGetEntryUsingPreStoreGeneratedValues(IForeignKey foreignKey, InternalEntityEntry dependentEntry);

-        InternalEntityEntry TryGetEntryUsingRelationshipSnapshot(IForeignKey foreignKey, InternalEntityEntry dependentEntry);

-    }
-    public interface IInternalEntityEntryFactory {
 {
-        InternalEntityEntry Create(IStateManager stateManager, IEntityType entityType, object entity);

-        InternalEntityEntry Create(IStateManager stateManager, IEntityType entityType, object entity, in ValueBuffer valueBuffer);

-    }
-    public interface IInternalEntityEntryNotifier {
 {
-        void KeyPropertyChanged(InternalEntityEntry entry, IProperty property, IReadOnlyList<IKey> keys, IReadOnlyList<IForeignKey> foreignKeys, object oldValue, object newValue);

-        void NavigationCollectionChanged(InternalEntityEntry entry, INavigation navigation, IEnumerable<object> added, IEnumerable<object> removed);

-        void NavigationReferenceChanged(InternalEntityEntry entry, INavigation navigation, object oldValue, object newValue);

-        void PropertyChanged(InternalEntityEntry entry, IPropertyBase property, bool setModified);

-        void PropertyChanging(InternalEntityEntry entry, IPropertyBase property);

-        void StateChanged(InternalEntityEntry entry, EntityState oldState, bool fromQuery);

-        void StateChanging(InternalEntityEntry entry, EntityState newState);

-        void TrackedFromQuery(InternalEntityEntry entry, ISet<IForeignKey> handledForeignKeys);

-    }
-    public interface IInternalEntityEntrySubscriber {
 {
-        bool SnapshotAndSubscribe(InternalEntityEntry entry);

-        void Unsubscribe(InternalEntityEntry entry);

-    }
-    public interface IKeyListener {
 {
-        void KeyPropertyChanged(InternalEntityEntry entry, IProperty property, IReadOnlyList<IKey> containingPrincipalKeys, IReadOnlyList<IForeignKey> containingForeignKeys, object oldValue, object newValue);

-    }
-    public interface IKeyPropagator {
 {
-        InternalEntityEntry PropagateValue(InternalEntityEntry entry, IProperty property);

-        Task<InternalEntityEntry> PropagateValueAsync(InternalEntityEntry entry, IProperty property, CancellationToken cancellationToken = default(CancellationToken));

-    }
-    public interface ILocalViewListener : IEntityStateListener {
 {
-        void RegisterView(Action<InternalEntityEntry, EntityState> viewAction);

-    }
-    public interface INavigationFixer : IEntityStateListener, IKeyListener, INavigationListener, IQueryTrackingListener

-    public interface INavigationListener {
 {
-        void NavigationCollectionChanged(InternalEntityEntry entry, INavigation navigation, IEnumerable<object> added, IEnumerable<object> removed);

-        void NavigationReferenceChanged(InternalEntityEntry entry, INavigation navigation, object oldValue, object newValue);

-    }
-    public class InternalClrEntityEntry : InternalEntityEntry {
 {
-        public InternalClrEntityEntry(IStateManager stateManager, IEntityType entityType, object entity);

-        public override object Entity { get; }

-    }
-    public abstract class InternalEntityEntry : IUpdateEntry {
 {
-        protected InternalEntityEntry(IStateManager stateManager, IEntityType entityType);

-        public abstract object Entity { get; }

-        public virtual EntityState EntityState { get; }

-        public virtual IEntityType EntityType { get; }

-        public virtual bool HasConceptualNull { get; }

-        public virtual bool HasOriginalValuesSnapshot { get; }

-        public virtual bool HasRelationshipSnapshot { get; }

-        public virtual bool IsKeySet { get; }

-        public virtual bool IsKeyUnknown { get; }

-        IUpdateEntry Microsoft.EntityFrameworkCore.Update.IUpdateEntry.SharedIdentityEntry { get; }

-        public virtual InternalEntityEntry SharedIdentityEntry { get; set; }

-        public virtual IStateManager StateManager { get; }

-        public object this[IPropertyBase propertyBase] { get; set; }

-        public virtual void AcceptChanges();

-        public virtual void AddRangeToCollectionSnapshot(IPropertyBase propertyBase, IEnumerable<object> addedEntities);

-        public virtual bool AddToCollection(INavigation navigation, InternalEntityEntry value);

-        public virtual void AddToCollectionSnapshot(IPropertyBase propertyBase, object addedEntity);

-        public virtual bool CollectionContains(INavigation navigation, InternalEntityEntry value);

-        public virtual void DiscardStoreGeneratedValues();

-        public virtual void EnsureOriginalValues();

-        public virtual void EnsureRelationshipSnapshot();

-        public virtual object GetCurrentValue(IPropertyBase propertyBase);

-        public virtual TProperty GetCurrentValue<TProperty>(IPropertyBase propertyBase);

-        public virtual object GetOrCreateCollection(INavigation navigation);

-        public virtual object GetOriginalValue(IPropertyBase propertyBase);

-        public virtual TProperty GetOriginalValue<TProperty>(IProperty property);

-        public virtual object GetPreStoreGeneratedCurrentValue(IPropertyBase propertyBase);

-        public virtual object GetRelationshipSnapshotValue(IPropertyBase propertyBase);

-        public virtual TProperty GetRelationshipSnapshotValue<TProperty>(IPropertyBase propertyBase);

-        public virtual void HandleConceptualNulls(bool sensitiveLoggingEnabled);

-        public virtual void HandleINotifyCollectionChanged(object sender, NotifyCollectionChangedEventArgs eventArgs);

-        public virtual void HandleINotifyPropertyChanged(object sender, PropertyChangedEventArgs eventArgs);

-        public virtual void HandleINotifyPropertyChanging(object sender, PropertyChangingEventArgs eventArgs);

-        public bool HasDefaultValue(IProperty property);

-        public virtual bool HasTemporaryValue(IProperty property);

-        public virtual bool IsConceptualNull(IProperty property);

-        public virtual bool IsLoaded(INavigation navigation);

-        public virtual bool IsModified(IProperty property);

-        public virtual bool IsStoreGenerated(IProperty property);

-        public virtual void MarkAsTemporary(IProperty property, bool isTemporary = true);

-        protected virtual void MarkShadowPropertiesNotSet(IEntityType entityType);

-        public virtual void MarkUnchangedFromQuery(ISet<IForeignKey> handledForeignKeys);

-        public virtual InternalEntityEntry PrepareToSave();

-        protected virtual bool PropertyHasDefaultValue(IPropertyBase propertyBase);

-        protected virtual object ReadPropertyValue(IPropertyBase propertyBase);

-        protected virtual T ReadShadowValue<T>(int shadowIndex);

-        public virtual void RemoveFromCollection(INavigation navigation, InternalEntityEntry value);

-        public virtual void RemoveFromCollectionSnapshot(IPropertyBase propertyBase, object removedEntity);

-        public virtual void SetCurrentValue(IPropertyBase propertyBase, object value);

-        public virtual void SetEntityState(EntityState entityState, bool acceptChanges = false, Nullable<EntityState> forceStateWhenUnknownKey = default(Nullable<EntityState>));

-        public virtual Task SetEntityStateAsync(EntityState entityState, bool acceptChanges, Nullable<EntityState> forceStateWhenUnknownKey, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual void SetIsLoaded(INavigation navigation, bool loaded = true);

-        public virtual void SetOriginalValue(IPropertyBase propertyBase, object value, int index = -1);

-        public virtual void SetProperty(IPropertyBase propertyBase, object value, bool setModified = true);

-        public virtual void SetPropertyModified(IProperty property, bool changeState = true, bool isModified = true, bool isConceptualNull = false);

-        public virtual void SetRelationshipSnapshotValue(IPropertyBase propertyBase, object value);

-        public virtual EntityEntry ToEntityEntry();

-        protected virtual void WritePropertyValue(IPropertyBase propertyBase, object value);

-    }
-    public class InternalEntityEntryFactory : IInternalEntityEntryFactory {
 {
-        public InternalEntityEntryFactory();

-        public virtual InternalEntityEntry Create(IStateManager stateManager, IEntityType entityType, object entity);

-        public virtual InternalEntityEntry Create(IStateManager stateManager, IEntityType entityType, object entity, in ValueBuffer valueBuffer);

-    }
-    public class InternalEntityEntryNotifier : IInternalEntityEntryNotifier {
 {
-        public InternalEntityEntryNotifier(IEnumerable<IEntityStateListener> entityStateListeners, IEnumerable<IPropertyListener> propertyListeners, IEnumerable<INavigationListener> navigationListeners, IEnumerable<IKeyListener> keyListeners, IEnumerable<IQueryTrackingListener> queryTrackingListeners);

-        public virtual void KeyPropertyChanged(InternalEntityEntry entry, IProperty property, IReadOnlyList<IKey> keys, IReadOnlyList<IForeignKey> foreignKeys, object oldValue, object newValue);

-        public virtual void NavigationCollectionChanged(InternalEntityEntry entry, INavigation navigation, IEnumerable<object> added, IEnumerable<object> removed);

-        public virtual void NavigationReferenceChanged(InternalEntityEntry entry, INavigation navigation, object oldValue, object newValue);

-        public virtual void PropertyChanged(InternalEntityEntry entry, IPropertyBase property, bool setModified);

-        public virtual void PropertyChanging(InternalEntityEntry entry, IPropertyBase property);

-        public virtual void StateChanged(InternalEntityEntry entry, EntityState oldState, bool fromQuery);

-        public virtual void StateChanging(InternalEntityEntry entry, EntityState newState);

-        public virtual void TrackedFromQuery(InternalEntityEntry entry, ISet<IForeignKey> handledForeignKeys);

-    }
-    public class InternalEntityEntrySubscriber : IInternalEntityEntrySubscriber {
 {
-        public InternalEntityEntrySubscriber();

-        public virtual bool SnapshotAndSubscribe(InternalEntityEntry entry);

-        public virtual void Unsubscribe(InternalEntityEntry entry);

-    }
-    public class InternalMixedEntityEntry : InternalEntityEntry {
 {
-        public InternalMixedEntityEntry(IStateManager stateManager, IEntityType entityType, object entity);

-        public InternalMixedEntityEntry(IStateManager stateManager, IEntityType entityType, object entity, in ValueBuffer valueBuffer);

-        public override object Entity { get; }

-        public override bool AddToCollection(INavigation navigation, InternalEntityEntry value);

-        public override bool CollectionContains(INavigation navigation, InternalEntityEntry value);

-        public override object GetOrCreateCollection(INavigation navigation);

-        protected override bool PropertyHasDefaultValue(IPropertyBase propertyBase);

-        protected override object ReadPropertyValue(IPropertyBase propertyBase);

-        protected override T ReadShadowValue<T>(int shadowIndex);

-        public override void RemoveFromCollection(INavigation navigation, InternalEntityEntry value);

-        protected override void WritePropertyValue(IPropertyBase propertyBase, object value);

-    }
-    public class InternalShadowEntityEntry : InternalEntityEntry {
 {
-        public InternalShadowEntityEntry(IStateManager stateManager, IEntityType entityType);

-        public InternalShadowEntityEntry(IStateManager stateManager, IEntityType entityType, in ValueBuffer valueBuffer);

-        public override object Entity { get; }

-        public override bool AddToCollection(INavigation navigation, InternalEntityEntry value);

-        public override bool CollectionContains(INavigation navigation, InternalEntityEntry value);

-        public override object GetOrCreateCollection(INavigation navigation);

-        protected override bool PropertyHasDefaultValue(IPropertyBase propertyBase);

-        protected override object ReadPropertyValue(IPropertyBase propertyBase);

-        protected override T ReadShadowValue<T>(int shadowIndex);

-        public override void RemoveFromCollection(INavigation navigation, InternalEntityEntry value);

-        protected override void WritePropertyValue(IPropertyBase propertyBase, object value);

-    }
-    public interface INullableValueFactory<TKey> : IDependentKeyValueFactory<TKey> {
 {
-        IEqualityComparer<TKey> EqualityComparer { get; }

-    }
-    public interface IPrincipalKeyValueFactory<TKey> {
 {
-        IEqualityComparer<TKey> EqualityComparer { get; }

-        object CreateFromBuffer(ValueBuffer valueBuffer);

-        TKey CreateFromCurrentValues(InternalEntityEntry entry);

-        object CreateFromKeyValues(object[] keyValues);

-        TKey CreateFromOriginalValues(InternalEntityEntry entry);

-        TKey CreateFromRelationshipSnapshot(InternalEntityEntry entry);

-        IProperty FindNullPropertyInCurrentValues(InternalEntityEntry entry);

-        IProperty FindNullPropertyInValueBuffer(ValueBuffer valueBuffer);

-    }
-    public interface IPropertyListener {
 {
-        void PropertyChanged(InternalEntityEntry entry, IPropertyBase propertyBase, bool setModified);

-        void PropertyChanging(InternalEntityEntry entry, IPropertyBase propertyBase);

-    }
-    public interface IQueryTrackingListener {
 {
-        void TrackedFromQuery(InternalEntityEntry entry, ISet<IForeignKey> handledForeignKeys);

-    }
-    public interface ISnapshot {
 {
-        object this[int index] { get; set; }

-        T GetValue<T>(int index);

-    }
-    public interface IStateManager : IResettableService {
 {
-        int ChangedCount { get; set; }

-        DbContext Context { get; }

-        IEntityMaterializerSource EntityMaterializerSource { get; }

-        IEnumerable<InternalEntityEntry> Entries { get; }

-        IInternalEntityEntryNotifier InternalEntityEntryNotifier { get; }

-        bool SensitiveLoggingEnabled { get; }

-        IDiagnosticsLogger<DbLoggerCategory.Update> UpdateLogger { get; }

-        IValueGenerationManager ValueGenerationManager { get; }

-        event EventHandler<EntityStateChangedEventArgs> StateChanged;

-        event EventHandler<EntityTrackedEventArgs> Tracked;

-        void AcceptAllChanges();

-        void BeginTrackingQuery();

-        IEntityFinder CreateEntityFinder(IEntityType entityType);

-        InternalEntityEntry CreateEntry(IDictionary<string, object> values, IEntityType entityType);

-        void EndSingleQueryMode();

-        IEnumerable<InternalEntityEntry> GetDependents(InternalEntityEntry principalEntry, IForeignKey foreignKey);

-        IEnumerable<InternalEntityEntry> GetDependentsFromNavigation(InternalEntityEntry principalEntry, IForeignKey foreignKey);

-        IEnumerable<InternalEntityEntry> GetDependentsUsingRelationshipSnapshot(InternalEntityEntry principalEntry, IForeignKey foreignKey);

-        IReadOnlyList<IUpdateEntry> GetEntriesToSave();

-        InternalEntityEntry GetOrCreateEntry(object entity);

-        InternalEntityEntry GetOrCreateEntry(object entity, IEntityType entityType);

-        InternalEntityEntry GetPrincipal(InternalEntityEntry dependentEntry, IForeignKey foreignKey);

-        InternalEntityEntry GetPrincipalUsingPreStoreGeneratedValues(InternalEntityEntry dependentEntry, IForeignKey foreignKey);

-        InternalEntityEntry GetPrincipalUsingRelationshipSnapshot(InternalEntityEntry dependentEntry, IForeignKey foreignKey);

-        IEnumerable<Tuple<INavigation, InternalEntityEntry>> GetRecordedReferrers(object referencedEntity, bool clear);

-        TrackingQueryMode GetTrackingQueryMode(IEntityType entityType);

-        void OnStateChanged(InternalEntityEntry internalEntityEntry, EntityState oldState);

-        void OnTracked(InternalEntityEntry internalEntityEntry, bool fromQuery);

-        void RecordReferencedUntrackedEntity(object referencedEntity, INavigation navigation, InternalEntityEntry referencedFromEntry);

-        int SaveChanges(bool acceptAllChangesOnSuccess);

-        Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default(CancellationToken));

-        InternalEntityEntry StartTracking(InternalEntityEntry entry);

-        InternalEntityEntry StartTrackingFromQuery(IEntityType baseEntityType, object entity, in ValueBuffer valueBuffer, ISet<IForeignKey> handledForeignKeys);

-        void StopTracking(InternalEntityEntry entry);

-        InternalEntityEntry TryGetEntry(IKey key, in ValueBuffer valueBuffer, bool throwOnNullKey);

-        InternalEntityEntry TryGetEntry(IKey key, object[] keyValues);

-        InternalEntityEntry TryGetEntry(object entity, IEntityType type);

-        InternalEntityEntry TryGetEntry(object entity, bool throwOnNonUniqueness = true);

-        void Unsubscribe();

-        void UpdateDependentMap(InternalEntityEntry entry, IForeignKey foreignKey);

-        void UpdateIdentityMap(InternalEntityEntry entry, IKey principalKey);

-    }
-    public interface IValueGenerationManager {
 {
-        void Generate(InternalEntityEntry entry);

-        Task GenerateAsync(InternalEntityEntry entry, CancellationToken cancellationToken = default(CancellationToken));

-        bool MayGetTemporaryValue(IProperty property, IEntityType entityType);

-        InternalEntityEntry Propagate(InternalEntityEntry entry);

-    }
-    public class KeyPropagator : IKeyPropagator {
 {
-        public KeyPropagator(IValueGeneratorSelector valueGeneratorSelector);

-        public virtual InternalEntityEntry PropagateValue(InternalEntityEntry entry, IProperty property);

-        public virtual Task<InternalEntityEntry> PropagateValueAsync(InternalEntityEntry entry, IProperty property, CancellationToken cancellationToken = default(CancellationToken));

-    }
-    public class KeyValueFactoryFactory {
 {
-        public KeyValueFactoryFactory();

-        public virtual IPrincipalKeyValueFactory<TKey> Create<TKey>(IKey key);

-    }
-    public class LocalViewListener : IEntityStateListener, ILocalViewListener {
 {
-        public LocalViewListener();

-        public virtual void RegisterView(Action<InternalEntityEntry, EntityState> viewAction);

-        public virtual void StateChanged(InternalEntityEntry entry, EntityState oldState, bool fromQuery);

-        public virtual void StateChanging(InternalEntityEntry entry, EntityState newState);

-    }
-    public readonly struct MultiSnapshot : ISnapshot {
 {
-        public MultiSnapshot(ISnapshot[] snapshots);

-        public object this[int index] { get; set; }

-        public T GetValue<T>(int index);

-    }
-    public class NavigationFixer : IEntityStateListener, IKeyListener, INavigationFixer, INavigationListener, IQueryTrackingListener {
 {
-        public NavigationFixer(IChangeDetector changeDetector, IEntityGraphAttacher attacher, ILoggingOptions loggingOptions);

-        public virtual void KeyPropertyChanged(InternalEntityEntry entry, IProperty property, IReadOnlyList<IKey> containingPrincipalKeys, IReadOnlyList<IForeignKey> containingForeignKeys, object oldValue, object newValue);

-        public virtual void NavigationCollectionChanged(InternalEntityEntry entry, INavigation navigation, IEnumerable<object> added, IEnumerable<object> removed);

-        public virtual void NavigationReferenceChanged(InternalEntityEntry entry, INavigation navigation, object oldValue, object newValue);

-        public virtual void StateChanged(InternalEntityEntry entry, EntityState oldState, bool fromQuery);

-        public virtual void StateChanging(InternalEntityEntry entry, EntityState newState);

-        public virtual void TrackedFromQuery(InternalEntityEntry entry, ISet<IForeignKey> handledForeignKeys);

-    }
-    public class NullableKeyIdentityMap<TKey> : IdentityMap<TKey> {
 {
-        public NullableKeyIdentityMap(IKey key, IPrincipalKeyValueFactory<TKey> principalKeyValueFactory, bool sensitiveLoggingEnabled);

-        public override void Add(InternalEntityEntry entry);

-        public override void RemoveUsingRelationshipSnapshot(InternalEntityEntry entry);

-    }
-    public class ObservableBackedBindingList<T> : SortableBindingList<T> {
 {
-        public ObservableBackedBindingList(ICollection<T> obervableCollection);

-        protected override object AddNewCore();

-        public override void CancelNew(int itemIndex);

-        protected override void ClearItems();

-        public override void EndNew(int itemIndex);

-        protected override void InsertItem(int index, T item);

-        protected override void RemoveItem(int index);

-        protected override void SetItem(int index, T item);

-    }
-    public class OriginalPropertyValues : EntryPropertyValues {
 {
-        public OriginalPropertyValues(InternalEntityEntry internalEntry);

-        public override TValue GetValue<TValue>(IProperty property);

-        public override TValue GetValue<TValue>(string propertyName);

-        protected override object GetValueInternal(IProperty property);

-        protected override void SetValueInternal(IProperty property, object value);

-    }
-    public class OriginalValuesFactoryFactory : SnapshotFactoryFactory<InternalEntityEntry> {
 {
-        public OriginalValuesFactoryFactory();

-        protected override int GetPropertyCount(IEntityType entityType);

-        protected override int GetPropertyIndex(IPropertyBase propertyBase);

-        protected override ValueComparer GetValueComparer(IProperty property);

-    }
-    public class RelationshipSnapshotFactoryFactory : SnapshotFactoryFactory<InternalEntityEntry> {
 {
-        public RelationshipSnapshotFactoryFactory();

-        protected override int GetPropertyCount(IEntityType entityType);

-        protected override int GetPropertyIndex(IPropertyBase propertyBase);

-        protected override ValueComparer GetValueComparer(IProperty property);

-    }
-    public class ShadowValuesFactoryFactory : SnapshotFactoryFactory<ValueBuffer> {
 {
-        public ShadowValuesFactoryFactory();

-        protected override bool UseEntityVariable { get; }

-        protected override Expression CreateReadShadowValueExpression(ParameterExpression parameter, IPropertyBase property);

-        protected override int GetPropertyCount(IEntityType entityType);

-        protected override int GetPropertyIndex(IPropertyBase propertyBase);

-        protected override ValueComparer GetValueComparer(IProperty property);

-    }
-    public class SimpleFullyNullableDependentKeyValueFactory<TKey> : IDependentKeyValueFactory<TKey> {
 {
-        public SimpleFullyNullableDependentKeyValueFactory(PropertyAccessors propertyAccessors);

-        public virtual bool TryCreateFromBuffer(in ValueBuffer valueBuffer, out TKey key);

-        public virtual bool TryCreateFromCurrentValues(InternalEntityEntry entry, out TKey key);

-        public virtual bool TryCreateFromOriginalValues(InternalEntityEntry entry, out TKey key);

-        public virtual bool TryCreateFromPreStoreGeneratedCurrentValues(InternalEntityEntry entry, out TKey key);

-        public virtual bool TryCreateFromRelationshipSnapshot(InternalEntityEntry entry, out TKey key);

-    }
-    public class SimpleNonNullableDependentKeyValueFactory<TKey> : IDependentKeyValueFactory<TKey> {
 {
-        public SimpleNonNullableDependentKeyValueFactory(PropertyAccessors propertyAccessors);

-        public virtual bool TryCreateFromBuffer(in ValueBuffer valueBuffer, out TKey key);

-        public virtual bool TryCreateFromCurrentValues(InternalEntityEntry entry, out TKey key);

-        public virtual bool TryCreateFromOriginalValues(InternalEntityEntry entry, out TKey key);

-        public virtual bool TryCreateFromPreStoreGeneratedCurrentValues(InternalEntityEntry entry, out TKey key);

-        public virtual bool TryCreateFromRelationshipSnapshot(InternalEntityEntry entry, out TKey key);

-    }
-    public class SimpleNullableDependentKeyValueFactory<TKey> : IDependentKeyValueFactory<TKey> where TKey : struct, ValueType {
 {
-        public SimpleNullableDependentKeyValueFactory(PropertyAccessors propertyAccessors);

-        public virtual bool TryCreateFromBuffer(in ValueBuffer valueBuffer, out TKey key);

-        public virtual bool TryCreateFromCurrentValues(InternalEntityEntry entry, out TKey key);

-        public virtual bool TryCreateFromOriginalValues(InternalEntityEntry entry, out TKey key);

-        public virtual bool TryCreateFromPreStoreGeneratedCurrentValues(InternalEntityEntry entry, out TKey key);

-        public virtual bool TryCreateFromRelationshipSnapshot(InternalEntityEntry entry, out TKey key);

-    }
-    public class SimpleNullablePrincipalDependentKeyValueFactory<TKey, TNonNullableKey> : IDependentKeyValueFactory<TKey> where TNonNullableKey : struct, ValueType {
 {
-        public SimpleNullablePrincipalDependentKeyValueFactory(PropertyAccessors propertyAccessors);

-        public virtual bool TryCreateFromBuffer(in ValueBuffer valueBuffer, out TKey key);

-        public virtual bool TryCreateFromCurrentValues(InternalEntityEntry entry, out TKey key);

-        public virtual bool TryCreateFromOriginalValues(InternalEntityEntry entry, out TKey key);

-        public virtual bool TryCreateFromPreStoreGeneratedCurrentValues(InternalEntityEntry entry, out TKey key);

-        public virtual bool TryCreateFromRelationshipSnapshot(InternalEntityEntry entry, out TKey key);

-    }
-    public class SimplePrincipalKeyValueFactory<TKey> : IPrincipalKeyValueFactory<TKey> {
 {
-        public SimplePrincipalKeyValueFactory(IProperty property);

-        public virtual IEqualityComparer<TKey> EqualityComparer { get; }

-        public virtual object CreateFromBuffer(ValueBuffer valueBuffer);

-        public virtual TKey CreateFromCurrentValues(InternalEntityEntry entry);

-        public virtual object CreateFromKeyValues(object[] keyValues);

-        public virtual TKey CreateFromOriginalValues(InternalEntityEntry entry);

-        public virtual TKey CreateFromRelationshipSnapshot(InternalEntityEntry entry);

-        public virtual IProperty FindNullPropertyInCurrentValues(InternalEntityEntry entry);

-        public virtual IProperty FindNullPropertyInValueBuffer(ValueBuffer valueBuffer);

-    }
-    public sealed class Snapshot : ISnapshot {
 {
-        public static ISnapshot Empty;

-        public const int MaxGenericTypes = 30;

-        public object this[int index] { get; set; }

-        public static Delegate[] CreateReaders<TSnapshot>();

-        public static Type CreateSnapshotType(Type[] types);

-        public T GetValue<T>(int index);

-    }
-    public sealed class Snapshot<T0> : ISnapshot {
 {
-        public Snapshot(T0 value0);

-        public object this[int index] { get; set; }

-        public T GetValue<T>(int index);

-    }
-    public sealed class Snapshot<T0, T1> : ISnapshot {
 {
-        public Snapshot(T0 value0, T1 value1);

-        public object this[int index] { get; set; }

-        public T GetValue<T>(int index);

-    }
-    public sealed class Snapshot<T0, T1, T2> : ISnapshot {
 {
-        public Snapshot(T0 value0, T1 value1, T2 value2);

-        public object this[int index] { get; set; }

-        public T GetValue<T>(int index);

-    }
-    public sealed class Snapshot<T0, T1, T2, T3> : ISnapshot {
 {
-        public Snapshot(T0 value0, T1 value1, T2 value2, T3 value3);

-        public object this[int index] { get; set; }

-        public T GetValue<T>(int index);

-    }
-    public sealed class Snapshot<T0, T1, T2, T3, T4> : ISnapshot {
 {
-        public Snapshot(T0 value0, T1 value1, T2 value2, T3 value3, T4 value4);

-        public object this[int index] { get; set; }

-        public T GetValue<T>(int index);

-    }
-    public sealed class Snapshot<T0, T1, T2, T3, T4, T5> : ISnapshot {
 {
-        public Snapshot(T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5);

-        public object this[int index] { get; set; }

-        public T GetValue<T>(int index);

-    }
-    public sealed class Snapshot<T0, T1, T2, T3, T4, T5, T6> : ISnapshot {
 {
-        public Snapshot(T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6);

-        public object this[int index] { get; set; }

-        public T GetValue<T>(int index);

-    }
-    public sealed class Snapshot<T0, T1, T2, T3, T4, T5, T6, T7> : ISnapshot {
 {
-        public Snapshot(T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7);

-        public object this[int index] { get; set; }

-        public T GetValue<T>(int index);

-    }
-    public sealed class Snapshot<T0, T1, T2, T3, T4, T5, T6, T7, T8> : ISnapshot {
 {
-        public Snapshot(T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8);

-        public object this[int index] { get; set; }

-        public T GetValue<T>(int index);

-    }
-    public sealed class Snapshot<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> : ISnapshot {
 {
-        public Snapshot(T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9);

-        public object this[int index] { get; set; }

-        public T GetValue<T>(int index);

-    }
-    public sealed class Snapshot<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> : ISnapshot {
 {
-        public Snapshot(T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10);

-        public object this[int index] { get; set; }

-        public T GetValue<T>(int index);

-    }
-    public sealed class Snapshot<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> : ISnapshot {
 {
-        public Snapshot(T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11);

-        public object this[int index] { get; set; }

-        public T GetValue<T>(int index);

-    }
-    public sealed class Snapshot<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> : ISnapshot {
 {
-        public Snapshot(T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11, T12 value12);

-        public object this[int index] { get; set; }

-        public T GetValue<T>(int index);

-    }
-    public sealed class Snapshot<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> : ISnapshot {
 {
-        public Snapshot(T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11, T12 value12, T13 value13);

-        public object this[int index] { get; set; }

-        public T GetValue<T>(int index);

-    }
-    public sealed class Snapshot<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> : ISnapshot {
 {
-        public Snapshot(T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11, T12 value12, T13 value13, T14 value14);

-        public object this[int index] { get; set; }

-        public T GetValue<T>(int index);

-    }
-    public sealed class Snapshot<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> : ISnapshot {
 {
-        public Snapshot(T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11, T12 value12, T13 value13, T14 value14, T15 value15);

-        public object this[int index] { get; set; }

-        public T GetValue<T>(int index);

-    }
-    public sealed class Snapshot<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> : ISnapshot {
 {
-        public Snapshot(T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11, T12 value12, T13 value13, T14 value14, T15 value15, T16 value16);

-        public object this[int index] { get; set; }

-        public T GetValue<T>(int index);

-    }
-    public sealed class Snapshot<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17> : ISnapshot {
 {
-        public Snapshot(T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11, T12 value12, T13 value13, T14 value14, T15 value15, T16 value16, T17 value17);

-        public object this[int index] { get; set; }

-        public T GetValue<T>(int index);

-    }
-    public sealed class Snapshot<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18> : ISnapshot {
 {
-        public Snapshot(T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11, T12 value12, T13 value13, T14 value14, T15 value15, T16 value16, T17 value17, T18 value18);

-        public object this[int index] { get; set; }

-        public T GetValue<T>(int index);

-    }
-    public sealed class Snapshot<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19> : ISnapshot {
 {
-        public Snapshot(T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11, T12 value12, T13 value13, T14 value14, T15 value15, T16 value16, T17 value17, T18 value18, T19 value19);

-        public object this[int index] { get; set; }

-        public T GetValue<T>(int index);

-    }
-    public sealed class Snapshot<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20> : ISnapshot {
 {
-        public Snapshot(T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11, T12 value12, T13 value13, T14 value14, T15 value15, T16 value16, T17 value17, T18 value18, T19 value19, T20 value20);

-        public object this[int index] { get; set; }

-        public T GetValue<T>(int index);

-    }
-    public sealed class Snapshot<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21> : ISnapshot {
 {
-        public Snapshot(T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11, T12 value12, T13 value13, T14 value14, T15 value15, T16 value16, T17 value17, T18 value18, T19 value19, T20 value20, T21 value21);

-        public object this[int index] { get; set; }

-        public T GetValue<T>(int index);

-    }
-    public sealed class Snapshot<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22> : ISnapshot {
 {
-        public Snapshot(T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11, T12 value12, T13 value13, T14 value14, T15 value15, T16 value16, T17 value17, T18 value18, T19 value19, T20 value20, T21 value21, T22 value22);

-        public object this[int index] { get; set; }

-        public T GetValue<T>(int index);

-    }
-    public sealed class Snapshot<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23> : ISnapshot {
 {
-        public Snapshot(T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11, T12 value12, T13 value13, T14 value14, T15 value15, T16 value16, T17 value17, T18 value18, T19 value19, T20 value20, T21 value21, T22 value22, T23 value23);

-        public object this[int index] { get; set; }

-        public T GetValue<T>(int index);

-    }
-    public sealed class Snapshot<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24> : ISnapshot {
 {
-        public Snapshot(T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11, T12 value12, T13 value13, T14 value14, T15 value15, T16 value16, T17 value17, T18 value18, T19 value19, T20 value20, T21 value21, T22 value22, T23 value23, T24 value24);

-        public object this[int index] { get; set; }

-        public T GetValue<T>(int index);

-    }
-    public sealed class Snapshot<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25> : ISnapshot {
 {
-        public Snapshot(T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11, T12 value12, T13 value13, T14 value14, T15 value15, T16 value16, T17 value17, T18 value18, T19 value19, T20 value20, T21 value21, T22 value22, T23 value23, T24 value24, T25 value25);

-        public object this[int index] { get; set; }

-        public T GetValue<T>(int index);

-    }
-    public sealed class Snapshot<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26> : ISnapshot {
 {
-        public Snapshot(T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11, T12 value12, T13 value13, T14 value14, T15 value15, T16 value16, T17 value17, T18 value18, T19 value19, T20 value20, T21 value21, T22 value22, T23 value23, T24 value24, T25 value25, T26 value26);

-        public object this[int index] { get; set; }

-        public T GetValue<T>(int index);

-    }
-    public sealed class Snapshot<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27> : ISnapshot {
 {
-        public Snapshot(T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11, T12 value12, T13 value13, T14 value14, T15 value15, T16 value16, T17 value17, T18 value18, T19 value19, T20 value20, T21 value21, T22 value22, T23 value23, T24 value24, T25 value25, T26 value26, T27 value27);

-        public object this[int index] { get; set; }

-        public T GetValue<T>(int index);

-    }
-    public sealed class Snapshot<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28> : ISnapshot {
 {
-        public Snapshot(T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11, T12 value12, T13 value13, T14 value14, T15 value15, T16 value16, T17 value17, T18 value18, T19 value19, T20 value20, T21 value21, T22 value22, T23 value23, T24 value24, T25 value25, T26 value26, T27 value27, T28 value28);

-        public object this[int index] { get; set; }

-        public T GetValue<T>(int index);

-    }
-    public sealed class Snapshot<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29> : ISnapshot {
 {
-        public Snapshot(T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10, T11 value11, T12 value12, T13 value13, T14 value14, T15 value15, T16 value16, T17 value17, T18 value18, T19 value19, T20 value20, T21 value21, T22 value22, T23 value23, T24 value24, T25 value25, T26 value26, T27 value27, T28 value28, T29 value29);

-        public object this[int index] { get; set; }

-        public T GetValue<T>(int index);

-    }
-    public abstract class SnapshotFactoryFactory {
 {
-        protected SnapshotFactoryFactory();

-        protected virtual bool UseEntityVariable { get; }

-        protected virtual Expression CreateConstructorExpression(IEntityType entityType, ParameterExpression parameter);

-        public virtual Func<ISnapshot> CreateEmpty(IEntityType entityType);

-        protected virtual Expression CreateReadShadowValueExpression(ParameterExpression parameter, IPropertyBase property);

-        protected abstract int GetPropertyCount(IEntityType entityType);

-        protected abstract int GetPropertyIndex(IPropertyBase propertyBase);

-        protected abstract ValueComparer GetValueComparer(IProperty property);

-    }
-    public abstract class SnapshotFactoryFactory<TInput> : SnapshotFactoryFactory {
 {
-        protected SnapshotFactoryFactory();

-        public virtual Func<TInput, ISnapshot> Create(IEntityType entityType);

-    }
-    public class SortableBindingList<T> : BindingList<T> {
 {
-        public SortableBindingList(List<T> list);

-        protected override bool IsSortedCore { get; }

-        protected override ListSortDirection SortDirectionCore { get; }

-        protected override PropertyDescriptor SortPropertyCore { get; }

-        protected override bool SupportsSortingCore { get; }

-        protected override void ApplySortCore(PropertyDescriptor prop, ListSortDirection direction);

-        protected override void RemoveSortCore();

-    }
-    public class StateManager : IResettableService, IStateManager {
 {
-        public StateManager(StateManagerDependencies dependencies);

-        public virtual int ChangedCount { get; set; }

-        public virtual DbContext Context { get; }

-        public virtual IEntityFinderFactory EntityFinderFactory { get; }

-        public virtual IEntityMaterializerSource EntityMaterializerSource { get; }

-        public virtual IEnumerable<InternalEntityEntry> Entries { get; }

-        public virtual IInternalEntityEntryNotifier InternalEntityEntryNotifier { get; }

-        public virtual bool SensitiveLoggingEnabled { get; }

-        public virtual IDiagnosticsLogger<DbLoggerCategory.Update> UpdateLogger { get; }

-        public virtual IValueGenerationManager ValueGenerationManager { get; }

-        public event EventHandler<EntityStateChangedEventArgs> StateChanged;

-        public event EventHandler<EntityTrackedEventArgs> Tracked;

-        public virtual void AcceptAllChanges();

-        public virtual void BeginTrackingQuery();

-        public virtual IEntityFinder CreateEntityFinder(IEntityType entityType);

-        public virtual InternalEntityEntry CreateEntry(IDictionary<string, object> values, IEntityType entityType);

-        public virtual void EndSingleQueryMode();

-        public virtual IEnumerable<InternalEntityEntry> GetDependents(InternalEntityEntry principalEntry, IForeignKey foreignKey);

-        public virtual IEnumerable<InternalEntityEntry> GetDependentsFromNavigation(InternalEntityEntry principalEntry, IForeignKey foreignKey);

-        public virtual IEnumerable<InternalEntityEntry> GetDependentsUsingRelationshipSnapshot(InternalEntityEntry principalEntry, IForeignKey foreignKey);

-        public virtual IReadOnlyList<IUpdateEntry> GetEntriesToSave();

-        public virtual IReadOnlyList<InternalEntityEntry> GetInternalEntriesToSave();

-        public virtual InternalEntityEntry GetOrCreateEntry(object entity);

-        public virtual InternalEntityEntry GetOrCreateEntry(object entity, IEntityType entityType);

-        public virtual InternalEntityEntry GetPrincipal(InternalEntityEntry dependentEntry, IForeignKey foreignKey);

-        public virtual InternalEntityEntry GetPrincipalUsingPreStoreGeneratedValues(InternalEntityEntry dependentEntry, IForeignKey foreignKey);

-        public virtual InternalEntityEntry GetPrincipalUsingRelationshipSnapshot(InternalEntityEntry dependentEntry, IForeignKey foreignKey);

-        public virtual IEnumerable<Tuple<INavigation, InternalEntityEntry>> GetRecordedReferrers(object referencedEntity, bool clear);

-        public virtual TrackingQueryMode GetTrackingQueryMode(IEntityType entityType);

-        public virtual void OnStateChanged(InternalEntityEntry internalEntityEntry, EntityState oldState);

-        public virtual void OnTracked(InternalEntityEntry internalEntityEntry, bool fromQuery);

-        public virtual void RecordReferencedUntrackedEntity(object referencedEntity, INavigation navigation, InternalEntityEntry referencedFromEntry);

-        public virtual void ResetState();

-        public virtual int SaveChanges(bool acceptAllChangesOnSuccess);

-        protected virtual int SaveChanges(IReadOnlyList<InternalEntityEntry> entriesToSave);

-        public virtual Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default(CancellationToken));

-        protected virtual Task<int> SaveChangesAsync(IReadOnlyList<InternalEntityEntry> entriesToSave, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual InternalEntityEntry StartTracking(InternalEntityEntry entry);

-        public virtual InternalEntityEntry StartTrackingFromQuery(IEntityType baseEntityType, object entity, in ValueBuffer valueBuffer, ISet<IForeignKey> handledForeignKeys);

-        public virtual void StopTracking(InternalEntityEntry entry);

-        public virtual InternalEntityEntry TryGetEntry(IKey key, in ValueBuffer valueBuffer, bool throwOnNullKey);

-        public virtual InternalEntityEntry TryGetEntry(IKey key, object[] keyValues);

-        public virtual InternalEntityEntry TryGetEntry(object entity, IEntityType entityType);

-        public virtual InternalEntityEntry TryGetEntry(object entity, bool throwOnNonUniqueness = true);

-        public virtual void Unsubscribe();

-        public virtual void UpdateDependentMap(InternalEntityEntry entry, IForeignKey foreignKey);

-        public virtual void UpdateIdentityMap(InternalEntityEntry entry, IKey key);

-    }
-    public sealed class StateManagerDependencies {
 {
-        public StateManagerDependencies(IInternalEntityEntryFactory internalEntityEntryFactory, IInternalEntityEntrySubscriber internalEntityEntrySubscriber, IInternalEntityEntryNotifier internalEntityEntryNotifier, IValueGenerationManager valueGenerationManager, IModel model, IDatabase database, IConcurrencyDetector concurrencyDetector, ICurrentDbContext currentContext, IEntityFinderSource entityFinderSource, IDbSetSource setSource, IEntityMaterializerSource entityMaterializerSource, ILoggingOptions loggingOptions, IDiagnosticsLogger<DbLoggerCategory.Update> updateLogger, IDiagnosticsLogger<DbLoggerCategory.ChangeTracking> changeTrackingLogger);

-        public IDiagnosticsLogger<DbLoggerCategory.ChangeTracking> ChangeTrackingLogger { get; }

-        public IConcurrencyDetector ConcurrencyDetector { get; }

-        public ICurrentDbContext CurrentContext { get; }

-        public IDatabase Database { get; }

-        public IEntityFinderSource EntityFinderSource { get; }

-        public IEntityMaterializerSource EntityMaterializerSource { get; }

-        public IInternalEntityEntryFactory InternalEntityEntryFactory { get; }

-        public IInternalEntityEntryNotifier InternalEntityEntryNotifier { get; }

-        public IInternalEntityEntrySubscriber InternalEntityEntrySubscriber { get; }

-        public ILoggingOptions LoggingOptions { get; }

-        public IModel Model { get; }

-        public IDbSetSource SetSource { get; }

-        public IDiagnosticsLogger<DbLoggerCategory.Update> UpdateLogger { get; }

-        public IValueGenerationManager ValueGenerationManager { get; }

-        public StateManagerDependencies With(IInternalEntityEntryFactory internalEntityEntryFactory);

-        public StateManagerDependencies With(IInternalEntityEntryNotifier internalEntityEntryNotifier);

-        public StateManagerDependencies With(IInternalEntityEntrySubscriber internalEntityEntrySubscriber);

-        public StateManagerDependencies With(ValueGenerationManager valueGenerationManager);

-        public StateManagerDependencies With(IDiagnosticsLogger<DbLoggerCategory.ChangeTracking> changeTrackingLogger);

-        public StateManagerDependencies With(IDiagnosticsLogger<DbLoggerCategory.Update> updateLogger);

-        public StateManagerDependencies With(ILoggingOptions loggingOptions);

-        public StateManagerDependencies With(IConcurrencyDetector concurrencyDetector);

-        public StateManagerDependencies With(ICurrentDbContext currentContext);

-        public StateManagerDependencies With(IDbSetSource setSource);

-        public StateManagerDependencies With(IEntityFinderSource entityFinderSource);

-        public StateManagerDependencies With(IModel model);

-        public StateManagerDependencies With(IEntityMaterializerSource entityMaterializerSource);

-        public StateManagerDependencies With(IDatabase database);

-    }
-    public enum TrackingQueryMode {
 {
-        Multiple = 2,

-        Simple = 0,

-        Single = 1,

-    }
-    public static class ValueComparerExtensions {
 {
-        public static ValueComparer ToNonNullNullableComparer(this ValueComparer comparer);

-    }
-    public class ValueGenerationManager : IValueGenerationManager {
 {
-        public ValueGenerationManager(IValueGeneratorSelector valueGeneratorSelector, IKeyPropagator keyPropagator, IDiagnosticsLogger<DbLoggerCategory.ChangeTracking> logger, ILoggingOptions loggingOptions);

-        public virtual void Generate(InternalEntityEntry entry);

-        public virtual Task GenerateAsync(InternalEntityEntry entry, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual bool MayGetTemporaryValue(IProperty property, IEntityType entityType);

-        public virtual InternalEntityEntry Propagate(InternalEntityEntry entry);

-    }
-}
```

