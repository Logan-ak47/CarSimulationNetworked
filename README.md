# Car Simulation Networked

A Unity networked car simulation system with authoritative server (Windows) and remote mobile client (Android) using raw TCP/UDP sockets.

---

## Overview

This project implements a client-server architecture where:
- **Server (Windows)**: Runs authoritative car physics simulation
- **Client (Android)**: Provides touchscreen controls and displays car state

Communication uses raw TCP/UDP sockets without any third-party networking libraries.

```
┌─────────────────────────────────┐          ┌──────────────────────────────┐
│   SERVER (Windows PC)           │          │   CLIENT (Android Phone)     │
│                                 │          │                              │
│  ┌─────────────────────────┐   │          │  ┌──────────────────────┐   │
│  │ Authoritative Physics   │   │          │  │ Touchscreen Controls │   │
│  │ - WheelColliders        │   │          │  │ - Steer, Throttle    │   │
│  │ - Engine/Gears          │   │          │  │ - Brake, Handbrake   │   │
│  │ - Torque Curves         │   │          │  │ - Gear Selection     │   │
│  └─────────────────────────┘   │          │  └──────────────────────┘   │
│                                 │          │                              │
│  ┌─────────────────────────┐   │          │  ┌──────────────────────┐   │
│  │ Network Layer           │   │          │  │ Connection & HUD     │   │
│  │ TCP :9000 (control)     │◄─┼─TCP─────►│  │ - Connection UI      │   │
│  │ UDP :9001 (state 25Hz)  │◄─┼─UDP─────►│  │ - Speed/Gear/Ping    │   │
│  └─────────────────────────┘   │          │  └──────────────────────┘   │
└─────────────────────────────────┘          └──────────────────────────────┘
```

---

## Features Implemented

### Server (Windows)
- ✅ Authoritative car physics simulation (50 Hz fixed timestep)
- ✅ 4-wheel drive with Unity WheelColliders
- ✅ Engine torque curves and 6-speed gearbox + reverse + neutral
- ✅ Steering, throttle, brake, and handbrake input
- ✅ TCP control channel (port 9000) for reliable commands
- ✅ UDP state broadcast (port 9001, ~25 Hz) for real-time updates
- ✅ UDP input receiving (port 9001) for continuous control input
- ✅ Input latency handling (holds last input for ≤200ms, then decays to neutral)
- ✅ Debug overlay showing connection status, input age, speed, gear
- ✅ Camera focus manager with 10 camera anchors (Dashboard, Wheels, Engine, etc.)
- ✅ Headlights and indicator system (Left/Right/Hazard/Off)
- ✅ Reset car functionality

### Client (Android)
- ✅ Async TCP connection with timeout handling (10s)
- ✅ Connection UI with server IP and token input
- ✅ Touchscreen controls:
  - ✅ Steering drag area
  - ✅ Throttle/Brake sliders
  - ✅ Handbrake toggle
  - ✅ Gear selector buttons (R/N/1-6)
- ✅ UDP input streaming to server (~60 Hz)
- ✅ UDP state receiving from server
- ✅ HUD displaying speed, gear, connection status, ping
- ✅ Comprehensive connection logging with "[CarSimulatorClient]" prefix
- ✅ Network diagnostics for troubleshooting connectivity issues

### Features Coded But Not Fully Wired
- ⚠️ **Camera Focus Dropdown**: Client UI has the dropdown, server has CameraFocusManager with 10 anchors, but the dropdown selection doesn't send messages to server yet
- ⚠️ **Headlights Toggle**: Server handles headlight state, client has toggle button, but toggle doesn't send TCP command yet
- ⚠️ **Indicator Buttons**: Server implements indicator logic (Left/Right/Hazard/Off), client has buttons, but they're not connected to send commands yet
- ⚠️ **Reset Car Button**: Server has reset functionality, client has button, but not wired to send reset command yet

---

## Network Protocol

### Message Types

| Type | Direction | Transport | Purpose |
|------|-----------|-----------|---------|
| `HELLO_C2S` | Client→Server | TCP | Initial handshake with auth token |
| `WELCOME_S2C` | Server→Client | TCP | Connection confirmation with session ID |
| `INPUT_C2S` | Client→Server | UDP | Continuous input (steer/throttle/brake/handbrake) |
| `STATE_S2C` | Server→Client | UDP | Car state (position, rotation, speed, RPM, etc.) |
| `SET_GEAR_C2S` | Client→Server | TCP | Change gear |
| `TOGGLE_HEADLIGHTS_C2S` | Client→Server | TCP | Toggle headlights (coded, not wired) |
| `SET_INDICATOR_C2S` | Client→Server | TCP | Set indicators (coded, not wired) |
| `SET_CAMERA_FOCUS_C2S` | Client→Server | TCP | Change camera focus (coded, not wired) |
| `RESET_CAR_C2S` | Client→Server | TCP | Reset car position (coded, not wired) |
| `PING_C2S` / `PONG_S2C` | Bidirectional | TCP | Connection heartbeat |

### Header Format (7 bytes, little-endian)
```
MsgType     : 1 byte
Sequence    : 2 bytes (ushort)
TimestampMs : 4 bytes (uint)
PayloadLen  : 2 bytes (ushort)
```

---

## Project Structure

```
Assets/
├── _Shared/                       # Shared code between client & server
│   ├── Config/
│   │   └── NetConfig.cs          # Network configuration ScriptableObject
│   └── Net/
│       ├── ByteCodec.cs          # Binary serialization utilities
│       ├── RingBuffer.cs         # Thread-safe queue
│       ├── StopwatchTime.cs      # Monotonic timestamp
│       ├── MessageTypes.cs       # Protocol enums
│       └── Protocol.cs           # Message structs & serializers
│
├── Server/
│   ├── Scenes/
│   │   └── Server_CarSim.unity   # Server scene
│   └── Scripts/
│       ├── TcpServerPeer.cs      # TCP listener and message handler
│       ├── UdpServerPeer.cs      # UDP receiver/sender
│       ├── ServerCommandRouter.cs     # Routes TCP messages to handlers
│       ├── ServerSimulationController.cs  # Car physics simulation
│       ├── CameraFocusManager.cs      # Camera anchor system
│       ├── StateBroadcaster.cs        # Broadcasts car state via UDP
│       └── DebugOverlay.cs            # Server debug UI
│
└── Client/
    ├── Scenes/
    │   └── Client_RemoteControl.unity  # Client scene
    └── Scripts/
        ├── TcpClientPeer.cs       # TCP connection with async timeout
        ├── UdpClientPeer.cs       # UDP sender/receiver
        ├── ClientConnectionUI.cs  # Connection UI and HELLO/WELCOME flow
        ├── ClientInputController.cs   # Touch input handling
        └── ClientStateHUD.cs          # HUD display
```

---

## Setup & Build

### Prerequisites
- Unity 2021.3 or newer
- Windows PC for server
- Android device for client
- Both on the same WiFi network

### Configuration
1. In Unity, create NetConfig asset: Right-click in Project → Create → CarSim → NetConfig
2. Configure network settings:
   - Token: `demo-token-123456`
   - TCP Port: `9000`
   - UDP Port Server: `9001`
   - UDP Port Client: `9002`
   - Sim Tick Rate: `50` Hz
   - Input Send Rate: `60` Hz
   - State Send Rate: `25` Hz

### Server Build (Windows)
1. Open `Assets/Server/Scenes/Server_CarSim.unity`
2. File → Build Settings
3. Platform: PC, Mac & Linux Standalone
4. Build and save as `CarSimServer.exe`

### Client Build (Android)
1. Open `Assets/Client/Scenes/Client_RemoteControl.unity`
2. File → Build Settings
3. Platform: Android
4. Player Settings → Other Settings:
   - Scripting Backend: IL2CPP
   - Target Architectures: ARM64 ✓
   - Internet Access: **Require**
5. Build and save as `CarSimClient.apk`
6. Install on Android device

---

## Running the System

### Step 1: Start Server
1. Run `CarSimServer.exe` on Windows PC
2. Server starts listening on TCP:9000 and UDP:9001
3. Console shows: `[TcpServer] Listening on port 9000`

### Step 2: Configure Firewall
If connection fails, allow ports through Windows Firewall:
- Windows Defender Firewall → Advanced Settings
- Inbound Rules → New Rule → Port
- TCP port 9000, UDP port 9001
- Allow the connection

### Step 3: Find Server IP
On Windows PC, open Command Prompt:
```cmd
ipconfig
```
Note the IPv4 Address of your WiFi adapter (e.g., `192.168.0.100`)

### Step 4: Connect Client
1. Ensure Android device is on the **same WiFi network**
2. Launch CarSimClient app
3. Enter server IP (e.g., `192.168.0.100`)
4. Token is pre-filled with default value
5. Tap **CONNECT**
6. Wait for status to show "Connected! Session: XXXX"
7. UI switches to drive panel

### Step 5: Drive!
- **Steer**: Drag left/right on the steer area
- **Throttle**: Slide throttle slider up
- **Brake**: Slide brake slider up
- **Handbrake**: Toggle handbrake button
- **Gear**: Tap R/N/1/2/3/4/5/6 buttons to change gears

---

## Connection Flow

The client establishes connection using an async TCP connection with timeout:

1. **User taps CONNECT**
   - Client validates server IP
   - Initiates async TCP connection (10-second timeout)
   - Status: "Connecting..."

2. **TCP Connection Established**
   - OnConnected event fires
   - Client sends HELLO message with token
   - Client starts UDP listener
   - Status: "Waiting for WELCOME..."

3. **Server Receives HELLO**
   - Validates authentication token
   - Sends WELCOME message with session ID
   - Learns client UDP endpoint

4. **Client Receives WELCOME**
   - Switches to drive panel
   - Starts sending input via UDP
   - Status: "Connected! Session: XXXX"

5. **Ongoing Communication**
   - Client sends INPUT_C2S via UDP (~60 Hz)
   - Server sends STATE_S2C via UDP (~25 Hz)
   - TCP PING/PONG heartbeat every 3 seconds

---

## Logging & Debugging

All client logs are prefixed with `[CarSimulatorClient]` for easy filtering when debugging Android builds via logcat:

```bash
adb logcat | grep CarSimulatorClient
```

Key logs to watch:
- `========== CONNECT BUTTON CLICKED ==========` - Connection initiated
- `TCP CONNECTION ESTABLISHED` - TCP socket connected
- `HELLO message sent via TCP` - Handshake sent
- `========== WELCOME RECEIVED FROM SERVER ==========` - Connection successful
- `CONNECTION FAILED` - Connection error with details
- Network diagnostics show phone IP, network interfaces, connectivity tests

---

## Controls Reference

### Working Controls
| Control | Type | Action |
|---------|------|--------|
| Steer Area | Touch Drag | Drag left/right to steer |
| Throttle Slider | Slider | Slide up to accelerate |
| Brake Slider | Slider | Slide up to brake |
| Handbrake Toggle | Toggle | Tap to engage/release handbrake |
| Gear Buttons | Buttons | Tap R/N/1-6 to shift gears |

### UI Elements (Not Yet Wired)
| Control | Status | Notes |
|---------|--------|-------|
| Camera Dropdown | UI Present, Backend Ready | Dropdown exists, server has CameraFocusManager, needs TCP command wiring |
| Headlights Toggle | UI Present, Backend Ready | Toggle exists, server handles state, needs TCP command wiring |
| Indicator Buttons | UI Present, Backend Ready | Buttons exist (Off/L/R/H), server handles logic, needs TCP command wiring |
| Reset Button | UI Present, Backend Ready | Button exists, server has reset, needs TCP command wiring |

---

## Technical Highlights

### Async Connection with Timeout
- Connection happens in background thread to avoid blocking UI
- 10-second timeout prevents indefinite hangs
- Proper error callbacks to UI with meaningful messages
- Clean thread management with proper join on disconnect

### Zero-Allocation Networking
- Pre-allocated byte buffers for serialization
- Ring buffers use fixed-size arrays
- No LINQ or boxing in hot paths
- Thread-safe queues for inter-thread communication

### Input Latency Handling
Server holds last valid input for 200ms, then gradually decays to neutral to prevent runaway cars when connection drops temporarily.

### Camera System
10 camera anchors strategically placed:
- Dashboard (default view)
- 4 Wheels (FL, FR, RL, RR)
- Engine
- Exhaust
- Steering Linkage
- Front Brake Caliper
- Front Suspension

---

## Known Limitations

1. **Single Client**: Server accepts only one TCP connection at a time
2. **LAN Only**: No NAT traversal, both devices must be on same network
3. **No Encryption**: Token sent in plaintext (use TLS for production)
4. **No Client-Side Prediction**: Some input lag may be noticeable on high-latency networks
5. **Incomplete UI Wiring**: Camera dropdown, headlights, indicators, reset button need final TCP command wiring

---

## Troubleshooting

### "Connection timed out" Error
- ✓ Check both devices are on same WiFi network
- ✓ Verify server is running and showing "Listening on port 9000"
- ✓ Confirm server IP with `ipconfig` matches what you entered
- ✓ Disable Windows Firewall temporarily to test
- ✓ Check router isn't blocking device-to-device communication

### "Connection FAILED" with Socket Error
- ✓ Check Android logs for specific SocketException details
- ✓ Verify phone has network connectivity (check WiFi icon)
- ✓ Ensure Android app has INTERNET permission
- ✓ Try pinging server from another device to verify it's reachable

### Car Doesn't Move
- ✓ Check server debug overlay shows "Input Seq" incrementing
- ✓ Verify UDP packets are arriving (server logs show input age < 200ms)
- ✓ Check WheelColliders have ground contact (adjust suspension in Inspector)
- ✓ Ensure gear is not in Neutral (tap gear button 1-6)

### High Ping (>100ms on LAN)
- ✓ Check WiFi signal strength on phone
- ✓ Close background apps using network bandwidth
- ✓ Reduce `inputSendRate` and `stateSendRate` in NetConfig
- ✓ Use 5GHz WiFi band instead of 2.4GHz if available

---

## Future Enhancements

### Easy Additions
- Wire the remaining 4 buttons (camera dropdown, headlights, indicators, reset) - just need to hook up button click events to send TCP messages
- Add visual feedback for headlights and indicators on server car model
- Implement client-side prediction for smoother steering

### Advanced Features
- Multi-client support (multiple phones controlling separate cars)
- Client-side interpolation between server states
- WebGL client version for browser-based control
- Replay system recording input and state streams

---

## Credits

- **Networking**: Raw TCP/UDP sockets (System.Net.Sockets)
- **Physics**: Unity WheelColliders
- **UI**: Unity UI (uGUI)
- **Build System**: Unity 2021.3 LTS

---

## License

This project is provided as-is for educational purposes. Feel free to modify and extend.

---

**Enjoy driving your networked car!** 🚗📱
