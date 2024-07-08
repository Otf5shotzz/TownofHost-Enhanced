using AmongUs.GameOptions;
using UnityEngine;
using Hazel;
using System.Collections.Generic;
using static TOHE.Options;
using static TOHE.Utils;
using static TOHE.Translator;

namespace TOHE.Roles.Crew;

internal class Bully : RoleBase
{
    private const int id = 15901;
    private static readonly HashSet<byte> PlayerIds = new HashSet<byte>();
    public static bool HasEnabled = PlayerIds.Any();
    
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.Crewmate;

    private static OptionItem BullyCooldown;

    private static readonly Dictionary<byte, float> BullyCooldowns = new Dictionary<byte, float>();

    public override void SetupCustomOption()
    {
        SetupRoleOptions(id, TabGroup.CrewmateRoles, CustomRoles.Bully);
        BullyCooldown = FloatOptionItem.Create(id + 10, "BullyCooldown", new FloatRange(0f, 180f, 5f), 50f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Bully])
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void Init()
    {
        PlayerIds.Clear();
        BullyCooldowns.Clear();
    }

    public override void Add(byte playerId)
    {
        PlayerIds.Add(playerId);

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }

    private static void SendBullyPlayerRPC(PlayerControl player, PlayerControl target)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetBullyPlayer, SendOption.Reliable, -1);
        writer.Write(player.PlayerId);
        writer.Write(target.PlayerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public static void ReceiveBullyPlayerRPC(MessageReader reader)
    {
        byte bullyId = reader.ReadByte();
        byte targetId = reader.ReadByte();

        if (PlayerControl.LocalPlayer.PlayerId == bullyId)
        {
            PlayGuardianAngelShieldAnimation(PlayerControl.LocalPlayer, targetId);
        }
    }

    private static void PlayGuardianAngelShieldAnimation(PlayerControl player, byte targetId)
    {
        if (player.PlayerId == PlayerControl.LocalPlayer.PlayerId)
        {
            player.MyPhysics.PlayAnimation("Shield", 0, 1f);
            player.StartCoroutine(PlaySadnessAddonAfterAnimation(player, targetId));
        }
    }

    private static System.Collections.IEnumerator PlaySadnessAddonAfterAnimation(PlayerControl player, byte targetId)
    {
        yield return new WaitForSeconds(1.5f); 

        PlayerControl target = Utils.GetPlayerById(targetId);
        if (target != null && !target.Data.IsDead)
        {
            Common.AddonManager.StartAddon(target, AddonType.Sadness, 40f);
        }
    }

    public override bool CanUseKillButton(PlayerControl pc)
    {
        if (BullyCooldowns.TryGetValue(pc.PlayerId, out float cooldownEnd))
        {
            if (cooldownEnd > Time.time)
                return false;
        }

        return true;
    }

    public override void PerformKill(PlayerControl pc, bool isAbilityButton)
    {
        PlayGuardianAngelShieldAnimation(pc, pc.PlayerId);
        BullyCooldowns[pc.PlayerId] = Time.time + BullyCooldown.GetFloat();
    }

    public override void OnCheckDeadBody(PlayerControl reporter, GameData.PlayerInfo target)
    {
    }
}
