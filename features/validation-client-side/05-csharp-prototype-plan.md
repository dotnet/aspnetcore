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

## Phase 1: Core Abstractions

Write interfaces, context class, and their tests. No implementation logic yet — just the public API surface.

### 1a. Tests + Interfaces

**Files to create:**
- `src/Components/Forms/src/IClientValidationService.cs` — interface
- `src/Components/Forms/src/IClientValidationAdapter.cs` — interface  
- `src/Components/Forms/src/IClientValidationAdapterProvider.cs` — interface
- `src/Components/Forms/src/ClientValidationContext.cs` — context class with `MergeAttribute()`
- `src/Components/Forms/test/ClientValidationContextTest.cs` — tests for context behavior

**Tests for `ClientValidationContext`:**
- `MergeAttribute_AddsNewKey` — verify key/value is added
- `MergeAttribute_DoesNotOverwriteExistingKey` — verify returns false, original value kept
- `MergeAttribute_SetsDataVal_True` — verify `data-val` = `"true"` convention
- `Properties_AreAccessible` — verify DisplayName, DeclaringType, PropertyName, Services are readable

**Public API updates:**
- `src/Components/Forms/src/PublicAPI.Unshipped.txt` — add all new public types

### 1b. Implementation

Implement `ClientValidationContext.MergeAttribute()` — the only logic in Phase 1.

---

## Phase 2: Built-In Adapters + Provider

Implement the 11 built-in adapters and the `DefaultClientValidationAdapterProvider`.

### 2a. Tests

**File to create:**
- `src/Components/Forms/test/ClientValidation/BuiltInAdapterTests.cs`
- `src/Components/Forms/test/ClientValidation/DefaultClientValidationAdapterProviderTest.cs`

**Tests for each adapter** (parameterized or individual):
- `RequiredAdapter_EmitsDataValRequired` — `data-val=true`, `data-val-required={message}`
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
- `Adapter_UsesDisplayNameInErrorMessage` — verify display name substitution
- `Adapter_UsesCustomErrorMessage` — verify `ErrorMessage` property on attribute is used

**Tests for `DefaultClientValidationAdapterProvider`:**
- `GetAdapter_ReturnsCorrectAdapterForEachBuiltInAttribute` — type switch coverage
- `GetAdapter_ReturnsNullForUnknownAttribute` — unknown attributes return null
- `GetAdapter_FallsBackToCustomProviders` — custom `IClientValidationAdapterProvider` is consulted
- `GetAdapter_BuiltInTakesPrecedenceOverCustom` — built-in checked first

### 2b. Implementation

**Files to create:**
- `src/Components/Forms/src/ClientValidation/RequiredClientAdapter.cs`
- `src/Components/Forms/src/ClientValidation/StringLengthClientAdapter.cs`
- `src/Components/Forms/src/ClientValidation/MinLengthClientAdapter.cs`
- `src/Components/Forms/src/ClientValidation/MaxLengthClientAdapter.cs`
- `src/Components/Forms/src/ClientValidation/RangeClientAdapter.cs`
- `src/Components/Forms/src/ClientValidation/RegexClientAdapter.cs`
- `src/Components/Forms/src/ClientValidation/DataTypeClientAdapter.cs` (email, url, creditcard, phone)
- `src/Components/Forms/src/ClientValidation/CompareClientAdapter.cs`
- `src/Components/Forms/src/ClientValidation/DefaultClientValidationAdapterProvider.cs`

All adapters are `internal sealed`. Each follows the pattern:
```csharp
internal sealed class RequiredClientAdapter : IClientValidationAdapter
{
    public void AddClientValidation(ClientValidationContext context) { ... }
    public string GetErrorMessage(ClientValidationContext context) { ... }
}
```

Error message resolution: call `attribute.FormatErrorMessage(context.DisplayName)`. The localization-aware `ErrorMessageProvider` path is a follow-up when #65539 lands.

---

## Phase 3: DefaultClientValidationService

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
- Creates `ClientValidationContext`, iterates adapters, collects attributes

---

## Phase 4: Component Integration

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

## Phase 5: DI Registration + Sample App Update

### 5a. Tests

**File to create:**
- `src/Components/Web/test/Forms/ClientValidationServiceCollectionExtensionsTest.cs`

**Tests:**
- `AddClientSideValidation_RegistersDefaultAdapterProvider` — resolves `IClientValidationAdapterProvider`
- `AddClientValidationAdapterProvider_RegistersCustomProvider` — custom provider is resolved

### 5b. Implementation

**Files to create:**
- `src/Components/Web/src/Forms/ClientValidationServiceCollectionExtensions.cs` (or in Forms assembly)

**Sample app changes:**
- `src/Components/Samples/BlazorSSR/Program.cs` — add `builder.Services.AddClientSideValidation()`
- `src/Components/Samples/BlazorSSR/Pages/Contact.razor` — add `<ClientSideValidator />` inside EditForm
- Remove hardcoded `data-val-*` from `ContactManual.razor` (or keep for comparison and create a third page `ContactAuto.razor`)
- Verify rendered HTML matches the manually-authored version

### 5c. Integration Verification

- Build the sample app
- Run and verify rendered HTML contains correct `data-val-*` attributes
- Test enhanced navigation (navigate between pages, verify attributes persist)
- Test form submission with JS validation (empty form shows errors)

---

## File Summary

### New files (Phase 1-5)

| File | Phase | Type |
|------|-------|------|
| `src/Components/Forms/src/IClientValidationService.cs` | 1 | Interface |
| `src/Components/Forms/src/IClientValidationAdapter.cs` | 1 | Interface |
| `src/Components/Forms/src/IClientValidationAdapterProvider.cs` | 1 | Interface |
| `src/Components/Forms/src/ClientValidationContext.cs` | 1 | Class |
| `src/Components/Forms/test/ClientValidation/ClientValidationContextTest.cs` | 1 | Test |
| `src/Components/Forms/src/ClientValidation/RequiredClientAdapter.cs` | 2 | Adapter |
| `src/Components/Forms/src/ClientValidation/StringLengthClientAdapter.cs` | 2 | Adapter |
| `src/Components/Forms/src/ClientValidation/MinLengthClientAdapter.cs` | 2 | Adapter |
| `src/Components/Forms/src/ClientValidation/MaxLengthClientAdapter.cs` | 2 | Adapter |
| `src/Components/Forms/src/ClientValidation/RangeClientAdapter.cs` | 2 | Adapter |
| `src/Components/Forms/src/ClientValidation/RegexClientAdapter.cs` | 2 | Adapter |
| `src/Components/Forms/src/ClientValidation/DataTypeClientAdapter.cs` | 2 | Adapter |
| `src/Components/Forms/src/ClientValidation/CompareClientAdapter.cs` | 2 | Adapter |
| `src/Components/Forms/src/ClientValidation/DefaultClientValidationAdapterProvider.cs` | 2 | Internal |
| `src/Components/Forms/test/ClientValidation/BuiltInAdapterTests.cs` | 2 | Test |
| `src/Components/Forms/test/ClientValidation/DefaultClientValidationAdapterProviderTest.cs` | 2 | Test |
| `src/Components/Forms/src/ClientValidation/DefaultClientValidationService.cs` | 3 | Internal |
| `src/Components/Forms/test/ClientValidation/DefaultClientValidationServiceTest.cs` | 3 | Test |
| `src/Components/Web/src/Forms/ClientSideValidator.cs` | 4 | Component |
| `src/Components/Web/test/Forms/ClientSideValidatorTest.cs` | 4 | Test |
| `src/Components/Web/test/Forms/ValidationMessageTest.cs` | 4 | Test |
| `src/Components/Web/test/Forms/ValidationSummaryTest.cs` | 4 | Test |
| DI extension method file (location TBD) | 5 | Extension |
| DI extension test file | 5 | Test |

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
