/**
 * Campaign briefings: an in-game newspaper front page for each historic fire, plus the mission
 * story. The masthead is fictional; events and figures are summarised from public reports.
 * Press photos are in-engine halftone illustrations (scripts/press → public/campaigns/<id>.png).
 */

export interface CampaignStory {
  /** Front-page edition label under the fictional masthead. */
  edition: string;
  /** Front-page date — the day after the fire's breakout. */
  date: string;
  headline: string;
  deck: string;
  /** Article paragraphs; the first opens with the dateline. */
  article: string[];
  caption: string;
  /** What history recorded — the toll this mission tries to rewrite. */
  record: { value: string; label: string }[];
  /** The player's mission framing. */
  situation: string;
}

export const NEWSPAPER_MASTHEAD = 'The Fireline Herald';

export const CAMPAIGN_STORIES: Record<string, CampaignStory> = {
  paradise: {
    edition: 'Northern California Edition',
    date: 'Friday, November 9, 2018',
    headline: 'Paradise Lost',
    deck: 'Wind-driven Camp Fire overruns Butte County ridge town as thousands flee on a single gridlocked road',
    article: [
      'PARADISE, Calif. — A fire that began near Pulga in the Feather River Canyon early Thursday raced into the town of Paradise within hours, pushed by dry northeast winds. Residents abandoned cars on the jammed Skyway and fled on foot as flames reached both sides of the road.',
      'At times the fire advanced faster than a football field every second, throwing embers far ahead of the main front. Hospitals and nursing homes were evacuated under smoke as crews fought to hold escape routes open.',
    ],
    caption: 'Illustration: evacuees on the Skyway as the fire crests the ridge.',
    record: [
      { value: '85', label: 'lives lost' },
      { value: '18,800+', label: 'structures destroyed' },
      { value: '153,336', label: 'acres burned' },
    ],
    situation:
      "Dawn, November 8. A failed transmission line has lit the canyon above Pulga and 45 mph Red Flag winds are driving embers toward Paradise. Your Guardian carrier is staged at Oroville. Get drones over the fire threatening the hospital before the town's only real escape route is cut.",
  },
  lahaina: {
    edition: 'Hawaiʻi Edition',
    date: 'Thursday, August 10, 2023',
    headline: 'Lahaina in Ashes',
    deck: 'Hurricane-driven winds push wildfire through historic Front Street; residents flee into the harbor',
    article: [
      'LAHAINA, Maui — Gusts topping 60 mph, driven by the pressure gradient between high pressure to the north and Hurricane Dora passing far to the south, pushed a fast-moving fire into the historic town on Tuesday afternoon. Some residents escaped into the ocean along the seawall as smoke closed over Front Street.',
      'The 150-year-old banyan tree in Courthouse Square was scorched but still standing as crews and residents took stock of a waterfront that had been the capital of the Hawaiian Kingdom.',
    ],
    caption: 'Illustration: Front Street burns as wind drives smoke out over the harbor.',
    record: [
      { value: '102', label: 'lives lost' },
      { value: '~2,200', label: 'structures destroyed' },
      { value: '100+ yrs', label: 'deadliest U.S. wildfire in over a century' },
    ],
    situation:
      'August 8. Downslope gusts off the West Maui Mountains are snapping power lines above Lahainaluna Road. Your carrier is at Kahului Airport, across the isthmus — every minute of transit is another block of Front Street. Save the historic district.',
  },
  fortmcmurray: {
    edition: 'Alberta Edition',
    date: 'Wednesday, May 4, 2016',
    headline: '“The Beast” Takes Fort McMurray',
    deck: 'Entire city ordered out as wildfire jumps into neighbourhoods; convoys crawl along Highway 63',
    article: [
      'FORT McMURRAY, Alta. — The Horse River wildfire, first spotted southwest of the city on Sunday, swept into residential neighbourhoods on Tuesday afternoon, forcing the evacuation of the entire city. Tens of thousands of residents joined bumper-to-bumper convoys north and south on Highway 63 as flames burned along the shoulders.',
      'Crews nicknamed the fire “the Beast.” Hot, dry, gusting winds threw embers across firebreaks and into the city faster than they could be put out.',
    ],
    caption: 'Illustration: the evacuation south on Highway 63 beneath the smoke column.',
    record: [
      { value: '~88,000', label: 'people evacuated' },
      { value: '~2,400', label: 'structures destroyed' },
      { value: '~590,000', label: 'hectares burned' },
    ],
    situation:
      'May 3. The Horse River fire is cresting the ridge southwest of town and ember storms are crossing toward the Athabasca. Your FIRE INCENDIE carrier is at the airport with a full swarm. Protect the regional hospital and hold the river line while the city gets out.',
  },
  pantanal: {
    edition: 'South America Edition',
    date: 'Tuesday, September 15, 2020',
    headline: 'The Wetland Is Burning',
    deck: "Record fire season sweeps Brazil's Pantanal as drought dries the world's largest tropical wetland",
    article: [
      'CORUMBÁ, Brazil — Satellites have counted more fires in the Pantanal this year than in any year since monitoring began in 1998. Firefighters, soldiers and volunteers are travelling by boat to reach blazes cut off by rivers and marsh.',
      'Further north, in Mato Grosso, fire swept through Encontro das Águas State Park, home to one of the densest jaguar populations on Earth. Rescuers are treating burned jaguars, tapirs and caimans.',
    ],
    caption: 'Illustration: a fire front crosses open wetland at dusk.',
    record: [
      { value: '>25%', label: 'of the Pantanal burned' },
      { value: '~17M', label: 'vertebrates killed (scientific estimate)' },
      { value: 'Record', label: 'fire count since monitoring began in 1998' },
    ],
    situation:
      'Drought has turned the floodplain to tinder and fires are breaking out across islands only reachable by air. Your Aerobombeiro carrier sits at the Corumbá riverine base. Fly long, drop foam where boats cannot reach, and keep the fire out of the wildlife refuge.',
  },
  chongqing: {
    edition: 'Asia–Pacific Edition',
    date: 'Friday, August 26, 2022',
    headline: 'Mountain of Fire',
    deck: 'Record heat ignites Jinyun Mountain above Chongqing; volunteers on motorbikes haul supplies up the slopes',
    article: [
      'CHONGQING — Fires on the forested ridges of Jinyun Mountain in Beibei District burned through the week as the region endured its most severe heatwave on record, with temperatures in Beibei reaching 45 °C. Volunteers, many riding motorcycles, carried water, food and tools up steep tracks to crews cutting firebreaks.',
      'Hundreds of kilometres to the southwest, the high conifer forests of Liangshan in Sichuan — where mountain fires in 2019 and 2020 killed dozens of firefighters — are a reminder of how dangerous this terrain is for crews on foot.',
    ],
    caption: 'Illustration: the fire line climbs Jinyun Mountain above the city at night.',
    record: [
      { value: '45 °C', label: 'peak temperature in Beibei' },
      { value: '70+ days', label: 'heatwave across southern China' },
      { value: '2019 · 2020', label: 'Liangshan fires that killed dozens of firefighters' },
    ],
    situation:
      'August 2022. The fire line is racing up Jinyun Mountain toward villages above the city, and a second fire is burning in the Liangshan conifers. Your EHang swarm is staged at Jiangbei, with a forward carrier at Liangshan. Keep crews off the slopes and let the swarms do the climbing.',
  },
  saxon: {
    edition: 'Central Europe Edition',
    date: 'Tuesday, July 26, 2022',
    headline: 'Fire in the Sandstone',
    deck: "Blaze in Bohemian Switzerland crosses into Germany's Saxon Switzerland; crews fight in gorges they cannot reach on foot",
    article: [
      'HŘENSKO, Czech Republic — A forest fire that broke out on Sunday in Bohemian Switzerland National Park spread through the sandstone gorges around Hřensko and across the border into Saxon Switzerland, as helicopters and firefighting aircraft from several European countries joined Czech and German crews.',
      'Steep rock walls and dense stands of dead spruce, killed by bark beetles, have made ground access dangerous, leaving much of the fight to the air.',
    ],
    caption: 'Illustration: a helicopter bucket run between the sandstone towers.',
    record: [
      { value: '~1,000 ha', label: 'burned on the Czech side' },
      { value: 'Largest', label: 'wildfire in modern Czech history' },
      { value: '2', label: 'nations — the fire crossed the border' },
    ],
    situation:
      'July 2022. Fire is climbing the sandstone towers on both sides of the border, in gorges no ground crew can safely reach. Your Feuerwehr carrier is at Pirna. Build fire breaks from the air and keep the flames off Großer Winterberg.',
  },
  blacksummer: {
    edition: 'Australia Edition',
    date: 'Wednesday, January 1, 2020',
    headline: "A New Year's Eve of Fire",
    deck: "Thousands shelter on Mallacoota's foreshore as the sky turns black; fires burn from the NSW South Coast to East Gippsland",
    article: [
      "MALLACOOTA, Vic. — Residents and holidaymakers crowded onto the beach and into boats on New Year's Eve as fire bore down on the coastal town, turning the morning sky black, then red. Across the border in New South Wales, the Currowan fire continued to burn along the South Coast.",
      'Fire authorities warned that hot, dry north-westerlies would bring more dangerous days, with communities from the coast to the alpine high country cut off by fire.',
    ],
    caption: 'Illustration: Mallacoota residents shelter on the beach under a smoke-dark sky.',
    record: [
      { value: '~24M ha', label: 'burned across Australia' },
      { value: '33', label: 'lives lost directly in the fires' },
      { value: '~3 billion', label: 'animals killed or displaced (WWF estimate)' },
    ],
    situation:
      'Summer 2019–20. Megafires span 300 km from the Currowan front to East Gippsland and ember attacks reach 30 km ahead. Your Kookaburra carrier is at Moruya airfield. Triage hard, protect the koala refuges, and stop the fires reaching the coast.',
  },
};

export const pressPhotoUrl = (id: string) => `/campaigns/${id}.png`;
