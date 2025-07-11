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
- [x] **Extend `WebPersistenceScenario` for reconnection**
  - [x] Add static `Reconnection` property for reconnection scenario
  - [x] Implement `IsRecurring` as `false` for reconnection (one-time operation)
  - [x] Add internal `WebPersistenceScenarioType` enum for type safety
  - [x] Add XML documentation with examples

- [x] **Extend `WebPersistenceFilter` for reconnection**
  - [x] Add static `Reconnection` property for reconnection filter
  - [x] Implement scenario support and restoration logic with scenario type matching
  - [x] Add comprehensive XML documentation

### Phase 2: Reconnection Attribute Implementation
- [x] **Create `RestoreStateOnReconnectionAttribute`**
  - [x] Implement `IPersistentStateFilter` interface
  - [x] Add constructor with optional `enabled` parameter (defaults to `true`)
  - [x] Implement `ShouldRestore()` method:
    - Return `Enabled` value for reconnection scenarios
    - Return `true` for all other scenarios (default behavior)
  - [x] Add comprehensive XML documentation with examples

### Phase 3: Circuit State Persistence Integration
- [x] **Integrate with circuit eviction and restoration**
  - [x] Updated `CircuitHost.UpdateRootComponents()` to accept `isRestore` parameter
  - [x] Updated `ComponentHub.UpdateRootComponentsCore()` to pass correct restore context
  - [x] Ensure `ComponentStatePersistenceManager.RestoreStateAsync()` is called with correct scenario:
    - `WebPersistenceScenario.Reconnection` during circuit restoration (`isRestore = true`)
    - `WebPersistenceScenario.Prerendering` during initial render (`isRestore = false`)

### Phase 4: Storage Integration
- [x] **Storage compatibility verified**
  - [x] Existing `IPersistentComponentStateStore` supports circuit state (no changes needed)
  - [x] State serialization and deserialization works with existing infrastructure

### Phase 5: Documentation and API Surface
- [x] **XML Documentation**
  - [x] Add comprehensive XML docs for `RestoreStateOnReconnectionAttribute`
  - [x] Include `<example>` sections showing usage patterns
  - [x] Document parameter meanings and return values
  - [x] Update existing documentation to mention reconnection support

- [x] **PublicAPI Management**
  - [x] Add `RestoreStateOnReconnectionAttribute` to `PublicAPI.Unshipped.txt`
  - [x] Add new `WebPersistenceScenario.Reconnection` property
  - [x] Add new `WebPersistenceFilter.Reconnection` property
  - [x] Ensure all signatures match actual implementation

### Phase 6: Testing and Validation
- [ ] **Add Core Logic Tests (Control Flow Only)**
  - [ ] **RestoreStateOnReconnectionAttribute Tests**:
    - `SupportsScenario_WithReconnectionScenario_ReturnsTrue`
    - `SupportsScenario_WithPrerenderingScenario_ReturnsFalse`
    - `SupportsScenario_WithNonWebScenario_ReturnsFalse`
    - `ShouldRestore_WhenEnabled_ReturnsTrue`
    - `ShouldRestore_WhenDisabled_ReturnsFalse`
  - [ ] **WebPersistenceFilter Tests**:
    - `SupportsScenario_WithMatchingScenarioType_ReturnsTrue`
    - `SupportsScenario_WithDifferentScenarioType_ReturnsFalse`
    - `SupportsScenario_WithNonWebScenario_ReturnsFalse`
    - `ShouldRestore_WhenEnabled_ReturnsTrue`
    - `ShouldRestore_WhenDisabled_ReturnsFalse`
  - [ ] **ComponentStatePersistenceManager Integration Tests**:
    - `RestoreStateAsync_WithReconnectionScenario_AppliesReconnectionFilters`
    - `RestoreStateAsync_WithPrerenderingScenario_AppliesPrerenderingFilters`
    - `RestoreStateAsync_WithReconnectionAttributeDisabled_SkipsProperty`
    - `RestoreStateAsync_WithReconnectionAttributeEnabled_RestoresProperty`
  - [ ] **PersistentStateValueProvider Tests**:
    - `Subscribe_WithReconnectionAttribute_CreatesFilteredSubscription`
    - `Subscribe_WithMultipleFilters_CreatesCorrectSubscriptions`
    - `Subscribe_WithNoFilters_CreatesUnfilteredSubscription`
  - [ ] **CircuitHost Integration Tests**:
    - `UpdateRootComponents_WithIsRestoreTrue_UsesReconnectionScenario`
    - `UpdateRootComponents_WithIsRestoreFalse_UsesPrerenderingScenario`

- [ ] **Validate Core Functionality**
  - [ ] Ensure all existing tests pass
  - [ ] Verify backward compatibility (properties without reconnection filters still work)
  - [ ] Test scenario-based filtering with reconnection attributes

### Phase 7: E2E Test Implementation
- [x] **Test: Server Reconnection Scenario Filtering**
  - [x] Update `src/Components/test/testassets/TestContentPackage/PersistentCounter.razor`:
    - Add `NonPersistedCounter` property with `[RestoreStateOnReconnection(false)]`
    - Add increment button for non-persisted counter
    - Initialize non-persisted counter to 5 during SSR
  - [x] Update `src/Components/test/E2ETest/ServerExecutionTests/ServerResumeTests.cs`:
    - Add `CanResumeCircuitAfterDisconnection` test method
    - Verify initial state (NonPersistedCounter = 5)
    - Test interactive session functionality
    - Force disconnection and reconnection
    - Verify persistent counter restores correctly
    - Verify non-persisted counter resets to default (0)
    - Test repeatability across multiple disconnection cycles

### Phase 8: Build and Integration
- [x] **Compilation Validation**
  - [x] Ensure code compiles without errors
  - [x] Fix all analyzer warnings (PublicAPI, nullable references, etc.)
  - [x] Validate that no breaking changes are introduced to existing APIs

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
- **Reconnection scenario support**: ✅ Complete
- **Reconnection attribute**: ✅ Complete  
- **Circuit integration**: ✅ Complete
- **Storage integration**: ✅ Complete (no changes needed)
- **Documentation and APIs**: ✅ Complete
- **E2E scenario tests**: ✅ Complete
- **Unit testing and validation**: ⏳ In Progress (Phase 6)
- **Build and integration**: ✅ Compilation Complete, Integration Testing Remaining

### Success Criteria
- All existing tests continue to pass
- New reconnection attribute filters state restoration correctly during reconnection scenarios
- Default behavior (without reconnection filter) preserves state across reconnections
- E2E test demonstrates disconnection/reconnection cycle with mixed state preservation
- No performance regression in reconnection scenarios
- Clean compilation with proper API surface management
