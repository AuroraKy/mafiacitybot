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
                    Ping.HandleCommand(command);
                    break;
                case "phase":
                    Phase.HandleCommand(command, Program);
                    break;
                case "setup":
                    Setup.HandleCommand(command, Program);
                    break;
                case "endgame":
                    EndGame.HandleCommand(command);
                    break;
                case "register":
                    Register.HandleCommand(command, Program);
                    break;
                case "letter":
                    Letter.HandleCommand(command, Program);
                    break;
                case "action":
                    Actions.HandleCommand(command, Program);
                    break;
                case "clear_players":
                    ClearPlayers.HandleCommand(command, Program);
                    break;
                case "clear_player":
                    ClearPlayer.HandleCommand(command, Program);
                    break;
                case "info":
                    Info.HandleCommand(command, Program);
                    break;
                case "enforce_roles":
                    EnforceRoles.HandleCommand(command, Program);
                    break;
            }
        }
        else
        {
            await command.RespondAsync("Sorry, but you need to run this command inside a server!");
        }
        
    }
}
