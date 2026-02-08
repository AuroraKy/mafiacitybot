using Discord.WebSocket;
using mafiacitybot.GuildCommands;

namespace mafiacitybot;

/**
 * 
 * 
[x] make sure letters can only be set to valid recipients (players)
[x] a command to send  out letters automatically
[x] some way for hosts to add letters (assassin)
[x] Lock actions/letters using command
[x] Repost full letter on letter add

test it lol

*/
public class SlashCommandHandler
{
    public Program Program { get; set; }

    public SlashCommandHandler(Program program)
    {
        Program = program;
    }

    public async Task SlCommandHandler(SocketSlashCommand command)
    {
        if (!command.IsDMInteraction)
        {
            Action<Task> OnFault = task =>
            {
                if (task.Exception?.InnerException is { } inner)
                {
                    Console.WriteLine("{0}: {1}\nAt {2}\n{3}",
                        inner.GetType().Name,
                        inner.Message,
                        inner.TargetSite,
                        inner.StackTrace);
                }
            };
            switch (command.Data.Name)
            {
                case "ping":
                    _ = Ping.HandleCommand(command, Program).ContinueWith(OnFault, TaskContinuationOptions.OnlyOnFaulted);
                    break;
                case "phase":
                    _ = Phase.HandleCommand(command, Program).ContinueWith(OnFault, TaskContinuationOptions.OnlyOnFaulted);
                    break;
                case "setup":
                    _ = Setup.HandleCommand(command, Program).ContinueWith(OnFault, TaskContinuationOptions.OnlyOnFaulted);
                    break;
                case "endgame":
                    _ = EndGame.HandleCommand(command).ContinueWith(OnFault, TaskContinuationOptions.OnlyOnFaulted);
                    break;
                case "register":
                    _ = Register.HandleCommand(command, Program).ContinueWith(OnFault, TaskContinuationOptions.OnlyOnFaulted);
                    break;
                case "letter":
                    _ = Letter.HandleCommand(command, Program).ContinueWith(OnFault, TaskContinuationOptions.OnlyOnFaulted);
                    break;
                case "host_letter":
                    _ = HostLetter.HandleCommand(command, Program).ContinueWith(OnFault, TaskContinuationOptions.OnlyOnFaulted);
                    break;
                case "action":
                    _ = Actions.HandleCommand(command, Program).ContinueWith(OnFault, TaskContinuationOptions.OnlyOnFaulted);
                    break;
                case "clear_players":
                    _ = ClearPlayers.HandleCommand(command, Program).ContinueWith(OnFault, TaskContinuationOptions.OnlyOnFaulted);   
                    break;
                case "clear_player":
                    _ = ClearPlayer.HandleCommand(command, Program).ContinueWith(OnFault, TaskContinuationOptions.OnlyOnFaulted);
                    break;
                case "info":
                    _ = Info.HandleCommand(command, Program).ContinueWith(OnFault, TaskContinuationOptions.OnlyOnFaulted);
                    break;
                case "enforce_roles":
                    _ = EnforceRoles.HandleCommand(command, Program).ContinueWith(OnFault, TaskContinuationOptions.OnlyOnFaulted);
                    break;
                case "register_commands":
                    _ = RegisterCommands.HandleCommand(command, Program).ContinueWith(OnFault, TaskContinuationOptions.OnlyOnFaulted);
                    break;
                case "view_actions":
                    _ = ViewActions.HandleCommand(command, Program).ContinueWith(OnFault, TaskContinuationOptions.OnlyOnFaulted);
                    break;
                case "view_action":
                    _ = ViewAction.HandleCommand(command, Program).ContinueWith(OnFault, TaskContinuationOptions.OnlyOnFaulted);
                    break;
                case "help":
                    _ = Help.HandleCommand(command, Program).ContinueWith(OnFault, TaskContinuationOptions.OnlyOnFaulted);
                    break;
                case "lock":
                    _ = Lock.HandleCommand(command, Program).ContinueWith(OnFault, TaskContinuationOptions.OnlyOnFaulted);
                    break;
                case "croak":
                    _ = Vote.HandleCommand(command, Program).ContinueWith(OnFault, TaskContinuationOptions.OnlyOnFaulted);
                    break;
                case "host_anonchat":
                    _ = AnonChat.HandleCommand(command, Program).ContinueWith(OnFault, TaskContinuationOptions.OnlyOnFaulted);
                    break;
                case "forward":
                    _ = Forward.HandleCommand(command, Program).ContinueWith(OnFault, TaskContinuationOptions.OnlyOnFaulted);
                    break;
                case "host_masquerade":
                    _ = Masquerade.HandleCommand(command, Program).ContinueWith(OnFault, TaskContinuationOptions.OnlyOnFaulted);
                    break;
                case "mask":
                    _ = Mask.HandleCommand(command, Program).ContinueWith(OnFault, TaskContinuationOptions.OnlyOnFaulted);
                    break;
                case "confirm_prefix":
                    _ = ConfirmPrefix.HandleCommand(command, Program).ContinueWith(OnFault, TaskContinuationOptions.OnlyOnFaulted);
                    break;
                case "whisper":
                    _ = Whisper.HandleCommand(command, Program).ContinueWith(OnFault, TaskContinuationOptions.OnlyOnFaulted);
                    break;
                case "host_whisper":
                    _ = SetWhispers.HandleCommand(command, Program).ContinueWith(OnFault, TaskContinuationOptions.OnlyOnFaulted);
                    break;
                case "host_contest":
                    _ = Contest.HandleCommand(command, Program).ContinueWith(OnFault, TaskContinuationOptions.OnlyOnFaulted);
                    break;
                case "forward_contest":
                    _ = ForwardContest.HandleCommand(command, Program).ContinueWith(OnFault, TaskContinuationOptions.OnlyOnFaulted);
                    break;
            }
        }
        else
        {
            await command.RespondAsync("Sorry, but you need to run this command inside a server!");
        }
        
    }
}
