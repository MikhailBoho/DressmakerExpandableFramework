# Play-test — first playable mail release (0.2.0)

Use a backed-up mid-game save with access to blue, green and white fabrics. Keep the plugin installed while loading this test save. Stop and share `BepInEx/LogOutput.log` if a step throws an error or changes the wrong commission.

## A short check before making a whole dress

1. Load the test save; open the holder/F8 at an idle desk. There should be six visible letters: two playable requests and four writing samples. Juniper's second request should be absent.
2. Read a letter and close it without accepting. Your current commission, money and dress should remain unchanged. Try a writing sample: it must have no usable acceptance button.
3. Open Juniper's first letter. Read the agreement: quality 40, blue 45%, payment 600. Click Take measurements. The normal measuring screen should appear. Confirm the measurements; the native sketchbook/sidebar should show Juniper's quest and its two requirements.
4. Return to the desk when available. Juniper's row should say In work. Continue commission should return to that request, without adding a second copy.
5. Save/quit through the game's normal controls, restart, and load the same save. The same custom commission, requirements, recorded measurements and any work should remain. Juniper's follow-up must still be absent. Verify any pre-existing native commission also remains.

## Choice and work preservation

6. Make a recognisable amount of progress on Juniper's dress. Note its design and fabric. Accept Beatrice's letter and confirm her measurements. Check her requirements: quality 40, green 45%, payment 650.
7. Switch between their In work letters using Continue commission. Each should restore its own work. Check the original native commission through the game's own selection controls as well. Restart with two active requests and confirm their work remains separate.

## Delivery, retry and continuity

8. If convenient, submit an otherwise deliverable dress that deliberately misses a colour requirement. The response should list the unmet rules. No payment or completed entry should be added; the same dress should remain available for adjustment. Close the reply and continue the commission.
9. Finish Juniper's first request successfully. Record your balance just before submitting. Expect one native payment of 600, a thank-you letter, the request archived, and normal native relationship/prestige progression. Close the reply and allow any native notices to finish.
10. Reopen mail. Juniper's first request should say Delivered; its acceptance control must be disabled. The new letter A little moonlight for everyone should be available and unread: quality 50, blue 35%, white 15%, payment 850. Opening it alone must not accept it.
11. Quit/restart after delivery. The delivered status and follow-up availability should remain; there should be no new payment merely from loading, reopening or rereading. Read/unread flags themselves are intentionally session-only.
12. Complete Beatrice and Juniper's follow-up in whichever order you like. Each pays once and has its own reply. This sample chain ends after Juniper's second dress; it does not loop or generate more work yet.

## Presentation / ordinary game checks

- Confirm the agreement button and page footer fit your resolution; body text should wrap and scroll as before.
- Try closing a delivery reply with Escape, its buttons, and F8. Native notices/desk interaction should resume. A forced scene transition should not leave mail owning input.
- Observe one ordinary native commission delivery and, when convenient in the normal story, the princess's letter. They should retain their original behavior.
- Optional on the test save: set General.Enabled=false and restart with an active mail quest. Its saved definition and normal delivery handling should remain available, while the mail holder/offers are hidden. Re-enable to browse/resume through the inbox again.

Please report: game/loader version, resolution for layout issues, letter title, action taken, whether a reload occurred, and the relevant LogOutput.log. Do not repeatedly submit after a completion exception; preserving the first log is more useful than making the failure harder to diagnose.
