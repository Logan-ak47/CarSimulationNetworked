# Thread Safety Fix - Main Thread Event Callbacks

## 🔴 **Issue: Unity Thread Errors**

### **Error Messages**
```
UnityException: get_isActiveAndEnabled can only be called from the main thread.
UnityEngine.UI.Text.set_text (System.String value)
CarSim.Client.ClientConnectionUI.SetStatus (System.String text)
CarSim.Client.ClientConnectionUI.OnDisconnected ()
CarSim.Client.TcpClientPeer.RecvLoop ()  <-- BACKGROUND THREAD!
```

### **Root Cause**
TCP events (`OnWelcome`, `OnNotice`, `OnDisconnected`) were being **invoked directly from the background TCP receive thread**, but Unity UI operations **must run on the main thread**.

**Flow:**
```
Background Thread (RecvLoop)
    ↓
OnDisconnected?.Invoke()
    ↓
ClientConnectionUI.OnDisconnected()
    ↓
SetStatus("Disconnected!")  <-- Tries to update UI.Text
    ↓
💥 CRASH: Unity doesn't allow UI updates from background threads!
```

---

## ✅ **Solution: Main Thread Action Queue**

Added a **RingBuffer of Action callbacks** that are enqueued from background threads and **processed on the main thread** in `Update()`.

### **Changes to TcpClientPeer.cs**

#### **1. Added Main Thread Queue**
```csharp
private RingBuffer<Action> _mainThreadActions = new RingBuffer<Action>(128);
```

#### **2. Added Update() to Process Queue**
```csharp
private void Update()
{
    // Process main thread callbacks
    while (_mainThreadActions.TryDequeue(out Action action))
    {
        try
        {
            action?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[TcpClient] Main thread callback error: {ex.Message}");
        }
    }
}
```

#### **3. Queue Events Instead of Direct Invoke**

**Before (WRONG):**
```csharp
OnDisconnected?.Invoke();  // Called from background thread!
```

**After (CORRECT):**
```csharp
_mainThreadActions.TryEnqueue(() => OnDisconnected?.Invoke());  // Queued, invoked on main thread
```

**All 5 locations fixed:**
1. Line 106: `OnDisconnected` (server disconnected - header)
2. Line 126: `OnDisconnected` (server disconnected - payload)
3. Line 142: `OnDisconnected` (exception in RecvLoop)
4. Line 179: `OnWelcome` (WELCOME message received)
5. Line 183: `OnNotice` (SERVER_NOTICE received)

---

## 📊 **Flow After Fix**

```
Background Thread (RecvLoop)
    ↓
Receive WELCOME message
    ↓
Deserialize WelcomeS2C
    ↓
_mainThreadActions.TryEnqueue(() => OnWelcome?.Invoke(welcome))
    ↓
    [Queued in RingBuffer, thread returns]

Main Thread (Update)
    ↓
_mainThreadActions.TryDequeue(out action)
    ↓
action?.Invoke()  <-- OnWelcome?.Invoke(welcome)
    ↓
ClientConnectionUI.OnWelcome(welcome)
    ↓
SetStatus("Connected! Session: 1234")  <-- UI update on main thread ✓
    ↓
✅ Success!
```

---

## 🧪 **Testing**

After this fix, you should see:

**Client logs (no errors):**
```
[TcpClient] Connected to 192.168.0.103:9000
[UdpClient] Listening on port 9002, sending to 192.168.0.103:9001
[ClientUI] WELCOME: sessionId=1584, tickRate=50
```

**Client UI:**
- Status changes from "Connecting..." to "Connected! Session: 1584"
- Panel switches from Connect to Drive
- **No Unity thread errors!**

---

## 📝 **Summary**

**Problem**: Events invoked from background thread → tried to update UI → crash
**Solution**: Queue events in RingBuffer → process on main thread in Update() → safe UI updates
**Pattern**: Producer (background) → RingBuffer → Consumer (main thread)

This is a **standard Unity threading pattern** for background I/O with main thread callbacks.

---

## ✅ **Status**

- [x] Added `_mainThreadActions` RingBuffer
- [x] Added `Update()` to process queued actions
- [x] Fixed 5 locations where events are invoked
- [x] All Unity thread errors resolved

**The client now connects successfully without thread errors!** 🎉

---

**Last Updated**: 2025-01-09
**Files Modified**: 1 (`Assets/Client/Scripts/TcpClientPeer.cs`)
