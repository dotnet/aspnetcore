import assert from 'node:assert/strict';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';

const repositoryRoot = path.resolve(import.meta.dirname, '../../..');
const workflowSource = path.join(repositoryRoot, '.github/workflows/pull-request-review.md');
const workflowLock = path.join(repositoryRoot, '.github/workflows/pull-request-review.lock.yml');
const stepName = 'Reject incomplete or partial publication sets';

function extractScript(filename) {
  const lines = fs.readFileSync(filename, 'utf8').split('\n');
  const step = lines.findIndex(line => line.trim() === `- name: ${stepName}`);
  assert.notEqual(step, -1, `${filename} does not contain the publication preflight step.`);
  const marker = lines.findIndex((line, index) => index > step && line.trim() === 'script: |');
  assert.notEqual(marker, -1, `${filename} does not contain the publication preflight script.`);
  const markerIndent = lines[marker].search(/\S/);
  const firstBodyLine = lines.findIndex((line, index) =>
    index > marker && line.trim().length > 0 && line.search(/\S/) > markerIndent);
  assert.notEqual(firstBodyLine, -1, `${filename} has an empty publication preflight script.`);
  const bodyIndent = lines[firstBodyLine].search(/\S/);
  const body = [];
  for (let index = marker + 1; index < lines.length; index++) {
    const line = lines[index];
    if (line.trim().length > 0 && line.search(/\S/) <= markerIndent) {
      break;
    }
    body.push(line.length >= bodyIndent ? line.slice(bodyIndent) : '');
  }
  return body.join('\n').trimEnd();
}

function execute(script, input) {
  const temporaryRoot = fs.mkdtempSync(path.join(os.tmpdir(), 'review-publication-preflight-'));
  try {
    const artifact = path.join(temporaryRoot, 'review-publication-gate');
    fs.mkdirSync(artifact);
    fs.writeFileSync(path.join(artifact, 'agent_output.json'),
      input?.rawJson ?? JSON.stringify(input));
    const failures = [];
    const core = { setFailed: message => failures.push(message) };
    let error;
    try {
      Function('require', 'process', 'core', script)(
        moduleName => moduleName === 'fs' ? fs : moduleName === 'path' ? path : undefined,
        { env: { RUNNER_TEMP: temporaryRoot } },
        core);
    } catch (caught) {
      error = caught;
    }
    return { failures, error };
  } finally {
    fs.rmSync(temporaryRoot, { recursive: true, force: true });
  }
}

const comment = () => ({ type: 'create_pull_request_review_comment' });
const review = () => ({ type: 'submit_pull_request_review' });
const rawJson = value => ({ rawJson: value });
const stopped = (type, reason = 'The review could not complete.', status = 'INCOMPLETE') => ({
  items: [
    { type, reason },
    {
      type: 'add_comment',
      body: `Review not published (${status}): ${reason}\n\nNo partial findings were published.`,
    },
  ],
  errors: [],
});
const findings = count => ({
  items: [...Array.from({ length: count }, comment), review()],
  errors: [],
});

const cases = [
  ['one finding', findings(1), true],
  ['five findings', findings(5), true],
  ['clean noop without errors property', { items: [{ type: 'noop' }] }, true],
  ['clean noop with empty errors', { items: [{ type: 'noop' }], errors: [] }, true],
  ['report incomplete', stopped('report_incomplete', 'The review could not complete.', 'BLOCKED'), true],
  ['missing data', stopped('missing_data'), true],
  ['missing tool', stopped('missing_tool'), true],
  ['empty items without errors property', { items: [] }, false],
  ['empty items with empty errors', { items: [], errors: [] }, false],
  ['collector errors with otherwise valid item',
    { items: [{ type: 'noop' }], errors: ["Line 2: Unexpected output type 'unknown'"] }, false],
  ['collector errors with no retained items',
    { items: [], errors: ["Line 1: Unexpected output type 'unknown'"] }, false],
  ['unknown only', { items: [{ type: 'unknown' }], errors: [] }, false],
  ['unknown extra', { items: [{ type: 'noop' }, { type: 'unknown' }], errors: [] }, false],
  ['duplicate noop', { items: [{ type: 'noop' }, { type: 'noop' }], errors: [] }, false],
  ['duplicate reviews', { items: [comment(), review(), review()], errors: [] }, false],
  ['mixed outputs', { items: [{ type: 'noop' }, comment(), review()], errors: [] }, false],
  ['comment only', { items: [comment()], errors: [] }, false],
  ['review only', { items: [review()], errors: [] }, false],
  ['too many findings', findings(6), false],
  ['signal only', { items: [{ type: 'report_incomplete', reason: 'Reason' }], errors: [] }, false],
  ['status only', {
    items: [{
      type: 'add_comment',
      body: 'Review not published (BLOCKED): Reason\n\nNo partial findings were published.',
    }],
    errors: [],
  }, false],
  ['duplicate signal', {
    items: [
      { type: 'report_incomplete', reason: 'Reason' },
      { type: 'missing_data', reason: 'Reason' },
      {
        type: 'add_comment',
        body: 'Review not published (BLOCKED): Reason\n\nNo partial findings were published.',
      },
    ],
    errors: [],
  }, false],
  ['duplicate status', {
    items: [
      { type: 'report_incomplete', reason: 'Reason' },
      {
        type: 'add_comment',
        body: 'Review not published (BLOCKED): Reason\n\nNo partial findings were published.',
      },
      {
        type: 'add_comment',
        body: 'Review not published (BLOCKED): Reason\n\nNo partial findings were published.',
      },
    ],
    errors: [],
  }, false],
  ['reason mismatch', {
    items: [
      { type: 'report_incomplete', reason: 'First' },
      {
        type: 'add_comment',
        body: 'Review not published (BLOCKED): Second\n\nNo partial findings were published.',
      },
    ],
    errors: [],
  }, false],
  ['malformed reason', {
    items: [
      { type: 'report_incomplete', reason: 42 },
      {
        type: 'add_comment',
        body: 'Review not published (BLOCKED): 42\n\nNo partial findings were published.',
      },
    ],
    errors: [],
  }, false],
  ['empty reason', stopped('report_incomplete', ''), false],
  ['long reason', stopped('report_incomplete', 'x'.repeat(241)), false],
  ['multiline reason', stopped('report_incomplete', 'First\nSecond'), false],
  ['status trailing newline', {
    items: [
      { type: 'report_incomplete', reason: 'Reason' },
      {
        type: 'add_comment',
        body: 'Review not published (BLOCKED): Reason\n\nNo partial findings were published.\n',
      },
    ],
    errors: [],
  }, false],
  ['invalid json', rawJson('{'), false],
  ['null root', null, false],
  ['array root', [], false],
  ['string root', 'root', false],
  ['missing items', {}, false],
  ['null items', { items: null }, false],
  ['object items', { items: {} }, false],
  ['null item', { items: [null] }, false],
  ['array item', { items: [[]] }, false],
  ['string item', { items: ['noop'] }, false],
  ['missing item type', { items: [{}] }, false],
  ['null item type', { items: [{ type: null }] }, false],
  ['object item type', { items: [{ type: {} }] }, false],
  ['null errors', { items: [{ type: 'noop' }], errors: null }, false],
  ['object errors', { items: [{ type: 'noop' }], errors: {} }, false],
  ['string errors', { items: [{ type: 'noop' }], errors: 'error' }, false],
  ['nonstring error item', { items: [{ type: 'noop' }], errors: [null] }, false],
];

const mismatches = [];
for (const filename of [workflowSource, workflowLock]) {
  const script = extractScript(filename);
  for (const [name, input, accepted] of cases) {
    const result = execute(script, input);
    if (result.error !== undefined) {
      mismatches.push(`${path.basename(filename)} ${name} threw instead of failing through core.setFailed: ${result.error}`);
    } else if ((result.failures.length === 0) !== accepted) {
      mismatches.push(
        `${path.basename(filename)} ${name} was ${result.failures.length === 0 ? 'accepted' : 'rejected'}: ${result.failures.join('; ')}`);
    }
  }
}

assert.equal(extractScript(workflowSource), extractScript(workflowLock),
  'The source and compiled workflow publication preflight scripts differ.');
assert.deepEqual(mismatches, [], mismatches.join('\n'));

console.log(`Validated ${cases.length} publication preflight cases against source and compiled lock.`);
