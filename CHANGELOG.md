# Changelog

## Pack Opening
- Fixed face-down cards no longer spoil card names before flipping
- Added card flip announcement (name + rarity) when card finishes flipping
- Fixed result screen now reads card name, rarity, and New/Owned status
- Result screen uses BindingCardMaterial.m_CardId for reliable card identification

## Duel Pass
- Added item detail popup speech (shows item name + description when clicking a reward)
- Fixed reward items now read item type from ConsumeIcon sprites via ItemUtil

## Card Browser
- Suppressed "Related Cards" button from being announced

## CP/Gem Reading (P Key)
- Fixed P key now reads correct values using game APIs
- Deck editor: reads actual CP balance per rarity via ItemUtil.GetHasItemQuantity()
- Shop: reads gem count via ItemUtil.GetHasTotalGem()
- Restricted P key to deck editor and shop only (no longer fires globally)

## Duel
- Changed phase change announcements (Draw Phase, Battle Phase, etc.) from interrupt to queued speech
