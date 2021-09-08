using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Vintagestory.API.Common.Entities;
using System.Collections.Generic;

namespace AutoDrifterHarvest
{
    /// <summary>
    /// Automatically harvests drifters when killed
    /// </summary>
    public class AutoDrifterHarvest : ModSystem
    {

        public override bool ShouldLoad(EnumAppSide side)
        {
            return side == EnumAppSide.Server;
        }

        public override void StartServerSide(ICoreServerAPI sapi)
        {
            sapi.Event.OnEntityDeath += AutoHarvestDrifter;
        }

        public void AutoHarvestDrifter(Entity entity, DamageSource damageSource)
        {
            if (entity.Properties.Code.Path.Contains("drifter"))
            {
                entity.Attributes.SetBool("isMechanical", true); // dumb workaround but better than the dummy entities below
                EntityBehaviorHarvestable behavior = entity.GetBehavior<EntityBehaviorHarvestable>();
                behavior.SetHarvested(null, 1f);
            }

        }

    }

    /*
    public static class DummyPlayerContainer
    {
        public static DummyEntityPlayer dummyEntityPlayer = new DummyEntityPlayer();
        public static DummyEntity dummyEntity = new DummyEntity();
    }

    public class DummyEntityPlayer : IPlayer
    {
        public List<Entitlement> Entitlements => throw new System.NotImplementedException();

        public BlockSelection CurrentBlockSelection => throw new System.NotImplementedException();

        public EntitySelection CurrentEntitySelection => throw new System.NotImplementedException();

        public string PlayerName => throw new System.NotImplementedException();

        public string PlayerUID => throw new System.NotImplementedException();

        public int ClientId => throw new System.NotImplementedException();

        public EntityPlayer Entity => DummyPlayerContainer.dummyEntity;

        public IWorldPlayerData WorldData => throw new System.NotImplementedException();

        public IPlayerInventoryManager InventoryManager => throw new System.NotImplementedException();

        public string[] Privileges => throw new System.NotImplementedException();

        public bool ImmersiveFpMode => throw new System.NotImplementedException();

        public PlayerGroupMembership GetGroup(int groupId)
        {
            throw new System.NotImplementedException();
        }

        public PlayerGroupMembership[] GetGroups()
        {
            throw new System.NotImplementedException();
        }

        public bool HasPrivilege(string privilegeCode)
        {
            throw new System.NotImplementedException();
        }
    }

    public class DummyEntity : EntityPlayer {

        public DummyEntity()
        {
            Stats = new EntityStats(this);
        }

    }
    */
}
