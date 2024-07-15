using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.DependencyInjection;

namespace Coree.NETASP.Services.Instancer
{
    public interface IServerConfigurationAnalyzer
    {
        Task<bool?> IsAnyIpWithDefaultPortAsync();
    }


    public class ServerConfigurationAnalyzer : IServerConfigurationAnalyzer
    {
        private readonly ILogger<ServerConfigurationAnalyzer> logger;
        private readonly IServiceProvider serviceProvider;
        private List<ServerListenDetail> serverListenDetails = new List<ServerListenDetail>();
        private readonly SemaphoreSlim threadSafetyLock = new SemaphoreSlim(1, 1);  // Semaphore for thread-safe access
        private bool isInitializationSuccessful = true;  // Flag to track initialization status

        public class ServerListenDetail
        {
            public Uri ListenUri { get; set; }
            public bool IsLocalHost { get; set; }
            public bool IsAnyIp { get; set; }
            public bool IsDefaultPort { get; set; }
            public bool IsSpecificAddress { get; set; }
        }

        public ServerConfigurationAnalyzer(ILogger<ServerConfigurationAnalyzer> logger, IServiceProvider serviceProvider)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            try
            {
                AnalyzeServerAddresses();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to analyze server addresses");
                isInitializationSuccessful = false;
            }
        }

        private void AnalyzeServerAddresses()
        {
            var server = serviceProvider.GetRequiredService<IServer>();
            var addressFeature = server.Features.Get<IServerAddressesFeature>();
            if (addressFeature == null)
            {
                throw new InvalidOperationException("Server does not have an address feature available.");
            }

            var addresses = addressFeature.Addresses.ToList();
            foreach (var address in addresses)
            {
                Uri uri = new Uri(address);
                bool isDefaultPort = (uri.Port == 80) || (uri.Port == 443);
                bool isLocalHost = uri.Host.Equals("localhost") || uri.Host.Equals("127.0.0.1") || uri.Host.Equals("[::1]");
                bool isAnyIp = uri.Host.Equals("+") || uri.Host.Equals("*") || uri.Host.Equals("0.0.0.0") || uri.Host.Equals("[::]");

                serverListenDetails.Add(new ServerListenDetail
                {
                    ListenUri = uri,
                    IsLocalHost = isLocalHost,
                    IsAnyIp = isAnyIp,
                    IsDefaultPort = isDefaultPort,
                    IsSpecificAddress = !isLocalHost && !isAnyIp
                });
            }
        }

        /// <summary>
        /// Thread-safe method to determine if any server listen detail entry has IsDefaultPort and IsAnyIp both set to true.
        /// If there is an exception or initialization failed, returns null.
        /// </summary>
        public async Task<bool?> IsAnyIpWithDefaultPortAsync()
        {
            if (!isInitializationSuccessful)
            {
                return null;
            }

            try
            {
                await threadSafetyLock.WaitAsync();
                return serverListenDetails.Any(li => li.IsDefaultPort && li.IsAnyIp);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error checking for Any IP with Default Port.");
                return null;
            }
            finally
            {
                threadSafetyLock.Release();
            }
        }
    }


}