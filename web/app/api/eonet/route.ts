import { NextResponse } from 'next/server';

export const runtime = 'nodejs';
export const dynamic = 'force-dynamic';

/** Proxies EONET v3 open wildfire events (public API, no key). */
export async function GET() {
  const url = 'https://eonet.gsfc.nasa.gov/api/v3/events?category=wildfires&status=open&limit=200';
  try {
    const res = await fetch(url, { next: { revalidate: 3600 } });
    if (!res.ok) return NextResponse.json({ error: `EONET ${res.status}` }, { status: 502 });
    const json = await res.json();
    return NextResponse.json(json, { headers: { 'cache-control': 'public, max-age=3600' } });
  } catch (e) {
    return NextResponse.json({ error: (e as Error).message }, { status: 502 });
  }
}
