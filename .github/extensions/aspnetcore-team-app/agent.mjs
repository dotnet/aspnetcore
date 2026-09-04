export function buildAgentActionPrompt(kind, item) {
  validateOperationalItem(item);

  if (kind === "review") {
    if (item.bucket !== "ReviewNow") {
      throw actionError("action_not_allowed", "Review requires a Review now item.");
    }

    return `Open a NEW pull-request session for ${item.repository}#${item.number}.

Use the open_pr_session tool with repo_full_name "${item.repository}", pr_number ${item.number}, and an autopilot kickoff containing these instructions:

Perform a thorough READ-ONLY code review of ${item.repository}#${item.number}. Fetch the current pull request and review its complete diff in repository context. Report only high-confidence correctness, security, reliability, or test-coverage findings with precise file and line evidence. Do not post or submit a GitHub review. Do not comment, approve, request changes, label, assign, close, merge, edit files, commit, or push.`;
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
