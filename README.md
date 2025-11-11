# 🏛️ AR Heritage Site Explorer  

### Bringing India’s Heritage to Life through Augmented Reality  

The **AR Heritage Site Explorer** is a Unity-based Augmented Reality (AR) project that allows users to explore Indian heritage monuments in 3D space. By leveraging **AR Foundation**, **ARCore**, and **ARKit**, users can visualize monuments in their surroundings, rotate and scale them, and learn about their historical significance — all through immersive AR experiences.

---

## ✨ Features

- 📱 **AR Visualization:** Place and explore 3D models of heritage sites in real-world space.  
- 🏗️ **Interactive Manipulation:** Rotate, scale, and move monuments using touch gestures.  
- 🗣️ **Cultural Learning:** Each monument includes historical information and facts.  
- 🌐 **Cross-Platform Compatibility:** Works on Android and iOS with ARCore/ARKit support.  
- ⚙️ **Modular Unity Scripts:** Clean, reusable C# scripts for AR object placement and manipulation.

---

## 🧩 Tech Stack

| Component | Technology |
|------------|-------------|
| **Game Engine** | Unity 2022+ |
| **AR Framework** | AR Foundation, ARCore, ARKit |
| **Programming Language** | C# |
| **3D Assets** | Blender / Sketchfab (optimized FBX models) |
| **Device Support** | Android (tested), iOS (supported) |
| **Version Control** | Git & GitHub |

---

## 🏗️ Core Scripts

| File | Purpose |
|------|----------|
| `ARPlacementManager.cs` | Handles placement of 3D models in the AR environment. |
| `ARObjectManipulator.cs` | Enables scaling, rotation, and translation of AR objects via gestures. |
| `TouchManipulation.cs` | Detects and manages multi-touch input. |

---

## ⚙️ How It Works

1. The app detects surfaces using **AR Plane Detection**.  
2. User taps on a plane to **place a 3D monument model**.  
3. The placed model can be **rotated, scaled, or moved** interactively.  
4. Information overlays or UI panels show historical details.  

---

## 📲 Installation & Usage

### 🧱 Prerequisites
- Unity 2022 or later  
- Android SDK + ARCore XR Plugin  
- (Optional) iOS Build Support + ARKit XR Plugin  

### 🔧 Setup
1. Clone the repository  
   ```bash
   git clone https://github.com/02manishku/ar-heritage-site.git
   cd ar-heritage-site
2. Open the main AR scene (e.g., SampleScene.unity).

3. Build and run the app on a supported device.
   Open the main AR scene (e.g., SampleScene.unity).

4.Build and run the app on a supported device.
