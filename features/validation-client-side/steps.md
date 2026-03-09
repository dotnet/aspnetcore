# Client-Side Form Validation for Blazor SSR — Progress Log

**Issue:** [dotnet/aspnetcore#51040](https://github.com/dotnet/aspnetcore/issues/51040)
**Branch:** `oroztocil/validation-client-side`

---

## Step 1: Research & Requirements

**Commit:** `2c675efa07`

Conducted comprehensive research and wrote `features/validation-client-side/01-research.md` covering:

- **GitHub issue analysis**: Read #51040 thread (18 comments), the design doc by @javiercn (#issuecomment-3706000376), and related issue #28640.
- **MVC unobtrusive validation architecture**: Studied the full pipeline from DataAnnotations → `IClientModelValidatorProvider` → adapter classes (`RequiredAttributeAdapter`, `RangeAttributeAdapter`, etc.) → `data-val-*` HTML attributes → `jquery.validate.unobtrusive.js`. Key code in `src/Mvc/Mvc.DataAnnotations/src/`, `src/Mvc/Mvc.TagHelpers/src/`, `src/Mvc/Mvc.ViewFeatures/src/`.
- **Blazor forms & validation architecture**: Studied `EditForm`, `EditContext`, `InputBase<T>`, `DataAnnotationsValidator`, `ValidationMessage<T>`, `ValidationSummary`, SSR form mapping (`FormMappingContext`, `FormMappingValidator`), and the enhanced navigation JS layer (`NavigationEnhancement.ts`, `DomSync.ts`, `Boot.Web.ts`).
- **Community & prior art**: Reviewed Phil Haack's [aspnet-client-validation](https://github.com/haacked/aspnet-client-validation) (~4 KB gzip, jQuery-free), Damian Edwards' [WasmClientSideValidation](https://github.com/DamianEdwards/WasmClientSideValidation) experiment, and community workaround attempts (SmartInput component by @sweeperq).
- **Web standards**: Documented the Constraint Validation API (`setCustomValidity`, `ValidityState`, `checkValidity`, `novalidate`) — universal browser support, zero dependencies.
- **Requirements**: Defined 8 functional requirements (attribute emission, client-side validation, message display, enhanced nav compatibility, opt-in behavior, etc.), 6 non-functional requirements (payload size, accessibility, extensibility, MVC convergence), explicit out-of-scope items, and 8 open design questions.

## Step 2: BlazorSSR Sample App

**Commits:** `2c675efa07`, `8ddac031ef`

Created a new sample app at `src/Components/Samples/BlazorSSR/` — a pure static SSR Blazor app with **no interactive render modes** (no `AddInteractiveServerComponents`, no WebAssembly).

### Structure

```
src/Components/Samples/BlazorSSR/
├── App.razor                    # Root component (HTML shell, blazor.web.js)
├── BlazorSSR.csproj             # Web SDK project, no interactivity references
├── Layout/
│   └── MainLayout.razor         # Minimal layout
├── Models/
│   └── ContactModel.cs          # Form model with DataAnnotations
├── Pages/
│   ├── Index.razor              # Home page with link to contact form
│   └── Contact.razor            # Form page with EditForm + validation
├── Program.cs                   # AddRazorComponents() / MapRazorComponents<App>() only
├── Properties/
│   └── launchSettings.json      # http://localhost:5280
├── Routes.razor
├── _Imports.razor
├── appsettings.json
├── appsettings.Development.json
└── wwwroot/css/site.css
```

### ContactModel Validation Attributes

The model (`Models/ContactModel.cs`) exercises the core DataAnnotations that will need client-side support:

| Property | Attributes |
|----------|-----------|
| `Name` | `[Required]`, `[StringLength(100, MinimumLength = 2)]` |
| `Email` | `[Required]`, `[EmailAddress]` |
| `PhoneNumber` | `[Phone]` (optional) |
| `Age` | `[Required]`, `[Range(18, 120)]` |
| `Website` | `[Url]` (optional) |
| `Message` | `[Required]`, `[MinLength(10)]`, `[MaxLength(1000)]` |
| `ReferenceCode` | `[RegularExpression(@"^[A-Z]{2}-\d{4}$")]` (optional) |

### Verification with Playwright

Tested the running app interactively:

1. **Home page** (`/`) — renders correctly, link to contact form works
2. **Empty form submit** — all `[Required]` errors appear in both `ValidationSummary` (list at top) and per-field `ValidationMessage` components
3. **Invalid data submit** — tested with short name (StringLength), bad email (EmailAddress), out-of-range age (Range), short message (MinLength), invalid reference code (RegularExpression) — all produce correct error messages
4. **Valid data submit** — filling all fields correctly shows success message: "Thank you, Jane Smith! We received your message."

**Key observation**: Today, Blazor SSR forms render **no `data-val-*` attributes** on inputs, and `ValidationMessage`/`ValidationSummary` render **nothing** when there are no errors. All validation requires a server round-trip. This is the gap the feature will close.

## Step 3: Prior Art Analysis

**File:** `features/validation-client-side/02-prior-art.md` (uncommitted)

Deep-dive analysis of [haacked/aspnet-client-validation](https://github.com/haacked/aspnet-client-validation) — a jQuery-free (~4 KB gzip) drop-in replacement for `jquery.validate.unobtrusive.js`. Reviewed the full TypeScript source (~1,565 lines) and documented:

- **Architecture**: `ValidationService` orchestrator with pluggable `ValidationProvider` functions `(value, element, params) => boolean | string`. Providers registered by name, parsed from `data-val-{rule}` / `data-val-{rule}-{param}` attributes via a clean two-pass algorithm.
- **Built-in providers**: 12 validators (required, length, maxlength, minlength, range, regex, equalto, email, url, phone, creditcard, remote) — all skip validation on empty values, deferring to `required`.
- **Validation timing**: Debounced input/change events with smart UX — `input` events only *clear* errors, `change`/blur events can *set* errors. This prevents "red while typing" annoyance.
- **DOM manipulation**: Updates `data-valmsg-for` spans and `data-valmsg-summary` containers with CSS class toggling.
- **MutationObserver**: Watches for dynamic DOM changes (added/removed inputs).
- **Extensibility**: `addProvider()`, overridable hooks (preValidate, handleValidated, highlight/unhighlight), configurable CSS class names.

### Key Decisions from Analysis

**Adopt from the library:**
1. `data-val-*` attribute protocol (same as MVC, ecosystem compatible)
2. Provider/plugin architecture with `addProvider(name, callback)`
3. Two-pass directive parsing algorithm
4. Smart validation timing (clear on input, invalidate on change/blur)
5. Empty-value passthrough convention
6. `formnovalidate` support per HTML spec

**Do differently in our implementation:**
1. **Constraint Validation API** (`setCustomValidity()`) instead of custom state tracking — enables `:invalid` CSS pseudo-class, screen reader integration
2. **ARIA from day one** — `aria-invalid`, `aria-describedby`, `aria-live` (library has none)
3. **`textContent` not `innerHTML`** — prevent XSS in error message display
4. **`WeakMap` for state tracking** — instead of GUID arrays (O(1) lookup, auto-GC)
5. **Synchronous validation** — no Promises needed without remote validation
6. **Enhanced navigation integration** — hook into submit flow before enhanced nav, don't call `form.submit()` (which bypasses enhanced nav entirely)
7. **`enhancedload` event** — instead of MutationObserver for post-navigation re-scan

## Step 4: Prototype Implementation Plan

**File:** `features/validation-client-side/03-prototype-plan.md` (uncommitted)

Wrote a full implementation plan for the JavaScript validation library prototype. Key architectural decisions:

- **Three-layer architecture**: Core validation engine (host-agnostic) → Unobtrusive adapter (event wiring, error display) → Blazor wiring layer (`enhancedload` integration)
- **Constraint Validation API**: `setCustomValidity()` as the primary validity state mechanism — enables `:invalid` CSS pseudo-class and screen reader integration
- **Capture-phase submit interception**: Validation handler runs in capture phase, before Blazor's enhanced navigation handler (which uses bubble phase). If validation fails, `preventDefault()` + `stopPropagation()` blocks enhanced nav.
- **WeakMap state tracking**: `WeakMap<Element, State>` for O(1) lookup and automatic GC when elements are removed by DOM patching
- **ARIA-ready design**: `markInvalid()`/`markValid()` are explicit methods with commented extension points for `aria-invalid`, `aria-describedby`
- **Synchronous-only**: No async/Promise pipeline — keeps the prototype simple
- **MVC protocol compatibility**: Uses `data-val-*` / `data-valmsg-*` attribute protocol, MVC CSS class defaults

Planned file structure under `src/Components/Web.JS/src/Validation/`:
- `Types.ts`, `ValidationEngine.ts`, `BuiltInProviders.ts`, `DirectiveParser.ts`
- `ValidationCoordinator.ts`, `EventManager.ts`, `ErrorDisplay.ts`, `DomScanner.ts`
- `BlazorWiring.ts`, `index.ts`

8 built-in providers: required, length, minlength, maxlength, range, regex, email, url.
22 implementation tasks across 5 phases. Sample app to be modified with manual `data-val-*` attributes for testing.
