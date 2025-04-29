using DeanOBrien.Foundation.DataAccess.AiOverview.Models;
using Sitecore.Data.Items;
using System;
using System.Collections.Generic;


namespace DeanOBrien.Foundation.DataAccess.AiOverview.Services
{
    public interface IGenAIService
    {
        string Call(List<Tuple<string, string>> prompts, string userPrompt, string context);
        ResponseWithCitations CallWithCitations(List<Tuple<string, string>> prompts, Item dataSource, string userPrompt = "", string context = "");

    }
}
