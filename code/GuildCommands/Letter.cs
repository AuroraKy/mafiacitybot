using Discord;
using Discord.Net;
using Discord.WebSocket;

namespace mafiacitybot.GuildCommands
{
    public static class Letter
    {
        public static async Task CreateCommand(SocketGuild guild)
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
                .AddOption("content", ApplicationCommandOptionType.String, "The contents of the letter. (<2000 chars)", isRequired: true)
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
                .AddOption("content", ApplicationCommandOptionType.String, "The contents of the letter. (<2000 chars)", isRequired: true)
            );

            try
            {
                await guild.CreateApplicationCommandAsync(command.Build());
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

            Player? player = guild.Players.Find(player => player.PlayerID == user.Id);
            if (player == null || player.ChannelID != channel.Id)
            {
                await command.RespondAsync("This command can only be used by a player in their channel!");
                return;
            }

            var fieldName = command.Data.Options.First().Name;
            var options = command.Data.Options.First().Options;


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
                            Int64 letter = (Int64)options.First().Value - 1;
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

                    SocketGuildUser recipient = options.ElementAt(0).Value as SocketGuildUser;
                    string content = (string)options.ElementAt(1).Value;

                    if(content.Length > 2000)
                    {
                        await command.RespondAsync($"Letters can be at most 2000 characters long! The content you send was {content.Length} letters!");
                        return;
                    }

                    player.letters.Add(new Player.Letter(recipient.Id, content));
                    await command.RespondAsync($"Letter added! Letter #{player.letters.Count} to {recipient.Username}: `{content.Substring(0, Math.Min(content.Length, 130))}...`");
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
                        string letterContent = (string)options.ElementAt(2).Value;
                        if (letter < 0 || player.letters.Count < letter)
                        {
                            await command.RespondAsync("Invalid letter number.\nValid letter numbers: " + new Range(0, player.letters.Count - 1));
                        }
                        else
                        {
                            Player.Letter l = player.letters[(int)letter];
                            l.recipientID = rec.Id;
                            l.content = letterContent;
                            player.letters.RemoveAt((int)letter);
                            player.letters.Insert((int)letter, l);
                            await command.RespondAsync($"Letter changed! Letter #{letter+1} to {rec.Username}: `{letterContent.Substring(0, Math.Min(letterContent.Length, 130))}...`");
                        }
                    }

                    break;

            }
            guild.Save();
        }
    }
}