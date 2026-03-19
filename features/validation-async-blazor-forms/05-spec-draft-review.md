# Spec Review — Async Validation Support for Blazor Forms

> Review of `04-spec-draft-v3.md` following the FeatureSpecReviewer guidelines.
> Decisions context: `spec-notes.md` and session design decisions log.

---

## Summary

- **Is the summary clear?** Yes. It clearly states the problem (sync-only pipeline), the impact (deadlocks, WASM incompatibility, third-party library limitations), and the proposed solution (ValidateAsync, async events, task tracking, UI feedback).
- **Is the summary specific enough?** Yes. It names the specific components affected (`EditContext`, `EditForm`, `DataAnnotationsValidator`) and the specific APIs being added (`ValidateAsync()`, async event model, task tracking).
- **Does it describe the solution at a high level?** Yes. The third paragraph summarizes the solution elements concisely.

**Issue:** The summary mentions "enabling async validation to work end-to-end across all Blazor rendering modes" but the spec only has a dedicated scenario for Static SSR (Scenario 13). Interactive Server and WebAssembly are not called out as separate scenarios. It would strengthen the summary to briefly note that interactive rendering modes (Server, WebAssembly, Auto) work naturally since `EditForm.HandleSubmitAsync` is already async — only SSR requires special consideration.

---

## Goals

- **Are the goals listed?** Yes — 6 goals.
- **Are the goals specific?** Mostly yes. Each goal describes a concrete capability.
- **Are the goals measurable?** Partially. "Enable async validation on form submit" is testable. "Maintain full backward compatibility" is testable. "Expose pending and cancelled validation state" is testable. However, "Enable third-party validator integration" is harder to measure — what constitutes "enabled"? A more measurable framing would be: "Provide a public async extension point that validator components can subscribe to."
- **Are the goals achievable?** Yes, with the caveat that some depend on BCL changes (noted in Assumptions).
- **Are the goals relevant?** Yes — each maps to documented community pain points.

**Issue:** The "Cancel stale validations" goal describes the mechanism (cancel via CancellationToken) but does not describe the user-visible outcome clearly. Consider rephrasing to emphasize the user outcome: "...so that only the validation result for the current field value is ever displayed."

**Issue:** Minor typo in "Maintain full backward compatibility" — "noticable" should be "noticeable."

---

## Non-goals

- **Are the non-goals listed?** Yes — 4 non-goals.
- **Are the reasons for the non-goals clear?** Yes. Each explains why it's excluded and what the alternative is.

**Issue:** The debouncing non-goal says "debouncing is unnecessary for the common case" but does not mention what developers should do if they *do* need debouncing (e.g., if they override input binding to use `oninput`). A brief note on the workaround (implement debounce manually in the component) would make this more complete.

---

## Proposed Solution

- **Does the proposed solution describe how it addresses each goal?** Partially. The two mechanisms (`OnValidationRequestedAsync` and `AddValidationTask`) are well described. However, the proposed solution intro does not explicitly map to the "cancelled state" goal — it mentions pending state and cancellation but not the cancelled/failed state that was added later.
- **Are the scenarios listed?** Yes — 13 scenarios.
- **Are the scenarios sorted from simple to complex?** Yes — basic submit → explicit submit → field-level → complex models → edge cases → customization → library authors → SSR.
- **Does it describe the main elements of the solution?** Yes.

### Scenario-by-scenario review:

**Scenario 1: Validate form with async validators on submit**
No issues found in this scenario.

**Scenario 2: Validate form with async validators explicitly**
No issues found in this scenario.

**Scenario 3: Validate individual form field with async validators**
No issues found in this scenario. (The earlier TODO about parallel vs queued was removed.)

**Scenario 4: Validate complex form with async validators on submit**
- The scenario requires `AddValidation()` in startup and `[ValidatableType]` on model classes. It would be helpful to describe what happens if the developer forgets `AddValidation()` — does it fall back to the legacy `Validator` path? Does async validation silently not run? This edge case is important for developer experience.

**Scenario 5: Submit form while per-field async validations are pending**
No issues found in this scenario.

**Scenario 6: Use existing sync-only forms without changes**
- The scenario presents two alternatives (Obsolete vs syncOnly) with a TODO. This is appropriate for a draft spec, but should be resolved before the spec is finalized.
- The scenario should explicitly state that existing `OnValidationRequested` sync event subscribers still work — `ValidateAsync()` invokes both sync and async handlers. This is mentioned in Scenario 2 but should be reinforced here since backward compatibility is the focus.

**Scenario 7: Display async validation state declaratively**
- `ValidationMessage` currently does not accept render fragments. The scenario proposes adding `PendingContent` and `CancelledContent`. It should note that this changes `ValidationMessage` from a "leaf" component to one that accepts child content, which may have implications for how it renders (currently renders a flat list of `<div>` elements).
- The TODO about alternative approaches is appropriate.

**Scenario 8: Check async validation state programmatically**
No issues found in this scenario.

**Scenario 9: Style input forms based on async validation state**
- The scenario does not describe whether the existing `FieldCssClassProvider` will return compound classes like `"modified pending"` and `"modified cancelled"` (matching the existing `"modified valid"` / `"modified invalid"` pattern), or just `"pending"` / `"cancelled"`. This should be clarified.

**Scenario 10: Handle stale validations and validation failures**
- The scenario describes field-level failure clearly. However, it does not describe what happens during **form-submit** async validation failure. If the `OnValidationRequestedAsync` handler throws (e.g., network error during full-form validation), does the form enter a cancelled state? Or does the exception propagate to `EditForm.HandleSubmitAsync`? This edge case should be addressed.

**Scenario 11: Retry validation for a specific field**
- `ValidateFieldAsync` is a new API that doesn't exist today. The scenario should state explicitly that this is a new method being proposed on `EditContext`.
- The scenario does not describe the expected outcome after retry succeeds — does the field transition from "cancelled" → "pending" → "valid"/"invalid"?

**Scenario 12: Integrate third-party async validators**
- The code snippet shows `FluentValidationValidator` declaring `IDisposable` but does not show a `Dispose` method with unsubscription from `OnValidationRequestedAsync`. This is important for preventing memory leaks and should be shown.
- The scenario describes form-submit integration via `OnValidationRequestedAsync` but does not show how a third-party library would integrate with per-field async validation (using `AddValidationTask`). A brief note about this would be helpful.

**Scenario 13: Validate forms with async validators in static SSR**
- The scenario shows `Enhance` on `EditForm` but does not discuss the non-enhanced case (full page reload on POST). In both cases `ValidateAsync` is awaited server-side, so the behavior is the same — but this could be stated explicitly.
- The two TODOs are appropriate for this stage.

---

## Assumptions

- **Are the assumptions listed?** Yes — 4 assumptions.
- **Are the assumptions valid?** Mostly yes. The `Validator.TryValidatePropertyAsync` dependency is well documented.
- **Are there any missing assumptions?**
  - The spec assumes that `OnFieldChanged` handlers can fire-and-forget async work (start async validation, register via `AddValidationTask`, return synchronously). This assumption about the sync event handler starting async work should be stated explicitly — it's a pattern that some developers may find surprising.
  - The spec assumes the `Microsoft.Extensions.Validation` package's `ValidateAsync` will properly handle `CancellationToken` propagation for field-level cancellation. This should be validated.

---

## References

- **Are the references listed?** Yes — 12 references.
- **Are the references relevant?** Yes — each includes an explanation of relevance.
- **Are there any missing references?**
  - The `spec-notes.md` file references [Blazored #31](https://github.com/Blazored/FluentValidation/issues/31) which is not in the spec's references. This is another discussion about async FluentValidation issues that provides additional context.
  - [dotnet/aspnetcore #64892](https://github.com/dotnet/aspnetcore/issues/64892) (the tracking epic) was in earlier versions of the spec but is missing from the v3 references. It should be included as it is the parent work item.
- **Are the references specific enough?** The aspnetcore issue links go to the issue level, which is appropriate. The #40244 link correctly points to a specific comment. The Angular and react-hook-form links could be more specific (link to the async validation section rather than the general forms page).

---

## Summary of Findings

| Severity | Count | Description |
|----------|-------|-------------|
| **Resolve** | 1 | Scenario 6 `Validate()` TODO (Obsolete vs syncOnly) needs resolution before finalization |
| **Clarify** | 5 | What happens without `AddValidation()` (Scenario 4), sync event backward compat in Scenario 6, `modified pending` CSS classes (Scenario 9), form-submit validation failure behavior (Scenario 10), retry state transitions (Scenario 11) |
| **Improve** | 4 | Note `ValidationMessage` render model change (Scenario 7), state `ValidateFieldAsync` is new API (Scenario 11), add disposal to Scenario 12 code, add per-field integration note to Scenario 12 |
| **Add** | 2 | Missing references: #64892 (tracking epic), Blazored #31 |
| **Consider** | 2 | Non-enhanced SSR note (Scenario 13), missing assumption about sync handlers starting async work |
