# Async Server-Side Validation — Compatibility with Client-Side Validation

This document analyzes how the upcoming server-side async validation support for Blazor (issue [#7680](https://github.com/dotnet/aspnetcore/issues/7680)) and the proposed `AsyncValidationAttribute` ([dotnet/designs#363](https://github.com/dotnet/designs/pull/363)) intersect with the client-side validation feature designed in this prototype.

---

## 1. Background: Two Async Validation Efforts

### Server-Side: `AsyncValidationAttribute` (dotnet/designs#363)

The proposal adds async validation to `System.ComponentModel.DataAnnotations`:

- New `AsyncValidationAttribute` class derives from `ValidationAttribute`
- Overrides sync `IsValid()` to throw `NotSupportedException`
- Adds `protected abstract ValueTask<ValidationResult?> IsValidAsync(...)`
- New `Validator.TryValidateObjectAsync()` and friends
- New `IAsyncValidatableObject` interface with `IAsyncEnumerable<ValidationResult>`

**Key design decision:** `AsyncValidationAttribute` inherits from `ValidationAttribute`. This means reflection-based attribute discovery (like our `DefaultClientValidationService.ComputeAttributes()`) will find it via `GetCustomAttributes<ValidationAttribute>()`.

### Server-Side: Blazor Async Form Validation (#7680)

The Blazor issue proposes:

- `EditContext.ValidateAsync()` — async counterpart to `Validate()`
- `EditForm.HandleSubmitAsync()` — changes sync `_editContext.Validate()` to `await _editContext.ValidateAsync()`
- Async `OnValidationRequested` event handler support
- Per-field pending validation tracking via `HasPendingValidationTasks()`

**Current state:** Blazor's `EditForm` already has a comment: `// This will likely become ValidateAsync later`. The infrastructure is partially prepared.

---

## 2. Impact on Client-Side Validation

### 2a. Adapter Discovery — `AsyncValidationAttribute` Will Be Found

Our `DefaultClientValidationService.ComputeAttributes()` iterates `ValidationAttribute` instances via reflection:

```csharp
var validationAttributes = property.GetCustomAttributes<ValidationAttribute>(inherit: true);
foreach (var validationAttribute in validationAttributes)
{
    var adapter = _adapterRegistry.GetAdapter(validationAttribute);
    // ...
}
```

Since `AsyncValidationAttribute` inherits from `ValidationAttribute`, it **will** be discovered. If no adapter is registered for it, it's silently skipped (returns `null`). This is **correct behavior** — most async server-side validations (e.g., database uniqueness checks) have no meaningful client-side equivalent.

**No changes needed.** The current design handles this correctly.

### 2b. The RemoteAttribute Guard — Needs Updating

Our `ThrowIfRemoteAttribute()` check in `DefaultClientValidationService` throws `NotSupportedException` for `RemoteAttributeBase`. With the introduction of `AsyncValidationAttribute`, we should reconsider this pattern:

- An `AsyncValidationAttribute` that checks database uniqueness is similar in spirit to `RemoteAttribute` — both do server-side async work
- Throwing for every async attribute would be too aggressive — most have no client-side aspect
- The current RemoteAttribute guard remains correct — it's specifically about MVC's remote validation pattern, not about async in general

**No changes needed.** The RemoteAttribute guard is specific and appropriate.

### 2c. Could an Async Attribute Also Have a Client-Side Adapter?

Yes. Consider a `UniqueEmailAttribute`:

```csharp
public class UniqueEmailAttribute : AsyncValidationAttribute, IClientValidationAdapter
{
    protected override async ValueTask<ValidationResult?> IsValidAsync(
        object? value, ValidationContext ctx, CancellationToken ct)
    {
        // Server-side: check database
        var exists = await db.Users.AnyAsync(u => u.Email == (string)value, ct);
        return exists ? new ValidationResult("Email already taken.") : null;
    }

    // Client-side: emit data-val-remote-* for JavaScript remote validation
    public void AddClientValidation(in ClientValidationContext context, string errorMessage)
    {
        context.MergeAttribute("data-val", "true");
        context.MergeAttribute("data-val-remote", errorMessage);
        context.MergeAttribute("data-val-remote-url", "/api/validate-email");
    }
}
```

This is the bridge between server-side async validation and client-side remote validation. The attribute:
1. Validates asynchronously on the server (via `IsValidAsync`)
2. Emits `data-val-remote-*` attributes for client-side remote validation (via `IClientValidationAdapter`)

**Our architecture supports this today.** The `IClientValidationAdapter` interface is independent of whether the attribute is sync or async on the server side. The adapter only emits HTML attributes.

However, this pattern is currently **blocked for Blazor** because:
- We throw for `RemoteAttributeBase` (but `UniqueEmailAttribute` wouldn't inherit from it)
- Our JS library only supports sync providers in Blazor mode
- The `data-val-remote` provider isn't registered in Blazor mode

**Future opportunity:** When `Blazor.submitForm()` API exists (see `10-blazor-async-compatibility.md`), we could enable async JS providers in Blazor mode, allowing this pattern to work end-to-end.

### 2d. Error Message Format — No Impact

`AsyncValidationAttribute` uses `FormatErrorMessage(displayName)` the same way `ValidationAttribute` does (inherited). Our adapter's `errorMessage` parameter is pre-resolved by `DefaultClientValidationService`, so no change needed.

---

## 3. Impact on `EditForm` Submit Flow

### 3a. Current Flow (Sync)

```
User clicks Submit
  → JS validation (client-side, our library) — sync path
  → If JS valid: event passes through to Blazor EventDelegator
  → EventDelegator dispatches to .NET
  → EditContext.Validate() — sync, calls DataAnnotationsValidator
  → If .NET valid: OnValidSubmit fires
```

### 3b. Future Flow (Async)

```
User clicks Submit
  → JS validation (client-side, our library) — sync path
  → If JS valid: event passes through to Blazor EventDelegator
  → EventDelegator dispatches to .NET
  → EditContext.ValidateAsync() — async, calls DataAnnotationsValidator
  → DataAnnotationsValidator uses Validator.TryValidateObjectAsync()
  → Awaits any AsyncValidationAttribute.IsValidAsync() calls
  → If .NET valid: OnValidSubmit fires
```

**Key insight:** Our client-side validation runs BEFORE the server-side validation. The two are complementary, not conflicting. Client-side validation prevents the round-trip when sync validations fail; server-side async validation runs only when the form passes client-side checks.

**No changes needed** to our submit handler. The async server validation happens on the .NET side after our JS validation passes the event through.

### 3c. The `ValidateAsync` Transition

When `EditForm` changes from `_editContext.Validate()` to `await _editContext.ValidateAsync()`, our client-side validation is unaffected because:

1. Our JS handler runs in the capture phase (before EventDelegator)
2. If client-side validation fails, `preventDefault()` blocks the event — server never sees it
3. If client-side validation passes (sync path), the event reaches EventDelegator → .NET
4. The .NET side then does sync or async validation independently
5. The result determines whether `OnValidSubmit` or `OnInvalidSubmit` fires

---

## 4. UX Considerations: Double Validation

With both client-side and server-side validation, the user experience is:

1. **Client-side catches fast errors** — Required, Email format, Range, StringLength, Regex, etc.
2. **Server-side catches deep errors** — Database uniqueness, business rules, cross-field validation with DB lookups
3. **Both show errors in the same UI** — client-side uses `data-valmsg-for` spans; server-side uses `<ValidationMessage>` Blazor components

### Potential Issue: Duplicate Error Display

After client-side validation passes and the form submits, server-side validation might fail on the same field. The user would see:
- Client-side error span (empty, since client validation passed)
- Server-side `<ValidationMessage>` span (showing the async error)

This is **not a problem** — the two are separate DOM elements. Client-side errors clear on successful validation. Server-side errors appear after the round-trip. They won't conflict.

### Potential Issue: Client Clears Server Errors on Input

After server-side validation shows an error (e.g., "Email already taken"), the user starts typing. Our JS library's `change` handler re-validates the field client-side. If the email format is valid, the client-side error clears — but the server-side error (from `<ValidationMessage>`) remains until the next form submission.

This is **acceptable behavior** — it matches how MVC works. The server error persists until the next POST.

---

## 5. Future: `AsyncValidationAttribute` with Client-Side Remote Validation

The ideal end state for an attribute like `UniqueEmailAttribute`:

| Layer | What Happens |
|-------|-------------|
| **C# attribute** | `AsyncValidationAttribute` with `IsValidAsync` (DB check) |
| **C# adapter** | `IClientValidationAdapter` emitting `data-val-remote-*` |
| **JS provider** | `remote` provider making `fetch()` to validation endpoint |
| **On submit** | JS remote check → if passes → form submits → server async check (redundant but safe) |

For this to work in Blazor, we need:
1. ✅ `IClientValidationAdapter` support (done)
2. ✅ `remote` JS provider (done, but MVC-only)
3. ❌ Async JS providers in Blazor mode (blocked by `requestSubmit` + EventDelegator issue)
4. ❌ `Blazor.submitForm()` API (proposed in `10-blazor-async-compatibility.md`)

For MVC, this **already works today** — the `remote` provider is registered in MVC mode, and `requestSubmit()` works fine for MVC forms.

---

## 6. Recommendations

### 6a. No Immediate Changes Required

Our client-side validation architecture is compatible with the async server-side validation proposal. The two systems operate at different layers and don't interfere with each other.

### 6b. Register Adapters for `AsyncValidationAttribute` When Applicable

When a developer creates an `AsyncValidationAttribute` that also implements `IClientValidationAdapter`, our registry will automatically pick it up (via the `attribute is IClientValidationAdapter selfAdapter` check in `GetAdapter()`). No registration needed.

### 6c. Consider a `RemoteClientAdapter` for Blazor (Future)

When `Blazor.submitForm()` exists, create a generic adapter that can turn any async server validation into a client-side remote check:

```csharp
// Future API
services.AddClientValidationAdapter<UniqueEmailAttribute>(a => new RemoteClientAdapter(
    validationEndpoint: "/api/validate-email",
    additionalFields: "UserId"
));
```

This would let developers opt-in to client-side remote validation for their async attributes without implementing `IClientValidationAdapter` themselves.

### 6d. `EditForm.ValidateAsync()` Transition — Watch for Timing Changes

When `EditForm` switches to `ValidateAsync()`, verify that the submit event flow timing doesn't change in a way that affects our capture-phase handler. The key invariant: our handler must run before EventDelegator processes the event. Since both use capture phase and our handler is registered on `DOMContentLoaded` (before Blazor bootstraps), the ordering should be stable.

### 6e. Document the Layered Validation Model

The validation story becomes:
1. **Client-side JS** — instant, sync-only in Blazor (all built-in + custom sync providers)
2. **Server-side sync** — `ValidationAttribute.IsValid()` after form POST
3. **Server-side async** — `AsyncValidationAttribute.IsValidAsync()` after form POST
4. **Client-side remote** — `fetch()` to server endpoint (MVC-only for now)

Each layer catches progressively more complex validation errors. Document this layered model for users.

---

## 7. Summary

| Concern | Impact | Action |
|---------|--------|--------|
| `AsyncValidationAttribute` discovered by reflection | None — silently skipped if no adapter | ✅ No change |
| `AsyncValidationAttribute` with `IClientValidationAdapter` | Works — self-implementing pattern | ✅ No change |
| `RemoteAttribute` guard | Remains correct — specific to MVC pattern | ✅ No change |
| `EditForm` switching to `ValidateAsync()` | No client-side impact — server-side only | ✅ No change |
| Error message formatting | Unchanged — inherited from `ValidationAttribute` | ✅ No change |
| Duplicate error display | Client and server errors are separate DOM elements | ✅ Acceptable |
| Remote validation in Blazor | Blocked by `requestSubmit` issue | ⏳ Future: `Blazor.submitForm()` |
| End-to-end async attribute story | Works for MVC today, Blazor needs future API | ⏳ Future |
