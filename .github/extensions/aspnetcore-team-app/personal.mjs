const DEFAULT_SCOPE = {
  name: "all-repo",
  description: "All open dotnet/aspnetcore pull requests with a personal signal",
  repository: "dotnet/aspnetcore",
};

export function normalizePersonalInbox(personal, queue, {
  cacheMode = "cold",
} = {}) {
  if (!personal || typeof personal !== "object" || personal.enabled !== true) {
    return createUnavailablePersonalInbox(
      queue?.repository ?? DEFAULT_SCOPE.repository,
      queue?.generatedAt ?? null,
    );
  }

  const rawItems = Array.isArray(personal.inventory)
    ? personal.inventory
    : Array.isArray(personal.preview)
      ? personal.preview
      : [];
  const items = orderPersonalItems(
    deduplicate(rawItems.map((item) => normalizePersonalItem(item))),
  );
  const activeItems = items.filter((item) => item.hasActionablePersonalSignal);
  const previewItems = orderPersonalItems(
    deduplicate(
      (Array.isArray(personal.preview) && personal.preview.length > 0
        ? personal.preview
        : activeItems
      ).map((item) => item.actionStatus ? item : normalizePersonalItem(item)),
    ),
  ).filter((item) => item.hasActionablePersonalSignal).slice(0, 5);
  const coverage = personal.coverage ?? {};
  const discovery = normalizeCoverage(coverage.discovery);
  const notifications = normalizeCoverage(coverage.notifications);
  const ownReview = normalizeCoverage(coverage.ownReview);
  const reviewThreads = normalizeCoverage(coverage.reviewThreads);
  const overall = normalizeCoverageState(coverage.state);
  const metrics = personal.metrics ?? {};
  const timing = queue?.timing ?? {};

  return {
    schemaVersion: "1.0.0",
    scope: {
      ...DEFAULT_SCOPE,
      name: personal.scope || DEFAULT_SCOPE.name,
      repository: queue?.repository ?? DEFAULT_SCOPE.repository,
    },
    identity: personal.login ?? null,
    activeCount: activeItems.length,
    generatedAt: queue?.generatedAt ?? null,
    previewItems,
    coverage: {
      overall,
      pullRequests: discovery.state,
      notifications: notifications.state,
      detail: overall,
      discovery,
      ownReview,
      reviewThreads,
    },
    metrics: {
      cacheMode: metrics.cacheMode ?? cacheMode,
      apiCalls: Number.isInteger(metrics.apiCalls)
        ? metrics.apiCalls
        : Number.isInteger(timing.personalApiCalls)
          ? timing.personalApiCalls
          : null,
      elapsedMs: Number.isInteger(metrics.elapsedMs)
        ? metrics.elapsedMs
        : Number.isInteger(timing.personalMs)
          ? timing.personalMs
          : null,
      personalMs: Number.isInteger(metrics.personalMs)
        ? metrics.personalMs
        : Number.isInteger(timing.personalMs)
          ? timing.personalMs
          : null,
      notification: personal.notificationMetrics ?? null,
      pullRequestsScanned: Number.isInteger(metrics.pullRequestsScanned)
        ? metrics.pullRequestsScanned
        : null,
    },
    items,
  };
}

export function createUnavailablePersonalInbox(repository = DEFAULT_SCOPE.repository, generatedAt = null) {
  return {
    schemaVersion: "1.0.0",
    scope: { ...DEFAULT_SCOPE, repository },
    identity: null,
    generatedAt,
    coverage: {
      overall: "unavailable",
      pullRequests: "unavailable",
      notifications: "unavailable",
      detail: "unavailable",
      discovery: normalizeCoverage({ state: "unavailable", detail: "The skill did not provide personal evidence." }),
      ownReview: normalizeCoverage({ state: "unavailable", detail: "The skill did not provide personal evidence." }),
      reviewThreads: normalizeCoverage({ state: "unavailable", detail: "The skill did not provide personal evidence." }),
    },
    metrics: {
      cacheMode: "cold",
      apiCalls: null,
      elapsedMs: null,
      personalMs: null,
      pullRequestsScanned: null,
    },
    items: [],
  };
}

export function orderPersonalItems(items) {
  return [...items].sort((left, right) => {
    const actionRank = personalActionRank(left) - personalActionRank(right);
    if (actionRank !== 0) {
      return actionRank;
    }

    const eventRank = compareDates(firstSignalDate(right), firstSignalDate(left));
    return eventRank || right.number - left.number;
  });
}

export function getPersonalDisplayModel(personalInbox) {
  const items = Array.isArray(personalInbox?.items) ? personalInbox.items : [];
  const previewItems = Array.isArray(personalInbox?.previewItems)
    ? personalInbox.previewItems
    : items.filter((item) => item.hasActionablePersonalSignal ?? item.hasPersonalSignal).slice(0, 5);
  const repository = personalInbox?.scope?.repository ?? DEFAULT_SCOPE.repository;
  return {
    scopeLabel: `All ${repository}`,
    activeCount: personalInbox?.activeCount ?? previewItems.length,
    previewItems,
    inventoryItems: items,
    inventoryCollapsedByDefault: true,
  };
}

export function createPersonalCacheKey({
  identity,
  repository = DEFAULT_SCOPE.repository,
  query = "personal",
} = {}) {
  return JSON.stringify({ identity: identity ?? null, repository, query });
}

export function reuseConditionalNotificationRepresentation({
  cache,
  key,
  response,
} = {}) {
  if (!cache || typeof cache !== "object" || typeof key !== "string") {
    throw new Error("A notification cache and stable key are required.");
  }
  if (response?.status === 304) {
    const cached = cache[key];
    if (!cached) {
      throw new Error("Notification response was 304 without a matching cached representation.");
    }
    return cached;
  }
  if (response?.status !== 200 || response.body === undefined) {
    throw new Error("Notification response did not contain a reusable representation.");
  }
  cache[key] = {
    body: response.body,
    etag: response.etag ?? null,
    lastModified: response.lastModified ?? null,
  };
  return cache[key];
}

function normalizePersonalItem(item) {
  const signals = Array.isArray(item?.signals) ? item.signals : [];
  const directRequest = item?.directRequest === true
    || signals.some((signal) => signal.kind === "direct-request");
  const notificationSignals = signals.filter((signal) => signal.kind === "follow-up-notification");
  const changedSignal = signals.find((signal) => signal.kind === "changed-since-own-review");
  const replySignals = signals.filter((signal) => signal.kind === "review-thread-reply");
  const latestReview = item?.latestOwnReview ?? null;
  const ownReviewCoverage = normalizeCoverage(item?.coverage?.ownReview);
  let changedSinceOwnReview;
  if (changedSignal) {
    changedSinceOwnReview = {
      status: "yes",
      reviewAt: latestReview?.submittedAt ?? null,
      reviewCommitOid: changedSignal.baselineCommit ?? latestReview?.commitOid ?? null,
      headSha: changedSignal.currentHead ?? item.headSha ?? null,
      evidenceUrl: changedSignal.evidenceUrl ?? latestReview?.url ?? item.url,
    };
  } else if (latestReview?.commitOid && item?.headSha) {
    changedSinceOwnReview = {
      status: latestReview.commitOid === item.headSha ? "no" : "unassessed",
      reviewAt: latestReview.submittedAt ?? null,
      reviewCommitOid: latestReview.commitOid,
      headSha: item.headSha,
      evidenceUrl: latestReview.url ?? item.url,
    };
  } else {
    changedSinceOwnReview = {
      status: ownReviewCoverage.state === "unavailable" ? "unavailable" : "unassessed",
      reviewAt: latestReview?.submittedAt ?? null,
      reviewCommitOid: latestReview?.commitOid ?? null,
      headSha: item?.headSha ?? null,
      evidenceUrl: latestReview?.url ?? item?.url,
    };
  }

  const threadCoverage = normalizeCoverage(item?.coverage?.reviewThreads);
  const replyEvidence = {
    status: replySignals.length
      ? "evidenced"
      : threadCoverage.state === "assessed"
        ? "none"
        : threadCoverage.state,
    replies: replySignals.map((signal) => ({
      author: signal.responder ?? "unknown",
      createdAt: signal.eventAt ?? null,
      url: signal.evidenceUrl ?? item.url,
      excerpt: signal.detail ?? "",
      threadId: signal.threadId ?? null,
      resolved: signal.resolved ?? null,
    })),
  };
  const actionStatus = classifyPersonalAction({
    directRequest,
    notificationSignals,
    replyEvidence,
    changedSinceOwnReview,
    participatedOrMentioned: item.participatedOrMentioned === true,
    queue: {
      bucket: item.bucket ?? "Unknown",
      nextActor: item.nextActor ?? "unknown",
    },
  });

  return {
    number: item.number,
    title: item.title,
    url: item.url,
    author: item.author ?? "unknown",
    authorIsBot: typeof item.author === "string" && item.author.endsWith("[bot]"),
    updatedAt: item.updatedAt ?? item.createdAt ?? null,
    headSha: item.headSha ?? null,
    headCommitAt: null,
    directRequests: directRequest ? [{ login: null }] : [],
    teamRequests: [],
    otherDirectRequests: [],
    notificationSignal: {
      present: notificationSignals.length > 0,
      reasons: notificationSignals.map((signal) => signal.reason).filter(Boolean),
      updatedAt: notificationSignals[0]?.eventAt ?? null,
      items: notificationSignals.map((signal) => ({
        reason: signal.reason ?? "unknown",
        updatedAt: signal.eventAt ?? null,
        url: signal.evidenceUrl ?? item.url,
        unread: signal.unread === true,
      })),
    },
    changedSinceOwnReview,
    participation: {
      reviews: latestReview ? [latestReview] : [],
      comments: [],
      reviewThreads: replySignals.map((signal) => ({ threadId: signal.threadId ?? null })),
      mentions: [],
      participatedOrMentioned: item.participatedOrMentioned === true,
    },
    replyEvidence,
    coverage: {
      overall: normalizeCoverageState(item?.coverage?.state),
      reviewRequests: directRequest ? "assessed" : "unassessed",
      reviewHistory: ownReviewCoverage.state,
      comments: item?.participatedOrMentioned ? "partial" : "unassessed",
      threads: threadCoverage.state,
      notifications: normalizeCoverage(item?.coverage?.notifications).state,
      discovery: normalizeCoverage(item?.coverage?.discovery).state,
      details: {
        discovery: normalizeCoverage(item?.coverage?.discovery),
        notifications: normalizeCoverage(item?.coverage?.notifications),
        ownReview: ownReviewCoverage,
        reviewThreads: threadCoverage,
      },
    },
    eligibility: getEligibility(item),
    inQueueScope: item.generalScope !== "personal-only",
    queue: {
      bucket: item.bucket ?? "Unknown",
      nextActor: item.nextActor ?? "unknown",
      shownInDigest: item.digestVisible === true,
      digestRank: item.digestRank ?? null,
    },
    actionStatus,
    blockers: Array.isArray(item.blockers) ? item.blockers : [],
    signals,
    hasPersonalSignal: signals.length > 0,
    hasActionablePersonalSignal: actionStatus.priority < 4,
  };
}

function getEligibility(item) {
  if (typeof item.author === "string" && item.author.endsWith("[bot]")) {
    return { eligibleForCanvasAction: false, reason: "bot-authored" };
  }
  if (item.generalScope === "personal-only" || item.bucket === "OutOfScope") {
    return { eligibleForCanvasAction: false, reason: "out-of-scope" };
  }
  if (item.bucket !== "ReviewNow" || item.digestVisible !== true) {
    return { eligibleForCanvasAction: false, reason: "general-queue-eligibility-required" };
  }
  return { eligibleForCanvasAction: true, reason: "general-queue-eligible" };
}

function deduplicate(items) {
  const unique = new Map();
  for (const item of items) {
    if (item && Number.isInteger(item.number) && item.number > 0 && !unique.has(item.number)) {
      unique.set(item.number, item);
    }
  }
  return [...unique.values()];
}

function normalizeCoverage(coverage) {
  if (typeof coverage === "string") {
    const normalized = coverage.toLowerCase();
    const state = normalized.includes("unavailable")
      ? "unavailable"
      : normalized.includes("partial") || normalized.includes("bounded")
        ? "partial"
        : normalized.includes("assessed") || normalized.includes("succeeded")
          ? "assessed"
          : "unassessed";
    return { state, detail: coverage };
  }
  if (!coverage || typeof coverage !== "object") {
    return { state: "unassessed", detail: "The skill did not provide this evidence." };
  }
  const state = normalizeCoverageState(coverage.state);
  return { state, detail: coverage.detail ?? "" };
}

function normalizeCoverageState(state) {
  return ["assessed", "unassessed", "partial", "unavailable"].includes(state)
    ? state
    : "unassessed";
}

function classifyPersonalAction(item) {
  const isReviewReady = item.queue?.bucket === "ReviewNow"
    && item.queue?.nextActor === "human reviewer";
  if (!isReviewReady) {
    const queueDetail = `Current queue: ${item.queue?.bucket ?? "Unknown"}`
      + ` | next actor: ${item.queue?.nextActor ?? "unknown"}.`;
    if (item.directRequest) {
      return {
        priority: 4,
        label: "Review request present — PR not ready",
        detail: queueDetail,
      };
    }
    if (item.notificationSignals?.some((notification) => notification.unread)
      || item.replyEvidence?.replies?.length
      || item.changedSinceOwnReview?.status === "yes") {
      return {
        priority: 4,
        label: "Follow-up present — no action now",
        detail: queueDetail,
      };
    }
  }

  if (item.directRequest) {
    return {
      priority: 0,
      label: "Needs your review",
      detail: "Explicit review request.",
    };
  }

  const hasReplyEvidence = item.replyEvidence?.replies?.some((reply) => reply.resolved !== true) === true;
  const hasUnreadNotification = item.notificationSignals?.some((notification) => notification.unread) === true;
  const hasParticipationSignal = item.participatedOrMentioned === true;

  if (hasReplyEvidence || (hasUnreadNotification && hasParticipationSignal)) {
    return {
      priority: 1,
      label: "Reply or inspect discussion",
      detail: hasReplyEvidence
        ? "Unresolved discussion needs a response."
        : "Unread activity is tied to your participation or mention.",
    };
  }

  if (hasUnreadNotification) {
    return {
      priority: 2,
      label: "Needs attention",
      detail: "Unread follow-up notification.",
    };
  }

  if (item.changedSinceOwnReview?.status === "yes") {
    return {
      priority: 3,
      label: "New changes since your review",
      detail: "The current head differs from your last submitted review.",
    };
  }

  return {
    priority: 4,
    label: "No action currently needed",
    detail: item.queue?.bucket === "ReviewNow"
      ? `Broader queue next actor: ${item.queue.nextActor}.`
      : "Informational personal feed item.",
  };
}

function personalActionRank(item) {
  return Number.isInteger(item?.actionStatus?.priority)
    ? item.actionStatus.priority
    : 4;
}

function firstSignalDate(item) {
  return item.signals?.[0]?.eventAt ?? item.updatedAt ?? null;
}

function compareDates(left, right) {
  return new Date(left ?? 0).getTime() - new Date(right ?? 0).getTime();
}
