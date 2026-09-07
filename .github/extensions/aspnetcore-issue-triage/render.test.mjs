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
