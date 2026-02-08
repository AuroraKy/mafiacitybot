using System.Net.Mime;
using Discord;
using Discord.Net;
using Discord.WebSocket;

namespace mafiacitybot.GuildCommands
{
    public static class WhisperMasquerade
    {
        public static async Task CreateCommand(DiscordSocketClient client, SocketGuild? guild = null)
        {
            var command = new SlashCommandBuilder();
            command.WithName("whisper");
            command.WithDescription("Whisper to another dancer at the Masquerade.");
            command.AddOption("alias", ApplicationCommandOptionType.String, "The alias of the dancer to whisper to", isRequired: true);

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
                await command.RespondAsync("You part your lips, but no sound comes out." +
                                           "\n*Either you have used all your whispers, or this Ballroom does not allow them.*");
            }
            
            string alias = command.Data.Options.First(o => o.Name == "alias").Value.ToString();
            
            var tunnel = guild.MasqueradeTunnels.Values
                .FirstOrDefault(t => t.AttendeeIds.Contains(user.Id));

            if (tunnel == null)
            {
                await command.RespondAsync("**No ballroom welcomes you.** " +
                                            "\n*The night is yet young. You will don your mask when the time is right.*");
                return;
            }

            if (!tunnel.Prefixes.TryGetValue(user.Id, out var senderPrefix))
            {
                await command.RespondAsync("The masquerade only admits the masked. In the moonlight, your naked face shows your shame." +
                                           "\n *Run \\forward first with the prefix option filled in.");
                return;
            }
            
            // wtf type even is this
            var recipient = tunnel.Prefixes
                .FirstOrDefault(kv => kv.Value.Equals(alias, StringComparison.OrdinalIgnoreCase));
            
            if (recipient.Key == 0)
            {
                await command.RespondAsync($"No dancer with the mask of **{alias}** was found.");
                return;
            }
            
            if (recipient.Key == user.Id)
            {
                await command.RespondAsync("It is not sane to whisper to oneself.");
                return;
            }
            
            var modal = new ModalBuilder()
                .WithTitle($"Whisper to {alias}")
                .WithCustomId($"whisper_modal:{tunnel.Id}:{recipient.Key}")
                .AddTextInput("Message", "whisper_message", TextInputStyle.Paragraph, "Type your secret whisper.", maxLength: 300);

            await command.RespondWithModalAsync(modal.Build());
        }

        public static async Task ModalSubmitted(SocketModal modal, DiscordSocketClient client)
        {
            if (!modal.Data.CustomId.StartsWith("whisper_modal:")) return;
            
            if (!Program.instance.guilds.TryGetValue((ulong)modal.GuildId, out Guild guild))
            {
                await modal.RespondAsync($"You must use /setup before being able to use this command!");
                return;
            }
            
            var parts = modal.Data.CustomId.Split(':');
            int tunnelId = int.Parse(parts[1]);
            ulong recipientId = ulong.Parse(parts[2]);

            if (!guild.MasqueradeTunnels.TryGetValue(tunnelId, out var tunnel)) return;

            var sender = (SocketGuildUser)modal.User;
            string message = modal.Data.Components.First(c => c.CustomId == "whisper_message").Value.Trim();

            if (message.Length == 0)
            {
                await modal.RespondAsync(
                    "You part your lips, but no sound escapes. \n*(You can't send an empty whisper.)*");
            }
            
            if (!tunnel.Prefixes.TryGetValue(sender.Id, out var senderPrefix))
            {
                await modal.RespondAsync("You have yet to don a mask." +
                                         "\n*Run \\forward first with the prefix option filled in.");
                return;
            }
            
            int recipientIndex = tunnel.AttendeeIds.IndexOf(recipientId);
            if (recipientIndex == -1)
            {
                await modal.RespondAsync("That dancer is not in your Ballroom. You whisper, but no one hears it.");
                return;
            }
            
            ulong recipientChannelId = tunnel.ChannelIds[recipientIndex];

            if (client.GetChannel(recipientChannelId) is IMessageChannel recipientChannel)
            {
                await recipientChannel.SendMessageAsync($"*From thin air, you hear a voice in your ear. **{senderPrefix}** whispers to you*: " +
                                                        $"\n```{message}```");
            }

            await modal.RespondAsync($"You whisper to the Dancer wearing the mask of **{tunnel.Prefixes[recipientId]}**." +
                                     $"\n*Your whisper was sent successfully. It read:*" +
                                     $"```{message}```");
            Player? player = guild.Players.Find(player => player.IsPlayer(sender.Id));
            player.whisperStock--;
        }
    }
}

