namespace Coree.NETASP.Extensions.WebApplicationBuilderExtensions
{

    /// <summary>
    /// Provides methods to configure, build, and run a WebApplication using a given WebApplicationBuilder.
    /// </summary>
    file class WebAppBuilder
    {
        public WebApplicationBuilder Builder;
        public WebApplication? App;

        private Action<WebApplication> configureApp = _ => { };

        /// <summary>Initializes a new instance of the WebAppBuilder class.</summary>
        /// <param name="builder">The WebApplicationBuilder used for application configuration and building.</param>
        public WebAppBuilder(WebApplicationBuilder builder)
        {
            this.Builder = builder;
        }

        /// <summary>Adds configuration actions to the WebApplicationBuilder and WebApplication.</summary>
        /// <param name="config">The configuration action to apply to the builder and application.</param>
        public void AddConfiguration(Action<WebApplicationBuilder?, WebApplication?> config)
        {
            configureApp += app => config(null, app);
        }

        /// <summary>Builds the WebApplication based on the provided configurations.</summary>
        /// <returns>The configured and built WebApplication.</returns>
        public WebApplication Build()
        {
            App = Builder.Build();
            configureApp(App);
            return App;
        }

        /// <summary>Builds and runs the WebApplication asynchronously.</summary>
        /// <returns>A Task representing the asynchronous operation of running the application.</returns>
        public async Task BuildAndRunAsync()
        {
            App = Builder.Build();
            configureApp(App);
            await App.RunAsync();
        }

        /// <summary>Builds and runs the WebApplication synchronously.</summary>
        public void BuildAndRun()
        {
            App = Builder.Build();
            configureApp(App);
            App.Run();
        }
    }

    /// <summary>
    /// Provides extension methods for WebApplicationBuilder to configure, build, and optionally run a WebApplication.
    /// </summary>
    public static partial class WebAppBuilderExtensions
    {
        /// <summary>
        /// Configures and builds a WebApplication using the specified configuration actions. This method centralizes
        /// the setup for various components of a web application, helping to keep the configuration clean and uncluttered.
        /// <param name="builder">The WebApplicationBuilder to configure.</param>
        /// <param name="configure">An action to configure both the builder and the application. This unified approach allows for
        /// keeping related configurations together, simplifying the overall setup process.</param>
        /// <returns>The configured and built WebApplication.</returns>
        /// <example>
        /// This example demonstrates how to use the SetupAndBuild method to configure and build a WebApplication.
        /// The configuration actions for controllers and Razor pages are defined within the method call, illustrating
        /// how to keep related setup tasks together in a single, cohesive block.
        /// <code>
        /// var app = WebApplication.CreateBuilder(args).SetupAndBuild((builderStage, appStage) => {
        ///     // Configure the services for controllers.
        ///     if (builderStage != null)
        ///     {
        ///         builderStage.Services.AddControllers();
        ///     }
        ///     
        ///     // Map controller routes.
        ///     if (appStage != null)
        ///     {
        ///         appStage.MapControllers();
        ///     }
        ///     
        ///     // Configure the services for Razor Pages.
        ///     builderStage?.Services.AddRazorPages();
        ///     // Map Razor Page routes.
        ///     appStage?.MapRazorPages();
        /// });
        /// app.Run();
        /// </code>
        /// </example>
        /// </summary>
        public static WebApplication SetupAndBuild(this WebApplicationBuilder builder, Action<WebApplicationBuilder?, WebApplication?> configure)
        {
            var setupManager = new WebAppBuilder(builder);
            setupManager.AddConfiguration(configure);
            configure(setupManager.Builder, setupManager.App);
            return setupManager.Build();
        }

        /// <summary>
        /// Configures, builds, and runs a WebApplication asynchronously using the specified configuration actions.
        /// </summary>
        /// <param name="builder">The WebApplicationBuilder to configure.</param>
        /// <param name="configure">An action to configure both the builder and the application.</param>
        /// <returns>A Task representing the asynchronous operation of building and running the application.</returns>
        public static async Task SetupAndBuildRunAsync(this WebApplicationBuilder builder, Action<WebApplicationBuilder?, WebApplication?> configure)
        {
            var setupManager = new WebAppBuilder(builder);
            setupManager.AddConfiguration(configure);
            configure(setupManager.Builder, setupManager.App);
            await setupManager.BuildAndRunAsync();
        }

        /// <summary>
        /// Configures, builds, and runs a WebApplication synchronously using the specified configuration actions.
        /// </summary>
        /// <param name="builder">The WebApplicationBuilder to configure.</param>
        /// <param name="configure">An action to configure both the builder and the application.</param>
        public static void SetupAndBuildRun(this WebApplicationBuilder builder, Action<WebApplicationBuilder?, WebApplication?> configure)
        {
            var setupManager = new WebAppBuilder(builder);
            setupManager.AddConfiguration(configure);
            configure(setupManager.Builder, setupManager.App);
            setupManager.BuildAndRun();
        }
    }
}
