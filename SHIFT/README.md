# SHIFT 🔷

> A mobile puzzle game where you resize objects using **perspective** — your eyes are your superpower.

## Concept
SHIFT is a first-person daily puzzle game. You pick up objects and move them closer or further from surfaces — the change in perspective makes them physically grow or shrink. Fit the object into the goal zone to win.

**Core formula:**
```
newScale = baseScale × (raycastDistance / pickupDistance)
newMass  = baseMass  × (currentScale³)
```

---

## Tech Stack
| Area | Technology |
|---|---|
| Engine | Unity 2022.3 LTS (URP) |
| Language | C# |
| Backend | Firebase (Auth + Firestore) |
| Animations | DOTween |
| Input | Unity Input System (new) |
| Build | IL2CPP / ARM64 |

---

## Project Structure
```
Assets/
└── Scripts/
    ├── Core/
    │   ├── PerspectivePickup.cs   ← THE mechanic
    │   ├── ShiftObject.cs         ← Interactable object behaviour
    │   ├── GoalZone.cs            ← Win trigger
    │   ├── PlayerController.cs    ← FPS movement + look
    │   ├── GameManager.cs         ← State machine + timer
    │   └── AudioManager.cs        ← All sound events
    ├── Levels/
    │   ├── LevelGenerator.cs      ← Procedural room builder (Phase 2)
    │   ├── DailySeed.cs           ← Seed formula + Firebase fetch (Phase 4)
    │   └── ObjectDatabase.cs      ← ScriptableObject registry (Phase 2)
    ├── Backend/
    │   └── FirebaseManager.cs     ← Auth, Firestore, leaderboard (Phase 4)
    ├── Rewards/
    │   ├── StreakManager.cs        ← Daily streak (Phase 6)
    │   ├── RewardSystem.cs         ← Shard grants (Phase 6)
    │   └── IAPManager.cs           ← Unity IAP (Phase 9)
    └── Utilities/
        └── Constants.cs            ← All magic strings + numbers
```

---

## Development Phases

| Phase | Weeks | Goal |
|---|---|---|
| **1 — Core Mechanic** ✅ | 1-2 | PerspectivePickup working in test scene |
| 2 — Room Generation | 2-3 | LevelGenerator from seed |
| 3 — Goal & Game Loop | 3 | Full puzzle start to finish |
| 4 — Firebase Backend | 4 | Daily seed, auth, leaderboard |
| 5 — UI Screens | 4-5 | Menu, HUD, Win screen |
| 6 — Rewards & Streaks | 5-6 | Shards, streaks, chest animation |
| 7 — Art & Audio | 6-7 | Final models, materials, sounds |
| 8 — Leaderboard | 7 | Live leaderboard on win screen |
| 9 — IAP & Shop | 7-8 | Shard bundles in Sandbox |
| 10 — QA & Performance | 8 | 55+ FPS on Galaxy A32 |

---

## Phase 1 Test Scene Setup
1. Create a new URP 3D scene
2. Add a **Plane** (floor) on layer `Surface`
3. Add a few **Cubes**, tag them `ShiftObject`, add `Rigidbody` + `ShiftObject` component
4. Add **Player** with `CharacterController` + `PlayerController`
5. Add **Camera** (child of Player) + `PerspectivePickup`
   - Set `Interactable Layer` → Interactable
   - Set `Surface Layer` → Surface
6. Add **GoalZone** (Box Collider trigger) + `GoalZone` script
7. Add **GameManager** and **AudioManager** to an empty GameObject

**Pass condition:** Pick up a cube → move toward/away from a wall → drop it. Object must stay scaled and feel magical.

---

## Git Workflow
```bash
git add .
git commit -m "feat: describe your change"
git push origin main
```

---

*Bundle ID: `com.polabathina.shift` — use this in Unity Player Settings, Firebase Console, Google Play, and App Store Connect.*
