import { pool } from "../db";
import { BUILDINGS } from "./buildingService";
import { TECHNOLOGIES } from "./researchService";
import { SHIPS } from "./fleetService";
import { scaleCost } from "./gameProgressionService";

const resourceMap:Record<string,string>={metal:"metal",crystal:"crystal",naquadah:"naquadah",energy:"energy",dark_matter:"dark_matter",food:"food",water:"water",population:"population"};

export async function getColonyResources(userId:string,colonyId:string){
 const r=await pool.query("SELECT r.resource_type,r.current_amount,r.capacity FROM resources r JOIN colonies c ON c.id=r.colony_id WHERE c.id=$1 AND c.user_id=$2 ORDER BY r.resource_type",[colonyId,userId]);
 return r.rows;
}
export async function debitResources(userId:string,colonyId:string,cost:Record<string,number>,referenceId:string,source:string){
 const client=await pool.connect();
 try{
  await client.query("BEGIN");
  const owned=await client.query("SELECT id FROM colonies WHERE id=$1 AND user_id=$2 FOR UPDATE",[colonyId,userId]);
  if(!owned.rowCount) throw new Error("Colony ownership violation");
  for(const [key,value] of Object.entries(cost)){
   if(!value) continue;
   const type=resourceMap[key]??key;
   const row=await client.query("SELECT current_amount FROM resources WHERE colony_id=$1 AND resource_type=$2 FOR UPDATE",[colonyId,type]);
   const amount=Number(row.rows[0]?.current_amount??0);
   if(amount<value) throw new Error("Insufficient "+type);
   await client.query("UPDATE resources SET current_amount=current_amount-$1,updated_at=NOW() WHERE colony_id=$2 AND resource_type=$3",[value,colonyId,type]);
   await client.query("INSERT INTO resource_transactions(user_id,colony_id,resource_type,amount,transaction_type,source,reference_id) VALUES($1,$2,$3,$4,'debit',$5,$6)",[userId,colonyId,type,-value,source,referenceId]);
  }
  await client.query("COMMIT");
 }catch(e){await client.query("ROLLBACK");throw e}finally{client.release()}
}
export async function startBuilding(userId:string,colonyId:string,buildingId:string,targetLevel:number,endTime:Date){
 const def=BUILDINGS.find(x=>x.id===buildingId);if(!def)throw new Error("Unknown building");
 if(targetLevel<1||targetLevel>def.levelMax)throw new Error("Invalid building level");
 const cost=scaleCost(def.cost,targetLevel);
 const q=await pool.query("INSERT INTO building_queues(colony_id,user_id,building_type,target_level,start_time,end_time,metal_cost,crystal_cost,deuterium_cost) VALUES($1,$2,$3,$4,NOW(),$5,$6,$7,$8) RETURNING *",[colonyId,userId,buildingId,targetLevel,endTime,cost.metal??0,cost.crystal??0,cost.naquadah??0]);
 return q.rows[0];
}
export async function startFleetBuild(userId:string,colonyId:string,shipId:string,quantity:number,endTime:Date){
 const def=SHIPS.find(x=>x.id===shipId);if(!def)throw new Error("Unknown ship");
 if(!Number.isInteger(quantity)||quantity<1)throw new Error("Invalid quantity");
 const cost=Object.fromEntries(Object.entries(def.cost).map(([k,v])=>[k,v*quantity]));
 const q=await pool.query("INSERT INTO fleet_build_queues(user_id,colony_id,ship_class,quantity,start_time,end_time) VALUES($1,$2,$3,$4,NOW(),$5) RETURNING *",[userId,colonyId,shipId,quantity,endTime]);
 return {queue:q.rows[0],cost};
}
export function technologyDefinition(id:string){return TECHNOLOGIES.find(x=>x.id===id);}