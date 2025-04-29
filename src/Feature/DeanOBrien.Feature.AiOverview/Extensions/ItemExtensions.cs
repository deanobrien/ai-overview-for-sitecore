using Sitecore.Data;
using Sitecore.Data.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeanOBrien.Feature.AiOverview.Extensions
{
    public static class ItemExtensions
    {
        public static bool ImplementsTemplateId(this Item item, string templateId)
        {
            if (string.IsNullOrEmpty(templateId))
                return true;

            var TemplateId = ID.Parse(templateId);


            bool result = false;

            if (item != null)
            {
                if (item.TemplateID.Equals(TemplateId))
                {
                    result = true;
                }
                else
                {
                    var template = Sitecore.Data.Managers.TemplateManager.GetTemplate(item);

                    if (template != null)
                    {
                        var baseTemplates = template.GetBaseTemplates();

                        if (baseTemplates != null)
                            result = baseTemplates.Any(x => x.ID.Equals(TemplateId));
                    }
                }
            }
            return result;
        }
    }
}
