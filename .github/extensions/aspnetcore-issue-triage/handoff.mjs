import { execFile } from "node:child_process";
import { promisify } from "node:util";

import { EXCLUDED_LABELS, REPOSITORY } from "./taxonomy.mjs";

const execFileAsync = promisify(execFile);

export function buildIssueSessionPrompt(issueNumber, destination) {
  if (!Number.isSafeInteger(issueNumber) || issueNumber < 1) {
    throw handoffError("invalid_issue", "issueNumber must be a positive integer.");
  }

  const issueUrl = `https://github.com/${REPOSITORY}/issues/${issueNumber}`;
  const research = `Use the repository's \`investigate-issue\` skill to research exactly ${REPOSITORY}#${issueNumber} (${issueUrl}). First confirm that the skill is available in this session. If it is unavailable, stop and explain that prerequisite in chat; do not substitute a generic investigation. Otherwise follow that skill using live public evidence only. Do not implement changes, execute reporter projects, build reproductions, launch browsers, retrieve private material, publish findings, or mutate GitHub issue state. Leave advisory findings and follow-up in this conversation, not the canvas.`;
  if (destination === "current") {
    return `Research the canonical public issue ${REPOSITORY}#${issueNumber} here in this foreground session. Do not open or create another session for this request.

${research}`;
  }
  if (destination !== "new-child") {
    throw handoffError("invalid_destination", "Investigation destination is invalid.");
  }
  return `Create a fresh nested project session for the canonical public issue ${REPOSITORY}#${issueNumber}.

Use the create_session tool with exactly:
- no project_id
- workspace_type: "worktree"
- detached: false
- coordinate_with_creator: false
- kickoff: { mode: "interactive", prompt: <the kickoff text below> }

Use the current ${REPOSITORY} project. Omit base-branch and model overrides.
Create a new child nested under this session; do not reuse or fork an existing session.
If session creation is unavailable, explain that in chat rather than researching here.

Kickoff text:
${research}`;
}

export function createForegroundHandoff({ send, log = null, isBusy = () => null, readIssue = readCanonicalIssue } = {}) {
  if (typeof send !== "function") {
    throw new Error("send is required");
  }

  let inFlight = 0;

  return {
    async dispatch(item, destination, isCurrent = () => true) {
      if (!["current", "new-child"].includes(destination)) {
        throw handoffError("invalid_destination", "Investigation destination is invalid.");
      }
      if (inFlight !== 0) {
        throw handoffError("handoff_in_progress", "An investigation request is already being sent.");
      }
      inFlight += 1;
      let queued = null;
      try {
        await validateLiveQueueIssue(item, { readIssue });
        if (!isCurrent()) {
          return { status: "cancelled", queued, messageId: null };
        }
        queued = isBusy();
        try {
          await log?.(`Requested ${destination} investigation for ${REPOSITORY}#${item.number}${queued ? " (queued behind the current task)" : ""}.`);
        } catch {
          // The timeline breadcrumb is cosmetic; dispatch remains authoritative.
        }
        if (!isCurrent()) {
          return { status: "cancelled", queued, messageId: null };
        }
        const prompt = buildIssueSessionPrompt(item.number, destination);
        let messageId;
        try {
          messageId = await send({ prompt });
        } catch (error) {
          return { status: "unknown", queued, messageId: null, error: error?.message ?? "Issue-session delivery could not be confirmed." };
        }
        return typeof messageId === "string" && messageId.trim() && messageId.length <= 512
          ? { status: "sent", queued, messageId }
          : { status: "unknown", queued, messageId: null };
      } catch (error) {
        if (error?.code) {
          throw error;
        }
        throw handoffError("handoff_failed", error?.message ?? "Unable to send the issue-session request.");
      } finally {
        inFlight -= 1;
      }
    },
  };
}

export async function validateLiveQueueIssue(item, { readIssue = readCanonicalIssue } = {}) {
  if (
    !item
    || item.repository !== REPOSITORY
    || !Number.isSafeInteger(item.number)
    || item.number < 1
    || item.url !== `https://github.com/${REPOSITORY}/issues/${item.number}`
    || typeof item.area !== "string"
  ) {
    throw handoffError("canonical_issue_invalid", "The selected item is not a canonical ASP.NET Core issue.");
  }
  const issue = await readIssue(item.number);
  if (
    issue.repository !== REPOSITORY
    || issue.number !== item.number
    || issue.url !== item.url
    || issue.isPullRequest
  ) {
    throw handoffError("canonical_issue_invalid", "The selected item is not the expected canonical ASP.NET Core issue.");
  }
  if (issue.state !== "open" || issue.milestone !== null) {
    throw handoffError("stale_issue", "The selected issue is no longer an open unmilestoned issue.");
  }
  if (!issue.labels.includes(item.area)) {
    throw handoffError("stale_scope", `The selected issue no longer has ${item.area}.`);
  }
  const excluded = issue.labels.find((label) => EXCLUDED_LABELS.includes(label));
  if (excluded) {
    throw handoffError("stale_issue", `The selected issue now has excluded label "${excluded}".`);
  }
  return issue;
}

export async function readCanonicalIssue(issueNumber) {
  let stdout;
  try {
    ({ stdout } = await execFileAsync("gh", ["api", `repos/${REPOSITORY}/issues/${issueNumber}`], {
      encoding: "utf8",
      maxBuffer: 4 * 1024 * 1024,
      timeout: 30_000,
      env: { ...process.env, GH_PAGER: "cat" },
    }));
  } catch (error) {
    const detail = error.stderr?.trim() || error.message;
    throw handoffError(/rate.?limit/i.test(detail) ? "github_rate_limited" : "github_issue_read_failed", detail);
  }
  let issue;
  try {
    issue = JSON.parse(stdout);
  } catch (error) {
    throw handoffError("github_response_invalid", `GitHub returned invalid JSON: ${error.message}`);
  }
  return {
    repository: REPOSITORY,
    number: issue.number,
    url: issue.html_url,
    state: issue.state,
    milestone: issue.milestone ? { number: issue.milestone.number } : null,
    labels: Array.isArray(issue.labels) ? issue.labels.map((label) => typeof label === "string" ? label : label.name) : [],
    isPullRequest: issue.pull_request !== undefined,
  };
}

function handoffError(code, message) {
  const error = new Error(message);
  error.code = code;
  return error;
}
