using DeanOBrien.Feature.AiOverview.Extensions;
using DeanOBrien.Feature.AiOverview.Helpers;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.DependencyInjection;
using Sitecore.Diagnostics;
using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace DeanOBrien.Feature.AiOverview.Tasks
{
    public class OwnDataTask
    {
        private Item _dataSourceItem;
        private int _maxItems;
        private bool _forceUpdate;
        private Database _master;
        private string[] _templates;
        private IOwnDataHelper _ownDataHelper;

        public void Execute(Item[] items, Sitecore.Tasks.CommandItem command, Sitecore.Tasks.ScheduleItem schedule)
        {
            Log.Info("Own Data Task: Started", this);

            Initialize();

            Func<Item, bool> templatePredicate = item =>
                        _templates.Any(templateName => item.ImplementsTemplateId(templateName));

            if (items != null && items.Any())
            {
                foreach (var item in items)
                {
                    var itemsToProcess = item.Axes.GetDescendants().Where(templatePredicate).Take(_maxItems).ToList();
                    Log.Info($"Own Data: Max items={_maxItems} \r\n item count = {itemsToProcess.Count()} \r\n Templates = {_dataSourceItem["Templates To Include"].ToString()}", this);

                    foreach (var itemToProcess in itemsToProcess)
                    {
                        Log.Info($"Own Data: itemToProcess = {itemToProcess.ID.ToString()}", this);

                        _ownDataHelper.AddItemToStorage(itemToProcess.ID.ToString(), _dataSourceItem);
                    }
                }
            }

            Log.Info("Own Data Task: Complete", this);
        }

        private void Initialize()
        {
            _master = Sitecore.Configuration.Factory.GetDatabase("master");
            var settings = _master.GetItem("/sitecore/system/Modules/AI Language Assistant");

            if (string.IsNullOrWhiteSpace(settings["Default Data Source"]))
            {
                Log.Info("PROBLEM: Own Data Task - No default data source specified", this);
                return;
            }
            var dataSource = settings["Default Data Source"];

            var dataSources = _master.GetItem("/sitecore/system/Modules/AI Language Assistant/Data Sources");
            if (dataSources == null)
            {
                Log.Info("PROBLEM: Own Data Task - No data sources", this);
                return;
            }

            _dataSourceItem = dataSources.Children.Where(x => x.DisplayName == dataSource).FirstOrDefault();
            if (_dataSourceItem == null) _dataSourceItem = dataSources.Children.FirstOrDefault();

            if (_dataSourceItem == null)
            {
                Log.Info("PROBLEM: Own Data Task - Failed to configure data source item", this);
                return;
            }

            _forceUpdate = _dataSourceItem["Force Update"] == "1";
            _maxItems = 10000;

            if (!string.IsNullOrWhiteSpace(_dataSourceItem["Max Items"]))
            {
                try
                {
                    _maxItems = Convert.ToInt32(_dataSourceItem["Max Items"]);
                }
                catch (Exception ex)
                {
                    Log.Error("PROBLEM: Own Data Task - " + ex.Message, this);
                }
            }

            _templates = _dataSourceItem["Templates To Include"].Split('|');
            _ownDataHelper = ServiceLocator.ServiceProvider.GetService<IOwnDataHelper>();

        }
    }
}
