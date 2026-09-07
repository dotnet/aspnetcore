export function buildAgentActionPrompt(kind, item, { destination } = {}) {
  validateOperationalItem(item);

  if (kind === "review") {
    if (item.bucket !== "ReviewNow") {
      throw actionError("action_not_allowed", "Review requires a Review now item.");
    }
    return buildReviewPrompt(item, destination ?? "new-session");
  }

  if (kind === "investigate-rescue") {
    if (item.bucket !== "NeedsRescue") {
      throw actionError("action_not_allowed", "Investigate rescue requires a Needs rescue item.");
    }

    return `Open a NEW pull-request session for ${item.repository}#${item.number}.

Use the open_pr_session tool with repo_full_name "${item.repository}", pr_number ${item.number}, and an autopilot kickoff containing these instructions:

Perform a READ-ONLY rescue investigation for ${item.repository}#${item.number}. Fetch the current pull request history, linked issue, human reviews and review requests, checks, mergeability, labels, ownership signals, and blockers. Recommend exactly one next path: review now, request author follow-up, restore maintainer ownership, ask the author to rebase, or close as no longer actionable. Support the recommendation with current evidence. Do not comment, label, assign, close, merge, edit files, commit, or push.`;
  }

  throw actionError("invalid_action", `Unsupported agent action: ${kind}`);
}

export function buildAgentActionLog(kind, item, { destination } = {}) {
  validateOperationalItem(item);
  if (kind === "review") {
    return buildReviewLog(item, destination ?? "new-session");
  }
  if (kind === "investigate-rescue") {
    return `Open read-only rescue investigation for ${item.repository}#${item.number}`;
  }
  throw actionError("invalid_action", `Unsupported agent action: ${kind}`);
}

export function buildReviewPrompt(item, destination = "new-session") {
  validateReviewItem(item);
  const headSha = item.headSha;
  const scope = `${item.repository}#${item.number}`;

  if (destination === "this-session") {
    return [
      `Review ${scope} in this session (${item.url}).`,
      "",
      `The current PR head SHA is ${headSha}. Review the complete diff in repository context. Use this session's existing tools and workspace, but do not open a child PR session, do not change checkout, do not rebase, and do not edit files. If the current checkout differs from this head SHA, read the target PR remotely rather than changing checkout.`,
      "",
      commonReviewInstructions(),
      "",
      `You may write review artifacts only in the session-state files directory; do not edit repository files.`,
      `Report only high-confidence correctness, security, reliability, or test-coverage findings with precise file and line evidence.`,
      `Report the reviewed head SHA when identifiable; if it cannot be identified, report a setup blocker.`,
      `Report the skill source/revision when identifiable; state when unavailable.`,
      `Do not post or submit a GitHub review.`,
      `Do not comment, approve, request changes, label, assign, close, merge, stage review comments, change statuses or branches, edit files, commit, or push.`,
    ].join("\n");
  }

  if (destination !== "new-session") {
    throw actionError("invalid_destination", `Unsupported review destination: ${String(destination)}`);
  }

  return [
    `Open a NEW pull-request session for ${scope} (${item.url}).`,
    "",
    `Before calling open_pr_session, copy any applicable explicit model/provider restrictions already available in your instructions into the actual kickoff.prompt you pass. If a known restriction cannot be carried forward or honored, stop and report a setup blocker. Do not invent restrictions or hardcode model names.`,
    "",
    `Use the open_pr_session tool with repo_full_name "${item.repository}", pr_number ${item.number}, and an autopilot kickoff containing these instructions:`,
    "",
    `The current PR head SHA is ${headSha}. Review the complete diff in repository context. Perform a thorough READ-ONLY code review of ${scope} against that head SHA. Fetch the current pull request and review its complete diff in repository context.`,
    "",
    commonReviewInstructions(),
    "",
    `You may write review artifacts only in the session-state files directory; do not edit repository files.`,
    `Report only high-confidence correctness, security, reliability, or test-coverage findings with precise file and line evidence.`,
    `Report the reviewed head SHA when identifiable; if it cannot be identified, report a setup blocker.`,
    `Report the skill source/revision when identifiable; state when unavailable.`,
    `Do not post or submit a GitHub review.`,
    `Do not comment, approve, request changes, label, assign, close, merge, stage review comments, change statuses or branches, edit files, commit, or push.`,
  ].join("\n");
}

export function buildReviewLog(item, destination = "new-session") {
  validateReviewItem(item);
  if (destination === "this-session") {
    return `Review in this session for ${item.repository}#${item.number}`;
  }
  if (destination === "new-session") {
    return `Review in new session for ${item.repository}#${item.number}`;
  }
  throw actionError("invalid_destination", `Unsupported review destination: ${String(destination)}`);
}

function commonReviewInstructions() {
  return [
    `Locate and invoke the intended review-pull-request skill from already-available session, user, plugin, project, or target-checkout skill mechanisms. Use the exposed skill invocation tool when one is registered; otherwise follow the supported available-skill mechanism, without inventing a new API. If the intended skill is unavailable or incompatible with the read-only/model restrictions, stop and report a setup blocker rather than substituting a generic review workflow or installing tools.`,
    `Preserve any applicable model/provider restrictions when selecting workers.`,
    `Keep the session source-only: do not execute the target PR code, builds, or tests.`,
    `Do not install, copy, or fetch a hardcoded remote skill.`,
  ].join("\n");
}

function validateOperationalItem(item) {
  if (
    !item
    || typeof item.repository !== "string"
    || !/^[A-Za-z0-9_.-]+\/[A-Za-z0-9_.-]+$/.test(item.repository)
    || !Number.isInteger(item.number)
    || item.number < 1
    || !["ReviewNow", "NeedsRescue", "ReadyToMerge", "WaitingOnAuthor", "WaitingOnCI",
      "DesignDecision", "Draft", "Excluded"].includes(item.bucket)
  ) {
    throw actionError("invalid_item", "Resolved queue item is invalid.");
  }
}

function validateReviewItem(item) {
  validateOperationalItem(item);
  if (typeof item.headSha !== "string" || item.headSha.length === 0) {
    throw actionError("invalid_item", "Resolved review item is missing a head SHA.");
  }
}

function actionError(code, message) {
  const error = new Error(message);
  error.code = code;
  return error;
}
