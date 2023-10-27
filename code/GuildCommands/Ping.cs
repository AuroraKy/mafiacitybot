using Discord;
using Discord.Net;
using Discord.WebSocket;
using mafiacitybot;
using System.Text.Json;

namespace mafiacitybot.GuildCommands;

public static class Ping
{
    public static async Task CreateCommand(SocketGuild guild)
    {
        var command = new SlashCommandBuilder();
        command.WithName("ping");
        command.WithDescription("pongs");

        try
        {
            await guild.CreateApplicationCommandAsync(command.Build());
        }
        catch (HttpException exception)
        {
            Console.WriteLine(exception.Message);
        }
    }

    public static async Task HandleCommand(SocketSlashCommand command)
    {
        await command.RespondAsync("pong");
    }
}
