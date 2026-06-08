# 🎨 Drawn Away

<p align="center">
  <img src="https://img.itch.zone/aW1nLzI1NjY0OTI0LnBuZw==/original/E26pxH.png" alt="Drawn Away Banner" width="600"/>
</p>

<p align="center">
  <em>A 2D platformer where your imagination becomes your greatest tool.<br>Draw your way through obstacles, climb a giant, and rewrite fate — with your own hand.</em>
</p>

<p align="center">
  <a href="https://adelf.itch.io/drawn-away"><img src="https://img.shields.io/badge/Play_on-itch.io-FA5C5C?style=for-the-badge&logo=itch.io&logoColor=white" alt="itch.io"></a>
  <img src="https://img.shields.io/badge/Made_with-Unity-000000?style=for-the-badge&logo=unity&logoColor=white" alt="Unity">
  <img src="https://img.shields.io/badge/Platform-Windows_|_macOS-0078D6?style=for-the-badge&logo=windows&logoColor=white" alt="Platforms">
</p>

---

## 📖 Story

Johnny wields **magical doodling powers** — the ability to draw his creations into existence. Foretold by an ancient prophecy, he is the child destined to save his village from the looming threat of an **evil giant**. Armed with nothing but a magical pencil and his wits, Johnny must climb treacherous landscapes, solve puzzles, and confront the colossal giant to rewrite fate with his own hand.

---

## ✨ The Drawing System — *The Heart of the Game*

> This is what makes Drawn Away truly special. You don't just play the game — you **create** the platforms, tools, and shields you need to survive.

### 🖊️ How It Works

Every stroke you draw becomes a **real physical object** in the world. The drawing system is a fully-featured creative engine:

| Feature | Description |
|---------|-------------|
| **Real-Time Drawing** | Draw freely with mouse, touch, or **graphics tablet with pressure sensitivity** |
| **Physics Integration** | Each line becomes a rigidbody with auto-calculated mass, collision, and gravity |
| **Polygon Colliders** | Lines have actual **thickness** — characters can stand, walk, and climb on your drawings |
| **Multiple Tools** | Switch between different drawing tools, each with unique physical properties |
| **Ink System** | Limited ink per tool — plan your strokes wisely! Ink is refunded when lines are destroyed |
| **RDP Optimization** | Ramer-Douglas-Peucker point simplification keeps performance smooth even with long strokes |

### 🎒 Drawing Tools

| Tool | Function |
|------|----------|
| 🖊️ **Pencil** | Basic drawing — creates platforms and bridges |
| 🪢 **Rope** | Converts drawn strokes into segmented ropes with realistic physics joints |
| 🛡️ **Iron Line** | Reinforced line that can **block fireballs** — essential for boss survival |
| 🧲 **Glue** | Makes objects stick to drawn lines using FixedJoint2D |
| 🧹 **Eraser** | Removes drawn lines on contact, refunding ink |

### 🧠 Architecture

```
┌─────────────────────────────────────────┐
│  🖱️ Input (Mouse / Pen / Touch)         │
├─────────────────────────────────────────┤
│  LineManager — Detects & manages drawing│
├─────────────────────────────────────────┤
│  Line — Renders (LineRenderer)          │
│       + Physics (Edge/Polygon Collider)  │
├─────────────────────────────────────────┤
│  DrawingConfigController — Singleton    │
│  Tool selection, ink tracking, settings │
├─────────────────────────────────────────┤
│  EventManager — Global event bus        │
└─────────────────────────────────────────┘
```

---

## 🎮 Gameplay & Controls

### Player Mechanics

- **Smooth Platformer Movement** — Acceleration/deceleration with separate ground & air physics
- **Variable Jump Height** — Release early for shorter jumps, hold for maximum height
- **Coyote Time** — Forgiving 0.15s window to jump after leaving a ledge
- **Jump Buffering** — Input a jump just before landing and it'll still register
- **Double/Triple Jump** — Extra air jumps for reaching high places
- **Checkpoint System** — Respawn at checkpoints after death, lines optionally cleared

### Hazards

- ⚠️ **Spikes** — Instant death on contact
- 🕳️ **Death Zones** — Bottomless pits and kill triggers
- 💀 **Boss Attacks** — Stomps, falling stalactites, and fireballs

---

## 👹 The Giant — Boss Fight

The climactic battle is a **multi-phase vertical climb** up the body of a colossal giant. The higher you climb, the more dangerous it gets.

### Phase Progression

```
         HEAD (Y ≥ 40m)
    ┌────┴────┐  🔥 Fireball Barrage
    │  PHASE 3 │  🦶 Stomp Attack
    └────┬────┘  💎 Stalactite Rain
         │
    ┌────┴────┐
    │  PHASE 2 │  🦶 Stomp Attack
    │  (Y ≥ 25m)│  💎 Stalactite Rain
    └────┬────┘
         │
    ┌────┴────┐
    │  PHASE 1 │  🦶 Stomp Attack
    │  (Y ≥ 10m)│
    └─────────┘
```

| Phase | Height | Attacks | Strategy |
|:-----:|:------:|---------|----------|
| **1** | 10m+ | Giant Stomp | Draw platforms to climb legs, dodge alternating stomps |
| **2** | 25m+ | Stomp + Stalactites | Watch for falling rocks from above while climbing |
| **3** | 40m+ | Stomp + Stalactites + Fireballs | Use **Iron Lines** as shields against fireball barrages! |

### Boss Mechanics

- **Material Stealing** — At the start, the boss disables your drawing tools. You must reclaim them!
- **Punish System** — Getting hit teleports you back to the ground; all attacks reset
- **Cinemachine Camera Shake** — Every stomp sends a shockwave through the screen
- **Win Condition** — Reach the top trigger zone to initiate the victory sequence

---

## 🔍 Traceable Shapes — Hidden Object Revelation

Scattered throughout the world are **hidden objects** that can only be revealed by tracing them with your pencil:

1. Find an area surrounded by invisible **TracePoint** markers
2. Draw over the shape to fill a progress bar (90% threshold)
3. Watch as the hidden object **scales into view** with particle effects
4. Gain resources and unlock new paths

---

## 🗺️ Level Structure

| Scene | Description |
|-------|-------------|
| 🏠 **Start Scene** | Main menu |
| 📚 **Tutorial** | Learn drawing and movement basics |
| 🌲 **Forest Level** | Open climbing area with platforms |
| 🕳️ **Cave Level** | Underground climbing challenge |
| 👹 **Boss Level** | Multi-phase giant boss arena |
| 🎬 **Video Scene** | Cinematic cutscenes |
| 🏆 **End Scene** | Victory & credits |

---

## 💬 NPC Dialogue System

Encounter characters throughout your journey:

- 👴 **Old Man** — Cryptic guidance and lore
- 👧 **Illana** — Story companion
- 🧔 **Dude** — Additional NPC encounters

Dialogue bubbles appear when approaching NPCs, with character-specific audio and auto-hiding mechanics.

---

## 🔊 Audio

Full soundscape managed by a singleton **AudioManager**:
- Background music with volume control
- Contextual sound effects (stomps, fireballs, impacts, dialogue)
- PlayerPrefs-based volume persistence
- Per-tool collision sounds with velocity thresholds

---

## 🛠️ Technical Highlights

### Performance
- **RDP line simplification** reduces collision point count
- **Max collider points clamping** prevents expensive physics calculations
- **DOTween** object pooling for smooth animations
- **Layer-based collision filtering** (rope self-collision prevention)

### Input
- Unity **New Input System** with gamepad, keyboard, mouse, pen & touch support
- **Pressure sensitivity** detection for graphics tablets
- **EventSystem UI blocking** — can't draw over buttons by accident

### Physics
- **Continuous collision detection** on all dynamic objects
- **Auto-calculated mass** based on line geometry (length × width × multiplier)
- **FixedJoint2D** adhesive mechanics for glue tool
- **PolygonCollider2D** thick path generation for realistic platform collision

---

## 🚀 Getting Started

### Prerequisites
- Unity 6 (6000.x) with 2D support
- Windows or macOS

### Opening the Project
1. Clone the repository
   ```bash
   git clone https://github.com/osherx1/HujiXBezalel-DevCourse-Drawn-away.git
   ```
2. Open the project folder in Unity Hub
3. Open the `Start scene` and press Play!

### Building
1. Go to `File > Build Settings`
2. Select target platform (Windows/macOS)
3. Click `Build`

---

## 🎓 About This Project

This game was created as part of the **Huji × Bezalel Digital Game Development Course** — a collaboration between the Hebrew University of Jerusalem and the Bezalel Academy of Arts and Design.

Team 1 brought together programmers and artists to create a game that showcases both technical excellence and creative vision.

---

## 👥 Credits

- **Development**: Team 1 — Huji × Bezalel Dev Course 2026
- **Art & Design**: Bezalel Academy students
- **Programming**: Hebrew University students
- **Engine**: Unity

---

<p align="center">
  <em>"Your pencil is mightier than any sword." ✏️</em>
</p>

<p align="center">
  <a href="https://adelf.itch.io/drawn-away">
    <img src="https://img.shields.io/badge/🎮_Play_Drawn_Away_on_itch.io-FA5C5C?style=for-the-badge&logo=itch.io&logoColor=white" alt="Play on itch.io">
  </a>
</p>
