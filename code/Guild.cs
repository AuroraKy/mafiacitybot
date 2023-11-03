using System.Collections.Generic;
using System.Text.Json;

namespace mafiacitybot;

public class Guild
{
    public enum Phase
    {
        Day,
        Night
    }

    public struct Ability
    {
        string user;
        string targets;
        string ability;
    }

    public struct Letter
    {
        string sender;
        string receiver;
        string contents;
    }

    public ulong GuildID { get; set; }
    public ulong HostRoleID {  get; set; }
    public ulong HostChannelID { get; set; }
    public List<Player> Players { get; set; }

    public Phase CurrentPhase { get; set; }

    public Guild(ulong guildID, ulong hostRoleID, ulong hostChannelID)
    {
        GuildID = guildID;
        HostRoleID = hostRoleID;
        HostChannelID = hostChannelID;
        Players = new List<Player>();
        CurrentPhase = Phase.Day;
    }

    public Guild(ulong guildID, ulong hostRoleID, ulong hostChannelID, Phase startingPhase) : this(guildID, hostRoleID, hostChannelID)
    {
        CurrentPhase = startingPhase;
    }

    public void Save(ulong id)
    {
        string json = JsonSerializer.Serialize(this);
        File.WriteAllText($"../../../../Data/Guild_{id}.json", json);
    }

    public void AdvancePhase()
    {
        CurrentPhase = (CurrentPhase == Phase.Day) ? Phase.Night : Phase.Day;
    }

}
