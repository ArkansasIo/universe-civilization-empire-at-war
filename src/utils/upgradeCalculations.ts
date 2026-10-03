import type { AnyRecord } from '../types';
export const DEFAULT_MASTER_UPGRADES_STATE: AnyRecord = { bankLevel: 1, storageLevel: 1 };
export const DEFAULT_BANK_VAULT_STATE: AnyRecord = { level: 1, balance: 0 };
export const IMPERIAL_UPGRADES_CATALOG: AnyRecord[] = [];
export function calculateBankCapacity(level = 1): number { return 10000 * Math.max(1, level); }
export function calculateBankInterestRate(level = 1): number { return Math.min(0.1, 0.01 + Math.max(0, level - 1) * 0.005); }
export function calculateMaxStorageCapacity(level = 1): number { return 10000 * Math.max(1, level); }
export function calculateStorageUpgradeCost(level = 1): number { return Math.max(100, level * level * 100); }
export function calculateMaxLoanAvailable(level = 1): number { return 5000 * Math.max(1, level); }
export function calculatePlunderProtectionPercent(level = 1): number { return Math.min(90, 10 + Math.max(0, level - 1) * 5); }
export function calculateUpgradeCost(upgrade: AnyRecord = {}, level = 1): AnyRecord { const base = Number(upgrade?.baseCost || 100); return { metal: base * level, crystal: base * level, deuterium: base * level, naquadah: base * level }; }
