# Custom Validation Attributes — Scenarios and Migration Analysis

This document analyzes what it takes to create a custom validation attribute with both server-side and client-side validation, comparing the old MVC workflow with our new library. It evaluates whether the new implementation has everything needed and identifies the migration path.

---

## 1. Scenario: "No Profanity" Validator

To ground the analysis, we use a concrete example: a `[NoProfanity]` attribute that rejects values containing words from a blocklist. This is representative of custom validators because:
- It has a server-side component (the actual word-checking logic)
- It needs a client-side component (JavaScript regex/match check)
- It has configurable parameters (the word list or a flag)
- It needs an error message

---

## 2. Old MVC Workflow — Three Layers

In MVC, creating a custom client-validated attribute requires changes in **three separate systems**:

### Step 1: The Validation Attribute (C#, server-side)

```csharp
public class NoProfanityAttribute : ValidationAttribute
{
    public string BlockedWords { get; set; } = "badword1,badword2,badword3";

    protected override ValidationResult? IsValid(object? value, ValidationContext context)
    {
        if (value is string text)
        {
            var words = BlockedWords.Split(',');
            foreach (var word in words)
            {
                if (text.Contains(word, StringComparison.OrdinalIgnoreCase))
                {
                    return new ValidationResult(FormatErrorMessage(context.DisplayName));
                }
            }
        }
        return ValidationResult.Success;
    }
}
```

### Step 2: The MVC Adapter (C#, HTML attribute emission)

Two options in MVC:

**Option A — Implement `IClientModelValidator` on the attribute itself:**

```csharp
public class NoProfanityAttribute : ValidationAttribute, IClientModelValidator
{
    public string BlockedWords { get; set; } = "badword1,badword2,badword3";

    // Server-side validation (same as above)
    protected override ValidationResult? IsValid(object? value, ValidationContext context) { ... }

    // Client-side attribute emission
    public void AddValidation(ClientModelValidationContext context)
    {
        MergeAttribute(context.Attributes, "data-val", "true");
        MergeAttribute(context.Attributes, "data-val-noprofanity",
            FormatErrorMessage(context.ModelMetadata.GetDisplayName()));
        MergeAttribute(context.Attributes, "data-val-noprofanity-words", BlockedWords);
    }

    private static void MergeAttribute(IDictionary<string, string> attrs, string key, string value)
    {
        if (!attrs.ContainsKey(key)) attrs.Add(key, value);
    }
}
```

**Option B — Create a separate adapter class + register via `IValidationAttributeAdapterProvider`:**

```csharp
// Adapter
public class NoProfanityAttributeAdapter : AttributeAdapterBase<NoProfanityAttribute>
{
    public NoProfanityAttributeAdapter(NoProfanityAttribute attribute, IStringLocalizer? localizer)
        : base(attribute, localizer) { }

    public override void AddValidation(ClientModelValidationContext context)
    {
        MergeAttribute(context.Attributes, "data-val", "true");
        MergeAttribute(context.Attributes, "data-val-noprofanity", GetErrorMessage(context));
        MergeAttribute(context.Attributes, "data-val-noprofanity-words", Attribute.BlockedWords);
    }

    public override string GetErrorMessage(ModelValidationContextBase context)
        => GetErrorMessage(context.ModelMetadata, context.ModelMetadata.GetDisplayName());
}

// Provider (registered in DI)
public class CustomValidationAttributeAdapterProvider : IValidationAttributeAdapterProvider
{
    private readonly IValidationAttributeAdapterProvider _fallback = new ValidationAttributeAdapterProvider();

    public IAttributeAdapter? GetAttributeAdapter(ValidationAttribute attribute, IStringLocalizer? localizer)
    {
        if (attribute is NoProfanityAttribute npa)
            return new NoProfanityAttributeAdapter(npa, localizer);

        return _fallback.GetAttributeAdapter(attribute, localizer);
    }
}

// In Startup.cs/Program.cs:
services.AddSingleton<IValidationAttributeAdapterProvider, CustomValidationAttributeAdapterProvider>();
```

### Step 3: The JavaScript Validator (two registrations!)

In the old stack, the developer must register BOTH a jquery-validation-unobtrusive adapter AND a jquery.validate method:

```javascript
// 1. Register the unobtrusive adapter — maps data-val-* HTML attributes to jquery-validate rules
$.validator.unobtrusive.adapters.add('noprofanity', ['words'], function (options) {
    options.rules['noprofanity'] = { words: options.params.words };
    options.messages['noprofanity'] = options.message;
});

// 2. Register the jquery.validate method — the actual validation logic
$.validator.addMethod('noprofanity', function (value, element, params) {
    if (this.optional(element)) return true;
    var words = params.words.split(',');
    for (var i = 0; i < words.length; i++) {
        if (value.toLowerCase().indexOf(words[i].toLowerCase()) >= 0) {
            return false;
        }
    }
    return true;
}, 'Default error message');
```

### Summary of Old Workflow

| Layer | What to Create | Boilerplate |
|-------|---------------|-------------|
| 1. Server-side | `ValidationAttribute` subclass | Low (standard .NET pattern) |
| 2. HTML emission | `IClientModelValidator` or adapter + provider | High (adapter class + DI registration) |
| 3. Client-side JS | Unobtrusive adapter **AND** jquery.validate method | Medium (two separate registrations) |

**Total: 3 C# classes + 2 JavaScript registrations for Option B, or 1 C# class (with IClientModelValidator) + 2 JS registrations for Option A.**

---

## 3. New Library Workflow — Blazor

### Step 1: The Validation Attribute (C#, server-side) — unchanged

```csharp
public class NoProfanityAttribute : ValidationAttribute
{
    public string BlockedWords { get; set; } = "badword1,badword2,badword3";

    protected override ValidationResult? IsValid(object? value, ValidationContext context)
    {
        // Same as MVC — identical logic
    }
}
```

### Step 2: The Blazor Adapter (C#, HTML attribute emission)

**Option A — Self-implementing adapter:**

The attribute itself implements `IClientValidationAdapter`:

```csharp
public class NoProfanityAttribute : ValidationAttribute, IClientValidationAdapter
{
    public string BlockedWords { get; set; } = "badword1,badword2,badword3";

    protected override ValidationResult? IsValid(object? value, ValidationContext context) { ... }

    public void AddClientValidation(in ClientValidationContext context, string errorMessage)
    {
        context.MergeAttribute("data-val", "true");
        context.MergeAttribute("data-val-noprofanity", errorMessage);
        context.MergeAttribute("data-val-noprofanity-words", BlockedWords);
    }
}
```

This works because `ClientValidationAdapterRegistry.GetAdapter()` first checks if the attribute itself implements `IClientValidationAdapter` before looking up registered factories.

**Option B — Separate adapter + DI registration:**

```csharp
// Adapter class
public class NoProfanityClientAdapter(NoProfanityAttribute attribute) : IClientValidationAdapter
{
    public void AddClientValidation(in ClientValidationContext context, string errorMessage)
    {
        context.MergeAttribute("data-val", "true");
        context.MergeAttribute("data-val-noprofanity", errorMessage);
        context.MergeAttribute("data-val-noprofanity-words", attribute.BlockedWords);
    }
}

// In Program.cs — single line:
builder.Services.AddClientValidationAdapter<NoProfanityAttribute>(a => new NoProfanityClientAdapter(a));
```

No need for a provider class. The generic `AddClientValidationAdapter<T>` extension method handles it.

### Step 3: The JavaScript Validator (single registration)

```javascript
// One registration — both adapter and validator logic in one
window.__aspnetValidation.addProvider('noprofanity', (value, element, params) => {
    if (!value) return true;
    const words = (params.words || '').split(',');
    for (const word of words) {
        if (value.toLowerCase().includes(word.toLowerCase())) {
            return false; // Use the default error message from data-val-noprofanity
        }
    }
    return true;
});
```

### Summary of New Workflow

| Layer | What to Create | Boilerplate |
|-------|---------------|-------------|
| 1. Server-side | `ValidationAttribute` subclass | Low (same as MVC) |
| 2. HTML emission | `IClientValidationAdapter` + 1-line DI registration | Low |
| 3. Client-side JS | Single `addProvider` call | Low |

**Total: 2 C# classes + 1 DI line + 1 JavaScript registration (Option B), or 1 C# class + 1 JS registration (Option A).**

---

## 4. New Library Workflow — MVC

For MVC, the developer already has `IClientModelValidator` (or an adapter via `IValidationAttributeAdapterProvider`). The HTML attributes are identical — they produce the same `data-val-*` output. The only difference is the JavaScript side.

### JavaScript Side — Migration from Old to New

```javascript
// OLD (two registrations):
$.validator.unobtrusive.adapters.add('noprofanity', ['words'], function (options) { ... });
$.validator.addMethod('noprofanity', function (value, element, params) { ... });

// NEW (one registration):
window.__aspnetValidation.addProvider('noprofanity', (value, element, params) => {
    if (!value) return true;
    const words = (params.words || '').split(',');
    return !words.some(w => value.toLowerCase().includes(w.toLowerCase()));
});
```

Key differences:
- `params` is a `Record<string, string>` (already parsed from `data-val-noprofanity-*` attributes)
- No `this.optional(element)` — the convention is: return `true` if value is empty
- Return `true` (valid), `false` (use default message), or `string` (custom error)
- No separate adapter registration — the provider IS the adapter and validator combined

### C# Side — No Migration Needed

The MVC `IClientModelValidator` or `IValidationAttributeAdapterProvider` workflow is unchanged. It produces the same `data-val-*` HTML attributes that our new JS library reads.

---

## 5. Does the New Implementation Support Custom Attributes?

### Checklist

| Capability | Supported? | Notes |
|-----------|-----------|-------|
| **Custom attribute with server validation** | ✅ | Standard `ValidationAttribute` — no change |
| **Self-implementing adapter (attribute implements interface)** | ✅ | `IClientValidationAdapter` on the attribute, auto-detected by registry |
| **Separate adapter class** | ✅ | Implement `IClientValidationAdapter`, register via `AddClientValidationAdapter<T>()` |
| **Custom parameters** | ✅ | Emitted as `data-val-{rule}-{param}`, parsed into `params` by `DirectiveParser` |
| **Custom error messages** | ✅ | `FormatErrorMessage` resolved before adapter call, emitted as `data-val-{rule}` |
| **Display name in error messages** | ✅ | `DefaultClientValidationService` resolves `[Display]`/`[DisplayName]` |
| **Custom JS validator** | ✅ | `window.__aspnetValidation.addProvider(name, fn)` |
| **Async custom validator** | ✅ | Provider can return `Promise<boolean \| string>` |
| **Override built-in validator** | ✅ | `setProvider` on engine (not directly exposed — need to add to API) |
| **Multiple validators on same field** | ✅ | Multiple `data-val-{rule}` attributes, each gets its own provider call |
| **Validator with no parameters** | ✅ | `params` is an empty `Record<string, string>` |
| **MVC compatibility** | ✅ | Same `data-val-*` protocol, `parse()` API for dynamic content |

### Gap Identified: Override Built-in Providers

The public API exposes `addProvider()` which **won't override** an existing provider (it checks `has(name)` first). To override a built-in, the developer would need `setProvider()`, which is not currently exposed on `window.__aspnetValidation`.

**Fix:** Expose `setProvider` on the public API (both Blazor and MVC), or rename to `overrideProvider` for clarity.

---

## 6. Migration Path — MVC Users

For teams migrating from jquery-validation + jquery-validation-unobtrusive to our library:

### Step 1: Replace Script References

```html
<!-- OLD -->
<script src="~/lib/jquery/dist/jquery.min.js"></script>
<script src="~/lib/jquery-validation/dist/jquery.validate.min.js"></script>
<script src="~/lib/jquery-validation-unobtrusive/dist/jquery.validate.unobtrusive.min.js"></script>

<!-- NEW -->
<script src="~/lib/aspnet-core-validation/aspnet-core-validation.min.js"></script>
```

### Step 2: No C# Changes Needed

MVC's `IClientModelValidator`, `IValidationAttributeAdapterProvider`, and tag helpers are unchanged. They produce the same HTML attributes. The new JS library reads them identically.

### Step 3: Migrate Custom JavaScript Validators

For each custom validator, convert from two registrations to one:

```javascript
// OLD
$.validator.unobtrusive.adapters.add('customrule', ['param1', 'param2'], function (options) {
    options.rules['customrule'] = { param1: options.params.param1, param2: options.params.param2 };
    options.messages['customrule'] = options.message;
});
$.validator.addMethod('customrule', function (value, element, params) {
    return this.optional(element) || /* validation logic using params.param1, params.param2 */;
});

// NEW
window.__aspnetValidation.addProvider('customrule', (value, element, params) => {
    if (!value) return true;
    return /* validation logic using params.param1, params.param2 */;
});
```

**Migration rules:**
- `this.optional(element)` → `if (!value) return true;`
- `params.xxx` from the adapter → `params.xxx` (already parsed from `data-val-customrule-xxx`)
- Return `true`/`false` → same
- Return custom error string → supported (return a string instead of `false`)
- `$.validator.addMethod` message → not needed (message comes from `data-val-customrule` attribute)

### Step 4: Migrate `parse()` Calls

```javascript
// OLD
$.validator.unobtrusive.parse('#dynamicContent');

// NEW
window.__aspnetValidation.parse('#dynamicContent');
```

### Step 5: Remove jQuery (if no other usage)

If jQuery is only used for validation, it can be removed entirely. The new library has zero dependencies.

---

## 7. Migration Path — Building for Both MVC and Blazor

For teams building a shared library of custom validators that works in both MVC and Blazor:

### Shared: The Validation Attribute

```csharp
// Works in both MVC and Blazor — no framework dependency
public class NoProfanityAttribute : ValidationAttribute
{
    public string BlockedWords { get; set; } = "badword1,badword2,badword3";

    protected override ValidationResult? IsValid(object? value, ValidationContext context) { ... }
}
```

### MVC: Use `IClientModelValidator` (existing pattern)

```csharp
// MVC adapter — uses MVC-specific context
public class NoProfanityAttribute : ValidationAttribute, IClientModelValidator
{
    public void AddValidation(ClientModelValidationContext context) { ... }
}
```

### Blazor: Use `IClientValidationAdapter` (new pattern)

```csharp
// Blazor adapter — register via DI
services.AddClientValidationAdapter<NoProfanityAttribute>(a => new NoProfanityClientAdapter(a));
```

### Dual Support: Self-Implementing Both Interfaces

An attribute can implement both `IClientModelValidator` (for MVC) and `IClientValidationAdapter` (for Blazor):

```csharp
public class NoProfanityAttribute : ValidationAttribute, IClientModelValidator, IClientValidationAdapter
{
    public string BlockedWords { get; set; } = "badword1,badword2,badword3";

    protected override ValidationResult? IsValid(object? value, ValidationContext context) { ... }

    // MVC path
    public void AddValidation(ClientModelValidationContext context)
    {
        MergeAttribute(context.Attributes, "data-val", "true");
        MergeAttribute(context.Attributes, "data-val-noprofanity",
            FormatErrorMessage(context.ModelMetadata.GetDisplayName()));
        MergeAttribute(context.Attributes, "data-val-noprofanity-words", BlockedWords);
    }

    // Blazor path
    public void AddClientValidation(in ClientValidationContext context, string errorMessage)
    {
        context.MergeAttribute("data-val", "true");
        context.MergeAttribute("data-val-noprofanity", errorMessage);
        context.MergeAttribute("data-val-noprofanity-words", BlockedWords);
    }

    private static void MergeAttribute(IDictionary<string, string> attrs, string key, string value)
    {
        if (!attrs.ContainsKey(key)) attrs.Add(key, value);
    }
}
```

Both interfaces emit the **same HTML attributes**, so the same JavaScript provider works for both:

```javascript
window.__aspnetValidation.addProvider('noprofanity', (value, element, params) => {
    if (!value) return true;
    const words = (params.words || '').split(',');
    return !words.some(w => value.toLowerCase().includes(w.toLowerCase()));
});
```

### Note on Interface Overlap

The two C# interfaces are nearly identical in purpose but differ in context:

| | `IClientModelValidator` (MVC) | `IClientValidationAdapter` (Blazor) |
|---|---|---|
| Method | `AddValidation(ClientModelValidationContext)` | `AddClientValidation(in ClientValidationContext, string)` |
| Context type | Class with `ActionContext`, `ModelMetadata` | Readonly struct with just `MergeAttribute()` |
| Error message | Must call `FormatErrorMessage()` yourself | Pre-resolved, passed as parameter |
| Merge helper | Static method, must define yourself | `context.MergeAttribute()` instance method |

Future improvement: consider a shared base interface or extension method to reduce duplication when supporting both.

---

## 8. Comparison Summary

| Aspect | Old MVC | New (Blazor) | New (MVC) |
|--------|---------|-------------|-----------|
| **C# attribute** | `ValidationAttribute` | Same | Same |
| **C# adapter** | `IClientModelValidator` or `AttributeAdapterBase<T>` + provider | `IClientValidationAdapter` + `AddClientValidationAdapter<T>()` | MVC's existing system (unchanged) |
| **JS registration** | 2 calls: `adapters.add()` + `$.validator.addMethod()` | 1 call: `addProvider()` | 1 call: `addProvider()` |
| **JS dependencies** | jQuery + jquery-validation + jquery-validation-unobtrusive | None | None |
| **Async support** | Limited (jQuery Deferred) | Native (`Promise`) | Native (`Promise`) |
| **Parameters** | Manual extraction in adapter function | Auto-parsed into `params` object | Auto-parsed into `params` object |
| **Error message** | Passed via `options.message` | Passed via `data-val-{rule}` attribute | Passed via `data-val-{rule}` attribute |
| **Override built-in** | `$.validator.addMethod()` overwrites | `setProvider()` (not yet exposed publicly) | `setProvider()` (not yet exposed publicly) |
| **Total boilerplate** | High (3 layers, 2 JS registrations) | Low (2 layers, 1 JS call) | Low (unchanged C#, 1 JS call) |

---

## 9. Recommendations

### 9a. Expose `setProvider` on Public API

Currently, `addProvider` won't override existing providers. For users who need to customize built-in validation behavior (e.g., stricter email regex), expose `setProvider` or add an `overrideProvider` method:

```typescript
// In BlazorWiring.ts and MvcWiring.ts
const api = {
  addProvider: (name, provider) => engine.addProvider(name, provider),
  setProvider: (name, provider) => engine.setProvider(name, provider),  // NEW
  // ...
};
```

### 9b. Consider Unifying the C# Interfaces

For library authors supporting both MVC and Blazor, implementing both `IClientModelValidator` and `IClientValidationAdapter` is tedious. A future improvement could provide a shared abstraction or source generator that auto-implements one from the other, since both emit the same `data-val-*` attributes.

### 9c. Document the `data-val-*` Protocol

The attribute naming convention (`data-val-{rule}` for message, `data-val-{rule}-{param}` for parameters) is an implicit protocol. It should be explicitly documented so that:
- Library authors know the contract
- Custom validator developers understand the naming rules
- The JS `DirectiveParser` behavior is clear (hyphens in param names are fine, but the rule name itself cannot contain hyphens)

### 9d. Rule Name Constraint

**Important:** Rule names cannot contain hyphens. The `DirectiveParser` uses the first hyphen after `data-val-` to separate rule name from parameter name. A rule named `my-rule` would be parsed as rule `my` with parameter `rule`. This should be documented and optionally validated at registration time.
