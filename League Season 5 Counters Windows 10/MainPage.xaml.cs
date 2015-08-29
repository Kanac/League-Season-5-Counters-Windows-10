﻿using League_of_Legends_Counterpicks.Common;
using League_of_Legends_Counterpicks.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.ApplicationModel.Resources;
using Windows.Graphics.Display;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Email;
using Windows.ApplicationModel;
using League_of_Legends_Counterpicks.Advertisement;
using Windows.UI.Xaml.Input;
using System.Collections.ObjectModel;
using Microsoft.Advertising.WinRT.UI;
using Windows.UI.Core;

// The Hub Application template is documented at http://go.microsoft.com/fwlink/?LinkId=391641

namespace League_Season_5_Counters_Windows_10
{
    /// <summary>
    /// A page that displays a grouped collection of items.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private readonly NavigationHelper navigationHelper;
        private readonly ObservableDictionary defaultViewModel = new ObservableDictionary();
        private List<AdControl> adList = new List<AdControl>();

        public MainPage()
        {
            this.InitializeComponent();

            // Hub is only supported in Portrait orientation
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;
            //this.NavigationCacheMode = NavigationCacheMode.Required;

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;
        }


        /// <summary>
        /// Gets the <see cref="NavigationHelper"/> associated with this <see cref="Page"/>.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        /// <summary>
        /// Gets the view model for this <see cref="Page"/>.
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }       //Default view model contains all the groups 
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session.  The state will be null the first time a page is visited.</param>
        private async void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            // Retrieve Json data, and load the roles and set up background toast (one time thing)

            var roles = await DataSource.GetRolesAsync();
            var rolesWithSearch = new ObservableCollection<Role>(roles);
            rolesWithSearch.Insert(0, new Role("Search"));
            this.DefaultViewModel["Roles"] = rolesWithSearch;

            // Toast background task setup 
            if (e.PageState == null || (bool)e.PageState["firstLoad"] == true)
                setupToast();

        }



        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper"/></param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with
        /// serializable state.</param>
        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
            e.PageState["firstLoad"] = false;
        }

        private async void setupToast()
        {
            CheckAppVersion();
            var toastTaskName = "ToastBackgroundTask";
            var taskRegistered = false;

            foreach (var task in Windows.ApplicationModel.Background.BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == toastTaskName)
                {
                    taskRegistered = true;
                    break;
                }
            }

            if (!taskRegistered)
            {
                await Windows.ApplicationModel.Background.BackgroundExecutionManager.RequestAccessAsync();
                var builder = new BackgroundTaskBuilder();
                builder.Name = toastTaskName;
                builder.TaskEntryPoint = "Tasks.ToastBackground";
                var hourlyTrigger = new TimeTrigger(30, false);
                builder.SetTrigger(hourlyTrigger);

                BackgroundTaskRegistration task = builder.Register();
            }
        }

        private async void CheckAppVersion()
        {
            String appVersion = String.Format("{0}.{1}.{2}.{3}",
                    Package.Current.Id.Version.Build,
                    Package.Current.Id.Version.Major,
                    Package.Current.Id.Version.Minor,
                    Package.Current.Id.Version.Revision);

            String lastVersion = Windows.Storage.ApplicationData.Current.LocalSettings.Values["AppVersion"] as String;

            if (lastVersion == null || lastVersion != appVersion)
            {
                // Our app has been updated
                Windows.Storage.ApplicationData.Current.LocalSettings.Values["AppVersion"] = appVersion;

                // Call RemoveAccess
                BackgroundExecutionManager.RemoveAccess();
            }

            BackgroundAccessStatus status = await BackgroundExecutionManager.RequestAccessAsync();
        }

        /// <summary>
        /// Shows the details of a clicked group in the <see cref="RolePage"/>.
        /// </summary>
        /// 
        private async void GroupSection_ItemClick(object sender, ItemClickEventArgs e)
        {
            var roleId = ((Role)e.ClickedItem).UniqueId;
            if (roleId == "Search")
                roleId = "Filter";

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => this.Frame.Navigate(typeof(RolePage), roleId));

        }


        #region NavigationHelper registration

        /// <summary>
        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// <para>
        /// Page specific logic should be placed in event handlers for the
        /// <see cref="NavigationHelper.LoadState"/>
        /// and <see cref="NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method
        /// in addition to page state preserved during an earlier session.
        /// </para>
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion
        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            AdGrid.Children.Clear();
            base.OnNavigatingFrom(e);
        }


        private async void Share_Clicked(object sender, RoutedEventArgs e)
        {
            EmailMessage mail = new EmailMessage();
            mail.Subject = "Amazing League of Legends App";
            String body = "Try this free app out to help you climb elo easily in League of Legends@https://www.windowsphone.com/en-us/store/app/league-season-5-counters/3366702e-67c7-48e7-bc82-d3a4534f3086";
            body = body.Replace("@", "\n");
            mail.Body = body;
            try
            {
                await EmailManager.ShowComposeNewEmailAsync(mail);
            }
            catch (Exception) { }
        }

        private void ChangeLog_Clicked(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(ChangeLog));
        }

        
        private void Tweet(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(Twitter));
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(RolePage), "Filter");
        }
        private void TextBox_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            Frame.Navigate(typeof(RolePage), "Filter");
        }

  

        private async void Comment_Click(object sender, RoutedEventArgs e)
        {
            EmailRecipient sendTo = new EmailRecipient() { Address = "testgglol@outlook.com" };
            EmailMessage mail = new EmailMessage();

            mail.Subject = "Feedback for League Season 5 Counters";
            mail.To.Add(sendTo);
            await EmailManager.ShowComposeNewEmailAsync(mail);
        }

        
        private void Ad_Loaded(object sender, RoutedEventArgs e)
        {
            var ad = sender as AdControl;
            //Check if the ad list already has a reference to this ad before inserting
            if (adList.Where(x => x.AdUnitId == ad.AdUnitId).Count() == 0)
                adList.Add(ad);

            if (App.licenseInformation.ProductLicenses["AdRemoval"].IsActive)
            {
                // Hide the app for the purchaser
                ad.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
            else
            {
                // Otherwise show the ad
                ad.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
        }

        private void GridAd_Loaded(object sender, RoutedEventArgs e)
        {
            var grid = sender as Grid;
            if (App.licenseInformation.ProductLicenses["AdRemoval"].IsActive)
            {
                var rowDefinitions = grid.RowDefinitions;
                foreach (var r in rowDefinitions)
                {
                    if (r.Height.Value == 90)
                    {
                        r.SetValue(RowDefinition.HeightProperty, new GridLength(0));
                    }
                }
            }
        }

        private void AdBlock_Click(object sender, RoutedEventArgs e)
        {
            AdRemover.Purchase();
        }

        private void Ad_Error(object sender, AdErrorEventArgs e)
        {

        }


    }
}