import assert from "node:assert/strict";
import test from "node:test";

import { HTML } from "./render.mjs";

test("renderer inserts untrusted queue and report text through textContent", () => {
  assert.match(HTML, /element\.textContent =/);
  assert.doesNotMatch(HTML, /\.innerHTML|\.outerHTML|insertAdjacentHTML/);
  assert.match(HTML, /rel = "noopener noreferrer"/);
});

test("renderer refreshes the server-side queue when the area changes", () => {
  assert.match(HTML, /areaSelect\.addEventListener\("change", async \(\) =>/);
  assert.match(HTML, /await refreshQueue\(\);/);
  assert.match(HTML, /body: JSON\.stringify\(\{ area: areaSelect\.value \}\)/);
});

test("renderer surfaces asynchronous page and event errors", () => {
  assert.match(HTML, /function showError\(error\)/);
  assert.match(HTML, /events\.addEventListener\("state", async \(event\) => \{\n      try \{/);
  assert.match(HTML, /loadPage\(\);\n      \} catch \(error\) \{\n        showError\(error\);/);
  assert.match(HTML, /loadState\(\)\.catch\(showError\)/);
});

test("renderer preserves editor focus and selection across metadata rerenders", () => {
  assert.match(HTML, /function captureEditorState\(\)/);
  assert.match(HTML, /selectionStart: element\.selectionStart/);
  assert.match(HTML, /element\.focus\(\{ preventScroll: true \}\)/);
  assert.match(HTML, /element\.setSelectionRange\(/);
  assert.match(HTML, /const editorState = captureEditorState\(\);/);
  assert.match(HTML, /restoreEditorState\(editorState\);/);
});

test("renderer binds draft saves and discussion responses to issue selection and revision", () => {
  assert.match(HTML, /selectionGeneration: 0/);
  assert.match(HTML, /function cancelPendingDraftSave\(\)/);
  assert.match(HTML, /clearTimeout\(state\.localDraft\.timer\)/);
  assert.match(HTML, /itemId: issue\.id,\n            issueNumber: issue\.number,\n            generation: state\.selectionGeneration/);
  assert.match(HTML, /function isResponseForContext\(responseState, context, revisions\)/);
  assert.match(HTML, /state\.localDraft !== draftContext/);
  assert.match(HTML, /state\.pendingSelection\?\.itemId !== itemId/);
  assert.match(HTML, /isCurrentIssueIdentity\(discussionContext\)/);
});

test("renderer rejects late selection and event responses", () => {
  assert.match(HTML, /const generation = \+\+state\.selectionGeneration/);
  assert.match(HTML, /state\.selectionGeneration !== generation/);
  assert.match(HTML, /incomingIssueId !== state\.pendingSelection\.itemId/);
  assert.match(HTML, /incomingIssueId !== currentIssue\(\)\.id/);
  assert.match(HTML, /incomingRevision < currentRevision/);
});

test("renderer flushes the newest draft before preview and blocks stale publication", () => {
  assert.match(HTML, /async function saveDraftContext\(draftContext/);
  assert.match(HTML, /state\.localDraft !== draftContext\n        \|\| !isCurrentIssueContext\(draftContext\)/);
  assert.match(HTML, /text\(document\.getElementById\("status"\), "Saving the newest draft before preparing preview\.\.\."\)/);
  assert.match(HTML, /draft\.value !== workspace\.draft\.content/);
  assert.match(HTML, /const previewContext = \{\n          \.\.\.operation,\n          revision: currentDraftRevision\(\)/);
  assert.match(HTML, /saveDraftContext\(draftContext, null, false\)/);
  assert.match(HTML, /preview\.revision !== currentDraftRevision\(\)/);
  assert.match(HTML, /The draft changed after preview; publish was blocked/);
});

test("renderer flushes pending edits before undo and renders durable discussion history", () => {
  assert.match(HTML, /async function undoDraft\(issueNumber\)/);
  assert.match(HTML, /The draft changed before it could be saved; undo was blocked/);
  assert.match(HTML, /body: JSON\.stringify\(\{ revision: context\.revision \}\)/);
  assert.match(HTML, /isResponseForContext\(response, context, \[context\.revision \+ 1\]\)/);
  assert.match(HTML, /const discussionHistory = snapshot\.selectedWorkspace\.discussion \?\? \[\]/);
  assert.match(HTML, /state\.metadata = response\.state/);
});

test("renderer preserves a newer human edit after the discussion restoration save completes", () => {
  assert.match(HTML, /if \(state\.localDraft === humanDraft\) \{\n\s+state\.localDraft = null;\n\s+\} else \{/);
  assert.match(HTML, /const newerHumanDraft = state\.localDraft;/);
  assert.match(HTML, /revision: savedWorkspace\.revision/);
  assert.match(HTML, /saveDraftContext\(followupDraft, null, false\)/);
});

test("renderer distinguishes published text from later local draft changes", () => {
  assert.match(HTML, /publication\.publishedRevision === snapshot\.selectedWorkspace\.draft\.revision/);
  assert.match(HTML, /publication\.publishedHash === snapshot\.selectedWorkspace\.draft\.hash/);
  assert.match(HTML, /Published comment reflects an earlier draft; local changes are not published\./);
});
