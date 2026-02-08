using Discord;
using Discord.Net;
using Discord.WebSocket;

namespace mafiacitybot.GuildCommands;

public static class Mask
{
    public static async Task CreateCommand(DiscordSocketClient client, SocketGuild? guild = null)
    {
        var command = new SlashCommandBuilder()
            .WithName("mask")
            .WithDescription("Don your mask. The Masquerade awaits.")
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("enabled")
                .WithDescription("Whether to put on or take off your mask.")
                .WithType(ApplicationCommandOptionType.Boolean)
                .WithRequired(true))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("prefix")
                .WithDescription("The mask you wish to wear.")
                .WithType(ApplicationCommandOptionType.String)
                .WithRequired(false));

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
        bool enabled = (bool)command.Data.Options.First(o => o.Name == "enabled").Value;
        string? prefix = command.Data.Options.FirstOrDefault(o => o.Name == "prefix")?.Value?.ToString();

        ulong userId = command.User.Id;
        ulong guildId = command.GuildId ?? 0;
        
        if (!program.guilds.TryGetValue(Convert.ToUInt64(command.GuildId), out Guild guild))
        {
            await command.RespondAsync($"You must use setup before being able to use this command!");
            return;
        }

        Masquerade.MasqueradeTunnel? tunnel = guild.MasqueradeTunnels.Values
            .FirstOrDefault(s => s.AttendeeIds.Contains(userId));
        if (tunnel == null)
        {
            await command.RespondAsync("**No ballroom welcomes you.** " +
                                       "\n*The night is yet young. You will don the mask when the time is right.*");
            return;
        }
        
        if (prefix != null)
        {
            if (tunnel.Prefixes.ContainsKey(userId))
            {
                await command.RespondAsync($"You already wear the mask of **{tunnel.Prefixes[userId]}**. It is too late to change it now.");
                return;
            }
            
            if (tunnel.Prefixes.Values.Any(p => p.Equals(prefix, StringComparison.OrdinalIgnoreCase)))
            {
                await command.RespondAsync($"The mask of **{prefix}** is worn by another dancer. Choose something unique.");
                return;
            }

            await command.RespondAsync($"You will **permanently** don the mask of **{prefix}**. Reply with `/confirm_prefix {tunnel.Id}` to confirm." +
                $"\n*This will set your chat prefix for the Ballroom. This* ***cannot be changed later.*** *Choose wisely.*");
            tunnel.PendingPrefixConfirmations[userId] = prefix;
            return;
        }
        
        string prefixDisplay = tunnel.Prefixes.TryGetValue(userId, out var pre) ? pre : "Dancer";
        if (enabled)
        {
            if (!tunnel.Prefixes.ContainsKey(userId))
            {
                await command.RespondAsync("The masquerade only admits the masked. In the moonlight, your naked face shows your shame. " +
                                           "\n*Rerun this command with the prefix option filled in.*");
                return;
            }

            tunnel.ForwardingEnabled[userId] = true;
            await command.RespondAsync($"Welcome to the ballroom, {prefixDisplay}. In the low lights, the mask hides all. " +
                                       $"\n*Your messages are now being sent to others in the same ballroom.*");
        }
        else
        {
            tunnel.ForwardingEnabled[userId] = false;
            await command.RespondAsync("You step out for a moment, removing your mask under the cover of night. " +
                                       "\n*Your messages are no longer being sent to others.*");
        }
        
    }
}
