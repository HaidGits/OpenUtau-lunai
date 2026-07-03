using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Platform;
using Newtonsoft.Json;
using OpenUtau.App.Models;
using Serilog;

namespace OpenUtau.App.Services {
    public static class ContributorsCatalog {
        public const string Repository = "stakira/OpenUtau";

        class GithubContributorDto {
            [JsonProperty("login")]
            public string Login { get; set; } = string.Empty;

            [JsonProperty("html_url")]
            public string HtmlUrl { get; set; } = string.Empty;

            [JsonProperty("type")]
            public string Type { get; set; } = string.Empty;

            [JsonProperty("contributions")]
            public int Contributions { get; set; }
        }

        public static ContributorsDocument LoadEmbedded() {
            try {
                using var stream = AssetLoader.Open(new Uri("avares://OpenUtau/Assets/contributors.json"));
                using var reader = new StreamReader(stream);
                var json = reader.ReadToEnd();
                var document = JsonConvert.DeserializeObject<ContributorsDocument>(json);
                if (document == null) {
                    return CreateEmptyDocument();
                }
                if (string.IsNullOrWhiteSpace(document.Repository)) {
                    document.Repository = Repository;
                }
                document.Contributors ??= new List<ContributorEntry>();
                return document;
            } catch (Exception e) {
                Log.Warning(e, "Failed to load embedded contributors list.");
                return CreateEmptyDocument();
            }
        }

        public static async Task<ContributorsDocument> LoadAsync() {
            var embedded = LoadEmbedded();
            try {
                using var client = CreateHttpClient();
                var contributors = await FetchRepositoryContributorsAsync(client, Repository);
                if (contributors.Count == 0) {
                    throw new InvalidOperationException("GitHub returned an empty contributor list.");
                }
                return new ContributorsDocument {
                    GeneratedAt = DateTime.UtcNow,
                    FetchedLive = true,
                    Repository = Repository,
                    Contributors = contributors,
                };
            } catch (Exception e) {
                Log.Warning(e, "Failed to fetch contributors from GitHub; using embedded list.");
                embedded.FetchedLive = false;
                return embedded;
            }
        }

        static HttpClient CreateHttpClient() {
            var client = new HttpClient {
                Timeout = TimeSpan.FromSeconds(30),
            };
            client.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
            client.DefaultRequestHeaders.Add("User-Agent", "OpenUtau-Contributors");
            client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
            return client;
        }

        static async Task<List<ContributorEntry>> FetchRepositoryContributorsAsync(HttpClient client, string repository) {
            var contributors = new List<ContributorEntry>();
            for (int page = 1; page <= 20; page++) {
                using var response = await client.GetAsync(
                    $"https://api.github.com/repos/{repository}/contributors?per_page=100&page={page}&anon=false");
                response.EnsureSuccessStatusCode();
                string body = await response.Content.ReadAsStringAsync();
                var pageItems = JsonConvert.DeserializeObject<List<GithubContributorDto>>(body)
                    ?? new List<GithubContributorDto>();
                if (pageItems.Count == 0) {
                    break;
                }
                foreach (var item in pageItems) {
                    if (!string.Equals(item.Type, "User", StringComparison.OrdinalIgnoreCase)
                            || string.IsNullOrWhiteSpace(item.Login)
                            || item.Contributions <= 0) {
                        continue;
                    }
                    contributors.Add(new ContributorEntry {
                        Login = item.Login,
                        Contributions = item.Contributions,
                        ProfileUrl = string.IsNullOrWhiteSpace(item.HtmlUrl)
                            ? $"https://github.com/{item.Login}"
                            : item.HtmlUrl,
                    });
                }
                if (pageItems.Count < 100) {
                    break;
                }
            }
            return contributors
                .OrderByDescending(c => c.Contributions)
                .ThenBy(c => c.Login, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        static ContributorsDocument CreateEmptyDocument() {
            return new ContributorsDocument {
                Repository = Repository,
            };
        }
    }
}
