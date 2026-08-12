# Blazor engineering and runtime behavior

Applies to `BEQ-*`.

## Consumer matrix

Use the smallest package-based applications that cover the maintainer's documented matrix:

- prerender and Static SSR;
- Interactive Server;
- Interactive WebAssembly or standalone WASM;
- Auto before and after interactivity, when claimed.

A build is not runtime proof. Confirm interactivity with state changes, callbacks, browser console,
and network evidence.

## Implementation review

- Public parameters, mutation, binding pairs, required parameters, docs, and compatibility policy.
- `EventCallback` invocation, awaiting, exception routing, renderer affinity, and re-rendering.
- Timers, subscriptions, cancellation, object references, modules, DOM listeners, and async cleanup.
- JS module ownership, initialization timing, identifiers, serialization, and DOM sinks.
- CSS isolation or the documented global-style contract.
- Analyzer and nullable posture.
- Generated output ownership: schema/generator, handwritten partial, or shared runtime.

## Lifecycle probes

Exercise initial render, parameter update, late child insertion, keyed reorder, detach/reattach,
navigation, disposal, callback failure, cancellation, and repeated initialization. Use deterministic
assertions against public behavior where possible.

## Dynamic child and selected-value matrix

For parent/child controls, option collections, or selected values, exercise:

1. value supplied before its matching child/item;
2. child/item registered before the value;
3. late matching child insertion;
4. selected child removal;
5. selected child becoming disabled;
6. keyed reorder;
7. form reset;
8. child replacement with the same and different value;
9. multiple related parameter changes in one render.

Assert public value, rendered selection, callback count/order, focus, and accessible selected state.
Combined parameter updates should reconcile atomically rather than transiently discarding state.

## Custom-element registration and upgrade

When JS-backed custom elements are involved, distinguish:

1. host/circuit connection;
2. static asset and module loading;
3. `customElements.get(name)` registration;
4. existing DOM element upgrade;
5. component initialization and first interactive callback.

Circuit connectivity does not prove custom-element registration or upgrade. Capture console and
network evidence so a missing asset/probe setup is not mislabeled as a component defect.

## Typed value round trips

Exercise both outbound callback serialization and inbound parameter conversion for strings,
numbers, enums, nullable values, arrays, and object values the component claims to support.
Success in one direction or in current source does not prove the released package's opposite
direction.

## Termination probes

For keyboard navigation over disabled/hidden items, include all-disabled and no-focusable-item
states. Assert the operation terminates and leaves focus/selection in a documented safe state.

## Scoring boundaries

- Prerender not throwing verifies `BEQ-04`; it does not prove useful Static SSR semantics in
  `BEQ-05`.
- Fire-and-forget callbacks or cleanup are defects when failures or resources escape host lifecycle
  handling.
- A shared loader or bridge is not a BEQ-17 defect by itself. Score a defect when exact source shows
  a broad/global callable surface that conflicts with the narrow module-scoped contract or causes a
  demonstrated collision, lifecycle, or trust-boundary failure. If only loader presence is known,
  use `not tested`.
- A shared-runtime defect should be reported at the owning layer while retaining the reviewed
  control as the demonstrated scope.
- Unsupported modes may be acceptable only when the limitation is explicit and safe.
- Do not generalize one control's success to generated siblings.
