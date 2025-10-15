# Quick Reference Guide

One-page cheat sheet for the Car Simulation Networked project.

---

## **Network Protocol**

```
┌─────────────────────────────────────────────────────────────────┐
│                         HEADER (7 bytes)                        │
├──────────────┬──────────────────────┬───────────────────────────┤
│  MsgType (1) │  Seq (2)             │  TimestampMs (4)          │
│  byte        │  ushort LE           │  uint LE                  │
└──────────────┴──────────────────────┴───────────────────────────┘
```

### **TCP Messages (Port 9000)**

| ID | Name | Direction | Payload |
|----|------|-----------|---------|
| 0 | `HELLO_C2S` | C→S | token[16] + udpPort(2) + name(var) |
| 1 | `WELCOME_S2C` | S→C | sessionId(4) + tickRate(1) + carId(1) |
| 4 | `SET_GEAR_C2S` | C→S | gear(1) sbyte |
| 5 | `TOGGLE_HEADLIGHTS_C2S` | C→S | on(1) byte |
| 6 | `SET_INDICATOR_C2S` | C→S | mode(1) byte |
| 7 | `SET_CAMERA_FOCUS_C2S` | C→S | partId(1) byte |
| 8 | `RESET_CAR_C2S` | C→S | (none) |
| 9 | `SERVER_NOTICE_S2C` | S→C | code(1) + text(var) |

### **UDP Messages**

| ID | Name | Direction | Rate | Payload |
|----|------|-----------|------|---------|
| 2 | `INPUT_C2S` | C→S (9001) | 30-60 Hz | steer(4) + throttle(4) + brake(4) + handbrake(1) = 13 bytes |
| 3 | `STATE_S2C` | S→C (9002) | 20-30 Hz | pos(12) + rot(16) + speed(4) + rpm(4) + gear(1) + steerAngle(4) + slips(16) + lights(1) + indicator(1) + camera(1) + lastSeq(2) = 62 bytes |

---

## **Component Wiring**

### **Server (ServerSystems GameObject)**

```
TcpServerPeer
├─ config: NetConfig.asset

UdpServerPeer
├─ config: NetConfig.asset
└─ simulateDropPercent: 0

ServerCommandRouter
├─ config: NetConfig.asset
├─ tcpPeer: TcpServerPeer
├─ udpPeer: UdpServerPeer
├─ simController: ServerSimulationController
└─ cameraFocusManager: CameraFocusManager (Main Camera)

ServerSimulationController
├─ carBody: Car Rigidbody
├─ centerOfMass: Car/CenterOfMass
├─ wheelFL/FR/RL/RR: 4× WheelCollider
├─ wheelMeshFL/FR/RL/RR: 4× Transform
├─ torqueCurve: (0,200)→(6000,400)
├─ gearRatios[8]: [-3.5, 0, 3.5, 2.5, 1.8, 1.3, 1.0, 0.8]
├─ finalDriveRatio: 3.5
├─ maxRpm: 6000
├─ maxSteerAngle: 30
├─ steerSpeed: 5
├─ brakeTorque: 3000
├─ handbrakeTorque: 5000
└─ brakeBiasFront: 0.6

CameraFocusManager (Main Camera)
├─ mainCamera: Main Camera
└─ focusPoints[10]: (anchor + offset + fov + lerpTime)

StateBroadcaster
├─ config: NetConfig.asset
├─ udpPeer: UdpServerPeer
├─ simController: ServerSimulationController
└─ cameraFocusManager: CameraFocusManager

DebugOverlay
├─ udpPeer: UdpServerPeer
├─ simController: ServerSimulationController
├─ cameraFocusManager: CameraFocusManager
└─ statusText: Canvas/Text
```

### **Client (ClientSystems GameObject)**

```
TcpClientPeer
└─ config: NetConfig.asset

UdpClientPeer
└─ config: NetConfig.asset

ClientConnectionUI
├─ config: NetConfig.asset
├─ tcpPeer: TcpClientPeer
├─ udpPeer: UdpClientPeer
├─ panelConnect: Panel_Connect
├─ inputServerIP: Input_ServerIP
├─ inputToken: Input_Token
├─ buttonConnect: Button_Connect
├─ textStatus: Text_Status
└─ panelDrive: Panel_Drive

ClientInputController
├─ config: NetConfig.asset
├─ tcpPeer: TcpClientPeer
├─ udpPeer: UdpClientPeer
├─ steerArea: Area_Steer RectTransform
├─ sliderThrottle: Slider_Throttle
├─ sliderBrake: Slider_Brake
├─ toggleHandbrake: Toggle_Handbrake
├─ btnGearR/N/1/2/3/4/5/6: 8× Button
├─ toggleHeadlights: Toggle_Headlights
├─ btnIndicatorOff/Left/Right/Hazard: 4× Button
├─ dropdownCameraFocus: Dropdown_CameraFocus
└─ btnResetCar: Button_ResetCar

ClientStateHUD
├─ udpPeer: UdpClientPeer
├─ textSpeed: Text_Speed
├─ textGear: Text_Gear
├─ textIndicator: Text_Indicator
├─ textCameraFocus: Text_CameraFocus
└─ textPing: Text_Ping
```

---

## **Car Hierarchy (Server)**

```
Car (Rigidbody, mass=1300, interpolate)
├─ Body (Cube mesh, no collider)
├─ CenterOfMass (empty, Y=-0.2)
├─ WheelCollider_FL (-0.8, 0, 1.2)
├─ WheelCollider_FR (0.8, 0, 1.2)
├─ WheelCollider_RL (-0.8, 0, -1.2)
├─ WheelCollider_RR (0.8, 0, -1.2)
├─ WheelMesh_FL (Cylinder, Y=90°)
├─ WheelMesh_FR (Cylinder, Y=90°)
├─ WheelMesh_RL (Cylinder, Y=90°)
├─ WheelMesh_RR (Cylinder, Y=90°)
├─ Anchor_Dashboard (0, 0.8, 1)
├─ Anchor_FL_Wheel (-0.8, 0.2, 1.2)
├─ Anchor_FR_Wheel (0.8, 0.2, 1.2)
├─ Anchor_RL_Wheel (-0.8, 0.2, -1.2)
├─ Anchor_RR_Wheel (0.8, 0.2, -1.2)
├─ Anchor_Engine (0, 0.5, 1.5)
├─ Anchor_Exhaust (0, 0, -2)
├─ Anchor_SteeringLinkage (0, 0.2, 1.5)
├─ Anchor_BrakeCaliperFront (-0.8, 0, 1.2)
└─ Anchor_SuspensionFront (-0.8, 0.3, 1.2)
```

---

## **Camera Focus Points**

| ID | Name | Anchor | Suggested Offset | FOV |
|----|------|--------|------------------|-----|
| 0 | FL_Wheel | Anchor_FL_Wheel | (0, 0.5, -1.5) | 60 |
| 1 | FR_Wheel | Anchor_FR_Wheel | (0, 0.5, -1.5) | 60 |
| 2 | RL_Wheel | Anchor_RL_Wheel | (0, 0.5, -1.5) | 60 |
| 3 | RR_Wheel | Anchor_RR_Wheel | (0, 0.5, -1.5) | 60 |
| 4 | Engine | Anchor_Engine | (0, 1, -2) | 60 |
| 5 | Exhaust | Anchor_Exhaust | (0, 1, -3) | 60 |
| 6 | SteeringLinkage | Anchor_SteeringLinkage | (0, 0.5, -1) | 60 |
| 7 | BrakeCaliperFront | Anchor_BrakeCaliperFront | (0, 0.3, -0.8) | 60 |
| 8 | SuspensionFront | Anchor_SuspensionFront | (0, 0.5, -1) | 60 |
| 9 | Dashboard | Anchor_Dashboard | (0, 0.2, -1) | 70 |

---

## **Controls Reference**

### **Input**

| Control | Type | Range | TCP/UDP | Rate |
|---------|------|-------|---------|------|
| Steer | Drag Area | -1 to 1 | UDP | 30-60 Hz |
| Throttle | Slider | 0 to 1 | UDP | 30-60 Hz |
| Brake | Slider | 0 to 1 | UDP | 30-60 Hz |
| Handbrake | Toggle | 0/1 | UDP | 30-60 Hz |
| Gear | Button | -1,0,1-6 | TCP | On-demand |
| Headlights | Toggle | On/Off | TCP | On-demand |
| Indicator | Button | 0-3 | TCP | On-demand |
| Camera | Dropdown | 0-9 | TCP | On-demand |
| Reset | Button | - | TCP | On-demand |

### **Gear Mapping**

| Button | Gear | Value |
|--------|------|-------|
| R | Reverse | -1 |
| N | Neutral | 0 |
| 1 | 1st | 1 |
| 2 | 2nd | 2 |
| 3 | 3rd | 3 |
| 4 | 4th | 4 |
| 5 | 5th | 5 |
| 6 | 6th | 6 |

### **Indicator Mapping**

| Button | Mode | Value |
|--------|------|-------|
| Off | Off | 0 |
| Left | Left | 1 |
| Right | Right | 2 |
| Hazard | Hazard | 3 |

---

## **Configuration (NetConfig.asset)**

```csharp
token = "demo-token-123456"      // Auth token
tcpPort = 9000                   // TCP control plane
udpPortServer = 9001             // UDP server listen
udpPortClientListen = 9002       // UDP client listen
simTickRate = 50                 // Physics Hz
inputSendRate = 60               // Client input Hz
stateSendRate = 25               // Server state Hz
inputStaleThresholdMs = 200      // Decay timeout
```

---

## **Project Settings**

### **Time**
- Fixed Timestep: **0.02** (50 Hz)

### **Player (Windows)**
- Run In Background: **✓**
- VSync: **Off**
- Target Framerate: 60

### **Player (Android)**
- Scripting Backend: **IL2CPP**
- Architecture: **ARM64**
- Internet Access: **Require**

---

## **Build Commands**

### **Windows Server**
```
File → Build Settings
Platform: PC, Mac & Linux Standalone
Add Scene: Server_CarSim.unity
Build → CarSimServer.exe
```

### **Android Client**
```
File → Build Settings
Platform: Android
Add Scene: Client_RemoteControl.unity
Build → CarSimClient.apk
```

---

## **Run Commands**

### **Server (Windows)**
```bash
# 1. Run server
CarSimServer.exe

# 2. Get IP
ipconfig
# Note: IPv4 Address (e.g., 192.168.1.100)
```

### **Client (Android)**
```bash
# 1. Install APK
adb install CarSimClient.apk

# 2. Launch app
# 3. Enter server IP: 192.168.1.100
# 4. Tap CONNECT
# 5. Drive!
```

---

## **Troubleshooting (Top 5 Issues)**

| Issue | Fix |
|-------|-----|
| **Client can't connect** | 1. Check same Wi-Fi<br>2. Allow TCP 9000, UDP 9001 in firewall<br>3. Verify server IP with `ipconfig` |
| **Car not moving** | 1. Check server console for "Client endpoint learned"<br>2. Verify WheelColliders grounded<br>3. Check Input Seq increasing in debug overlay |
| **High ping (>150ms)** | 1. Use 5GHz Wi-Fi<br>2. Reduce inputSendRate to 30<br>3. Close bandwidth-heavy apps |
| **Camera not switching** | 1. Verify 10 anchors assigned in focusPoints[]<br>2. Check anchors have distinct positions |
| **Build errors** | 1. Set API Compatibility = .NET 4.x<br>2. Ensure IL2CPP build completes |

---

## **File Locations**

```
Assets/
├─ _Shared/Config/NetConfig.cs         → Config SO
├─ _Shared/Net/Protocol.cs             → Message serializers
├─ Server/Scripts/ServerSimulationController.cs  → Physics
├─ Client/Scripts/ClientInputController.cs       → Input
└─ Editor/SceneSetupHelper.cs          → Scene generator

Docs:
├─ README.md                           → Main guide
├─ SCENE_SETUP.md                      → Wiring guide
├─ FILE_TREE.md                        → File reference
├─ IMPLEMENTATION_SUMMARY.md           → Completion report
└─ QUICK_REFERENCE.md                  → This file
```

---

## **Menu Commands**

```
CarSim/
├─ Create NetConfig Asset              → Creates NetConfig.asset
└─ Setup/
   ├─ Create Server Scene              → Generates Server_CarSim.unity
   └─ Create Client Scene              → Generates Client_RemoteControl.unity
```

---

## **Performance Targets**

| Metric | Target | Typical |
|--------|--------|---------|
| Ping | < 150ms | 20-50ms |
| Input Rate | 30-60 Hz | 60 Hz |
| State Rate | 20-30 Hz | 25 Hz |
| GC Alloc | 0 B/frame | 0 B/frame |

---

## **Message Flow Diagram**

```
   CLIENT                    SERVER
     │                         │
     │────── HELLO_C2S ───────►│  (TCP)
     │◄───── WELCOME_S2C ──────│  (TCP)
     │                         │
     ╞═ Connected ═════════════╡
     │                         │
     │────── INPUT_C2S ───────►│  (UDP, 60 Hz)
     │◄────── STATE_S2C ───────│  (UDP, 25 Hz)
     │                         │
     │─── SET_GEAR_C2S ───────►│  (TCP, on button)
     │─ TOGGLE_LIGHTS_C2S ────►│  (TCP, on button)
     │─ SET_INDICATOR_C2S ────►│  (TCP, on button)
     │─ SET_CAMERA_C2S ───────►│  (TCP, on button)
     │─── RESET_CAR_C2S ──────►│  (TCP, on button)
     │                         │
```

---

## **State Machine (Client)**

```
┌───────────────┐
│  DISCONNECTED │
└───────┬───────┘
        │ Tap CONNECT
        │ Send HELLO_C2S
        ▼
┌───────────────┐
│  CONNECTING   │
└───────┬───────┘
        │ Receive WELCOME_S2C
        │ Start UDP
        ▼
┌───────────────┐
│   CONNECTED   │◄─────┐
│  (Drive Mode) │      │
└───────┬───────┘      │
        │              │
        │ Send INPUT   │ Receive STATE
        │ (UDP loop)   │ (UDP loop)
        │              │
        └──────────────┘
```

---

## **Physics Parameters**

### **WheelCollider**
- Mass: 20 kg
- Radius: 0.4 m
- Suspension Distance: 0.2 m
- Spring: 35000 N/m
- Damper: 4500 Ns/m

### **Rigidbody**
- Mass: 1300 kg
- Center of Mass: (0, -0.2, 0) local
- Drag: 0.05
- Angular Drag: 0.5

### **Engine**
- Max RPM: 6000
- Torque Curve: 200 Nm @ idle → 400 Nm @ peak
- Final Drive: 3.5:1

### **Steering**
- Max Angle: ±30°
- Rate Limit: 5°/s

### **Brakes**
- Brake Torque: 3000 Nm
- Handbrake Torque: 5000 Nm
- Front Bias: 60%

---

## **Packet Sizes (Typical)**

| Message | Direction | Size (bytes) |
|---------|-----------|--------------|
| HELLO_C2S | C→S | 7 + 16 + 2 + ~20 = **~45** |
| WELCOME_S2C | S→C | 7 + 4 + 1 + 1 = **13** |
| INPUT_C2S | C→S | 7 + 13 = **20** |
| STATE_S2C | S→C | 7 + 62 = **69** |
| SET_GEAR_C2S | C→S | 7 + 1 = **8** |

### **Bandwidth (Typical)**
- Client → Server: 20 bytes × 60 Hz = **1200 B/s** (~1.2 KB/s)
- Server → Client: 69 bytes × 25 Hz = **1725 B/s** (~1.7 KB/s)
- **Total**: ~3 KB/s (very low!)

---

**Print this page for quick reference during development!**
