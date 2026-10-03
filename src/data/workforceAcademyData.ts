import type { AnyRecord } from '../types';
export type WorkforceAcademyState = AnyRecord;
export const DEFAULT_WORKFORCE_ACADEMY_STATE: WorkforceAcademyState = { units: [], facilities: [], totalPower: 0 };
export function calculateWorkforceTotals(state: WorkforceAcademyState): AnyRecord { return { totalUnits: state?.units?.length ?? 0, totalPower: state?.totalPower ?? 0 }; }
export function createDefaultUnitExperience(): AnyRecord { return { level: 1, experience: 0 }; }
