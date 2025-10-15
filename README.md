# Car Simulation Networked

A complete Unity networked car simulation system with authoritative server (Windows) and remote touchscreen client (Android) using raw TCP/UDP sockets.

---

## **System Overview**

```
┌─────────────────────────────────┐          ┌──────────────────────────────┐
│   SERVER (Windows PC)           │          │   CLIENT (Android Device)    │
│                                 │          │                              │
│  ┌─────────────────────────┐   │          │  ┌──────────────────────┐   │
│  │ Authoritative Physics   │   │          │  │ Touchscreen Controls │   │
│  │ - WheelColliders        │   │          │  │ - Steer, Throttle    │   │
│  │ - Engine/Gears          │   │          │  │ - Brake, Handbrake   │   │
│  │ - Torque Curves         │   │          │  │ - Gear Select        │   │
│  └─────────────────────────┘   │          │  │ - Lights/Indicators  │   │
│                                 │          │  │ - Camera Select      │   │
│  ┌─────────────────────────┐   │          │  └──────────────────────┘   │
│  │ Camera Focus Manager    │   │          │                              │
│  │ - 10 camera anchors     │   │          │  ┌──────────────────────┐   │
│  │ - Smooth transitions    │   │          │  │ State HUD            │   │
│  └─────────────────────────┘   │          │  │ - Speed, Gear        │   │
│                                 │          │  │ - Ping, Camera       │   │
│  ┌─────────────────────────┐   │          │  └──────────────────────┘   │
│  │ Network Layer           │   │          │                              │
│  │ TCP :9000 (control)     │◄─┼─TCP─────►│  TCP (control)               │
│  │ UDP :9001 (state 25Hz)  │◄─┼─UDP─────►│  UDP :9002 (input 60Hz)      │
│  └─────────────────────────┘   │          │  UDP (state recv)            │
└─────────────────────────────────┘          └──────────────────────────────┘
```

---

## **Features**

### **Server (Windows)**
- Authoritative car physics simulation (50 Hz fixed timestep)
- 4-wheel drive with WheelColliders
- Engine torque curves, 6 forward gears + reverse + neutral
- Steering, throttle, brake, handbrake
- Headlights, indicators (left/right/hazard)
- **10 camera anchors** with smooth focus transitions:
  - FL/FR/RL/RR Wheels, Engine, Exhaust, Steering Linkage, Brake Caliper, Suspension, Dashboard
- TCP control plane (port 9000)
- UDP realtime state broadcast (port 9001, 20-30 Hz)
- Input latency handling: holds last input ≤200ms, then decays to neutral
- Debug overlay (ping, input seq, speed, gear, focus)

### **Client (Android)**
- Touchscreen controls (steer drag area, throttle/brake sliders, handbrake toggle)
- Gear selector (R/N/1-6)
- Headlights & indicator toggles
- Camera focus dropdown (10 parts)
- Reset car button
- HUD (speed, gear, indicator, camera focus, ping)
- TCP control plane
- UDP input streaming (30-60 Hz, configurable)

---

## **Network Protocol**

### **Transports**
- **TCP (port 9000)**: Reliable control messages (HELLO, WELCOME, gear, lights, indicators, camera focus, reset)
- **UDP (port 9001)**: Server → Client state broadcast (position, rotation, speed, RPM, wheel slip, etc.)
- **UDP (port 9002)**: Client → Server input streaming (steer, throttle, brake, handbrake)

### **Message Types**

| Type | Direction | Transport | Rate | Description |
|------|-----------|-----------|------|-------------|
| `HELLO_C2S` | Client→Server | TCP | Once | Authentication token, UDP port, client name |
| `WELCOME_S2C` | Server→Client | TCP | Once | Session ID, tick rate, car ID |
| `INPUT_C2S` | Client→Server | UDP | 30-60 Hz | Steer, throttle, brake, handbrake |
| `STATE_S2C` | Server→Client | UDP | 20-30 Hz | Position, rotation, speed, RPM, gear, wheel slip, lights, indicator, camera focus, last processed input seq |
| `SET_GEAR_C2S` | Client→Server | TCP | On demand | Gear (-1=R, 0=N, 1-6) |
| `TOGGLE_HEADLIGHTS_C2S` | Client→Server | TCP | On demand | Headlights on/off |
| `SET_INDICATOR_C2S` | Client→Server | TCP | On demand | Indicator mode (Off/Left/Right/Hazard) |
| `SET_CAMERA_FOCUS_C2S` | Client→Server | TCP | On demand | Camera part ID (0-9) |
| `RESET_CAR_C2S` | Client→Server | TCP | On demand | Reset car to spawn position |
| `SERVER_NOTICE_S2C` | Server→Client | TCP | On error | Error code + text |

### **Header Format (7 bytes, little-endian)**
```
MsgType    : 1 byte
Seq        : 2 bytes (ushort)
TimestampMs: 4 bytes (uint)
```

### **Camera Part IDs**
| ID | Name | Description |
|----|------|-------------|
| 0 | FL_Wheel | Front Left Wheel |
| 1 | FR_Wheel | Front Right Wheel |
| 2 | RL_Wheel | Rear Left Wheel |
| 3 | RR_Wheel | Rear Right Wheel |
| 4 | Engine | Engine Bay |
| 5 | Exhaust | Exhaust Pipe |
| 6 | SteeringLinkage | Steering Mechanism |
| 7 | BrakeCaliperFront | Front Brake Caliper |
| 8 | SuspensionFront | Front Suspension |
| 9 | Dashboard | Dashboard (default) |

---

## **Project Structure**

```
Assets/
├── _Shared/
│   ├── Config/
│   │   └── NetConfig.cs              # ScriptableObject config
│   └── Net/
│       ├── ByteCodec.cs              # Little-endian serialization
│       ├── RingBuffer.cs             # Thread-safe queue
│       ├── StopwatchTime.cs          # Monotonic clock
│       ├── MessageTypes.cs           # Enums
│       └── Protocol.cs               # Message structs & serializers
├── Server/
│   ├── Scenes/
│   │   └── Server_CarSim.unity
│   └── Scripts/
│       ├── TcpServerPeer.cs          # TCP listener
│       ├── UdpServerPeer.cs          # UDP receiver/sender
│       ├── ServerCommandRouter.cs    # Message dispatcher
│       ├── ServerSimulationController.cs  # Authoritative physics
│       ├── CameraFocusManager.cs     # Camera transitions
│       ├── StateBroadcaster.cs       # State broadcast loop
│       └── DebugOverlay.cs           # Server debug UI
├── Client/
│   ├── Scenes/
│   │   └── Client_RemoteControl.unity
│   └── Scripts/
│       ├── TcpClientPeer.cs          # TCP connector
│       ├── UdpClientPeer.cs          # UDP sender/receiver
│       ├── ClientConnectionUI.cs     # Connection UI
│       ├── ClientInputController.cs  # Input handling
│       └── ClientStateHUD.cs         # HUD display
└── Editor/
    └── SceneSetupHelper.cs           # Scene creation helpers
```

---

## **Setup Instructions**

### **1. Create NetConfig Asset**
1. In Unity, go to **CarSim → Create NetConfig Asset**
2. Asset created at `Assets/_Shared/Config/NetConfig.asset`
3. Default settings:
   - **Token**: `demo-token-123456`
   - **TCP Port**: 9000
   - **UDP Port Server**: 9001
   - **UDP Port Client**: 9002
   - **Sim Tick Rate**: 50 Hz
   - **Input Send Rate**: 60 Hz
   - **State Send Rate**: 25 Hz

### **2. Setup Server Scene**

#### **Option A: Auto-Generate (Quick Start)**
1. Go to **CarSim → Setup → Create Server Scene**
2. Opens `Server_CarSim.unity` with basic car + ground

#### **Option B: Manual Setup**

**Hierarchy:**
```
Server_CarSim
├── Ground (Plane, scale 10x10)
├── Directional Light
├── Car
│   ├── Body (Cube mesh, no collider)
│   ├── CenterOfMass (empty, local pos Y=-0.2)
│   ├── WheelCollider_FL/FR/RL/RR (WheelCollider components)
│   ├── WheelMesh_FL/FR/RL/RR (Cylinder meshes)
│   └── Anchors (10 empty GameObjects for camera parts)
│       ├── Anchor_Dashboard
│       ├── Anchor_FL_Wheel
│       ├── Anchor_FR_Wheel
│       ├── Anchor_RL_Wheel
│       ├── Anchor_RR_Wheel
│       ├── Anchor_Engine
│       ├── Anchor_Exhaust
│       ├── Anchor_SteeringLinkage
│       ├── Anchor_BrakeCaliperFront
│       └── Anchor_SuspensionFront
├── Main Camera
│   └── CameraFocusManager component
└── ServerSystems (empty GameObject)
    ├── TcpServerPeer
    ├── UdpServerPeer
    ├── ServerCommandRouter
    ├── ServerSimulationController
    ├── StateBroadcaster
    └── DebugOverlay
```

**Component Wiring (ServerSystems):**

1. **Add all 7 components** to `ServerSystems` GameObject

2. **TcpServerPeer:**
   - Config: Assign `NetConfig.asset`

3. **UdpServerPeer:**
   - Config: Assign `NetConfig.asset`
   - Simulate Drop Percent: 0 (set to 10 for testing)

4. **ServerCommandRouter:**
   - Config: Assign `NetConfig.asset`
   - Tcp Peer: Assign `TcpServerPeer`
   - Udp Peer: Assign `UdpServerPeer`
   - Sim Controller: Assign `ServerSimulationController`
   - Camera Focus Manager: Assign `CameraFocusManager` (on Main Camera)

5. **ServerSimulationController:**
   - Car Body: Assign `Car` Rigidbody
   - Center Of Mass: Assign `Car/CenterOfMass`
   - Wheel FL/FR/RL/RR: Assign 4 WheelColliders
   - Wheel Mesh FL/FR/RL/RR: Assign 4 Cylinder meshes
   - **Torque Curve**: Create AnimationCurve (0,200) → (6000,400)
   - **Gear Ratios**: Array size 8: `[-3.5, 0, 3.5, 2.5, 1.8, 1.3, 1.0, 0.8]`
   - Final Drive Ratio: 3.5
   - Max Rpm: 6000
   - Max Steer Angle: 30
   - Steer Speed: 5
   - Brake Torque: 3000
   - Handbrake Torque: 5000
   - Brake Bias Front: 0.6

6. **CameraFocusManager** (on Main Camera):
   - Main Camera: Assign Main Camera
   - **Focus Points**: Array size 10, fill each with:
     - Part Id: (0-9 corresponding to enum)
     - Anchor: Assign corresponding anchor transform
     - Offset: e.g., `(0, 1, -3)` for behind/above view
     - FOV: 60
     - Lerp Time: 1

7. **StateBroadcaster:**
   - Config: Assign `NetConfig.asset`
   - Udp Peer: Assign `UdpServerPeer`
   - Sim Controller: Assign `ServerSimulationController`
   - Camera Focus Manager: Assign `CameraFocusManager`

8. **DebugOverlay:**
   - Create Canvas (Screen Space Overlay) with Text child
   - Udp Peer: Assign `UdpServerPeer`
   - Sim Controller: Assign `ServerSimulationController`
   - Camera Focus Manager: Assign `CameraFocusManager`
   - Status Text: Assign Text component

### **3. Setup Client Scene**

#### **Option A: Auto-Generate (Basic)**
1. Go to **CarSim → Setup → Create Client Scene**
2. Opens `Client_RemoteControl.unity` with basic UI

#### **Option B: Manual Setup (Full UI)**

**Hierarchy:**
```
Client_RemoteControl
├── UI_Root (Canvas - Screen Space Overlay)
│   ├── Panel_Connect
│   │   ├── Text_Title ("REMOTE CAR CONTROL")
│   │   ├── Input_ServerIP (InputField, placeholder: 192.168.1.100)
│   │   ├── Input_Token (InputField, placeholder: demo-token-123456)
│   │   ├── Button_Connect (Button, text: CONNECT)
│   │   └── Text_Status (Text, "Enter server IP...")
│   └── Panel_Drive (Initially disabled)
│       ├── Area_Steer (RectTransform, drag area for steering)
│       ├── Slider_Throttle (Slider, 0-1)
│       ├── Slider_Brake (Slider, 0-1)
│       ├── Toggle_Handbrake (Toggle)
│       ├── Gear Buttons (8 buttons: R/N/1/2/3/4/5/6)
│       ├── Toggle_Headlights (Toggle)
│       ├── Indicator Buttons (4 buttons: Off/Left/Right/Hazard)
│       ├── Dropdown_CameraFocus (Dropdown, 10 options)
│       ├── Button_ResetCar (Button, text: RESET)
│       └── HUD (5 Text elements: Speed, Gear, Indicator, Focus, Ping)
└── ClientSystems (empty GameObject)
    ├── TcpClientPeer
    ├── UdpClientPeer
    ├── ClientConnectionUI
    ├── ClientInputController
    └── ClientStateHUD
```

**Component Wiring (ClientSystems):**

1. **ClientConnectionUI:**
   - Config: Assign `NetConfig.asset`
   - Tcp Peer: Assign `TcpClientPeer`
   - Udp Peer: Assign `UdpClientPeer`
   - Panel Connect: Assign `Panel_Connect`
   - Input Server IP: Assign `Input_ServerIP`
   - Input Token: Assign `Input_Token`
   - Button Connect: Assign `Button_Connect`
   - Text Status: Assign `Text_Status`
   - Panel Drive: Assign `Panel_Drive`

2. **ClientInputController:**
   - Config: Assign `NetConfig.asset`
   - Tcp Peer: Assign `TcpClientPeer`
   - Udp Peer: Assign `UdpClientPeer`
   - Steer Area: Assign `Area_Steer` RectTransform
   - Slider Throttle: Assign `Slider_Throttle`
   - Slider Brake: Assign `Slider_Brake`
   - Toggle Handbrake: Assign `Toggle_Handbrake`
   - Gear Buttons: Assign all 8 gear buttons
   - Toggle Headlights: Assign `Toggle_Headlights`
   - Indicator Buttons: Assign 4 buttons
   - Dropdown Camera Focus: Assign `Dropdown_CameraFocus`
   - Btn Reset Car: Assign `Button_ResetCar`

3. **ClientStateHUD:**
   - Udp Peer: Assign `UdpClientPeer`
   - Text Speed/Gear/Indicator/Camera Focus/Ping: Assign HUD Text elements

4. **TcpClientPeer & UdpClientPeer:**
   - Config: Assign `NetConfig.asset`

### **4. Unity Project Settings**

**Time Settings:**
- Edit → Project Settings → Time
- Fixed Timestep: `0.02` (50 Hz)

**Player Settings (Server - Windows):**
- Edit → Project Settings → Player → PC, Mac & Linux Standalone
- Resolution and Presentation:
  - Run In Background: ✓
  - VSync Count: Don't Sync
- Other Settings:
  - Scripting Backend: Mono or IL2CPP
  - API Compatibility Level: .NET 4.x

**Player Settings (Client - Android):**
- Edit → Project Settings → Player → Android
- Other Settings:
  - Scripting Backend: IL2CPP
  - Target Architectures: ARM64 ✓
  - Internet Access: Require
  - Write Permission: External (optional)

---

## **Build Instructions**

### **Windows Server Build**
1. Open `Server_CarSim.unity`
2. File → Build Settings
3. Platform: PC, Mac & Linux Standalone
4. Architecture: x86_64
5. Add Open Scene (`Server_CarSim`)
6. Build → Save as `CarSimServer.exe`

### **Android Client Build**
1. Open `Client_RemoteControl.unity`
2. File → Build Settings
3. Platform: Android
4. Add Open Scene (`Client_RemoteControl`)
5. Build → Save as `CarSimClient.apk`
6. Install on Android device via USB or ADB

---

## **Running the System**

### **Step 1: Start Server**
1. Run `CarSimServer.exe` on Windows PC
2. Server listens on:
   - TCP: `0.0.0.0:9000`
   - UDP: `0.0.0.0:9001`
3. Check console for `[TcpServer] Listening on port 9000`

### **Step 2: Find Server IP**
- On Windows, open Command Prompt:
  ```
  ipconfig
  ```
- Note the **IPv4 Address** of your Wi-Fi adapter (e.g., `192.168.1.100`)

### **Step 3: Connect Client**
1. Ensure Android device is on **same Wi-Fi network** as server
2. Launch `CarSimClient` APK on Android
3. Enter server IP (e.g., `192.168.1.100`)
4. Tap **CONNECT**
5. Wait for "Connected!" status

### **Step 4: Drive**
- **Steer**: Drag left/right on steer area
- **Throttle/Brake**: Adjust sliders
- **Handbrake**: Toggle on/off
- **Gear**: Tap R/N/1-6 buttons
- **Headlights**: Toggle on/off
- **Indicators**: Tap Left/Right/Hazard/Off buttons
- **Camera Focus**: Select from dropdown (10 parts)
- **Reset**: Tap RESET button

---

## **Controls Reference**

### **Client Controls**

| Control | Type | Range | Description |
|---------|------|-------|-------------|
| **Steer Area** | Drag | -1 to 1 | Horizontal drag to steer |
| **Throttle Slider** | Slider | 0 to 1 | Accelerate |
| **Brake Slider** | Slider | 0 to 1 | Brake |
| **Handbrake Toggle** | Toggle | On/Off | Rear wheel brake |
| **Gear Buttons** | Buttons | R/N/1-6 | Gear selection |
| **Headlights Toggle** | Toggle | On/Off | Toggle headlights |
| **Indicator Buttons** | Buttons | Off/L/R/H | Turn signals |
| **Camera Dropdown** | Dropdown | 0-9 | Camera focus part |
| **Reset Button** | Button | - | Reset car to spawn |

### **Server Debug Overlay**
- **Input Seq**: Last processed input sequence number
- **Input Age**: Milliseconds since last input received
- **Speed**: Current speed in km/h
- **RPM**: Engine RPM
- **Gear**: Current gear (R/N/1-6)
- **Steer**: Steering angle in degrees
- **Focus**: Current camera part name
- **Lights**: Headlight state
- **Indicator**: Indicator state

---

## **Testing Matrix**

### **Basic Connectivity**
- ✓ Client connects to server on same Wi-Fi
- ✓ HELLO/WELCOME handshake completes
- ✓ Client enters drive mode

### **Input Controls (all 5+ required controls)**
- ✓ Steer: Car turns left/right on server
- ✓ Throttle: Car accelerates
- ✓ Brake: Car decelerates
- ✓ Handbrake: Rear wheels lock
- ✓ Gear R: Car reverses
- ✓ Gear N: Car idles
- ✓ Gear 1-6: Car shifts gears

### **Light & Indicator Toggles**
- ✓ Headlights toggle on/off
- ✓ Indicator Left/Right/Hazard/Off

### **Camera Focus**
- ✓ Select all 10 camera parts from dropdown
- ✓ Server camera smoothly transitions to anchor
- ✓ Client HUD shows current camera part name

### **Latency & Performance**
- ✓ Ping < 150ms on LAN
- ✓ Input stream ≤60 Hz (check with Wireshark)
- ✓ State stream ~25 Hz
- ✓ GC allocations near-zero during play (Unity Profiler)

### **Resilience**
- ✓ Server handles stale input (>200ms) by decaying to neutral
- ✓ Simulate 10% packet drop: car still responsive
- ✓ Client disconnect: server continues, client can reconnect

---

## **Network Rates Summary**

| Layer | Rate | Description |
|-------|------|-------------|
| **Physics** | 50 Hz | Fixed timestep (0.02s) |
| **Client Input** | 30-60 Hz | Configurable via NetConfig.inputSendRate |
| **Server State** | 20-30 Hz | Configurable via NetConfig.stateSendRate |
| **TCP Control** | On-demand | Gear, lights, indicators, camera, reset |

---

## **Limitations & Known Issues**

1. **Single Client**: Server accepts only one TCP connection at a time
2. **LAN Only**: No NAT traversal or relay server
3. **No Interpolation**: Client displays raw server state (consider adding for smoother visuals)
4. **No Encryption**: Token sent in plaintext over TCP (add TLS for production)
5. **Fixed Camera Offsets**: Camera offsets are serialized in Inspector (adjust per anchor for best view)

---

## **Troubleshooting**

### **Client can't connect**
- Ensure both devices on same Wi-Fi
- Check Windows Firewall allows TCP 9000, UDP 9001
- Verify server IP with `ipconfig`
- Check server console for `[TcpServer] Listening on port 9000`

### **High ping (>150ms)**
- Check Wi-Fi signal strength
- Close bandwidth-heavy apps
- Reduce `inputSendRate` and `stateSendRate` in NetConfig

### **Car not moving**
- Check server console for `[UdpServer] Client endpoint learned`
- Verify client is sending inputs (server debug overlay shows `Input Seq` increasing)
- Check WheelColliders have proper ground contact (adjust suspension)

### **Camera not switching**
- Verify all 10 anchors assigned in `CameraFocusManager.focusPoints`
- Check anchor positions are distinct
- Ensure `CameraFocusManager.mainCamera` is assigned

### **Build errors**
- Ensure .NET 4.x API Compatibility Level
- Check IL2CPP build (Android) completed without stripping errors
- Verify all scripts have no syntax errors

---

## **Performance Tuning**

### **Reduce Latency**
- Increase `inputSendRate` to 60 Hz
- Reduce `stateSendRate` to 20 Hz (lower bandwidth)
- Use wired Ethernet for server PC (if possible)

### **Reduce Bandwidth**
- Lower `stateSendRate` to 20 Hz
- Compress state data (e.g., quantize floats to shorts)

### **Avoid GC Spikes**
- All byte buffers pre-allocated
- RingBuffers use fixed-size arrays
- No LINQ or boxing in hot paths
- Use Unity Profiler to verify

---

## **Extending the System**

### **Add More Clients**
- Modify `TcpServerPeer` to accept multiple clients (list of connections)
- Track client IDs and separate input queues
- Broadcast state to all clients

### **Add Interpolation**
- Store last 2-3 server states on client
- Lerp between states based on timestamps

### **Add Prediction**
- Client simulates car locally using same physics
- Apply server corrections when state arrives

### **Add Voice Chat**
- Use separate UDP channel for Opus-encoded audio

---

## **License**

This project is provided as-is for educational purposes. Feel free to modify and extend.

---

## **Credits**

- **Networking**: Raw TCP/UDP with System.Net.Sockets
- **Physics**: Unity WheelColliders
- **UI**: Unity UI (uGUI)

---

**Enjoy your networked car simulation!**
