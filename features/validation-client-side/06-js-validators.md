# Client-Side Validation Attribute Support — Comparative Analysis

This document compares the validation logic across five layers of the ASP.NET validation stack for each supported validation attribute.

## Layers Compared

| # | Layer | Role |
|---|-------|------|
| 1 | **.NET `System.ComponentModel.DataAnnotations`** | Server-side validation (source of truth) |
| 2 | **MVC built-in adapters** (`Mvc.DataAnnotations`) | Maps .NET attributes → `data-val-*` HTML attributes |
| 3 | **jquery-validation-unobtrusive** | Bridge: reads `data-val-*` attrs, maps to jquery-validation rules |
| 4 | **jquery-validation** | Actual JS validation logic (used by MVC) |
| 5 | **Our JS prototype** (`BuiltInProviders.ts`) | Actual JS validation logic (our new library) |

---

## Per-Attribute Comparison

### 1. `RequiredAttribute`

| Layer | Implementation |
|-------|---------------|
| **.NET** | Returns invalid if value is `null`, empty string, or whitespace-only (when `AllowEmptyStrings = false`, the default). |
| **MVC adapter** | Emits `data-val-required="{message}"`. |
| **jquery-val-unobtrusive** | Maps to jquery-validation `required: true`. Special case: checkbox inputs are excluded (checkbox unchecked submits `"false"` via hidden input). |
| **jquery-validation** | `required` method: for select, checks `val().length > 0`; for checkable, checks `getLength() > 0`; otherwise `value.length > 0`. |
| **Our prototype** | Checkbox: checks `element.checked`. Radio: checks if any same-name radio is checked in the form. Otherwise: `value.trim().length > 0`. |

**Differences:** jquery-validation does NOT trim whitespace (relies on separate `normalizer` option). Our prototype trims, matching .NET's default behavior (`AllowEmptyStrings = false`). Our prototype handles radio buttons explicitly; jquery-validation uses `getLength` which counts checked elements for checkable types.

---

### 2. `StringLengthAttribute`

| Layer | Implementation |
|-------|---------------|
| **.NET** | Validates `MinimumLength <= value.Length <= MaximumLength`. Null is valid. |
| **MVC adapter** | Emits `data-val-length="{message}"`, `data-val-length-max="{max}"`, `data-val-length-min="{min}"` (min only if > 0). |
| **jquery-val-unobtrusive** | Maps to `rangelength: [min, max]` (if both), `minlength: min` (if min only), `maxlength: max` (if max only). |
| **jquery-validation** | `rangelength`: `length >= param[0] && length <= param[1]`. `minlength`/`maxlength`: straightforward length comparison. |
| **Our prototype** | `length` provider: parses `min`/`max` params, compares `value.length`. Empty → valid. |

**Differences:** Essentially equivalent. jquery-validation splits into three possible rules; our prototype handles both in a single `length` provider. We also register separate `minlength` and `maxlength` providers for `MinLengthAttribute` and `MaxLengthAttribute`.

---

### 3. `MinLengthAttribute`

| Layer | Implementation |
|-------|---------------|
| **.NET** | Validates `value.Length >= Length`. Also supports arrays. Null is valid. |
| **MVC adapter** | Emits `data-val-minlength="{message}"`, `data-val-minlength-min="{length}"`. |
| **jquery-val-unobtrusive** | Maps to `minlength: min`. |
| **jquery-validation** | `minlength`: `getLength(value, element) >= param`. |
| **Our prototype** | `minlength` provider: `value.length >= min`. Empty → valid. |

**Differences:** None significant for string inputs. jquery-validation uses `getLength` which can count checked checkboxes or selected options — our prototype only handles string length.

---

### 4. `MaxLengthAttribute`

| Layer | Implementation |
|-------|---------------|
| **.NET** | Validates `value.Length <= Length`. Also supports arrays. Null is valid. |
| **MVC adapter** | Emits `data-val-maxlength="{message}"`, `data-val-maxlength-max="{length}"`. |
| **jquery-val-unobtrusive** | Maps to `maxlength: max`. |
| **jquery-validation** | `maxlength`: `getLength(value, element) <= param`. |
| **Our prototype** | `maxlength` provider: `value.length <= max`. Empty → valid. |

**Differences:** Same as MinLength — equivalent for string inputs.

---

### 5. `RangeAttribute`

| Layer | Implementation |
|-------|---------------|
| **.NET** | Validates `Minimum <= value <= Maximum`. Supports `int`, `double`, `string`, and (since .NET 8) any `IComparable`. Null is valid. Triggers conversion of string min/max values. |
| **MVC adapter** | Emits `data-val-range="{message}"`, `data-val-range-min="{min}"`, `data-val-range-max="{max}"`. Calls `attribute.IsValid(null)` first to trigger `Minimum`/`Maximum` conversion. |
| **jquery-val-unobtrusive** | Maps to `range: [min, max]` (if both), `min: min`, `max: max`. |
| **jquery-validation** | `range`: `value >= param[0] && value <= param[1]`. Uses numeric comparison. |
| **Our prototype** | `range` provider: `parseFloat(value)`, then compares against `min`/`max`. `NaN` → invalid. Empty → valid. |

**Differences:** jquery-validation coerces values via `>= <=` operators. Our prototype explicitly `parseFloat`s and rejects `NaN`. Both are correct for numeric ranges. .NET's `RangeAttribute` can also compare strings and dates — this is not supported client-side in any library.

---

### 6. `RegularExpressionAttribute`

| Layer | Implementation |
|-------|---------------|
| **.NET** | Creates `Regex` from pattern, calls `Regex.Match(value)`, validates the match starts at index 0 and spans the entire string length. Uses `MatchTimeout` (default 2s). |
| **MVC adapter** | Emits `data-val-regex="{message}"`, `data-val-regex-pattern="{pattern}"`. |
| **jquery-val-unobtrusive** | Maps to `regex: pattern`. Uses custom `regex` method (NOT jquery-validation's built-in — it adds its own via `addMethod`). |
| **jquery-validation** (custom method) | `var match = new RegExp(params).exec(value); return match && match.index === 0 && match[0].length === value.length`. This is a **full-string match** — equivalent to .NET's behavior. |
| **Our prototype** | Uses `exec()` and verifies `match.index === 0 && match[0].length === value.length`. **Full-string match** — matches .NET and jquery-validation-unobtrusive. |

**Differences:** All implementations now agree on full-string match semantics.

---

### 7. `CompareAttribute`

| Layer | Implementation |
|-------|---------------|
| **.NET** | Compares two property values for equality using `Object.Equals`. |
| **MVC adapter** | Emits `data-val-equalto="{message}"`, `data-val-equalto-other="*.{OtherProperty}"`. The `*.` prefix is an ASP.NET naming convention. |
| **jquery-val-unobtrusive** | Resolves the `*.Property` selector to find the other input by name, maps to `equalTo: element`. |
| **jquery-validation** | `equalTo`: `value === $(param).val()`. Simple string equality. |
| **Our prototype** | ✅ Resolves `*.PropertyName` convention using `form.elements.namedItem()`, compares `value === otherElement.value`. |

**Gap:** None — Compare validation is now implemented. The C# adapter (`CompareClientAdapter`) emits `data-val-equalto` and `data-val-equalto-other`, and the JS `equalto` provider consumes them.

---

### 8. `EmailAddressAttribute`

| Layer | Implementation |
|-------|---------------|
| **.NET** | Very simple check: ensures exactly one `@` character, not at start or end. No CR/LF allowed. Does NOT validate domain or use regex. |
| **MVC adapter** | Emits `data-val-email="{message}"` via `DataTypeAttributeAdapter("data-val-email")`. |
| **jquery-val-unobtrusive** | Maps to `email: true` via `addBool("email")`. |
| **jquery-validation** | RFC 5322-derived regex: `/^[a-zA-Z0-9.!#$%&'*+/=?^_`{\|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$/`. Far stricter than .NET. |
| **Our prototype** | Same RFC 5322 regex as jquery-validation. |

**Differences:** The .NET `EmailAddressAttribute` is intentionally lenient — it accepts `user@host` (no TLD), `a@b`, etc. Both jquery-validation and our prototype are stricter, requiring RFC-compliant format. This means some strings valid server-side will be rejected client-side. This is the established MVC behavior and is acceptable: client-side is stricter, server-side is the final arbiter.

---

### 9. `UrlAttribute`

| Layer | Implementation |
|-------|---------------|
| **.NET** | Checks that the string starts with `http://`, `https://`, or `ftp://` (case-insensitive). Extremely lenient — `"http://"` alone is valid. |
| **MVC adapter** | Emits `data-val-url="{message}"` via `DataTypeAttributeAdapter("data-val-url")`. |
| **jquery-val-unobtrusive** | Maps to `url: true` via `addBool("url")`. |
| **jquery-validation** | Diego Perini's comprehensive URL regex. Validates scheme, optional auth, hostname (excluding private IP ranges), optional port, optional path/query. Far stricter than .NET. |
| **aspnet-client-validation** | `value.toLowerCase().indexOf('http://') > -1 \|\| ...` — even matches if scheme appears anywhere in string (substring match). |
| **Our prototype** | `value.toLowerCase().startsWith('http://') \|\| startsWith('https://') \|\| startsWith('ftp://')`. Matches .NET `UrlAttribute.IsValid()` exactly. |

**Differences:** jquery-validation is much stricter than .NET. aspnet-client-validation is too lenient (substring match). Our prototype matches the .NET source of truth — correct behavior for server/client parity.

---

### 10. `PhoneAttribute`

| Layer | Implementation |
|-------|---------------|
| **.NET** | Strips `+`, trims trailing whitespace, removes extension suffixes (`ext.`, `ext`, `x` followed by digits). Validates remaining chars are digits, whitespace, or `-.()`. Must contain at least one digit. |
| **MVC adapter** | Emits `data-val-phone="{message}"` via `DataTypeAttributeAdapter("data-val-phone")`. |
| **jquery-val-unobtrusive** | ❌ **No phone adapter registered!** `addBool` chain includes `creditcard`, `date`, `digits`, `email`, `number`, `url` but NOT `phone`. |
| **jquery-validation** | ❌ **No built-in `phone` method.** Only `phoneUS` in `additional-methods.js` (US-only: `^\+?1?\d{10}$`). |
| **aspnet-client-validation** | Simple regex: rejects consecutive separators, then `^\+?[0-9\-\s]+$`. Too restrictive — rejects parentheses and dots. |
| **Our prototype** | Port of .NET `PhoneAttribute.IsValid()`: strips `+`, trims end, removes extensions, validates chars `[\d\s\-\.()]`, requires at least one digit. |

**Notable finding:** MVC's `jquery-validation-unobtrusive` **does NOT register a phone adapter**, even though the MVC adapter provider emits `data-val-phone`. This means phone validation is silently ignored client-side in MVC applications. Our prototype fills this gap with a faithful .NET port.

---

### 11. `CreditCardAttribute`

| Layer | Implementation |
|-------|---------------|
| **.NET** | Iterates chars in reverse. Skips `-` and ` `. Rejects non-digit, non-separator chars. Applies Luhn algorithm. |
| **MVC adapter** | Emits `data-val-creditcard="{message}"` via `DataTypeAttributeAdapter("data-val-creditcard")`. |
| **jquery-val-unobtrusive** | Maps to `creditcard: true` via `addBool("creditcard")`. |
| **jquery-validation** (additional method) | Rejects non-digit/space/dash chars. Strips non-digits. Validates length 13–19. Luhn algorithm. |
| **aspnet-client-validation** | Same as jquery-validation: strips non-digits, validates length 13–19, Luhn. |
| **Our prototype** | ✅ Port of .NET `CreditCardAttribute.IsValid()`: Luhn algorithm, iterates in reverse, skips `-` and ` `, rejects other non-digit chars. |

**Differences:** jquery-validation adds length validation (13–19 digits) that .NET does not. .NET's implementation processes chars in a single pass (skipping separators), while jquery-validation strips all non-digits first, then validates. Both use Luhn. Our prototype matches .NET exactly (no length check).

---

### 12. `FileExtensionsAttribute`

| Layer | Implementation |
|-------|---------------|
| **.NET** | Validates that the file extension is in the allowed list (default: `png,jpg,jpeg,gif`). |
| **MVC adapter** | `FileExtensionsAttributeAdapter`: Emits `data-val-fileextensions="{message}"`, `data-val-fileextensions-extensions="{csv-extensions}"`. |
| **jquery-val-unobtrusive** | Maps to `extension: extensions` rule. |
| **jquery-validation** (additional method) | Checks file extension against comma-separated list. |
| **aspnet-client-validation** | ❌ Not supported. |
| **Our prototype** | ✅ Port of .NET `FileExtensionsAttribute.IsValid()`: extracts extension via `lastIndexOf('.')`, checks against comma-separated allowed list (default: `png,jpg,jpeg,gif`). Normalizes by stripping spaces/dots and lowering case. |

**Notes:** File extension validation is niche (file upload forms). C# adapter (`FileExtensionsClientAdapter`) emits `data-val-fileextensions` and `data-val-fileextensions-extensions`.

---

### 13. `RemoteAttribute` (MVC-only)

| Layer | Implementation |
|-------|---------------|
| **.NET** | MVC-specific attribute — makes AJAX call to server endpoint for validation. |
| **MVC adapter** | Not a standard DataAnnotations attribute. Registers via `IClientModelValidator` directly. Emits `data-val-remote`, `data-val-remote-url`, `data-val-remote-type`, `data-val-remote-additionalfields`. |
| **jquery-val-unobtrusive** | Sends GET/POST to server URL with field values. |
| **jquery-validation** | `remote` method: AJAX call, caches results. |
| **aspnet-client-validation** | Full implementation with XMLHttpRequest. |
| **Our prototype** | ❌ Not supported (by design — no async validation in prototype). |

**Notes:** Not a DataAnnotation — MVC-specific. Out of scope for Blazor.

---

## Summary Matrix

| Attribute | .NET | MVC Adapter | jquery-val-unobtrusive | jquery-validation | aspnet-client-validation | Our Prototype |
|-----------|------|-------------|----------------------|-------------------|--------------------------|---------------|
| `Required` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| `StringLength` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| `MinLength` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| `MaxLength` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| `Range` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| `RegularExpression` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ (full-string match) |
| `Compare` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| `EmailAddress` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| `Url` | ✅ | ✅ | ✅ | ✅ (stricter) | ✅ (looser) | ✅ (matches .NET) |
| `Phone` | ✅ | ✅ | ❌ (not wired!) | ❌ | ✅ (simpler) | ✅ (matches .NET) |
| `CreditCard` | ✅ | ✅ | ✅ | ✅ (additional) | ✅ | ✅ (matches .NET) |
| `FileExtensions` | ✅ | ✅ | ✅ | ✅ (additional) | ❌ | ✅ (matches .NET) |
| `Remote` | ✅ (MVC) | ✅ | ✅ | ✅ | ✅ | ❌ (by design) |

### Key Findings

1. **Phone validation is broken in MVC's client-side stack.** The MVC adapter emits `data-val-phone`, but `jquery-validation-unobtrusive` never registers a `phone` adapter. jquery-validation has no built-in `phone` method. The attribute is silently ignored client-side. Our prototype is the first implementation to correctly port the .NET `PhoneAttribute` logic.

2. **Regex matching semantics now aligned.** The .NET `RegularExpressionAttribute` and jquery-validation-unobtrusive both enforce **full-string matching**. Our prototype now uses `exec()` with index/length verification — matching this behavior exactly.

3. **URL validation strictness varies widely.** jquery-validation uses a comprehensive regex (too strict vs .NET). aspnet-client-validation uses substring search (too loose). Our prototype matches the .NET `UrlAttribute` exactly — `startsWith` check.

4. **Email validation is stricter client-side than server-side** across all libraries. The .NET `EmailAddressAttribute` only checks for exactly one `@` not at boundaries. All JS implementations use RFC 5322 regex. This is the established MVC behavior.

5. **All standard validation attributes are now covered in our prototype.** The only omission is `Remote` (MVC-only, out of scope for Blazor by design).

### Recommended Actions

| Priority | Action | Rationale |
|----------|--------|-----------|
| **Done** | ~~Fix regex provider to use full-string matching~~ | Fixed — now uses `exec()` with index/length check |
| **Done** | ~~Add `equalto` (Compare) provider~~ | Implemented with `*.PropertyName` resolution |
| **Done** | ~~Add `creditcard` provider~~ | Implemented — faithful Luhn port from .NET |
| **Done** | ~~Add `fileextensions` provider~~ | Implemented with normalized extension matching |
| **N/A** | Remote validation | Out of scope by design (no async in prototype, MVC-only attribute) |
