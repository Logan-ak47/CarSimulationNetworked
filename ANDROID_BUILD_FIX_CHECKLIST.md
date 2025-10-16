# Android Build Fix Checklist

## Overview
This checklist contains all the steps needed to fix the Android build crash issue for the Car Simulation Networked project. The crash was caused by missing permissions and IL2CPP compatibility issues.

---

## ✅ Code Fixes (COMPLETED)

The following code changes have been automatically applied:

### 1. TcpClientPeer.cs
- ✅ Added socket timeouts (5 seconds) to prevent hangs
- ✅ Increased thread join timeout from 500ms to 2000ms

### 2. UdpClientPeer.cs
- ✅ Increased thread join timeout from 500ms to 2000ms

### 3. ClientConnectionUI.cs
- ✅ Added Android permission warning check

---

## 🔧 Unity Editor Settings (MANUAL STEPS REQUIRED)

You must manually configure these settings in Unity Editor:

### Step 1: Open Project Settings
1. In Unity, go to **Edit → Project Settings**
2. Select **Player** in the left sidebar
3. Click on the **Android tab** (Android icon)

---

### Step 2: Configure "Other Settings" Section

Expand **Other Settings** and change the following:

#### 2.1 Scripting Settings
| Setting | Current Value | New Value | Location |
|---------|--------------|-----------|----------|
| **Scripting Backend** | IL2CPP ✓ | IL2CPP ✓ (No change needed) | Other Settings → Configuration |
| **API Compatibility Level** | .NET Framework ✓ | .NET Framework ✓ (No change needed) | Other Settings → Configuration |
| **Allow 'unsafe' Code** | ❌ OFF | ✅ **ON** | Other Settings → Configuration |

**How to find:**
- Scroll down to **Configuration** subsection
- Find checkbox labeled **"Allow 'unsafe' Code"**
- ✅ **CHECK THIS BOX**

---

#### 2.2 Android Identification Settings
| Setting | Current Value | New Value | Location |
|---------|--------------|-----------|----------|
| **Minimum API Level** | Android 6.0 (API 23) ✓ | Android 6.0 (API 23) ✓ (No change) | Other Settings → Identification |
| **Target API Level** | ❌ Automatic (0) | ✅ **Android 13 (API 33)** or higher | Other Settings → Identification |

**How to find:**
- Scroll to **Identification** subsection
- Find dropdown **"Target API Level"**
- Select **"Android 13.0 'Tiramisu' (API level 33)"** or **"Android 14.0 'UpsideDownCake' (API level 34)"**

---

#### 2.3 Android Internet Permission
| Setting | Current Value | New Value | Location |
|---------|--------------|-----------|----------|
| **Internet Access** | ❌ Auto | ✅ **Require** | Other Settings → Configuration |

**How to find:**
- Look for **"Internet Access"** dropdown in **Configuration** subsection
- Change from **"Auto"** to **"Require"**
- This forces the Android manifest to include `<uses-permission android:name="android.permission.INTERNET" />`

---

### Step 3: Configure "Resolution and Presentation" Section

1. In **Player Settings**, expand **Resolution and Presentation**
2. Find **"Run In Background"**
3. ✅ **CHECK THIS BOX**

**Why?** This ensures networking continues even if the app loses focus (important for multiplayer).

---

## 📱 AndroidManifest.xml (OPTIONAL BUT RECOMMENDED)

If the "Internet Access = Require" setting doesn't work, manually create an AndroidManifest.xml:

### Location
Create this file at:
```
Assets/Plugins/Android/AndroidManifest.xml
```

### Full AndroidManifest.xml Content
```xml
<?xml version="1.0" encoding="utf-8"?>
<manifest
    xmlns:android="http://schemas.android.com/apk/res/android"
    package="com.DefaultCompany.CarSimulationNetworked"
    android:versionCode="1"
    android:versionName="1.0">

    <!-- Required Permissions for Networking -->
    <uses-permission android:name="android.permission.INTERNET" />
    <uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />
    <uses-permission android:name="android.permission.ACCESS_WIFI_STATE" />

    <!-- Minimum and Target SDK -->
    <uses-sdk android:minSdkVersion="23" android:targetSdkVersion="33" />

    <!-- Application Declaration -->
    <application
        android:allowBackup="true"
        android:icon="@mipmap/app_icon"
        android:label="@string/app_name"
        android:usesCleartextTraffic="true">
        <!-- Note: usesCleartextTraffic="true" allows non-HTTPS connections for local IP addresses -->
    </application>
</manifest>
```

**Important:**
- Replace `com.DefaultCompany.CarSimulationNetworked` with your actual package name (found in Player Settings → Other Settings → Package Name)
- The `usesCleartextTraffic="true"` attribute allows connecting to local IP addresses without HTTPS (needed for your local server setup)

---

## 🔍 Build Settings Configuration

### Step 4: Configure Build Settings

1. Go to **File → Build Settings**
2. Verify **Platform** is set to **Android**
3. Click **"Switch Platform"** if needed (wait for switch to complete)

#### Build Settings Checklist
| Setting | Recommended Value | Why |
|---------|------------------|-----|
| **Development Build** | ✅ ON (for debugging) | Enables logging and debugging on device |
| **Script Debugging** | ✅ ON (optional) | Allows attaching debugger |
| **Compression Method** | LZ4 or default | Faster build times during testing |

---

## 🏗️ Building for Android

### Step 5: Build and Deploy

1. **Connect Android Device/Emulator**
   - If using emulator: Start it from Android Studio
   - If using device: Enable USB Debugging in Developer Options

2. **Build and Run**
   - In Build Settings, click **"Build And Run"**
   - Save APK to a convenient location (e.g., `Builds/Android/client.apk`)
   - Unity will automatically install and launch on device/emulator

3. **Check Logcat for Logs**
   - Open terminal/command prompt
   - Run: `adb logcat -s Unity`
   - This shows Unity debug logs from the device

---

## 🧪 Testing the Fix

### After Building:

1. **App Should Launch** (no immediate crash)
2. **Enter Server IP** in the connection UI
3. **Click Connect** button
4. **Check Logcat** for connection logs:
   ```
   [TcpClient] Connected to <ip>:9000
   [UdpClient] Listening on port 9002, sending to <ip>:9001
   ```

### If It Still Crashes:

Run this command to get full crash logs:
```bash
adb logcat -d > android_crash_log.txt
```

Then search for:
- `FATAL EXCEPTION`
- `SecurityException`
- `SocketException`
- `MissingMethodException`

---

## 📋 Quick Reference Summary

### Settings to Change in Unity Editor:

1. ✅ **Player Settings → Android → Other Settings → Allow 'unsafe' Code** = ON
2. ✅ **Player Settings → Android → Other Settings → Internet Access** = Require
3. ✅ **Player Settings → Android → Other Settings → Target API Level** = Android 13 (API 33)
4. ✅ **Player Settings → Android → Resolution and Presentation → Run In Background** = ON
5. ✅ **Build Settings → Development Build** = ON (for testing)

### Code Changes (Already Applied):
- ✅ Socket timeouts added to TcpClientPeer.cs
- ✅ Thread join timeouts increased to 2000ms
- ✅ Permission warning added to ClientConnectionUI.cs

---

## 🎯 Priority Order (Most Critical First)

| Priority | Setting | Impact if Missing | Fix Time |
|----------|---------|------------------|----------|
| 🔴 CRITICAL | Internet Access = Require | App crashes instantly | 30 sec |
| 🔴 CRITICAL | Allow 'unsafe' Code = ON | IL2CPP compilation fails | 30 sec |
| 🟡 HIGH | Target API Level = 33 | May crash on Android 12+ | 30 sec |
| 🟡 HIGH | Socket timeouts (code) | App hangs on connect | ✅ Done |
| 🟢 MEDIUM | Run In Background = ON | Networking stops on focus loss | 30 sec |

---

## ✅ Completion Checklist

Before building, verify:

- [ ] Player Settings → Android → Allow 'unsafe' Code = **ON**
- [ ] Player Settings → Android → Internet Access = **Require**
- [ ] Player Settings → Android → Target API Level = **Android 13 (API 33)**
- [ ] Player Settings → Android → Run In Background = **ON**
- [ ] Build Settings → Platform = **Android** (switched)
- [ ] Build Settings → Development Build = **ON** (for testing)
- [ ] Code changes verified in Assets/Client/Scripts/

---

## 📞 Troubleshooting

### Issue: "Manifest merger failed"
**Solution:** If you created AndroidManifest.xml manually, ensure the package name matches Player Settings.

### Issue: "Build failed with IL2CPP error"
**Solution:** Verify "Allow 'unsafe' Code" is checked. Clean build folder (Assets → Clean → All).

### Issue: "App installs but crashes immediately"
**Solution:**
1. Check `adb logcat` for SecurityException → Internet permission not set
2. Check for MissingMethodException → IL2CPP unsafe code not enabled

### Issue: "Cannot connect to server"
**Solution:**
1. Verify server IP is correct (192.168.x.x for local network)
2. Ensure server is running on Windows PC
3. Check firewall allows ports 9000 (TCP) and 9001 (UDP)
4. Try pinging server from Android: Install Network Tools app

---

## 📝 Notes

- **IL2CPP vs Mono:** Your project uses IL2CPP (ARM64) which requires unsafe code for socket operations
- **Target SDK:** Android 13 (API 33) is recommended. Don't use "Automatic" as it causes permission issues
- **Development Build:** Keep enabled during testing for better error messages
- **Release Build:** After testing, disable Development Build for production APK

---

## 🚀 Next Steps After Successful Build

1. Test connection to server from Android device
2. Test all input controls (steering, throttle, brake, camera)
3. Test network stability (move away from Wi-Fi router to test reconnection)
4. Optimize UI for mobile screen sizes
5. Add better error messages for connection failures
6. Consider adding automatic reconnection logic

---

**Last Updated:** 2025-10-16
**Unity Version:** 2020.3 LTS+
**Target Platform:** Android 6.0+ (API 23+)
**Build Backend:** IL2CPP (ARM64)
