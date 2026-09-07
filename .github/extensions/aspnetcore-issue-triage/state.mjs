import { randomUUID } from "node:crypto";
import { AREA_OPTIONS, DEFAULT_AREA, REPOSITORY, normalizeArea } from "./taxonomy.mjs";

export function createTriageController({
  initialArea = DEFAULT_AREA,
  load,
  areaStore,
  createId = randomUUID,
  now = () => new Date().toISOString(),
} = {}) {
  if (typeof load !== "function" || !areaStore) {
    throw new Error("load and areaStore are required");
  }
  let requestedArea = normalizeArea(initialArea);
  let snapshot = null;
  let actions = new Map();
  let selectedItemId = null;
  let refreshPromise = null;
  let refreshArea = null;
  let active = true;
  let generation = 0;
  let revision = 0;
  let handoff = idleHandoff();
  let refresh = { phase: "idle", stale: false, startedAt: null, completedAt: null, error: null };
  const listeners = new Set();

  function publish() {
    if (active) {
      revision++;
      for (const listener of listeners) {
        listener(getState());
      }
    }
  }

  function requireActive() {
    if (!active) {
      throw stateError("controller_disposed", "The issue triage instance is closed.");
    }
  }

  function requireNotRefreshing() {
    requireActive();
    if (refreshPromise) {
      throw stateError("refresh_in_progress", `A ${refreshArea} refresh is already in progress.`);
    }
  }

  function refreshQueue({ area = requestedArea } = {}) {
    requireActive();
    area = normalizeArea(area);
    if (refreshPromise) {
      if (refreshArea !== area) {
        throw stateError("refresh_in_progress", `A ${refreshArea} refresh is already in progress.`);
      }
      return refreshPromise;
    }
    const requestGeneration = ++generation;
    const isCurrent = () => active && generation === requestGeneration;
    refreshArea = area;
    handoff = idleHandoff();
    refresh = { ...refresh, phase: "refreshing", stale: snapshot !== null, startedAt: now(), error: null };
    // Defer the loader until the promise is assigned, including synchronous failures.
    const pending = Promise.resolve().then(() => load({ area })).then(async (queue) => {
      if (!isCurrent()) {
        throw stateError("controller_disposed", "The issue triage refresh is no longer active.");
      }
      if (queue.area !== area) {
        throw stateError("queue_invalid", "The loaded queue does not match the requested area.");
      }
      const built = createSnapshot(queue, createId);
      await areaStore.writeArea(area, isCurrent);
      if (!isCurrent()) {
        throw stateError("controller_disposed", "The issue triage refresh is no longer active.");
      }
      requestedArea = area;
      snapshot = built.public;
      actions = built.actions;
      selectedItemId = null;
      refresh = { ...refresh, phase: "ready", stale: false, completedAt: now(), error: null };
      publish();
      return getState();
    }).catch((error) => {
      if (isCurrent()) {
        refresh = { ...refresh, phase: "error", stale: snapshot !== null, completedAt: now(), error: error.message };
        publish();
      }
      throw error;
    }).finally(() => {
      if (refreshPromise === pending) {
        refreshPromise = null;
        refreshArea = null;
      }
    });
    refreshPromise = pending;
    publish();
    return pending;
  }

  function select({ itemId } = {}) {
    requireNotRefreshing();
    const item = resolve(itemId);
    generation++;
    selectedItemId = item.id;
    handoff = idleHandoff();
    publish();
    return getState();
  }

  async function investigate({ itemId } = {}, dispatch) {
    requireNotRefreshing();
    if (handoff.phase === "sending") {
      throw stateError("handoff_in_progress", "An issue-session request is already being sent.");
    }
    const item = resolve(itemId);
    selectedItemId = item.id;
    const requestGeneration = ++generation;
    const isCurrent = () => active && generation === requestGeneration && selectedItemId === item.id;
    handoff = { phase: "sending", issueNumber: item.number, itemId: item.id, queued: null, messageId: null, error: null };
    publish();
    try {
      const result = await dispatch(item, isCurrent);
      if (isCurrent()) {
        handoff = { ...handoff, phase: result.status, queued: result.queued, messageId: result.messageId, error: result.error ?? null };
        publish();
      }
    } catch (error) {
      if (isCurrent()) {
        handoff = { ...handoff, phase: "failed", error: error.message };
        publish();
      }
      throw error;
    }
    return getState();
  }

  function getPage({ offset = 0, limit = 50 } = {}) {
    requireActive();
    if (!snapshot) {
      throw stateError("queue_unavailable", "No issue queue snapshot is available.");
    }
    if (!Number.isSafeInteger(offset) || offset < 0 || ![25, 50, 100].includes(limit)) {
      throw stateError("invalid_page", "Page offset or limit is invalid.");
    }
    offset = Math.min(offset, snapshot.items.length);
    const items = snapshot.items.slice(offset, offset + limit).map(publicItem);
    return {
      snapshotId: snapshot.id,
      area: snapshot.area,
      total: snapshot.items.length,
      offset,
      limit,
      nextOffset: offset + items.length < snapshot.items.length ? offset + items.length : null,
      items,
    };
  }

  function getState() {
    const { items, ...metadata } = snapshot ?? {};
    return {
      revision,
      requestedArea,
      areaOptions: AREA_OPTIONS,
      refresh: { ...refresh },
      snapshot: snapshot && {
        ...metadata,
        selectedIssue: selectedItemId ? publicItem(actions.get(selectedItemId)) : null,
        handoff: { ...handoff },
      },
    };
  }

  function resolve(itemId) {
    requireActive();
    const item = actions.get(itemId);
    if (!item) {
      throw stateError("stale_selection", "The selected issue is stale. Refresh and try again.");
    }
    return item;
  }

  return {
    initialize: refreshQueue,
    refresh: refreshQueue,
    select,
    investigate,
    getPage,
    getState,
    subscribe(listener) {
      listeners.add(listener);
      return () => listeners.delete(listener);
    },
    dispose() {
      active = false;
      generation++;
      listeners.clear();
    },
  };
}

export function createSnapshot(queue, createId = randomUUID) {
  if (!queue || queue.schemaVersion !== "1.0.0" || queue.repository !== REPOSITORY
    || !Array.isArray(queue.issues) || !queue.coverage) {
    throw stateError("queue_invalid", "Issue queue has an invalid shape.");
  }
  const area = normalizeArea(queue.area);
  const actions = new Map();
  const items = queue.issues.map((issue) => {
    if (!Number.isSafeInteger(issue.number) || issue.number < 1
      || issue.url !== `https://github.com/${REPOSITORY}/issues/${issue.number}`
      || !Array.isArray(issue.labels) || !Array.isArray(issue.whyIncluded)) {
      throw stateError("queue_invalid", "Issue queue contains an invalid issue.");
    }
    const item = { ...issue, id: createId(), repository: REPOSITORY, area };
    if (actions.has(item.id)) {
      throw stateError("queue_invalid", "Issue identifiers must be unique.");
    }
    actions.set(item.id, item);
    return publicItem(item);
  });
  return {
    actions,
    public: {
      id: randomUUID(),
      repository: REPOSITORY,
      area,
      generatedAt: queue.generatedAt,
      predicate: queue.predicate,
      coverage: queue.coverage,
      totalCount: items.length,
      items,
    },
  };
}

export function summarizeState(state) {
  return state;
}

function publicItem(item) {
  return item && { ...item, labels: [...item.labels], whyIncluded: [...item.whyIncluded] };
}

function idleHandoff() {
  return { phase: "idle", issueNumber: null, itemId: null, queued: null, messageId: null, error: null };
}

function stateError(code, message) {
  return Object.assign(new Error(message), { code });
}
