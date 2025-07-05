# Server Reconnection Persistent State Filtering Implementation Plan

## Overview
Implement server reconnection support for scenario-based persistent component state filtering in Blazor. Extend the persistent state system to allow declarative and imperative control over state restoration during server disconnection and reconnection events.

## Core Requirements
- Incremental implementation that compiles cleanly at each step
- Support for scenario-based state restoration during server reconnection
- Attribute-based filtering for fine-grained control during reconnection
- Robust callback management with proper disposal
- Comprehensive public/internal API documentation
- Maintain backward compatibility with existing persistent state infrastructure

## Implementation Plan

### Phase 1: Reconnection Scenario Infrastructure
- [ ] **Extend `WebPersistenceScenario` for reconnection**
  - [ ] Add static `Reconnection` property for reconnection scenario
  - [ ] Implement `IsRecurring` as `false` for reconnection (one-time operation)
  - [ ] Add XML documentation with examples

- [ ] **Extend `WebPersistenceFilter` for reconnection**
  - [ ] Add static `Reconnection` property for reconnection filter
  - [ ] Implement scenario support and restoration logic
  - [ ] Add comprehensive XML documentation

### Phase 2: Reconnection Attribute Implementation
- [ ] **Create `RestoreStateOnReconnectionAttribute`**
  - [ ] Implement `IPersistentStateFilter` interface
  - [ ] Add constructor with optional `enabled` parameter (defaults to `true`)
  - [ ] Implement `ShouldRestore()` method:
    - Return `Enabled` value for reconnection scenarios
    - Return `true` for all other scenarios (default behavior)
  - [ ] Add comprehensive XML documentation with examples

### Phase 3: Circuit State Persistence Integration
- [ ] **Integrate with circuit eviction and restoration**
  - [ ] Ensure `ComponentStatePersistenceManager.PersistState()` is called before circuit eviction
  - [ ] Ensure `ComponentStatePersistenceManager.RestoreStateAsync()` is called with `WebPersistenceScenario.Reconnection` during circuit restoration
  - [ ] Verify state is saved to and loaded from storage with circuit ID

### Phase 4: Storage Integration
- [ ] **Verify storage compatibility**
  - [ ] Ensure existing `IPersistentComponentStateStore` supports circuit state
  - [ ] Validate that root components and persistent state are both stored and restored
  - [ ] Test state serialization and deserialization

### Phase 5: Documentation and API Surface
- [ ] **XML Documentation**
  - [ ] Add comprehensive XML docs for `RestoreStateOnReconnectionAttribute`
  - [ ] Include `<example>` sections showing usage patterns
  - [ ] Document parameter meanings and return values
  - [ ] Update existing documentation to mention reconnection support

- [ ] **PublicAPI Management**
  - [ ] Add `RestoreStateOnReconnectionAttribute` to `PublicAPI.Unshipped.txt`
  - [ ] Add new `WebPersistenceScenario.Reconnection` property
  - [ ] Add new `WebPersistenceFilter.Reconnection` property
  - [ ] Ensure all signatures match actual implementation

### Phase 6: Testing and Validation
- [ ] **Add Core Logic Tests**
  - [ ] **ComponentStatePersistenceManager Tests**:
    - `RestoreStateAsync_WithReconnectionScenario_FiltersCallbacks`
    - `RestoreStateAsync_WithReconnectionScenario_RestoresEnabledProperties`
    - `RestoreStateAsync_WithReconnectionScenario_SkipsDisabledProperties`
  - [ ] **PersistentStateValueProvider Tests**:
    - `Subscribe_WithRestoreOnReconnectionAttribute_CreatesCorrectSubscriptions`
    - `Subscribe_WithReconnectionDisabled_CreatesCorrectSubscriptions`

- [ ] **Validate Core Functionality**
  - [ ] Ensure all existing tests pass
  - [ ] Verify backward compatibility (properties without reconnection filters still work)
  - [ ] Test scenario-based filtering with reconnection attributes
  - [ ] Validate that default behavior restores state during reconnection

### Phase 7: E2E Test Implementation
- [ ] **Test: Server Reconnection Scenario Filtering**
  - [ ] Update `src/Components/test/testassets/TestContentPackage/PersistentCounter.razor`:
    - Add `NonPersistedCounter` property with `[RestoreStateOnReconnection(false)]`
    - Add increment button for non-persisted counter
    - Initialize non-persisted counter to 5 during SSR
  - [ ] Update `src/Components/test/E2ETest/ServerExecutionTests/ServerResumeTests.cs`:
    - Add `NonPersistedStateIsNotRestoredAfterReconnection` test method
    - Verify initial state (NonPersistedCounter = 5)
    - Test interactive session functionality
    - Force disconnection and reconnection
    - Verify persistent counter restores correctly
    - Verify non-persisted counter resets to default (0)
    - Test repeatability across multiple disconnection cycles

### Phase 8: Build and Integration
- [ ] **Compilation Validation**
  - [ ] Ensure code compiles without errors
  - [ ] Fix all analyzer warnings (PublicAPI, nullable references, etc.)
  - [ ] Validate that no breaking changes are introduced to existing APIs

- [ ] **Integration Testing**
  - [ ] Run full test suite to ensure no regressions
  - [ ] Verify end-to-end reconnection scenarios work correctly
  - [ ] Test with actual Blazor server applications

## Detailed Test Specifications

### Test: Server Reconnection Scenario Filtering

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

**Test Flow**:
1. Verify initial state during/after SSR (NonPersistedCounter = 5)
2. Wait for interactivity and verify state persists
3. Increment both persistent and non-persisted counters
4. Force disconnection and reconnection
5. Verify persistent counter restores correctly
6. Verify non-persisted counter resets to default (0)
7. Test repeatability with second disconnection cycle

## Implementation Notes

### Key Design Decisions
1. **Non-Recurring Scenario**: Reconnection is a one-time operation (`IsRecurring = false`)
2. **Default Behavior**: Properties without `[RestoreStateOnReconnection(false)]` restore normally
3. **Circuit Lifecycle Integration**: State persistence occurs during circuit eviction, restoration during circuit resurrection
4. **Backward Compatibility**: Existing persistent state infrastructure remains unchanged

### Existing Infrastructure (Already Implemented)
- **Core abstractions**: `IPersistentComponentStateScenario`, `IPersistentStateFilter`
- **State management**: `PersistentComponentState.RegisterOnRestoring()` with filter support
- **Persistence manager**: `ComponentStatePersistenceManager.RestoreStateAsync()` with scenario support
- **Value provider**: `PersistentStateValueProvider` with attribute discovery and subscription management
- **Supporting types**: `RestoreComponentStateRegistration`, `RestoringComponentStateSubscription`

### Current Status
- **Core infrastructure**: ✅ Complete (from prerendering implementation)
- **Reconnection scenario support**: ⏳ To be implemented
- **Reconnection attribute**: ⏳ To be implemented
- **Circuit integration**: ⏳ To be implemented
- **Storage integration**: ⏳ To be implemented
- **Documentation and APIs**: ⏳ To be implemented
- **Testing and validation**: ⏳ To be implemented
- **E2E scenario tests**: ⏳ To be implemented
- **Build and integration**: ⏳ To be implemented

### Success Criteria
- All existing tests continue to pass
- New reconnection attribute filters state restoration correctly during reconnection scenarios
- Default behavior (without reconnection filter) preserves state across reconnections
- E2E test demonstrates disconnection/reconnection cycle with mixed state preservation
- No performance regression in reconnection scenarios
- Clean compilation with proper API surface management
