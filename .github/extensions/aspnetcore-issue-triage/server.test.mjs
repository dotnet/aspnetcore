import assert from "node:assert/strict";
import { Readable } from "node:stream";
import test from "node:test";

import {
  configureServer,
  isAllowedPostRequest,
  hasInstanceToken,
  parseItemRequest,
  parseDiscussionRequest,
  parseDraftRequest,
  parsePageRequest,
  parsePublicationRequest,
  parseRefreshRequest,
  parseRevisionRequest,
  readJsonBody,
  startInstance,
  stopInstance,
} from "./server.mjs";

test("refresh accepts only one supported public area", () => {
  assert.deepEqual(parseRefreshRequest({ area: "area-blazor" }), {
    area: "area-blazor",
  });
  assert.throws(
    () => parseRefreshRequest({ area: "area-runtime" }),
    (error) => error.code === "invalid_area",
  );
  assert.throws(
    () => parseRefreshRequest({ area: "area-blazor", source: "fixture" }),
    (error) => error.code === "invalid_refresh",
  );
});

test("selection and investigation requests accept only opaque item IDs", () => {
  assert.deepEqual(parseItemRequest({ itemId: "opaque-item-id-123" }), {
    itemId: "opaque-item-id-123",
  });
  assert.throws(
    () => parseItemRequest({ itemId: "opaque-item-id-123", number: 123 }),
    (error) => error.code === "invalid_selection",
  );
  assert.throws(
    () => parseItemRequest({ itemId: "../issues/123" }),
    (error) => error.code === "invalid_selection",
  );
});

test("page requests are explicit and bounded", () => {
  assert.deepEqual(
    parsePageRequest(new URLSearchParams("offset=50&limit=100")),
    { offset: 50, limit: 100 },
  );
  assert.deepEqual(
    parsePageRequest(new URLSearchParams("offset=0&limit=25&token=fixture")),
    { offset: 0, limit: 25 },
  );
  assert.throws(
    () => parsePageRequest(new URLSearchParams("offset=0&limit=1000")),
    (error) => error.code === "invalid_page",
  );
  assert.throws(
    () => parsePageRequest(new URLSearchParams("offset=0&limit=50&url=https://attacker.example")),
    (error) => error.code === "invalid_page",
  );
});

test("POST protection permits loopback same-origin and rejects forged origins", () => {
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
});

test("every HTTP route requires an unguessable per-instance capability token", () => {
  const token = "a".repeat(43);
  assert.equal(
    hasInstanceToken(new URL(`http://127.0.0.1:43123/?token=${token}`), token),
    true,
  );
  assert.equal(
    hasInstanceToken(new URL("http://127.0.0.1:43123/"), token),
    false,
  );
  assert.equal(
    hasInstanceToken(new URL("http://127.0.0.1:43123/?token=wrong"), token),
    false,
  );
});

test("request bodies reject oversized and invalid JSON payloads", async () => {
  await assert.rejects(
    readJsonBody(Readable.from([JSON.stringify({ value: "x".repeat(5_000) })])),
    (error) => error.code === "request_too_large",
  );
  await assert.rejects(
    readJsonBody(Readable.from(["{not-json"])),
    (error) => error.code === "invalid_json",
  );
});

test("draft, discussion, preview, and publish payloads are bounded and exact", () => {
  assert.deepEqual(
    parseDraftRequest({ content: "draft", issueNumber: 123, provenance: "human", revision: 1 }),
    { content: "draft", issueNumber: 123, provenance: "human", revision: 1 },
  );
  assert.throws(
    () => parseDraftRequest({
      content: "draft",
      issueNumber: 123,
      provenance: "human",
      revision: 0,
      extra: true,
    }),
    (error) => error.code === "invalid_draft",
  );
  assert.deepEqual(
    parseDiscussionRequest({ issueNumber: 123, question: "Check this", revision: 1 }),
    { issueNumber: 123, question: "Check this", revision: 1 },
  );
  assert.throws(
    () => parseDiscussionRequest({ issueNumber: 123, question: "q".repeat(4_001), revision: 1 }),
    (error) => error.code === "invalid_discussion",
  );
  assert.deepEqual(parseRevisionRequest({ revision: 1 }), { revision: 1 });
  assert.deepEqual(
    parsePublicationRequest({ confirmation: "token", revision: 1 }),
    { confirmation: "token", revision: 1 },
  );
});

test("local draft, discussion, and preview endpoints do not publish to GitHub", async () => {
  const issue = {
    id: "issue-endpoint-fixture-123",
    repository: "dotnet/aspnetcore",
    number: 123,
    title: "Fixture issue",
    url: "https://github.com/dotnet/aspnetcore/issues/123",
    createdAt: "2026-09-06T20:00:00Z",
    author: "fixture",
    labels: ["area-blazor"],
    area: "area-blazor",
    whyIncluded: ["fixture"],
  };
  const report = {
    repository: issue.repository,
    issueNumber: issue.number,
    issueUrl: issue.url,
    area: issue.area,
    createdAt: "2026-09-06T20:01:00Z",
    model: "fixture-model",
    messageId: "fixture-message",
    skill: "fixture-skill",
    skillDigest: "a".repeat(64),
    content: "fixture report",
  };
  let workspace = {
    issueNumber: issue.number,
    draft: {
      content: "fixture draft",
      previousContent: "fixture draft",
      revision: 1,
    },
    discussion: [],
    publication: {},
    stopPath: false,
  };
  let writes = 0;
  configureServer({
    load: async ({ area }) => ({
      schemaVersion: "1.0.0",
      repository: "dotnet/aspnetcore",
      area,
      areaOptions: [],
      generatedAt: "2026-09-06T20:00:00Z",
      predicate: `label:${area}`,
      coverage: {
        complete: true,
        sourceOpenAreaCount: 1,
        retrievedOpenAreaCount: 1,
        qualifyingCount: 1,
        uncertainMembershipCount: 0,
        pages: 1,
        limitation: null,
        warnings: [],
        rateLimit: null,
        searchCapApplied: false,
        retrieval: "fixture",
      },
      issues: [issue],
    }),
    store: {
      read: async () => report,
      readArea: async () => "area-blazor",
      write: async (value) => value,
      writeArea: async (area) => area,
      seedWorkspace: async () => workspace,
      saveDraft: async (_issueNumber, content, revision) => {
        assert.equal(revision, workspace.draft.revision);
        workspace = {
          ...workspace,
          draft: {
            ...workspace.draft,
            content,
            revision: revision + 1,
          },
        };
        return workspace;
      },
      saveWorkspace: async (value) => {
        workspace = value;
        return value;
      },
    },
    discuss: async () => ({
      answer: "fixture answer",
      replacement: null,
      evidence: [],
      hostEvidence: [],
    }),
    publish: {
      preview: async () => ({
        issueNumber: issue.number,
        revision: workspace.draft.revision,
        body: workspace.draft.content,
        confirmationToken: "fixture-token",
      }),
      publish: async () => {
        writes += 1;
        throw new Error("publish must not be called by preview flow");
      },
    },
    scheduleInvestigation: () => {},
  });

  const instanceId = "endpoint-no-publish-instance";
  const entry = await startInstance(instanceId, {}, () => {});
  try {
    const pageUrl = new URL("/api/issues?offset=0&limit=25", entry.url);
    pageUrl.searchParams.set("token", new URL(entry.url).searchParams.get("token"));
    const pageResponse = await fetch(pageUrl);
    const pageBody = await pageResponse.text();
    assert.equal(pageResponse.ok, true, pageBody);
    const page = JSON.parse(pageBody);
    const itemId = page.items[0].id;
    const endpoint = async (path, body) => {
      const url = new URL(path, entry.url);
      url.searchParams.set("token", new URL(entry.url).searchParams.get("token"));
      const response = await fetch(url, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(body),
      });
      const responseBody = await response.text();
      assert.equal(response.ok, true, responseBody);
      return JSON.parse(responseBody);
    };
    await endpoint("/api/select", { itemId });
    await endpoint("/api/draft", {
      content: "edited fixture draft",
      issueNumber: issue.number,
      provenance: "human",
      revision: 1,
    });
    await endpoint("/api/discuss", {
      issueNumber: issue.number,
      question: "Is this bounded?",
      revision: 2,
    });
    await endpoint("/api/preview", { revision: 2 });
    assert.equal(writes, 0);
  } finally {
    await stopInstance(instanceId);
  }
});

test("late issue-bound draft and discussion responses cannot cross issue selections", async () => {
  let releaseDiscussion;
  let discussionStarted;
  const discussionGate = new Promise((resolve) => {
    releaseDiscussion = resolve;
  });
  const started = new Promise((resolve) => {
    discussionStarted = resolve;
  });
  const issues = [123, 124].map((number) => ({
    number,
    title: `Fixture issue ${number}`,
    url: `https://github.com/dotnet/aspnetcore/issues/${number}`,
    author: "fixture",
    createdAt: "2026-09-06T20:00:00Z",
    updatedAt: "2026-09-06T20:00:00Z",
    labels: ["area-blazor"],
    whyIncluded: ["fixture"],
  }));
  const reports = new Map(issues.map((issue) => [issue.number, {
    schemaVersion: "1.0.0",
    repository: "dotnet/aspnetcore",
    issueNumber: issue.number,
    issueUrl: issue.url,
    area: "area-blazor",
    createdAt: "2026-09-06T20:01:00Z",
    model: "fixture-model",
    messageId: `fixture-message-${issue.number}`,
    skill: "fixture-skill",
    skillDigest: "a".repeat(64),
    content: `fixture report ${issue.number}`,
  }]));
  const workspaces = new Map(issues.map((issue) => [issue.number, {
    issueNumber: issue.number,
    draft: {
      content: `draft ${issue.number}`,
      previousContent: `draft ${issue.number}`,
      revision: 1,
    },
    discussion: [],
    publication: {},
    stopPath: false,
  }]));
  const queue = {
    schemaVersion: "1.0.0",
    repository: "dotnet/aspnetcore",
    area: "area-blazor",
    areaOptions: [],
    generatedAt: "2026-09-06T20:00:00Z",
    predicate: "label:area-blazor",
    coverage: {
      complete: true,
      sourceOpenAreaCount: 2,
      retrievedOpenAreaCount: 2,
      qualifyingCount: 2,
      uncertainMembershipCount: 0,
      pages: 1,
      limitation: null,
      warnings: [],
      rateLimit: null,
      searchCapApplied: false,
      retrieval: "fixture",
    },
    issues,
  };
  const instanceId = "late-response-issue-binding-instance";
  configureServer({
    load: async () => queue,
    store: {
      read: async (number) => reports.get(number) ?? null,
      readArea: async () => "area-blazor",
      write: async (report) => report,
      writeArea: async (area) => area,
      readWorkspace: async (number) => workspaces.get(number) ?? null,
      seedWorkspace: async (report) => workspaces.get(report.issueNumber),
      saveDraft: async (number, content, revision) => {
        const current = workspaces.get(number);
        if (current.draft.revision !== revision) {
          const error = new Error("stale");
          error.code = "stale_revision";
          throw error;
        }
        const next = {
          ...current,
          draft: {
            ...current.draft,
            content,
            previousContent: current.draft.content,
            revision: revision + 1,
          },
        };
        workspaces.set(number, next);
        return next;
      },
      saveWorkspace: async (workspace) => {
        workspaces.set(workspace.issueNumber, workspace);
        return workspace;
      },
    },
    discuss: async ({ issue, workspace }) => {
      assert.equal(issue.number, 123);
      assert.equal(workspace.draft.revision, 1);
      discussionStarted();
      await discussionGate;
      return {
        answer: "response for issue 123",
        replacement: {
          content: "response draft for issue 123",
          baseRevision: workspace.draft.revision,
        },
        evidence: [],
        hostEvidence: [],
      };
    },
    publish: null,
    scheduleInvestigation: () => {},
  });
  const entry = await startInstance(instanceId, {}, () => {});
  try {
    const token = new URL(entry.url).searchParams.get("token");
    const request = async (path, body) => {
      const url = new URL(path, entry.url);
      url.searchParams.set("token", token);
      return fetch(url, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(body),
      });
    };
    const pageUrl = new URL("/api/issues?offset=0&limit=25", entry.url);
    pageUrl.searchParams.set("token", token);
    const page = await (await fetch(pageUrl)).json();
    const [first, second] = page.items;
    await request("/api/select", { itemId: first.id });
    const discussion = request("/api/discuss", {
      issueNumber: 123,
      question: "Check issue 123",
      revision: 1,
    });
    await started;
    await request("/api/select", { itemId: second.id });
    const staleDraft = await request("/api/draft", {
      content: "draft belonging only to issue 123",
      issueNumber: 123,
      provenance: "human",
      revision: 1,
    });
    assert.equal(staleDraft.status, 400);
    assert.equal(workspaces.get(124).draft.content, "draft 124");
    releaseDiscussion();
    const discussionResponse = await discussion;
    assert.equal(discussionResponse.status, 200);
    const discussionBody = await discussionResponse.json();
    assert.equal(discussionBody.applied, false);
    assert.equal(workspaces.get(124).draft.content, "draft 124");
    assert.equal(workspaces.get(123).draft.content, "draft 123");
    assert.equal(workspaces.get(123).discussion.at(-1).answer, "response for issue 123");
  } finally {
    await stopInstance(instanceId);
  }
});

test("saved-workspace discussion and publication reselect the saved issue after queue exit", async () => {
  const issue = {
    id: "saved-issue-endpoint-123",
    repository: "dotnet/aspnetcore",
    number: 123,
    title: "Saved fixture issue",
    url: "https://github.com/dotnet/aspnetcore/issues/123",
    createdAt: "2026-09-06T20:00:00Z",
    updatedAt: "2026-09-06T20:00:00Z",
    labels: ["area-blazor"],
    area: "area-blazor",
    whyIncluded: ["fixture"],
  };
  const report = {
    repository: issue.repository,
    issueNumber: issue.number,
    issueUrl: issue.url,
    area: issue.area,
    createdAt: issue.createdAt,
    model: "fixture-model",
    messageId: "fixture-message",
    skill: "fixture-skill",
    skillDigest: "a".repeat(64),
    content: "fixture report",
  };
  let queue = [issue];
  let workspace = {
    schemaVersion: "2.0.0",
    repository: issue.repository,
    issueNumber: issue.number,
    issueUrl: issue.url,
    area: issue.area,
    originalReport: { createdAt: issue.createdAt, content: report.content },
    draft: {
      content: "saved draft",
      previousContent: "saved draft",
      revision: 1,
      hash: "fixture",
      provenance: "copilot",
      updatedAt: issue.updatedAt,
    },
    discussion: [],
    publication: { pending: null, commentId: null },
    stopPath: false,
  };
  configureServer({
    load: async () => ({
      schemaVersion: "1.0.0",
      repository: issue.repository,
      area: issue.area,
      areaOptions: [],
      generatedAt: issue.updatedAt,
      predicate: "fixture",
      coverage: {
        complete: true,
        sourceOpenAreaCount: 1,
        retrievedOpenAreaCount: 1,
        qualifyingCount: queue.length,
        uncertainMembershipCount: 0,
        pages: 1,
        limitation: null,
        warnings: [],
        rateLimit: null,
        searchCapApplied: false,
        retrieval: "fixture",
      },
      issues: queue,
    }),
    store: {
      read: async () => report,
      readArea: async () => issue.area,
      write: async (value) => value,
      writeArea: async (value) => value,
      readWorkspace: async () => workspace,
      listWorkspaces: async () => [workspace],
      seedWorkspace: async () => workspace,
      saveWorkspace: async (value) => {
        workspace = value;
        return value;
      },
      saveDraft: async (_issueNumber, content, revision) => {
        assert.equal(revision, workspace.draft.revision);
        workspace = {
          ...workspace,
          draft: {
            ...workspace.draft,
            content,
            previousContent: workspace.draft.content,
            revision: revision + 1,
          },
        };
        return workspace;
      },
    },
    discuss: async ({ question }) => ({
      answer: `saved answer: ${question}`,
      replacement: null,
      evidence: [],
      hostEvidence: [],
    }),
    publish: {
      preview: async ({ workspace: current }) => ({
        issueNumber: issue.number,
        revision: current.draft.revision,
        body: current.draft.content,
        confirmationToken: "saved-fixture-token",
      }),
      publish: async ({ workspace: current }) => ({
        status: "published",
        workspace: {
          ...current,
          publication: { ...current.publication, commentId: 456 },
        },
      }),
    },
    scheduleInvestigation: () => {},
  });

  const instanceId = "saved-workspace-follow-through-instance";
  const entry = await startInstance(instanceId, {}, () => {});
  try {
    const token = new URL(entry.url).searchParams.get("token");
    const request = async (path, body) => {
      const url = new URL(path, entry.url);
      url.searchParams.set("token", token);
      const response = await fetch(url, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(body),
      });
      const text = await response.text();
      assert.equal(response.ok, true, text);
      return JSON.parse(text);
    };
    const pageUrl = new URL("/api/issues?offset=0&limit=25", entry.url);
    pageUrl.searchParams.set("token", token);
    const page = await (await fetch(pageUrl)).json();
    await request("/api/select", { itemId: page.items[0].id });
    queue = [];
    await request("/api/refresh", { area: issue.area });
    await request("/api/select-saved", { issueNumber: issue.number });
    const discussed = await request("/api/discuss", {
      issueNumber: issue.number,
      question: "follow up",
      revision: workspace.draft.revision,
    });
    assert.equal(discussed.state.snapshot.selectedWorkspace.discussion.at(-1).answer, "saved answer: follow up");
    const preview = await request("/api/preview", { revision: workspace.draft.revision });
    assert.equal(preview.revision, workspace.draft.revision);
    const published = await request("/api/publish", {
      confirmation: "saved-fixture-token",
      revision: workspace.draft.revision,
    });
    assert.equal(published.state.snapshot.selectedWorkspace.publication.commentId, 456);
  } finally {
    await stopInstance(instanceId);
  }
});

test("concurrent opens share one usable server and capability URL", async () => {
  let releaseArea;
  const areaReady = new Promise((resolve) => {
    releaseArea = resolve;
  });
  configureServer({
    load: async ({ area }) => ({
      schemaVersion: "1.0.0",
      repository: "dotnet/aspnetcore",
      area,
      areaOptions: [],
      generatedAt: "2026-09-06T20:00:00Z",
      predicate: `label:${area}`,
      coverage: {
        complete: true,
        sourceOpenAreaCount: 0,
        retrievedOpenAreaCount: 0,
        qualifyingCount: 0,
        uncertainMembershipCount: 0,
        pages: 1,
        limitation: null,
        warnings: [],
        rateLimit: null,
        searchCapApplied: false,
        retrieval: "fixture",
      },
      issues: [],
    }),
    store: {
      read: async () => null,
      readArea: async () => {
        await areaReady;
        return "area-blazor";
      },
      write: async (report) => report,
      writeArea: async (area) => area,
    },
    scheduleInvestigation: () => {},
  });

  const first = startInstance("concurrent-open-instance", {}, () => {});
  const second = startInstance("concurrent-open-instance", {}, () => {});
  releaseArea();
  const [firstEntry, secondEntry] = await Promise.all([first, second]);
  assert.strictEqual(firstEntry, secondEntry);
  assert.equal(firstEntry.url, secondEntry.url);
  assert.equal((await fetch(firstEntry.url)).status, 200);
  assert.equal((await fetch(secondEntry.url)).status, 200);
  await stopInstance("concurrent-open-instance");
});

test("failed startup releases its reservation for a later open", async () => {
  configureServer({
    load: async () => {
      throw new Error("unused");
    },
    store: {
      read: async () => null,
      readArea: async () => {
        throw new Error("startup failed");
      },
      write: async (report) => report,
      writeArea: async (area) => area,
    },
    scheduleInvestigation: () => {},
  });
  await assert.rejects(
    startInstance("retry-open-instance", {}, () => {}),
    /startup failed/,
  );

  configureServer({
    load: async ({ area }) => ({
      schemaVersion: "1.0.0",
      repository: "dotnet/aspnetcore",
      area,
      areaOptions: [],
      generatedAt: "2026-09-06T20:00:00Z",
      predicate: `label:${area}`,
      coverage: {
        complete: true,
        sourceOpenAreaCount: 0,
        retrievedOpenAreaCount: 0,
        qualifyingCount: 0,
        uncertainMembershipCount: 0,
        pages: 1,
        limitation: null,
        warnings: [],
        rateLimit: null,
        searchCapApplied: false,
        retrieval: "fixture",
      },
      issues: [],
    }),
    store: {
      read: async () => null,
      readArea: async () => "area-blazor",
      write: async (report) => report,
      writeArea: async (area) => area,
    },
    scheduleInvestigation: () => {},
  });
  const entry = await startInstance("retry-open-instance", {}, () => {});
  assert.equal((await fetch(entry.url)).status, 200);
  await stopInstance("retry-open-instance");
});

test("start stop start stop operations execute in invocation order", async () => {
  let releaseFirstArea;
  const firstAreaReady = new Promise((resolve) => {
    releaseFirstArea = resolve;
  });
  let areaReads = 0;
  configureServer({
    load: async ({ area }) => ({
      schemaVersion: "1.0.0",
      repository: "dotnet/aspnetcore",
      area,
      areaOptions: [],
      generatedAt: "2026-09-06T20:00:00Z",
      predicate: `label:${area}`,
      coverage: {
        complete: true,
        sourceOpenAreaCount: 0,
        retrievedOpenAreaCount: 0,
        qualifyingCount: 0,
        uncertainMembershipCount: 0,
        pages: 1,
        limitation: null,
        warnings: [],
        rateLimit: null,
        searchCapApplied: false,
        retrieval: "fixture",
      },
      issues: [],
    }),
    store: {
      read: async () => null,
      readArea: async () => {
        areaReads += 1;
        if (areaReads === 1) {
          await firstAreaReady;
        }
        return "area-blazor";
      },
      write: async (report) => report,
      writeArea: async (area) => area,
    },
    scheduleInvestigation: () => {},
  });

  const firstOpen = startInstance("start-stop-start-stop-instance", {}, () => {});
  const firstStop = stopInstance("start-stop-start-stop-instance");
  const secondOpen = startInstance("start-stop-start-stop-instance", {}, () => {});
  const secondStop = stopInstance("start-stop-start-stop-instance");
  releaseFirstArea();

  const firstEntry = await firstOpen;
  await firstStop;
  const secondEntry = await secondOpen;
  await secondStop;
  assert.equal(areaReads, 2);
  assert.notEqual(firstEntry.url, secondEntry.url);
  await assert.rejects(fetch(firstEntry.url));
  await assert.rejects(fetch(secondEntry.url));
});

test("a stopped initialization cannot overwrite a fresh instance area", async () => {
  let releaseStoppedLoad;
  const stoppedLoadGate = new Promise((resolve) => {
    releaseStoppedLoad = resolve;
  });
  let stoppedLoadStarted;
  const stoppedLoadObserved = new Promise((resolve) => {
    stoppedLoadStarted = resolve;
  });
  const writes = [];
  configureServer({
    load: async ({ area }) => {
      if (area === "area-blazor") {
        stoppedLoadStarted();
        await stoppedLoadGate;
      }
      return {
        schemaVersion: "1.0.0",
        repository: "dotnet/aspnetcore",
        area,
        areaOptions: [],
        generatedAt: "2026-09-06T20:00:00Z",
        predicate: `label:${area}`,
        coverage: {
          complete: true,
          sourceOpenAreaCount: 0,
          retrievedOpenAreaCount: 0,
          qualifyingCount: 0,
          uncertainMembershipCount: 0,
          pages: 1,
          limitation: null,
          warnings: [],
          rateLimit: null,
          searchCapApplied: false,
          retrieval: "fixture",
        },
        issues: [],
      };
    },
    store: {
      read: async () => null,
      readArea: async () => null,
      write: async (report) => report,
      writeArea: async (area, isCurrent) => {
        if (isCurrent()) {
          writes.push(area);
        }
        return area;
      },
    },
    scheduleInvestigation: () => {},
  });

  const firstEntry = await startInstance(
    "stopped-initialization-instance",
    { area: "area-blazor" },
    () => {},
  );
  await stoppedLoadObserved;
  const firstStop = stopInstance("stopped-initialization-instance");
  const secondOpen = startInstance(
    "stopped-initialization-instance",
    { area: "area-mvc" },
    () => {},
  );
  releaseStoppedLoad();
  await firstStop;
  const secondEntry = await secondOpen;
  await secondEntry.initialization;

  assert.deepEqual(writes, ["area-mvc"]);
  await assert.rejects(fetch(firstEntry.url));
  assert.equal((await fetch(secondEntry.url)).status, 200);
  await stopInstance("stopped-initialization-instance");
});
