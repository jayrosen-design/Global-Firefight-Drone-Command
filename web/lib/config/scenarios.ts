/**
 * Pitch-deck campaign presets. Each preset positions the camera, seeds the
 * historical fire cluster, places the national carrier and lists objectives.
 */
import type { CountryCode } from './fleets';

export interface ScenarioFire {
  lat: number;
  lon: number;
  frp: number;
  label?: string;
}

export interface Objective {
  id: string;
  title: string;
  optional?: boolean;
  /** Structure to protect (rendered in tactical view) */
  protect?: { lat: number; lon: number; valueUSD: number; label: string };
  /** Civilians to rescue */
  civilians?: number;
  /** Retardant line required */
  line?: boolean;
}

export interface Scenario {
  id: string;
  country: CountryCode;
  title: string;
  location: string;
  year: string;
  center: { lat: number; lon: number };
  constraints: string[];
  objectives: Objective[];
  fires: ScenarioFire[];
  carrier: { lat: number; lon: number; label: string };
  wind: { speedMph: number; directionDeg: number; label: string };
  budgetUSD: number;
  /** Property and lives at risk used by the economic model */
  propertyAtRiskUSD: number;
  populationAtRisk: number;
  /** Named EONET-style event marker */
  eventTitle: string;
}

export const SCENARIOS: Scenario[] = [
  {
    id: 'paradise',
    country: 'USA',
    title: 'Camp Ridge Complex',
    location: 'Paradise, California',
    year: '2018',
    center: { lat: 39.76, lon: -121.62 },
    constraints: ['35–50 MPH NE Red Flag winds', 'Tight WUI canyons', 'Single evacuation route (Skyway)'],
    objectives: [
      { id: 'hospital', title: 'Protect Paradise Ridge Hospital', protect: { lat: 39.771, lon: -121.6, valueUSD: 180_000_000, label: 'Paradise Ridge Hospital' } },
      { id: 'skyway', title: 'Establish retardant line along Skyway', line: true },
      { id: 'rescue', title: 'Rescue 5 civilians', optional: true, civilians: 5 },
    ],
    fires: [
      { lat: 39.813, lon: -121.437, frp: 1450, label: 'Camp Fire origin — Pulga' },
      { lat: 39.79, lon: -121.52, frp: 980 },
      { lat: 39.775, lon: -121.58, frp: 1200 },
      { lat: 39.76, lon: -121.62, frp: 860 },
      { lat: 39.745, lon: -121.66, frp: 620 },
      { lat: 39.73, lon: -121.6, frp: 410 },
    ],
    carrier: { lat: 39.51, lon: -121.55, label: 'Oroville Staging' },
    wind: { speedMph: 45, directionDeg: 225, label: 'NE Red Flag' },
    budgetUSD: 4_000_000,
    propertyAtRiskUSD: 16_500_000_000,
    populationAtRisk: 26_000,
    eventTitle: 'Camp Fire, Butte County',
  },
  {
    id: 'lahaina',
    country: 'USA',
    title: 'Lahaina Firestorm',
    location: 'Lahaina, Maui, Hawaiʻi',
    year: '2023',
    center: { lat: 20.878, lon: -156.68 },
    constraints: ['Strong coastal wind events (Hurricane Dora gradient)', 'Urban–wildland edge', 'Power-infrastructure hazards'],
    objectives: [
      { id: 'town', title: 'Preserve historic coastal town (Front Street)', protect: { lat: 20.874, lon: -156.679, valueUSD: 5_500_000_000, label: 'Front Street Historic District' } },
      { id: 'poles', title: 'Extinguish power-pole ignitions', line: false },
      { id: 'rescue', title: 'Rescue 8 trapped residents', optional: true, civilians: 8 },
    ],
    fires: [
      { lat: 20.887, lon: -156.66, frp: 720, label: 'Lahainaluna Rd ignition' },
      { lat: 20.882, lon: -156.67, frp: 940 },
      { lat: 20.876, lon: -156.677, frp: 1300 },
      { lat: 20.869, lon: -156.68, frp: 880 },
      { lat: 20.862, lon: -156.672, frp: 520 },
    ],
    carrier: { lat: 20.899, lon: -156.43, label: 'Kahului Airport Staging' },
    wind: { speedMph: 65, directionDeg: 260, label: 'E downslope gusts' },
    budgetUSD: 3_500_000,
    propertyAtRiskUSD: 5_500_000_000,
    populationAtRisk: 12_700,
    eventTitle: 'Lahaina Fire, Maui',
  },
  {
    id: 'fortmcmurray',
    country: 'CAN',
    title: 'Horse River Complex',
    location: 'Fort McMurray, Alberta',
    year: '2016',
    center: { lat: 56.725, lon: -111.381 },
    constraints: ['Ember storms', 'Oil-sands adjacency', 'Low humidity / crossover conditions'],
    objectives: [
      { id: 'hospital', title: 'Protect Northern Lights Regional Health Centre', protect: { lat: 56.733, lon: -111.39, valueUSD: 420_000_000, label: 'Northern Lights Regional Health Centre' } },
      { id: 'river', title: 'Hold line at Athabasca River', line: true },
      { id: 'rescue', title: 'Escort 6 stranded evacuees', optional: true, civilians: 6 },
    ],
    fires: [
      { lat: 56.66, lon: -111.5, frp: 1600, label: 'Horse River origin' },
      { lat: 56.69, lon: -111.45, frp: 1250 },
      { lat: 56.71, lon: -111.42, frp: 1100 },
      { lat: 56.725, lon: -111.381, frp: 900 },
      { lat: 56.74, lon: -111.36, frp: 640 },
      { lat: 56.76, lon: -111.33, frp: 380 },
    ],
    carrier: { lat: 56.65, lon: -111.22, label: 'Fort McMurray Airport Staging' },
    wind: { speedMph: 25, directionDeg: 60, label: 'SW gusting' },
    budgetUSD: 4_500_000,
    propertyAtRiskUSD: 9_900_000_000,
    populationAtRisk: 88_000,
    eventTitle: 'Horse River Wildfire, Fort McMurray',
  },
  {
    id: 'pantanal',
    country: 'BRA',
    title: 'Amazonas / Pantanal Surges',
    location: 'Pantanal, Mato Grosso do Sul',
    year: '2020–2024',
    center: { lat: -17.9, lon: -57.4 },
    constraints: ['Wetland islands', 'Riverine communities', 'Heavy smoke-plume navigation'],
    objectives: [
      { id: 'corridor', title: 'Safeguard jaguar wildlife corridor', protect: { lat: -17.85, lon: -57.45, valueUSD: 750_000_000, label: 'Encontro das Águas Wildlife Refuge' } },
      { id: 'ignitions', title: 'Suppress dispersed rainforest ignitions' },
      { id: 'rescue', title: 'Evacuate 4 riverine residents', optional: true, civilians: 4 },
    ],
    fires: [
      { lat: -17.6, lon: -57.7, frp: 540 },
      { lat: -17.75, lon: -57.55, frp: 870 },
      { lat: -17.9, lon: -57.4, frp: 1150, label: 'Pantanal surge core' },
      { lat: -18.05, lon: -57.3, frp: 690 },
      { lat: -18.2, lon: -57.1, frp: 430 },
      { lat: -17.5, lon: -57.2, frp: 320 },
      { lat: -18.3, lon: -57.6, frp: 510 },
    ],
    carrier: { lat: -19.0, lon: -57.65, label: 'Corumbá Riverine Base' },
    wind: { speedMph: 15, directionDeg: 120, label: 'Dry NW' },
    budgetUSD: 3_000_000,
    propertyAtRiskUSD: 2_100_000_000,
    populationAtRisk: 6_500,
    eventTitle: 'Pantanal Wildfires, Brazil',
  },
  {
    id: 'chongqing',
    country: 'CHN',
    title: 'Chongqing & Liangshan Mountains',
    location: 'Beibei District, Chongqing',
    year: '2022',
    center: { lat: 29.83, lon: 106.43 },
    constraints: ['Extreme 45 °C heatwave terrain', 'High-altitude conifer forests', 'Sudden wind shifts'],
    objectives: [
      { id: 'villages', title: 'Protect mountain villages', protect: { lat: 29.84, lon: 106.41, valueUSD: 260_000_000, label: 'Jinyun Mountain villages' } },
      { id: 'multidrone', title: 'Coordinate multi-drone high-rise & forest suppression', line: true },
      { id: 'rescue', title: 'Evacuate 10 hikers', optional: true, civilians: 10 },
    ],
    fires: [
      { lat: 29.86, lon: 106.44, frp: 780, label: 'Jinyun Mountain fire' },
      { lat: 29.83, lon: 106.43, frp: 1020 },
      { lat: 29.8, lon: 106.45, frp: 860 },
      { lat: 29.78, lon: 106.4, frp: 470 },
      { lat: 28.3, lon: 102.9, frp: 640, label: 'Liangshan conifer fire' },
      { lat: 28.25, lon: 102.95, frp: 520 },
    ],
    carrier: { lat: 29.72, lon: 106.64, label: 'Jiangbei Airfield Staging' },
    wind: { speedMph: 20, directionDeg: 180, label: 'Shifting N/S' },
    budgetUSD: 2_800_000,
    propertyAtRiskUSD: 3_400_000_000,
    populationAtRisk: 42_000,
    eventTitle: 'Chongqing Mountain Fires',
  },
  {
    id: 'saxon',
    country: 'DEU',
    title: 'Saxon Switzerland & UXO Zones',
    location: 'Sächsische Schweiz National Park',
    year: '2022',
    center: { lat: 50.91, lon: 14.23 },
    constraints: ['Sandstone gorges', 'Munitions-contaminated forests (UXO risk)', 'No ground access'],
    objectives: [
      { id: 'irsweep', title: 'Conduct aerial IR sweep of UXO zone', protect: { lat: 50.915, lon: 14.25, valueUSD: 140_000_000, label: 'Bad Schandau spa town' } },
      { id: 'firebreak', title: 'Build secure fire breaks without ground risk', line: true },
      { id: 'rescue', title: 'Locate 3 missing hikers', optional: true, civilians: 3 },
    ],
    fires: [
      { lat: 50.9, lon: 14.2, frp: 380 },
      { lat: 50.91, lon: 14.23, frp: 560, label: 'Großer Winterberg' },
      { lat: 50.92, lon: 14.27, frp: 450 },
      { lat: 50.88, lon: 14.3, frp: 300 },
      { lat: 50.86, lon: 14.36, frp: 410, label: 'Hřensko (CZ side)' },
    ],
    carrier: { lat: 50.99, lon: 13.95, label: 'Pirna Feuerwache' },
    wind: { speedMph: 12, directionDeg: 90, label: 'Light W' },
    budgetUSD: 2_500_000,
    propertyAtRiskUSD: 900_000_000,
    populationAtRisk: 3_800,
    eventTitle: 'Bohemian–Saxon Switzerland Fire',
  },
  {
    id: 'blacksummer',
    country: 'AUS',
    title: 'Black Summer Megacomplexes',
    location: 'NSW South Coast / East Gippsland',
    year: '2019–20',
    center: { lat: -36.2, lon: 149.6 },
    constraints: ['Coast-to-alpine spans', 'Heavy smoke inversions', 'Extreme ember cast (up to 30 km)'],
    objectives: [
      { id: 'wui', title: 'Defend WUI corridors (Princes Highway)', protect: { lat: -36.25, lon: 149.9, valueUSD: 1_200_000_000, label: 'Cobargo township' } },
      { id: 'species', title: 'Protect endangered species refuges', protect: { lat: -35.7, lon: 150.1, valueUSD: 400_000_000, label: 'Koala refuge — Murramarang' } },
      { id: 'rescue', title: 'Evacuate 12 beach-stranded residents', optional: true, civilians: 12 },
    ],
    fires: [
      { lat: -35.7, lon: 150.0, frp: 1900, label: 'Currowan megafire' },
      { lat: -36.0, lon: 149.7, frp: 1500, label: 'Badja Forest Rd' },
      { lat: -36.25, lon: 149.85, frp: 1300 },
      { lat: -36.6, lon: 149.4, frp: 900 },
      { lat: -37.3, lon: 148.8, frp: 1700, label: 'East Gippsland complex' },
      { lat: -37.0, lon: 149.2, frp: 1100 },
      { lat: -36.4, lon: 148.3, frp: 800, label: 'Dunns Road — Snowy Mountains' },
    ],
    carrier: { lat: -36.09, lon: 150.13, label: 'Moruya Airfield Staging' },
    wind: { speedMph: 40, directionDeg: 135, label: 'NW hot & dry' },
    budgetUSD: 6_000_000,
    propertyAtRiskUSD: 7_800_000_000,
    populationAtRisk: 65_000,
    eventTitle: 'Black Summer Bushfires',
  },
];

export const SCENARIO_BY_ID = Object.fromEntries(SCENARIOS.map((s) => [s.id, s])) as Record<string, Scenario>;
