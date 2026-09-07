import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { CanvasError, createCanvas, joinSession } from "@github/copilot-sdk/extension";

import { createForegroundHandoff } from "./handoff.mjs";
import { loadIssueQueue } from "./queue.mjs";
import {
  configureServer,
  getInstancePage,
  getInstanceState,
  investigateInstanceIssue,
  refreshInstance,
  selectInstanceIssue,
  startInstance,
  stopInstance,
} from "./server.mjs";
import { createAreaStore } from "./storage.mjs";
import { AREA_OPTIONS } from "./taxonomy.mjs";

const areaSchema = { type: "string", enum: AREA_OPTIONS.map((option) => option.label) };
const selectionSchema = {
  type: "object",
  properties: { itemId: { type: "string", pattern: "^[A-Za-z0-9-]{16,64}$" } },
  required: ["itemId"],
  additionalProperties: false,
};

function actionHandler(operation) {
  return async (ctx) => {
    try {
      return await operation(ctx.instanceId, ctx.input);
    } catch (error) {
      throw new CanvasError(error.code ?? "queue_action_failed", error.message);
    }
  };
}

const session = await joinSession({
  canvases: [
    createCanvas({
      id: "aspnetcore-issue-triage",
      displayName: "ASP.NET Core Issue Triage",
      description: "Browse public ASP.NET Core area queues and request issue-session research.",
      inputSchema: {
        type: "object",
        properties: { area: areaSchema },
        additionalProperties: false,
      },
      actions: [
        {
          name: "refresh",
          description: "Refresh the selected public area queue.",
          inputSchema: {
            type: "object",
            properties: { area: areaSchema },
            required: ["area"],
            additionalProperties: false,
          },
          handler: actionHandler(refreshInstance),
        },
        {
          name: "summary",
          description: "Return selected queue state and dispatch status, not child-session progress.",
          inputSchema: { type: "object", properties: {}, additionalProperties: false },
          handler: actionHandler(getInstanceState),
        },
        {
          name: "page",
          description: "Return an explicit queue page.",
          inputSchema: {
            type: "object",
            properties: {
              offset: { type: "integer", minimum: 0 },
              limit: { type: "integer", enum: [25, 50, 100] },
            },
            required: ["offset", "limit"],
            additionalProperties: false,
          },
          handler: actionHandler(getInstancePage),
        },
        {
          name: "select",
          description: "Select one server-owned issue.",
          inputSchema: selectionSchema,
          handler: actionHandler(selectInstanceIssue),
        },
        {
          name: "investigate",
          description: "Send a request to the foreground agent to open a research-only issue session.",
          inputSchema: selectionSchema,
          handler: actionHandler(investigateInstanceIssue),
        },
      ],
      open: async (ctx) => {
        try {
          const entry = await startInstance(ctx.instanceId, ctx.input ?? {}, (message) =>
            session.log(message, { level: "warning", ephemeral: true }));
          return {
            title: "ASP.NET Core Issue Triage",
            status: "Read-only public GitHub issue queue",
            url: entry.url,
          };
        } catch (error) {
          throw new CanvasError(error.code ?? "queue_open_failed", error.message);
        }
      },
      onClose: (ctx) => stopInstance(ctx.instanceId),
    }),
  ],
});

const repositoryRoot = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
let busy = null;
session.on("assistant.turn_start", () => { busy = true; });
session.on("session.idle", () => { busy = false; });

configureServer({
  load: loadIssueQueue,
  store: createAreaStore(session.workspacePath, repositoryRoot),
  launch: createForegroundHandoff({
    send: (request) => session.send(request),
    log: (message) => session.log(message, { level: "info", ephemeral: true }),
    isBusy: () => busy,
  }),
});
