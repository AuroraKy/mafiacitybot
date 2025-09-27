using Discord;
using Discord.Net;
using Discord.WebSocket;

namespace mafiacitybot.GuildCommands;

public static class Forward
{
    public static async Task CreateCommand(DiscordSocketClient client, SocketGuild? guild = null)
    {
        var command = new SlashCommandBuilder();
        command.WithName("forward");
        command.WithDescription("Manage forwarding messages through anon chats.");
        command.AddOption("status", ApplicationCommandOptionType.Boolean, "Whether to enable or disable forwarding.", isRequired: true);
        command.AddOption("prefix", ApplicationCommandOptionType.String, "The prefix attached to your forwarded messages.");
        command.AddOption("id", ApplicationCommandOptionType.String, "The ID of the anonymous chat to manage.");

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

        /* if (!player.ForwardingAllowed)
        {
            await command.RespondAsync("You are not allowed to enable message forwarding. You are likely not in any anon chats.");
            return;
        } */

        bool status = (bool)command.Data.Options.First(option => option.Name == "status").Value;
        
        char? id = null;
        var userSetId = command.Data.Options.FirstOrDefault(option => option.Name == "id");
        if (userSetId != null) id = ((string)userSetId)[0];
        
        var userSetPrefix = command.Data.Options.FirstOrDefault(option => option.Name == "prefix");
        string? prefix = userSetPrefix?.Value?.ToString();

        // i love linq i love linq i love linq im going fucking insane
        var chatTunnels = guild.AnonChats.Values.Where(t => t.Source == user.Id || t.Receiver == user.Id).ToList();
        if (chatTunnels.Count == 0)
        {
            await command.RespondAsync("You are not currently in any anonymous chats.");
            return;
        }

        AnonChat.AnonChatTunnel? tunnel = null;
        // sorry i made the logic flow here kinda bad
        // TODO: make this less garbage
        if (id is not null)
        {
            guild.AnonChats.TryGetValue((char)id, out tunnel);
            if (tunnel == null || (tunnel.Source != user.Id && tunnel.Receiver != user.Id))
            {
                await command.RespondAsync("Either this chat does not exist, or you do not have access to it.");
                return;
            }
        }
        else
        {
            if (chatTunnels.Count > 1)
            {
                await command.RespondAsync("You are in multiple anonymous chats and must specify an ID.");
                return; 
            }
            
            tunnel = chatTunnels.FirstOrDefault(t => t.Source == user.Id || t.Receiver == user.Id);
        }
        
        tunnel.ForwardingUsers[user.Id] = status;
        bool resetPrefix = false;
        if (!string.IsNullOrWhiteSpace(prefix)) tunnel.ForwardingPrefixes[user.Id] = prefix;
        else
        {
            if (tunnel.ForwardingPrefixes.ContainsKey(user.Id))
            {
                tunnel.ForwardingPrefixes[user.Id] = "Source";
                resetPrefix = true;
            }
            else resetPrefix = false;
        }

        string toSend = $"Forwarding {(status ? "enabled" : "disabled")} for chat with ID `{tunnel.Id}`. ";
        toSend += ((resetPrefix && !status) ? $"Your messages will be prefixed with \"{prefix}\"." : "");
        await command.RespondAsync(toSend);
    }
}
