# Microsoft.EntityFrameworkCore.ChangeTracking

``` diff
-namespace Microsoft.EntityFrameworkCore.ChangeTracking {
 {
-    public class ArrayStructuralComparer<TElement> : ValueComparer<TElement[]> {
 {
-        public ArrayStructuralComparer();

-    }
-    public class ChangeTracker : IInfrastructure<IStateManager>, IResettableService {
 {
-        public ChangeTracker(DbContext context, IStateManager stateManager, IChangeDetector changeDetector, IModel model, IEntityEntryGraphIterator graphIterator);

-        public virtual bool AutoDetectChangesEnabled { get; set; }

-        public virtual DbContext Context { get; }

-        public virtual bool LazyLoadingEnabled { get; set; }

-        IStateManager Microsoft.EntityFrameworkCore.Infrastructure.IInfrastructure<Microsoft.EntityFrameworkCore.ChangeTracking.Internal.IStateManager>.Instance { get; }

-        public virtual QueryTrackingBehavior QueryTrackingBehavior { get; set; }

-        public event EventHandler<EntityStateChangedEventArgs> StateChanged;

-        public event EventHandler<EntityTrackedEventArgs> Tracked;

-        public virtual void AcceptAllChanges();

-        public virtual void DetectChanges();

-        public virtual IEnumerable<EntityEntry> Entries();

-        public virtual IEnumerable<EntityEntry<TEntity>> Entries<TEntity>() where TEntity : class;

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public virtual bool HasChanges();

-        void Microsoft.EntityFrameworkCore.Infrastructure.IResettableService.ResetState();

-        public override string ToString();

-        public virtual void TrackGraph(object rootEntity, Action<EntityEntryGraphNode> callback);

-        public virtual void TrackGraph<TState>(object rootEntity, TState state, Func<EntityEntryGraphNode, TState, bool> callback);

-    }
-    public class CollectionEntry : NavigationEntry {
 {
-        public CollectionEntry(InternalEntityEntry internalEntry, INavigation navigation);

-        public CollectionEntry(InternalEntityEntry internalEntry, string name);

-        public virtual new IEnumerable CurrentValue { get; set; }

-        protected virtual void EnsureInitialized();

-        public override void Load();

-        public override Task LoadAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public override IQueryable Query();

-    }
-    public class CollectionEntry<TEntity, TProperty> : CollectionEntry where TEntity : class where TProperty : class {
 {
-        public CollectionEntry(InternalEntityEntry internalEntry, INavigation navigation);

-        public CollectionEntry(InternalEntityEntry internalEntry, string name);

-        public virtual new IEnumerable<TProperty> CurrentValue { get; set; }

-        public virtual new EntityEntry<TEntity> EntityEntry { get; }

-        public virtual new IQueryable<TProperty> Query();

-    }
-    public class EntityEntry : IInfrastructure<InternalEntityEntry> {
 {
-        public EntityEntry(InternalEntityEntry internalEntry);

-        public virtual IEnumerable<CollectionEntry> Collections { get; }

-        public virtual DbContext Context { get; }

-        public virtual PropertyValues CurrentValues { get; }

-        public virtual object Entity { get; }

-        protected virtual InternalEntityEntry InternalEntry { get; }

-        public virtual bool IsKeySet { get; }

-        public virtual IEnumerable<MemberEntry> Members { get; }

-        public virtual IEntityType Metadata { get; }

-        InternalEntityEntry Microsoft.EntityFrameworkCore.Infrastructure.IInfrastructure<Microsoft.EntityFrameworkCore.ChangeTracking.Internal.InternalEntityEntry>.Instance { get; }

-        public virtual IEnumerable<NavigationEntry> Navigations { get; }

-        public virtual PropertyValues OriginalValues { get; }

-        public virtual IEnumerable<PropertyEntry> Properties { get; }

-        public virtual IEnumerable<ReferenceEntry> References { get; }

-        public virtual EntityState State { get; set; }

-        public virtual CollectionEntry Collection(string propertyName);

-        public override bool Equals(object obj);

-        public virtual PropertyValues GetDatabaseValues();

-        public virtual Task<PropertyValues> GetDatabaseValuesAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public override int GetHashCode();

-        public virtual MemberEntry Member(string propertyName);

-        public virtual NavigationEntry Navigation(string propertyName);

-        public virtual PropertyEntry Property(string propertyName);

-        public virtual ReferenceEntry Reference(string propertyName);

-        public virtual void Reload();

-        public virtual Task ReloadAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public override string ToString();

-    }
-    public class EntityEntry<TEntity> : EntityEntry where TEntity : class {
 {
-        public EntityEntry(InternalEntityEntry internalEntry);

-        public virtual new TEntity Entity { get; }

-        public virtual CollectionEntry<TEntity, TProperty> Collection<TProperty>(Expression<Func<TEntity, IEnumerable<TProperty>>> propertyExpression) where TProperty : class;

-        public virtual CollectionEntry<TEntity, TProperty> Collection<TProperty>(string propertyName) where TProperty : class;

-        public virtual PropertyEntry<TEntity, TProperty> Property<TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression);

-        public virtual PropertyEntry<TEntity, TProperty> Property<TProperty>(string propertyName);

-        public virtual ReferenceEntry<TEntity, TProperty> Reference<TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression) where TProperty : class;

-        public virtual ReferenceEntry<TEntity, TProperty> Reference<TProperty>(string propertyName) where TProperty : class;

-    }
-    public class EntityEntryEventArgs : EventArgs {
 {
-        public EntityEntryEventArgs(InternalEntityEntry internalEntityEntry);

-        public virtual EntityEntry Entry { get; }

-    }
-    public class EntityEntryGraphNode : IInfrastructure<InternalEntityEntry> {
 {
-        public EntityEntryGraphNode(InternalEntityEntry entry, InternalEntityEntry sourceEntry, INavigation inboundNavigation);

-        public virtual EntityEntry Entry { get; }

-        public virtual INavigation InboundNavigation { get; }

-        InternalEntityEntry Microsoft.EntityFrameworkCore.Infrastructure.IInfrastructure<Microsoft.EntityFrameworkCore.ChangeTracking.Internal.InternalEntityEntry>.Instance { get; }

-        public virtual object NodeState { get; set; }

-        public virtual EntityEntry SourceEntry { get; }

-        public virtual EntityEntryGraphNode CreateNode(EntityEntryGraphNode currentNode, InternalEntityEntry internalEntityEntry, INavigation reachedVia);

-    }
-    public class EntityStateChangedEventArgs : EntityEntryEventArgs {
 {
-        public EntityStateChangedEventArgs(InternalEntityEntry internalEntityEntry, EntityState oldState, EntityState newState);

-        public virtual EntityState NewState { get; }

-        public virtual EntityState OldState { get; }

-    }
-    public class EntityTrackedEventArgs : EntityEntryEventArgs {
 {
-        public EntityTrackedEventArgs(InternalEntityEntry internalEntityEntry, bool fromQuery);

-        public virtual bool FromQuery { get; }

-    }
-    public class GeometryValueComparer<TGeometry> : ValueComparer<TGeometry> {
 {
-        public GeometryValueComparer();

-    }
-    public interface IEntityEntryGraphIterator {
 {
-        void TraverseGraph<TState>(EntityEntryGraphNode node, TState state, Func<EntityEntryGraphNode, TState, bool> handleNode);

-        Task TraverseGraphAsync<TState>(EntityEntryGraphNode node, TState state, Func<EntityEntryGraphNode, TState, CancellationToken, Task<bool>> handleNode, CancellationToken cancellationToken = default(CancellationToken));

-    }
-    public class LocalView<TEntity> : ICollection<TEntity>, IEnumerable, IEnumerable<TEntity>, IListSource, INotifyCollectionChanged, INotifyPropertyChanged, INotifyPropertyChanging where TEntity : class {
 {
-        public LocalView(DbSet<TEntity> @set);

-        public virtual int Count { get; }

-        public virtual bool IsReadOnly { get; }

-        bool System.ComponentModel.IListSource.ContainsListCollection { get; }

-        public virtual event NotifyCollectionChangedEventHandler CollectionChanged;

-        public virtual event PropertyChangedEventHandler PropertyChanged;

-        public virtual event PropertyChangingEventHandler PropertyChanging;

-        public virtual void Add(TEntity item);

-        public virtual void Clear();

-        public virtual bool Contains(TEntity item);

-        public virtual void CopyTo(TEntity[] array, int arrayIndex);

-        public virtual IEnumerator<TEntity> GetEnumerator();

-        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e);

-        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e);

-        protected virtual void OnPropertyChanging(PropertyChangingEventArgs e);

-        public virtual bool Remove(TEntity item);

-        IEnumerator System.Collections.IEnumerable.GetEnumerator();

-        IList System.ComponentModel.IListSource.GetList();

-        public virtual BindingList<TEntity> ToBindingList();

-        public virtual ObservableCollection<TEntity> ToObservableCollection();

-    }
-    public abstract class MemberEntry : IInfrastructure<InternalEntityEntry> {
 {
-        protected MemberEntry(InternalEntityEntry internalEntry, IPropertyBase metadata);

-        public virtual object CurrentValue { get; set; }

-        public virtual EntityEntry EntityEntry { get; }

-        protected virtual InternalEntityEntry InternalEntry { get; }

-        public abstract bool IsModified { get; set; }

-        public virtual IPropertyBase Metadata { get; }

-        InternalEntityEntry Microsoft.EntityFrameworkCore.Infrastructure.IInfrastructure<Microsoft.EntityFrameworkCore.ChangeTracking.Internal.InternalEntityEntry>.Instance { get; }

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public override string ToString();

-    }
-    public abstract class NavigationEntry : MemberEntry {
 {
-        protected NavigationEntry(InternalEntityEntry internalEntry, INavigation navigation);

-        protected NavigationEntry(InternalEntityEntry internalEntry, string name, bool collection);

-        public virtual bool IsLoaded { get; set; }

-        public override bool IsModified { get; set; }

-        public virtual new INavigation Metadata { get; }

-        public virtual void Load();

-        public virtual Task LoadAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public virtual IQueryable Query();

-    }
-    public class ObservableCollectionListSource<T> : ObservableCollection<T>, IListSource where T : class {
 {
-        public ObservableCollectionListSource();

-        public ObservableCollectionListSource(IEnumerable<T> collection);

-        public ObservableCollectionListSource(List<T> list);

-        bool System.ComponentModel.IListSource.ContainsListCollection { get; }

-        IList System.ComponentModel.IListSource.GetList();

-    }
-    public class ObservableHashSet<T> : ICollection<T>, IEnumerable, IEnumerable<T>, INotifyCollectionChanged, INotifyPropertyChanged, INotifyPropertyChanging, IReadOnlyCollection<T>, ISet<T> {
 {
-        public ObservableHashSet();

-        public ObservableHashSet(IEnumerable<T> collection);

-        public ObservableHashSet(IEnumerable<T> collection, IEqualityComparer<T> comparer);

-        public ObservableHashSet(IEqualityComparer<T> comparer);

-        public virtual IEqualityComparer<T> Comparer { get; }

-        public virtual int Count { get; }

-        public virtual bool IsReadOnly { get; }

-        public virtual event NotifyCollectionChangedEventHandler CollectionChanged;

-        public virtual event PropertyChangedEventHandler PropertyChanged;

-        public virtual event PropertyChangingEventHandler PropertyChanging;

-        public virtual bool Add(T item);

-        public virtual void Clear();

-        public virtual bool Contains(T item);

-        public virtual void CopyTo(T[] array);

-        public virtual void CopyTo(T[] array, int arrayIndex);

-        public virtual void CopyTo(T[] array, int arrayIndex, int count);

-        public virtual void ExceptWith(IEnumerable<T> other);

-        public virtual HashSet<T>.Enumerator GetEnumerator();

-        public virtual void IntersectWith(IEnumerable<T> other);

-        public virtual bool IsProperSubsetOf(IEnumerable<T> other);

-        public virtual bool IsProperSupersetOf(IEnumerable<T> other);

-        public virtual bool IsSubsetOf(IEnumerable<T> other);

-        public virtual bool IsSupersetOf(IEnumerable<T> other);

-        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e);

-        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e);

-        protected virtual void OnPropertyChanging(PropertyChangingEventArgs e);

-        public virtual bool Overlaps(IEnumerable<T> other);

-        public virtual bool Remove(T item);

-        public virtual int RemoveWhere(Predicate<T> match);

-        public virtual bool SetEquals(IEnumerable<T> other);

-        public virtual void SymmetricExceptWith(IEnumerable<T> other);

-        void System.Collections.Generic.ICollection<T>.Add(T item);

-        IEnumerator<T> System.Collections.Generic.IEnumerable<T>.GetEnumerator();

-        IEnumerator System.Collections.IEnumerable.GetEnumerator();

-        public virtual void TrimExcess();

-        public virtual void UnionWith(IEnumerable<T> other);

-    }
-    public class PropertyEntry : MemberEntry {
 {
-        public PropertyEntry(InternalEntityEntry internalEntry, IProperty property);

-        public PropertyEntry(InternalEntityEntry internalEntry, string name);

-        public override bool IsModified { get; set; }

-        public virtual bool IsTemporary { get; set; }

-        public virtual new IProperty Metadata { get; }

-        public virtual object OriginalValue { get; set; }

-    }
-    public class PropertyEntry<TEntity, TProperty> : PropertyEntry where TEntity : class {
 {
-        public PropertyEntry(InternalEntityEntry internalEntry, IProperty property);

-        public PropertyEntry(InternalEntityEntry internalEntry, string name);

-        public virtual new TProperty CurrentValue { get; set; }

-        public virtual new EntityEntry<TEntity> EntityEntry { get; }

-        public virtual new TProperty OriginalValue { get; set; }

-    }
-    public abstract class PropertyValues {
 {
-        protected PropertyValues(InternalEntityEntry internalEntry);

-        public virtual IEntityType EntityType { get; }

-        protected virtual InternalEntityEntry InternalEntry { get; }

-        public abstract IReadOnlyList<IProperty> Properties { get; }

-        public abstract object this[IProperty property] { get; set; }

-        public abstract object this[string propertyName] { get; set; }

-        public abstract PropertyValues Clone();

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public abstract TValue GetValue<TValue>(IProperty property);

-        public abstract TValue GetValue<TValue>(string propertyName);

-        public abstract void SetValues(PropertyValues propertyValues);

-        public virtual void SetValues(IDictionary<string, object> values);

-        public abstract void SetValues(object obj);

-        public abstract object ToObject();

-        public override string ToString();

-    }
-    public class ReferenceEntry : NavigationEntry {
 {
-        public ReferenceEntry(InternalEntityEntry internalEntry, INavigation navigation);

-        public ReferenceEntry(InternalEntityEntry internalEntry, string name);

-        public virtual EntityEntry TargetEntry { get; }

-        protected virtual InternalEntityEntry GetTargetEntry();

-    }
-    public class ReferenceEntry<TEntity, TProperty> : ReferenceEntry where TEntity : class where TProperty : class {
 {
-        public ReferenceEntry(InternalEntityEntry internalEntry, INavigation navigation);

-        public ReferenceEntry(InternalEntityEntry internalEntry, string name);

-        public virtual new TProperty CurrentValue { get; set; }

-        public virtual new EntityEntry<TEntity> EntityEntry { get; }

-        public virtual new EntityEntry<TProperty> TargetEntry { get; }

-        public virtual new IQueryable<TProperty> Query();

-    }
-    public abstract class ValueComparer : IEqualityComparer {
 {
-        protected ValueComparer(LambdaExpression equalsExpression, LambdaExpression hashCodeExpression, LambdaExpression snapshotExpression);

-        public virtual LambdaExpression EqualsExpression { get; }

-        public virtual LambdaExpression HashCodeExpression { get; }

-        public virtual LambdaExpression SnapshotExpression { get; }

-        public abstract Type Type { get; }

-        public abstract bool Equals(object left, object right);

-        public virtual Expression ExtractEqualsBody(Expression leftExpression, Expression rightExpression);

-        public virtual Expression ExtractHashCodeBody(Expression expression);

-        public virtual Expression ExtractSnapshotBody(Expression expression);

-        public abstract int GetHashCode(object instance);

-        public abstract object Snapshot(object instance);

-    }
-    public class ValueComparer<T> : ValueComparer, IEqualityComparer<T> {
 {
-        public ValueComparer(bool favorStructuralComparisons);

-        public ValueComparer(Expression<Func<T, T, bool>> equalsExpression, Expression<Func<T, int>> hashCodeExpression);

-        public ValueComparer(Expression<Func<T, T, bool>> equalsExpression, Expression<Func<T, int>> hashCodeExpression, Expression<Func<T, T>> snapshotExpression);

-        public virtual new Expression<Func<T, T, bool>> EqualsExpression { get; }

-        public virtual new Expression<Func<T, int>> HashCodeExpression { get; }

-        public virtual new Expression<Func<T, T>> SnapshotExpression { get; }

-        public override Type Type { get; }

-        protected static Expression<Func<T, T, bool>> CreateDefaultEqualsExpression();

-        protected static Expression<Func<T, int>> CreateDefaultHashCodeExpression(bool favorStructuralComparisons);

-        public override bool Equals(object left, object right);

-        public virtual bool Equals(T left, T right);

-        public override int GetHashCode(object instance);

-        public virtual int GetHashCode(T instance);

-        public override object Snapshot(object instance);

-        public virtual T Snapshot(T instance);

-    }
-}
```

