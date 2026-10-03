import { FLEETS, type CountryCode } from '@/lib/config/fleets';
import { flagUrl } from '@/lib/config/teamArt';

/**
 * National flag as an image (from the pitch deck), not an emoji: Windows does not render
 * regional-indicator flag emoji, so emoji flags show up as letter pairs there.
 */
export function Flag({ code, h = 14, className = '' }: { code: CountryCode; h?: number; className?: string }) {
  return (
    // eslint-disable-next-line @next/next/no-img-element
    <img src={flagUrl(code)} alt={`${FLEETS[code].country} flag`} className={`flag ${className}`} style={{ height: h }} draggable={false} />
  );
}
