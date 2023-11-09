

namespace mafiacitybot;

public class Player
{
    public ulong PlayerID { get; set; }
    public ulong ChannelID { get; set; }
    public string Name { get; set; }
    public bool Alive { get; set; }

    public Player(ulong playerID, ulong channelID, string name)
    {
        PlayerID = playerID;
        ChannelID = channelID;
        Name = name;
        Alive = true;
    }
}
