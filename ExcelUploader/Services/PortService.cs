using System.Net;
using System.Net.Sockets;

namespace ExcelUploader.Services
{
    public class PortService : IPortService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<PortService> _logger;
        private int? _currentPort;

        public PortService(IConfiguration configuration, ILogger<PortService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public int GetAvailablePort(int startPort = 5000)
        {
            try
            {
                // Check if auto port detection is enabled
                var autoPortDetection = _configuration.GetValue<bool>("PortConfiguration:AutoPortDetection", true);
                if (!autoPortDetection)
                {
                    var defaultPort = _configuration.GetValue<int>("PortConfiguration:DefaultPort", 5000);
                    _logger.LogInformation($"Auto port detection disabled, using default port: {defaultPort}");
                    return defaultPort;
                }

                // Find available port starting from startPort
                for (int port = startPort; port < startPort + 100; port++)
                {
                    if (IsPortAvailable(port))
                    {
                        _currentPort = port;
                        _logger.LogInformation($"Available port found: {port}");
                        return port;
                    }
                }

                // If no port found in range, use a random available port
                var randomPort = GetRandomAvailablePort();
                _currentPort = randomPort;
                _logger.LogInformation($"Using random available port: {randomPort}");
                return randomPort;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding available port");
                var fallbackPort = startPort;
                _currentPort = fallbackPort;
                return fallbackPort;
            }
        }

        public bool IsPortAvailable(int port)
        {
            try
            {
                using var client = new TcpClient();
                var result = client.BeginConnect(IPAddress.Loopback, port, null, null);
                var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(100));
        client.EndConnect(result);
                client.Close();
                return !success; // Port is available if connection fails
            }
            catch
            {
                return true; // Port is available if connection fails
            }
        }

        public int GetCurrentPort()
        {
            if (_currentPort.HasValue)
                return _currentPort.Value;

            // Try to get from configuration or find available port
            var configuredPort = _configuration.GetValue<int>("PortConfiguration:DefaultPort", 5000);
            if (IsPortAvailable(configuredPort))
            {
                _currentPort = configuredPort;
                return configuredPort;
            }

            var availablePort = GetAvailablePort(configuredPort);
            _currentPort = availablePort;
            return availablePort;
        }

        public string GetBaseUrl()
        {
            var port = GetCurrentPort();
            var scheme = _configuration.GetValue<string>("Kestrel:Endpoints:Http:Url", "http://localhost");
            
            if (scheme.Contains("localhost"))
            {
                return $"http://localhost:{port}";
            }
            
            return $"{scheme}:{port}";
        }

        public async Task<bool> TestPortAsync(int port)
        {
            try
            {
                using var client = new TcpClient();
                var result = client.BeginConnect(IPAddress.Loopback, port, null, null);
                var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(1000));
                
                if (success)
                {
                    client.EndConnect(result);
                    client.Close();
                    return true;
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }

        private int GetRandomAvailablePort()
        {
            var random = new Random();
            var attempts = 0;
            
            while (attempts < 100)
            {
                var port = random.Next(5000, 65535);
                if (IsPortAvailable(port))
                    return port;
                attempts++;
            }
            
            // Fallback to a known available port
            return 8080;
        }
    }
}
