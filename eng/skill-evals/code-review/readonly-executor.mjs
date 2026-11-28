import assert from "node:assert/strict";
import { createRequire } from "node:module";
import { resolve } from "node:path";
import { pathToFileURL } from "node:url";
import { runtimeToolName } from "./workflow.mjs";

assert.ok(process.env.VALLY_TOOLS, "Set VALLY_TOOLS to the evaluation-tools directory");
const require = createRequire(resolve(process.env.VALLY_TOOLS, "package.json"));
const load = name => import(pathToFileURL(require.resolve(name)));
const { CopilotClient } = await load("@github/copilot-sdk");
const { CopilotSdkExecutor } = await load("@microsoft/vally/executor");

export class ReadonlyClient extends CopilotClient {
    constructor(env, tools, telemetry, traceContext) {
        assert.ok(process.getuid?.() > 0, "Run this eval as an unprivileged user");
        const { REVIEW_EXPECTED_CLI_VERSION, REVIEW_TOOLS, ...runtimeEnv } = env;
        super({ env: runtimeEnv, telemetry, onGetTraceContext: traceContext });
        this.tools = tools;
        this.expectedVersion = REVIEW_EXPECTED_CLI_VERSION;
        assert.match(this.expectedVersion ?? "", /^\d+\.\d+\.\d+$/);
        assert.ok(runtimeEnv.COPILOT_CLI_PATH, "Set COPILOT_CLI_PATH to the pinned CLI binary");
    }

    async createSession(config) {
        const { version } = await this.getStatus();
        assert.equal(version, this.expectedVersion, "Unexpected Copilot CLI version");
        const session = await super.createSession({
            ...config,
            availableTools: this.tools,
            requestExtensions: false,
        });
        try {
            await session.rpc.tools.initializeAndValidate();
            const { tools } = await session.rpc.tools.getCurrentMetadata();
            assert.deepEqual(tools.map(tool => runtimeToolName(tool.name)).sort(), this.tools.map(runtimeToolName).sort(),
                "Unexpected runtime tool set");
            return session;
        } catch (error) {
            await session.disconnect();
            throw error;
        }
    }

    async resumeSession() {
        throw new Error("Every trial requires a fresh session");
    }
}

export function registerExecutors(registry) {
    const executor = new CopilotSdkExecutor({
        createClient() {
            throw new Error("A review trial requires an isolated environment");
        },
        createEnvClient(env, telemetry, traceContext) {
            const tools = env.REVIEW_TOOLS?.split(",");
            assert.deepEqual(tools, ["glob", "rg", "skill", "view"], "Unexpected review tool profile");
            return new ReadonlyClient(env, tools, telemetry, traceContext);
        },
    });
    executor.name = "readonly-copilot";
    registry.register(executor);
}
