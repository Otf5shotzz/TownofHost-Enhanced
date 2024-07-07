using AmongUs.GameOptions;
using static TOHE.Options;

namespace TOHE.Roles.Crew;

internal class Photographer : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 21500;
    private static readonly HashSet<byte> PlayerIds = new();
    public static bool HasEnabled => PlayerIds.Any();
    
    public override CustomRoles ThisRoleBase => CustomRoles.Photographer;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.Crewmate;
    //==================================================================\\

    private static OptionItem PhotographerCanUseButton;
    private static OptionItem PhotographerHasImpostorVision;
    private static OptionItem PhotographerCooldown;

    private static readonly Dictionary<byte, List<string>> PhotosTaken = new();

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewRoles, CustomRoles.Photographer);
        PhotographerCanUseButton = BooleanOptionItem.Create(Id + 2, "PhotographerCanUseButton", false, TabGroup.CrewRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Photographer]);
        PhotographerHasImpostorVision = BooleanOptionItem.Create(Id + 3, "ImpostorVision", false, TabGroup.CrewRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Photographer]);
        PhotographerCooldown = FloatOptionItem.Create(Id + 4, "PhotographerCooldown", new(5f, 60f, 5f), 20f, TabGroup.CrewRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Photographer])
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void Init()
    {
        PlayerIds.Clear();
        PhotosTaken.Clear();
    }

    public override void Add(byte playerId)
    {
        PlayerIds.Add(playerId);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.EngineerCooldown = PhotographerCooldown.GetFloat();
        opt.SetVision(PhotographerHasImpostorVision.GetBool());
    }

    public override bool HideVote(PlayerVoteArea votedPlayer) => false;
    public override bool OnCheckStartMeeting(PlayerControl reporter) => PhotographerCanUseButton.GetBool();

    public void TakePhoto(PlayerControl photographer)
    {
        var currentRoom = Utils.GetPlayerCurrentRoom(photographer);
        var playersInRoom = Utils.GetPlayersInRoom(currentRoom);
        var playerNames = playersInRoom.Select(p => p.PlayerName).ToList();

        if (PhotosTaken.ContainsKey(photographer.PlayerId))
        {
            PhotosTaken[photographer.PlayerId].AddRange(playerNames);
        }
        else
        {
            PhotosTaken[photographer.PlayerId] = playerNames;
        }

        Logger.Info($"Photographer {photographer.PlayerId} took a photo in room {currentRoom} with players: {string.Join(", ", playerNames)}", "Photographer");
    }

    public override void OnMeetingStart()
    {
        foreach (var photographerId in PhotosTaken.Keys)
        {
            var photographer = Utils.GetPlayerById(photographerId);
            if (photographer == null) continue;

            var photoEvidence = string.Join(", ", PhotosTaken[photographerId]);
            photographer.RpcSendChatMessage($"Photo evidence collected: {photoEvidence}");
        }
    }

    public void OnPhotographerShapeshift(PlayerControl photographer)
    {
        TakePhoto(photographer);
    }
}
