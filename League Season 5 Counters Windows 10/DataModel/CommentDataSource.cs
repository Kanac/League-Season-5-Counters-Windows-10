using Microsoft.WindowsAzure.MobileServices;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using Windows.System.UserProfile;
using League_of_Legends_Counterpicks.Data;

namespace League_of_Legends_Counterpicks.DataModel
{

    //Comment Model
    public class PageEnum
    {
        public enum CommentPage
        {
            Counter,
            Playing,
        }

        public enum ClientChampionPage
        {
            Counter,
            EasyMatchup,
            Synergy,
        }

        public enum ChampionPage
        {
            Counter,
            Synergy,
        }


    }

    public class ChampionFeedback : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public ChampionFeedback()
        {
            Comments = new ObservableCollection<Comment>();
            Counters = new ObservableCollection<Counter>();
            EasyMatchups = new ObservableCollection<Counter>();
        }
        
        public string Name { get; set; }
        public ObservableCollection<Comment> Comments { get; set; }
        public ObservableCollection<Counter> Counters { get; set; }
        public ObservableCollection<Counter> EasyMatchups { get; set; }


        public string Id { get; set; }

        public void SortComments()
        {
            var sorted = Comments.OrderByDescending(x => x.Score).ToList();
            for (int i = 0; i < sorted.Count(); i++)
                Comments.Move(Comments.IndexOf(sorted[i]), i);

            NotifyPropertyChanged("Comments");
        }

        public void SortCounters()
        {
            var sorted = Counters.Where(c => c.Page == PageEnum.ChampionPage.Counter).OrderByDescending(x => x.Score).ToList();
            for (int i = 0; i < sorted.Count(); i++)
            {
                Counters.Move(Counters.IndexOf(sorted[i]), i);
                Counters.ElementAt(i).Value = (sorted.Count() - i) * 100 / sorted.Count();
            }
            NotifyPropertyChanged("Counters");
        }

        public void SortEasyMatchups() {
            var sorted = EasyMatchups.OrderByDescending(x => x.Score).ToList();
            for (int i = 0; i < sorted.Count(); i++)
            {
                EasyMatchups.Move(EasyMatchups.IndexOf(sorted[i]), i);
                EasyMatchups.ElementAt(i).Value = (EasyMatchups.Count() - i) * 100 / EasyMatchups.Count();
            }
            NotifyPropertyChanged("EasyMatchups");
        }

        public void SortSynergy()
        {
            var sorted = Counters.Where(c => c.Page == PageEnum.ChampionPage.Synergy).OrderByDescending(x => x.Score).ToList();
            for (int i = 0; i < sorted.Count(); i++)
            {
                Counters.Move(Counters.IndexOf(sorted[i]), i);
                Counters.ElementAt(i).Value = (sorted.Count() - i) * 100 / sorted.Count();
            }
            NotifyPropertyChanged("Counters");
        }

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

    }

    public class Comment : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public Comment() {
            UserRatings = new ObservableCollection<UserRating>();
        }
        public string Text { get; set; }
        public string User { get; set; }
        public string ChampionFeedbackName { get; set; }
        private int score { get; set; }
        public int Score { get 
        {
            return score;
        }
            set {
                if (value != score) {
                    score = value;
                    NotifyPropertyChanged("Score");
                }
                    
            }
        }

        public PageEnum.CommentPage Page { get; set; }
        public ICollection<UserRating> UserRatings { get; set; }
        public string Id { get; set; }
        public string ChampionFeedbackId { get; set; }
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
    }


    public class Counter : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public Counter()
        {
            CounterRatings = new ObservableCollection<CounterRating>();
        }
        public int Value { get; set; }  //Local value to be used to set progress bar percentage
        public string Name { get; set; }
        public string ChampionFeedbackName { get; set; }
        public PageEnum.ChampionPage Page { get; set; }
        private int score { get; set; }
        public int Score
        {
            get
            {
                return score;
            }
            set
            {
                if (value != score)
                {
                    score = value;
                    NotifyPropertyChanged("Score");
                }

            }
        }

        public ICollection<CounterRating> CounterRatings { get; set; }
        public string Id { get; set; }
        public string ChampionFeedbackId { get; set; }
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
    }

    public class UserRating 
    {
        public string UniqueUser { get; set; }
        public int Score { get; set; }
        public string Id { get; set; }
        public string CommentId { get; set; }
    }

    public class CounterRating
    {
        public string UniqueUser { get; set; }
        public int Score { get; set; }
        public string Id { get; set; }
        public string CounterId { get; set; }
    }


    // Comment View Model
    class CommentDataSource : INotifyPropertyChanged
    {
        MobileServiceClient _client;

        public CommentDataSource(MobileServiceClient client)
        {
            _client = client;
            champTable = _client.GetTable<ChampionFeedback>();
            commentTable = _client.GetTable<Comment>();
            userTable = _client.GetTable<UserRating>();
            counterTable = _client.GetTable<Counter>();
            counterRatingTable = _client.GetTable<CounterRating>();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this,
                    new PropertyChangedEventArgs(propertyName));
            }
        }

        // View model properties

        // This value will change as user changes champions 
        // This is to prevent excess loading of all champion data 
        private ChampionFeedback _ChampionFeedback;
        public ChampionFeedback ChampionFeedback
        {
            get { return _ChampionFeedback; }
            set
            {
                _ChampionFeedback = value;
                NotifyPropertyChanged("ChampionFeedback");
            }
        }

        public String DeviceId { get; set; }

        private bool _IsPending;
        public bool IsPending
        {
            get { return _IsPending; }
            set
            {
                _IsPending = value;
                NotifyPropertyChanged("IsPending");
            }
        }

        private string _ErrorMessage = null;
        public string ErrorMessage
        {
            get { return _ErrorMessage; }
            set
            {
                _ErrorMessage = value;
                NotifyPropertyChanged("ErrorMessage");
            }
        }

        public IMobileServiceTable<ChampionFeedback> champTable { get; set; }
        public IMobileServiceTable<Comment> commentTable { get; set; }
        public IMobileServiceTable<UserRating> userTable { get; set; }
        public IMobileServiceTable<Counter> counterTable { get; set; }
        public IMobileServiceTable<CounterRating> counterRatingTable { get; set; }



        // Service operations

       

        //public async Task SeedDataAsync(Role allChampions)
        //{
        //    IsPending = true;
        //    ErrorMessage = null;

        //    try
        //    {
        //        var champFeedbacks = await champTable.Take(1000).ToListAsync();

        //        foreach (Champion champion in allChampions.Champions)
        //        {
        //            ChampionFeedback champFeedback = champFeedbacks.Where(c => c.Name == champion.UniqueId).FirstOrDefault();
        //            //If the champion feedback does not exist, create it
        //            if (champFeedback == null)
        //            {
        //                champFeedback = new ChampionFeedback()
        //                {
        //                    Name = champion.UniqueId,
        //                    Id = Guid.NewGuid().ToString(),
        //                };

        //                await champTable.InsertAsync(champFeedback);
        //            }

        //            // Check if the champion feedback contains counters yet. If not, create them.
        //            if (champFeedback.Counters.Where(x => x.Page == PageEnum.ChampionPage.Counter).Count() == 0)
        //            {
        //                foreach (string counter in champion.Counters)
        //                {
        //                    var newCounter = new Counter()
        //                    {
        //                        Name = counter,
        //                        ChampionFeedbackId = ChampionFeedback.Id,
        //                        ChampionFeedbackName = ChampionFeedback.Name,
        //                        Score = 7 - champion.Counters.IndexOf(counter),
        //                        Page = PageEnum.ChampionPage.Counter,
        //                        Id = Guid.NewGuid().ToString(),
        //                    };

        //                    await counterTable.InsertAsync(newCounter);
        //                }

        //            }

        //            // Check if the champion feedback contains easy matchups yet. If not, create them.
        //            if (champFeedback.Counters.Where(x => x.Page == PageEnum.ChampionPage.EasyMatchup).Count() == 0) {
        //                // Iterate through all the counters and select the ones where the current champion has this champion as a counter (easy matchup in the reverse relationship)
        //                var easyMatchups = champFeedbacks.SelectMany(c => c.Counters).Where(x => x.Name == champion.UniqueId);
                       
        //                // The counter is this champion everytime. We want the champion feedback to whom the counter is pertaining to, to get the easy matchup. 
        //                // Essentially reverse the relationship of counter to champion feedback but with the other properties the same
        //                foreach (var easyMatchup in easyMatchups) {
        //                    var newEasyMatchup = new Counter()
        //                    {
        //                        Name = easyMatchup.ChampionFeedbackName,  
        //                        ChampionFeedbackId = easyMatchup.Id, 
        //                        ChampionFeedbackName = easyMatchup.Name, 
        //                        Score = easyMatchup.Score,
        //                        Page = PageEnum.ChampionPage.EasyMatchup,
        //                        CounterRatings = easyMatchup.CounterRatings,
        //                        Id = Guid.NewGuid().ToString(),
        //                    };

        //                    await counterTable.InsertAsync(newEasyMatchup);
        //                }
                    
        //            }
        //        }

        //    }
        //    catch (MobileServiceInvalidOperationException ex)
        //    {
        //        ErrorMessage = ex.Message;
        //    }
        //    catch (HttpRequestException ex2)
        //    {
        //        ErrorMessage = ex2.Message;
        //    }
        //    finally
        //    {
        //        IsPending = false;
        //    }
        //}


        public async Task GetChampionFeedbackAsync(string championName)
        {
            IsPending = true;
            ErrorMessage = null;

            try
            {
                var result = await champTable.Where(c => c.Name == championName).ToListAsync();
                if (result.Count != 0)
                {   // If there was a match found for champion, 
                    ChampionFeedback = result.FirstOrDefault();    // Get the first (and only) result
                    ChampionFeedback.SortComments();
                    ChampionFeedback.SortCounters();

                    //Find where this champion is listed as a counter to another champion (easy matchup in reverse relationship)
                    ChampionFeedback.EasyMatchups = new ObservableCollection<Counter>(await counterTable.Where(x => x.Name == championName && x.Page == PageEnum.ChampionPage.Counter).ToListAsync());
                    ChampionFeedback.SortEasyMatchups();

                    //The counter collection of this champion only contains synergies where the champion is the parent. Add the synergies where the champion is a child.
                    var synergy = await counterTable.Where(c => c.Name == championName && c.Page == PageEnum.ChampionPage.Synergy).ToListAsync();
                    foreach (var synergyChampion in synergy)
                        ChampionFeedback.Counters.Add(synergyChampion);

                    ChampionFeedback.SortSynergy();
                }
            }
            catch (MobileServiceInvalidOperationException ex)
            {
                ErrorMessage = ex.Message;
            }
            catch (HttpRequestException ex2)
            {
                ErrorMessage = ex2.Message;
            }
            finally
            {
                IsPending = false;
            }

        }


        public async Task<Comment> SubmitCommentAsync(String text, String name, PageEnum.CommentPage type)
        {
            IsPending = true;
            ErrorMessage = null;

            // Create the new comment
            var comment = new Comment()
            {
                Score = 0,
                Text = text,
                User = name,
                Page = type,
                ChampionFeedbackName = ChampionFeedback.Name,
                ChampionFeedbackId = ChampionFeedback.Id,
                Id = Guid.NewGuid().ToString(),
            };

            try
            {
                this.ChampionFeedback.Comments.Add(comment);
                await commentTable.InsertAsync(comment);
                return comment;

            }
            catch (MobileServiceInvalidOperationException ex)
            {
                ErrorMessage = ex.Message;
                return null;
            }
            catch (HttpRequestException ex2)
            {
                ErrorMessage = ex2.Message;
                return null;
            }
            finally
            {
                IsPending = false;
            }
        }

        public async Task SubmitUserRating(Comment comment, int score) {

            IsPending = true;
            ErrorMessage = null;

            // Check if the user already rated the comment
            var existingRating = comment.UserRatings.Where(x => x.UniqueUser == GetDeviceId()).FirstOrDefault();
            // If already rated, update to the new score 
            if (existingRating != null) {
                //Case for pressing the opposite vote button -- change rating to it
                if (existingRating.Score != score){
                    existingRating.Score = score;
                }
                // Case for pressing the same vote button -- simply a score of 0
                else {
                    existingRating.Score = 0;
                }
                try { await userTable.UpdateAsync(existingRating); }
                catch (Exception e) { ErrorMessage = e.Message; }
            }
            // Create a new rating otherwise
            else{
                var newRating = new UserRating()
                {
                    Score = score,
                    UniqueUser = GetDeviceId(),
                    Id = Guid.NewGuid().ToString(),
                    CommentId = comment.Id,
                };

                // Update the comment 
                comment.UserRatings.Add(newRating);
                try { await userTable.InsertAsync(newRating); }
                catch (Exception e) { ErrorMessage = e.Message; }

            }
            try
            {
                var updatedComment = await commentTable.LookupAsync(comment.Id);
                comment.Score = updatedComment.Score;
                comment.UserRatings = updatedComment.UserRatings;
            }
            catch (MobileServicePreconditionFailedException ex)
            {
                ErrorMessage = ex.Message;
            }
            // Server conflict 
            catch (MobileServiceInvalidOperationException ex1)
            {
                ErrorMessage = ex1.Message;
            }
            catch (HttpRequestException ex2)
            {
                ErrorMessage = ex2.Message;
            }

            finally
            {

                IsPending = false;
            }

        }

        public async Task<Counter> SubmitCounter(Champion champion, PageEnum.ClientChampionPage? page) { 
            IsPending = true;
            ErrorMessage = null;

            //Convert the champion to a champion feedback
            var result = await champTable.Where(c => c.Name == champion.UniqueId).ToListAsync();
            var submitChamp = result.FirstOrDefault();

            // Create the new counter
            var counter = new Counter()
            {
                Score = 0,
                Id = Guid.NewGuid().ToString(),
            };

            //Swap names around for easy matchup 
            if (page == PageEnum.ClientChampionPage.EasyMatchup){
                counter.ChampionFeedbackName = submitChamp.Name;
                counter.ChampionFeedbackId = submitChamp.Id;
                counter.Name = ChampionFeedback.Name;
                counter.Page = PageEnum.ChampionPage.Counter;
                this.ChampionFeedback.EasyMatchups.Add(counter);
            }

            //Handle the counter and synergy submissions normally
            else{

                counter.ChampionFeedbackName = ChampionFeedback.Name;
                counter.ChampionFeedbackId = ChampionFeedback.Id;
                counter.Name = submitChamp.Name;

                if (page == PageEnum.ClientChampionPage.Counter)
                    counter.Page = PageEnum.ChampionPage.Counter;
                else
                    counter.Page = PageEnum.ChampionPage.Synergy;

                this.ChampionFeedback.Counters.Add(counter);
            }
            try
            {
                await counterTable.InsertAsync(counter);
                return counter;

            }
            catch (MobileServiceInvalidOperationException ex)
            {
                ErrorMessage = ex.Message;
                return null;
            }
            catch (HttpRequestException ex2)
            {
                ErrorMessage = ex2.Message;
                return null;
            }
            finally
            {
                IsPending = false;
            }
        }


        public async Task SubmitCounterRating(Counter counter, int score)
        {

            IsPending = true;
            ErrorMessage = null;

            //  Check if the user already rated the counter
            var existingRating = counter.CounterRatings.Where(x => x.UniqueUser == GetDeviceId()).FirstOrDefault();
            // If already rated, update to the new score 
            if (existingRating != null){
                // Case for pressing the opposite vote button -- change rating to it
                if (existingRating.Score != score){
                    existingRating.Score = score;
                }
                // Case for pressing the same vote button -- simply a score of 0
                else{
                    existingRating.Score = 0;
                }
                // Update the rating in database
                try { await counterRatingTable.UpdateAsync(existingRating); }
                catch (Exception e) { ErrorMessage = e.Message; }
            }
            // Create a new rating otherwise
            else{
                var newRating = new CounterRating(){
                    Score = score,
                    UniqueUser = GetDeviceId(),
                    Id = Guid.NewGuid().ToString(),
                    CounterId = counter.Id,
                };

                // Update the counter 
                counter.CounterRatings.Add(newRating);
                // Insert rating into database
                try { await counterRatingTable.InsertAsync(newRating); }
                catch (Exception e) { ErrorMessage = e.Message; }
            }
            try{
                var updatedCounter = await counterTable.LookupAsync(counter.Id);
                counter.Score = updatedCounter.Score;
                counter.CounterRatings = updatedCounter.CounterRatings;
            }
            catch (MobileServicePreconditionFailedException ex)
            {
                ErrorMessage = ex.Message;
            }
            // Server conflict 
            catch (MobileServiceInvalidOperationException ex1)
            {
                ErrorMessage = ex1.Message;
            }
            catch (HttpRequestException ex2)
            {
                ErrorMessage = ex2.Message;
            }

            finally
            {

                IsPending = false;
            }

        }

        public string GetDeviceId()
        {
            if (DeviceId != null)
                return DeviceId;

            var token = Windows.System.Profile.HardwareIdentification.GetPackageSpecificToken(null);
            var hardwareId = token.Id;
            var dataReader = Windows.Storage.Streams.DataReader.FromBuffer(hardwareId);

            byte[] bytes = new byte[hardwareId.Length];
            dataReader.ReadBytes(bytes);

            DeviceId =  BitConverter.ToString(bytes).Replace("-", "");
            return DeviceId;
        }
    }
}
