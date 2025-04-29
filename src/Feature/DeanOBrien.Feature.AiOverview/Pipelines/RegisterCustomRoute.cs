using System.Web.Mvc;
using System.Web.Routing;
using Sitecore.Pipelines;

namespace DeanOBrien.Feature.AiOverview.Pipelines
{
    public class RegisterCustomRoute
    {
        public virtual void Process(PipelineArgs args)
        {
            Register();
        }

        public static void Register()
        {
            RouteTable.Routes.MapRoute("GetAIOverview", "AIOverview/Get/{searchTerm}/",
                new { controller = "AIOverview", action = "GetAIOverview" }
            );
        }
    }
}
