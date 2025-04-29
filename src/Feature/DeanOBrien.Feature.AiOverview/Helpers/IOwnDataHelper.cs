using Sitecore.Data.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeanOBrien.Feature.AiOverview.Helpers
{
    public interface IOwnDataHelper
    {
        void AddItemToStorage(string id, Item dataSourceItem = null, int allowedTimespanInHours = 168);
    }
}
