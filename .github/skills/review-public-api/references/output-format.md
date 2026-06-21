# Output format

Produce exactly these parts, in order. Keep it concrete and skimmable: reviewers want the deltas, not prose.

## 1. Verdict

One line, one of:
- **Looks good as proposed**: no shape changes needed.
- **Changes recommended**: the API is welcome but the shape needs fixes (the common case).
- **Recommend not adding**: the surface isn't justified; see the need assessment for why and the alternative.

## 2. Need assessment (always)

Show your scenario model, then judge, even when the shape is fine:
- **Scenarios & environment:** the runtime environments this API runs in and whether it actually works there (e.g. server-rendered apps, server APIs, real-time apps, browser/client apps, mobile apps, console apps, or all of them; plus scaled-out, AOT/trimmed), and the **common vs rare** usage patterns (name the dominant one). Call out any environment where it silently doesn't work.
- **Commonality:** happy-path | extensibility | edge-case: how often the dominant scenario occurs.
- **Target audience:** end-app-developer | library-author | unjustified-extensibility: who it's for.

Then one sentence concluding whether the surface is justified. Precedence: an API that serves the **happy path for end-app developers** clears the bar easily; pure **extensibility for library authors** needs a demonstrated scenario; **unjustified/generic extensibility for an edge case** should be declined with a pointer to an existing mechanism or workaround.

**Carry the scenario model into the per-type review:** wherever a required input, default, or shape makes the **common** scenario harder in order to serve a **rare** one, or where the API is inert in a target environment, flag it as a change (and put the reason in that type's Why).

## 3. Per type / file: changes + diff

For **each** affected type (or ref-assembly file), emit:

1. A `### <Namespace.Type>` heading.
2. A bullet list of concrete changes: imperative, one per line (e.g. *"Move to namespace `Microsoft.AspNetCore.Http.Metadata`"*, *"Seal the class"*, *"Add `CancellationToken cancellationToken = default`"*, *"Return `IReadOnlyList<T>` instead of `List<T>`"*, *"Rename `EnableX` → `ServeX`"*). If nothing changes for this type, write *"No changes."*
3. A **Why**: one short paragraph of prose, in a human reviewer's voice, explaining the reasoning behind the changes for this type (see [Grounding the Why](#grounding-the-why) below).
4. A ```` ```diff ```` fence comparing the **existing repo API surface** (lines starting `-`, or none if the type is brand new) to the **recommended shape** (lines starting `+`). Show signatures only (ref-assembly style, no bodies). Unchanged context lines have no prefix.

### Grounding the Why

The **Why** is the most important part of the review: it's what a maintainer reads to decide. Write it as **prose, the way a human reviewer would explain it in the meeting**, not a restatement of the bullet or a citation dump. Every recommended change must be grounded in one of:
- a convention this repo enforces (name the principle in plain words, e.g. "we seal types by default and unseal later if a real extensibility scenario shows up"), or
- a concrete, checkable design consequence (a specific break, allocation, ambiguity, or pit-of-failure).

Two hard rules:
- **Don't invent advice you can't justify.** If you can't articulate a sound reason a reviewer would accept, drop the recommendation. A change with a hand-wavy or made-up rationale is worse than no change.
- **Check every recommendation against the proposal's own stated goals.** Never suggest something that defeats the author's explicit purpose (e.g. don't propose auto-generated/randomized values when the goal is stable, shareable, bookmarkable output). If a change trades off against a stated goal, say so in the Why and let the tradeoff be explicit.

When recommending *not adding*, still show the proposed type in the diff as all `-` (or note "no API added") and rely on the need assessment for the rationale.

## Worked example

> **Verdict:** Changes recommended **Need assessment** - Scenarios & environment: runs in server-rendered apps and server APIs that inspect inbound request headers (e.g. behind a proxy/load balancer); not applicable to browser/client or console apps. Common case: the header is read once per request; reading it repeatedly is rare. - Commonality: happy-path, most apps that read this header want the typed accessor. - Target audience: end-app-developer. - Justified: a common end-developer convenience with no current equivalent → add, with shape fixes. ### Microsoft.AspNetCore.Http.HeaderExtensions - Add `CancellationToken cancellationToken = default` to the async method and flow it through. - Return `IReadOnlyList<string>` instead of `List<string>` (read-only result). - Seal the class (no extensibility scenario). - Rename `GetForwarded` → `TryGetForwarded` and return `bool` (optional lookup). **Why:** These are the standard shapes this repo expects. New async public methods take a `CancellationToken` so callers can cancel I/O, and we flow it through rather than dropping it. The method returns a snapshot the caller shouldn't mutate, so `IReadOnlyList<string>` states that intent and avoids handing out a mutable `List`. There's no scenario for subclassing this helper, and we seal by default (we can always unseal later if one appears). Finally, a header may be absent, so the `Try*`+`bool` form models "might not be there" honestly instead of returning an empty list that hides the difference between unset and empty. ```diff namespace Microsoft.AspNetCore.Http; -public class HeaderExtensions +public sealed class HeaderExtensions { -    public List<string> GetForwarded(this HttpRequest request); +    public bool TryGetForwarded(this HttpRequest request, out IReadOnlyList<string> values); -    public Task<HeaderResult> ReadAsync(this HttpRequest request); +    public Task<HeaderResult> ReadAsync(this HttpRequest request, CancellationToken cancellationToken = default); } ```

If the verdict is **Looks good as proposed**, parts 1 and 2 still appear; part 3 is a single line per type: *"No changes."*
