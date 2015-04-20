using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeaderManip.Tests
{
    using System.Net.Http;
    using Microsoft.Owin.Builder;
    using Xunit;

    public class CookieTests
    {
        [Fact]
        public async Task Should_Have_2_Headers()
        {
            var app = GetAppBuilder();
            var handler = GetHandler(app);
            var client = CreateHttpClient(handler);

            var response = await client.PostAsync("http://localhost/login", new StringContent(""));

            var authCookie = handler
               .CookieContainer
               .GetCookies(new Uri("http://localhost"))[".AspNet.MyCookie"];

            var xsrfCookie = handler
                .CookieContainer
                .GetCookies(new Uri("http://localhost/login"))["XSRF-TOKEN"];

            //Then
            Assert.NotNull(authCookie);
            Assert.NotNull(xsrfCookie);
        }

        private AppBuilder GetAppBuilder()
        {
            var app = new AppBuilder();
            new Startup().Configuration(app);
            return app;
        }

        private OwinHttpMessageHandler GetHandler(AppBuilder builder)
        {
            return new OwinHttpMessageHandler(builder.Build())
            {
                UseCookies = true
            };
        }

        private HttpClient CreateHttpClient(OwinHttpMessageHandler handler)
        {
            var client = new HttpClient(handler)
            {
                BaseAddress = new Uri("http://example.com")
            };

            return client;
        }
    }
}
