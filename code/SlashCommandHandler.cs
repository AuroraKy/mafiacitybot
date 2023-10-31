using Discord.WebSocket;
using mafiacitybot.GuildCommands;
using System;

namespace mafiacitybot;

public static class SlashCommandHandler
{
    public static async Task SlCommandHandler(SocketSlashCommand command)
    {
        switch (command.Data.Name)
        {
            case "ping":
                await Ping.HandleCommand(command);
                break;
            case "phase":
                await Phase.HandleCommand(command);
                break;
            case "startgame":
                await StartGame.HandleCommand(command);
                break;
            case "endgame":
                await EndGame.HandleCommand(command);
                break;
        }
    }
}
