# Project Aegis

> *"You don't fight the war. You build the machine that wins it."*

A 2D strategy and management simulation for PC (Steam). Run a defense technology company in a fictional near-future world — hire engineers, fund research, sign government contracts, and navigate a world in permanent geopolitical flux. You never control combat. Every decision is made from the boardroom.

---

## Status

**Pre-launch / feature-complete (M6).** All six planned milestones are implemented, well ahead of the original 48-week schedule. The core loop — hire, research, contract, deliver, grow — is fully playable start to finish.

Three items are open before launch sign-off:

- Audio assets are not yet sourced (code and mixer routing are complete; see [Known Gaps](#known-gaps))
- Final QA reconciliation of the bug tracker
- Approved copy verification for win condition and tutorial text

---

## Core Concept

The player grows a company through three phases — **Garage Startup → Regional Contractor → Global Defense Giant** — by balancing five interlocking pressures:

| Pressure | Description |
|---|---|
| Research | Unlock new technology across four branches (Drone, AI, Cyber, Space) |
| Finances | Manage budget, revenue, and company valuation |
| Reputation | Build industry trust to unlock better contracts |
| Employees | Hire, assign, and retain staff across four roles |
| Market Competition | Respond to four rival corporations and a living world |

Full design details live in [`02_Game_Design_Document.md`](./02_Game_Design_Document.md).

---

## Tech Stack

| | |
|---|---|
| **Engine** | Unity 6.1 |
| **Language** | C# |
| **UI** | Unity UI Toolkit (UXML/USS) |
| **Serialization** | Newtonsoft JSON |
| **Animation** | DOTween (non-UITK elements only) + USS transitions |
| **Architecture** | Manager pattern with a central event bus; ScriptableObject-driven content |
| **Platform** | PC (Steam) |

See [`03_Technical_Architecture.md`](./03_Technical_Architecture.md) for the full system breakdown.

---

## Project Structure

```
Assets/
├── _Project/
│   ├── Scripts/
│   │   ├── Core/           # TimeManager, BootController, SettingsManager
│   │   ├── Systems/        # Manager classes (Employee, Research, Contract, etc.)
│   │   ├── Data/            # ScriptableObject definitions
│   │   ├── Models/          # Runtime state classes
│   │   ├── Save/            # GameSaveData and per-manager save/load
│   │   ├── UI/               # Panels, HUD controller, notifications
│   │   └── Utilities/       # AegisConstants, SceneLoader, extensions
│   ├── Data/                # Authored SO assets (research, products, contracts, events)
│   ├── Prefabs/
│   ├── Scenes/               # Boot → MainMenu → Game
│   ├── UI/                   # USS, UXML, fonts, sprites
│   └── Audio/
└── Plugins/
```

---

## Core Systems

| System | Description |
|---|---|
| **TimeManager** | Drives the weekly simulation tick (pause / 1x / 2x / 4x) |
| **EmployeeManager** | Hiring pool, roster, stats, traits, phase-weighted candidate generation |
| **ResearchManager** | 17-node research tree across 4 branches, live researcher stat progression |
| **ContractManager** | Offer generation, acceptance, explicit engineer assignment, risk-based delivery |
| **FinanceManager** | Cash balance, salary deductions, revenue tracking |
| **ReputationManager** | 5-tier reputation score, drives contract access and hiring phase |
| **WorldEventManager** | 4 event types, demand/reward modifiers, weighted contract generation |
| **RivalManager** | Lightweight progress model for 4 rival corporations |
| **MarketManager** | Branch-level market share derived from research + rival progress |
| **WinConditionManager** | Financial / Technology / Market victory conditions |
| **TutorialController** | Five-beat guided onboarding sequence, skippable |
| **SaveManager** | JSON save/load, 1 autosave + 3 manual slots, version-gated compatibility |

---

## Documentation

| File | Contents |
|---|---|
| [`01_Project_Vision.md`](./01_Project_Vision.md) | Core fantasy, design pillars, non-goals |
| [`02_Game_Design_Document.md`](./02_Game_Design_Document.md) | Full gameplay systems and MVP scope |
| [`03_Technical_Architecture.md`](./03_Technical_Architecture.md) | Engine architecture, manager pattern, save system |
| [`04_Art_Direction.md`](./04_Art_Direction.md) | Visual identity, palette, typography, UI components |
| [`05_Lore_and_Worldbuilding.md`](./05_Lore_and_Worldbuilding.md) | Setting, rival corporations, world events |
| [`06_Feature_Backlog.md`](./06_Feature_Backlog.md) | MVP and post-launch feature tracking, QA log, milestones |
| [`07_Development_Log.md`](./07_Development_Log.md) | Session-by-session development history |
| [`08_Coding_Standards.md`](./08_Coding_Standards.md) | C# conventions, file organization, commit format |
| [`09_Design_Decisions.md`](./09_Design_Decisions.md) | Recorded decisions (DD-01 through DD-16) with rationale |

---

## Known Gaps

These are documented and intentional — not oversights:

- **Audio** — `AudioManager` is fully wired to gameplay events and mixer volume controls; no clips are imported yet. See sourcing brief in dev log.
- **World map art** — World panel renders event data as text; 2D political map visualization is deferred pending art assets.
- **Market panel charting** — Branch share renders as bars; historical time-series charting is deferred.
- **Company rename** — Player cannot rename their company at New Game for MVP (cosmetic, low priority).

---

## Getting Started

1. Clone the repository
2. Open with **Unity 6.1** or later
3. Open `Assets/_Project/Scenes/Boot.unity`
4. Press Play — boot sequence transitions to Main Menu automatically

**Required packages** (installed via Unity Package Manager):
- Newtonsoft JSON (`com.unity.nuget.newtonsoft-json`)
- DOTween (Demigiant, free tier — run the DOTween Setup utility after import)

---

## Coding Standards

All contributions follow [`08_Coding_Standards.md`](./08_Coding_Standards.md):

- PascalCase for types/methods/properties, `_camelCase` for private fields
- One class per file, organized by responsibility
- Managers communicate via C# events — never direct method calls between managers
- All tunable values live in `AegisConstants.cs` — no magic numbers
- All game content is ScriptableObject-driven, read-only at runtime

Commit format:
```
[SYSTEM] Short description of what changed
```
Where `SYSTEM` is one of: `CORE`, `EMPLOYEE`, `RESEARCH`, `CONTRACT`, `FINANCE`, `REPUTATION`, `WORLD`, `RIVAL`, `MARKET`, `UI`, `SAVE`, `DATA`, `FIX`, `DOCS`.

---

## License

*Add license information here before public release.*
