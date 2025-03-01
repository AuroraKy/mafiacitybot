using Discord;
using Discord.Net;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Runtime.CompilerServices;

namespace mafiacitybot.GuildCommands;

public static class Ping
{
    public static async Task CreateCommand(DiscordSocketClient client, SocketGuild? guild = null)
    {
        var command = new SlashCommandBuilder();
        command.WithName("ping");
        command.WithDescription("pongs");

        try
        {
            if(guild != null) {
                await guild.CreateApplicationCommandAsync(command.Build());
            } else {
                await client.CreateGlobalApplicationCommandAsync(command.Build());
            }
        }
        catch (HttpException exception)
        {
            Console.WriteLine(exception.Message);
        }
    }

    public static async Task HandleCommand(SocketSlashCommand command, Program program)
    {
        DateTimeOffset now = DateTimeOffset.Now;

        await command.RespondAsync("pong");
        RestInteractionMessage msg = await command.GetOriginalResponseAsync();

        DateTimeOffset time = msg.Timestamp;
        await command.ModifyOriginalResponseAsync((MessageProperties props) =>
        {
            props.Content = $"pong! ({(time - now).Milliseconds} ms)";
        });
    }
}
