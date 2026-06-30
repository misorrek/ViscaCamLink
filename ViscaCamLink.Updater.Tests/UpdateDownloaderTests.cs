namespace ViscaCamLink.Updater.Tests;

using System.IO;
using System.Net;
using System.Text;

using Shouldly;

using ViscaCamLink.Updater;

public sealed class UpdateDownloaderTests
{
    private const string FileName = "test-setup.exe";

    private static HttpClient MakeClient(byte[] content, long? contentLength = null)
    {
        var handler = new FixedResponseHandler(content, contentLength);
        return new HttpClient(handler);
    }

    // --- basic download ---

    [Fact]
    public async Task DownloadAsync_WritesContentToTempFile()
    {
        var content    = Encoding.UTF8.GetBytes("fake installer bytes");
        var downloader = new UpdateDownloader(MakeClient(content));

        var path = await downloader.DownloadAsync("https://example.com/setup.exe", FileName);

        try
        {
            File.Exists(path).ShouldBeTrue();
            (await File.ReadAllBytesAsync(path)).ShouldBe(content);
        }
        finally { TryDelete(path); }
    }

    [Fact]
    public async Task DownloadAsync_ReturnsPathInTempFolder()
    {
        var downloader = new UpdateDownloader(MakeClient([]));

        var path = await downloader.DownloadAsync("https://example.com/setup.exe", FileName);

        try
        {
            path.ShouldStartWith(Path.GetTempPath());
            Path.GetFileName(path).ShouldBe(FileName);
        }
        finally { TryDelete(path); }
    }

    // --- progress reporting ---

    [Fact]
    public async Task DownloadAsync_ReportsProgressTo100_WhenContentLengthKnown()
    {
        var content    = new byte[1000];
        var downloader = new UpdateDownloader(MakeClient(content, contentLength: content.Length));
        var reported   = new List<int>();

        var path = await downloader.DownloadAsync(
            "https://example.com/setup.exe", FileName,
            new Progress<int>(p => reported.Add(p)));

        try
        {
            reported.ShouldNotBeEmpty();
            reported.Last().ShouldBe(100);
        }
        finally { TryDelete(path); }
    }

    [Fact]
    public async Task DownloadAsync_NoProgressReported_WhenContentLengthUnknown()
    {
        var handler    = new StreamHttpHandler(new byte[500]);
        var downloader = new UpdateDownloader(new HttpClient(handler));
        var reported   = new List<int>();

        var path = await downloader.DownloadAsync(
            "https://example.com/setup.exe", FileName,
            new Progress<int>(p => reported.Add(p)));

        try { reported.ShouldBeEmpty(); }
        finally { TryDelete(path); }
    }

    // --- error handling ---

    [Fact]
    public async Task DownloadAsync_ServerError_ThrowsHttpRequestException()
    {
        var handler    = new ErrorHttpHandler(HttpStatusCode.NotFound);
        var downloader = new UpdateDownloader(new HttpClient(handler));

        await Should.ThrowAsync<HttpRequestException>(
            () => downloader.DownloadAsync("https://example.com/setup.exe", FileName));
    }

    // --- corrupt download / Content-Length mismatch ---

    [Fact]
    public async Task DownloadAsync_ContentLengthMismatch_RetriesAndSucceeds()
    {
        // First response: claims 100 bytes but only sends 50 (corrupt).
        // Second response: correct 100 bytes — retry should succeed.
        var correctContent = new byte[100];
        new Random(42).NextBytes(correctContent);

        var handler    = new SequencedHandler(
            (new byte[50], claimedLength: 100),          // attempt 1 — truncated
            (correctContent, claimedLength: 100));        // attempt 2 — correct

        var downloader = new UpdateDownloader(new HttpClient(handler));

        var path = await downloader.DownloadAsync("https://example.com/setup.exe", FileName);

        try
        {
            (await File.ReadAllBytesAsync(path)).ShouldBe(correctContent);
            handler.CallCount.ShouldBe(2);
        }
        finally { TryDelete(path); }
    }

    [Fact]
    public async Task DownloadAsync_ContentLengthMismatch_ThrowsAfterOneRetry()
    {
        // Both attempts return truncated content → second attempt also throws.
        var handler    = new SequencedHandler(
            (new byte[50], claimedLength: 100),
            (new byte[50], claimedLength: 100));

        var downloader = new UpdateDownloader(new HttpClient(handler));

        var ex = await Should.ThrowAsync<InvalidDataException>(
            () => downloader.DownloadAsync("https://example.com/setup.exe", FileName));
        ex.Message.ShouldContain("expected 100 bytes");
        ex.Message.ShouldContain("received 50");

        handler.CallCount.ShouldBe(2, "exactly one retry is expected");
    }

    [Fact]
    public async Task DownloadAsync_NoContentLength_DoesNotThrowOnShortContent()
    {
        // Without a Content-Length header there is nothing to validate — download must succeed
        // regardless of how many bytes the server sends.
        var downloader = new UpdateDownloader(MakeClient(new byte[30], contentLength: null));

        var path = await downloader.DownloadAsync("https://example.com/setup.exe", FileName);

        try { File.Exists(path).ShouldBeTrue(); }
        finally { TryDelete(path); }
    }

    [Fact]
    public async Task DownloadAsync_ContentLengthMismatch_DeletesTempFileBeforeRetry()
    {
        // Verify that a partial temp file is removed before the retry so the
        // second attempt starts with a clean slate.
        var correctContent = Encoding.UTF8.GetBytes("complete file contents");
        var partialContent = Encoding.UTF8.GetBytes("partial");

        var handler = new SequencedHandler(
            (partialContent, claimedLength: correctContent.Length), // attempt 1 — truncated
            (correctContent, claimedLength: correctContent.Length)); // attempt 2 — correct

        var downloader = new UpdateDownloader(new HttpClient(handler));

        var path = await downloader.DownloadAsync("https://example.com/setup.exe", FileName);

        try
        {
            // Final file must contain only the correct content, not a mix of partial + correct.
            var written = await File.ReadAllBytesAsync(path);
            written.ShouldBe(correctContent);
        }
        finally { TryDelete(path); }
    }

    // --- helpers ---

    private static void TryDelete(string path)
    {
        try { File.Delete(path); } catch { /* best effort */ }
    }

    /// <summary>Returns the same fixed response on every request.</summary>
    private sealed class FixedResponseHandler(byte[] content, long? contentLength) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(content)
            };
            if (contentLength.HasValue)
                response.Content.Headers.ContentLength = contentLength.Value;

            return Task.FromResult(response);
        }
    }

    /// <summary>Returns a different response per call; repeats the last entry if exhausted.</summary>
    private sealed class SequencedHandler(params (byte[] content, long? claimedLength)[] responses) : HttpMessageHandler
    {
        private int _callCount;

        public int CallCount => _callCount;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var index = Math.Min(_callCount, responses.Length - 1);
            var (content, claimedLength) = responses[index];
            _callCount++;

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(content)
            };
            if (claimedLength.HasValue)
                response.Content.Headers.ContentLength = claimedLength.Value;

            return Task.FromResult(response);
        }
    }

    private sealed class ErrorHttpHandler(HttpStatusCode status) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(new HttpResponseMessage(status));
    }

    /// <summary>Returns a <see cref="StreamContent"/> without a Content-Length header (unknown length).</summary>
    private sealed class StreamHttpHandler(byte[] content) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                // Non-seekable stream → TryComputeLength returns false → no Content-Length header.
                Content = new StreamContent(new NonSeekableStream(content))
            };
            return Task.FromResult(response);
        }
    }

    private sealed class NonSeekableStream(byte[] data) : Stream
    {
        private readonly MemoryStream _inner = new(data);
        public override bool CanRead  => true;
        public override bool CanSeek  => false;
        public override bool CanWrite => false;
        public override long Length   => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int  Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override void Flush()                                     => _inner.Flush();
        public override long Seek(long offset, SeekOrigin origin)        => throw new NotSupportedException();
        public override void SetLength(long value)                       => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
