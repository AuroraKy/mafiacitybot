using Discord;
using Discord.Net;
using Discord.WebSocket;

namespace mafiacitybot.GuildCommands;

public static class Info
{
    public static async Task CreateCommand(DiscordSocketClient client, SocketGuild? guild = null)
    {
        var command = new SlashCommandBuilder();
        command.WithName("info");
        command.WithDescription("Provides information about the current game.");

        try
        {
            if (guild != null) {
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
        if (!program.guilds.TryGetValue(Convert.ToUInt64(command.GuildId), out Guild guild))
        {
            await command.RespondAsync($"You must use setup before being able to use this command!");
            return;
        }

        await command.RespondAsync($"Mafia City Bot by Aurora V{Program.VERSION}" +
            $"\nPhase: {guild.CurrentPhase}" +
            $"\nPlayer amount: {guild.Players.Count}" +
            $"\nPlayers: {String.Join(", ", guild.Players.Select(pl => pl.Name + (pl.LinkedNames.Count > 0 ? $" ({String.Join(", ",pl.LinkedNames.Select(x => x.Value))})": "")))}" +
            $"\nActions and letters are currently {(guild.isLocked ? "locked" : "available")}");
    }
}
