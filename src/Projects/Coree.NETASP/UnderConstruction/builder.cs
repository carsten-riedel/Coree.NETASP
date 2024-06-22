namespace Coree.NETASP.UnderConstruction
{
    public class EmptyWebHostBuilder3
    {
        private IWebHostBuilder _internalBuilder;
        private IServiceCollection _services;

        public EmptyWebHostBuilder3()
        {
            _internalBuilder = new WebHostBuilder();
            _services = new ServiceCollection();
        }

        public void ConfigureServices(Action<IServiceCollection> configureServices)
        {
            configureServices(_services);
        }

        public CustomWebApplication2 Build()
        {
            _internalBuilder.ConfigureServices(services =>
            {
                foreach (var service in _services)
                {
                    services.Add(service);
                }
            });

            _internalBuilder.Configure(app =>
            {
                // Possibly configure some default middleware
            });

            var host = _internalBuilder.Build();
            var appBuilder = new ApplicationBuilder(host.Services);
            return new CustomWebApplication2(appBuilder);
        }
    }


    public class CustomWebApplication2
    {
        private IApplicationBuilder _app;

        internal CustomWebApplication2(IApplicationBuilder app)
        {
            _app = app;
        }

        public void Run()
        {
            var host = (IWebHost)_app.ApplicationServices.GetService(typeof(IWebHost));
            host.Run();
        }

        public void UseMiddleware<TMiddleware>() where TMiddleware : IMiddleware
        {
            _app.UseMiddleware<TMiddleware>();
        }

        // Add other middleware methods as needed
        public void Use(Action<IApplicationBuilder> configure)
        {
            configure(_app);
        }
    }
}
