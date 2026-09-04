import assert from "node:assert/strict";
import test from "node:test";

import {
  loadQueue,
  normalizeOptions,
  validateQueue,
} from "./queue.mjs";

test("normalizeOptions defaults to live Blazor data", () => {
  assert.deepEqual(normalizeOptions(), {
    source: "live",
    preset: "blazor",
  });
});

test("fixture execution preserves the skill classifications and display contract", async () => {
  const { options, queue } = await loadQueue({ source: "fixture", preset: "blazor" });
  const visibleItems = queue.items.filter((item) => item.shownInDigest);

  assert.equal(options.source, "fixture");
  assert.equal(queue.query.complete, true);
  assert.equal(queue.census.byBucket.ReviewNow, 3);
  assert.equal(queue.census.byBucket.NeedsRescue, 3);
  assert.equal(queue.census.byBucket.ReadyToMerge, 1);
  assert.equal(visibleItems.length, 7);
  assert.ok(visibleItems.every((item) => item.reasonCodes.length > 0));
  assert.ok(queue.items.every((item) =>
    item.reasonCodes.every((reasonCode) => queue.display.reasonCodes[reasonCode])));
});

test("validation accepts additive fields and additive reason codes with display metadata", async () => {
  const { queue } = await loadQueue({ source: "fixture", preset: "blazor" });
  const candidate = structuredClone(queue);
  candidate.futureField = { value: true };
  candidate.items[0].futureItemField = "value";
  candidate.items[0].reasonCodes.push("future-reason");
  candidate.display.reasonCodes["future-reason"] = {
    label: "Future reason",
    description: "A compatible additive reason.",
  };

  assert.equal(validateQueue(candidate), candidate);
});

test("validation rejects incomplete query results and missing reason metadata", async () => {
  const { queue } = await loadQueue({ source: "fixture", preset: "blazor" });
  const incomplete = structuredClone(queue);
  incomplete.query.complete = false;
  assert.throws(
    () => validateQueue(incomplete),
    (error) => error.code === "queue_incomplete",
  );

  const missingMetadata = structuredClone(queue);
  delete missingMetadata.display.reasonCodes[missingMetadata.items[0].reasonCodes[0]];
  assert.throws(
    () => validateQueue(missingMetadata),
    (error) => error.code === "queue_display_invalid",
  );
});
