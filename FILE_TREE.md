# Complete File Tree

```
d:\Unity Projects\CarSimulationNetworked\
│
├── README.md                          # Main documentation
├── SCENE_SETUP.md                     # Detailed scene wiring guide
├── FILE_TREE.md                       # This file
│
├── Assets\
│   │
│   ├── _Shared\                       # Shared code between Server & Client
│   │   │
│   │   ├── Config\
│   │   │   ├── NetConfig.cs           # ScriptableObject config (ports, rates, token)
│   │   │   └── NetConfig.asset        # (Create via menu: CarSim → Create NetConfig Asset)
│   │   │
│   │   └── Net\                       # Core networking primitives
│   │       ├── ByteCodec.cs           # Little-endian serialization (Write/Read primitives)
│   │       ├── RingBuffer.cs          # Thread-safe single-producer/consumer queue
│   │       ├── StopwatchTime.cs       # Monotonic timestamp (uint ms)
│   │       ├── MessageTypes.cs        # Enums: MsgType, CameraPartId, IndicatorMode, LightFlags
│   │       └── Protocol.cs            # Message structs & serializers/deserializers
│   │
│   ├── Server\                        # Server-only code (Windows build)
│   │   │
│   │   ├── Scenes\
│   │   │   └── Server_CarSim.unity    # Main server scene
│   │   │
│   │   └── Scripts\
│   │       ├── TcpServerPeer.cs       # TCP listener (port 9000), accepts 1 client
│   │       ├── UdpServerPeer.cs       # UDP receiver (port 9001) + sender
│   │       ├── ServerCommandRouter.cs # Routes TCP messages (HELLO, gear, lights, etc.)
│   │       ├── ServerSimulationController.cs  # Authoritative car physics (WheelColliders, engine, gears)
│   │       ├── CameraFocusManager.cs  # Smooth camera transitions (10 focus points)
│   │       ├── StateBroadcaster.cs    # UDP state broadcast loop (20-30 Hz)
│   │       └── DebugOverlay.cs        # Server debug UI (ping, speed, gear, etc.)
│   │
│   ├── Client\                        # Client-only code (Android build)
│   │   │
│   │   ├── Scenes\
│   │   │   └── Client_RemoteControl.unity  # Main client scene
│   │   │
│   │   └── Scripts\
│   │       ├── TcpClientPeer.cs       # TCP connector (to port 9000)
│   │       ├── UdpClientPeer.cs       # UDP sender (to port 9001) + receiver (port 9002)
│   │       ├── ClientConnectionUI.cs  # Connection UI (IP input, token, connect button)
│   │       ├── ClientInputController.cs  # Input handling (steer, throttle, brake, gears, etc.)
│   │       └── ClientStateHUD.cs      # HUD display (speed, gear, ping, camera focus)
│   │
│   ├── Editor\                        # Editor-only helpers
│   │   └── SceneSetupHelper.cs        # Menu: CarSim → Setup → Create Server/Client Scene
│   │
│   ├── Art\                           # (Optional) 3D models, textures
│   ├── Materials\                     # (Optional) Car materials
│   └── UI\                            # (Optional) UI sprites
│
├── ProjectSettings\
│   ├── TimeManager.asset              # Fixed Timestep = 0.02
│   ├── ProjectSettings.asset          # Target platforms: Windows + Android
│   └── ...
│
└── Packages\
    └── manifest.json                  # Unity package dependencies (default only)
```

---

## **Key Files Summary**

### **Configuration**
- **NetConfig.cs / .asset**: Single source of truth for ports, rates, token

### **Network Layer (Shared)**
- **ByteCodec.cs**: Serialization/deserialization (little-endian)
- **RingBuffer.cs**: Thread-safe queue for cross-thread communication
- **StopwatchTime.cs**: Monotonic timestamps
- **Protocol.cs**: All message types & payloads

### **Server Scripts**
- **TcpServerPeer**: Listens for TCP connections, spawns recv/send threads
- **UdpServerPeer**: Receives INPUT_C2S, sends STATE_S2C
- **ServerCommandRouter**: Dispatches TCP messages to sim controller & camera manager
- **ServerSimulationController**: Authoritative physics (WheelColliders, engine, gears, brakes)
- **CameraFocusManager**: Manages 10 camera anchors with smooth transitions
- **StateBroadcaster**: Packages & sends STATE_S2C at 20-30 Hz
- **DebugOverlay**: Displays server state on-screen

### **Client Scripts**
- **TcpClientPeer**: Connects to server, sends HELLO, receives WELCOME
- **UdpClientPeer**: Sends INPUT_C2S at 30-60 Hz, receives STATE_S2C
- **ClientConnectionUI**: Manages connection flow (IP/token → connect → drive mode)
- **ClientInputController**: Reads touchscreen input, sends to server
- **ClientStateHUD**: Displays speed, gear, ping, camera focus

### **Editor Helpers**
- **SceneSetupHelper**: Auto-generates server/client scenes with basic hierarchy

---

## **Network Ports**

| Port | Protocol | Direction | Purpose |
|------|----------|-----------|---------|
| 9000 | TCP | Bidirectional | Control plane (HELLO, WELCOME, gear, lights, indicators, camera, reset) |
| 9001 | UDP | Server → Client | State broadcast (position, rotation, speed, RPM, wheel slip, etc.) |
| 9002 | UDP | Client → Server | Input stream (steer, throttle, brake, handbrake) |

---

## **Message Flow**

### **Connection Handshake (TCP)**
```
Client                          Server
  |                               |
  |-------- HELLO_C2S ----------->|  (token, UDP port, name)
  |                               |
  |<------- WELCOME_S2C ----------|  (sessionId, tickRate, carId)
  |                               |
```

### **Realtime Loop (UDP)**
```
Client                          Server
  |                               |
  |-------- INPUT_C2S ----------->|  @ 30-60 Hz
  |   (steer, throttle, brake)    |
  |                               |
  |<------- STATE_S2C ------------|  @ 20-30 Hz
  |   (pos, rot, speed, RPM...)   |
  |                               |
```

### **Discrete Commands (TCP)**
```
Client                          Server
  |                               |
  |---- SET_GEAR_C2S ------------>|  (on button press)
  |---- TOGGLE_HEADLIGHTS_C2S --->|
  |---- SET_INDICATOR_C2S ------->|
  |---- SET_CAMERA_FOCUS_C2S ---->|
  |---- RESET_CAR_C2S ----------->|
  |                               |
```

---

## **Build Outputs**

### **Windows Server**
- **Executable**: `CarSimServer.exe`
- **Data Folder**: `CarSimServer_Data/`
- **Run**: Double-click `CarSimServer.exe`
- **Ports**: Opens TCP 9000, UDP 9001

### **Android Client**
- **APK**: `CarSimClient.apk`
- **Install**: `adb install CarSimClient.apk`
- **Ports**: Binds UDP 9002, connects to server TCP 9000 / UDP 9001

---

## **Quick Start Checklist**

1. ✓ Create NetConfig.asset: **CarSim → Create NetConfig Asset**
2. ✓ Create Server scene: **CarSim → Setup → Create Server Scene**
3. ✓ Create Client scene: **CarSim → Setup → Create Client Scene**
4. ✓ Wire all Inspector references (see SCENE_SETUP.md)
5. ✓ Set Fixed Timestep to 0.02: **Edit → Project Settings → Time**
6. ✓ Build Windows Standalone (Server scene)
7. ✓ Build Android (Client scene)
8. ✓ Run server, get IP with `ipconfig`
9. ✓ Connect client, enter server IP
10. ✓ Drive!

---

## **Testing Scenarios**

### **Basic Functionality**
- [ ] Client connects to server
- [ ] Steer left/right → car turns
- [ ] Throttle → car accelerates
- [ ] Brake → car decelerates
- [ ] Handbrake → rear wheels lock
- [ ] Gear R → car reverses
- [ ] Gear N → car idles
- [ ] Gear 1-6 → car shifts

### **Lights & Indicators**
- [ ] Headlights toggle on/off
- [ ] Indicator Left → server shows left
- [ ] Indicator Right → server shows right
- [ ] Indicator Hazard → both blink
- [ ] Indicator Off → all off

### **Camera Focus**
- [ ] Select all 10 camera parts from dropdown
- [ ] Server camera smoothly transitions to each anchor
- [ ] Client HUD displays current camera part name

### **Latency & Resilience**
- [ ] Ping < 150ms on LAN
- [ ] Set "Simulate Drop Percent" to 10% → car still responsive
- [ ] Input age > 200ms → car decays to neutral throttle/steer

---

## **Common Issues & Fixes**

| Issue | Cause | Fix |
|-------|-------|-----|
| Client can't connect | Firewall blocking | Allow TCP 9000, UDP 9001 in Windows Firewall |
| High ping (>150ms) | Wi-Fi congestion | Use 5GHz Wi-Fi, reduce send rates |
| Car not moving | WheelColliders not grounded | Adjust suspension distance, check ground collider |
| Camera not switching | Anchors not assigned | Verify all 10 anchors in CameraFocusManager.focusPoints |
| Build errors | Missing .NET 4.x | Edit → Project Settings → Player → API Compatibility = .NET 4.x |

---

**For detailed setup, see README.md and SCENE_SETUP.md**
