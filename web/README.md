# Global Firefight: Drone Command — Web Prototype (God Eye fork)

Dual-mode RTS / third-person tactical firefighting simulation on a live NASA fire globe.
Built with Next.js 14 (App Router), TypeScript, Tailwind, Zustand, Three.js r170 and React Three Fiber.

## Screenshots

| Campaign menu | Global RTS — free play on the live FIRMS globe |
| --- | --- |
| ![Campaign menu](docs/screenshots/01-campaign-menu.png) | ![Global RTS free play](docs/screenshots/02-global-rts-freeplay.png) |

| RTS dispatch — Black Summer, arc trajectory + carrier | Tactical drone view — standard colour |
| --- | --- |
| ![RTS dispatch](docs/screenshots/03-rts-dispatch.png) | ![Tactical standard](docs/screenshots/04-tactical-standard.png) |

| Tactical — retardant drop knock-down on the reticle | Tactical — IR White-Hot (PERSONNEL DETECTED, hazard callouts) |
| --- | --- |
| ![Tactical drop](docs/screenshots/05-tactical-drop.png) | ![Tactical IR](docs/screenshots/06-tactical-ir-white-hot.png) |

| Tactical — LIDAR point cloud (canopy pathways) | Mission debrief — economic ledger |
| --- | --- |
| ![Tactical LIDAR](docs/screenshots/07-tactical-lidar.png) | ![Mission debrief](docs/screenshots/08-mission-debrief.png) |

Captured from a scripted headless Chromium run against the bundled fallback dataset (no FIRMS key configured).

## Run

```bash
cd web
npm install
cp .env.example .env.local   # optional — see below
npm run dev                  # http://localhost:3000
```

Without a FIRMS key the app uses a bundled fallback fire dataset so it always runs. For live data set
`FIRMS_MAP_KEY` (free key: https://firms.modaps.eosdis.nasa.gov/api/map_key/). EONET needs no key.
Set `NEXT_PUBLIC_BASEMAP_TILE_URL` to a `{z}/{x}/{y}` raster template to stream a tile basemap onto the globe.

## Controls

| Global RTS | Tactical drone |
| --- | --- |
| Drag to orbit, wheel to zoom | `W/S` throttle, `A/D` yaw, `Q/E` altitude, `SHIFT` boost |
| Click carrier → click fire = dispatch | `SPACE` drop suppressant (reticle turns gold when the predicted impact is on a fire) |
| Click a live FIRMS hotspot to engage it | `R` (hold, < 30 m AGL, within 40 m) extract a civilian |
| `MOVE` then click globe relocates a carrier | `V` cycle Standard / IR White-Hot / LIDAR |
| Fleet panel → click a nation, then the globe, to deploy a forward carrier | `ESC` back to the global view |

## Software architecture

### 1. Layered system architecture

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

### 2. Domain model

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

### 3. Application mode state machine

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

### 4. Drone sortie state machine

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

### 5. Sequence — data load and RTS dispatch

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

### 6. Sequence — tactical drop

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

### 7. Rendering pipeline per frame

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

### Runtime notes

* **Simulation clock** — the RTS runs at 60× (1 real second = 1 simulated minute, with 1×/3×/8× multipliers), the tactical view at 6×. A 40 km sortie takes about a minute of real time while property loss stays gradual.
* **Coordinate systems** — globe space is a unit sphere (1 unit = 6371 km); the tactical arena is a local tangent plane in metres with 0.12× geographic compression so a 15 km fire complex fits a ±3 km flyable area.
* **Data fallback** — when either proxy fails (no key, network policy, upstream error) the store seeds a deterministic offline dataset and the HUD reports `FEED FALLBACK`.
* **Economic model** — `FinalScore = (PropertyValueSaved + LivesSavedBonus) − TotalSuppressionCost`, where lives bonus = civilians × $10M VSL + residents protected × $12.5K, and cost = deployment + flight time + payload per nation.

## Code layout

```
app/                      Next.js App Router shell + API proxies
  api/firms/route.ts      FIRMS area-CSV proxy (server-side MAP_KEY, 15-min revalidate)
  api/eonet/route.ts      EONET v3 open wildfire events proxy
lib/geo/wgs84.ts          lat/lon ⇄ globe Cartesian, haversine, great-circle interpolation
lib/geo/trajectory.ts     TrajectoryManager — great-circle arcs with parabolic altitude
lib/geo/earthTexture.ts   Dark tactical basemap from Natural Earth (world-atlas) TopoJSON
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

### Economic model

`FinalScore = (PropertyValueSaved + LivesSavedBonus) − TotalSuppressionCost`

* Property saved is credited when a fire is extinguished (what is still standing at that moment).
* Lives Saved Bonus = civilians rescued × $10M (value of a statistical life) + residents protected × $12.5K.
* Suppression cost = deployment (per sortie) + flight time (per minute) + payload (per litre), per nation.

### Vision modes

* **IR White-Hot** — all materials go monochrome, fires and personnel render white with
  `PERSONNEL DETECTED` and `CRITICAL STRUCTURE / VALVE HAZARD` callouts.
* **LIDAR** — terrain and canopy render as an elevation-coloured point cloud (cyan → green), buildings as wireframe,
  fires as ground rings, so pathways through smoke stay legible.

### God Eye adaptation

The globe layer follows God Eye's approach (unit-sphere WGS84 globe, streamed Web-Mercator raster tiles as sphere
patches, fresnel atmosphere) but is re-implemented here in React Three Fiber; no God Eye source is vendored.

## Scripts

`npm run dev` · `npm run build` · `npm run start` · `npm run lint` · `npm run typecheck`
