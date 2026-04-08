# Blazor Form Submit Interception & Input Change Handling

## 1. Interactive Forms (Server / WebAssembly)

`EventDelegator` (`src/Rendering/Events/EventDelegator.ts`) is the core mechanism.

### How submit is intercepted

- `submit` is listed in `nonBubblingEvents` (line 41) and `alwaysPreventDefaultEvents` (line 52).
- When a `<form>` has a Blazor `@onsubmit` handler, `EventInfoStore.addGlobalListener()` registers a **document-level capturing listener**:
  ```typescript
  // EventDelegator.ts, lines 297-300
  const useCapture = Object.prototype.hasOwnProperty.call(nonBubblingEvents, eventName);
  document.addEventListener(eventName, this.globalListener, useCapture);
  ```
- Because `submit` is in `alwaysPreventDefaultEvents`, any Blazor-handled submit **always calls `browserEvent.preventDefault()`** (EventDelegator.ts, lines 237-239). This prevents the browser's native form submission.
- The event is dispatched to .NET via `dispatchEvent()` → `interopMethods.invokeMethodAsync('DispatchEventAsync', ...)` in `WebRendererInteropMethods.ts`.
- Because it uses **capturing phase**, it runs **before** NavigationEnhancement's bubbling listener — and sets `defaultPrevented`, which NavigationEnhancement checks at the top of its handler.

### How input changes are handled

- `input` and `change` are both registered in `EventTypes.ts` (lines 72-74) with `parseChangeEvent` as the arg extractor.
- `change` is in `nonBubblingEvents` → captured at document level; `input` is NOT → uses bubbling.
- `parseChangeEvent()` (EventTypes.ts, lines 178-193) handles:
  - Time-based inputs → normalized ISO value
  - Multi-select → array of selected option values
  - Checkboxes → `element.checked` (boolean)
  - Everything else → `element.value` (string)
- `EventFieldInfo.ts` additionally extracts the field value for **two-way binding** (`@bind`), doing the reverse of `BrowserRenderer.tryApplySpecialProperty`.

**Key insight:** Blazor does **no debouncing** and **no client-side validation** on input events. Every `@onchange` / `@oninput` event is dispatched immediately to .NET. Validation logic (e.g., `EditForm`/`EditContext`) runs server-side or in WASM — never in this JS layer.

---

## 2. Static SSR Forms (No Enhanced Navigation)

Without `data-enhance` on the form AND without an interactive router, **none of the JS intercepts fire**:

- `EventDelegator` only registers listeners when interactive components with event handlers are rendered — no interactive component = no `submit` listener.
- `NavigationEnhancement.onDocumentSubmit()` checks `enhancedNavigationIsEnabledForForm(formElem)` which requires `data-enhance` or `data-enhance="true"` on the `<form>` — without it, the function returns early.
- Result: **The browser handles the form natively** — a standard full-page POST/GET.

---

## 3. Static SSR Forms WITH Enhanced Navigation

When a form has `data-enhance` (or `data-enhance="true"`), `onDocumentSubmit()` in `NavigationEnhancement.ts` (lines 134-203) takes over.

### Preconditions checked (lines 134-141)

```typescript
if (hasInteractiveRouter() || event.defaultPrevented) { return; }
// ↑ Yields to interactive forms (which already called preventDefault via capturing)
if (!enhancedNavigationIsEnabledForForm(formElem)) { return; }
```

### The flow

1. **Extract metadata** from the submitter button (overrides form attrs):
   - `formmethod` / `formaction` / `formenctype` / `formtarget` from `event.submitter`
   - Falls back to `<form>` element attributes

2. **`event.preventDefault()`** — stops native form submission

3. **Build FormData** from the form, including the submitter's name/value if present

4. **GET request path** (lines 178-185):
   - Converts FormData to `URLSearchParams` and appends to URL
   - Pushes a history entry (matches native `<form method=get>` behavior)
   - Calls `performEnhancedPageLoad()` with no request body

5. **POST request path** (lines 186-199):
   - `multipart/form-data` enctype → sends `FormData` as body (browser sets `Content-Type` with boundary)
   - `application/x-www-form-urlencoded` → sends `URLSearchParams.toString()` as body
   - Headers include `accept: text/html; blazor-enhanced-nav=on` so the server knows this is enhanced nav

6. **`performEnhancedPageLoad()`** (line 205+):
   - Sends `fetch()` with `mode: 'no-cors'` (prevents receiving cross-origin content)
   - Server responds with HTML + `ssr-framing` header for streaming boundary markers
   - Response is parsed, DOM is synchronized via `synchronizeDomContent()` (DOM merging)
   - Handles redirects (including POST-Redirect-GET pattern enforcement)
   - Streaming SSR updates arrive as subsequent chunks separated by boundary markers

**For input changes:** There is **no JS-level handling of individual input events** in SSR-enhanced forms. The form data is collected from the DOM at submit time. The user types into native HTML inputs with no interception until submit.

---

## 4. Event Priority / Ordering

```
  submit event fires
       │
       ▼
  ┌─────────────────────────────────┐
  │  EventDelegator (CAPTURING)     │  ← Interactive forms
  │  • Dispatches to .NET           │
  │  • Calls preventDefault()       │
  │  • Sets event.defaultPrevented  │
  └─────────────────────────────────┘
       │
       ▼
  ┌─────────────────────────────────┐
  │  Validation EventManager        │  ← Our code (CAPTURING)
  │  (CAPTURING)                    │
  │  • Checks data-val="true"       │
  │  • Prevents submit if invalid   │
  └─────────────────────────────────┘
       │
       ▼
  ┌─────────────────────────────────┐
  │  NavigationEnhancement          │  ← Enhanced SSR forms (BUBBLING)
  │  (BUBBLING - default)           │
  │  • Checks !defaultPrevented     │
  │  • Checks data-enhance          │
  │  • Submits via fetch            │
  └─────────────────────────────────┘
       │
       ▼
  Native browser submit (if nothing called preventDefault)
```

The key design contract: EventDelegator uses **capturing**, NavigationEnhancement uses **bubbling** (default `addEventListener`). This guarantees interactive forms always win.

The Validation EventManager also uses capturing (`true` flag on line 17), so it runs in the same phase as EventDelegator. Ordering between two capturing listeners on the same target (`document`) is determined by registration order — whichever calls `addEventListener` first runs first.

---

## 5. Key Files Reference

| File | Role |
|---|---|
| `src/Rendering/Events/EventDelegator.ts` | Document-level event delegation for interactive components |
| `src/Rendering/Events/EventTypes.ts` | Maps browser events to .NET EventArgs (parseChangeEvent, etc.) |
| `src/Rendering/Events/EventFieldInfo.ts` | Extracts field values for two-way binding reverse mapping |
| `src/Rendering/WebRendererInteropMethods.ts` | Dispatches events to .NET via `DispatchEventAsync` interop |
| `src/Services/NavigationEnhancement.ts` | Enhanced navigation form interception (fetch-based) |
| `src/Rendering/StreamingRendering.ts` | Streaming SSR response processing |
| `src/Validation/EventManager.ts` | Our validation submit interception (capturing phase) |
