#!/usr/bin/env python3
"""Refresh OpenUtau/Assets/contributors.json from GitHub contributors API."""

from __future__ import annotations

import json
import os
import sys
import urllib.error
import urllib.request
from datetime import datetime, timezone
from pathlib import Path

REPOSITORY = "stakira/OpenUtau"
OUT = Path(__file__).resolve().parents[1] / "OpenUtau" / "Assets" / "contributors.json"


def fetch_repository_contributors(repository: str) -> list[dict]:
    contributors: dict[str, dict] = {}
    headers = {
        "Accept": "application/vnd.github+json",
        "User-Agent": "OpenUtau-Contributors-Updater",
        "X-GitHub-Api-Version": "2022-11-28",
    }
    token = os.environ.get("GITHUB_TOKEN")
    if token:
        headers["Authorization"] = f"Bearer {token}"

    for page in range(1, 21):
        url = (
            f"https://api.github.com/repos/{repository}/contributors"
            f"?per_page=100&page={page}&anon=false"
        )
        request = urllib.request.Request(url, headers=headers)
        try:
            with urllib.request.urlopen(request, timeout=30) as response:
                page_items = json.load(response)
        except urllib.error.HTTPError as error:
            raise RuntimeError(f"GitHub API error for {repository} page {page}: {error}") from error

        if not page_items:
            break

        for item in page_items:
            if item.get("type") != "User":
                continue
            login = item.get("login") or ""
            contributions = int(item.get("contributions") or 0)
            if not login or contributions <= 0:
                continue
            contributors[login] = {
                "login": login,
                "contributions": contributions,
                "profileUrl": item.get("html_url") or f"https://github.com/{login}",
            }

        if len(page_items) < 100:
            break

    return sorted(
        contributors.values(),
        key=lambda entry: (-entry["contributions"], entry["login"].lower()),
    )


def main() -> int:
    contributors = fetch_repository_contributors(REPOSITORY)
    document = {
        "generatedAt": datetime.now(timezone.utc).replace(microsecond=0).isoformat().replace("+00:00", "Z"),
        "repository": REPOSITORY,
        "contributors": contributors,
    }
    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(json.dumps(document, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"Wrote {len(contributors)} contributors to {OUT}")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as error:
        print(error, file=sys.stderr)
        raise SystemExit(1) from error
