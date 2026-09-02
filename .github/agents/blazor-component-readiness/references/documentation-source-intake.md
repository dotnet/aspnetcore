# Supplied documentation source intake

Use this protocol only for documentation or sample sources explicitly supplied by a maintainer,
reviewer, or user. It does not authorize obtaining any other source.

## Preserve the supplied evidence

For each supplied source, retain:

- source provider and source kind;
- requested URL and final URL;
- retrieval timestamp, result, and redirect count;
- retained snapshot path, byte length, and SHA-256;
- reviewed package ID/version and component;
- package, version, and component alignment, each recorded as `verified`, `unknown`, or `mismatch`.

A URL without retained content is navigation input, not evidence of what the page says. A content
digest commits to retained bytes; it does not authenticate the provider's claim that the source is
canonical or release-aligned. Do not infer alignment from a hostname, page title, or generic product
wording.

Use only the supplied source and retained snapshot. Do not fetch another URL, follow page links,
query an archive, or expand the evidence set during this protocol.

## Classify within the evidence boundary

- If no canonical source was supplied or inspected, the reviewer has not established presence or
  absence. Classify an applicable documentation row `not tested`, request the specific source, and
  do not create an absence defect.
- If supplied content materially addresses a row but exact package/version/component alignment is
  `unknown`, it can correct an unsupported absence claim but cannot verify the reviewed release.
  Use `maintainer evidence required` and name the missing alignment evidence.
- If supplied content makes a product-wide statement that logically covers the reviewed component
  (e.g., "all Product X components require Y" and the component belongs to Product X), treat
  component alignment as `verified` at product scope. Apply this consistently to every component
  in the same product reviewed under the same supplied source.
- Use `verified` only when the retained content directly satisfies the row and its package, version,
  and component alignment are all established.
- Use `defect` for absence only after the retained source was successfully inspected and its
  documented scope establishes that the required public content should be present there.
- Documentation proves only what it states. It does not establish runtime behavior, accessibility
  behavior, test execution, or site-wide completeness unless the retained evidence directly covers
  that separate claim.
