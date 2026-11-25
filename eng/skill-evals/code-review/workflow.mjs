import assert from "node:assert/strict";

// The SDK can report the CLI's rg command as grep in tool metadata.
export const runtimeToolName = name => name === "rg" ? "grep" : name;
export const reviewToolNames = tools => [...new Set(tools.flatMap(name => [name, runtimeToolName(name)]))];

export function reviewProfile(source) {
    const text = source.replaceAll("\r\n", "\n");
    const model = /^model: ([\w.-]+)$/m.exec(text)?.[1];
    const cliVersion = /^engine:\n  id: copilot\n(?:  #[^\n]*\n)*  version: "(\d+\.\d+\.\d+)"/m.exec(text)?.[1];
    const tools = /^    - --available-tools=([\w,-]+)$/m.exec(text)?.[1]?.split(",")
        .filter(tool => !tool.startsWith("safeoutputs-")).sort();
    assert.ok(model && cliVersion && tools, "Cannot read the /review runtime profile");
    assert.deepEqual(tools, ["glob", "rg", "skill", "view"], "Review the offline tool boundary before adopting a workflow tool change");

    const prompt = /\n## Invoke the skill and consume the trusted bundle\n\n([\s\S]+?)\nAll GitHub-visible output/.exec(text)?.[1];
    assert.ok(prompt, "Cannot identify the /review analysis prompt");
    const hostedBundle = /The bundle is\n`\/tmp\/gh-aw\/review-bundle\/manifest\.json`, prepared before you started by a producer fetched\nat the immutable workflow revision\. Require its complete version-2 readiness, the target head\n`\$\{\{ needs\.freeze_pr_head\.outputs\.head_sha \}\}`, and the immutable provenance recorded in the bundle\./;
    const hostedResults = "Determine the structured result before selecting the safe output below. Return it in the final message.";
    const hostedGitHub = "No live GitHub tool is available to the agent;\nthe trusted safe-output dependency checks the live head after agent completion.";
    for (const boundary of [hostedBundle, hostedResults, hostedGitHub]) {
        assert.ok(typeof boundary === "string" ? prompt.includes(boundary) : boundary.test(prompt),
            "The hosted review instructions changed; update the offline bundle or publication adaptation");
    }

    return {
        model, cliVersion, tools,
        prompt: prompt.replace(hostedBundle, "The bundle is `bundle/manifest.json` in the trial workspace. Require complete version-2 readiness.")
            .replace(hostedGitHub, "No live GitHub tool is available to the agent.")
            .replace(hostedResults, "Return the structured result in the final message. Do not publish."),
    };
}

export function reviewSpec({ name, runs, ids }, { prompt, model, cliVersion, tools }) {
    return {
        name,
        description: "Frozen code-only review corpus",
        type: "capability",
        defaults: { executor: "readonly-copilot", runs, model, timeout: "900s" },
        scoring: { weights: { "tool-calls": 1 }, threshold: 1 },
        agent_environment: {
            env: { REVIEW_EXPECTED_CLI_VERSION: cliVersion, REVIEW_TOOLS: tools.join(",") },
            skills: [],
            files: [{ src: "SKILL.md", dest: ".github/skills/code-review/SKILL.md" }],
        },
        stimuli: ids.map(id => ({
            name: id,
            prompt,
            agent_environment: { files: [{ src: `bundles/${id}`, dest: "bundle" }] },
            graders: [{ type: "tool-calls", config: {
                disallowed: [{ name: `^(?!(?:${reviewToolNames(tools).join("|")})$).+` }],
            } }],
        })),
    };
}
