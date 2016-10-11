using Microsoft.WindowsAzure.MobileServices;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using League_Season_5_Counters_Windows_10;
using League_of_Legends_Counterpicks.DataModel;

namespace League_Season_5_Counters_Windows_10.DataModel
{

    public class AdUnit 
    {
        public string Id { get; set; }
        public int Ad { get; set; }
        public string App { get; set; }
        public DateTime LastUseddate { get; set; }
        public bool InUse { get; set; }
    }

    public static class AdData
    {
        private static MobileServiceClient _client = App.MobileService;
        public static IMobileServiceTable<AdUnit> AdTable { get; set; }
        public static IMobileServiceTable<ChampionFeedback> Table { get; set; }

        private static int AdId;

        static AdData()
        {
            AdTable = _client.GetTable<AdUnit>();
            Table = _client.GetTable<ChampionFeedback>();
        }

        public async static Task<int> GetAdId()
        {
            if (AdId != 0)
                return AdId;

            var result = await AdTable.Where(x => x.InUse && x.App == App.AppId).ToListAsync();
            AdId = result.FirstOrDefault().Ad;
            return AdId;
        }
    }
}
