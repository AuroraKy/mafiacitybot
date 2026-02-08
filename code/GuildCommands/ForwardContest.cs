using Discord;
using Discord.Net;
using Discord.WebSocket;

namespace mafiacitybot.GuildCommands;

public static class ForwardContest
{
    public static async Task CreateCommand(DiscordSocketClient client, SocketGuild? guild = null)
    {
        var command = new SlashCommandBuilder();
        command.WithName("forward_contest");
        command.WithDescription("Set up your personality for a contest.");
        command.AddOption("contest_id", ApplicationCommandOptionType.String, "The ID of the contest you're setting up for.", isRequired: true);
        command.AddOption("personality", ApplicationCommandOptionType.String, "Your alias for others in the contest to see.", isRequired: true);
        command.AddOption("prefix", ApplicationCommandOptionType.String, "Your message prefix (e.g. !)", isRequired: true);

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
        var user = command.User;
        var channel = command.Channel;

        if(!program.guilds.TryGetValue((ulong)command.GuildId, out Guild guild))
        {
            await command.RespondAsync($"You must use /setup before being able to use this command!");
            return;
        }

        Player? player = guild.Players.Find(player => player.IsPlayer(user.Id));
        if (player == null || player.ChannelID != channel.Id)
        {
            await command.RespondAsync("This command can only be used by a PLAYER in their channel!");
            return;
        }

        var contestIdStr = (string)command.Data.Options.First(option => option.Name == "contest_id").Value;
        if (contestIdStr.Length != 1)
        {
            await command.RespondAsync("Either this contest does not exist, or you are not in it.");
            return;
        }
        
        char contestId = contestIdStr[0];
        string alias = (string)command.Data.Options.First(option => option.Name == "personality").Value;
        string prefix = (string)command.Data.Options.First(option => option.Name == "prefix").Value;
        
        if (!guild.Contests.TryGetValue(contestId, out var contest))
        {
            await command.RespondAsync("Either this contest does not exist, or you are not in it.");
            return;
        }
        
        if (!contest.RegisteredUsers.TryGetValue(user.Id, out var registration))
        {
            await command.RespondAsync("Either this contest does not exist, or you are not in it.");
            return;
        }
        
        if (registration.Aliases.TryGetValue(alias, out var existingAlias))
        {
            existingAlias.Prefix = prefix;
            await command.RespondAsync($"Updated personality `{alias}` with new prefix `{prefix}`.");
            return;
        }
        
        if (registration.Aliases.Count >= registration.MaxAliases)
        {
            await command.RespondAsync("**Don't try to steal the spotlight.**\n*You cannot set more aliases.*");
            return;
        }

        var newAlias = new Contest.ContestAlias(alias, prefix);
        registration.Aliases[alias] = newAlias;
        
        await command.RespondAsync($"Added personality `{alias}` with prefix `{prefix}` for contest `{contestId}`");
    }
}