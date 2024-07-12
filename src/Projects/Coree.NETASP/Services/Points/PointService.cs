using System.Collections.Concurrent;
using System.Text.Json;

using Microsoft.Extensions.Options;

namespace Coree.NETASP.Services.Points
{
    public class Entry
    {
        public DateTime Created { get; set; }
        public DateTime LastUpdated { get; set; }
        public List<PointEntry> PointEntries { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Entry"/> class.
        /// </summary>
        /// <param name="ipAddress">The IP address.</param>
        public Entry()
        {
            Created = DateTime.UtcNow;
            PointEntries = new List<PointEntry>();
        }
    }

    public class PointEntry
    {
        public int Points { get; set; }
        public string Reason { get; set; }
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PointEntry"/> class.
        /// </summary>
        /// <param name="points">The number of points assigned.</param>
        /// <param name="reason">The reason for assigning points.</param>
        public PointEntry(int points, string reason)
        {
            Points = points;
            Reason = reason;
            Timestamp = DateTime.UtcNow;
        }
    }

    public interface IPointService
    {
        /// <summary>
        /// Assigns points to a specific IP address.
        /// </summary>
        /// <param name="ipAddress">The IP address.</param>
        /// <param name="points">The number of points to assign.</param>
        /// <param name="reason">The reason for assigning points.</param>
        void AssignFailureRating(string ipAddress, int points, string reason);

        /// <summary>
        /// Gets all point entries for a specific IP address.
        /// </summary>
        /// <param name="ipAddress">The IP address.</param>
        /// <returns>An Entry object containing point entries.</returns>
        Entry? GetPoints(string ipAddress);

        /// <summary>
        /// Loads the point data from disk.
        /// </summary>
        void LoadData();

        /// <summary>
        /// Saves the point data to disk.
        /// </summary>
        Task SaveData();
    }

    public class PointService : IPointService
    {
        private readonly ConcurrentDictionary<string, Entry> _entries;
        private readonly string _filePath;
        private readonly ILogger<PointService> _logger;
        private readonly IHostApplicationLifetime hostApplicationLifetime;

        public PointService(IOptions<PointServiceOptions> options, ILogger<PointService> logger, IHostApplicationLifetime hostApplicationLifetime)
        {
            _entries = new ConcurrentDictionary<string, Entry>();
            _filePath = options.Value.FilePath;
            _logger = logger;
            this.hostApplicationLifetime = hostApplicationLifetime;
            hostApplicationLifetime.ApplicationStopped.Register(async () => await SaveData());
            LoadData();
        }

        public void AssignFailureRating(string ipAddress, int points, string reason)
        {
            var pointEntry = new PointEntry(points, reason);

            _entries.AddOrUpdate(ipAddress,
                // Correctly include key in the delegate for creating new Entry
                addValueFactory: (key) => new Entry
                {
                    LastUpdated = DateTime.UtcNow,
                    PointEntries = new List<PointEntry>() { pointEntry } // Initialize with the pointEntry
                },
                // Update method used if the key already exists
                updateValueFactory: (key, existingEntry) =>
                {
                    existingEntry.LastUpdated = DateTime.UtcNow;
                    existingEntry.PointEntries.Add(pointEntry);
                    return existingEntry;
                });
        }

        public Entry? GetPoints(string ipAddress)
        {
            _entries.TryGetValue(ipAddress, out var entry);
            return entry ?? null;
        }

        public void LoadData()
        {
            if (File.Exists(_filePath))
            {
                try
                {
                    var jsonData = File.ReadAllText(_filePath);
                    var data = JsonSerializer.Deserialize<ConcurrentDictionary<string, Entry>>(jsonData);
                    if (data != null)
                    {
                        foreach (var kvp in data)
                        {
                            _entries[kvp.Key] = kvp.Value;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load point data from disk.");
                }
            }
        }

        public async Task SaveData()
        {
            try
            {
                var jsonData = JsonSerializer.Serialize(_entries, new JsonSerializerOptions() { WriteIndented = true});
                await File.WriteAllTextAsync(_filePath, jsonData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save point data to disk.");
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // No actions needed on start
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await SaveData();
            
        }
    }

    public class PointServiceOptions
    {
        public string FilePath { get; set; }
    }
}