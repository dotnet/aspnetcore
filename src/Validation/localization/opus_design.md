# Localization & Customization Design for Microsoft.Extensions.Validation

## Table of Contents

1. [Current State Analysis (main branch)](#current-state-analysis-main-branch)
2. [Upstream Design Principles](#upstream-design-principles)
3. [Comparison with MVC Validation Localization](#comparison-with-mvc-validation-localization)
4. [Client-Side Validation Considerations (Blazor)](#client-side-validation-considerations-blazor)
5. [Design Questions & Proposed Solutions](#design-questions--proposed-solutions)
6. [Summary of Open Questions](#summary-of-open-questions)

---

## Current State Analysis (main branch)

### Package Architecture

`Microsoft.Extensions.Validation` is a framework-independent validation package with these key characteristics:

- **Source-generated validation metadata** via `ValidationsGenerator` (`IIncrementalGenerator`)
- **Resolver-based architecture**: `IValidatableInfoResolver` implementations registered in `ValidationOptions.Resolvers`; source-generated resolver inserted at position 0
- **Async-first**: `IValidatableInfo.ValidateAsync()` is the only validation method
- **Framework-agnostic core**: depends only on `Microsoft.Extensions.DependencyInjection.Abstractions` and `Microsoft.Extensions.Options`
- **No localization dependency**: no reference to `Microsoft.Extensions.Localization.Abstractions` or any `IStringLocalizer` type

### Current Types

| Type | Role |
|------|------|
| `IValidatableInfo` | Core interface: `ValidateAsync(object?, ValidateContext, CancellationToken)` |
| `IValidatableInfoResolver` | Resolves `IValidatableInfo` for a `Type` or `ParameterInfo`; first-match-wins chain |
| `ValidatableTypeInfo` | Abstract base for type validation; holds `ValidatablePropertyInfo[]` members; virtual `ValidateAsync` |
| `ValidatablePropertyInfo` | Abstract base for property validation; abstract `GetValidationAttributes()`; virtual `ValidateAsync` |
| `ValidatableParameterInfo` | Abstract base for parameter validation; abstract `GetValidationAttributes()` |
| `ValidateContext` | Sealed mutable context: `ValidationContext`, `ValidationOptions`, `CurrentValidationPath`, `CurrentDepth`, `ValidationErrors`, `OnValidationError` event |
| `ValidationOptions` | Configuration: `Resolvers` list, `MaxDepth` (default 32) |
| `ValidationErrorContext` | `readonly struct` raised via `OnValidationError` event: `Name`, `Path`, `Errors`, `Container` |
| `SkipValidationAttribute` | Opt-out attribute for properties, parameters, or types |
| `ValidatableTypeAttribute` | Discovery marker for the source generator |
| `ValidationServiceCollectionExtensions` | `AddValidation()` DI registration |
| `RuntimeValidatableParameterInfoResolver` | Runtime fallback resolver for `ParameterInfo` (uses reflection) |

### How Validation Currently Works

**Error message flow**: All error messages come directly from `ValidationAttribute.GetValidationResult()`. The package passes the result's `ErrorMessage` string into the error dictionary unchanged. There is no interception, transformation, or localization of error messages at any point.

**Display names**: The source generator reads `[Display(Name = "...")]` at compile time and emits the `Name` value as a string literal in the generated code (e.g., `displayName: "Customer Age"`). This value is set on `ValidationContext.DisplayName` before `GetValidationResult()` is called. Display names are never localized — `Display.ResourceType` is ignored by the generator.

**Error accumulation**: Errors are stored in `Dictionary<string, string[]>`. The `AddValidationError` method replaces errors for a key; `AddOrExtendValidationErrors` appends to existing arrays. The `OnValidationError` event fires for each error, carrying a `ValidationErrorContext` struct.

### Validation Order (from upstream design)

The `ValidatableTypeInfo.ValidateAsync` method follows this order:
1. **Null check** — returns immediately if value is null
2. **Depth limit** — checks `MaxDepth` (default 32) to prevent stack overflows
3. **Property validation** — iterates `Members`, validates `RequiredAttribute` first (other attributes skipped if required fails), then other attributes, then recurses into complex/enumerable properties
4. **Inherited member validation** — validates members from base types
5. **Type-level attribute validation** — validates `ValidationAttribute`s on the type itself (skipped if property errors found)
6. **`IValidatableObject`** — calls `Validate()` if the type implements it (skipped if type-level attribute errors found)

### Current Integration Points

**Minimal APIs** (`ValidationEndpointFilterFactory` in `src/Http/Routing/src/`):
- Creates `ValidateContext` with no localization
- Sets `ValidationOptions` from `IOptions<ValidationOptions>`
- Creates `System.ComponentModel.DataAnnotations.ValidationContext` per argument with `HttpContext.RequestServices`
- Returns `HttpValidationProblemDetails` on failure via `IProblemDetailsService` if available

**Blazor** (`EditContextDataAnnotationsExtensions` in `src/Components/Forms/src/`):
- Creates `ValidateContext` with no localization
- Subscribes to `OnValidationError` to map validation paths → `FieldIdentifier`
- Calls `ValidateAsync` synchronously (throws if not completed)
- Falls back to `System.ComponentModel.DataAnnotations.Validator.TryValidateObject()` if no `IValidatableInfo` resolved

### Source Generator Behavior

The generator (`src/Validation/gen/`):
1. Discovers types from `MapGet/MapPost/...` endpoint parameters and `[ValidatableType]`-annotated types
2. Discovers types from `[JsonDerivedType]` attributes for polymorphic validation
3. Emits `GeneratedValidatableTypeInfo` / `GeneratedValidatablePropertyInfo` subclasses with type-checked if-chains in `TryGetValidatableTypeInfo`
4. Emits a `ValidationAttributeCache` that lazily loads `ValidationAttribute[]` via reflection (`GetCustomAttributes<ValidationAttribute>`) and caches per `(Type, PropertyName)` in a `ConcurrentDictionary`
5. Intercepts `AddValidation()` via `[InterceptsLocation]` to insert the generated resolver at position 0
6. Display names are emitted as string literals from `[Display(Name)]` or the property name itself
7. **Does NOT emit any localization code, error message metadata, or `ErrorMessage` values**

### What the Package Does NOT Have

- No `IStringLocalizer` or `IStringLocalizerFactory` integration
- No `ErrorMessageLocalizerProvider` delegate
- No error message transformation or interception hooks
- No `MaxValidationErrors` limit
- No `ValidateComplexTypesIfChildValidationFails` option
- No client-side validation support
- No adapter abstraction for per-attribute customization
- No public API for error reporting from custom validators (internal `AddValidationError` methods)
- No awareness of `ErrorMessageResourceType` / `ErrorMessageResourceName` on attributes

---

## Upstream Design Principles

Based on the [upstream design document](https://github.com/captainsafia/minapi-validation-support/blob/main/README.md), the key design principles are:

1. **Source generation first**: Compile-time discovery of validatable types; runtime reflection only as fallback for `ParameterInfo` resolution and `ValidationAttribute` loading
2. **Resolver chain**: `IValidatableInfoResolver` implementations queried in order; first match wins. Custom resolvers can be inserted at any position for priority control
3. **Framework independence**: The core `Microsoft.Extensions.Validation` package has no ASP.NET Core dependency; integration is done in framework-specific layers (`ValidationEndpointFilterFactory`, `EditContextDataAnnotationsExtensions`)
4. **`ValidationAttribute` as the primary validation mechanism**: The system builds on top of `System.ComponentModel.DataAnnotations`, not replacing it
5. **Extensibility through custom resolvers**: Users create custom `IValidatableInfoResolver` implementations with custom `ValidatableTypeInfo`/`ValidatablePropertyInfo`/`ValidatableParameterInfo` subclasses to define completely custom validation behavior
6. **Error format**: `Dictionary<string, string[]>` with dot-separated paths as keys — directly compatible with `HttpValidationProblemDetails`
7. **Polymorphic/recursive type support**: Built on `[JsonDerivedType]` discovery and depth limiting

---

## Comparison with MVC Validation Localization

### MVC's Localization Architecture (for reference)

MVC has a mature, multi-layered localization system for validation:

1. **`MvcDataAnnotationsLocalizationOptions.DataAnnotationLocalizerProvider`** — `Func<Type, IStringLocalizerFactory, IStringLocalizer>` that creates per-model-type localizers
2. **`IValidationAttributeAdapterProvider`** → `AttributeAdapterBase<T>` — per-attribute-type adapters that intercept error messages *before* `FormatErrorMessage()` is called
3. **Pre-format interception** — adapters read `attribute.ErrorMessage`, check if `ErrorMessageResourceType` is set (if so, skip `IStringLocalizer`), and call `IStringLocalizer[errorMessage, args]` with attribute-specific format arguments
4. **Display name localization** — `ModelMetadata.GetDisplayName()` is localizable through `[Display(ResourceType)]` or `IStringLocalizer`
5. **Configurable via `AddDataAnnotationsLocalization()`** extension method
6. **Dual-path**: resource-based (`.resx` via `ErrorMessageResourceType`) and `IStringLocalizer`-based localization coexist, with the adapter deciding which path to use
7. **Client-side validation** — `IClientModelValidator` / `AttributeAdapterBase<T>.AddValidation()` emits `data-val-*` HTML attributes consumed by jQuery Unobtrusive Validation

### Key Architectural Differences

| Aspect | MVC | Microsoft.Extensions.Validation |
|--------|-----|-------------------------------|
| **Error message source** | Adapter intercepts before formatting | `GetValidationResult()` returns final message |
| **Localization timing** | Before `FormatErrorMessage()` | None (no localization at all) |
| **Attribute awareness** | Per-type adapters know format args | All attributes treated uniformly |
| **Display names** | Runtime `ModelMetadata.GetDisplayName()` | Compile-time string literal |
| **Dependency on localization** | `Microsoft.Extensions.Localization.Abstractions` | None |
| **Reflection** | Pervasive (`ModelMetadata`, attribute discovery) | Minimal (source-generated structure, reflection for attributes only) |
| **Configuration** | `MvcOptions`, `MvcDataAnnotationsLocalizationOptions`, `MvcViewOptions` | `ValidationOptions` only |
| **Client-side** | `IClientModelValidator` → `data-val-*` → jQuery Unobtrusive | N/A |

### What Should and Should Not Be Borrowed

**Worth borrowing (concepts)**:
- The `DataAnnotationLocalizerProvider` delegate pattern — simple, flexible, framework-agnostic
- The pre-format interception idea — localize the template, then format with args
- Awareness of `ErrorMessageResourceType` to avoid double-localization
- `MaxValidationErrors` to prevent unbounded error accumulation
- The idea of exposing validation *metadata* (rule type, constraints, error template) separately from the validation *execution* — this is what enables client-side validation

**Should NOT be borrowed**:
- The full adapter class hierarchy (`AttributeAdapterBase<T>`, per-attribute sealed adapters) — too heavy, too much reflection, not source-gen friendly
- `ModelMetadata` / `IModelMetadataProvider` — the resolver pattern is simpler and better
- The jQuery Unobtrusive `data-val-*` attribute approach — Blazor can do better with direct C#-to-JS metadata transfer
- `ModelStateDictionary` integration — framework-specific

---

## Client-Side Validation Considerations (Blazor)

### Current Blazor Validation Architecture

Blazor currently has **no JavaScript-based client-side validation**. All validation runs in C#/.NET:

- `EditContext` manages validation state, fires events (`OnFieldChanged`, `OnValidationRequested`, `OnValidationStateChanged`)
- `DataAnnotationsValidator` component enables validation via `EditContextDataAnnotationsExtensions.EnableDataAnnotationsValidation()`
- `InputBase<T>` renders CSS classes (`valid`, `invalid`, `modified`) and `aria-invalid="true"` based on `EditContext` state
- `ValidationMessage<T>` and `ValidationSummary` display errors from `ValidationMessageStore`
- The entire validation roundtrip requires a server call (Blazor Server) or WASM execution — no pure-JS validation

### Why Client-Side JS Validation Matters for Blazor

For **Blazor SSR (Static Server Rendering)** and **Blazor Server with enhanced navigation**, forms may need instant client-side validation without a roundtrip. MVC solved this with jQuery Unobtrusive Validation parsing `data-val-*` attributes. Blazor could implement a similar system without jQuery dependency.

### How This Affects Localization Design

The localization system must be designed so that the **same localized error messages** can be used for both:
1. **Server-side validation** — C# code in `ValidatablePropertyInfo.ValidateAsync()`
2. **Client-side validation** — JavaScript code running in the browser

This means the localization design must support **extracting validation metadata** (rule type, constraints, error message template, display name) in a form that can be serialized to the client. Key implications:

#### Implication 1: Error Message Templates Must Be Accessible Separately from Execution

MVC's `data-val-required="The {0} field is required."` pattern works because the *template* is available before validation runs. In the new system, if localization only happens inside `GetValidationResult()`, the template is never exposed — only the formatted result. The localization design should make templates (localized or not) available as metadata.

#### Implication 2: Validation Rule Metadata Must Be Extractable

For client-side validation, the system needs to expose per-property metadata like:
- Rule type: `required`, `range`, `regex`, `stringlength`, etc.
- Constraints: `min`, `max`, `pattern`, `minLength`, `maxLength`
- Error message template (localized): `"The {0} field must be between {1} and {2}."`
- Display name (localized): `"Customer Age"`

This metadata is already partially available in `ValidatablePropertyInfo` (via `GetValidationAttributes()`). The localization design should ensure that when error message templates are localized, the localized template (not the formatted result) is available for extraction.

#### Implication 3: Display Name Localization Must Be Resolvable at Render Time

For client-side validation, the localized display name must be available when rendering the form (to embed in HTML or pass to JS), not just when validation runs. This argues for display name localization being resolvable from `ValidatablePropertyInfo` metadata, not just inside `ValidateAsync`.

#### Implication 4: Localization Should Not Depend on `ValidationAttribute` Instance State

Client-side validation cannot call `ValidationAttribute.GetValidationResult()` — it runs in JS. So the localization of error message templates must be possible without executing the attribute. This reinforces the case for separating the localization key/template from the validation execution.

### Sketch of a Possible Blazor Client-Side Validation Flow

```
Server (C#):                                Client (JS):
─────────────────────────────────────────────────────────────
ValidatableTypeInfo                         
    │                                       
    ├─ GetValidationMetadata()              
    │   Returns per-property:               
    │   - rules: [{type:"required", msg:"..."}, 
    │             {type:"range", min:1, max:100, msg:"..."}]
    │   - displayName: "Customer Age"       
    │                                       
    ▼                                       
Blazor renders <input>                      
    with data-validation="{json}"           
    or via JS interop pushes metadata       
                                            JS validation library
                                                │
                                                ├─ Parses metadata
                                                ├─ Validates on input/blur/submit
                                                ├─ Uses localized msg templates
                                                └─ Updates DOM (error messages, CSS)
```

The localization design in `Microsoft.Extensions.Validation` should ensure that steps like "GetValidationMetadata()" can return *localized* templates and display names without running `GetValidationResult()`.

---

## Design Questions & Proposed Solutions

### DQ1: Should the Package Take a Dependency on `Microsoft.Extensions.Localization.Abstractions`?

**Context**: On `main`, the package has no localization dependency. MVC takes a dependency on `Microsoft.Extensions.Localization.Abstractions` for `IStringLocalizer` / `IStringLocalizerFactory`.

**Variants**:

**(A) Yes — add `Microsoft.Extensions.Localization.Abstractions` as a dependency**
This enables first-class `IStringLocalizer` support in `ValidateContext` and `ValidationOptions`. The abstractions package is lightweight (only interfaces). This is the approach MVC uses.

**(B) No — use a framework-agnostic delegate instead**
Define localization as a `Func<string errorMessage, string memberDisplayName, object? value, string>` delegate on `ValidationOptions` or `ValidateContext`. No dependency on localization abstractions. Consumers wire up `IStringLocalizer` themselves in the delegate. Keeps the package dependency-free.

**(C) Optional — make localization an add-on package or extension method**
Keep the core package dependency-free. Provide `AddValidationLocalization()` as an extension in a separate package (or in the framework integration layer) that adds the `IStringLocalizer` wiring.

**Client-side validation impact**: Client-side validation needs localized *templates*, not `IStringLocalizer` instances. A delegate-based approach **(B)** that returns localized strings is equally consumable whether the output goes to server validation or to client-side metadata extraction. **(A)** works too since the localizer would be called server-side before metadata is pushed to the client.

**Tradeoffs**:
- **(A)** is simplest for consumers, matches MVC convention, but adds a permanent dependency
- **(B)** is most flexible and dependency-free, but consumers must write more boilerplate
- **(C)** is cleanest architecturally but adds packaging/discoverability complexity

---

### DQ2: Where Should Localization Be Configured — `ValidationOptions` vs `ValidateContext`?

**Context**: Localization could be configured globally (on `ValidationOptions`, shared across all validations) or per-invocation (on `ValidateContext`, set by the framework integration).

**Variants**:

**(A) On `ValidationOptions` only (global)**
Simple, configured once at startup. But cannot vary per-request (e.g., per-culture in a multi-tenant app where culture is on the request).

**(B) On `ValidateContext` only (per-invocation)**
The framework integration (Minimal APIs, Blazor) sets localization on each `ValidateContext` instance. Maximum flexibility but more work for every integration point.

**(C) On `ValidationOptions` with per-invocation override on `ValidateContext`**
`ValidationOptions` provides the default; `ValidateContext` can override it. The global setting covers most cases, but per-request culture or localizer can be injected at the call site.

**Client-side validation impact**: For client-side metadata extraction (not triggered by a validation run), only `ValidationOptions`-level configuration is available — there is no `ValidateContext` at render time. This argues for **(A)** or **(C)** where the global setting is sufficient for metadata extraction.

**Tradeoffs**:
- **(A)** works if `IStringLocalizer` implementations are culture-aware internally (most are — they read `CultureInfo.CurrentUICulture`)
- **(B)** is correct but forces every consumer to wire up localization, and is not available at metadata-extraction time
- **(C)** is most flexible but adds API surface

---

### DQ3: How Should Error Messages Be Localized — Template vs Formatted?

**Context**: This is the most important design decision. `ValidationAttribute.GetValidationResult()` internally calls `FormatErrorMessage(displayName)` which uses `ErrorMessage` as a format string. MVC's adapters intercept *before* this call, localizing the template and then formatting. The new package currently just uses the result's `ErrorMessage` as-is.

For **client-side validation**, the localized *template* (e.g., `"The {0} field must be between {1} and {2}."`) must be available separately from the formatted result, so that JS code can substitute display names and parameters client-side.

**Variants**:

**(A) Pre-format: intercept `ErrorMessage` on the attribute before `GetValidationResult()`**
Before calling `attribute.GetValidationResult()`, set `attribute.ErrorMessage` to the localized string, then let `FormatErrorMessage` do the formatting with the localized template.

- Pros: Correct format argument substitution; the localized template is used by the attribute's own formatting logic
- Cons: Mutating `ValidationAttribute.ErrorMessage` is a side effect; attributes are shared/cached, so it must be restored after. Thread-safety concern if attributes are shared across requests.
- Client-side: The localized template is accessible at pre-format time and can be extracted for client metadata.

**(B) Post-format: localize the already-formatted message as a key lookup**
After `GetValidationResult()` returns, pass `result.ErrorMessage` through a localizer.

- Pros: No mutation of attributes, simple to implement
- Cons: The formatted message (e.g., `"The field Name is required."`) is a poor localization key. Only works if users set `ErrorMessage` to a custom key.
- Client-side: The formatted message cannot be decomposed back into template + args for JS use.

**(C) Metadata-based: expose localized templates as extractable metadata alongside validation execution**
Add a method to `ValidatablePropertyInfo` (or a new interface) that returns validation rule metadata including the localized error message *template* and format arguments, without running validation:

```csharp
public record struct ValidationRuleMetadata(
    string RuleType,           // "required", "range", "regex", etc.
    string ErrorMessageTemplate, // localized template: "The {0} field is required."
    string DisplayName,        // localized: "Customer Age"
    IReadOnlyDictionary<string, object>? Parameters  // { "min": 1, "max": 100 }
);

// On ValidatablePropertyInfo or new interface:
ValidationRuleMetadata[] GetValidationRuleMetadata(/* localization context */);
```

- Pros: Serves both server-side validation (format template + args) and client-side metadata extraction. Clean separation of concerns. Source-generator can emit this directly.
- Cons: Parallel metadata path alongside `GetValidationAttributes()` — must be kept in sync. More API surface.
- Client-side: **This is the only variant that fully enables client-side validation** since JS needs the template, display name, and constraint parameters separately.

**(D) Dual-hook: provide both pre-format and post-format callbacks**
```csharp
// On ValidationOptions or ValidateContext:
Func<ValidationAttribute, string? currentErrorMessage, string?>? OnFormatErrorMessage
Func<string errorMessage, string propertyPath, string>? OnErrorMessageCreated
```
- Pros: Maximum flexibility
- Cons: Two hooks to document and maintain; client-side still needs separate metadata extraction
- Client-side: The pre-format hook could be called at metadata-extraction time to get templates, but the API isn't designed for that use case.

**Recommendation**: **(C)** is the best long-term design because it serves both server and client validation. For v1, **(A)** or **(D)** can bridge the gap while **(C)** is developed. **(B)** should be avoided — it's fundamentally incompatible with client-side validation and proper localization.

---

### DQ4: Should There Be an Attribute Adapter Abstraction?

**Context**: MVC has `IValidationAttributeAdapterProvider` → `AttributeAdapterBase<T>` → 9 sealed adapters. The new package treats all attributes uniformly via `GetValidationResult()`.

**Variants**:

**(A) No adapter abstraction — keep it simple**
All attributes go through `GetValidationResult()`. Localization (if added) is handled via callbacks on `ValidationOptions`/`ValidateContext`. No per-attribute-type customization.

**(B) Lightweight per-attribute error message provider registered on `ValidationOptions`**
```csharp
public interface IValidationErrorMessageProvider
{
    string GetErrorMessage(ValidationAttribute attribute, string displayName, object? value);
}
```
Registered per attribute type. Checked before `GetValidationResult()`.

**(C) Source-generate format argument extraction per attribute type**
For known BCL attributes, the generator emits code that extracts format arguments (e.g., `min`, `max`, `pattern`) for localization and client-side metadata. Falls back to `GetValidationResult()` for unknown types.

**Client-side validation impact**: Client-side validation *requires* knowing the rule type and constraints. **(C)** is the only variant that enables this without runtime reflection on attribute properties. **(A)** is fine for server-only v1, but client-side validation will eventually need something like **(C)**.

**Recommendation**: **(A)** for v1, with **(C)** as the path forward when client-side validation is added.

---

### DQ5: How Should Display Names Be Localized?

**Context**: The generator emits `displayName: "Customer Age"` as a literal. `[Display(Name = "Key", ResourceType = typeof(Resources))]` is partially parsed — only `Name` is extracted, `ResourceType` is ignored.

**Variants**:

**(A) Generator emits resource-aware display name resolution**
If `[Display(ResourceType = typeof(R), Name = "Key")]` is present, emit:
```csharp
displayName: global::R.Key  // direct property access on the resource type
```
Zero reflection, zero allocation.

**(B) Runtime callback on `ValidationOptions`**
```csharp
public Func<Type declaringType, string propertyName, string defaultDisplayName, string>? DisplayNameProvider { get; set; }
```
Called at runtime to resolve display names.

**(C) Pass through `IStringLocalizer`**
If localization is configured, look up `displayName` via `IStringLocalizer[displayName]` before setting it on `ValidationContext.DisplayName`.

**(D) Keep static display names, document limitation**
Accept that display names are not localized in source-gen mode.

**Client-side validation impact**: The display name must be available at render time for client-side error message formatting. **(A)** and **(B)** both work — they produce the localized display name that can be embedded in client-side metadata. **(C)** also works if the localizer is available at render time. **(D)** means client-side messages would show unlocalized display names.

**Recommendation**: **(A)** for `ResourceType` cases (zero-cost, compile-time), **(B)** as runtime fallback.

---

### DQ6: How Should `ErrorMessageResourceType` / `ErrorMessageResourceName` Be Handled?

**Context**: `ValidationAttribute` has built-in resource-based localization. When set, `FormatErrorMessage()` reads from the resource type automatically. Adding `IStringLocalizer`-based localization must not double-localize these messages.

**Variants**:

**(A) Detect at source-gen time, emit a flag**
The generator can check whether `ErrorMessageResourceType` is set and emit metadata. At runtime, skip `IStringLocalizer` for those properties.

**(B) Detect at runtime before localizing**
Check `attribute.ErrorMessageResourceType != null`. If set, skip `IStringLocalizer`.

**(C) Let the user handle it**
Document that users should not combine `ErrorMessageResourceType` with `IStringLocalizer`.

**Client-side validation impact**: If `ErrorMessageResourceType` is set, the attribute's own `FormatErrorMessage()` produces the localized message. For client-side, we still need the *template* (which the resource provides). **(A)** is best — the generator can emit the resource lookup directly, producing the localized template at compile-time resolution.

**Recommendation**: **(A)** for correctness and client-side support.

---

### DQ7: Should There Be a `MaxValidationErrors` Limit?

**Context**: MVC has `MvcOptions.MaxModelValidationErrors`. The new package has `MaxDepth` but no error count limit.

**Variants**:

**(A) Add to `ValidationOptions`**
```csharp
public int MaxValidationErrors { get; set; } = 200;
```
**(B) Per-invocation on `ValidateContext`**
**(C) No limit**

**Recommendation**: **(A)** with **(B)** override.

---

### DQ8: Should `ValidateComplexTypesIfChildValidationFails` Be Configurable?

**Context**: Currently hardcoded: "if any property-level errors, skip type-level attribute validation and `IValidatableObject`". MVC has this as configurable.

**Recommendation**: Add to `ValidationOptions` **(A)** — simple, matches MVC.

---

### DQ9: Should `ValidateContext.AddValidationError` Be Public?

**Context**: Internal methods prevent custom `IValidatableInfo` implementations from participating in the error pipeline (localization, events).

**Variants**:

**(A) Make existing methods public**
**(B) Add a simplified public API**
```csharp
public void ReportError(string path, string errorMessage);
public void ReportErrors(string path, string[] errorMessages);
```

**Recommendation**: **(B)** — simpler, still fires events.

---

### DQ10: Should Validation Metadata Be Extractable for Client-Side Use?

**Context**: This is a new question driven by the client-side validation requirement. Currently, `ValidatablePropertyInfo` only exposes `GetValidationAttributes()` (protected) and `ValidateAsync()` (public). There is no way to extract structured validation rule metadata without running validation.

**Variants**:

**(A) Add a metadata extraction method to `ValidatablePropertyInfo`**
```csharp
public virtual ValidationRuleMetadata[] GetValidationRuleMetadata(ValidationMetadataContext context);
```
Where `ValidationMetadataContext` carries localization configuration. Returns rule type, constraints, localized error template, and localized display name.

**(B) Define a separate `IValidationMetadataProvider` interface**
```csharp
public interface IValidationMetadataProvider
{
    ValidationRuleMetadata[] GetRulesForProperty(Type declaringType, string propertyName);
}
```
Registered in DI. Implemented by the source generator. Blazor queries this at render time.

**(C) Expose `GetValidationAttributes()` as public and let consumers extract metadata themselves**
Simplest change — make the existing protected method public. Consumers (Blazor) interpret `ValidationAttribute` properties to build client-side rules.

**(D) Source-generate a JSON-serializable metadata model**
The generator emits a static method that returns validation rules as a serializable object graph. Blazor serializes this to JSON and embeds it in rendered HTML or pushes it via JS interop.

**Client-side validation impact**: This question is directly about client-side validation enablement.
- **(A)** is cleanest — metadata extraction is part of the validation info hierarchy
- **(B)** is most decoupled but adds a new interface and DI registration
- **(C)** is simplest but pushes all interpretation logic to consumers; each framework re-implements attribute-to-rule mapping
- **(D)** is most efficient for the client but tightly couples the generator to the output format

**Recommendation**: **(A)** for v1 of client-side support. The source generator can override `GetValidationRuleMetadata()` with precomputed metadata. For v0 (before client-side validation ships), **(C)** is a reasonable stepping stone.

---

### DQ11: How Should `ValidationAttribute[]` Be Obtained — Reflection vs Source Generation?

**Context**: The generated `ValidationAttributeCache` uses `GetCustomAttributes<ValidationAttribute>()` at runtime.

**Variants**:

**(A) Keep reflection-based with caching (current)** — acceptable for v1
**(B) Source-generate attribute instantiation for known BCL attributes**
**(C) Source-generate a static `ValidationAttribute[]` per property**

**Client-side validation impact**: If metadata extraction (DQ10) is source-generated, attribute instances may not be needed for client-side rules at all — the generator can emit constraint values directly. This reduces the urgency of eliminating reflection for attribute loading.

**Recommendation**: **(A)** for v1.

---

### DQ12: How Should Blazor's Synchronous Constraint Be Handled?

**Context**: `EditContextDataAnnotationsExtensions` calls `ValidateAsync` and throws if not completed synchronously.

**Variants**:

**(A) Add a synchronous `Validate` method to `IValidatableInfo`**
**(B) Add a `ValidateSync` extension method**
**(C) Keep current approach**

**Recommendation**: **(B)** to centralize the pattern.

---

### DQ13: Where Should the Localization Integration Live?

**Context**: The package is framework-independent. Where should the `IStringLocalizer` wiring go?

**Variants**:

**(A) In `Microsoft.Extensions.Validation` directly** — adds localization dependency
**(B) In framework integrations only** — core defines delegate shape, frameworks wire `IStringLocalizer`
**(C) In a separate `Microsoft.Extensions.Validation.Localization` package**

**Client-side validation impact**: Blazor needs localized templates at render time (not just validation time). If localization lives only in `ValidateContext` (per-invocation), Blazor's metadata extraction at render time can't access it. **(A)** or **(B)** with `ValidationOptions`-level configuration are both fine.

**Recommendation**: **(B)** — core defines `Func<...>?` on `ValidationOptions`, framework integrations wire `IStringLocalizer`.

---

### DQ14: What Is the Right Error Message Localization Granularity?

**Context**: MVC creates one `IStringLocalizer` per model type. Options range from per-attribute to global.

**Variants**:

**(A) Per-model-type (MVC's approach)** — `Func<Type, IStringLocalizerFactory, IStringLocalizer>`
**(B) Single localizer for all** — simplest
**(C) Full context delegate** — `Func<Type, string propertyName, Type attributeType, string errorMessage, string>`

**Client-side validation impact**: For client-side, all localized templates for a form are resolved at once at render time. Per-model-type **(A)** is natural — Blazor resolves templates for the model being rendered.

**Recommendation**: **(A)** — proven, matches MVC. The delegate can be specialized by consumers.

---

### DQ15: How Should Error Accumulation Be Optimized?

**Context**: Each new error for an existing key allocates a new `string[]`.

**Recommendation**: Keep current approach for v1 — error counts per key are small.

---

### DQ16: Should the Source Generator Emit Localization-Aware Code?

**Context**: The generator currently emits only structural metadata.

**Variants**:

**(A) No — keep generator localization-unaware** — v1 target
**(B) Emit `ErrorMessage` metadata per property** — enables localization key extraction without attribute access
**(C) Emit `Display.ResourceType` resolution** — `global::R.Key` for display names
**(D) Emit validation rule metadata** — types, constraints, templates for client-side use

**Client-side validation impact**: **(D)** is the eventual goal — the generator emits everything needed for client-side validation rules. **(C)** is a high-value incremental step.

**Recommendation**: **(A)** for v1, **(C)** as quick win, **(D)** when client-side validation is designed.

---

## Summary of Open Questions

| # | Question | Recommended Direction | Priority |
|---|----------|----------------------|----------|
| DQ1 | Dependency on Localization.Abstractions? | Framework-agnostic delegate, no dependency **(B)** | **Critical** — foundational decision |
| DQ2 | Configuration on `ValidationOptions` vs `ValidateContext`? | Both, with `ValidateContext` override **(C)**; but `ValidationOptions` must be sufficient for metadata extraction | **Critical** |
| DQ3 | How to localize error messages — template vs formatted? | Metadata-based **(C)** long-term; pre-format hook **(A)** for v1 | **Critical** — correctness + client-side compatibility |
| DQ4 | Adapter abstraction? | No adapters for v1 **(A)**; source-gen metadata **(C)** when client-side is added | Low |
| DQ5 | Display name localization? | Generator emits `ResourceType` resolution **(A)** + runtime callback **(B)** | High |
| DQ6 | `ErrorMessageResourceType` awareness? | Source-gen flag **(A)** | High — prevents double-localization |
| DQ7 | Max validation errors? | `ValidationOptions` **(A)** + `ValidateContext` override **(B)** | High |
| DQ8 | `ValidateComplexTypesIfChildValidationFails`? | Add to `ValidationOptions` **(A)** | Medium |
| DQ9 | Public error reporting API? | Simplified public method **(B)** | High |
| DQ10 | Extractable validation metadata for client-side? | Method on `ValidatablePropertyInfo` **(A)** long-term; expose `GetValidationAttributes()` **(C)** for v1 | **Critical** for client-side |
| DQ11 | Attribute loading — reflection vs source-gen? | Keep reflection with caching **(A)** for v1 | Low |
| DQ12 | Blazor sync constraint? | `ValidateSync` extension **(B)** | Low |
| DQ13 | Where does localization integration live? | Framework integrations wire the delegate **(B)** | **Critical** |
| DQ14 | Localization granularity? | Per-model-type **(A)**, matching MVC | Medium |
| DQ15 | Error accumulation optimization? | Keep current **(C)** for v1 | Low |
| DQ16 | Generator localization awareness? | No for v1 **(A)**, `ResourceType` **(C)** as quick win, full metadata **(D)** for client-side | Medium |

### Key Design Constraint: Client-Side Validation Compatibility

Any localization design must ensure that:

1. **Localized error message templates** (not just formatted results) are extractable from `ValidatablePropertyInfo` / `ValidatableTypeInfo` at render time — before validation runs
2. **Localized display names** are available at render time for embedding in client-side metadata
3. **Validation rule constraints** (min, max, pattern, etc.) are extractable alongside templates
4. The localization mechanism configured on `ValidationOptions` is accessible outside of a `ValidateContext` (since metadata extraction happens at render time, not validation time)

This rules out designs where localization happens *only* inside `ValidateAsync()` or *only* on `ValidateContext`. The `ValidationOptions`-level localization configuration must be the primary mechanism, with `ValidateContext` as an optional per-invocation override.

### Suggested Incremental Roadmap

**v1: Server-side localization**
1. Add `Func<string errorMessage, string>? ErrorMessageTransformer` to `ValidationOptions` (and optionally `ValidateContext`)
2. Call it in `AddValidationError` / `AddOrExtendValidationErrors` before storing the error
3. Framework integrations wire `IStringLocalizer` into the delegate
4. Document that users should set `ErrorMessage` to a resource key on their attributes
5. Make `GetValidationAttributes()` public or add `ReportError()` API

**v2: Display name & resource-type support**
1. Generator emits `Display.ResourceType` resolution (DQ5-A, DQ16-C)
2. Generator emits `ErrorMessageResourceType` awareness flag (DQ6-A)
3. Add `DisplayNameProvider` callback to `ValidationOptions` (DQ5-B)

**v3: Client-side validation metadata**
1. Add `GetValidationRuleMetadata()` to `ValidatablePropertyInfo` (DQ10-A)
2. Source-generate metadata for known BCL attributes (DQ4-C, DQ16-D)
3. Blazor extracts metadata at render time, serializes to JS
4. Implement JS validation library (non-jQuery) that consumes the metadata
