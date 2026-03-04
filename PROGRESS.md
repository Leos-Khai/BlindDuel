# BlindDuel — Rewrite Progress

## Status: Phase 5 — All Phases Complete

## Completed
- [x] Phase 1: Foundation (project setup, core files, GitHub repo)
- [x] Phase 2: Detection & UI extraction layer
- [x] Phase 3: Patches, models, handler infrastructure
- [x] Phase 4: All menu handlers + Speech API rewrite
- [x] Phase 5: Polish & verification

## Architecture Summary

### Speech Flow (Tolk-based)
- `Speech.AnnounceScreen(text)` — interrupts, for screen/menu changes
- `Speech.SayItem(text)` — interrupts, for item navigation
- `Speech.SayIndex(current, total)` — queued after item name
- `Speech.SayDescription(text)` — queued after item + index
- `Speech.SayQueued(text)` — queued, for supplementary info
- `Speech.SayImmediate(text)` — interrupts, for urgent info (LP changes)

### Handler Pattern
- Implement `IMenuHandler` in a new file under `Handlers/`
- `HandlerRegistry` auto-discovers via reflection at startup
- `CanHandle(vcName)` matches ViewController names
- `OnButtonFocused(button)` returns enhanced text or null for default
- Adding a new screen = one new file, zero changes elsewhere

### File Structure
- `Core/` — Plugin, BlindDuelCore, Speech, ScreenReader
- `Detection/` — ScreenDetector, DialogDetector, NavigationState
- `UI/` — TextExtractor, ElementReader, TransformSearch
- `Handlers/` — IMenuHandler + 13 handler implementations
- `Patches/` — Button, Dialog, Duel, Navigation, Browser, Solo patches
- `Models/` — CardData, PreviewData, DuelState, SoloState, CardBrowserState, Enums
- `Util/` — Log, TextUtil, EnumUtil

## Known TODOs
- [ ] Card info reading (CopyUI equivalent) — CardData extraction from UI paths
- [ ] CardInfo.SetDescriptionArea patch → schedule card reading via DuelHandler
- [ ] DuelListCard click → card info via DuelHandler
- [ ] Preview elements OnClick → card info reading with delay
- [ ] ProcessDailyReward equivalent (not yet ported)
- [ ] Test in-game with actual Master Duel
