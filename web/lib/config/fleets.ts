/**
 * Country-specific arsenal: drones, carriers, liveries, abilities and costs.
 * Colours are the primary drivers for the procedural 3D liveries.
 */
export type CountryCode = 'USA' | 'CAN' | 'BRA' | 'CHN' | 'DEU' | 'AUS';

export type SuppressantType = 'water' | 'foam' | 'retardant';

export interface Ability {
  id: string;
  name: string;
  description: string;
}

export interface DroneSpec {
  model: string;
  agency: string;
  /** Payload in litres */
  payloadLitres: number;
  suppressant: SuppressantType;
  /** Cruise speed km/h in the RTS view */
  cruiseKmh: number;
  /** Endurance in minutes */
  enduranceMin: number;
  /** Drones dispatched per sortie (Canada / China swarm) */
  swarmSize: number;
  /** Tactical-view stats */
  tactical: { maxSpeed: number; turnRate: number; dropRadius: number };
  livery: { primary: string; secondary: string; accent: string; description: string };
  abilities: Ability[];
}

export interface CarrierSpec {
  model: string;
  description: string;
  livery: { primary: string; secondary: string; accent: string };
  droneCapacity: number;
  /** Rearm time for a returning drone, seconds */
  rearmSeconds: number;
}

export interface FleetConfig {
  code: CountryCode;
  country: string;
  flag: string;
  agency: string;
  drone: DroneSpec;
  carrier: CarrierSpec;
  /** Default regional base (lat, lon, label) */
  base: { lat: number; lon: number; label: string };
  costs: { deploymentUSD: number; flightPerMinuteUSD: number; payloadPerLitreUSD: number };
}

export const SUPPRESSANT_EFFECTIVENESS: Record<SuppressantType, number> = {
  water: 1.0,
  foam: 1.35,
  retardant: 1.6,
};

export const FLEETS: Record<CountryCode, FleetConfig> = {
  USA: {
    code: 'USA',
    country: 'United States',
    flag: '🇺🇸',
    agency: 'USFS / CAL FIRE',
    drone: {
      model: 'Guardian Mk IV',
      agency: 'USFS',
      payloadLitres: 1200,
      suppressant: 'retardant',
      cruiseKmh: 380,
      enduranceMin: 140,
      swarmSize: 1,
      tactical: { maxSpeed: 46, turnRate: 1.6, dropRadius: 14 },
      livery: { primary: '#e8ebee', secondary: '#7d858d', accent: '#ff7a1a', description: 'White/grey with USFS badge and orange accents' },
      abilities: [
        { id: 'thermal', name: 'Thermal Imaging', description: 'IR white-hot vision reveals hotspots and personnel through smoke.' },
        { id: 'pinpoint', name: 'PinPoint Target Marking', description: 'Laser-designates a structure for priority protection.' },
        { id: 'phoschek', name: 'Phos-Chek Retardant Lines', description: 'Lays a long-term retardant line that halts spread.' },
      ],
    },
    carrier: {
      model: 'USFS Guardian Carrier Mk IV',
      description: 'Heavy 6x6 red/white command truck',
      livery: { primary: '#c8102e', secondary: '#f4f4f4', accent: '#ffb000' },
      droneCapacity: 4,
      rearmSeconds: 45,
    },
    base: { lat: 38.58, lon: -121.49, label: 'CAL FIRE Sacramento HQ' },
    costs: { deploymentUSD: 50000, flightPerMinuteUSD: 500, payloadPerLitreUSD: 25 },
  },
  CAN: {
    code: 'CAN',
    country: 'Canada',
    flag: '🇨🇦',
    agency: 'BC Wildfire Service',
    drone: {
      model: 'FireSwarm Thunder Wasp Mk II',
      agency: 'BC Wildfire Service',
      payloadLitres: 900,
      suppressant: 'foam',
      cruiseKmh: 320,
      enduranceMin: 110,
      swarmSize: 3,
      tactical: { maxSpeed: 40, turnRate: 1.9, dropRadius: 12 },
      livery: { primary: '#f7f7f7', secondary: '#d80621', accent: '#d80621', description: 'White/red Maple Leaf livery' },
      abilities: [
        { id: 'swarm', name: 'Swarm Coordination', description: 'Three-drone synchronized drops triple line coverage.' },
        { id: 'heavypayload', name: 'Heavy Water/Foam Payload', description: 'Class-A foam for structure protection and ember storms.' },
      ],
    },
    carrier: {
      model: 'FIRE INCENDIE Mobile Carrier',
      description: 'Bilingual red/white heavy carrier',
      livery: { primary: '#d80621', secondary: '#ffffff', accent: '#ffffff' },
      droneCapacity: 6,
      rearmSeconds: 40,
    },
    base: { lat: 53.55, lon: -113.49, label: 'Edmonton Forward Base' },
    costs: { deploymentUSD: 42000, flightPerMinuteUSD: 420, payloadPerLitreUSD: 15 },
  },
  BRA: {
    code: 'BRA',
    country: 'Brazil',
    flag: '🇧🇷',
    agency: 'Corpo de Bombeiros / CBMGO',
    drone: {
      model: 'Nauru 500C ISR / Aerobombeiro Arara',
      agency: 'CBMGO',
      payloadLitres: 700,
      suppressant: 'foam',
      cruiseKmh: 290,
      enduranceMin: 240,
      swarmSize: 1,
      tactical: { maxSpeed: 38, turnRate: 1.5, dropRadius: 12 },
      livery: { primary: '#009c3b', secondary: '#ffdf00', accent: '#002776', description: 'Green/yellow/navy Amazon scheme' },
      abilities: [
        { id: 'vtol', name: 'Long-Range VTOL', description: 'Vertical launch with 4-hour endurance over roadless rainforest.' },
        { id: 'riverine', name: 'Amazon Riverine Scan', description: 'Maps river channels as natural firebreaks and water sources.' },
        { id: 'humidfoam', name: 'High-Humidity Foam Drop', description: 'Foam formulated for tropical humidity and peat ignition.' },
      ],
    },
    carrier: {
      model: 'Aerobombeiro Arara Carrier',
      description: 'Riverine-capable green/yellow carrier',
      livery: { primary: '#009c3b', secondary: '#ffdf00', accent: '#002776' },
      droneCapacity: 4,
      rearmSeconds: 50,
    },
    base: { lat: -15.79, lon: -47.88, label: 'Brasília CBM Command' },
    costs: { deploymentUSD: 36000, flightPerMinuteUSD: 380, payloadPerLitreUSD: 15 },
  },
  CHN: {
    code: 'CHN',
    country: 'China',
    flag: '🇨🇳',
    agency: 'CN Fire / EHang',
    drone: {
      model: 'EHang 216F',
      agency: '中国消防 CN FIRE',
      payloadLitres: 150,
      suppressant: 'foam',
      cruiseKmh: 130,
      enduranceMin: 35,
      swarmSize: 3,
      tactical: { maxSpeed: 36, turnRate: 2.2, dropRadius: 8 },
      livery: { primary: '#d7191c', secondary: '#ffffff', accent: '#ffcc00', description: 'Red/white with Mandarin/English firefighting branding' },
      abilities: [
        { id: 'highrise', name: 'Urban High-Rise Foam Nozzles', description: 'Six-rotor hover for 600m-altitude building fires.' },
        { id: 'projectile', name: 'Extinguisher Window Projectiles', description: 'Breaches glazing and fires dry-powder canisters inside.' },
        { id: 'swarm', name: 'Swarm Mode', description: 'Three-craft coordinated attack.' },
      ],
    },
    carrier: {
      model: 'Air Control Carrier',
      description: 'Red/white airfield control carrier',
      livery: { primary: '#d7191c', secondary: '#f2f2f2', accent: '#ffcc00' },
      droneCapacity: 9,
      rearmSeconds: 30,
    },
    base: { lat: 30.57, lon: 104.07, label: 'Chengdu Fire Aviation Base' },
    costs: { deploymentUSD: 22000, flightPerMinuteUSD: 260, payloadPerLitreUSD: 15 },
  },
  DEU: {
    code: 'DEU',
    country: 'Germany',
    flag: '🇩🇪',
    agency: 'Feuerwehr',
    drone: {
      model: 'DE GF-CA',
      agency: 'Feuerwehr',
      payloadLitres: 600,
      suppressant: 'water',
      cruiseKmh: 300,
      enduranceMin: 120,
      swarmSize: 1,
      tactical: { maxSpeed: 42, turnRate: 1.8, dropRadius: 10 },
      livery: { primary: '#e2001a', secondary: '#ffd400', accent: '#ffffff', description: 'Bright red/yellow Feuerwehr livery' },
      abilities: [
        { id: 'uxo', name: 'UXO Infrared Sweep', description: 'Flags unexploded ordnance so ground crews stay clear.' },
        { id: 'perimeter', name: 'Precision Forest Perimeter Spray', description: 'Metre-accurate perimeter line along gorge rims.' },
      ],
    },
    carrier: {
      model: 'Feuerwehr Drone Carrier',
      description: 'Red/yellow rapid-response carrier',
      livery: { primary: '#e2001a', secondary: '#ffd400', accent: '#ffffff' },
      droneCapacity: 4,
      rearmSeconds: 40,
    },
    base: { lat: 51.05, lon: 13.74, label: 'Dresden Feuerwehr Leitstelle' },
    costs: { deploymentUSD: 45000, flightPerMinuteUSD: 460, payloadPerLitreUSD: 10 },
  },
  AUS: {
    code: 'AUS',
    country: 'Australia',
    flag: '🇦🇺',
    agency: 'NSW RFS',
    drone: {
      model: 'RFS Kookaburra Mk III',
      agency: 'NSW Rural Fire Service',
      payloadLitres: 1000,
      suppressant: 'retardant',
      cruiseKmh: 400,
      enduranceMin: 180,
      swarmSize: 1,
      tactical: { maxSpeed: 48, turnRate: 1.7, dropRadius: 13 },
      livery: { primary: '#0b3d2e', secondary: '#c9a227', accent: '#ffffff', description: 'Dark green/gold with NSW RFS logo' },
      abilities: [
        { id: 'recon', name: 'Rapid Recon', description: 'High-speed reconnaissance sweep of the fire front.' },
        { id: 'highwind', name: 'High-Wind Stabilization', description: 'Maintains drop accuracy in 60 km/h gusts.' },
        { id: 'ferry', name: 'Long-Range Coastal Ferry', description: 'Extended ferry range along the coast-to-alpine span.' },
      ],
    },
    carrier: {
      model: 'Kookaburra Control Carrier',
      description: 'Green/gold RFS control carrier',
      livery: { primary: '#0b3d2e', secondary: '#c9a227', accent: '#ffffff' },
      droneCapacity: 4,
      rearmSeconds: 45,
    },
    base: { lat: -33.87, lon: 151.21, label: 'NSW RFS Sydney HQ' },
    costs: { deploymentUSD: 48000, flightPerMinuteUSD: 480, payloadPerLitreUSD: 25 },
  },
};

export const COUNTRY_CODES = Object.keys(FLEETS) as CountryCode[];

/** High-contrast colour for HUD chrome (Brazil's navy accent is too dark on the tactical UI). */
export function fleetUiColor(code: CountryCode): string {
  const l = FLEETS[code].drone.livery;
  return code === 'BRA' ? l.secondary : l.accent;
}
