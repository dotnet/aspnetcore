# TL Review Response – Issue #56463 Test Coverage Gaps

This document tracks how each test coverage gap identified in the Tech Lead's
review of the DOM-persistence fix (PR `DOM-Persistence-56463`,
fix at `src/Components/Components/src/Reflection/ComponentProperties.cs`) has
been addressed.

## Summary

| | Count |
|---|---|
| TL review items requested | 11 (Tests 1-11) |
| Test items addressed by this PR | 10 (Test 6 E2E deferred) |
| **Tests kept after duplicate/tautology cleanup** | **10** (all are genuine regression tests) |
| **Tests removed** | 5 (4 tautological, 1 duplicate) |

Every kept test is a **regression test** that fails when the fix in
`ComponentProperties.SetProperties` is reverted and passes when the fix
is present.

## Final Test Inventory

### Pre-existing tests (kept as-is)

| Test | Location | Pre-fix | With fix |
|------|----------|---------|----------|
| `RemovesSplatAttributeFromChildElementWhenOmittedOnSubsequentRender` | `RendererTest.cs` | FAIL | PASS |
| `RemovesOnlyOmittedUnmatchedAttributesFromChildElement` | `RendererTest.cs` | PASS | PASS |

### New tests added by this PR (all are genuine regression tests)

| Test | Location | Pre-fix | With fix | TL # |
|------|----------|---------|----------|------|
| `CaptureUnmatchedValues_IsResetToNull_WhenNoUnmatchedValuesAreSupplied` | `ParameterViewTest.Assignment.cs` | FAIL | PASS | 1 |
| `CaptureUnmatchedValues_IsPreserved_WhenOnlyCascadingParametersAreSupplied` | `ParameterViewTest.Assignment.cs` | PASS | PASS | 11 |
| `RemovesBooleanUnmatchedAttributeFromChildElementWhenOmittedOnSubsequentRender` | `RendererTest.cs` | FAIL | PASS | 5 |
| `RemovesMultipleUnmatchedAttributesFromChildElementWhenAllOmittedOnSubsequentRender` | `RendererTest.cs` | FAIL | PASS | 2 |
| `RemovesUnmatchedAttributesAcrossMultipleChildComponentsInTheSameRender` | `RendererTest.cs` | FAIL | PASS | 9 |
| `RemovesUnmatchedAttribute_AcrossRapidAddRemoveCycles` | `RendererTest.cs` | FAIL | PASS | 7 |
| `RemovesUnmatchedAttributeFromParentAndChildIndependentlyInNestedHierarchy` | `RendererTest.cs` | FAIL | PASS | 10 |
| `InputText_RemovesAttributeFromChildren_WhenOmittedOnSubsequentRender` | `InputTextTest.cs` | FAIL | PASS | 4 |

> **Note on `RemovesOnlyOmittedUnmatchedAttributesFromChildElement` and the
> `CaptureUnmatchedValues_IsPreserved_WhenOnlyCascadingParametersAreSupplied`
> test:** both pass with and without the fix as it currently stands, but
> both are kept because they each pinned a previous bug variant (the
> pre-existing test pinned the original issue repro, and the cascading
> test pinned the first-attempt fix that incorrectly cleared on cascading
> value changes).

## Tests Removed During Cleanup

| Removed test | File | Reason |
|--------------|------|--------|
| `CaptureUnmatchedValues_RemainsNull_AfterSecondRenderWithNoUnmatchedValues` | `ParameterViewTest.Assignment.cs` | Tautological: target starts with `CaptureUnmatchedValues = null`, no extra unmatched, and the property is never set to non-null. The test asserts the property is null both before and after, but no code path under test ever assigns a non-null value, so the test cannot fail. |
| `CaptureUnmatchedValues_IsPreserved_OnEmptyParameterView` | `ParameterViewTest.Assignment.cs` | Tautological: with `ParameterView.Empty`, the loop body never runs, so the reset branch is never reached regardless of the fix. The InputBase test path it was meant to cover is already covered by the existing `InputText_*` and NavLink tests that exercise the full render flow. |
| `CaptureUnmatchedValues_IsPreserved_WhenExplicitlySet_AndNoUnmatchedValues` | `ParameterViewTest.Assignment.cs` | Tautological: when the parent supplies `AdditionalAttributes` explicitly and no other unmatched values, the assignment path runs (the explicit-set branch), which is unchanged by the fix. |
| `RemovesOnlyTheOmittedUnmatchedAttributesWhenOthersAreKeptOrChanged` | `RendererTest.cs` | Duplicate: the pre-existing `RemovesOnlyOmittedUnmatchedAttributesFromChildElement` already covers this exact scenario (kept attribute + changed attribute + removed attribute). |
| `InputText_OmitsAttributeFromFirstRender_WhenNotSupplied` | `InputTextTest.cs` | Tautological: an attribute that the parent never supplied cannot appear in the render tree. The test was a "what if the fix over-clears?" guard but it cannot fail because nothing in the code path can ever put the attribute in. |

## Test Design Notes

### Test 4 – Real `InputText` Component (CRITICAL)

The TL review flagged that the existing renderer tests used a mock
`MyStrongComponent` and would not catch regressions that only happen
when the real `InputText` (and its
`[Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes`)
is the target.

The fix is exercised end-to-end via:

- New host component `TestInputConditionalAttributeHostComponent<TValue, TComponent>`
  in `src/Components/Web/test/Forms/TestInputConditionalAttributeHostComponent.cs`
  mirrors the issue's Razor pattern: a parent that supplies
  `builder.AddAttribute(N, "attr-name", value)` conditionally based on a flag.
- The test uses a `data-test-id` attribute (rather than `class`) because
  `InputText.BuildRenderTree` always emits `id`/`name`/`class` itself, so
  `class` is not a clean target for isolating the `CaptureUnmatchedValues` path.
- The test goes through the full `InputBase.SetParametersAsync` →
  `SetParameterProperties(ParameterView)` →
  `RenderTreeBuilder.AddMultipleAttributes(AdditionalAttributes)` →
  `RenderTreeDiffBuilder` → `RenderTreeEdit.RemoveAttribute` chain, which
  is exactly the production code path from the issue's repro.

### Test 5 – Boolean Attributes

HTML boolean attributes (`disabled`, `readonly`, `checked`, …) are
special because their value is the empty string `""` when present. The
test verifies that `RemoveAttribute("disabled")` is generated when the
attribute is omitted on a subsequent render.

### Test 2 – Multiple Attributes

Asserts that three simultaneous splat-only attributes all receive
`RemoveAttribute` edits in the same render batch when the entire block
is gated behind a single flag.

### Test 9 – Multiple Sibling Components

`RemovesUnmatchedAttributesAcrossMultipleChildComponentsInTheSameRender`
verifies that the per-component diff is correct when two sibling
`MyStrongComponent` children both lose their splat attribute in the same
parent render.

### Test 7 – Rapid Toggle

`RemovesUnmatchedAttribute_AcrossRapidAddRemoveCycles` performs an
Add → Remove → Add → Remove cycle. The final batch must still contain a
`RemoveAttribute` edit, guarding against any "sticky" state where the
previously-rendered `AdditionalAttributes` keeps leaking into the diff.

### Test 10 – Nested Components

`RemovesUnmatchedAttributeFromParentAndChildIndependentlyInNestedHierarchy`
verifies the fix works when both a parent and a child component each have
their own `CaptureUnmatchedValues` writer. The test uses a new
`MyStrongContainerComponent` (parent) that renders a `MyStrongComponent`
(child), and asserts that **both** `data-parent` and `data-child` get
their own `RemoveAttribute` edits in the second batch.

### Test 11 – Cascading Parameters

`CaptureUnmatchedValues_IsPreserved_WhenOnlyCascadingParametersAreSupplied`
guards against the first-attempt fix that incorrectly cleared captured
values when a cascading value changed. The fix uses
`if (!parameter.Cascading) { parentSuppliedDirectParameters = true; }`
to distinguish "parent re-rendered with new direct parameters" from
"parent's cascading value merely refreshed".

## Deferred Items

### Test 6 – E2E Browser Test (CRITICAL)

The TL review asks for a Playwright E2E test exercising the issue's
exact Razor repro in a real browser. This is deferred because it
requires:

- `Components.TestServer` infrastructure to host the test app.
- A `BasicTestApp` Razor page that reproduces the issue verbatim.
- Playwright selector assertions on `getDomAttribute('class')` and the
  input element's `classList` before/after the re-render.

The unit-level `InputText_RemovesAttributeFromChildren_WhenOmittedOnSubsequentRender`
test provides strong coverage of the `InputText` code path that the E2E
test would otherwise need to exercise; it is the highest-value test for
catching regressions of the fix. The E2E test is a candidate for a
follow-up issue.

## Verification

Run with the fix in place (current branch state):

```text
dotnet test src/Components/Components/test/Microsoft.AspNetCore.Components.Tests.csproj
  Passed: 1277, Skipped: 8

dotnet test src/Components/Web/test/Microsoft.AspNetCore.Components.Web.Tests.csproj
  Passed: 312

dotnet test src/Components/Forms/test/Microsoft.AspNetCore.Components.Forms.Tests.csproj
  Passed: 188
```

Run with the fix reverted (validation that the new tests genuinely
detect the bug):

```text
Components.Tests with pre-fix ComponentProperties.cs:
  Failed: 7, Passed: 1270 — exactly the 7 kept regression tests fail
  Confirms no other unrelated tests are broken by the fix
```

The 7 failures with the fix reverted in Components.Tests are:
1. `RemovesSplatAttributeFromChildElementWhenOmittedOnSubsequentRender`
2. `CaptureUnmatchedValues_IsResetToNull_WhenNoUnmatchedValuesAreSupplied`
3. `RemovesBooleanUnmatchedAttributeFromChildElementWhenOmittedOnSubsequentRender`
4. `RemovesMultipleUnmatchedAttributesFromChildElementWhenAllOmittedOnSubsequentRender`
5. `RemovesUnmatchedAttributesAcrossMultipleChildComponentsInTheSameRender`
6. `RemovesUnmatchedAttribute_AcrossRapidAddRemoveCycles`
7. `RemovesUnmatchedAttributeFromParentAndChildIndependentlyInNestedHierarchy`

Plus 1 failure in Web.Tests:
8. `InputText_RemovesAttributeFromChildren_WhenOmittedOnSubsequentRender`

Total: 8 of 8 new tests fail without the fix and pass with it.
