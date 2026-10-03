import type { AnyRecord, PlanetColony } from '../types';
export function calculateColonyMaintenance(colony: PlanetColony = {}): number { return Math.max(0, Number(colony?.population || 0) * 0.01); }
export function getColonyMaintenanceDetails(colony: PlanetColony = {}): AnyRecord { const maintenance = calculateColonyMaintenance(colony); return { maintenance, population: Number(colony?.population || 0) }; }
export function getEmpireColonialSummary(planets: PlanetColony[] = []): AnyRecord { return { colonyCount: planets.length, totalMaintenance: planets.reduce((sum, planet) => sum + calculateColonyMaintenance(planet), 0) }; }
