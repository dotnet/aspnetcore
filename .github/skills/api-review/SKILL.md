---
name: api-review
description: >-
  Author and file a separate ASP.NET Core API review issue from an originating issue and an implementation pull request or commits. USE FOR writing API review issues, preparing api-ready-for-review proposals, filling API proposal templates, creating API review descriptions from issues and code changes, "fill out the API review", "prepare API review", or opening a pull request that changes public API. DO NOT USE FOR reviewing API design decisions, approving APIs, implementing APIs, or general code review.
---

# Author an API review issue

Create a separate ASP.NET Core API proposal issue from the originating issue and implementation changes.

## Inputs

Gather:

- **Originating issue** — the issue or feature request that prompted the API change, including all comments
- **Implementation pull request or commits** — the proposed implementation and its public API changes, especially changes to `PublicAPI.Unshipped.txt`

Present a checklist of the available inputs. If either input is missing, use the user-input tool to request it before drafting sections that depend on it.

## Workflow

1. **Gather the evidence.** Read the originating issue and all comments, then inspect the implementation pull request, commits, and diff. Account for every changed public or protected type, member, signature, default, or convention. *Artifact:* a checklist containing the originating issue and implementation source. *Check:* both sources are available and every public API change is represented.
2. **Draft the proposal.** Fill each section of [the issue body template](assets/issue-template.md) using [the section guidelines](references/section-guidelines.md):
   - Background and Motivation
   - Usage Examples
   - Proposed API
   - Alternative Designs, when available
   - Risks, when available

   Background and Motivation, Usage Examples, and Proposed API are required. Before writing `N/A` for Alternative Designs or Risks, ask the user one focused question for each optional section whose information is not present in the originating issue or implementation changes.
3. **Add source justifications.** At the end of the issue body, map every substantive claim to a quote from the originating issue or implementation changes:

   ```text
   <<CONTENT>>: "<<QUOTE FROM SOURCE>>"
   ```

4. **Check the draft.** Verify:
   - [ ] The background gives reviewers outside the feature area enough context
   - [ ] The complete API proposal is in ref-assembly diff format
   - [ ] The originating issue and implementation pull request or commits are linked
   - [ ] Larger changes include proportionate explanation
   - [ ] Usage examples demonstrate the intended consumption
   - [ ] Alternative designs are documented when available; otherwise the section is `N/A`
   - [ ] Risks and breaking changes are documented when available; otherwise the section is `N/A`
   - [ ] A champion is identified for the API review meeting
5. **File a separate issue.** Create an issue in `dotnet/aspnetcore` with:
   - A concise title prefixed with `[API Proposal]`
   - The completed issue body
   - The `api-suggestion` and `api-proposal` labels
   - The `Feature` issue type when available

   Use the native issue-creation tool when available so the user can confirm the operation. If issue creation is unavailable, provide the ready-to-file title, body, labels, and issue type, and state that the issue was not filed.
6. **Link the pull request.** Add the API proposal issue link to the implementation pull request without removing its existing description.

## Rules

- Do not invent information. Every statement must come from the originating issue or implementation changes. General C# and API knowledge may interpret evidence but must not create missing section content.
- If the evidence is insufficient for a required section, request the missing information. For Alternative Designs and Risks, ask the user one focused question per section; write `N/A` only when they confirm there is none.
- The Proposed API section must use ref-assembly diff format with complete namespaces and type declarations.
- `PublicAPI.Unshipped.txt` tracks compatibility but does not grant API approval.
- API review may happen before, during, or after implementation. Pull request readiness and merge do not require `api-approved`.
- The API proposal issue must have the `api-approved` label covering the final implemented API shape before the API is included in an RTM release.
- If implementation changes the proposed or previously approved shape, update the proposal and return it to API review before the API is included in an RTM release.
- Notify `@dotnet/aspnet-api-review` when applying `api-ready-for-review`. A representative must attend the API review meeting.

## Completion

The task is complete when a separate API proposal issue exists with the required title, body, labels, source links, and implementation pull request backlink. If a tool or missing input blocks creation, report the exact blocker and leave a complete ready-to-file draft.

## References

- **Section guidelines:** [references/section-guidelines.md](references/section-guidelines.md)
- **Issue body template:** [assets/issue-template.md](assets/issue-template.md)
