# CLAUDE.md

## Project

BlindDuel is a MelonLoader accessibility mod for Yu-Gi-Oh Master Duel (Unity 6 / Il2Cpp). It provides screen reader support via Tolk.dll so blind players can navigate menus, read cards, and play duels. This is a clean-room rewrite of the BlindMode mod with improved architecture.

## Build

```bash
"/c/Program Files/dotnet/dotnet.exe" build BlindDuel.csproj
```

Target: net6.0. Output: `bin/Debug/net6.0/BlindDuel.dll`.

## Architecture

### Core Pattern: Handler Registry
Each screen/menu has its own handler class implementing `IMenuHandler`. The `HandlerRegistry` auto-discovers all implementations at startup via reflection. Adding speech for a new screen = create one file in `Handlers/`.

### Speech Flow (Tolk API)
- `Speech.AnnounceScreen()` — interrupt, screen/menu changes
- `Speech.SayItem()` — interrupt, item navigation (includes description + index as one utterance)
- `Speech.SayQueued()` — queued, supplementary info
- `Speech.SayImmediate()` — interrupt, urgent (LP changes)

**Important:** Each item should be spoken as ONE complete utterance: item name + details + index. Do NOT use separate `SayDescription()` or `SayIndex()` calls — combine everything into the text returned from `OnButtonFocused()`.

### File Structure
- **Core/** — Plugin.cs (MelonMod entry), BlindDuelCore.cs (MonoBehaviour singleton), Speech.cs (speech coordination), ScreenReader.cs (Tolk P/Invoke)
- **Detection/** — ScreenDetector.cs (VC changes), DialogDetector.cs (dialog polling + patched), NavigationState.cs (current menu, focus VC)
- **UI/** — TextExtractor.cs (unified tree walker), ElementReader.cs (EOM serializedElements), TransformSearch.cs (name-based search), CardReader.cs (card data extraction from UI panels)
- **Handlers/** — IMenuHandler interface + 13 implementations (Home, Settings, Notifications, Missions, Shop, DuelPass, Deck, Duel, Solo, Title, Setup, CardBrowser, Profile, Friends)
- **Patches/** — ButtonPatches, DialogPatches, DuelPatches, NavigationPatches, BrowserPatches, SoloPatches
- **Models/** — CardData/PreviewData, DuelState, SoloState, CardBrowserState, Enums (Menu, CardAttribute, CardRarity)
- **Util/** — Log (debug logging), TextUtil (StripTags, IsBannedText), EnumUtil (parse from sprite names)

### Key Patterns
- **No global mutable state god object** — state split into NavigationState, DuelState, SoloState, CardBrowserState
- **All Harmony patches are postfix** (Il2Cpp constraint)
- **TextExtractor** is the single unified text finder — configurable via TextSearchOptions
- **ElementReader** reads game's EOM serializedElements (more reliable than hierarchy scanning)
- **Il2Cpp type casting** uses `.TryCast<T>()` from Il2CppInterop.Runtime

## Decompiled Game Source

`decompiled/` contains Assembly-CSharp.dll decompiled into individual .cs files, one per class, organized by namespace folders (e.g. `decompiled/Il2CppYgomGame.Duel/DuelClient.cs`). Use these to look up game class internals, method signatures, fields, and UI hierarchy details when adding or reworking features.

**Key locations:**
- `decompiled/Il2CppYgomGame.Duel/` — DuelClient, CardInfo, CardRoot, DuelLP
- `decompiled/Il2CppYgomSystem.UI/` — SelectionButton, ViewController, SnapContentManager
- `decompiled/Il2CppYgomSystem.ElementSystem/` — ElementObjectManager
- `decompiled/Il2CppYgomGame.Home/` — Home screen classes
- `decompiled/Il2CppYgomGame.Deck/` — Deck editor classes
- `decompiled/Il2CppYgomGame.Solo/` — Solo mode classes
- `decompiled/Il2CppYgomGame.Shop/` — Shop classes

**NEVER commit decompiled/ to git** — it contains copyrighted game code and is in .gitignore.

## Dependencies

Game assemblies referenced from `../Master-Duels-BlindMode/libs/` (relative path in csproj).

## Adding a New Screen

1. **Research first** — Read the decompiled game code for the screen's ViewController, widget classes, and data models. Understand what properties/fields are available (item names, indices, counts, details).
2. Create `Handlers/MyNewHandler.cs`
3. Implement `IMenuHandler`: `CanHandle()`, `OnScreenEntered()`, `OnButtonFocused()`
4. Build. The registry auto-discovers it. No other files need changes.

### Handler Responsibilities
- **`OnScreenEntered()`** — Called only after screen is fully loaded (`IsReadyTransition` + `!isLoading`). Read the localized header via `ScreenDetector.ReadGameHeaderText()` when possible. Return `true` to suppress generic announcement.
- **`OnButtonFocused()`** — Return the COMPLETE speech text for the item: name + description/details + index. Use game data (widget properties, VC fields) for index rather than generic `TransformSearch.GetButtonIndex()`. Return `null` only for buttons that should use default behavior.
- **One handler per screen** — Every navigable screen should have its own handler. The generic fallback in ScreenDetector is a discovery tool for unhandled screens, not a long-term solution.
- **Use localized game data** — Read text from the game's own UI elements/properties. Never hardcode English strings for content the game provides in multiple languages.

## Future Improvement: Detection & Patch Layer

The current `ScreenDetector`, `ButtonPatches`, and `TransformSearch` layer has known hacky workarounds that should eventually be rewritten using proper game APIs from decompiled source:

- **`PatchColorContainerGraphic`** hooks a low-level rendering method (`SetColor`) to detect button focus, with hardcoded parent name dictionaries. The game likely has proper selection/focus events.
- **`QueueFocusedItem`** brute-force scans every `SelectionButton` in the scene to find the focused one. The game tracks this already.
- **`TransformSearch.GetButtonIndex`** counts sibling buttons, which breaks for nested widget hierarchies (e.g. shop tabs where the button is inside a ShopTabWidget, not a sibling of other tabs).
- **Screen/button timing** requires manual coordination (`HasPendingScreen`) because button focus patches fire independently of screen announcement readiness.

Research the decompiled `SelectionButton`, `ViewControllerManager`, and `Selector` classes for cleaner alternatives that use the game's own focus tracking, selection indices, and screen lifecycle events.
