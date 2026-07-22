using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Serilog;

namespace OpenUtau.Core.Util;

public static class ExpressionStyleStore {
    static readonly Dictionary<string, string> stylePaths = new(StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyDictionary<string, string> StylePaths => stylePaths;

    public static IReadOnlyList<ExpressionStyleYaml> LoadAll() {
        stylePaths.Clear();
        Directory.CreateDirectory(PathManager.Inst.ExpressionStylesPath);
        var loaded = new List<ExpressionStyleYaml>();
        foreach (var file in Directory.EnumerateFiles(PathManager.Inst.ExpressionStylesPath, "*.yaml")) {
            try {
                var yaml = ExpressionStyleYaml.LoadFromFile(file);
                if (string.IsNullOrWhiteSpace(yaml.Name)) {
                    yaml.Name = Path.GetFileNameWithoutExtension(file);
                }
                yaml.Name = yaml.Name.Trim();
                yaml.SingerName = yaml.SingerName?.Trim() ?? string.Empty;
                yaml.Values ??= new Dictionary<string, float>();
                var resolved = ResolveUniqueDisplayName(yaml.Name);
                yaml.Name = resolved;
                if (stylePaths.ContainsKey(resolved)) {
                    continue;
                }
                stylePaths.Add(resolved, file);
                loaded.Add(yaml);
            } catch (Exception exception) {
                Log.Error(exception, "Failed to parse expression style yaml in {Path}", file);
            }
        }
        return loaded.OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public static bool Exists(string name) {
        if (string.IsNullOrWhiteSpace(name)) {
            return false;
        }
        if (stylePaths.Count == 0) {
            LoadAll();
        }
        return stylePaths.ContainsKey(name.Trim());
    }

    public static bool TrySave(ExpressionStyleYaml yaml, out string? savedPath, bool overwrite = false) {
        savedPath = null;
        if (yaml == null || string.IsNullOrWhiteSpace(yaml.Name)) {
            return false;
        }
        yaml.Name = yaml.Name.Trim();
        yaml.SingerName = yaml.SingerName?.Trim() ?? string.Empty;
        yaml.Values ??= new Dictionary<string, float>();

        Directory.CreateDirectory(PathManager.Inst.ExpressionStylesPath);
        string path = Path.Combine(PathManager.Inst.ExpressionStylesPath, SanitizeFileName(yaml.Name) + ".yaml");

        if (stylePaths.Count == 0) {
            LoadAll();
        }

        if (!overwrite) {
            if (File.Exists(path) || stylePaths.ContainsKey(yaml.Name)) {
                return false;
            }
        } else if (stylePaths.TryGetValue(yaml.Name, out var existingPath)
            && !string.Equals(existingPath, path, StringComparison.OrdinalIgnoreCase)
            && File.Exists(existingPath)) {
            File.Delete(existingPath);
            stylePaths.Remove(yaml.Name);
        }

        yaml.SaveToFile(path);
        stylePaths[yaml.Name] = path;
        savedPath = path;
        return true;
    }

    public static bool TryDelete(string name) {
        if (string.IsNullOrWhiteSpace(name)) {
            return false;
        }
        if (stylePaths.Count == 0) {
            LoadAll();
        }
        name = name.Trim();
        if (!stylePaths.TryGetValue(name, out var path)) {
            return false;
        }
        if (File.Exists(path)) {
            File.Delete(path);
        }
        stylePaths.Remove(name);
        return true;
    }

    public static string SanitizeFileName(string name) {
        var sanitized = string.Concat(name.Where(c => char.IsLetterOrDigit(c) || c == ' '))
            .Replace(' ', '-')
            .ToLowerInvariant();
        return string.IsNullOrWhiteSpace(sanitized) ? "style" : sanitized;
    }

    static string ResolveUniqueDisplayName(string baseName) {
        string resolved = baseName.Trim();
        int dup = 1;
        while (stylePaths.ContainsKey(resolved)) {
            resolved = $"{baseName.Trim()} ({dup})";
            dup++;
        }
        return resolved;
    }
}
