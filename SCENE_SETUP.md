# Scene Setup & Inspector Wiring Guide

Complete step-by-step instructions for setting up both Server and Client scenes with all component references.

---

## **SERVER SCENE: Server_CarSim.unity**

### **Scene Hierarchy**

```
Server_CarSim
├── Ground
├── Directional Light
├── Car
│   ├── Body (visual mesh)
│   ├── CenterOfMass
│   ├── WheelCollider_FL
│   ├── WheelCollider_FR
│   ├── WheelCollider_RL
│   ├── WheelCollider_RR
│   ├── WheelMesh_FL
│   ├── WheelMesh_FR
│   ├── WheelMesh_RL
│   ├── WheelMesh_RR
│   ├── Anchor_Dashboard
│   ├── Anchor_FL_Wheel
│   ├── Anchor_FR_Wheel
│   ├── Anchor_RL_Wheel
│   ├── Anchor_RR_Wheel
│   ├── Anchor_Engine
│   ├── Anchor_Exhaust
│   ├── Anchor_SteeringLinkage
│   ├── Anchor_BrakeCaliperFront
│   └── Anchor_SuspensionFront
├── Main Camera
└── ServerSystems
    └── DebugCanvas
```

---

### **1. Ground**

1. Create: GameObject → 3D Object → Plane
2. Rename: "Ground"
3. Transform:
   - Position: (0, 0, 0)
   - Scale: (10, 1, 10)
4. Add Material (optional): Create simple gray material

---

### **2. Car GameObject**

#### **A. Car Root**

1. Create: GameObject (empty)
2. Rename: "Car"
3. Transform Position: (0, 1, 0)
4. Add Component: **Rigidbody**
   - Mass: **1300**
   - Drag: 0.05
   - Angular Drag: 0.5
   - Use Gravity: ✓
   - Is Kinematic: ✗
   - Interpolation: **Interpolate**
   - Collision Detection: Discrete
5. Add Component: **BoxCollider**
   - Center: (0, 0.3, 0)
   - Size: (2, 0.5, 4)

#### **B. Body (Visual)**

1. Create: GameObject → 3D Object → Cube (as child of Car)
2. Rename: "Body"
3. Transform:
   - Position: (0, 0.3, 0) (local)
   - Scale: (2, 0.5, 4) (local)
4. **Remove** BoxCollider component (visual only)
5. Add Material (optional): Car paint material

#### **C. Center of Mass**

1. Create: GameObject (empty, child of Car)
2. Rename: "CenterOfMass"
3. Transform Local Position: (0, **-0.2**, 0)

#### **D. WheelColliders**

Create 4 WheelColliders as children of Car:

**WheelCollider_FL:**
- Position: (-0.8, 0, 1.2)
- Add Component: WheelCollider
  - Mass: 20
  - Radius: 0.4
  - Wheel Damping Rate: 0.25
  - Suspension Distance: 0.2
  - Force App Point Distance: 0
  - Suspension Spring:
    - Spring: **35000**
    - Damper: **4500**
    - Target Position: 0.5
  - Forward Friction / Sideways Friction: Default

**WheelCollider_FR:**
- Position: (0.8, 0, 1.2)
- (Same settings as FL)

**WheelCollider_RL:**
- Position: (-0.8, 0, -1.2)
- (Same settings as FL)

**WheelCollider_RR:**
- Position: (0.8, 0, -1.2)
- (Same settings as FL)

#### **E. WheelMeshes**

Create 4 visual wheels as children of Car:

**WheelMesh_FL:**
- Create: 3D Object → Cylinder
- Position: (-0.8, 0, 1.2) (local)
- Rotation: (0, 0, 90) (local)
- Scale: (0.4, 0.2, 0.4) (local)
- **Remove** CapsuleCollider

**WheelMesh_FR/RL/RR:**
- Same as FL, adjust positions

#### **F. Camera Anchors (10 total)**

Create 10 empty GameObjects as children of Car:

| Name | Local Position | Description |
|------|---------------|-------------|
| Anchor_Dashboard | (0, 0.8, 1) | Interior dashboard view |
| Anchor_FL_Wheel | (-0.8, 0.2, 1.2) | Front left wheel |
| Anchor_FR_Wheel | (0.8, 0.2, 1.2) | Front right wheel |
| Anchor_RL_Wheel | (-0.8, 0.2, -1.2) | Rear left wheel |
| Anchor_RR_Wheel | (0.8, 0.2, -1.2) | Rear right wheel |
| Anchor_Engine | (0, 0.5, 1.5) | Engine bay |
| Anchor_Exhaust | (0, 0, -2) | Exhaust pipe |
| Anchor_SteeringLinkage | (0, 0.2, 1.5) | Steering mechanism |
| Anchor_BrakeCaliperFront | (-0.8, 0, 1.2) | Brake caliper |
| Anchor_SuspensionFront | (-0.8, 0.3, 1.2) | Suspension |

---

### **3. Main Camera**

1. Select existing Main Camera
2. Position: (0, 3, -5)
3. Rotation: (20, 0, 0)
4. Add Component: **CameraFocusManager**

**CameraFocusManager Inspector:**

- **Main Camera**: Drag Main Camera itself
- **Focus Points**: Size = **10**

For each focus point (0-9):

| Index | Part Id | Anchor | Offset | FOV | Lerp Time |
|-------|---------|--------|--------|-----|-----------|
| 0 | FL_Wheel | Anchor_FL_Wheel | (0, 0.5, -1.5) | 60 | 1 |
| 1 | FR_Wheel | Anchor_FR_Wheel | (0, 0.5, -1.5) | 60 | 1 |
| 2 | RL_Wheel | Anchor_RL_Wheel | (0, 0.5, -1.5) | 60 | 1 |
| 3 | RR_Wheel | Anchor_RR_Wheel | (0, 0.5, -1.5) | 60 | 1 |
| 4 | Engine | Anchor_Engine | (0, 1, -2) | 60 | 1 |
| 5 | Exhaust | Anchor_Exhaust | (0, 1, -3) | 60 | 1 |
| 6 | SteeringLinkage | Anchor_SteeringLinkage | (0, 0.5, -1) | 60 | 1 |
| 7 | BrakeCaliperFront | Anchor_BrakeCaliperFront | (0, 0.3, -0.8) | 60 | 1 |
| 8 | SuspensionFront | Anchor_SuspensionFront | (0, 0.5, -1) | 60 | 1 |
| 9 | Dashboard | Anchor_Dashboard | (0, 0.2, -1) | 70 | 1 |

---

### **4. ServerSystems GameObject**

1. Create: GameObject (empty)
2. Rename: "ServerSystems"
3. Add 7 Components:
   - TcpServerPeer
   - UdpServerPeer
   - ServerCommandRouter
   - ServerSimulationController
   - StateBroadcaster
   - DebugOverlay

---

#### **Component A: TcpServerPeer**

- **Config**: Drag `Assets/_Shared/Config/NetConfig.asset`

---

#### **Component B: UdpServerPeer**

- **Config**: Drag `NetConfig.asset`
- **Simulate Drop Percent**: 0 (set to 10 for testing packet loss)

---

#### **Component C: ServerCommandRouter**

- **Config**: Drag `NetConfig.asset`
- **Tcp Peer**: Drag `TcpServerPeer` (same GameObject)
- **Udp Peer**: Drag `UdpServerPeer` (same GameObject)
- **Sim Controller**: Drag `ServerSimulationController` (same GameObject)
- **Camera Focus Manager**: Drag `CameraFocusManager` (Main Camera)

---

#### **Component D: ServerSimulationController**

- **Car Body**: Drag `Car` (Rigidbody)
- **Center Of Mass**: Drag `Car/CenterOfMass`
- **Wheel FL**: Drag `Car/WheelCollider_FL`
- **Wheel FR**: Drag `Car/WheelCollider_FR`
- **Wheel RL**: Drag `Car/WheelCollider_RL`
- **Wheel RR**: Drag `Car/WheelCollider_RR`
- **Wheel Mesh FL**: Drag `Car/WheelMesh_FL`
- **Wheel Mesh FR**: Drag `Car/WheelMesh_FR`
- **Wheel Mesh RL**: Drag `Car/WheelMesh_RL`
- **Wheel Mesh RR**: Drag `Car/WheelMesh_RR`
- **Torque Curve**: Create curve with 2 keys:
  - Key 0: Time=0, Value=200
  - Key 1: Time=6000, Value=400
  - Mode: Linear
- **Gear Ratios**: Size = **8**
  - [0] = -3.5 (Reverse)
  - [1] = 0 (Neutral)
  - [2] = 3.5 (1st gear)
  - [3] = 2.5 (2nd gear)
  - [4] = 1.8 (3rd gear)
  - [5] = 1.3 (4th gear)
  - [6] = 1.0 (5th gear)
  - [7] = 0.8 (6th gear)
- **Final Drive Ratio**: 3.5
- **Max Rpm**: 6000
- **Max Steer Angle**: 30
- **Steer Speed**: 5
- **Brake Torque**: 3000
- **Handbrake Torque**: 5000
- **Brake Bias Front**: 0.6
- **Current Gear**: 1 (start in 1st)
- **Headlights On**: false
- **Indicator Mode**: Off

---

#### **Component E: StateBroadcaster**

- **Config**: Drag `NetConfig.asset`
- **Udp Peer**: Drag `UdpServerPeer`
- **Sim Controller**: Drag `ServerSimulationController`
- **Camera Focus Manager**: Drag `CameraFocusManager` (Main Camera)

---

#### **Component F: DebugOverlay**

First create the UI:

1. Create: UI → Canvas (as child of ServerSystems)
2. Rename: "DebugCanvas"
3. Canvas Component:
   - Render Mode: Screen Space - Overlay
4. Create: UI → Text (as child of DebugCanvas)
5. Rename: "StatusText"
6. RectTransform:
   - Anchor: Top-Left
   - Position: (10, -10)
   - Width: 400, Height: 300
7. Text Component:
   - Font Size: 14
   - Color: White
   - Alignment: Top-Left
   - Text: "Server Debug"

**Then wire DebugOverlay:**

- **Udp Peer**: Drag `UdpServerPeer`
- **Sim Controller**: Drag `ServerSimulationController`
- **Camera Focus Manager**: Drag `CameraFocusManager` (Main Camera)
- **Status Text**: Drag `StatusText` (Text component)

---

## **CLIENT SCENE: Client_RemoteControl.unity**

### **Scene Hierarchy**

```
Client_RemoteControl
├── UI_Root (Canvas)
│   ├── Panel_Connect
│   │   ├── Text_Title
│   │   ├── Input_ServerIP
│   │   ├── Input_Token
│   │   ├── Button_Connect
│   │   └── Text_Status
│   └── Panel_Drive (initially disabled)
│       ├── Area_Steer
│       ├── Slider_Throttle
│       ├── Slider_Brake
│       ├── Toggle_Handbrake
│       ├── GearPanel
│       │   ├── Btn_Gear_R
│       │   ├── Btn_Gear_N
│       │   ├── Btn_Gear_1
│       │   ├── Btn_Gear_2
│       │   ├── Btn_Gear_3
│       │   ├── Btn_Gear_4
│       │   ├── Btn_Gear_5
│       │   └── Btn_Gear_6
│       ├── Toggle_Headlights
│       ├── IndicatorPanel
│       │   ├── Btn_Indicator_Off
│       │   ├── Btn_Indicator_Left
│       │   ├── Btn_Indicator_Right
│       │   └── Btn_Indicator_Hazard
│       ├── Dropdown_CameraFocus
│       ├── Button_ResetCar
│       └── HUDPanel
│           ├── Text_Speed
│           ├── Text_Gear
│           ├── Text_Indicator
│           ├── Text_CameraFocus
│           └── Text_Ping
└── ClientSystems
```

---

### **1. UI_Root Canvas**

1. Create: UI → Canvas
2. Rename: "UI_Root"
3. Canvas:
   - Render Mode: Screen Space - Overlay
   - Pixel Perfect: ✓
4. Canvas Scaler:
   - UI Scale Mode: Scale With Screen Size
   - Reference Resolution: 1920 x 1080
5. Add: Graphic Raycaster

---

### **2. Panel_Connect**

1. Create: UI → Panel (child of UI_Root)
2. Rename: "Panel_Connect"
3. RectTransform:
   - Anchor: Stretch/Stretch (full screen)
   - Left/Top/Right/Bottom: 0
4. Image:
   - Color: (0.1, 0.1, 0.1, 0.95)

**Child: Text_Title**
- Create: UI → Text
- Text: "REMOTE CAR CONTROL"
- Font Size: 48
- Alignment: Center
- Color: White
- RectTransform: Anchor Middle-Center, PosY=200, Width=800, Height=100

**Child: Input_ServerIP**
- Create: UI → Input Field
- Placeholder Text: "192.168.1.100"
- RectTransform: Anchor Middle-Center, PosY=100, Width=500, Height=50

**Child: Input_Token**
- Create: UI → Input Field
- Placeholder Text: "demo-token-123456"
- RectTransform: Anchor Middle-Center, PosY=30, Width=500, Height=50

**Child: Button_Connect**
- Create: UI → Button
- Text: "CONNECT"
- RectTransform: Anchor Middle-Center, PosY=-50, Width=300, Height=60
- Button Color: (0.2, 0.6, 0.2, 1)

**Child: Text_Status**
- Create: UI → Text
- Text: "Enter server IP and connect"
- Font Size: 20
- Alignment: Center
- Color: Yellow
- RectTransform: Anchor Middle-Center, PosY=-150, Width=600, Height=50

---

### **3. Panel_Drive**

1. Create: UI → Panel (child of UI_Root)
2. Rename: "Panel_Drive"
3. **Initially disabled**: Uncheck GameObject active
4. RectTransform: Full screen (Stretch/Stretch)
5. Image: Color (0.05, 0.05, 0.05, 1)

**Layout suggestion:**
```
┌────────────────────────────────────────────────┐
│  [HUD: Speed | Gear | Ping]          Top-Right│
├────────────────────────────────────────────────┤
│                                                │
│         [STEER AREA - Drag Here]               │
│                                                │
├────────────────────────────────────────────────┤
│  [Throttle Slider]                             │
│  [Brake Slider]                                │
│  [Handbrake Toggle]                            │
├────────────────────────────────────────────────┤
│  [R][N][1][2][3][4][5][6]  Gear Buttons        │
├────────────────────────────────────────────────┤
│  [Headlights Toggle]                           │
│  [Off][Left][Right][Hazard]  Indicators        │
│  [Camera Dropdown]                             │
│  [RESET BUTTON]                                │
└────────────────────────────────────────────────┘
```

**Child: Area_Steer**
- Create: UI → Panel
- Rename: "Area_Steer"
- RectTransform: Anchor Top-Center, Width=800, Height=400, PosY=-220
- Image: Color (0.2, 0.2, 0.3, 0.5)
- Add: **EventTrigger** component (handled by ClientInputController)

**Child: Slider_Throttle**
- Create: UI → Slider
- Direction: Bottom to Top
- Value: 0
- RectTransform: Left side, Width=80, Height=300

**Child: Slider_Brake**
- Create: UI → Slider
- Direction: Bottom to Top
- Value: 0
- RectTransform: Right side, Width=80, Height=300

**Child: Toggle_Handbrake**
- Create: UI → Toggle
- Label: "Handbrake"
- RectTransform: Bottom-Center

**Child: GearPanel (empty GameObject with Horizontal Layout Group)**
- Create 8 Buttons: Btn_Gear_R, Btn_Gear_N, Btn_Gear_1..6
- Labels: "R", "N", "1", "2", "3", "4", "5", "6"
- RectTransform: Bottom-Center, Width=640, Height=60

**Child: Toggle_Headlights**
- Create: UI → Toggle
- Label: "Headlights"

**Child: IndicatorPanel**
- Create 4 Buttons: Btn_Indicator_Off/Left/Right/Hazard
- Labels: "OFF", "LEFT", "RIGHT", "HAZARD"

**Child: Dropdown_CameraFocus**
- Create: UI → Dropdown
- Options (10):
  - "FL Wheel"
  - "FR Wheel"
  - "RL Wheel"
  - "RR Wheel"
  - "Engine"
  - "Exhaust"
  - "Steering"
  - "Brake Caliper"
  - "Suspension"
  - "Dashboard"

**Child: Button_ResetCar**
- Create: UI → Button
- Label: "RESET CAR"
- Color: Red

**Child: HUDPanel**
- Create 5 Text elements:
  - Text_Speed: "0 km/h"
  - Text_Gear: "N"
  - Text_Indicator: "Off"
  - Text_CameraFocus: "Dashboard"
  - Text_Ping: "0 ms"
- Position: Top-Right corner
- Font Size: 24, Bold, White

---

### **4. ClientSystems GameObject**

1. Create: GameObject (empty)
2. Rename: "ClientSystems"
3. Add 5 Components:
   - TcpClientPeer
   - UdpClientPeer
   - ClientConnectionUI
   - ClientInputController
   - ClientStateHUD

---

#### **Component A: TcpClientPeer**

- **Config**: Drag `NetConfig.asset`

---

#### **Component B: UdpClientPeer**

- **Config**: Drag `NetConfig.asset`

---

#### **Component C: ClientConnectionUI**

- **Config**: Drag `NetConfig.asset`
- **Tcp Peer**: Drag `TcpClientPeer` (same GameObject)
- **Udp Peer**: Drag `UdpClientPeer` (same GameObject)
- **Panel Connect**: Drag `Panel_Connect`
- **Input Server IP**: Drag `Input_ServerIP`
- **Input Token**: Drag `Input_Token`
- **Button Connect**: Drag `Button_Connect`
- **Text Status**: Drag `Text_Status`
- **Panel Drive**: Drag `Panel_Drive`

---

#### **Component D: ClientInputController**

- **Config**: Drag `NetConfig.asset`
- **Tcp Peer**: Drag `TcpClientPeer`
- **Udp Peer**: Drag `UdpClientPeer`
- **Steer Area**: Drag `Area_Steer` RectTransform
- **Slider Throttle**: Drag `Slider_Throttle`
- **Slider Brake**: Drag `Slider_Brake`
- **Toggle Handbrake**: Drag `Toggle_Handbrake`
- **Gear Buttons (8)**: Drag all 8 gear buttons (R, N, 1-6)
- **Toggle Headlights**: Drag `Toggle_Headlights`
- **Indicator Buttons (4)**: Drag Off/Left/Right/Hazard buttons
- **Dropdown Camera Focus**: Drag `Dropdown_CameraFocus`
- **Btn Reset Car**: Drag `Button_ResetCar`
- **Steer Smooth Speed**: 10
- **Throttle Smooth Speed**: 5
- **Brake Smooth Speed**: 5

---

#### **Component E: ClientStateHUD**

- **Udp Peer**: Drag `UdpClientPeer`
- **Text Speed**: Drag `Text_Speed`
- **Text Gear**: Drag `Text_Gear`
- **Text Indicator**: Drag `Text_Indicator`
- **Text Camera Focus**: Drag `Text_CameraFocus`
- **Text Ping**: Drag `Text_Ping`

---

## **Script Execution Order (Optional)**

To ensure proper initialization order:

1. Edit → Project Settings → Script Execution Order
2. Add:
   - `TcpServerPeer`: -100
   - `UdpServerPeer`: -100
   - `TcpClientPeer`: -100
   - `UdpClientPeer`: -100
   - `ServerCommandRouter`: -50
   - `ClientConnectionUI`: -50
   - `ServerSimulationController`: 0 (default)
   - `StateBroadcaster`: 100
   - `ClientStateHUD`: 100

---

## **Final Checklist**

### **Server Scene**
- ✓ NetConfig.asset created and assigned to all components
- ✓ Car Rigidbody mass=1300, interpolate on
- ✓ 4 WheelColliders configured with suspension
- ✓ 4 WheelMeshes positioned correctly
- ✓ 10 Camera anchors created and assigned to CameraFocusManager
- ✓ All 7 server components on ServerSystems wired
- ✓ DebugCanvas with Text created and assigned

### **Client Scene**
- ✓ NetConfig.asset assigned to all components
- ✓ Panel_Connect with IP/Token inputs, Connect button
- ✓ Panel_Drive with all controls (steer, throttle, brake, handbrake, gears, lights, indicators, camera, reset)
- ✓ HUD with 5 Text elements
- ✓ All 5 client components on ClientSystems wired

### **Project Settings**
- ✓ Fixed Timestep = 0.02
- ✓ Server (Windows): Run In Background = On
- ✓ Client (Android): IL2CPP, ARM64, Internet Access = Require

---

**You're ready to build and test!**
