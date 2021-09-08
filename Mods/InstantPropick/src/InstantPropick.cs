using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using HarmonyLib;
using System.Reflection;
using Vintagestory.GameContent;
using System.Reflection.Emit;
using System.Linq;

namespace InstantPropick
{
    /// <summary>
    /// Removes the 3 sample requirement from the Prospecting Pick.
    /// </summary>
    public class InstantPropick : ModSystem
    {
        private static Harmony harmonyInstance;
        private static MethodInfo anchorMethod = typeof(Vintagestory.API.Common.IWorldPlayerData).GetMethod("get_CurrentGameMode");

        public override bool ShouldLoad(EnumAppSide side)
        {
            return side == EnumAppSide.Server;
        }

        public override void StartServerSide(ICoreServerAPI sapi)
        {
            harmonyInstance = new Harmony("charagarlnad.instantpropick");
            harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
        }

        [HarmonyPatch(typeof(ItemProspectingPick))]
        [HarmonyPatch("ProbeBlockDensityMode")]
        public static class ItemProspectingPick_Patch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = new List<CodeInstruction>(instructions);
                for (var i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Callvirt && codes[i].operand == anchorMethod)
                    {
                        codes[i - 2].opcode = OpCodes.Nop;
                        codes[i - 1].opcode = OpCodes.Nop;
                        codes[i].opcode = OpCodes.Nop;
                        codes[i + 1].opcode = OpCodes.Nop;
                        codes[i + 2].opcode = OpCodes.Nop;
                        break;
                    }
                }

                return codes.AsEnumerable();
            }
        }

    }
}
