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

When the skill has a public repository, use a structured GitHub issue form containing the fields
above. Ask users to attach or paste the scrubbed observations note, not a review containing private
or proprietary evidence.
Use labels for `guidance-gap`, `probe-recipe`, `status-ambiguity`, and `false-positive`.
