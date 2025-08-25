using Discord;
using Discord.Net;
using Discord.WebSocket;

namespace mafiacitybot.GuildCommands;

public static class Vote
{
    public static async Task CreateCommand(DiscordSocketClient client, SocketGuild? guild = null)
    {
        var command = new SlashCommandBuilder();
        command.WithName("vote");
        command.WithDescription("Cast or reset your Croak Vote.");
        command.AddOption(new SlashCommandOptionBuilder()
            .WithName("cast")
            .WithDescription("Cast your Croak Vote.")
            .WithType(ApplicationCommandOptionType.SubCommand)
            .AddOption("player", ApplicationCommandOptionType.User, "The PLAYER to cast your Vote for.", isRequired: true)
        );
        command.AddOption(new SlashCommandOptionBuilder()
            .WithName("reset")
            .WithDescription("Reset your Croak Vote.")
            .WithType(ApplicationCommandOptionType.SubCommand)
        );

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

        if (guild.CurrentPhase != Guild.Phase.Day)
        {
            await command.RespondAsync($"Croak Voting can only be done during the Day!");
            return;
        }

        if (guild.isLocked) {
            await command.RespondAsync($"Action commands are currently locked.");
            return;
        }
        
        Player? player = guild.Players.Find(player => player.IsPlayer(user.Id));
        if (player == null || player.ChannelID != channel.Id)
        {
            await command.RespondAsync("This command can only be used by a PLAYER in their personal channel!");
            return;
        }
        
        var subcommand = command.Data.Options.FirstOrDefault();
        switch (subcommand?.Name)
        {
            case "cast":
                SocketGuildUser? p = subcommand.Options.First().Value as SocketGuildUser;
                Player? croak = guild.Players.Find(x => x != null && x.IsPlayer(p.Id));

                if (croak == null)
                {
                    await command.RespondAsync("You can only *Croak Vote* for a PLAYER currently in the game.", ephemeral: true);
                    return;
                }

                if (player.CroakVote != "")
                {
                    guild.Votes[player.CroakVote].Remove(player);
                }
                
                var toCroak = subcommand.Options.First().Value as SocketGuildUser;
                player.CroakVote = toCroak.Username;
                if (!guild.Votes.ContainsKey(player.CroakVote))
                {
                    guild.Votes[player.CroakVote] = new List<Player>() { player };
                }
                else
                {
                    guild.Votes[player.CroakVote].Add(player);
                }

                await command.RespondAsync($"You cast your *Croak Vote* for {player.CroakVote}.");
                break; 
            
            
            case "reset":
                if (player.CroakVote == "")
                {
                    await command.RespondAsync("You haven't *Croak Voted* yet.");
                    return;
                }
                guild.Votes[player.CroakVote].Remove(player);
                player.CroakVote = "";
                await command.RespondAsync($"You reset your *Croak Vote*.");
                break;
            
            default:
                await command.RespondAsync("You must either cast or reset your Vote.", ephemeral: true);
                return;
        }
    }
}