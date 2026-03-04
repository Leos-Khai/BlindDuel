# BlindDuel — Rewrite Progress

## Status: Phase 1 Complete

## Completed
- [x] Phase 1: Foundation
  - Project structure (Core/, Detection/, UI/, Handlers/, Patches/, Models/, Util/)
  - BlindDuel.csproj with lib references
  - Core/Plugin.cs — MelonMod entry point
  - Core/ScreenReader.cs — Tolk P/Invoke wrapper
  - Core/BlindDuelCore.cs — MonoBehaviour singleton with Update loop
  - Core/Speech.cs — Priority-based speech pipeline (replaces queueNextSpeech/pendingButtonText)
  - Util/DebugLog.cs — Thread-safe file logging
  - Util/TextUtil.cs — StripTags, IsBannedText
  - Models/Enums.cs — Menu, CardAttribute, CardRarity enums
  - Detection/NavigationState.cs — Navigation tracking state
  - Detection/ScreenDetector.cs — Stub
  - Detection/DialogDetector.cs — Stub
  - Handlers/IMenuHandler.cs — Handler interface
  - Handlers/HandlerRegistry.cs — Auto-discovery registry

## In Progress
- [ ] Phase 2: Detection & UI extraction

## Remaining
- [ ] Phase 3: Handler infrastructure & patches
- [ ] Phase 4: All menu handlers (full parity)
- [ ] Phase 5: Polish & verification

## Architecture Notes
- Speech.Say(text, priority) queues messages; Speech.FlushPending() processes them in priority order each frame
- HandlerRegistry auto-discovers IMenuHandler implementations via reflection
- Detection runs before speech flush in Update(), ensuring headers speak before buttons
- No global mutable state god object — state split into NavigationState, Speech, and per-handler state
