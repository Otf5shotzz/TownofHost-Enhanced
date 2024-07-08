using Hazel;
using System;
using System.Collections.Generic;
using TOHE.Modules;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE
{
    internal static class ZombiesManager
    {
        private static Dictionary<byte, bool> InfectedList = new();
        public static Dictionary<byte, int> InfectionCount = new();
        public static Dictionary<byte, ZombieType> ZombieTypeList = new();
        public static int RoundTime;

        // Options
        public static OptionItem Zombies_GameTime;
        public static OptionItem Zombies_InfectionCooldown;

        public static void SetupCustomOption()
        {
            Zombies_GameTime = IntegerOptionItem.Create(67_224_001, "Zombies_GameTime", new(30, 600, 10), 300, TabGroup.GameSettings, false)
                .SetGameMode(CustomGameMode.Zombies)
                .SetColor(new Color32(0, 255, 0, byte.MaxValue))
                .SetValueFormat(OptionFormat.Seconds)
                .SetHeader(true);
            Zombies_InfectionCooldown = FloatOptionItem.Create(67_224_002, "Zombies_InfectionCooldown", new(1f, 60f, 1f), 10f, TabGroup.GameSettings, false)
                .SetGameMode(CustomGameMode.Zombies)
                .SetColor(new Color32(0, 255, 0, byte.MaxValue))
                .SetValueFormat(OptionFormat.Seconds);
        }

        public static void Init()
        {
            if (Options.CurrentGameMode != CustomGameMode.Zombies) return;

            InfectedList.Clear();
            InfectionCount.Clear();
            ZombieTypeList.Clear();

            _ = new LateTask(() =>
            {
                try
                {
                    Utils.SetChatVisible();
                }
                catch (Exception error)
                {
                    Logger.Error($"Error: {error}", "Zombies Init");
                }
                RoundTime = Zombies_GameTime.GetInt() + 8;
                foreach (PlayerControl pc in Main.AllAlivePlayerControls)
                {
                    InfectionCount.TryAdd(pc.PlayerId, 0);
                    InfectedList.TryAdd(pc.PlayerId, false); // Initially, no one is infected
                    ZombieTypeList.TryAdd(pc.PlayerId, ZombieType.None);
                }
            }, 10f, "Set Chat Visible for Everyone");
        }

        public static void SendRPCSyncInfection(byte playerId)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncInfection, SendOption.Reliable, -1);
            writer.Write(playerId);
            writer.Write(InfectedList[playerId]);
            writer.Write((byte)ZombieTypeList[playerId]);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }

        public static void ReceiveRPCSyncInfection(MessageReader reader)
        {
            byte playerId = reader.ReadByte();
            InfectedList[playerId] = reader.ReadBoolean();
            ZombieTypeList[playerId] = (ZombieType)reader.ReadByte();
        }

        public static void OnPlayerInfect(PlayerControl infector, PlayerControl target)
        {
            if (infector == null || target == null || Options.CurrentGameMode != CustomGameMode.Zombies) return;

            InfectedList[target.PlayerId] = true;
            InfectionCount[infector.PlayerId]++;
            AssignZombieType(target);
            SendRPCSyncInfection(target.PlayerId);

            target.Notify("You have been infected!");

            infector.Notify($"You infected {target.name}!");
        }

        private static void AssignZombieType(PlayerControl target)
        {
            float randomValue = UnityEngine.Random.value;
            if (randomValue < 0.8f)
            {
                ZombieTypeList[target.PlayerId] = ZombieType.DefaultZombie;
                target.myRend.color = new Color32(0, 255, 0, byte.MaxValue); 
                target.GetModData().SpeedMod = 1.0f; // Default speed
            }
            else
            {
                ZombieTypeList[target.PlayerId] = ZombieType.FastZombie;
                target.myRend.color = new Color32(0, 200, 0, byte.MaxValue); 
                target.GetModData().SpeedMod = 1.5f; 
            }
        }

        public static string GetHudText()
        {
            return string.Format(GetString("ZombiesTimeRemain"), RoundTime.ToString());
        }

        public static void OnPlayerAttack(PlayerControl attacker, PlayerControl target)
        {
            if (attacker == null || target == null || Options.CurrentGameMode != CustomGameMode.Zombies) return;

            if (InfectedList[attacker.PlayerId] && !InfectedList[target.PlayerId])
            {
                OnPlayerInfect(attacker, target);
            }
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
    class FixedUpdateInGameModeZombiesPatch
    {
        private static long LastFixedUpdate;
        public static void Postfix()
        {
            if (!GameStates.IsInTask || Options.CurrentGameMode != CustomGameMode.Zombies) return;

            var now = Utils.GetTimeStamp();

            if (LastFixedUpdate == now) return;
            LastFixedUpdate = now;

            ZombiesManager.RoundTime--;
            if (ZombiesManager.RoundTime <= 0)
            {

            }
        }
    }
    public enum ZombieType
    {
        None,
        DefaultZombie,
        FastZombie
    }
}
