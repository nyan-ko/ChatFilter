using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Microsoft.Xna.Framework;
using TerrariaApi.Server;
using TShockAPI;
using ProfanityFilter;
using TShockAPI.Hooks;

namespace ChatFilter
{
    [ApiVersion(2, 1)]
    public class ChatFilter : TerrariaPlugin
    {
        #region Plugin info
        public override string Name => "ChatFilter";

        public override Version Version => new Version(1, 0);

        public override string Author => "nyan-ko";
        #endregion

        private ProfanityFilter.ProfanityFilter _filter;
        private ProfanityDictionary _dict;

        public ChatFilter(Main game) : base(game)
        {

        }

        public override void Initialize()
        {
            PlayerHooks.PlayerChat += OnChat;

            Reload();

            Commands.ChatCommands.Add(new Command("chatfilter.admin", Profanity, "chatfilter", "cf"));
            Commands.ChatCommands.Add(new Command("chatfilter.admin", ToggleRegisteredUserCheck, "cfcheck"));
            Commands.ChatCommands.Add(new Command("chatfilter.admin", ReloadConfig, "cfreload"));
        }

        private void OnChat(PlayerChatEventArgs args)
        {
            var plr = args.Player;

            if ((plr.Group?.Name ?? "") != "1" && !_dict.CheckRegistered)
            {
                return;
            }

            // changing args.RawText won't do anything as the formatted text is already made before this method is called(?)
            // either way, we have to find the message sent by the player, then add a filtered version of that onto the rest of the message.
            int raw = args.TShockFormattedText.LastIndexOf(args.RawText);
            args.TShockFormattedText = args.TShockFormattedText.Substring(0, raw) + _filter.CensorString(args.RawText);
        }

        private void Profanity(CommandArgs args)
        {
            var sub = args.Parameters.Count == 0 ? "help" : args.Parameters[0];
            switch (sub)
            {
                default:
                case "help":
                {
                    var help = new List<string>()
                    {
                        "Command for managing the chat filter.",
                        "/cf add - Adds a word to the profanity list.",
                        "/cf del - Deletes a word from the profanity list.",
                        "/cf list - Lists the contents of the profanity list.",
                        "/cfcheck - Toggles filtering messages from registered users."
                    };

                    args.Player.SendInfoMessage(string.Join("\n", help));
                }
                    break;
                case "add":
                {
                    if (args.Parameters.Count == 1)
                    {
                        args.Player.SendErrorMessage("Expected a word to add.");
                        return;
                    }

                    string badWord = string.Join(" ", args.Parameters.Skip(1));

                    bool added = AddNewProfanity(badWord);

                    if (added)
                    {
                        args.Player.SendSuccessMessage($"Successfully added {badWord} as profanity.");
                    }
                    else
                    {
                        args.Player.SendErrorMessage("Profanity list already contains this word.");
                    }
                }
                    break;
                case "delete":
                case "del":
                {
                    if (args.Parameters.Count == 1)
                    {
                        args.Player.SendErrorMessage("Expected a word to remove.");
                        return;
                    }

                    string badWord = string.Join(" ", args.Parameters.Skip(1));

                    bool success = _dict.BannedWords.Remove(badWord) | _filter.RemoveProfanity(badWord);
                    _dict.Write();

                    if (success)
                    {
                        args.Player.SendSuccessMessage($"Successfully removed {badWord} as profanity.");
                    }
                    else
                    {
                        args.Player.SendSuccessMessage($"Could not find {badWord} to remove.");
                    }
                }
                    break;
                case "list":
                {
                    if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out int pg))
                    {
                        return;
                    }

                    PaginationTools.SendPage(args.Player, pg, _dict.BannedWords, _dict.BannedWords.Count,
                       new PaginationTools.Settings
                       {
                           HeaderFormat = "Profanity List ({0}/{1})",
                           FooterFormat = "Type /cf list {0} for more.",
                           NothingToDisplayString = "There are currently no bad words."
                       });
                    break;
                }
            }

        }

        private void ToggleRegisteredUserCheck(CommandArgs args)
        {
            _dict.CheckRegistered = !_dict.CheckRegistered;
            args.Player.SendSuccessMessage($"{(_dict.CheckRegistered ? "En" : "Dis")}abled checking registered users for profanity.");
        }

        private void ReloadConfig(CommandArgs args)
        {
            Reload();

            args.Player.SendSuccessMessage("Successfully reloaded profanity filter config.");
        }

        private void Reload()
        {
            _dict = ProfanityDictionary.Read();
            _filter = new ProfanityFilter.ProfanityFilter(_dict.BannedWords.ToArray());
        }

        private bool AddNewProfanity(string badWord)
        {
            bool added = _dict.BannedWords.Add(badWord);
            _filter.AddProfanity(badWord);
            _dict.Write();

            return added;
        }
    }
}
