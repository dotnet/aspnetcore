import { createHash } from "node:crypto";
import { readFile } from "node:fs/promises";
import { homedir } from "node:os";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { CopilotClient } from "@github/copilot-sdk";
import { CanvasError, createCanvas, joinSession } from "@github/copilot-sdk/extension";

import {
  buildIsolatedSystemMessage,
  createPinnedSourceTracker,
  createInvestigationTools,
  validateIsolatedEvents,
} from "./agent.mjs";
import {
  buildInvestigationPrompt,
  createStoredReport,
  getConfiguredInvestigationModel,
  INVESTIGATION_MODEL_ENVIRONMENT_VARIABLE,
  validateLiveQueueIssue,
  verifyInvestigationSource,
} from "./investigation.mjs";
import {
  abortIsolatedSession,
  cleanupIsolatedResources,
} from "./isolation.mjs";
import { loadIssueQueue } from "./queue.mjs";
import {
  configureServer,
  getInstancePage,
  getInstanceState,
  performInstanceInvestigation,
  queueInstanceInvestigation,
  refreshInstance,
  selectInstanceIssue,
  startInstance,
  stopInstance,
} from "./server.mjs";
import { createReportStore } from "./storage.mjs";
import { summarizeState } from "./state.mjs";
import { AREA_OPTIONS, DEFAULT_AREA } from "./taxonomy.mjs";

const AREA_ENUM = AREA_OPTIONS.map((option) => option.label);
const repositoryRoot = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const skillPath = resolve(repositoryRoot, ".github/skills/investigate-issue/SKILL.md");
const pendingInvestigations = [];
let investigationRunning = false;

const session = await joinSession({
  canvases: [
    createCanvas({
      id: "aspnetcore-issue-triage",
      displayName: "ASP.NET Core Issue Triage",
      description:
        "Read-only complete ASP.NET Core area issue queues with one-issue project-skill investigation.",
      inputSchema: {
        type: "object",
        properties: {
          area: {
            type: "string",
            enum: AREA_ENUM,
            description: `Public ASP.NET Core area label. Defaults to ${DEFAULT_AREA}.`,
          },
        },
        additionalProperties: false,
      },
      actions: [
        {
          name: "refresh",
          description: "Refresh the entire selected area queue from public GitHub data.",
          inputSchema: {
            type: "object",
            properties: {
              area: { type: "string", enum: AREA_ENUM },
            },
            required: ["area"],
            additionalProperties: false,
          },
          handler: async (ctx) => {
            try {
              return summarizeState(await refreshInstance(ctx.instanceId, ctx.input));
            } catch (error) {
              throw new CanvasError(error.code ?? "queue_refresh_failed", error.message);
            }
          },
        },
        {
          name: "summary",
          description: "Return queue counts, completeness, predicate, selection, and investigation status.",
          inputSchema: {
            type: "object",
            properties: {},
            additionalProperties: false,
          },
          handler: (ctx) => {
            const state = getInstanceState(ctx.instanceId);
            if (!state) {
              throw new CanvasError("queue_not_open", "Open the ASP.NET Core Issue Triage canvas first.");
            }
            return summarizeState(state);
          },
        },
        {
          name: "page",
          description: "Return one explicit page from the complete selected queue without silent truncation.",
          inputSchema: {
            type: "object",
            properties: {
              offset: { type: "integer", minimum: 0 },
              limit: { type: "integer", enum: [25, 50, 100] },
            },
            required: ["offset", "limit"],
            additionalProperties: false,
          },
          handler: (ctx) => {
            try {
              return getInstancePage(ctx.instanceId, ctx.input);
            } catch (error) {
              throw new CanvasError(error.code ?? "queue_page_failed", error.message);
            }
          },
        },
        {
          name: "select",
          description: "Select one server-owned issue item and load any durable advisory report.",
          inputSchema: {
            type: "object",
            properties: {
              itemId: { type: "string", pattern: "^[A-Za-z0-9-]{16,64}$" },
            },
            required: ["itemId"],
            additionalProperties: false,
          },
          handler: async (ctx) => {
            try {
              return summarizeState(await selectInstanceIssue(ctx.instanceId, ctx.input));
            } catch (error) {
              throw new CanvasError(error.code ?? "queue_select_failed", error.message);
            }
          },
        },
        {
          name: "investigate",
          description:
            "Queue the exact project investigate-issue skill for one revalidated server-owned issue.",
          inputSchema: {
            type: "object",
            properties: {
              itemId: { type: "string", pattern: "^[A-Za-z0-9-]{16,64}$" },
            },
            required: ["itemId"],
            additionalProperties: false,
          },
          handler: (ctx) => {
            try {
              return summarizeState(queueInstanceInvestigation(ctx.instanceId, ctx.input));
            } catch (error) {
              throw new CanvasError(error.code ?? "investigation_queue_failed", error.message);
            }
          },
        },
      ],
      open: async (ctx) => {
        try {
          const entry = await startInstance(
            ctx.instanceId,
            ctx.input ?? {},
            (message) => session.log(message, { level: "warning", ephemeral: true }),
          );
          return {
            title: "ASP.NET Core Issue Triage",
            status: "Read-only public GitHub issue queue",
            url: entry.url,
          };
        } catch (error) {
          throw new CanvasError(error.code ?? "queue_open_failed", error.message);
        }
      },
      onClose: async (ctx) => {
        await stopInstance(ctx.instanceId);
      },
    }),
  ],
});

const reportStore = createReportStore(session.workspacePath, repositoryRoot);
configureServer({
  load: loadIssueQueue,
  store: reportStore,
  scheduleInvestigation: (job) => {
    pendingInvestigations.push(job);
    queueMicrotask(drainInvestigations);
  },
});

async function drainInvestigations() {
  if (investigationRunning || pendingInvestigations.length === 0) {
    return;
  }
  const queued = pendingInvestigations.shift();
  investigationRunning = true;
  try {
    await performInstanceInvestigation(queued.instanceId, queued.job, async (item) => {
      await validateLiveQueueIssue(item);
      return runIsolatedInvestigation(item);
    });
  } catch (error) {
    await session.log(
      `ASP.NET Core issue triage investigation failed: ${error.message}`,
      { level: "error", ephemeral: true },
    );
  } finally {
    investigationRunning = false;
    queueMicrotask(drainInvestigations);
  }
}

async function runIsolatedInvestigation(item) {
  const configuredModel = process.env[INVESTIGATION_MODEL_ENVIRONMENT_VARIABLE] !== undefined
    ? getConfiguredInvestigationModel(process.env)
    : getConfiguredInvestigationModel(process.env, await reportStore.readModel());
  const diagnosticRun = reportStore.createDiagnosticCapture().createRun(item.number);
  const skillContent = await readFile(skillPath, "utf8");
  const skillDigest = createHash("sha256").update(skillContent).digest("hex");
  const sourceTracker = createPinnedSourceTracker();
  const tools = createInvestigationTools(repositoryRoot, { sourceTracker });
  const allowedToolNames = tools.map((tool) => tool.name);
  const client = new CopilotClient({
    mode: "empty",
    workingDirectory: repositoryRoot,
    baseDirectory: process.env.COPILOT_HOME ?? join(homedir(), ".copilot"),
    clientInfo: {
      applicationName: "ASP.NET Core Issue Triage Canvas",
      integrationName: "isolated-issue-investigation",
    },
  });
  let isolatedSession;
  let response = null;
  let events = null;
  let investigationError = null;
  let report = null;

  async function captureDiagnostics(phase, error = null) {
    if (!(await diagnosticRun.isEnabled())) {
      return;
    }
    let eventError = null;
    if (events === null && isolatedSession) {
      try {
        events = await isolatedSession.getEvents();
      } catch (getEventsError) {
        eventError = getEventsError;
        events = [];
      }
    }
    await diagnosticRun.capture({
      configuredModel,
      response,
      events: events ?? [],
      error: error ?? eventError,
      phase,
    });
  }

  try {
    isolatedSession = await client.createSession({
      clientName: "aspnetcore-issue-triage",
      model: configuredModel,
      reasoningEffort: "high",
      workingDirectory: repositoryRoot,
      enableConfigDiscovery: false,
      skipEmbeddingRetrieval: true,
      infiniteSessions: { enabled: false },
      availableTools: allowedToolNames,
      tools,
      toolSearch: { enabled: false },
      systemMessage: {
        mode: "append",
        content: buildIsolatedSystemMessage(
          skillContent,
          ".github/skills/investigate-issue/SKILL.md",
          skillDigest,
        ),
      },
      onPermissionRequest: async () => ({
        kind: "reject",
        feedback: "The isolated investigation exposes only permissionless read-only tools.",
      }),
    });
    response = await isolatedSession.sendAndWait(
      {
        prompt: buildInvestigationPrompt(item.number),
        displayPrompt: `Investigate ${item.repository}#${item.number}`,
        agentMode: "interactive",
      },
      10 * 60 * 1000,
    );
    events = await isolatedSession.getEvents();
    await captureDiagnostics("response");
    validateIsolatedEvents(events, allowedToolNames, configuredModel);
    const sourceVerification = await verifyInvestigationSource(
      response?.data?.content,
      { executionSource: sourceTracker.getSource() },
    );
    report = createStoredReport(item, response, {
      skillLoaded: true,
      skillPath: ".github/skills/investigate-issue/SKILL.md",
      skillDigest,
      model: configuredModel,
      configuredModel,
      ...sourceVerification,
    });
  } catch (error) {
    investigationError = error;
    try {
      await captureDiagnostics("failure", error);
    } catch (captureError) {
      investigationError = captureError;
    }
    await abortIsolatedSession(isolatedSession);
  } finally {
    try {
      await cleanupIsolatedResources(isolatedSession, client);
    } catch (cleanupError) {
      if (!investigationError) {
        investigationError = cleanupError;
      }
    }
    try {
      await captureDiagnostics("cleanup", investigationError);
    } catch (captureError) {
      investigationError = captureError;
    }
  }
  if (investigationError) {
    throw investigationError;
  }
  return report;
}
