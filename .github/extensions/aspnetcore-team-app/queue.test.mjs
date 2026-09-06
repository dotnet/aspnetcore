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

test("normalizeOptions accepts an explicit digest author exclusion", () => {
  assert.deepEqual(normalizeOptions({ excludeDigestAuthor: "PureWeen" }), {
    source: "live",
    preset: "blazor",
    excludeDigestAuthor: "PureWeen",
  });
  assert.throws(
    () => normalizeOptions({ excludeDigestAuthor: "@PureWeen" }),
    (error) => error.code === "invalid_excluded_author",
  );
});

test("normalizeOptions keeps an authenticated identity only as a cache scope", () => {
  assert.deepEqual(normalizeOptions({
    source: "live",
    preset: "blazor",
    identityScope: "alice.example",
  }), {
    source: "live",
    preset: "blazor",
    identityScope: "alice.example",
  });
  assert.throws(
    () => normalizeOptions({ identityScope: "alice@example.com" }),
    (error) => error.code === "invalid_identity_scope",
  );
});

test("fixture execution preserves the skill classifications and display contract", async () => {
  const { options, queue } = await loadQueue({ source: "fixture", preset: "blazor" });
  const visibleItems = queue.items.filter((item) => item.shownInDigest);

  assert.equal(options.source, "fixture");
  assert.equal(queue.query.complete, true);
  assert.equal(queue.census.byBucket.ReviewNow, 5);
  assert.equal(queue.census.byBucket.NeedsRescue, 3);
  assert.equal(queue.census.byBucket.ReadyToMerge, 0);
  assert.equal(visibleItems.length, 5);
  assert.equal(queue.inbox.recentCommunity.count, 3);
  assert.equal(queue.inbox.recentCommunity.inventory[0].number, 201);
  assert.equal(queue.inbox.unclassified.count, 1);
  assert.equal(queue.inbox.community.inventory.length, 7);
  assert.ok(visibleItems.every((item) => item.reasonCodes.length > 0));
  assert.ok(queue.items.every((item) =>
    item.reasonCodes.every((reasonCode) => queue.display.reasonCodes[reasonCode])));
  assert.ok(queue.items.every((item) =>
    item.digestExclusionReasons.every(
      (reasonCode) => queue.display.digestExclusionReasons[reasonCode],
    )));
});

test("fixture execution applies digest-only author exclusions", async () => {
  const { options, queue } = await loadQueue({
    source: "fixture",
    preset: "blazor",
    excludeDigestAuthor: "community-author",
  });
  const item = queue.items.find((candidate) => candidate.number === 201);

  assert.equal(options.excludeDigestAuthor, "community-author");
  assert.equal(item.bucket, "ReviewNow");
  assert.equal(item.shownInDigest, false);
  assert.deepEqual(item.digestExclusionReasons, ["excluded-author"]);
  assert.deepEqual(queue.filter.excludeDigestAuthors, ["community-author"]);
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

test("validation remains compatible with earlier 1.0.0 producers", async () => {
  const { queue } = await loadQueue({ source: "fixture", preset: "blazor" });
  const candidate = structuredClone(queue);
  delete candidate.display.digestExclusionReasons;
  delete candidate.filter.excludeDigestAuthors;
  for (const item of candidate.items) {
    delete item.headBranch;
    delete item.mergeStateStatus;
    delete item.digestRank;
    delete item.digestExclusionReasons;
    delete item.stackDepth;
    delete item.stackBlockedBy;
  }

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

  const duplicateRank = structuredClone(queue);
  const reviewNow = duplicateRank.items.filter(
    (item) => item.bucket === "ReviewNow" && item.shownInDigest,
  );
  reviewNow[1].digestRank = reviewNow[0].digestRank;
  assert.throws(
    () => validateQueue(duplicateRank),
    (error) => error.code === "queue_item_invalid",
  );
});
