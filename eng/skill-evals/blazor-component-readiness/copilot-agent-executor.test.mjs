// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import assert from "node:assert/strict";
import { spawn } from "node:child_process";
import { createHash } from "node:crypto";
import {
  access,
  mkdtemp,
  mkdir,
  readFile,
  realpath,
  rm,
  symlink,
  writeFile,
} from "node:fs/promises";
import { tmpdir } from "node:os";
import path from "node:path";
import test from "node:test";
import {
  AGENT_NAME,
  EXECUTOR_NAME,
  PROFILE_RELATIVE_PATH,
  buildCopilotArguments,
  createIsolatedEnvironment,
  createExecutor,
  inspectAgentProfile,
  parseCopilotEvent,
  parseCopilotOutputLine,
  runCommand,
  terminateChildTree,
} from "./copilot-agent-executor.mjs";

const profile = `---
name: ${AGENT_NAME}
description: Test profile.
disable-model-invocation: true
user-invocable: true
---

# Test profile
`;

async function createFixture() {
  const root = await mkdtemp(path.join(tmpdir(), "readiness-executor-test-"));
  const repositoryRoot = path.join(root, "repo");
  const workDir = path.join(root, "workspace");
  const profilePath = path.join(repositoryRoot, PROFILE_RELATIVE_PATH);
  await mkdir(path.dirname(profilePath), { recursive: true });
  await mkdir(
    path.join(
      repositoryRoot,
      ".github",
      "agents",
      "blazor-component-readiness",
      "references",
    ),
    { recursive: true },
  );
  await mkdir(workDir, { recursive: true });
  await writeFile(profilePath, profile, "utf8");
  await writeFile(
    path.join(
      repositoryRoot,
      ".github",
      "agents",
      "blazor-component-readiness",
      "references",
      "checklist.md",
    ),
    "# Test\n",
    "utf8",
  );

  return {
    root,
    repositoryRoot,
    workDir: await realpath(workDir),
    async dispose() {
      await rm(root, { recursive: true, force: true });
    },
  };
}

function event(type, data, overrides = {}) {
  return JSON.stringify({
    id: crypto.randomUUID(),
    timestamp: new Date().toISOString(),
    parentId: null,
    type,
    data,
    ...overrides,
  });
}

function resultEnvelope() {
  return JSON.stringify({
    type: "result",
    timestamp: new Date().toISOString(),
    sessionId: "session-1",
    exitCode: 0,
    usage: {
      premiumRequests: 0,
      totalApiDurationMs: 1,
      sessionDurationMs: 2,
      codeChanges: {
        linesAdded: 0,
        linesRemoved: 0,
        filesModified: [],
      },
    },
  });
}

function createFakeRunner({
  prompt = "Review one synthetic vendor control.",
  agentExitCode = 0,
  agentStdout,
  agentStderr = "",
  probeStderr,
} = {}) {
  const calls = [];
  const runner = async (_command, args, options) => {
    calls.push({ args: [...args], options });
    if (args[0] === "--version") {
      return {
        exitCode: 0,
        signal: null,
        stdout: "GitHub Copilot CLI 1.0.81-7\n",
        stderr: "",
        timedOut: false,
      };
    }
    if (args[0] === "--help") {
      return {
        exitCode: 0,
        signal: null,
        stdout: [
          "--agent",
          "--allow-all-tools",
          "--disable-builtin-mcps",
          "--no-ask-user",
          "--no-auto-update",
          "--no-remote",
          "--no-remote-export",
          "--output-format",
          "--reasoning-effort",
        ].join("\n"),
        stderr: "",
        timedOut: false,
      };
    }

    const agentIndex = args.indexOf("--agent");
    const requestedAgent = args[agentIndex + 1];
    if (requestedAgent !== AGENT_NAME) {
      return {
        exitCode: 1,
        signal: null,
        stdout: "",
        stderr:
          probeStderr ??
          `No such agent: ${requestedAgent}, available: ${AGENT_NAME}\n`,
        timedOut: false,
      };
    }

    const workDir = args[args.indexOf("-C") + 1];
    const lines =
      agentStdout ??
      [
        event("session.start", {
          sessionId: "session-1",
          version: 1,
          producer: "copilot",
          copilotVersion: "1.0.81-7",
          startTime: new Date().toISOString(),
          selectedModel: "gpt-5.6-sol",
          context: { cwd: workDir },
        }),
        event("user.message", { content: prompt }),
        event("assistant.turn_start", { turnId: "turn-1" }),
        event("assistant.message", {
          messageId: "message-1",
          content: "A grounded readiness response.",
        }),
        event("assistant.usage", {
          model: "gpt-5.6-sol",
          inputTokens: 10,
          outputTokens: 5,
          cacheReadTokens: 0,
          cacheWriteTokens: 0,
        }),
        event("assistant.turn_end", { turnId: "turn-1" }),
        resultEnvelope(),
      ].join("\n") + "\n";
    for (const line of lines.trimEnd().split("\n")) {
      options.onStdoutLine?.(line);
    }

    return {
      exitCode: agentExitCode,
      signal: null,
      stdout: lines,
      stderr: agentStderr,
      timedOut: false,
    };
  };

  return { calls, prompt, runner };
}

async function executeFixture(fixture, fake, overrides = {}) {
  const rawEvents = [];
  const executor = createExecutor({
    repositoryRoot: fixture.repositoryRoot,
    commandRunner: fake.runner,
  });
  const trajectory = await executor.execute(
    {
      name: "test",
      prompt: fake.prompt,
    },
    {
      timeout: 10_000,
      workDir: fixture.workDir,
      model: "gpt-5.6-sol",
      reasoningEffort: "high",
      onRawEvent: (rawEvent) => rawEvents.push(rawEvent),
      ...overrides,
    },
  );
  return { executor, rawEvents, trajectory };
}

test("command uses an argument array and preserves the exact prompt", () => {
  const prompt = "literal $HOME; $(echo unsafe) \"quoted\" prompt";
  const args = buildCopilotArguments({
    prompt,
    workDir: "/isolated/workspace",
    model: "gpt-5.6-sol",
    reasoningEffort: "high",
  });

  assert.equal(args[args.indexOf("-p") + 1], prompt);
  assert.equal(args[args.indexOf("--agent") + 1], AGENT_NAME);
  assert.equal(args[args.indexOf("-C") + 1], "/isolated/workspace");
  assert.equal(args[args.indexOf("--model") + 1], "gpt-5.6-sol");
  assert.equal(args[args.indexOf("--reasoning-effort") + 1], "high");
  assert.ok(args.includes("--output-format"));
  assert.ok(args.includes("--disable-builtin-mcps"));
  assert.ok(!args.join("\n").includes("# Test profile"));
});

test("profile validation records the exact SHA-256 digest", async () => {
  const fixture = await createFixture();
  try {
    const inspected = await inspectAgentProfile(fixture.repositoryRoot);
    assert.equal(
      inspected.digest,
      createHash("sha256").update(profile, "utf8").digest("hex"),
    );
  } finally {
    await fixture.dispose();
  }
});

test("profile validation rejects a wrong profile identity", async () => {
  const fixture = await createFixture();
  try {
    await writeFile(
      path.join(fixture.repositoryRoot, PROFILE_RELATIVE_PATH),
      profile.replace(AGENT_NAME, "wrong-agent"),
      "utf8",
    );
    await assert.rejects(
      inspectAgentProfile(fixture.repositoryRoot),
      /Expected agent profile name/,
    );
  } finally {
    await fixture.dispose();
  }
});

test("isolated environment removes inherited Copilot configuration", () => {
  const isolatedHome = path.join(tmpdir(), "isolated-home");
  const env = createIsolatedEnvironment(isolatedHome, {
    PATH: "/tools",
    COPILOT_HOME: "/outside",
    COPILOT_CUSTOM_INSTRUCTIONS_DIRS: "/outside/instructions",
    COPILOT_MODEL: "wrong-model",
    COPILOT_PROVIDER_BASE_URL: "https://provider.invalid",
    COPILOT_OTEL_FILE_EXPORTER_PATH: "/outside/telemetry.jsonl",
    OTEL_EXPORTER_OTLP_ENDPOINT: "https://telemetry.invalid",
  });

  assert.equal(env.PATH, "/tools");
  assert.equal(env.HOME, isolatedHome);
  assert.equal(env.USERPROFILE, isolatedHome);
  assert.equal(env.COPILOT_HOME, path.join(isolatedHome, ".copilot"));
  assert.equal(env.COPILOT_OTEL_ENABLED, "false");
  assert.equal(env.COPILOT_CUSTOM_INSTRUCTIONS_DIRS, undefined);
  assert.equal(env.COPILOT_MODEL, undefined);
  assert.equal(env.COPILOT_PROVIDER_BASE_URL, undefined);
  assert.equal(env.COPILOT_OTEL_FILE_EXPORTER_PATH, undefined);
  assert.equal(env.OTEL_EXPORTER_OTLP_ENDPOINT, undefined);
});

test("executor binds the sole custom agent and emits retained metadata", async () => {
  const fixture = await createFixture();
  const artifactDir = path.join(fixture.root, "artifacts");
  const fake = createFakeRunner();
  try {
    const { rawEvents, trajectory } = await executeFixture(fixture, fake, {
      sessionLog: {
        rootDir: path.join(fixture.root, "session"),
        executorArtifactsDir: artifactDir,
      },
    });

    assert.equal(trajectory.output, "A grounded readiness response.");
    assert.equal(trajectory.metadata.executor, EXECUTOR_NAME);
    assert.equal(trajectory.metadata.sessionID, "session-1");
    assert.equal(trajectory.metrics.tokenUsage.totalTokens, 15);
    assert.equal(rawEvents.length, 6);
    assert.equal(fake.calls.length, 4);
    const runArgs = fake.calls[3].args;
    assert.equal(runArgs[runArgs.indexOf("-p") + 1], fake.prompt);
    assert.equal(runArgs[runArgs.indexOf("--agent") + 1], AGENT_NAME);

    const retained = JSON.parse(
      await readFile(
        path.join(artifactDir, "executor-metadata.json"),
        "utf8",
      ),
    );
    assert.equal(retained.requestedAgent, AGENT_NAME);
    assert.equal(
      retained.profileSha256,
      createHash("sha256").update(profile, "utf8").digest("hex"),
    );
    assert.equal(
      retained.arguments[retained.arguments.indexOf("-p") + 1],
      "<stimulus>",
    );
    assert.equal(retained.workDir, fixture.workDir);
  } finally {
    await fixture.dispose();
  }
});

test("executor preserves the caller's aliased workspace path", async () => {
  const fixture = await createFixture();
  const aliasedWorkDir = path.join(fixture.root, "workspace-alias");
  const fake = createFakeRunner();
  try {
    await symlink(fixture.workDir, aliasedWorkDir);
    const { trajectory } = await executeFixture(fixture, fake, {
      workDir: aliasedWorkDir,
    });

    assert.equal(trajectory.workDir, aliasedWorkDir);
    const runArgs = fake.calls[3].args;
    assert.equal(runArgs[runArgs.indexOf("-C") + 1], aliasedWorkDir);
  } finally {
    await fixture.dispose();
  }
});

test("executor rejects an ambiguous agent resolution probe", async () => {
  const fixture = await createFixture();
  const fake = createFakeRunner({
    probeStderr:
      "No such agent: ignored, available: blazor-component-readiness, shadow\n",
  });
  try {
    await assert.rejects(
      executeFixture(fixture, fake),
      /resolution probe did not fail closed/,
    );
  } finally {
    await fixture.dispose();
  }
});

test("executor rejects a Copilot CLI older than the supported floor", async () => {
  const fixture = await createFixture();
  const fake = createFakeRunner();
  const runner = async (command, args, options) => {
    if (args[0] === "--version") {
      return {
        exitCode: 0,
        signal: null,
        stdout: "GitHub Copilot CLI 1.0.76\n",
        stderr: "",
        timedOut: false,
      };
    }
    return await fake.runner(command, args, options);
  };
  try {
    const executor = createExecutor({
      repositoryRoot: fixture.repositoryRoot,
      commandRunner: runner,
    });
    await assert.rejects(
      executor.execute(
        { name: "test", prompt: fake.prompt },
        { timeout: 10_000, workDir: fixture.workDir },
      ),
      /older than the supported 1.0.77 release/,
    );
  } finally {
    await fixture.dispose();
  }
});

test("executor rejects a nonzero Copilot agent exit", async () => {
  const fixture = await createFixture();
  const fake = createFakeRunner({
    agentExitCode: 7,
    agentStderr: "agent failed",
  });
  try {
    await assert.rejects(
      executeFixture(fixture, fake),
      /exited with code 7: agent failed/,
    );
  } finally {
    await fixture.dispose();
  }
});

test("executor rejects malformed JSONL", async () => {
  const fixture = await createFixture();
  const fake = createFakeRunner({ agentStdout: "not-json\n" });
  try {
    await assert.rejects(
      executeFixture(fixture, fake),
      /malformed JSONL/,
    );
  } finally {
    await fixture.dispose();
  }
});

test("executor rejects a missing terminal assistant response", async () => {
  const fixture = await createFixture();
  const fake = createFakeRunner({
    agentStdout:
      [
        event("session.start", {
          sessionId: "session-1",
          version: 1,
          producer: "copilot",
          copilotVersion: "1.0.81-7",
          startTime: new Date().toISOString(),
          context: { cwd: fixture.workDir },
        }),
        event("user.message", {
          content: "Review one synthetic vendor control.",
        }),
        resultEnvelope(),
      ].join("\n") + "\n",
  });
  try {
    await assert.rejects(
      executeFixture(fixture, fake),
      /no terminal root assistant response/,
    );
  } finally {
    await fixture.dispose();
  }
});

test("executor rejects a changed stimulus in the CLI event stream", async () => {
  const fixture = await createFixture();
  const fake = createFakeRunner({
    agentStdout:
      [
        event("session.start", {
          sessionId: "session-1",
          version: 1,
          producer: "copilot",
          copilotVersion: "1.0.81-7",
          startTime: new Date().toISOString(),
          context: { cwd: fixture.workDir },
        }),
        event("user.message", { content: "changed prompt" }),
        event("assistant.message", {
          messageId: "message-1",
          content: "Response",
        }),
        resultEnvelope(),
      ].join("\n") + "\n",
  });
  try {
    await assert.rejects(
      executeFixture(fixture, fake),
      /did not preserve the exact Vally stimulus/,
    );
  } finally {
    await fixture.dispose();
  }
});

test("runCommand terminates on timeout", async () => {
  const startedAt = Date.now();
  const result = await runCommand(
    process.execPath,
    ["-e", "setTimeout(() => {}, 30000)"],
    { timeoutMs: 50 },
  );

  assert.equal(result.timedOut, true);
  assert.ok(Date.now() - startedAt < 5_000);
});

test("runCommand terminates descendant processes on timeout", async () => {
  const root = await mkdtemp(path.join(tmpdir(), "readiness-process-tree-test-"));
  const marker = path.join(root, "descendant-survived");
  const descendant = [
    "const fs = require('node:fs');",
    `setTimeout(() => fs.writeFileSync(${JSON.stringify(marker)}, 'bad'), 500);`,
    "setTimeout(() => {}, 30000);",
  ].join("");
  const parent = [
    "const { spawn } = require('node:child_process');",
    `spawn(process.execPath, ['-e', ${JSON.stringify(descendant)}], { stdio: 'ignore' });`,
    "setTimeout(() => {}, 30000);",
  ].join("");

  try {
    const result = await runCommand(process.execPath, ["-e", parent], {
      timeoutMs: 50,
    });
    assert.equal(result.timedOut, true);
    await new Promise((resolve) => setTimeout(resolve, 750));
    await assert.rejects(access(marker), /ENOENT/);
  } finally {
    await rm(root, { recursive: true, force: true });
  }
});

test("runCommand terminates immediately on malformed streamed output", async () => {
  const startedAt = Date.now();
  const result = await runCommand(
    process.execPath,
    ["-e", "console.log('not-json'); setTimeout(() => {}, 30000)"],
    {
      timeoutMs: 10_000,
      onStdoutLine() {
        throw new Error("Copilot emitted malformed JSONL");
      },
    },
  );

  assert.match(result.streamError.message, /malformed JSONL/);
  assert.ok(Date.now() - startedAt < 5_000);
});

test("termination helper supports explicit cancellation", async () => {
  const child = spawn(
    process.execPath,
    ["-e", "setTimeout(() => {}, 30000)"],
    {
      detached: process.platform !== "win32",
      stdio: "ignore",
    },
  );

  await terminateChildTree(child, 100);
  assert.ok(child.exitCode !== null || child.signalCode !== null);
});

test("parseCopilotEvent rejects non-object JSON", () => {
  assert.throws(() => parseCopilotEvent("\"text\""), /invalid session event/);
});

test("parseCopilotOutputLine accepts the terminal result envelope", () => {
  const parsed = parseCopilotOutputLine(resultEnvelope());
  assert.equal(parsed.kind, "result");
  assert.equal(parsed.value.sessionId, "session-1");
});
