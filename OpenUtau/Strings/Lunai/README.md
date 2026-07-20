# Lunai string overlays

These files are **not** managed by Crowdin. They are merged at runtime after upstream `Strings*.axaml` (see `App.SetLanguage`).

| Path | Role |
|------|------|
| `Strings.Lunai.axaml` | English source (Lunai-only keys + overrides) |
| `Strings.Lunai.<locale>.axaml` | Full locale overlays (321 keys each) |
| `translations/<locale>.json` | Editable translation source used to regenerate axaml |

Rebuild overlays after editing JSON:

```powershell
python Misc/build_lunai_locale_overlays.py
```
