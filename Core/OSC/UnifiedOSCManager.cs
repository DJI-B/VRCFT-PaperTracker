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

// 统一OSC管理器
public class UnifiedOSCManager
{
    private readonly ILogger _logger;
    private readonly EyeTrackingManager? _eyeTrackingManager;
    private readonly FaceTrackingManager? _faceTrackingManager;
    
    private readonly Dictionary<int, Socket> _receivers = new();
    private readonly Dictionary<int, Thread> _listenerThreads = new();
    private readonly Dictionary<int, ManualResetEvent> _terminateEvents = new();
    
    private UnifiedConfigManager? _configManager;
    private const int ConnectionTimeout = 10000;
    
    public OSCState State { get; private set; } = OSCState.IDLE;
    
    public UnifiedOSCManager(ILogger logger, EyeTrackingManager? eyeManager, FaceTrackingManager? faceManager)
    {
        _logger = logger;
        _eyeTrackingManager = eyeManager;
        _faceTrackingManager = faceManager;
    }
    
    public void RegisterConfigManager(UnifiedConfigManager configManager)
    {
        _configManager = configManager;
        configManager.RegisterListener(OnConfigChanged);
    }
    
    public void Start()
    {
        if (_configManager == null) return;
        
        var config = _configManager.Config;
        
        // 启动眼部追踪OSC监听
        if (config.EnableEyeTracking && _eyeTrackingManager != null)
        {
            StartOSCListener(config.EyeTracking.PortNumber, config.EyeTracking.ListeningAddress, ProcessEyeTrackingMessage);
        }
        
        // 启动面部追踪OSC监听 - 修复：绑定到IPAddress.Any而不是特定IP
        if (config.EnableFaceTracking && _faceTrackingManager != null)
        {
            _logger.LogInformation($"Attempting to start face tracking OSC listener on port {config.FaceTracking.FacePort}");
            StartOSCListener(config.FaceTracking.FacePort, IPAddress.Any, ProcessFaceTrackingMessage);
        }
        
        State = OSCState.CONNECTED;
    }
    
    private void StartOSCListener(int port, IPAddress address, Action<OSCMessage> messageHandler)
    {
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
            _terminateEvents[port] = new ManualResetEvent(false);
            
            var thread = new Thread(() => OSCListenLoop(port, messageHandler))
            {
                Name = $"OSC-Listener-{port}",
                IsBackground = true
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
    
    private void OSCListenLoop(int port, Action<OSCMessage> messageHandler)
    {
        var buffer = new byte[4096];
        
        if (!_receivers.TryGetValue(port, out var socket) || 
            !_terminateEvents.TryGetValue(port, out var terminateEvent))
        {
            _logger.LogError($"Failed to get socket or terminate event for port {port}");
            return;
        }
        
        _logger.LogInformation($"OSC listen loop started for port {port}");
        
        while (!terminateEvent.WaitOne(0))
        {
            try
            {
                if (socket.IsBound)
                {
                    var length = socket.Receive(buffer);
                    _logger.LogDebug($"Received {length} bytes on port {port}");
                    
                    var message = ParseOSCMessage(buffer, length);
                    if (message.success)
                    {
                        _logger.LogDebug($"Successfully parsed OSC message: {message.address}");
                        messageHandler(message);
                    }
                    else
                    {
                        _logger.LogDebug($"Failed to parse OSC message on port {port}");
                    }
                }
            }
            catch (SocketException ex) when (ex.ErrorCode == 10060) // WSAETIMEDOUT
            {
                // 接收超时，这是正常的，继续循环
                continue;
            }
            catch (SocketException ex)
            {
                _logger.LogError(ex, $"Socket error in OSC listen loop for port {port}. Error code: {ex.ErrorCode}");
                if (ex.ErrorCode == 10054) // WSAECONNRESET
                {
                    _logger.LogWarning($"Connection reset on port {port}");
                    continue;
                }
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error in OSC listen loop for port {port}");
                // 短暂等待后继续，避免快速循环
                Thread.Sleep(100);
            }
        }
        
        _logger.LogInformation($"OSC listen loop ended for port {port}");
    }
    
    private void ProcessEyeTrackingMessage(OSCMessage message)
    {
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
        // 处理面部表情数据
        _logger.LogDebug($"Processing face tracking message: {message.address}");
        _faceTrackingManager?.ProcessMessage(message);
    }
    
    private void HandleConfigCommand(OSCMessage message)
    {
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
        // 配置变更时重启OSC监听器
        _logger.LogInformation("Config changed, restarting OSC listeners");
        TearDown();
        Thread.Sleep(100); // 短暂等待确保端口释放
        Start();
    }
    
    public void TearDown()
    {
        foreach (var kvp in _terminateEvents)
        {
            kvp.Value.Set();
        }
        
        foreach (var kvp in _listenerThreads)
        {
            if (kvp.Value.IsAlive)
            {
                kvp.Value.Join(5000); // 5秒超时
            }
        }
        
        foreach (var kvp in _receivers)
        {
            try
            {
                if (kvp.Value.Connected)
                {
                    kvp.Value.Shutdown(SocketShutdown.Both);
                }
                kvp.Value.Close();
                kvp.Value.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error closing socket for port {Port}", kvp.Key);
            }
        }
        
        _receivers.Clear();
        _listenerThreads.Clear();
        
        foreach (var kvp in _terminateEvents)
        {
            kvp.Value.Dispose();
        }
        _terminateEvents.Clear();
        
        State = OSCState.IDLE;
        _logger.LogInformation("OSC managers shut down");
    }
}