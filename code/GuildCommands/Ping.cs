using Discord;
using Discord.Net;
using Discord.Rest;
using Discord.WebSocket;

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
