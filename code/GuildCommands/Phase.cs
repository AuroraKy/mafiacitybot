using Discord;
using Discord.Net;
using Discord.WebSocket;

namespace mafiacitybot.GuildCommands;

public static class Phase
{
    public static async Task CreateCommand(SocketGuild guild)
    {
        var command = new SlashCommandBuilder();
        command.WithName("phase");
        command.WithDescription("Changes the phase from day to night or vice versa.");

        try
        {
            await guild.CreateApplicationCommandAsync(command.Build());
        }
        catch (HttpException exception)
        {
            Console.WriteLine(exception.Message);
        }
    }

    public static async Task HandleCommand(SocketSlashCommand command, Program program)
    {
        Guild guild = program.guilds[Convert.ToUInt64(command.GuildId)];
        guild.AdvancePhase();
        await command.RespondAsync($"Changed the phase! It is now {guild.CurrentPhase}.");
    }
}
