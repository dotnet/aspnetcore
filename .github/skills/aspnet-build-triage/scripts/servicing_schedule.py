"""Work out which monthly .NET servicing release a PR merged now would ship in.

Reads the per-month sub-pages of the DevDiv ".NET Servicing Schedule" wiki page and finds the first
release whose code-complete cut-off hasn't passed yet.
"""

from __future__ import annotations

import json
import re
import shutil
import subprocess
from datetime import datetime
from typing import Any, Callable
from zoneinfo import ZoneInfo, ZoneInfoNotFoundError

SCHEDULE_URL = "https://devdiv.visualstudio.com/DevDiv/_wiki/wikis/DevDiv.wiki/8694/NET-Servicing-Schedule"
WIKI_PATH = "/dot.NET Wiki/NET Core/Product Lifecycle and Release/NET Servicing/NET Servicing Schedule"
MONTHS = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"]
MONTH_NAMES = ["January", "February", "March", "April", "May", "June", "July", "August",
               "September", "October", "November", "December"]
LOOKAHEAD_MONTHS = 4

CODE_COMPLETE = re.compile(r"code[\s-]*complete", re.IGNORECASE)
DAY_MONTH = re.compile(r"\b(\d{1,2})-([A-Za-z]{3})[a-z]*\b")
MONTH_DAY = re.compile(r"\b([A-Za-z]{3})[a-z]*\.?\s+(\d{1,2})\b")
CUTOFF_TIME = re.compile(r"\b(\d{1,2})(?::(\d{2}))?\s*([AP])M\b", re.IGNORECASE)
VERSION = re.compile(r"(?<![\d.])(\d{1,2})(?:\.0)?(?![\d.])")


def _month_index(token: str) -> int | None:
    token = token[:3].title()
    return MONTHS.index(token) + 1 if token in MONTHS else None


def _parse_date(cell: str) -> tuple[int, int] | None:
    """Return (month, day) for '16-Oct', 'Nov 16' or 'November 16' style cells."""
    for pattern, swap in ((DAY_MONTH, True), (MONTH_DAY, False)):
        for match in pattern.finditer(cell):
            day, month = (match.group(1), match.group(2)) if swap else (match.group(2), match.group(1))
            month_number = _month_index(month)
            if month_number:
                return month_number, int(day)
    return None


def code_complete_rows(content: str) -> list[dict[str, Any]]:
    """Code-complete rows from the page's markdown tables (activity, versions, date, cut-off)."""
    rows = []
    for line in content.splitlines():
        if not line.lstrip().startswith("|"):
            continue
        cells = [c.strip() for c in line.strip().strip("|").split("|")]
        activity = re.sub(r"[*_`]", "", cells[0]).strip()
        # Long first cells are MSRC/notes tables that merely mention code complete.
        if not CODE_COMPLETE.search(activity) or len(activity) > 80:
            continue
        dates = [d for d in (_parse_date(c) for c in cells[1:]) if d]
        if not dates:
            continue
        time = CUTOFF_TIME.search(line)
        hour, minute = 16, 0
        if time:
            hour = int(time.group(1)) % 12 + (12 if time.group(3).upper() == "P" else 0)
            minute = int(time.group(2) or 0)
        version_text = re.sub(CODE_COMPLETE, "", activity)
        rows.append({
            "activity": activity,
            "versions": sorted({int(v) for v in VERSION.findall(version_text)}),
            "date": dates[-1],
            "time": (hour, minute),
        })
    return rows


def pacific() -> ZoneInfo:
    try:
        return ZoneInfo("America/Los_Angeles")
    except ZoneInfoNotFoundError:
        raise RuntimeError("No time zone data; run `python -m pip install tzdata` (needed on Windows)") from None


def code_complete_cutoff(content: str, release_year: int, release_month: int, major: int) -> datetime | None:
    """Cut-off for `major` in a release page, or None if the page has no usable code-complete row.

    Rows naming specific versions apply only to those versions. Generic rows (for example interim and
    final code complete) apply to everyone, and the latest one is the real cut-off.
    """
    rows = code_complete_rows(content)
    applicable = [r for r in rows if major in r["versions"]] or [r for r in rows if not r["versions"]]
    if not applicable:
        return None
    cutoffs = []
    for row in applicable:
        month, day = row["date"]
        year = release_year if month <= release_month else release_year - 1
        hour, minute = row["time"]
        cutoffs.append(datetime(year, month, day, hour, minute, tzinfo=pacific()))
    return max(cutoffs)


def fetch_page(title: str) -> dict[str, Any] | None:
    """Fetch a schedule sub-page (for example 'January 2027'); None if it doesn't exist."""
    az = shutil.which("az")
    if not az:
        raise RuntimeError("Azure CLI (az) is required to read the servicing schedule")
    result = subprocess.run(
        [az, "devops", "invoke", "--org", "https://dev.azure.com/devdiv", "--area", "wiki",
         "--resource", "pages", "--route-parameters", "project=DevDiv", "wikiIdentifier=DevDiv.wiki",
         "--query-parameters", f"path={WIKI_PATH}/{title}", "includeContent=true",
         "--api-version", "7.1", "-o", "json"],
        capture_output=True, text=True)
    if result.returncode != 0:
        if "could not be found" in result.stderr or "WikiPageNotFoundException" in result.stderr:
            return None
        raise RuntimeError(f"Could not read the servicing schedule wiki: {result.stderr.strip()[:300]}")
    return json.loads(result.stdout)


def page_url(page: dict[str, Any], title: str) -> str:
    page_id = page.get("id")
    slug = title.replace(" ", "-")
    return f"https://devdiv.visualstudio.com/DevDiv/_wiki/wikis/DevDiv.wiki/{page_id}/{slug}" if page_id else SCHEDULE_URL


def target_release(major: int, now: datetime,
                   fetch: Callable[[str], dict[str, Any] | None] = fetch_page) -> dict[str, Any]:
    """The first monthly release whose code-complete cut-off for `major` is still ahead of `now`."""
    year, month = now.year, now.month
    for _ in range(LOOKAHEAD_MONTHS):
        title = f"{MONTH_NAMES[month - 1]} {year}"
        page = fetch(title)
        if page is None:
            return {"release": None, "note": f"The {title} schedule isn't published yet.",
                    "scheduleUrl": SCHEDULE_URL}
        cutoff = code_complete_cutoff(page.get("content") or "", year, month, major)
        if cutoff is None:
            return {"release": None, "note": f"Couldn't find a code-complete date for {major}.0 on the {title} page.",
                    "scheduleUrl": page_url(page, title)}
        if cutoff > now:
            return {"release": title, "codeComplete": cutoff.isoformat(), "scheduleUrl": page_url(page, title)}
        if month == 12:
            year, month = year + 1, 1
        else:
            month += 1
    return {"release": None, "note": "No upcoming code-complete date found.", "scheduleUrl": SCHEDULE_URL}


def release_for_branches(branches: list[str], now: datetime,
                         fetch: Callable[[str], dict[str, Any] | None] = fetch_page) -> dict[str, dict[str, Any]]:
    """Map each `release/N.x` branch to its target release, fetching each wiki page once."""
    cache: dict[str, dict[str, Any] | None] = {}

    def cached(title: str) -> dict[str, Any] | None:
        if title not in cache:
            cache[title] = fetch(title)
        return cache[title]

    results = {}
    for branch in sorted(set(branches)):
        match = re.match(r"release/(\d+)\.", branch)
        if not match:
            continue
        try:
            results[branch] = target_release(int(match.group(1)), now, cached)
        except RuntimeError as error:
            results[branch] = {"release": None, "note": str(error), "scheduleUrl": SCHEDULE_URL}
    return results

