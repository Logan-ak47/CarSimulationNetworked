# Critical Protocol Fix - TCP Length Prefixing

## 🔴 **ISSUE: Client Stuck at "Connecting..."**

### **Root Cause**
The HELLO message has a **variable-length payload** (token + port + client name string), but the server was expecting a **fixed 200-byte payload**. This caused the TCP receiver to:
1. Wait for 200 bytes that would never arrive
2. Block indefinitely
3. Never process the HELLO message
4. Never send WELCOME back

### **Symptom**
```
Server logs:
  [TcpServer] Listening on port 9000
  [TcpServer] Client connected: <IP>
  (NO HELLO/WELCOME logs - stuck waiting for bytes!)

Client logs:
  [TcpClient] Connected to <IP>:9000
  (Stuck at "Connecting..." - waiting for WELCOME that never comes)
```

---

## ✅ **SOLUTION: Length-Prefixed Protocol**

Added a **2-byte payload length field** to the header so both sender and receiver know exactly how many bytes to expect.

### **Old Header (7 bytes)**
```
MsgType (1) | Seq (2) | Timestamp (4)
```

### **New Header (9 bytes)**
```
MsgType (1) | Seq (2) | Timestamp (4) | PayloadLength (2)
```

---

## 📝 **Files Changed**

### **1. ByteCodec.cs** - Header Structure
- Changed `HEADER_SIZE` from 7 to 9 bytes
- Updated `WriteHeader()` to include `ushort payloadLength` parameter
- Updated `ReadHeader()` to read and return `payloadLength`

### **2. Protocol.cs** - All Serializers
Rewrote **all 10 message serializers** to:
1. Write payload to buffer starting at `ByteCodec.HEADER_SIZE`
2. Measure actual payload length
3. Write header with measured length
4. Return total size (header + payload)

**Example (HELLO_C2S):**
```csharp
public static int SerializeHello(byte[] buffer, ushort seq, HelloC2S msg)
{
    // Write payload first to measure length
    int payloadOffset = ByteCodec.HEADER_SIZE;
    int offset = payloadOffset;
    ByteCodec.WriteFixedBytes(buffer, ref offset, msg.token, HelloC2S.TOKEN_SIZE);
    ByteCodec.WriteUShort(buffer, ref offset, msg.clientUdpPort);
    ByteCodec.WriteString(buffer, ref offset, msg.clientName);
    ushort payloadLength = (ushort)(offset - payloadOffset);

    // Write header with length
    offset = 0;
    ByteCodec.WriteHeader(buffer, ref offset, MsgType.HELLO_C2S, seq,
                          StopwatchTime.TimestampMs(), payloadLength);

    return ByteCodec.HEADER_SIZE + payloadLength;
}
```

### **3. TcpServerPeer.cs** - Read Length from Header
- Changed: Read `payloadLength` from header
- Removed: `GetPayloadSize()` method (no longer needed)
- Removed: Unused `_sendBuffer` field

**Before:**
```csharp
int payloadSize = GetPayloadSize(msgType); // WRONG: guessing!
```

**After:**
```csharp
ByteCodec.ReadHeader(_recvBuffer, ref offset, out MsgType msgType,
                     out ushort seq, out uint ts, out ushort payloadLength);
int payloadSize = payloadLength; // Exact size from sender
```

### **4. TcpClientPeer.cs** - Same Fix
- Read `payloadLength` from header
- Removed `GetPayloadSize()` method

### **5. UdpServerPeer.cs** - Updated for Consistency
- Updated `ReadHeader()` call to match new signature
- UDP doesn't need length (datagram has it), but header must match

### **6. UdpClientPeer.cs** - Updated for Consistency
- Updated `ReadHeader()` call to match new signature

---

## 🧪 **Testing**

After this fix, you should see:

**Server logs:**
```
[TcpServer] Listening on port 9000
[UdpServer] Listening on port 9001
[TcpServer] Client connected: 192.168.0.103:xxxxx
[ServerRouter] HELLO from <device-name>, UDP port: 9002
[ServerRouter] Sent WELCOME, sessionId=1234
[UdpServer] Client endpoint learned: 192.168.0.103:9002
```

**Client logs:**
```
[TcpClient] Connected to 192.168.0.103:9000
[UdpClient] Listening on port 9002, sending to 192.168.0.103:9001
[ClientUI] WELCOME: sessionId=1234, tickRate=50
Connected! Session: 1234
(Panel switches from "Connect" to "Drive")
```

---

## ⚠️ **Breaking Change**

This is a **protocol-breaking change**. Old clients/servers will NOT work with new ones because:
- Old: Expects 7-byte header
- New: Sends 9-byte header

**Solution**: Rebuild both server and client after this fix.

---

## 📊 **Message Sizes**

| Message | Header | Payload | Total |
|---------|--------|---------|-------|
| HELLO_C2S | 9 | 16 + 2 + ~10 = ~28 | ~37 bytes |
| WELCOME_S2C | 9 | 4 + 1 + 1 = 6 | 15 bytes |
| INPUT_C2S | 9 | 4 + 4 + 4 + 1 = 13 | 22 bytes |
| STATE_S2C | 9 | 12 + 16 + 4×9 + 3 + 2 = 69 | 78 bytes |
| SET_GEAR_C2S | 9 | 1 | 10 bytes |
| ... | ... | ... | ... |

---

## ✅ **Status**

- [x] Header updated (7 → 9 bytes)
- [x] All 10 serializers updated
- [x] TcpServerPeer reads length from header
- [x] TcpClientPeer reads length from header
- [x] UdpServerPeer updated for consistency
- [x] UdpClientPeer updated for consistency
- [x] Removed unused `GetPayloadSize()` methods

**Ready to test!** 🚀

---

**Next Steps:**
1. Rebuild server (Unity → Windows)
2. Rebuild client (Unity → Android)
3. Connect and verify HELLO/WELCOME exchange works
4. Confirm client enters "Drive" mode
