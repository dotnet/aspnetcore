# Prior exact-snapshot report

The complete prior report covers `Example.Components.Blazor.Tree` 4.2.0 at repository commit
`aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa` and nupkg SHA-256
`bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb`.

Relevant scorecard rows:

| Requirement ID | Status | Evidence | Maintainer action | Reviewer follow-up |
|---|---|---|---|---|
| BEQ-02 | defect | The repository documentation reviewed at the exact source commit did not state supported render modes. | Publish a render-mode support statement. | Check supplied product documentation if the maintainer identifies it. |
| BEQ-12 | defect | Browser probe `callback-awaiting.json` showed that an incomplete callback task did not delay completion and its later exception was unobserved. | Await the callback task. | Re-run the retained callback probe after a fix. |
| A11Y-08 | verified | Browser probe `tree-keyboard.json` exercised the documented keyboard matrix and retained the computed accessibility-tree snapshot. | - | - |

The tracker summary contains this reviewer feedback:

| Area | Requirement IDs | Feedback after review |
|---|---|---|
| Render modes and callbacks | `BEQ-02`, `BEQ-12` | The supported render modes are documented at https://example.com/components/blazor/render-modes. Callback issue: https://github.com/example/components/issues/42. |

New supplied evidence:

- The named product documentation explicitly says every component in this package supports
  Interactive Server, Interactive WebAssembly, and Interactive Auto.
- The documentation is attributable to the same released product and was supplied by the
  maintainer.
- No new callback or accessibility evidence was supplied.
- The package, repository commit, component, rubric, runtime probes, and evidence ledgers are
  unchanged.
