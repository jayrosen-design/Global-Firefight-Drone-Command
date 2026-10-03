/** Store / marketing links. Set NEXT_PUBLIC_STEAM_URL to the game's Steam store page once it exists. */
export const STEAM_URL = process.env.NEXT_PUBLIC_STEAM_URL || 'https://store.steampowered.com/search/?term=Global%20Firefight%20Drone%20Command';
export const STEAM_PAGE_LIVE = Boolean(process.env.NEXT_PUBLIC_STEAM_URL);
export const PLAY_PATH = '/play';
export const REPO_URL = 'https://github.com/jayrosen-design/Global-Firefight-Drone-Command';
