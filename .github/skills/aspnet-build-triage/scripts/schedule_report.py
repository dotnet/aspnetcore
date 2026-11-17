#!/usr/bin/env python3
"""Schedule the aspnet-build triage report email on any cadence.

The cadence is a standard 5-field cron expression in local time ("minute hour day-of-month month
day-of-week"), e.g. "0 8 * * 1-5" for 8:00 AM on weekdays. Installing copies the skill's scripts to
a per-user directory, so switching branches in the repository doesn't break the job; rerun install
to pick up script updates.

- macOS: a launchd LaunchAgent (runs in the login session, so it can use the keychain and Outlook;
  a run missed while asleep happens on wake).
- Linux: a crontab entry.
- Windows: a Task Scheduler task. Only cadences Task Scheduler can express directly are supported
  (one time per day on all or some weekdays, or every N hours).

Send settings (recipients, transport) come from send_report.py's config file; set them first with
`send_report.py --to <address> --transport <transport> --save-config --dry-run`.
"""

from __future__ import annotations

import argparse
import itertools
import os
import platform
import plistlib
import shutil
import subprocess
import sys
from pathlib import Path
from typing import Any

import send_report

LABEL = "com.github.dotnet.aspnet-build-triage.report"
CRON_MARKER = "# aspnet-build-triage-report"
WINDOWS_TASK = "aspnet-build-triage-report"
DAY_NAMES = ["sun", "mon", "tue", "wed", "thu", "fri", "sat"]
FIELDS = [("minute", 0, 59), ("hour", 0, 23), ("day", 1, 31), ("month", 1, 12), ("weekday", 0, 7)]
MAX_LAUNCHD_ENTRIES = 1000


def parse_field(text: str, low: int, high: int, names: list[str] | None = None) -> list[int] | None:
    """Values matched by one cron field, or None for "*". Supports lists, ranges, steps and day names."""
    if text == "*":
        return None
    values: set[int] = set()
    for part in text.lower().split(","):
        base, _, step_text = part.partition("/")
        step = int(step_text) if step_text else 1
        if step < 1:
            raise ValueError(f"Invalid step in {text!r}")

        def number(token: str) -> int:
            if names and token in names:
                return names.index(token)
            return int(token)

        if base == "*":
            start, end = low, high
        elif "-" in base:
            start, end = (number(t) for t in base.split("-", 1))
        else:
            start = number(base)
            end = high if step_text else start
        if not low <= start <= end <= high:
            raise ValueError(f"{text!r} is outside {low}-{high}")
        values.update(range(start, end + 1, step))
    return sorted(values)


def parse_cron(expression: str) -> dict[str, list[int] | None]:
    parts = expression.split()
    if len(parts) != 5:
        raise ValueError("Use a 5-field cron expression: minute hour day-of-month month day-of-week")
    fields = {name: parse_field(text, low, high, DAY_NAMES if name == "weekday" else None)
              for (name, low, high), text in zip(FIELDS, parts)}
    if fields["weekday"] is not None:
        fields["weekday"] = sorted({d % 7 for d in fields["weekday"]})  # 7 is Sunday too
    if fields["day"] is not None and fields["weekday"] is not None:
        # cron ORs these two fields; launchd and Task Scheduler AND them.
        raise ValueError("Restrict day-of-month or day-of-week, not both.")
    return fields


def launchd_intervals(fields: dict[str, list[int] | None]) -> list[dict[str, int]]:
    keys = {"minute": "Minute", "hour": "Hour", "day": "Day", "month": "Month", "weekday": "Weekday"}
    restricted = [(keys[name], values) for name, values in fields.items() if values is not None]
    combos = list(itertools.islice(itertools.product(*(v for _, v in restricted)), MAX_LAUNCHD_ENTRIES + 1))
    if len(combos) > MAX_LAUNCHD_ENTRIES:
        raise ValueError("That cadence expands to too many launchd entries; use a coarser schedule.")
    return [dict(zip((k for k, _ in restricted), combo)) for combo in combos]


def schtasks_schedule(fields: dict[str, list[int] | None]) -> list[str]:
    """Task Scheduler /SC arguments for the cadences it can express directly."""
    minute, hour, weekday = fields["minute"], fields["hour"], fields["weekday"]
    if fields["day"] is not None or fields["month"] is not None or minute is None or len(minute) != 1:
        raise ValueError("On Windows, use one fixed minute and no day-of-month or month restriction.")
    if hour is not None and len(hour) == 1:
        start = ["/ST", f"{hour[0]:02d}:{minute[0]:02d}"]
        if weekday is None:
            return ["/SC", "DAILY", *start]
        return ["/SC", "WEEKLY", "/D", ",".join(DAY_NAMES[d].upper() for d in weekday), *start]
    steps = {b - a for a, b in zip(hour, hour[1:])} if hour else {1}
    full_day = hour is None or (len(steps) == 1 and hour[0] < min(steps) and 24 % min(steps) == 0
                                and len(hour) == 24 // min(steps))
    if weekday is None and full_day:
        every = min(steps)
        first = hour[0] if hour else 0
        return ["/SC", "HOURLY", "/MO", str(every), "/ST", f"{first:02d}:{minute[0]:02d}"]
    raise ValueError("Task Scheduler can't express that cadence; pick a simpler one or create the task by hand.")


def install_dir() -> Path:
    if platform.system() == "Darwin":
        return Path.home() / "Library" / "Application Support" / "aspnet-build-triage"
    if os.name == "nt":
        return Path(os.environ.get("LOCALAPPDATA", Path.home())) / "aspnet-build-triage"
    return Path(os.environ.get("XDG_DATA_HOME", Path.home() / ".local" / "share")) / "aspnet-build-triage"


def log_path() -> Path:
    return install_dir() / "report.log"


def plist_path() -> Path:
    return Path.home() / "Library" / "LaunchAgents" / f"{LABEL}.plist"


def copy_scripts(target: Path) -> Path:
    source = Path(__file__).resolve().parent
    scripts = target / "scripts"
    if scripts.exists():
        shutil.rmtree(scripts)
    shutil.copytree(source, scripts, ignore=shutil.ignore_patterns("__pycache__", "*.pyc"))
    return scripts / "send_report.py"


def command(script: Path) -> list[str]:
    return [sys.executable, str(script)]


def run(args: list[str], check: bool = True) -> subprocess.CompletedProcess[str]:
    result = subprocess.run(args, capture_output=True, text=True)
    if check and result.returncode != 0:
        raise RuntimeError(f"{' '.join(args[:2])} failed: {(result.stderr or result.stdout).strip()[:500]}")
    return result


def launchd_plist(script: Path, fields: dict[str, list[int] | None]) -> dict[str, Any]:
    return {
        "Label": LABEL,
        "ProgramArguments": command(script),
        "StartCalendarInterval": launchd_intervals(fields),
        "EnvironmentVariables": {"PATH": os.environ.get("PATH", "/usr/bin:/bin"), "HOME": str(Path.home()),
                                 "PYTHONUNBUFFERED": "1"},
        "WorkingDirectory": str(script.parent),
        "StandardOutPath": str(log_path()),
        "StandardErrorPath": str(log_path()),
        "ProcessType": "Background",
    }


def cron_line(script: Path, expression: str) -> str:
    path = os.environ.get("PATH", "/usr/bin:/bin").replace("'", "")
    cmd = " ".join(f"'{part}'" for part in command(script))
    return f"{expression} PATH='{path}' {cmd} >> '{log_path()}' 2>&1 {CRON_MARKER}"


def current_crontab() -> list[str]:
    result = run(["crontab", "-l"], check=False)
    return [l for l in result.stdout.splitlines() if CRON_MARKER not in l] if result.returncode == 0 else []


def install(expression: str, dry_run: bool) -> None:
    fields = parse_cron(expression)
    system = platform.system()
    target = install_dir()
    script = target / "scripts" / "send_report.py"
    config = send_report.config_path()
    settings = send_report.load_config()
    if settings.get("transport") == "smtp":
        raise ValueError("Scheduled runs can't read ABT_SMTP_PASSWORD from your shell; "
                         "use the outlook or webhook transport for scheduled reports.")
    if not settings.get("to"):
        print(f"Warning: no recipient configured in {config}. Run send_report.py --to <address> --save-config first.")
    if system == "Darwin":
        plan = plistlib.dumps(launchd_plist(script, fields)).decode()
        where = plist_path()
    elif system == "Windows":
        plan = " ".join(["schtasks", "/Create", "/F", "/TN", WINDOWS_TASK, "/TR",
                         subprocess.list2cmdline(command(script)), *schtasks_schedule(fields)])
        where = f"Task Scheduler task {WINDOWS_TASK}"
    else:
        plan = cron_line(script, expression)
        where = "crontab"
    if dry_run:
        print(f"Would copy scripts to {target / 'scripts'} and install into {where}:\n{plan}")
        return
    copy_scripts(target)
    if system == "Darwin":
        where.parent.mkdir(parents=True, exist_ok=True)
        domain = f"gui/{os.getuid()}"
        run(["launchctl", "bootout", f"{domain}/{LABEL}"], check=False)
        where.write_bytes(plistlib.dumps(launchd_plist(script, fields)))
        run(["launchctl", "bootstrap", domain, str(where)])
    elif system == "Windows":
        run(["schtasks", "/Create", "/F", "/TN", WINDOWS_TASK, "/TR", subprocess.list2cmdline(command(script)),
             *schtasks_schedule(fields)])
    else:
        lines = current_crontab() + [cron_line(script, expression)]
        subprocess.run(["crontab", "-"], input="\n".join(lines) + "\n", text=True, check=True)
    print(f"Installed: report runs on '{expression}' (local time) via {where}. Log: {log_path()}")


def uninstall() -> None:
    system = platform.system()
    if system == "Darwin":
        run(["launchctl", "bootout", f"gui/{os.getuid()}/{LABEL}"], check=False)
        plist_path().unlink(missing_ok=True)
    elif system == "Windows":
        run(["schtasks", "/Delete", "/F", "/TN", WINDOWS_TASK], check=False)
    else:
        lines = current_crontab()
        subprocess.run(["crontab", "-"], input="\n".join(lines) + ("\n" if lines else ""), text=True, check=True)
    shutil.rmtree(install_dir() / "scripts", ignore_errors=True)
    print("Uninstalled the scheduled report.")


def status() -> None:
    system = platform.system()
    if system == "Darwin":
        installed = plist_path().exists()
        if installed:
            schedule = plistlib.loads(plist_path().read_bytes())["StartCalendarInterval"]
            print(f"Installed: {plist_path()}\nStartCalendarInterval: {schedule}")
            print(run(["launchctl", "print", f"gui/{os.getuid()}/{LABEL}"], check=False).stdout[:800])
    elif system == "Windows":
        result = run(["schtasks", "/Query", "/TN", WINDOWS_TASK, "/V", "/FO", "LIST"], check=False)
        installed = result.returncode == 0
        if installed:
            print(result.stdout)
    else:
        entries = [l for l in run(["crontab", "-l"], check=False).stdout.splitlines() if CRON_MARKER in l]
        installed = bool(entries)
        print("\n".join(entries))
    if not installed:
        print("No scheduled report is installed.")
    print(f"Config: {send_report.config_path()}")
    if log_path().exists():
        print(f"Last log lines ({log_path()}):")
        print("\n".join(log_path().read_text(errors="replace").splitlines()[-10:]))


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = parser.add_subparsers(dest="action", required=True)
    add = sub.add_parser("install", help="Install or replace the scheduled report.")
    add.add_argument("--cron", required=True, help='Cadence in local time, e.g. "0 8 * * 1-5".')
    add.add_argument("--dry-run", action="store_true", help="Show what would be installed.")
    sub.add_parser("uninstall", help="Remove the scheduled report.")
    sub.add_parser("status", help="Show the installed schedule, config path and recent log output.")
    args = parser.parse_args(argv)
    if args.action == "install":
        try:
            install(args.cron, args.dry_run)
        except ValueError as error:
            parser.error(str(error))
    elif args.action == "uninstall":
        uninstall()
    else:
        status()
    return 0


if __name__ == "__main__":
    sys.exit(main())
