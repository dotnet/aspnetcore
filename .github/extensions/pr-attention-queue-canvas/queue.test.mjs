import assert from "node:assert/strict";
import test from "node:test";

import { loadQueue, normalizeOptions, summarizeQueue } from "./queue.mjs";

test("normalizeOptions defaults to the offline Blazor fixture", () => {
  assert.deepEqual(normalizeOptions(), {
    source: "fixture",
    preset: "blazor",
  });
});

test("fixture execution preserves classifications and evidence", async () => {
  const { options, queue } = await loadQueue();
  const summary = summarizeQueue(queue);

  assert.equal(options.source, "fixture");
  assert.equal(queue.query.complete, true);
  assert.equal(queue.census.byBucket.ReviewNow, 3);
  assert.equal(queue.census.byBucket.NeedsRescue, 2);
  assert.equal(queue.census.byBucket.ReadyToMerge, 1);
  assert.equal(summary.visibleItems.length, 6);
  assert.deepEqual(
    summary.visibleItems.map((item) => item.nextActor),
    [
      "human reviewer",
      "human reviewer",
      "human reviewer",
      "maintainer/triager",
      "maintainer/triager",
      "merger",
    ],
  );
  assert.ok(summary.visibleItems.every((item) => item.reasonCodes.length > 0));
});
