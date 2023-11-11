

namespace mafiacitybot;

public class Player
{
    public ulong PlayerID { get; set; }
    public ulong ChannelID { get; set; }
    public string Name { get; set; }
    public bool Alive { get; set; }

    public struct Letter
    {
        public ulong recipientID { get; set; }
        public string content { get; set; }
        public Letter(ulong recipientID, string content)
        {
            this.recipientID = recipientID;
            this.content = content;
        }
    }

    public List<Letter> letters { get; set; }
    public string Action { get; set; }

    public Player(ulong playerID, ulong channelID, string name)
    {
        PlayerID = playerID;
        ChannelID = channelID;
        Name = name;
        Alive = true;
        this.letters = new List<Letter>();
        Action = "";

    }
}
