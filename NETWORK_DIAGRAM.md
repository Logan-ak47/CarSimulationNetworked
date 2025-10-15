# Network Architecture Diagram

Visual representation of the complete network communication system.

---

## **System Architecture**

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                           Wi-Fi Network (192.168.1.x)                        │
└──────────────────────────────────────────────────────────────────────────────┘
                                      │
                ┌─────────────────────┴─────────────────────┐
                │                                           │
                │                                           │
┌───────────────▼──────────────┐               ┌───────────▼──────────────────┐
│      WINDOWS SERVER          │               │      ANDROID CLIENT          │
│      192.168.1.100           │               │      192.168.1.50            │
├──────────────────────────────┤               ├──────────────────────────────┤
│                              │               │                              │
│  ┌────────────────────────┐  │               │  ┌────────────────────────┐  │
│  │ TcpServerPeer          │  │               │  │ TcpClientPeer          │  │
│  │ Listen: 0.0.0.0:9000   │◄─┼───── TCP ─────┼─►│ Connect to :9000       │  │
│  │ Accept 1 client        │  │               │  │ Send HELLO             │  │
│  │ Recv: HELLO, cmds      │  │               │  │ Recv: WELCOME, notices │  │
│  │ Send: WELCOME, notices │  │               │  │                        │  │
│  └────────────────────────┘  │               │  └────────────────────────┘  │
│                              │               │                              │
│  ┌────────────────────────┐  │               │  ┌────────────────────────┐  │
│  │ UdpServerPeer          │  │               │  │ UdpClientPeer          │  │
│  │ Listen: 0.0.0.0:9001   │◄─┼───── UDP ─────┼──│ Send to :9001          │  │
│  │ Recv: INPUT_C2S        │  │               │  │ @ 30-60 Hz             │  │
│  │ @ any rate             │  │               │  │                        │  │
│  │                        │  │               │  │ Listen: 0.0.0.0:9002   │  │
│  │ Send: STATE_S2C        │──┼───── UDP ─────┼─►│ Recv: STATE_S2C        │  │
│  │ @ 20-30 Hz             │  │               │  │ @ 20-30 Hz             │  │
│  └────────────────────────┘  │               │  └────────────────────────┘  │
│                              │               │                              │
│  ┌────────────────────────┐  │               │  ┌────────────────────────┐  │
│  │ ServerSimulationCtrl   │  │               │  │ ClientInputController  │  │
│  │ - WheelColliders       │  │               │  │ - Steer drag area      │  │
│  │ - Engine/Gears         │  │               │  │ - Throttle/Brake slider│  │
│  │ - Physics @ 50 Hz      │  │               │  │ - Handbrake toggle     │  │
│  │ - Input decay (200ms)  │  │               │  │ - Gear buttons         │  │
│  └────────────────────────┘  │               │  │ - Lights/Indicators    │  │
│                              │               │  │ - Camera dropdown      │  │
│  ┌────────────────────────┐  │               │  │ - Reset button         │  │
│  │ CameraFocusManager     │  │               │  └────────────────────────┘  │
│  │ - 10 camera anchors    │  │               │                              │
│  │ - Smooth transitions   │  │               │  ┌────────────────────────┐  │
│  │ - FOV blending         │  │               │  │ ClientStateHUD         │  │
│  └────────────────────────┘  │               │  │ - Speed (km/h)         │  │
│                              │               │  │ - Gear (R/N/1-6)       │  │
│  ┌────────────────────────┐  │               │  │ - Indicator mode       │  │
│  │ DebugOverlay           │  │               │  │ - Camera focus         │  │
│  │ - Ping                 │  │               │  │ - Ping (ms)            │  │
│  │ - Input seq/age        │  │               │  └────────────────────────┘  │
│  │ - Speed/Gear/RPM       │  │               │                              │
│  └────────────────────────┘  │               │                              │
│                              │               │                              │
└──────────────────────────────┘               └──────────────────────────────┘
```

---

## **Message Flow Timeline**

```
TIME  CLIENT                           SERVER
────────────────────────────────────────────────────────────────────────
  0   Launch app
      Enter IP: 192.168.1.100
      Tap CONNECT
      │
  1   ├─────── TCP SYN ──────────────►│
  2   │◄──── TCP SYN-ACK ─────────────┤
  3   ├─────── TCP ACK ──────────────►│
      │                                │
      │                                │ TCP connection established
      │                                │
  4   ├──── HELLO_C2S (TCP) ─────────►│
      │  token="demo-token-123456"     │
      │  udpPort=9002                  │
      │  name="Android Device"         │
      │                                │
  5   │                                ├─ Validate token
      │                                ├─ Set UDP endpoint
      │                                │
  6   │◄──── WELCOME_S2C (TCP) ────────┤
      │  sessionId=1234                │
      │  tickRate=50                   │
      │  carId=1                       │
      │                                │
  7   Show "Connected!" status         │
      Show Panel_Drive                 │
      Start UDP sender loop            │
      │                                │
      │                                │
  8   ├─── INPUT_C2S (UDP) ──────────►│
      │  steer=0.0                     ├─ Store latest input
      │  throttle=0.0                  │
      │  brake=0.0                     │
      │  handbrake=0                   │
      │                                │
  9   │                                │
 10   │                                ├─ FixedUpdate (50 Hz)
      │                                ├─ Apply input to physics
      │                                │
 11   │                                ├─ StateBroadcaster tick
      │                                │
 12   │◄──── STATE_S2C (UDP) ──────────┤
      │  pos=(0,1,0)                   │
      │  rot=(0,0,0,1)                 │
      │  speed=0.0                     │
      │  rpm=800.0                     │
      │  gear=1                        │
      │  ... (wheel slips, lights)     │
      │                                │
 13   Update HUD: Speed=0 km/h         │
      Update HUD: Gear=1               │
      Update HUD: Ping=5ms             │
      │                                │
 14   User drags steer area right      │
      steer → 0.5                      │
      │                                │
 15   ├─── INPUT_C2S (UDP) ──────────►│
      │  steer=0.5                     ├─ Apply steer
      │  throttle=0.0                  │
      │                                │
 16   User slides throttle to 0.8      │
      throttle → 0.8                   │
      │                                │
 17   ├─── INPUT_C2S (UDP) ──────────►│
      │  steer=0.5                     ├─ Apply throttle
      │  throttle=0.8                  ├─ Car accelerates
      │                                │
      │                                │
 18   │◄──── STATE_S2C (UDP) ──────────┤
      │  speed=15.3                    │
      │  rpm=2500                      │
      │  steerAngle=15.0               │
      │                                │
 19   Update HUD: Speed=15 km/h        │
      │                                │
 20   User taps Gear "2" button        │
      │                                │
 21   ├─── SET_GEAR_C2S (TCP) ───────►│
      │  gear=2                        ├─ Shift to 2nd gear
      │                                │
 22   │◄──── STATE_S2C (UDP) ──────────┤
      │  gear=2                        │
      │  rpm=1800                      │
      │                                │
 23   Update HUD: Gear=2               │
      │                                │
 24   User selects "Engine" in         │
      camera dropdown                  │
      │                                │
 25   ├─ SET_CAMERA_FOCUS_C2S (TCP) ─►│
      │  partId=4 (Engine)             ├─ CameraFocusManager.SetFocus(4)
      │                                ├─ Begin lerp to Engine anchor
      │                                │
 26   │◄──── STATE_S2C (UDP) ──────────┤
      │  cameraPart=4 (Engine)         │
      │                                │
 27   Update HUD: Focus=Engine         │
      │                                │
      │                                │
...  │◄───── UDP loop continues ───────┤
      │  INPUT @ 60 Hz                 │
      │  STATE @ 25 Hz                 │
```

---

## **Thread Model**

### **Server (TcpServerPeer + UdpServerPeer)**

```
┌─────────────────────────────────────────────────────────────┐
│                      SERVER PROCESS                         │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌──────────────┐     ┌─────────────┐     ┌──────────────┐│
│  │ AcceptThread │     │  RecvThread │     │  SendThread  ││
│  │              │────►│             │     │              ││
│  │ TcpListener  │     │ Read TCP    │     │ Write TCP    ││
│  │ .Accept()    │     │ Parse msg   │     │ Send queue   ││
│  │              │     │ → RingBuffer│     │ ← RingBuffer ││
│  └──────────────┘     └─────────────┘     └──────────────┘│
│                                                             │
│  ┌──────────────┐                                          │
│  │ UdpRecvThread│                                          │
│  │              │                                          │
│  │ UdpClient    │                                          │
│  │ .Receive()   │                                          │
│  │ → Store input│                                          │
│  └──────────────┘                                          │
│                                                             │
│  ────────────────────────────────────────────────────────  │
│                                                             │
│  ┌──────────────────────────────────────────────────────┐ │
│  │               UNITY MAIN THREAD                      │ │
│  ├──────────────────────────────────────────────────────┤ │
│  │                                                      │ │
│  │  Update():                                           │ │
│  │    - ServerCommandRouter.Update()                   │ │
│  │      - Dequeue TCP messages from RingBuffer         │ │
│  │      - Dispatch to handlers                         │ │
│  │    - StateBroadcaster.Update()                      │ │
│  │      - Timer tick → send STATE_S2C via UDP          │ │
│  │                                                      │ │
│  │  FixedUpdate() @ 50 Hz:                             │ │
│  │    - ServerSimulationController.FixedUpdate()       │ │
│  │      - Read latest input (atomic)                   │ │
│  │      - Apply to WheelColliders                      │ │
│  │      - Step physics                                 │ │
│  │      - Compute state (speed, rpm, slip)             │ │
│  │                                                      │ │
│  │    - CameraFocusManager.Update()                    │ │
│  │      - Lerp camera to target anchor                 │ │
│  │                                                      │ │
│  └──────────────────────────────────────────────────────┘ │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### **Client (TcpClientPeer + UdpClientPeer)**

```
┌─────────────────────────────────────────────────────────────┐
│                      CLIENT PROCESS                         │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌──────────────┐     ┌─────────────┐                      │
│  │  RecvThread  │     │  SendThread │                      │
│  │              │     │             │                      │
│  │ Read TCP     │     │ Write TCP   │                      │
│  │ Parse msg    │     │ Send queue  │                      │
│  │ → Callback   │     │ ← RingBuffer│                      │
│  └──────────────┘     └─────────────┘                      │
│                                                             │
│  ┌──────────────┐                                          │
│  │ UdpRecvThread│                                          │
│  │              │                                          │
│  │ UdpClient    │                                          │
│  │ .Receive()   │                                          │
│  │ → Store state│                                          │
│  └──────────────┘                                          │
│                                                             │
│  ────────────────────────────────────────────────────────  │
│                                                             │
│  ┌──────────────────────────────────────────────────────┐ │
│  │               UNITY MAIN THREAD                      │ │
│  ├──────────────────────────────────────────────────────┤ │
│  │                                                      │ │
│  │  Update():                                           │ │
│  │    - ClientInputController.Update()                 │ │
│  │      - Read UI (steer, throttle, brake)             │ │
│  │      - Smooth input                                 │ │
│  │      - Timer tick → send INPUT_C2S via UDP          │ │
│  │      - Handle button clicks → send TCP commands     │ │
│  │                                                      │ │
│  │    - ClientStateHUD.Update()                        │ │
│  │      - Read latest state (atomic)                   │ │
│  │      - Update HUD text elements                     │ │
│  │                                                      │ │
│  └──────────────────────────────────────────────────────┘ │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## **Data Flow: Steer Input → Car Turns**

```
┌─────────────────────────────────────────────────────────────────────────┐
│                                                                         │
│  CLIENT                           NETWORK              SERVER           │
│                                                                         │
│  User drags finger                                                      │
│  on steer area                                                          │
│       │                                                                 │
│       ▼                                                                 │
│  ClientInputController                                                  │
│  .OnSteerDrag()                                                         │
│  _rawSteer = deltaX / width                                             │
│       │                                                                 │
│       ▼                                                                 │
│  .Update()                                                              │
│  _smoothSteer = Lerp(_smoothSteer, _rawSteer, dt*10)                   │
│       │                                                                 │
│       ▼                                                                 │
│  Timer tick (60 Hz)                                                     │
│  Build INPUT_C2S:                                                       │
│    steer = _smoothSteer                                                 │
│    throttle = _smoothThrottle                                           │
│    brake = _smoothBrake                                                 │
│    handbrake = _handbrake                                               │
│       │                                                                 │
│       ▼                                                                 │
│  Protocol.SerializeInput()                                              │
│  → byte[20]                                                             │
│       │                                                                 │
│       ▼                                                                 │
│  UdpClientPeer.Send()                   ┌────────┐                      │
│       ├──────────────────────────────►  │  UDP   │                      │
│       │                                 │ Packet │                      │
│       │                                 └────┬───┘                      │
│       │                                      │                          │
│       │                                      ▼                          │
│       │                            UdpServerPeer.RecvLoop()             │
│       │                                      │                          │
│       │                                      ▼                          │
│       │                            Parse header + payload               │
│       │                            _latestInput = input (atomic)        │
│       │                            _latestInputSeq = seq                │
│       │                            _latestInputTimestamp = ts           │
│       │                                      │                          │
│       │                                      ▼                          │
│       │                            [Wait for FixedUpdate]               │
│       │                                      │                          │
│       │                                      ▼                          │
│       │                            ServerSimulationController           │
│       │                            .FixedUpdate() @ 50 Hz               │
│       │                                      │                          │
│       │                                      ▼                          │
│       │                            Read _latestInput (atomic)           │
│       │                            Check staleness (<200ms)             │
│       │                                      │                          │
│       │                                      ▼                          │
│       │                            Apply steer to front wheels:         │
│       │                            targetSteer = input.steer * 30°      │
│       │                            _currentSteer = Lerp(..., dt*5)      │
│       │                            wheelFL.steerAngle = _currentSteer   │
│       │                            wheelFR.steerAngle = _currentSteer   │
│       │                                      │                          │
│       │                                      ▼                          │
│       │                            Apply throttle to rear wheels:       │
│       │                            torque = input.throttle * curve      │
│       │                            wheelRL.motorTorque = torque * 0.5   │
│       │                            wheelRR.motorTorque = torque * 0.5   │
│       │                                      │                          │
│       │                                      ▼                          │
│       │                            Unity Physics.Simulate()             │
│       │                            → Car body rotates                   │
│       │                            → Wheels rotate                      │
│       │                                      │                          │
│       │                                      ▼                          │
│       │                            Compute state:                       │
│       │                            speed = velocity.magnitude * 3.6     │
│       │                            rpm = wheelRpm * gearRatio           │
│       │                            wheelSlip = hitInfo.slip             │
│       │                                      │                          │
│       │                                      ▼                          │
│       │                            [Wait for StateBroadcaster]          │
│       │                                      │                          │
│       │                                      ▼                          │
│       │                            StateBroadcaster.Update()            │
│       │                            Timer tick (25 Hz)                   │
│       │                                      │                          │
│       │                                      ▼                          │
│       │                            Build STATE_S2C:                     │
│       │                              pos = carBody.position             │
│       │                              rot = carBody.rotation             │
│       │                              speed = _speedKmh                  │
│       │                              rpm = _currentRpm                  │
│       │                              steerAngle = _currentSteerAngle    │
│       │                              ... (gear, slip, lights)           │
│       │                                      │                          │
│       │                                      ▼                          │
│       │                            Protocol.SerializeState()            │
│       │                            → byte[69]                           │
│       │                                      │                          │
│       │                 ┌────────┐           ▼                          │
│       │                 │  UDP   │◄────  UdpServerPeer.Send()           │
│       │                 │ Packet │                                      │
│       │                 └────┬───┘                                      │
│       │                      │                                          │
│       ▼                      ▼                                          │
│  UdpClientPeer.RecvLoop()                                               │
│       │                                                                 │
│       ▼                                                                 │
│  Parse STATE_S2C                                                        │
│  _latestState = state (atomic)                                          │
│       │                                                                 │
│       ▼                                                                 │
│  ClientStateHUD.Update()                                                │
│  Display: "Speed: 23 km/h"                                              │
│  Display: "Gear: 2"                                                     │
│  Display: "Ping: 28 ms"                                                 │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘

     Total latency: ~30-50ms on LAN
```

---

## **Packet Format Examples**

### **INPUT_C2S (UDP)**

```
Byte   0      1-2      3-6         7-10     11-14    15-18    19
     ┌───┬─────────┬──────────┬─────────┬────────┬────────┬───┐
     │0x02│ Seq=42 │  Ts=1234 │ steer   │throttle│ brake  │hb │
     │    │  (LE)  │   (LE)   │ 0.5 (f) │0.8 (f) │0.0 (f) │ 0 │
     └───┴─────────┴──────────┴─────────┴────────┴────────┴───┘
       ↑     ↑         ↑          ↑        ↑        ↑      ↑
     MsgType Seq   TimestampMs  float    float    float  byte
                   (Header=7)        (Payload=13)

Total: 20 bytes
```

### **STATE_S2C (UDP)**

```
Byte   0      1-2      3-6         7-18      19-34      35-38   39-42
     ┌───┬─────────┬──────────┬──────────┬──────────┬────────┬────────┐
     │0x03│ Seq=10 │  Ts=1235 │ position │ rotation │ speed  │  rpm   │
     │    │  (LE)  │   (LE)   │Vector3(f)│Quaternion│float(f)│float(f)│
     └───┴─────────┴──────────┴──────────┴──────────┴────────┴────────┘

Byte  43    44-47     48-51    52-55    56-59    60-63   64  65  66    67-68
     ┌───┬────────┬────────┬────────┬────────┬────────┬───┬───┬───┬─────────┐
     │gear│steerAngle│slipFL│slipFR│slipRL│slipRR│lights│ind│cam│lastInSeq│
     │sbyte│float(f)│float │float │float │float │byte│byte│byte│ushort(LE)│
     └───┴────────┴────────┴────────┴────────┴────────┴───┴───┴───┴─────────┘

Total: 69 bytes
```

### **SET_GEAR_C2S (TCP)**

```
Byte   0      1-2      3-6         7
     ┌───┬─────────┬──────────┬──────┐
     │0x04│ Seq=50 │  Ts=2000 │ gear │
     │    │  (LE)  │   (LE)   │  2   │
     └───┴─────────┴──────────┴──────┘
       ↑     ↑         ↑         ↑
     MsgType Seq   TimestampMs sbyte

Total: 8 bytes
```

---

## **Latency Budget (Typical LAN)**

```
┌─────────────────────────────────────────────────────────────┐
│                   TOTAL LATENCY: ~30-50ms                   │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  User Input (touch)                       0 ms              │
│       │                                                     │
│       ▼                                                     │
│  Client Input Smoothing                   ~8 ms (0.5 frame)│
│       │                                                     │
│       ▼                                                     │
│  Serialize INPUT_C2S                      <1 ms            │
│       │                                                     │
│       ▼                                                     │
│  UDP Send (Client → Server)               5-10 ms (Wi-Fi)  │
│       │                                                     │
│       ▼                                                     │
│  Server RecvThread → Parse                <1 ms            │
│       │                                                     │
│       ▼                                                     │
│  Wait for FixedUpdate                     0-20 ms (worst)  │
│       │                                                     │
│       ▼                                                     │
│  Physics Simulation @ 50 Hz               20 ms (1 tick)   │
│       │                                                     │
│       ▼                                                     │
│  Wait for StateBroadcaster                0-40 ms (worst)  │
│       │                                                     │
│       ▼                                                     │
│  Serialize STATE_S2C                      <1 ms            │
│       │                                                     │
│       ▼                                                     │
│  UDP Send (Server → Client)               5-10 ms (Wi-Fi)  │
│       │                                                     │
│       ▼                                                     │
│  Client RecvThread → Parse                <1 ms            │
│       │                                                     │
│       ▼                                                     │
│  HUD Update                               <1 ms            │
│       │                                                     │
│       ▼                                                     │
│  Display Frame                            ~8 ms (0.5 frame)│
│                                                             │
└─────────────────────────────────────────────────────────────┘

Best case: ~20ms (all timers align perfectly)
Typical:   ~30-50ms (some timer misalignment)
Worst case: ~80ms (max timer misalignment)
```

---

**For more details, see [README.md](README.md) and [QUICK_REFERENCE.md](QUICK_REFERENCE.md)**
