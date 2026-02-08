using Discord;
using Discord.Net;
using Discord.WebSocket;
using System.Collections.Concurrent;

namespace mafiacitybot.GuildCommands;

public static class Contest
{
    public static async Task CreateCommand(DiscordSocketClient client, SocketGuild? guild = null)
    {
        var command = new SlashCommandBuilder();
        command.WithName("host_contest");
        command.WithDescription("(Host-only) Manage a contest.");
        
        command.AddOption(new SlashCommandOptionBuilder()
            .WithName("setup")
            .WithDescription("Host-only.")
            .WithType(ApplicationCommandOptionType.SubCommand)
            .AddOption("target", ApplicationCommandOptionType.Channel,
                "The channel to log all messages to.", isRequired: true)
            .AddOption("announce", ApplicationCommandOptionType.Boolean,
                "Whether to alert participants when they're added.")
        );
        
        command.AddOption(new SlashCommandOptionBuilder()
            .WithName("add")
            .WithDescription("Host-only.")
            .WithType(ApplicationCommandOptionType.SubCommand)
            .AddOption("contest_id", ApplicationCommandOptionType.String, 
                "The ID of the contest you're adding a PLAYER to.", isRequired: true)
            .AddOption("user", ApplicationCommandOptionType.User, 
                "The PLAYER you're adding to the contest.", isRequired: true)
            .AddOption("max_aliases", ApplicationCommandOptionType.Integer, 
                "How many personalities (aliases) they can set up.", isRequired: true)
        );
        
        command.AddOption(new SlashCommandOptionBuilder()
            .WithName("remove")
            .WithDescription("Host-only")
            .WithType(ApplicationCommandOptionType.SubCommand)
            .AddOption("id", ApplicationCommandOptionType.String, 
                "The ID of the contest to remove a PLAYER from.", isRequired: true)
            .AddOption("user", ApplicationCommandOptionType.User, 
                "The PLAYER to remove from the contest.", isRequired: true)
        );
        
        command.AddOption(new SlashCommandOptionBuilder()
            .WithName("close")
            .WithDescription("Host-only")
            .WithType(ApplicationCommandOptionType.SubCommand)
            .AddOption("id", ApplicationCommandOptionType.String, 
                "The ID of the contest to close.", isRequired: true)
        );
        
        command.AddOption(new SlashCommandOptionBuilder()
            .WithName("list")
            .WithDescription("Host-only.")
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
        if(!program.guilds.TryGetValue((ulong)command.GuildId, out Guild guild))
        {
            await command.RespondAsync($"You must use /setup before being able to use this command!");
            return;
        }
        
        if (!Guild.IsHostRoleUser(command, guild.HostRoleID)) {
            await command.RespondAsync($"You must have the host role to use this command!");
            return;
        }
        if (guild.HostChannelID != command.ChannelId) {
            await command.RespondAsync($"Command must be executed in the host channel!");
            return;
        }
        
        var subcommand = command.Data.Options.First().Name;
        switch (subcommand)
        {
            case "setup":
                var setupParams = command.Data.Options.First().Options.ToDictionary(o => o.Name, o => o.Value);
                var targetChannel = (SocketGuildChannel)setupParams["target"];
                setupParams.TryGetValue("announce", out var announceSetup);
                bool shouldAnnounce = announceSetup != null && (bool)announceSetup;
                
                char contestId = guild.GetChatID();
                var newContest = new ContestRoom(contestId, targetChannel.Id, shouldAnnounce);
                guild.Contests[contestId] = newContest;
                
                await command.RespondAsync($"Created contest with ID `{contestId}` logging to {targetChannel.Name}.");
                break;
            
            case "add":
                var addParams = command.Data.Options.First().Options.ToDictionary(o => o.Name, o => o.Value);
                var addContestId = ((string)addParams["contest_id"])[0];
                
                if (!guild.Contests.TryGetValue(addContestId, out var contest))
                {
                    await command.RespondAsync($"No contest found with ID `{addContestId}`.");
                    return;
                }
                
                var user = (SocketGuildUser)addParams["user"];
                var maxAliases = Convert.ToInt32(addParams["max_aliases"]);
                
                if (maxAliases < 1)
                {
                    await command.RespondAsync("You must give each user at least one personality.");
                    return;
                }
                
                Player? playerObj = guild.Players.Find(p => p.IsPlayer(user.Id));
                if (playerObj == null)
                {
                    await command.RespondAsync($"{user.Username} is not a PLAYER in the game.");
                    return;
                }
                
                if (contest.RegisteredUsers.ContainsKey(user.Id))
                {
                    await command.RespondAsync($"{user.Username} is already registered in this contest.");
                    return;
                }
                
                contest.RegisteredUsers[user.Id] = new ContestUserRegistration(user.Id, playerObj.ChannelID, maxAliases);
                
                await command.RespondAsync($"Registered {user.Username} for contest `{addContestId}` with {maxAliases} personalities.");
                
                if (contest.announce)
                {
                    var participantChannel = await program.client.GetChannelAsync(playerObj.ChannelID) as ITextChannel;
                    if (participantChannel != null)
                    {
                        await participantChannel.SendMessageAsync(
                            $"**Step right up for a thrilling contest!**\n*You are now a participant in the Ringleader Puppet's contest with id `{addContestId}`. Use `/forward_contest {addContestId} [personality] [messagePrefix]` to set up your personality and message prefix.*");
                    }
                }
                break;
            
            case "remove":
                var removeParams = command.Data.Options.First().Options.ToDictionary(o => o.Name, o => o.Value);
                var removeContestId = ((string)removeParams["id"])[0];
                
                if (!guild.Contests.TryGetValue(removeContestId, out var removeContest))
                {
                    await command.RespondAsync($"No contest found with ID `{removeContestId}`.");
                    return;
                }
                
                var removeUser = (SocketGuildUser)removeParams["user"];
                
                if (!removeContest.RegisteredUsers.TryRemove(removeUser.Id, out var registration))
                {
                    await command.RespondAsync($"{removeUser.Username} is not registered in contest `{removeContestId}`.");
                    return;
                }
                
                await command.RespondAsync($"Removed {removeUser.Username} from contest `{removeContestId}`.");
                break;
            
            case "close":
                var closeId = ((string)command.Data.Options.First().Options.First().Value)[0];
                
                if (guild.Contests.TryRemove(closeId, out var closedContest))
                {
                    int totalAliases = closedContest.RegisteredUsers.Values.Sum(r => r.Aliases.Count);
                    await command.RespondAsync($"Closed contest `{closeId}`.");
                    guild.ChatIDs.Enqueue(closeId);
                }
                else
                {
                    await command.RespondAsync($"No contest found with ID `{closeId}`.");
                }
                break;
            
            case "list":
                if (guild.Contests.Count == 0)
                {
                    await command.RespondAsync("No active contests.");
                    return;
                }
                
                var server = (command.User as SocketGuildUser)?.Guild;
                if (server == null)
                {
                    await command.RespondAsync("This bot only works inside a server.");
                    return;
                }
                
                string result = "";
                foreach (var contestEntry in guild.Contests.OrderBy(c => c.Key))
                {
                    var contestRoom = contestEntry.Value;
                    int totalAliases = contestRoom.RegisteredUsers.Values.Sum(r => r.Aliases.Count);
                    result += $"Contest {contestRoom.Id} ({contestRoom.RegisteredUsers.Count} users, {totalAliases} aliases):\n";
                    
                    foreach (var p in contestRoom.RegisteredUsers.Values.OrderBy(r => r.UserId))
                    {
                        var discordUser = server.GetUser(p.UserId);
                        var channel = server.GetTextChannel(p.ChannelId);
                        result += $"  {discordUser?.Username ?? "Unknown"} (max: {p.MaxAliases}, channel: {channel?.Name ?? "Unknown"}):\n";
                        
                        if (p.Aliases.Count == 0)
                        {
                            result += $"    (no aliases set yet)\n";
                        }
                        else
                        {
                            foreach (var alias in p.Aliases.Values)
                            {
                                result += $"    - Alias: '{alias.Alias}', Prefix: '{alias.Prefix}'\n";
                            }
                        }
                    }
                    result += "\n";
                }
                
                await command.RespondAsync($"```{result}```");
                break;
            
            default:
                await command.RespondAsync("Unknown subcommand.");
                break;
        }
    }

    public static async Task HandleMessage(SocketMessage msg, Dictionary<ulong, Guild> guilds)
    {
        if (msg.Author.IsBot) return;
        if (msg.Channel is not SocketGuildChannel textChannel) return;
        ulong guildId = textChannel.Guild.Id;
        if (!guilds.TryGetValue(guildId, out Guild guild)) return;
        
        foreach (var contest in guild.Contests.Values)
        {
            // go my checks
            if (!contest.RegisteredUsers.TryGetValue(msg.Author.Id, out var senderRegistration)) continue;
            if (senderRegistration.ChannelId != msg.Channel.Id) continue;
            
            foreach (var senderAlias in senderRegistration.Aliases.Values)
            {
                if (!msg.CleanContent.StartsWith(senderAlias.Prefix)) continue;
                
                string messageContent = msg.CleanContent.Substring(senderAlias.Prefix.Length);
                
                if (string.IsNullOrWhiteSpace(messageContent)) continue;
                
                // dont we all love nested foreach loops!!
                foreach (var recipientRegistration in contest.RegisteredUsers.Values)
                {
                    if (recipientRegistration.UserId == msg.Author.Id) continue;
                    
                    var recipientChannel = textChannel.Guild.GetTextChannel(recipientRegistration.ChannelId);
                    if (recipientChannel != null)
                    {
                        await recipientChannel.SendMessageAsync($"**{senderAlias.Alias}**: {messageContent}");
                    }
                }
                
                var logChannel = textChannel.Guild.GetTextChannel(contest.targetChannelId);
                if (logChannel != null)
                {
                    var senderUser = textChannel.Guild.GetUser(msg.Author.Id);
                    await logChannel.SendMessageAsync($"[{senderUser?.Username ?? "PLAYER"} as {senderAlias.Alias}]: {messageContent}");
                }

                return;
            }
            
        }
    }
    
    
    public class ContestRoom
    {
        public char Id { get; }
        public ulong targetChannelId { get; }
        public bool announce { get; set; }
        public ConcurrentDictionary<ulong, ContestUserRegistration> RegisteredUsers { get; set; } = new();

        public ContestRoom(char Id, ulong targetChannelId, bool announce = false)
        {
            this.Id = Id;
            this.targetChannelId = targetChannelId;
            this.announce = announce;
        }
    }
    
    public class ContestUserRegistration
    {
        public ulong UserId { get; }
        public ulong ChannelId { get; }
        public int MaxAliases { get; }
        public ConcurrentDictionary<string, ContestAlias> Aliases { get; set; } = new();

        public ContestUserRegistration(ulong userId, ulong channelId, int maxAliases)
        {
            this.UserId = userId;
            this.ChannelId = channelId;
            this.MaxAliases = maxAliases;
        }
    }
    
    public class ContestAlias
    {
        public string Alias { get; set; }
        public string Prefix { get; set; }

        public ContestAlias(string alias, string prefix)
        {
            this.Alias = alias;
            this.Prefix = prefix;
        }
    }
}