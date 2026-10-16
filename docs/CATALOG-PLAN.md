# Prewritten correspondence catalog — revised work plan

## Direction

Prepare characters, archetypes, backstories, occasion-specific letters, and validated commission definitions ahead of release. At runtime the mod selects eligible offers from this catalog. It does not run a local LLM, generate images, or wait for content to be written. The mail presentation means each correspondent needs a written identity rather than a character sprite set.

The user confirmed the 0.1.4 presentation. Version 0.2.0 now implements three playable sample commissions (Juniper twice and Beatrice once), native save/score/delivery paths, and one successful-completion follow-up gate. It still needs the in-game checks in PLAYTEST.md. Favorites, rotation, external catalog loading, and broader content are future work.

## Data to author

| Record | Fields and purpose |
| --- | --- |
| Character | Stable character ID, name, archetype, occupation, background, voice notes, preferences, and relationship/return rules. |
| Letter | Stable letter ID, character ID, subject, occasion, authored prose, follow-up references, eligibility conditions, and associated commission ID. |
| Commission | Stable definition ID, explicit supported requirement IDs/values, progression eligibility, and audited native scoring/reward data. |
| Save state | Catalog version, offered/accepted/completed IDs, active commission, favorite character IDs, cooldowns, and rotation history. |

Use archetypes as writing guidance: an anxious scholar and a flamboyant performer should sound different, while people sharing an archetype still have individual motives and histories. Keep preferences and gameplay requirements explicit. The game should display its exact validated requirement list beside the narrative, so a playful phrase cannot become an ambiguous scoring condition.

IDs must stay stable as the catalog is edited. Never derive persistent IDs from list position, display names, or a new random number on every launch. Catalog updates need a version/migration policy for changed or retired definitions.

## Startup and save compatibility

Load and validate the catalog once. Reject duplicate IDs, unresolved character references, contradictory requirements, and requirements unavailable at the player's progression. Length checks should flag unusually long letters for editing; the reader's scrolling remains a fallback.

Audit the loader's registration and the game's save-loading order before committing to an integration method. The intended approach is to register all definitions needed to restore saves at the supported initialization phase, before native saved references are resolved. Selecting the next offer should then select existing definitions, without injecting new definitions mid-session.

Mail history and favorites need a proven per-save persistence path. Determine whether the framework offers one; otherwise design an explicitly associated, versioned sidecar with safe restoration. Do not assume a global plugin setting can hold multiple playthroughs' history correctly. The sample release uses native active/completed state. A separate per-save mail/favorites persistence design is still needed before those features are added.

## Offer rotation and favorite correspondents

A small inbox can still use the previous batch idea: select an initial six eligible offers and top it back up when it falls to three. These numbers are provisional. Refilling is a fast selection operation over already-loaded content; no background model process is needed. Avoid changing the letter currently being read or an accepted request when replenishing.

Prefer unseen letters and avoid immediate repeats. Favorites increase the chance that an eligible character returns, subject to cooldowns and available unread/follow-up letters; they should not crowd out all new faces. Remember the character across requests, but give each distinct authored commission its own stable identity. Eligibility should prevent promises about impossible garments or unavailable materials.

A finite authored catalog provides finite unique prose. Once exhausted, choose an explicit policy such as rotating repeatable requests or waiting for a content pack. Do not silently duplicate a one-time reward or let repeated offers overwrite an active commission. Combinations can provide mechanical variety, but genuinely new stories still need authoring.

## Implementation sequence and exit checks

1. **Presentation confirmed.** The user confirmed 0.1.4's holder, correspondence UI, and opening behavior work in game.
2. **Prove one real commission.** Audit registration, accept/cancel behavior, scoring, delivery, reward, and persistence. Implement one fixed letter whose exact requirements are registered. Accept it on a mid-game save, restart during work, finish it, and verify rewards occur once. Preserve the normal campaign and final-quest letter.
3. **Add catalog loading and small rotation.** Start with a deliberately small set of validated definitions. Verify startup registration, save restore, eligibility, inbox refill, exhaustion, and no duplicate active offers.
4. **Author the initial cast.** Create around 10–20 distinct characters with 2–3 letters each. Establish archetype/voice notes and cross-check every story against its requirements. This is the future writing step requested by the user.
5. **Add favorites and continuity.** Persist favorites per playthrough; test recurring characters, cooldowns, follow-up ordering, exhausted favorites, and loading older catalog versions.
6. **Expand content.** Add reviewed content packs once the system is stable. A hundred commissions at the user's 5–30 minutes each already represents roughly 8–50 hours of dressmaking, before counting replayable requests.

The current playable prototype now needs acceptance, reload, and delivery proved in game. Building hundreds of records before that integration is reliable would multiply the content that might need restructuring.
