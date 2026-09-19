import { NextResponse } from 'next/server';

export const runtime = 'nodejs';
export const dynamic = 'force-dynamic';

/**
 * Proxies the NASA FIRMS area CSV endpoint using the server-side MAP_KEY so the
 * key never ships to the browser. Returns raw CSV; the client parses it.
 * Docs: https://firms.modaps.eosdis.nasa.gov/api/area/
 */
export async function GET() {
  const key = process.env.FIRMS_MAP_KEY;
  if (!key) {
    return NextResponse.json({ error: 'FIRMS_MAP_KEY not configured; using fallback dataset' }, { status: 503 });
  }
  const source = process.env.FIRMS_SOURCE ?? 'VIIRS_SNPP_NRT';
  const days = process.env.FIRMS_DAYS ?? '1';
  const url = `https://firms.modaps.eosdis.nasa.gov/api/area/csv/${key}/${source}/world/${days}`;
  try {
    const res = await fetch(url, { next: { revalidate: 900 } });
    if (!res.ok) return NextResponse.json({ error: `FIRMS ${res.status}` }, { status: 502 });
    const csv = await res.text();
    return new NextResponse(csv, {
      headers: { 'content-type': 'text/csv; charset=utf-8', 'cache-control': 'public, max-age=900' },
    });
  } catch (e) {
    return NextResponse.json({ error: (e as Error).message }, { status: 502 });
  }
}
