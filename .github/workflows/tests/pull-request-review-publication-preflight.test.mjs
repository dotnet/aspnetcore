import assert from 'node:assert/strict';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';

const repositoryRoot = path.resolve(import.meta.dirname, '../../..');
const workflowSource = path.join(repositoryRoot, '.github/workflows/pull-request-review.md');
const workflowLock = path.join(repositoryRoot, '.github/workflows/pull-request-review.lock.yml');
const stepName = 'Reject incomplete or partial publication sets';

function extractScript(filename) {
  const lines = fs.readFileSync(filename, 'utf8').split(/\r?\n/);
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
    const filename = path.join(artifact, 'agent_output.json');
    fs.writeFileSync(filename,
      input?.rawJson ?? JSON.stringify(input));
    const failures = [];
    const notices = [];
    const core = {
      setFailed: message => failures.push(message),
      notice: message => notices.push(message),
    };
    let error;
    try {
      Function('require', 'process', 'core', script)(
        moduleName => moduleName === 'fs' ? fs : moduleName === 'path' ? path : undefined,
        { env: { RUNNER_TEMP: temporaryRoot, GH_AW_AGENT_OUTPUT: filename } },
        core);
    } catch (caught) {
      error = caught;
    }
    const rawOutput = fs.readFileSync(filename, 'utf8');
    return {
      failures, error, notices, rawOutput,
      output: failures.length === 0 && error === undefined ? JSON.parse(rawOutput) : undefined,
    };
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
const findingWithBody = body => ({
  items: [{ ...comment(), body }, review()],
  errors: [],
});

const bodyCases = [
  ['unresolved finding unchanged', findingWithBody(
    '[UNRESOLVED] External behavior.\nConfidence: unresolved\nWhy unresolved: Missing version-bound evidence.'), true],
  ['discarded heading', findingWithBody('[DISCARDED] Pre-existing behavior.'), false],
  ['discarded reason', findingWithBody('**Why discarded:** Old and head match.'), false],
  ['discarded confidence', findingWithBody('**Confidence:** `discarded`'), false],
  ['discarded marker case insensitive', findingWithBody('[discarded] Pre-existing behavior.'), false],
  ['canonical discarded inline', findingWithBody(
    '- [DISCARDED] Pre-existing behavior.\n  - confidence: discarded\n  - Why discarded: Old and head match.'), false],
  ['styled severity-prefixed discarded claim', findingWithBody(
    '\n## **[P2] [DISCARDED] Pre-existing behavior.**'), false],
  ...[
    ['adjacent severity and discarded labels', '[P2][DISCARDED] Pre-existing behavior.', false],
    ['bold adjacent severity and discarded labels', '**[P2][DISCARDED] Pre-existing behavior.**', false],
    ['lowercase adjacent severity and discarded labels', '**[p2][discarded] Pre-existing behavior.**', false],
    ['reversed adjacent discarded and severity labels', '**[DISCARDED][P2] Pre-existing behavior.**', false],
    ['adjacent unresolved and severity labels', '**[P2][UNRESOLVED] A candidate.**\nConfidence: unresolved', true],
    ['quoted adjacent discarded and severity labels', '`[P2][DISCARDED]` is parsed incorrectly.\nConfidence: verified', true],
  ].map(([name, body, accepted]) => [name, findingWithBody(body), accepted]),
  ['numbered discarded claim', findingWithBody('1. **[DISCARDED] Pre-existing behavior.**'), false],
  ['bulleted discarded reason', findingWithBody(
    'A candidate.\n* **Why discarded:** Old and head match.'), false],
  ['bulleted discarded confidence', findingWithBody(
    'A candidate.\n+ **Confidence:** `discarded`'), false],
  ['verified label does not override discarded claim', findingWithBody(
    '[DISCARDED] Pre-existing behavior.\nConfidence: verified'), false],
  ['unresolved label does not override discarded reason', findingWithBody(
    '[UNRESOLVED] A candidate.\nWhy discarded: Old and head match.'), false],
  ['verified label does not override discarded confidence', findingWithBody(
    'A candidate.\nConfidence: verified\nConfidence: discarded'), false],
  ...['verified', 'unresolved'].flatMap(confidence => {
    const claim = confidence === 'unresolved' ? '[UNRESOLVED] Incorrect marker handling.' : 'Incorrect marker handling.';
    const prefix = `${claim}\nConfidence: ${confidence}\n`;
    return [
      [`${confidence} inline marker literals`, findingWithBody(
        `${prefix}The parser rejects \`[DISCARDED]\`, \`Why discarded:\`, and \`Confidence: discarded\` in valid findings.`), true],
      [`${confidence} backtick-fenced marker literals`, findingWithBody(
        `${prefix}\`\`\`text\n[DISCARDED] Example.\nWhy discarded: Example.\nConfidence: discarded\n\`\`\``), true],
      [`${confidence} tilde-fenced marker literals`, findingWithBody(
        `${prefix}~~~text\n[DISCARDED] Example.\nWhy discarded: Example.\nConfidence: discarded\n~~~`), true],
      [`${confidence} blockquoted marker literals`, findingWithBody(
        `${prefix}> [DISCARDED] Example.\n> Why discarded: Example.\n> Confidence: discarded`), true],
    ];
  }),
  ['quoted marker before real claim', findingWithBody(
    '> [DISCARDED] Example.\n\nA verified regression.\nConfidence: verified'), true],
  ['fenced marker before real claim', findingWithBody(
    '```\n[DISCARDED] Example.\n```\nA verified regression.\nConfidence: verified'), true],
  ['inline code label is not a field', findingWithBody(
    'A verified regression.\n`Why discarded:` is treated as metadata.\n`Confidence: discarded` is also mishandled.'), true],
  ['discarded marker in later prose is not claim', findingWithBody(
    'A verified regression.\n[DISCARDED] is an example of a parsed marker.'), true],
  ['confidence value must be discarded exactly', findingWithBody(
    'A verified regression.\nConfidence: discarded-value is parsed incorrectly.'), true],
  ['field labels cannot cross lines', findingWithBody(
    'A verified regression.\nWhy\ndiscarded: is parsed incorrectly.\nConfidence:\ndiscarded'), true],
  ['different fence does not close quoted example', findingWithBody(
    'A verified regression.\n````text\n```\nConfidence: discarded\n````'), true],
  ['inline code does not hide discarded field', findingWithBody(
    'A candidate.\n```quoted inline code```\nConfidence: discarded'), false],
  ['discarded claim with punctuation', findingWithBody('[DISCARDED]: Old behavior.'), false],
  ['discarded claim after quoted evidence', findingWithBody(
    '~~~text\nConfidence: verified\n~~~\n> A quote.\n**[DISCARDED] Pre-existing behavior.**'), false],
  ['discarded field after fenced evidence', findingWithBody(
    'A candidate.\n```\nConfidence: verified\n```\nConfidence: discarded'), false],
  ...[
    ['hyphen list fence', '- ```text\n  Confidence: discarded\n  ```'],
    ['ordered list fence', '1. ```text\n   Confidence: discarded\n   ```'],
    ['parenthesized ordered list fence', '12) ```text\n    Confidence: discarded\n    ```'],
    ['asterisk list fence', '* ```text\n  Confidence: discarded\n  ```'],
    ['plus list fence', '+ ```text\n  Confidence: discarded\n  ```'],
    ['nested list fence', '- Outer\n  - ```text\n    Confidence: discarded\n    ```'],
    ['nested ordered tilde fence', '1. Outer\n   + ~~~text\n     Confidence: discarded\n     ~~~'],
    ['sibling ordered list fence', '1. Context.\n2. ```text\n   Confidence: discarded\n   ```'],
    ['sibling hyphen list fence', '- Context.\n- ```text\n  Confidence: discarded\n  ```'],
    ['sibling asterisk list fence', '* Context.\n* ```text\n  Confidence: discarded\n  ```'],
    ['sibling plus list fence', '+ Context.\n+ ```text\n  Confidence: discarded\n  ```'],
    ['nested sibling ordered list fence', '1. Outer\n   1. Context.\n   2. ```text\n      Confidence: discarded\n      ```'],
    ['lazy sibling ordered list fence', '1. Context.\nlazy continuation\n2. ```text\n   Confidence: discarded\n   ```'],
    ['nested lazy sibling ordered list fence', '1. Outer\n   1. Context.\n   lazy continuation\n   2. ```text\n      Confidence: discarded\n      ```'],
    ['blockquote lazy sibling ordered list fence', '> 1. Context.\n> lazy continuation\n> 2. ```text\n>   Confidence: discarded\n>   ```'],
    ['blockquote list fence', '> - ```text\n>   Confidence: discarded\n>   ```'],
    ['list blockquote fence', '- > ```text\n  > Confidence: discarded\n  > ```'],
    ['nested blockquote list fence', '> - > 1. ```text\n>   >    Confidence: discarded\n>   >    ```'],
    ['tabbed list fence', '-\t```text\n\tConfidence: discarded\n\t```'],
    ['tabbed ordered list fence', '1.\t```text\n\tConfidence: discarded\n\t```'],
    ['tabbed nested list fence', '-\tOuter\n\t+\t~~~text\n\t\tConfidence: discarded\n\t\t~~~'],
    ['list fence ending with container', '- ```text\n  Confidence: discarded\n\nOutside the list.'],
    ['blockquote fence ending with container', '> ```text\n> Confidence: discarded\n\nOutside the quote.'],
    ['root indented code example', '    ```text\n    Confidence: discarded\n    ```'],
    ['tabbed indented code example', '\t```text\n\tConfidence: discarded\n\t```'],
    ['list indented code example', '- Example:\n\n      ```text\n      Confidence: discarded\n      ```'],
    ['plain indented code example', '    Confidence: discarded'],
    ...[0, 1, 2, 3].map(indent => [
      `list continuation fence with ${indent} extra spaces`,
      `- Example:\n\n${' '.repeat(2 + indent)}\`\`\`text\n  Confidence: discarded\n${' '.repeat(2 + indent)}\`\`\``,
    ]),
  ].flatMap(([name, example]) => [
    [`${name} ignores quoted discard`, findingWithBody(
      `A candidate.\n\n${example}\nConfidence: verified`), true],
    [`${name} keeps following discard visible`, findingWithBody(
      `A candidate.\n\n${example.replace('Confidence: discarded', 'Confidence: verified')}\nConfidence: discarded`), false],
  ]),
  ['four-space root indentation is not a fence', findingWithBody(
    'A candidate.\n    ```text\nConfidence: discarded\n    ```'), false],
  ['noninitial ordered marker cannot interrupt a paragraph', findingWithBody(
    'A candidate.\n2. ```text\n   Confidence: discarded\n   ```'), false],
  ['zero-padded initial ordered list interrupts a paragraph', findingWithBody(
    'A candidate.\n01. ```text\n    Confidence: discarded\n    ```\nConfidence: verified'), true],
  ['thematic break does not create list fence context', findingWithBody(
    'A candidate.\n- - -\n  ```text\n  Confidence: verified\n```\nConfidence: discarded'), false],
  ['four-space list content indentation is not a fence', findingWithBody(
    'A candidate.\n- Example:\n\n      ```text\n  Confidence: discarded\n      ```'), false],
  ['over-indented closing fence stays inside list example', findingWithBody(
    'A candidate.\n- ```text\n      ```\n  Confidence: discarded\n  ```\nConfidence: verified'), true],
  ['indented paragraph continuation is not code', findingWithBody(
    'A candidate.\n    Confidence: discarded'), false],
  ['indented list paragraph continuation is not code', findingWithBody(
    '- A candidate.\n      Confidence: discarded'), false],
  ['different ordered delimiter cannot continue a list paragraph', findingWithBody(
    'A candidate.\n\n1. Context.\n2) ```text\n   Confidence: discarded\n   ```\nConfidence: verified'), false],
  ['blank line ends lazy list continuation', findingWithBody(
    'A candidate.\n\n1. Context.\n\nOutside the list.\n2. ```text\n   Confidence: discarded\n   ```\nConfidence: verified'), false],
  ['unmatched nested blockquote does not hide discarded field', findingWithBody(
    '- Context:\n  > Quoted snippet\nConfidence: discarded'), false],
  ['nested blockquote before verified field remains publishable', findingWithBody(
    '- Context:\n  > [DISCARDED] Quoted\n\nConfidence: verified'), true],
  ['indented discarded heading before real claim', findingWithBody(
    '    [DISCARDED] Example.\n\nA verified regression.\nConfidence: verified'), true],
  ['real discarded claim after indented example', findingWithBody(
    '    Example.\n\n[DISCARDED] Pre-existing behavior.'), false],
];

const historicalMixed = {
  items: [
    {
      ...comment(),
      path: 'src/Components/test/E2ETest/ServerExecutionTests/AutoPauseEnhancedNavigationTests.cs',
      line: 135,
      side: 'RIGHT',
      body: '**[P3][UNRESOLVED] The cleanup-enabled row installs the wrong callback name**\n\n' +
        '`handler.dispose` is never called by the paused-circuit disposal path; production invokes ' +
        '`onPausedCircuitDisposed`, so the only `retirement.cleanup++` in this test is not connected to the callback under test.\n\n' +
        '**Why unresolved:** A version-bound Selenium/WebDriver contract or immutable runtime artifact is still needed to prove ' +
        '`ExecuteScript` executes this JavaScript unchanged and that the inactivity-disposal sequence reaches the assertion ' +
        'with `retirement.cleanup === 0`.\n\n' +
        '**Minimal repro:** Execute `PausedCircuitDisposal_DoesNotReportConnectionUp(true)`; the row installs `handler.dispose`, ' +
        'waits for circuit closure, and then expects `retirement.cleanup` to equal `1`.\n\n' +
        '```suggestion\nhandler.onPausedCircuitDisposed = () => retirement.cleanup++;\n```',
    },
    {
      ...comment(),
      path: 'src/Components/Web.JS/src/Boot.Server.Common.ts',
      line: 161,
      side: 'RIGHT',
      body: '**[P3][DISCARDED] Replacement startup is not blocked by this teardown await**\n\n' +
        '**Why discarded:** For the proposed sequence, `startCircuit()` returns the existing `circuitStarting` promise at ' +
        '`Boot.Server.Common.ts:139-140` while the first startup is pending, before execution can reach changed line 161. ' +
        'The base implementation likewise retained a rejected startup promise while awaiting cleanup, so the claimed ' +
        'old/new replacement-startup regression is not established.\n\n' +
        '**Minimal repro:** Start a circuit, invoke `startCircuit()` again while the first start remains pending, ' +
        'and compare merge-base with head. Both calls share the existing startup promise; no replacement start reaches this await.\n\n' +
        '**What goes wrong:** Nothing on this sequence; frozen source directly refutes the proposed causal path.\n\n' +
        '**Fix:** none',
    },
    {
      ...review(),
      event: 'COMMENT',
      body: 'Source-only review of frozen head `59cd5bddfcc4c34d2acd5e76a3c3ba369f1df49f`.\n\n' +
        '<details><summary>DISCARDED</summary>\nReplacement startup returns the existing startup promise.\n</details>',
    },
  ],
  errors: [],
};

const cases = [
  ['one finding', findings(1), true],
  ['five findings', findings(5), true],
  ...bodyCases.map(([name, input, retained]) => [name, input, true, retained ? [] : [0]]),
  ['mixed valid quoted findings', {
    items: [
      { ...comment(), body: 'The parser rejects `[DISCARDED]` in valid claims.\nConfidence: verified' },
      { ...comment(), body: '[UNRESOLVED] Another finding.\nConfidence: unresolved\nWhy unresolved: Missing external evidence.' },
      review(),
    ],
    errors: [],
  }, true],
  ['mixed real and discarded findings', {
    items: [
      { ...comment(), body: 'Real regression.\nConfidence: verified' },
      { ...comment(), body: '[DISCARDED] Pre-existing behavior.\nWhy discarded: Old and head match.' },
      review(),
    ],
    errors: [],
  }, true, [1]],
  ['run 43875900411 keeps unresolved r4586400680 and drops discarded r4586400709',
    historicalMixed, true, [1]],
  ['all discarded inline candidates become one no-findings noop', {
    items: [
      { ...comment(), body: '[DISCARDED] First old behavior.' },
      { ...comment(), body: 'Second old behavior.\nConfidence: discarded' },
      { ...review(), body: 'STATUS: FINDINGS\n<details><summary>DISCARDED</summary>\nBoth are old.\n</details>' },
    ],
    errors: [],
  }, true, [0, 1]],
  ['discarded audit summary with real finding', {
    items: [
      { ...comment(), body: 'Real regression.\nConfidence: verified' },
      { ...review(), body: '<details><summary>DISCARDED</summary>\n[DISCARDED] Old and head match.\n</details>' },
    ],
    errors: [],
  }, true],
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
  ['discarded item does not repair mixed noop and findings', {
    items: [{ type: 'noop' }, ...findingWithBody('[DISCARDED] Old behavior.').items],
    errors: [],
  }, false],
  ['discarded item does not repair missing review', {
    items: [{ ...comment(), body: '[DISCARDED] Old behavior.' }],
    errors: [],
  }, false],
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
  for (const [name, input, accepted, dropped = []] of cases) {
    const result = execute(script, input);
    if (result.error !== undefined) {
      mismatches.push(`${path.basename(filename)} ${name} threw instead of failing through core.setFailed: ${result.error}`);
    } else if ((result.failures.length === 0) !== accepted) {
      mismatches.push(
        `${path.basename(filename)} ${name} was ${result.failures.length === 0 ? 'accepted' : 'rejected'}: ${result.failures.join('; ')}`);
    } else if (accepted) {
      try {
        const remaining = input.items.filter((_, index) => !dropped.includes(index));
        if (dropped.length > 0 && !remaining.some(item => item.type === 'create_pull_request_review_comment')) {
          assert.equal(result.output.items.length, 1);
          assert.equal(result.output.items[0].type, 'noop');
          assert.match(result.output.items[0].message, /^STATUS: NO_FINDINGS\b/);
          assert.deepEqual(result.output, { ...input, items: result.output.items });
        } else {
          assert.deepEqual(result.output, { ...input, items: remaining });
        }
        assert.equal(result.notices.length, dropped.length > 0 ? 1 : 0);
      } catch (error) {
        mismatches.push(`${path.basename(filename)} ${name} published the wrong items: ${error.message}`);
      }
    } else {
      assert.equal(result.rawOutput, input?.rawJson ?? JSON.stringify(input),
        `${name} must not rewrite a rejected publication set.`);
    }
  }
}

assert.equal(extractScript(workflowSource), extractScript(workflowLock),
  'The source and compiled workflow publication preflight scripts differ.');
assert.deepEqual(mismatches, [], mismatches.join('\n'));

const compiledSafeOutputs = fs.readFileSync(workflowLock, 'utf8')
  .match(/^  safe_outputs:\r?\n([\s\S]*?)(?=^  [a-z_]+:)/m)?.[1];
assert.ok(compiledSafeOutputs, 'The compiled safe-outputs job must be extractable.');
const downloadIndex = compiledSafeOutputs.indexOf('name: Download agent output artifact');
const preflightIndex = compiledSafeOutputs.indexOf(`name: ${stepName}`);
const publishIndex = compiledSafeOutputs.indexOf('name: Process Safe Outputs');
assert.ok(downloadIndex >= 0 && downloadIndex < preflightIndex && preflightIndex < publishIndex,
  'Publication preflight must filter the downloaded payload inside safe outputs before processing.');
assert.equal(compiledSafeOutputs.split(
  'GH_AW_AGENT_OUTPUT: ${{ steps.setup-agent-output-env.outputs.GH_AW_AGENT_OUTPUT }}').length - 1, 2,
  'Preflight and the publisher must consume the same agent output file.');

console.log(`Validated ${cases.length} publication preflight cases against source and compiled lock.`);
