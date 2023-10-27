using System;
using System.Text.Json;

namespace mafiacitybot;

public class Settings
{
    public string Token;
    public ulong GuildID;

    public Settings(string token, ulong guildid)
    {
        Token = token;
        GuildID = guildid;
    }
}


