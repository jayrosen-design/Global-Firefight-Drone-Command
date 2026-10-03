# Team art from the pitch deck

Extracted from `Assets/Research/Global Firefight Drone Command Pitch.pdf` (the embedded transparent cut-outs, not
screenshots), trimmed and converted to WebP:

| File | Source |
| --- | --- |
| `<CODE>/drone.webp` | Equipment slide, left image (slides 17–22) |
| `<CODE>/carrier.webp` | Equipment slide, right image |
| `<CODE>/insignia.webp` | Equipment slide, unit badge (bottom right) |
| `<CODE>/flag.png` | Equipment slide, national flag (used instead of emoji flags, which Windows doesn't render) |
| `<CODE>/crew/1…6.webp` | Team slide figures, left to right (slides 32–37) |

Slides: 17/32 USA · 18/33 CAN · 19/34 BRA · 20/35 DEU · 21/36 CHN · 22/37 AUS.

Crew order matches `crew[]` in `web/lib/config/fleets.ts`; the deck gives no names, so names are placeholders and
roles follow each figure's equipment. Missing files fall back gracefully (flag, initial-letter avatar).
