using System.Net.Mime;
using System.Runtime.Loader;
using Discord;
using Discord.Net;
using Discord.WebSocket;

namespace mafiacitybot.GuildCommands
{
    public static class Whisper
    {
        public static async Task CreateCommand(DiscordSocketClient client, SocketGuild? guild = null)
        {
            var command = new SlashCommandBuilder();
            command.WithName("whisper");
            command.WithDescription("Whisper to another PLAYER.");
            command.AddOption("name", ApplicationCommandOptionType.User, "The PLAYER to send your message to.", isRequired: true);

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
            
            SocketGuildUser user = (SocketGuildUser)command.User;
            SocketGuildChannel channel = (SocketGuildChannel)command.Channel;
            Player? player = guild.Players.Find(player => player.IsPlayer(user.Id));
            if (player == null || player.ChannelID != channel.Id)
            {
                await command.RespondAsync("This command can only be used by a PLAYER in their channel!");
                return;
            }

            if (player.whisperStock == 0)
            {
                await command.RespondAsync("You have no whispers remaining.");
            }
            
            SocketGuildUser? p = (SocketGuildUser)command.Data.Options.First().Value;
            Player? recipient = guild.Players.Find(x => x != null && x.IsPlayer(p.Id));

            if (recipient == null) {
                await command.RespondAsync("Recipient must be a valid PLAYER in this game.");
                return;
            }
            
            if (recipient.PlayerID == user.Id)
            {
                await command.RespondAsync("You can't whisper to yourself!");
                return;
            }
            
            var modal = new ModalBuilder()
                .WithTitle($"Write Whisper")
                .WithCustomId($"norm_whisper_modal:{recipient.PlayerID}")
                .AddTextInput("Message", "whisper_message", TextInputStyle.Paragraph, "Type your secret whisper.", maxLength: 300);

            await command.RespondWithModalAsync(modal.Build());
        }

        public static async Task ModalSubmitted(SocketModal modal, Program program)
        {
            if (!modal.Data.CustomId.StartsWith("norm_whisper_modal:")) return;
            
            if (!Program.instance.guilds.TryGetValue((ulong)modal.GuildId, out Guild guild))
            {
                await modal.RespondAsync($"You must use /setup before being able to use this command!");
                return;
            }
            
            var parts = modal.Data.CustomId.Split(':');
            ulong recipientId = ulong.Parse(parts[1]);
            

            var sender = (SocketGuildUser)modal.User;
            string message = modal.Data.Components.First(c => c.CustomId == "whisper_message").Value.Trim();

            if (message.Length == 0)
            {
                await modal.RespondAsync("You can't send an empty whisper.");
            }
            
            Player? sendTo = guild.Players.Find(p => p != null && p.IsPlayer(recipientId));
            if (sendTo == null)
            {
                await modal.RespondAsync("Something is straight fuuuuucked up man");
            }
            IMessageChannel channelToSendTo = await program.client.GetChannelAsync(sendTo.ChannelID) as IMessageChannel;
            if (channelToSendTo == null) {
                await modal.RespondAsync("Cannot find channel of player " + sendTo.Name + ".");
                return;
            }
            
            await channelToSendTo.SendMessageAsync($"*You hear a whisper in your ear...*\n```{message}```");
            await modal.RespondAsync($"You whispered the following to {sendTo.Name}: \n```{message}```");
            Player? player = guild.Players.Find(player => player.IsPlayer(sender.Id));
            player.whisperStock--;
        }
    }
    
    public static class SetWhispers
    {
        public static async Task CreateCommand(DiscordSocketClient client, SocketGuild? guild = null)
        {
            var command = new SlashCommandBuilder();
            command.WithName("host_whisper");
            command.WithDescription("Host-only.");
            command.AddOption(new SlashCommandOptionBuilder()
                .WithName("set_all")
                .WithDescription("Host-only.")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("amt", ApplicationCommandOptionType.Integer, "The amount of whispers to set.", true)
            );
            command.AddOption(new SlashCommandOptionBuilder()
                .WithName("set")
                .WithDescription("Host-only.")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("player", ApplicationCommandOptionType.User, "The player to give whispers to.", isRequired: true)
                .AddOption("amt",  ApplicationCommandOptionType.Integer, "The amount of whispers to set.", isRequired: true)
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
            var fieldName = command.Data.Options.First().Name;
            var options = command.Data.Options.First().Options;
            switch (fieldName)
            {
                case "set_all":
                    int whispers = Convert.ToInt32(options.First(o => o.Name == "amt")?.Value ?? 0);
                    foreach (Player player in guild.Players)
                    {
                        player.whisperStock = whispers;
                    }

                    await command.RespondAsync($"Gave all PLAYERS {whispers} whispers.");
                    break;
                
                case "set":
                    SocketGuildUser? p = options.ElementAt(0).Value as SocketGuildUser;
                    Player? whisperer = guild.Players.Find(x => x != null && x.IsPlayer(p.Id));

                    if (whisperer == null) {
                        await command.RespondAsync("Target is not a valid PLAYER in this game.");
                        return;
                    }
                    int whisperAmt =  Convert.ToInt32(options.First(o => o.Name == "amt")?.Value ?? 0);
                    whisperer.whisperStock = whisperAmt;

                    await command.RespondAsync($"Set {whisperer.Name}'s whisper count to {whisperAmt}.");
                    break;
                
                default:
                    await command.RespondAsync("Uh oh! Something very bad has happened.");
                    break;
            }
                
        }
    }
}