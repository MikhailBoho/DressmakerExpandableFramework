# Developer notes — 0.2.0

## Registration and save identity

`QuestRuntime` binds the supplied native APIs by exact parameter types, then registers two CharacterDefinitions and three QuestDefinitions through the loader's internal `DressmakerMods.StoryAssetRegistry.Register`. BepInEx's hard dependency on `yuu.dressmaker.mods` establishes loader-first initialization. Registration occurs in plugin Awake before normal save loading, including when General.Enabled is false.

Stable names have the prefix `community.dressmaker.deskmail.samples`; they do not include a release version or UI index. The loader computes deterministic GUIDs and retains registered assets across ScriptableEnum cache rebuilds. Clients and quests are resolved before creation to avoid duplicate registration. No vanilla ScriptableObject is mutated. A client is an independent clone with its own normalized body shape, blank gift/unlock lists, and our existing mail-holder artwork in portrait fields. The existing native measuring silhouette is retained. Juniper's two requests share one client definition and measurement history.

These definitions are not a StoryPackRuntime and are not inserted into the campaign or pigeon offer list. The framework's JSON commission loader accepts visit/pigeon delivery, not this custom mail presentation. We use its save registry and validated requirement factory while handling offer and response presentation ourselves. This relies on internal APIs audited against the supplied loader 0.7.4; later game/loader versions need re-auditing. The DLL must remain installed for saves referencing its definitions. No framework cache supplies missing definitions after removal.

## Acceptance and resumption

`SampleQuests` is the single source of scored terms, payments, stable IDs, prerequisite, and thank-you text. `MailCatalog` appends terms from those same records; decorative narrative requests are explicitly unscored. Two initial requests are independent. Juniper's second letter appears only when the first has a native completed outcome of Succeeded or Excelled. Active and completed states are resolved from the current PlayerProgress instance, so they survive reloads and remain separate between saves.

Reading does not create a commission. The agreement page has an explicit Take measurements control. After checking the idle desk, pending proposal and pending unlocks, acceptance sets GameManager.proposedQuest and enters SetSceneMeasuring. The original MeasurePaper.ConfirmBodyMeasurement records measured centimetres, preserves the prior dress, appends a QuestState, saves, and opens the sketchbook. The measurements are not guessed from normalized body sliders. A narrow prefix prevents accepting an already active/completed custom request and repeated same-frame confirmation. Native clients and mannequin confirmation follow their original path.

Continue commission calls PlayerProgress.SwitchActiveCommission(index), then native Save and SetSceneSketchbook. The native switch retains dress/sketchbook data and reloads the chosen work. Custom code does not replace the game's active commission list or assign an index without the native switching method.

## Delivery

A prefix on FrontDesk.PlayDebriefing substitutes an IEnumerator only when the submitted current definition is one of our three registered quests. Native quest delivery and the finale keep their own paths. The mail routine clears submission flags, checks the current active QuestState identity, then uses Dress.DoesDressSatisfyCommission. Forced debug outcomes are cleared for these test commissions; actual requirements decide the result.

Unmet rules produce an alteration letter, without archiving, payment, prestige or relationship changes. The current dress and request remain available. Success sets the native outcome to Succeeded and invokes FrontDesk.CompleteQuest: native relationship/prestige, budget payment, archive, current-dress restoration, and Save. The thank-you reader holds the native presentation loop until closed; queued progress notices then continue normally.

The DeliveryGate rejects overlapping/repeated completion of the same state. It allows retry after an unmet check but retains a guard once reward processing starts, even if a native callback throws. An active state whose outcome already changed is not paid again. This is not an atomic replacement for the game's save function: a native exception during reward/archive/save can still require recovery from the pre-test backup. The error is logged and shown; automatic reward retries are deliberately avoided.

Scene changes, desk disabling, and switching saves explicitly release presentation ownership. We do not depend solely on Unity disposing a stopped coroutine. The reward guard survives scene teardown and resets when the loaded PlayerProgress object changes. Saved completed state prevents re-offering after a restart.

## UI and data boundaries

The 0.1.4 art, holder layout, modal protections, font fallback, wrapping, scroll behavior, and cancellable envelope animation are retained. The inbox hides locked letters and packs visible rows without gaps. In work, Delivered, and Sample labels make the pool's behavior explicit. Read agreement jumps to the scored terms before Take measurements is enabled. Active requests offer Continue commission; delivered requests cannot be accepted again.

MailSession tracks visible letters and per-session read flags. Temporary delivery notices do not alter unread counts. Closing a notice returns to the desk instead of allowing another acceptance inside the delivery coroutine. Accepted/active/completed state is native; there is no sidecar save or global config used as quest progress. Replies are not a persistent mail archive. Read flags reset on process restart or a different loaded save.

No runtime LLM, image model, network request, NPC dialogue node, generated portrait, or new native animation dependency is added. Definitions load once; availability checks cover a tiny fixed catalog at the existing once-per-second probe. Live performance has not been measured.

## Hooks

The original six UI prefixes remain: FrontDesk.NextPresentation, GameManager.SetScene, GameManager.ShowHideOptions, KnighthoodScene.ShowKnighthoodEnvelope, MessengerPigeonUI.Show, and FrontDesk.OnDisable. A separate Harmony owner `.commissions` holds the PlayDebriefing and ConfirmBodyMeasurement prefixes. Disabling inbox display does not unregister saved definitions or remove delivery support. UI failure is logged; it does not erase native saved quests.

## Build and verify

With .NET 8 SDK and the installed game's BepInEx/Unity reference DLLs:

```powershell
dotnet build .\source\DeskMailPrototype.csproj -c Release "-p:GameDir=C:\Games\Dressmaker"
dotnet run --project .\tests\StateTests.csproj -c Release
python -m pip install dnfile dncil
python .\tests\audit_contract.py "C:\Games\Dressmaker\Dressmaker_Data\Managed\Assembly-CSharp.dll" "C:\Games\Dressmaker\BepInEx\plugins\DressmakerMods.dll" audit.json
```

Adjust the loader DLL location to the actual installation. If necessary, set `-p:GameManagedDir=...` for a differently named data folder. Build output is `source/bin/Release/netstandard2.1/Dressmaker.DeskMailPrototype.dll`. PNGs beside source embed as Dressmaker.DeskMail.Assets.<filename>.

The packaged DLL uses SDK 8.0.426/Roslyn 4.11 with netstandard2.1, BepInEx 5.4.23.5, HarmonyX 2.9.0.0, and UnityEngine.Modules 6000.0.24 compile-time references. The exact installed Unity version is not known. Game, UI and loader types are reflected, not compile-linked; no reference/game/loader DLL is redistributed. The test executable is not shipped. `audit_contract.py` reads CLR metadata/CIL only and does not execute the supplied assemblies.
