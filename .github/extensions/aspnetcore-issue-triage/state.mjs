import { randomUUID } from "node:crypto";

import { AREA_OPTIONS, DEFAULT_AREA, REPOSITORY, normalizeArea } from "./taxonomy.mjs";

const PAGE_SIZES = [25, 50, 100];

export function createTriageController({
  initialArea = DEFAULT_AREA,
  load,
  reportStore,
  createId = randomUUID,
  now = () => new Date().toISOString(),
} = {}) {
  if (typeof load !== "function") {
    throw new Error("load is required");
  }
  if (!reportStore || typeof reportStore.read !== "function" || typeof reportStore.write !== "function") {
    throw new Error("reportStore is required");
  }

  let requestedArea = normalizeArea(initialArea);
  let snapshot = null;
  let actions = new Map();
  let refreshPromise = null;
  let refreshArea = null;
  let selectedItemId = null;
  let selectedReport = null;
  let selectionGeneration = 0;
  let lifecycleGeneration = 0;
  let active = true;
  let investigation = idleInvestigation();
  let refresh = {
    phase: "idle",
    stale: false,
    startedAt: null,
    completedAt: null,
    error: null,
  };
  const listeners = new Set();

  function initialize() {
    return refreshQueue({ area: requestedArea });
  }

  function refreshQueue(input = {}) {
    requireActive();
    const area = normalizeArea(input.area, requestedArea);
    if (["queued", "running"].includes(investigation.phase)) {
      throw stateError(
        "investigation_in_progress",
        `Investigation for ${REPOSITORY}#${investigation.issueNumber} is already in progress.`,
      );
    }
    if (refreshPromise) {
      if (area !== refreshArea) {
        throw stateError(
          "refresh_in_progress",
          `A ${refreshArea} refresh is already in progress.`,
        );
      }
      return refreshPromise;
    }

    refreshArea = area;
    const generation = ++lifecycleGeneration;
    refresh = {
      phase: "refreshing",
      stale: snapshot !== null,
      startedAt: now(),
      completedAt: refresh.completedAt,
      error: null,
    };
    publish();

    refreshPromise = Promise.resolve()
      .then(() => load({ area }))
      .then((loaded) => {
        requireCurrent(generation);
        const built = createSnapshot(loaded, createId);
        return Promise.resolve(reportStore.writeArea?.(
          area,
          () => active && lifecycleGeneration === generation,
        ))
          .then(() => {
            requireCurrent(generation);
            requestedArea = area;
            snapshot = built.public;
            actions = built.actions;
            selectedItemId = null;
            selectedReport = null;
            selectionGeneration += 1;
            investigation = idleInvestigation();
            refresh = {
              phase: "ready",
              stale: false,
              startedAt: refresh.startedAt,
              completedAt: now(),
              error: null,
            };
            publish();
            return getState();
          });
      })
      .catch((error) => {
        if (!active || lifecycleGeneration !== generation) {
          throw error;
        }
        refresh = {
          phase: "error",
          stale: snapshot !== null,
          startedAt: refresh.startedAt,
          completedAt: now(),
          error: error.message,
        };
        publish();
        throw error;
      })
      .finally(() => {
        if (lifecycleGeneration === generation) {
          refreshPromise = null;
          refreshArea = null;
        }
      });

    return refreshPromise;
  }

  function getState() {
    return {
      requestedArea,
      areaOptions: AREA_OPTIONS,
      refresh: { ...refresh },
      snapshot: snapshot
        ? {
          schemaVersion: snapshot.schemaVersion,
          repository: snapshot.repository,
          area: snapshot.area,
          areaOptions: snapshot.areaOptions,
          generatedAt: snapshot.generatedAt,
          predicate: snapshot.predicate,
          coverage: snapshot.coverage,
          totalCount: snapshot.totalCount,
          selectedIssue: selectedItemId ? publicItem(actions.get(selectedItemId)) : null,
          selectedReport,
          investigation: { ...investigation },
        }
        : null,
    };
  }

  function getPage(input = {}) {
    if (!snapshot) {
      throw stateError("queue_unavailable", "No issue queue snapshot is available.");
    }
    const offset = normalizeInteger(input.offset, 0, 0, snapshot.totalCount);
    const limit = normalizePageSize(input.limit);
    const items = snapshot.items.slice(offset, offset + limit);
    return {
      area: snapshot.area,
      generatedAt: snapshot.generatedAt,
      complete: snapshot.coverage.complete,
      total: snapshot.totalCount,
      offset,
      limit,
      nextOffset: offset + items.length < snapshot.totalCount
        ? offset + items.length
        : null,
      items,
    };
  }

  async function select(input) {
    requireActive();
    const item = resolveItem(input?.itemId);
    const generation = ++selectionGeneration;
    selectedItemId = item.id;
    selectedReport = null;
    const report = await reportStore.read(item.number);
    if (active && selectedItemId === item.id && selectionGeneration === generation) {
      selectedReport = report;
      publish();
    }
    return getState();
  }

  function queueInvestigation(input) {
    requireActive();
    if (refreshPromise) {
      throw stateError(
        "refresh_in_progress",
        `A ${refreshArea} refresh is already in progress.`,
      );
    }
    const item = resolveItem(input?.itemId);
    if (["queued", "running"].includes(investigation.phase)) {
      throw stateError(
        "investigation_in_progress",
        `Investigation for ${REPOSITORY}#${investigation.issueNumber} is already in progress.`,
      );
    }
    selectionGeneration += 1;
    selectedItemId = item.id;
    selectedReport = null;
    investigation = {
      phase: "queued",
      issueNumber: item.number,
      itemId: item.id,
      area: item.area,
      queuedAt: now(),
      startedAt: null,
      completedAt: null,
      error: null,
    };
    publish();
    return {
      job: {
        itemId: item.id,
        issueNumber: item.number,
        area: item.area,
        queuedAt: investigation.queuedAt,
      },
      state: getState(),
    };
  }

  function beginInvestigation(job) {
    requireActive();
    const item = resolveJob(job);
    investigation = {
      ...investigation,
      phase: "running",
      startedAt: now(),
      error: null,
    };
    publish();
    return item;
  }

  async function completeInvestigation(job, report) {
    requireActive();
    resolveJob(job);
    const generation = lifecycleGeneration;
    const storedReport = await reportStore.write(
      report,
      () => active && lifecycleGeneration === generation,
    );
    requireCurrent(generation);
    if (selectedItemId === job.itemId) {
      selectionGeneration += 1;
      selectedReport = storedReport;
    }
    investigation = {
      ...investigation,
      phase: "complete",
      completedAt: now(),
      error: null,
    };
    publish();
    return getState();
  }

  function failInvestigation(job, error) {
    if (!active) {
      return getState();
    }
    if (
      investigation.itemId !== job.itemId
      || investigation.issueNumber !== job.issueNumber
      || investigation.area !== job.area
    ) {
      return getState();
    }
    investigation = {
      ...investigation,
      phase: "error",
      completedAt: now(),
      error: error.message,
    };
    publish();
    return getState();
  }

  function resolveJob(job) {
    if (
      !job
      || investigation.itemId !== job.itemId
      || investigation.issueNumber !== job.issueNumber
      || investigation.area !== job.area
    ) {
      throw stateError("stale_selection", "The queued issue selection is stale.");
    }
    return resolveItem(job.itemId);
  }

  function resolveItem(itemId) {
    if (typeof itemId !== "string" || !/^[A-Za-z0-9-]{16,64}$/.test(itemId)) {
      throw stateError("invalid_selection", "itemId is invalid.");
    }
    const item = actions.get(itemId);
    if (!item) {
      throw stateError("stale_selection", "The selected issue is stale. Refresh and try again.");
    }
    return item;
  }

  function subscribe(listener) {
    requireActive();
    listeners.add(listener);
    return () => listeners.delete(listener);
  }

  function publish() {
    if (!active) {
      return;
    }
    const state = getState();
    for (const listener of listeners) {
      listener(state);
    }
  }

  function dispose() {
    if (!active) {
      return;
    }
    active = false;
    lifecycleGeneration += 1;
    selectionGeneration += 1;
    listeners.clear();
  }

  function requireActive() {
    if (!active) {
      throw stateError("controller_disposed", "The issue triage instance is closed.");
    }
  }

  function requireCurrent(generation) {
    if (!active || lifecycleGeneration !== generation) {
      throw stateError("controller_disposed", "The issue triage instance is closed.");
    }
  }

  return {
    beginInvestigation,
    completeInvestigation,
    dispose,
    failInvestigation,
    getPage,
    getState,
    initialize,
    queueInvestigation,
    refresh: refreshQueue,
    select,
    subscribe,
  };
}

export function createSnapshot(queue, createId = randomUUID) {
  validateQueue(queue);
  const actions = new Map();
  const items = queue.issues.map((issue) => {
    const id = createId();
    const item = {
      id,
      repository: REPOSITORY,
      number: issue.number,
      title: issue.title,
      url: issue.url,
      author: issue.author,
      createdAt: issue.createdAt,
      updatedAt: issue.updatedAt,
      labels: [...issue.labels],
      area: queue.area,
      whyIncluded: [...issue.whyIncluded],
    };
    actions.set(id, item);
    return publicItem(item);
  });
  return {
    actions,
    public: {
      schemaVersion: queue.schemaVersion,
      repository: queue.repository,
      area: queue.area,
      areaOptions: queue.areaOptions,
      generatedAt: queue.generatedAt,
      predicate: queue.predicate,
      coverage: queue.coverage,
      totalCount: items.length,
      items,
    },
  };
}

export function summarizeState(state) {
  if (!state?.snapshot) {
    return {
      requestedArea: state?.requestedArea ?? DEFAULT_AREA,
      areaOptions: state?.areaOptions ?? AREA_OPTIONS,
      refresh: state?.refresh ?? null,
      snapshot: null,
    };
  }
  return {
    requestedArea: state.requestedArea,
    areaOptions: state.areaOptions,
    refresh: state.refresh,
    snapshot: {
      repository: state.snapshot.repository,
      area: state.snapshot.area,
      generatedAt: state.snapshot.generatedAt,
      predicate: state.snapshot.predicate,
      coverage: state.snapshot.coverage,
      totalCount: state.snapshot.totalCount,
      selectedIssue: state.snapshot.selectedIssue,
      selectedReport: state.snapshot.selectedReport,
      investigation: state.snapshot.investigation,
    },
  };
}

function validateQueue(queue) {
  if (
    !queue
    || queue.schemaVersion !== "1.0.0"
    || queue.repository !== REPOSITORY
    || typeof queue.generatedAt !== "string"
    || !Array.isArray(queue.issues)
    || !queue.coverage
  ) {
    throw stateError("queue_invalid", "Issue queue has an invalid shape.");
  }
  normalizeArea(queue.area);
}

function publicItem(item) {
  if (!item) {
    return null;
  }
  return {
    id: item.id,
    number: item.number,
    title: item.title,
    url: item.url,
    author: item.author,
    createdAt: item.createdAt,
    updatedAt: item.updatedAt,
    labels: [...item.labels],
    area: item.area,
    whyIncluded: [...item.whyIncluded],
  };
}

function idleInvestigation() {
  return {
    phase: "idle",
    issueNumber: null,
    itemId: null,
    area: null,
    queuedAt: null,
    startedAt: null,
    completedAt: null,
    error: null,
  };
}

function normalizeInteger(value, fallback, min, max) {
  const parsed = Number(value);
  if (!Number.isInteger(parsed)) {
    return fallback;
  }
  return Math.max(min, Math.min(max, parsed));
}

function normalizePageSize(value) {
  const parsed = Number(value);
  return PAGE_SIZES.includes(parsed) ? parsed : 50;
}

function stateError(code, message) {
  const error = new Error(message);
  error.code = code;
  return error;
}
