namespace OpenUtau.App.Views {
    public sealed class InstallPromptOptions {
        public bool OfferPackageManager { get; init; } = true;
        public string? DownloadUrl { get; init; }
    }
}
