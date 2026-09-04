import assert from "node:assert/strict";
import test from "node:test";

import { HTML } from "./render.mjs";

test("renderer exposes the focused read-only action set", () => {
  assert.match(HTML, /Open PR/);
  assert.match(HTML, /Investigate rescue/);
  assert.match(HTML, /item\.bucket === "ReviewNow"/);
  assert.doesNotMatch(HTML, /Refresh fixture/);
  assert.doesNotMatch(HTML, />Merge</);
  assert.doesNotMatch(HTML, />Close PR</);
});

test("renderer presents two primary lanes and secondary classifications", () => {
  assert.match(HTML, /snapshot\.primary\.reviewNow/);
  assert.match(HTML, /snapshot\.primary\.needsRescue/);
  assert.match(HTML, /Secondary classifications/);
  assert.match(HTML, /snapshot\.readyToMerge/);
});

test("browser actions send only opaque item IDs and action kinds", () => {
  assert.match(HTML, /JSON\.stringify\(\{ itemId: itemId, kind: kind \}\)/);
  assert.doesNotMatch(HTML, /JSON\.stringify\(\{[^}]*title/);
});
