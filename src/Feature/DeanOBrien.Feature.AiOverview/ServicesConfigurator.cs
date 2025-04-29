using DeanOBrien.Feature.AiOverview.Helpers;
using DeanOBrien.Foundation.DataAccess.AiOverview.Services;
using Microsoft.Extensions.DependencyInjection;
using Sitecore.DependencyInjection;

namespace DeanOBrien.Feature.AiOverview
{
    public class ServicesConfigurator : IServicesConfigurator
    {
        public void Configure(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<IGenAIService, GenAIService>();
            serviceCollection.AddScoped<IOwnDataHelper, OwnDataHelper>();
            serviceCollection.AddScoped<IDataLakeService, DataLakeService>();
        }
    }
}
