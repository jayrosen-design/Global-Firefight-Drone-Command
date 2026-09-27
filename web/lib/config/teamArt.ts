import type { CountryCode, CrewMember } from './fleets';

/** File convention for pitch-deck art — see public/teams/README.md. */
export const TEAM_ART_EXT = 'jpg';

export function conceptArtUrl(code: CountryCode, frame: 'concept' | 'scene' = 'concept') {
  return `/teams/${code}/${frame}.${TEAM_ART_EXT}`;
}

export function portraitUrl(code: CountryCode, member: CrewMember) {
  return `/teams/${code}/crew/${member.callSign}.${TEAM_ART_EXT}`;
}
