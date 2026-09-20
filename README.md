# Signal Audio Manager

> A robust, zero-friction audio management system for Unity, designed for developers who want AAA audio architecture without the headache.

[![Version](https://img.shields.io/badge/version-1.1.0-blue.svg)](https://github.com/carlosbobao/signal-audio-manager-unity)
[![License](https://img.shields.io/badge/license-Commercial-green.svg)](./LICENSE.md)

Built on a **UI Toolkit Dashboard**, automated **Lazy Loading**, smart **Object Pooling**, **3D Spatial Controls**, **Surface Footsteps**, and **Strongly Typed Audio Keys** to prevent runtime errors before they happen.

---

## Core Features

| Feature | Description |
|---|---|
| **1-Click Initial Setup** | Instantly generates your Audio Mixers, Prefabs, and Global Settings in any folder you choose |
| **UI Toolkit Dashboard** | Manage Music, SFX, and UI sounds from a single, responsive central hub with pagination and search |
| **Surface Footstep Engine** | Detects Physics Materials and Terrain textures dynamically via Raycast to play ground-specific footstep SFX |
| **Ambient Trigger Zones (3D & 2D)** | Dynamic audio zones with seamless crossfading, custom volume curves, and exit behaviors |
| **Fine-Grained 3D Spatialization** | Custom Doppler Level, Spread, Audio Rolloff curves, and Min/Max Distance configurable per sound entry |
| **Smart Audio Ducking** | Automatically lower background music and ambient sounds when a Voice-Over or UI sound plays |
| **Automated Lazy Loading** | Call the API from anywhere; Signal auto-instantiates itself safely, no scene setup required |
| **Smart Audio Pooling** | High-performance Object Pooling for SFX and UI sounds: zero GC spikes, optimal mobile performance |
| **Typo-Proof API** | Auto-generates a C# script with all your Audio IDs as constants (`AudioKeys.cs`), enabling full IDE autocomplete |
| **Built-in Interactive Manual** | Full API documentation natively integrated directly into the Unity Editor inside the Signal Dashboard |

---

## Installation

### Option A: Unity Package Manager (Recommended)

1. Open Unity and go to `Window > Package Manager`
2. Click `+` and select **Add package from git URL...**
3. Paste the URL below and click **Add**

```
https://github.com/carlosbobao/signal-audio-manager.git
```

### Option B: Unity Package

1. Download the `.unitypackage` file from the [Releases](../../releases) page
2. Drag and drop it into your Unity project
3. Import all files

---

## Quick Start & Usage

### 1. Basic Sound Playback

Call Signal from anywhere in your codebase without worrying about scene setup:

```csharp
using SignalAudioManagerUnity.Communication;
using SignalAudioManagerUnity;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private void Jump()
    {
        // 2D SFX at player position
        Signal.PlaySFX(AudioKeys.SFX.player_jump, transform.position);
    }

    private void StartGame()
    {
        // Play Background Music with fade-in
        Signal.PlayMusic(AudioKeys.Music.main_menu_theme, fadeInDuration: 1.5f);
    }
}
```

---

### 2. Surface-Aware Footstep Engine

Attach `FootstepController` to your player to automatically play surface-specific sound effects based on Ground Physics Materials or Terrain textures:

1. Create a `SurfaceSoundMapSO` (Right-click > `Create > Signal Audio > Surface Sound Map`).
2. Map Physics Materials or Terrain layers to your SFX Audio IDs (e.g., `footstep_grass`, `footstep_concrete`).
3. Add `FootstepController` to your player character, assign the map, and choose **Animation Event** or **Velocity-Based** mode.

```csharp
// Call directly from Animation Events on exact footstep frames
public void OnFootstepAnimationEvent()
{
    footstepController.PlayFootstep();
}
```

---

### 3. Ambient Trigger Zones

Create immersive 3D or 2D ambient zones (e.g., entering a forest, cave, or underwater):

1. Add an `AmbientTrigger` (3D) or `AmbientTrigger2D` component to a GameObject with a Trigger Collider.
2. Select your `AmbientTypeSO` and set fade-in / fade-out overrides.
3. Signal will seamlessly crossfade ambient tracks when the player enters or exits the zone.

---

### 4. Fine-Grained 3D Spatial Audio Settings

Configure individual spatial settings directly inside the **Signal Dashboard**:
- **Spatial Blend**: 2D (0.0) vs 3D (1.0)
- **Doppler Level**: Set to `0` to prevent pitch distortion on fast-moving cameras
- **Spread & Min/Max Distance**: Fine-tune volume decay spheres for 3D environments
- **Audio Rolloff Mode**: Logarithmic, Linear, or Custom curves

---

## Strongly Typed Audio Keys

Typing strings like `"player_jump"` manually leads to typos and silent runtime bugs. Signal solves this with a **1-Click Code Generator**.

1. Open `ResenhaTools > Signal > Dashboard`
2. Go to the **Global Settings** tab
3. Click **Generate AudioKeys.cs**

Signal creates a static class with all your Audio IDs. Your IDE will autocomplete every sound, completely typo-proof.

```csharp
// Full IDE autocomplete, no more guessing string names!
Signal.PlaySFX(AudioKeys.SFX.player_jump, transform.position);
```

---

## Roadmap

- [x] **Advanced Ambient Trigger Zones (3D & 2D)**: Dynamic audio zones with seamless crossfading, custom volume curves, and exit behaviors
- [x] **Smart Audio Ducking**: Automatically lower background music and ambient sounds when a Voice-Over or UI sound plays
- [x] **Surface-Aware Footstep Engine**: Plug-and-play system detecting Physics Materials and Terrain textures to trigger surface sound effects
- [x] **Fine-Grained 3D Spatialization Controls**: Custom Doppler, Spread, Rolloff, and Min/Max distances configurable per sound entry
- [ ] **Dynamic Audio Occlusion (Raycasting)**: Real-time spatial acoustics; sounds behind walls automatically apply Low-Pass filters to simulate realistic muffling
- [ ] **Adaptive Music System (Stems)**: Dynamically fade individual instrument layers in and out based on game intensity
- [ ] **In-Scene Visual Debugger**: Scene View visualizer showing active audio pools, 3D spatialization spheres, and occlusion raycasts

---

## License

This asset is provided under a **Commercial EULA**. You are free to use it in personal and commercial compiled end products (e.g., video games). Reselling, sub-licensing, or redistributing the source code as a standalone product is strictly prohibited.

See [LICENSE.md](./LICENSE.md) for full details.

---

<p align="center">Copyright © 2026 carlosbobao. All rights reserved.</p>
