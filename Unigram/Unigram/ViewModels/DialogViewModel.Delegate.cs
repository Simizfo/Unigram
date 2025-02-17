﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Telegram.Td.Api;
using Unigram.Common;
using Unigram.Controls;
using Unigram.Controls.Views;
using Unigram.ViewModels.Dialogs;
using Unigram.Views;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Unigram.Services;
using Unigram.ViewModels.Delegates;
using Unigram.ViewModels.Chats;
using Unigram.ViewModels.Gallery;
using Unigram.Services.Settings;
using Unigram.Controls.Gallery;

namespace Unigram.ViewModels
{
    public partial class DialogViewModel : IMessageDelegate
    {
        private FileContext<MessageViewModel> _filesMap = new FileContext<MessageViewModel>();
        private FileContext<MessageViewModel> _photosMap = new FileContext<MessageViewModel>();

        public bool CanBeDownloaded(MessageViewModel message)
        {
            var content = message.Content as object;
            if (content is MessageAnimation animationMessage)
            {
                content = animationMessage.Animation;
            }
            else if (content is MessageAudio audioMessage)
            {
                content = audioMessage.Audio;
            }
            else if (content is MessageDocument documentMessage)
            {
                content = documentMessage.Document;
            }
            else if (content is MessageGame gameMessage)
            {
                if (gameMessage.Game.Animation != null)
                {
                    content = gameMessage.Game.Animation;
                }
                else if (gameMessage.Game.Photo != null)
                {
                    content = gameMessage.Game.Photo;
                }
            }
            else if (content is MessageInvoice invoiceMessage)
            {
                content = invoiceMessage.Photo;
            }
            else if (content is MessageLocation locationMessage)
            {
                content = locationMessage.Location;
            }
            else if (content is MessagePhoto photoMessage)
            {
                content = photoMessage.Photo;
            }
            else if (content is MessageSticker stickerMessage)
            {
                content = stickerMessage.Sticker;
            }
            else if (content is MessageText textMessage)
            {
                if (textMessage?.WebPage?.Animation != null)
                {
                    content = textMessage?.WebPage?.Animation;
                }
                else if (textMessage?.WebPage?.Document != null)
                {
                    content = textMessage?.WebPage?.Document;
                }
                else if (textMessage?.WebPage?.Sticker != null)
                {
                    content = textMessage?.WebPage?.Sticker;
                }
                else if (textMessage?.WebPage?.Video != null)
                {
                    content = textMessage?.WebPage?.Video;
                }
                else if (textMessage?.WebPage?.VideoNote != null)
                {
                    content = textMessage?.WebPage?.VideoNote;
                }
                // PHOTO SHOULD ALWAYS BE AT THE END!
                else if (textMessage?.WebPage?.Photo != null)
                {
                    content = textMessage?.WebPage?.Photo;
                }
            }
            else if (content is MessageVideo videoMessage)
            {
                content = videoMessage.Video;
            }
            else if (content is MessageVideoNote videoNoteMessage)
            {
                content = videoNoteMessage.VideoNote;
            }
            else if (content is MessageVoiceNote voiceNoteMessage)
            {
                content = voiceNoteMessage.VoiceNote;
            }

            var chat = _chat;
            if (chat == null)
            {
                return false;
            }

            if (content is Animation animation)
            {
                return Settings.AutoDownload.ShouldDownloadDocument(GetChatType(chat), animation.AnimationValue.Size);
            }
            else if (content is Audio audio)
            {
                return Settings.AutoDownload.ShouldDownloadDocument(GetChatType(chat), audio.AudioValue.Size);
            }
            else if (content is Document document)
            {
                return Settings.AutoDownload.ShouldDownloadDocument(GetChatType(chat), document.DocumentValue.Size);
            }
            else if (content is Photo photo)
            {
                return Settings.AutoDownload.ShouldDownloadPhoto(GetChatType(chat));
            }
            else if (content is Sticker sticker)
            {
                // Stickers aren't part of the deal
            }
            else if (content is Video video)
            {
                return Settings.AutoDownload.ShouldDownloadVideo(GetChatType(chat), video.VideoValue.Size);
            }
            else if (content is VideoNote videoNote)
            {
                return Settings.AutoDownload.ShouldDownloadDocument(GetChatType(chat), videoNote.Video.Size);
            }
            else if (content is VoiceNote voiceNote)
            {
                return Settings.AutoDownload.ShouldDownloadDocument(GetChatType(chat), voiceNote.Voice.Size);
            }

            return false;
        }

        private AutoDownloadChat GetChatType(Chat chat)
        {
            if (chat.Type is ChatTypeSupergroup supergroup && !supergroup.IsChannel || chat.Type is ChatTypeBasicGroup)
            {
                return AutoDownloadChat.Group;
            }
            else if (chat.Type is ChatTypePrivate || chat.Type is ChatTypeSecret)
            {
                var user = ProtoService.GetUser(chat);
                if (user == null)
                {
                    return AutoDownloadChat.OtherPrivateChat;
                }
                else if (user.OutgoingLink is LinkStateIsContact)
                {
                    return AutoDownloadChat.Contact;
                }
            }

            return AutoDownloadChat.Channel;
        }

        public void DownloadFile(MessageViewModel message, File file)
        {
            ProtoService.DownloadFile(file.Id, 1);
        }

        public bool TryGetMessagesForFileId(int fileId, out IList<MessageViewModel> items)
        {
            if (_filesMap.TryGetValue(fileId, out List<MessageViewModel> messages))
            {
                items = messages;
                return true;
            }

            items = null;
            return false;
        }

        public bool TryGetMessagesForPhotoId(int fileId, out IList<MessageViewModel> items)
        {
            if (_photosMap.TryGetValue(fileId, out List<MessageViewModel> messages))
            {
                items = messages;
                return true;
            }

            items = null;
            return false;
        }



        public void ReplyToMessage(MessageViewModel message)
        {
            MessageReplyCommand.Execute(message);
        }

        public async void OpenReply(MessageViewModel message)
        {
            await LoadMessageSliceAsync(message.Id, message.ReplyToMessageId);
        }



        public async void OpenFile(File file)
        {
            if (file.Local.IsDownloadingCompleted)
            {
                if (file.Local.Path.EndsWith(".unigram-theme"))
                {
                    await new ThemePreviewView(file.Local.Path).ShowQueuedAsync();
                    return;
                }

                try
                {
                    var temp = await StorageFile.GetFileFromPathAsync(file.Local.Path);
                    var result = await Windows.System.Launcher.LaunchFileAsync(temp);
                    //var folder = await temp.GetParentAsync();
                    //var options = new Windows.System.FolderLauncherOptions();
                    //options.ItemsToSelect.Add(temp);

                    //var result = await Windows.System.Launcher.LaunchFolderAsync(folder, options);
                }
                catch { }
            }
        }

        public void OpenWebPage(WebPage webPage)
        {
            if (webPage.InstantViewVersion != 0)
            {
                //if (NavigationService is UnigramNavigationService asdas)
                //{
                //    asdas.NavigateToInstant(webPage.Url);
                //    return;
                //}

                NavigationService.Navigate(typeof(InstantPage), webPage.Url);
            }
            else if (MessageHelper.TryCreateUri(webPage.Url, out Uri uri) &&
                    (string.Equals(webPage.Type, "telegram_megagroup", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(webPage.Type, "telegram_channel", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(webPage.Type, "telegram_message", StringComparison.OrdinalIgnoreCase)))
            {
                MessageHelper.OpenTelegramUrl(ProtoService, NavigationService, uri);
            }
        }

        public async void OpenSticker(Sticker sticker)
        {
            if (sticker.SetId != 0)
            {
                await StickerSetView.GetForCurrentView().ShowAsync(sticker.SetId, Sticker_Click);
            }
        }

        public async void OpenLocation(Location location, string title)
        {
            if (title != null)
            {
                await Windows.System.Launcher.LaunchUriAsync(new Uri(string.Format(CultureInfo.InvariantCulture, "bingmaps:?collection=point.{0}_{1}_{2}", location.Latitude, location.Longitude, WebUtility.UrlEncode(title))));
            }
            else
            {
                await Windows.System.Launcher.LaunchUriAsync(new Uri(string.Format(CultureInfo.InvariantCulture, "bingmaps:?collection=point.{0}_{1}", location.Latitude, location.Longitude)));
            }
        }

        public void OpenLiveLocation(MessageViewModel message)
        {
            NavigationService.Navigate(typeof(LiveLocationPage), message.ChatId);
        }

        public void OpenInlineButton(MessageViewModel message, InlineKeyboardButton button)
        {
            KeyboardButtonExecute(message, button);
        }

        public void Call(MessageViewModel message)
        {
            CallCommand.Execute();
        }

        public void VotePoll(MessageViewModel message, PollOption option)
        {
            var poll = message.Content as MessagePoll;
            if (poll == null)
            {
                return;
            }

            ProtoService.Send(new SetPollAnswer(message.ChatId, message.Id, new int[] { poll.Poll.Options.IndexOf(option) }));
        }


        public async void OpenUsername(string username)
        {
            var response = await ProtoService.SendAsync(new SearchPublicChat(username));
            if (response is Chat chat)
            {
                if (chat.Type is ChatTypePrivate privata)
                {
                    var user = ProtoService.GetUser(privata.UserId);
                    if (user?.Type is UserTypeBot)
                    {
                        NavigationService.NavigateToChat(chat);
                    }
                    else
                    {
                        NavigationService.Navigate(typeof(ProfilePage), chat.Id);
                    }
                }
                else
                {
                    NavigationService.NavigateToChat(chat);
                }
            }
            else
            {
                await TLMessageDialog.ShowAsync(Strings.Resources.NoUsernameFound, Strings.Resources.AppName, Strings.Resources.OK);
            }
        }

        public async void OpenUser(int userId)
        {
            var response = await ProtoService.SendAsync(new CreatePrivateChat(userId, false));
            if (response is Chat chat)
            {
                var user = ProtoService.GetUser(userId);
                if (user?.Type is UserTypeBot)
                {
                    NavigationService.NavigateToChat(chat);
                }
                else
                {
                    NavigationService.Navigate(typeof(ProfilePage), chat.Id);
                }
            }
        }

        public void OpenViaBot(int viaBotUserId)
        {
            var chat = Chat;
            if (chat != null && chat.Type is ChatTypeSupergroup super && super.IsChannel)
            {
                var supergroup = ProtoService.GetSupergroup(super.SupergroupId);
                if (supergroup != null && !supergroup.CanPostMessages())
                {
                    return;
                }
            }

            var user = ProtoService.GetUser(viaBotUserId);
            if (user != null)
            {
                SetText($"@{user.Username} ");
                ResolveInlineBot(user.Username);
            }
        }

        public void OpenChat(long chatId)
        {
            var chat = ProtoService.GetChat(chatId);
            if (chat == null)
            {
                return;
            }

            NavigationService.NavigateToChat(chat);
        }

        public void OpenChat(long chatId, long messageId)
        {
            var chat = ProtoService.GetChat(chatId);
            if (chat == null)
            {
                return;
            }

            NavigationService.NavigateToChat(chat, message: messageId);
        }

        public void OpenHashtag(string hashtag)
        {
            var search = Search = new ChatSearchViewModel(ProtoService, CacheService, Settings, Aggregator, this);
            search.Search(hashtag, null, null);
        }

        public async void OpenUrl(string url, bool untrust)
        {
            if (MessageHelper.TryCreateUri(url, out Uri uri))
            {
                if (MessageHelper.IsTelegramUrl(uri))
                {
                    MessageHelper.OpenTelegramUrl(ProtoService, NavigationService, uri);
                }
                else
                {
                    //if (message?.Media is TLMessageMediaWebPage webpageMedia)
                    //{
                    //    if (webpageMedia.WebPage is TLWebPage webpage && webpage.HasCachedPage && webpage.Url.Equals(navigation))
                    //    {
                    //        var service = WindowWrapper.Current().NavigationServices.GetByFrameId("Main");
                    //        if (service != null)
                    //        {
                    //            service.Navigate(typeof(InstantPage), webpageMedia);
                    //            return;
                    //        }
                    //    }
                    //}

                    if (untrust)
                    {
                        var confirm = await TLMessageDialog.ShowAsync(string.Format(Strings.Resources.OpenUrlAlert, url), Strings.Resources.AppName, Strings.Resources.OK, Strings.Resources.Cancel);
                        if (confirm != ContentDialogResult.Primary)
                        {
                            return;
                        }
                    }

                    try
                    {
                        await Windows.System.Launcher.LaunchUriAsync(uri);
                    }
                    catch { }
                }
            }
        }

        public async void OpenMedia(MessageViewModel message, FrameworkElement target)
        {
            GalleryViewModelBase viewModel = null;

            var webPage = message.Content is MessageText text ? text.WebPage : null;
            if (webPage != null && webPage.IsInstantGallery())
            {
                viewModel = await InstantGalleryViewModel.CreateAsync(ProtoService, Aggregator, message, webPage);

                if (viewModel.Items.IsEmpty())
                {
                    viewModel = null;
                }
            }

            if (viewModel == null && (message.Content is MessageVideoNote || (webPage != null && webPage.VideoNote != null) || message.Content is MessageAnimation || (webPage != null && webPage.Animation != null) || (message.Content is MessageGame game && game.Game.Animation != null)))
            {
                Delegate?.PlayMessage(message, target);
            }
            else
            {
                if (viewModel == null)
                {
                    if ((message.Content is MessagePhoto || message.Content is MessageVideo) && !message.IsSecret())
                    {
                        viewModel = new ChatGalleryViewModel(ProtoService, Aggregator, message.ChatId, message.Get());
                    }
                    else
                    {
                        viewModel = new SingleGalleryViewModel(ProtoService, Aggregator, new GalleryMessage(ProtoService, message.Get()));
                    }
                }

                await GalleryView.GetForCurrentView().ShowAsync(viewModel, () => target);
            }

            TextField?.Focus(FocusState.Programmatic);
        }

        public void PlayMessage(MessageViewModel message)
        {
            if ((message.Content is MessageVideoNote videoNote && !videoNote.IsViewed && !message.IsOutgoing) || (message.Content is MessageVoiceNote voiceNote && !voiceNote.IsListened && !message.IsOutgoing))
            {
                ProtoService.Send(new OpenMessageContent(message.ChatId, message.Id));
            }

            _playbackService.Enqueue(message.Get());
        }



        public async void SendBotCommand(string command)
        {
            await SendMessageAsync(command);
        }



        public bool IsAdmin(int userId)
        {
            var chat = _chat;
            if (chat == null)
            {
                return false;
            }

            return _admins.TryGetValue(chat.Id, out IList<int> value) && value.Contains(userId);
        }
    }
}
