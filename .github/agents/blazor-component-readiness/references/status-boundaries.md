# Status-boundary examples

Use these paired examples to keep classification stable across reviewers. The requirement wording
and exact evidence still control the final result.

Status tokens are exact and case-sensitive. Use `not applicable` exactly; `N/A`, capitalization
variants, and misspellings are invalid.

| Situation | Status | Why |
|---|---|---|
| No canonical documentation or sample source was supplied or inspected | `not tested` plus maintainer clarification | The evidence set cannot establish either presence or absence. |
| Supplied documentation contains relevant product guidance, but exact package/version/component alignment is unknown | `maintainer evidence required` | The content narrows an absence claim, but only the maintainer can bind it to the reviewed release or provide an aligned source. |
| Required public nuspec field is absent from the exact nupkg | `defect` | The released public artifact observably conflicts with the requirement. |
| Package is listed, but exact bytes remain unavailable after the acquisition protocol | `not tested` | The requirement applies, but transport prevented direct inspection. |
| Maintainer legal approval or retention record is private and unavailable | `maintainer evidence required` | The maintainer owns an inaccessible record. |
| No distributed package exists and release-only IDs are outside a targeted source-only review | Omit from targeted selection | Targeted mode makes no claim about the unselected release family. |
| Public SBOM is absent where the requirement requires publication | `defect` | A required public artifact is observably missing. |
| Private SBOM-generation audit or retention evidence is unavailable | `maintainer evidence required` | The control may exist, but only the maintainer can supply it. |
| Browser route or probe exists but was not run, or environment setup blocked it | `not tested` | An applicable reviewer-side behavior check remains unexecuted. |
| Optional behavior is explicitly unsupported, unclaimed, and safe | `not applicable` | There is no applicable supported surface. |
| Support claim is unknown | `not tested` plus maintainer clarification | Unknown is not the same as unsupported. |
| Source has plausible ARIA attributes | `verified` only for a source-structure requirement | Source can establish markup structure, not computed semantics or conformance. |
| Computed role/name/state was not inspected in a browser | `not tested` | Source structure does not prove the accessibility tree. |
| Formal WCAG or screen-reader record is private | `maintainer evidence required` | The maintainer owns the inaccessible assessment. |
| Package signature is directly verified | `verified` for that signature layer | It does not verify strong name, Authenticode, or another signature layer. |

Environmental blockers are evidence about the probe, not automatic product defects. Record the
failed prerequisite and smallest next probe.
