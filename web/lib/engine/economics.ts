/**
 * Economic impact model.
 *   FinalScore = (PropertyValueSaved + LivesSavedBonus) − TotalSuppressionCost
 */
export const VALUE_OF_STATISTICAL_LIFE_USD = 10_000_000; // per rescued civilian
export const POPULATION_PROTECTED_USD = 12_500; // per resident kept out of the burn zone

export interface Ledger {
  propertySavedUSD: number;
  civiliansRescued: number;
  populationProtected: number;
  deploymentCostUSD: number;
  flightTimeCostUSD: number;
  payloadCostUSD: number;
  sorties: number;
  drops: number;
  firesExtinguished: number;
  litresDropped: number;
}

export function emptyLedger(): Ledger {
  return {
    propertySavedUSD: 0,
    civiliansRescued: 0,
    populationProtected: 0,
    deploymentCostUSD: 0,
    flightTimeCostUSD: 0,
    payloadCostUSD: 0,
    sorties: 0,
    drops: 0,
    firesExtinguished: 0,
    litresDropped: 0,
  };
}

export function livesSavedBonus(l: Ledger) {
  return l.civiliansRescued * VALUE_OF_STATISTICAL_LIFE_USD + l.populationProtected * POPULATION_PROTECTED_USD;
}

export function totalSuppressionCost(l: Ledger) {
  return l.deploymentCostUSD + l.flightTimeCostUSD + l.payloadCostUSD;
}

export function finalScore(l: Ledger) {
  return l.propertySavedUSD + livesSavedBonus(l) - totalSuppressionCost(l);
}

export function fmtUSD(v: number, compact = true) {
  if (compact) {
    const abs = Math.abs(v);
    const sign = v < 0 ? '−' : '';
    if (abs >= 1e9) return `${sign}$${(abs / 1e9).toFixed(2)}B`;
    if (abs >= 1e6) return `${sign}$${(abs / 1e6).toFixed(2)}M`;
    if (abs >= 1e3) return `${sign}$${(abs / 1e3).toFixed(0)}K`;
    return `${sign}$${abs.toFixed(0)}`;
  }
  return (v < 0 ? '−' : '') + '$' + Math.abs(Math.round(v)).toLocaleString('en-US');
}
