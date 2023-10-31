using System.Collections.Generic;
using System.Threading.Channels;

namespace mafiacitybot;

public class Guild
{
    public ulong GuildID { get; set; }
    public ulong HostRoleID {  get; set; }
    public ulong HostChannelID { get; set; }
    public List<Player> Players { get; set; }

    public Guild(ulong guildID, ulong hostRoleID, ulong hostChannelID)
    {
        GuildID = guildID;
        HostRoleID = hostRoleID;
        HostChannelID = hostChannelID;
        Players = new List<Player>();
    }

}
