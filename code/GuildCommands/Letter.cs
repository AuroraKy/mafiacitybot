using Discord;
using Discord.Net;
using Discord.WebSocket;
using System.ComponentModel;

namespace mafiacitybot.GuildCommands
{
    public static class Letter
    {
        public static async Task CreateCommand(DiscordSocketClient client, SocketGuild? guild = null)
        {
            var command = new SlashCommandBuilder();
            command.WithName("letter");
            command.WithDescription("View, add, remove or change the letter(s) being send this day!");
            command.AddOption(new SlashCommandOptionBuilder()
                .WithName("view")
                .WithDescription("View your current letters or a specific letter.")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("letter", ApplicationCommandOptionType.Integer, "The letter to view", isRequired: false)
            ).AddOption(new SlashCommandOptionBuilder()
                .WithName("add")
                .WithDescription("Add a letter to send.")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("recipient", ApplicationCommandOptionType.User, "The person you wish to send the letter to.", isRequired: true)
            ).AddOption(new SlashCommandOptionBuilder()
                .WithName("remove")
                .WithDescription("Remove a letter in the list.")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("letter", ApplicationCommandOptionType.Integer, "The letter number to delete.", isRequired: true)
            ).AddOption(new SlashCommandOptionBuilder()
                .WithName("edit")
                .WithDescription("Edit a letter in the list.")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("letter", ApplicationCommandOptionType.Integer, "The letter number to delete.", isRequired: true)
                .AddOption("recipient", ApplicationCommandOptionType.User, "The person you wish to send the letter to.", isRequired: true)
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
                await command.RespondAsync($"You must use setup before being able to use this command!");
                return;
            }

            if (guild.CurrentPhase != Guild.Phase.Night)
            {
                await command.RespondAsync($"Letter commands can only be used during the night!");
                return;
            }

            if (guild.isLocked) {
                await command.RespondAsync($"Letter commands are currently locked, please notify the hosts if you believe this is not intended!");
                return;
            }

            var fieldName = command.Data.Options.First().Name;
            var options = command.Data.Options.First().Options;

            bool isHost = Guild.IsHostRoleUser(command, guild.HostRoleID) && guild.HostChannelID == command.ChannelId;

            Player? player = guild.Players.Find(player => player.IsPlayer(user.Id));
            if (player == null || player.ChannelID != channel.Id)
            {
                await command.RespondAsync("This command can only be used by a player in their channel!");
                return;
            }

            switch(fieldName)
            {
                case "view":

                    if(player.letters?.Count < 1)
                    {
                        await command.RespondAsync("You have no letters!");
                    } else
                    {
                        if(options != null && options.Count > 0)
                        {
                            long letter = (long)options.First().Value - 1;
                            if(letter < 0 || player.letters.Count < letter)
                            {
                                await command.RespondAsync("Invalid letter number.\nValid letter numbers: " + new Range(0, player.letters.Count - 1));
                            } else
                            {
                                await command.RespondAsync(player.letters[(int)letter].content);
                            }
                        }
                        else
                        {
                            string response = "";
                            int count = 1;
                            foreach (Player.Letter letter in player.letters)
                            {
                                response += $"Letter #{count} to {program.client.GetUser(letter.recipientID).Username}:\n`{letter.content.Substring(0, Math.Min(letter.content.Length, 130))}...`\n";
                                count++;
                            }

                            await command.RespondAsync(response);
                        }
                    }

                    break;
                case "add":

                    if(player.letters.Count >= player.letterLimit) {
                        await command.RespondAsync($"You've reached the allowed letter limit of {player.letterLimit}! You may not add any more letters.");
                        return;
                    }

                    SocketGuildUser? p = options.ElementAt(0).Value as SocketGuildUser;
                    Player? recipient = guild.Players.Find(x => x != null && x.IsPlayer(p.Id));

                    if (recipient == null) {
                        await command.RespondAsync("Recipient must be a valid player in this game");
                        return;
                    }

                    var mb = new ModalBuilder()
                        .WithTitle("Add Letter")
                        .WithCustomId("add_letter|" + recipient.PlayerID)
                        .AddTextInput("Letter content", "content", TextInputStyle.Paragraph, maxLength: 2000);

                    await command.RespondWithModalAsync(mb.Build());
                   break;

                case "remove":
                    if (player.letters?.Count < 1)
                    {
                        await command.RespondAsync("You have no letters!");
                    }
                    else 
                    {
                        long letter = (long)options.First().Value - 1;
                        if (letter < 0 || player.letters.Count < letter)
                        {
                            await command.RespondAsync("Invalid letter number.\nValid letter numbers: " + new Range(0, player.letters.Count - 1));
                        }
                        else
                        {
                            player.letters.RemoveAt((int)letter);
                            await command.RespondAsync("Removed letter #" + letter+1);
                        }
                    }
                    break;
                case "edit":
                    if (player.letters?.Count < 1)
                    {
                        await command.RespondAsync("You have no letters!");
                    }
                    else
                    {
                        long letter = (long)options.First().Value - 1;
                        SocketGuildUser rec = (SocketGuildUser)options.ElementAt(1).Value;
                        Player? to = guild.Players.Find(x => x != null && x.IsPlayer(rec.Id));

                        if (to == null) {
                            await command.RespondAsync("Recipient must be a valid player in this game");
                            return;
                        }


                        if (letter < 0 || player.letters.Count < letter)
                        {
                            await command.RespondAsync("Invalid letter number.\nValid letter numbers: " + new Range(0, player.letters.Count - 1));
                        }
                        else
                        {
                            var modalb = new ModalBuilder()
                            .WithTitle("Edit Letter")
                            .WithCustomId("edit_letter|" + rec.Id + "|" + letter)
                            .AddTextInput("Letter content", "content", TextInputStyle.Paragraph, maxLength: 2000);

                            await command.RespondWithModalAsync(modalb.Build());
                        }
                    }

                    break;
                default:
                    await command.RespondAsync("You cannot use this command.");
                    break;
            }
            guild.Save();
        }

        public static async Task ModalSubmitted(SocketModal modal)
        {
            if (!modal.Data.CustomId.StartsWith("add_letter") && !modal.Data.CustomId.StartsWith("edit_letter")) return;

            var user = modal.User;
            var channel = modal.Channel;

            if (!Program.instance.guilds.TryGetValue((ulong)modal.GuildId, out Guild guild))
            {
                await modal.RespondAsync($"You must use setup before being able to use this command!");
                return;
            }

            Player? player = guild.Players.Find(player => player != null && player.IsPlayer(user.Id));
            if(player == null) {
                await modal.RespondAsync("Cannot find player with ID " + user.Id);
                return;
            }
            if (!modal.Data.CustomId.StartsWith("host") && (player == null || player.ChannelID != channel.Id))
            {
                await modal.RespondAsync("This command can only be used by a player in their channel!");
                return;
            }

            // Get the values of components.
            List<SocketMessageComponentData> components = modal.Data.Components.ToList();
            string content = components.First(x => x.CustomId == "content").Value;

            if (modal.Data.CustomId.StartsWith("add_letter"))
            {
                ulong recipientId = Convert.ToUInt64(modal.Data.CustomId.Split("|")[1]);


                SocketGuild g = Program.instance.client.Guilds.First(guild => guild.Id == modal.GuildId);

                SocketUser? recipient = g?.GetUser(recipientId);

                if (recipient == null ) {
                    await modal.RespondAsync("Recipient must be a valid user in this guild");
                    return;
                }

                player.letters.Add(new Player.Letter(recipientId, content));
                string header = $"Letter added! Letter #{player.letters.Count} to {Program.instance.client.GetUser(recipientId)?.Username ?? $"<@{recipientId}>"}:"; 
                await modal.RespondAsync($"{header}\n`{content.Substring(0, Math.Min(content.Length, 2000-header.Length - 3))}`");
            } else if(modal.Data.CustomId.StartsWith("edit_letter"))
            {

                ulong recipientId = Convert.ToUInt64(modal.Data.CustomId.Split("|")[1]);
                long letter = Convert.ToInt64(modal.Data.CustomId.Split("|")[2]);

                Player.Letter l = player.letters[(int)letter];
                l.recipientID = recipientId;
                l.content = content;
                player.letters.RemoveAt((int)letter);
                player.letters.Insert((int)letter, l);
                string header = $"Letter changed! Letter #{letter + 1} to {Program.instance.client.GetUser(recipientId)?.Username ?? $"<@{recipientId}>"}:";
                await modal.RespondAsync($"{header}\n`{content.Substring(0, Math.Min(content.Length, 2000 - header.Length - 3))}`");
            }
            guild.Save();
        }
    }
}