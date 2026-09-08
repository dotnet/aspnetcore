import assert from "node:assert/strict";
import fs from "node:fs";
import test from "node:test";

import {
  createPersonalCacheKey,
  getPersonalDisplayModel,
  normalizePersonalInbox,
  orderPersonalItems,
  reuseConditionalNotificationRepresentation,
} from "./personal.mjs";

function card(number, signals = [], overrides = {}) {
  return {
    number,
    title: `Personal follow-up ${number}`,
    url: `https://github.com/dotnet/aspnetcore/pull/${number}`,
    author: "author",
    bucket: "ReviewNow",
    nextActor: "human reviewer",
    blockers: [],
    headSha: `head-${number}`,
    latestOwnReview: null,
    participatedOrMentioned: false,
    directRequest: signals.some((signal) => signal.kind === "direct-request"),
    signals,
    coverage: {
      discovery: { state: "assessed", detail: "bounded search" },
      notifications: { state: "assessed", detail: "notification feed" },
      ownReview: { state: "assessed", detail: "latest review" },
      reviewThreads: { state: "assessed", detail: "hydrated thread" },
    },
    digestVisible: true,
    digestRank: number,
    generalScope: "all-repo",
    generalDigestExclusionReasons: [],
    ...overrides,
  };
}

function personal(overrides = {}) {
  return {
    enabled: true,
    login: "PureWeen",
    scope: "all-repo",
    activeCount: 2,
    preview: [],
    inventory: [
      card(101, [
        {
          kind: "direct-request",
          eventAt: "2026-09-06T10:00:00Z",
          evidenceUrl: "https://github.com/dotnet/aspnetcore/pull/101#review-requested",
        },
        {
          kind: "changed-since-own-review",
          eventAt: "2026-09-05T10:00:00Z",
          baselineCommit: "review-101",
          currentHead: "head-101",
          evidenceUrl: "https://github.com/dotnet/aspnetcore/pull/101#review",
        },
        {
          kind: "review-thread-reply",
          eventAt: "2026-09-04T10:00:00Z",
          evidenceUrl: "https://github.com/dotnet/aspnetcore/pull/101#discussion_r1",
          responder: "author",
          threadId: "thread-101",
        },
      ], {
        latestOwnReview: {
          state: "COMMENTED",
          submittedAt: "2026-09-03T10:00:00Z",
          commitOid: "review-101",
          url: "https://github.com/dotnet/aspnetcore/pull/101#review",
        },
      }),
      card(102, [{
        kind: "follow-up-notification",
        eventAt: "2026-09-06T09:00:00Z",
        evidenceUrl: "https://github.com/dotnet/aspnetcore/pull/102#issuecomment-1",
        reason: "comment",
        unread: true,
      }]),
      card(103, [], { participatedOrMentioned: true, bucket: "WaitingOnAuthor" }),
      card(101, [{
        kind: "follow-up-notification",
        eventAt: "2026-09-01T09:00:00Z",
        evidenceUrl: "https://github.com/dotnet/aspnetcore/pull/101",
      }]),
    ],
    coverage: {
      state: "assessed",
      discovery: "Four bounded repository searches were unioned.",
      notifications: { state: "assessed", detail: "Repository notification access succeeded." },
      ownReview: "bounded",
      reviewThreads: "partial; only hydrated evidence is asserted",
    },
    ...overrides,
  };
}

test("normalization preserves distinct personal signals and changed-head OIDs", () => {
  const inbox = normalizePersonalInbox(personal(), {
    repository: "dotnet/aspnetcore",
    generatedAt: "2026-09-06T10:00:00Z",
    query: { openPullRequestCount: 346 },
    timing: { personalMs: 42 },
  });
  assert.deepEqual(inbox.items.map((item) => item.number), [101, 102, 103]);
  assert.equal(inbox.items[0].directRequests.length, 1);
  assert.equal(inbox.items[0].changedSinceOwnReview.status, "yes");
  assert.equal(inbox.items[0].changedSinceOwnReview.reviewCommitOid, "review-101");
  assert.equal(inbox.items[0].replyEvidence.status, "evidenced");
  assert.equal(inbox.items[0].replyEvidence.replies[0].author, "author");
  assert.equal(inbox.items[0].actionStatus.label, "Needs your review");
  assert.equal(inbox.items[1].actionStatus.label, "Needs attention");
  assert.equal(inbox.items[2].actionStatus.label, "No action currently needed");
  assert.equal(inbox.activeCount, 2);
  assert.deepEqual(inbox.previewItems.map((item) => item.number), [101, 102]);
  assert.equal(inbox.metrics.elapsedMs, 42);
});

test("personal action status ordering prioritizes review work above no-action items", () => {
  const inbox = normalizePersonalInbox({
    enabled: true,
    login: "PureWeen",
    scope: "all-repo",
    inventory: [
      card(101, [{
        kind: "direct-request",
        eventAt: "2026-09-06T10:00:00Z",
        evidenceUrl: "https://github.com/dotnet/aspnetcore/pull/101#review-requested",
      }]),
      card(102, [{
        kind: "review-thread-reply",
        eventAt: "2026-09-06T09:30:00Z",
        evidenceUrl: "https://github.com/dotnet/aspnetcore/pull/102#discussion_r102",
        responder: "reviewer",
        threadId: "thread-102",
      }], {
        participatedOrMentioned: true,
      }),
      card(103, [{
        kind: "follow-up-notification",
        eventAt: "2026-09-06T09:00:00Z",
        evidenceUrl: "https://github.com/dotnet/aspnetcore/pull/103#issuecomment-1",
        reason: "comment",
        unread: true,
      }]),
      card(104, [{
        kind: "changed-since-own-review",
        eventAt: "2026-09-05T10:00:00Z",
        baselineCommit: "review-104",
        currentHead: "head-104",
        evidenceUrl: "https://github.com/dotnet/aspnetcore/pull/104#pullrequestreview-104",
      }], {
        latestOwnReview: {
          state: "COMMENTED",
          submittedAt: "2026-09-04T10:00:00Z",
          commitOid: "review-104",
          url: "https://github.com/dotnet/aspnetcore/pull/104#pullrequestreview-104",
        },
      }),
      card(105, [], {
        participatedOrMentioned: true,
        bucket: "ReviewNow",
      }),
      card(106, [{
        kind: "direct-request",
        eventAt: "2026-09-06T08:00:00Z",
        evidenceUrl: "https://github.com/dotnet/aspnetcore/pull/106#review-requested",
      }], {
        bucket: "Draft",
        nextActor: "author",
      }),
      card(107, [{
        kind: "follow-up-notification",
        eventAt: "2026-09-06T07:00:00Z",
        evidenceUrl: "https://github.com/dotnet/aspnetcore/pull/107#issuecomment-1",
        reason: "comment",
        unread: true,
      }], {
        bucket: "WaitingOnCI",
        nextActor: "author/CI investigation",
      }),
    ],
    coverage: {
      state: "assessed",
      discovery: { state: "assessed", detail: "bounded search" },
      notifications: { state: "assessed", detail: "notification feed" },
      ownReview: { state: "assessed", detail: "latest review" },
      reviewThreads: { state: "assessed", detail: "hydrated thread" },
    },
  }, {
    repository: "dotnet/aspnetcore",
    generatedAt: "2026-09-06T10:00:00Z",
  });
  const display = getPersonalDisplayModel(inbox);

  assert.deepEqual(inbox.items.map((item) => item.number), [101, 102, 103, 104, 106, 107, 105]);
  assert.deepEqual(inbox.items.map((item) => item.actionStatus.label), [
    "Needs your review",
    "Reply or inspect discussion",
    "Needs attention",
    "New changes since your review",
    "Review request present — PR not ready",
    "Follow-up present — no action now",
    "No action currently needed",
  ]);
  assert.equal(inbox.activeCount, 4);
  assert.deepEqual(display.previewItems.map((item) => item.number), [101, 102, 103, 104]);
  assert.equal(display.inventoryItems.length, 7);
  assert.equal(display.inventoryItems[4].hasActionablePersonalSignal, false);
  assert.equal(display.inventoryItems[5].hasActionablePersonalSignal, false);
  assert.equal(display.inventoryItems[6].actionStatus.label, "No action currently needed");
});

test("ordering is deterministic and deduplicates one card per PR", () => {
  const inbox = normalizePersonalInbox(personal(), { repository: "dotnet/aspnetcore" });
  const reordered = orderPersonalItems([...inbox.items].reverse());
  assert.deepEqual(reordered.map((item) => item.number), [101, 102, 103]);
  assert.equal(new Set(inbox.items.map((item) => item.number)).size, inbox.items.length);
});

test("coverage distinguishes partial and unavailable evidence", () => {
  const partial = normalizePersonalInbox(personal({
    coverage: {
      state: "partial",
      discovery: { state: "partial", detail: "Search incomplete." },
      notifications: { state: "assessed", detail: "Available." },
      ownReview: { state: "partial", detail: "Commit OID unavailable." },
      reviewThreads: { state: "unassessed", detail: "Deferred." },
    },
  }), { repository: "dotnet/aspnetcore" });
  assert.equal(partial.coverage.overall, "partial");
  assert.equal(partial.coverage.pullRequests, "partial");
  assert.equal(partial.coverage.reviewThreads.state, "unassessed");

  const unavailable = normalizePersonalInbox(null, { repository: "dotnet/aspnetcore" });
  assert.equal(unavailable.coverage.overall, "unavailable");
  assert.equal(unavailable.items.length, 0);
});

test("conditional notification cache reuses the exact representation on 304", () => {
  const cache = {};
  const key = createPersonalCacheKey({
    identity: "PureWeen",
    repository: "dotnet/aspnetcore",
    query: "notifications?all=true",
  });
  const representation = reuseConditionalNotificationRepresentation({
    cache,
    key,
    response: { status: 200, body: [{ id: 1 }], etag: "abc" },
  });
  const reused = reuseConditionalNotificationRepresentation({
    cache,
    key,
    response: { status: 304 },
  });
  assert.strictEqual(reused, representation);
  assert.deepEqual(reused.body, [{ id: 1 }]);
  assert.throws(
    () => reuseConditionalNotificationRepresentation({
      cache,
      key: createPersonalCacheKey({ identity: "OtherUser", repository: "dotnet/aspnetcore" }),
      response: { status: 304 },
    }),
    /without a matching cached representation/,
  );
});

test("skill output normalizes from a clean process without repository cwd assumptions", async () => {
  const { execFile } = await import("node:child_process");
  const { promisify } = await import("node:util");
  const run = promisify(execFile);
  const modulePath = new URL("./personal.mjs", import.meta.url).pathname;
  const persistedPath = `/tmp/aspnetcore-personal-inbox-${process.pid}.json`;
  fs.writeFileSync(
    persistedPath,
    JSON.stringify({
      personal: personal(),
      queue: { repository: "dotnet/aspnetcore" },
    }),
  );
  try {
    const childScript = `
      import { readFile } from "node:fs/promises";
      import { normalizePersonalInbox } from ${JSON.stringify(modulePath)};
      const persisted = JSON.parse(await readFile(${JSON.stringify(persistedPath)}, "utf8"));
      const normalized = normalizePersonalInbox(persisted.personal, persisted.queue);
      console.log(JSON.stringify({
        identity: normalized.identity,
        membership: normalized.items.map((item) => item.number),
      }));
    `;
    const results = await Promise.all([
      run(process.execPath, ["--input-type=module", "-e", childScript], { cwd: "/tmp" }),
      run(process.execPath, ["--input-type=module", "-e", childScript], { cwd: "/tmp" }),
    ]);
    const normalizedResults = results.map((result) => JSON.parse(result.stdout.trim()));
    assert.deepEqual(normalizedResults[0], {
      identity: "PureWeen",
      membership: [101, 102, 103],
    });
    assert.deepEqual(normalizedResults[1], normalizedResults[0]);
  } finally {
    fs.rmSync(persistedPath, { force: true });
  }
});

test("fixture personal display keeps active preview separate from collapsed inventory", async () => {
  const { execFile } = await import("node:child_process");
  const { promisify } = await import("node:util");
  const run = promisify(execFile);
  const root = new URL("../../skills/pr-attention-queue/", import.meta.url);
  const scriptPath = new URL("scripts/Get-PRAttentionQueue.ps1", root).pathname;
  const fixturePath = new URL("tests/fixtures/inbox-pull-requests.json", root).pathname;
  const result = await run(
    "pwsh",
    [
      "-NoProfile",
      "-File",
      scriptPath,
      "-InputPath",
      fixturePath,
      "-PersonalLogin",
      "PureWeen",
      "-Now",
      "2026-09-05T12:00:00Z",
      "-OutputFormat",
      "Json",
    ],
    { cwd: "/tmp", maxBuffer: 8 * 1024 * 1024 },
  );
  const queue = JSON.parse(result.stdout);
  const inbox = normalizePersonalInbox(queue.personal, queue);
  const display = getPersonalDisplayModel(inbox);
  assert.equal(display.scopeLabel, "All dotnet/aspnetcore");
  assert.equal(display.activeCount, queue.personal.activeCount);
  assert.equal(display.previewItems.length, queue.personal.preview.length);
  assert.equal(display.inventoryItems.length, queue.personal.inventory.length);
  assert.equal(display.inventoryCollapsedByDefault, true);
  assert.ok(display.inventoryItems.length > display.previewItems.length);
  assert.deepEqual(display.previewItems.map((item) => item.number), [204, 203, 201]);
  assert.deepEqual(display.inventoryItems.map((item) => item.number), [204, 203, 201, 205]);
  assert.deepEqual(display.previewItems.map((item) => item.actionStatus.label), [
    "Reply or inspect discussion",
    "Needs attention",
    "New changes since your review",
  ]);
  assert.deepEqual(display.inventoryItems.map((item) => item.actionStatus.label), [
    "Reply or inspect discussion",
    "Needs attention",
    "New changes since your review",
    "No action currently needed",
  ]);
  assert.equal(display.inventoryItems[3].actionStatus.detail.includes("human reviewer"), true);
  assert.ok(display.previewItems.every((item) => item.hasPersonalSignal));
  assert.equal(inbox.metrics.pullRequestsScanned, 5);
  assert.equal(fs.existsSync(fixturePath), true);
});
