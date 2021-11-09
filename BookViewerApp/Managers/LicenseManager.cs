using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.ApplicationModel.Store;
using Windows.Services.Store;
using System.Threading;

namespace BookViewerApp.Managers;

public static class LicenseManager
{
    //http://bluewatersoft.cocolog-nifty.com/blog/2017/04/uwp-iap-windows.html

    public static LicenseInformation CurrentLicenseInformation
    {
        get
        {
#if DEBUG
            return CurrentAppSimulator.LicenseInformation;
#else
                return CurrentApp.LicenseInformation;
#endif
        }
    }

    public static bool IsActive(ProductListing product)
    {
        CurrentLicenseInformation.ProductLicenses.TryGetValue(product.ProductId, out var license);
        return license?.IsActive == true;
    }

    static LicenseManager()
    {
        Initialize().ConfigureAwait(false);
    }

    static SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

    static bool initialized = false;

    public static async Task Initialize()
    {
        if (initialized) return;
#if DEBUG
        await semaphore.WaitAsync();
        try
        {
            if (initialized) return;
            var proxyFile = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(
                new Uri("ms-appx:///res/values/WindowsStoreProxy.xml")
            );
            await CurrentAppSimulator.ReloadSimulatorAsync(proxyFile);
        }
        finally
        {
            semaphore.Release();
        }
#endif
        initialized = true;
    }

    public static async Task<ListingInformation> GetListingAsync()
    {
        await semaphore.WaitAsync();
        try
        {
#if DEBUG
            return await CurrentAppSimulator.LoadListingInformationAsync();
#else
                return await CurrentApp.LoadListingInformationAsync();
#endif
        }
        finally
        {
            semaphore.Release();
        }
    }

    public static async Task<PurchaseResults> RequestPurchaseAsync(ProductListing product)
    {
        await semaphore.WaitAsync();
        try
        {
#if DEBUG
            return await CurrentAppSimulator.RequestProductPurchaseAsync(product.ProductId);
#else
                return await CurrentApp.RequestProductPurchaseAsync(product.ProductId);
#endif
        }
        finally
        {
            semaphore.Release();
        }
    }

    public static bool ProductIsActive(ProductListing product)
    {
        return CurrentLicenseInformation.ProductLicenses[product.ProductId].IsActive;
    }
}
