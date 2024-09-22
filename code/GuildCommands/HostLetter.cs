using Discord;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Numerics;

namespace mafiacitybot.GuildCommands
{
    public static class HostLetter {
        public static async Task CreateCommand(DiscordSocketClient client, SocketGuild? guild = null)
        {
            var command = new SlashCommandBuilder();
            command.WithName("host_letter");
            command.WithDescription("View, add, remove the Host-letter(s) being send this day! Also lets you set letter limits.");
            command.AddOption(new SlashCommandOptionBuilder()
                .WithName("add")
                .WithDescription("Add a letter as the host.")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("recipient", ApplicationCommandOptionType.User, "The person you wish to send the letter to.", isRequired: true)
            ).AddOption(new SlashCommandOptionBuilder()
                .WithName("view")
                .WithDescription("View host letters or a specific letter.")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("letter", ApplicationCommandOptionType.Integer, "The letter to view", isRequired: false)
            ).AddOption(new SlashCommandOptionBuilder()
                .WithName("remove")
                .WithDescription("Remove a letter in the host list.")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("letter", ApplicationCommandOptionType.Integer, "The letter number to delete.", isRequired: true)
            ).AddOption(new SlashCommandOptionBuilder()
                .WithName("edit")
                .WithDescription("Edit a letter in the list.")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("letter", ApplicationCommandOptionType.Integer, "The letter number to delete.", isRequired: true)
                .AddOption("recipient", ApplicationCommandOptionType.User, "The person you wish to send the letter to.", isRequired: true)
            ).AddOption(new SlashCommandOptionBuilder()
                .WithName("limit")
                .WithDescription("Set letter limit of a player.")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("player", ApplicationCommandOptionType.User, "Player to change the limit for", isRequired: true)
                .AddOption("limit", ApplicationCommandOptionType.Number, "New letter limit", isRequired: true)
            ).AddOption(new SlashCommandOptionBuilder()
                .WithName("send")
                .WithDescription("Send all letters.")
                .WithType(ApplicationCommandOptionType.SubCommand)
            );

            try
            {
                if (guild != null) {
                    await guild.CreateApplicationCommandAsync(command.Build());
                } else {
                    SocketApplicationCommand cmd = await client.CreateGlobalApplicationCommandAsync(command.Build());
                    Console.WriteLine($"Added {cmd.Name} at {cmd.CreatedAt}");
                }
            }
            catch (ApplicationCommandException exception)
            {
                    // If our command was invalid, we should catch an ApplicationCommandException. This exception contains the path of the error as well as the error message. You can serialize the Error field in the exception to get a visual of where your error is.
                    var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);

                    // You can send this error somewhere or just print it to the console, for this example we're just going to print it.
                    Console.WriteLine(json);
            }
        }

        public static async Task HandleCommand(SocketSlashCommand command, Program program)
        {
            var user = command.User;
            var channel = command.Channel;

            if(!program.guilds.TryGetValue((ulong)command.GuildId, out Guild guild))
            {
                await command.RespondAsync($"You must use setup before being able to use this command!");
                return;
            }

            if (guild.CurrentPhase != Guild.Phase.Night)
            {
                await command.RespondAsync($"Letter commands can only be used during the night!");
                return;
            }

            //if (guild.isLocked) {
            //    await command.RespondAsync($"Letter commands are currently locked, please notify the hosts if you believe this is not intended!");
            //    return;
            //}

            var fieldName = command.Data.Options.First().Name;
            var options = command.Data.Options.First().Options;

            if (!Guild.IsHostRoleUser(command, guild.HostRoleID)) {
                await command.RespondAsync($"You must have the host role to use this command!");
                return;
            }
            if (guild.HostChannelID != command.ChannelId) {
                await command.RespondAsync($"Command must be executed in the host channel!");
                return;
            }

            switch(fieldName)
            {
                case "view":

                    if (guild.hostLetters?.Count < 1) {
                        await command.RespondAsync("No host letters!");
                    } else {
                        if (options != null && options.Count > 0) {
                            long letter = (long)options.First().Value - 1;
                            if (letter < 0 || guild.hostLetters.Count < letter) {
                                await command.RespondAsync("Invalid letter number.\nValid letter numbers: " + new Range(0, guild.hostLetters.Count - 1));
                            } else {
                                await command.RespondAsync(guild.hostLetters[(int)letter].content);
                            }
                        } else {
                            string response = "";
                            int count = 1;
                            foreach (Guild.Letter letter in guild.hostLetters) {
                                response += $"Letter #{count} to {program.client.GetUser(letter.recipientID).Username}:\n`{letter.content.Substring(0, Math.Min(letter.content.Length, 130))}...`\n";
                                count++;
                            }

                            await command.RespondAsync(response);
                        }
                    }

                    break;
                case "add":

                    SocketGuildUser p = options.ElementAt(0).Value as SocketGuildUser;
                    Player recipient = guild.Players.Find(x => x != null && x.PlayerID == p.Id);

                    if (recipient == null) {
                        await command.RespondAsync("Recipient must be a valid player in this game");
                        return;
                    }
                    var mb = new ModalBuilder()
                        .WithTitle("Add Letter")
                        .WithCustomId("host_add_letter|" + recipient.PlayerID)
                        .AddTextInput("Letter content", "content", TextInputStyle.Paragraph, maxLength: 2000);

                    await command.RespondWithModalAsync(mb.Build());
                    break;

                case "remove":
                    if (guild.hostLetters?.Count < 1) {
                        await command.RespondAsync("No host letters!");
                    } else {
                        long letter = (long)options.First().Value - 1;
                        if (letter < 0 || guild.hostLetters.Count < letter) {
                            await command.RespondAsync("Invalid letter number.\nValid letter numbers: " + new Range(0, guild.hostLetters.Count - 1));
                        } else {
                            guild.hostLetters.RemoveAt((int)letter);
                            await command.RespondAsync("Removed letter #" + letter + 1);
                        }
                    }
                    break;
                case "edit":
                    if (guild.hostLetters.Count < 1)
                    {
                        await command.RespondAsync("No host letters found.");
                    }
                    else
                    {
                        long letter = (long)options.First().Value - 1;
                        SocketGuildUser rec = (SocketGuildUser)options.ElementAt(1).Value;

                        Player to = guild.Players.Find(x => x != null && x.PlayerID == rec.Id);

                        if (to == null) {
                            await command.RespondAsync("Recipient must be a valid player in this game");
                            return;
                        }

                        if (letter < 0 || guild.hostLetters.Count < letter)
                        {
                            await command.RespondAsync("Invalid letter number.\nValid letter numbers: " + new Range(0, guild.hostLetters.Count - 1));
                        }
                        else
                        {
                            var modalb = new ModalBuilder()
                            .WithTitle("Edit Letter")
                            .WithCustomId("host_edit_letter|" + to.PlayerID + "|" + letter)
                            .AddTextInput("Letter content", "content", TextInputStyle.Paragraph, maxLength: 2000);

                            await command.RespondWithModalAsync(modalb.Build());
                        }
                    }

                    break;
                case "send":
                    Dictionary<Player, List<string>> lettersToSend = new();

                    foreach (Player ply in guild.Players) {
                        if (ply.letters.Count > 0) {
                            foreach (Player.Letter letter in ply.letters) {
                                Player? sendTo = guild.Players.Find(p => p != null && p.PlayerID == letter.recipientID);
                                if (sendTo == null) {
                                    await command.RespondAsync("Cannot find recipient with id " + letter.recipientID + " as user.. stopping.");
                                    return;
                                }

                                if (lettersToSend.TryGetValue(sendTo, out List<string> letters)) {
                                    letters.Add(letter.content);
                                } else {
                                    lettersToSend.Add(sendTo, new List<string> { letter.content });
                                }
                            }
                        }
                    }
                    foreach (Guild.Letter letter in guild.hostLetters) {

                        Player? sendTo = guild.Players.Find(p => p != null && p.PlayerID == letter.recipientID);
                        if (sendTo == null) {
                            await command.RespondAsync("Cannot find recipient with id " + letter.recipientID + " as player.. stopping.");
                            return;
                        }

                        if (lettersToSend.TryGetValue(sendTo, out List<string> letters)) {
                            letters.Add(letter.content);
                        } else {
                            lettersToSend.Add(sendTo, new List<string> { letter.content });
                        }
                    }

                    await command.RespondAsync("Sending letters...");

                    // check if all channels work
                    foreach (Player ply in lettersToSend.Keys) {
                        IMessageChannel channelToSendTo = await program.client.GetChannelAsync(ply.ChannelID) as IMessageChannel;

                        if (channelToSendTo == null) {
                            await command.RespondAsync("Cannot find channel of player " + ply.Name);
                            return;
                        }
                    }

                    foreach (Player ply in lettersToSend.Keys) {
                        IMessageChannel channelToSendTo = await program.client.GetChannelAsync(ply.ChannelID) as IMessageChannel;

                        if (channelToSendTo == null) {
                            await command.RespondAsync("Cannot find channel of player " + ply.Name);
                            return;
                        }
                        int i = 0;
                        string[] msg = { "first", "second", "third", "fourth", "fifth", "sixth", "seventh", "eight", "ninth", "tenth" };
                        foreach (string content in lettersToSend[ply]) {
                            await channelToSendTo.SendMessageAsync($"**You received your {(i < 10 ? msg[i] : i + "th")} letter. It reads:**");
                            if(content.Length < 2000-6) await channelToSendTo.SendMessageAsync($"```{content}```");
                            else await channelToSendTo.SendMessageAsync($"{content}");
                            i++;
                            await Task.Delay(100);
                        }

                    }

                    await channel.SendMessageAsync("Done!");
                    break;
                case "limit":
                    SocketGuildUser player = (SocketGuildUser)options.ElementAt(0).Value;
                    int limit = Convert.ToInt32((double)options.ElementAt(1).Value);

                    Player playerToSetLimitOf = guild.Players.Find(x => x != null && x.PlayerID == player.Id);

                    if (playerToSetLimitOf == null) {
                        await command.RespondAsync("Recipient must be a valid player in this game");
                        return;
                    }

                    playerToSetLimitOf.letterLimit = limit;

                    await command.RespondAsync($"Set limit of player {playerToSetLimitOf.Name} to {playerToSetLimitOf.letterLimit}");
                    break;
                default:
                    await command.RespondAsync("You cannot use this command.");
                    break;
            }
            guild.Save();
        }

        public static async Task ModalSubmitted(SocketModal modal)
        {
            if (!modal.Data.CustomId.StartsWith("host_add_letter") && !modal.Data.CustomId.StartsWith("host_edit_letter")) return;

            var user = modal.User;
            var channel = modal.Channel;

            if (!Program.instance.guilds.TryGetValue((ulong)modal.GuildId, out Guild guild))
            {
                await modal.RespondAsync($"You must use setup before being able to use this command!");
                return;
            }

            // Get the values of components.
            List<SocketMessageComponentData> components = modal.Data.Components.ToList();
            string content = components.First(x => x.CustomId == "content").Value;

            if (modal.Data.CustomId.StartsWith("host_add_letter"))
            {

                ulong recipientId = Convert.ToUInt64(modal.Data.CustomId.Split("|")[1]);

                Player recipient = guild.Players.Find(x => x != null && x.PlayerID == recipientId);

                if (recipient == null) {
                    await modal.RespondAsync("Recipient must be a valid player in this game");
                    return;
                }

                guild.hostLetters.Add(new Guild.Letter(recipientId, content));
                string header = $"Letter added! Letter #{guild.hostLetters.Count} to {Program.instance.client.GetUser(recipientId)?.Username ?? $"<@{recipientId}>"}:";
                await modal.RespondAsync($"{header}\n`{content.Substring(0, Math.Min(content.Length, 2000 - header.Length - 3))}`");
            } else if(modal.Data.CustomId.StartsWith("host_edit_letter"))
            {

                ulong recipientId = Convert.ToUInt64(modal.Data.CustomId.Split("|")[1]);
                long letter = Convert.ToInt64(modal.Data.CustomId.Split("|")[2]);

                Guild.Letter l = guild.hostLetters[(int)letter];
                l.recipientID = recipientId;
                l.content = content;
                guild.hostLetters.RemoveAt((int)letter);
                guild.hostLetters.Insert((int)letter, l);
                string header = $"Letter changed! Letter #{letter + 1} to {Program.instance.client.GetUser(recipientId)?.Username ?? $"<@{recipientId}>"}:";
                await modal.RespondAsync($"{header}\n`{content.Substring(0, Math.Min(content.Length, 2000 - header.Length - 3))}`");
            }
            guild.Save();
        }
    }
}