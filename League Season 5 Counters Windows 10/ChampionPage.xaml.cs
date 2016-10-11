using League_of_Legends_Counterpicks.Common;
using League_of_Legends_Counterpicks.Data;
using League_of_Legends_Counterpicks.DataModel;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Popups;
using Windows.System;
using Microsoft.AdMediator.Universal;
using Microsoft.Advertising.WinRT.UI;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.UI;
using League_Season_5_Counters_Windows_10.Helper;
using League_Season_5_Counters_Windows_10.DataModel;


// The Hub Application template is documented at http://go.microsoft.com/fwlink/?LinkID=391641

namespace League_Season_5_Counters_Windows_10
{
    /// <summary>
    /// A page that displays details for a single item within a group.
    /// </summary>
    public sealed partial class ChampionPage : Page
    {
        private readonly NavigationHelper navigationHelper;
        private readonly ObservableDictionary defaultViewModel = new ObservableDictionary();
        private Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
        private Role champions = DataSource.GetAllChampions();
        private TextBox nameBox;
        private TextBox feedbackBox;
        private String name = String.Empty;
        private bool emptyComments, emptyPlayingComments, emptySynergyChampions;
        private bool championFeedbackLoaded;
        private PageEnum.CommentPage? pageType;
        private PageEnum.ClientChampionPage? championPageType;
        private TextBlock counterMessage, playingMessage, synergyMessage;
        private TextBox filterBox;
        private TextBlock selectedTextBlock;
        private ProgressRing counterLoadingRing, easyMatchupLoadingRing, synergyLoadingRing, counterCommentsLoadingRing, playingCommentsLoadingRing;
        private CommentDataSource commentViewModel = new CommentDataSource(App.MobileService);

        public ChampionPage()
        {
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;
            DefaultViewModel["AdVisibility"] = App.licenseInformation.ProductLicenses["AdRemoval"].IsActive ? Visibility.Collapsed : Visibility.Visible;
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
            get { return this.defaultViewModel; }
        }

        
        /// <summary>
        /// Populates the page with content passed during navigation. Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>.
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session.  The state will be null the first time a page is visited.</param>
        private async void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            int id = await AdData.GetAdId();
            HelperMethods.CreateSingleAdUnit(id,"TallAd", AdGrid);
            HelperMethods.CreateAdUnits(id, "TallAd", AdGrid2, 40);

            // Setup the underlying UI 
            var champName = (string)e.NavigationParameter;
            Champion champion = DataSource.GetChampion(champName);
            champions = DataSource.GetAllChampions();
            this.DefaultViewModel["Champion"] = champion;
            this.DefaultViewModel["Role"] = DataSource.GetRoleId(champion.UniqueId);
            this.DefaultViewModel["Filter"] = champions.Champions;

            
            // If navigating via a counterpick, on loading that page, remove the previous history so the back page will go to main or role, not champion
            var prevPage = Frame.BackStack.ElementAt(Frame.BackStackDepth - 1);
            if (prevPage.SourcePageType.Equals(typeof(ChampionPage))){
                Frame.BackStack.RemoveAt(Frame.BackStackDepth - 1);
            }

            // Champion feedback code 
            // await commentViewModel.SeedDataAsync(champions);
            // Grab the champion feedback from the server 
            await commentViewModel.GetChampionFeedbackAsync(champName);

            // Check if an there was no champion retrieved as well as an error message (must be internet connection problem)
            if (commentViewModel.ChampionFeedback == null && commentViewModel.ErrorMessage != null){
                MessageDialog messageBox = new MessageDialog("Make sure your internet connection is working and try again!");
                await messageBox.ShowAsync();
                Application.Current.Exit();
            }

           
            // Collapse the progress ring once counters have been loaded. If the ring hasn't loaded yet, set a boolean to collapse it once it loads.
            championFeedbackLoaded = true;

            if (counterLoadingRing != null)
                counterLoadingRing.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            if (easyMatchupLoadingRing != null)
                easyMatchupLoadingRing.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            if (synergyLoadingRing != null)
                synergyLoadingRing.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            if (counterCommentsLoadingRing != null)
                counterCommentsLoadingRing.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            if (playingCommentsLoadingRing != null)
                playingCommentsLoadingRing.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

            // Check if comments exist for counter comments. If not, show a message indicating so. 
            if (commentViewModel.ChampionFeedback.Comments.Where(x => x.Page == PageEnum.CommentPage.Counter).Count() == 0) {
                if (counterMessage == null) 
                    emptyComments = true;
                else 
                    counterMessage.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }

            // Check if comments exist for playing comments. If not, show a message indicating so. 
            if (commentViewModel.ChampionFeedback.Comments.Where(x => x.Page == PageEnum.CommentPage.Playing).Count() == 0)
            {
                if (playingMessage == null)
                    emptyPlayingComments = true;
                else 
                    playingMessage.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }

            // Check if synergy champions existing. If not, show a message indicating so. 
            if (commentViewModel.ChampionFeedback.Counters.Where(x => x.Page == PageEnum.ChampionPage.Synergy).Count() == 0)
            {
                if (synergyMessage == null)
                    emptySynergyChampions = true;
                else
                    synergyMessage.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }

            // Make updates to champion comments observable
            this.DefaultViewModel["ChampionFeedback"] = commentViewModel.ChampionFeedback;
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper"/>.</param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with
        /// serializable state.</param>
        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
            // TODO: Save the unique state of the page here.
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
        /// <param name="e">Provides data for navigation methods and event
        /// handlers that cannot cancel the navigation request.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            AdGrid2.Children.Clear();
            AdGrid.Children.Clear();
            if (e.NavigationMode == NavigationMode.Back)
            {
                ResetPageCache();
            }
            base.OnNavigatingFrom(e);
        }

        #endregion
        private void ResetPageCache()
        {
            var cacheSize = ((Frame)Parent).CacheSize;
            ((Frame)Parent).CacheSize = 0;
            ((Frame)Parent).CacheSize = cacheSize;
        }

        private void Key_Down(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Back && Frame.CanGoBack)
                if ((nameBox != null && nameBox.FocusState == FocusState.Unfocused) && (filterBox != null && filterBox.FocusState == FocusState.Unfocused) &&
                    (feedbackBox != null && feedbackBox.FocusState == FocusState.Unfocused))
                    Frame.GoBack();
        }

        private void Comment_Flyout(object sender, TappedRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout(sender as FrameworkElement);
            BottomAppBar.Visibility = Visibility.Collapsed;
            selectedTextBlock = sender as TextBlock;
        }

        private void Comment_FlyoutClosed(object sender, object e)
        {
            BottomAppBar.Visibility = Visibility.Visible;
        }

        private async void Submit_CounterComment(object sender, TappedRoutedEventArgs e)
        {
            Button button = sender as Button;
            TextBox nameBox = button.FindName("CounterCommentNameBox") as TextBox;
            TextBox commentBox = button.FindName("CounterCommentBox") as TextBox;
            Counter counter = button.DataContext as Counter;

            // Ensure all collections are loaded first
            if (counter.CounterComments == null)
            {
                MessageDialog emptyBox = new MessageDialog("Wait for data to finish loading!");
                await emptyBox.ShowAsync();
                return;
            }

            // Ensure user inputs names
            if (String.IsNullOrEmpty(nameBox.Text))
            {
                MessageDialog emptyBox = new MessageDialog("Enter your name first!");
                await emptyBox.ShowAsync();
                return;
            }

            // Check for empty message
            if (String.IsNullOrEmpty(commentBox.Text)){
                MessageDialog emptyBox = new MessageDialog("Write a message first!");
                await emptyBox.ShowAsync();
                return;
            }

            // Ensure user inputs proper feedback (if english string, check for non-spam with enough words, if chinese, check for at least 8 characters)
            if ((commentBox.Text[0] <= 255 && commentBox.Text[0] >= 0 && (commentBox.Text.Count() < 30 || commentBox.Text.Distinct().Count() < 5 || !commentBox.Text.Contains(' '))) || ((commentBox.Text[0] >= 0x4E00 && commentBox.Text[0] <= 0x9FA5) && commentBox.Text.Count() < 8))
            {
                MessageDialog emptyBox = new MessageDialog("Write a proper and long enough message first!");
                await emptyBox.ShowAsync();
                return;
            }

            // Ensure at least 8 chars for non-ascii or non-chinese cases
            if ((commentBox.Text.Count() < 8 || commentBox.Text.Distinct().Count() < 5))
            {
                MessageDialog emptyBox = new MessageDialog("Write an actual message first!");
                await emptyBox.ShowAsync();
                return;
            }

            string commentText = commentBox.Text;
            commentBox.Text = String.Empty;
            (button.FindName("EmptyMessage") as TextBlock).Visibility = Visibility.Collapsed;

            // Submit the comment and a self-user rating of 1
            var comment = await commentViewModel.SubmitCounterCommentAsync(commentText, nameBox.Text, counter);
            await commentViewModel.SubmitCounterCommentRating(comment, 1);

            // Update the view
            counter.SortCounterComments();
            int count = counter.CounterComments.Count();
            selectedTextBlock.Text = (count == 1) ? count + " comment" : count + " comments";
            if (count > 0) selectedTextBlock.Foreground = new SolidColorBrush(Colors.Yellow);
        }

        // Normal method of handling counter tapped
        private void Champ_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Image counterImage = (sender as Image);
            counterImage.Opacity = 0.5;
            var counter = counterImage.DataContext as Counter;
            Frame.Navigate(typeof(ChampionPage), counter.Name);

        }

        // Reverse relationship for easy matchups 
        private void EasyMatchup_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Image easyMatchupImage = (sender as Image);
            easyMatchupImage.Opacity = 0.5;
            var easyMatchup = easyMatchupImage.DataContext as Counter;
            Frame.Navigate(typeof(ChampionPage), easyMatchup.ChampionFeedbackName);
        }

        // Bidirectional relationship for synergy picks
        private void SynergyChamp_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Image synergyImage = (sender as Image);
            synergyImage.Opacity = 0.5;
            var synergy = synergyImage.DataContext as Counter;
            string championName;
            // Choose the synergy name that is not the current champion's name for this page (We want to navigate on the other end of the relationship)
            if (synergy.ChampionFeedbackName == commentViewModel.ChampionFeedback.Name)
                championName = synergy.Name;
            else
                championName = synergy.ChampionFeedbackName;
            Frame.Navigate(typeof(ChampionPage), championName);

        }

        private void Synergy_Loaded(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            var synergyImage = sender as Image;
            var synergyChamp = synergyImage.DataContext as Counter;
            //If null, simply return until datacontext changes to a proper form and comes back here again
            if (synergyChamp == null)
                return;

            // Get the champion's name for the page
            var champName = commentViewModel.ChampionFeedback.Name;
            string imageName;
            // The synergy relationship could be either way -- but we know we want the image that's not the champion for the page
            if (synergyChamp.ChampionFeedbackName == champName)
                imageName = synergyChamp.Name;
            else
                imageName = synergyChamp.ChampionFeedbackName;

            var uri = "ms-appx:///Assets/" + DataSource.TransformNameToKey(imageName) + "_Square_0.png";
            synergyImage.Source = new BitmapImage(new Uri(uri, UriKind.Absolute));

            (synergyImage.FindName("SynergyImageToolTip") as ToolTip).Content = imageName;
        }



        private async void CounterUpvote_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var upvote = (Image)sender;
            var counter = upvote.DataContext as Counter;

            //Find the exisiting counter rating, if any
            var existingRating = counter.CounterRatings.Where(x => x.UniqueUser == commentViewModel.GetDeviceId()).FirstOrDefault();
            //If there was a previous rating, change vote images respectively
            if (existingRating != null && existingRating.Score != 0)
            {
                //Pressing upvote again? Unhighlight the button.
                if (existingRating.Score == 1)
                {
                    upvote.Source = new BitmapImage(new Uri("ms-appx:///Assets/upvoteblank.png", UriKind.Absolute));
                }

                //Going from downvote to upvote? Change to highlighted upvote 
                else if (existingRating.Score == -1)
                {
                    var downvote = ((Image)sender).FindName("DownvoteImage") as Image;
                    downvote.Source = new BitmapImage(new Uri("ms-appx:///Assets/downvoteblank.png", UriKind.Absolute));
                    upvote.Source = new BitmapImage(new Uri("ms-appx:///Assets/upvote.png", UriKind.Absolute));
                }
            }
            //Otherwise, highlight the upvote button
            else
            {
                upvote.Source = new BitmapImage(new Uri("ms-appx:///Assets/upvote.png", UriKind.Absolute));
            }
            await commentViewModel.SubmitCounterRating(counter, 1);

        }

        private async void CounterDownvote_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var downvote = (Image)sender;
            var counter = downvote.DataContext as Counter;

            var existingRating = counter.CounterRatings.Where(x => x.UniqueUser == commentViewModel.GetDeviceId()).FirstOrDefault();
            if (existingRating != null && existingRating.Score != 0)
            {
                if (existingRating.Score == 1)
                {
                    var upvote = ((Image)sender).FindName("UpvoteImage") as Image;
                    downvote.Source = new BitmapImage(new Uri("ms-appx:///Assets/downvote.png", UriKind.Absolute));
                    upvote.Source = new BitmapImage(new Uri("ms-appx:///Assets/upvoteblank.png", UriKind.Absolute));
                }

                else if (existingRating.Score == -1)
                {
                    downvote.Source = new BitmapImage(new Uri("ms-appx:///Assets/downvoteblank.png", UriKind.Absolute));
                }
            }
            else
            {
                downvote.Source = new BitmapImage(new Uri("ms-appx:///Assets/downvote.png", UriKind.Absolute));
            }

            await commentViewModel.SubmitCounterRating(counter, -1);
        }


        // Using dataloaded instead for counter votes due to the fact that >5 counters causes the datacontext of the subsequent images to be null briefly
        private void CounterUpvote_DataLoaded(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            Image upvote = sender as Image;
            var counter = upvote.DataContext as Counter;
            if (counter == null)
                return;
            var existingRating = counter.CounterRatings.Where(x => x.UniqueUser == commentViewModel.GetDeviceId()).FirstOrDefault();
            if (existingRating != null)
            {
                if (existingRating.Score == 1)
                {
                    upvote.Source = new BitmapImage(new Uri("ms-appx:///Assets/upvote.png", UriKind.Absolute));
                }
            }
        }

        private void CounterDownvote_DataLoaded(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            Image downvote = sender as Image;
            var counter = downvote.DataContext as Counter;
            if (counter == null)
                return;
            var existingRating = counter.CounterRatings.Where(x => x.UniqueUser == commentViewModel.GetDeviceId()).FirstOrDefault();
            if (existingRating != null)
            {
                if (existingRating.Score == -1)
                {
                    downvote.Source = new BitmapImage(new Uri("ms-appx:///Assets/downvote.png", UriKind.Absolute));
                }
            }
        }

        private async void CounterCommentUpvote_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var upvote = (Image)sender;
            var counterComment = upvote.DataContext as CounterComment;

            //Find the exisiting counter rating, if any
            var existingRating = counterComment.CounterCommentRatings.Where(x => x.UniqueUser == commentViewModel.GetDeviceId()).FirstOrDefault();
            //If there was a previous rating, change vote images respectively
            if (existingRating != null && existingRating.Score != 0)
            {
                //Pressing upvote again? Unhighlight the button.
                if (existingRating.Score == 1)
                {
                    upvote.Source = new BitmapImage(new Uri("ms-appx:///Assets/upvoteblank.png", UriKind.Absolute));
                }

                //Going from downvote to upvote? Change to highlighted upvote 
                else if (existingRating.Score == -1)
                {
                    var downvote = ((Image)sender).FindName("DownvoteImage") as Image;
                    downvote.Source = new BitmapImage(new Uri("ms-appx:///Assets/downvoteblank.png", UriKind.Absolute));
                    upvote.Source = new BitmapImage(new Uri("ms-appx:///Assets/upvote.png", UriKind.Absolute));
                }
            }
            //Otherwise, highlight the upvote button
            else
            {
                upvote.Source = new BitmapImage(new Uri("ms-appx:///Assets/upvote.png", UriKind.Absolute));
            }
            await commentViewModel.SubmitCounterCommentRating(counterComment, 1);

        }

        private async void CounterCommentDownvote_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var downvote = (Image)sender;
            var counterComment = downvote.DataContext as CounterComment;

            var existingRating = counterComment.CounterCommentRatings.Where(x => x.UniqueUser == commentViewModel.GetDeviceId()).FirstOrDefault();
            if (existingRating != null && existingRating.Score != 0)
            {
                if (existingRating.Score == 1)
                {
                    var upvote = ((Image)sender).FindName("UpvoteImage") as Image;
                    downvote.Source = new BitmapImage(new Uri("ms-appx:///Assets/downvote.png", UriKind.Absolute));
                    upvote.Source = new BitmapImage(new Uri("ms-appx:///Assets/upvoteblank.png", UriKind.Absolute));
                }

                else if (existingRating.Score == -1)
                {
                    downvote.Source = new BitmapImage(new Uri("ms-appx:///Assets/downvoteblank.png", UriKind.Absolute));
                }
            }
            else
            {
                downvote.Source = new BitmapImage(new Uri("ms-appx:///Assets/downvote.png", UriKind.Absolute));
            }

            await commentViewModel.SubmitCounterCommentRating(counterComment, -1);
        }


        // Using dataloaded instead for counter votes due to the fact that >5 counters causes the datacontext of the subsequent images to be null briefly
        private void CounterCommentUpvote_DataLoaded(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            Image upvote = sender as Image;
            var counterComment = upvote.DataContext as CounterComment;
            if (counterComment == null)
                return;
            var existingRating = counterComment.CounterCommentRatings.Where(x => x.UniqueUser == commentViewModel.GetDeviceId()).FirstOrDefault();
            if (existingRating != null)
            {
                if (existingRating.Score == 1)
                {
                    upvote.Source = new BitmapImage(new Uri("ms-appx:///Assets/upvote.png", UriKind.Absolute));
                }
            }
        }

        private void CounterCommentDownvote_DataLoaded(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            Image downvote = sender as Image;
            var counterComment = downvote.DataContext as CounterComment;
            if (counterComment == null)
                return;
            var existingRating = counterComment.CounterCommentRatings.Where(x => x.UniqueUser == commentViewModel.GetDeviceId()).FirstOrDefault();
            if (existingRating != null)
            {
                if (existingRating.Score == -1)
                {
                    downvote.Source = new BitmapImage(new Uri("ms-appx:///Assets/downvote.png", UriKind.Absolute));
                }
            }
        }

        private async void Submit_Counter(object sender, TappedRoutedEventArgs e)
        {
            var filter = ((DefaultViewModel["Filter"]) as ObservableCollection<Champion>);


            // Ensure all collections are loaded first
            if (!championFeedbackLoaded)
            {
                MessageDialog emptyBox = new MessageDialog("Wait for data to finish loading!");
                await emptyBox.ShowAsync();
                return;
            }

            if (championPageType == null)
            {
                MessageDialog messageBox = new MessageDialog("Choose whether this champion is a counter, easy matchup, or synergy pick!");
                await messageBox.ShowAsync();
                return;
            }

            // Only allow counter submision if there is exactly one selected
            if (filter.Count() != 1)
            {
                MessageDialog messageBox = new MessageDialog("Select a champion first!");
                await messageBox.ShowAsync();
                return;
            }

            // Get the selected champion's name
            var selectedChamp = filter.FirstOrDefault().UniqueId;

            // Prevent duplicate counter submissions for counters 
            if (championPageType == PageEnum.ClientChampionPage.Counter)
            {
                if (commentViewModel.ChampionFeedback.Counters.Where(c => c.Page == PageEnum.ChampionPage.Counter && c.Name == selectedChamp).Count() == 1)
                {
                    String message = filter.FirstOrDefault().UniqueId + " is already a counter!";
                    MessageDialog messageBox = new MessageDialog(message);
                    await messageBox.ShowAsync();
                    return;
                }
            }

            // Prevent duplicate counter submissions for easy matchups (it is reversed) 
            if (championPageType == PageEnum.ClientChampionPage.EasyMatchup)
            {
                if (commentViewModel.ChampionFeedback.EasyMatchups.Where(c => c.ChampionFeedbackName == selectedChamp).Count() == 1)
                {
                    String message = filter.FirstOrDefault().UniqueId + " is already an easy matchup!";
                    MessageDialog messageBox = new MessageDialog(message);
                    await messageBox.ShowAsync();
                    return;
                }
            }

            // Prevent duplicate synergy submissions
            if (championPageType == PageEnum.ClientChampionPage.Synergy)
            {
                // Check both ways (whether the synergy is the child as a counter or the parent as a champion feedback)
                if (commentViewModel.ChampionFeedback.Counters.Where(c => c.Page == PageEnum.ChampionPage.Synergy && (c.Name == selectedChamp || c.ChampionFeedbackName == selectedChamp)).Count() == 1)
                {
                    String message = filter.FirstOrDefault().UniqueId + " is already a synergy pick!";
                    MessageDialog messageBox = new MessageDialog(message);
                    await messageBox.ShowAsync();
                    return;
                }
            }

            if (filter.FirstOrDefault().UniqueId == commentViewModel.ChampionFeedback.Name)
            {
                MessageDialog messageBox = new MessageDialog("You cannot submit a champion as a counter of itself!");
                await messageBox.ShowAsync();
                return;
            }

            // Scroll to the relevant section first before submitting to prevent lag
            if (championPageType == PageEnum.ClientChampionPage.Counter)
                MainHub.ScrollToSection(CounterSection);
            else if (championPageType == PageEnum.ClientChampionPage.EasyMatchup)
                MainHub.ScrollToSection(EasyMatchupSection);
            else
            {
                MainHub.ScrollToSection(SynergySection);
                synergyMessage.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }

            // Finally submit the counter
            var counter = await commentViewModel.SubmitCounter(filter.FirstOrDefault().UniqueId, championPageType);
            await commentViewModel.SubmitCounterRating(counter, 1);

            //Sort the appropriate collection
            if (championPageType == PageEnum.ClientChampionPage.Counter)
                commentViewModel.ChampionFeedback.SortCounters();
            else if (championPageType == PageEnum.ClientChampionPage.EasyMatchup)
                commentViewModel.ChampionFeedback.SortEasyMatchups();
            else
                commentViewModel.ChampionFeedback.SortSynergy();
        }

        private void CounterChampion_Checked(object sender, RoutedEventArgs e)
        {
            championPageType = PageEnum.ClientChampionPage.Counter;

        }

        private void EasyMatchupChampion_Checked(object sender, RoutedEventArgs e)
        {
            championPageType = PageEnum.ClientChampionPage.EasyMatchup;
        }

        private void SynergyChampion_Checked(object sender, RoutedEventArgs e)
        {
            championPageType = PageEnum.ClientChampionPage.Synergy;
        }

        private void Filter_GotFocus(object sender, RoutedEventArgs e)
        {
            var filterBox = sender as TextBox;
            filterBox.Text = String.Empty;
        }

        private void Filter_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox filterBox = sender as TextBox;
            Role filter = DataSource.FilterChampions(filterBox.Text);
            DefaultViewModel["Filter"] = filter.Champions;
        }

        private void Counter_Click(object sender, ItemClickEventArgs e)
        {
            Champion counter = e.ClickedItem as Champion;
            filterBox.TextChanged -= Filter_TextChanged;
            filterBox.Text = counter.UniqueId;
            var OneChampionFilter = new ObservableCollection<Champion>() { DataSource.GetChampion(counter.UniqueId)};
            DefaultViewModel["Filter"] = OneChampionFilter;
            filterBox.TextChanged += Filter_TextChanged;
        }

        private void FilterBox_Loaded(object sender, RoutedEventArgs e)
        {
            filterBox = sender as TextBox;
        }

        private async void Send_Feedback(object sender, TappedRoutedEventArgs e)
        {
            // Ensure all collections are loaded first
            if (!championFeedbackLoaded)
            {
                MessageDialog emptyBox = new MessageDialog("Wait for data to finish loading!");
                await emptyBox.ShowAsync();
                return;
            }

            // Ensure user selects comment type
            if (this.pageType == null)
            {
                MessageDialog emptyBox = new MessageDialog("Choose your comment as countering or playing as!");
                await emptyBox.ShowAsync();
                return;
            }

            // Ensure user inputs names
            if (String.IsNullOrEmpty(name) || name == "Your name")
            {
                MessageDialog emptyBox = new MessageDialog("Enter your name first!");
                await emptyBox.ShowAsync();
                return;
            }

            // Check for empty message
            if (String.IsNullOrEmpty(feedbackBox.Text))
            {
                MessageDialog emptyBox = new MessageDialog("Write a message first!");
                await emptyBox.ShowAsync();
                return;
            }

            // Ensure user inputs proper feedback (if english string, check for non-spam with enough words, if chinese, check for at least 8 characters)
            if ((feedbackBox.Text[0] <= 255 && feedbackBox.Text[0] >= 0 && (feedbackBox.Text.Count() < 30 || feedbackBox.Text.Distinct().Count() < 5 || !feedbackBox.Text.Contains(' '))) || ((feedbackBox.Text[0] >= 0x4E00 && feedbackBox.Text[0] <= 0x9FA5) && feedbackBox.Text.Count() < 8))
            {
                MessageDialog emptyBox = new MessageDialog("Write a proper and long enough message first!");
                await emptyBox.ShowAsync();
                return;
            }

            // Ensure at least 8 chars for non-ascii or non-chinese cases
            if ((feedbackBox.Text.Count() < 8 || feedbackBox.Text.Distinct().Count() < 5))
            {
                MessageDialog emptyBox = new MessageDialog("Write an actual message first!");
                await emptyBox.ShowAsync();
                return;
            }

            // After ensuring the data is allowed, scroll to the proper hub section according to the type of comment (doing this before actual submission prevents lag)
            if (pageType == PageEnum.CommentPage.Counter)
            {
                MainHub.ScrollToSection(CounterCommentSection);
                counterMessage.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

            }
            else if (pageType == PageEnum.CommentPage.Playing)
            {
                MainHub.ScrollToSection(PlayingCommentSection);
                playingMessage.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }

            // Submit the comment and a self-user rating of 1
            var text = feedbackBox.Text;
            feedbackBox.Text = String.Empty;
            var comment = await commentViewModel.SubmitCommentAsync(text, name, (PageEnum.CommentPage)pageType);
            await commentViewModel.SubmitUserRating(comment, 1);   // This will then generate Upvote_Loaded to highlight the upvote image

            // Update the view
            commentViewModel.ChampionFeedback.SortComments();

        }

        private void Counter_Checked(object sender, RoutedEventArgs e)
        {
            pageType = PageEnum.CommentPage.Counter;
        }

        private void Playing_Checked(object sender, RoutedEventArgs e)
        {
            pageType = PageEnum.CommentPage.Playing;
        }

        private void Name_Written(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            name = textBox.Text;
        }

        private void Name_Focus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            textBox.Text = String.Empty;
        }

        private async void Upvote_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var upvote = (Image)sender;
            var comment = upvote.DataContext as Comment;

            var existingRating = comment.UserRatings.Where(x => x.UniqueUser == commentViewModel.GetDeviceId()).FirstOrDefault();
            //If there was a previous rating, change vote images respectively
            if (existingRating != null && existingRating.Score != 0)
            {
                //Pressing upvote again? Unhighlight the button.
                if (existingRating.Score == 1)
                {
                    upvote.Source = new BitmapImage(new Uri("ms-appx:///Assets/upvoteblank.png", UriKind.Absolute));
                }

                //Going from downvote to upvote? Change to highlighted upvote 
                else if (existingRating.Score == -1)
                {
                    var downvote = ((Image)sender).FindName("DownvoteImage") as Image;
                    downvote.Source = new BitmapImage(new Uri("ms-appx:///Assets/downvoteblank.png", UriKind.Absolute));
                    upvote.Source = new BitmapImage(new Uri("ms-appx:///Assets/upvote.png", UriKind.Absolute));
                }
            }
            //Otherwise, highlight the upvote button
            else
            {
                upvote.Source = new BitmapImage(new Uri("ms-appx:///Assets/upvote.png", UriKind.Absolute));
            }
            await commentViewModel.SubmitUserRating(comment, 1);
        }

        private async void Downvote_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var downvote = (Image)sender;
            var comment = downvote.DataContext as Comment;

            var existingRating = comment.UserRatings.Where(x => x.UniqueUser == commentViewModel.GetDeviceId()).FirstOrDefault();
            if (existingRating != null && existingRating.Score != 0)
            {
                if (existingRating.Score == 1)
                {
                    var upvote = ((Image)sender).FindName("UpvoteImage") as Image;
                    downvote.Source = new BitmapImage(new Uri("ms-appx:///Assets/downvote.png", UriKind.Absolute));
                    upvote.Source = new BitmapImage(new Uri("ms-appx:///Assets/upvoteblank.png", UriKind.Absolute));
                }

                else if (existingRating.Score == -1)
                {
                    downvote.Source = new BitmapImage(new Uri("ms-appx:///Assets/downvoteblank.png", UriKind.Absolute));
                }
            }
            else
            {
                downvote.Source = new BitmapImage(new Uri("ms-appx:///Assets/downvote.png", UriKind.Absolute));

            }

            await commentViewModel.SubmitUserRating(comment, -1);
        }

        private void Upvote_DataLoaded(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            Image upvote = sender as Image;
            var comment = upvote.DataContext as Comment;
            if (comment == null)
                return;

            var existingRating = comment.UserRatings.Where(x => x.UniqueUser == commentViewModel.GetDeviceId()).FirstOrDefault();
            if (existingRating != null)
            {
                if (existingRating.Score == 1)
                {
                    upvote.Source = new BitmapImage(new Uri("ms-appx:///Assets/upvote.png", UriKind.Absolute));
                }
            }
        }

        private void Downvote_DataLoaded(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            Image downvote = sender as Image;
            var comment = downvote.DataContext as Comment;
            if (comment == null)
                return;

            var existingRating = comment.UserRatings.Where(x => x.UniqueUser == commentViewModel.GetDeviceId()).FirstOrDefault();
            if (existingRating != null)
            {
                if (existingRating.Score == -1)
                {
                    downvote.Source = new BitmapImage(new Uri("ms-appx:///Assets/downvote.png", UriKind.Absolute));
                }
            }
        }
       
        private void CounterMessage_Loaded(object sender, RoutedEventArgs e)
        {
            counterMessage = sender as TextBlock;
            if (emptyComments)
                counterMessage.Visibility = Windows.UI.Xaml.Visibility.Visible;
        }

        private void PlayingComments_Loaded(object sender, RoutedEventArgs e)
        {
            playingMessage = sender as TextBlock;
            if (emptyPlayingComments)
                playingMessage.Visibility = Windows.UI.Xaml.Visibility.Visible;
        }

        private void SynergyChampions_Loaded(object sender, RoutedEventArgs e)
        {
            synergyMessage = sender as TextBlock;
            if (emptySynergyChampions)
                synergyMessage.Visibility = Windows.UI.Xaml.Visibility.Visible;
        }

        private void CounterRing_Loaded(object sender, RoutedEventArgs e)
        {
            counterLoadingRing = sender as ProgressRing;
            if (championFeedbackLoaded)
                counterLoadingRing.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

        private void EasyMatchupRing_Loaded(object sender, RoutedEventArgs e)
        {
            easyMatchupLoadingRing = sender as ProgressRing;
            if (championFeedbackLoaded)
                easyMatchupLoadingRing.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

        private void SynergyRing_Loaded(object sender, RoutedEventArgs e)
        {
            synergyLoadingRing = sender as ProgressRing;
            if (championFeedbackLoaded)
                synergyLoadingRing.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

        private void CounterCommentsRing_Loaded(object sender, RoutedEventArgs e)
        {
            counterCommentsLoadingRing = sender as ProgressRing;
            if (championFeedbackLoaded)
                counterCommentsLoadingRing.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

        private void PlayingCommentsRing_Loaded(object sender, RoutedEventArgs e)
        {
            playingCommentsLoadingRing = sender as ProgressRing;
            if (championFeedbackLoaded)
                playingCommentsLoadingRing.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

        private void FeedbackBox_Loaded(object sender, RoutedEventArgs e)
        {
            feedbackBox = sender as TextBox;
        }

        private void NameBox_Loaded(object sender, RoutedEventArgs e)
        {
            nameBox = sender as TextBox;
        }
    }
}
