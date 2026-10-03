export type ResourceType="metal"|"crystal"|"naquadah"|"energy"|"dark_matter"|"food"|"water"|"population";
export interface CatalogItem{id:string;name:string;category?:string;levelMax?:number;cost?:Record<string,number>;durationSeconds?:number};
async function getCatalog(path:string):Promise<CatalogItem[]>{const r=await fetch(path);if(!r.ok)throw new Error("Catalog request failed");const j=await r.json();return j.items??[];}
export const gameCatalog={buildings:()=>getCatalog("/api/catalog/buildings"),research:()=>getCatalog("/api/catalog/research"),ships:()=>getCatalog("/api/catalog/ships"),npcs:()=>getCatalog("/api/catalog/npcs")};
export async function getEconomyResources(){const r=await fetch("/api/economy/resources");if(!r.ok)throw new Error("Economy request failed");return r.json();}