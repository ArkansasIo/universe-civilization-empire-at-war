import {scaleCost} from "../server/services/gameProgressionService";
import {combatRound} from "../server/services/combatService";
const cost=scaleCost({metal:100,crystal:50},3);
if(cost.metal!==225||cost.crystal!==113)throw new Error("progression formula failed");
const result=combatRound({hull:100,shield:0,attack:100,accuracy:100,defense:1},{hull:50,shield:0,attack:1,accuracy:100,defense:1});
if(result.events.length<1)throw new Error("combat round failed");
console.log("dependency system smoke tests passed");