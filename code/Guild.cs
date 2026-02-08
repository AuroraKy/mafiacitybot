using Discord.WebSocket;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Concurrent;
using mafiacitybot.GuildCommands;

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

    public struct Letter {
        public ulong recipientID { get; set; }
        public string content { get; set; }
        public Letter(ulong recipientID, string content) {
            this.recipientID = recipientID;
            this.content = content;
        }
    }

    public ulong GuildID { get; set; }
    public ulong HostRoleID {  get; set; }
    public ulong HostChannelID { get; set; }
    public List<Player> Players { get; set; }

    public List<Letter> hostLetters { get; set; }
    public Dictionary<string, List<Player>> Votes { get; set; }
    public ConcurrentDictionary<char, AnonChat.AnonChatTunnel> AnonChats { get; set; }
    public ConcurrentDictionary<int, Masquerade.MasqueradeTunnel> MasqueradeTunnels { get; set; }
    public ConcurrentDictionary<char, Contest.ContestRoom> Contests { get; set; }
    public Queue<char> ChatIDs { get; set; }
    public Phase CurrentPhase { get; set; }

    public bool isLocked { get; set; } = false;

    public Guild(ulong guildID, ulong hostRoleID, ulong hostChannelID)
    {
        GuildID = guildID;
        HostRoleID = hostRoleID;
        HostChannelID = hostChannelID;
        Players = new List<Player>();
        hostLetters = new List<Letter>();
        Votes = new();
        AnonChats = new();
        MasqueradeTunnels = new();
        Contests = new();
        SetupChatIDs();
        isLocked = false;
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
        File.WriteAllText($"{Program.DataPath}/Guild_{GuildID}.json", json);
    }

    public static Guild? Load(ulong id)
    {
        if (!File.Exists($"{Program.DataPath}/Guild_{id}.json")) return null;
        string json = File.ReadAllText($"{Program.DataPath}/Guild_{id}.json");
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

    public void SetupChatIDs()
    {
        List<char> ids = new() { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z' };
        Random rng = new();
        List<char> shuffledIds = ids.OrderBy(c => rng.Next()).ToList();
        ChatIDs = new(shuffledIds);
    }

    public char GetChatID()
    {
        if (ChatIDs.Count == 0) SetupChatIDs();
        return ChatIDs.Dequeue();
    }
}
