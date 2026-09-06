import assert from "node:assert/strict";
import test from "node:test";

import { HTML } from "./render.mjs";

test("renderer inserts untrusted queue and report text through textContent", () => {
  assert.match(HTML, /element\.textContent =/);
  assert.doesNotMatch(HTML, /\.innerHTML|\.outerHTML|insertAdjacentHTML/);
  assert.match(HTML, /rel = "noopener noreferrer"/);
});
