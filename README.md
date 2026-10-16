# Dressmaker Desk Mail Prototype — 0.2.0

Three playable, prewritten commissions, including a returning client. This is the first release that adds quests to game saves. The user has confirmed the 0.1.4 presentation changes work; the new gameplay needs in-game testing.

## Install / upgrade

1. Close Dressmaker and back up the save you will test. Keep an untouched copy: this release records real custom clients and quests. The supplied game names its slot files `player.json`, `player1.json`, etc.; back up the whole save directory, including its cache/backup files.
2. Copy this ZIP's **BepInEx** folder into the game folder, merging folders and replacing `BepInEx/plugins/DeskMailPrototype/Dressmaker.DeskMailPrototype.dll`.
3. Keep your existing configuration and Dressmaker Mod Loader **0.7.4**. Remove duplicate old copies of this plugin DLL from other plugin folders.
4. Launch and load your test save. Look for `Desk Mail 0.2.0 ready` in `BepInEx/LogOutput.log`.

All artwork is embedded. No model, server, compiler, separate story-pack installation, or additional runtime is needed. The build uses BepInEx 5.4.23.5 and HarmonyX 2.9.0.0, with the supplied game assembly as the compatibility reference.

## Test commissions

| Client / occasion | Exact scored requirements | Payment | Availability |
| --- | --- | ---: | --- |
| Juniper Reed — An evening among the stars | Quality ≥ 40; blue fabric area ≥ 45% | 600 | Initially available |
| Beatrice Briar — A rose with excellent timing | Quality ≥ 40; green fabric area ≥ 45% | 650 | Initially available |
| Juniper Reed — A little moonlight for everyone | Quality ≥ 50; blue area ≥ 35%; white area ≥ 15% | 850 | Successful delivery of Juniper's first request |

Percentages use the game's colour categories and fabric area scoring. A mention of silver, ivory, decoration, sleeves, or a particular hem is creative guidance; only the listed requirements determine success. Each playable letter has an additional **Our agreement** page generated from the same definitions used for scoring.

The other four correspondents remain writing samples. Their rows say **Sample**, and their readers have a disabled **Correspondence sample** control. There are six letters initially, then seven when Juniper's follow-up arrives.

## How to play

- Open the holder or press **F8** at an idle desk.
- Choose Juniper or Beatrice. **Read agreement** jumps to the exact requirements. **Take measurements** opens the normal measuring screen. Confirm that sheet to accept the commission.
- Measurements use the game's existing body silhouette and measuring tools. Clients use mail artwork as their small UI icon; there are no new face variants or customer dialogue scenes.
- Continue through the ordinary design, cutting, sewing, and delivery controls. Multiple commissions can remain active. A letter marked **In work** offers **Continue commission**, which uses the game's commission-switching function.
- Successful delivery shows a thank-you letter, awards the native payment and progression, archives the request, and saves. Juniper's first success also makes her second request appear.
- Unmet requirements show an alteration letter naming the outstanding rules. The same commission and dress stay active; no payment or completion is recorded. Adjust and submit again.

Closing a reply returns control to the normal desk presentation sequence. Original story requests and the princess's final letter retain their native flow.

## First play-test

See **docs/PLAYTEST.md** for a short sequence covering browsing, accepting both clients, changing the active dress, restarting during work, adjustment requests, successful payment, follow-up unlocking, and restarting after completion. Please start with Juniper's first letter and a save/reload before investing a full dressmaking session.

## Existing presentation and controls

The approved 300-unit holder, left-of-bell placement, red unread pulse, opaque parchment inbox, centered text, three stationery families, wrapping, scrolling, and optional 0.95-second opening animation are retained. No art assets were changed.

Wheel/drag scrolls. Previous/Next or Page Up/Down turns pages. Escape returns to the inbox, or puts a delivery reply away. F8 closes mail. The holder remains a desk prop while input is blocked by dialogue, pause, or our reader; it disappears when the desk itself disappears.

Configuration remains in `BepInEx/config/community.dressmaker.deskmailprototype.cfg`. Existing holder offsets, width, stationery selection, `PulseUnread`, and `AnimateOpening` are preserved. `General.Enabled=false` hides the inbox/offers but still registers saved definitions and handles deliveries of already accepted mail commissions.

## Save compatibility and removal

Accepted quests, measured clients, dresses, payments, and successful chain progress use the game's native save. Read/unread markers are session-only and reset on a restart or save change. Delivery reply text is shown at delivery; it is not a separate saved letter archive. Completed requests remain readable but cannot be accepted again.

Keep this plugin installed for a save containing its custom quests or clients, including completed ones. The definitions are registered at startup; they are **not** cached as a standalone framework story pack. Removing the DLL or downgrading to 0.1.4 leaves unresolved references. To return to the earlier presentation-only version, restore the untouched pre-test save as well as the earlier DLL. Do not load and resave a quest-bearing save with the plugin missing.

## Validation

Release compilation has zero warnings/errors. **39 standalone behavior/regression checks** and **72 static native-contract checks** pass. Source, artwork, tests, hashes, build evidence, and technical notes are included. **This build has not been run inside Dressmaker here**; native patch installation, save round-tripping, scoring, rewards, and UI appearance remain play-test checks.

For a problem, send `BepInEx/LogOutput.log`, the letter title, the last action, and whether it happened before or after restarting. For a delivery error, stop retrying and keep the log: the mod deliberately blocks automatic repeat payment after a native completion error.

Project direction and holder artwork: Miguel Bojorquez. Code, correspondence, and stationery: ChatGPT / Codex, in collaboration with Miguel. The holder retains its existing rights. Original code/documentation use the included MIT license. No game fonts, native artwork, or third-party assemblies are redistributed. Stationery provenance is in `docs/STATIONERY.md`.
