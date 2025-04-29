using DeanOBrien.Foundation.DataAccess.AiOverview.Services;
using Sitecore.Data.Fields;
using Sitecore.Data;
using Sitecore.Diagnostics;
using System;
using System.Collections.Generic;
using System.Text;
using Sitecore.Data.Items;

namespace DeanOBrien.Feature.AiOverview.Helpers
{
    public class OwnDataHelper : IOwnDataHelper
    {
        private readonly IDataLakeService _dataLakeService;
        private readonly Database _master;
        private string _accountName;
        private string _fileSystemName;
        private string _accountKey;

        public OwnDataHelper(IDataLakeService dataLakeService)
        {
            Log.Info("OwnDataHelper()", "OwnDataHelper");
            _dataLakeService = dataLakeService;
            _master = Sitecore.Configuration.Factory.GetDatabase("master");
        }

        public void AddItemToStorage(string id, Item dataSourceItem = null, int allowedTimespanInHours = 168)
        {
            try
            {
                if (dataSourceItem == null)
                {
                    Log.Info("AddItemToStorage - Failed no data source item", this);
                    return;
                }

                _accountName = dataSourceItem["Storage Account Name"];
                _fileSystemName = dataSourceItem["Storage File System Name"];
                _accountKey = dataSourceItem["Storage Account Key"];

                id = id.ToLower().Replace("{", "").Replace("}", "");

                Dictionary<string, string> metadata = new Dictionary<string, string>();
                var item = _master.GetItem(id);

                if (item == null)
                {
                    Log.Info("AddItemToStorage - Failed item not in the master database", this);
                    return;
                }
                var title = item.DisplayName;
                var content = item["Content"];
                var category = "";
                DateField dateField;
                DateTime articleDate = DateTime.MinValue; ;

                if (!string.IsNullOrWhiteSpace(item["Article Date"]))
                {
                    dateField = item.Fields["Article Date"];
                }
                else
                {
                    dateField = item.Fields["__Created"];
                }
                if (dateField != null)
                {
                    articleDate = Sitecore.DateUtil.ToServerTime(dateField.DateTime);
                }
                string date = string.Format("{0} {1} {2}", articleDate.Day, articleDate.ToString("MMMM"), articleDate.Year);

                category = item.TemplateName;
                content = string.Format("<html><body><h1>{0}</h1>{1}<div><h2>Published: {2}</h2><h3>Category: {3}</h3></div></body></html>", title, content, date, category);
                
                metadata.Add("Category", category);
                metadata.Add("Date", date);
                var destinationPathTrail = string.Format("/Items/{0}.html", id);
                byte[] bytes = Encoding.ASCII.GetBytes(content);

                _dataLakeService.UpdateConnection(_accountName, _fileSystemName, _accountKey);
                _dataLakeService.uploadFileFromStream(destinationPathTrail, bytes, metadata);
            }
            catch (Exception ex)
            {
                Log.Info($"AddItemToStorage - Failed: {ex.Message}", this);
            }
        }
    }
}
