using Discord;
using Discord.Net;
using Discord.WebSocket;
using System.Numerics;

namespace mafiacitybot.GuildCommands;

public static class ViewActions
{
    public static async Task CreateCommand(DiscordSocketClient client, SocketGuild? guild = null)
    {
        var command = new SlashCommandBuilder();
        command.WithDefaultMemberPermissions(GuildPermission.ManageRoles);
        command.WithName("view_actions");
        command.WithDescription("(Host-Only) Views all current actions.");
        command.AddOption(new SlashCommandOptionBuilder()
            .WithName("Action Type")
            .WithDescription("The type of action you want to view.")
            .WithRequired(false)
            .AddChoice("All (Default)", 0)
            .AddChoice("Abilities", 1)
            .AddChoice("Letters", 2)
            .AddChoice("Votes", 3)
            .WithType(ApplicationCommandOptionType.Integer)
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
        if (!program.guilds.TryGetValue(Convert.ToUInt64(command.GuildId), out Guild guild))
        {
            await command.RespondAsync($"You must use setup before being able to use this command!");
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

        await command.RespondAsync($"It is currently {(guild.CurrentPhase == Guild.Phase.Day ? "*Day*" : "*Night*")}.");

        var channel = command.Channel;
        List<List<Embed>> queue = new List<List<Embed>>();
        List<Player> playersWithoutAction = new List<Player>();
        var typeToView = command.Data.Options.FirstOrDefault()?.Value as long? ?? 0;
        
        foreach (Player player in guild.Players)
        {
            Color embedColor = new((int)(player.PlayerID % 256), (int)(player.PlayerID / 1000 % 256), (int)(player.PlayerID / 1e6 % 256));
            IUser? user = await program.client.GetUserAsync(player.PlayerID);
            if(user == null) continue;

            // seperate every letter/action
            List<Embed> toSend = new();
            
            // Abilities
            if (typeToView == 0 || typeToView == 1)
            {
                EmbedBuilder embed = new EmbedBuilder()
                    .WithAuthor(user)
                    .WithColor(player.Action.Length == 0 ? Color.Red : embedColor)
                    .WithTitle($"{(guild.CurrentPhase == Guild.Phase.Day ? "Day" : "Night")} Action")
                    .WithDescription((player.Action.Length == 0 ? "N/A" : player.Action));
                
                if (player.Action.Length == 0)
                {
                    playersWithoutAction.Add(player);
                } else
                {
                    toSend.Add(embed.Build());
                }
            }

            // Letters
            if ((typeToView == 0 || typeToView == 2) && guild.CurrentPhase == Guild.Phase.Night)
            {
                List<EmbedBuilder> letters = new List<EmbedBuilder>();
                if (player.letters.Count > 0)
                {
                    int count = 1;
                    foreach (Player.Letter letter in player.letters)
                    {
                        IUser? usertmp = program.client.GetUser(letter.recipientID);
                        letters.Add(new EmbedBuilder()
                            .WithAuthor(user)
                            .WithTitle($"Letter #{count} to {usertmp?.Username ?? " <@" + letter.recipientID + ">"}")
                            .WithDescription(letter.content)
                            .WithColor(embedColor));
                        count++;
                    }
                }
                
                foreach (EmbedBuilder letter in letters)
                {
                    toSend.Add(letter.Build());
                }
            }
            
            // this doesn't work idk why.
            //toSend.Last().ToEmbedBuilder().WithTimestamp(DateTimeOffset.UtcNow).Build();

            if (toSend.Count > 0) queue.Add(toSend);
        }

        foreach(List<Embed> messages in queue) {
            foreach(Embed msg in messages)
            {
                await channel.SendMessageAsync(embed:msg);
                await Task.Delay(100);
            }
        }

        // We only need to send this if we're viewing abilities specifically.
        if (typeToView == 0 || typeToView == 1)
        {
            await channel.SendMessageAsync(embed:
                new EmbedBuilder()
                    .WithAuthor("-- N/A -- ")
                    .WithColor(Color.Red)
                    .WithTitle($"Players without action")
                    .WithDescription(String.Join(", ",
                        playersWithoutAction.Select(player =>
                            player.Name + (player.LinkedNames.Count > 0
                                ? $" ({String.Join(", ", player.LinkedNames.Select(x => x.Value))})"
                                : ""))))
                    .Build()
            );
            await Task.Delay(100);
        }

        if ((typeToView == 0 || typeToView == 2) && guild.CurrentPhase == Guild.Phase.Night)
        {
            List<EmbedBuilder> hletters = new List<EmbedBuilder>();
            if (guild.hostLetters.Count > 0)
            {
                foreach (Guild.Letter letter in guild.hostLetters)
                {
                    IUser? usertmp = program.client.GetUser(letter.recipientID);
                    hletters.Add(new EmbedBuilder()
                        .WithAuthor("HOST LETTER")
                        .WithTitle($"Letter to {usertmp?.Username ?? " <@" + letter.recipientID + ">"}")
                        .WithDescription(letter.content)
                        .WithColor(Color.DarkerGrey));
                }
            }

            foreach (EmbedBuilder msg in hletters)
            {
                await channel.SendMessageAsync(embed: msg.Build());
                await Task.Delay(100);
            }
        }

        if ((typeToView == 0 || typeToView == 3) && guild.CurrentPhase == Guild.Phase.Day)
        {
            var croakEmbed = new EmbedBuilder()
                .WithTitle("Croak Votes of the Day")
                .WithColor(Color.DarkRed);
            
            if (guild.Votes.Count == 0)
            {
                croakEmbed.WithDescription("*No croak votes set yet.*");
            }
            else
            {
                List<EmbedFieldBuilder> allCroaks = new List<EmbedFieldBuilder>();
                var sortedCroaks = guild.Votes.OrderByDescending(x => x.Value.Count)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                foreach (var kvp in sortedCroaks)
                {
                    string croakedPlayer = kvp.Key;
                    // This happens if someone did vote them at one point but changed their mind and now they have 0 votes
                    if (guild.Votes[croakedPlayer].Count == 0) continue;
                    
                    var croakersList = new List<string>();
                    foreach (Player player in guild.Votes[croakedPlayer]) croakersList.Add(player.Name);
                    var croakers = string.Join(", ", croakersList);
                    
                    allCroaks.Add(new EmbedFieldBuilder()
                        .WithName($"**{croakedPlayer} - {guild.Votes[croakedPlayer].Count} Votes**"
                                  + (guild.Votes[croakedPlayer].Count >=
                                     (Math.Floor((float)guild.Players.Count / 2) + 1)
                                      ? "*(Past Threshold)*"
                                      : ""))
                        .WithValue($"Voters: {croakers}")
                    );
                }

                croakEmbed.WithFields(allCroaks);
            }
            
            await channel.SendMessageAsync(embed: croakEmbed.Build());
            await Task.Delay(100);
        }

        guild.Save();

        await channel.SendMessageAsync("All actions displayed! Actions will be cleared upon next use of /phase.");
    }
}
