# Reigns Framework for Unity

A designer-first framework for building Reigns-style swipe card games in Unity. Cards are ScriptableObjects. Conditions, outcomes, and scheduling are all inspector-driven. No code required to build content.

---

## Requirements

- Unity **2021.3 LTS** or newer
- **TextMeshPro** (for UI text labels)
- `[SerializeReference]` support — available since Unity 2019.3

---

## Installation

Drop the `ReignsFramework/` folder anywhere inside your project's `Assets/` directory. Unity will compile everything automatically.

The `Editor/` subfolder is excluded from builds automatically by Unity's standard conventions.

---

## Quick Start

### 1. Generate starter assets

Go to **Tools → Reigns Framework → Create Starter Assets**.

This creates an `Assets/ReignsGame/` folder containing:

- A `GameConfig` asset wired to everything below
- Four starter `StatDefinition` assets (Church, People, Army, Treasury)
- An empty `FlagRegistry`
- A single `DefaultPool`

### 2. Set up your scene

Add these MonoBehaviours to a `GameManager` GameObject in your scene and assign their inspector references:

| Component | Assign |
|---|---|
| `GameStateManager` | `GameConfig` |
| `CardScheduler` | `GameStateManager`, `GameConfig` |
| `CardPlayer` | `GameStateManager`, `CardScheduler` |

### 3. Wire up the UI

| Component | Where | Assign |
|---|---|---|
| `CardView` | Card prefab root | `CardPlayer` |
| `SwipeInputHandler` | Drag area (full-screen RectTransform) | `CardPlayer` |
| `StatHudView` | HUD root | `GameStateManager` + your `StatBarView` array |
| `DeathScreenView` | Death screen panel | `GameStateManager`, `GameConfig` |

### 4. Create content

Use the **Reigns/** submenu in the Project window Create menu to make cards, characters, and pools. Add them to your `GameConfig` and you're playing.

---

## Architecture Overview

The framework is split into four layers that have no upward dependencies — data doesn't know about runtime, runtime doesn't know about UI.

```
┌─────────────────────────────────────────┐
│  UI Layer                               │
│  CardView · SwipeInputHandler           │
│  StatBarView · DeathScreenView          │
├─────────────────────────────────────────┤
│  Runtime Layer                          │
│  GameStateManager · CardScheduler       │
│  CardPlayer                             │
├─────────────────────────────────────────┤
│  Data Layer  (ScriptableObjects)        │
│  CardData · CardPoolData · CharacterData│
│  StatDefinition · FlagRegistry          │
│  GameConfig                             │
├─────────────────────────────────────────┤
│  Editor Layer  (Editor builds only)     │
│  ReignsEditors · StarterAssetWizard     │
└─────────────────────────────────────────┘
```

---

## Asset Reference

### `GameConfig`
_Create → Reigns → Game Config_ · One per game.

The top-level wiring asset. References all `StatDefinition`s, the `FlagRegistry`, and all `CardPoolData` assets. Both `GameStateManager` and `CardScheduler` read from this at startup.

---

### `StatDefinition`
_Create → Reigns → Stat Definition_ · One per stat.

| Field | Description |
|---|---|
| `statId` | Unique string key. Never change after shipping — used in save data. |
| `displayName` | Shown in the HUD. |
| `startingValue` | Value at the start of each run (0–100). |
| `minValue` / `maxValue` | Clamped range. |
| `killAtMin` / `killAtMax` | Whether reaching the boundary ends the run. |
| `deathMessageMin/Max` | Text shown on the death screen. |
| `icon` / `barColour` | Visual appearance in the HUD. |

---

### `FlagRegistry`
_Create → Reigns → Flag Registry_ · One per game.

The single source of truth for all flag names. Every flag used anywhere in card conditions or outcomes **must** be declared here first. The custom inspector will warn you about duplicate IDs.

| Field | Description |
|---|---|
| `flagId` | Unique string key. Never change after shipping. |
| `scope` | `Run` — resets on death. `Campaign` — persists across runs. |
| `defaultValue` | Starting value for a new run (or new campaign for Campaign-scope flags). |

---

### `CharacterData`
_Create → Reigns → Character_

This is where **artists work**. Designers reference a `CharacterData` from each `CardData` — they never touch the art fields directly.

| Field | Description |
|---|---|
| `portrait` | Main portrait sprite displayed on the card. |
| `animatorController` | Optional — overrides the portrait with an animated version. |
| `backgroundSprite` | Scene/background image behind the character. |
| `ambientClip` | Audio loop played while this character's card is shown. |
| `dealSounds` | Random clip played when the card is dealt. |
| `portraitOffset` / `portraitScale` | Framing controls so artists can reposition art without changing prefabs. |

---

### `CardData`
_Create → Reigns → Card_ · The main designer asset.

Each card has a **prompt**, a **character**, a **left outcome**, a **right outcome**, **scheduling settings**, and optionally a list of **variants**.

#### Outcomes

Each outcome (left swipe / right swipe) contains:

| Field | Description |
|---|---|
| `swipeHintText` | Short label shown while the player is mid-swipe. |
| `responseText` | Text shown after the player commits. |
| `statDeltas` | Array of stat changes applied on commit. |
| `conditionalOverrides` | Replaces the base deltas when conditions are met (first match wins). |
| `flagEffects` | Flags to Set, Clear, Increment, or Decrement. |
| `schedulingEffects` | Cards or pools to affect (see Scheduling Effects below). |

#### Scheduling settings

| Field | Description |
|---|---|
| `recurrence` | `OneShot`, `Repeatable`, or `Cycling` (see Recurrence Types). |
| `cooldownTurns` | Minimum turns before this card can appear again. `0` = no cooldown. |
| `weight` | Relative likelihood of being chosen within its pool (1–100). |
| `forcePriority` | If true, this card jumps to the front of the draw order whenever its conditions are met. |
| `conditions` | Array of conditions that must all pass for the card to be eligible. |

#### Variants (Cycling only)

When `recurrence = Cycling`, the base card is shown on the first visit. Each entry in the `variants` array covers the 2nd, 3rd, ... visit. Variants can override prompt text, the left outcome, the right outcome, or any combination. If visits exceed the number of variants, the last variant repeats.

---

### `CardPoolData`
_Create → Reigns → Card Pool_

A named, weighted collection of cards. The scheduler evaluates pool-level conditions before looking at individual cards inside the pool. Pools can be enabled or disabled at runtime by outcome effects.

| Field | Description |
|---|---|
| `poolId` | Unique string key. |
| `cards` | List of `CardData` assets in this pool. |
| `poolConditions` | Conditions that must pass for any card in this pool to be eligible. |
| `poolWeight` | Relative likelihood of this pool being selected vs other active pools. |
| `enabledByDefault` | Whether this pool is active at the start of a run. |

---

## Conditions

Conditions gate whether a card or pool is eligible to appear. Add them via the **inspector buttons** on `CardData` and `CardPoolData` — you do not need to know the class names.

Every condition has an **Invert** checkbox that negates the result.

| Condition | What it checks |
|---|---|
| **Stat Range** | A stat's current value is between `minValue` and `maxValue` (inclusive). |
| **Flag** | A flag satisfies a comparison: `==`, `!=`, `>`, `<`, `>=`, `<=`. |
| **Turn Range** | The current turn number is within `firstTurn`–`lastTurn` (`0` = no limit). |
| **AND** | All child conditions pass. Child conditions can be any type, including other ANDs and ORs. |
| **OR** | At least one child condition passes. |

---

## Scheduling Effects

Outcomes can trigger scheduling effects that control the flow of future cards:

| Effect | What it does |
|---|---|
| `ForceNext` | The specified card will be the very next card shown, bypassing all other logic. |
| `QueueWithPriority` | The specified card is added to a priority queue, shown before normal pool draws. |
| `EnablePool` | Activates a pool so its cards become eligible. |
| `DisablePool` | Deactivates a pool so none of its cards can appear. |
| `RemoveCardFromRun` | Permanently removes a specific card from all pools for the rest of this run. |

---

## Card Recurrence Types

| Type | Behaviour |
|---|---|
| `OneShot` | Shown once per run, then permanently removed from the eligible set. |
| `Repeatable` | Can appear any number of times, subject to `cooldownTurns`. |
| `Cycling` | Shows the base card on the first visit, then works through `variants` on each subsequent visit. Loops at the last variant. |

---

## Scheduler Priority Order

Each turn the `CardScheduler` selects a card using this strict priority:

1. **ForceNext** — a card explicitly forced by the previous outcome
2. **Priority queue** — cards queued via `QueueWithPriority`, in FIFO order
3. **ForcePriority cards** — any eligible card with `forcePriority = true` (first found wins)
4. **Weighted pool draw** — a pool is selected by `poolWeight`, then a card within it by `weight`

If no eligible card exists, the scheduler logs a warning and returns `null`. Make sure at least one pool always has an eligible repeatable card to avoid dead ends.

---

## Run vs Campaign State

All flags and stats have a **scope**:

- **Run scope** — resets every time the player dies and a new run begins. Use this for story flags, relationship states, and anything that should reset with each monarch.
- **Campaign scope** — survives death and persists across runs. Use this for the overarching mystery, unlocked content, or how many times the player has died.

Set scope in the `FlagRegistry`. Campaign state is persisted with `PlayerPrefs` using JSON serialisation. To use a different save backend, replace `SaveCampaignState()` and `LoadCampaignState()` in `GameStateManager`.

---

## Extending the Framework

### Adding a new condition type

```csharp
[Serializable]
public class YearEvenCondition : CardCondition
{
    public override bool Evaluate(GameStateManager state)
    {
        return ApplyInvert(state.CurrentTurn % 2 == 0);
    }
}
```

Then add a button for it in the `CardDataEditor` and `CardPoolDataEditor` classes in `ReignsEditors.cs`.

### Customising card animations

Subclass `CardView` and override `PlayEnterAnimation()` and `PlayExitAnimation()`. The base implementations are virtual coroutine launchers — replace them with DOTween, LeanTween, or a full animation state machine without touching anything else.

```csharp
public class MyCardView : CardView
{
    protected override void PlayEnterAnimation()
    {
        // your animation here
    }
}
```

### Custom save system

Replace the two methods in `GameStateManager`:

```csharp
private void SaveCampaignState() { /* your backend */ }
private void LoadCampaignState() { /* your backend */ }
```

Everything else in the framework is unaffected.

### Adding new scheduling effect types

Add a value to the `CardEffectType` enum in `OutcomeData.cs`, then handle the new case in `CardScheduler.ApplySchedulingEffects()`.

---

## File Structure

```
ReignsFramework/
├── Data/
│   ├── GameConfig.cs           — top-level wiring asset
│   ├── StatDefinition.cs       — one per stat
│   ├── FlagRegistry.cs         — central flag dictionary
│   ├── CardConditions.cs       — all condition types
│   ├── OutcomeData.cs          — stat deltas, flag effects, scheduling effects
│   ├── CharacterData.cs        — art and audio per character
│   ├── CardData.cs             — the main card asset
│   └── CardPoolData.cs         — named card collections
├── Runtime/
│   ├── GameStateManager.cs     — stat values, flags, save/load
│   ├── CardScheduler.cs        — picks the next card each turn
│   └── CardPlayer.cs           — mediates input and applies outcomes
├── UI/
│   ├── CardView.cs             — renders a card; subclass to customise animations
│   ├── SwipeInputHandler.cs    — translates drag/touch into CardPlayer calls
│   ├── StatBarView.cs          — individual stat bar + StatHudView container
│   └── DeathScreenView.cs      — end-of-run screen with auto-restart
└── Editor/
    └── ReignsEditors.cs        — custom inspectors + StarterAssetWizard
```

---

## Designer Workflow Summary

1. **Define stats** — create a `StatDefinition` per stat, set starting values and death conditions.
2. **Define flags** — add every flag to the `FlagRegistry` with the correct scope before referencing it anywhere.
3. **Create characters** — hand off `CharacterData` assets to artists; they fill in the art fields independently.
4. **Create pools** — organise cards into thematic `CardPoolData` groups and add pool-level conditions where needed.
5. **Create cards** — fill in prompt, pick a character, author outcomes (deltas, flags, scheduling effects), set conditions and recurrence.
6. **Register everything** — make sure all pools are listed in `GameConfig.pools`.
7. **Test** — enable `verboseSchedulerLogging` on `GameConfig` to see every scheduler decision in the console.

---

## Common Pitfalls

**A card never appears** — check that its pool is enabled (by default or by an effect), that all pool-level and card-level conditions can actually be satisfied with the current game state, and that `cooldownTurns` isn't longer than your average run.

**A flag has no effect** — the `flagId` string on the `FlagEffect` must exactly match an entry in the `FlagRegistry`. The registry editor warns about duplicates but not missing references.

**The game deadlocks** — every run needs at least one `Repeatable` card with no conditions in a default-enabled pool. Treat this as your floor of ambient filler content that can always appear.

**Campaign flags reset unexpectedly** — verify the flag's `scope` is set to `Campaign` in the `FlagRegistry`, not `Run`.

**`statId` or `flagId` changed after shipping** — these strings are keys in save data. Renaming them silently orphans existing player saves. Treat them as immutable once a build is in players' hands.
