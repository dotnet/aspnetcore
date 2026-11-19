#!/usr/bin/env python3
"""Build the full aspnet-build triage report as HTML and email it.

Collects every category (a failing category is reported in the email instead of aborting), renders
the markdown inventory as an HTML email and sends it with the configured transport:

- outlook: the signed-in Outlook desktop client (AppleScript on macOS with classic Outlook, COM on
  Windows with classic Outlook). Sends from the user's own mailbox; no secrets needed.
- webhook: POSTs {"to", "subject", "html", "markdown"} as JSON to a URL, such as a Power Automate
  "When an HTTP request is received" flow that sends the email.
- smtp: any SMTP server with STARTTLS. The password comes from ABT_SMTP_PASSWORD.
- file: only writes the HTML (also what --dry-run does).

Settings are read from a per-user config file (see --print-config-path) and can be overridden on the
command line; --save-config stores the effective settings. Secrets never belong in the repository.
"""

from __future__ import annotations

import argparse
import html
import json
import os
import platform
import re
import smtplib
import subprocess
import sys
import tempfile
import urllib.request
from datetime import datetime, timezone
from email.message import EmailMessage
from pathlib import Path
from typing import Any

import get_attention_inventory as inventory

TRANSPORTS = ("outlook", "webhook", "smtp", "file")
# PR next actions that mean the build-duty person has something to do now.
ACTIONABLE = {"investigate-ci", "retry-ci", "resolve-conflicts", "ready-to-merge", "needs-approval", "address-feedback"}


def config_dir() -> Path:
    if os.name == "nt":
        return Path(os.environ.get("APPDATA", Path.home())) / "aspnet-build-triage"
    return Path(os.environ.get("XDG_CONFIG_HOME", Path.home() / ".config")) / "aspnet-build-triage"


def config_path() -> Path:
    return config_dir() / "report.json"


def load_config(path: Path | None = None) -> dict[str, Any]:
    path = path or config_path()
    return json.loads(path.read_text()) if path.exists() else {}


def save_config(config: dict[str, Any], path: Path | None = None) -> Path:
    path = path or config_path()
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(config, indent=2) + "\n")
    if os.name != "nt":
        path.chmod(0o600)  # may hold a webhook URL, which is a bearer secret
    return path


# --- Rendering -------------------------------------------------------------------------------

LINK = re.compile(r"\[([^\]]+)\]\((https?://[^)\s]+)\)")
BARE_URL = re.compile(r"https?://[^\s<)]+[^\s<).,;:]")
CODE = re.compile(r"`([^`]+)`")
BOLD = re.compile(r"\*\*(.+?)\*\*")
ITALIC = re.compile(r"(?<![\w])_(.+?)_(?![\w])")
CELL = 'style="border:1px solid #d0d7de;padding:4px 8px;text-align:left"'


def inline(text: str) -> str:
    """Escape and render the inline markdown the collectors emit (code, bold, italics, links, URLs)."""
    tokens: list[str] = []

    def stash(rendered: str) -> str:
        tokens.append(rendered)
        return f"\x00{len(tokens) - 1}\x00"

    def href(url: str) -> str:
        return html.escape(url, quote=True)

    text = LINK.sub(lambda m: stash(f'<a href="{href(m.group(2))}">{inline(m.group(1))}</a>'), text)
    text = CODE.sub(lambda m: stash(f'<code style="background:#f3f3f3;padding:0 3px">{html.escape(m.group(1))}</code>'), text)
    text = BARE_URL.sub(lambda m: stash(f'<a href="{href(m.group(0))}">{html.escape(m.group(0))}</a>'), text)
    text = html.escape(text, quote=False)
    text = BOLD.sub(r"<strong>\1</strong>", text)
    text = ITALIC.sub(r"<em>\1</em>", text)
    while "\x00" in text:
        text = re.sub("\x00(\\d+)\x00", lambda m: tokens[int(m.group(1))], text)
    return text


def split_row(line: str) -> list[str]:
    return [c.strip() for c in line.strip().strip("|").split("|")]


def markdown_to_html(markdown: str) -> str:
    """Convert the subset of markdown the collectors produce: headings, tables, nested bullets, paragraphs."""
    out: list[str] = []
    lines = markdown.splitlines()
    i = 0
    while i < len(lines):
        line = lines[i]
        heading = re.match(r"(#{1,4}) (.*)", line)
        if heading:
            level = len(heading.group(1))
            out.append(f"<h{level}>{inline(heading.group(2))}</h{level}>")
            i += 1
        elif line.startswith("|"):
            rows = []
            while i < len(lines) and lines[i].startswith("|"):
                rows.append(split_row(lines[i]))
                i += 1
            header, body = rows[0], [r for r in rows[1:] if not all(re.fullmatch(r":?-+:?", c) for c in r)]
            out.append('<table style="border-collapse:collapse;margin:4px 0 8px">')
            out.append("<tr>" + "".join(f"<th {CELL}>{inline(c)}</th>" for c in header) + "</tr>")
            out += ["<tr>" + "".join(f"<td {CELL}>{inline(c)}</td>" for c in r) + "</tr>" for r in body]
            out.append("</table>")
        elif re.match(r"\s*- ", line):
            depth = -1
            while i < len(lines) and re.match(r"\s*- ", lines[i]):
                item = re.match(r"(\s*)- (.*)", lines[i])
                level = len(item.group(1)) // 2
                while depth < level:
                    out.append("<ul>")
                    depth += 1
                while depth > level:
                    out.append("</ul>")
                    depth -= 1
                out.append(f"<li>{inline(item.group(2))}</li>")
                i += 1
            out += ["</ul>"] * (depth + 1)
        elif line.strip():
            para = []
            while i < len(lines) and lines[i].strip() and not re.match(r"(#{1,4} |\||\s*- )", lines[i]):
                para.append(inline(lines[i]))
                i += 1
            out.append("<p>" + "<br>".join(para) + "</p>")
        else:
            i += 1
    return "\n".join(out)


def summarize(result: dict[str, Any]) -> dict[str, Any]:
    """Counts for the subject line and the summary at the top of the email."""
    rows = []
    for category in result["categories"]:
        items = [i for i in result["items"] if i["category"] == category]
        actionable = [i for i in items if i.get("nextAction") in ACTIONABLE]
        rows.append({"category": category, "title": inventory.CATEGORIES[category], "count": len(items),
                     "actionable": len(actionable), "error": result.get("errors", {}).get(category)})
    by = {r["category"]: r for r in rows}
    prs = sum(r["actionable"] for r in rows if r["category"] not in ("component-governance", "workflow-failures"))
    return {"rows": rows, "actionablePrs": prs,
            "cgAlerts": by.get("component-governance", {}).get("count", 0),
            "workflowFailures": by.get("workflow-failures", {}).get("count", 0),
            "errors": len(result.get("errors", {}))}


def subject(result: dict[str, Any], summary: dict[str, Any]) -> str:
    date = datetime.fromisoformat(result["generatedAt"]).astimezone().strftime("%a %b %d")
    def count(n: int, noun: str) -> str:
        return f"{n} {noun}{'' if n == 1 else 's'}"

    parts = [count(summary["actionablePrs"], "PR") + " to act on"]
    if "component-governance" in result["categories"]:
        parts.append(count(summary["cgAlerts"], "CG alert"))
    if "workflow-failures" in result["categories"]:
        parts.append(count(summary["workflowFailures"], "workflow failure"))
    if summary["errors"]:
        parts.append(count(summary["errors"], "section") + " not collected")
    return f"aspnet-build triage report ({date}): " + ", ".join(parts)


def render_html(result: dict[str, Any], markdown: str, summary: dict[str, Any]) -> str:
    rows = "".join(
        f"<tr><td {CELL}>{html.escape(r['title'])}</td>"
        f"<td {CELL}>{'not collected' if r['error'] else r['count']}</td>"
        f"<td {CELL}>{'' if r['error'] or r['category'] in ('component-governance', 'workflow-failures') else r['actionable']}</td></tr>"
        for r in summary["rows"])
    table = (f'<table style="border-collapse:collapse"><tr><th {CELL}>Category</th><th {CELL}>Open</th>'
             f"<th {CELL}>Need action</th></tr>{rows}</table>")
    body = markdown_to_html(markdown)
    return (
        '<html><body style="font-family:Segoe UI,Helvetica,Arial,sans-serif;font-size:14px;color:#1f2328">'
        f"<h2>Summary</h2>{table}"
        "<p>Ask the aspnet-build-triage skill to investigate, retry or walk you through any of these. "
        "Remember to elevate at "
        f'<a href="{inventory.ADMIN_ELEVATION_URL}">{inventory.ADMIN_ELEVATION_URL}</a> '
        "before acting on <code>release/*</code> PRs.</p><hr>"
        f"{body}</body></html>")


def build_report(categories: list[str] | None = None, now: datetime | None = None) -> dict[str, Any]:
    result = inventory.collect(categories or list(inventory.CATEGORIES), now or datetime.now(timezone.utc),
                               keep_going=True)
    markdown = inventory.render_markdown(result)
    summary = summarize(result)
    return {"result": result, "markdown": markdown, "summary": summary,
            "subject": subject(result, summary), "html": render_html(result, markdown, summary)}


# --- Transports ------------------------------------------------------------------------------

OUTLOOK_APPLESCRIPT = """
on run argv
    set subj to item 1 of argv
    set htmlBody to read (POSIX file (item 2 of argv)) as «class utf8»
    tell application "Microsoft Outlook"
        set msg to make new outgoing message with properties {subject:subj, content:htmlBody}
        repeat with addr in (items 3 thru -1 of argv)
            make new to recipient at msg with properties {email address:{address:(contents of addr)}}
        end repeat
        send msg
    end tell
end run
"""

OUTLOOK_POWERSHELL = """
$ErrorActionPreference = 'Stop'
$outlook = New-Object -ComObject Outlook.Application
$mail = $outlook.CreateItem(0)
$mail.To = $env:ABT_TO
$mail.Subject = $env:ABT_SUBJECT
$mail.HTMLBody = [IO.File]::ReadAllText($env:ABT_HTML_PATH)
$mail.Send()
"""


def send_outlook(to: list[str], subj: str, body: str) -> None:
    with tempfile.NamedTemporaryFile("w", suffix=".html", delete=False, encoding="utf-8") as f:
        f.write(body)
        path = f.name
    try:
        system = platform.system()
        if system == "Darwin":
            result = subprocess.run(["osascript", "-e", OUTLOOK_APPLESCRIPT, subj, path, *to],
                                    capture_output=True, text=True)
            hint = ("Classic Outlook for Mac is required (New Outlook has no AppleScript support), and the "
                    "terminal or launchd job needs Automation permission for Outlook.")
        elif system == "Windows":
            env = dict(os.environ, ABT_TO="; ".join(to), ABT_SUBJECT=subj, ABT_HTML_PATH=path)
            result = subprocess.run(["powershell", "-NoProfile", "-NonInteractive", "-Command", OUTLOOK_POWERSHELL],
                                    capture_output=True, text=True, env=env)
            hint = "Classic Outlook for Windows is required (New Outlook has no COM automation)."
        else:
            raise RuntimeError("The outlook transport needs macOS or Windows; use webhook or smtp.")
        if result.returncode != 0:
            raise RuntimeError(f"Outlook send failed: {result.stderr.strip()[:500]}\n{hint}")
    finally:
        os.unlink(path)


def send_webhook(url: str, to: list[str], subj: str, body: str, markdown: str) -> None:
    payload = json.dumps({"to": "; ".join(to), "subject": subj, "html": body, "markdown": markdown}).encode()
    request = urllib.request.Request(url, data=payload, headers={"Content-Type": "application/json"}, method="POST")
    with urllib.request.urlopen(request, timeout=60) as response:
        if response.status >= 300:
            raise RuntimeError(f"Webhook returned HTTP {response.status}")


def send_smtp(smtp: dict[str, Any], to: list[str], subj: str, body: str, markdown: str) -> None:
    password = os.environ.get("ABT_SMTP_PASSWORD")
    message = EmailMessage()
    message["Subject"] = subj
    message["From"] = smtp.get("from") or smtp.get("username") or to[0]
    message["To"] = ", ".join(to)
    message.set_content(markdown)
    message.add_alternative(body, subtype="html")
    with smtplib.SMTP(smtp["host"], int(smtp.get("port", 587)), timeout=60) as server:
        server.starttls()
        if smtp.get("username"):
            if not password:
                raise RuntimeError("Set ABT_SMTP_PASSWORD for the smtp transport.")
            server.login(smtp["username"], password)
        server.send_message(message)


def deliver(config: dict[str, Any], report: dict[str, Any]) -> None:
    transport, to = config["transport"], config["to"]
    if transport == "outlook":
        send_outlook(to, report["subject"], report["html"])
    elif transport == "webhook":
        url = os.environ.get("ABT_REPORT_WEBHOOK_URL") or config.get("webhookUrl")
        if not url:
            raise RuntimeError("Set webhookUrl in the config file or ABT_REPORT_WEBHOOK_URL.")
        send_webhook(url, to, report["subject"], report["html"], report["markdown"])
    elif transport == "smtp":
        if not config.get("smtp", {}).get("host"):
            raise RuntimeError("Configure smtp.host (and optionally port, username, from) in the config file.")
        send_smtp(config["smtp"], to, report["subject"], report["html"], report["markdown"])


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--to", action="append", help="Recipient address (repeatable). Overrides the config.")
    parser.add_argument("--transport", choices=TRANSPORTS, help="How to send. Overrides the config.")
    parser.add_argument("--webhook-url", help="Webhook URL for the webhook transport. Prefer the config file.")
    parser.add_argument("--smtp-host")
    parser.add_argument("--smtp-port", type=int)
    parser.add_argument("--smtp-username")
    parser.add_argument("--smtp-from")
    parser.add_argument("--category", action="append", choices=list(inventory.CATEGORIES),
                        help="Limit to categories (repeatable; default: all).")
    parser.add_argument("--output", type=Path, help="Also write the HTML report to this path.")
    parser.add_argument("--dry-run", action="store_true", help="Build the report and write the HTML; don't send.")
    parser.add_argument("--save-config", action="store_true", help="Store the effective settings in the config file.")
    parser.add_argument("--config", type=Path, help="Config file path (default: per-user config).")
    parser.add_argument("--print-config-path", action="store_true")
    args = parser.parse_args(argv)

    path = args.config or config_path()
    if args.print_config_path:
        print(path)
        return 0
    config = load_config(path)
    if args.to:
        config["to"] = args.to
    if args.transport:
        config["transport"] = args.transport
    if args.webhook_url:
        config["webhookUrl"] = args.webhook_url
    smtp = {k: v for k, v in {"host": args.smtp_host, "port": args.smtp_port, "username": args.smtp_username,
                              "from": args.smtp_from}.items() if v}
    if smtp:
        config["smtp"] = {**config.get("smtp", {}), **smtp}
    config.setdefault("transport", "outlook")
    if args.save_config:
        print(f"Saved settings to {save_config(config, path)}")
    if not config.get("to") and config["transport"] != "file" and not args.dry_run:
        parser.error(f"No recipient. Pass --to (and --save-config to remember it) or set 'to' in {path}.")

    print(f"[{datetime.now().astimezone().isoformat(timespec='seconds')}] Building the aspnet-build triage report")
    report = build_report(args.category)
    output = args.output
    if args.dry_run or config["transport"] == "file":
        output = output or Path(tempfile.gettempdir()) / "aspnet-build-triage-report.html"
    if output:
        output.write_text(report["html"], encoding="utf-8")
        print(f"Wrote {output}")
    print(report["subject"])
    if args.dry_run or config["transport"] == "file":
        return 0
    deliver(config, report)
    print(f"Sent via {config['transport']} to {', '.join(config['to'])}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
