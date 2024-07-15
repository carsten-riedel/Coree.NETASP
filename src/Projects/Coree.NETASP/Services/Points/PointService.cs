using System.Collections.Concurrent;
using System.Text.Json;

using Microsoft.Extensions.Options;

using Spectre.Console;

namespace Coree.NETASP.Services.Points
{
    public class Entry
    {
        public DateTime Created { get; set; }
        public DateTime LastUpdated { get; set; }
        public List<PointEntry> PointEntries { get; set; }
        public ulong PointEntriesAccumated { get; set; }
        public ulong PointAllTime { get; set; }

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
        Task AddOrUpdateEntry(string key, int points, string reason);

        /// <summary>
        /// Gets all point entries for a specific IP address.
        /// </summary>
        /// <param name="ipAddress">The IP address.</param>
        /// <returns>An Entry object containing point entries.</returns>
        Task<Entry?> GetEntry(string key);

        Task ShrinkEntrys(string key);

        Task DeleteEntry(string key);

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
        private readonly Dictionary<string, Entry> _entries = new Dictionary<string, Entry>();
        private readonly Dictionary<string, SemaphoreSlim> _keyLocks = new Dictionary<string, SemaphoreSlim>();
        private ManualResetEventSlim _dictFree = new ManualResetEventSlim(true);

        private readonly string _filePath;
        private readonly ILogger<PointService> _logger;
        private readonly IHostApplicationLifetime hostApplicationLifetime;

        private Timer _timer;

        public PointService(IOptions<PointServiceOptions> options, ILogger<PointService> logger, IHostApplicationLifetime hostApplicationLifetime)
        {
            _filePath = options.Value.FilePath;
            _logger = logger;
            this.hostApplicationLifetime = hostApplicationLifetime;
            hostApplicationLifetime.ApplicationStopped.Register(async () => await SaveData());
            _timer = new Timer(async state => await SaveData(), null,TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));
            LoadData();
        }

        public async Task AddOrUpdateEntry(string key, int points, string reason)
        {
            SemaphoreSlim keyLock = GetKeySemaphore(key);
            await keyLock.WaitAsync(); // Acquire the lock for the key
            try
            {
                if (_entries.ContainsKey(key))
                {
                    _entries[key].LastUpdated = DateTime.UtcNow;
                    _entries[key].PointEntries.Add(new PointEntry(points, reason));  // Here Update is a hypothetical method to update properties of Entry
                }
                else
                {
                    var newEntry = new Entry();
                    newEntry.LastUpdated = DateTime.UtcNow;
                    newEntry.Created = DateTime.UtcNow;
                    newEntry.PointEntriesAccumated = 0;
                    newEntry.PointAllTime = 0;
                    newEntry.PointEntries = new List<PointEntry>();
                    newEntry.PointEntries.Add(new PointEntry(points, reason));
                    _entries.Add(key, newEntry);
                }
            }
            finally
            {
                keyLock.Release(); // Always release the lock
            }
        }

        public async Task<Entry?> GetEntry(string key)
        {
            SemaphoreSlim keyLock = GetKeySemaphore(key);
            await keyLock.WaitAsync(); // Acquire the lock for the key
            try
            {
                // Attempt to get the entry; return null if the key doesn't exist
                _entries.TryGetValue(key, out Entry? entry);
                return entry; // Value stored and finally block executed before returning
            }
            finally
            {
                keyLock.Release(); // Always released, regardless of how the try block is exited
            }
        }

        public async Task DeleteEntry(string key)
        {
            SemaphoreSlim keyLock = GetKeySemaphore(key);
            await keyLock.WaitAsync(); // Acquire the lock for the key
            try
            {
                // Check if the entry exists in the dictionary
                if (_entries.ContainsKey(key))
                {
                    // Remove the entry from the dictionary
                    _entries.Remove(key);
                }
            }
            finally
            {
                keyLock.Release(); // Always release the semaphore, regardless of how the try block is exited
                DeleteKeySemaphore(key); // Delete the semaphore associated with the key
            }
        }

        public async Task ShrinkEntrys(string key)
        {
            SemaphoreSlim keyLock = GetKeySemaphore(key);
            await keyLock.WaitAsync(); // Acquire the lock for the key
            try
            {
                // Attempt to get the entry; return null if the key doesn't exist
                _entries.TryGetValue(key, out Entry? entry);
                if (entry != null)
                {
                    if (entry.PointEntries.Count > 30)
                    {
                        var pointlist = entry.PointEntries.Sum(e => e.Points);
                        entry.PointEntriesAccumated += (ulong)pointlist;
                        entry.PointEntries.Clear();
                    }
                }
            }
            finally
            {
                keyLock.Release(); // Always released, regardless of how the try block is exited
            }
        }

        /// <summary>
        /// Sorts the dictionary by the LastUpdated property of each entry.
        /// </summary>
        public void SortEntriesByLastUpdatedAsync()
        {
            _dictFree.Reset(); // Signal that the dictionary is being modified
            try
            {
                var sorted = _entries.OrderByDescending(kvp => kvp.Value.PointAllTime).ToList();
                _entries.Clear(); // Clear the current dictionary

                foreach (var item in sorted)
                {
                    var pointlist = item.Value.PointEntries.Sum(e => e.Points);
                    item.Value.PointEntriesAccumated += (ulong)pointlist;
                    item.Value.PointEntries.Clear();
                    item.Value.PointAllTime += item.Value.PointEntriesAccumated;
                    item.Value.PointEntriesAccumated = 0;
                }

                sorted = sorted.OrderByDescending(kvp => kvp.Value.PointAllTime).ToList();

                foreach (var item in sorted)
                {
                    _entries.Add(item.Key, item.Value); // Re-add entries in sorted order
                }
            }
            finally
            {
                _dictFree.Set(); // Signal that the dictionary is free again
            }
        }

        private SemaphoreSlim GetKeySemaphore(string key)
        {
            SemaphoreSlim keyLock;

            // Wait until the dictionary is signaled as free
            _dictFree.Wait();

            lock (_keyLocks) // Use a local lock to check and possibly add semaphore
            {
                if (!_keyLocks.TryGetValue(key, out keyLock))
                {
                    keyLock = new SemaphoreSlim(1, 1);
                    _keyLocks.Add(key, keyLock);
                }
            }

            return keyLock;
        }

        /// <summary>
        /// Deletes the semaphore for a specified key if it exists.
        /// </summary>
        /// <param name="key">The key for which to delete the semaphore.</param>
        public void DeleteKeySemaphore(string key)
        {
            // Wait until the dictionary is signaled as free
            _dictFree.Wait();

            lock (_keyLocks) // Use a local lock to safely remove semaphore
            {
                if (_keyLocks.TryGetValue(key, out var keyLock))
                {
                    _keyLocks.Remove(key);
                    keyLock.Dispose();
                }
            }

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
                SortEntriesByLastUpdatedAsync();
                _dictFree.Reset();
                var jsonData = JsonSerializer.Serialize(_entries, new JsonSerializerOptions() { WriteIndented = true });
                await File.WriteAllTextAsync(_filePath, jsonData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save point data to disk.");
            }
            finally
            {
                _dictFree.Set(); // Signal that the dictionary is free again
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