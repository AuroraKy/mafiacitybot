using Discord.WebSocket;
using mafiacitybot.GuildCommands;
using System;

namespace mafiacitybot;

public class SlashCommandHandler
{
    public Program Program { get; set; }

    public SlashCommandHandler(Program program)
    {
        Program = program;
    }

    public async Task SlCommandHandler(SocketSlashCommand command)
    {
        if (!command.IsDMInteraction)
        {
            switch (command.Data.Name)
            {
                case "ping":
                    await Ping.HandleCommand(command);
                    break;
                case "phase":
                    await Phase.HandleCommand(command, Program);
                    break;
                case "setup":
                    await Setup.HandleCommand(command, Program);
                    break;
                case "endgame":
                    await EndGame.HandleCommand(command);
                    break;
                case "register":
                    await Register.HandleCommand(command, Program);
                    break;
            }
        }
        else
        {
            await command.RespondAsync("Sorry, but you need to run this command inside a server!");
        }
        
    }
}
