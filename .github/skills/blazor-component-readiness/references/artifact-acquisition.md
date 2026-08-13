# Released artifact acquisition

Use this protocol before selecting complete or targeted mode for a deliverable that appears to be
distributed as a NuGet package. Transport failure must not silently redefine a released package as
source-only.

## Ordered acquisition

1. Record the configured public package source and package ID/version.
2. Discover NuGet v3 endpoints from the source service index. Query registration to confirm the exact
   version, then retrieve the original nupkg from the advertised package-base-address/flat-container
   endpoint.
3. If v3 registration or package transport fails, try the source's NuGet v2 package endpoint. For
   nuget.org this is `https://www.nuget.org/api/v2/package/{id}/{version}`.
4. Record every attempted endpoint and outcome. Do not interpret a network, authentication, proxy,
   DNS, or tool failure as package absence.
5. Hash the original nupkg before extraction. Record package ID, exact version, endpoint, digest
   algorithm/value, retrieval time, and whether the package was listed.
6. Preserve inspection evidence outside the reviewed repository and remove disposable extracted
   files after the review.

A package-manager cache is acceptable only when its source and exact original bytes can be
established. A locally rebuilt package is not the released artifact.

## Mode decision

| Evidence state | Review consequence |
|---|---|
| Exact released nupkg obtained and source mapping is valid | Use complete mode. |
| Exact released nupkg obtained but source mapping is absent or invalid | Use complete mode and score mapping/public-artifact rows from direct evidence. |
| Package is publicly listed but exact bytes remain unavailable after v3 and v2 attempts | Use targeted mode. Include applicable package-identification rows when relevant and classify unavailable checks `not tested`, not `not applicable`. |
| No released/distributed package exists for the bounded deliverable | Use targeted source-only mode; release-only rows remain outside the named targeted set. |

Record the acquisition receipt and the reason for mode selection in the report. Do not switch mode
only because the first retrieval method failed.

## Complete-mode minimum checks

Before classifying package rows in complete mode:

1. inspect nuspec identity, license, repository, author, project, and source-commit metadata;
2. inventory packaged files and every target framework;
3. inspect every shipped managed assembly for strong-name identity;
4. inspect NuGet author/repository signatures separately;
5. inspect Authenticode identity when the platform supports it and distinguish presence, signer,
   chain, revocation, and current validity;
6. compare the exact package digest with public SBOM/provenance references;
7. confirm the public source commit/tag is reachable and corresponds to the package;
8. record bundled assets and direct/transitive dependency evidence.

Unavailable platform-specific verification remains an explicit evidence gap. Do not replace it with
workflow configuration.

## Shared exact-artifact ledger

For multiple controls sharing the same repository SHA and package ID/version/digest:

1. create one repository-wide evidence ledger keyed by those exact identities;
2. classify package/repository rows once;
3. import those ledger rows into later reports with the original provenance and recheck state;
4. explicitly supersede a shared classification only with stronger or newer exact-snapshot evidence;
5. keep component-specific source and behavior evidence in each control report.

Never merge evidence across a different package version, digest, source SHA, or release candidate.
Shared evidence reduces clerical work; it does not turn one control's runtime result into evidence
for another control.
