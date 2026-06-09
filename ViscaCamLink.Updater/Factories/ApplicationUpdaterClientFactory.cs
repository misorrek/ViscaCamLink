namespace ViscaCamLink.Updater.Factories;

using System;
using System.Net.Http;

using ViscaCamLink.Updater.Util;

public class ApplicationUpdaterClientFactory : IApplicationUpdaterClientFactory
{
    public ApplicationUpdaterClientFactory(IHttpClientFactory httpClientFactory)
    {
        HttpClientFactory = httpClientFactory;
    }

    public const String HttpClientName = "ApplicationUpdaterHttpClient";

    private IHttpClientFactory HttpClientFactory { get; }

    public IApplicationUpdaterClient CreateClient(String baseAddress)
    {
        var httpClient = HttpClientFactory.CreateClient(HttpClientName);

        httpClient.BaseAddress = new Uri(baseAddress);
        //Timeout?

        return new ApplicationUpdaterClient(httpClient);
    }
}
