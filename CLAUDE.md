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
- `Speech.SayItem()` — interrupt, item navigation
- `Speech.SayIndex()` — queued after item name ("2 of 5")
- `Speech.SayDescription()` — queued after item + index (help text, details)
- `Speech.SayQueued()` — queued, supplementary info
- `Speech.SayImmediate()` — interrupt, urgent (LP changes)

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

1. Create `Handlers/MyNewHandler.cs`
2. Implement `IMenuHandler`: `CanHandle()`, `OnScreenEntered()`, `OnButtonFocused()`
3. Build. The registry auto-discovers it. No other files need changes.
