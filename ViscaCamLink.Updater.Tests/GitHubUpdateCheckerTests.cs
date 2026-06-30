namespace ViscaCamLink.Updater.Tests;

using System.Net;
using System.Text;

using Shouldly;

using ViscaCamLink.Updater;

public sealed class GitHubUpdateCheckerTests
{
    private static HttpClient MakeClient(string json, HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = new FakeHttpHandler(status, json);
        return new HttpClient(handler);
    }

    private static string ReleaseJson(string tag, string? installerUrl = null, string? portableUrl = null) =>
        $$"""
        {
          "tag_name": "{{tag}}",
          "body": "Release notes for {{tag}}",
          "html_url": "https://github.com/misorrek/ViscaCamLink/releases/tag/{{tag}}",
          "assets": [
            { "name": "ViscaCamLink-setup.exe", "browser_download_url": "{{installerUrl ?? $"https://example.com/{tag}/setup.exe"}}" },
            { "name": "ViscaCamLink-portable.zip", "browser_download_url": "{{portableUrl ?? $"https://example.com/{tag}/portable.zip"}}" }
          ]
        }
        """;

    // --- update available ---

    [Fact]
    public async Task CheckAsync_NewerVersionAvailable_ReturnsUpdateInfo()
    {
        var checker = new GitHubUpdateChecker(MakeClient(ReleaseJson("v1.0.0")));

        var result = await checker.CheckAsync(new Version(0, 9, 0));

        result.ShouldNotBeNull();
        result!.Version.ShouldBe(new Version(1, 0, 0));
        result.ReleaseNotes.ShouldContain("1.0.0");
        result.InstallerAssetUrl.ShouldEndWith("setup.exe");
        result.PortableAssetUrl.ShouldEndWith("portable.zip");
    }

    // --- up to date ---

    [Fact]
    public async Task CheckAsync_SameVersion_ReturnsNull()
    {
        var checker = new GitHubUpdateChecker(MakeClient(ReleaseJson("v1.0.0")));

        var result = await checker.CheckAsync(new Version(1, 0, 0));

        result.ShouldBeNull();
    }

    [Fact]
    public async Task CheckAsync_CurrentVersionIsNewer_ReturnsNull()
    {
        var checker = new GitHubUpdateChecker(MakeClient(ReleaseJson("v1.0.0")));

        var result = await checker.CheckAsync(new Version(2, 0, 0));

        result.ShouldBeNull();
    }

    // --- malformed / error responses ---

    [Fact]
    public async Task CheckAsync_MalformedJson_ReturnsNull()
    {
        var checker = new GitHubUpdateChecker(MakeClient("this is not json"));

        var result = await checker.CheckAsync(new Version(0, 1, 0));

        result.ShouldBeNull();
    }

    [Fact]
    public async Task CheckAsync_NetworkError_ReturnsNull()
    {
        var handler = new ThrowingHttpHandler();
        var checker = new GitHubUpdateChecker(new HttpClient(handler));

        var result = await checker.CheckAsync(new Version(0, 1, 0));

        result.ShouldBeNull();
    }

    [Fact]
    public async Task CheckAsync_MissingInstallerAsset_ReturnsNull()
    {
        const string json = """
            {
              "tag_name": "v1.0.0",
              "body": "notes",
              "html_url": "https://example.com",
              "assets": []
            }
            """;
        var checker = new GitHubUpdateChecker(MakeClient(json));

        var result = await checker.CheckAsync(new Version(0, 1, 0));

        result.ShouldBeNull();
    }

    [Fact]
    public async Task CheckAsync_TagWithoutVPrefix_ParsedCorrectly()
    {
        var checker = new GitHubUpdateChecker(MakeClient(ReleaseJson("2.0.0")));

        var result = await checker.CheckAsync(new Version(1, 0, 0));

        result.ShouldNotBeNull();
        result!.Version.ShouldBe(new Version(2, 0, 0));
    }

    // --- helpers ---

    private sealed class FakeHttpHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var response = new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }

    private sealed class ThrowingHttpHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => throw new HttpRequestException("Network failure");
    }
}
