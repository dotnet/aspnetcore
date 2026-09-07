import assert from "node:assert/strict";
import test from "node:test";

import { HTML } from "./render.mjs";

test("renderer exposes the focused read-only action set", () => {
  assert.match(HTML, /Open PR/);
  assert.match(HTML, /Investigate rescue/);
  assert.match(HTML, /item\.bucket === "ReviewNow"/);
  assert.match(HTML, /Attention workspace/);
  assert.match(HTML, /selected-card/);
  assert.match(HTML, /row-button/);
  assert.match(HTML, /aria-pressed/);
  assert.doesNotMatch(HTML, /Refresh fixture/);
  assert.doesNotMatch(HTML, />Merge</);
  assert.doesNotMatch(HTML, />Close PR</);
});

test("renderer presents two primary lanes and secondary classifications", () => {
  assert.match(HTML, /snapshot\.primary\.reviewNow/);
  assert.match(HTML, /snapshot\.primary\.needsRescue/);
  assert.match(HTML, /Secondary classifications/);
  assert.match(HTML, /snapshot\.readyToMerge/);
  assert.match(HTML, /snapshot\.discussionVerification/);
  assert.match(HTML, /Verify discussion/);
  assert.doesNotMatch(HTML, /review-menu/);
});

test("renderer exposes the inbox areas and evidence disclosure", () => {
  assert.match(HTML, /Worth reviewing now/);
  assert.match(HTML, /Recently opened community PRs/);
  assert.match(HTML, /Community attention/);
  assert.match(HTML, /Unclassified/);
  assert.match(HTML, /snapshot\.inbox/);
  assert.match(HTML, /responseEvidence\.status/);
});

test("renderer exposes the review destination menu with exact labels", () => {
  assert.match(HTML, /Review in new session/);
  assert.match(HTML, /Review in this session/);
  assert.match(HTML, /Review in new session queued\./);
  assert.match(HTML, /Review in this session queued\./);
});

test("renderer uses one ordinary-review shortlist when inbox data is available", () => {
  assert.match(HTML, /const inboxAvailable = hasInboxData\(snapshot\)/);
  assert.match(HTML, /renderPrimaryLanes\(snapshot, inboxAvailable\)/);
  assert.match(HTML, /if \(!inboxAvailable\) \{\s*lanes\.unshift\(/);
  assert.match(HTML, /const verificationNumbers = new Set/);
  assert.match(HTML, /\.filter\(\(item\) => !verificationNumbers\.has\(item\.number\)\)/);
  assert.match(HTML, /elements\.inbox\.replaceChildren\(\)/);
});

test("renderer exposes cached freshness and action-withheld states", () => {
  assert.match(HTML, /Showing cached/);
  assert.match(HTML, /freshnessSuffix/);
  assert.match(HTML, /Action withheld/);
  assert.match(HTML, /Evidence is incomplete or ambiguous; no-response is not claimed\./);
});

test("renderer exposes the personal PR inbox without creating a follow-up lane", () => {
  assert.match(HTML, /My PR inbox/);
  assert.match(HTML, /snapshot\.personalInbox/);
  assert.match(HTML, /formatOptionalDate\(item\.updatedAt\)/);
  assert.match(HTML, /formatOptionalDate\(item\.createdAt, \(date\) => date\.toLocaleDateString\(\)\)/);
  assert.match(HTML, /formatOptionalDate\(comment\.createdAt\)/);
  assert.match(HTML, /Direct review request/);
  assert.match(HTML, /Changed since own review/);
  assert.match(HTML, /Reply in participated thread/);
  assert.match(HTML, /Personal signal only; no canvas action granted/);
  assert.match(HTML, /View full personal inventory/);
  assert.match(HTML, /All " \+ repository/);
  assert.match(HTML, /previewItems/);
  assert.match(HTML, /item\.number/);
  assert.match(HTML, /selectionKeyForInboxItem/);
  assert.doesNotMatch(HTML, /inventory\.open\s*=\s*true/);
  assert.doesNotMatch(HTML, /My followups/);
});

test("renderer keeps direct, team, notification, and coverage signals distinct", () => {
  assert.match(HTML, /Team request:/);
  assert.match(HTML, /Notification:/);
  assert.match(HTML, /Coverage:/);
  assert.match(HTML, /review history:/);
  assert.match(HTML, /notifications:/);
  assert.match(HTML, /Evidence is incomplete or ambiguous; no-response is not claimed\./);
  assert.match(HTML, /metrics\.apiCalls \?\? "unknown"/);
  assert.match(HTML, /metrics\.elapsedMs \?\? "unknown"/);
});

test("browser actions send only opaque item IDs and action kinds", () => {
  assert.match(HTML, /const payload = \{ itemId: itemId, kind: kind \};/);
  assert.match(HTML, /if \(destination\) \{\s*payload\.destination = destination;/);
  assert.match(HTML, /body: JSON\.stringify\(payload\),/);
  assert.doesNotMatch(HTML, /JSON\.stringify\(\{[^}]*title/);
});
