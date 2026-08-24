# Trimming, native AOT, and performance

Applies to `TA-*` and `PERF-*`.

## Trimming and AOT

- Restore the exact package into a standalone consumer.
- Publish with trimming and trim analysis enabled.
- Record warnings attributable to the package separately from application/toolchain warnings.
- Load the published output in a browser and exercise the component.
- Inspect reflection, dynamic-code, serialization, and JS interop surfaces.
- Verify whether the package opts into trim analysis with `<IsTrimmable>true</IsTrimmable>` or an
  explicitly documented equivalent.
- Run native WASM AOT only when claimed or specifically requested; keep it separate from trimming.

Score configuration and runtime separately. A package can pass trimmed publish/browser probes while
still failing the explicit trim-analysis configuration requirement. Conversely, configuration alone
cannot verify runtime behavior, and a successful build without browser exercise cannot verify
`TA-03`.

## Performance

Choose a representative scenario before measuring:

- realistic item count, depth, template complexity, and interaction pattern;
- Interactive Server circuit count and state;
- WASM startup and bundle composition;
- serialization and server-to-browser payloads.

Collect comparative measurements for startup, render, interaction latency, allocations, payload
size, and retained state as applicable. Source inspection can identify risks but cannot verify a
budget.

## Scoring boundaries

- Concrete identity instability, avoidable repeated work, unbounded state, or demonstrated budget
  failure can be a `defect`.
- Missing targets, budgets, representative measurements, or private performance records normally
  require maintainer evidence.
- Use `not tested` when a relevant deterministic benchmark or runtime probe was not performed.
- Do not infer native AOT success from trimming success.
- Do not infer acceptable performance from virtualization source alone.
