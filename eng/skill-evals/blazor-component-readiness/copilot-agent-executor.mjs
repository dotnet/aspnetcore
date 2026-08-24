// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { createHash, randomUUID } from "node:crypto";
import { spawn } from "node:child_process";
import {
  access,
  cp,
  mkdir,
  readFile,
  readdir,
  realpath,
  stat,
  writeFile,
} from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";

export const EXECUTOR_NAME = "blazor-component-readiness-agent";
export const AGENT_NAME = "blazor-component-readiness";
export const PROFILE_RELATIVE_PATH =
  ".github/agents/blazor-component-readiness.agent.md";
export const RESOURCE_RELATIVE_PATH =
  ".github/agents/blazor-component-readiness";

const REQUIRED_CLI_FLAGS = [
  "--agent",
  "--allow-all-tools",
  "--disable-builtin-mcps",
  "--no-ask-user",
  "--no-auto-update",
  "--no-remote",
  "--no-remote-export",
  "--output-format",
  "--reasoning-effort",
];
const MINIMUM_CLI_VERSION = [1, 0, 77];
const MAX_OUTPUT_BYTES = 64 * 1024 * 1024;
const moduleDirectory = path.dirname(fileURLToPath(import.meta.url));
const defaultRepositoryRoot = path.resolve(moduleDirectory, "../../..");

function isRecord(value) {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function parseFrontmatter(content, profilePath) {
  const normalized = content.replaceAll("\r\n", "\n");
  if (!normalized.startsWith("---\n")) {
    throw new Error(`Agent profile is missing frontmatter: ${profilePath}`);
  }

  const end = normalized.indexOf("\n---\n", 4);
  if (end < 0) {
    throw new Error(`Agent profile frontmatter is not terminated: ${profilePath}`);
  }

  const values = new Map();
  for (const line of normalized.slice(4, end).split("\n")) {
    const match = /^([a-z][a-z-]*):\s*(.*?)\s*$/.exec(line);
    if (match && !values.has(match[1])) {
      values.set(match[1], match[2]);
    }
  }

  return values;
}

function parseVersion(output) {
  const match = /(?:^|\s)(\d+)\.(\d+)\.(\d+)(?:[-+][0-9A-Za-z.-]+)?/.exec(
    output,
  );
  if (!match) {
    throw new Error(`Unsupported Copilot CLI version output: ${output.trim()}`);
  }

  return {
    text: output.trim(),
    parts: match.slice(1, 4).map(Number),
  };
}

function compareVersions(left, right) {
  for (let index = 0; index < Math.max(left.length, right.length); index++) {
    const delta = (left[index] ?? 0) - (right[index] ?? 0);
    if (delta !== 0) {
      return delta;
    }
  }

  return 0;
}

function normalizeTimestamp(value) {
  if (typeof value !== "string") {
    return undefined;
  }

  const timestamp = new Date(value);
  return Number.isNaN(timestamp.getTime()) ? undefined : timestamp;
}

function createEmptyMetrics() {
  return {
    tokenUsage: {
      inputTokens: 0,
      outputTokens: 0,
      totalTokens: 0,
      cacheReadTokens: 0,
      cacheWriteTokens: 0,
      callCount: 0,
      byModel: {},
    },
    toolCallCount: 0,
    toolCallBreakdown: {},
    simulatedToolCallCount: 0,
    skillActivationCount: 0,
    skillActivationBreakdown: {},
    turnCount: 0,
    wallTimeMs: 0,
    errorCount: 0,
  };
}

function computeMetrics(events, wallTimeMs) {
  const metrics = createEmptyMetrics();
  metrics.wallTimeMs = wallTimeMs;
  for (const event of events) {
    switch (event.type) {
      case "token_usage": {
        const data = event.data;
        metrics.tokenUsage.inputTokens += data.inputTokens;
        metrics.tokenUsage.outputTokens += data.outputTokens;
        metrics.tokenUsage.totalTokens += data.inputTokens + data.outputTokens;
        metrics.tokenUsage.cacheReadTokens += data.cacheReadTokens ?? 0;
        metrics.tokenUsage.cacheWriteTokens += data.cacheWriteTokens ?? 0;
        metrics.tokenUsage.callCount++;
        const byModel = (metrics.tokenUsage.byModel[data.model] ??= {
          inputTokens: 0,
          outputTokens: 0,
          callCount: 0,
        });
        byModel.inputTokens += data.inputTokens;
        byModel.outputTokens += data.outputTokens;
        byModel.callCount++;
        break;
      }
      case "tool_call":
        metrics.toolCallCount++;
        metrics.toolCallBreakdown[event.data.toolName] =
          (metrics.toolCallBreakdown[event.data.toolName] ?? 0) + 1;
        break;
      case "turn_end":
        metrics.turnCount++;
        break;
      case "error":
        metrics.errorCount++;
        break;
    }
  }

  return metrics;
}

export function buildCopilotArguments({
  prompt,
  workDir,
  model,
  reasoningEffort,
  agentName = AGENT_NAME,
}) {
  const args = [
    "-p",
    prompt,
    "--agent",
    agentName,
    "-C",
    workDir,
    "--output-format",
    "json",
    "--allow-all-tools",
    "--disable-builtin-mcps",
    "--no-ask-user",
    "--no-auto-update",
    "--no-remote",
    "--no-remote-export",
    "--disallow-temp-dir",
  ];
  if (model) {
    args.push("--model", model);
  }
  if (reasoningEffort) {
    args.push("--reasoning-effort", reasoningEffort);
  }

  return args;
}

export function buildResolutionProbeArguments({
  workDir,
  probeAgentName,
}) {
  return buildCopilotArguments({
    prompt: "This prompt must not execute.",
    workDir,
    agentName: probeAgentName,
  });
}

function redactPromptArgument(args) {
  const redacted = [...args];
  const promptIndex = redacted.indexOf("-p");
  if (promptIndex >= 0 && promptIndex + 1 < redacted.length) {
    redacted[promptIndex + 1] = "<stimulus>";
  }

  return redacted;
}

function isChildRunning(child) {
  return child.exitCode === null && child.signalCode === null;
}

function signalChildTree(child, signal) {
  if (!isChildRunning(child)) {
    return;
  }

  if (process.platform !== "win32" && child.pid) {
    try {
      process.kill(-child.pid, signal);
      return;
    } catch {
      child.kill(signal);
      return;
    }
  }

  child.kill(signal);
}

function processGroupExists(pid) {
  if (process.platform === "win32" || !pid) {
    return false;
  }

  try {
    process.kill(-pid, 0);
    return true;
  } catch (error) {
    if (error?.code === "ESRCH") {
      return false;
    }
    throw error;
  }
}

async function forceKillChildTree(child) {
  if (process.platform !== "win32") {
    if (processGroupExists(child.pid)) {
      process.kill(-child.pid, "SIGKILL");
    } else if (isChildRunning(child)) {
      child.kill("SIGKILL");
    }
    return;
  }

  if (!child.pid) {
    child.kill("SIGKILL");
    return;
  }

  await new Promise((resolve) => {
    const killer = spawn(
      "taskkill.exe",
      ["/PID", String(child.pid), "/T", "/F"],
      {
        shell: false,
        stdio: "ignore",
      },
    );
    killer.once("error", () => {
      child.kill("SIGKILL");
      resolve();
    });
    killer.once("close", resolve);
  });
}

export async function terminateChildTree(child, graceMs = 2_000) {
  if (!isChildRunning(child) && !processGroupExists(child.pid)) {
    return;
  }

  if (process.platform === "win32") {
    await forceKillChildTree(child);
    return;
  }

  const closed = new Promise((resolve) => child.once("close", resolve));
  signalChildTree(child, "SIGTERM");
  await Promise.race([
    closed,
    new Promise((resolve) => setTimeout(resolve, graceMs)),
  ]);
  await forceKillChildTree(child);
  if (isChildRunning(child)) {
    await Promise.race([
      closed,
      new Promise((resolve) => setTimeout(resolve, graceMs)),
    ]);
  }
}

export async function runCommand(
  command,
  args,
  {
    cwd,
    env,
    timeoutMs,
    onStdoutLine,
    children,
  } = {},
) {
  return await new Promise((resolve, reject) => {
    const child = spawn(command, args, {
      cwd,
      detached: process.platform !== "win32",
      env,
      shell: false,
      stdio: ["ignore", "pipe", "pipe"],
    });
    children?.add(child);

    let stdout = "";
    let stderr = "";
    let lineBuffer = "";
    let settled = false;
    let timedOut = false;
    let streamError;

    const finish = (callback) => {
      if (settled) {
        return;
      }

      settled = true;
      clearTimeout(timeoutTimer);
      children?.delete(child);
      callback();
    };

    const stopForStreamError = (error) => {
      streamError ??= error;
      void terminateChildTree(child).catch((terminationError) => {
        streamError ??= terminationError;
      });
    };

    const append = (current, chunk, streamName) => {
      const next = current + chunk;
      if (Buffer.byteLength(next, "utf8") > MAX_OUTPUT_BYTES) {
        throw new Error(`${streamName} exceeded ${MAX_OUTPUT_BYTES} bytes`);
      }

      return next;
    };

    child.stdout.setEncoding("utf8");
    child.stderr.setEncoding("utf8");
    child.stdout.on("data", (chunk) => {
      try {
        stdout = append(stdout, chunk, "Copilot stdout");
        lineBuffer += chunk;
        let newlineIndex;
        while ((newlineIndex = lineBuffer.indexOf("\n")) >= 0) {
          const line = lineBuffer.slice(0, newlineIndex).replace(/\r$/, "");
          lineBuffer = lineBuffer.slice(newlineIndex + 1);
          if (line.length > 0) {
            onStdoutLine?.(line);
          }
        }
      } catch (error) {
        stopForStreamError(error);
      }
    });
    child.stderr.on("data", (chunk) => {
      try {
        stderr = append(stderr, chunk, "Copilot stderr");
      } catch (error) {
        stopForStreamError(error);
      }
    });
    child.on("error", (error) => finish(() => reject(error)));
    child.on("close", (exitCode, signal) => {
      if (lineBuffer.length > 0) {
        try {
          onStdoutLine?.(lineBuffer.replace(/\r$/, ""));
        } catch (error) {
          streamError ??= error;
        }
      }

      finish(() =>
        resolve({
          exitCode: exitCode ?? 1,
          signal,
          stdout,
          stderr,
          streamError,
          timedOut,
        }),
      );
    });

    const timeoutTimer = setTimeout(() => {
      timedOut = true;
      void terminateChildTree(child).catch((error) => {
        streamError ??= error;
      });
    }, timeoutMs);
    timeoutTimer.unref?.();
  });
}

export async function inspectAgentProfile(repositoryRoot = defaultRepositoryRoot) {
  const profilePath = path.join(repositoryRoot, PROFILE_RELATIVE_PATH);
  const resourcePath = path.join(repositoryRoot, RESOURCE_RELATIVE_PATH);
  const [content, resourceStat] = await Promise.all([
    readFile(profilePath, "utf8"),
    stat(resourcePath),
  ]);
  if (!resourceStat.isDirectory()) {
    throw new Error(`Agent resource path is not a directory: ${resourcePath}`);
  }

  const frontmatter = parseFrontmatter(content, profilePath);
  if (frontmatter.get("name") !== AGENT_NAME) {
    throw new Error(
      `Expected agent profile name ${AGENT_NAME}, found ${
        frontmatter.get("name") ?? "<missing>"
      }`,
    );
  }
  if (frontmatter.get("disable-model-invocation") !== "true") {
    throw new Error("Agent profile must disable model invocation");
  }
  if (frontmatter.get("user-invocable") !== "true") {
    throw new Error("Agent profile must be user invocable");
  }

  return {
    content,
    digest: createHash("sha256").update(content, "utf8").digest("hex"),
    profilePath,
    resourcePath,
  };
}

export function createIsolatedEnvironment(isolatedHome, baseEnvironment) {
  const env = { ...(baseEnvironment ?? process.env) };
  for (const key of Object.keys(env)) {
    if (
      key === "COPILOT_ALLOW_ALL" ||
      key === "COPILOT_AUTO_UPDATE" ||
      key === "COPILOT_CUSTOM_INSTRUCTIONS_DIRS" ||
      key === "COPILOT_MODEL" ||
      key === "COPILOT_OFFLINE" ||
      key.startsWith("COPILOT_OTEL_") ||
      key.startsWith("COPILOT_PROVIDER_") ||
      key.startsWith("OTEL_")
    ) {
      delete env[key];
    }
  }

  env.HOME = isolatedHome;
  env.USERPROFILE = isolatedHome;
  env.XDG_CONFIG_HOME = path.join(isolatedHome, ".config");
  env.COPILOT_HOME = path.join(isolatedHome, ".copilot");
  env.COPILOT_OTEL_ENABLED = "false";
  return env;
}

async function listMatchingAgents(agentDirectory) {
  const matches = [];
  let entries;
  try {
    entries = await readdir(agentDirectory, { withFileTypes: true });
  } catch (error) {
    if (error?.code === "ENOENT") {
      return matches;
    }
    throw error;
  }

  for (const entry of entries) {
    if (!entry.isFile() || !entry.name.endsWith(".agent.md")) {
      continue;
    }

    const candidatePath = path.join(agentDirectory, entry.name);
    const candidate = await readFile(candidatePath, "utf8");
    const frontmatter = parseFrontmatter(candidate, candidatePath);
    if (frontmatter.get("name") === AGENT_NAME) {
      matches.push({
        path: candidatePath,
        digest: createHash("sha256").update(candidate, "utf8").digest("hex"),
      });
    }
  }

  return matches;
}

async function stageAgent(profile, workDir, repositoryRoot) {
  const requestedWorkDir = path.resolve(workDir);
  const resolvedWorkDir = await realpath(workDir);
  const resolvedRepositoryRoot = await realpath(repositoryRoot);
  if (
    resolvedWorkDir === resolvedRepositoryRoot ||
    resolvedWorkDir.startsWith(resolvedRepositoryRoot + path.sep)
  ) {
    throw new Error(
      "Agent eval workDir must be isolated from the developer checkout",
    );
  }

  const agentDirectory = path.join(resolvedWorkDir, ".github", "agents");
  await mkdir(agentDirectory, { recursive: true });
  const matches = await listMatchingAgents(agentDirectory);
  if (
    matches.length > 1 ||
    (matches.length === 1 && matches[0].digest !== profile.digest)
  ) {
    throw new Error(
      `Ambiguous or shadowed ${AGENT_NAME} profile in eval workspace`,
    );
  }

  const stagedProfilePath = path.join(
    resolvedWorkDir,
    PROFILE_RELATIVE_PATH,
  );
  const stagedResourcePath = path.join(
    resolvedWorkDir,
    RESOURCE_RELATIVE_PATH,
  );
  await mkdir(path.dirname(stagedProfilePath), { recursive: true });
  await writeFile(stagedProfilePath, profile.content, "utf8");
  await cp(profile.resourcePath, stagedResourcePath, {
    recursive: true,
    force: true,
  });

  const staged = await inspectAgentProfile(resolvedWorkDir);
  if (staged.digest !== profile.digest) {
    throw new Error("Staged agent profile digest does not match repository profile");
  }

  return {
    workDir: requestedWorkDir,
    profilePath: stagedProfilePath,
  };
}

function normalizeCopilotEvent(event) {
  const timestamp = normalizeTimestamp(event.timestamp);
  const common = timestamp ? { timestamp } : {};
  switch (event.type) {
    case "user.message":
      return {
        ...common,
        type: "user_message",
        data: { content: event.data.content },
      };
    case "assistant.message":
      return {
        ...common,
        type: "assistant_message",
        data: { content: event.data.content },
      };
    case "assistant.reasoning":
      return {
        ...common,
        type: "reasoning",
        data: { content: event.data.content },
      };
    case "assistant.turn_start":
      return {
        ...common,
        type: "turn_start",
        data: { turnId: event.data.turnId },
      };
    case "assistant.turn_end":
      return {
        ...common,
        type: "turn_end",
        data: { turnId: event.data.turnId },
      };
    case "assistant.usage":
      return {
        ...common,
        type: "token_usage",
        data: {
          inputTokens: event.data.inputTokens,
          outputTokens: event.data.outputTokens,
          cacheReadTokens: event.data.cacheReadTokens,
          cacheWriteTokens: event.data.cacheWriteTokens,
          model: event.data.model,
        },
      };
    case "tool.execution_start":
      return {
        ...common,
        type: "tool_call",
        data: {
          toolName: event.data.toolName,
          toolCallId: event.data.toolCallId,
          arguments: isRecord(event.data.arguments)
            ? event.data.arguments
            : undefined,
        },
      };
    case "tool.execution_complete":
      return {
        ...common,
        type: "tool_result",
        data: {
          toolName: "unknown",
          toolCallId: event.data.toolCallId,
          success: event.data.success,
          result: event.data.result,
        },
      };
    case "session.error":
      return {
        ...common,
        type: "error",
        data: {
          message: event.data.message,
          type: event.data.errorType,
          code: event.data.statusCode,
        },
      };
    default:
      return {
        ...common,
        type: "custom",
        data: {
          source: "copilot-cli",
          eventType: event.type,
        },
      };
  }
}

export function parseCopilotEvent(line) {
  let event;
  try {
    event = JSON.parse(line);
  } catch (error) {
    throw new Error(`Copilot emitted malformed JSONL: ${error.message}`);
  }

  if (
    !isRecord(event) ||
    typeof event.type !== "string" ||
    !isRecord(event.data)
  ) {
    throw new Error("Copilot emitted an invalid session event");
  }

  return event;
}

export function parseCopilotOutputLine(line) {
  let value;
  try {
    value = JSON.parse(line);
  } catch (error) {
    throw new Error(`Copilot emitted malformed JSONL: ${error.message}`);
  }

  if (
    isRecord(value) &&
    value.type === "result" &&
    typeof value.sessionId === "string" &&
    Number.isInteger(value.exitCode) &&
    isRecord(value.usage)
  ) {
    return { kind: "result", value };
  }

  return { kind: "event", value: parseCopilotEvent(line) };
}

function getPrompt(stimulus) {
  if (typeof stimulus.prompt !== "string" || stimulus.prompt.length === 0) {
    throw new Error("Custom agent executor requires a non-empty prompt stimulus");
  }

  return stimulus.prompt;
}

function validateSessionEvents(rawEvents, resultEnvelope, prompt) {
  if (!resultEnvelope) {
    throw new Error("Copilot emitted no terminal result envelope");
  }
  if (resultEnvelope.exitCode !== 0) {
    throw new Error(
      `Copilot terminal result reported exit code ${resultEnvelope.exitCode}`,
    );
  }

  const exactUserMessages = rawEvents.filter(
    (event) =>
      event.type === "user.message" && event.data.content === prompt,
  );
  if (exactUserMessages.length !== 1) {
    throw new Error("Copilot did not preserve the exact Vally stimulus");
  }

  const sessionError = rawEvents.find((event) => event.type === "session.error");
  if (sessionError) {
    throw new Error(`Copilot session error: ${sessionError.data.message}`);
  }

  const finalMessage = rawEvents
    .filter(
      (event) =>
        event.type === "assistant.message" &&
        event.data.parentToolCallId === undefined &&
        typeof event.data.content === "string" &&
        event.data.content.trim().length > 0,
    )
    .at(-1);
  if (!finalMessage) {
    throw new Error("Copilot emitted no terminal root assistant response");
  }

  const selectedModel = rawEvents
    .filter(
      (event) =>
        (event.type === "model.call_start" ||
          event.type === "model.turn_started" ||
          event.type === "assistant.usage") &&
        typeof event.data.model === "string",
    )
    .at(-1)?.data.model;

  return {
    output: finalMessage.data.content,
    sessionID: resultEnvelope.sessionId,
    selectedModel,
  };
}

async function persistArtifacts(directory, metadata, stdout, stderr) {
  if (!directory) {
    return;
  }

  await mkdir(directory, { recursive: true });
  await Promise.all([
    writeFile(
      path.join(directory, "executor-metadata.json"),
      `${JSON.stringify(metadata, null, 2)}\n`,
      "utf8",
    ),
    writeFile(
      path.join(directory, "copilot-cli.stdout.jsonl"),
      stdout,
      "utf8",
    ),
    writeFile(
      path.join(directory, "copilot-cli.stderr.txt"),
      stderr,
      "utf8",
    ),
  ]);
}

export function createExecutor({
  repositoryRoot = defaultRepositoryRoot,
  copilotCommand = "copilot",
  commandRunner = runCommand,
} = {}) {
  const children = new Set();
  let cliInfoPromise;

  async function validateCli(workDir, env, timeoutMs) {
    cliInfoPromise ??= (async () => {
      const versionResult = await commandRunner(copilotCommand, ["--version"], {
        cwd: workDir,
        env,
        timeoutMs,
        children,
      });
      if (versionResult.exitCode !== 0 || versionResult.timedOut) {
        throw new Error(
          `Copilot CLI version probe failed: ${versionResult.stderr.trim()}`,
        );
      }

      const version = parseVersion(versionResult.stdout);
      if (compareVersions(version.parts, MINIMUM_CLI_VERSION) < 0) {
        throw new Error(
          `Copilot CLI ${version.text} is older than the supported 1.0.77 release`,
        );
      }

      const helpResult = await commandRunner(copilotCommand, ["--help"], {
        cwd: workDir,
        env,
        timeoutMs,
        children,
      });
      if (helpResult.exitCode !== 0 || helpResult.timedOut) {
        throw new Error(
          `Copilot CLI help probe failed: ${helpResult.stderr.trim()}`,
        );
      }
      for (const flag of REQUIRED_CLI_FLAGS) {
        if (!helpResult.stdout.includes(flag)) {
          throw new Error(`Copilot CLI does not support required flag ${flag}`);
        }
      }

      return version.text;
    })();

    return await cliInfoPromise;
  }

  return {
    name: EXECUTOR_NAME,
    supportsPreparedWorkspace: true,
    validateConfig(config) {
      if (config !== undefined) {
        throw new Error(`${EXECUTOR_NAME} does not accept executor config`);
      }
    },
    async execute(stimulus, options) {
      const prompt = getPrompt(stimulus);
      const startedAt = new Date();
      const profile = await inspectAgentProfile(repositoryRoot);
      const staged = await stageAgent(profile, options.workDir, repositoryRoot);
      const isolatedHome = path.join(
        staged.workDir,
        ".copilot-agent-executor",
        "home",
      );
      await mkdir(isolatedHome, { recursive: true });
      const env = createIsolatedEnvironment(isolatedHome);
      const cliVersion = await validateCli(
        staged.workDir,
        env,
        options.timeout,
      );

      const probeAgentName = `missing-agent-${randomUUID()}`;
      const probe = await commandRunner(
        copilotCommand,
        buildResolutionProbeArguments({
          workDir: staged.workDir,
          probeAgentName,
        }),
        {
          cwd: staged.workDir,
          env,
          timeoutMs: Math.min(options.timeout, 30_000),
          children,
        },
      );
      const expectedProbe = new RegExp(
        `No such agent: ${probeAgentName.replaceAll("-", "\\-")},\\s*` +
          `available:\\s*${AGENT_NAME}\\s*$`,
        "m",
      );
      if (
        probe.timedOut ||
        probe.exitCode === 0 ||
        probe.stdout.trim().length > 0 ||
        !expectedProbe.test(probe.stderr)
      ) {
        throw new Error(
          "Copilot custom-agent resolution probe did not fail closed with the " +
            `sole expected profile; stderr: ${probe.stderr.trim()}`,
        );
      }

      const args = buildCopilotArguments({
        prompt,
        workDir: staged.workDir,
        model: options.model,
        reasoningEffort: options.reasoningEffort,
      });
      const metadata = {
        executor: EXECUTOR_NAME,
        requestedAgent: AGENT_NAME,
        requestedModel: options.model ?? null,
        requestedReasoningEffort: options.reasoningEffort ?? null,
        workDir: staged.workDir,
        profilePath: PROFILE_RELATIVE_PATH,
        profileSha256: profile.digest,
        copilotCliVersion: cliVersion,
        command: copilotCommand,
        arguments: redactPromptArgument(args),
        identityAttestation:
          "isolated-home sole-profile native --agent resolution probe",
        startedAt: startedAt.toISOString(),
      };

      const rawEvents = [];
      let resultEnvelope;
      const result = await commandRunner(copilotCommand, args, {
        cwd: staged.workDir,
        env,
        timeoutMs: options.timeout,
        children,
        onStdoutLine(line) {
          if (resultEnvelope) {
            throw new Error("Copilot emitted output after its terminal result");
          }
          const parsed = parseCopilotOutputLine(line);
          if (parsed.kind === "result") {
            resultEnvelope = parsed.value;
          } else {
            rawEvents.push(parsed.value);
            options.onRawEvent?.(parsed.value);
          }
        },
      });
      const completedAt = new Date();
      const finalMetadata = {
        ...metadata,
        completedAt: completedAt.toISOString(),
        exitCode: result.exitCode,
        signal: result.signal,
        timedOut: result.timedOut,
      };
      await persistArtifacts(
        options.sessionLog?.executorArtifactsDir,
        finalMetadata,
        result.stdout,
        result.stderr,
      );

      if (result.streamError) {
        throw result.streamError;
      }
      if (result.timedOut) {
        throw new Error(`Copilot CLI timed out after ${options.timeout}ms`);
      }
      if (result.exitCode !== 0) {
        throw new Error(
          `Copilot CLI exited with code ${result.exitCode}: ${result.stderr.trim()}`,
        );
      }

      const session = validateSessionEvents(
        rawEvents,
        resultEnvelope,
        prompt,
      );
      const events = rawEvents.map(normalizeCopilotEvent);
      events.unshift({
        type: "custom",
        timestamp: startedAt,
        data: {
          ...metadata,
          source: "blazor-component-readiness-agent-executor",
        },
      });

      return {
        id: options.sessionID ?? randomUUID(),
        stimulus,
        events,
        metrics: computeMetrics(
          events,
          completedAt.getTime() - startedAt.getTime(),
        ),
        output: session.output,
        workDir: staged.workDir,
        artifactDir: options.sessionLog?.executorArtifactsDir,
        endReason: "completed",
        metadata: {
          model: session.selectedModel ?? options.model ?? "unknown",
          skillsLoaded: [],
          startedAt,
          completedAt,
          executor: EXECUTOR_NAME,
          sessionID: session.sessionID,
        },
      };
    },
    async shutdown() {
      await Promise.allSettled(
        [...children].map((child) => terminateChildTree(child)),
      );
    },
  };
}

export async function registerExecutors(registry) {
  registry.register(createExecutor());
}
