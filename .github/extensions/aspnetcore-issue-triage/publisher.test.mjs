import assert from "node:assert/strict";
import { createHash } from "node:crypto";
import { mkdtemp, rm } from "node:fs/promises";
import { tmpdir } from "node:os";
import { join } from "node:path";
import test from "node:test";

import { createGitHubAdapter, createPublisher } from "./publisher.mjs";
import { ASSISTANCE_DISCLOSURE } from "./publication.mjs";
import { createReportStore } from "./storage.mjs";

test("production GitHub adapter sends exact JSON comment bodies through its seam", async () => {
  const calls = [];
  const adapter = createGitHubAdapter({
    runJson: async (args, input = {}) => {
      calls.push({ args, input });
      return {
        id: 123,
        issue_url: "https://api.github.com/repos/dotnet/aspnetcore/issues/456",
        html_url: "https://github.com/dotnet/aspnetcore/issues/456#issuecomment-123",
        body: JSON.parse(input.body).body,
        user: { login: "human", id: 7 },
      };
    },
  });

  const comment = await adapter.createComment({ issueNumber: 456, body: "**exact** Markdown" });

  assert.deepEqual(JSON.parse(calls[0].input.body), { body: "**exact** Markdown" });
  assert.equal(comment.issueNumber, 456);
  assert.equal(comment.authorId, 7);
});

test("production GitHub adapter preserves ambiguous mutating transport failures", async () => {
  const adapter = createGitHubAdapter({
    runJson: async () => {
      const error = new Error("network interrupted");
      error.code = "github_transport_failed";
      error.ambiguous = true;
      throw error;
    },
  });

  await assert.rejects(
    adapter.updateComment({ issueNumber: 456, commentId: 123, body: "body" }),
    (error) => error.code === "github_transport_failed" && error.ambiguous === true,
  );
});

const publicationBody = `**Classification:** Research

${ASSISTANCE_DISCLOSURE}

Synthetic public assessment.`;

async function withPublisherFixture(run) {
  const artifactRoot = await mkdtemp(join(tmpdir(), "aspnetcore-triage-publisher-"));
  try {
    const store = createReportStore(artifactRoot, process.cwd());
    const issue = {
      repository: "dotnet/aspnetcore",
      number: 123,
      url: "https://github.com/dotnet/aspnetcore/issues/123",
    };
    const initial = await store.seedWorkspace({
      schemaVersion: "1.0.0",
      ...issue,
      issueNumber: issue.number,
      issueUrl: issue.url,
      area: "area-blazor",
      createdAt: new Date().toISOString(),
      model: "test-model",
      content: publicationBody,
    });
    const calls = [];
    const comments = [];
    let responseMode = "normal";
    let nextId = 1000;

    function comment(id, body, createdAt = new Date().toISOString()) {
      return {
        id,
        body,
        user: { login: "fixture", id: 1 },
        issue_url: "https://api.github.com/repos/dotnet/aspnetcore/issues/123",
        html_url: `${issue.url}#issuecomment-${id}`,
        created_at: createdAt,
        updated_at: createdAt,
      };
    }

    const adapter = createGitHubAdapter({
      runJson: async (args, input = {}) => {
        const endpoint = args[1];
        const method = args.includes("--method")
          ? args[args.indexOf("--method") + 1]
          : "GET";
        calls.push({ endpoint, method, input });
        if (method !== "GET") {
          const body = JSON.parse(input.body).body;
          const result = method === "POST"
            ? comment(nextId++, body)
            : comments.find((item) => item.id === Number(endpoint.split("/").at(-1)));
          assert.ok(result);
          result.body = body;
          result.updated_at = new Date().toISOString();
          comments.push(...(method === "POST" ? [result] : []));
          const mode = responseMode;
          responseMode = "normal";
          if (mode === "malformed") {
            return {};
          }
          if (mode === "lost") {
            const error = new Error("Synthetic lost acknowledgement after accepted write");
            error.code = "github_transport_failed";
            error.ambiguous = true;
            throw error;
          }
          return { ...result };
        }
        if (endpoint === "repos/dotnet/aspnetcore") {
          return { private: false, visibility: "public" };
        }
        if (endpoint === "user") {
          return { login: "fixture", id: 1, type: "User" };
        }
        if (endpoint === "repos/dotnet/aspnetcore/issues/123") {
          return {
            number: 123,
            html_url: issue.url,
            state: "open",
            updated_at: "synthetic-issue-revision",
          };
        }
        if (endpoint.startsWith("repos/dotnet/aspnetcore/issues/123/comments")) {
          return args.includes("--slurp") ? [comments] : comments;
        }
        if (endpoint.startsWith("repos/dotnet/aspnetcore/issues/comments/")) {
          return { ...comments.find((item) => item.id === Number(endpoint.split("/").at(-1))) };
        }
        throw new Error(`Unexpected fixture endpoint: ${endpoint}`);
      },
    });
    await run({
      store,
      issue,
      initial,
      comments,
      calls,
      comment,
      adapter,
      publisher: () => createPublisher(store, { adapter }),
      setResponseMode: (mode) => { responseMode = mode; },
    });
  } finally {
    await rm(artifactRoot, { recursive: true, force: true });
  }
}

test("production publisher persists a create receipt and restarts as update", async () => {
  await withPublisherFixture(async (fixture) => {
    let publisher = fixture.publisher();
    let preview = await publisher.preview({ issue: fixture.issue, workspace: fixture.initial });
    const created = await publisher.publish({
      workspace: fixture.initial,
      confirmation: preview.confirmationToken,
    });
    assert.equal(created.status, "published");
    let workspace = await fixture.store.readWorkspace(123);
    workspace = await fixture.store.saveDraft(
      123,
      `${publicationBody}\n\nHuman refinement.`,
      workspace.draft.revision,
    );
    publisher = fixture.publisher();
    preview = await publisher.preview({ issue: fixture.issue, workspace });
    assert.equal(preview.operation, "update");
    assert.equal(
      (await publisher.publish({ workspace, confirmation: preview.confirmationToken })).status,
      "published",
    );
    assert.deepEqual(
      fixture.calls.filter((call) => call.method !== "GET").map((call) => call.method),
      ["POST", "PATCH"],
    );
  });
});

test("production publisher treats malformed mutation receipts as pending", async () => {
  await withPublisherFixture(async (fixture) => {
    const publisher = fixture.publisher();
    const preview = await publisher.preview({ issue: fixture.issue, workspace: fixture.initial });
    fixture.setResponseMode("malformed");
    const result = await publisher.publish({
      workspace: fixture.initial,
      confirmation: preview.confirmationToken,
    });
    assert.equal(result.status, "unknown");
    assert.ok((await fixture.store.readWorkspace(123)).publication.pending);
    await assert.rejects(
      publisher.preview({ issue: fixture.issue, workspace: fixture.initial }),
      (error) => error.code === "publication_pending",
    );
    assert.equal(fixture.calls.filter((call) => call.method === "POST").length, 1);
  });
});

test("production publisher requires unique recent provenance during create recovery", async () => {
  await withPublisherFixture(async (fixture) => {
    fixture.comments.push(fixture.comment(17, publicationBody, "2000-01-01T00:00:00.000Z"));
    let publisher = fixture.publisher();
    const preview = await publisher.preview({ issue: fixture.issue, workspace: fixture.initial });
    fixture.setResponseMode("lost");
    assert.equal(
      (await publisher.publish({ workspace: fixture.initial, confirmation: preview.confirmationToken })).status,
      "unknown",
    );
    publisher = fixture.publisher();
    const recovered = await publisher.resolvePending({
      workspace: await fixture.store.readWorkspace(123),
    });
    assert.notEqual(recovered.recordedComment?.id, 17);
  });
});

test("production GitHub adapter abstains on same-second create recovery candidates", async () => {
  const attemptedAt = "2026-09-07T02:20:00.000Z";
  const body = "synthetic body";
  const runJson = async (args) => {
    assert.match(args[1], /issues\/123\/comments/);
    return [[
      {
        id: 17,
        body,
        user: { login: "fixture", id: 1 },
        issue_url: "https://api.github.com/repos/dotnet/aspnetcore/issues/123",
        html_url: "https://github.com/dotnet/aspnetcore/issues/123#issuecomment-17",
        created_at: attemptedAt,
        updated_at: attemptedAt,
      },
    ]];
  };
  const adapter = createGitHubAdapter({ runJson });

  assert.equal(
    await adapter.readComment({
      issueNumber: 123,
      accountId: 1,
      bodyHash: createHash("sha256").update(body, "utf8").digest("hex"),
      attemptedAt,
    }),
    null,
  );
});

test("production publisher rejects a stale panel workspace before mutation", async () => {
  await withPublisherFixture(async (fixture) => {
    const publisher = fixture.publisher();
    const preview = await publisher.preview({ issue: fixture.issue, workspace: fixture.initial });
    await fixture.store.saveDraft(
      123,
      `${publicationBody}\n\nNewer other-panel correction.`,
      fixture.initial.draft.revision,
    );
    await assert.rejects(
      publisher.publish({ workspace: fixture.initial, confirmation: preview.confirmationToken }),
      (error) => error.code === "stale_revision",
    );
    assert.equal(fixture.calls.filter((call) => call.method !== "GET").length, 0);
  });
});

test("production publisher fails closed without durable publication transactions", async () => {
  await withPublisherFixture(async (fixture) => {
    const incompleteStore = {
      readWorkspace: fixture.store.readWorkspace.bind(fixture.store),
    };
    let approvedPreview;
    const publisher = createPublisher(incompleteStore, {
      adapter: fixture.adapter,
      createService: (_adapter, { recordAttempt }) => ({
        publish: async () => {
          await recordAttempt({
            status: "pending",
            issueNumber: fixture.issue.number,
            preview: approvedPreview,
          });
        },
        resolvePending: async () => {
          throw new Error("Not used by this test.");
        },
        getPendingAttempt: () => null,
        restorePendingAttempt: () => {},
      }),
    });
    approvedPreview = await publisher.preview({
      issue: fixture.issue,
      workspace: fixture.initial,
    });

    await assert.rejects(
      publisher.publish({
        workspace: fixture.initial,
        confirmation: approvedPreview.confirmationToken,
      }),
      (error) => error.code === "publication_store_unsupported",
    );
  });
});
