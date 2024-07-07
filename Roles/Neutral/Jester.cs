using AmongUs.GameOptions;
using static TOHE.Options;

namespace TOHE.Roles.Neutral;

internal class Jester : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 14400;
    private static readonly HashSet<byte> PlayerIds = new();
    public static bool HasEnabled => PlayerIds.Any();
    
    public override CustomRoles ThisRoleBase => JesterCanVent.GetBool() ? CustomRoles.Engineer : CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralEvil;
    //==================================================================\\

    private static OptionItem JesterCanUseButton;
    private static OptionItem JesterHasImpostorVision;
    private static OptionItem JesterCanVent;
    private static OptionItem HideJesterVote;
    public static OptionItem SunnyboyChance;

    private static readonly Dictionary<byte, byte> JesterRevengeTarget = new();
    private static readonly HashSet<byte> ExiledJesters = new();

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Jester);
        JesterCanUseButton = BooleanOptionItem.Create(Id + 2, "JesterCanUseButton", false, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Jester]);
        JesterCanVent = BooleanOptionItem.Create(Id + 3, "CanVent", true, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Jester]);
        JesterHasImpostorVision = BooleanOptionItem.Create(Id + 4, "ImpostorVision", true, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Jester]);
        HideJesterVote = BooleanOptionItem.Create(Id + 5, "HideJesterVote", true, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Jester]);
        SunnyboyChance = IntegerOptionItem.Create(Id + 6, "SunnyboyChance", new(0, 100, 5), 0, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Jester])
            .SetValueFormat(OptionFormat.Percent);
    }

    public override void Init()
    {
        PlayerIds.Clear();
        JesterRevengeTarget.Clear();
        ExiledJesters.Clear();
    }

    public override void Add(byte playerId)
    {
        PlayerIds.Add(playerId);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.EngineerCooldown = 1f;
        AURoleOptions.EngineerInVentMaxTime = 0f;

        opt.SetVision(JesterHasImpostorVision.GetBool());
    }

    public override bool HideVote(PlayerVoteArea votedPlayer) => HideJesterVote.GetBool();
    public override bool OnCheckStartMeeting(PlayerControl reporter) => JesterCanUseButton.GetBool();

    public override void CheckExile(GameData.PlayerInfo exiled, ref bool DecidedWinner, bool isMeetingHud, ref string name)
    {
        if (PlayerIds.Contains(exiled.PlayerId))
        {
            ExiledJesters.Add(exiled.PlayerId);
            JesterRevengeTarget[exiled.PlayerId] = byte.MaxValue; // Set to a default value, the player will choose the target later

            if (isMeetingHud)
            {
                name = $"{exiled.PlayerId} will get their revenge from the grave...";
            }
        }
    }

    public static void ChooseRevengeTarget(byte jesterId, byte targetId)
    {
        if (JesterRevengeTarget.ContainsKey(jesterId))
        {
            JesterRevengeTarget[jesterId] = targetId;
        }
    }

    public override void HandleDeath(byte killerId, byte targetId)
    {
        foreach (var jesterId in JesterRevengeTarget.Keys.ToList())
        {
            if (JesterRevengeTarget[jesterId] == byte.MaxValue) continue;

            if (targetId == JesterRevengeTarget[jesterId])
            {
                Utils.KillPlayer(Utils.GetPlayerById(targetId), true);
                JesterRevengeTarget.Remove(jesterId);
                break;
            }
        }
    }

    public override void OnGameEnd(WinningTeam winningTeam)
    {
        foreach (var jesterId in ExiledJesters)
        {
            CustomWinnerHolder.AdditionalWinnerTeams.Add(winningTeam);
            CustomWinnerHolder.WinnerIds.Add(jesterId);
        }
    }
}
