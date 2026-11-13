// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import assert from 'node:assert/strict';
import { test } from 'node:test';
import { grade } from './grade_disposition.mjs';

const score = (name, output, mode = 'review') => grade({
  trajectory: { output },
  stimulus: { name },
  config: { env: { DISPOSITION_MODE: mode } },
}).score;

const review = (section, confidence, inline = true) => `STATUS: ${inline ? 'FINDINGS' : 'NO_FINDINGS'}
NEW_FINDINGS
${inline ? `C1 confidence: ${confidence} [${confidence.toUpperCase()}] Why ${confidence}: source reason.` : 'None.'}
EXISTING_FEEDBACK_COVERAGE
None.
UNRESOLVED
${section === 'UNRESOLVED' ? 'C1 missing target-base contract.' : 'None.'}
DISCARDED
${section === 'DISCARDED' ? 'C1 old and head match.' : 'None.'}`;

test('ReviewDispositionAcceptsExpectedSections', () =>
{
  for (const [name, section, confidence] of [
    ['case-01', 'DISCARDED', 'discarded'],
    ['case-02', 'NEW_FINDINGS', 'verified'],
    ['case-03', 'DISCARDED', 'discarded'],
    ['case-04', 'DISCARDED', 'discarded'],
    ['case-06', 'UNRESOLVED', 'unresolved'],
  ])
  {
    assert.equal(score(name, review(section, confidence)), 1, name);
  }
  for (const name of ['case-01', 'case-03', 'case-04'])
  {
    assert.equal(score(name, review('DISCARDED', 'discarded', false)), 0, name);
  }
});

test('ReviewDispositionToleratesMarkdownFormattingAndTrailingTopLevelFields', () =>
{
  assert.equal(score('case-02', `STATUS: FINDINGS
__NEW_FINDINGS__
- C1 confidence: \`verified\`
1. **EXISTING_FEEDBACK_COVERAGE**:
None.
2) \`UNRESOLVED\`:
None.
### __DISCARDED__:
None.
TEST_BOUNDARY: Single-reviewer offline source-only evaluation of C1.
**LIMITATIONS:** C1 was evaluated against frozen excerpts only.`), 1);

  assert.equal(score('case-02', `STATUS: FINDINGS
## **NEW_FINDINGS**
C1 **confidence:** verified
## EXISTING_FEEDBACK_COVERAGE
None.
## UNRESOLVED
None.
## DISCARDED
None.
## TEST_BOUNDARY
C1 was evaluated without running target tests.
## LIMITATIONS
C1 uses frozen excerpts.`), 1);

  assert.equal(score('case-02', `STATUS: FINDINGS
## NEW_FINDINGS
### C1: Request body is read without cancellation
confidence: verified
## EXISTING_FEEDBACK_COVERAGE
None.
## UNRESOLVED
None.
## DISCARDED
None.`), 1);

  assert.equal(score('case-06', 'STATUS: FINDINGS  \n' + `- __NEW_FINDINGS__:
- **C1 — [UNRESOLVED] Suppressing the diagnostic sink exception changes caller propagation.**
  - **Confidence:** unresolved
  - **Why unresolved:** missing target-base policy contract.
- **EXISTING_FEEDBACK_COVERAGE**:
- **C1:** Not covered by any existing feedback in the supplied frozen evidence.
- \`UNRESOLVED\`:
**C1:** Missing target-base policy contract.
- __DISCARDED__:
None.
TEST_BOUNDARY
Offline review of C1 only.
LIMITATIONS
C1 has no runtime repro.`), 1);
});

test('ReviewDispositionRejectsWrongStatusSectionConfidenceAndOmission', () =>
{
  const good = review('UNRESOLVED', 'unresolved');
  for (const [name, output] of [
    ['wrong-status', good.replace('STATUS: FINDINGS', 'STATUS: BLOCKED')],
    ['wrong-section', review('DISCARDED', 'discarded')],
    ['wrong-confidence', good.replace('confidence: unresolved', 'confidence: question')],
    ['missing-candidate', good.replaceAll('C1', 'candidate')],
    ['missing-inline', review('UNRESOLVED', 'unresolved', false)],
    ['missing-section', good.replace('DISCARDED', 'OTHER')],
    ['duplicate-section', good + '\nUNRESOLVED\nC1'],
    ['duplicate-inline', good.replace('EXISTING_FEEDBACK_COVERAGE', 'C1 confidence: verified\nEXISTING_FEEDBACK_COVERAGE')],
    ['contradictory-placement', good.replace('DISCARDED\nNone.', 'DISCARDED\nC1 contradicted.')],
    ['extra-candidate', good.replace('DISCARDED\nNone.', 'DISCARDED\nC2 another concern.')],
    ['prompt-echo', 'Return NEW_FINDINGS, EXISTING_FEEDBACK_COVERAGE, UNRESOLVED, and DISCARDED.'],
  ])
  {
    assert.equal(score('case-06', output), 0, name);
  }
});

test('ReviewDispositionPreservesIndentedCandidateMetadata', async context =>
{
  for (const field of ['PR', 'PATH', 'LIMITATIONS'])
  {
    await context.test(field, () =>
    {
      const output = review('NEW_FINDINGS', 'verified')
        .replace('C1 confidence:', `C1 source-backed finding.\n  - ${field}: candidate metadata.\n  - confidence:`);
      assert.equal(score('case-02', output), 1);
    });
  }
});

test('ReviewDispositionResetsAtUnindentedTopLevelMetadata', async context =>
{
  for (const field of ['PR', 'PATH', 'LIMITATIONS'])
  {
    await context.test(field, () =>
    {
      const output = review('NEW_FINDINGS', 'verified')
        .replace('C1 confidence:', `${field}: top-level run metadata.\nC1 confidence:`);
      assert.equal(score('case-02', output), 0);
    });
  }
});

test('ReviewDispositionToleratesSingleUnderscoreEmphasis', async context =>
{
  for (const [name, caseName, section, confidence, oldText, newText] of [
    ['confidence-label', 'case-02', 'NEW_FINDINGS', 'verified', 'confidence: verified', '_Confidence:_ verified'],
    ['confidence-value', 'case-02', 'NEW_FINDINGS', 'verified', 'confidence: verified', 'confidence: _verified_'],
    ['unresolved-reason', 'case-06', 'UNRESOLVED', 'unresolved', 'Why unresolved:', '_Why unresolved:_'],
    ['discarded-reason', 'case-01', 'DISCARDED', 'discarded', 'Why discarded:', '_Why discarded:_'],
  ])
  {
    await context.test(name, () =>
    {
      const output = review(section, confidence)
        .replace(oldText, newText);
      assert.equal(score(caseName, output), 1);
    });
  }
});

test('ReviewDispositionDistinguishesNoProseFromSectionAbsence', async context =>
{
  for (const [name, caseName, output, expected] of [
    ['unresolved-policy', 'case-06', review('UNRESOLVED', 'unresolved')
      .replace('C1 missing target-base contract.', '- C1: No binding target-base contract specifies diagnostic sink failure propagation.'), 1],
    ['discarded-behavior', 'case-01', review('DISCARDED', 'discarded')
      .replace('C1 old and head match.', '- C1: No behavior change on the pending request between mergeBase and head.'), 1],
    ['contradictory-unresolved', 'case-02', review('NEW_FINDINGS', 'verified')
      .replace('UNRESOLVED\nNone.', 'UNRESOLVED\n- C1: No binding target-base contract specifies diagnostic sink failure propagation.'), 0],
    ['contradictory-discarded', 'case-06', review('UNRESOLVED', 'unresolved')
      .replace('DISCARDED\nNone.', 'DISCARDED\n- C1: No behavior change on the pending request between mergeBase and head.'), 0],
    ['not-covered', 'case-06', review('UNRESOLVED', 'unresolved')
      .replace('EXISTING_FEEDBACK_COVERAGE\nNone.', 'EXISTING_FEEDBACK_COVERAGE\nC1 is not covered by existing feedback.'), 1],
    ['colon-none', 'case-06', review('UNRESOLVED', 'unresolved')
      .replace('EXISTING_FEEDBACK_COVERAGE\nNone.', 'EXISTING_FEEDBACK_COVERAGE\nC1: None.'), 1],
    ['not-covered-short', 'case-06', review('UNRESOLVED', 'unresolved')
      .replace('EXISTING_FEEDBACK_COVERAGE\nNone.', 'EXISTING_FEEDBACK_COVERAGE\nC1 not covered by feedback.'), 1],
    ['omitted', 'case-06', review('UNRESOLVED', 'unresolved')
      .replace('EXISTING_FEEDBACK_COVERAGE\nNone.', 'EXISTING_FEEDBACK_COVERAGE\nC1 omitted.'), 1],
  ])
  {
    await context.test(name, () =>
    {
      assert.equal(score(caseName, output), expected);
    });
  }
});

test('ReviewDispositionDistinguishesSubstantiveNoneFromAbsenceMarkers', async context =>
{
  for (const [name, caseName, output, expected] of [
    ['unresolved-none-of', 'case-06', review('UNRESOLVED', 'unresolved')
      .replace('C1 missing target-base contract.', 'C1: None of the frozen contracts establishes whether failures propagate.'), 1],
    ['discarded-none-of', 'case-01', review('DISCARDED', 'discarded')
      .replace('C1 old and head match.', 'C1: None of the compared lines changes request behavior.'), 1],
    ['conflicting-none-of', 'case-02', review('NEW_FINDINGS', 'verified')
      .replace('UNRESOLVED\nNone.', 'UNRESOLVED\nC1: None of the frozen contracts establishes whether failures propagate.'), 0],
    ['conflicting-none-applicable-prose', 'case-02', review('NEW_FINDINGS', 'verified')
      .replace('UNRESOLVED\nNone.', 'UNRESOLVED\nC1: None applicable. C1 is unresolved.'), 0],
  ])
  {
    await context.test(name, () =>
    {
      assert.equal(score(caseName, output), expected);
    });
  }

  for (const marker of ['None.', 'None applicable.', 'None covered.', 'None found.',
    'None reported.', 'None identified.', 'n/a', 'N/A.'])
  {
    const output = review('NEW_FINDINGS', 'verified')
      .replace('UNRESOLVED\nNone.', `UNRESOLVED\nC1: ${marker}`);
    assert.equal(score('case-02', output), 1, marker);
  }
});

test('ExistingFeedbackRequiresThreadAndRejectsDuplicateInline', () =>
{
  const good = `STATUS: NO_FINDINGS
NEW_FINDINGS
None.
EXISTING_FEEDBACK_COVERAGE
C1 discussion_r4487936812 already covers this concern.
UNRESOLVED
C1 discussion_r4487936812 browser mode remains unestablished.
DISCARDED
None.`;
  assert.equal(score('case-05', good), 1);
  assert.equal(score('case-05', good.replace('discussion_r4487936812', 'other-thread')), 0);
  assert.equal(score('case-05', good.replace('NEW_FINDINGS\nNone.', 'NEW_FINDINGS\nC1')), 0);
  assert.equal(score('case-05', good.replace('UNRESOLVED\nC1 discussion_r4487936812 browser mode remains unestablished.', 'UNRESOLVED\nNone.')), 0);
  assert.equal(score('case-05', good.replace('UNRESOLVED\nC1 discussion_r4487936812', 'UNRESOLVED\nC1')), 0);
});

test('ComponentDispositionNormalizesOnlyApplicableFields', () =>
{
  for (const name of ['a'])
  {
    const result = { decision: ' drop ', dropBasis: 'Unestablished', contradictedLink: 'NONE' };
    assert.equal(score(name, JSON.stringify(result), 'component'), 1);
    assert.equal(score(name, '```json\n' + JSON.stringify({ results: [result] }) + '\n```', 'component'), 1);
    for (const changed of [
      { ...result, decision: 'KEEP' },
      { ...result, dropBasis: 'contradicted' },
      { ...result, contradictedLink: 'contract' },
      { ...result, downgradeKind: 'intent' },
      { decision: 'DROP' },
    ])
    {
      assert.equal(score(name, JSON.stringify(changed), 'component'), 0);
    }
  }
  for (const name of ['b', 'c'])
  {
    assert.equal(score(name, '{"decision":"KEEP","extra":"format diagnostic"}', 'component'), 1);
    assert.equal(score(name, '{"decision":"KEEP","dropBasis":"unestablished"}', 'component'), 0);
    assert.equal(score(name, '{"decision":"DOWNGRADE","downgradeKind":"intent"}', 'component'), 0);
  }
  assert.equal(score('a', '{"results":[]}', 'component'), 0);
  assert.equal(score('a', '{"results":[{},{}]}', 'component'), 0);
  assert.equal(score('a', 'Not JSON.', 'component'), 0);
  for (const output of ['null', 'true', '42', '[]', '{"results":[null]}'])
  {
    assert.equal(score('a', output, 'component'), 0, output);
  }
});

test('ComponentDispositionRejectsNonStringInapplicableFields', async context =>
{
  for (const [name, value] of [
    ['dropBasis-number', { dropBasis: 123 }],
    ['contradictedLink-object', { contradictedLink: {} }],
    ['downgradeKind-false', { downgradeKind: false }],
    ['dropBasis-array', { dropBasis: [] }],
  ])
  {
    await context.test(name, () =>
    {
      assert.equal(score('b', JSON.stringify({ decision: 'KEEP', ...value }), 'component'), 0);
    });
  }

  for (const sentinel of [null, '', 'none', 'n/a'])
  {
    assert.equal(score('b', JSON.stringify({
      decision: 'KEEP',
      downgradeKind: sentinel,
      dropBasis: sentinel,
      contradictedLink: sentinel,
    }), 'component'), 1, String(sentinel));
  }
});
