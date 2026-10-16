# Stationery assets and provenance

The three new stationery textures were made with the built-in image-generation tool for this prototype. They are included unchanged at 1122 × 1402 pixels. These are shipped assets; the game performs no image generation or network requests.

| File in the package | Default correspondents |
| --- | --- |
| assets/stationery-celestial.png | Juniper Reed; Emilia Vale |
| assets/stationery-botanical.png | Beatrice Briar; Celia Moss |
| assets/stationery-rose.png | Odette Bellamy; Rosalind Aster |

All three are embedded in the DLL. In 0.1.4 the rose paper also backs the correspondence folio and stays loaded with the UI. At most one additional reader texture is loaded and replaced when its theme changes. The original supplied holder is assets/mail-holder.png, also embedded and unchanged. The opening envelope uses simple code-defined UI geometry and adds no raster asset.

Visual inspection found that the generated letterheads occupy roughly the top 18% rather than the requested 9%; the actual reader layout reserves that larger area. Writing is overlaid by TextMeshPro and is never part of these images. Corners and borders remain decorative; the center is blank for readable correspondence.

The three themes are selected by sender by default. The Stationery configuration can override the theme globally or select Classic for the original game paper.

## Final generation prompts

### celestial

Use case: stylized-concept. Asset type: production-ready blank parchment texture for a romantic hand-painted 2D dressmaking game letter UI. Create ONE full-frame portrait 4:5 stationery sheet, perfectly frontal and rectangular, no perspective, no surrounding desk, no background beyond the paper, no text, letters, numbers, logo or watermark. Fine antique paper grain, lightly irregular aged edges, elegant Victorian/rococo engraving, muted colors, graceful handmade ink contours. Keep ALL ornament confined to the outermost 6% on the sides and bottom and the topmost 9%. The large central rectangle from x=10% to90%, y=12% to94% must be very light uniform unmarked paper for dark text and buttons. No central watermark or lines. A refined ornate letterhead flourish spans the very top, with a small central emblem and symmetric scrollwork, coordinated fine corner flourishes and a delicate perimeter rule. Dense detail only at the very top and corners; never intrude on the writing area. Texture must remain quiet enough for easy reading. Design: celestial observatory stationery. Pale warm ivory vellum with a whisper of cool blue at its outer edges. Delicate ink-blue and tarnished gold filigree, tiny stars restricted to the border, and a crescent moon nested in a fine brass armillary motif in the very top 9%. Refined, scholarly, wistfully romantic; no glitter and no dark center.

### botanical

Use case: stylized-concept. Asset type: production-ready blank parchment texture for a romantic hand-painted 2D dressmaking game letter UI. Create ONE full-frame portrait 4:5 stationery sheet, perfectly frontal and rectangular, no perspective, no surrounding desk, no background beyond the paper, no text, letters, numbers, logo or watermark. Fine antique paper grain, lightly irregular aged edges, elegant Victorian/rococo engraving, muted colors, graceful handmade ink contours. Keep ALL ornament confined to the outermost 6% on the sides and bottom and the topmost 9%. The large central rectangle from x=10% to90%, y=12% to94% must be very light uniform unmarked paper for dark text and buttons. No central watermark or lines. A refined ornate letterhead flourish spans the very top, with a small central emblem and symmetric scrollwork, coordinated fine corner flourishes and a delicate perimeter rule. Dense detail only at the very top and corners; never intrude on the writing area. Texture must remain quiet enough for easy reading. Design: botanical conservatory stationery. Pale cream subtly green-tinted handmade parchment. Fine sage-green vine tracery with delicate fern curls and small ivory blossoms restricted to the border, a small floral sprig medallion in the very top 9%. Romantic naturalist's correspondence, understated antique brass accents, no flowers in the central writing area.

### rose

Use case: stylized-concept. Asset type: production-ready blank parchment texture for a romantic hand-painted 2D dressmaking game letter UI. Create ONE full-frame portrait 4:5 stationery sheet, perfectly frontal and rectangular, no perspective, no surrounding desk, no background beyond the paper, no text, letters, numbers, logo or watermark. Fine antique paper grain, lightly irregular aged edges, elegant Victorian/rococo engraving, muted colors, graceful handmade ink contours. Keep ALL ornament confined to the outermost 6% on the sides and bottom and the topmost 9%. The large central rectangle from x=10% to90%, y=12% to94% must be very light uniform unmarked paper for dark text and buttons. No central watermark or lines. A refined ornate letterhead flourish spans the very top, with a small central emblem and symmetric scrollwork, coordinated fine corner flourishes and a delicate perimeter rule. Dense detail only at the very top and corners; never intrude on the writing area. Texture must remain quiet enough for easy reading. Design: rose salon stationery. Pale blush ivory parchment. Muted dusty-rose and antique-gold rococo filigree, tiny rosebuds in the outer corners, a refined ribbon-and-rose crest in the very top 9%. Elegant romantic correspondence in a candlelit dressmaker's world; restrained soft highlights, no saturated pink, no text.
