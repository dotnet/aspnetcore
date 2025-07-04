# Scenario-Based Persistent State Filtering Implementation Plan

## Overview
Implement scenario-based persistent state filtering for Blazor, starting with prerendering. Refactor the persistent state infrastructure to support scenario-aware restoration, attribute-based filtering, and robust callback management.

## Core Requirements
- Incremental implementation that compiles cleanly at each step
- Support for scenario-aware state restoration (starting with prerendering)
- Attribute-based filtering for fine-grained control
- Robust callback management with proper disposal
- Comprehensive public/internal API documentation
- Maintain backward compatibility where possible

## Implementation Plan

### Phase 1: Core Abstractions
- [x] **Design scenario abstraction (`IPersistentComponentStateScenario`)**
  - [x] Create interface with `IsRecurring` property to distinguish one-time vs recurring scenarios
  - [x] Add XML documentation with examples

- [x] **Design filter abstraction (`IPersistentStateFilter`)**
  - [x] Create interface with `SupportsScenario()` and `ShouldRestore()` methods
  - [x] Add comprehensive XML documentation

- [x] **Implement web-specific scenarios (`WebPersistenceScenario`)**
  - [x] Create sealed class implementing `IPersistentComponentStateScenario`
  - [x] Add static `Prerendering` property for prerendering scenario
  - [x] Implement `IsRecurring` as `false` for prerendering (one-time operation)

- [x] **Implement web-specific filters (`WebPersistenceFilter`)**
  - [x] Create sealed class implementing `IPersistentStateFilter`
  - [x] Add static `Prerendering` property for prerendering filter
  - [x] Implement scenario support and restoration logic

### Phase 2: Attribute-Based Filtering
- [x] **Create `RestoreStateOnPrerenderingAttribute`**
  - [x] Implement `IPersistentStateFilter` interface
  - [x] Add constructor with optional `enable` parameter (defaults to `true`)
  - [x] Implement `SupportsScenario()` to only support prerendering
  - [x] Implement `ShouldRestore()` based on enable flag
  - [x] Add comprehensive XML documentation with examples

### Phase 3: Core State Management Refactoring
- [x] **Refactor `PersistentComponentState`**
  - [x] Add support for registering restoration callbacks with filters
  - [x] Add method: `RegisterOnRestoring(IPersistentStateFilter? filter, Action callback)`
  - [x] Support nullable filters (null = always restore)
  - [x] Add scenario and state tracking to support filtered restoration
  - [x] Update `UpdateExistingState` to accept and store scenario
  - [x] Move callback invocation responsibility to `ComponentStatePersistenceManager`

- [x] **Refactor `ComponentStatePersistenceManager`**
  - [x] Add overload: `RestoreStateAsync(IPersistentComponentStateStore store, IPersistentComponentStateScenario? scenario)`
  - [x] Track first vs. subsequent state restoration calls
  - [x] Call appropriate `InitializeExistingState` or `UpdateExistingState` methods
  - [x] Centralize callback invocation logic with scenario and filter support
  - [x] Handle null filters (always restore when filter is null)
  - [x] Implement proper filter evaluation logic:
    - If scenario is null: restore all callbacks
    - If filter is null: restore for non-recurring scenarios or when scenario is null
    - If filter supports scenario and should restore: restore
    - If filter doesn't support scenario but scenario is non-recurring: restore

### Phase 4: Value Provider Integration
- [x] **Refactor `PersistentStateValueProvider`**
  - [x] Create `ComponentSubscription` class to group persistence and restoration subscriptions
  - [x] Register restoration callbacks for each filter attribute found on properties
  - [x] Fix attribute discovery to filter for `IPersistentStateFilter` implementers only
  - [x] Handle properties without filter attributes (register with null filter for backward compatibility)
  - [x] Update subscription management to track both persistence and restoration subscriptions
  - [x] Ensure proper cleanup and disposal of all subscriptions

### Phase 5: Supporting Infrastructure
- [x] **Create `RestoreComponentStateRegistration`**
  - [x] Store filter and callback for restoration scenarios
  - [x] Support nullable filters
  - [x] Add proper equality and disposal semantics

- [x] **Refactor `RestoringComponentStateSubscription`**
  - [x] Move to separate file as readonly struct
  - [x] Implement proper disposal to remove callbacks from registration list
  - [x] Handle subscription cleanup correctly

- [x] **Update ComponentSubscription (in PersistentStateValueProvider)**
  - [x] Store persistence subscription, restoration subscriptions, and last value
  - [x] Handle multiple restoration subscriptions per property (one per filter)
  - [x] Implement proper value tracking and updates
  - [x] Fix accessibility (public class, not internal struct)

### Phase 6: Documentation and API Surface
- [x] **XML Documentation**
  - [x] Add comprehensive XML docs for all public interfaces and classes
  - [x] Include `<example>` sections showing usage patterns
  - [x] Document parameter meanings and return values
  - [x] Fix all XML documentation validation errors

- [x] **PublicAPI Management**
  - [x] Add all new public APIs to `PublicAPI.Unshipped.txt`
  - [x] Include interfaces: `IPersistentComponentStateScenario`, `IPersistentStateFilter`
  - [x] Include classes: `WebPersistenceScenario`, `WebPersistenceFilter`, `RestoreStateOnPrerenderingAttribute`
  - [x] Include new methods on existing classes
  - [x] Include struct: `RestoringComponentStateSubscription`
  - [x] Ensure all signatures match actual implementation

### Phase 7: Testing and Validation
- [x] **Fix Existing Tests**
  - [x] Update test constructor calls to match new `PersistentComponentState` signature
  - [x] Add empty restoration callback lists to all test instantiations
  - [x] Fix tests in `ComponentApplicationStateTest.cs`
  - [x] Fix tests in `PersistentStateValueProviderTests.cs`
  - [x] Fix tests in `PersistentServicesRegistryTest.cs`

- [x] **Validate Core Functionality**
  - [x] Ensure all existing tests pass
  - [x] Verify backward compatibility (properties without filters still work)
  - [x] Test scenario-based filtering with prerendering attributes
  - [x] Validate that null filters work as expected (always restore)

- [x] **Add Core Logic Tests**
  - [x] **ComponentStatePersistenceManager Tests** (5 new tests added):
    - `RestoreStateAsync_WithoutScenario_InvokesAllCallbacks`
    - `RestoreStateAsync_WithPrerenderingScenario_FiltersCallbacks`
    - `RestoreStateAsync_WithNonPrerenderingScenario_FiltersCallbacks`
    - `RestoreStateAsync_WithRecurringScenario_SkipsFilteredCallbacks`
    - `RestoreStateAsync_CallbackDisposal_RemovesFromInvocation`
  - [x] **PersistentStateValueProvider Tests** (3 new tests added):
    - `Subscribe_WithRestoreOnPrerenderingAttribute_CreatesCorrectSubscriptions`
    - `Subscribe_WithFilteredProperty_CreatesValidSubscription`
    - `Unsubscribe_RemovesSubscriptionCorrectly`
  - [x] All tests passing - 17 total ComponentStatePersistenceManager tests, 38 total PersistentStateValueProvider tests

### Phase 8: Scenario Validation E2E Tests
- [x] **Test 1: Declarative Prerendering Scenario Filtering**
  - [x] Update `DeclarativePersistStateComponent.razor` with prerendering filter test properties
  - [x] Add properties with `[RestoreStateOnPrerendering]` and `[RestoreStateOnPrerendering(false)]`
  - [x] Update test file `InteractivityTest.cs` to validate prerendering filtering behavior
  - [x] Verify that enabled properties restore during prerendering while disabled ones don't

- [x] **Note**: Tests 2 and 3 (Server Reconnection and Enhanced Navigation) are out of scope as they require scenarios not yet implemented

### Phase 9: Build and Integration
- [x] **Compilation Validation**
  - [x] Ensure code compiles without errors
  - [x] Fix all analyzer warnings (PublicAPI, nullable references, etc.)
  - [x] Validate that no breaking changes are introduced to existing APIs

- [x] **Final Integration Testing**
  - [x] Run full test suite to ensure no regressions
  - [x] Verify end-to-end scenarios work correctly
  - [x] Test with actual Blazor applications if possible

## Detailed Test Specifications

### Test 1: Declarative Prerendering Scenario Filtering

**Purpose**: Validates that `[RestoreStateOnPrerendering]` and `[RestoreStateOnPrerendering(false)]` attributes correctly control state restoration during prerendering scenarios.

**Components to Update**:
- `src/Components/test/testassets/TestContentPackage/DeclarativePersistStateComponent.razor`
- `src/Components/test/E2ETest/ServerRenderingTests/InteractivityTest.cs`

**Test Properties**:
```csharp
[SupplyParameterFromPersistentComponentState]
[RestoreStateOnPrerendering]
public string PrerenderingEnabledValue { get; set; }

[SupplyParameterFromPersistentComponentState]
[RestoreStateOnPrerendering(false)]
public string PrerenderingDisabledValue { get; set; }
```

**Expected Behavior**:
- Properties with `[RestoreStateOnPrerendering]` (default true) should restore state during prerendering
- Properties with `[RestoreStateOnPrerendering(false)]` should NOT restore state during prerendering
- Test validates both Server and WebAssembly prerendering scenarios

### Test 2: Server Reconnection Scenario Filtering

**Purpose**: Validates that `[RestoreStateOnReconnection(false)]` prevents state restoration during server reconnection while default behavior preserves state.

**Components to Update**:
- `src/Components/test/testassets/TestContentPackage/PersistentCounter.razor`
- `src/Components/test/E2ETest/ServerExecutionTests/ServerResumeTests.cs`

**Test Properties**:
```csharp
[SupplyParameterFromPersistentComponentState]
[RestoreStateOnReconnection(false)]
public int NonPersistedCounter { get; set; }
```

**Expected Behavior**:
- Properties without filters should restore normally after reconnection
- Properties with `[RestoreStateOnReconnection(false)]` should reset to default values after reconnection
- Test validates disconnection/reconnection cycles preserve correct state behavior

### Test 3: Enhanced Navigation State Updates

**Purpose**: Validates that `[UpdateStateOnEnhancedNavigation]` enables state updates for retained components during enhanced navigation scenarios.

**Components to Update**:
- `src/Components/test/testassets/TestContentPackage/NonStreamingComponentWithPersistentState.razor`
- `src/Components/test/E2ETest/Tests/StatePersistenceTest.cs`

**Test Properties**:
```csharp
[Parameter]
[SupplyParameterFromPersistentComponentState]
[UpdateStateOnEnhancedNavigation]
public string EnhancedNavState { get; set; }
```

**Expected Behavior**:
- Retained components should receive state updates during enhanced navigation
- Only properties with `[UpdateStateOnEnhancedNavigation]` should update during navigation
- Properties without the attribute should maintain their existing values
- Test validates multiple navigation cycles and state persistence

## Implementation Notes

### Key Design Decisions
1. **Nullable Filters**: Decided to support `null` filters meaning "restore always" rather than creating additional overloads
2. **Scenario-Aware Restoration**: Centralized in `ComponentStatePersistenceManager` for consistency
3. **Backward Compatibility**: Properties without filter attributes continue to work (registered with null filter)
4. **Incremental Safety**: Each phase maintains compilation and basic functionality

### Current Status
- **Core infrastructure**: ✅ Complete
- **Attribute-based filtering**: ✅ Complete
- **State management refactoring**: ✅ Complete
- **Value provider integration**: ✅ Complete
- **Documentation and APIs**: ✅ Complete
- **Test compatibility**: ✅ Complete
- **Build validation**: ✅ Complete
- **Core functionality validation**: ✅ Complete (all unit tests passing)
- **Core logic testing**: ✅ Complete (8 new tests added for scenario-based filtering)
- **E2E scenario validation tests**: ✅ Complete (Test 1 - prerendering)
- **Final integration testing**: ✅ Complete

### Remaining Work
- None. All planned phases and validation steps are complete.

## Future Enhancements (Not in Scope)
- Additional scenarios (enhanced navigation, reconnection, etc.)
- More sophisticated filtering logic
- Performance optimizations for large-scale applications
- Additional convenience attributes for common scenarios
