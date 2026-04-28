---
name: api-review
description: >
  Fill out API review issues for ASP.NET Core following the team's process and template.
  USE FOR: writing API review issues, preparing api-ready-for-review proposals, filling
  API proposal templates, creating API review descriptions from issues and commits,
  "fill out the API review", "prepare API review".
  DO NOT USE FOR: reviewing API design decisions, approving APIs, general code review.
---

# API Review Issue Writer

Fills out ASP.NET Core API review issues from an original issue and implementation commits, following the team's [API Review Process](../../../docs/APIReviewProcess.md).

## Prerequisites

Before starting, gather:

- **Original issue** — the GitHub issue or feature request that prompted the API change
- **Implementation commit(s)** — links to commits, especially `PublicAPI.Unshipped.txt` changes

**HARD STOP**: Present a checklist of gathered inputs to the user. If any item is missing, ask the user to provide it before proceeding. Do not draft sections that depend on missing evidence.

## Workflow

1. **Gather inputs** — Read the original issue and all implementation commits. Pay special attention to `PublicAPI.Unshipped.txt` files for API surface changes.

2. **Confirm completeness** — Show the user a checklist marking what you have vs. what's missing. Stop and ask if anything is missing.

3. **Fill sections** — Complete each section of the API review template following the [section guidelines](references/section-guidelines.md):
   - Background and Motivation
   - Proposed API (ref-assembly diff format)
   - Usage Examples
   - Alternative Designs
   - Risks

4. **Add source justifications** — At the end of the document, for **every piece of content** in each section, provide a justification mapping in this format:
   ```
   <<CONTENT>>: "<<QUOTE FROM SOURCE>>"
   ```

5. **Quality check** — Verify against the checklist:
   - [ ] Clear description for reviewers outside the feature area
   - [ ] Complete API specification in ref-assembly format
   - [ ] Links to implementation commits and `PublicAPI.Unshipped.txt`
   - [ ] Adequate context (larger changes need more explanation)
   - [ ] Realistic usage examples
   - [ ] Risk assessment with identified breaking changes
   - [ ] Champion identified for the API review meeting

## Output Shape

The final output must contain:

1. A prerequisite checklist (with status of each input)
2. The filled issue body using the [issue template](assets/issue-template.md)
3. A per-section source justification block with quote fragments

## ❌ Critical Anti-patterns

1. **Never invent information** — Every statement must come from the original issue or code changes. General C#/API knowledge may help *interpret* evidence but must not *create* section content absent source evidence.

2. **Never skip justifications** — The source mapping block at the end is mandatory. Every content claim needs a quote fragment from the original issue or code changes.

3. **Never guess when uncertain** — If there isn't enough context to fill a section, either ask the user for more information or write "N/A". Do not fill gaps with plausible-sounding content.

4. **Never skip the API diff format** — The Proposed API section must use ref-assembly diff format with `+` prefixed additions. Do not use plain prose to describe API changes.

5. **Never omit the confirmation step** — Always present the prerequisite checklist and get user confirmation before drafting sections.

## Process Notes

- **Label progression**: `api-suggestion` → `api-ready-for-review` → `api-approved`
- **Team notification**: Notify @asp-net-api-reviews when marking as `api-ready-for-review`
- **Meeting requirement**: A representative must attend the API review meeting
- **Implementation changes**: If the proposal changes during implementation, the review becomes obsolete and the process restarts

## References

- **Section guidelines**: [references/section-guidelines.md](references/section-guidelines.md) — detailed per-section guidance with examples
- **Issue template**: [assets/issue-template.md](assets/issue-template.md) — the markdown template for the issue body
- **Canonical repo template**: `.github/ISSUE_TEMPLATE/30_api_proposal.md` — the repo's live API proposal template (source of truth for template structure)
- **API Review Process**: `docs/APIReviewProcess.md` — full process documentation
- **API Review Principles**: `docs/APIReviewPrinciples.md` — team conventions and principles
- **Framework Design Guidelines**: https://github.com/dotnet/runtime/blob/master/docs/coding-guidelines/framework-design-guidelines-digest.md
