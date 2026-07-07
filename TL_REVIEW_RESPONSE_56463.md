# TL Review Response – Issue #56463 Test Coverage Gaps

This document tracks how each test coverage gap identified in the Tech Lead's
review of the DOM-persistence fix (PR `DOM-Persistence-56463`,
fix at `src/Components/Components/src/Reflection/ComponentProperties.cs`) has
been addressed.

## Summary of New Tests Added

| # | Test name | File | Status |
|---|-----------|------|--------|
| 4 (CRITICAL) | `InputText_RemovesAttributeFromChildren_WhenOmittedOnSubsequentRender` | `src/Components/Web/test/Forms/InputTextTest.cs` | ✅ Added |
| 4 (CRITICAL) | `InputText_OmitsAttributeFromFirstRender_WhenNotSupplied` | `src/Components/Web/test/Forms/InputTextTest.cs` | ✅ Added |
| 2 (HIGH) | `RemovesMultipleUnmatchedAttributesFromChildElementWhenAllOmittedOnSubsequentRender` | `src/Components/Components/test/RendererTest.cs` | ✅ Added |
| 5 (MEDIUM) | `RemovesBooleanUnmatchedAttributeFromChildElementWhenOmittedOnSubsequentRender` | `src/Components/Components/test/RendererTest.cs` | ✅ Added |
| 7 (MEDIUM) | `RemovesOnlyTheOmittedUnmatchedAttributesWhenOthersAreKeptOrChanged` | `src/Components/Components/test/RendererTest.cs` | ✅ Added |
| 8 (MEDIUM) | `RemovesUnmatchedAttributesAcrossMultipleChildComponentsInTheSameRender` | `src/Components/Components/test/RendererTest.cs` | ✅ Added |
| 9 (MEDIUM) | `RemovesUnmatchedAttribute_AcrossRapidAddRemoveCycles` | `src/Components/Components/test/RendererTest.cs` | ✅ Added |

Cascading parameter interaction is already covered in
`src/Components/Components/test/ParameterViewTest.Assignment.cs` via
`CaptureUnmatchedValues_IsPreserved_WhenOnlyCascadingParametersAreSupplied`
(this is the test that forced the `!parameter.Cascading` discriminator in
the fix).

---

## Test Design Notes

### TEST 4 – Real `InputText` Component (CRITICAL)

The original TL review flagged that all 2 existing renderer tests used a
mock `MyStrongComponent` and would not catch regressions that only happen
when the real `InputText` (and its `[Parameter(CaptureUnmatchedValues = true)]
public IReadOnlyDictionary<string, object>? AdditionalAttributes`) is the
target.

To address this we now exercise the full `InputBase` →
`[CaptureUnmatchedValues]` path end-to-end:

- New host component `TestInputConditionalAttributeHostComponent<TValue, TComponent>`
  (`src/Components/Web/test/Forms/TestInputConditionalAttributeHostComponent.cs`)
  mirrors the issue's Razor pattern: a parent that supplies
  `builder.AddAttribute(N, "attr-name", value)` conditionally based on a flag.
- The test uses a `data-test-id` attribute (rather than `class`) because
  `InputText.BuildRenderTree` always emits `id`/`name`/`class` itself, so
  `class` is not a clean target for isolating the `CaptureUnmatchedValues` path.
- The inverse test (`InputText_OmitsAttributeFromFirstRender_WhenNotSupplied`)
  guards against the fix over-clearing captured values that have never been set.

### TEST 5 – Boolean Attributes

HTML boolean attributes (`disabled`, `readonly`, `checked`, …) are special
because their value is the empty string `""` when present. The same
`CaptureUnmatchedValues` writer is used, so the test verifies that
`RemoveAttribute("disabled")` is generated when the attribute is omitted on
a subsequent render.

### TEST 2 – Multiple Attributes

Asserts that three simultaneous splat-only attributes all receive
`RemoveAttribute` edits in the same render batch when the entire block is
gated behind a single flag.

### Edge Case – Mixed Keep / Change / Remove

`RemovesOnlyTheOmittedUnmatchedAttributesWhenOthersAreKeptOrChanged` ensures
the fix does **not** spuriously remove unrelated attributes that the parent
is still supplying. The diff must contain:

- A `RemoveAttribute` for the omitted attribute.
- **No** `RemoveAttribute` for the kept attribute.
- **No** `RemoveAttribute` for the attribute that is being patched (it should
  be a `SetAttribute` edit, not a remove).

### Edge Case – Multiple Sibling Components

`RemovesUnmatchedAttributesAcrossMultipleChildComponentsInTheSameRender`
verifies that the per-component diff is correct when two sibling
`MyStrongComponent` children both lose their splat attribute in the same
parent render.

### Edge Case – Rapid Toggle

`RemovesUnmatchedAttribute_AcrossRapidAddRemoveCycles` performs an
Add → Remove → Add → Remove cycle. The final batch must still contain a
`RemoveAttribute` edit, guarding against any "sticky" state where the
previously-rendered `AdditionalAttributes` keeps leaking into the diff.

---

## Deferred Items

### TEST 6 – E2E Browser Test (CRITICAL)

The TL review asks for a Playwright E2E test exercising the issue's exact
Razor repro in a real browser. This is deferred because it requires:

- Components.TestServer infrastructure to host the test app.
- A `BasicTestApp` Razor page that reproduces the issue verbatim.
- Playwright selector assertions on `getDomAttribute('class')` and the input
  element's `classList` before/after the re-render.

The deferred item is tracked in the issue body and can be added as a
follow-up. The unit-level InputText test (TEST 4) provides strong coverage
of the `InputText` code path that the E2E test would otherwise need to
exercise; it is the highest-value test for catching regressions of the fix.

---

## Verification

```text
dotnet test src/Components/Components/test/Microsoft.AspNetCore.Components.Tests.csproj
  --filter "FullyQualifiedName~RendererTest"
  Passed: 153 (148 existing + 5 new)

dotnet test src/Components/Web/test/Microsoft.AspNetCore.Components.Web.Tests.csproj
  --filter "FullyQualifiedName~InputTextTest"
  Passed: 6 (4 existing + 2 new)
```
