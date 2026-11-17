#!/usr/bin/env python3
"""Check that the .NET CI investigation plugins are installed in GitHub Copilot CLI.

Read-only: prints the commands needed to add missing marketplaces and install or enable plugins.
"""

from __future__ import annotations

import argparse
import json
import re
import shutil
import subprocess
import sys

MARKETPLACES = {
    "dotnet-arcade-skills": "dotnet/arcade-skills",
    "dotnet-agent-skills": "dotnet/skills",
}

# Plugins that help investigate the failures this skill surfaces.
RECOMMENDED = {
    "dotnet-dnceng@dotnet-arcade-skills": "ci-analysis, Helix and known-issue history for failing PR builds",
    "dotnet-helix@dotnet-arcade-skills": "deep Helix and Azure DevOps investigation through the hlx MCP server",
    "dotnet-binlog@dotnet-arcade-skills": "MSBuild binlog analysis of failed Azure DevOps builds",
    "dotnet-codeflow@dotnet-arcade-skills": "Maestro subscriptions and dotnet/dotnet codeflow health",
    "dotnet-msbuild@dotnet-agent-skills": "MSBuild build failure diagnosis",
    "dotnet-test@dotnet-agent-skills": "running and diagnosing .NET tests",
    "dotnet-diag@dotnet-agent-skills": "crash dumps, hangs and runtime diagnostics",
}

PLUGIN_LINE = re.compile(r"^\s*[•*-]\s*(\S+@\S+)(?:\s+\(v[^)]*\))?(\s+\[disabled\])?\s*$")
MARKETPLACE_LINE = re.compile(r"^\s*[•*◆-]\s*(\S+)\s+\(GitHub:\s*([^)]+)\)")


def run(args: list[str]) -> str:
    result = subprocess.run(args, capture_output=True, text=True)
    if result.returncode != 0:
        raise RuntimeError(f"{' '.join(args)} failed: {result.stderr.strip()}")
    return result.stdout


def parse_plugins(text: str) -> dict[str, bool]:
    """Installed plugin -> enabled."""
    plugins = {}
    for line in text.splitlines():
        match = PLUGIN_LINE.match(line)
        if match:
            plugins[match.group(1)] = match.group(2) is None
    return plugins


def parse_marketplaces(text: str) -> dict[str, str]:
    return {m.group(1): m.group(2).strip() for m in map(MARKETPLACE_LINE.match, text.splitlines()) if m}


def check(plugins: dict[str, bool], marketplaces: dict[str, str]) -> dict[str, object]:
    missing_marketplaces = [name for name, repo in MARKETPLACES.items()
                            if marketplaces.get(name, "").lower() != repo]
    missing = [p for p in RECOMMENDED if p not in plugins]
    disabled = [p for p in RECOMMENDED if plugins.get(p) is False]
    commands = [f"copilot plugin marketplace add {MARKETPLACES[m]}" for m in missing_marketplaces]
    commands += [f"copilot plugin install {p}" for p in missing]
    commands += [f"copilot plugin enable {p}" for p in disabled]
    return {
        "missingMarketplaces": missing_marketplaces,
        "missingPlugins": missing,
        "disabledPlugins": disabled,
        "commands": commands,
        "ready": not commands,
    }


def render(result: dict[str, object]) -> str:
    if result["ready"]:
        return "All recommended .NET investigation plugins are installed and enabled.\n"
    lines = ["Recommended .NET investigation plugins are missing or disabled:", ""]
    for plugin in result["missingPlugins"]:
        lines.append(f"- missing: `{plugin}` ({RECOMMENDED[plugin]})")
    for plugin in result["disabledPlugins"]:
        lines.append(f"- disabled: `{plugin}` ({RECOMMENDED[plugin]})")
    lines += ["", "Run these in a terminal, then restart Copilot CLI:", "", "```bash", *result["commands"], "```", ""]
    return "\n".join(lines)


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--format", choices=["markdown", "json"], default="markdown")
    args = parser.parse_args(argv)
    copilot = shutil.which("copilot")
    if not copilot:
        print("GitHub Copilot CLI (`copilot`) isn't on PATH; can't check installed plugins.", file=sys.stderr)
        return 2
    result = check(parse_plugins(run([copilot, "plugin", "list"])),
                   parse_marketplaces(run([copilot, "plugin", "marketplace", "list"])))
    print(json.dumps(result, indent=2) if args.format == "json" else render(result), end="")
    return 0


if __name__ == "__main__":
    sys.exit(main())
