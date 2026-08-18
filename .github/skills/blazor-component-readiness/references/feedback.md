# Capturing feedback after a review

Do not depend on a product-specific slash command. A portable prompt plus a small observations
artifact works across Copilot CLI, other agents, and manual reviews.

## Run-observations artifact

Write `[component]-readiness-run-observations.md` with:

```markdown
# Readiness skill run observations

- **Skill/rubric version:**
- **Component and exact snapshot:**
- **Review mode and elapsed/timebox:**
- **Guidance that prevented a likely mistake:**
- **Guidance that was unclear, contradictory, or missing:**
- **Highest-cost clerical or investigation step:**
- **Evidence that was safely reusable:**
- **Rows/status boundaries that required judgment:**
- **Probe recipe worth generalizing:**
- **Did the scorecard annex change or materially qualify the decision?:**
- **Suggested skill change, if any:**
- **Evidence supporting that change:**
```

Exclude proprietary source, credentials, private URLs, and unrelated organization context.

## Portable mining prompt

Users can paste this after a run:

```text
Review this completed Blazor component-readiness session as workflow evidence, not as a new
component review. Read the readiness report, handoff, run-observations note, and available tool
transcript. Identify: (1) guidance that prevented an error, (2) repeated friction or ambiguity,
(3) any unsupported conclusion the skill encouraged, (4) useful probe recipes, and (5) proposed
skill changes. Separate one-off component facts from changes that generalize. Recommend a core
change only when supported by at least two independent runs or a public standard. For each proposed
change provide evidence, exact destination file, smallest edit, and a regression case. Do not
modify the skill or publish reviewed component evidence.
```

## Collection channel

Direct feedback to the current repository's issue tracker unless the user explicitly selects
another public destination.

Use a structured issue containing the fields above. Ask users to attach or paste the scrubbed
observations note, not a review containing private or proprietary evidence. Use labels such as
`guidance-gap`, `probe-recipe`, `status-ambiguity`, and `false-positive` when the repository
provides them.

Do not create an issue, upload an artifact, or share a session without explicit user approval.
First show the selected destination repository, issue title, and complete scrubbed body.

Do not request a full session link by default. A session can contain reviewed source, private URLs,
organization context, package locations, credentials, or unrelated conversation. A session link is
optional supplementary context only when the user explicitly requests it, confirms who can access
it, reviews what it contains, and approves including it. The issue must remain useful without the
session link.

## Issue-ready feedback

Suggested title:

```text
[Blazor component readiness] Feedback from a [complete/targeted] review
```

Suggested body:

```markdown
## Run context

- **Skill/rubric version:**
- **Review mode:**
- **Component type:** [generic description; omit confidential names]
- **Approximate elapsed time or timebox:**

## What helped

[Guidance that prevented an error or improved the decision.]

## Friction or ambiguity

[The highest-cost step, unclear guidance, status boundary, false positive, or false negative.]

## Reusable improvement

[A generalized probe recipe or smallest proposed skill change.]

## Supporting workflow evidence

[Scrubbed evidence showing why this generalizes. Do not include proprietary component evidence.]

## Regression idea

[A prompt and expected behavior that would catch the problem in the future.]
```

End a pilot review with one concise invitation to prepare this issue. Publication remains a
separate, user-approved action.
