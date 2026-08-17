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

For the stable ledger, canonical package ID and version come only from `<metadata><id>` and
`<metadata><version>` inside the exact digest-identified nupkg. Registration metadata is discovery
evidence, not canonical identity. Routed acquisition rejects nupkgs larger than 256 MiB before
hashing/ZIP inspection and independently stops nuspec expansion after 1 MiB.

Canonical repository identity requires a public DNS/IDN HTTPS host. IP literals, localhost,
single-label hosts, and `.local`/`.internal` authorities are rejected.

Serialized reports, evidence bundles, and validation receipts have a shared 64 MiB public-command
ceiling with length precheck plus growth detection. Ledger bundling enforces the same aggregate and
output ceiling. This is separate from the 256 MiB streamed nupkg input and 1 MiB nuspec expansion
limits.

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

Repository-wide evidence may be reused only for the same pinned repository SHA and exact package
ID/version/digest. For multiple controls sharing that exact identity:

1. build one immutable `repository` source ledger keyed by those exact identities;
2. classify only rubric-owned `repository-wide` rows once;
3. embed the complete source ledger in each later companion and select the required stable EV1 subset;
4. represent a changed observation as a new immutable record and use a compatible `supersedes` link
   when reviewer judgment supports it;
5. keep component-specific source and behavior evidence in an exact-component `component` ledger.

Never merge evidence across a different package version, digest, source SHA, or release candidate.
Shared evidence reduces clerical work; it does not turn one control's runtime result into evidence
for another control.

For source-only reviews, repository-wide rows remain available but the repository ledger subject is
bound to the exact component ID. It may be reused by a same-component follow-up at the same snapshot,
never by a sibling control.
