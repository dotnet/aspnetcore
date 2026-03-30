# Design Notes — Interactive Mode Detection

Research findings from the spec drafting process, extracted for use in a future design document.

## Context

The C# service layer needs to detect whether a form is rendering in a static SSR context vs. an interactive context to decide whether to emit `data-val-*` attributes. This must work correctly for apps with islands of interactivity — a single page may contain both static SSR forms and interactive forms, and each must behave independently.

## Available signals for SSR vs. interactive detection

The Blazor component model provides several signals:

| Signal | SSR (static) | Interactive | Prerendering of interactive component |
|---|---|---|---|
| `ComponentBase.AssignedRenderMode` | `null` | `InteractiveServerRenderMode` / `InteractiveWebAssemblyRenderMode` / `InteractiveAutoRenderMode` | Same as interactive (the mode instance has `Prerender = true`) |
| `ComponentBase.RendererInfo.IsInteractive` | `false` | `true` | `false` (during prerender pass), `true` (after activation) |
| `EditForm`'s cascaded `FormMappingContext` | non-null (SSR form handling) | `null` | non-null (during prerender pass) |

## Key nuances for islands of interactivity

- Render modes do not cascade globally — they create **render mode boundaries**. An SSR page can contain `<EditForm @rendermode="InteractiveServer">`, and all children inside that boundary (including `InputText`, `ValidationMessage`, etc.) see `AssignedRenderMode = InteractiveServerRenderMode`.
- A component cannot have *both* a caller-specified and a class-level `@rendermode` — Blazor throws at runtime if both are set.
- During **prerendering** of an interactive component, `AssignedRenderMode` is already set to the interactive mode, but `RendererInfo.IsInteractive` is `false`. After the circuit/WebAssembly activates, `RendererInfo.IsInteractive` becomes `true` and the component re-renders.

## Recommended approach — C# layer (emit `data-val-*` only for SSR)

The check should go in `InputBase<T>.MergeClientValidationAttributes()` (or its caller), gating on whether the component is rendering in a static SSR context. The cleanest signal is:

```
Do NOT emit data-val-* if AssignedRenderMode is not null
```

This covers all interactive cases including prerendering: when an interactive component prerenders, it will soon be replaced by the live interactive version, so emitting `data-val-*` during the brief prerender pass would be wasteful and potentially conflicting. `AssignedRenderMode == null` precisely means "this component is purely static SSR, with no interactive activation coming."

This approach works correctly for islands:

- A static `<EditForm>` on an SSR page → `AssignedRenderMode` is `null` on `InputBase` children → `data-val-*` emitted ✓
- An `<EditForm @rendermode="InteractiveServer">` on the same page → `AssignedRenderMode` is `InteractiveServerRenderMode` on `InputBase` children → `data-val-*` NOT emitted ✓
- A global `@rendermode InteractiveServer` on a page → all components see the mode → no `data-val-*` emitted ✓

The same check should apply in `ValidationMessage<T>` and `ValidationSummary` to decide whether to render `data-valmsg-for` / `data-valmsg-summary` or the standard interactive rendering.

Existing precedent: `FocusOnNavigate` uses exactly this pattern — `if (AssignedRenderMode is not null) return;` to skip SSR-specific rendering in interactive mode.

## JS layer — no special detection needed

If the C# layer correctly gates `data-val-*` emission, the JS library naturally ignores interactive forms because they have no `data-val="true"` elements to discover. The JS scans for `[data-val="true"]` — if there are none, it does nothing. This is the simplest and most robust approach: the C# layer is the single point of control.

## Open question for design doc

Confirm `AssignedRenderMode is not null` as the gating condition, or discuss alternatives if prerendering of interactive components needs different treatment.
