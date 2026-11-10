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
    ['case-04', 'UNRESOLVED', 'unresolved'],
    ['case-06', 'UNRESOLVED', 'unresolved'],
  ])
  {
    assert.equal(score(name, review(section, confidence)), 1, name);
  }
  for (const name of ['case-01', 'case-03'])
  {
    assert.equal(score(name, review('DISCARDED', 'discarded', false)), 0, name);
  }
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
