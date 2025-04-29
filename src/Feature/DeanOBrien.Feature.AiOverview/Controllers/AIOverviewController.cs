using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using DeanOBrien.Foundation.DataAccess.AiOverview.Models;
using DeanOBrien.Foundation.DataAccess.AiOverview.Services;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.DependencyInjection;
using Sitecore.Mvc.Extensions;
using Sitecore.Mvc.Presentation;
using Microsoft.Extensions.DependencyInjection;

namespace DeanOBrien.Feature.AiOverview.Controllers
{
    public class AIOverviewController : Controller
    {
        private readonly IGenAIService _genAIService;
        private readonly Database _web;
        private string _term;

        public AIOverviewController()
        {
            _web = Sitecore.Configuration.Factory.GetDatabase("web");
            _genAIService = ServiceLocator.ServiceProvider.GetService<IGenAIService>(); ;

        }

        public AIOverviewController(IGenAIService genAIService)
        {
            _genAIService = genAIService;
            _web = Sitecore.Configuration.Factory.GetDatabase("web");

        }

        public ActionResult AIOverview()
        {
            var searchParam = "q";
            var settingsSearchPageParam = Sitecore.Configuration.Settings.GetSetting("Search Parameter");
            if (!string.IsNullOrWhiteSpace(settingsSearchPageParam)) searchParam = settingsSearchPageParam;
            var renderingSearchParam = RenderingContext.Current.Rendering.Parameters["Search Parameter"];
            if (!string.IsNullOrWhiteSpace(renderingSearchParam)) searchParam = renderingSearchParam;
            _term = Request[searchParam];

            var systemPrompt = "You are a helpful assistant that responds completely in valid HTML.";
            var renderingSystemPrompt = RenderingContext.Current.Rendering.Parameters["System Prompt"];
            if (!string.IsNullOrWhiteSpace(renderingSystemPrompt)) systemPrompt = renderingSystemPrompt;

            ResponseWithCitations model = new ResponseWithCitations();
            model.IsAjax = RenderingContext.Current.Rendering.Parameters["Is Ajax"] == "1";
            model.Citations = new List<Citation>();
            model.SearchTerm = _term;
            model.SystemPrompt = systemPrompt;

            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(systemPrompt);
            model.SystemPromptB64 = System.Convert.ToBase64String(plainTextBytes);

            if (model.IsAjax) return View(model);

            model = GenerateAIResponse(systemPrompt, _term);
            return View(model);
        }
        public ActionResult GetAIOverview(string searchTerm, string systemPrompt)
        {
            if (string.IsNullOrWhiteSpace(systemPrompt))
            {
                systemPrompt = "You are a helpful assistant that responds completely in valid HTML.";
            }
            else
            {
                var base64EncodedBytes = System.Convert.FromBase64String(systemPrompt);
                systemPrompt = System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
            }

            ResponseWithCitations model = new ResponseWithCitations();
            model.Citations = new List<Citation>();
            model.SearchTerm = searchTerm;
            model.SystemPrompt = systemPrompt;

            if (model.IsAjax) return View(model);

            model = GenerateAIResponse(systemPrompt, searchTerm);
            return Json(model, JsonRequestBehavior.AllowGet);
        }

        private ResponseWithCitations GenerateAIResponse(string systemPrompt, string searchTerm)
        {
            ResponseWithCitations response = new ResponseWithCitations();
            response.Citations = new List<Citation>();

            if (!Sitecore.Context.PageMode.IsExperienceEditor)
            {
                try
                {
                    string dataSourceName = string.Empty;
                    Item dataSourceItem = null;

                    var settingsItem = _web.GetItem("/sitecore/system/Modules/AI Language Assistant");
                    if (settingsItem != null && settingsItem["Default Data Source"] != null) dataSourceName = settingsItem["Default Data Source"].ValueOrEmpty();

                    var dataSources = _web.GetItem("/sitecore/system/Modules/AI Language Assistant/Data Sources");
                    if (dataSources != null) dataSourceItem = dataSources.Children.Where(x => x.DisplayName == dataSourceName).FirstOrDefault();

                    if (dataSourceItem == null) dataSourceItem = dataSources.Children.FirstOrDefault();

                    var prompts = new List<Tuple<string, string>>() {
                        new Tuple<string,string>("System", systemPrompt),
                        new Tuple<string,string>("System","Please return the result in HTML format."),
                        new Tuple<string,string>("User","Please supply a maximum of 250 words about the following search term: " + searchTerm),
                        new Tuple<string,string>("System","The response should be a minimum of 500 words and formatted as HTML")

                    };

                    if (!string.IsNullOrWhiteSpace(searchTerm) && dataSourceItem != null) response = _genAIService.CallWithCitations(prompts, dataSourceItem);

                    if (response.Citations != null && response.Citations.Count > 0)
                    {
                        foreach (var citation in response.Citations)
                        {
                            var citationItem = _web.GetItem(citation.Id);
                            if (citationItem != null)
                            {
                                citation.Title = citationItem.DisplayName;
                                citation.Link = Sitecore.Links.LinkManager.GetItemUrl(citationItem);
                            }
                        }
                    }

                    var responseWithLinks = response.Response;
                    int count = 1;
                    foreach (var item in response.Citations)
                    {
                        string refString = "[doc" + count + "]";
                        string linkString = "<a href='" + item.Link + "'><span class='reference'><svg title='" + item.Title + "' focusable='false' xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24'><path d='M3.9 12c0-1.71 1.39-3.1 3.1-3.1h4V7H7c-2.76 0-5 2.24-5 5s2.24 5 5 5h4v-1.9H7c-1.71 0-3.1-1.39-3.1-3.1zM8 13h8v-2H8v2zm9-6h-4v1.9h4c1.71 0 3.1 1.39 3.1 3.1s-1.39 3.1-3.1 3.1h-4V17h4c2.76 0 5-2.24 5-5s-2.24-5-5-5z'></path></svg></span></a>";

                        responseWithLinks = responseWithLinks.Replace(refString, linkString);
                        count++;
                    }
                    response.Response = responseWithLinks;
                }
                catch (Exception ex)
                {
                    response.Response = ex.Message;
                    response.Citations = new List<Citation>();
                }
                return response;
            }
            response.Response = "AI Response is not called in the experience editor";
            return response;
        }
    }
}
