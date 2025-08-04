using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using VRCFaceTracking.PaperTracker.Configuration;
using VRCFaceTracking.PaperTracker.Core.Models;
using VRCFaceTracking.PaperTracker.Tracking.Eye;
using VRCFaceTracking.PaperTracker.Tracking.Face;
using VRCFaceTracking.PaperTracker.Utils;

namespace VRCFaceTracking.PaperTracker.Core.OSC;

public enum OSCState
{
    IDLE,
    CONNECTED,
    ERROR,
}

// 统一OSC管理器 - 修复版本
public class UnifiedOSCManager : IDisposable
{
    private readonly ILogger _logger;
    private readonly EyeTrackingManager? _eyeTrackingManager;
    private readonly FaceTrackingManager? _faceTrackingManager;
    
    private readonly Dictionary<int, Socket> _receivers = new();
    private readonly Dictionary<int, Thread> _listenerThreads = new();
    private readonly Dictionary<int, CancellationTokenSource> _cancellationTokens = new();
    
    private UnifiedConfigManager? _configManager;
    private const int ConnectionTimeout = 1000; // 减少超时时间
    private bool _disposed = false;
    private readonly object _lockObject = new object();
    
    public OSCState State { get; private set; } = OSCState.IDLE;
    
    public UnifiedOSCManager(ILogger logger, EyeTrackingManager? eyeManager, FaceTrackingManager? faceManager)
    {
        _logger = logger;
        _eyeTrackingManager = eyeManager;
        _faceTrackingManager = faceManager;
    }
    
    public void RegisterConfigManager(UnifiedConfigManager configManager)
    {
        if (_disposed) return;
        
        _configManager = configManager;
        configManager.RegisterListener(OnConfigChanged);
    }
    
    public void Start()
    {
        lock (_lockObject)
        {
            if (_configManager == null || _disposed) return;
            
            var config = _configManager.Config;
            
            // 启动眼部追踪OSC监听
            if (config.EnableEyeTracking && _eyeTrackingManager != null)
            {
                StartOSCListener(config.EyeTracking.PortNumber, config.EyeTracking.ListeningAddress, ProcessEyeTrackingMessage);
            }
            
            // 启动面部追踪OSC监听
            if (config.EnableFaceTracking && _faceTrackingManager != null)
            {
                _logger.LogInformation($"Attempting to start face tracking OSC listener on port {config.FaceTracking.FacePort}");
                StartOSCListener(config.FaceTracking.FacePort, IPAddress.Any, ProcessFaceTrackingMessage);
            }
            
            State = OSCState.CONNECTED;
        }
    }
    
    private void StartOSCListener(int port, IPAddress address, Action<OSCMessage> messageHandler)
    {
        if (_disposed) return;
        
        try
        {
            // 检查端口是否已被占用
            if (_receivers.ContainsKey(port))
            {
                _logger.LogWarning($"Port {port} is already in use by this application");
                return;
            }
            
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            
            // 设置socket选项
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            
            // 绑定socket
            socket.Bind(new IPEndPoint(address, port));
            socket.ReceiveTimeout = ConnectionTimeout;
            
            _receivers[port] = socket;
            
            // 使用CancellationToken替代ManualResetEvent
            var cancellationTokenSource = new CancellationTokenSource();
            _cancellationTokens[port] = cancellationTokenSource;
            
            var thread = new Thread(() => OSCListenLoop(port, messageHandler, cancellationTokenSource.Token))
            {
                Name = $"OSC-Listener-{port}",
                IsBackground = true // 确保是后台线程
            };
            _listenerThreads[port] = thread;
            thread.Start();
            
            _logger.LogInformation($"Successfully started OSC listener on {address}:{port}");
        }
        catch (SocketException ex)
        {
            _logger.LogError(ex, $"Socket error starting OSC listener on {address}:{port}. Error code: {ex.ErrorCode}");
            if (ex.ErrorCode == 10048) // WSAEADDRINUSE
            {
                _logger.LogError($"Port {port} is already in use by another application");
            }
            State = OSCState.ERROR;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to start OSC listener on {address}:{port}");
            State = OSCState.ERROR;
        }
    }
    
    private void OSCListenLoop(int port, Action<OSCMessage> messageHandler, CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];
        
        if (!_receivers.TryGetValue(port, out var socket))
        {
            _logger.LogError($"Failed to get socket for port {port}");
            return;
        }
        
        _logger.LogInformation($"OSC listen loop started for port {port}");
        
        try
        {
            while (!cancellationToken.IsCancellationRequested && !_disposed)
            {
                try
                {
                    if (socket.IsBound && !_disposed)
                    {
                        // 检查取消令牌
                        if (cancellationToken.IsCancellationRequested) break;
                        
                        var length = socket.Receive(buffer);
                        _logger.LogDebug($"Received {length} bytes on port {port}");
                        
                        var message = ParseOSCMessage(buffer, length);
                        if (message.success && !_disposed)
                        {
                            _logger.LogDebug($"Successfully parsed OSC message: {message.address}");
                            messageHandler(message);
                        }
                        else if (!_disposed)
                        {
                            _logger.LogDebug($"Failed to parse OSC message on port {port}");
                        }
                    }
                }
                catch (SocketException ex) when (ex.ErrorCode == 10060) // WSAETIMEDOUT
                {
                    // 接收超时，检查取消令牌后继续
                    if (cancellationToken.IsCancellationRequested) break;
                    continue;
                }
                catch (SocketException ex) when (ex.ErrorCode == 10054 || ex.ErrorCode == 10004) // WSAECONNRESET or WSAEINTR
                {
                    // 连接被重置或被中断，这通常意味着socket被关闭
                    _logger.LogDebug($"Socket operation interrupted on port {port}, exiting gracefully");
                    break;
                }
                catch (ObjectDisposedException)
                {
                    // Socket已被释放，正常退出
                    _logger.LogDebug($"Socket disposed for port {port}, exiting gracefully");
                    break;
                }
                catch (SocketException ex)
                {
                    if (!_disposed && !cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogError(ex, $"Socket error in OSC listen loop for port {port}. Error code: {ex.ErrorCode}");
                    }
                    break;
                }
                catch (Exception ex)
                {
                    if (!_disposed && !cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogError(ex, $"Unexpected error in OSC listen loop for port {port}");
                    }
                    // 短暂等待后继续，避免快速循环
                    Thread.Sleep(100);
                }
            }
        }
        catch (Exception ex)
        {
            if (!_disposed)
            {
                _logger.LogError(ex, $"Fatal error in OSC listen loop for port {port}");
            }
        }
        finally
        {
            _logger.LogInformation($"OSC listen loop ended for port {port}");
        }
    }
    
    private void ProcessEyeTrackingMessage(OSCMessage message)
    {
        if (_disposed) return;
        
        if (message.address.Contains("/command/"))
        {
            // 处理配置命令
            HandleConfigCommand(message);
        }
        else
        {
            // 处理眼部追踪数据
            _eyeTrackingManager?.ProcessMessage(message);
        }
    }
    
    private void ProcessFaceTrackingMessage(OSCMessage message)
    {
        if (_disposed) return;
        
        // 处理面部表情数据
        _logger.LogDebug($"Processing face tracking message: {message.address}");
        _faceTrackingManager?.ProcessMessage(message);
    }
    
    private void HandleConfigCommand(OSCMessage message)
    {
        if (_disposed) return;
        
        // 处理配置命令 /command/set/field/ value
        var parts = message.address.Split("/");
        
        if (parts.Length >= 4 && parts[2].ToLower() == "set")
        {
            _configManager?.UpdateConfigFromOSC(parts[3], message.value);
        }
    }
    
    private OSCMessage ParseOSCMessage(byte[] buffer, int length)
    {
        OSCMessage msg = new OSCMessage();
        int currentStep = 0;
        string address = ParseOSCAddress(buffer, length, ref currentStep);

        if (address == "")
        {
            _logger.LogDebug("Failed to parse OSC address");
            return msg;
        }
        msg.address = address;

        // OSC addresses are composed of /address, types value, so we need to check if we have a type
        if (currentStep >= length || buffer[currentStep] != 44)
        {
            _logger.LogDebug($"Invalid OSC type marker at position {currentStep}");
            return msg;
        }
        // skipping , char
        currentStep++;

        var types = ParseOSCTypes(buffer, length, ref currentStep);
        switch (types)
        {
            case "s":
                msg.success = true;
                var value = ParseOSCString(buffer, length, ref currentStep);
                if (Validators.CheckIfIPAddress(value))
                    msg.value = new OSCIPAddress(IPAddress.Parse(value));
                else
                    msg.value = new OSCString(value);
                break;
            case "i":
                msg.success = true;
                msg.value = new OSCInteger(ParseOSCInt(buffer, length, ref currentStep));
                break;
            case "f":
                msg.success = true;
                msg.value = new OSCFloat(ParseOSCFloat(buffer, length, ref currentStep));
                break;
            case "F":
                msg.success = true;
                msg.value = new OSCBool(false);
                break;
            case "T":
                msg.success = true;
                msg.value = new OSCBool(true);
                break;
            
            default:
                _logger.LogDebug("Encountered unsupported type: {Type} from {Address}", types, msg.address);
                msg.success = false;
                break;
        }

        return msg;
    }
    
    private string ParseOSCAddress(byte[] buffer, int length, ref int step)
    {
        string oscAddress = "";

        // check if the message starts with /, every OSC address should 
        if (buffer[0] != 47)
        {
            _logger.LogDebug("OSC message does not start with /");
            return oscAddress;
        }

        OSCValueUtils.ExtractStringData(buffer, length, ref step, out oscAddress);
        return oscAddress;
    }

    private string ParseOSCTypes(byte[] buffer, int length, ref int step)
    {
        OSCValueUtils.ExtractStringData(buffer, length, ref step, out var types);
        return types;
    }

    private byte[] ConvertToBigEndian(byte[] buffer)
    {
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(buffer);
        }
        return buffer;
    }

    private float ParseOSCFloat(byte[] buffer, int length, ref int step)
    {
        var valueSection = ConvertToBigEndian(buffer[step..length]);
        float oscValue = BitConverter.ToSingle(valueSection, 0);
        return oscValue;
    }

    private int ParseOSCInt(byte[] buffer, int length, ref int step) 
    {
        var valueSection = ConvertToBigEndian(buffer[step..length]);
        int oscValue = BitConverter.ToInt32(valueSection, 0);
        return oscValue;
    }

    private string ParseOSCString(byte[] buffer, int length, ref int step)
    {
        OSCValueUtils.ExtractStringData(buffer, length, ref step, out var value);
        return value;
    }
    
    private void OnConfigChanged(UnifiedConfig config)
    {
        if (_disposed) return;
        
        // 配置变更时重启OSC监听器
        _logger.LogInformation("Config changed, restarting OSC listeners");
        TearDown();
        Thread.Sleep(100); // 短暂等待确保端口释放
        Start();
    }
    
    public void TearDown()
    {
        lock (_lockObject)
        {
            if (_disposed) return;
            
            _logger.LogInformation("Starting OSC managers shutdown");
            
            // 1. 首先发送取消信号
            foreach (var kvp in _cancellationTokens)
            {
                try
                {
                    kvp.Value.Cancel();
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error cancelling token for port {Port}", kvp.Key);
                }
            }
            
            // 2. 立即关闭所有socket以中断阻塞的接收操作
            foreach (var kvp in _receivers)
            {
                try
                {
                    if (kvp.Value.Connected)
                    {
                        kvp.Value.Shutdown(SocketShutdown.Both);
                    }
                    kvp.Value.Close();
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error closing socket for port {Port}", kvp.Key);
                }
            }
            
            // 3. 等待线程结束，但设置较短的超时时间
            foreach (var kvp in _listenerThreads)
            {
                try
                {
                    if (kvp.Value.IsAlive)
                    {
                        if (!kvp.Value.Join(2000)) // 2秒超时
                        {
                            _logger.LogWarning($"Thread for port {kvp.Key} did not terminate within timeout, it will be abandoned");
                            // 注意：不要调用Abort()，让它作为后台线程自然终止
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error joining thread for port {Port}", kvp.Key);
                }
            }
            
            // 4. 清理资源
            foreach (var socket in _receivers.Values)
            {
                try
                {
                    socket.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error disposing socket");
                }
            }
            
            foreach (var cts in _cancellationTokens.Values)
            {
                try
                {
                    cts.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error disposing cancellation token source");
                }
            }
            
            _receivers.Clear();
            _listenerThreads.Clear();
            _cancellationTokens.Clear();
            
            State = OSCState.IDLE;
            _logger.LogInformation("OSC managers shut down completed");
        }
    }
    
    public void Dispose()
    {
        if (_disposed) return;
        
        _disposed = true;
        TearDown();
    }
}