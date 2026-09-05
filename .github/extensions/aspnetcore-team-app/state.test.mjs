import assert from "node:assert/strict";
import test from "node:test";

import { loadQueue } from "./queue.mjs";
import { createQueueController } from "./state.mjs";

const fixture = await loadQueue({ source: "fixture", preset: "blazor" });

test("controller publishes an opaque, action-safe snapshot", async () => {
  let nextId = 0;
  const controller = createQueueController({
    initialOptions: fixture.options,
    load: async () => fixture,
    createId: () => `opaque-item-id-${++nextId}`,
    now: () => "2026-09-03T18:00:00.000Z",
  });

  await controller.initialize();
  const state = controller.getState();
  const items = [
    ...state.snapshot.primary.reviewNow,
    ...state.snapshot.primary.needsRescue,
    ...state.snapshot.readyToMerge,
    ...Object.values(state.snapshot.secondary).flat(),
  ];

  assert.equal(state.refresh.phase, "ready");
  assert.ok(items.length > 0);
  assert.equal(new Set(items.map((item) => item.id)).size, items.length);
  assert.ok(items.every((item) => !Object.hasOwn(item, "url")));

  const reviewItem = state.snapshot.primary.reviewNow[0];
  const action = controller.resolveAction({ itemId: reviewItem.id, kind: "review" });
  assert.equal(action.item.number, reviewItem.number);
  assert.match(action.item.url, /^https:\/\/github\.com\/dotnet\/aspnetcore\/pull\/\d+$/);
});

test("controller renders digest lanes by the engine-provided rank", async () => {
  const reversed = structuredClone(fixture);
  reversed.queue.items.reverse();
  const controller = createQueueController({
    initialOptions: reversed.options,
    load: async () => reversed,
  });

  await controller.initialize();
  const reviewNow = controller.getState().snapshot.primary.reviewNow;

  assert.deepEqual(
    reviewNow.map((item) => item.digestRank),
    reviewNow.map((_, index) => index + 1),
  );
});

test("controller separates discussion verification from ordinary review actions", async () => {
  const withDiscussionVerification = structuredClone(fixture);
  const candidate = withDiscussionVerification.queue.items.find(
    (item) => item.bucket === "ReviewNow" && item.shownInDigest,
  );
  candidate.shownInDigest = false;
  candidate.digestRank = null;
  candidate.digestExclusionReasons.push("discussion-verification-needed");
  candidate.shownInDiscussionVerification = true;
  candidate.discussionVerificationRank = 1;
  candidate.discussionAssessment = {
    state: "verification-needed",
    complete: true,
    signals: ["author-disposition-mentioned"],
    commentTotalCount: 1,
    commentEvidenceTruncated: false,
    comments: [{
      author: candidate.author,
      actor: "author",
      association: "CONTRIBUTOR",
      createdAt: "2026-09-02T18:00:00.000Z",
      kind: "disposition",
      excerpt: "I am happy to close this pull request.",
    }],
    threads: {
      totalCount: 1,
      returnedCount: 1,
      complete: true,
      unresolvedCount: 1,
      outdatedUnresolvedCount: 0,
    },
  };
  withDiscussionVerification.queue.display.digestExclusionReasons[
    "discussion-verification-needed"
  ] = {
    label: "Discussion verification needed",
    description: "Human interpretation is needed before review.",
  };
  withDiscussionVerification.queue.display.discussion = {
    states: {
      "verification-needed": {
        label: "Verify discussion",
        description: "Discussion requires human interpretation.",
      },
    },
    signals: {
      "author-disposition-mentioned": {
        label: "Author requested disposition",
        description: "The author asked whether to close the pull request.",
      },
    },
    commentKinds: {
      disposition: {
        label: "Disposition",
        description: "The comment raises whether work should continue.",
      },
    },
  };
  withDiscussionVerification.queue.discussion = {
    candidateLimit: 20,
    assessedCandidateCount: 3,
    verificationNeededCount: 1,
    unassessedReviewNowCount: 0,
  };
  withDiscussionVerification.queue.items
    .filter((item) => item.bucket === "ReviewNow" && item.shownInDigest)
    .sort((left, right) => left.digestRank - right.digestRank)
    .forEach((item, index) => {
      item.digestRank = index + 1;
    });

  const controller = createQueueController({
    initialOptions: withDiscussionVerification.options,
    load: async () => withDiscussionVerification,
  });
  await controller.initialize();
  const state = controller.getState();
  const verificationItem = state.snapshot.discussionVerification[0];

  assert.equal(verificationItem.number, candidate.number);
  assert.equal(verificationItem.discussion.state, "verification-needed");
  assert.equal(verificationItem.discussion.threads.unresolvedCount, 1);
  assert.equal(
    state.snapshot.primary.reviewNow.some((item) => item.number === candidate.number),
    false,
  );
  assert.throws(
    () => controller.resolveAction({ itemId: verificationItem.id, kind: "review" }),
    (error) => error.code === "action_not_allowed",
  );
});

test("refresh coalesces callers and atomically replaces the snapshot", async () => {
  let calls = 0;
  let release;
  const pending = new Promise((resolve) => {
    release = resolve;
  });
  const controller = createQueueController({
    initialOptions: fixture.options,
    load: async () => {
      calls += 1;
      if (calls === 1) {
        return fixture;
      }
      await pending;
      return fixture;
    },
  });

  await controller.initialize();
  const previousId = controller.getState().snapshot.primary.reviewNow[0].id;
  const first = controller.refresh();
  const second = controller.refresh();

  await Promise.resolve();
  assert.equal(calls, 2);
  assert.equal(controller.getState().refresh.phase, "refreshing");
  assert.equal(controller.getState().refresh.stale, true);
  assert.equal(controller.getState().snapshot.primary.reviewNow[0].id, previousId);

  release();
  await Promise.all([first, second]);
  assert.equal(calls, 2);
  assert.notEqual(controller.getState().snapshot.primary.reviewNow[0].id, previousId);
});

test("refresh rejects a different scope instead of returning the in-flight scope", async () => {
  let calls = 0;
  let release;
  const pending = new Promise((resolve) => {
    release = resolve;
  });
  const controller = createQueueController({
    initialOptions: fixture.options,
    load: async () => {
      calls += 1;
      if (calls === 1) {
        return fixture;
      }
      await pending;
      return fixture;
    },
  });

  await controller.initialize();
  const refresh = controller.refresh({ preset: "blazor" });
  assert.throws(
    () => controller.refresh({ preset: "all-repo" }),
    (error) => error.code === "refresh_in_progress",
  );
  release();
  await refresh;
});

test("failed refresh retains the previous snapshot and invalidates stale action IDs after success", async () => {
  let calls = 0;
  const controller = createQueueController({
    initialOptions: fixture.options,
    load: async () => {
      calls += 1;
      if (calls === 2) {
        throw new Error("simulated failure");
      }
      return fixture;
    },
  });

  await controller.initialize();
  const firstItem = controller.getState().snapshot.primary.reviewNow[0];
  await assert.rejects(() => controller.refresh(), /simulated failure/);
  assert.equal(controller.getState().refresh.phase, "error");
  assert.equal(controller.getState().refresh.stale, true);
  assert.equal(controller.getState().snapshot.primary.reviewNow[0].id, firstItem.id);

  await controller.refresh();
  assert.throws(
    () => controller.resolveAction({ itemId: firstItem.id, kind: "open" }),
    (error) => error.code === "stale_item",
  );
});

test("controller rejects actions that do not match the current bucket", async () => {
  const controller = createQueueController({
    initialOptions: fixture.options,
    load: async () => fixture,
  });
  await controller.initialize();

  const rescueItem = controller.getState().snapshot.primary.needsRescue[0];
  assert.throws(
    () => controller.resolveAction({ itemId: rescueItem.id, kind: "review" }),
    (error) => error.code === "action_not_allowed",
  );

  const reviewItem = controller.getState().snapshot.primary.reviewNow[0];
  assert.throws(
    () => controller.resolveAction({ itemId: reviewItem.id, kind: "investigate-rescue" }),
    (error) => error.code === "action_not_allowed",
  );
});
