import assert from "node:assert/strict";
import test from "node:test";

import {
  buildLiveOptions,
  dispatchResolvedAction,
  isAllowedPostRequest,
  loadCanvasData,
  parseRefreshRequest,
} from "./server.mjs";
import { parseActionRequest } from "./state.mjs";

test("action requests accept only opaque IDs and declared kinds", () => {
  assert.deepEqual(
    parseActionRequest({ itemId: "opaque-item-id-123", kind: "review" }),
    { itemId: "opaque-item-id-123", kind: "review", destination: "new-session" },
  );
  assert.deepEqual(
    parseActionRequest({
      itemId: "opaque-item-id-123",
      kind: "review",
      destination: "this-session",
    }),
    { itemId: "opaque-item-id-123", kind: "review", destination: "this-session" },
  );
  assert.throws(
    () => parseActionRequest({
      itemId: "opaque-item-id-123",
      kind: "review",
      number: 69040,
    }),
    (error) => error.code === "invalid_action",
  );
  assert.throws(
    () => parseActionRequest({
      itemId: "opaque-item-id-123",
      kind: "review",
      destination: null,
    }),
    (error) => error.code === "invalid_action",
  );
  assert.throws(
    () => parseActionRequest({
      itemId: "opaque-item-id-123",
      kind: "review",
      destination: "child-session",
    }),
    (error) => error.code === "invalid_action",
  );
  assert.throws(
    () => parseActionRequest({
      itemId: "opaque-item-id-123",
      kind: "open",
      destination: "this-session",
    }),
    (error) => error.code === "invalid_action",
  );
});

test("refresh requests accept only an optional preset", () => {
  assert.deepEqual(parseRefreshRequest({ preset: "blazor" }), {
    source: "live",
    preset: "blazor",
  });
  assert.deepEqual(parseRefreshRequest({}), {
    source: "live",
    preset: undefined,
  });
  assert.throws(
    () => parseRefreshRequest({ source: "fixture" }),
    (error) => error.code === "invalid_refresh",
  );
  assert.throws(
    () => parseRefreshRequest({ identityScope: "forged-user" }),
    (error) => error.code === "invalid_refresh",
  );
});

test("canvas open and refresh options preserve digest author exclusions", () => {
  assert.deepEqual(
    buildLiveOptions({ preset: "blazor", excludeDigestAuthor: "PureWeen" }, "blazor"),
    {
      source: "live",
      preset: "blazor",
      excludeDigestAuthor: "PureWeen",
    },
  );
  assert.deepEqual(buildLiveOptions({ excludeDigestAuthor: "PureWeen" }), {
    source: "live",
    preset: undefined,
    excludeDigestAuthor: "PureWeen",
  });
});

test("POST protection permits same-origin iframe requests and rejects cross-origin requests", () => {
  assert.equal(isAllowedPostRequest({
    headers: {
      host: "127.0.0.1:43123",
      origin: "http://127.0.0.1:43123",
      "sec-fetch-site": "same-origin",
    },
  }), true);
  assert.equal(isAllowedPostRequest({
    headers: {
      host: "127.0.0.1:43123",
      origin: "https://attacker.example",
      "sec-fetch-site": "cross-site",
    },
  }), false);
  assert.equal(isAllowedPostRequest({
    headers: {
      host: "attacker.example:43123",
      origin: "http://attacker.example:43123",
      "sec-fetch-site": "same-origin",
    },
  }), false);
  assert.equal(isAllowedPostRequest({ headers: {} }), false);
});

test("canvas data consumes skill-owned personal coverage without hiding the queue", async () => {
  const queueResult = {
    options: {
      source: "live",
      preset: "blazor",
      identityScope: "PureWeen",
    },
    queue: {
      repository: "dotnet/aspnetcore",
      generatedAt: "2026-09-06T15:00:00.000Z",
      personal: null,
    },
  };

  const unavailable = await loadCanvasData(queueResult.options, {
    loadQueueImpl: async () => queueResult,
  });
  assert.equal(unavailable.queue, queueResult.queue);
  assert.equal(unavailable.personalInbox.coverage.overall, "unavailable");

  const partialQueue = {
    ...queueResult,
    queue: {
      ...queueResult.queue,
      timing: { personalMs: 10 },
      personal: {
        enabled: true,
        login: "PureWeen",
        scope: "all-repo",
        inventory: [],
        coverage: {
          state: "partial",
          discovery: { state: "partial", detail: "Search incomplete." },
          notifications: { state: "assessed", detail: "Available." },
          ownReview: { state: "partial", detail: "Bounded." },
          reviewThreads: { state: "unassessed", detail: "Deferred." },
        },
      },
    },
  };
  const partial = await loadCanvasData(partialQueue.options, {
    loadQueueImpl: async () => partialQueue,
  });
  assert.equal(partial.personalInbox.coverage.overall, "partial");
  assert.equal(partial.personalInbox.metrics.elapsedMs, 10);
});

test("review dispatch routes new session and this session prompts distinctly", async () => {
  const reviewItem = {
    repository: "dotnet/aspnetcore",
    number: 69063,
    bucket: "ReviewNow",
    url: "https://github.com/dotnet/aspnetcore/pull/69063",
    headSha: "52d785e4885b4a320da7ceaa78672886aba868eb",
  };

  const sent = [];
  const newSession = await dispatchResolvedAction(
    { kind: "review", destination: "new-session", item: reviewItem },
    {
      agentSend: async (request) => {
        sent.push({ destination: "new-session", request });
        return { messageId: "message-new" };
      },
    },
  );
  const thisSession = await dispatchResolvedAction(
    { kind: "review", destination: "this-session", item: reviewItem },
    {
      agentSend: async (request) => {
        sent.push({ destination: "this-session", request });
        return { messageId: "message-this" };
      },
    },
  );

  assert.equal(newSession.messageId, "message-new");
  assert.equal(newSession.destination, "new-session");
  assert.equal(thisSession.messageId, "message-this");
  assert.equal(thisSession.destination, "this-session");
  assert.equal(sent.length, 2);
  assert.match(sent[0].request.prompt, /Open a NEW pull-request session/);
  assert.match(sent[0].request.prompt, /The current PR head SHA is 52d785e4885b4a320da7ceaa78672886aba868eb\./);
  assert.match(sent[1].request.prompt, /Review dotnet\/aspnetcore#69063 in this session \(https:\/\/github\.com\/dotnet\/aspnetcore\/pull\/69063\)\./);
  assert.match(sent[1].request.prompt, /do not open a child PR session/i);
  assert.doesNotMatch(sent[1].request.prompt, /open_pr_session/);
});
