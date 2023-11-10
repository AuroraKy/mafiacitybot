using Discord.WebSocket;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

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

    [JsonConstructor]
    public Guild(ulong GuildID, ulong HostRoleID, ulong HostChannelID, List<Player> Players, Phase CurrentPhase) : this(GuildID, HostRoleID, HostChannelID)
    {
        this.CurrentPhase = CurrentPhase;
        this.Players = Players;
    }

    public void Save()
    {
        string json = JsonSerializer.Serialize(this);
        File.WriteAllText($"../../../../Data/Guild_{GuildID}.json", json);
    }

    public static Guild? Load(ulong id)
    {
        if (!File.Exists($"../../../../Data/Guild_{id}.json")) return null;
        string json = File.ReadAllText($"../../../../Data/Guild_{id}.json");
        return JsonSerializer.Deserialize<Guild>(json);
    }

    public void AdvancePhase()
    {
        CurrentPhase = (CurrentPhase == Phase.Day) ? Phase.Night : Phase.Day;
        Save();
    }

    public void AddPlayer(Player player)
    {
        if(Players.Contains(player))
        {
            Players.Remove(player);
        }
        Players.Add(player);
        Save();
    }

    public static bool IsHostRoleUser(SocketSlashCommand command, ulong roleID)
    {
        if (command.Channel == null || command.User == null) return false;

        SocketGuildUser user = (SocketGuildUser)command.User;
        SocketGuild Guild = (command.Channel as SocketGuildChannel).Guild;
        return user.Roles.Contains<SocketRole>(Guild.GetRole(roleID));
    }

}
