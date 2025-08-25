using Discord;
using Discord.Net;
using Discord.WebSocket;
using System.Numerics;

namespace mafiacitybot.GuildCommands;

public static class Phase
{
    public static async Task CreateCommand(DiscordSocketClient client, SocketGuild? guild = null)
    {
        var command = new SlashCommandBuilder();
        command.WithDefaultMemberPermissions(GuildPermission.ManageRoles);
        command.WithName("phase");
        command.WithDescription("Changes the Phase from Day to Night (or vice versa).");

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
            await command.RespondAsync($"You must use /setup before being able to use this command!");
            return;
        }

        if (!Guild.IsHostRoleUser(command, guild.HostRoleID))
        {
            await command.RespondAsync($"You must have the HOST role to use this command!");
            return;
        }
        if (guild.HostChannelID != command.ChannelId)
        {
            await command.RespondAsync($"Command must be executed in the HOST bot channel!");
            return;
        }


        var channel = command.Channel;
        guild.AdvancePhase();
        
        foreach (Player player in guild.Players) {
            player.Action = "";
            player.letters = new();
            player.CroakVote = "";
        }
        
        guild.hostLetters = new();
        guild.Votes = new();
        guild.Save();

        await command.RespondAsync($"{ (guild.CurrentPhase == Guild.Phase.Day ? "Night" : "Day")} has ended.\nIt is now " + guild.CurrentPhase + ".\nAll Actions have been cleared!");
    }
}
