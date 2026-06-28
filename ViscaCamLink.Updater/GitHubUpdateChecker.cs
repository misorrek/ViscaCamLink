namespace ViscaCamLink.Updater;

using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class GitHubUpdateChecker : IGitHubUpdateChecker
{
    private const string ReleasesApiUrl = "https://api.github.com/repos/misorrek/ViscaCamLink/releases/latest";

    private readonly HttpClient _http;

    public GitHubUpdateChecker(HttpClient http)
    {
        _http = http;

        if (!_http.DefaultRequestHeaders.UserAgent.Any())
        {
            _http.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("ViscaCamLink", "1.0"));
        }
    }

    public async Task<UpdateInfo?> CheckAsync(Version currentVersion, CancellationToken ct = default)
    {
        try
        {
            var json = await _http.GetStringAsync(ReleasesApiUrl, ct).ConfigureAwait(false);
            var release = JsonSerializer.Deserialize(json, GitHubReleaseJsonContext.Default.GitHubRelease);

            if (release is null || string.IsNullOrEmpty(release.TagName))
                return null;

            var tagVersion = ParseTagVersion(release.TagName);
            if (tagVersion is null || tagVersion <= currentVersion)
                return null;

            var installerUrl = FindAssetUrl(release.Assets, ".exe");
            var portableUrl  = FindAssetUrl(release.Assets, ".zip");

            if (installerUrl is null)
                return null;

            return new UpdateInfo(
                Version: tagVersion,
                ReleaseNotes: release.Body ?? string.Empty,
                InstallerAssetUrl: installerUrl,
                PortableAssetUrl: portableUrl,
                HtmlUrl: release.HtmlUrl ?? string.Empty);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            return null;
        }
    }

    private static Version? ParseTagVersion(string tag)
    {
        var s = tag.TrimStart('v', 'V');
        return Version.TryParse(s, out var v) ? v : null;
    }

    private static string? FindAssetUrl(IReadOnlyList<GitHubAsset>? assets, string extension)
        => assets?.FirstOrDefault(a => a.Name.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                 ?.BrowserDownloadUrl;
}

internal record GitHubRelease(
    [property: JsonPropertyName("tag_name")]  string? TagName,
    [property: JsonPropertyName("body")]      string? Body,
    [property: JsonPropertyName("html_url")]  string? HtmlUrl,
    [property: JsonPropertyName("assets")]    IReadOnlyList<GitHubAsset>? Assets);

internal record GitHubAsset(
    [property: JsonPropertyName("name")]                 string Name,
    [property: JsonPropertyName("browser_download_url")] string BrowserDownloadUrl);

[JsonSerializable(typeof(GitHubRelease))]
internal sealed partial class GitHubReleaseJsonContext : JsonSerializerContext { }
