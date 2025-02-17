﻿using System;
using Telegram.Td.Api;
using Unigram.Common;
using Unigram.Services;
using Unigram.ViewModels.Delegates;

namespace Unigram.ViewModels
{
    public class MessageViewModel
    {
        private readonly IProtoService _protoService;
        private readonly IPlaybackService _playbackService;
        private readonly IMessageDelegate _delegate;

        private Message _message;

        public MessageViewModel(IProtoService protoService, IPlaybackService playbackService, IMessageDelegate delegato, Message message)
        {
            _protoService = protoService;
            _playbackService = playbackService;
            _delegate = delegato;

            _message = message;
        }

        public IProtoService ProtoService => _protoService;
        public IPlaybackService PlaybackService => _playbackService;
        public IMessageDelegate Delegate => _delegate;

        public bool IsFirst { get; set; }
        public bool IsLast { get; set; }

        public DateTime ContentOpenedAt { get; set; }

        public ReplyMarkup ReplyMarkup { get => _message.ReplyMarkup; set => _message.ReplyMarkup = value; }
        public MessageContent Content { get => _message.Content; set => _message.Content = value; }
        public long MediaAlbumId => _message.MediaAlbumId;
        public int Views { get => _message.Views; set => _message.Views = value; }
        public string AuthorSignature => _message.AuthorSignature;
        public int ViaBotUserId => _message.ViaBotUserId;
        public double TtlExpiresIn { get => _message.TtlExpiresIn; set => _message.TtlExpiresIn = value; }
        public int Ttl => _message.Ttl;
        public long ReplyToMessageId => _message.ReplyToMessageId;
        public MessageForwardInfo ForwardInfo => _message.ForwardInfo;
        public int EditDate { get => _message.EditDate; set => _message.EditDate = value; }
        public int Date => _message.Date;
        public bool ContainsUnreadMention { get => _message.ContainsUnreadMention; set => _message.ContainsUnreadMention = value; }
        public bool IsChannelPost => _message.IsChannelPost;
        public bool CanBeDeletedForAllUsers => _message.CanBeDeletedForAllUsers;
        public bool CanBeDeletedOnlyForSelf => _message.CanBeDeletedOnlyForSelf;
        public bool CanBeForwarded => _message.CanBeForwarded;
        public bool CanBeEdited => _message.CanBeEdited;
        public bool IsOutgoing { get => _message.IsOutgoing; set => _message.IsOutgoing = value; }
        public MessageSendingState SendingState => _message.SendingState;
        public long ChatId => _message.ChatId;
        public int SenderUserId => _message.SenderUserId;
        public long Id => _message.Id;

        public Photo GetPhoto() => _message.GetPhoto();
        public File GetAnimation() => _message.GetAnimation();
        public File GetAnimatedSticker() => _message.GetAnimatedSticker();
        public File GetFile() => _message.GetFile();

        public bool IsService() => _message.IsService();
        public bool IsSaved() => _message.IsSaved(_protoService.Options.MyId);
        public bool IsSecret() => _message.IsSecret();

        public MessageViewModel ReplyToMessage { get; set; }
        public ReplyToMessageState ReplyToMessageState { get; set; } = ReplyToMessageState.None;

        public User GetSenderUser()
        {
            return ProtoService.GetUser(_message.SenderUserId);
        }

        public User GetViaBotUser()
        {
            if (_message.ViaBotUserId != 0)
            {
                return ProtoService.GetUser(_message.ViaBotUserId);
            }

            var user = ProtoService.GetUser(_message.SenderUserId);
            if (user?.Type is UserTypeBot)
            {
                return user;
            }

            return null;
        }

        public Chat GetChat()
        {
            return ProtoService.GetChat(_message.ChatId);
        }

        public Message Get()
        {
            return _message;
        }

        public void Replace(Message message)
        {
            _message = message;
        }

        public bool UpdateFile(File file)
        {
            var message = _message.UpdateFile(file);

            var reply = ReplyToMessage;
            if (reply != null)
            {
                return reply.UpdateFile(file) || message;
            }

            return message;
        }

        public bool IsShareable()
        {
            var message = this;
            //if (currentPosition != null && !currentPosition.last)
            //{
            //    return false;
            //}
            //else if (messageObject.eventId != 0)
            //{
            //    return false;
            //}
            //else if (messageObject.messageOwner.fwd_from != null && !messageObject.isOutOwner() && messageObject.messageOwner.fwd_from.saved_from_peer != null && messageObject.getDialogId() == UserConfig.getInstance(currentAccount).getClientUserId())
            //{
            //    drwaShareGoIcon = true;
            //    return true;
            //}
            //else 
            if (message.Content is MessageSticker)
            {
                return false;
            }
            //else if (messageObject.messageOwner.fwd_from != null && messageObject.messageOwner.fwd_from.channel_id != 0 && !messageObject.isOutOwner())
            //{
            //    return true;
            //}
            else if (message.SenderUserId != 0)
            {
                if (message.Content is MessageText)
                {
                    return false;
                }

                var user = message.GetSenderUser();
                if (user != null && user.Type is UserTypeBot)
                {
                    return true;
                }
                if (!message.IsOutgoing)
                {
                    if (message.Content is MessageGame || message.Content is MessageInvoice)
                    {
                        return true;
                    }

                    var chat = message.ProtoService.GetChat(message.ChatId);
                    if (chat != null && chat.Type is ChatTypeSupergroup super && !super.IsChannel)
                    {
                        var supergroup = message.ProtoService.GetSupergroup(super.SupergroupId);
                        return supergroup != null && supergroup.Username.Length > 0 && !(message.Content is MessageContact) && !(message.Content is MessageLocation);
                    }
                }
            }
            //else if (messageObject.messageOwner.from_id < 0 || messageObject.messageOwner.post)
            //{
            //    if (messageObject.messageOwner.to_id.channel_id != 0 && (messageObject.messageOwner.via_bot_id == 0 && messageObject.messageOwner.reply_to_msg_id == 0 || messageObject.type != 13))
            //    {
            //        return true;
            //    }
            //}
            else if (message.IsChannelPost)
            {
                if (message.ViaBotUserId == 0 && message.ReplyToMessageId == 0 || !(message.Content is MessageSticker))
                {
                    return true;
                }
            }

            return false;
        }

        public override bool Equals(object obj)
        {
            if (obj is Message y)
            {
                return Id == y.Id && ChatId == y.ChatId;
            }

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode() ^ ChatId.GetHashCode();
        }

        public void UpdateWith(MessageViewModel message)
        {
            _message.AuthorSignature = message.AuthorSignature;
            _message.CanBeDeletedForAllUsers = message.CanBeDeletedForAllUsers;
            _message.CanBeDeletedOnlyForSelf = message.CanBeDeletedOnlyForSelf;
            _message.CanBeEdited = message.CanBeEdited;
            _message.CanBeForwarded = message.CanBeForwarded;
            _message.ChatId = message.ChatId;
            _message.ContainsUnreadMention = message.ContainsUnreadMention;
            //_message.Content = message.Content;
            //_message.Date = message.Date;
            _message.EditDate = message.EditDate;
            _message.ForwardInfo = message.ForwardInfo;
            _message.Id = message.Id;
            _message.IsChannelPost = message.IsChannelPost;
            _message.IsOutgoing = message.IsOutgoing;
            _message.MediaAlbumId = message.MediaAlbumId;
            _message.ReplyMarkup = message.ReplyMarkup;
            _message.ReplyToMessageId = message.ReplyToMessageId;
            _message.SenderUserId = message.SenderUserId;
            _message.SendingState = message.SendingState;
            _message.Ttl = message.Ttl;
            _message.TtlExpiresIn = message.TtlExpiresIn;
            _message.ViaBotUserId = message.ViaBotUserId;
            _message.Views = message.Views;

            if (_message.Content is MessageAlbum album)
            {
                FormattedText caption = null;

                foreach (var child in album.Layout.Messages)
                {
                    var childCaption = child.Content?.GetCaption();
                    if (childCaption != null && !string.IsNullOrEmpty(childCaption.Text))
                    {
                        if (caption == null || string.IsNullOrEmpty(caption.Text))
                        {
                            caption = childCaption;
                        }
                        else
                        {
                            caption = null;
                            break;
                        }
                    }
                }

                album.Caption = caption ?? new FormattedText();
            }
        }
    }
}
