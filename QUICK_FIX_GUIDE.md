# 🚀 Quick Fix Guide - Android Crash Issue

## ⚡ 3-Minute Fix

### Problem
Android app crashes immediately on launch with no logs.

### Root Cause
1. Missing Internet permission
2. IL2CPP unsafe code disabled
3. Missing Android Target SDK

---

## ✅ Solution (Do These 4 Things)

### 1️⃣ Open Unity → Edit → Project Settings → Player → Android

### 2️⃣ In "Other Settings" section, change:
```
✅ Allow 'unsafe' Code = ON
✅ Internet Access = Require
✅ Target API Level = Android 13 (API 33)
```

### 3️⃣ In "Resolution and Presentation" section:
```
✅ Run In Background = ON
```

### 4️⃣ Build and Run
- File → Build Settings → Build And Run
- Enable "Development Build" to see logs

---

## 📱 How to See Logs

Open terminal and run:
```bash
adb logcat -s Unity
```

---

## ✅ What's Been Fixed Automatically

The following code changes have already been applied:

- ✅ [TcpClientPeer.cs:60-62](Assets/Client/Scripts/TcpClientPeer.cs#L60-L62) - Socket timeouts added
- ✅ [TcpClientPeer.cs:92-93](Assets/Client/Scripts/TcpClientPeer.cs#L92-L93) - Thread cleanup timeout increased
- ✅ [UdpClientPeer.cs:62](Assets/Client/Scripts/UdpClientPeer.cs#L62) - Thread cleanup timeout increased
- ✅ [ClientConnectionUI.cs:94-100](Assets/Client/Scripts/ClientConnectionUI.cs#L94-L100) - Android permission check added
- ✅ [Assets/Plugins/Android/AndroidManifest.xml](Assets/Plugins/Android/AndroidManifest.xml) - Created with all required permissions

---

## 🔍 If It Still Crashes

1. **Check package name in AndroidManifest.xml matches Player Settings:**
   - Open: Assets/Plugins/Android/AndroidManifest.xml
   - Find: `package="com.DefaultCompany.CarSimulationNetworked"`
   - Compare with: Player Settings → Other Settings → Package Name
   - Update if different

2. **Verify settings were saved:**
   - Close and reopen Project Settings
   - Verify all 4 checkboxes above are still checked

3. **Clean build:**
   - Delete `Library/Bee` folder
   - Delete `Temp` folder
   - Build again

4. **Get full crash log:**
   ```bash
   adb logcat -d > crash_log.txt
   ```
   Search for: FATAL EXCEPTION, SecurityException, SocketException

---

## 📋 Checklist Before Building

- [ ] Allow 'unsafe' Code = ON
- [ ] Internet Access = Require
- [ ] Target API Level = Android 13 (API 33)
- [ ] Run In Background = ON
- [ ] Development Build = ON (for testing)

---

## 🎯 Expected Result

After fix:
1. ✅ App launches successfully
2. ✅ Connection UI appears
3. ✅ Can enter server IP and connect
4. ✅ Logs show: `[TcpClient] Connected to <ip>:9000`

---

## 📚 More Details

See [ANDROID_BUILD_FIX_CHECKLIST.md](ANDROID_BUILD_FIX_CHECKLIST.md) for comprehensive documentation.

---

**Last Updated:** 2025-10-16
