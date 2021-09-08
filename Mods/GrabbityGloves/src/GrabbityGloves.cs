using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using HarmonyLib;
using System.Reflection;
using Vintagestory.GameContent;

namespace GrabbityGloves
{
    /// <summary>
    /// Instantly adds broken blocks to the player inventory if possible.
    /// </summary>
    public class GrabbityGloves : ModSystem
    {
        private static Item glovesItem;
        private static ICoreServerAPI sapi;
        private static Harmony harmonyInstance;

        public override void StartServerSide(ICoreServerAPI _sapi)
        {
            sapi = _sapi;

            harmonyInstance = new Harmony("charagarlnad.grabbitygloves");
            harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());

            sapi.Event.SaveGameLoaded += OnSaveLoaded;
        }

        public override void StartClientSide(ICoreClientAPI capi)
        {
            
        }

        private void OnSaveLoaded()
        {
            glovesItem = sapi.World.GetItem(new AssetLocation("grabbitygloves", "grabbitygloves"));
        }

        // should probably turn these into transpilers one day
        [HarmonyPatch(typeof(Block))]
        [HarmonyPatch(nameof(Block.OnBlockBroken))]
        public class Patch_Block_OnBlockBroken
        {
            public static bool Prefix(Block __instance, IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
            {
                bool flag = false;
                foreach (BlockBehavior blockBehavior in __instance.BlockBehaviors)
                {
                    EnumHandling enumHandling = EnumHandling.PassThrough;
                    blockBehavior.OnBlockBroken(world, pos, byPlayer, ref enumHandling);
                    if (enumHandling == EnumHandling.PreventDefault)
                    {
                        flag = true;
                    }
                    if (enumHandling == EnumHandling.PreventSubsequent)
                    {
                        return false;
                    }
                }
                if (flag)
                {
                    return false;
                }
                if (world.Side == EnumAppSide.Server && (byPlayer == null || byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative))
                {
                    ItemStack[] drops = __instance.GetDrops(world, pos, byPlayer, dropQuantityMultiplier);
                    if (drops != null)
                    {
                        for (int j = 0; j < drops.Length; j++)
                        {
                            if (__instance.SplitDropStacks)
                            {
                                for (int k = 0; k < drops[j].StackSize; k++)
                                {
                                    ItemStack itemStack = drops[j].Clone();
                                    itemStack.StackSize = 1;
                                    IInventory inv = byPlayer.InventoryManager.GetOwnInventory(Vintagestory.API.Config.GlobalConstants.characterInvClassName);
                                    bool wearingGrabbityGloves = inv[(int)EnumCharacterDressType.Hand].Itemstack?.Item == glovesItem;
                                    if (wearingGrabbityGloves && byPlayer.InventoryManager.TryGiveItemstack(itemStack, true))
                                    {
                                        world.PlaySoundAt(new AssetLocation("sounds/player/collect"), byPlayer);
                                    }
                                    else
                                    {
                                        world.SpawnItemEntity(itemStack, new Vec3d((double)pos.X + 0.5, (double)pos.Y + 0.5, (double)pos.Z + 0.5), null);
                                    }
                                }
                            }
                        }
                    }
                    BlockSounds sounds = __instance.Sounds;
                    world.PlaySoundAt((sounds != null) ? sounds.GetBreakSound(byPlayer) : null, (double)pos.X, (double)pos.Y, (double)pos.Z, byPlayer, true, 32f, 1f);
                }
                if (__instance.EntityClass != null)
                {
                    BlockEntity blockEntity = world.BlockAccessor.GetBlockEntity(pos);
                    if (blockEntity != null)
                    {
                        blockEntity.OnBlockBroken();
                    }
                }
                world.BlockAccessor.SetBlock(0, pos);
                return false;
            }
        }

        [HarmonyPatch(typeof(Vintagestory.GameContent.BlockOre))]
        [HarmonyPatch(nameof(Vintagestory.GameContent.BlockOre.OnBlockBroken))]
        public class Patch_BlockOre_OnBlockBroken
        {
            public static bool Prefix(Vintagestory.GameContent.BlockOre __instance, IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
            {
                dropQuantityMultiplier *= byPlayer?.Entity.Stats.GetBlended("oreDropRate") ?? 1;

                EnumHandling handled = EnumHandling.PassThrough;

                foreach (BlockBehavior behavior in __instance.BlockBehaviors)
                {
                    behavior.OnBlockBroken(world, pos, byPlayer, ref handled);
                    if (handled == EnumHandling.PreventSubsequent) return false;
                }

                if (handled == EnumHandling.PreventDefault) return false;

                if (world.Side == EnumAppSide.Server && (byPlayer == null || byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative))
                {
                    ItemStack[] drops = __instance.GetDrops(world, pos, byPlayer, dropQuantityMultiplier);

                    if (drops != null)
                    {
                        for (int i = 0; i < drops.Length; i++)
                        {
                            IInventory inv = byPlayer.InventoryManager.GetOwnInventory(Vintagestory.API.Config.GlobalConstants.characterInvClassName);
                            bool wearingGrabbityGloves = inv[(int)EnumCharacterDressType.Hand].Itemstack?.Item == glovesItem;
                            if (wearingGrabbityGloves && byPlayer.InventoryManager.TryGiveItemstack(drops[i], true))
                            {
                                world.PlaySoundAt(new AssetLocation("sounds/player/collect"), byPlayer);
                            }
                            else
                            {
                                world.SpawnItemEntity(drops[i], new Vec3d((double)pos.X + 0.5, (double)pos.Y + 0.5, (double)pos.Z + 0.5), null);
                            }
                        }
                    }

                    world.PlaySoundAt(__instance.Sounds.GetBreakSound(byPlayer), pos.X, pos.Y, pos.Z, byPlayer);
                }

                world.BlockAccessor.SetBlock(0, pos);


                if (byPlayer != null && byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative)
                {
                    CollectibleObject coll = byPlayer?.InventoryManager.ActiveHotbarSlot.Itemstack?.Collectible;
                    if (__instance.LastCodePart(1) == "flint" && (coll == null || coll.ToolTier == 0))
                    {
                        world.BlockAccessor.SetBlock(world.GetBlock(new AssetLocation("rock-" + __instance.LastCodePart())).BlockId, pos);
                    }
                }

                return false;
            }
        }

        [HarmonyPatch(typeof(Vintagestory.GameContent.BlockLantern))]
        [HarmonyPatch(nameof(Vintagestory.GameContent.BlockLantern.OnBlockBroken))]
        public class Patch_BlockLantern_OnBlockBroken
        {
            public static bool Prefix(Vintagestory.GameContent.BlockLantern __instance, IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
            {
                bool preventDefault = false;
                foreach (BlockBehavior behavior in __instance.BlockBehaviors)
                {
                    EnumHandling handled = EnumHandling.PassThrough;

                    behavior.OnBlockBroken(world, pos, byPlayer, ref handled);
                    if (handled == EnumHandling.PreventDefault) preventDefault = true;
                    if (handled == EnumHandling.PreventSubsequent) return false;
                }

                if (preventDefault) return false;


                if (world.Side == EnumAppSide.Server && (byPlayer == null || byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative))
                {
                    ItemStack[] drops = new ItemStack[] { __instance.OnPickBlock(world, pos) };

                    if (drops != null)
                    {
                        for (int i = 0; i < drops.Length; i++)
                        {
                            IInventory inv = byPlayer.InventoryManager.GetOwnInventory(Vintagestory.API.Config.GlobalConstants.characterInvClassName);
                            bool wearingGrabbityGloves = inv[(int)EnumCharacterDressType.Hand].Itemstack?.Item == glovesItem;
                            if (wearingGrabbityGloves && byPlayer.InventoryManager.TryGiveItemstack(drops[i], true))
                            {
                                world.PlaySoundAt(new AssetLocation("sounds/player/collect"), byPlayer);
                            }
                            else
                            {
                                world.SpawnItemEntity(drops[i], new Vec3d((double)pos.X + 0.5, (double)pos.Y + 0.5, (double)pos.Z + 0.5), null);
                            }
                        }
                    }

                    world.PlaySoundAt(__instance.Sounds.GetBreakSound(byPlayer), pos.X, pos.Y, pos.Z, byPlayer);
                }

                if (__instance.EntityClass != null)
                {
                    BlockEntity entity = world.BlockAccessor.GetBlockEntity(pos);
                    if (entity != null)
                    {
                        entity.OnBlockBroken();
                    }
                }

                world.BlockAccessor.SetBlock(0, pos);
                return false;
            }
        }

        [HarmonyPatch(typeof(Vintagestory.GameContent.BlockReeds))]
        [HarmonyPatch(nameof(Vintagestory.GameContent.BlockReeds.OnBlockBroken))]
        public class Patch_BlockReeds_OnBlockBroken
        {
            public static bool Prefix(Vintagestory.GameContent.BlockReeds __instance, IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
            {
                int habitat = (int)Traverse.Create(__instance).Field("habitat").GetValue(); // added this
                if (world.Side == EnumAppSide.Server && (byPlayer == null || byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative))
                {
                    bool isReed = __instance.Variant["type"] == "coopersreed";
                    ItemStack drop = null;
                    if (__instance.Variant["state"] == "normal")
                    {
                        drop = new ItemStack(world.GetItem(new AssetLocation(isReed ? "cattailtops" : "papyrustops")));
                    }
                    else
                    {
                        drop = new ItemStack(world.GetItem(new AssetLocation(isReed ? "cattailroot" : "papyrusroot")));
                    }

                    if (drop != null)
                    {
                        IInventory inv = byPlayer.InventoryManager.GetOwnInventory(Vintagestory.API.Config.GlobalConstants.characterInvClassName);
                        bool wearingGrabbityGloves = inv[(int)EnumCharacterDressType.Hand].Itemstack?.Item == glovesItem;
                        if (wearingGrabbityGloves && byPlayer.InventoryManager.TryGiveItemstack(drop, true))
                        {
                            world.PlaySoundAt(new AssetLocation("sounds/player/collect"), byPlayer);
                        }
                        else
                        {
                            world.SpawnItemEntity(drop, new Vec3d((double)pos.X + 0.5, (double)pos.Y + 0.5, (double)pos.Z + 0.5), null);
                        }
                    }

                    world.PlaySoundAt(__instance.Sounds.GetBreakSound(byPlayer), pos.X, pos.Y, pos.Z, byPlayer);
                }

                if (byPlayer != null && __instance.Variant["state"] == "normal" && (byPlayer.InventoryManager.ActiveTool == EnumTool.Knife || byPlayer.InventoryManager.ActiveTool == EnumTool.Sickle || byPlayer.InventoryManager.ActiveTool == EnumTool.Scythe))
                {
                    world.BlockAccessor.SetBlock(world.GetBlock(habitat == EnumReedsHabitat.Ice ? __instance.CodeWithVariants(new string[] { "habitat", "state" }, new string[] { "water", "harvested" }) : __instance.CodeWithVariant("state", "harvested")).BlockId, pos);
                    return false;
                }

                if (habitat != 0)
                {
                    world.BlockAccessor.SetBlock(world.GetBlock(new AssetLocation("water-still-7")).BlockId, pos);
                    world.BlockAccessor.GetBlock(pos).OnNeighbourBlockChange(world, pos, pos);
                }
                else
                {
                    world.BlockAccessor.SetBlock(0, pos);
                }
                return false;
            }
        }

    }
}
