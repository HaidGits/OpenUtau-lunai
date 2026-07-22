#!/usr/bin/env python3
"""Build Strings.Lunai.<locale>.axaml from translations/*.json and register in App.axaml."""
from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
LUNAI = ROOT / "OpenUtau" / "Strings" / "Lunai"
TRANS = LUNAI / "translations"
APP = ROOT / "OpenUtau" / "App.axaml"


def load_en_keys() -> dict[str, str]:
    """English Lunai keys from Strings.Lunai.axaml (source of truth)."""
    axaml = (LUNAI / "Strings.Lunai.axaml").read_text(encoding="utf-8")
    pat = re.compile(
        r'<system:String x:Key="([^"]+)">(.*?)</system:String>',
        re.S,
    )
    en: dict[str, str] = {}
    for m in pat.finditer(axaml):
        text = (
            m.group(2)
            .replace("&lt;", "<")
            .replace("&gt;", ">")
            .replace("&amp;", "&")
        )
        en[m.group(1)] = text
    if not en:
        raise SystemExit("no keys found in Strings.Lunai.axaml")
    return en


EN = load_en_keys()


def write_axaml(locale: str, entries: dict[str, str]) -> None:
    missing = [k for k in EN if k not in entries]
    if missing:
        raise SystemExit(f"{locale}: missing {len(missing)} keys, e.g. {missing[:5]}")
    extra = [k for k in entries if k not in EN]
    if extra:
        print(f"warning {locale}: ignoring {len(extra)} unknown keys")
    lines = [
        f"<!--Lunai overlay for {locale}. Not managed by Crowdin.-->",
        "<!--Professional UI translation of Lunai-only strings.-->",
        '<ResourceDictionary xmlns="https://github.com/avaloniaui"',
        '                    xmlns:system="clr-namespace:System;assembly=mscorlib"',
        '                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">',
    ]
    last = None
    for key in sorted(EN.keys()):
        section = key.split(".", 1)[0]
        if last != section:
            lines.append("")
        last = section
        text = entries[key]
        esc = (
            text.replace("&", "&amp;")
            .replace("<", "&lt;")
            .replace(">", "&gt;")
        )
        # undo double-escape if translator already used &amp;
        esc = esc.replace("&amp;amp;", "&amp;").replace("&amp;lt;", "&lt;").replace("&amp;gt;", "&gt;")
        lines.append(f'  <system:String x:Key="{key}">{esc}</system:String>')
    lines.append("</ResourceDictionary>")
    lines.append("")
    out = LUNAI / f"Strings.Lunai.{locale}.axaml"
    out.write_text("\n".join(lines), encoding="utf-8")
    print(f"wrote {out.name} ({len(EN)} keys)")


def update_app_axaml(locales: list[str]) -> None:
    text = APP.read_text(encoding="utf-8")
    # Remove existing lunai-strings includes
    text = re.sub(
        r'\s*<ResourceInclude x:Key="lunai-strings-[^"]+" Source="/Strings/Lunai/[^"]+"/>\n',
        "\n",
        text,
    )
    block_lines = [
        '      <!-- Lunai overlays: merged after upstream locales in App.SetLanguage -->',
        '      <ResourceInclude x:Key="lunai-strings-en-US" Source="/Strings/Lunai/Strings.Lunai.axaml"/>',
    ]
    for loc in sorted(locales):
        if loc == "en-US":
            continue
        block_lines.append(
            f'      <ResourceInclude x:Key="lunai-strings-{loc}" Source="/Strings/Lunai/Strings.Lunai.{loc}.axaml"/>'
        )
    block = "\n".join(block_lines) + "\n"
    # Insert before themes-dark
    if 'x:Key="themes-dark"' not in text:
        raise SystemExit("themes-dark marker not found in App.axaml")
    text = re.sub(
        r'(?:\s*<!-- Lunai overlays:.*?-->\n)?(?=\s*<ResourceInclude x:Key="themes-dark")',
        "\n" + block,
        text,
        count=1,
        flags=re.S,
    )
    # Clean duplicate blank lines a bit
    text = re.sub(r"\n{3,}", "\n\n", text)
    APP.write_text(text, encoding="utf-8")
    print(f"updated App.axaml with {len(locales)} lunai locales")


def main() -> None:
    locales = []
    for path in sorted(TRANS.glob("*.json")):
        loc = path.stem
        data = json.loads(path.read_text(encoding="utf-8"))
        write_axaml(loc, data)
        locales.append(loc)
    if not locales:
        raise SystemExit("no translation JSON files found")
    update_app_axaml(locales)


if __name__ == "__main__":
    main()
