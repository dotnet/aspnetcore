# Testing Report — Client-Side Validation Feature

**Date:** 2026-03-31
**Spec:** `features/validation-client-side/13-spec-draft.md`

## Test Summary

| Test Suite | Tests | Passed | Failed | Notes |
|---|---|---|---|---|
| **JS Unit Tests** (Jest) | 134 | 134 | 0 | All built-in providers, async coordination, submit interception, DOM sync |
| **C# Unit Tests** — Forms | 131 | 131 | 0 | Adapters, service, component rendering, EditContext integration |
| **C# Unit Tests** — Web/Validation | 18 | 18 | 0 | ValidationMessage, ValidationSummary, InputBase with client validation |
| **Playwright E2E Tests** | 37 | 37 | 0 | Full browser-based testing of all spec scenarios |
| **Total** | **320** | **320** | **0** | |

## JS Unit Tests (134 tests)

**Location:** `src/Components/Web.JS/test/`
**Runner:** Jest
**Command:** `npx jest --config jest.config.js`

### Coverage by file:
- `Validation.BuiltInProviders.test.ts` — All 12 built-in providers (required, length, minlength, maxlength, range, regex, email, url, phone, creditcard, equalto, fileextensions) with valid/invalid/edge cases
- `Validation.Async.test.ts` — ValidationCoordinator async flow, EventManager submit interception (sync/async paths, formnovalidate)
- `Validation.RemoteProvider.test.ts` — Remote provider HTTP behavior, caching, additional fields, POST support
- `DomSync.test.ts` — DOM synchronization for enhanced navigation

## C# Unit Tests (149 tests)

**Location:** `src/Components/Forms/test/`, `src/Components/Web/test/`
**Runner:** xUnit via `dotnet test`

### Coverage:
- 38 tests specifically for `ClientValidation` — adapter registration, attribute discovery, error message resolution, display name resolution, RemoteAttribute guard, caching
- 131 total Forms tests including EditContext, DataAnnotations integration
- 18 Web validation tests for component rendering with/without client validation

## Playwright E2E Tests (37 tests)

**Location:** `features/validation-client-side/test_validation.py`
**Runner:** pytest + pytest-playwright (Chromium)
**App under test:** BlazorSSR sample at `src/Components/Samples/BlazorSSR/`
**Test page:** `/validation-test`

### Test categories and results:

#### 1. Basic Validation (10 tests) ✅
| Test | Spec scenario | Result |
|---|---|---|
| `test_data_val_attributes_present` | Scenario 1 | ✅ PASS |
| `test_novalidate_on_form` | Scenario 8 | ✅ PASS |
| `test_submit_blocked_when_invalid` | Scenario 1 | ✅ PASS |
| `test_validation_summary_populated` | Scenario 1 | ✅ PASS |
| `test_blur_shows_error` | Scenario 4 | ✅ PASS |
| `test_typing_clears_error_after_submit` | Scenario 4 | ✅ PASS |
| `test_valid_form_submits` | Scenario 1 | ✅ PASS |
| `test_formnovalidate_skips_validation` | Scenario 6 | ✅ PASS |
| `test_email_validation` | Scenario 2 | ✅ PASS |
| `test_stringlength_validation` | Scenario 2 | ✅ PASS |

#### 2. Form Reset (2 tests) ✅
| Test | Spec scenario | Result |
|---|---|---|
| `test_reset_clears_validation` | Scenario 4 | ✅ PASS |
| `test_reset_returns_to_pristine` | Scenario 4 | ✅ PASS |

#### 3. Validation Timing / data-val-event (5 tests) ✅
| Test | Spec scenario | Result |
|---|---|---|
| `test_pristine_typing_no_validation` | Scenario 4 | ✅ PASS |
| `test_blur_only_field` | Scenario 4 (data-val-event) | ✅ PASS |
| `test_submit_only_field_no_blur_error` | Scenario 4 (data-val-event="none") | ✅ PASS |
| `test_submit_only_field_validates_on_submit` | Scenario 4 (data-val-event="none") | ✅ PASS |
| `test_after_submit_typing_validates` | Scenario 4 | ✅ PASS |
| `test_blur_only_no_typing_after_submit` | Scenario 4 (data-val-event) | ✅ PASS |

#### 4. Hidden Fields (2 tests) ✅
| Test | Spec scenario | Result |
|---|---|---|
| `test_hidden_field_skipped_on_submit` | Scenario 4 | ✅ PASS |
| `test_visible_field_still_validated` | Scenario 4 | ✅ PASS |

#### 5. All Validation Rules (12 tests) ✅
| Test | Validation rule | Result |
|---|---|---|
| `test_required` | `[Required]` | ✅ PASS |
| `test_email_invalid` / `test_email_valid` | `[EmailAddress]` | ✅ PASS |
| `test_url_invalid` / `test_url_valid` | `[Url]` | ✅ PASS |
| `test_phone_invalid` / `test_phone_valid` | `[Phone]` | ✅ PASS |
| `test_regex_invalid` / `test_regex_valid` | `[RegularExpression]` | ✅ PASS |
| `test_minlength_invalid` | `[MinLength]` | ✅ PASS |
| `test_maxlength_invalid` | `[MaxLength]` | ✅ PASS |
| `test_stringlength_invalid` | `[StringLength]` | ✅ PASS |

#### 6. Constraint Validation API (3 tests) ✅
| Test | Spec scenario | Result |
|---|---|---|
| `test_setcustomvalidity_set_on_invalid` | Scenario 8 | ✅ PASS |
| `test_setcustomvalidity_cleared_on_valid` | Scenario 8 | ✅ PASS |
| `test_validationmessage_readable` | Scenario 8 | ✅ PASS |

#### 7. ARIA Accessibility (2 tests) ✅
| Test | Spec scenario | Result |
|---|---|---|
| `test_aria_invalid_set_on_error` | Scenario 8 | ✅ PASS |
| `test_aria_invalid_removed_on_valid` | Scenario 8 | ✅ PASS |

## Spec Scenario Coverage

| Spec Scenario | E2E Tests | Unit Tests | Status |
|---|---|---|---|
| 1. Basic Blazor SSR form | 5 tests | ✅ C# adapters + JS providers | ✅ Covered |
| 2. Supported validation rules | 12 tests | ✅ 69 provider tests | ✅ Covered |
| 3. Localized error messages | — | — | ⚠️ Not testable (localization package not wired in sample) |
| 4. Validation timing / UX | 8 tests (incl. reset, data-val-event, hidden) | ✅ | ✅ Covered |
| 5. Enhanced navigation | — | ✅ DomSync tests + fingerprinting | ⚠️ E2E not covered (needs multi-page nav test) |
| 6. Opt-in/opt-out | 1 test (formnovalidate) | — | ✅ Partially covered |
| 7. Interactive modes | — | — | ⚠️ Not testable (sample is SSR-only) |
| 8. Constraint API + ARIA | 5 tests | — | ✅ Covered |
| 9. Custom validation attrs | — | ✅ C# adapter registry tests | ⚠️ E2E covered by existing Contact page (NoProfanity) |
| 10. MVC drop-in replacement | — | — | ⚠️ Not testable (no MVC sample in scope) |

## Prototype Changes Validated

The following implementation changes were tested end-to-end:

1. **Hidden field skipping** (`DomScanner.isHidden` + `ValidationCoordinator` skip) — ✅ Hidden fields don't block submit
2. **Form submitted tracking** (`EventManager.submittedForms` WeakSet) — ✅ Typing validates after submit, not before
3. **Full validate on input** (not clear-only) — ✅ Errors can be shown/replaced while typing after submit
4. **`data-val-event` per-field override** — ✅ `"change"` (blur-only) and `"none"` (submit-only) work correctly
5. **Form reset** (`EventManager.attachResetInterception`) — ✅ Clears all state, returns to pristine
6. **ARIA management** (`ErrorDisplay` aria-invalid, aria-describedby) — ✅ Set on error, removed on valid

## JS Bundle Size

| Metric | Value |
|---|---|
| **aspnet-core-validation.js** (standalone) | 12.86 KB (raw), 3.61 KB (Brotli) |
| **blazor.web.js** (includes validation) | 212.7 KB (raw), 50.34 KB (Brotli) |
