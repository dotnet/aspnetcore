import assert from "node:assert/strict";
import test from "node:test";

import {
  buildIsolatedSystemMessage,
  createInvestigationTools,
  createPinnedSourceTracker,
  readBoundedPublicResponse,
  validateIssueSearchQuery,
  validateIsolatedEvents,
} from "./agent.mjs";

const TEST_MODEL = "test-model";

test("isolated agent exposes only extension-owned read-only evidence tools", () => {
  const tools = createInvestigationTools("/repo");
  assert.deepEqual(tools.map((tool) => tool.name), [
    "aspnetcore_triage_read_item",
    "aspnetcore_triage_search_items",
    "aspnetcore_triage_read_file",
    "aspnetcore_triage_search_repository",
    "aspnetcore_triage_file_history",
    "aspnetcore_triage_fetch_public",
  ]);
  assert.ok(tools.every((tool) => tool.skipPermission === true));
  const searchTool = tools.find((tool) => tool.name === "aspnetcore_triage_search_repository");
  assert.deepEqual(searchTool.parameters.required, ["pattern", "path", "ref"]);
  const historyTool = tools.find((tool) => tool.name === "aspnetcore_triage_file_history");
  assert.deepEqual(historyTool.parameters.required, ["path", "ref", "limit"]);
});

test("repository evidence is pinned to the first publicly resolved commit", async () => {
  const resolutions = [];
  const tracker = createPinnedSourceTracker(async (ref) => {
    resolutions.push(ref);
    return "a".repeat(40);
  });

  assert.equal(await tracker.resolve("main"), "a".repeat(40));
  assert.equal(tracker.getSource(), null);
  tracker.markUsed("main", "a".repeat(40));
  assert.equal(await tracker.resolve("main"), "a".repeat(40));
  assert.equal(await tracker.resolve("a".repeat(40)), "a".repeat(40));
  assert.deepEqual(tracker.getSource(), {
    ref: "main",
    commit: "a".repeat(40),
  });
  assert.deepEqual(resolutions, ["main"]);
  await assert.rejects(
    tracker.resolve("release/10.0"),
    (error) => error.code === "repository_ref_mismatch",
  );
});

test("generic public fetch cannot bypass pinned ASP.NET Core source tools", async () => {
  const tools = createInvestigationTools("/repo", {
    fetchImpl: async () => {
      throw new Error("fetch should not run");
    },
  });
  const publicFetch = tools.find((tool) => tool.name === "aspnetcore_triage_fetch_public");
  for (const url of [
    "https://github.com/dotnet/aspnetcore/blob/main/README.md",
    "https://raw.githubusercontent.com/dotnet/aspnetcore/main/README.md",
  ]) {
    await assert.rejects(
      publicFetch.handler({ url }),
      (error) => error.code === "public_url_forbidden",
    );
  }
});

test("issue search cannot add another repository scope", () => {
  assert.equal(validateIssueSearchQuery("StaticHtmlRenderer is:open"), "StaticHtmlRenderer is:open");
  for (const query of ["repo:other/repo", "org:microsoft", "user:octocat", "-repo:dotnet/runtime"]) {
    assert.throws(
      () => validateIssueSearchQuery(query),
      (error) => error.code === "search_query_invalid",
    );
  }
});

test("public fetch bodies are streamed within the evidence limit", async () => {
  const response = new Response("public text", {
    headers: { "content-type": "text/plain; charset=utf-8" },
  });
  assert.equal(await readBoundedPublicResponse(response), "public text");

  const oversized = new Response("ignored", {
    headers: {
      "content-type": "text/plain",
      "content-length": String(512 * 1024 + 1),
    },
  });
  await assert.rejects(
    readBoundedPublicResponse(oversized),
    (error) => error.code === "evidence_too_large",
  );

  const binary = new Response(new Uint8Array([0, 1, 2]), {
    headers: { "content-type": "application/octet-stream" },
  });
  await assert.rejects(
    readBoundedPublicResponse(binary),
    (error) => error.code === "public_content_type_forbidden",
  );
});

test("exact skill content and digest are loaded into isolated system context", () => {
  const message = buildIsolatedSystemMessage(
    "# Exact skill content",
    ".github/skills/investigate-issue/SKILL.md",
    "a".repeat(64),
  );
  assert.match(message, /# Exact skill content/);
  assert.match(message, /\.github\/skills\/investigate-issue\/SKILL\.md/);
  assert.match(message, new RegExp("a".repeat(64)));
});

test("isolated event audit rejects other tools, models, and sub-agents", () => {
  const allowed = ["aspnetcore_triage_read_item"];
  assert.equal(validateIsolatedEvents([
    {
      type: "tool.execution_start",
      data: {
        toolName: "aspnetcore_triage_read_item",
        model: TEST_MODEL,
      },
    },
  ], allowed, TEST_MODEL), true);
  assert.throws(
    () => validateIsolatedEvents([
      { type: "tool.execution_start", data: { toolName: "bash", model: TEST_MODEL } },
    ], allowed, TEST_MODEL),
    (error) => error.code === "investigation_tool_boundary_violated",
  );
  assert.throws(
    () => validateIsolatedEvents([
      { type: "assistant.message", data: { model: "other-test-model" } },
    ], allowed, TEST_MODEL),
    (error) => error.code === "investigation_model_disallowed",
  );
  assert.throws(
    () => validateIsolatedEvents([
      { type: "assistant.message", agentId: "subagent-1", data: { model: TEST_MODEL } },
    ], allowed, TEST_MODEL),
    (error) => error.code === "investigation_delegation_detected",
  );
});
