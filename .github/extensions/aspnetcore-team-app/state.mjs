import { randomUUID } from "node:crypto";

import {
  BUCKETS,
  SECONDARY_BUCKETS,
  normalizeOptions,
  validateQueue,
} from "./queue.mjs";
import { orderPersonalItems } from "./personal.mjs";

const CACHE_TTL_MS = 5 * 60 * 1000;

export function createQueueController({
  initialOptions = {},
  load,
  createId = randomUUID,
  now = () => new Date().toISOString(),
  nowMs = () => Date.now(),
} = {}) {
  if (typeof load !== "function") {
    throw new Error("load is required");
  }

  let options = normalizeOptions(initialOptions);
  let snapshot = null;
  let refreshPromise = null;
  let refreshOptions = null;
  let refresh = {
    phase: "idle",
    stale: false,
    cached: false,
    startedAt: null,
    completedAt: null,
    error: null,
  };
  const listeners = new Set();
  const cache = new Map();

  function getCacheKey(requestedOptions) {
    return JSON.stringify({
      source: requestedOptions.source ?? "live",
      preset: requestedOptions.preset ?? "blazor",
      excludeDigestAuthor: requestedOptions.excludeDigestAuthor ?? null,
      identityScope: requestedOptions.identityScope ?? null,
    });
  }

  function getCachedSnapshot(requestedOptions) {
    const key = getCacheKey(requestedOptions);
    const candidate = cache.get(key);
    if (!candidate) {
      return null;
    }
    const ageMs = Math.max(0, nowMs() - candidate.loadedAt);
    if (ageMs >= CACHE_TTL_MS) {
      cache.delete(key);
      return null;
    }
    return { ...candidate, ageMs };
  }

  function initialize(input = {}) {
    return refreshQueue(input, { forceRefresh: false });
  }

  function refreshQueue(input = {}, { forceRefresh = false } = {}) {
    const requestedOptions = normalizeOptions(input, options);
    if (!forceRefresh && !refreshPromise) {
      const cached = getCachedSnapshot(requestedOptions);
      if (cached) {
        options = normalizeOptions(requestedOptions);
        snapshot = cached.snapshot;
        snapshot.cache = {
          hit: true,
          ageMs: cached.ageMs,
          loadedAt: cached.loadedAt,
        };
        snapshot.public.cache = { ...snapshot.cache };
        if (snapshot.public.personalInbox?.metrics) {
          snapshot.public.personalInbox.metrics = {
            ...snapshot.public.personalInbox.metrics,
            cacheMode: "warm",
            apiCalls: 0,
            elapsedMs: 0,
          };
        }
        refresh = {
          phase: "ready",
          stale: false,
          cached: true,
          startedAt: cached.loadedAt,
          completedAt: now(),
          error: null,
        };
        publish();
        return getState();
      }
    }

    if (refreshPromise) {
      if (
        requestedOptions.source !== refreshOptions.source
        || requestedOptions.preset !== refreshOptions.preset
        || requestedOptions.excludeDigestAuthor !== refreshOptions.excludeDigestAuthor
        || requestedOptions.identityScope !== refreshOptions.identityScope
      ) {
        throw stateError(
          "refresh_in_progress",
          `A ${refreshOptions.preset} refresh is already in progress.`,
        );
      }
      return refreshPromise;
    }

    refreshOptions = requestedOptions;
    refresh = {
      ...refresh,
      phase: "refreshing",
      stale: snapshot !== null,
      cached: false,
      startedAt: now(),
      error: null,
    };
    publish();

    refreshPromise = Promise.resolve()
      .then(() => load(requestedOptions))
      .then((loaded) => {
        const effectiveOptions = normalizeOptions({
          ...requestedOptions,
          ...(loaded.options ?? {}),
        });
        const candidate = createSnapshot(
          validateQueue(loaded.queue),
          effectiveOptions,
          createId,
          loaded.personalInbox,
        );
        const loadedAt = nowMs();
        candidate.cache = {
          hit: false,
          ageMs: 0,
          loadedAt,
        };
        candidate.public.cache = { ...candidate.cache };
        cache.set(getCacheKey(effectiveOptions), {
          snapshot: candidate,
          loadedAt,
        });
        options = effectiveOptions;
        snapshot = candidate;
        refresh = {
          phase: "ready",
          stale: false,
          cached: false,
          startedAt: refresh.startedAt,
          completedAt: now(),
          error: null,
        };
        publish();
        return getState();
      })
      .catch((error) => {
        refresh = {
          phase: "error",
          stale: snapshot !== null,
          cached: false,
          startedAt: refresh.startedAt,
          completedAt: refresh.completedAt,
          error: error.message,
        };
        publish();
        throw error;
      })
      .finally(() => {
        refreshPromise = null;
        refreshOptions = null;
      });

    return refreshPromise;
  }

  function getState() {
    if (!snapshot) {
      return {
        options,
        refresh: { ...refresh },
        snapshot: null,
      };
    }

    const publicSnapshot = {
      ...snapshot.public,
      cache: {
        ...(snapshot.cache ?? { hit: false, ageMs: 0, loadedAt: null }),
        ageMs: Number.isFinite(snapshot.cache?.loadedAt)
          ? Math.max(0, nowMs() - snapshot.cache.loadedAt)
          : 0,
      },
    };

    return {
      options,
      refresh: { ...refresh },
      snapshot: publicSnapshot,
    };
  }

  function resolveAction(body) {
    const { itemId, kind } = parseActionRequest(body);
    if (!snapshot) {
      throw stateError("snapshot_unavailable", "No complete queue snapshot is available.");
    }

    const item = snapshot.actions.get(itemId);
    if (!item) {
      throw stateError("stale_item", "This queue item is stale. Refresh and try again.");
    }
    if (
      kind === "review"
      && (
        item.bucket !== "ReviewNow"
        || item.discussionState === "verification-needed"
        || item.discussionState === "not-assessed"
      )
    ) {
      throw stateError("action_not_allowed", "Review is only available for Review now items.");
    }
    if (kind === "investigate-rescue" && item.bucket !== "NeedsRescue") {
      throw stateError(
        "action_not_allowed",
        "Investigate rescue is only available for Needs rescue items.",
      );
    }

    if (options.source === "live" && (kind === "review" || kind === "investigate-rescue")) {
      return (async () => {
        const liveQueue = await load({
          source: "live",
          preset: options.preset,
          ...(options.excludeDigestAuthor ? { excludeDigestAuthor: options.excludeDigestAuthor } : {}),
          ...(options.identityScope ? { identityScope: options.identityScope } : {}),
        });
        const live = validateQueue(liveQueue.queue);
        if (live.repository !== item.repository) {
          throw stateError("action_revalidation_failed", "The pull request repository changed after the snapshot.");
        }
        const liveItem = live.items.find((candidate) => candidate.number === item.number);
        if (!liveItem) {
          throw stateError("action_revalidation_failed", "The pull request no longer matches the live scope.");
        }
        if (liveItem.url !== item.url) {
          throw stateError("action_revalidation_failed", "The pull request changed URLs after the snapshot.");
        }
        if (liveItem.headSha !== item.headSha) {
          throw stateError("action_revalidation_failed", "The pull request head changed after the snapshot.");
        }
        if (
          kind === "review"
          && (
            liveItem.bucket !== "ReviewNow"
            || liveItem.discussionAssessment?.state === "verification-needed"
            || liveItem.discussionAssessment?.state === "not-assessed"
          )
        ) {
          throw stateError("action_revalidation_failed", "The live queue no longer allows a review action.");
        }
        if (kind === "investigate-rescue" && liveItem.bucket !== "NeedsRescue") {
          throw stateError("action_revalidation_failed", "The live queue no longer allows the rescue action.");
        }

        return { kind, item };
      })();
    }

    return { kind, item };
  }

  function subscribe(listener) {
    listeners.add(listener);
    return () => listeners.delete(listener);
  }

  function publish() {
    const state = getState();
    for (const listener of listeners) {
      listener(state);
    }
  }

  return {
    getState,
    initialize,
    refresh: (input = {}, request = {}) => refreshQueue(input, {
      ...request,
      forceRefresh: request.forceRefresh ?? true,
    }),
    resolveAction,
    subscribe,
  };
}

function buildInbox(queue) {
  if (queue.inbox) {
    return queue.inbox;
  }

  return {
    recentCommunityWindowDays: 7,
    recentCommunityWindowStart: queue.generatedAt,
    recentCommunityWindowEnd: queue.generatedAt,
    recentCommunity: {
      count: 0,
      newest: null,
      preview: [],
      inventory: [],
    },
    community: {
      count: 0,
      preview: [],
      inventory: [],
    },
    unclassified: {
      count: 0,
      preview: [],
      inventory: [],
    },
    evidence: {
      collection: "not-collected",
      coverage: "not-collected",
      recordedResponseCount: 0,
      unknownResponseCount: 0,
      noResponseCount: 0,
    },
  };
}

export function normalizePersonalInbox(personalInbox) {
  if (!personalInbox || typeof personalInbox !== "object") {
    return null;
  }

  if (
    Array.isArray(personalInbox.items)
    && personalInbox.coverage
    && typeof personalInbox.coverage === "object"
  ) {
    return {
      ...personalInbox,
      items: orderPersonalItems(deduplicatePersonalItems(personalInbox.items)),
    };
  }

  return {
    schemaVersion: personalInbox.schemaVersion ?? "1.0.0",
    scope: personalInbox.scope ?? {
      name: "all-repo",
      description: "All open dotnet/aspnetcore pull requests with a personal signal",
      repository: "dotnet/aspnetcore",
    },
    identity: personalInbox.identity ?? null,
    generatedAt: personalInbox.generatedAt ?? null,
    coverage: personalInbox.coverage ?? { overall: "unassessed" },
    metrics: personalInbox.metrics ?? {
      cacheMode: "unknown",
      apiCalls: 0,
      elapsedMs: 0,
      pullRequestsScanned: 0,
    },
    items: orderPersonalItems(deduplicatePersonalItems(personalInbox.items)),
  };
}

function deduplicatePersonalItems(items) {
  const uniqueItems = new Map();
  for (const item of Array.isArray(items) ? items : []) {
    if (
      item
      && Number.isInteger(item.number)
      && item.number > 0
      && !uniqueItems.has(item.number)
    ) {
      uniqueItems.set(item.number, item);
    }
  }
  return [...uniqueItems.values()];
}

export function createSnapshot(
  queue,
  options,
  createId = randomUUID,
  personalInbox = null,
) {
  validateQueue(queue);
  const actions = new Map();
  const groups = Object.fromEntries(BUCKETS.map((bucket) => [bucket, []]));

  for (const item of queue.items) {
    const id = createId();
    const publicItem = {
      id,
      number: item.number,
      title: item.title,
      author: item.author,
      bucket: item.bucket,
      bucketDisplay: queue.display.buckets[item.bucket],
      nextActor: item.nextActor,
      reasons: item.reasonCodes.map((code) => ({
        code,
        ...queue.display.reasonCodes[code],
      })),
      blockers: [...item.blockers],
      ageDays: item.ageDays,
      idleDays: item.idleDays,
      changedFiles: item.changedFiles,
      scopeMatch: item.scopeMatch,
      headSha: item.headSha ?? null,
      shownInDigest: item.shownInDigest,
      digestRank: item.digestRank ?? null,
      digestExclusions: (item.digestExclusionReasons ?? []).map((code) => ({
        code,
        ...queue.display.digestExclusionReasons?.[code],
      })),
      stackDepth: item.stackDepth ?? 0,
      stackBlockedBy: [...(item.stackBlockedBy ?? [])],
      shownInDiscussionVerification: item.shownInDiscussionVerification ?? false,
      discussionVerificationRank: item.discussionVerificationRank ?? null,
      discussion: item.discussionAssessment
        ? {
          state: item.discussionAssessment.state,
          complete: item.discussionAssessment.complete,
          signals: item.discussionAssessment.signals.map((code) => ({
            code,
            ...queue.display.discussion.signals[code],
          })),
          commentTotalCount: item.discussionAssessment.commentTotalCount,
          commentEvidenceTruncated: item.discussionAssessment.commentEvidenceTruncated,
          comments: item.discussionAssessment.comments.map((comment) => ({
            ...comment,
            kindDisplay: queue.display.discussion.commentKinds[comment.kind],
          })),
          threads: { ...item.discussionAssessment.threads },
          display: queue.display.discussion.states[item.discussionAssessment.state],
        }
        : null,
    };
    groups[item.bucket].push(publicItem);
    actions.set(id, {
      id,
      repository: queue.repository,
      number: item.number,
      bucket: item.bucket,
      discussionState: item.discussionAssessment?.state ?? null,
      url: `https://github.com/${queue.repository}/pull/${item.number}`,
      headSha: item.headSha ?? null,
    });
  }

  return {
    actions,
    public: {
      schemaVersion: queue.schemaVersion,
      generatedAt: queue.generatedAt,
      repository: queue.repository,
      display: queue.display,
      filter: queue.filter,
      query: queue.query,
      census: queue.census,
      overflow: queue.overflow,
      caps: queue.caps,
      discussion: queue.discussion ?? null,
      inbox: buildInbox(queue),
      personalInbox: personalInbox ? normalizePersonalInbox(personalInbox) : null,
      warnings: [...queue.warnings],
      primary: {
        reviewNow: groups.ReviewNow
          .filter((item) => item.shownInDigest)
          .sort((left, right) =>
            (left.digestRank ?? Number.MAX_SAFE_INTEGER)
            - (right.digestRank ?? Number.MAX_SAFE_INTEGER)),
        needsRescue: groups.NeedsRescue
          .filter((item) => item.shownInDigest)
          .sort((left, right) =>
            (left.digestRank ?? Number.MAX_SAFE_INTEGER)
            - (right.digestRank ?? Number.MAX_SAFE_INTEGER)),
      },
      discussionVerification: groups.ReviewNow
        .filter((item) => item.shownInDiscussionVerification)
        .sort((left, right) => left.discussionVerificationRank - right.discussionVerificationRank),
      readyToMerge: groups.ReadyToMerge
        .filter((item) => item.shownInDigest)
        .sort((left, right) =>
          (left.digestRank ?? Number.MAX_SAFE_INTEGER)
          - (right.digestRank ?? Number.MAX_SAFE_INTEGER)),
      secondary: Object.fromEntries(
        SECONDARY_BUCKETS.map((bucket) => [bucket, groups[bucket]]),
      ),
      overflowItems: {
        ReviewNow: groups.ReviewNow.filter((item) => !item.shownInDigest),
        NeedsRescue: groups.NeedsRescue.filter((item) => !item.shownInDigest),
        ReadyToMerge: groups.ReadyToMerge.filter((item) => !item.shownInDigest),
      },
      options,
    },
  };
}

export function summarizeState(state) {
  if (!state?.snapshot) {
    return {
      refresh: state?.refresh ?? null,
      snapshot: null,
    };
  }

  return {
    refresh: state.refresh,
    schemaVersion: state.snapshot.schemaVersion,
    generatedAt: state.snapshot.generatedAt,
    repository: state.snapshot.repository,
    filter: {
      name: state.snapshot.filter.name,
      description: state.snapshot.filter.description,
      selection: state.snapshot.filter.selection,
      excludeDigestAuthors: state.snapshot.filter.excludeDigestAuthors ?? [],
    },
    query: state.snapshot.query,
    census: state.snapshot.census,
    overflow: state.snapshot.overflow,
    caps: state.snapshot.caps,
    discussion: state.snapshot.discussion,
    personalInbox: state.snapshot.personalInbox,
    warnings: state.snapshot.warnings,
    visibleItems: [
      ...state.snapshot.primary.reviewNow,
      ...state.snapshot.primary.needsRescue,
      ...state.snapshot.readyToMerge,
    ].map((item) => ({
      number: item.number,
      title: item.title,
      author: item.author,
      bucket: item.bucket,
      nextActor: item.nextActor,
      reasonCodes: item.reasons.map((reason) => reason.code),
      blockers: item.blockers,
      ageDays: item.ageDays,
      idleDays: item.idleDays,
      digestRank: item.digestRank,
      discussion: item.discussion,
    })),
  };
}

export function parseActionRequest(body) {
  if (!body || typeof body !== "object" || Array.isArray(body)) {
    throw stateError("invalid_action", "Action request must be an object.");
  }
  const keys = Object.keys(body).sort();
  if (keys.length !== 2 || keys[0] !== "itemId" || keys[1] !== "kind") {
    throw stateError("invalid_action", "Action request accepts only itemId and kind.");
  }
  if (typeof body.itemId !== "string" || !/^[A-Za-z0-9-]{16,64}$/.test(body.itemId)) {
    throw stateError("invalid_action", "itemId is invalid.");
  }
  if (!["open", "review", "investigate-rescue"].includes(body.kind)) {
    throw stateError("invalid_action", "Action kind is invalid.");
  }

  return {
    itemId: body.itemId,
    kind: body.kind,
  };
}

function stateError(code, message) {
  const error = new Error(message);
  error.code = code;
  return error;
}
