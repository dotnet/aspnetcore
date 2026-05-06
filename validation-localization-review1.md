# Validation localization implementation review

Branch: `oroztocil/validation-localization3` (WIP commit `a2e90a5c89` — "Core implementation").

Scope: review of `Microsoft.Extensions.Validation` localization pipeline introduced in this branch (`ValidationLocalizer`, `ValidationOptions` additions, attribute formatters, integration in Blazor `EditContextDataAnnotationsExtensions` and Minimal-API `ValidationEndpointFilterFactory`). Goal of the change: extract message resolution into a reusable `ValidationLocalizer` so it can also be used outside of validation execution (Blazor SSR client-side validation rule emission).

---

## 1. Summary of the design

- `Microsoft.Extensions.Validation.Localization.ValidationLocalizer` is a new public, sealed class with two methods:
  - `ResolveDisplayName(string? displayName, Type? displayResource, Type? declaringType)`
  - `ResolveErrorMessage(ValidationAttribute attribute, string displayName, Type? declaringType)`
  - constructed from `(ValidationOptions options, IStringLocalizerFactory? factory)`; reads `LocalizerProvider`, `ErrorMessageKeyProvider`, `AttributeFormatters` off the options at construction time
  - caches resolved `IStringLocalizer` per declaring type in a `ConcurrentDictionary`
- `ValidationOptions` gains `LocalizerProvider`, `ErrorMessageKeyProvider`, `AttributeFormatters` (with built-in formatters for the standard multi-arg attributes).
- `ValidateContext` gains a `ValidationLocalizer` property that lazily falls back to `new ValidationLocalizer(ValidationOptions, null)` if not assigned.
- `AddValidation()` registers the localizer via `services.TryAddSingleton<ValidationLocalizer>()`.
- `ValidationEndpointFilterFactory` resolves `ValidationLocalizer` via `GetRequiredService` and assigns it on the per-request `ValidateContext`.
- `EditContextDataAnnotationsExtensions` (Blazor) optionally resolves `ValidationLocalizer` from the service provider and assigns it on the `ValidateContext`.
- The validation pipeline (`ValidatableTypeInfo` / `ValidatablePropertyInfo` / `ValidatableParameterInfo`) reads `context.ValidationLocalizer` and calls `ResolveDisplayName` / `ResolveErrorMessage` per member.

---

## 2. Goal evaluation

> Be able to use the localization processing outside of validation execution itself.

The split between configuration (`ValidationOptions`) and processing (`ValidationLocalizer`) is the right shape for the goal. A consumer in Blazor SSR can grab `IServiceProvider.GetService<ValidationLocalizer>()` and call `ResolveErrorMessage` for each attribute it intends to render, then emit the result into HTML. This works because both methods are pure functions of `(attribute, displayName, declaringType)` plus ambient `CurrentUICulture` — no `ValidateContext`, no model instance.

**However, the goal is currently blocked** by the DI registration issue (§3.1). After that is fixed, the design will deliver on the goal.

---

## 3. Findings

### 3.1 (Blocking) `ValidationLocalizer` cannot be resolved from DI

`AddValidation` registers:

```csharp
services.TryAddSingleton<ValidationLocalizer>();
```

The only public constructor is:

```csharp
public ValidationLocalizer(ValidationOptions options, IStringLocalizerFactory? factory)
```

- `ValidationOptions` is **not** a registered service — only `IOptions<ValidationOptions>` is. DI cannot satisfy this parameter.
- `IStringLocalizerFactory?` being declared nullable in C# does **not** make it optional from the container's perspective. The container will attempt to resolve it and throw `InvalidOperationException` if missing.

**Consequences (cascading):**

- `ValidationEndpointFilterFactory.Create` calls `context.ApplicationServices.GetRequiredService<ValidationLocalizer>()`. This throws at endpoint materialization for any app that calls `AddValidation()` without also calling `AddLocalization()`. Note the resolution happens unconditionally — even before the loop discovers there are no validatable parameters, so endpoints with no validation requirements still pay the cost / suffer the throw.
- Blazor's `EditContextDataAnnotationsExtensions` uses `_serviceProvider?.GetService<ValidationLocalizer>()`. `GetService` returns `null` only when the descriptor is unregistered — if the descriptor exists but cannot be constructed, it throws. So the optional retrieval pattern does not protect Blazor either.

**Fix:** register via a factory that pulls `IOptions<ValidationOptions>.Value` and uses `GetService` (not required) for the factory:

```csharp
services.TryAddSingleton(sp => new ValidationLocalizer(
    sp.GetRequiredService<IOptions<ValidationOptions>>().Value,
    sp.GetService<IStringLocalizerFactory>()));
```

### 3.2 (Blocking) Minimal API endpoint filter resolves the localizer too eagerly

```csharp
var validationLocalizer = context.ApplicationServices.GetRequiredService<ValidationLocalizer>();
// ... then the loop may discover no validatable parameters and return next ...
```

The localizer is resolved **before** the parameter scan. After fixing §3.1, this is just a minor wasted resolution; before the fix, it makes endpoints fail even when they have no validatable parameters. Move the resolution after the `if (validatableParameters is null …) return next;` check.

### 3.3 (Blocking) `ErrorMessageResourceType` is no longer bypassed

`ValidationLocalizer.ResolveErrorMessage` does not check `attribute.ErrorMessageResourceType` before consulting `_keyProvider`. The previous WIP (`validation-localization2`) explicitly skipped:

```csharp
if (attribute.ErrorMessageResourceType is not null) return null;
```

Removing that means: if an attribute has `ErrorMessageResourceType` set (so `ErrorMessage` is null) **and** the user configured a global `ErrorMessageKeyProvider` (e.g. for built-in attributes), the key provider runs against the resource-backed attribute and may overwrite its already-localized message with a different localizer lookup. That's a behavior change that breaks composition of "convention-based localization for built-in attributes" with "explicit `ResourceType` for one specific attribute".

**Fix:** restore the early-return when `ErrorMessageResourceType` (or `ErrorMessageResourceName`) is set — both branches of the lookup-key resolution should be skipped, since the attribute will produce its own localized result via `GetValidationResult`.

### 3.4 (Blocking) `DisplayAttribute.ResourceType` is not honored

Three connected gaps:

1. `ValidationLocalizer.ResolveDisplayName` has a literal TODO comment for the `displayResource` branch — it never reads the static resource property.
2. The source generator (`ValidationsGenerator.Emitter.cs:64`) still emits the 4-argument `(containingType, propertyType, name, displayName)` constructor call and never sets `displayResource`. The generator's `ValidatableProperty` model has no `DisplayResourceType` field; `ISymbolExtensions.GetDisplayName` reads only the `Name` named argument.
3. Runtime parameter discovery (`RuntimeValidatableParameterInfoResolver.GetDisplayName` — wait, this isn't in the runtime resolver in this branch; it's in the endpoint filter) and `ValidationEndpointFilterFactory.GetDisplayName` use `displayAttribute.Name` rather than `displayAttribute.GetName()` and never propagate `ResourceType`.

The new `Type? displayResource` constructor parameter exists but nothing populates it. That regresses the static-resource-based display name functionality even without enabling the new `ValidationLocalizer`. (The previous WIP solved this with a `displayNameAccessor: Func<string>?` parameter that wrapped `displayAttribute.GetName()`.)

**Fix:** Either re-introduce the `displayNameAccessor: Func<string>?` approach (simpler, no reflection at runtime, no trimming concerns) or finish the reflection-based `displayResource` path. If the latter, also annotate `Type? displayResource` parameters and stored properties with `[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]` for AOT/trimming. Then update the source generator (`ValidatableProperty`, `TypesParser`, `Emitter`) and the runtime resolver / endpoint filter to flow the resource type through.

### 3.5 Blazor `OnFieldChanged` bypasses the localizer entirely

```csharp
private void OnFieldChanged(...)
{
    Validator.TryValidateProperty(propertyValue, validationContext, results);
    ...
}
```

Field-change validation goes through `Validator.TryValidateProperty`, which has no awareness of `ValidationLocalizer` or `ValidationOptions`. `TryValidateTypeInfo` (full validation) does use the localizer. Result: a Blazor user typing in a single field gets non-localized messages, but pressing submit shows localized ones. This is a user-visible inconsistency.

**Fix options:** route field-level validation through the same `ValidatableTypeInfo`-based path when type info is available (locating the `ValidatablePropertyInfo` for the changed field), or document the limitation and disable the localizer-only behavior for field-level validation explicitly.

### 3.6 Nullable contract violation when assigning the localizer in Blazor

`EditContextDataAnnotationsExtensions`:

```csharp
private readonly ValidationLocalizer? _validationLocalizer;
...
var validateContext = new ValidateContext
{
    ...
    ValidationLocalizer = _validationLocalizer  // CS8601: assigning ValidationLocalizer? to non-nullable
};
```

`ValidateContext.ValidationLocalizer` is declared as non-nullable `ValidationLocalizer` with a non-nullable setter. The property has fallback logic for null in the getter, but the setter accepts `null` only because of the absent runtime null check. This will produce a nullable warning (likely warnings-as-errors in this repo) and weakens the contract.

**Fix:** either declare the setter `value` as `ValidationLocalizer?` (matching the field) or only assign when non-null:

```csharp
if (_validationLocalizer is not null)
{
    validateContext.ValidationLocalizer = _validationLocalizer;
}
```

The latter preserves the lazy fallback in the getter.

### 3.7 Parameter validation always uses `typeof(object)` as the resource source

For `ValidatableParameterInfo`, both `ResolveDisplayName` and `ResolveErrorMessage` are called with `declaringType: null`. `GetStringLocalizer` then falls back to `typeof(object)`. With the default `LocalizerProvider`, that means `factory.Create(typeof(object))` — pointing at `object.resx`, which won't exist.

So Minimal-API parameter validation only localizes if the user sets a custom `LocalizerProvider` that ignores the type argument (shared resource) or registers `IStringLocalizer` for `typeof(object)`. This is a usability footgun for the most basic parameter validation scenario.

**Fix options:**

- Carry the parameter's containing method's `DeclaringType` through `ValidatableParameterInfo` (extra constructor parameter) and pass it as the resource source.
- Or document that Minimal-API parameter localization requires shared-resource configuration.
- Or fall back to a more useful default (e.g., the entry-point assembly's resource).

### 3.8 `ValidationOptions` has weak DI guarantees but `ValidationLocalizer` captures most of it eagerly

`ValidationLocalizer` constructor copies `LocalizerProvider`, `ErrorMessageKeyProvider` into private fields and caches `AttributeFormatters` by reference. Because the factory uses `IOptions<ValidationOptions>.Value`, all `IConfigureOptions` and `IPostConfigureOptions` callbacks run before `Value` is materialized, so capture-at-construction is normally fine.

But: `AttributeFormatters` is a long-lived mutable `Dictionary<Type, …>`. If anyone mutates it after the singleton `ValidationLocalizer` is built (at runtime, not during options configuration), the changes are visible (because we hold a reference), but reads are not thread-safe versus writes. For startup-only configuration (the documented pattern), this is a non-issue.

**Fix (defensive):** if mutation outside of options configuration is to be supported, switch the registry's storage to `ConcurrentDictionary`. Otherwise, leave it and rely on the convention.

### 3.9 `CompareAttributeFormatter` likely uses the wrong other-property name

```csharp
internal sealed class CompareAttributeFormatter(CompareAttribute attribute) : IValidationAttributeFormatter
{
    public string FormatErrorMessage(...) =>
        string.Format(culture, messageTemplate, displayName, attribute.OtherProperty);
}
```

`CompareAttribute.OtherProperty` is the raw property name. The framework's own `FormatErrorMessage` substitutes `OtherPropertyDisplayName` (populated during validation) into `{1}` so users see "Confirm password must match Password", not "Confirm password must match PasswordHash". This formatter regresses that behavior under localization.

**Fix:** prefer `attribute.OtherPropertyDisplayName ?? attribute.OtherProperty`.

### 3.10 Cache key collision risk in `_localizerCache` if `LocalizerProvider` returns context-dependent results

`_localizerCache` keys by declaring type, but the user-supplied `LocalizerProvider(type, factory)` could in principle compose multiple sub-localizers into one without being purely a function of `type`. Today the contract is implicit: the provider is called once per declaring type and the result memoized. That's reasonable but should be documented in the XML doc on `LocalizerProvider`.

Defensive: a `LocalizerProvider` returning `null` would currently NRE on the next access (`localizer[name]`). Add a `?? throw` in `GetStringLocalizer`.

### 3.11 Minor: `ResolveDisplayName` return type and contract

`ResolveDisplayName` is declared `string?` but in practice never returns `null` — the only `null` path is "input was null/empty", in which case it returns the input (also null). Callers (`var displayName = localizer.ResolveDisplayName(...) ?? Name;`) all coalesce to the member name. Consider returning non-nullable `string` (returning the member name internally if input was null/empty) and removing the `?? Name` from each call site, OR keep the contract symmetric ("returns null when the input was null and no override exists") and document it. The current state is ambiguous.

### 3.12 `ValidationOptions.AttributeFormatters` namespace inconsistency

`PublicAPI.Unshipped.txt` lists both `Microsoft.Extensions.Validation.ValidationAttributeFormatterRegistry` (namespace mismatch — it's actually under `Microsoft.Extensions.Validation.Localization`) and the correctly-namespaced `Microsoft.Extensions.Validation.Localization.ValidationAttributeFormatterRegistry`. The actual file declares `namespace Microsoft.Extensions.Validation.Localization`. The shipping file should be corrected to a single accurate entry. Same applies to `Microsoft.Extensions.Validation.IValidationAttributeFormatter` — file declares `namespace Microsoft.Extensions.Validation`, but the registry it's used by is in `.Localization`. Choose one namespace and align.

### 3.13 Source generator regression

Generator emits the legacy 4-arg `ValidatablePropertyInfo` constructor and never passes `displayResource`. Compiles only because `displayResource` has a default of `null`. That means generated metadata silently loses the `DisplayAttribute.ResourceType` information. See §3.4 — this should be tackled together with the `ResourceType` work.

---

## 4. Test coverage analysis

The test file (`LocalizationTests.cs`, ~464 lines, ~14 test methods) covers a useful slice but has structural gaps. Tests construct `ValidationLocalizer` directly via `CreateContext` — they bypass DI entirely. As a result:

- The DI registration bug in §3.1 is not caught by the test suite.
- `LocalizerProvider` behavior is a `// TODO` stub (line 268).
- `DisplayAttribute.ResourceType` behavior is a `// TODO` stub (line 71).
- Auto-detection / DI integration is a `// TODO` (line 19).

**What is covered well:**

- Display-name localization end-to-end via the in-memory `TestStringLocalizer`.
- Error-message localization via explicit `ErrorMessage` keys.
- Range/multi-arg formatting via the registered built-in formatters.
- `ErrorMessageKeyProvider` happy path and null-return path.
- Type-level `[ValidationAttribute]` localization.
- `ErrorMessageResourceType` short-circuit on the attribute side (i.e. the attribute returns its resource string and the localizer returns null because `"RequiredError"` is not in the dictionary). Note: this passes accidentally — see §3.3.
- `IValidatableObject` messages are not double-localized.
- `AddValidationAttributeFormatter` registration.

**What is missing:**

| Area | Existing | Needed |
|---|---|---|
| DI integration | Only `AddValidationAttributeFormatter` exercises DI. | (a) `AddValidation` then `GetRequiredService<ValidationLocalizer>()` succeeds with no `IStringLocalizerFactory` registered. (b) Same with `AddLocalization()` registered: returned localizer actually localizes. (c) End-to-end `ValidationEndpointFilterFactory`-style flow (build a service provider, run a fake endpoint, assert localized error appears). |
| `LocalizerProvider` | `// TODO` | (a) Custom provider returns shared localizer for all types — verify display name and error message both use it. (b) Provider receives the correct declaring type. (c) Provider returning null throws a clear error (or document as undefined). |
| `DisplayAttribute.ResourceType` | `// TODO` | After §3.4 is fixed: (a) Resource-backed display name appears in the error template. (b) The `IStringLocalizer` is **not** consulted for the display name when ResourceType is set. (c) Source-generator snapshot test for `ResourceType` propagation. |
| `ErrorMessageResourceType` + key provider | None — bug masked | After §3.3 is fixed: attribute with `ErrorMessageResourceType` set is **not** overridden when a global `ErrorMessageKeyProvider` is configured. |
| Parameter localization | None | (a) `ValidatableParameterInfo` end-to-end test verifying `ResolveErrorMessage` is invoked and produces a localized result. (b) Behavior with `declaringType: null` and shared `LocalizerProvider`. |
| `ValidationLocalizer` standalone (SSR scenario) | None | (a) Direct call: `localizer.ResolveErrorMessage(new RangeAttribute(1, 100) { ErrorMessage = "K" }, "Age", typeof(MyModel))` returns a localized formatted string without ever running validation. (b) `localizer.ResolveDisplayName(...)` likewise. |
| `CompareAttribute` formatter | None | After §3.9 is fixed: localized message should reference the **other** property's display name, not its raw property name. |
| Blazor field-level vs full validation | None in this file (existing Blazor tests probably don't exercise localization) | Test that field-level validation is consistent with full-form validation w.r.t. localized messages (or, if the inconsistency is intentional, document and pin it with a regression test). |
| Nullable / culture edge cases | None | (a) `displayName == null && declaringType == null && _localizerFactory == null` returns `null`/`Name` as expected. (b) `CurrentUICulture` switching between two cultures during the lifetime of a single `ValidationLocalizer` instance returns different translations (localizer cache is keyed by type, not culture — verify `IStringLocalizer` itself is culture-aware, which it is). |
| Trimming / AOT | None | After §3.4 is fixed with `Type` reflection: verify the trimming test asset includes a model using `[Display(ResourceType = ..., Name = ...)]`. |
| Public API baseline | None | `Microsoft.Extensions.Validation.PublicAPI.Unshipped.txt` should match the actually-emitted symbols (see §3.12). |

---

## 5. Recommended remediation plan

Suggested order (each step independently mergeable):

1. **Fix DI registration** (§3.1, §3.2, §3.6). Add a regression test that `AddValidation()` followed by `BuildServiceProvider()` followed by `GetRequiredService<ValidationLocalizer>()` succeeds, both with and without `AddLocalization()`. Add a sanity test that exercises `ValidationEndpointFilterFactory.Create` end-to-end with a dummy endpoint (or, if too heavy, at minimum a unit test that `WebApplication`-style host with `AddValidation` resolves all validation services).
2. **Restore `ErrorMessageResourceType` short-circuit** (§3.3). Add explicit test.
3. **Decide and implement `DisplayAttribute.ResourceType` strategy** (§3.4): re-introduce `Func<string>?` accessor (recommended — simpler, no AOT concern) **or** complete the reflection-based path with proper trim annotations. Update the source generator (`ValidatableProperty` model, parser, emitter) and snapshot tests. Update the runtime resolver and the Minimal-API parameter `GetDisplayName`.
4. **Fix `CompareAttributeFormatter`** (§3.9). Add a test using `[Compare(nameof(Password))]` and a `[Display(Name="Password")]` on the compared property.
5. **Address Blazor field-level localization gap** (§3.5). At minimum, add a Blazor E2E or component test that exercises both the field-changed and submit paths to capture current behavior; then either fix the inconsistency or document it.
6. **Fix parameter resource source default or document it** (§3.7). Add a parameter-level localization test that uses a shared-resource `LocalizerProvider`.
7. **Fill in the test TODOs** (`LocalizerProvider`, ResourceType, auto-detection) and add the `ValidationLocalizer`-standalone tests called out above.
8. **Polish** (§3.10 — defensive `LocalizerProvider` null-return; §3.11 — `ResolveDisplayName` return type contract; §3.12 — `PublicAPI.Unshipped.txt` namespace fixes).

---

## 6. What's good

- Splitting message resolution out of `ValidationOptions` into a constructible class is the right design for the SSR client-side rendering goal; the boundary (`ValidationAttribute`, display name, declaring type) is exactly the information available at HTML emission time.
- `ValidateContext` falling back to a no-op `ValidationLocalizer` when DI hasn't injected one keeps validation execution working in unit-test setups and other non-DI contexts.
- The `IValidationAttributeFormatter` registry with self-formatting attribute support is a clean replacement for MVC's adapter system.
- The built-in formatter coverage matches the standard multi-arg attributes well.
- `ErrorMessageKeyContext` is small and focused — no accidental coupling to validation-execution types.
