using System.Collections.Concurrent;
using Discord;
using Discord.Net;
using Discord.WebSocket;

namespace mafiacitybot.GuildCommands;

public static class Masquerade
{
    public static async Task CreateCommand(DiscordSocketClient client, SocketGuild? guild = null)
    {
        var command = new SlashCommandBuilder()
            .WithName("host_masquerade")
            .WithDescription("Host-only.");


        command.AddOption(new SlashCommandOptionBuilder()
            .WithName("create")
            .WithDescription("Host-only.")
            .WithType(ApplicationCommandOptionType.SubCommand)
            .AddOption("target", ApplicationCommandOptionType.Channel, "Secret", isRequired: true)
        );

        command.AddOption(new SlashCommandOptionBuilder()
            .WithName("add")
            .WithDescription("Host-only.")
            .WithType(ApplicationCommandOptionType.SubCommand)
            .AddOption("id", ApplicationCommandOptionType.Integer, "Secret", isRequired: true)
            .AddOption("user", ApplicationCommandOptionType.User, "Secret", isRequired: true)
            .AddOption("channel", ApplicationCommandOptionType.Channel, "Secret", isRequired: true)
            .AddOption("whispers", ApplicationCommandOptionType.Integer, "Secret", isRequired: false)
        );
        
        command.AddOption(new SlashCommandOptionBuilder()
            .WithName("end")
            .WithDescription("Host-only.")
            .WithType(ApplicationCommandOptionType.SubCommand)
            .AddOption("id", ApplicationCommandOptionType.Integer, "Secret", isRequired: true)
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
        
        
        switch (command.Data.Options.First().Name)
        {
            case "create":
                await HandleCreate(command, guild);
                break;
            case "add":
                await HandleAdd(command, guild);
                break;
            case "end":
                await HandleEnd(command, guild);
                break;
        }
    }

    public static async Task HandleCreate(SocketSlashCommand command, Guild guild)
    {
        var targetChannel = (SocketTextChannel)command.Data.Options.First().Options
            .First(o => o.Name == "target").Value;

        int tunnelId = guild.MasqueradeTunnels.Keys.Any() ? guild.MasqueradeTunnels.Keys.Max() + 1 : 1;
        var tunnel = new MasqueradeTunnel(tunnelId, targetChannel.Id);
        
        guild.MasqueradeTunnels[tunnelId] = tunnel;

        await command.RespondAsync($"Created a new ballroom with ID {tunnelId} in {targetChannel.Mention}.");
    }
    
    public static async Task HandleAdd(SocketSlashCommand command, Guild guild)
    {
        var options = command.Data.Options.First().Options;
        int id = Convert.ToInt32(options.First(o => o.Name == "id").Value);
        SocketGuildUser user = (SocketGuildUser)options.First(o => o.Name == "user").Value;
        SocketTextChannel channel = (SocketTextChannel)options.First(o => o.Name == "channel").Value;
        int whispers = Convert.ToInt32(options.FirstOrDefault(o => o.Name == "whispers")?.Value ?? 0);

        if (!guild.MasqueradeTunnels.TryGetValue(id, out var tunnel))
        {
            await command.RespondAsync($"No ballroom exists with ID {id}. Run `/host_masquerade create` first.");
            return;
        }

        if (guild.MasqueradeTunnels.Values.Any(t => t.AttendeeIds.Contains(user.Id)))
        {
            await command.RespondAsync($"{user.Mention} is already in another ballroom.");
            return;
        }

        tunnel.AttendeeIds.Add(user.Id);
        tunnel.ChannelIds.Add(channel.Id);

        await command.RespondAsync($"{user.Mention} added to ballroom #{id} with personal channel {channel.Mention}.");
        if (whispers > 0)
        {
            Player? player = guild.Players.Find(player => player.IsPlayer(user.Id));
            player.whisperStock = whispers;
        }
    }

    public static async Task HandleEnd(SocketSlashCommand command, Guild guild)
    {
        var sub = command.Data.Options.First();
        var options = sub.Options;

        var idSubmitted = options.FirstOrDefault(o => o.Name == "id");
        if (idSubmitted == null || idSubmitted.Value == null)
        {
            await command.RespondAsync("You must provide a valid ballroom ID.");
            return;
        }

        int id = Convert.ToInt32(idSubmitted.Value);
        if (guild.MasqueradeTunnels.TryRemove(id, out _))
            await command.RespondAsync($"Closed ballroom #{id}.");
        else
            await command.RespondAsync($"No ballroom found with ID {id}.");
        
    }
    
    public static async Task HandleMessage(SocketMessage msg, Dictionary<ulong, Guild> guilds, DiscordSocketClient client)
    {
        if (msg.Author.IsBot) return;
        if (msg.Channel is not SocketGuildChannel textChannel) return;
        ulong guildId = textChannel.Guild.Id;
        if (!guilds.TryGetValue(guildId, out Guild guild)) return;
        
        var tunnel = guild.MasqueradeTunnels.Values
            .FirstOrDefault(s => s.AttendeeIds.Contains(msg.Author.Id));

        if (tunnel == null) return;
        
        int userIndex = tunnel.AttendeeIds.IndexOf(msg.Author.Id);
        if (userIndex == -1) return;
        
        ulong expectedChannelId = tunnel.ChannelIds[userIndex];
        if (textChannel.Id != expectedChannelId) return;
        if (!tunnel.ForwardingEnabled.TryGetValue(msg.Author.Id, out var active) || !active) return;
        
        if (!tunnel.Prefixes.TryGetValue(msg.Author.Id, out var prefix))
        {
            if (client.GetChannel(expectedChannelId) is IMessageChannel channel)
                await channel.SendMessageAsync("You must choose and don a mask before the Masquerade will open its doors for you." +
                                               "\n*Run \\mask with the prefix option filled in first.*");
            return;
        } 
        if (client.GetChannel(tunnel.TargetId) is IMessageChannel ballroom)
            await ballroom.SendMessageAsync($"[{msg.Author.Username} as {prefix}] {msg.Content}");
        
        for (int i = 0; i < tunnel.AttendeeIds.Count; i++)
        {
            if (i == userIndex) continue;

            if (client.GetChannel(tunnel.ChannelIds[i]) is IMessageChannel playerChannel)
                await playerChannel.SendMessageAsync($"[{prefix}] {msg.Content}");
        }

    }
    
    public class MasqueradeTunnel
    {
        public int Id;
        public List<ulong> AttendeeIds = new();
        public List<ulong> ChannelIds = new();
        public ulong TargetId;

        public ConcurrentDictionary<ulong, bool> ForwardingEnabled { get; set; } = new();
        
        public ConcurrentDictionary<ulong, string> Prefixes { get; set; } = new();
        
        public ConcurrentDictionary<ulong, string> PendingPrefixConfirmations { get; set; } = new();

        // fuck you json deserialization bullshit
        public MasqueradeTunnel() { }
        
        public MasqueradeTunnel(int Id, ulong TargetId)
        {
            this.Id = Id;
            this.TargetId = TargetId;
        }
    }
}

