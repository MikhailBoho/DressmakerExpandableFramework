# Validation record — 0.2.0

The user confirmed the previous 0.1.4 presentation changes work in Dressmaker. This release adds gameplay; no in-game run of 0.2.0 has been performed here.

Completed in the build environment:

- Release DLL compiles with warnings treated as errors: zero warnings/errors.
- 39 standalone C# checks pass. The previous 29 presentation/state/layout checks remain. New checks cover exact scored terms, two initial offers, the four non-playable samples, locked-letter visibility/unread counts, success-only follow-up gating, active/completed restoration policy, receipt isolation, save-change read-state reset, duplicate delivery exclusion, retry after adjustment, and an interrupted reward callback.
- 72 exact metadata/native-call checks pass against the supplied Assembly-CSharp.dll and DressmakerMods.dll. These include loader registry/factory signatures, measurement and switch methods, native scoring, nested outcome type, saved lists, fields/containers, and the native completion/archive/save call chain. The earlier UI contract audit is also included.
- The plugin is managed IL-only AnyCPU, version 0.2.0.0. Assets retain their existing bytes, including the user's holder. Only our DLL is packaged as executable code.
- Release source, artwork resources, output hashes and ZIP contents are checked before delivery.

Boundaries: standalone tests exercise pure policy/state logic, not Unity or actual save serialization. Static audits prove the supplied API shapes/call sites, not successful Harmony installation or gameplay. Native measurement, active-dress switching, saves/reloads, scores, rewards, notices, fonts/layout, and other-mod interactions require the sequence in PLAYTEST.md. Performance has not been benchmarked.

Save behavior: this release registers real custom definitions and calls native acceptance/save/completion routines. A pre-test backup and continued availability of the plugin are required for a reliable rollback. Native completion is not replaced with a transactional save system; a partial native exception can require recovery. Do not confuse the duplicate-payment guard with a guarantee against every possible native failure.
