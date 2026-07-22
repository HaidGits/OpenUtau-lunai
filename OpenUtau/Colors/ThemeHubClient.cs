using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OpenUtau.Core;
using OpenUtau.Core.Util;
using Serilog;

namespace OpenUtau.Colors;

public class ThemeHubEntry {
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("author")]
    public string Author { get; set; } = string.Empty;

    [JsonProperty("version")]
    public string Version { get; set; } = "1.0.0";

    /// <summary>Relative path under the catalog base, e.g. themes/amber-harbor.yaml</summary>
    [JsonProperty("file")]
    public string File { get; set; } = string.Empty;

    /// <summary>Optional absolute YAML URL (overrides base + file).</summary>
    [JsonProperty("url")]
    public string Url { get; set; } = string.Empty;
}

public class ThemeHubCatalog {
    [JsonProperty("themes")]
    public List<ThemeHubEntry> Themes { get; set; } = new();
}

public class ThemeHubInstalledEntry {
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("version")]
    public string Version { get; set; } = string.Empty;

    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("author")]
    public string Author { get; set; } = string.Empty;
}

public class ThemeHubInstalledManifest {
    [JsonProperty("themes")]
    public List<ThemeHubInstalledEntry> Themes { get; set; } = new();
}

public class ThemeHubSyncResult {
    public bool Ok { get; init; }
    public bool Changed { get; init; }
    public int Downloaded { get; init; }
    public int Updated { get; init; }
    public string? Error { get; init; }
}

/// <summary>
/// Fetches a GitHub-hosted theme catalog (JSON index + YAML theme files) into Themes/Hub.
/// Default: https://raw.githubusercontent.com/keirokeer/oulunai-themes/main/catalog.json
/// </summary>
public static class ThemeHubClient {
    public const string DefaultCatalogUrl =
        "https://raw.githubusercontent.com/keirokeer/oulunai-themes/main/catalog.json";

    static readonly HttpClient Http = CreateClient();

    static HttpClient CreateClient() {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("OpenUtau-Lunai-ThemeHub");
        return client;
    }

    public static string HubPath => Path.Combine(PathManager.Inst.ThemesPath, "Hub");
    public static string InstalledManifestPath => Path.Combine(HubPath, "_installed.json");

    public static string ResolveCatalogUrl() {
        var url = Preferences.Default.ThemeHubCatalogUrl?.Trim();
        return string.IsNullOrWhiteSpace(url) ? DefaultCatalogUrl : url;
    }

    public static async Task<ThemeHubSyncResult> SyncAsync(CancellationToken ct = default) {
        try {
            Directory.CreateDirectory(HubPath);
            string catalogUrl = ResolveCatalogUrl();
            Log.Information("ThemeHub: fetching catalog {Url}", catalogUrl);

            string catalogJson = await Http.GetStringAsync(catalogUrl, ct).ConfigureAwait(false);
            var catalog = JsonConvert.DeserializeObject<ThemeHubCatalog>(catalogJson)
                ?? new ThemeHubCatalog();
            if (catalog.Themes.Count == 0) {
                Log.Information("ThemeHub: catalog empty");
                return new ThemeHubSyncResult { Ok = true };
            }

            string baseUrl = GetDirectoryUrl(catalogUrl);
            var installed = LoadInstalledManifest();
            var installedById = installed.Themes
                .Where(t => !string.IsNullOrWhiteSpace(t.Id))
                .ToDictionary(t => t.Id, StringComparer.OrdinalIgnoreCase);

            int downloaded = 0;
            int updated = 0;
            var nextInstalled = new ThemeHubInstalledManifest();

            foreach (var entry in catalog.Themes) {
                ct.ThrowIfCancellationRequested();
                if (string.IsNullOrWhiteSpace(entry.Id)) {
                    continue;
                }
                string version = string.IsNullOrWhiteSpace(entry.Version) ? "1.0.0" : entry.Version;
                bool have = installedById.TryGetValue(entry.Id, out var local);
                bool needsDownload = !have
                    || !string.Equals(local!.Version, version, StringComparison.OrdinalIgnoreCase)
                    || !File.Exists(ThemeFilePath(entry.Id));

                if (needsDownload) {
                    string yamlUrl = ResolveThemeUrl(entry, baseUrl);
                    if (string.IsNullOrWhiteSpace(yamlUrl)) {
                        Log.Warning("ThemeHub: skip {Id} — no file/url", entry.Id);
                        if (have) {
                            nextInstalled.Themes.Add(local!);
                        }
                        continue;
                    }
                    Log.Information("ThemeHub: downloading {Id} from {Url}", entry.Id, yamlUrl);
                    string yamlText = await Http.GetStringAsync(yamlUrl, ct).ConfigureAwait(false);
                    WriteThemeFile(entry, yamlText);
                    if (have) {
                        updated++;
                    } else {
                        downloaded++;
                    }
                } else if (have && File.Exists(ThemeFilePath(entry.Id))) {
                    // Keep existing file; refresh author/name in manifest only.
                }

                nextInstalled.Themes.Add(new ThemeHubInstalledEntry {
                    Id = entry.Id,
                    Version = version,
                    Name = string.IsNullOrWhiteSpace(entry.Name) ? entry.Id : entry.Name,
                    Author = entry.Author ?? string.Empty,
                });
            }

            SaveInstalledManifest(nextInstalled);
            bool changed = downloaded > 0 || updated > 0;
            if (changed) {
                CustomTheme.ListThemes();
            }
            Log.Information(
                "ThemeHub: sync done — downloaded {Downloaded}, updated {Updated}",
                downloaded, updated);
            return new ThemeHubSyncResult {
                Ok = true,
                Changed = changed,
                Downloaded = downloaded,
                Updated = updated,
            };
        } catch (Exception ex) {
            Log.Warning(ex, "ThemeHub: sync failed (catalog may not exist yet)");
            return new ThemeHubSyncResult { Ok = false, Error = ex.Message };
        }
    }

    static string ThemeFilePath(string id) {
        var safe = string.Concat(id.Select(c =>
            char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '-'));
        return Path.Combine(HubPath, safe + ".yaml");
    }

    static void WriteThemeFile(ThemeHubEntry entry, string yamlText) {
        ThemeYaml theme;
        try {
            theme = Yaml.DefaultDeserializer.Deserialize<ThemeYaml>(yamlText) ?? new ThemeYaml();
        } catch (Exception ex) {
            Log.Error(ex, "ThemeHub: failed to parse YAML for {Id}", entry.Id);
            throw;
        }
        if (!string.IsNullOrWhiteSpace(entry.Name)) {
            theme.Name = entry.Name.Trim();
        }
        if (!string.IsNullOrWhiteSpace(entry.Author)) {
            theme.Author = entry.Author.Trim();
        } else if (string.IsNullOrWhiteSpace(theme.Author)) {
            theme.Author = string.Empty;
        }
        string path = ThemeFilePath(entry.Id);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, Yaml.DefaultSerializer.Serialize(theme), Encoding.UTF8);
    }

    static string ResolveThemeUrl(ThemeHubEntry entry, string baseUrl) {
        if (!string.IsNullOrWhiteSpace(entry.Url)) {
            return entry.Url.Trim();
        }
        if (string.IsNullOrWhiteSpace(entry.File)) {
            return string.Empty;
        }
        string rel = entry.File.Trim().TrimStart('/');
        return baseUrl.TrimEnd('/') + "/" + rel;
    }

    static string GetDirectoryUrl(string catalogUrl) {
        int lastSlash = catalogUrl.LastIndexOf('/');
        return lastSlash > 0 ? catalogUrl[..lastSlash] : catalogUrl;
    }

    public static ThemeHubInstalledManifest LoadInstalledManifest() {
        try {
            if (!File.Exists(InstalledManifestPath)) {
                return new ThemeHubInstalledManifest();
            }
            return JsonConvert.DeserializeObject<ThemeHubInstalledManifest>(
                       File.ReadAllText(InstalledManifestPath, Encoding.UTF8))
                   ?? new ThemeHubInstalledManifest();
        } catch (Exception ex) {
            Log.Warning(ex, "ThemeHub: failed to read installed manifest");
            return new ThemeHubInstalledManifest();
        }
    }

    static void SaveInstalledManifest(ThemeHubInstalledManifest manifest) {
        File.WriteAllText(
            InstalledManifestPath,
            JsonConvert.SerializeObject(manifest, Formatting.Indented),
            Encoding.UTF8);
    }

    public static string? TryGetAuthorForThemeName(string themeName) {
        var installed = LoadInstalledManifest();
        var hit = installed.Themes.FirstOrDefault(t =>
            string.Equals(t.Name, themeName, StringComparison.OrdinalIgnoreCase));
        if (hit != null && !string.IsNullOrWhiteSpace(hit.Author)) {
            return hit.Author;
        }
        return null;
    }
}

/// <summary>Raised on UI thread after a hub sync that changed installed themes.</summary>
public class ThemeHubSyncedEvent {
    public int Downloaded { get; init; }
    public int Updated { get; init; }
}
