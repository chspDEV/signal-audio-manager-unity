# 🎧 Signal Audio Manager

> A robust, zero-friction audio management system for Unity — designed for developers who want AAA audio architecture without the headache.

Built on a **UI Toolkit Dashboard**, automated **Lazy Loading**, smart **Object Pooling**, and **Strongly Typed Audio Keys** to prevent runtime errors before they happen.

---

## ✨ Core Features

| Feature | Description |
|---|---|
| 🚀 **1-Click Initial Setup** | Instantly generates your Audio Mixers, Prefabs, and Global Settings in any folder you choose |
| 🖥️ **UI Toolkit Dashboard** | Manage Music, SFX, and UI sounds from a single, responsive central hub with visual error warnings |
| ⚡ **Automated Lazy Loading** | Call the API from anywhere — Signal auto-instantiates itself safely, no scene setup required |
| 🏊 **Smart Audio Pooling** | High-performance Object Pooling for SFX and UI sounds — zero GC spikes, optimal mobile performance |
| 🔑 **Typo-Proof API** | Auto-generates a C# script with all your Audio IDs as constants, enabling IDE autocomplete and eliminating string-based bugs |

---

## 📦 Installation

### Option A — Unity Package Manager (Recommended)

1. Open Unity and go to `Window > Package Manager`
2. Click `+` and select **Add package from git URL...**
3. Paste the URL below and click **Add**

```
https://github.com/carlosbobao/signal-audio-manager.git
```

### Option B — Unity Package

1. Download the `.unitypackage` file from the [Releases](../../releases) page
2. Drag and drop it into your Unity project
3. Import all files

---

## 🚀 Quick Start

1. Open `Window > Signal Audio > Dashboard`
2. Click **🚀 Generate Initial Setup** and choose your destination folder
3. Go to the **SFX Library** tab and add a new entry (e.g., `player_jump`)
4. Assign an `AudioClip` and tweak the random pitch/volume variations
5. Call it from anywhere in your code:

```csharp
using SignalAudioManagerUnity.Communication;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private void Jump()
    {
        Signal.PlaySFX("player_jump", transform.position);
    }
}
```

---

## ⚡ Strongly Typed Audio Keys

Typing strings like `"player_jump"` manually leads to typos and silent runtime bugs. Signal solves this with a **1-Click Code Generator**.

1. Open the **Signal Dashboard**
2. Go to the **Global Settings** tab
3. Click **⚡ Generate AudioKeys.cs**

Signal creates a static class with all your Audio IDs. Your IDE will autocomplete every sound — 100% typo-proof.

```csharp
using SignalAudioManagerUnity.Communication;
using SignalAudioManagerUnity;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private void Jump()
    {
        // Full IDE autocomplete — no more guessing string names
        Signal.PlaySFX(AudioKeys.SFX.player_jump, transform.position);
    }
}
```

---

## 🗺️ Roadmap

- [ ] **Advanced Ambient Trigger Zones** — Dynamic audio zones with seamless crossfading, spline-based boundaries, and automatic time-of-day integration
- [ ] **Surface-Aware Footstep Engine** — Plug-and-play system that detects Physics Materials and Terrain textures to trigger the correct surface sounds dynamically
- [ ] **Dynamic Audio Occlusion (Raycasting)** — Real-time spatial acoustics: sounds behind walls automatically apply Low-Pass filters to simulate realistic muffling
- [ ] **Adaptive Music System (Stems)** — Dynamically fade individual instrument layers in and out based on game intensity
- [ ] **Smart Audio Ducking** — Automatically lower background music and ambient sounds when a Voice-Over or important UI notification plays
- [ ] **In-Scene Visual Debugger** — Scene View visualizer showing active audio pools, 3D spatialization spheres, and occlusion raycasts

---

## 📄 License

This asset is provided under a **Commercial EULA**. You are free to use it in personal and commercial compiled end products (e.g., video games). Reselling, sub-licensing, or redistributing the source code as a standalone product is strictly prohibited.

See [LICENSE.md](./LICENSE.md) for full details.

---

<p align="center">Copyright © 2026 carlosbobao — All rights reserved.</p>
