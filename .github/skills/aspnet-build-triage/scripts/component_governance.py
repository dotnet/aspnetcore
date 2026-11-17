"""Read the .NET support policy and Azure DevOps Component Governance."""

import json
import re
import subprocess
from concurrent.futures import ThreadPoolExecutor
from html.parser import HTMLParser
from urllib.parse import quote, urlencode
from urllib.request import urlopen

SUPPORT_POLICY = "https://dotnet.microsoft.com/en-us/platform/support/policy"
DASHBOARD = "https://dev.azure.com/dnceng/internal/_componentGovernance/dotnet-aspnetcore"
API = "https://governance.dev.azure.com/dnceng/internal/_apis/ComponentGovernance/GovernedRepositories/102242"
AZURE_DEVOPS_RESOURCE = "499b84ac-1321-427f-aa17-267ca6975798"
VERSION_DETAILS = "https://raw.githubusercontent.com/dotnet/aspnetcore/{branch}/eng/Version.Details.xml"
# Runtime, targeting and host packs come from the SDK pinned in global.json, which only changes via
# dotnet/dotnet source updates. aspnetcore can't pick their versions directly.
SDK_PACK = re.compile(
    r"Microsoft\.(NETCore|AspNetCore|WindowsDesktop)\.App\.(Runtime|Ref|Host|Crossgen2)(\..+)?"
    r"|(runtime\.[^.]+\.)?Microsoft\.DotNet\.ILCompiler|Microsoft\.NET\.ILLink\.Tasks",
    re.IGNORECASE)
MIRROR_PIPELINE = "https://dev.azure.com/dnceng/internal/_build?definitionId=931&_a=summary"


class SupportedVersions(HTMLParser):
    def __init__(self):
        super().__init__()
        self.in_table = False
        self.cell = None
        self.rows = []

    def handle_starttag(self, tag, attrs):
        if tag == "table":
            self.in_table = dict(attrs).get("aria-labelledby") == "supported-versions"
        elif self.in_table and tag == "tr":
            self.rows.append([])
        elif self.in_table and tag in ("td", "th"):
            self.cell = []

    def handle_data(self, data):
        if self.cell is not None:
            self.cell.append(data)

    def handle_endtag(self, tag):
        if tag == "table":
            self.in_table = False
        elif tag in ("td", "th") and self.cell is not None:
            self.rows[-1].append(" ".join("".join(self.cell).split()))
            self.cell = None


def supported_release_branches(html):
    parser = SupportedVersions()
    parser.feed(html)
    if not parser.rows or not {"Version", "Support phase"}.issubset(parser.rows[0]):
        raise ValueError("Could not read the supported versions table")
    version_index = parser.rows[0].index("Version")
    phase_index = parser.rows[0].index("Support phase")
    branches = []
    for row in parser.rows[1:]:
        if len(row) != len(parser.rows[0]):
            raise ValueError("Unexpected row in the supported versions table")
        if row[phase_index].casefold() not in {"active", "maintenance"}:
            continue
        match = re.fullmatch(r"\.NET(?: Core)? (\d+)(?:\.(\d+))?", row[version_index])
        if not match:
            raise ValueError("Unrecognized in-support .NET version")
        branches.append(f"release/{match[1]}.{match[2] or '0'}")
    return list(dict.fromkeys(branches))


def fetch_support_policy():
    with urlopen(SUPPORT_POLICY, timeout=20) as response:
        return response.read().decode("utf-8")


def azure_get(path):
    try:
        result = subprocess.run(
            ["az", "rest", "--resource", AZURE_DEVOPS_RESOURCE, "--method", "get",
             "--url", f"{API}/{path}?api-version=7.1-preview.1",
             "--only-show-errors", "-o", "json"],
            check=True, capture_output=True, text=True, timeout=30,
        )
    except FileNotFoundError:
        raise ValueError("Azure CLI is required for Component Governance") from None
    except (subprocess.CalledProcessError, subprocess.TimeoutExpired):
        raise ValueError("Azure DevOps request failed; verify Azure CLI login and Component Governance access") from None
    return json.loads(result.stdout)


def alert_summaries(branch):
    # The CG route decodes a branch name containing '/' twice.
    encoded = quote(quote(branch, safe=""), safe="")
    data = azure_get(f"Branches/{encoded}/AlertSummary")
    alerts = data["value"]
    if not isinstance(alerts, list) or data["count"] != len(alerts):
        raise ValueError("Component Governance returned an incomplete alert summary")
    return [
        alert for alert in alerts
        if alert["alertState"] == "active" and not alert["isDismissed"]
        and alert["severity"] in {"critical", "high"}
        # Legal/license alerts are not owned by aspnet-build.
        and alert["type"] == "security"
    ]


def alert_details(alert_id):
    detail = azure_get(f"Alerts/{alert_id}")
    # actionItems is CG's remediation hint, e.g. "Upgrade X from 1.0 to 1.1 to fix the vulnerability."
    return detail["component"]["type"].casefold(), " ".join(str(detail.get("actionItems") or "").split())


def summarize_remediations(hints):
    """Collapse CG actionItems into one hint, keeping the highest 'Upgrade X from A to B' target."""
    upgrades = {}
    other = []
    for hint in hints:
        match = re.fullmatch(r"Upgrade (\S+) from (\S+) to (\S+) to fix the vulnerability\.", hint)
        if match:
            target = upgrades.get(match[1])
            if target is None or version_key(match[3]) > version_key(target):
                upgrades[match[1]] = match[3]
        elif "has not been provided" in hint:
            other.append("No fix version in Component Governance; check the advisory")
        elif hint:
            other.append(hint)
    parts = [f"Upgrade {package} to at least {version}" for package, version in upgrades.items()]
    return "; ".join(list(dict.fromkeys(parts + other))) or "No remediation hint; check the advisory"


def version_key(version):
    return [int(part) if part.isdigit() else part for part in re.split(r"[.-]", version)]


def version_details_dependencies(xml):
    """Return {(name, version)} for dependencies Maestro manages via eng/Version.Details.xml."""
    import xml.etree.ElementTree as ET
    root = ET.fromstring(xml)
    return {(d.get("Name", "").casefold(), d.get("Version", "")) for d in root.iter("Dependency")}


def fetch_version_details(branch):
    with urlopen(VERSION_DETAILS.format(branch=quote(branch)), timeout=20) as response:
        return version_details_dependencies(response.read())


def managed_by_dotnet_dotnet(package, version, dependencies):
    """True if the alerting package version is supplied by dotnet/dotnet dependency flow."""
    if SDK_PACK.fullmatch(package):
        return True
    # Name and version must both match; e.g. a transitive Pkcs 8.0.1 is not the flowed Pkcs 11.0.1.
    return dependencies is not None and (package.casefold(), version) in dependencies


def collect():
    errors = {}
    branches = ["main"]
    try:
        branches += supported_release_branches(fetch_support_policy())
    except (OSError, ValueError) as error:
        errors["release branch discovery"] = str(error)

    summaries = {}
    ecosystems = {}
    remediations = {}
    with ThreadPoolExecutor(max_workers=6) as pool:
        branch_requests = {branch: pool.submit(alert_summaries, branch) for branch in branches}
        for branch, request in branch_requests.items():
            try:
                summaries[branch] = request.result()
            except (ValueError, KeyError, TypeError) as error:
                errors[branch] = str(error)

        flow_requests = {branch: pool.submit(fetch_version_details, branch) for branch in summaries}
        flowed = {}
        for branch, request in flow_requests.items():
            try:
                flowed[branch] = request.result()
            except Exception as error:  # noqa: BLE001 - fall back to manual guidance, but say so
                flowed[branch] = None
                errors[f"{branch} Version.Details.xml"] = str(error)

        ids = sorted({alert["alertId"] for alerts in summaries.values() for alert in alerts})
        detail_requests = {alert_id: pool.submit(alert_details, alert_id) for alert_id in ids}
        for alert_id, request in detail_requests.items():
            try:
                ecosystems[alert_id], remediations[alert_id] = request.result()
            except (ValueError, KeyError, TypeError, AttributeError) as error:
                errors[f"alert {alert_id} ecosystem"] = str(error)

    items = []
    other_ecosystems = 0
    for branch, alerts in summaries.items():
        for alert in sorted(alerts, key=lambda a: (a["severity"], a["componentName"].casefold(), a["alertId"])):
            alert_id = alert["alertId"]
            ecosystem = ecosystems.get(alert_id, "unknown")
            if ecosystem not in {"npm", "nuget", "unknown"}:
                other_ecosystems += 1
                continue
            if ecosystem == "npm":
                action = "Use update-npm-dependencies for a targeted fix"
            elif ecosystem == "nuget" and managed_by_dotnet_dotnet(
                    alert["componentName"], alert["componentVersion"], flowed.get(branch)):
                action = "Wait for the next dotnet/dotnet source update; don't update manually"
            elif ecosystem == "nuget":
                action = "Open a dependency-update PR after checking quarantine and dotnet-public"
            else:
                action = "Confirm package ecosystem in Component Governance"
            items.append({
                "category": "component-governance", "id": alert_id, "base": branch,
                "severity": alert["severity"], "ecosystem": ecosystem,
                "package": alert["componentName"], "version": alert["componentVersion"],
                "title": alert["title"], "remediation": remediations.get(alert_id, ""), "nextAction": action,
                "requiresAdminElevation": branch.startswith("release/"),
                "url": f"{DASHBOARD}?{urlencode({'branch': branch, 'alertId': alert_id})}",
            })
    return items, {"branches": branches, "checkedBranches": list(summaries), "errors": errors,
                   "otherEcosystemAlertOccurrences": other_ecosystems}


def render_markdown(items, coverage):
    def cell(value):
        return str(value).replace("|", r"\|").replace("\r", " ").replace("\n", " ")

    lines = ["Scope: active, non-dismissed high and critical security alerts across all build phases (legal alerts excluded).",
             f"[Support policy]({SUPPORT_POLICY}) · [Component Governance]({DASHBOARD})", ""]
    if coverage["errors"]:
        lines += ["**Incomplete Component Governance results:**"] + [
            f"- {scope}: {cell(error)}" for scope, error in coverage["errors"].items()
        ] + [""]
    for branch in coverage["checkedBranches"]:
        alerts = [item for item in items if item["base"] == branch]
        lines += [f"### `{branch}` ({len(alerts)})", ""]
        if not alerts:
            lines += ["_No matching alerts found._", ""]
            continue
        lines += ["| Alert | Severity | Package | Next action |", "|:--|:--|:--|:--|"]
        for item in alerts:
            package = f"{item['package']} {item['version']} ({item['ecosystem']})"
            lines.append(f"| [{item['id']}]({item['url']}) | {item['severity']} | "
                         f"{cell(package)} | {item['nextAction']} |")
        lines.append("")
        packages = {}
        for item in alerts:
            entry = packages.setdefault((item["package"], item["version"]), {"titles": [], "fixes": []})
            for key, value in (("titles", item["title"]), ("fixes", item["remediation"])):
                if value and value not in entry[key]:
                    entry[key].append(value)
        for (package, version), entry in packages.items():
            fixes = summarize_remediations(entry["fixes"])
            lines.append(f"- {cell(package)} {cell(version)} ({cell(', '.join(entry['titles']))}): {cell(fixes)}")
        lines.append("")
    if coverage["otherEcosystemAlertOccurrences"]:
        lines += [f"Other ecosystem alert occurrences omitted: {coverage['otherEcosystemAlertOccurrences']}.", ""]
    if any(item["nextAction"].startswith("Wait for the next dotnet/dotnet") for item in items):
        lines += ["**dotnet/dotnet-managed packages:** These versions come from the SDK in `global.json` or "
                  "from `eng/Version.Details.xml`, which Maestro updates through `Source code updates from "
                  "dotnet/dotnet` PRs. Don't bump them by hand; merge the next codeflow PR for the branch. "
                  "Escalate only if the alert remains after a codeflow that should have fixed it.", ""]
    if any(item["nextAction"].startswith("Open a dependency-update PR") for item in items):
        lines += ["**NuGet:** Verify the patched version's publish time. Wait until it is at least "
                  "seven days old and verify the exact version is on `dotnet-public` before updating. "
                  f"If missing, use the [mirroring pipeline]({MIRROR_PIPELINE}); never queue it without an explicit request.", ""]
    return lines
