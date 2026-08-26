Welcome! This document provides essential information about **VAULT & VAULT**, including how to play, game mechanics, controls, academic context, system requirements, and project credits.

---

### Team Student Choice Project Team
- **Game Name**: VAULT & VAULT
- **Course**: 3D Interactive Development & Game Design (Azrieli College of Engineering)
- **Participants**:
  - **Mohammad Othman** — Project Lead, Level Design & Environment Architecture
  - **Ibrahim Amo** — Audio Direction, UI/UX Systems & Asset Integration
  - **Mohammad Enaby** — Parkour Movement Systems & Pursuer AI Intelligence
  - **Nazieh Sayegh** — Game AI Behaviors, Game Over & Ending Modals

---

### Table of Contents
1. [Game Overview](#game-overview)
2. [Credits](#credits)
3. [How to Play](#how-to-play)
4. [Controls](#controls)
5. [Objectives & Goals](#objectives--goals)
6. [Game Mechanics & Features](#game-mechanics--features)
7. [System Requirements](#system-requirements)
8. [Known Issues & Troubleshooting](#known-issues--troubleshooting)
9. [Additional Notes](#additional-notes)
10. [Media](#media)

---

## Game Overview
- **Genre**: 3D Cyberpunk First-Person Parkour Action & Pursuit Escape
- **Platform**: Windows PC (Unity 3D URP)
- **Team**: Mohammad Othman, Ibrahim Amo, Mohammad Enaby, Nazieh Sayegh

**VAULT & VAULT** is a fast-paced cyberpunk 1st-person parkour action game where players infiltrate high-security subterranean industrial facilities to extract classified Data Fragments. To survive, players must master fluid momentum-based parkour mechanics—wall running, vertical wall climbing, horizontal bar swinging, and sliding—while evading a relentless automated Pursuer AI Bot programmed to hunt down intruders.

---

## Credits

### Development Team
- **Mohammad Othman** — Project Lead, Level Design & Environment Architecture
- **Ibrahim Amo** — Audio Direction, UI/UX Systems & Asset Integration
- **Mohammad Enaby** — Parkour Movement Systems & Pursuer AI Intelligence
- **Nazieh Sayegh** — Game AI Behaviors, Game Over & Ending Modals

### Art & Engine Assets
- **Game Engine**: Unity 3D (Universal Render Pipeline - URP 2022.3.62f3)
- **Visual Assets**: Sci-Fi Modular Cyberpunk Environment Prefabs, Industrial Lighting Assets, Custom URP Shaders & Particle Systems
- **Audio & Sound Effects**: Industrial Cyberpunk BGM, Dynamic Alarm Systems, Footsteps, Parkour Interaction SFX, Collectible Pickup SFX & Soundscapes
- **Development Assistance**: Code Optimization & Technical Assistance via Google DeepMind Antigravity

---

## How to Play

1. **Starting the Game**:
   - Launch `Main Menu.unity`.
   - Click **PLAY** to begin Level 1, or access **LEVELS** to select any specific level.

2. **Gameplay Basics**:
   - Navigate 3D sci-fi environments using fluid 1st-person movement.
   - Combine sprinting, wall running, wall climbing, bar swinging, and sliding to overcome vertical gaps and obstacles.
   - Locate the glowing **Data Fragment** in each level and press **E** to collect it.
   - Collecting the Data Fragment unlocks the level exit door and activates the Pursuer AI Bot.

3. **Levels Structure**:
   - **Level 1**: Parkour training course introducing wall jumps, momentum sliding, and horizontal bar swinging.
   - **Level 2**: High-security vault facility featuring complex vertical platforming, security alarms, and active AI pursuit.
   - **Level 3**: Final high-tech core. Pick up the master Data Fragment, experience the cinematic AI reveal, navigate the final escape corridor, and reach the extraction door to trigger the automated Ending cutscene.

---

## Controls

- **Movement (WASD)**: Move Forward (W), Backward (S), Left (A), Right (D) with dynamic sprint acceleration.
- **Look / Camera (Mouse)**: 1st-person camera control with adjustable mouse sensitivity.
- **Jump / Leap (Spacebar)**: Standard jump, wall jump, and bar swing launch.
- **180° Camera Flip / Wall Turn (Q)**: Instant 180-degree turn while wall running or wall climbing.
- **Crouch / Slide (Left Ctrl)**: Hold while walking to crouch, or press while sprinting to perform a momentum slide under low obstacles.
- **Interact (E)**: Collect Data Fragment / Open level exit doors.
- **Pause Menu (ESC)**: Pause game, access audio/sensitivity settings, developer cheats, or return to Main Menu.

---

## Objectives & Goals

- **Main Objective**: Infiltrate each high-tech vault, retrieve the classified Data Fragment, survive the Pursuer AI Bot, and reach the extraction door to complete the level.
- **Secondary Goals**: Achieve **Rank S** speedrun time trials across all levels:
  - **Level 1**: Finish in under **45 seconds**
  - **Level 2**: Finish in under **75 seconds**
  - **Level 3**: Finish in under **95 seconds**

---

## Game Mechanics & Features

1. **First-Person Parkour Engine (`PlayerMovement.cs`, `BarSwinging.cs`, `CameraAnimator.cs`)**:
   - **Wall Running & Wall Climbing**: Raycast wall detection with camera tilting and 180° Q-key wall turns.
   - **Bar Swinging**: Pendulum physics and momentum launch jump off horizontal bars.
   - **Momentum Sliding**: Low-profile crouching and slide mechanics under industrial obstacles.
   - **Procedural Motion Effects**: Dynamic speed-based FOV, chromatic aberration, motion blur, and landing camera dips.

2. **Pursuer AI Bot Engine (`RubberBandBot.cs`)**:
   - Custom-engineered ghost-mode AI entity that records player position breadcrumbs (`recordInterval = 0.02s`) and follows exact player movement paths through 3D geometry.
   - **Adaptive Rubber-Banding**: Dynamic speed adjustment (`baseSpeed = 10.8 m/s`, `catchUpSpeed = 15.2 m/s`, `slowDownSpeed = 7.5 m/s`) based on real-time distance calculations (`catchUpDistance = 15m`, `slowDownDistance = 5m`).
   - **Cinematic Reveal Sequence**: Procedural over-the-shoulder camera pan, dolly zoom, and eye power-up lighting ignition.
   - **Level 3 Exit Alignment**: Smoothly spherical-lerps camera rotation during fly-in and re-orients player body and POV directly toward the open exit door (`-31.29905, 10.69943, 53.51`).

3. **Interactive Security & Exit System (`CollectibleItem.cs`, `LevelTransitionTrigger.cs`)**:
   - Picking up the Data Fragment triggers alarm lights, chase BGM, unlocks the exit door, and activates the AI bot.

4. **2-Option Game Over & Restart System (`GameOverManager.cs`)**:
   - Falling off the map or getting caught by the AI bot triggers a 2-option modal (**RESTART** level from 0:00 or return to **MAIN MENU**).
   - Prevents Pause Menu (`ESC`) input or character movement bypasses while active.

5. **Precision Time & Rank System (`LevelCompleteManager.cs`)**:
   - Evaluates clear time against S/A/B/C rank thresholds with high-precision time formatting (`00:00.00`).

6. **Developer Debug & Cheat Suite (`PauseMenuManager.cs`)**:
   - Built directly into the **Pause Menu (`ESC`)** under the **CHEATS** tab for testing, mechanics demonstration, and rapid debugging.
   - **Speed Multiplier**: Adjust player movement and sprint speed dynamically (`1.0x` to `3.0x`).
   - **Jump Multiplier**: Adjust jump height dynamics (`1.0x` to `3.0x`).
   - **Moon / Low Gravity**: Reduces world gravity buildup for floating jumps.
   - **Ghost Flying Mode**: Disables collisions (`CharacterController.detectCollisions = false`) and enables free 3D noclip flight control across map geometry (**Space** to fly up, **Left Ctrl** to fly down).
   - **Disable Pursuer AI**: Instantly freezes the Pursuer AI Bot in place to test level platforming without pursuit pressure.

7. **Automated Cutscene & Ending System (`EndingSceneManager.cs`)**:
   - Level 3 victory smoothly transitions directly to `Ending.unity` with automated 1.5s fade-in, 4.0s hold, and 1.5s fade-out sequence returning to Main Menu.

---

## System Requirements

- **Minimum Requirements**:
  - **OS**: Windows 10 64-bit
  - **Processor**: Intel Core i5-4460 / AMD FX-6300
  - **Memory**: 8 GB RAM
  - **Graphics**: NVIDIA GeForce GTX 760 / AMD Radeon R7 260x
  - **DirectX**: Version 11
  - **Storage**: 1.5 GB available space

- **Recommended Requirements**:
  - **OS**: Windows 10/11 64-bit
  - **Processor**: Intel Core i7-8700K / AMD Ryzen 5 3600
  - **Memory**: 16 GB RAM
  - **Graphics**: NVIDIA GeForce GTX 1060 (6GB) / AMD Radeon RX 580
  - **DirectX**: Version 12
  - **Storage**: 2.0 GB SSD space

---

## Known Issues & Troubleshooting

- **Issue 1**: Mouse sensitivity feels too high or too low.
  - *Workaround*: Adjust the **Mouse Sensitivity** slider in the **Pause Menu** or **Main Menu Settings** (saves automatically via PlayerPrefs).
- **Issue 2**: Mouse camera jump on initial level load.
  - *Workaround*: `MouseLook.cs` includes a 5-frame input spike filter upon scene load to ensure smooth camera initialization.
- **Issue 3**: Falling off the map during platforming.
  - *Workaround*: Automatic out-of-bounds detection triggers the 2-option Game Over screen to quickly restart the level.

---

## Additional Notes

- **Game Title**: VAULT & VAULT
- **Game Version**: 1.0.0 (Final Release)
- **Last Update**: August 8, 2026
- **Academic Course**: 3D Interactive Development & Game Design (Azrieli College of Engineering)
- **Contact Information**: Mohammad Othman, Ibrahim Amo, Mohammad Enaby, Nazieh Sayegh

---

## Media

### YouTube Gameplay Demonstration
[![VAULT & VAULT - Gameplay Walkthrough & Project Showcase](https://img.youtube.com/vi/-eIDERGnmZ0/maxresdefault.jpg)](https://www.youtube.com/watch?v=-eIDERGnmZ0)

▶️ **Watch the full project showcase on YouTube**: [https://www.youtube.com/watch?v=-eIDERGnmZ0](https://www.youtube.com/watch?v=-eIDERGnmZ0)

---

### Gameplay Screenshots

#### 1. Main Menu UI & Sci-Fi Aesthetic
![Main Menu UI](Media/MainMenu.png)

#### 2. How To Play & Controls Modal
![How To Play Modal](Media/HowToPlay.png)

#### 3. First-Person Parkour Mechanics & Wall Running
![Parkour Mechanics](Media/Parkour.png)

#### 4. Classified Data Fragment Interaction
![Data Fragment Pickup](Media/DataShard.png)

#### 5. High-Tech Vault Level Environments
![Vault Level Environment 1](Media/Gameplay%201.png)
![Vault Level Environment 2](Media/Gameplay%202.png)

#### 6. Pursuer AI Bot Activation & Emergency Alarms
![Pursuer AI Encounter](Media/Chasing%201.png)
![Pursuer AI High-Speed Pursuit](Media/Chasing%202.png)

#### 7. Precision Level Clear Rank Evaluation (Rank S)
![Level Victory & Rank Evaluation](Media/LevelCompleteRank.png)

#### 8. In-Game Developer Debug & Cheat Suite
![In-Game Cheat Suite](Media/CheatEngine.png)
