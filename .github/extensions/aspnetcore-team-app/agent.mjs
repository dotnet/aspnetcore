export function buildAgentActionPrompt(kind, item) {
  validateOperationalItem(item);

  if (kind === "review") {
    if (item.bucket !== "ReviewNow") {
      throw actionError("action_not_allowed", "Review requires a Review now item.");
    }

    return `Open a NEW pull-request session for ${item.repository}#${item.number}.

Before calling open_pr_session, copy any applicable explicit model/provider restrictions already available in your instructions into the actual kickoff.prompt you pass. If a known restriction cannot be carried forward or honored, stop and report a setup blocker. Do not invent restrictions or hardcode model names.

Use the open_pr_session tool with repo_full_name "${item.repository}", pr_number ${item.number}, and an autopilot kickoff containing these instructions:

Perform a thorough READ-ONLY code review of ${item.repository}#${item.number}. Fetch the current pull request and review its complete diff in repository context. Locate and invoke the intended review-pull-request skill from already-available session, user, plugin, project, or target-checkout skill mechanisms. Use the exposed skill invocation tool when one is registered; otherwise follow the supported available-skill mechanism, without inventing a new API. If the intended skill is unavailable or incompatible with the read-only/model restrictions, stop and report a setup blocker rather than substituting a generic review workflow or installing tools. Preserve the copied model/provider restrictions when selecting workers. Keep the child source-only: do not execute the target PR code, builds, or tests. Do not install, copy, or fetch a hardcoded remote skill. You may write review artifacts only in the session-state files directory; do not edit repository files. Report only high-confidence correctness, security, reliability, or test-coverage findings with precise file and line evidence. Report the reviewed head SHA when identifiable; if it cannot be identified, report a setup blocker. Report the skill source/revision when identifiable; state when unavailable. Do not post or submit a GitHub review. Do not comment, approve, request changes, label, assign, close, merge, stage review comments, change statuses or branches, edit files, commit, or push.`;
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

export function buildAgentActionLog(kind, item) {
  validateOperationalItem(item);
  if (kind === "review") {
    return `Open read-only review session for ${item.repository}#${item.number}`;
  }
  if (kind === "investigate-rescue") {
    return `Open read-only rescue investigation for ${item.repository}#${item.number}`;
  }
  throw actionError("invalid_action", `Unsupported agent action: ${kind}`);
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

function actionError(code, message) {
  const error = new Error(message);
  error.code = code;
  return error;
}
