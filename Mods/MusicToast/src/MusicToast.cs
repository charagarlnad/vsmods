using HarmonyLib;
using System.Reflection;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace MusicToast
{
    /// <summary>
    /// Notifications about what music track is playing
    /// </summary>
    public class MusicToast : ModSystem
    {
        private static ICoreClientAPI capi;
        private static Vintagestory.Client.NoObf.ClientEventManager eventManager;

        private static Vintagestory.Client.NoObf.ClientMain game;
        private static Harmony harmonyInstance;

        public override void StartClientSide(ICoreClientAPI _capi)
        {
            capi = _capi;

            game = (Vintagestory.Client.NoObf.ClientMain)capi.World;
            eventManager = (Vintagestory.Client.NoObf.ClientEventManager)(game.GetType().GetField("eventManager", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(game)); // pls don't crucify me, i know it's bad :(

            harmonyInstance = new Harmony("charagarlnad.musictoast");
            harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
        }

        [HarmonyPatch(typeof(Vintagestory.Client.NoObf.SystemMusicEngine))]
        [HarmonyPatch("OnEverySecond")]
        public class Patch_MusicTrack_OnMusicCommand
        {
            private static IMusicTrack lastTrack;
            static void Postfix(Vintagestory.Client.NoObf.SystemMusicEngine __instance)
            {
                if (__instance.CurrentTrack != null && lastTrack != __instance.CurrentTrack)
                {
                    eventManager.TriggerNewServerChatLine(GlobalConstants.InfoLogChatGroup, "<icon name=dice></icon> <strong>Now playing: </strong>" + __instance.CurrentTrack.Name, EnumChatType.Notification, null);
                    lastTrack = __instance.CurrentTrack;
                }
            }

        }

    }
}
