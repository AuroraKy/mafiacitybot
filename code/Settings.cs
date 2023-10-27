using System;
using System.Text.Json;

namespace mafiacitybot;

public class Settings
{
    public string Token { get; set; }
    public ulong GuildID { get; set; }

    public Settings(string token, ulong guildid)
    {
        Token = token;
        GuildID = guildid;
    }
}


