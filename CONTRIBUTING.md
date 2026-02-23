# Git Branching & Commit Guide — SHIFT

## Branch Strategy

```
main
 └── develop
      ├── feature/phase-1-core-mechanic     ← You are here
      ├── feature/phase-2-level-generator
      ├── feature/phase-3-game-loop
      ├── feature/phase-4-firebase
      ├── feature/phase-5-ui
      ├── feature/phase-6-rewards-streaks
      ├── feature/phase-7-art-audio
      ├── feature/phase-8-leaderboard
      ├── feature/phase-9-iap-shop
      └── feature/phase-10-qa-performance
```

| Branch | Purpose |
|---|---|
| `main` | Stable, shippable code only. Merge from `develop` after each phase passes QA. |
| `develop` | Integration branch. All features merge here first. |
| `feature/*` | One branch per GDD development phase. |
| `hotfix/*` | Emergency fixes applied directly to `main` + backported to `develop`. |

---

## Commit Message Format

```
<type>(<scope>): <short description>

[optional body]
[optional footer]
```

### Types
| Type | When to use |
|---|---|
| `feat` | New feature or script |
| `fix` | Bug fix |
| `refactor` | Code restructure, no behaviour change |
| `test` | Adding or updating tests |
| `docs` | README, comments, CONTRIBUTING |
| `chore` | Build, config, `.gitignore` changes |
| `perf` | Performance improvement |
| `style` | Formatting only |

### Examples
```bash
feat(core): add PerspectivePickup raycast with perspective scale formula
fix(goalzone): use CompareTag instead of layer check per GDD recommendation
refactor(gamemanager): extract timer logic into TimerService
docs(readme): add Phase 1 test scene setup instructions
chore: add Unity .gitignore
```

---

## Workflow (Each Phase)

```bash
# 1. Start a new phase
git checkout develop
git pull origin develop
git checkout -b feature/phase-N-description

# 2. Work and commit often
git add Assets/Scripts/Core/MyNewScript.cs
git commit -m "feat(core): implement MyNewScript with X behaviour"

# 3. Finish phase — merge into develop
git checkout develop
git merge --no-ff feature/phase-N-description -m "merge: Phase N complete"
git push origin develop

# 4. After QA passes — release to main
git checkout main
git merge --no-ff develop -m "release: Phase N — description"
git tag -a vN.0 -m "Phase N release"
git push origin main --tags
```

---

## Current Branch Status

| Branch | Status |
|---|---|
| `main` | ✅ Phase 1 scripts committed |
| `develop` | ✅ Synced with main |
| `feature/phase-1-core-mechanic` | 🔨 Active development |
