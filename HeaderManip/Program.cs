namespace HeaderManip
{
    using Microsoft.Owin;
    using Microsoft.Owin.Hosting;
    using Microsoft.Owin.Security;
    using Microsoft.Owin.Security.Cookies;
    using Nancy;
    using Nancy.Security;
    using Owin;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading;
    using System.Threading.Tasks;

    class Program
    {
        static void Main(string[] args)
        {
            var options = new StartOptions();
            var urls = new[] { "http://127.0.0.1:1999", "http://localhost:1999" };
            urls.ToList().ForEach(options.Urls.Add);

            using (WebApp.Start<Startup>(options))
            {
                Console.WriteLine("Running a http server on {0}", options.Urls.Aggregate((a, b) => a + ", " + b));
                do
                {
                    Thread.Sleep(60000);
                } while (!Console.KeyAvailable);
            }
        }
    }

    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.UseMyMiddleware();

            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationMode = AuthenticationMode.Active,
                CookieHttpOnly = true,
                CookieSecure = Microsoft.Owin.Security.Cookies.CookieSecureOption.SameAsRequest,
                SlidingExpiration = true,
                AuthenticationType = "MyCookie",
            });

            app.UseNancy();
        }
    }

    public static class MyMiddlewareExtensions
    {
        public static IAppBuilder UseMyMiddleware(this IAppBuilder app)
        {
            return app.Use(typeof(MyMiddleware));
        }
    }

    public class MyMiddleware
    {
        private readonly Func<IDictionary<string, object>, Task> nextFunc;

        public MyMiddleware(Func<IDictionary<string, object>, Task> nextFunc)
        {
            this.nextFunc = nextFunc;
        }

        public Task Invoke(IDictionary<string, object> env)
        {
            var context = new OwinContext(env);

            context.Response.OnSendingHeaders(_ => 
            {
                
                if ((string)env["owin.RequestPath"] == "/login" && (string)env["owin.RequestMethod"] == "POST")
                {
                    var responseHeaders = (IDictionary<string, string[]>)env["owin.ResponseHeaders"];
                    if (responseHeaders.ContainsKey("Set-Cookie"))
                    {
                        var setcookies = responseHeaders["Set-Cookie"].ToList();
                        var authcookie = setcookies.FirstOrDefault(x => x.StartsWith(".AspNet.MyCookie"));
                        if (authcookie != null)
                        {
                            var authcookieValue = authcookie.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries)[1].Split(new[] { ';' })[0];
                            var csrfToken = authcookie.Reverse();
                            setcookies.Add("XSRF-TOKEN=" + csrfToken + ";path=/");
                            responseHeaders["Set-Cookie"] = setcookies.ToArray();
                        }
                    }
                }

            }, null);

            return nextFunc(env);
            
        }
    }

    public class HomeModule : NancyModule
    {
        public HomeModule()
        {
            Get["/"] = _ => "Hi";

            Post["/login"] = _ =>
            {
                var claims = new List<Claim>(new[]
                    {
                        new Claim(ClaimTypes.Email, "blha@blha.com"), 
                        new Claim(ClaimTypes.Name, "jim")
                    });

                this.Context.GetAuthenticationManager().SignIn(new ClaimsIdentity(claims, "MyCookie"));

                return Response.AsRedirect("/");
            };
        }
    }
}
