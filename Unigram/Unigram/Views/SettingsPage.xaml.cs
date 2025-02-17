﻿using System;
using System.Diagnostics;
using Unigram.Common;
using Unigram.Controls;
using Unigram.Controls.Views;
using Unigram.Views;
using Unigram.ViewModels;
using Unigram.Views.Settings;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.Storage;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation.Collections;
using Windows.Storage.Pickers;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Media.Animation;
using Unigram.ViewModels.Users;
using Windows.UI.Xaml.Markup;
using System.Linq;
using Windows.UI.Xaml.Media;
using Telegram.Td.Api;
using Windows.Media.Capture;
using Unigram.ViewModels.Delegates;
using Windows.ApplicationModel;
using Unigram.Views.Passport;
using Unigram.Controls.Gallery;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml.Controls.Primitives;
using Unigram.Converters;

namespace Unigram.Views
{
    public sealed partial class SettingsPage : Page, ISettingsDelegate, IDisposable
    {
        public SettingsViewModel ViewModel => DataContext as SettingsViewModel;

        public SettingsPage()
        {
            InitializeComponent();
            DataContext = TLContainer.Current.Resolve<SettingsViewModel, ISettingsDelegate>(this);

            NavigationCacheMode = NavigationCacheMode.Required;

            Diagnostics.Text = $"Unigram " + GetVersion();

            if (ApiInformation.IsEnumNamedValuePresent("Windows.UI.Xaml.Controls.Primitives.FlyoutPlacementMode", "BottomEdgeAlignedRight"))
            {
                PhotoFlyout.Placement = FlyoutPlacementMode.BottomEdgeAlignedRight;
            }
        }

        public void Dispose()
        {
            //DataContext = null;
            //Bindings?.Update();
            Bindings?.StopTracking();
        }

        private string GetVersion()
        {
            Package package = Package.Current;
            PackageId packageId = package.Id;
            PackageVersion version = packageId.Version;
            return string.Format("{0}.{1}.{2}", version.Major, version.Minor, version.Build, version.Revision);
        }

        private MasterDetailView _masterDetail;
        public MasterDetailView MasterDetail
        {
            get
            {
                return _masterDetail;
            }
            set
            {
                _masterDetail = value;
                ViewModel.NavigationService = value.NavigationService;
            }
        }

        private void Advanced_Click(object sender, RoutedEventArgs e)
        {
            MasterDetail.NavigationService.Navigate(typeof(SettingsAdvancedPage));
            MasterDetail.NavigationService.GoBackAt(0, false);
        }

        private void Phone_Click(object sender, RoutedEventArgs e)
        {
            MasterDetail.NavigationService.Navigate(typeof(SettingsPhoneIntroPage));
            MasterDetail.NavigationService.GoBackAt(0, false);
        }

        private void Username_Click(object sender, RoutedEventArgs e)
        {
            MasterDetail.NavigationService.Navigate(typeof(SettingsUsernamePage));
            MasterDetail.NavigationService.GoBackAt(0, false);
        }

        public async void EditName_Click(object sender, RoutedEventArgs e)
        {
            var chat = ViewModel.Chat;
            if (chat == null)
            {
                return;
            }

            if (chat.Type is ChatTypePrivate privata)
            {
                var user = ViewModel.ProtoService.GetUser(privata.UserId);
                if (user == null)
                {
                    return;
                }

                var dialog = new EditUserNameView(user.FirstName, user.LastName);

                var confirm = await dialog.ShowQueuedAsync();
                if (confirm == ContentDialogResult.Primary)
                {
                    ViewModel.ProtoService.Send(new SetName(dialog.FirstName, dialog.LastName));
                }
            }
        }

        private async void About_Click(object sender, RoutedEventArgs e)
        {
            var chat = ViewModel.Chat;
            if (chat == null)
            {
                return;
            }

            if (chat.Type is ChatTypePrivate privata)
            {
                var user = ViewModel.ProtoService.GetUserFull(privata.UserId);
                if (user == null)
                {
                    return;
                }

                var dialog = new EditYourAboutView(user.Bio);

                var confirm = await dialog.ShowQueuedAsync();
                if (confirm == ContentDialogResult.Primary)
                {
                    ViewModel.ProtoService.Send(new SetBio(dialog.About));
                }
            }
        }

        private void Privacy_Click(object sender, RoutedEventArgs e)
        {
            MasterDetail.NavigationService.Navigate(typeof(SettingsPrivacyAndSecurityPage));
            MasterDetail.NavigationService.GoBackAt(0, false);
        }

        private void Stickers_Click(object sender, RoutedEventArgs e)
        {
            MasterDetail.NavigationService.Navigate(typeof(SettingsStickersPage));
            MasterDetail.NavigationService.GoBackAt(0, false);
        }

        private void Data_Click(object sender, RoutedEventArgs e)
        {
            MasterDetail.NavigationService.Navigate(typeof(SettingsDataAndStoragePage));
            MasterDetail.NavigationService.GoBackAt(0, false);
        }

        private void Notifications_Click(object sender, RoutedEventArgs e)
        {
            MasterDetail.NavigationService.Navigate(typeof(SettingsNotificationsPage));
            MasterDetail.NavigationService.GoBackAt(0, false);
        }

        private void Appearance_Click(object sender, RoutedEventArgs e)
        {
            MasterDetail.NavigationService.Navigate(typeof(SettingsAppearancePage));
            MasterDetail.NavigationService.GoBackAt(0, false);
        }

        private void Language_Click(object sender, RoutedEventArgs e)
        {
            MasterDetail.NavigationService.Navigate(typeof(SettingsLanguagePage));
            MasterDetail.NavigationService.GoBackAt(0, false);
        }

        private void Wallet_Click(object sender, RoutedEventArgs e)
        {
            MasterDetail.NavigationService.NavigateToWallet();
            MasterDetail.NavigationService.GoBackAt(0, false);
        }

        private void Passport_Click(object sender, RoutedEventArgs e)
        {
            MasterDetail.NavigationService.Navigate(typeof(PassportPage));
            MasterDetail.NavigationService.GoBackAt(0, false);
        }

        private void Questions_Click(object sender, RoutedEventArgs e)
        {
            MasterDetail.NavigationService.Navigate(typeof(InstantPage), "https://telegram.org/faq");
            MasterDetail.NavigationService.GoBackAt(0, false);
        }

        private async void Photo_Click(object sender, RoutedEventArgs e)
        {
            var chat = ViewModel.Chat;
            if (chat == null)
            {
                return;
            }

            if (chat.Type is ChatTypePrivate)
            {
                var user = ViewModel.ProtoService.GetUser(chat);
                if (user == null || user.ProfilePhoto == null)
                {
                    return;
                }

                var viewModel = new UserPhotosViewModel(ViewModel.ProtoService, ViewModel.Aggregator, user);
                await GalleryView.GetForCurrentView().ShowAsync(viewModel, () => Photo);
            }
        }

        public async void EditPhoto_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            picker.ViewMode = PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.AddRange(Constants.PhotoTypes);

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                var dialog = new EditYourPhotoView(file)
                {
                    CroppingProportions = ImageCroppingProportions.Square,
                    IsCropEnabled = false
                };
                var dialogResult = await dialog.ShowAsync();
                if (dialogResult == ContentDialogResult.Primary)
                {
                    ViewModel.EditPhotoCommand.Execute(dialog.Result);
                }
            }
        }

        private async void EditCamera_Click(object sender, RoutedEventArgs e)
        {
            var capture = new CameraCaptureUI();
            capture.PhotoSettings.AllowCropping = false;
            capture.PhotoSettings.Format = CameraCaptureUIPhotoFormat.Jpeg;
            capture.PhotoSettings.MaxResolution = CameraCaptureUIMaxPhotoResolution.HighestAvailable;

            var file = await capture.CaptureFileAsync(CameraCaptureUIMode.Photo);
            if (file != null)
            {
                var dialog = new EditYourPhotoView(file)
                {
                    CroppingProportions = ImageCroppingProportions.Square,
                    IsCropEnabled = false
                };
                var dialogResult = await dialog.ShowAsync();
                if (dialogResult == ContentDialogResult.Primary)
                {
                    ViewModel.EditPhotoCommand.Execute(dialog.Result);
                }
            }
        }

        #region Binding

        public void UpdateUser(Chat chat, User user, bool secret)
        {
            Photo.Source = PlaceholderHelper.GetUser(ViewModel.ProtoService, user, 64);
            Title.Text = user.GetFullName();

            Verified.Visibility = user.IsVerified ? Visibility.Visible : Visibility.Collapsed;

#if DEBUG
            PhoneNumber.Badge = "+39 --- --- ----";
#else
            PhoneNumber.Badge = Common.PhoneNumber.Format(user.PhoneNumber);
#endif

            Username.Badge = string.IsNullOrEmpty(user.Username) ? Strings.Resources.UsernameEmpty : $"{user.Username}";
        }

        public void UpdateUserFullInfo(Chat chat, User user, UserFullInfo fullInfo, bool secret, bool accessToken)
        {
            Bio.Badge = string.IsNullOrEmpty(fullInfo.Bio) ? Strings.Resources.UserBioDetail : fullInfo.Bio;
        }

        public void UpdateUserStatus(Chat chat, User user)
        {
        }

        public void UpdateChat(Chat chat)
        {
        }

        public void UpdateChatTitle(Chat chat)
        {
        }

        public void UpdateChatPhoto(Chat chat)
        {
        }



        public void UpdateFile(File file)
        {
            var chat = ViewModel.Chat;
            if (chat == null)
            {
                return;
            }

            var user = ViewModel.CacheService.GetUser(chat);
            if (user == null)
            {
                return;
            }

            if (user.UpdateFile(file))
            {
                Photo.Source = PlaceholderHelper.GetUser(ViewModel.ProtoService, user, 64);
            }
        }

#endregion

        private int _advanced;

        private void Diagnostics_Click(object sender, RoutedEventArgs e)
        {
            _advanced++;

            if (_advanced >= 10)
            {
                _advanced = 0;

                MasterDetail.NavigationService.Navigate(typeof(DiagnosticsPage));
            }
        }
    }
}
