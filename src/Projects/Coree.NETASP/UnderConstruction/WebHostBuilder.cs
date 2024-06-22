using Microsoft.AspNetCore.Http.Features;

namespace Coree.NETASP.UnderConstruction
{
    public class EmptyWebHostBuilder2 : WebHostBuilder
    {
        private readonly IServiceCollection _services = new ServiceCollection();

        private readonly IApplicationBuilder _appbuilder;


        public EmptyWebHostBuilder2()
        {
            // Capture the services added via the default builder's ConfigureServices
            base.ConfigureServices(services =>
            {
                foreach (var service in services)
                {
                    _services.Add(service);
                }
            });

        }

        public IServiceCollection Services => _services;

        public new IWebHost Build()
        {
            // Ensure the custom service collection is used in the final build
            base.ConfigureServices(services =>
            {
                foreach (var serviceDescriptor in _services)
                {
                    services.Add(serviceDescriptor);
                }

            });
            return base.Build();
        }
    }

    public class CustomWebApplication : IApplicationBuilder
    {
        private IWebHost _app;

        public IServiceProvider Srv
        {
            get { return _app.Services; }

        }

        public IWebHost Host
        {
            get { return _app; }

        }

        public IServiceProvider ApplicationServices { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public IFeatureCollection ServerFeatures => throw new NotImplementedException();

        public IDictionary<string, object?> Properties => throw new NotImplementedException();

        internal CustomWebApplication(IWebHost app)
        {

            _app = app;
        }

        public void Run()
        {
            _app.Run();
        }

        public IApplicationBuilder Use(Func<RequestDelegate, RequestDelegate> middleware)
        {
            throw new NotImplementedException();
        }

        public IApplicationBuilder New()
        {
            throw new NotImplementedException();
        }

        public RequestDelegate Build()
        {
            throw new NotImplementedException();
        }
    }

    public class EmptyWebHostBuilder
    {
        private IWebHostBuilder _internalBuilder;
        private IServiceCollection _services;

        public EmptyWebHostBuilder()
        {
            _internalBuilder = new WebHostBuilder();
            _services = new ServiceCollection();
        }

        public void ConfigureServices(Action<IServiceCollection> configureServices)
        {
            configureServices(_services);
        }

        public CustomWebApplication Build()
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

            return new CustomWebApplication(host);
        }
    }

    public class EmptyWebHostBuilderx
    {
        private IWebHostBuilder _internalBuilder;
        private IServiceCollection _services;

        public EmptyWebHostBuilderx()
        {
            _internalBuilder = new WebHostBuilder();
            _services = new ServiceCollection();
        }

        public void ConfigureServices(Action<IServiceCollection> configureServices)
        {
            configureServices(_services);
        }

        public ApplicationBuilder Build()
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

            return new ApplicationBuilder(host.Services); ;
        }
    }

    public class MyWebHostBuilder : IWebHostBuilder
    {
        private WebHostBuilder wbuilder = new WebHostBuilder();
        private IServiceCollection _services;
        private IApplicationBuilder _appbuilder;

        public IServiceCollection Servicesx => _services;

        public MyWebHostBuilder()
        {
            _services = new ServiceCollection();
        }

        public IWebHost Buildx()
        {
            wbuilder.ConfigureServices(services =>
            {
                foreach (var service in _services)
                {
                    services.Add(service);
                }
            });

            wbuilder.Build();


            return wbuilder.Build();
        }

        public IWebHost Build()
        {
            wbuilder.ConfigureServices(services =>
            {
                foreach (var service in _services)
                {
                    services.Add(service);
                }
            });

            return wbuilder.Build();
        }

        public IWebHostBuilder ConfigureAppConfiguration(Action<WebHostBuilderContext, IConfigurationBuilder> configureDelegate)
        {
            return wbuilder.ConfigureAppConfiguration(configureDelegate);
        }

        public IWebHostBuilder ConfigureServices(Action<IServiceCollection> configureServices)
        {
            return wbuilder.ConfigureServices(configureServices);
        }

        public IWebHostBuilder ConfigureServices(Action<WebHostBuilderContext, IServiceCollection> configureServices)
        {
            return wbuilder.ConfigureServices(configureServices);
        }

        public string? GetSetting(string key)
        {
            return wbuilder.GetSetting(key);
        }

        public IWebHostBuilder UseSetting(string key, string? value)
        {
            return wbuilder.UseSetting(key, value);
        }
    }
}
