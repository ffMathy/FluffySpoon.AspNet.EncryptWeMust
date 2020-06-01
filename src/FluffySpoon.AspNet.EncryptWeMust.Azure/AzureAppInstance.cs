using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.AppService.Fluent.Models;
using Microsoft.Azure.Management.Fluent;

namespace FluffySpoon.AspNet.LetsEncrypt.Azure
{
    /// <summary>
    /// A reference to an Azure app, which might be either a simple app, or a deployment slot
    /// </summary>
    public class AzureAppInstance
    {
        readonly IWebApp _app;
        readonly IDeploymentSlot _slot;

        public AzureAppInstance(IWebApp app)
        {
            _app = app;
        }

        public AzureAppInstance(IWebApp app, IDeploymentSlot slot)
        {
            _app = app;
            _slot = slot;
        }
        
        /// <summary>
        /// A general purpose name for this instance
        /// </summary>
        public string DisplayName => IsSlot ? $"{_app.Name}/{_slot.Name}" : _app.Name;

        /// <summary>
        /// Get the host names associated with this instance
        /// </summary>
        public ISet<string> HostNames => IsSlot ? _slot.HostNames : _app.HostNames;

        private bool IsSlot => _slot != null;

        public Task<HostNameBindingInner> GetHostNameBindingAsync(IAzure client, string resourceGroupName, string domain)
        {
            if (IsSlot)
            {
                return client.WebApps.Inner.GetHostNameBindingSlotAsync(resourceGroupName,
                    _app.Name,
                    _slot.Name,
                    domain);
            }
            else
            {
                return client.WebApps.Inner.GetHostNameBindingAsync(resourceGroupName,
                    _app.Name,
                    domain);
            }
        }
        
        public Task SetHostNameBindingAsync(IAzure client, string resourceGroupName, string domain, HostNameBindingInner binding)
        {
            if (IsSlot)
            {
                return client.WebApps.Inner.CreateOrUpdateHostNameBindingSlotWithHttpMessagesAsync(
                    resourceGroupName,
                    _app.Name,
                    domain,
                    binding,
                    _slot.Name);
            }
            else
            {
                return client.WebApps.Inner.CreateOrUpdateHostNameBindingWithHttpMessagesAsync(
                    resourceGroupName,
                    _app.Name,
                    domain,
                    binding
                );
            }
        }

        public Task<IReadOnlyDictionary<string, IAppSetting>> GetAppSettings()
        {
            if (IsSlot)
            {
                return _slot.GetAppSettingsAsync();
            }
            else
            {
                return _app.GetAppSettingsAsync();
            }
        }

        public Task UpdateAppSettingAsync(string key, string value)
        {
            if (IsSlot)
            {
                return _slot.Update()
                    .WithAppSetting(key, value)
                    .ApplyAsync();
            }
            else
            {
                return _app.Update()
                    .WithAppSetting(key, value)
                    .ApplyAsync();
            }
        }
        
        public string RegionName => IsSlot ? _slot.RegionName : _app.RegionName;
    }
}