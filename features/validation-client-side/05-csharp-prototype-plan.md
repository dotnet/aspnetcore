# Implementation Plan: .NET Server-Side Client Validation Attribute Emission

## Problem Statement

Blazor SSR input components (`InputText`, `InputNumber`, etc.) don't emit `data-val-*` HTML attributes. The JS validation library (built in Step 5) needs these attributes to discover validation rules. This plan implements the C# infrastructure to automatically emit them, following the design in `04-csharp-design.md`.

## Approach

Tests-first implementation in 5 phases. Each phase writes tests + interfaces first, then implementation until tests pass. All new types go in `Microsoft.AspNetCore.Components.Forms` (core abstractions) and `Microsoft.AspNetCore.Components.Web` (component changes).

### Key Scoping Decisions

- **Localization:** The `ErrorMessageProvider`/`DisplayNameProvider` delegates from #65539 don't exist yet. Our code will work without them (fallback to `ValidationAttribute.FormatErrorMessage()` and `DisplayAttribute`). When #65539 lands, integration is a follow-up.
- **Source generator path:** `ValidatablePropertyInfo.GetValidationAttributes()` is `protected`. We start with reflection-only. Source gen support is a follow-up when the API is made public.
- **Scope:** Only the reflection-based metadata discovery path for this prototype.

---

## Phase 1: Core Abstractions ✅

Write interfaces, context class, and their tests. No implementation logic yet — just the public API surface.

### 1a. Tests + Interfaces

**Files to create:**
- `src/Components/Forms/src/IClientValidationService.cs` — interface
- `src/Components/Forms/src/IClientValidationAdapter.cs` — interface (single method: `AddClientValidation(in ClientValidationContext, string)`)
- `src/Components/Forms/src/ClientValidation/ClientValidationAdapterRegistry.cs` — options class (adapter registry)
- `src/Components/Forms/src/ClientValidationContext.cs` — readonly struct: `Attributes` + `MergeAttribute()`
- `src/Components/Forms/test/ClientValidation/ClientValidationContextTest.cs` — tests for context behavior

**Tests for `ClientValidationContext`:**
- `Constructor_SetsAttributes` — verify MergeAttribute writes to the underlying dictionary
- `DefaultConstructor_HasNullAttributes` — verify default struct has null backing field
- `Constructor_ThrowsOnNullAttributes` — verify null check
- `MergeAttribute_AddsNewKey` — verify key/value is added to underlying dictionary
- `MergeAttribute_DoesNotOverwriteExistingKey` — verify returns false, original value kept
- `MergeAttribute_AddsMultipleKeys` — verify multiple data-val-* keys
- `MergeAttribute_ThrowsOnNullKey` / `MergeAttribute_ThrowsOnNullValue` — null guards
- `ContextCanBeReusedAcrossMultipleAdapters` — single instance accumulates attributes from multiple adapters

**Design note:** `ClientValidationContext` is a `readonly struct` with no public `Attributes` property. The underlying dictionary is stored in a private field. Adapters interact only through `MergeAttribute()`, which enforces add-only, first-wins semantics. The error message is passed as a separate argument to `AddClientValidation(in ClientValidationContext context, string errorMessage)`. A single context instance is created per field and reused across all adapters, enabling zero per-attribute allocation. The error message is pre-resolved by the service (using `ValidationOptions.ErrorMessageProvider` + `IValidationAttributeFormatter` from #65539, or `ValidationAttribute.FormatErrorMessage()` as fallback). The adapter receives the `ValidationAttribute` instance via its constructor from the provider factory. This separates message resolution (service + localization) from HTML attribute mapping (adapter).

**Public API updates:**
- `src/Components/Forms/src/PublicAPI.Unshipped.txt` — add all new public types

### 1b. Implementation

Implement `ClientValidationContext.MergeAttribute()` — the only logic in Phase 1.

---

## Phase 2: Built-In Adapters + Provider ✅

Implement the 11 built-in adapters and the `BuiltInAdapterRegistration` (registers adapters on `ClientValidationAdapterRegistry`).

### 2a. Tests

**File to create:**
- `src/Components/Forms/test/ClientValidation/BuiltInAdapterTests.cs`
- `src/Components/Forms/test/ClientValidation/ClientValidationAdapterRegistryTest.cs`

**Tests for each adapter** (parameterized or individual):
- `RequiredAdapter_EmitsDataValRequired` — `data-val=true`, `data-val-required={errorMessage}`
- `StringLengthAdapter_EmitsLengthAttributes` — `data-val-length`, `-max`, `-min`
- `MinLengthAdapter_EmitsMinLengthAttributes` — `data-val-minlength`, `-min`
- `MaxLengthAdapter_EmitsMaxLengthAttributes` — `data-val-maxlength`, `-max`
- `RangeAdapter_EmitsRangeAttributes` — `data-val-range`, `-min`, `-max`
- `RegexAdapter_EmitsRegexAttributes` — `data-val-regex`, `-pattern`
- `EmailAdapter_EmitsEmailAttribute` — `data-val-email`
- `UrlAdapter_EmitsUrlAttribute` — `data-val-url`
- `CreditCardAdapter_EmitsCreditCardAttribute` — `data-val-creditcard`
- `PhoneAdapter_EmitsPhoneAttribute` — `data-val-phone`
- `CompareAdapter_EmitsEqualtoAttributes` — `data-val-equalto`, `-other`
- `Adapter_UsesErrorMessageArgument` — verify `errorMessage` parameter is placed in the correct `data-val-*` key

**Tests for `ClientValidationAdapterRegistry`:**
- `GetAdapter_ReturnsCorrectAdapterForEachBuiltInAttribute` — factory registration coverage
- `GetAdapter_ReturnsNullForUnknownAttribute` — unknown attributes return null
- `GetAdapter_ReturnsSelfAdaptingAttribute` — attribute implementing `IClientValidationAdapter` is returned directly
- `GetAdapter_CustomAdapterOverridesBuiltIn` — last-wins replacement semantics

### 2b. Implementation

**Files to create:**
- `src/Components/Forms/src/ClientValidation/Adapters/RequiredClientAdapter.cs`
- `src/Components/Forms/src/ClientValidation/Adapters/StringLengthClientAdapter.cs`
- `src/Components/Forms/src/ClientValidation/Adapters/MinLengthClientAdapter.cs`
- `src/Components/Forms/src/ClientValidation/Adapters/MaxLengthClientAdapter.cs`
- `src/Components/Forms/src/ClientValidation/Adapters/RangeClientAdapter.cs`
- `src/Components/Forms/src/ClientValidation/Adapters/RegexClientAdapter.cs`
- `src/Components/Forms/src/ClientValidation/Adapters/DataTypeClientAdapter.cs` (email, url, creditcard, phone)
- `src/Components/Forms/src/ClientValidation/Adapters/CompareClientAdapter.cs`
- `src/Components/Forms/src/ClientValidation/BuiltInAdapterRegistration.cs`

All adapters are `internal sealed`. Each follows the pattern — adapter receives the attribute in its constructor, uses the `errorMessage` parameter for the pre-resolved message:
```csharp
internal sealed class RequiredClientAdapter(RequiredAttribute attribute) : IClientValidationAdapter
{
    public void AddClientValidation(in ClientValidationContext context, string errorMessage)
    {
        context.MergeAttribute("data-val", "true");
        context.MergeAttribute("data-val-required", errorMessage);
    }
}
```

Adapters do NOT resolve error messages. The service resolves messages before calling the adapter, passing the result as the `errorMessage` argument.

---

## Phase 3: DefaultClientValidationService ✅

The service that discovers `ValidationAttribute`s on model properties, maps them to adapters, and returns the `data-val-*` dictionary.

### 3a. Tests

**File to create:**
- `src/Components/Forms/test/ClientValidation/DefaultClientValidationServiceTest.cs`

**Test model classes** (in the test file):
```csharp
class TestModel
{
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(100, MinimumLength = 2)]
    [Display(Name = "Full Name")]
    public string Name { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }

    public string Optional { get; set; } // no validation attributes
}
```

**Tests:**
- `GetValidationAttributes_ReturnsCorrectAttributes_ForRequiredField` — Name field → `data-val`, `data-val-required`, `data-val-length`, `-min`, `-max`
- `GetValidationAttributes_ReturnsCorrectAttributes_ForEmailField` — Email → `data-val-required`, `data-val-email`
- `GetValidationAttributes_ReturnsEmpty_ForFieldWithNoAttributes` — Optional → empty dictionary
- `GetValidationAttributes_ReturnsEmpty_ForNonExistentField` — unknown field → empty
- `GetValidationAttributes_UsesDisplayName_FromDisplayAttribute` — "Full Name" in error message
- `GetValidationAttributes_FallsBackToPropertyName_WhenNoDisplayAttribute` — "Email" in error message
- `GetValidationAttributes_CachesResults` — second call returns same dictionary instance
- `GetValidationAttributes_DifferentModelsOfSameType_ShareCache` — type-level caching, not instance-level

### 3b. Implementation

**File to create:**
- `src/Components/Forms/src/ClientValidation/DefaultClientValidationService.cs`

Key implementation details:
- `ConcurrentDictionary<(Type, string), IReadOnlyDictionary<string, string>>` cache
- Reflection-based attribute discovery: `PropertyInfo.GetCustomAttributes<ValidationAttribute>(inherit: true)`
- Display name resolution: `DisplayAttribute.GetName()` → `DisplayNameAttribute.DisplayName` → property name
- Error message resolution per attribute: `ValidationOptions.ErrorMessageProvider` → `attribute.FormatErrorMessage(displayName)` fallback
- Creates one `ClientValidationContext(attributes)` per field, reuses it across all adapters; passes error message as argument to each adapter call

---

## Phase 4: Component Integration ✅

Modify existing components to consume `IClientValidationService` and emit the appropriate HTML attributes.

### 4a. Tests

**Files to create/modify:**
- `src/Components/Web/test/Forms/ClientSideValidatorTest.cs` — new test file
- `src/Components/Web/test/Forms/InputBaseTest.cs` — add new tests
- `src/Components/Web/test/Forms/ValidationMessageTest.cs` — new test file (none exists)
- `src/Components/Web/test/Forms/ValidationSummaryTest.cs` — new test file (none exists)

**ClientSideValidator tests:**
- `StoresServiceOnEditContextProperties` — after render, `EditContext.Properties[typeof(IClientValidationService)]` is set
- `ThrowsWithoutEditContext` — `InvalidOperationException` without cascading `EditContext`
- `EmitsScriptTag_WhenIncludeScriptIsTrue` — renders `<script src="...">` 
- `DoesNotEmitScriptTag_WhenIncludeScriptIsFalse` — no `<script>` when `IncludeScript="false"`

**InputBase / InputText tests (additions to existing test file):**
- `EmitsDataValAttributes_WhenServiceIsOnEditContext` — `data-val-*` attrs in rendered output
- `DoesNotEmitDataValAttributes_WhenNoServiceOnEditContext` — backwards-compatible, no change
- `DataValAttributes_DoNotOverrideExplicitAttributes` — explicit `data-val-required` on component wins
- `EmitsDataValTrue_WhenAnyValidationAttributeExists` — `data-val="true"` is always first

**ValidationMessage tests:**
- `RendersSpanWithDataValmsgFor_WhenServiceIsPresent` — `<span data-valmsg-for="Model.Name">`
- `RendersOriginalDivs_WhenServiceIsNotPresent` — original `<div class="validation-message">` per message
- `DataValmsgFor_MatchesInputNameAttribute` — verify the `for` value matches InputBase's `NameAttributeValue`

**ValidationSummary tests:**
- `RendersContainerWithDataValmsgSummary_WhenServiceIsPresent` — `<div data-valmsg-summary="true">`
- `RendersOriginalUl_WhenServiceIsNotPresent` — original behavior unchanged

### 4b. Implementation

**Files to create:**
- `src/Components/Web/src/Forms/ClientSideValidator.cs`

**Files to modify:**
- `src/Components/Web/src/Forms/InputBase.cs` — add `GetClientValidationService()` method, merge data-val-* into attributes
- `src/Components/Web/src/Forms/ValidationMessage.cs` — conditional render path with `data-valmsg-for`
- `src/Components/Web/src/Forms/ValidationSummary.cs` — conditional render path with `data-valmsg-summary`

**InputBase changes (Option B from design doc — merge into AdditionalAttributes):**
- Add private `GetClientValidationService()` that reads from `EditContext.Properties`
- Override `AdditionalAttributes` getter (or add computed property) to merge `data-val-*` attributes
- The merge ensures explicit attributes from the developer take precedence (use `TryAdd`)

**ValidationMessage changes:**
- Check `EditContext.Properties` for `IClientValidationService`
- If present: render `<span data-valmsg-for="{fieldName}">` (always, even if no messages)
- The `fieldName` must match `InputBase.NameAttributeValue` — use same expression formatting

**ValidationSummary changes:**
- Check `EditContext.Properties` for `IClientValidationService`
- If present: always render `<div data-valmsg-summary="true"><ul>...</ul></div>`

**Public API updates:**
- `src/Components/Web/src/PublicAPI.Unshipped.txt` — add `ClientSideValidator` type

---

## Phase 5: DI Registration + Sample App Update ✅

### 5a. Tests

**File created:**
- `src/Components/Forms/test/ClientValidation/ClientValidationServiceCollectionExtensionsTest.cs`

**Tests (8 total):**
- `AddClientSideValidation_RegistersService` — resolves `IClientValidationService` from DI
- `AddClientSideValidation_RegistersScopedService` — different scopes get different instances
- `AddClientSideValidation_DoesNotOverrideExistingRegistration` — `TryAdd` semantics respected
- `AddClientValidationAdapter_CustomAdapterIsUsed` — custom attribute gets custom adapter
- `AddClientValidationAdapter_CustomOverridesBuiltIn` — custom registered after built-in wins (last-wins)
- `AddClientSideValidation_BuiltInRegistrationIsIdempotent` — `TryAddEnumerable` dedup
- `AddClientValidationAdapter_SelfAdaptingAttributeWorks` — attribute implementing `IClientValidationAdapter` returns itself
- `AddClientValidationAdapter_MultipleCustomAdaptersCanBeRegistered` — multiple custom adapters coexist

### 5b. Implementation

**File created:**
- `src/Components/Forms/src/ClientValidation/ClientValidationServiceCollectionExtensions.cs`

Two extension methods:
- `AddClientSideValidation()` — registers `ClientValidationAdapterRegistry` options with built-in adapters via `IConfigureOptions<ClientValidationAdapterRegistry>` (idempotent via `TryAddEnumerable`)
- `AddClientValidationAdapter<TAttribute>(services, factory)` — registers custom adapter via `Configure<ClientValidationAdapterRegistry>()` (last-wins override semantics)

**DI architecture:** The options-based pattern composes naturally — `BuiltInAdapterRegistration` (an `IConfigureOptions<ClientValidationAdapterRegistry>`) registers built-in adapters, and custom `AddClientValidationAdapter<T>()` calls add additional `Configure<>` registrations that run after built-ins. `IOptions<ClientValidationAdapterRegistry>` resolves the fully composed registry.

**Sample app changes:**
- `src/Components/Samples/BlazorSSR/Program.cs` — added `builder.Services.AddClientSideValidation()`
- `src/Components/Samples/BlazorSSR/Pages/Contact.razor` — added `<ClientSideValidator />` inside EditForm after DataAnnotationsValidator
- `ContactManual.razor` kept for comparison (hardcoded data-val-* attributes)

### 5c. Integration Verification

- ✅ Sample app builds successfully
- ✅ All 138 Forms tests pass (including 8 new DI tests)
- ✅ All 284 Web tests pass

---

## File Summary

### New files (Phase 1-5)

| File | Phase | Type |
|------|-------|------|
| `src/Components/Forms/src/IClientValidationService.cs` | 1 | Interface |
| `src/Components/Forms/src/IClientValidationAdapter.cs` | 1 | Interface |
| `src/Components/Forms/src/ClientValidation/ClientValidationAdapterRegistry.cs` | 1 | Options Class |
| `src/Components/Forms/src/ClientValidationContext.cs` | 1 | Readonly Struct |
| `src/Components/Forms/test/ClientValidation/ClientValidationContextTest.cs` | 1 | Test |
| `src/Components/Forms/src/ClientValidation/Adapters/RequiredClientAdapter.cs` | 2 | Adapter |
| `src/Components/Forms/src/ClientValidation/Adapters/StringLengthClientAdapter.cs` | 2 | Adapter |
| `src/Components/Forms/src/ClientValidation/Adapters/MinLengthClientAdapter.cs` | 2 | Adapter |
| `src/Components/Forms/src/ClientValidation/Adapters/MaxLengthClientAdapter.cs` | 2 | Adapter |
| `src/Components/Forms/src/ClientValidation/Adapters/RangeClientAdapter.cs` | 2 | Adapter |
| `src/Components/Forms/src/ClientValidation/Adapters/RegexClientAdapter.cs` | 2 | Adapter |
| `src/Components/Forms/src/ClientValidation/Adapters/DataTypeClientAdapter.cs` | 2 | Adapter |
| `src/Components/Forms/src/ClientValidation/Adapters/CompareClientAdapter.cs` | 2 | Adapter |
| `src/Components/Forms/src/ClientValidation/BuiltInAdapterRegistration.cs` | 2 | Internal |
| `src/Components/Forms/test/ClientValidation/BuiltInAdapterTests.cs` | 2 | Test |
| `src/Components/Forms/test/ClientValidation/ClientValidationAdapterRegistryTest.cs` | 2 | Test |
| `src/Components/Forms/src/ClientValidation/DefaultClientValidationService.cs` | 3 | Internal |
| `src/Components/Forms/test/ClientValidation/DefaultClientValidationServiceTest.cs` | 3 | Test |
| `src/Components/Web/src/Forms/ClientSideValidator.cs` | 4 | Component |
| `src/Components/Web/test/Forms/InputTextClientValidationTest.cs` | 4 | Test |
| `src/Components/Web/test/Forms/ValidationMessageClientValidationTest.cs` | 4 | Test |
| `src/Components/Web/test/Forms/ValidationSummaryClientValidationTest.cs` | 4 | Test |
| `src/Components/Web/test/Forms/TestClientValidationService.cs` | 4 | Test Helper |
| `src/Components/Forms/src/ClientValidation/ClientValidationServiceCollectionExtensions.cs` | 5 | Extension |
| `src/Components/Forms/test/ClientValidation/ClientValidationServiceCollectionExtensionsTest.cs` | 5 | Test |

### Modified files

| File | Phase | Change |
|------|-------|--------|
| `src/Components/Forms/src/PublicAPI.Unshipped.txt` | 1 | Add interfaces + context |
| `src/Components/Web/src/PublicAPI.Unshipped.txt` | 4 | Add ClientSideValidator |
| `src/Components/Web/src/Forms/InputBase.cs` | 4 | Merge data-val-* into attributes |
| `src/Components/Web/src/Forms/ValidationMessage.cs` | 4 | Conditional data-valmsg-for rendering |
| `src/Components/Web/src/Forms/ValidationSummary.cs` | 4 | Conditional data-valmsg-summary rendering |
| `src/Components/Web/test/Forms/InputBaseTest.cs` | 4 | Add data-val-* emission tests |
| `src/Components/Samples/BlazorSSR/Program.cs` | 5 | AddClientSideValidation() |
| `src/Components/Samples/BlazorSSR/Pages/Contact.razor` | 5 | Add ClientSideValidator |

---

## Build & Test Commands

```powershell
# Activate .NET environment
cd C:\code\aspnetcore2 && . .\activate.ps1

# Build Forms library + tests
./src/Components/Forms/build.sh -test

# Build Web library + tests  
./src/Components/Web/build.sh -test

# Build sample app
cd src/Components/Samples/BlazorSSR && dotnet build
```

## Dependencies Between Phases

- Phase 2 depends on Phase 1 (adapters implement interfaces)
- Phase 3 depends on Phases 1+2 (service uses adapter provider)
- Phase 4 depends on Phases 1+3 (components consume service)
- Phase 5 depends on all previous phases
