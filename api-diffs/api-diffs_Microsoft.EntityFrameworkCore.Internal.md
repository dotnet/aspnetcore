# Microsoft.EntityFrameworkCore.Internal

``` diff
-namespace Microsoft.EntityFrameworkCore.Internal {
 {
-    public static class AbstractionsStrings {
 {
-        public static string ArgumentIsEmpty(object argumentName);

-        public static string CollectionArgumentIsEmpty(object argumentName);

-    }
-    public sealed class AsyncLock {
 {
-        public AsyncLock();

-        public AsyncLock.Releaser Lock();

-        public Task<AsyncLock.Releaser> LockAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public readonly struct Releaser : IDisposable {
 {
-            public void Dispose();

-        }
-    }
-    public class ConcurrencyDetector : IConcurrencyDetector, IDisposable {
 {
-        public ConcurrencyDetector();

-        public virtual void Dispose();

-        public virtual IDisposable EnterCriticalSection();

-        public virtual Task<IDisposable> EnterCriticalSectionAsync(CancellationToken cancellationToken);

-    }
-    public static class CoreLoggerExtensions {
 {
-        public static void CascadeDelete(this IDiagnosticsLogger<DbLoggerCategory.Update> diagnostics, InternalEntityEntry childEntry, InternalEntityEntry parentEntry, EntityState state);

-        public static void CascadeDeleteOrphan(this IDiagnosticsLogger<DbLoggerCategory.Update> diagnostics, InternalEntityEntry childEntry, IEntityType parentEntityType, EntityState state);

-        public static void CascadeDeleteOrphanSensitive(this IDiagnosticsLogger<DbLoggerCategory.Update> diagnostics, InternalEntityEntry childEntry, IEntityType parentEntityType, EntityState state);

-        public static void CascadeDeleteSensitive(this IDiagnosticsLogger<DbLoggerCategory.Update> diagnostics, InternalEntityEntry childEntry, InternalEntityEntry parentEntry, EntityState state);

-        public static void CollectionChangeDetected(this IDiagnosticsLogger<DbLoggerCategory.ChangeTracking> diagnostics, InternalEntityEntry internalEntityEntry, INavigation navigation, ISet<object> added, ISet<object> removed);

-        public static void CollectionChangeDetectedSensitive(this IDiagnosticsLogger<DbLoggerCategory.ChangeTracking> diagnostics, InternalEntityEntry internalEntityEntry, INavigation navigation, ISet<object> added, ISet<object> removed);

-        public static void ConflictingForeignKeyAttributesOnNavigationAndPropertyWarning(this IDiagnosticsLogger<DbLoggerCategory.Model> diagnostics, INavigation navigation, MemberInfo property);

-        public static void ConflictingShadowForeignKeysWarning(this IDiagnosticsLogger<DbLoggerCategory.Model> diagnostics, IForeignKey foreignKey);

-        public static void ContextDisposed(this IDiagnosticsLogger<DbLoggerCategory.Infrastructure> diagnostics, DbContext context);

-        public static void ContextInitialized(this IDiagnosticsLogger<DbLoggerCategory.Infrastructure> diagnostics, DbContext context, DbContextOptions contextOptions);

-        public static void DetachedLazyLoadingWarning(this IDiagnosticsLogger<DbLoggerCategory.Infrastructure> diagnostics, DbContext context, object entityType, string navigationName);

-        public static void DetectChangesCompleted(this IDiagnosticsLogger<DbLoggerCategory.ChangeTracking> diagnostics, DbContext context);

-        public static void DetectChangesStarting(this IDiagnosticsLogger<DbLoggerCategory.ChangeTracking> diagnostics, DbContext context);

-        public static void DuplicateDependentEntityTypeInstanceWarning(this IDiagnosticsLogger<DbLoggerCategory.Update> diagnostics, IEntityType dependent1, IEntityType dependent2);

-        public static void ExecutionStrategyRetrying(this IDiagnosticsLogger<DbLoggerCategory.Infrastructure> diagnostics, IReadOnlyList<Exception> exceptionsEncountered, TimeSpan delay, bool async);

-        public static void FirstWithoutOrderByAndFilterWarning(this IDiagnosticsLogger<DbLoggerCategory.Query> diagnostics, QueryModel queryModel);

-        public static void ForeignKeyAttributesOnBothNavigationsWarning(this IDiagnosticsLogger<DbLoggerCategory.Model> diagnostics, INavigation firstNavigation, INavigation secondNavigation);

-        public static void ForeignKeyAttributesOnBothPropertiesWarning(this IDiagnosticsLogger<DbLoggerCategory.Model> diagnostics, INavigation firstNavigation, INavigation secondNavigation, MemberInfo firstProperty, MemberInfo secondProperty);

-        public static void ForeignKeyChangeDetected(this IDiagnosticsLogger<DbLoggerCategory.ChangeTracking> diagnostics, InternalEntityEntry internalEntityEntry, IProperty property, object oldValue, object newValue);

-        public static void ForeignKeyChangeDetectedSensitive(this IDiagnosticsLogger<DbLoggerCategory.ChangeTracking> diagnostics, InternalEntityEntry internalEntityEntry, IProperty property, object oldValue, object newValue);

-        public static void IncludeIgnoredWarning(this IDiagnosticsLogger<DbLoggerCategory.Query> diagnostics, IncludeResultOperator includeResultOperator);

-        public static void IncompatibleMatchingForeignKeyProperties(this IDiagnosticsLogger<DbLoggerCategory.Model> diagnostics, IReadOnlyList<IPropertyBase> foreignKeyProperties, IReadOnlyList<IPropertyBase> principalKeyProperties);

-        public static void LazyLoadOnDisposedContextWarning(this IDiagnosticsLogger<DbLoggerCategory.Infrastructure> diagnostics, DbContext context, object entityType, string navigationName);

-        public static void ManyServiceProvidersCreatedWarning(this IDiagnosticsLogger<DbLoggerCategory.Infrastructure> diagnostics, ICollection<IServiceProvider> serviceProviders);

-        public static void MultipleInversePropertiesSameTargetWarning(this IDiagnosticsLogger<DbLoggerCategory.Model> diagnostics, IEnumerable<Tuple<MemberInfo, Type>> conflictingNavigations, MemberInfo inverseNavigation, Type targetType);

-        public static void MultipleNavigationProperties(this IDiagnosticsLogger<DbLoggerCategory.Model> diagnostics, IEnumerable<Tuple<MemberInfo, Type>> firstPropertyCollection, IEnumerable<Tuple<MemberInfo, Type>> secondPropertyCollection);

-        public static void MultiplePrimaryKeyCandidates(this IDiagnosticsLogger<DbLoggerCategory.Model> diagnostics, IProperty firstProperty, IProperty secondProperty);

-        public static void NavigationIncluded(this IDiagnosticsLogger<DbLoggerCategory.Query> diagnostics, IncludeResultOperator includeResultOperator);

-        public static void NavigationLazyLoading(this IDiagnosticsLogger<DbLoggerCategory.Infrastructure> diagnostics, DbContext context, object entityType, string navigationName);

-        public static void NonDefiningInverseNavigationWarning(this IDiagnosticsLogger<DbLoggerCategory.Model> diagnostics, IEntityType declaringType, MemberInfo navigation, IEntityType targetType, MemberInfo inverseNavigation, MemberInfo definingNavigation);

-        public static void NonOwnershipInverseNavigationWarning(this IDiagnosticsLogger<DbLoggerCategory.Model> diagnostics, IEntityType declaringType, MemberInfo navigation, IEntityType targetType, MemberInfo inverseNavigation, MemberInfo ownershipNavigation);

-        public static void OptimisticConcurrencyException(this IDiagnosticsLogger<DbLoggerCategory.Update> diagnostics, DbContext context, Exception exception);

-        public static void PossibleUnintendedCollectionNavigationNullComparisonWarning(this IDiagnosticsLogger<DbLoggerCategory.Query> diagnostics, IReadOnlyList<IPropertyBase> navigationPath);

-        public static void PossibleUnintendedReferenceComparisonWarning(this IDiagnosticsLogger<DbLoggerCategory.Query> diagnostics, Expression left, Expression right);

-        public static void PropertyChangeDetected(this IDiagnosticsLogger<DbLoggerCategory.ChangeTracking> diagnostics, InternalEntityEntry internalEntityEntry, IProperty property, object oldValue, object newValue);

-        public static void PropertyChangeDetectedSensitive(this IDiagnosticsLogger<DbLoggerCategory.ChangeTracking> diagnostics, InternalEntityEntry internalEntityEntry, IProperty property, object oldValue, object newValue);

-        public static void QueryExecutionPlanned(this IDiagnosticsLogger<DbLoggerCategory.Query> diagnostics, IExpressionPrinter expressionPrinter, Expression queryExecutorExpression);

-        public static void QueryIterationFailed(this IDiagnosticsLogger<DbLoggerCategory.Query> diagnostics, Type contextType, Exception exception);

-        public static void QueryModelCompiling(this IDiagnosticsLogger<DbLoggerCategory.Query> diagnostics, QueryModel queryModel);

-        public static void QueryModelOptimized(this IDiagnosticsLogger<DbLoggerCategory.Query> diagnostics, QueryModel queryModel);

-        public static void RedundantForeignKeyWarning(this IDiagnosticsLogger<DbLoggerCategory.Model> diagnostics, IForeignKey redundantForeignKey);

-        public static void RedundantIndexRemoved(this IDiagnosticsLogger<DbLoggerCategory.Model> diagnostics, IReadOnlyList<IPropertyBase> redundantIndex, IReadOnlyList<IPropertyBase> otherIndex);

-        public static void ReferenceChangeDetected(this IDiagnosticsLogger<DbLoggerCategory.ChangeTracking> diagnostics, InternalEntityEntry internalEntityEntry, INavigation navigation, object oldValue, object newValue);

-        public static void ReferenceChangeDetectedSensitive(this IDiagnosticsLogger<DbLoggerCategory.ChangeTracking> diagnostics, InternalEntityEntry internalEntityEntry, INavigation navigation, object oldValue, object newValue);

-        public static void RequiredAttributeOnBothNavigations(this IDiagnosticsLogger<DbLoggerCategory.Model> diagnostics, INavigation firstNavigation, INavigation secondNavigation);

-        public static void RequiredAttributeOnDependent(this IDiagnosticsLogger<DbLoggerCategory.Model> diagnostics, INavigation navigation);

-        public static void RowLimitingOperationWithoutOrderByWarning(this IDiagnosticsLogger<DbLoggerCategory.Query> diagnostics, QueryModel queryModel);

-        public static void SaveChangesCompleted(this IDiagnosticsLogger<DbLoggerCategory.Update> diagnostics, DbContext context, int entitiesSavedCount);

-        public static void SaveChangesFailed(this IDiagnosticsLogger<DbLoggerCategory.Update> diagnostics, DbContext context, Exception exception);

-        public static void SaveChangesStarting(this IDiagnosticsLogger<DbLoggerCategory.Update> diagnostics, DbContext context);

-        public static void SensitiveDataLoggingEnabledWarning<TLoggerCategory>(this IDiagnosticsLogger<TLoggerCategory> diagnostics) where TLoggerCategory : LoggerCategory<TLoggerCategory>, new();

-        public static void ServiceProviderCreated(this IDiagnosticsLogger<DbLoggerCategory.Infrastructure> diagnostics, IServiceProvider serviceProvider);

-        public static void ServiceProviderDebugInfo(this IDiagnosticsLogger<DbLoggerCategory.Infrastructure> diagnostics, IDictionary<string, string> newDebugInfo, IList<IDictionary<string, string>> cachedDebugInfos);

-        public static void ShadowPropertyCreated(this IDiagnosticsLogger<DbLoggerCategory.Model> diagnostics, IProperty property);

-        public static void StartedTracking(this IDiagnosticsLogger<DbLoggerCategory.ChangeTracking> diagnostics, InternalEntityEntry entry);

-        public static void StartedTrackingSensitive(this IDiagnosticsLogger<DbLoggerCategory.ChangeTracking> diagnostics, InternalEntityEntry entry);

-        public static void StateChanged(this IDiagnosticsLogger<DbLoggerCategory.ChangeTracking> diagnostics, InternalEntityEntry entry, EntityState oldState, EntityState newState);

-        public static void StateChangedSensitive(this IDiagnosticsLogger<DbLoggerCategory.ChangeTracking> diagnostics, InternalEntityEntry entry, EntityState oldState, EntityState newState);

-        public static void ValueGenerated(this IDiagnosticsLogger<DbLoggerCategory.ChangeTracking> diagnostics, InternalEntityEntry entry, IProperty property, object value, bool temporary);

-        public static void ValueGeneratedSensitive(this IDiagnosticsLogger<DbLoggerCategory.ChangeTracking> diagnostics, InternalEntityEntry entry, IProperty property, object value, bool temporary);

-    }
-    public class CoreSingletonOptions : ICoreSingletonOptions, ISingletonOptions {
 {
-        public CoreSingletonOptions();

-        public virtual bool AreDetailedErrorsEnabled { get; private set; }

-        public virtual void Initialize(IDbContextOptions options);

-        public virtual void Validate(IDbContextOptions options);

-    }
-    public static class CoreStrings {
 {
-        public static readonly EventDefinition LogManyServiceProvidersCreated;

-        public static readonly EventDefinition LogSensitiveDataLoggingEnabled;

-        public static readonly EventDefinition LogServiceProviderCreated;

-        public static readonly EventDefinition<Exception> LogOptimisticConcurrencyException;

-        public static readonly EventDefinition<int, int, string, string, string> LogCollectionChangeDetectedSensitive;

-        public static readonly EventDefinition<int, int, string, string> LogCollectionChangeDetected;

-        public static readonly EventDefinition<int, string, Exception> LogExecutionStrategyRetrying;

-        public static readonly EventDefinition<object, object> LogPossibleUnintendedReferenceComparison;

-        public static readonly EventDefinition<string, EntityState, string> LogCascadeDelete;

-        public static readonly EventDefinition<string, EntityState, string> LogCascadeDeleteOrphan;

-        public static readonly EventDefinition<string, int> LogSaveChangesCompleted;

-        public static readonly EventDefinition<string, object, string, string> LogTempValueGeneratedSensitive;

-        public static readonly EventDefinition<string, object, string, string> LogValueGeneratedSensitive;

-        public static readonly EventDefinition<string, string, EntityState, EntityState> LogStateChanged;

-        public static readonly EventDefinition<string, string, EntityState, string, string> LogCascadeDeleteSensitive;

-        public static readonly EventDefinition<string, string, EntityState, string> LogCascadeDeleteOrphanSensitive;

-        public static readonly EventDefinition<string, string, object, object, string> LogForeignKeyChangeDetectedSensitive;

-        public static readonly EventDefinition<string, string, object, object, string> LogPropertyChangeDetectedSensitive;

-        public static readonly EventDefinition<string, string, string, EntityState, EntityState> LogStateChangedSensitive;

-        public static readonly EventDefinition<string, string, string, string, string, string> LogForeignKeyAttributesOnBothProperties;

-        public static readonly EventDefinition<string, string, string, string, string> LogNonDefiningInverseNavigation;

-        public static readonly EventDefinition<string, string, string, string, string> LogNonOwnershipInverseNavigation;

-        public static readonly EventDefinition<string, string, string, string> LogConflictingForeignKeyAttributesOnNavigationAndProperty;

-        public static readonly EventDefinition<string, string, string, string> LogContextInitialized;

-        public static readonly EventDefinition<string, string, string, string> LogForeignKeyAttributesOnBothNavigations;

-        public static readonly EventDefinition<string, string, string, string> LogMultipleNavigationProperties;

-        public static readonly EventDefinition<string, string, string, string> LogRequiredAttributeOnBothNavigations;

-        public static readonly EventDefinition<string, string, string> LogConflictingShadowForeignKeys;

-        public static readonly EventDefinition<string, string, string> LogMultiplePrimaryKeyCandidates;

-        public static readonly EventDefinition<string, string, string> LogRedundantIndexRemoved;

-        public static readonly EventDefinition<string, string, string> LogReferenceChangeDetectedSensitive;

-        public static readonly EventDefinition<string, string, string> LogStartedTrackingSensitive;

-        public static readonly EventDefinition<string, string, string> LogTempValueGenerated;

-        public static readonly EventDefinition<string, string, string> LogValueGenerated;

-        public static readonly EventDefinition<string, string> LogCompilingQueryModel;

-        public static readonly EventDefinition<string, string> LogDetachedLazyLoading;

-        public static readonly EventDefinition<string, string> LogDuplicateDependentEntityTypeInstance;

-        public static readonly EventDefinition<string, string> LogForeignKeyChangeDetected;

-        public static readonly EventDefinition<string, string> LogIncompatibleMatchingForeignKeyProperties;

-        public static readonly EventDefinition<string, string> LogLazyLoadOnDisposedContext;

-        public static readonly EventDefinition<string, string> LogMultipleInversePropertiesSameTarget;

-        public static readonly EventDefinition<string, string> LogNavigationLazyLoading;

-        public static readonly EventDefinition<string, string> LogOptimizedQueryModel;

-        public static readonly EventDefinition<string, string> LogPropertyChangeDetected;

-        public static readonly EventDefinition<string, string> LogRedundantForeignKey;

-        public static readonly EventDefinition<string, string> LogReferenceChangeDetected;

-        public static readonly EventDefinition<string, string> LogRequiredAttributeOnDependent;

-        public static readonly EventDefinition<string, string> LogShadowPropertyCreated;

-        public static readonly EventDefinition<string, string> LogStartedTracking;

-        public static readonly EventDefinition<string> LogContextDisposed;

-        public static readonly EventDefinition<string> LogDetectChangesCompleted;

-        public static readonly EventDefinition<string> LogDetectChangesStarting;

-        public static readonly EventDefinition<string> LogFirstWithoutOrderByAndFilter;

-        public static readonly EventDefinition<string> LogIgnoredInclude;

-        public static readonly EventDefinition<string> LogIncludingNavigation;

-        public static readonly EventDefinition<string> LogPossibleUnintendedCollectionNavigationNullComparison;

-        public static readonly EventDefinition<string> LogQueryExecutionPlanned;

-        public static readonly EventDefinition<string> LogRowLimitingOperationWithoutOrderBy;

-        public static readonly EventDefinition<string> LogSaveChangesStarting;

-        public static readonly EventDefinition<string> LogServiceProviderDebugInfo;

-        public static readonly EventDefinition<Type, string, Exception> LogExceptionDuringQueryIteration;

-        public static readonly EventDefinition<Type, string, Exception> LogExceptionDuringSaveChanges;

-        public static string CanConnectNotImplemented { get; }

-        public static string ConcurrentMethodInvocation { get; }

-        public static string ContextDisposed { get; }

-        public static string ConventionsInfiniteLoop { get; }

-        public static string DataBindingWithIListSource { get; }

-        public static string ErrorInvalidQueryable { get; }

-        public static string ErrorMaterializingValue { get; }

-        public static string ExpressionParameterizationException { get; }

-        public static string HiLoBadBlockSize { get; }

-        public static string InvalidMemberInitBinding { get; }

-        public static string InvalidPoolSize { get; }

-        public static string IQueryableProviderNotAsync { get; }

-        public static string NoEfServices { get; }

-        public static string NoProviderConfigured { get; }

-        public static string PoolingOptionsModified { get; }

-        public static string PropertyMethodInvoked { get; }

-        public static string RecursiveOnConfiguring { get; }

-        public static string RecursiveOnModelCreating { get; }

-        public static string RelationshipCannotBeInverted { get; }

-        public static string ResetNotSupported { get; }

-        public static string StillUsingTypeMapper { get; }

-        public static string TransactionsNotSupported { get; }

-        public static string AbstractLeafEntityType(object entityType);

-        public static string AmbiguousDependentEntity(object entityType, object targetEntryCall);

-        public static string AmbiguousForeignKeyPropertyCandidates(object firstDependentToPrincipalNavigationSpecification, object firstPrincipalToDependentNavigationSpecification, object secondDependentToPrincipalNavigationSpecification, object secondPrincipalToDependentNavigationSpecification, object foreignKeyProperties);

-        public static string AmbiguousOneToOneRelationship(object dependentToPrincipalNavigationSpecification, object principalToDependentNavigationSpecification);

-        public static string AmbiguousOwnedNavigation(object entityType, object otherEntityType);

-        public static string AmbiguousServiceProperty(object property, object serviceType, object entityType);

-        public static string AnnotationNotFound(object annotation);

-        public static string ArgumentIsEmpty(object argumentName);

-        public static string ArgumentPropertyNull(object property, object argument);

-        public static string BadBackingFieldType(object field, object fieldType, object entityType, object property, object propertyType);

-        public static string BadDependencyRegistration(object dependenciesType);

-        public static string BadFilterDerivedType(object filter, object entityType);

-        public static string BadFilterExpression(object filter, object entityType, object clrType);

-        public static string BadValueGeneratorType(object givenType, object expectedType);

-        public static string CannotAccessEntityAsQuery(object queryType);

-        public static string CannotAccessQueryAsEntity(object entityType);

-        public static string CannotBeNullable(object property, object entityType, object propertyType);

-        public static string CannotBeNullablePK(object property, object entityType);

-        public static string CannotCreateValueGenerator(object generatorType);

-        public static string CannotLoadDetached(object navigation, object entityType);

-        public static string CannotMaterializeAbstractType(object entityType);

-        public static string ChangeTrackingInterfaceMissing(object entityType, object changeTrackingStrategy, object notificationInterface);

-        public static string CircularDependency(object cycle);

-        public static string CircularInheritance(object entityType, object baseEntityType);

-        public static string ClashingNonOwnedEntityType(object entityType);

-        public static string ClashingNonWeakEntityType(object entityType);

-        public static string ClashingWeakEntityType(object entityType);

-        public static string ClrPropertyOnShadowEntity(object property, object entityType);

-        public static string CollectionArgumentIsEmpty(object argumentName);

-        public static string CollectionIsReference(object property, object entityType, object CollectionMethod, object ReferenceMethod);

-        public static string ComparerPropertyMismatch(object type, object entityType, object propertyName, object propertyType);

-        public static string CompositeFkOnProperty(object navigation, object entityType);

-        public static string CompositePKWithDataAnnotation(object entityType);

-        public static string ConflictingForeignKeyAttributes(object propertyList, object entityType);

-        public static string ConflictingPropertyOrNavigation(object member, object entityType, object conflictingEntityType);

-        public static string ConflictingRelationshipNavigation(object newPrincipalEntityType, object newPrincipalNavigation, object newDependentEntityType, object newDependentNavigation, object existingPrincipalEntityType, object existingPrincipalNavigation, object existingDependentEntityType, object existingDependentNavigation);

-        public static string ConstructorBindingFailed(object failedBinds, object parameters);

-        public static string ConstructorConflict(object firstConstructor, object secondConstructor);

-        public static string ConstructorNotFound(object entityType, object constructors);

-        public static string ConverterBadType(object converter, object type, object allowed);

-        public static string ConverterCloneNotImplemented(object mapping);

-        public static string ConverterPropertyMismatch(object converterType, object entityType, object propertyName, object propertyType);

-        public static string ConvertersCannotBeComposed(object typeOneIn, object typeOneOut, object typeTwoIn, object typeTwoOut);

-        public static string CustomMetadata(object method, object interfaceType, object concreteType);

-        public static string DatabaseGeneratedNull(object property, object entityType);

-        public static string DbContextMissingConstructor(object contextType);

-        public static string DependentEntityTypeNotInRelationship(object dependentEntityType, object principalEntityType, object entityType);

-        public static string DerivedEntityCannotHaveKeys(object entityType);

-        public static string DerivedEntityTypeKey(object derivedType, object rootType);

-        public static string DerivedQueryTypeDefiningQuery(object queryType, object baseType);

-        public static string DuplicateAnnotation(object annotation);

-        public static string DuplicateEntityType(object entityType);

-        public static string DuplicateForeignKey(object foreignKey, object entityType, object duplicateEntityType, object key, object principalType);

-        public static string DuplicateIndex(object index, object entityType, object duplicateEntityType);

-        public static string DuplicateKey(object key, object entityType, object duplicateEntityType);

-        public static string DuplicateNavigationsOnBase(object entityType, object baseType, object navigations);

-        public static string DuplicatePropertiesOnBase(object entityType, object baseType, object derivedPropertyType, object derivedProperty, object basePropertyType, object baseProperty);

-        public static string DuplicatePropertyInList(object propertyList, object property);

-        public static string DuplicateQueryType(object queryType);

-        public static string DuplicateServicePropertyType(object property, object serviceType, object entityType, object duplicateName, object duplicateEntityType);

-        public static string EntityRequiresKey(object entityType);

-        public static string EntityTypeInUseByDerived(object entityType, object derivedEntityType);

-        public static string EntityTypeInUseByForeignKey(object entityType, object referencedEntityType, object foreignKey);

-        public static string EntityTypeInUseByReferencingForeignKey(object entityType, object foreignKey, object referencingEntityType);

-        public static string EntityTypeModelMismatch(object firstEntityType, object secondEntityType);

-        public static string EntityTypeNotFound(object entityType);

-        public static string EntityTypeNotInRelationship(object entityType, object dependentType, object principalType);

-        public static string EntityTypeNotInRelationshipStrict(object entityType, object dependentType, object principalType);

-        public static string EntityTypesNotInRelationship(object invalidDependentType, object invalidPrincipalType, object dependentType, object principalType);

-        public static string ErrorMaterializingProperty(object entityType, object property);

-        public static string ErrorMaterializingPropertyInvalidCast(object entityType, object property, object expectedType, object actualType);

-        public static string ErrorMaterializingPropertyNullReference(object entityType, object property, object expectedType);

-        public static string ErrorMaterializingValueInvalidCast(object expectedType, object actualType);

-        public static string ErrorMaterializingValueNullReference(object expectedType);

-        public static string ExecutionStrategyExistingTransaction(object strategy, object getExecutionStrategyMethod);

-        public static string ExpressionParameterizationExceptionSensitive(object expression);

-        public static string FindNotCompositeKey(object entityType, object valuesCount);

-        public static string FindValueCountMismatch(object entityType, object propertiesCount, object valuesCount);

-        public static string FindValueTypeMismatch(object index, object entityType, object valueType, object propertyType);

-        public static string FkAttributeOnNonUniquePrincipal(object navigation, object principalType, object dependentType);

-        public static string FkAttributeOnPropertyNavigationMismatch(object property, object navigation, object entityType);

-        public static string ForeignKeyCannotBeOptional(object foreignKey, object entityType);

-        public static string ForeignKeyCountMismatch(object foreignKey, object dependentType, object principalKey, object principalType);

-        public static string ForeignKeyPropertiesWrongEntity(object foreignKey, object entityType);

-        public static string ForeignKeyPropertyInKey(object property, object entityType, object key, object baseEntityType);

-        public static string ForeignKeyReferencedEntityKeyMismatch(object principalKey, object principalEntityType);

-        public static string ForeignKeySelfReferencingDependentEntityType(object dependentType);

-        public static string ForeignKeyTypeMismatch(object foreignKey, object dependentType, object principalKey, object principalType);

-        public static string GraphDoesNotContainVertex(object vertex);

-        public static string IdentifyingRelationshipCycle(object entityType);

-        public static string IdentityConflict(object entityType, object keyProperties);

-        public static string IdentityConflictOwned(object entityType, object keyProperties);

-        public static string IdentityConflictOwnedSensitive(object entityType, object keyValue);

-        public static string IdentityConflictSensitive(object entityType, object keyValue);

-        public static string ImplementationTypeRequired(object service);

-        public static string IncludeBadNavigation(object property, object entityType);

-        public static string IncludeNotSpecifiedDirectlyOnEntityType(object include, object invalidNavigation);

-        public static string IncompatiblePrincipalEntry(object foreignKey, object dependentEntityType, object foundPrincipalEntityType, object principalEntityType);

-        public static string IncompatiblePrincipalEntrySensitive(object foreignKeyValues, object dependentEntityType, object keyValue, object foundPrincipalEntityType, object principalEntityType);

-        public static string InconsistentInheritance(object entityType, object baseEntityType);

-        public static string InconsistentOwnership(object ownedEntityType, object nonOwnedEntityType);

-        public static string IndexPropertiesWrongEntity(object index, object entityType);

-        public static string InheritedPropertyCannotBeIgnored(object property, object entityType, object baseEntityType);

-        public static string InterfacePropertyNotAdded(object entityType, object navigation, object propertyType);

-        public static string IntraHierarchicalAmbiguousTargetEntityType(object entityType, object foreignKey, object principalEntityType, object dependentEntityType);

-        public static string InvalidAlternateKeyValue(object entityType, object keyProperty);

-        public static string InvalidEntityType(object type);

-        public static string InvalidEnumValue(object argumentName, object enumType);

-        public static string InvalidIncludeLambdaExpression(object methodName, object includeLambdaExpression);

-        public static string InvalidKeyValue(object entityType, object keyProperty);

-        public static string InvalidNavigationWithInverseProperty(object property, object entityType, object referencedProperty, object referencedEntityType);

-        public static string InvalidPropertiesExpression(object expression);

-        public static string InvalidPropertyExpression(object expression);

-        public static string InvalidPropertyListOnNavigation(object navigation, object entityType);

-        public static string InvalidRelationshipUsingDataAnnotations(object navigation, object entityType, object referencedNavigation, object referencedEntityType);

-        public static string InvalidReplaceService(object replaceService, object useInternalServiceProvider);

-        public static string InvalidSetType(object typeName);

-        public static string InvalidSetTypeEntity(object typeName);

-        public static string InvalidSetTypeQuery(object typeName);

-        public static string InvalidSetTypeWeak(object typeName);

-        public static string InvalidType(object property, object entityType, object valueType, object propertyType);

-        public static string InvalidUseService(object useService, object useInternalServiceProvider, object service);

-        public static string InvalidValueGeneratorFactoryProperty(object factory, object property, object entityType);

-        public static string InversePropertyMismatch(object navigation, object entityType, object referencedNavigation, object referencedEntityType);

-        public static string InverseToOwnedType(object principalEntityType, object navigation, object ownedType, object ownerType);

-        public static string IQueryableNotAsync(object genericParameter);

-        public static string KeyAttributeOnDerivedEntity(object derivedType, object property);

-        public static string KeyInUse(object key, object entityType, object dependentType);

-        public static string KeyPropertiesWrongEntity(object key, object entityType);

-        public static string KeyPropertyCannotBeNullable(object property, object entityType, object key);

-        public static string KeyPropertyInForeignKey(object property, object entityType);

-        public static string KeyPropertyMustBeReadOnly(object property, object entityType);

-        public static string KeyReadOnly(object property, object entityType);

-        public static string LiteralGenerationNotSupported(object type);

-        public static string MissingBackingField(object field, object property, object entityType);

-        public static string MixedQueryEntityTypeInheritance(object baseType, object derivedType);

-        public static string MultipleEntries(object entityType);

-        public static string MultipleNavigationsSameFk(object entityType, object propertyList);

-        public static string MultipleOwnerships(object entityType);

-        public static string MultipleProvidersConfigured(object storeNames);

-        public static string MutableKeyProperty(object keyProperty);

-        public static string NavigationArray(object navigation, object entityType, object foundType);

-        public static string NavigationBadType(object navigation, object entityType, object foundType, object targetType);

-        public static string NavigationCannotCreateType(object navigation, object entityType, object foundType);

-        public static string NavigationCollectionWrongClrType(object navigation, object entityType, object clrType, object targetType);

-        public static string NavigationForWrongForeignKey(object navigation, object entityType, object targetFk, object actualFk);

-        public static string NavigationIsProperty(object property, object entityType, object ReferenceMethod, object CollectionMethod, object PropertyMethod);

-        public static string NavigationNoSetter(object navigation, object entityType);

-        public static string NavigationNotAdded(object entityType, object navigation, object propertyType);

-        public static string NavigationSingleWrongClrType(object navigation, object entityType, object clrType, object targetType);

-        public static string NavigationToQueryType(object navigation, object queryType);

-        public static string NavigationToShadowEntity(object navigation, object entityType, object targetType);

-        public static string NoBackingField(object property, object entity, object pam);

-        public static string NoBackingFieldLazyLoading(object property, object entity);

-        public static string NoClrNavigation(object navigation, object entityType);

-        public static string NoClrType(object entityType);

-        public static string NoDefiningNavigation(object navigation, object entityType, object definingEntityType);

-        public static string NoFieldOrGetter(object property, object entity);

-        public static string NoFieldOrSetter(object property, object entity);

-        public static string NoGetter(object property, object entity, object pam);

-        public static string NonClrBaseType(object entityType, object baseEntityType);

-        public static string NonDefiningOwnership(object ownershipNavigation, object definingNavigation, object entityType);

-        public static string NonGenericOptions(object contextType);

-        public static string NonNotifyingCollection(object navigation, object entityType, object changeTrackingStrategy);

-        public static string NonShadowBaseType(object entityType, object baseEntityType);

-        public static string NoParameterlessConstructor(object entityType);

-        public static string NoProperty(object field, object entity, object pam);

-        public static string NoPropertyType(object property, object entityType);

-        public static string NoProviderConfiguredFailedToResolveService(object service);

-        public static string NoSetter(object property, object entity, object pam);

-        public static string NotAnEFService(object service);

-        public static string NotAssignableClrBaseType(object entityType, object baseEntityType, object clrType, object baseClrType);

-        public static string NoValueGenerator(object property, object entityType, object propertyType);

-        public static string NullableKey(object entityType, object property);

-        public static string OptionsExtensionNotFound(object optionsExtension);

-        public static string OriginalValueNotTracked(object property, object entityType);

-        public static string OwnedDerivedType(object entityType);

-        public static string OwnerlessOwnedType(object ownedType);

-        public static string PoolingContextCtorError(object contextType);

-        public static string PrincipalEntityTypeNotInRelationship(object dependentEntityType, object principalEntityType, object entityType);

-        public static string PrincipalOwnedType(object referencingEntityTypeOrNavigation, object referencedEntityTypeOrNavigation, object ownedType);

-        public static string PropertyCalledOnNavigation(object property, object entityType);

-        public static string PropertyConceptualNull(object property, object entityType);

-        public static string PropertyConceptualNullSensitive(object property, object entityType, object keyValue);

-        public static string PropertyDoesNotBelong(object property, object entityType, object expectedType);

-        public static string PropertyInUseForeignKey(object property, object entityType, object foreignKey, object foreignKeyType);

-        public static string PropertyInUseIndex(object property, object entityType, object index, object indexType);

-        public static string PropertyInUseKey(object property, object entityType, object key);

-        public static string PropertyIsNavigation(object property, object entityType, object PropertyMethod, object ReferenceMethod, object CollectionMethod);

-        public static string PropertyNotAdded(object entityType, object property, object propertyType);

-        public static string PropertyNotFound(object property, object entityType);

-        public static string PropertyNotMapped(object entityType, object property, object propertyType);

-        public static string PropertyReadOnlyAfterSave(object property, object entityType);

-        public static string PropertyReadOnlyBeforeSave(object property, object entityType);

-        public static string PropertyWrongClrType(object property, object entityType, object clrType, object propertyType);

-        public static string PropertyWrongEntityClrType(object property, object entityType, object clrType);

-        public static string QueryTypeCannotBePrincipal(object queryType);

-        public static string QueryTypeNotValid(object type);

-        public static string QueryTypeWithKey(object key, object queryType);

-        public static string ReadonlyField(object field, object entity);

-        public static string ReferencedShadowKey(object referencingEntityTypeOrNavigation, object referencedEntityTypeOrNavigation, object foreignKeyPropertiesWithTypes, object primaryKeyPropertiesWithTypes);

-        public static string ReferenceIsCollection(object property, object entityType, object ReferenceMethod, object CollectionMethod);

-        public static string ReferenceMustBeLoaded(object navigation, object entityType);

-        public static string RelationshipConceptualNull(object firstType, object secondType);

-        public static string RelationshipConceptualNullSensitive(object firstType, object secondType, object secondKeyValue);

-        public static string RetryLimitExceeded(object retryLimit, object strategy);

-        public static string SeedDatumDefaultValue(object entityType, object property, object defaultValue);

-        public static string SeedDatumDerivedType(object entityType, object derivedType);

-        public static string SeedDatumDuplicate(object entityType, object keyProperties);

-        public static string SeedDatumDuplicateSensitive(object entityType, object keyValue);

-        public static string SeedDatumIncompatibleValue(object entityType, object property, object type);

-        public static string SeedDatumIncompatibleValueSensitive(object entityType, object value, object property, object type);

-        public static string SeedDatumMissingValue(object entityType, object property);

-        public static string SeedDatumNavigation(object entityType, object navigation, object relatedEntityType, object foreignKeyProperties);

-        public static string SeedDatumNavigationSensitive(object entityType, object keyValue, object navigation, object relatedEntityType, object foreignKeyProperties);

-        public static string SeedDatumSignedNumericValue(object entityType, object property);

-        public static string SelfReferencingNavigationWithInverseProperty(object property, object entityType, object referencedProperty, object referencedEntityType);

-        public static string ServiceProviderConfigAdded(object key);

-        public static string ServiceProviderConfigChanged(object key);

-        public static string ServiceProviderConfigRemoved(object key);

-        public static string ShadowEntity(object entityType);

-        public static string SingletonOptionChanged(object optionCall, object useInternalServiceProvider);

-        public static string SingletonRequired(object scope, object service);

-        public static string TempValue(object property, object entityType);

-        public static string TempValuePersists(object property, object entityType, object state);

-        public static string UntrackedDependentEntity(object entityType, object targetEntryCall);

-        public static string ValueCannotBeNull(object property, object entityType, object propertyType);

-        public static string ValueGenWithConversion(object entityType, object property, object converter);

-        public static string WarningAsErrorTemplate(object eventName, object message, object eventId);

-        public static string WeakBaseType(object entityType, object baseType);

-        public static string WeakDerivedType(object entityType);

-        public static string WrongGenericPropertyType(object property, object entityType, object actualType, object genericType);

-        public static string WrongStateManager(object entityType);

-    }
-    public sealed class CurrentDbContext : ICurrentDbContext {
 {
-        public CurrentDbContext(DbContext context);

-        public DbContext Context { get; }

-    }
-    public sealed class DbContextDependencies : IDbContextDependencies {
 {
-        public DbContextDependencies(ICurrentDbContext currentContext, IChangeDetector changeDetector, IDbSetSource setSource, IDbQuerySource querySource, IEntityFinderSource entityFinderSource, IEntityGraphAttacher entityGraphAttacher, IModel model, IAsyncQueryProvider queryProvider, IStateManager stateManager, IDiagnosticsLogger<DbLoggerCategory.Update> updateLogger, IDiagnosticsLogger<DbLoggerCategory.Infrastructure> infrastuctureLogger);

-        public IChangeDetector ChangeDetector { get; }

-        public IEntityFinderFactory EntityFinderFactory { get; }

-        public IEntityGraphAttacher EntityGraphAttacher { get; }

-        public IDiagnosticsLogger<DbLoggerCategory.Infrastructure> InfrastructureLogger { get; }

-        public IModel Model { get; }

-        public IAsyncQueryProvider QueryProvider { get; }

-        public IDbQuerySource QuerySource { get; }

-        public IDbSetSource SetSource { get; }

-        public IStateManager StateManager { get; }

-        public IDiagnosticsLogger<DbLoggerCategory.Update> UpdateLogger { get; }

-    }
-    public static class DbContextDependenciesExtensions {
 {
-        public static IDbContextDependencies GetDependencies(this ICurrentDbContext context);

-        public static IDbContextDependencies GetDependencies(this IDbContextDependencies context);

-    }
-    public static class DbContextOptionsExtensions {
 {
-        public static string BuildOptionsFragment(this IDbContextOptions contextOptions);

-    }
-    public class DbContextPool<TContext> : IDbContextPool, IDisposable where TContext : DbContext {
 {
-        public DbContextPool(DbContextOptions options);

-        public virtual void Dispose();

-        DbContext Microsoft.EntityFrameworkCore.Internal.IDbContextPool.Rent();

-        bool Microsoft.EntityFrameworkCore.Internal.IDbContextPool.Return(DbContext context);

-        public virtual TContext Rent();

-        public virtual bool Return(TContext context);

-        public sealed class Lease : IDisposable {
 {
-            public Lease(DbContextPool<TContext> contextPool);

-            public TContext Context { get; private set; }

-            void System.IDisposable.Dispose();

-        }
-    }
-    public class DbContextPoolConfigurationSnapshot {
 {
-        public DbContextPoolConfigurationSnapshot(Nullable<bool> autoDetectChangesEnabled, Nullable<QueryTrackingBehavior> queryTrackingBehavior, Nullable<bool> autoTransactionsEnabled, Nullable<bool> lazyLoadingEnabled);

-        public virtual Nullable<bool> AutoDetectChangesEnabled { get; }

-        public virtual Nullable<bool> AutoTransactionsEnabled { get; }

-        public virtual Nullable<bool> LazyLoadingEnabled { get; }

-        public virtual Nullable<QueryTrackingBehavior> QueryTrackingBehavior { get; }

-    }
-    public class DbContextServices : IDbContextServices {
 {
-        public DbContextServices();

-        public virtual IDbContextOptions ContextOptions { get; }

-        public virtual ICurrentDbContext CurrentContext { get; }

-        public virtual IServiceProvider InternalServiceProvider { get; }

-        public virtual IModel Model { get; }

-        public virtual IDbContextServices Initialize(IServiceProvider scopedProvider, IDbContextOptions contextOptions, DbContext context);

-    }
-    public class DbSetFinder : IDbSetFinder {
 {
-        public DbSetFinder();

-        public virtual IReadOnlyList<DbSetProperty> FindSets(DbContext context);

-    }
-    public static class DbSetFinderExtensions {
 {
-        public static IDictionary<Type, DbSetProperty> CreateClrTypeDbSetMapping(this IDbSetFinder setFinder, DbContext context);

-    }
-    public class DbSetInitializer : IDbSetInitializer {
 {
-        public DbSetInitializer(IDbSetFinder setFinder, IDbSetSource setSource, IDbQuerySource querySource);

-        public virtual void InitializeSets(DbContext context);

-    }
-    public readonly struct DbSetProperty {
 {
-        public DbSetProperty(string name, Type clrType, IClrPropertySetter setter, bool queryType = false);

-        public Type ClrType { get; }

-        public bool IsQueryType { get; }

-        public string Name { get; }

-        public IClrPropertySetter Setter { get; }

-    }
-    public class DbSetSource : IDbQuerySource, IDbSetSource {
 {
-        public DbSetSource();

-        public virtual object Create(DbContext context, Type type);

-        public virtual object CreateQuery(DbContext context, Type type);

-    }
-    public static class DesignStrings {
 {
-        public static string DestructiveOperation { get; }

-        public static string Done { get; }

-        public static string FindingBuildWebHost { get; }

-        public static string FindingContextFactories { get; }

-        public static string FindingContexts { get; }

-        public static string FindingReferencedContexts { get; }

-        public static string FindingServiceProvider { get; }

-        public static string ManuallyDeleted { get; }

-        public static string MultipleContexts { get; }

-        public static string NoDesignTimeServices { get; }

-        public static string NoReferencedServices { get; }

-        public static string NoServiceProvider { get; }

-        public static string NoSnapshot { get; }

-        public static string RemovingSnapshot { get; }

-        public static string RevertingSnapshot { get; }

-        public static string SensitiveInformationWarning { get; }

-        public static string SequencesRequireName { get; }

-        public static string BadSequenceType(object sequenceName, object typeName);

-        public static string CannotFindDesignTimeProviderAssemblyAttribute(object attributeName, object runtimeProviderAssemblyName);

-        public static string CannotFindRuntimeProviderAssembly(object assemblyName);

-        public static string CannotFindTypeMappingForColumn(object columnName, object dateType);

-        public static string ContextClassNotValidCSharpIdentifier(object contextClassName);

-        public static string DatabaseDropped(object name);

-        public static string DroppingDatabase(object name);

-        public static string DuplicateMigrationName(object migrationName);

-        public static string ExistingFiles(object outputDirectoryName, object existingFiles);

-        public static string FindingDesignTimeServices(object startupAssembly);

-        public static string FindingProviderServices(object provider);

-        public static string FindingReferencedServices(object startupAssembly);

-        public static string ForceRemoveMigration(object name, object error);

-        public static string ForeignKeyPrincipalEndContainsNullableColumns(object foreignKeyName, object indexName, object columnNames);

-        public static string ForeignKeyScaffoldErrorPrincipalKeyNotFound(object foreignKeyName, object columnsList, object principalEntityType);

-        public static string ForeignKeyScaffoldErrorPrincipalTableNotFound(object foreignKeyName);

-        public static string ForeignKeyScaffoldErrorPrincipalTableScaffoldingError(object foreignKeyName, object principaltableName);

-        public static string ForeignKeyScaffoldErrorPropertyNotFound(object foreignKeyName, object columnNames);

-        public static string ForeignMigrations(object migrationsNamespace);

-        public static string FoundContextFactory(object factory);

-        public static string FoundDbContext(object contextType);

-        public static string InvokeBuildWebHostFailed(object startupClass, object error);

-        public static string LiteralExpressionNotSupported(object expression, object type);

-        public static string MigrationsAssemblyMismatch(object assembly, object migrationsAssembly);

-        public static string MissingPrimaryKey(object tableName);

-        public static string MultipleAnnotationConflict(object annotationName);

-        public static string MultipleContextsWithName(object name);

-        public static string MultipleContextsWithQualifiedName(object name);

-        public static string NoBuildWebHost(object programClass);

-        public static string NoContext(object assembly);

-        public static string NoContextWithName(object name);

-        public static string NoEntryPoint(object startupAssembly);

-        public static string NoLanguageService(object language, object service);

-        public static string NoMigrationFile(object file, object migrationClass);

-        public static string NoMigrationMetadataFile(object file);

-        public static string NonNullableBoooleanColumnHasDefaultConstraint(object columnName);

-        public static string NonRelationalProvider(object provider);

-        public static string NoParameterlessConstructor(object contextType);

-        public static string NoSnapshotFile(object file, object snapshotClass);

-        public static string NotExistDatabase(object name);

-        public static string PrimaryKeyErrorPropertyNotFound(object tableName, object columnNames);

-        public static string ProviderReturnedNullModel(object providerTypeName);

-        public static string ReadOnlyFiles(object outputDirectoryName, object readOnlyFiles);

-        public static string RemovingMigration(object name);

-        public static string ReusingNamespace(object type);

-        public static string ReusingSnapshotName(object name);

-        public static string RevertingMigration(object name);

-        public static string RevertMigration(object name);

-        public static string UnableToGenerateEntityType(object tableName);

-        public static string UnableToScaffoldIndexMissingProperty(object indexName, object columnNames);

-        public static string UnknownLiteral(object literalType);

-        public static string UnknownOperation(object operationType);

-        public static string UnreferencedAssembly(object assembly, object startupProject);

-        public static string UseContext(object name);

-        public static string UsingBuildWebHost(object programClass);

-        public static string UsingDbContextFactory(object factory);

-        public static string UsingDesignTimeServices(object designTimeServices);

-        public static string UsingEnvironment(object environment);

-        public static string UsingProviderServices(object provider);

-        public static string UsingReferencedServices(object referencedAssembly);

-        public static string VersionMismatch(object toolsVersion, object runtimeVersion);

-        public static string WritingMigration(object file);

-        public static string WritingSnapshot(object file);

-    }
-    public class DiagnosticsLogger<TLoggerCategory> : IDiagnosticsLogger<TLoggerCategory> where TLoggerCategory : LoggerCategory<TLoggerCategory>, new() {
 {
-        public DiagnosticsLogger(ILoggerFactory loggerFactory, ILoggingOptions loggingOptions, DiagnosticSource diagnosticSource);

-        public virtual DiagnosticSource DiagnosticSource { get; }

-        public virtual ILogger Logger { get; }

-        public virtual ILoggingOptions Options { get; }

-        public virtual WarningBehavior GetLogBehavior(EventId eventId, LogLevel logLevel);

-        public virtual bool ShouldLogSensitiveData();

-    }
-    public class EntityFinder<TEntity> : IEntityFinder, IEntityFinder<TEntity> where TEntity : class {
 {
-        public EntityFinder(IStateManager stateManager, IDbSetSource setSource, IDbSetCache setCache, IEntityType entityType);

-        public virtual TEntity Find(object[] keyValues);

-        public virtual Task<TEntity> FindAsync(object[] keyValues, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual object[] GetDatabaseValues(InternalEntityEntry entry);

-        public virtual Task<object[]> GetDatabaseValuesAsync(InternalEntityEntry entry, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual void Load(INavigation navigation, InternalEntityEntry entry);

-        public virtual Task LoadAsync(INavigation navigation, InternalEntityEntry entry, CancellationToken cancellationToken = default(CancellationToken));

-        object Microsoft.EntityFrameworkCore.Internal.IEntityFinder.Find(object[] keyValues);

-        Task<object> Microsoft.EntityFrameworkCore.Internal.IEntityFinder.FindAsync(object[] keyValues, CancellationToken cancellationToken);

-        IQueryable Microsoft.EntityFrameworkCore.Internal.IEntityFinder.Query(INavigation navigation, InternalEntityEntry entry);

-        public virtual IQueryable<TEntity> Query(INavigation navigation, InternalEntityEntry entry);

-    }
-    public class EntityFinderFactory : IEntityFinderFactory {
 {
-        public EntityFinderFactory(IEntityFinderSource entityFinderSource, IStateManager stateManager, IDbSetSource setSource, IDbSetCache setCache);

-        public virtual IEntityFinder Create(IEntityType type);

-    }
-    public class EntityFinderSource : IEntityFinderSource {
 {
-        public EntityFinderSource();

-        public virtual IEntityFinder Create(IStateManager stateManager, IDbSetSource setSource, IDbSetCache setCache, IEntityType type);

-    }
-    public static class EnumerableExtensions {
 {
-        public static bool Any(this IEnumerable source);

-        public static IEnumerable<T> Distinct<T>(this IEnumerable<T> source, Func<T, T, bool> comparer) where T : class;

-        public static T FirstOr<T>(this IEnumerable<T> source, Func<T, bool> predicate, T alternate);

-        public static T FirstOr<T>(this IEnumerable<T> source, T alternate);

-        public static int IndexOf<T>(this IEnumerable<T> source, T item);

-        public static int IndexOf<T>(this IEnumerable<T> source, T item, IEqualityComparer<T> comparer);

-        public static string Join(this IEnumerable<object> source, string separator = ", ");

-        public static IOrderedEnumerable<TSource> OrderByOrdinal<TSource>(this IEnumerable<TSource> source, Func<TSource, string> keySelector);

-        public static bool StartsWith<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second);

-        public static bool StructuralSequenceEqual<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second);

-    }
-    public static class ExpressionExtensions {
 {
-        public static BinaryExpression CreateAssignExpression(this Expression left, Expression right);

-        public static Expression CreateKeyAccessExpression(this Expression target, IReadOnlyList<IProperty> properties);

-        public static ConstantExpression GenerateDefaultValueConstantExpression(this Type type);

-        public static IReadOnlyList<PropertyInfo> GetComplexPropertyAccess(this LambdaExpression propertyAccessExpression, string methodName);

-        public static PropertyInfo GetPropertyAccess(this LambdaExpression propertyAccessExpression);

-        public static IReadOnlyList<PropertyInfo> GetPropertyAccessList(this LambdaExpression propertyAccessExpression);

-        public static TExpression GetRootExpression<TExpression>(this Expression expression) where TExpression : Expression;

-        public static bool IsComparisonOperation(this Expression expression);

-        public static bool IsEntityQueryable(this ConstantExpression constantExpression);

-        public static bool IsLogicalOperation(this Expression expression);

-        public static bool IsNullConstantExpression(this Expression expression);

-        public static bool IsNullPropagationCandidate(this ConditionalExpression conditionalExpression, out Expression testExpression, out Expression resultExpression);

-        public static MemberExpression MakeMemberAccess(this Expression expression, MemberInfo member);

-        public static Expression RemoveConvert(this Expression expression);

-        public static Expression RemoveNullConditional(this Expression expression);

-        public static Expression RemoveTypeAs(this Expression expression);

-        public static bool TryGetComplexPropertyAccess(this LambdaExpression propertyAccessExpression, out IReadOnlyList<PropertyInfo> propertyPath);

-        public static IQuerySource TryGetReferencedQuerySource(this Expression expression);

-    }
-    public abstract class Graph<TVertex> {
 {
-        protected Graph();

-        public abstract IEnumerable<TVertex> Vertices { get; }

-        public abstract IEnumerable<TVertex> GetIncomingNeighbours(TVertex to);

-        public abstract IEnumerable<TVertex> GetOutgoingNeighbours(TVertex from);

-        public virtual ISet<TVertex> GetUnreachableVertices(IReadOnlyList<TVertex> roots);

-    }
-    public interface IConcurrencyDetector {
 {
-        IDisposable EnterCriticalSection();

-        Task<IDisposable> EnterCriticalSectionAsync(CancellationToken cancellationToken);

-    }
-    public interface ICurrentDbContext {
 {
-        DbContext Context { get; }

-    }
-    public interface IDbContextDependencies {
 {
-        IChangeDetector ChangeDetector { get; }

-        IEntityFinderFactory EntityFinderFactory { get; }

-        IEntityGraphAttacher EntityGraphAttacher { get; }

-        IDiagnosticsLogger<DbLoggerCategory.Infrastructure> InfrastructureLogger { get; }

-        IModel Model { get; }

-        IAsyncQueryProvider QueryProvider { get; }

-        IDbQuerySource QuerySource { get; }

-        IDbSetSource SetSource { get; }

-        IStateManager StateManager { get; }

-        IDiagnosticsLogger<DbLoggerCategory.Update> UpdateLogger { get; }

-    }
-    public interface IDbContextPool {
 {
-        DbContext Rent();

-        bool Return(DbContext context);

-    }
-    public interface IDbContextPoolable {
 {
-        void ResetState();

-        void Resurrect(DbContextPoolConfigurationSnapshot configurationSnapshot);

-        void SetPool(IDbContextPool contextPool);

-        DbContextPoolConfigurationSnapshot SnapshotConfiguration();

-    }
-    public interface IDbContextServices {
 {
-        IDbContextOptions ContextOptions { get; }

-        ICurrentDbContext CurrentContext { get; }

-        IServiceProvider InternalServiceProvider { get; }

-        IModel Model { get; }

-        IDbContextServices Initialize(IServiceProvider scopedProvider, IDbContextOptions contextOptions, DbContext context);

-    }
-    public interface IDbQueryCache {
 {
-        object GetOrAddQuery(IDbQuerySource source, Type type);

-    }
-    public interface IDbQuerySource {
 {
-        object CreateQuery(DbContext context, Type type);

-    }
-    public interface IDbSetCache {
 {
-        object GetOrAddSet(IDbSetSource source, Type type);

-    }
-    public interface IDbSetFinder {
 {
-        IReadOnlyList<DbSetProperty> FindSets(DbContext context);

-    }
-    public interface IDbSetInitializer {
 {
-        void InitializeSets(DbContext context);

-    }
-    public interface IDbSetSource {
 {
-        object Create(DbContext context, Type type);

-    }
-    public interface IEntityFinder {
 {
-        object Find(object[] keyValues);

-        Task<object> FindAsync(object[] keyValues, CancellationToken cancellationToken = default(CancellationToken));

-        object[] GetDatabaseValues(InternalEntityEntry entry);

-        Task<object[]> GetDatabaseValuesAsync(InternalEntityEntry entry, CancellationToken cancellationToken = default(CancellationToken));

-        void Load(INavigation navigation, InternalEntityEntry entry);

-        Task LoadAsync(INavigation navigation, InternalEntityEntry entry, CancellationToken cancellationToken = default(CancellationToken));

-        IQueryable Query(INavigation navigation, InternalEntityEntry entry);

-    }
-    public interface IEntityFinder<TEntity> : IEntityFinder where TEntity : class {
 {
-        new TEntity Find(object[] keyValues);

-        new Task<TEntity> FindAsync(object[] keyValues, CancellationToken cancellationToken = default(CancellationToken));

-        new IQueryable<TEntity> Query(INavigation navigation, InternalEntityEntry entry);

-    }
-    public interface IEntityFinderFactory {
 {
-        IEntityFinder Create(IEntityType type);

-    }
-    public interface IEntityFinderSource {
 {
-        IEntityFinder Create(IStateManager stateManager, IDbSetSource setSource, IDbSetCache setCache, IEntityType type);

-    }
-    public class IndentedStringBuilder {
 {
-        public IndentedStringBuilder();

-        public IndentedStringBuilder(IndentedStringBuilder from);

-        public virtual int Length { get; }

-        public virtual IndentedStringBuilder Append(object o);

-        public virtual IndentedStringBuilder AppendLine();

-        public virtual IndentedStringBuilder AppendLine(object o);

-        public virtual IndentedStringBuilder AppendLines(object o, bool skipFinalNewline = false);

-        public virtual IndentedStringBuilder Clear();

-        public virtual IndentedStringBuilder DecrementIndent();

-        public virtual void DisconnectCurrentNode();

-        public virtual IndentedStringBuilder IncrementIndent();

-        public virtual IndentedStringBuilder IncrementIndent(bool connectNode);

-        public virtual IDisposable Indent();

-        public virtual void ReconnectCurrentNode();

-        public virtual void SuspendCurrentNode();

-        public override string ToString();

-    }
-    public static class InMemoryLoggerExtensions {
 {
-        public static void ChangesSaved(this IDiagnosticsLogger<DbLoggerCategory.Update> diagnostics, IEnumerable<IUpdateEntry> entries, int rowsAffected);

-        public static void TransactionIgnoredWarning(this IDiagnosticsLogger<DbLoggerCategory.Database.Transaction> diagnostics);

-    }
-    public static class InternalAccessorExtensions {
 {
-        public static TService GetService<TService>(IInfrastructure<IServiceProvider> accessor);

-    }
-    public class InternalDbQuery<TQuery> : DbQuery<TQuery>, IAsyncEnumerableAccessor<TQuery>, IEnumerable, IEnumerable<TQuery>, IInfrastructure<IServiceProvider>, IQueryable, IQueryable<TQuery> where TQuery : class {
 {
-        public InternalDbQuery(DbContext context);

-        IAsyncEnumerable<TQuery> Microsoft.EntityFrameworkCore.Query.Internal.IAsyncEnumerableAccessor<TQuery>.AsyncEnumerable { get; }

-        Type System.Linq.IQueryable.ElementType { get; }

-        Expression System.Linq.IQueryable.Expression { get; }

-        IQueryProvider System.Linq.IQueryable.Provider { get; }

-        IEnumerator<TQuery> System.Collections.Generic.IEnumerable<TQuery>.GetEnumerator();

-        IEnumerator System.Collections.IEnumerable.GetEnumerator();

-    }
-    public class InternalDbSet<TEntity> : DbSet<TEntity>, IAsyncEnumerableAccessor<TEntity>, IEnumerable, IEnumerable<TEntity>, IInfrastructure<IServiceProvider>, IQueryable, IQueryable<TEntity>, IResettableService where TEntity : class {
 {
-        public InternalDbSet(DbContext context);

-        public override LocalView<TEntity> Local { get; }

-        IAsyncEnumerable<TEntity> Microsoft.EntityFrameworkCore.Query.Internal.IAsyncEnumerableAccessor<TEntity>.AsyncEnumerable { get; }

-        Type System.Linq.IQueryable.ElementType { get; }

-        Expression System.Linq.IQueryable.Expression { get; }

-        IQueryProvider System.Linq.IQueryable.Provider { get; }

-        public override EntityEntry<TEntity> Add(TEntity entity);

-        public override Task<EntityEntry<TEntity>> AddAsync(TEntity entity, CancellationToken cancellationToken = default(CancellationToken));

-        public override void AddRange(IEnumerable<TEntity> entities);

-        public override void AddRange(params TEntity[] entities);

-        public override Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default(CancellationToken));

-        public override Task AddRangeAsync(params TEntity[] entities);

-        public override EntityEntry<TEntity> Attach(TEntity entity);

-        public override void AttachRange(IEnumerable<TEntity> entities);

-        public override void AttachRange(params TEntity[] entities);

-        public override TEntity Find(params object[] keyValues);

-        public override Task<TEntity> FindAsync(params object[] keyValues);

-        public override Task<TEntity> FindAsync(object[] keyValues, CancellationToken cancellationToken);

-        void Microsoft.EntityFrameworkCore.Infrastructure.IResettableService.ResetState();

-        public override EntityEntry<TEntity> Remove(TEntity entity);

-        public override void RemoveRange(IEnumerable<TEntity> entities);

-        public override void RemoveRange(params TEntity[] entities);

-        IEnumerator<TEntity> System.Collections.Generic.IEnumerable<TEntity>.GetEnumerator();

-        IEnumerator System.Collections.IEnumerable.GetEnumerator();

-        public override EntityEntry<TEntity> Update(TEntity entity);

-        public override void UpdateRange(IEnumerable<TEntity> entities);

-        public override void UpdateRange(params TEntity[] entities);

-    }
-    public class InternalServiceCollectionMap {
 {
-        public InternalServiceCollectionMap(IServiceCollection serviceCollection);

-        public virtual IServiceCollection ServiceCollection { get; }

-        public virtual InternalServiceCollectionMap AddDependency(Type serviceType, ServiceLifetime lifetime);

-        public virtual InternalServiceCollectionMap AddDependencyScoped<TDependencies>();

-        public virtual InternalServiceCollectionMap AddDependencySingleton<TDependencies>();

-        public virtual void AddNewDescriptor(IList<int> indexes, ServiceDescriptor newDescriptor);

-        public virtual InternalServiceCollectionMap DoPatchInjection<TService>() where TService : class;

-        public virtual IList<int> GetOrCreateDescriptorIndexes(Type serviceType);

-    }
-    public interface IPatchServiceInjectionSite {
 {
-        void InjectServices(IServiceProvider serviceProvider);

-    }
-    public interface IReferenceRoot<T> {
 {
-        void Release(Reference<T> reference);

-        Reference<T> Track(T @object);

-    }
-    public interface IRegisteredServices {
 {
-        ISet<Type> Services { get; }

-    }
-    public interface ISingletonOptionsInitializer {
 {
-        void EnsureInitialized(IServiceProvider serviceProvider, IDbContextOptions options);

-    }
-    public class LazyLoader : IDisposable, ILazyLoader {
 {
-        public LazyLoader(ICurrentDbContext currentContext, IDiagnosticsLogger<DbLoggerCategory.Infrastructure> logger);

-        protected virtual DbContext Context { get; }

-        protected virtual IDiagnosticsLogger<DbLoggerCategory.Infrastructure> Logger { get; }

-        public virtual void Dispose();

-        public virtual void Load(object entity, string navigationName = null);

-        public virtual Task LoadAsync(object entity, CancellationToken cancellationToken = default(CancellationToken), string navigationName = null);

-    }
-    public sealed class LazyRef<T> {
 {
-        public LazyRef(Func<T> initializer);

-        public LazyRef(T value);

-        public bool HasValue { get; }

-        public T Value { get; set; }

-        public void Reset(Func<T> initializer);

-    }
-    public class LoggingOptions : ILoggingOptions, ISingletonOptions {
 {
-        public LoggingOptions();

-        public virtual bool IsSensitiveDataLoggingEnabled { get; private set; }

-        public virtual bool IsSensitiveDataLoggingWarned { get; set; }

-        public virtual WarningsConfiguration WarningsConfiguration { get; private set; }

-        public virtual void Initialize(IDbContextOptions options);

-        public virtual void Validate(IDbContextOptions options);

-    }
-    public static class MethodInfoExtensions {
 {
-        public static string DisplayName(this MethodInfo methodInfo);

-    }
-    public class Multigraph<TVertex, TEdge> : Graph<TVertex> {
 {
-        public Multigraph();

-        public virtual IEnumerable<TEdge> Edges { get; }

-        public override IEnumerable<TVertex> Vertices { get; }

-        public virtual void AddEdge(TVertex from, TVertex to, TEdge edge);

-        public virtual void AddEdges(TVertex from, TVertex to, IEnumerable<TEdge> edges);

-        public virtual void AddVertex(TVertex vertex);

-        public virtual void AddVertices(IEnumerable<TVertex> vertices);

-        public virtual IReadOnlyList<List<TVertex>> BatchingTopologicalSort();

-        public virtual IReadOnlyList<List<TVertex>> BatchingTopologicalSort(Func<IReadOnlyList<Tuple<TVertex, TVertex, IEnumerable<TEdge>>>, string> formatCycle);

-        public virtual IEnumerable<TEdge> GetEdges(TVertex from, TVertex to);

-        public override IEnumerable<TVertex> GetIncomingNeighbours(TVertex to);

-        public override IEnumerable<TVertex> GetOutgoingNeighbours(TVertex from);

-        public virtual IReadOnlyList<TVertex> TopologicalSort();

-        public virtual IReadOnlyList<TVertex> TopologicalSort(Func<IEnumerable<Tuple<TVertex, TVertex, IEnumerable<TEdge>>>, string> formatCycle);

-        public virtual IReadOnlyList<TVertex> TopologicalSort(Func<TVertex, TVertex, IEnumerable<TEdge>, bool> canBreakEdge);

-        public virtual IReadOnlyList<TVertex> TopologicalSort(Func<TVertex, TVertex, IEnumerable<TEdge>, bool> canBreakEdge, Func<IReadOnlyList<Tuple<TVertex, TVertex, IEnumerable<TEdge>>>, string> formatCycle);

-        protected virtual string ToString(TVertex vertex);

-    }
-    public static class NonCapturingLazyInitializer {
 {
-        public static TValue EnsureInitialized<TParam, TValue>(ref TValue target, TParam param, Action<TParam> valueFactory) where TValue : class;

-        public static TValue EnsureInitialized<TParam, TValue>(ref TValue target, TParam param, Func<TParam, TValue> valueFactory) where TValue : class;

-        public static TValue EnsureInitialized<TParam1, TParam2, TValue>(ref TValue target, TParam1 param1, TParam2 param2, Func<TParam1, TParam2, TValue> valueFactory) where TValue : class;

-        public static TValue EnsureInitialized<TValue>(ref TValue target, TValue value) where TValue : class;

-    }
-    public static class ProductInfo {
 {
-        public static string GetVersion();

-    }
-    public class Reference<T> : IDisposable {
 {
-        public Reference(T @object);

-        public Reference(T @object, IReferenceRoot<T> root);

-        public virtual T Object { get; set; }

-        public virtual void Dispose();

-    }
-    public sealed class ReferenceEnumerableEqualityComparer<TEnumerable, TValue> : IEqualityComparer<TEnumerable> where TEnumerable : IEnumerable<TValue> {
 {
-        public ReferenceEnumerableEqualityComparer();

-        public bool Equals(TEnumerable x, TEnumerable y);

-        public int GetHashCode(TEnumerable obj);

-    }
-    public sealed class ReferenceEqualityComparer : IEqualityComparer<object> {
 {
-        public static ReferenceEqualityComparer Instance { get; }

-        bool System.Collections.Generic.IEqualityComparer<System.Object>.Equals(object x, object y);

-        int System.Collections.Generic.IEqualityComparer<System.Object>.GetHashCode(object obj);

-    }
-    public class RegisteredServices : IRegisteredServices {
 {
-        public RegisteredServices(IEnumerable<Type> services);

-        public virtual ISet<Type> Services { get; }

-    }
-    public static class RelationalExpressionExtensions {
 {
-        public static ColumnExpression FindOriginatingColumnExpression(this Expression expression);

-        public static IProperty FindProperty(this Expression expression, Type targetType);

-        public static bool IsSimpleExpression(this Expression expression);

-        public static ColumnReferenceExpression LiftExpressionFromSubquery(this Expression expression, TableExpressionBase table);

-        public static Expression UnwrapAliasExpression(this Expression expression);

-        public static Expression UnwrapNullableExpression(this Expression expression);

-    }
-    public static class RelationalLoggerExtensions {
 {
-        public static void AmbientTransactionEnlisted(this IDiagnosticsLogger<DbLoggerCategory.Database.Transaction> diagnostics, IRelationalConnection connection, Transaction transaction);

-        public static void AmbientTransactionWarning(this IDiagnosticsLogger<DbLoggerCategory.Database.Transaction> diagnostics, IRelationalConnection connection, DateTimeOffset startDate);

-        public static void BatchReadyForExecution(this IDiagnosticsLogger<DbLoggerCategory.Update> diagnostics, IEnumerable<IUpdateEntry> entries, int commandCount);

-        public static void BatchSmallerThanMinBatchSize(this IDiagnosticsLogger<DbLoggerCategory.Update> diagnostics, IEnumerable<IUpdateEntry> entries, int commandCount, int minBatchSize);

-        public static void BoolWithDefaultWarning(this IDiagnosticsLogger<DbLoggerCategory.Model.Validation> diagnostics, IProperty property);

-        public static void CommandError(this IDiagnosticsLogger<DbLoggerCategory.Database.Command> diagnostics, DbCommand command, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, Exception exception, bool async, DateTimeOffset startTime, TimeSpan duration);

-        public static void CommandExecuted(this IDiagnosticsLogger<DbLoggerCategory.Database.Command> diagnostics, DbCommand command, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, object methodResult, bool async, DateTimeOffset startTime, TimeSpan duration);

-        public static void CommandExecuting(this IDiagnosticsLogger<DbLoggerCategory.Database.Command> diagnostics, DbCommand command, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, bool async, DateTimeOffset startTime);

-        public static void ConnectionClosed(this IDiagnosticsLogger<DbLoggerCategory.Database.Connection> diagnostics, IRelationalConnection connection, DateTimeOffset startTime, TimeSpan duration);

-        public static void ConnectionClosing(this IDiagnosticsLogger<DbLoggerCategory.Database.Connection> diagnostics, IRelationalConnection connection, DateTimeOffset startTime);

-        public static void ConnectionError(this IDiagnosticsLogger<DbLoggerCategory.Database.Connection> diagnostics, IRelationalConnection connection, Exception exception, DateTimeOffset startTime, TimeSpan duration, bool async, bool logErrorAsDebug);

-        public static void ConnectionOpened(this IDiagnosticsLogger<DbLoggerCategory.Database.Connection> diagnostics, IRelationalConnection connection, DateTimeOffset startTime, TimeSpan duration, bool async);

-        public static void ConnectionOpening(this IDiagnosticsLogger<DbLoggerCategory.Database.Connection> diagnostics, IRelationalConnection connection, DateTimeOffset startTime, bool async);

-        public static void DataReaderDisposing(this IDiagnosticsLogger<DbLoggerCategory.Database.Command> diagnostics, IRelationalConnection connection, DbCommand command, DbDataReader dataReader, Guid commandId, int recordsAffected, int readCount, DateTimeOffset startTime, TimeSpan duration);

-        public static void ExplicitTransactionEnlisted(this IDiagnosticsLogger<DbLoggerCategory.Database.Transaction> diagnostics, IRelationalConnection connection, Transaction transaction);

-        public static void MigrateUsingConnection(this IDiagnosticsLogger<DbLoggerCategory.Migrations> diagnostics, IMigrator migrator, IRelationalConnection connection);

-        public static void MigrationApplying(this IDiagnosticsLogger<DbLoggerCategory.Migrations> diagnostics, IMigrator migrator, Migration migration);

-        public static void MigrationAttributeMissingWarning(this IDiagnosticsLogger<DbLoggerCategory.Migrations> diagnostics, TypeInfo migrationType);

-        public static void MigrationGeneratingDownScript(this IDiagnosticsLogger<DbLoggerCategory.Migrations> diagnostics, IMigrator migrator, Migration migration, string fromMigration, string toMigration, bool idempotent);

-        public static void MigrationGeneratingUpScript(this IDiagnosticsLogger<DbLoggerCategory.Migrations> diagnostics, IMigrator migrator, Migration migration, string fromMigration, string toMigration, bool idempotent);

-        public static void MigrationReverting(this IDiagnosticsLogger<DbLoggerCategory.Migrations> diagnostics, IMigrator migrator, Migration migration);

-        public static void MigrationsNotApplied(this IDiagnosticsLogger<DbLoggerCategory.Migrations> diagnostics, IMigrator migrator);

-        public static void MigrationsNotFound(this IDiagnosticsLogger<DbLoggerCategory.Migrations> diagnostics, IMigrator migrator, IMigrationsAssembly migrationsAssembly);

-        public static void ModelValidationKeyDefaultValueWarning(this IDiagnosticsLogger<DbLoggerCategory.Model.Validation> diagnostics, IProperty property);

-        public static void QueryClientEvaluationWarning(this IDiagnosticsLogger<DbLoggerCategory.Query> diagnostics, QueryModel queryModel, object queryModelElement);

-        public static void QueryPossibleExceptionWithAggregateOperator(this IDiagnosticsLogger<DbLoggerCategory.Query> diagnostics);

-        public static void QueryPossibleUnintendedUseOfEqualsWarning(this IDiagnosticsLogger<DbLoggerCategory.Query> diagnostics, MethodCallExpression methodCallExpression);

-        public static void TransactionCommitted(this IDiagnosticsLogger<DbLoggerCategory.Database.Transaction> diagnostics, IRelationalConnection connection, DbTransaction transaction, Guid transactionId, DateTimeOffset startTime, TimeSpan duration);

-        public static void TransactionDisposed(this IDiagnosticsLogger<DbLoggerCategory.Database.Transaction> diagnostics, IRelationalConnection connection, DbTransaction transaction, Guid transactionId, DateTimeOffset startDate);

-        public static void TransactionError(this IDiagnosticsLogger<DbLoggerCategory.Database.Transaction> diagnostics, IRelationalConnection connection, DbTransaction transaction, Guid transactionId, string action, Exception exception, DateTimeOffset startTime, TimeSpan duration);

-        public static void TransactionRolledBack(this IDiagnosticsLogger<DbLoggerCategory.Database.Transaction> diagnostics, IRelationalConnection connection, DbTransaction transaction, Guid transactionId, DateTimeOffset startTime, TimeSpan duration);

-        public static void TransactionStarted(this IDiagnosticsLogger<DbLoggerCategory.Database.Transaction> diagnostics, IRelationalConnection connection, DbTransaction transaction, Guid transactionId, DateTimeOffset startDate);

-        public static void TransactionUsed(this IDiagnosticsLogger<DbLoggerCategory.Database.Transaction> diagnostics, IRelationalConnection connection, DbTransaction transaction, Guid transactionId, DateTimeOffset startDate);

-        public static void ValueConversionSqlLiteralWarning(this IDiagnosticsLogger<DbLoggerCategory.Query> diagnostics, Type mappingClrType, ValueConverter valueConverter);

-    }
-    public static class RelationalPropertyExtensions {
 {
-        public static RelationalTypeMapping FindRelationalMapping(this IProperty property);

-        public static string FormatColumns(this IEnumerable<IProperty> properties);

-        public static string GetConfiguredColumnType(this IProperty property);

-    }
-    public static class RelationalStrings {
 {
-        public static readonly EventDefinition LogAmbientTransaction;

-        public static readonly EventDefinition LogDisposingDataReader;

-        public static readonly EventDefinition LogNoMigrationsApplied;

-        public static readonly EventDefinition LogQueryPossibleExceptionWithAggregateOperator;

-        public static readonly EventDefinition LogRelationalLoggerCommittingTransaction;

-        public static readonly EventDefinition LogRelationalLoggerDisposingTransaction;

-        public static readonly EventDefinition LogRelationalLoggerRollingbackTransaction;

-        public static readonly EventDefinition LogRelationalLoggerTransactionError;

-        public static readonly EventDefinition<int, int> LogBatchSmallerThanMinBatchSize;

-        public static readonly EventDefinition<int> LogBatchReadyForExecution;

-        public static readonly EventDefinition<object, object> LogValueConversionSqlLiteralWarning;

-        public static readonly EventDefinition<object> LogClientEvalWarning;

-        public static readonly EventDefinition<object> LogPossibleUnintendedUseOfEquals;

-        public static readonly EventDefinition<string, CommandType, int, string, string> LogRelationalLoggerExecutingCommand;

-        public static readonly EventDefinition<string, string, CommandType, int, string, string> LogRelationalLoggerCommandFailed;

-        public static readonly EventDefinition<string, string, CommandType, int, string, string> LogRelationalLoggerExecutedCommand;

-        public static readonly EventDefinition<string, string> LogBoolWithDefaultWarning;

-        public static readonly EventDefinition<string, string> LogKeyHasDefaultValue;

-        public static readonly EventDefinition<string, string> LogMigrating;

-        public static readonly EventDefinition<string, string> LogRelationalLoggerClosedConnection;

-        public static readonly EventDefinition<string, string> LogRelationalLoggerClosingConnection;

-        public static readonly EventDefinition<string, string> LogRelationalLoggerConnectionError;

-        public static readonly EventDefinition<string, string> LogRelationalLoggerConnectionErrorAsDebug;

-        public static readonly EventDefinition<string, string> LogRelationalLoggerOpenedConnection;

-        public static readonly EventDefinition<string, string> LogRelationalLoggerOpeningConnection;

-        public static readonly EventDefinition<string> LogAmbientTransactionEnlisted;

-        public static readonly EventDefinition<string> LogApplyingMigration;

-        public static readonly EventDefinition<string> LogExplicitTransactionEnlisted;

-        public static readonly EventDefinition<string> LogGeneratingDown;

-        public static readonly EventDefinition<string> LogGeneratingUp;

-        public static readonly EventDefinition<string> LogMigrationAttributeMissingWarning;

-        public static readonly EventDefinition<string> LogNoMigrationsFound;

-        public static readonly EventDefinition<string> LogRelationalLoggerBeginningTransaction;

-        public static readonly EventDefinition<string> LogRelationalLoggerUsingTransaction;

-        public static readonly EventDefinition<string> LogRevertingMigration;

-        public static string BadSequenceString { get; }

-        public static string BadSequenceType { get; }

-        public static string ConflictingAmbientTransaction { get; }

-        public static string ConflictingEnlistedTransaction { get; }

-        public static string ConnectionAndConnectionString { get; }

-        public static string InvalidCommandTimeout { get; }

-        public static string InvalidMaxBatchSize { get; }

-        public static string InvalidMinBatchSize { get; }

-        public static string MultipleProvidersConfigured { get; }

-        public static string NoActiveTransaction { get; }

-        public static string NoConnectionOrConnectionString { get; }

-        public static string NoElements { get; }

-        public static string NoProviderConfigured { get; }

-        public static string RelationalNotInUse { get; }

-        public static string SqlFunctionArgumentsAndMappingsMismatch { get; }

-        public static string SqlFunctionNullArgumentMapping { get; }

-        public static string SqlFunctionUnexpectedInstanceMapping { get; }

-        public static string StoredProcedureIncludeNotSupported { get; }

-        public static string TransactionAlreadyStarted { get; }

-        public static string TransactionAssociatedWithDifferentConnection { get; }

-        public static string UpdateStoreException { get; }

-        public static string CaseElseResultTypeUnexpected(object elseResultType, object resultType);

-        public static string CaseWhenClauseResultTypeUnexpected(object whenResultType, object resultType);

-        public static string CaseWhenClauseTestTypeUnexpected(object whenOperandType, object expectedWhenOperandType);

-        public static string ConflictingColumnServerGeneration(object conflictingConfiguration, object property, object existingConfiguration);

-        public static string ConflictingOriginalRowValues(object firstEntityType, object secondEntityType, object firstProperties, object secondProperties, object columns);

-        public static string ConflictingOriginalRowValuesSensitive(object firstEntityType, object secondEntityType, object keyValue, object firstConflictingValues, object secondConflictingValues, object columns);

-        public static string ConflictingRowUpdateTypes(object firstEntityType, object firstState, object secondEntityType, object secondState);

-        public static string ConflictingRowUpdateTypesSensitive(object firstEntityType, object firstKeyValue, object firstState, object secondEntityType, object secondKeyValue, object secondState);

-        public static string ConflictingRowValues(object firstEntityType, object secondEntityType, object firstProperties, object secondProperties, object columns);

-        public static string ConflictingRowValuesSensitive(object firstEntityType, object secondEntityType, object keyValue, object firstConflictingValues, object secondConflictingValues, object columns);

-        public static string DbFunctionExpressionIsNotMethodCall(object expression);

-        public static string DbFunctionGenericMethodNotSupported(object function);

-        public static string DbFunctionInvalidInstanceType(object function, object type);

-        public static string DbFunctionInvalidParameterType(object parameter, object function, object type);

-        public static string DbFunctionInvalidReturnType(object function, object type);

-        public static string DbFunctionNameEmpty(object function);

-        public static string DerivedQueryTypeView(object queryType, object baseType);

-        public static string DiscriminatorEntityTypeNotDerived(object entityType, object rootEntityType);

-        public static string DiscriminatorPropertyMustBeOnRoot(object entityType);

-        public static string DiscriminatorPropertyNotFound(object property, object entityType);

-        public static string DiscriminatorValueIncompatible(object value, object discriminator, object discriminatorType);

-        public static string DuplicateColumnNameComputedSqlMismatch(object entityType1, object property1, object entityType2, object property2, object columnName, object table, object value1, object value2);

-        public static string DuplicateColumnNameDataTypeMismatch(object entityType1, object property1, object entityType2, object property2, object columnName, object table, object dataType1, object dataType2);

-        public static string DuplicateColumnNameDefaultSqlMismatch(object entityType1, object property1, object entityType2, object property2, object columnName, object table, object value1, object value2);

-        public static string DuplicateColumnNameNullabilityMismatch(object entityType1, object property1, object entityType2, object property2, object columnName, object table);

-        public static string DuplicateDiscriminatorValue(object entityType1, object discriminatorValue, object entityType2);

-        public static string DuplicateForeignKeyColumnMismatch(object index1, object entityType1, object index2, object entityType2, object table, object foreignKeyName, object columnNames1, object columnNames2);

-        public static string DuplicateForeignKeyDeleteBehaviorMismatch(object index1, object entityType1, object index2, object entityType2, object table, object foreignKeyName, object deleteBehavior1, object deleteBehavior2);

-        public static string DuplicateForeignKeyPrincipalColumnMismatch(object index1, object entityType1, object index2, object entityType2, object table, object foreignKeyName, object principalColumnNames1, object principalColumnNames2);

-        public static string DuplicateForeignKeyPrincipalTableMismatch(object index1, object entityType1, object index2, object entityType2, object table, object foreignKeyName, object principalTable1, object principalTable2);

-        public static string DuplicateForeignKeyUniquenessMismatch(object index1, object entityType1, object index2, object entityType2, object table, object foreignKeyName);

-        public static string DuplicateIndexColumnMismatch(object index1, object entityType1, object index2, object entityType2, object table, object indexName, object columnNames1, object columnNames2);

-        public static string DuplicateIndexUniquenessMismatch(object index1, object entityType1, object index2, object entityType2, object table, object indexName);

-        public static string DuplicateKeyColumnMismatch(object key1, object entityType1, object key2, object entityType2, object table, object keyName, object columnNames1, object columnNames2);

-        public static string ExpectedNonNullParameter(object parameter);

-        public static string FromSqlMissingColumn(object column);

-        public static string IncompatibleTableKeyNameMismatch(object table, object entityType, object otherEntityType, object keyName, object primaryKey, object otherName, object otherPrimaryKey);

-        public static string IncompatibleTableNoRelationship(object table, object entityType, object otherEntityType);

-        public static string IncorrectDefaultValueType(object value, object valueType, object property, object propertyType, object entityType);

-        public static string MigrationNotFound(object migrationName);

-        public static string MissingParameterValue(object parameter);

-        public static string ModificationCommandInvalidEntityState(object entityState);

-        public static string NamedConnectionStringNotFound(object name);

-        public static string NoDiscriminatorForValue(object entityType, object rootEntityType);

-        public static string NoDiscriminatorProperty(object entityType);

-        public static string NoDiscriminatorValue(object entityType);

-        public static string ParameterNotObjectArray(object parameter);

-        public static string RelationalCloneNotImplemented(object mapping);

-        public static string SharedRowEntryCountMismatch(object entityType, object tableName, object missingEntityType, object state);

-        public static string SharedRowEntryCountMismatchSensitive(object entityType, object tableName, object missingEntityType, object keyValue, object state);

-        public static string TimeoutTooBig(object seconds);

-        public static string TimeoutTooSmall(object seconds);

-        public static string UnableToDiscriminate(object entityType);

-        public static string UnknownOperation(object sqlGeneratorType, object operationType);

-        public static string UnsupportedPropertyType(object entity, object property, object clrType);

-        public static string UnsupportedType(object clrType);

-        public static string UpdateConcurrencyException(object expectedRows, object actualRows);

-    }
-    public class SemanticVersionComparer : IComparer<string> {
 {
-        public SemanticVersionComparer();

-        public virtual int Compare(string x, string y);

-    }
-    public class ServiceProviderCache {
 {
-        public ServiceProviderCache();

-        public static ServiceProviderCache Instance { get; }

-        public virtual IServiceProvider GetOrAdd(IDbContextOptions options, bool providerRequired);

-    }
-    public class SingletonOptionsInitializer : ISingletonOptionsInitializer {
 {
-        public SingletonOptionsInitializer();

-        public virtual void EnsureInitialized(IServiceProvider serviceProvider, IDbContextOptions options);

-    }
-    public static class SqlServerLoggerExtensions {
 {
-        public static void ByteIdentityColumnWarning(this IDiagnosticsLogger<DbLoggerCategory.Model.Validation> diagnostics, IProperty property);

-        public static void ColumnFound(this IDiagnosticsLogger<DbLoggerCategory.Scaffolding> diagnostics, string tableName, string columnName, int ordinal, string dataTypeName, int maxLength, int precision, int scale, bool nullable, bool identity, string defaultValue, string computedValue);

-        public static void DecimalTypeDefaultWarning(this IDiagnosticsLogger<DbLoggerCategory.Model.Validation> diagnostics, IProperty property);

-        public static void DefaultSchemaFound(this IDiagnosticsLogger<DbLoggerCategory.Scaffolding> diagnostics, string schemaName);

-        public static void ForeignKeyFound(this IDiagnosticsLogger<DbLoggerCategory.Scaffolding> diagnostics, string foreignKeyName, string tableName, string principalTableName, string onDeleteAction);

-        public static void ForeignKeyPrincipalColumnMissingWarning(this IDiagnosticsLogger<DbLoggerCategory.Scaffolding> diagnostics, string foreignKeyName, string tableName, string principalColumnName, string principalTableName);

-        public static void ForeignKeyReferencesMissingPrincipalTableWarning(this IDiagnosticsLogger<DbLoggerCategory.Scaffolding> diagnostics, string foreignKeyName, string tableName, string principalTableName);

-        public static void IndexFound(this IDiagnosticsLogger<DbLoggerCategory.Scaffolding> diagnostics, string indexName, string tableName, bool unique);

-        public static void MissingSchemaWarning(this IDiagnosticsLogger<DbLoggerCategory.Scaffolding> diagnostics, string schemaName);

-        public static void MissingTableWarning(this IDiagnosticsLogger<DbLoggerCategory.Scaffolding> diagnostics, string tableName);

-        public static void PrimaryKeyFound(this IDiagnosticsLogger<DbLoggerCategory.Scaffolding> diagnostics, string primaryKeyName, string tableName);

-        public static void ReflexiveConstraintIgnored(this IDiagnosticsLogger<DbLoggerCategory.Scaffolding> diagnostics, string foreignKeyName, string tableName);

-        public static void SequenceFound(this IDiagnosticsLogger<DbLoggerCategory.Scaffolding> diagnostics, string sequenceName, string sequenceTypeName, bool cyclic, int increment, long start, long min, long max);

-        public static void TableFound(this IDiagnosticsLogger<DbLoggerCategory.Scaffolding> diagnostics, string tableName);

-        public static void TypeAliasFound(this IDiagnosticsLogger<DbLoggerCategory.Scaffolding> diagnostics, string typeAliasName, string systemTypeName);

-        public static void UniqueConstraintFound(this IDiagnosticsLogger<DbLoggerCategory.Scaffolding> diagnostics, string uniqueConstraintName, string tableName);

-    }
-    public class SqlServerModelValidator : RelationalModelValidator {
 {
-        public SqlServerModelValidator(ModelValidatorDependencies dependencies, RelationalModelValidatorDependencies relationalDependencies);

-        public override void Validate(IModel model);

-        protected virtual void ValidateByteIdentityMapping(IModel model);

-        protected virtual void ValidateDefaultDecimalMapping(IModel model);

-        protected virtual void ValidateIndexIncludeProperties(IModel model);

-        protected virtual void ValidateNonKeyValueGeneration(IModel model);

-        protected override void ValidateSharedColumnsCompatibility(IReadOnlyList<IEntityType> mappedTypes, string tableName);

-        protected override void ValidateSharedKeysCompatibility(IReadOnlyList<IEntityType> mappedTypes, string tableName);

-        protected override void ValidateSharedTableCompatibility(IReadOnlyList<IEntityType> mappedTypes, string tableName);

-    }
-    public class SqlServerOptions : ISingletonOptions, ISqlServerOptions {
 {
-        public SqlServerOptions();

-        public virtual bool RowNumberPagingEnabled { get; private set; }

-        public virtual void Initialize(IDbContextOptions options);

-        public virtual void Validate(IDbContextOptions options);

-    }
-    public static class TypeExtensions {
 {
-        public static string DisplayName(this Type type, bool fullName = true);

-        public static FieldInfo GetFieldInfo(this Type type, string fieldName);

-        public static IEnumerable<string> GetNamespaces(this Type type);

-        public static bool IsDefaultValue(this Type type, object value);

-        public static string ShortDisplayName(this Type type);

-    }
-}
```

