# Car Simulation Networked - Documentation Index

**Complete Unity networked car simulation with authoritative server (Windows) and remote client (Android).**

---

## **📚 Documentation Files**

### **🚀 Getting Started**

1. **[README.md](README.md)** - **START HERE**
   - Complete project overview
   - Feature list (server + client)
   - Network protocol specification
   - Setup instructions (step-by-step)
   - Build instructions (Windows + Android)
   - Running the system
   - Controls reference
   - Testing matrix
   - Troubleshooting guide
   - **Read time**: 30 minutes
   - **Essential**: ⭐⭐⭐⭐⭐

2. **[SCENE_SETUP.md](SCENE_SETUP.md)** - **Setup Guide**
   - Detailed scene hierarchy (server + client)
   - Component-by-component wiring instructions
   - Inspector field values
   - Camera anchor configuration
   - UI layout specifications
   - Script execution order
   - **Read time**: 45 minutes
   - **Essential**: ⭐⭐⭐⭐⭐

---

### **📖 Reference Documentation**

3. **[FILE_TREE.md](FILE_TREE.md)** - **File Structure**
   - Complete file tree visualization
   - Key files summary
   - Network ports table
   - Message flow diagrams
   - Build outputs
   - Quick start checklist
   - Common issues & fixes
   - **Read time**: 15 minutes
   - **Essential**: ⭐⭐⭐⭐

4. **[QUICK_REFERENCE.md](QUICK_REFERENCE.md)** - **Cheat Sheet**
   - One-page quick reference
   - Protocol header format
   - Component wiring summary
   - Controls table
   - Configuration values
   - Troubleshooting (top 5)
   - Message flow diagram
   - **Read time**: 5 minutes
   - **Essential**: ⭐⭐⭐⭐⭐ (keep open while working)

5. **[IMPLEMENTATION_SUMMARY.md](IMPLEMENTATION_SUMMARY.md)** - **Completion Report**
   - Requirements coverage verification
   - Code statistics
   - Architecture highlights
   - Performance metrics
   - Extensibility guide
   - **Read time**: 20 minutes
   - **Essential**: ⭐⭐⭐

---

## **🗂️ Code Files (18 scripts)**

### **Shared Layer** (`Assets/_Shared/`)

#### **Config**
- **`NetConfig.cs`** - ScriptableObject configuration (ports, rates, token)

#### **Network Core**
- **`ByteCodec.cs`** - Little-endian serialization primitives
- **`RingBuffer.cs`** - Thread-safe single-producer/consumer queue
- **`StopwatchTime.cs`** - Monotonic timestamp (uint milliseconds)
- **`MessageTypes.cs`** - Enums (MsgType, CameraPartId, IndicatorMode, LightFlags)
- **`Protocol.cs`** - Message structs + serializers/deserializers

---

### **Server Scripts** (`Assets/Server/Scripts/`)

- **`TcpServerPeer.cs`** - TCP listener (port 9000), accept/recv/send threads
- **`UdpServerPeer.cs`** - UDP receiver (INPUT_C2S) + sender (STATE_S2C)
- **`ServerCommandRouter.cs`** - Routes TCP messages to handlers
- **`ServerSimulationController.cs`** - Authoritative car physics (WheelColliders, engine, gears)
- **`CameraFocusManager.cs`** - 10 camera anchors with smooth transitions
- **`StateBroadcaster.cs`** - STATE_S2C broadcast loop (20-30 Hz)
- **`DebugOverlay.cs`** - Server debug UI (ping, speed, gear, focus)

---

### **Client Scripts** (`Assets/Client/Scripts/`)

- **`TcpClientPeer.cs`** - TCP connector, recv/send threads, event callbacks
- **`UdpClientPeer.cs`** - UDP sender (INPUT_C2S @ 30-60 Hz) + receiver (STATE_S2C)
- **`ClientConnectionUI.cs`** - Connection flow (IP/token → HELLO → WELCOME → drive)
- **`ClientInputController.cs`** - Touchscreen input handling (steer, throttle, brake, gears, lights, camera)
- **`ClientStateHUD.cs`** - HUD display (speed, gear, indicator, camera focus, ping)

---

### **Editor Helpers** (`Assets/Editor/`)

- **`SceneSetupHelper.cs`** - Auto-generates server/client scenes via menu

---

## **📋 Reading Order (Recommended)**

### **For First-Time Setup**
1. [README.md](README.md) - Understand the system
2. [SCENE_SETUP.md](SCENE_SETUP.md) - Follow wiring instructions
3. [QUICK_REFERENCE.md](QUICK_REFERENCE.md) - Keep open during work
4. Build & test!

### **For Understanding the Code**
1. [FILE_TREE.md](FILE_TREE.md) - See file structure
2. Read `_Shared/Net/` scripts (networking primitives)
3. Read `Server/Scripts/` (server-side logic)
4. Read `Client/Scripts/` (client-side logic)

### **For Extending the System**
1. [IMPLEMENTATION_SUMMARY.md](IMPLEMENTATION_SUMMARY.md) - Architecture overview
2. [README.md](README.md) - "Extending the System" section
3. Modify relevant scripts

### **For Troubleshooting**
1. [QUICK_REFERENCE.md](QUICK_REFERENCE.md) - Top 5 issues
2. [README.md](README.md) - "Troubleshooting" section
3. [FILE_TREE.md](FILE_TREE.md) - Common issues table

---

## **🎯 Quick Access**

### **Network Protocol**
- [README.md](README.md#network-protocol) - Full specification
- [QUICK_REFERENCE.md](QUICK_REFERENCE.md#network-protocol) - Header format
- `_Shared/Net/Protocol.cs` - Implementation

### **Controls**
- [README.md](README.md#controls-reference) - Full controls table
- [QUICK_REFERENCE.md](QUICK_REFERENCE.md#controls-reference) - Quick table
- `Client/Scripts/ClientInputController.cs` - Implementation

### **Camera Focus**
- [README.md](README.md#camera-part-ids) - Camera parts table
- [QUICK_REFERENCE.md](QUICK_REFERENCE.md#camera-focus-points) - Anchor positions
- `Server/Scripts/CameraFocusManager.cs` - Implementation

### **Setup Instructions**
- [README.md](README.md#setup-instructions) - High-level steps
- [SCENE_SETUP.md](SCENE_SETUP.md) - Detailed wiring
- [FILE_TREE.md](FILE_TREE.md#quick-start-checklist) - Checklist

### **Build Instructions**
- [README.md](README.md#build-instructions) - Windows + Android
- [QUICK_REFERENCE.md](QUICK_REFERENCE.md#build-commands) - Quick commands

### **Troubleshooting**
- [README.md](README.md#troubleshooting) - Full guide
- [QUICK_REFERENCE.md](QUICK_REFERENCE.md#troubleshooting-top-5-issues) - Top 5
- [FILE_TREE.md](FILE_TREE.md#common-issues--fixes) - Issue table

---

## **📊 Documentation Statistics**

| File | Words | Lines | Read Time |
|------|-------|-------|-----------|
| README.md | ~6500 | ~900 | 30 min |
| SCENE_SETUP.md | ~4500 | ~700 | 45 min |
| FILE_TREE.md | ~2000 | ~400 | 15 min |
| QUICK_REFERENCE.md | ~1800 | ~500 | 5 min |
| IMPLEMENTATION_SUMMARY.md | ~3200 | ~500 | 20 min |
| **Total** | **~18,000** | **~3000** | **115 min** |

---

## **🔍 Search Guide**

Looking for something specific? Use this guide:

| Topic | File | Section |
|-------|------|---------|
| **How to connect client to server** | README.md | "Running the System" |
| **How to wire Inspector fields** | SCENE_SETUP.md | "Component Wiring" |
| **TCP message format** | QUICK_REFERENCE.md | "Network Protocol" |
| **Camera anchor positions** | SCENE_SETUP.md | "Camera Anchors" |
| **Input controls mapping** | QUICK_REFERENCE.md | "Controls Reference" |
| **Build settings** | README.md | "Build Instructions" |
| **Firewall issues** | QUICK_REFERENCE.md | "Troubleshooting" |
| **Code architecture** | IMPLEMENTATION_SUMMARY.md | "Architecture Highlights" |
| **Performance metrics** | IMPLEMENTATION_SUMMARY.md | "Performance Metrics" |
| **Extending controls** | IMPLEMENTATION_SUMMARY.md | "Extensibility" |
| **File structure** | FILE_TREE.md | (entire file) |
| **Project checklist** | FILE_TREE.md | "Quick Start Checklist" |

---

## **💡 Tips**

### **For New Users**
- Start with [README.md](README.md) (don't skip!)
- Use [SCENE_SETUP.md](SCENE_SETUP.md) as a checklist while setting up
- Keep [QUICK_REFERENCE.md](QUICK_REFERENCE.md) open in a browser tab

### **For Experienced Developers**
- Skim [IMPLEMENTATION_SUMMARY.md](IMPLEMENTATION_SUMMARY.md) for architecture
- Use [FILE_TREE.md](FILE_TREE.md) to navigate code
- Extend from "Extensibility" sections in docs

### **For Debugging**
1. Check [QUICK_REFERENCE.md](QUICK_REFERENCE.md) troubleshooting table
2. Read [README.md](README.md) troubleshooting section
3. Enable "Simulate Drop Percent" in UdpServerPeer to test resilience

---

## **🛠️ Menu Commands**

All accessible via Unity Editor menu bar:

```
CarSim/
├─ Create NetConfig Asset
│  └─ Creates: Assets/_Shared/Config/NetConfig.asset
│
└─ Setup/
   ├─ Create Server Scene
   │  └─ Creates: Assets/Server/Scenes/Server_CarSim.unity
   │
   └─ Create Client Scene
      └─ Creates: Assets/Client/Scenes/Client_RemoteControl.unity
```

---

## **📦 Package Contents**

```
CarSimulationNetworked/
├─ README.md                           ← Start here
├─ SCENE_SETUP.md                      ← Wiring guide
├─ FILE_TREE.md                        ← File reference
├─ QUICK_REFERENCE.md                  ← Cheat sheet
├─ IMPLEMENTATION_SUMMARY.md           ← Architecture
├─ INDEX.md                            ← This file
│
└─ Assets/
   ├─ _Shared/                         ← Shared code (6 files)
   ├─ Server/                          ← Server code (7 files)
   ├─ Client/                          ← Client code (5 files)
   └─ Editor/                          ← Editor helpers (1 file)
```

---

## **🎓 Learning Path**

### **Beginner** (Never used Unity networking)
1. Read [README.md](README.md) sections:
   - System Overview
   - Features
   - Network Protocol
2. Follow [SCENE_SETUP.md](SCENE_SETUP.md) step-by-step
3. Build & run (don't modify code yet)
4. Experiment with controls
5. Read code comments in `Protocol.cs` and `ByteCodec.cs`

### **Intermediate** (Familiar with Unity, new to raw sockets)
1. Skim [README.md](README.md)
2. Read [IMPLEMENTATION_SUMMARY.md](IMPLEMENTATION_SUMMARY.md) - "Architecture"
3. Study `TcpServerPeer.cs` and `TcpClientPeer.cs` (threading model)
4. Study `Protocol.cs` (serialization)
5. Build & test
6. Add new control (e.g., horn button)

### **Advanced** (Experienced networked game developer)
1. Read [IMPLEMENTATION_SUMMARY.md](IMPLEMENTATION_SUMMARY.md)
2. Review `ServerSimulationController.cs` (physics authority)
3. Review `ClientInputController.cs` (input smoothing)
4. Build & profile with Unity Profiler
5. Extend system (e.g., add client-side prediction)

---

## **🚀 Quick Start (3 Steps)**

### **1. Setup**
```
Unity → CarSim → Create NetConfig Asset
Unity → CarSim → Setup → Create Server Scene
Unity → CarSim → Setup → Create Client Scene
Follow SCENE_SETUP.md for Inspector wiring
```

### **2. Build**
```
File → Build Settings → Windows Standalone → Server_CarSim.unity → Build
File → Build Settings → Android → Client_RemoteControl.unity → Build
```

### **3. Run**
```
1. Run CarSimServer.exe on Windows PC
2. ipconfig → Note Wi-Fi IP (e.g., 192.168.1.100)
3. Install CarSimClient.apk on Android (same Wi-Fi)
4. Enter server IP → Connect → Drive!
```

---

## **📞 Support**

If you encounter issues:

1. **Check troubleshooting tables**:
   - [QUICK_REFERENCE.md](QUICK_REFERENCE.md#troubleshooting-top-5-issues)
   - [README.md](README.md#troubleshooting)
   - [FILE_TREE.md](FILE_TREE.md#common-issues--fixes)

2. **Verify setup**:
   - [SCENE_SETUP.md](SCENE_SETUP.md) - "Final Checklist"
   - [FILE_TREE.md](FILE_TREE.md#quick-start-checklist)

3. **Review architecture**:
   - [IMPLEMENTATION_SUMMARY.md](IMPLEMENTATION_SUMMARY.md)

---

## **📝 Version**

- **Project**: Car Simulation Networked
- **Version**: 1.0.0
- **Unity**: 2020.3 LTS or later
- **Platforms**: Windows (Server) + Android (Client)
- **Network**: TCP (port 9000) + UDP (ports 9001/9002)
- **Scripting**: C# with .NET 4.x, System.Net.Sockets

---

## **✅ Documentation Checklist**

- ✓ Complete system overview
- ✓ Step-by-step setup instructions
- ✓ Component wiring guide
- ✓ Network protocol specification
- ✓ Build instructions (Windows + Android)
- ✓ Running instructions
- ✓ Controls reference
- ✓ Testing matrix
- ✓ Troubleshooting guide
- ✓ Architecture overview
- ✓ Performance metrics
- ✓ Extensibility guide
- ✓ Quick reference cheat sheet
- ✓ File structure visualization
- ✓ Code comments (inline)

**Total documentation**: 18,000 words across 5 files

---

**Happy coding! 🚗💨**

*For questions or issues, refer to the troubleshooting sections in README.md and QUICK_REFERENCE.md*
