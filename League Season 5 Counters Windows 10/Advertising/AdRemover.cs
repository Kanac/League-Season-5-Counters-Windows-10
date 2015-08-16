using League_Season_5_Counters_Windows_10;
using System;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Store;
using Windows.Storage;

namespace League_of_Legends_Counterpicks.Advertisement
{
    class AdRemover
    {
        public static async void Purchase()
        {
            if (!App.licenseInformation.ProductLicenses["AdRemoval"].IsActive)
            {
                try
                {
#if DEBUG
                    StorageFolder installFolder = await Package.Current.InstalledLocation.GetFolderAsync("Assets");
                    StorageFile appSimulatorStorageFile = await installFolder.GetFileAsync("WindowsStoreProxy.xml");
                    await CurrentAppSimulator.ReloadSimulatorAsync(appSimulatorStorageFile);
                    PurchaseResults result = await CurrentAppSimulator.RequestProductPurchaseAsync("AdRemoval");
#else
                    PurchaseResults result = await CurrentApp.RequestProductPurchaseAsync("AdRemoval");
#endif
                }
                catch (Exception)
                {
                    // handle error or do nothing
                }
            }
        }

    }
}
