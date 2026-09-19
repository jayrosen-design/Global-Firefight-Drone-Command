export interface FirmsHotspot {
  id: string;
  latitude: number;
  longitude: number;
  /** Fire Radiative Power in megawatts */
  frp: number;
  /** 0-100 (VIIRS reports l/n/h which we map to 30/60/90) */
  confidence: number;
  acqDate?: string;
  acqTime?: string;
  satellite?: string;
  daynight?: 'D' | 'N';
}

export interface EonetEvent {
  id: string;
  title: string;
  link: string;
  latitude: number;
  longitude: number;
  date?: string;
}

export interface FireFeed {
  source: 'live' | 'fallback';
  fetchedAt: number;
  hotspots: FirmsHotspot[];
  events: EonetEvent[];
  message?: string;
}
