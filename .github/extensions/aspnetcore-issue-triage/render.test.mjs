import assert from "node:assert/strict";
import test from "node:test";
import { HTML } from "./render.mjs";
test("renderer keeps tokenized queue paging and foreground handoff", () => {
  assert.match(HTML, /\/api\/investigate/);
  assert.match(HTML, /Investigate here/);
  assert.match(HTML, /Investigate in new child session/);
  assert.match(HTML, /investigate\("current"\)/);
  assert.match(HTML, /investigate\("new-child"\)/);
  assert.match(HTML, /handoff.destination/);
  assert.match(HTML, /Request sent to chat/);
  assert.match(HTML, /\/api\/issues\?offset=/);
  assert.doesNotMatch(HTML, /\/api\/publish|\/api\/draft|\/api\/discuss|Undo|Preview/);
});
