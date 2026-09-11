import { execFile } from "node:child_process";
import { promisify } from "node:util";

import {
  AREA_OPTIONS,
  EXCLUDED_LABELS,
  REPOSITORY,
  normalizeArea,
} from "./taxonomy.mjs";

const execFileAsync = promisify(execFile);
const [owner, name] = REPOSITORY.split("/");

const QUEUE_QUERY = `
query($owner: String!, $name: String!, $area: String!, $first: Int!, $after: String) {
  repository(owner: $owner, name: $name) {
    issues(
      first: $first
      after: $after
      states: OPEN
      labels: [$area]
      orderBy: { field: CREATED_AT, direction: ASC }
    ) {
      totalCount
      pageInfo { hasNextPage endCursor }
      nodes {
        number
        title
        url
        author { login }
        createdAt
        updatedAt
        milestone { number title }
        labels(first: 100) {
          totalCount
          nodes { name }
        }
      }
    }
  }
  rateLimit {
    cost
    remaining
    resetAt
  }
}`;

export function buildQueuePredicate(area) {
  const normalizedArea = normalizeArea(area);
  return [
    `repo:${REPOSITORY}`,
    "is:issue",
    "is:open",
    "no:milestone",
    `label:${normalizedArea}`,
    ...EXCLUDED_LABELS.map((label) => `-label:"${label}"`),
    "sort:created-asc",
  ].join(" ");
}

export async function loadIssueQueue(
  input = {},
  { executeGraphql = executeGhGraphql, now = () => new Date().toISOString() } = {},
) {
  const area = normalizeArea(input.area);
  const rawIssues = [];
  const warnings = [];
  let after = null;
  let sourceTotal = null;
  let pages = 0;
  let complete = true;
  let limitation = null;
  let rateLimit = null;

  do {
    let payload;
    try {
      payload = await executeGraphql({ area, after, query: QUEUE_QUERY });
    } catch (error) {
      complete = false;
      limitation = error.message;
      if (pages === 0) {
        throw queueError(
          error.code ?? "github_query_failed",
          `Unable to retrieve the ${area} issue queue: ${error.message}`,
        );
      }
      break;
    }

    if (Array.isArray(payload?.errors) && payload.errors.length > 0) {
      const detail = payload.errors
        .map((error) => error?.message)
        .filter((message) => typeof message === "string" && message)
        .join("; ") || "GitHub returned a partial GraphQL response.";
      complete = false;
      limitation = detail;
      if (pages === 0) {
        throw queueError("github_response_partial", `Unable to establish queue membership: ${detail}`);
      }
      break;
    }

    const issues = payload?.data?.repository?.issues;
    if (!issues || !Array.isArray(issues.nodes) || !issues.pageInfo) {
      throw queueError("github_response_invalid", "GitHub returned an invalid issue queue response.");
    }

    pages += 1;
    sourceTotal ??= issues.totalCount;
    rateLimit = normalizeRateLimit(payload.data.rateLimit);

    for (const node of issues.nodes.filter(Boolean)) {
      const issue = normalizeIssue(node, area);
      if (!issue.labelsComplete) {
        complete = false;
        warnings.push(
          `${REPOSITORY}#${issue.number} has more than 100 labels; exclusion membership is not fully established.`,
        );
      }
      rawIssues.push(issue);
    }

    if (issues.pageInfo.hasNextPage) {
      if (typeof issues.pageInfo.endCursor !== "string" || !issues.pageInfo.endCursor) {
        complete = false;
        limitation = "GitHub reported another page without an end cursor.";
        break;
      }
      after = issues.pageInfo.endCursor;
    } else {
      after = null;
    }
  } while (after);

  if (sourceTotal !== rawIssues.length) {
    complete = false;
    limitation ??= `Retrieved ${rawIssues.length} of ${sourceTotal ?? "an unknown number of"} open ${area} issues.`;
  }

  const issues = rawIssues
    .filter((issue) => issue.labelsComplete)
    .filter((issue) => issue.milestone === null)
    .filter((issue) => !issue.labels.some((label) => EXCLUDED_LABELS.includes(label)))
    .sort(compareIssues)
    .map((issue) => ({
      ...issue,
      whyIncluded: [
        "Open issue",
        `Labeled ${area}`,
        "No milestone",
        "No excluded response or resolution label",
      ],
    }));

  return {
    schemaVersion: "1.0.0",
    repository: REPOSITORY,
    area,
    areaOptions: AREA_OPTIONS,
    generatedAt: now(),
    predicate: buildQueuePredicate(area),
    issues,
    coverage: {
      complete,
      sourceOpenAreaCount: sourceTotal,
      retrievedOpenAreaCount: rawIssues.length,
      qualifyingCount: issues.length,
      uncertainMembershipCount: rawIssues.filter((issue) => !issue.labelsComplete).length,
      pages,
      limitation,
      warnings,
      rateLimit,
      searchCapApplied: false,
      retrieval: "Repository issue connection filtered by the selected area label, fully paginated.",
    },
  };
}

export async function executeGhGraphql({ area, after, query = QUEUE_QUERY }) {
  const args = [
    "api",
    "graphql",
    "-f",
    `query=${query}`,
    "-f",
    `owner=${owner}`,
    "-f",
    `name=${name}`,
    "-f",
    `area=${normalizeArea(area)}`,
    "-F",
    "first=100",
  ];
  if (after) {
    args.push("-f", `after=${after}`);
  }

  let stdout;
  try {
    ({ stdout } = await execFileAsync("gh", args, {
      encoding: "utf8",
      maxBuffer: 8 * 1024 * 1024,
      timeout: 60_000,
      env: { ...process.env, GH_PAGER: "cat" },
    }));
  } catch (error) {
    const detail = error.stderr?.trim() || error.message;
    const code = /rate.?limit/i.test(detail) ? "github_rate_limited" : "github_query_failed";
    throw queueError(code, detail);
  }

  try {
    return JSON.parse(stdout);
  } catch (error) {
    throw queueError("github_response_invalid", `GitHub returned invalid JSON: ${error.message}`);
  }
}

function normalizeIssue(node, area) {
  if (!Number.isInteger(node?.number) || node.number < 1) {
    throw queueError("github_response_invalid", "GitHub returned an invalid issue number.");
  }
  const expectedUrl = `https://github.com/${REPOSITORY}/issues/${node.number}`;
  if (node.url !== expectedUrl) {
    throw queueError("github_response_invalid", `Issue URL must match ${expectedUrl}.`);
  }
  if (typeof node.title !== "string" || typeof node.createdAt !== "string") {
    throw queueError("github_response_invalid", `GitHub returned invalid metadata for ${REPOSITORY}#${node.number}.`);
  }

  const labels = node.labels?.nodes?.filter(Boolean).map((label) => label.name) ?? [];
  return {
    number: node.number,
    title: node.title,
    url: expectedUrl,
    author: node.author?.login ?? "",
    createdAt: node.createdAt,
    updatedAt: typeof node.updatedAt === "string" ? node.updatedAt : node.createdAt,
    milestone: node.milestone
      ? { number: node.milestone.number, title: node.milestone.title }
      : null,
    labels,
    labelsComplete: node.labels?.totalCount === labels.length,
    area,
  };
}

function compareIssues(left, right) {
  return left.createdAt.localeCompare(right.createdAt) || left.number - right.number;
}

function normalizeRateLimit(rateLimit) {
  if (!rateLimit) {
    return null;
  }
  return {
    cost: Number.isInteger(rateLimit.cost) ? rateLimit.cost : null,
    remaining: Number.isInteger(rateLimit.remaining) ? rateLimit.remaining : null,
    resetAt: typeof rateLimit.resetAt === "string" ? rateLimit.resetAt : null,
  };
}

function queueError(code, message) {
  const error = new Error(message);
  error.code = code;
  return error;
}
