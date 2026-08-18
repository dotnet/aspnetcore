# Output filter solution-selection fixture

## Review goal

Select the preferred production correction from two materially different
mechanisms after both have comparable evidence.

## Accepted contract

Every output eligibility filter runs, while only the first formatter registered
for a field contributes to the serialized value. Field identity is
case-insensitive.

## Defect and common comparison contract

A newly introduced dual-role adapter is emitted once for each role and then
reclassified by the serializer. Frozen output contains the formatting suffix
twice and both role counters are two. The shared comparison matrix covers:

1. one dual-role adapter;
2. two dual-role adapters for one field;
3. a formatter before a dual-role adapter;
4. an omitted optional field;
5. a pure formatter control;
6. a plain eligibility control.

## Candidate A: typed handoff

The producer constructs separate eligibility and first-formatter collections and
calls a new internal serializer constructor. It retains the patch's specialized
adapter and a project exclusion for that adapter.

- Literal result: all six cases pass.
- Net production surface: two changed files, one new constructor, one new adapter,
  and one project exclusion relative to the pre-change base.
- Caller map: no public constructor changes.

## Candidate B: effective consumer classification

The producer restores the ordinary optional adapter. The serializer resolves a
direct formatter or the inner formatter from that adapter.

- Literal result: four of six cases pass; both ordering cases fail.
- Failure disposition: bounded. The accepted first-formatter contract supplies a
  local refinement at the point where effective formatters are classified.
- Refined result: all six shared cases pass.
- Supplementary result: all directly impacted serializer, producer, and adapter
  tests pass.
- Net production surface: one changed file; the specialized adapter, new
  constructor, and project exclusion are absent relative to the pre-change base.
- Caller map: all constructors are internal, and no repository caller depends on
  multiple formatters for one field.

## Proof limits

Both candidates have targeted evidence in one local configuration. Neither has
cross-platform or production-wide proof.
