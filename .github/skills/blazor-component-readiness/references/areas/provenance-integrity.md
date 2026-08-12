# Licensing, package integrity, and provenance

Applies to `LP-*` and `PI-*`.

## Evidence to collect

- Download the exact published package and preserve its version, digest, nuspec, signatures, and
  assembly list.
- Map `RepositoryCommit` and public release metadata to a reachable source commit.
- Inspect every shipped managed assembly rather than one representative target framework.
- Distinguish strong-name identity, Authenticode signing, NuGet author signing, and repository
  countersigning. One does not prove another.
- Resolve direct and transitive NuGet/npm dependencies plus bundled JS, CSS, fonts, themes, and
  notices.
- Trace the release artifact through build, signing, SBOM generation, provenance, and publication.
  Compare final package digests at each boundary.
- Record whether evidence applies to the published package, a release candidate, or a separate
  rebuild.

## Minimum probes

1. Inspect nuspec metadata and package repository signature.
2. Enumerate DLLs for every target framework and verify strong-name state.
3. Verify Authenticode presence and signer identity using an appropriate platform. Record
   certificate-chain and revocation/current-validity evidence separately; request maintainer
   evidence when the review environment cannot establish it.
4. Compare published package digest with the digest named by SBOM/provenance.
5. Confirm the SBOM covers the package's actual dependency and bundled-asset inventory.

## Scoring boundaries

- A configured signing step is not evidence that `PI-01` through `PI-04` hold for the release.
- A valid workflow change can be a promising remediation while the current released package
  remains a `defect`.
- Private Authenticode, retention, or license-review records are `maintainer evidence required` when
  public inspection cannot establish them.
- A separately rebuilt package cannot verify `PI-10`.
- Missing package metadata is an artifact defect; missing private legal approval is a maintainer
  evidence request.

## Common traps

- Treating NuGet repository countersigning as author signing.
- Checking only the newest target framework's DLL.
- Hashing an unsigned package before a later signing mutation.
- Accepting a source-tree SBOM as the inventory of the final nupkg.
- Inferring dependency-license acceptability from the root repository license.
