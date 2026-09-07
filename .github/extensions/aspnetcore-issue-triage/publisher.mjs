import { createHash } from "node:crypto";
import { spawn } from "node:child_process";

import {
  createPublicationPreview,
  createPublicationService,
  hashPublicationBody,
} from "./publication.mjs";

const REPOSITORY = "dotnet/aspnetcore";

export function createPublisher(store, {
  adapter = createGitHubAdapter(),
  createService = createPublicationService,
} = {}) {
  const service = createService(adapter, {
    recordAttempt: async (record) => {
      if (record.status === "pending" && typeof store.recordPublicationAttempt === "function") {
        await store.recordPublicationAttempt(
          record.issueNumber,
          record.preview.draftRevision,
          record,
        );
        return;
      }
      if (record.status !== "pending" && typeof store.completePublicationAttempt === "function") {
        await store.completePublicationAttempt(
          record.issueNumber,
          record.attemptId,
          record,
          record.status === "published"
            ? {
              commentId: record.recordedComment.id,
              commentUrl: record.recordedComment.htmlUrl ?? record.recordedComment.html_url,
              account: {
                login: record.recordedComment.accountLogin,
                id: record.recordedComment.accountId ?? record.recordedComment.authorId,
              },
              publishedRevision: record.preview.draftRevision,
              publishedHash: record.preview.bodyHash,
              recordedComment: record.recordedComment,
              remote: {
                bodyHash: record.recordedComment.bodyHash,
                commentRevision: record.recordedComment.commentRevision,
                issueRevision: record.recordedComment.issueRevision,
              },
            }
            : {},
        );
        return;
      }
      throw publicationError(
        "publication_store_unsupported",
        "Durable publication transactions are required for GitHub comment publication.",
      );
    },
  });
  const previews = new Map();

  function restorePending(workspace) {
    const pending = workspace.publication?.pending;
    if (pending && !service.getPendingAttempt(workspace.issueNumber)) {
      service.restorePendingAttempt(pending);
    }
  }

  async function currentWorkspace(workspace) {
    if (!workspace || !Number.isSafeInteger(workspace.issueNumber)) {
      throw publicationError("workspace_invalid", "A durable issue workspace is required.");
    }
    const current = await store.readWorkspace(workspace.issueNumber);
    if (!current) {
      throw publicationError("workspace_not_found", "The publication workspace no longer exists.");
    }
    if (
      current.draft.revision !== workspace.draft.revision
      || current.draft.hash !== workspace.draft.hash
    ) {
      throw publicationError("stale_revision", "The issue draft changed before publication started.");
    }
    return current;
  }

  async function preview({ issue, workspace }) {
    const current = await currentWorkspace(workspace);
    if (current.publication?.pending) {
      throw publicationError(
        "publication_pending",
        "A publication attempt has an unknown outcome; resolve it before previewing another comment.",
      );
    }
    if (!issue || issue.number !== current.issueNumber) {
      throw publicationError("stale_selection", "The selected issue changed before publication started.");
    }
    const currentIssue = await adapter.readIssue(issue.number);
    const account = await adapter.readAccount();
    const preview = createPublicationPreview({
      issue: currentIssue,
      account,
      report: {
        content: current.draft.content,
        stopPath: current.stopPath,
      },
      draftRevision: current.draft.revision,
      recordedComment: current.publication.recordedComment ?? null,
    });
    previews.set(preview.confirmationToken, preview);
    setTimeout(() => {
      if (previews.get(preview.confirmationToken) === preview) {
        previews.delete(preview.confirmationToken);
      }
    }, Math.max(0, Date.parse(preview.expiresAt) - Date.now())).unref?.();
    return preview;
  }

  async function publish({ workspace, confirmation }) {
    const current = await currentWorkspace(workspace);
    restorePending(current);
    const preview = previews.get(confirmation);
    if (!preview) {
      throw publicationError("confirmation_required", "The publication preview is missing or expired.");
    }
    if (
      preview.draftRevision !== current.draft.revision
      || preview.bodyHash !== hashPublicationBody(current.draft.content)
    ) {
      throw publicationError("stale_revision", "The draft changed after preview; prepare a new preview.");
    }
    const result = await service.publish(preview, confirmation);
    if (result.status !== "published") {
      return {
        ...result,
        workspace: await store.readWorkspace(current.issueNumber),
        workspacePersisted: true,
      };
    }
    previews.delete(confirmation);
    return {
      ...result,
      workspace: await store.readWorkspace(current.issueNumber),
      workspacePersisted: true,
    };
  }

  async function resolvePending({ workspace }) {
    const current = await currentWorkspace(workspace);
    restorePending(current);
    const pending = current.publication?.pending;
    if (!pending) {
      throw publicationError("publication_not_pending", "No publication attempt is pending reconciliation.");
    }
    const result = await service.resolvePending(pending.preview);
    return {
      ...result,
      workspace: await store.readWorkspace(current.issueNumber),
      workspacePersisted: true,
    };
  }

  return { preview, publish, resolvePending };
}

function publicationError(code, message) {
  const error = new Error(message);
  error.code = code;
  return error;
}

export function createGitHubAdapter({ runJson = ghJson } = {}) {
  return {
    async readIssue(number) {
      const issue = await runJson(["api", `repos/${REPOSITORY}/issues/${number}`]);
      const repository = await runJson(["api", `repos/${REPOSITORY}`]);
      return {
        repository: REPOSITORY,
        number: issue.number,
        url: issue.html_url,
        state: issue.state,
        isPullRequest: issue.pull_request !== undefined,
        public: repository.private === false,
        visibility: repository.visibility,
        updatedAt: issue.updated_at,
      };
    },
    async readAccount() {
      const account = await runJson(["api", "user"]);
      return { login: account.login, id: account.id, type: account.type };
    },
    async readComment(query) {
      try {
        const comment = query.id
          ? await runJson(["api", `repos/${REPOSITORY}/issues/comments/${query.id}`])
          : findPendingComment(
            await runJson([
              "api",
              `repos/${REPOSITORY}/issues/${query.issueNumber}/comments?per_page=100&sort=created&direction=asc${query.attemptedAt
                ? `&since=${encodeURIComponent(query.attemptedAt)}`
                : ""}`,
              "--paginate",
              "--slurp",
            ]),
            query,
          );
        return comment ? normalizeComment(comment, query.issueNumber) : null;
      } catch (error) {
        if (error.code === "github_not_found") {
          return null;
        }
        throw error;
      }
    },
    async createComment(input) {
      const comment = await runJson(
        [
          "api",
          `repos/${REPOSITORY}/issues/${input.issueNumber}/comments`,
          "--method",
          "POST",
          "--input",
          "-",
        ],
        { body: JSON.stringify({ body: input.body }), mutating: true },
      );
      return normalizeMutatingComment(comment, input.issueNumber);
    },
    async updateComment(input) {
      const comment = await runJson(
        [
          "api",
          `repos/${REPOSITORY}/issues/comments/${input.commentId}`,
          "--method",
          "PATCH",
          "--input",
          "-",
        ],
        { body: JSON.stringify({ body: input.body }), mutating: true },
      );
      return normalizeMutatingComment(comment, input.issueNumber);
    },
  };
}

function findPendingComment(pages, query) {
  if (
    !query
    || typeof query.bodyHash !== "string"
    || !/^[a-f0-9]{64}$/.test(query.bodyHash)
    || !Number.isSafeInteger(query.accountId)
    || typeof query.attemptedAt !== "string"
    || !Number.isFinite(Date.parse(query.attemptedAt))
  ) {
    return null;
  }
  const candidates = Array.isArray(pages)
    ? pages.flatMap((page) => Array.isArray(page) ? page : [])
    : [];
  const attemptedAt = Date.parse(query.attemptedAt);
  const matches = candidates
    .filter((candidate) =>
      typeof candidate?.body === "string"
      && createHash("sha256").update(candidate.body, "utf8").digest("hex") === query.bodyHash
      && candidate.user?.id === query.accountId
      && Number.isFinite(Date.parse(candidate.created_at))
      && Date.parse(candidate.created_at) > attemptedAt);
  return matches.length === 1 ? matches[0] : null;
}

export function normalizeComment(comment, issueNumber) {
  const actualIssueNumber = parseIssueNumber(comment.issue_url);
  if (actualIssueNumber === null) {
    throw githubError("github_response_invalid", "GitHub did not identify the issue for the comment.");
  }
  if (actualIssueNumber !== issueNumber) {
    throw githubError("github_comment_issue_mismatch", "GitHub comment is bound to a different issue.");
  }
  return {
    id: comment.id,
    repository: REPOSITORY,
    issueNumber: actualIssueNumber,
    author: comment.user?.login,
    authorId: comment.user?.id,
    body: comment.body,
    updatedAt: comment.updated_at,
    html_url: comment.html_url,
    htmlUrl: comment.html_url,
  };
}

function normalizeMutatingComment(comment, issueNumber) {
  try {
    return normalizeComment(comment, issueNumber);
  } catch (error) {
    error.ambiguous = true;
    throw error;
  }
}

export function ghJson(args, input = {}) {
  return runGh(args, input).then((stdout) => {
    try {
      return JSON.parse(stdout);
    } catch (error) {
      const invalid = githubError("github_response_invalid", `GitHub returned invalid JSON: ${error.message}`);
      if (input.mutating) invalid.ambiguous = true;
      throw invalid;
    }
  });
}

export function runGh(args, { body = null, mutating = false, cwd = process.cwd() } = {}) {
  return new Promise((resolve, reject) => {
    const child = spawn("gh", args, {
      cwd,
      env: { ...process.env, GH_PAGER: "cat" },
      stdio: ["pipe", "pipe", "pipe"],
    });
    let stdout = "";
    let stderr = "";
    child.stdout.setEncoding("utf8");
    child.stderr.setEncoding("utf8");
    child.stdout.on("data", (chunk) => { stdout += chunk; });
    child.stderr.on("data", (chunk) => { stderr += chunk; });
    child.once("error", (error) => {
      error.code = "github_transport_failed";
      error.ambiguous = mutating;
      reject(error);
    });
    child.once("close", (code) => {
      if (code === 0) {
        resolve(stdout);
        return;
      }
      const notFound = /\bHTTP 404\b|\b404 Not Found\b/i.test(stderr);
      const error = githubError(
        notFound ? "github_not_found" : "github_request_failed",
        stderr.trim() || `gh exited with code ${code}`,
      );
      error.ambiguous = mutating && !notFound;
      reject(error);
    });
    child.stdin.end(body === null ? undefined : body);
  });
}

function parseIssueNumber(issueUrl) {
  if (typeof issueUrl !== "string") {
    return null;
  }
  const match = /^https:\/\/api\.github\.com\/repos\/dotnet\/aspnetcore\/issues\/([1-9][0-9]*)$/.exec(issueUrl);
  return match ? Number(match[1]) : null;
}

function githubError(code, message) {
  const error = new Error(message);
  error.code = code;
  return error;
}
