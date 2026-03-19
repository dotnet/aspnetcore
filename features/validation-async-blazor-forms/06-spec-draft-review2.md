# Spec Review (Iteration 2) — Async Validation Support for Blazor Forms

> Fresh review of `04-spec-draft-v3.md` following the FeatureSpecReviewer guidelines.

---

## Summary

- **Is the summary clear?** Yes. Three well-structured paragraphs: problem, current pain with ecosystem context, and proposed solution.
- **Is the summary specific enough?** Yes. Names specific components, APIs, and the BCL dependency.
- **Does it describe the solution at a high level?** Yes.

No issues found in this section.

---

## Goals

- **Are the goals listed?** Yes — 7 goals.
- **Are the goals specific?** Yes. Each describes a concrete capability with context on today's limitations.
- **Are the goals measurable?** Mostly. "Enable async validation on form submit" and "Maintain full backward compatibility" are testable. "Improve third-party async validator integration" could be more measurable — what constitutes "improved"? However, the description clarifies the intent (async extension point without forking EditForm).
- **Are the goals relevant?** Yes — each maps to documented community pain.

No issues found in this section.

---

## Non-goals

- **Are the non-goals listed?** Yes — 4 non-goals.
- **Are the reasons for the non-goals clear?** Yes. Each explains why it's excluded and what alternatives exist.

No issues found in this section.

---

## Proposed Solution

- **Does the proposed solution describe how it addresses each goal?** Yes. The two mechanisms (`OnValidationRequestedAsync` for form-submit, `AddValidationTask` for field-level) clearly map to the submit and field-change goals. Pending/cancelled state, cancellation, backward compat, third-party integration, and rendering modes are each addressed by dedicated scenarios.
- **Are the scenarios listed?** Yes — 13 scenarios.
- **Are the scenarios sorted from simple to complex?** Yes — basic submit → explicit submit → per-field → complex models → edge cases → UI customization → library integration → SSR.
- **Does it describe the main elements of the solution?** Yes.
- **Are the outputs of each scenario described clearly?** Mostly — see per-scenario notes below.

### Scenario-by-scenario review:

**Scenario 1: Validate form with async validators on submit**
No issues found in this scenario.

**Scenario 2: Validate form with async validators explicitly**
No issues found in this scenario.

**Scenario 3: Validate individual form field with async validators**
- The Razor snippet was trimmed with `...` to avoid repeating Scenario 1's boilerplate — good.
- The model class shows only `Email` with `[UniqueEmail]` but the Razor also references `Username`. Minor inconsistency — the model was trimmed but the Razor still shows both fields. Either trim the Razor to match or keep both fields in the model.

**Scenario 4: Validate complex form with async validators on submit**
- `OrderItem` is missing the `[ValidatableType]` attribute that `Order` has. Since the scenario text says `AddValidation()` + `[ValidatableType]` enables complex object graph traversal, `OrderItem` should also have the attribute for consistency.

**Scenario 5: Submit form while per-field async validations are pending**
No issues found in this scenario.

**Scenario 6: Use existing sync-only forms without changes**
- The TODO about resolving Obsolete vs syncOnly is appropriate for a draft.
- The scenario clearly states that `ValidateAsync()` invokes both sync and async handlers — good for backward compat clarity.

No issues found in this scenario.

**Scenario 7: Display async validation state declaratively**
- The two TODOs (DOM breaking change concern, alternative approaches) are well-articulated.

No issues found in this scenario.

**Scenario 8: Check async validation state programmatically**
- The naming TODO is appropriate and well-described with concrete alternatives.

No issues found in this scenario.

**Scenario 9: Style input forms based on async validation state**
- The CSS snippet still includes `input.valid` and `input.invalid` lines (existing behavior, not new). The earlier code review pass intended to trim these to focus on new classes only, but the trim appears incomplete — the `background-repeat` and `background-position` properties were added back for `input.pending`. This is minor.

**Scenario 10: Handle stale validations and validation failures**
- The TODO about form-submit validation failure is important and well-scoped with three concrete options.

No issues found in this scenario.

**Scenario 11: Retry validation for a specific field**
- The state transition is now clearly described (cancelled → pending → valid/invalid) — good.
- The TODO about `NotifyFieldChanged` vs `ValidateFieldAsync` provides good context.

No issues found in this scenario.

**Scenario 12: Integrate third-party async validators**
- The code now shows both form-submit (`OnValidationRequestedAsync`) and per-field (`OnFieldChanged` + `AddValidationTask`) integration — good.
- The code references `ValidateFieldAsync(args.FieldIdentifier, cts.Token)` as a method on the component, but this could be confused with `EditContext.ValidateFieldAsync` from Scenario 11. A different method name (e.g., `RunFieldValidationAsync`) or a comment clarifying this is the component's own method would help.

**Scenario 13: Validate forms with async validators in static SSR**
- Now explicitly covers both enhanced and non-enhanced SSR forms — good.
- The two TODOs (client validation interaction, streaming UX) are appropriate for future consideration.

No issues found in this scenario.

---

## Assumptions

- **Are the assumptions listed?** Yes — 4 assumptions.
- **Are the assumptions valid?** Yes.
- **Are there any missing assumptions?**
  - The spec assumes that `OnFieldChanged` handlers can start async work (fire-and-forget via `AddValidationTask`) and return synchronously. This pattern is demonstrated in Scenario 12 but not stated as an assumption. It's a pattern that may surprise developers unfamiliar with async event handling. Consider adding it as an assumption.

---

## References

- **Are the references listed?** Yes — 14 references.
- **Are the references relevant?** Yes — each includes an explanation of why it's relevant.
- **Are the references specific enough?** Yes — issue links point to specific issues, the #40244 link points to a specific comment, Angular/react-hook-form links point to specific documentation pages.
- **Are there any missing references?** No — the tracking epic, original design, BCL dependencies, community issues, third-party libraries, and prior art are all covered.

No issues found in this section.

---

## Summary of Findings

| Severity | Count | Description |
|----------|-------|-------------|
| **Fix** | 1 | Scenario 4: `OrderItem` missing `[ValidatableType]` attribute |
| **Fix** | 1 | Scenario 3: Model class trimmed to one field but Razor still shows two fields |
| **Clarify** | 1 | Scenario 12: `ValidateFieldAsync` method name could be confused with `EditContext.ValidateFieldAsync` |
| **Consider** | 1 | Missing assumption about sync event handlers starting async work via `AddValidationTask` |
| **Open TODOs** | 5 | Validate() Obsolete vs syncOnly (Scenario 6), ValidationMessage DOM change (Scenario 7), naming of "cancelled" state (Scenario 8), form-submit validation failure behavior (Scenario 10), ValidateFieldAsync vs NotifyFieldChanged (Scenario 11) |

Overall the spec is well-structured, complete, and actionable. The remaining open TODOs are clearly scoped and can be resolved during design/implementation.
