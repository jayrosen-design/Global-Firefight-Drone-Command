# Global Firefight - Drone Command

**🚧 Work in Progress - NDIA Hackathon Project 🚧**

Dual-mode RTS and third-person tactical firefighting simulation driven by live NASA fire data. This repository holds two builds:

- **[Web prototype](#web-prototype-god-eye-fork)** in [`web/`](web/) — Next.js 14 + Three.js / React Three Fiber, playable in the browser (documented below, with screenshots and architecture diagrams).
- **[Unity project](#unity-project-original-hackathon-build)** in [`Assets/`](Assets/) — the original Cesium for Unity hackathon build.

## Web Prototype (God Eye fork)

Dual-mode RTS / third-person tactical firefighting simulation on a live NASA fire globe.
Built with Next.js 14 (App Router), TypeScript, Tailwind, Zustand, Three.js r170 and React Three Fiber.

### Screenshots

| Campaign menu | Global RTS — free play on the live FIRMS globe |
| --- | --- |
| ![Campaign menu](web/docs/screenshots/01-campaign-menu.png) | ![Global RTS free play](web/docs/screenshots/02-global-rts-freeplay.png) |

| RTS dispatch — Black Summer, arc trajectory + carrier | Tactical drone view — standard colour |
| --- | --- |
| ![RTS dispatch](web/docs/screenshots/03-rts-dispatch.png) | ![Tactical standard](web/docs/screenshots/04-tactical-standard.png) |

| Tactical — retardant drop knock-down on the reticle | Tactical — IR White-Hot (PERSONNEL DETECTED, hazard callouts) |
| --- | --- |
| ![Tactical drop](web/docs/screenshots/05-tactical-drop.png) | ![Tactical IR](web/docs/screenshots/06-tactical-ir-white-hot.png) |

| Tactical — LIDAR point cloud (canopy pathways) | Mission debrief — economic ledger |
| --- | --- |
| ![Tactical LIDAR](web/docs/screenshots/07-tactical-lidar.png) | ![Mission debrief](web/docs/screenshots/08-mission-debrief.png) |

Captured from a scripted headless Chromium run against the bundled fallback dataset (no FIRMS key configured).

### Run

```bash
cd web
npm install
cp .env.example .env.local   # optional — see below
npm run dev                  # http://localhost:3000
```

Without a FIRMS key the app uses a bundled fallback fire dataset so it always runs. For live data set
`FIRMS_MAP_KEY` (free key: https://firms.modaps.eosdis.nasa.gov/api/map_key/). EONET needs no key.

### Real-world 3D map (God Eye map stack)

The globe and the tactical arena are real-world 3D tiles streamed with [`3d-tiles-renderer`](https://github.com/NASA-AMMOS/3DTilesRendererJS).
The map route is chosen the same way as [gods-eye-view](https://github.com/bilawalsidhu/gods-eye-view) (`src/maps/google3d.js`):

| Credential in `web/.env.local` | Route | What you see |
| --- | --- | --- |
| `NEXT_PUBLIC_GOOGLE_MAPS_API_KEY` | **google-direct** | Google Photorealistic 3D Tiles (Map Tiles API) — buildings, trees, terrain |
| `NEXT_PUBLIC_CESIUM_ION_TOKEN` | **google-ion** | The same Google tiles served via Cesium ion asset `2275207` |
| *(neither)* | **keyless** | Globe: Re:Earth / Mapterhorn quantized-mesh terrain (CC BY 4.0) draped with Esri World Imagery. Drone view: procedural 3D terrain |
| `NEXT_PUBLIC_DISABLE_3D_TILES=1` | **off** | Vector basemap globe + procedural tactical terrain |

Both credentials are client-exposed by design (they are used in the browser) — restrict them by HTTP referrer and API scope
in the provider console. On Vercel add them as project environment variables and redeploy; `NEXT_PUBLIC_*` values are
inlined at build time.

If the tile source cannot be reached (root tileset error, or nothing within 20 s) the app falls back the way God Eye does:
the globe restores its raster basemap and the tactical view uses procedural terrain, with a `MAP UNAVAILABLE` notice in the HUD.

Photorealistic tiles in the tactical drone view require a Google or ion credential; without one the drone view keeps the
procedural heightmap terrain so it is always playable.

In the global view the tiles are scaled into the WGS84 unit-globe frame under the HUD; in the tactical view the tileset is
re-oriented so the target fire sits at the origin with +Y up, and fires, protected structures and survivors are projected from
their real coordinates and settled onto the streamed surface by raycasting the tiles. IR White-Hot swaps tile materials for a
monochrome shader; LIDAR re-renders the tile geometry as an elevation-coloured point cloud.

### Controls

| Global RTS | Tactical drone |
| --- | --- |
| Drag to orbit, wheel to zoom | `W/S` throttle, `A/D` yaw, `Q/E` altitude, `SHIFT` boost |
| Click carrier → click fire = dispatch | `SPACE` drop suppressant (reticle turns gold when the predicted impact is on a fire) |
| Click a live FIRMS hotspot to engage it | `R` (hold, < 30 m AGL, within 40 m) extract a civilian |
| `MOVE` then click globe relocates a carrier | `V` cycle Standard / IR White-Hot / LIDAR |
| Fleet panel → click a nation, then the globe, to deploy a forward carrier | `ESC` back to the global view |

### Software architecture

#### 1. Layered system architecture

```mermaid
flowchart TB
  subgraph EXT["External services"]
    FIRMS["NASA FIRMS<br/>area CSV API<br/><i>latitude, longitude, frp, confidence</i>"]
    EONET["NASA EONET v3<br/>events GeoJSON<br/><i>title, geometry, link</i>"]
    TILES["Raster tile server (optional)<br/>{z}/{x}/{y} Web Mercator"]
    NE["Natural Earth 1:110m<br/>world-atlas TopoJSON (bundled)"]
  end

  subgraph SRV["Server — Next.js 14 App Router · Node runtime"]
    RF["app/api/firms/route.ts<br/>injects FIRMS_MAP_KEY · revalidate 900 s · returns CSV"]
    RE["app/api/eonet/route.ts<br/>category=wildfires&status=open · revalidate 3600 s"]
    LAYOUT["app/layout.tsx · app/page.tsx<br/>static shell, globals.css (Tailwind)"]
  end

  subgraph CLIENT["Browser — React 18 client"]
    direction TB

    subgraph SHELL["Application shell"]
      CC["components/CommandCenter.tsx<br/>mode router · loadFeed() on mount<br/>dynamic() imports of both canvases (ssr:false)"]
    end

    subgraph DATA["Data access — lib/data"]
      PF["nasa-firms.ts<br/>parseFirmsCsv · frpToIntensity · hotspotToVector3"]
      PE["nasa-eonet.ts<br/>parseEonet (Point / Polygon centroid)"]
      FB["fallback-fires.ts<br/>seeded offline dataset (1.3k hotspots + 7 events)"]
      TY["types.ts<br/>FirmsHotspot · EonetEvent · FireFeed"]
    end

    subgraph STATE["State — Zustand"]
      GS["store/gameStore.ts<br/>mode · feed · scenario · fires[] · carriers[] · drones[]<br/>selection · ledger · simTime · timeScale · visionMode · cameraRequest<br/><b>tick(dt)</b> · dispatch · deploySuppressant · tacticalDrop · rescueCivilian · endMission"]
      TS["store/telemetryStore.ts<br/>altitude · airspeed · heading · reticleLocked · nearCivilian<br/>(per-frame, isolated from HUD re-renders)"]
    end

    subgraph DOMAIN["Domain engine — lib/engine · lib/geo · lib/config"]
      FIRE["engine/fire.ts<br/>Fire · createScenarioFires · fireFromHotspot<br/>stepFire (growth, damage) · applyDrop"]
      DU["engine/DroneUnit.ts<br/>DroneUnit · createDrone · progressRate · batteryDrainPerSec"]
      CV["engine/CarrierVehicle.ts<br/>CarrierVehicle · createCarrier · stepCarrier (rearm, MOVE)"]
      EC["engine/economics.ts<br/>Ledger · livesSavedBonus · totalSuppressionCost · finalScore · fmtUSD"]
      WGS["geo/wgs84.ts<br/>latLonToVector3 · vector3ToLatLon · haversineKm<br/>greatCirclePoint · surfaceFrame"]
      TRJ["geo/trajectory.ts<br/>TrajectoryManager · apexForDistance · sample(t)"]
      ETX["geo/earthTexture.ts<br/>buildEarthTexture (canvas from TopoJSON)"]
      FLC["config/fleets.ts<br/>FLEETS[6] · DroneSpec · CarrierSpec · costs · abilities"]
      SCC["config/scenarios.ts<br/>SCENARIOS[7] · objectives · fires · wind · budget"]
      TAC["tactical/local.ts · tactical/noise.ts<br/>toLocal (tangent plane, 0.12 compression) · terrainHeight (fBm)"]
    end

    subgraph RENDER["Rendering — Three.js r170 via React Three Fiber"]
      subgraph GL["Global RTS canvas — components/globe"]
        SC["Scene.tsx<br/>Canvas · lights · Simulation (useFrame → tick)"]
        GB["Globe.tsx<br/>unit sphere · atmosphere GLSL · click → place / MOVE"]
        TB["TileBasemap.tsx<br/>Mercator sphere patches"]
        FLR["FireLayer.tsx<br/>InstancedMesh + GLSL billboard shader<br/>size/colour/pulse ∝ log(FRP), far-side cull"]
        FE["FireEntities.tsx<br/>engaged fires · point lights · rings · labels"]
        EBc["EonetBeacons.tsx"]
        CR["Carriers.tsx · Drones.tsx<br/>surface frame orientation · orbit · swarm offsets"]
        TR["Trajectories.tsx<br/>arc polyline + progress GLSL"]
        CAMR["CameraRig.tsx<br/>OrbitControls · flyTo lerp"]
      end
      subgraph TL["Tactical canvas — components/tactical"]
        TSC["TacticalScene.tsx<br/>local arena · fires ≤ 30 km · structures · civilians"]
        TER["Terrain.tsx<br/>heightmap mesh · LIDAR point cloud · instanced trees"]
        TF["TacticalFires.tsx<br/>GLSL flame + smoke point sprites · white-hot uniform"]
        STC["Structures.tsx · Civilians.tsx"]
        DCT["DroneController.tsx<br/>keyboard flight · chase cam · ballistic drops<br/>predicted-impact reticle · rescue · telemetry"]
      end
      MD["components/models<br/>DroneModel (6 silhouettes) · CarrierModel (6x6)<br/>livery / IR / LIDAR material variants"]
    end

    subgraph HUD["HUD overlay — components/hud (DOM, Tailwind glassmorphism)"]
      TOP["TopBar<br/>net score · property · lives · cost · budget · clock · speed"]
      BOT["BottomBar<br/>selection panel · MOVE / DEPLOY SUPPRESSANT / DISPATCH<br/>fleet deployment · TACTICAL DRONE VIEW"]
      LOG["MissionLog"]
      MENU["ScenarioMenu"]
      THUD["TacticalHUD<br/>reticle · gauges · vision toggle · abilities"]
      DEB["Debrief<br/>itemised ledger · objectives · grade"]
    end
  end

  FIRMS --> RF
  EONET --> RE
  RF -- "text/csv" --> PF
  RE -- "GeoJSON" --> PE
  PF --> GS
  PE --> GS
  FB -. "on 5xx / no key" .-> GS
  TILES -. "TextureLoader" .-> TB
  NE --> ETX --> GB
  LAYOUT --> CC
  CC --> GS
  CC --> GL
  CC --> TL
  CC --> HUD
  FLC --> GS
  SCC --> GS
  FIRE --> GS
  DU --> GS
  CV --> GS
  EC --> GS
  EC --> TOP
  EC --> DEB
  WGS --> TRJ --> CR
  WGS --> TRJ --> TR
  WGS --> FLR
  WGS --> FE
  WGS --> CR
  TAC --> TSC
  TAC --> TER
  TAC --> DCT
  GS --> GL
  GS --> TL
  GS --> HUD
  DCT --> TS --> THUD
  DCT -- "tacticalDrop · rescueCivilian" --> GS
  MD --> CR
  MD --> DCT
```

#### 2. Domain model

```mermaid
classDiagram
  direction LR

  class FirmsHotspot {
    +string id
    +number latitude
    +number longitude
    +number frp
    +number confidence
    +string acqDate
    +string acqTime
  }
  class EonetEvent {
    +string id
    +string title
    +string link
    +number latitude
    +number longitude
  }
  class FireFeed {
    +source: live | fallback
    +number fetchedAt
    +FirmsHotspot[] hotspots
    +EonetEvent[] events
  }
  FireFeed o-- FirmsHotspot
  FireFeed o-- EonetEvent

  class Fire {
    +string id
    +number lat
    +number lon
    +number frp
    +number initialFrp
    +number propertyRemainingUSD
    +number populationRemaining
    +boolean extinguished
    +boolean contained
    +number burnTime
    +source: scenario | live
  }
  class DroneUnit {
    +string id
    +string callSign
    +CountryCode country
    +string carrierId
    +string targetFireId
    +DroneState state
    +LatLon origin
    +LatLon destination
    +number t
    +number distanceKm
    +number battery
    +number payloadLitres
    +number flightTimeSec
    +number orbit
    +number swarmIndex
  }
  class CarrierVehicle {
    +string id
    +CountryCode country
    +number lat
    +number lon
    +number dronesReady
    +number[] rearmQueue
    +LatLon moveTarget
  }
  class Trajectory {
    +LatLon from
    +LatLon to
    +number distanceKm
    +number apexKm
    +Vector3[] points
  }
  class Ledger {
    +number propertySavedUSD
    +number civiliansRescued
    +number populationProtected
    +number deploymentCostUSD
    +number flightTimeCostUSD
    +number payloadCostUSD
    +number sorties
    +number drops
    +number firesExtinguished
  }
  class FleetConfig {
    +CountryCode code
    +string country
    +DroneSpec drone
    +CarrierSpec carrier
    +LatLon base
    +Costs costs
  }
  class DroneSpec {
    +string model
    +number payloadLitres
    +SuppressantType suppressant
    +number cruiseKmh
    +number enduranceMin
    +number swarmSize
    +Livery livery
    +Ability[] abilities
  }
  class Scenario {
    +string id
    +CountryCode country
    +string title
    +LatLon center
    +string[] constraints
    +Objective[] objectives
    +ScenarioFire[] fires
    +Wind wind
    +number budgetUSD
    +number propertyAtRiskUSD
    +number populationAtRisk
  }
  class GameState {
    +GameMode mode
    +FireFeed feed
    +Scenario scenario
    +Fire[] fires
    +CarrierVehicle[] carriers
    +DroneUnit[] drones
    +Ledger ledger
    +number simTime
    +number timeScale
    +VisionMode visionMode
    +tick(dt)
    +dispatch(carrierId, fireId)
    +tacticalDrop(fireId, litres)
    +endMission()
  }

  FirmsHotspot ..> Fire : fireFromHotspot()
  Scenario ..> Fire : createScenarioFires()
  Scenario ..> CarrierVehicle : stages
  FleetConfig *-- DroneSpec
  FleetConfig ..> DroneUnit : specs
  FleetConfig ..> CarrierVehicle : capacity, rearm
  CarrierVehicle "1" --> "0..*" DroneUnit : launches
  DroneUnit --> Fire : targets
  DroneUnit ..> Trajectory : TrajectoryManager.build()
  GameState *-- Fire
  GameState *-- CarrierVehicle
  GameState *-- DroneUnit
  GameState *-- Ledger
  GameState o-- FireFeed
  GameState o-- Scenario
```

#### 3. Application mode state machine

```mermaid
stateDiagram-v2
  [*] --> menu : CommandCenter mounts · loadFeed()
  menu --> rts : startScenario(id) / startFreePlay()
  rts --> tactical : enterTactical(droneId)  [drone airborne]
  tactical --> rts : exitTactical() · ESC · drone recovered
  rts --> debrief : endMission()
  debrief --> rts : REPLAY
  debrief --> menu : CAMPAIGN SELECT
  rts --> menu : MENU

  state rts {
    [*] --> idle
    idle --> carrierSelected : click carrier
    carrierSelected --> idle : click empty globe
    carrierSelected --> moveArmed : MOVE
    moveArmed --> idle : click globe → moveCarrier()
    carrierSelected --> droneSelected : click fire → dispatch()
    idle --> fireSelected : click fire / hotspot (engageHotspot)
    fireSelected --> droneSelected : DISPATCH FROM…
    idle --> placingCarrier : fleet panel click
    placingCarrier --> carrierSelected : click globe → placeCarrierAt()
  }

  state tactical {
    [*] --> standard
    standard --> ir : V / toggle
    ir --> lidar : V / toggle
    lidar --> standard : V / toggle
  }
```

#### 4. Drone sortie state machine

```mermaid
stateDiagram-v2
  [*] --> enroute : dispatch() · createDrone()<br/>ledger.deploymentCost += cost × swarm
  enroute --> onstation : t ≥ 1 (great-circle progress)
  onstation --> suppressing : dropCooldown ≤ 0 · payload > 0<br/>applyDrop(fire, payload/3, effectiveness)
  suppressing --> onstation : cooldown 12 s
  onstation --> returning : fire extinguished
  suppressing --> returning : payload = 0 · battery < 20 %
  returning --> landed : t ≤ 0 (back at carrier)
  landed --> [*] : removed · carrier.rearmQueue.push(rearmSeconds)

  note right of enroute
    every tick: flightTimeSec += dt
    ledger.flightTimeCost += perMinute/60 × dt
    battery −= drain(state) × dt
  end note
  note right of suppressing
    ledger.payloadCost += litres × perLitre
    fire.frp −= litres × 0.6 × effectiveness
    retardant ⇒ fire.contained = true
    frp ≤ 2 % ⇒ extinguished ⇒ creditExtinguish()
  end note
```

#### 5. Sequence — data load and RTS dispatch

```mermaid
sequenceDiagram
  autonumber
  participant U as Player
  participant CC as CommandCenter
  participant GS as gameStore
  participant API as /api/firms · /api/eonet
  participant NASA as NASA FIRMS / EONET
  participant SC as Globe Scene (R3F)
  participant TM as TrajectoryManager

  CC->>GS: loadFeed()
  GS->>API: fetch /api/firms, /api/eonet
  API->>NASA: GET area CSV (MAP_KEY) · GET events?category=wildfires
  NASA-->>API: CSV / GeoJSON
  API-->>GS: text/csv · JSON
  GS->>GS: parseFirmsCsv · parseEonet
  alt proxy 5xx or no key
    GS->>GS: buildFallbackFeed()
  end
  GS-->>SC: feed.hotspots → FireLayer (InstancedMesh), feed.events → EonetBeacons

  U->>CC: select scenario
  CC->>GS: startScenario(id)
  GS->>GS: createScenarioFires · createCarrier · cameraRequest
  GS-->>SC: fires[], carriers[] · CameraRig lerps to centre

  U->>SC: click carrier
  SC->>GS: select({carrier})
  U->>SC: click fire
  SC->>GS: dispatch(carrierId, fireId)
  GS->>GS: haversineKm · createDrone × swarmSize · ledger.deploymentCost
  loop every frame (useFrame → tick(dt × timeScale))
    GS->>GS: stepFire · stepCarrier · advance drones · costs
    SC->>TM: build(id, origin, destination)
    TM-->>SC: Trajectory (96 pts, apexKm)
    SC->>SC: sample(t) → position, heading · arc progress uniform
  end
  GS-->>CC: log entries · ledger → TopBar / MissionLog
```

#### 6. Sequence — tactical drop

```mermaid
sequenceDiagram
  autonumber
  participant U as Player
  participant DC as DroneController
  participant TS as telemetryStore
  participant GS as gameStore
  participant TF as TacticalFires
  participant HUD as TacticalHUD

  U->>GS: enterTactical(droneId)
  GS-->>DC: mode = tactical · timeScale = 6 · target = drone.destination
  DC->>DC: spawn 650 m short of target, 150 m AGL
  loop every frame
    U->>DC: W/A/S/D · Q/E · SHIFT
    DC->>DC: integrate flight · clamp to terrain · chase camera
    DC->>DC: predicted impact = pos + fwd × 0.8·v × √(2·AGL / 2.2g)
    DC->>TS: set(altitude, airspeed, reticleLocked, targetDistance)
    TS-->>HUD: gauges · reticle lock (gold)
  end
  U->>DC: SPACE
  DC->>GS: setState(payload −= litres)   (debit at release)
  DC->>DC: spawn Drop{pos, vel}, fall under 2.2 g
  DC->>DC: impact → nearest fire within window
  alt hit
    DC->>GS: tacticalDrop(fireId, litres × accuracy, debit=false)
    GS->>GS: applyDrop · ledger.payloadCost · drops++
    GS-->>TF: fire.frp ↓ → smaller flame/smoke uniforms
    GS-->>TS: lastDropKnockdownMW
    TS-->>HUD: "−N MW" flash
  else miss
    DC->>TS: lastDropKnockdownMW = 0
  end
  U->>DC: hold R near civilian (< 30 m AGL)
  DC->>GS: rescueCivilian() → ledger.civiliansRescued++
```

#### 7. Rendering pipeline per frame

```mermaid
flowchart LR
  RAF["requestAnimationFrame<br/>(R3F render loop)"] --> SIM["Simulation.useFrame<br/>gameStore.tick(dt)"]
  SIM --> F1["stepFire × fires<br/>growth · damage"]
  SIM --> F2["stepCarrier × carriers<br/>rearm · MOVE"]
  SIM --> F3["advance drones<br/>enroute / orbit / return · auto-drops"]
  F1 & F2 & F3 --> SET["set({fires, carriers, drones, ledger, simTime})"]
  SET --> SUB["Zustand subscribers<br/>(HUD re-render, entity lists)"]
  RAF --> UF["per-entity useFrame (no React re-render)"]
  UF --> D1["Drones: TrajectoryManager.sample → position/quaternion"]
  UF --> D2["Carriers/Fires/Beacons: surface frame · camera-distance scale · far-side label fade"]
  UF --> D3["FireLayer: uTime, uCamDist uniforms"]
  UF --> D4["Trajectories: uProgress, uTime uniforms"]
  UF --> D5["CameraRig: flyTo lerp · OrbitControls.update"]
  D1 & D2 & D3 & D4 & D5 --> GPU["WebGL2 draw<br/>InstancedMesh fires · ShaderMaterial arcs · Html labels"]
```

#### Runtime notes

* **Simulation clock** — the RTS runs at 60× (1 real second = 1 simulated minute, with 1×/3×/8× multipliers), the tactical view at 6×. A 40 km sortie takes about a minute of real time while property loss stays gradual.
* **Coordinate systems** — globe space is a unit sphere (1 unit = 6371 km); the tactical arena is a local tangent plane in metres with 0.12× geographic compression so a 15 km fire complex fits a ±3 km flyable area.
* **Data fallback** — when either proxy fails (no key, network policy, upstream error) the store seeds a deterministic offline dataset and the HUD reports `FEED FALLBACK`.
* **Economic model** — `FinalScore = (PropertyValueSaved + LivesSavedBonus) − TotalSuppressionCost`, where lives bonus = civilians × $10M VSL + residents protected × $12.5K, and cost = deployment + flight time + payload per nation.

### Code layout

```
app/                      Next.js App Router shell + API proxies
  api/firms/route.ts      FIRMS area-CSV proxy (server-side MAP_KEY, 15-min revalidate)
  api/eonet/route.ts      EONET v3 open wildfire events proxy
lib/geo/wgs84.ts          lat/lon ⇄ globe Cartesian, haversine, great-circle interpolation
lib/geo/trajectory.ts     TrajectoryManager — great-circle arcs with parabolic altitude
lib/geo/earthTexture.ts   Dark tactical basemap from Natural Earth (world-atlas) TopoJSON (loading fallback)
lib/config/tiles.ts       Map route selection (google-direct / google-ion / keyless / off)
lib/tiles/                DRACO + BVH setup, globe surface sampler, vision-mode tile materials
components/tiles/         WorldTiles — 3d-tiles-renderer wrapper for both views (auth, terrain, imagery, attribution)
components/tactical/TacticalWorld.tsx  Terrain provider: reoriented tiles or procedural fallback
lib/data/nasa-firms.ts    CSV parser, FRP → intensity, hotspot → Vector3
lib/data/nasa-eonet.ts    GeoJSON parser (title / geometry / link)
lib/data/fallback-fires.ts Offline dataset
lib/config/fleets.ts      Six national fleets: drones, carriers, liveries, abilities, costs
lib/config/scenarios.ts   Seven pitch-deck campaign presets
lib/engine/               Fire model, DroneUnit, CarrierVehicle, economics (ledger + FinalScore)
store/gameStore.ts        Zustand store: simulation tick, dispatch, suppression, scoring
components/globe/         God Eye globe adapter: Globe, TileBasemap, instanced FireLayer shader,
                          EonetBeacons, Carriers, Drones, Trajectories, CameraRig
components/tactical/      Tactical arena: terrain + LIDAR cloud, flame/smoke particles,
                          structures, civilians, player DroneController
components/models/        Procedural drone & 6x6 carrier models per livery
components/hud/           TopBar, BottomBar, MissionLog, ScenarioMenu, TacticalHUD, Debrief
```

#### Economic model

`FinalScore = (PropertyValueSaved + LivesSavedBonus) − TotalSuppressionCost`

* Property saved is credited when a fire is extinguished (what is still standing at that moment).
* Lives Saved Bonus = civilians rescued × $10M (value of a statistical life) + residents protected × $12.5K.
* Suppression cost = deployment (per sortie) + flight time (per minute) + payload (per litre), per nation.

#### Vision modes

* **IR White-Hot** — all materials go monochrome, fires and personnel render white with
  `PERSONNEL DETECTED` and `CRITICAL STRUCTURE / VALVE HAZARD` callouts.
* **LIDAR** — terrain and canopy render as an elevation-coloured point cloud (cyan → green), buildings as wireframe,
  fires as ground rings, so pathways through smoke stay legible.

#### God Eye adaptation

[gods-eye-view](https://github.com/bilawalsidhu/gods-eye-view) is a CesiumJS console; this project's spec is Three.js / React Three Fiber, so the
same map stack (Google Photorealistic 3D Tiles directly or via Cesium ion, keyless Re:Earth terrain + Esri imagery) is streamed
through `3d-tiles-renderer` instead of Cesium. Credential names and route precedence follow God Eye's `google3d.js`; no God Eye
source is vendored.

### Scripts

`npm run dev` · `npm run build` · `npm run start` · `npm run lint` · `npm run typecheck`

---

# Unity Project (original hackathon build)

## Project Overview

Global Firefight - Drone Command is an innovative real-time strategy and simulation game developed for the NDIA (National Defense Industrial Association) Hackathon. The project combines real-world wildfire data with strategic drone fleet management to create an immersive firefighting command experience.

## Project Vision

This game transforms players into incident commanders managing international drone fleets to combat global wildfires using real-time NASA satellite data. Players switch between a strategic global view for fleet deployment and a tactical drone pilot view for precision firefighting operations.

### Key Features (In Development)

- **Real-Time Data Integration**: Live wildfire data from NASA FIRMS and EONET APIs
- **Dual-Mode Gameplay**: 
  - Strategic RTS view on a 3D globe for fleet management
  - Tactical third-person drone control for precision operations
- **Authentic Drone Fleet**: Based on real international firefighting aircraft and systems
- **Economic Impact Scoring**: Players scored on property and lives saved vs. operational costs
- **Photorealistic 3D Environment**: Powered by Google's Photorealistic 3D Tiles via Cesium for Unity

## Current Development Status

### ✅ Completed Components
- Core game architecture and state management
- NASA FIRMS API integration for real-time fire data
- Fire positioning and visualization on Cesium 3D globe
- Interactive fire selection system
- Camera transitions from global to drone view
- Basic drone deployment system
- Geospatial coordinate conversion system

### 🔄 In Progress
- Fire suppression mechanics and visual effects
- Complete drone fleet management system
- Economic scoring system implementation
- UI/UX for both strategic and tactical views
- Google Maps 3D tiles integration for tactical view

### 📋 Planned Features
- Multi-national drone specifications and capabilities
- Advanced fire spread simulation
- Resource management and logistics
- Post-mission analytics and scoring
- Performance optimization for WebGL deployment

## Technical Stack

- **Engine**: Unity 2023.x with Universal Render Pipeline (URP)
- **Geospatial**: Cesium for Unity (3D globe and coordinate systems)
- **Data Sources**: 
  - NASA FIRMS (Fire Information for Resource Management System)
  - NASA EONET (Earth Observatory Natural Event Tracker)
  - Google Maps Platform (Photorealistic 3D Tiles)
- **Platform Targets**: PC (Primary), WebGL (Secondary)

## Project Structure

```
web/               # Web prototype (Next.js + Three.js) — see above
Assets/
├── Scripts/           # Core game logic and systems
│   ├── Core/         # Game managers and state systems
│   ├── Data/         # Data models and structures
│   ├── API/          # External API integrations
│   ├── Geospatial/   # Location and coordinate systems
│   ├── Fire/         # Fire simulation and effects
│   ├── Drones/       # Drone fleet management
│   ├── Systems/      # Scoring, camera, and other systems
│   └── UI/           # User interface components
├── Research/         # Technical documentation and requirements
└── README.md         # This file
```

## Development Team

This project is being developed as part of the NDIA Hackathon, focusing on innovative applications of real-time data for emergency response and strategic planning.

## Getting Started

### Prerequisites
- Unity 2023.x or later
- NASA Earthdata API key (for FIRMS data)
- Google Cloud API key (for Maps Platform)
- Cesium for Unity plugin

### Current Build Status
The project currently compiles successfully with all core systems functional. The basic fire interaction and camera transition systems are operational for demonstration purposes.

## License

This project is developed for the NDIA Hackathon and is currently proprietary. Licensing terms will be determined based on hackathon outcomes and further development decisions.

---

**Note**: This is an active development project. Features and implementation details are subject to change as development progresses toward the hackathon deadline.
