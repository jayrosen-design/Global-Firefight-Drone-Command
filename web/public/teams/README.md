# Team art from the pitch deck

Drop the pitch-deck images here and the country select screen picks them up automatically (no code change).

```
public/teams/<CODE>/concept.jpg              hero concept art (drone / carrier / scene) — shown beside the 3D turntable
public/teams/<CODE>/scene.jpg                optional second concept frame (campaign environment)
public/teams/<CODE>/crew/<CALLSIGN>.jpg      character portrait per crew member (call signs are in web/lib/config/fleets.ts)
```

`<CODE>` is one of `USA`, `CAN`, `BRA`, `CHN`, `DEU`, `AUS`. `.png` and `.webp` also work if you update the
extension in `web/lib/config/teamArt.ts`. Recommended sizes: concept 1600×900, portraits 512×512.

Example: `public/teams/USA/concept.jpg`, `public/teams/USA/crew/RIDGE-ACTUAL.jpg`, `public/teams/USA/crew/GUARDIAN-1.jpg`.

Missing files fall back to the procedural turntable and initial-letter avatars, so partial sets are fine.
