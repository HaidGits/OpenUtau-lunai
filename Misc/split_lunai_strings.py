#!/usr/bin/env python3
"""
One-shot (and reusable) split: move Lunai-only / overridden strings out of
Crowdin-managed Strings*.axaml into OpenUtau/Strings/Lunai/Strings.Lunai*.axaml,
then restore upstream Strings*.axaml from the given git ref (default upstream/master).
"""
from __future__ import annotations

import argparse
import os
import re
import subprocess
import xml.etree.ElementTree as ET
from pathlib import Path

XKEY = "{http://schemas.microsoft.com/winfx/2006/xaml}Key"
GIT = os.environ.get("GIT_EXE", r"G:\Program Files\Git\cmd\git.exe")


def parse_axaml_text(text: str) -> dict[str, tuple[str, str]]:
    root = ET.fromstring(text)
    out: dict[str, tuple[str, str]] = {}
    for child in root:
        key = child.get(XKEY)
        if not key or child.text is None or child.text == "":
            continue
        tag = child.tag.split("}")[-1]
        out[key] = (tag, child.text)
    return out


def parse_axaml_file(path: Path) -> dict[str, tuple[str, str]]:
    return parse_axaml_text(path.read_text(encoding="utf-8"))


def git_show(ref_path: str) -> str | None:
    r = subprocess.run(
        [GIT, "show", ref_path],
        capture_output=True,
        text=True,
        encoding="utf-8",
    )
    if r.returncode != 0:
        return None
    return r.stdout


def write_axaml(path: Path, entries: dict[str, tuple[str, str]], header_comments: list[str]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    lines: list[str] = []
    for c in header_comments:
        lines.append(f"<!--{c}-->")
    lines.append('<ResourceDictionary xmlns="https://github.com/avaloniaui"')
    lines.append('                    xmlns:system="clr-namespace:System;assembly=mscorlib"')
    lines.append('                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">')
    last_section = None
    for key in sorted(entries.keys()):
        try:
            section = key[: key.index(".")]
        except ValueError:
            section = key
        if last_section != section:
            lines.append("")
        last_section = section
        tag, text = entries[key]
        if tag == "String":
            tag = "system:String"
        # Escape XML special chars in text minimally
        esc = (
            text.replace("&", "&amp;")
            .replace("<", "&lt;")
            .replace(">", "&gt;")
        )
        lines.append(f'  <{tag} x:Key="{key}">{esc}</{tag}>')
    lines.append("</ResourceDictionary>")
    lines.append("")
    path.write_text("\n".join(lines), encoding="utf-8")


def lunai_keys(current_en: dict, upstream_en: dict) -> set[str]:
    only = set(current_en) - set(upstream_en)
    changed = {k for k in set(current_en) & set(upstream_en) if current_en[k][1] != upstream_en[k][1]}
    return only | changed


def only_lunai_keys(current_en: dict, upstream_en: dict) -> set[str]:
    return set(current_en) - set(upstream_en)


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--upstream-ref", default="upstream/master")
    ap.add_argument("--strings-dir", default="OpenUtau/Strings")
    ap.add_argument("--dry-run", action="store_true")
    args = ap.parse_args()

    strings_dir = Path(args.strings_dir)
    lunai_dir = strings_dir / "Lunai"
    en_path = strings_dir / "Strings.axaml"
    current_en = parse_axaml_file(en_path)
    up_en_text = git_show(f"{args.upstream_ref}:OpenUtau/Strings/Strings.axaml")
    if not up_en_text:
        raise SystemExit(f"Cannot read {args.upstream_ref}:OpenUtau/Strings/Strings.axaml")
    upstream_en = parse_axaml_text(up_en_text)
    keys = lunai_keys(current_en, upstream_en)
    only_keys = only_lunai_keys(current_en, upstream_en)
    print(f"Lunai overlay keys: {len(keys)} (new {len(only_keys)}, overrides {len(keys) - len(only_keys)})")

    # English Lunai overlay from current fork file
    en_lunai = {k: current_en[k] for k in sorted(keys) if k in current_en}
    if not args.dry_run:
        write_axaml(
            lunai_dir / "Strings.Lunai.axaml",
            en_lunai,
            [
                "Lunai-only strings and overrides. Not managed by Crowdin.",
                "Merged after upstream Strings*.axaml in App.SetLanguage.",
            ],
        )
        en_path.write_text(up_en_text, encoding="utf-8", newline="\n")
        print(f"Wrote {lunai_dir / 'Strings.Lunai.axaml'} ({len(en_lunai)} keys)")
        print(f"Restored {en_path} from {args.upstream_ref}")

    # Locale files
    for path in sorted(strings_dir.glob("Strings.*.axaml")):
        if "Lunai" in path.name:
            continue
        # Strings.ru-RU.axaml -> ru-RU
        m = re.match(r"Strings\.(.+)\.axaml$", path.name)
        if not m:
            continue
        locale = m.group(1)
        current_loc = parse_axaml_file(path)
        # Full overlay for Russian; other locales only get truly new keys so
        # English Lunai overrides are not blocked by stale upstream translations.
        locale_key_set = keys if locale == "ru-RU" else only_keys
        loc_lunai = {k: current_loc[k] for k in sorted(locale_key_set) if k in current_loc}
        up_text = git_show(f"{args.upstream_ref}:OpenUtau/Strings/Strings.{locale}.axaml")

        if not args.dry_run:
            if loc_lunai:
                write_axaml(
                    lunai_dir / f"Strings.Lunai.{locale}.axaml",
                    loc_lunai,
                    [
                        f"Lunai overlay for {locale}. Not managed by Crowdin.",
                        "Missing keys fall back to Strings.Lunai.axaml (en-US).",
                    ],
                )
                print(f"Wrote Lunai/{path.name.replace('Strings.', 'Strings.Lunai.')} ({len(loc_lunai)} keys)")
            if up_text is not None:
                path.write_text(up_text, encoding="utf-8", newline="\n")
                print(f"Restored {path.name} from upstream")
            else:
                # Locale exists only in fork: strip Lunai keys, keep the rest
                kept = {k: v for k, v in current_loc.items() if k not in keys}
                write_axaml(
                    path,
                    kept,
                    [
                        "DO NOT modify Lunai keys here; use Strings/Lunai/.",
                        "To contribute upstream localization: https://crowdin.com/project/oxygen-dioxideopenutau",
                    ],
                )
                print(f"Stripped Lunai keys from fork-only {path.name} ({len(kept)} keys left)")

    # Restore locales present upstream but missing locally
    up_list = subprocess.run(
        [GIT, "ls-tree", "--name-only", f"{args.upstream_ref}:OpenUtau/Strings"],
        capture_output=True,
        text=True,
        encoding="utf-8",
        check=True,
    ).stdout.splitlines()
    for name in up_list:
        if not name.startswith("Strings.") or not name.endswith(".axaml"):
            continue
        if name == "Strings.axaml":
            continue
        dest = strings_dir / name
        if not dest.exists() and not args.dry_run:
            text = git_show(f"{args.upstream_ref}:OpenUtau/Strings/{name}")
            if text:
                dest.write_text(text, encoding="utf-8", newline="\n")
                print(f"Added missing upstream {name}")

    # Also restore unhandled_strings.json from upstream if desired
    if not args.dry_run:
        uh = git_show(f"{args.upstream_ref}:OpenUtau/Strings/unhandled_strings.json")
        if uh:
            (strings_dir / "unhandled_strings.json").write_text(uh, encoding="utf-8", newline="\n")
            print("Restored unhandled_strings.json")


if __name__ == "__main__":
    main()
