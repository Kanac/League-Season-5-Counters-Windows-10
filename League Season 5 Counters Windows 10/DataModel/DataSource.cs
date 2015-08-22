using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Storage;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using QKit.JumpList;
using QKit;
using Windows.UI.Xaml.Data;


// The data model defined by this file serves as a representative example of a strongly-typed
// model.  The property names chosen coincide with data bindings in the standard champion templates.
//
// Applications may use this model as a starting point and build on it, or discard it entirely and
// replace it with something appropriate to their needs. If using this model, you might improve app 
// responsiveness by initiating the data loading task in the code behind for App.xaml when the app 
// is first launched.

namespace League_of_Legends_Counterpicks.Data
{
    /// <summary>
    /// Generic champion data model.
    /// </summary>
    /// 

    public class Champion
    {
        public Champion(String uniqueId, String imagePath, ObservableCollection<string> Counters)
        {
            this.UniqueId = uniqueId;
            this.ImagePath = imagePath;
            this.Counters = Counters;
        }

        public string UniqueId { get; private set; }
        public string ImagePath { get; private set; }
        public ObservableCollection<string> Counters { get; private set; }
    }

    /// <summary>
    /// Generic role data model.
    /// </summary>
    public class Role
    {
        public Role(String uniqueId)
        {
            this.UniqueId = uniqueId;
            this.Champions = new ObservableCollection<Champion>();
            this.GroupedChampions = new CollectionViewSource();
            this.GroupedChampions.IsSourceGrouped = true;
        }

        public string UniqueId { get; private set; }
        public ObservableCollection<Champion> Champions { get; set; }
        public List<JumpListGroup<Champion>> QkitChampions { get; set; }
        public CollectionViewSource GroupedChampions { get; set; } 
    }

    /// <summary>
    /// Creates a collection of roles and champions with content read from a static json file.
    /// 
    /// SampleDataSource initializes with data read from a static json file included in the 
    /// project.  This provides sample data at both design-time and run-time.
    /// </summary>
    public sealed class DataSource
    {
        private static DataSource _DataSource = new DataSource();         //static property of class itself allows its properties and methods to be referenced 
        private ObservableCollection<Role> _roles = new ObservableCollection<Role>();        //Initalize a collection of roles
        public ObservableCollection<Role> Roles
        {
            get { return this._roles; }
        }

        public static async Task<ObservableCollection<Role>> GetRolesAsync()
        {
            await _DataSource.GetDataAsync();

            return _DataSource.Roles;
        }

        public static Role GetRole(string uniqueId)
        {
            // Simple linear search is acceptable for small data sets
            var matches = _DataSource.Roles.Where(role => role.UniqueId.Equals(uniqueId));       //Cycle through the collection of roles, returning first role that matches the ID (Lambda expression)
            if (matches.Count() == 1) return matches.First();
            return null;
        }

        public static string GetRoleId(string champId)
        {
            foreach (var role in _DataSource.Roles)
            {
                foreach (var champion in role.Champions)
                {
                    if (champion.UniqueId.ToLower() == champId.ToLower() && role.UniqueId != "All")
                        return role.UniqueId;
                }
            }
            return null;
        }


        public static Champion GetChampion(string champId)
        {
            // Simple linear search is acceptable for small data sets
            var matches = new List<Champion>();
            foreach (var role in _DataSource.Roles)
            {
                foreach (var champion in role.Champions)
                {
                    if (champion.UniqueId.ToLower().Equals(champId.ToLower()) && role.UniqueId != "All")
                        return champion;
                }
            }
            return null;

        }
        public static Role FilterChampions(string filter)
        {
            var matches = new Role(filter);
            foreach (var role in _DataSource.Roles)
            {
                foreach (var champion in role.Champions)
                {
                    if (champion.UniqueId.ToLower().StartsWith(filter.ToLower()) && role.UniqueId != "All")
                        matches.Champions.Add(champion);
                }
            }
            matches.Champions = new ObservableCollection<Champion>(matches.Champions.OrderBy(p => p.UniqueId));
            return matches;
        }

        public static Role GetAllChampions()
        {
            var allrole = GetRole("All");  // Will be the empty but existant if first time calling this function 

            if (allrole.Champions.Count == 0)  //For first time fetch of all role
            {
                foreach (var role in _DataSource.Roles)
                {
                    foreach (var champion in role.Champions)
                        allrole.Champions.Add(champion);
                }

                allrole.Champions = new ObservableCollection<Champion>(allrole.Champions.OrderBy(p => p.UniqueId));
            }
            
            return allrole;
        }

        private async Task GetDataAsync()
        {
            if (this.Roles.Count != 0)            //HERE IS THE ANSWER. IF LOADED, DO NOT LOAD AGAIN
                return;

            Uri dataUri = new Uri("ms-appx:///DataModel/Data.json");      //Get location of data 
            StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(dataUri);       //Get the file from where the data is located

            string jsonText = await FileIO.ReadTextAsync(file);     //Returns the json text in which it was saved as 
            JsonObject jsonObject = JsonObject.Parse(jsonText);     //Parse the json text into object 
            JsonArray jsonArray = jsonObject["Roles"].GetArray();

            foreach (JsonValue roleValue in jsonArray)
            {
                JsonObject roleObject = roleValue.GetObject();
                Role role = new Role(roleObject["UniqueId"].GetString());

                foreach (JsonValue championValue in roleObject["Champions"].GetArray())
                {
                    JsonObject championObject = championValue.GetObject();
                    var counterList = new ObservableCollection<String>();

                    foreach (JsonValue counterValue in championObject["Counters"].GetArray())
                    {
                        var counterString = counterValue.GetString();
                        counterList.Add(counterString);
                    }
                    role.Champions.Add(new Champion(championObject["UniqueId"].GetString(),
                                                       championObject["ImagePath"].GetString(),
                                                       counterList));


                }
                this.Roles.Add(role);
            }

            GetAllChampions(); //Fills up the all role immediately with json data serialization
        }

    }

}
