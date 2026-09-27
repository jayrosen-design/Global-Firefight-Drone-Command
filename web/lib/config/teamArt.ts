import type { CountryCode } from './fleets';

/**
 * Pitch-deck artwork extracted from Assets/Research/Global Firefight Drone Command Pitch.pdf
 * (slides 17–22 equipment + insignia, slides 32–37 team line-ups). See public/teams/README.md.
 */
export const droneArtUrl = (code: CountryCode) => `/teams/${code}/drone.webp`;
export const carrierArtUrl = (code: CountryCode) => `/teams/${code}/carrier.webp`;
export const insigniaUrl = (code: CountryCode) => `/teams/${code}/insignia.webp`;
/** index is the crew position (0-based), matching the left-to-right figure order on the team slide. */
export const portraitUrl = (code: CountryCode, index: number) => `/teams/${code}/crew/${index + 1}.webp`;
