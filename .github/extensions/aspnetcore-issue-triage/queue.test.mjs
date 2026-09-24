import assert from "node:assert/strict";
import test from "node:test";

import { buildQueuePredicate, loadIssueQueue } from "./queue.mjs";

function issue(number, overrides = {}) {
  const labels = overrides.labels ?? ["area-blazor"];
  return {
    number,
    title: overrides.title ?? `Issue ${number}`,
    url: `https://github.com/dotnet/aspnetcore/issues/${number}`,
    author: { login: "reporter" },
    createdAt: overrides.createdAt ?? `2026-01-${String(number).padStart(2, "0")}T00:00:00Z`,
    updatedAt: overrides.updatedAt ?? "2026-09-01T00:00:00Z",
    milestone: overrides.milestone ?? null,
    labels: {
      totalCount: overrides.labelTotal ?? labels.length,
      nodes: labels.map((name) => ({ name })),
    },
  };
}

function payload(nodes, {
  totalCount = nodes.length,
  hasNextPage = false,
  endCursor = null,
  remaining = 4999,
} = {}) {
  return {
    data: {
      repository: {
        issues: {
          totalCount,
          pageInfo: { hasNextPage, endCursor },
          nodes,
        },
      },
      rateLimit: { cost: 1, remaining, resetAt: "2026-09-06T20:00:00Z" },
    },
  };
}

test("predicate matches the confirmed unmilestoned area queue", () => {
  assert.equal(
    buildQueuePredicate("area-blazor"),
    'repo:dotnet/aspnetcore is:issue is:open no:milestone label:area-blazor -label:"Needs: Author Feedback" -label:":heavy_check_mark: Resolution: Answered" -label:":heavy_check_mark: Resolution: Duplicate" sort:created-asc',
  );
});

test("empty queues are complete and retain source counts", async () => {
  const queue = await loadIssueQueue(
    { area: "area-blazor" },
    { executeGraphql: async () => payload([]), now: () => "2026-09-06T18:00:00Z" },
  );
  assert.equal(queue.coverage.complete, true);
  assert.equal(queue.coverage.sourceOpenAreaCount, 0);
  assert.equal(queue.coverage.qualifyingCount, 0);
  assert.deepEqual(queue.issues, []);
});

test("multi-page queues retrieve all pages, filter mechanically, and sort stably", async () => {
  const calls = [];
  const pages = [
    payload(
      [
        issue(3, { createdAt: "2026-01-03T00:00:00Z" }),
        issue(1, { createdAt: "2026-01-01T00:00:00Z", milestone: { number: 1, title: "Backlog" } }),
      ],
      { totalCount: 5, hasNextPage: true, endCursor: "page-2" },
    ),
    payload(
      [
        issue(2, { createdAt: "2026-01-02T00:00:00Z" }),
        issue(4, { labels: ["area-blazor", "Needs: Author Feedback"] }),
        issue(5, { createdAt: "2026-01-03T00:00:00Z" }),
      ],
      { totalCount: 5, remaining: 4998 },
    ),
  ];
  const queue = await loadIssueQueue(
    { area: "area-blazor" },
    {
      executeGraphql: async (request) => {
        calls.push(request);
        return pages.shift();
      },
    },
  );

  assert.equal(calls[0].area, "area-blazor");
  assert.equal(calls[1].after, "page-2");
  assert.equal(queue.coverage.complete, true);
  assert.equal(queue.coverage.retrievedOpenAreaCount, 5);
  assert.equal(queue.coverage.qualifyingCount, 3);
  assert.deepEqual(queue.issues.map((item) => item.number), [2, 3, 5]);
  assert.equal(queue.coverage.rateLimit.remaining, 4998);
  assert.equal(queue.coverage.searchCapApplied, false);
});

test("scope changes are sent to GitHub rather than filtered from another area", async () => {
  const areas = [];
  for (const area of ["area-blazor", "area-mvc"]) {
    await loadIssueQueue(
      { area },
      {
        executeGraphql: async (request) => {
          areas.push(request.area);
          return payload([]);
        },
      },
    );
  }
  assert.deepEqual(areas, ["area-blazor", "area-mvc"]);
});

test("later-page failures retain partial evidence and mark the queue incomplete", async () => {
  let calls = 0;
  const queue = await loadIssueQueue(
    { area: "area-blazor" },
    {
      executeGraphql: async () => {
        calls += 1;
        if (calls === 1) {
          return payload([issue(1)], {
            totalCount: 2,
            hasNextPage: true,
            endCursor: "page-2",
          });
        }
        const error = new Error("API rate limit exceeded");
        error.code = "github_rate_limited";
        throw error;
      },
    },
  );
  assert.equal(queue.coverage.complete, false);
  assert.equal(queue.coverage.retrievedOpenAreaCount, 1);
  assert.match(queue.coverage.limitation, /rate limit/i);
});

test("truncated per-issue labels make membership visibly incomplete", async () => {
  const queue = await loadIssueQueue(
    { area: "area-blazor" },
    {
      executeGraphql: async () => payload([
        issue(1, { labelTotal: 101 }),
      ]),
    },
  );
  assert.equal(queue.coverage.complete, false);
  assert.equal(queue.coverage.uncertainMembershipCount, 1);
  assert.equal(queue.coverage.qualifyingCount, 0);
  assert.deepEqual(queue.issues, []);
  assert.match(queue.coverage.warnings[0], /more than 100 labels/);
});

test("GraphQL field errors never become affirmative queue membership", async () => {
  await assert.rejects(
    loadIssueQueue(
      { area: "area-blazor" },
      {
        executeGraphql: async () => ({
          ...payload([issue(1)]),
          errors: [{ message: "milestone field could not be resolved" }],
        }),
      },
    ),
    (error) => error.code === "github_response_partial",
  );
});
