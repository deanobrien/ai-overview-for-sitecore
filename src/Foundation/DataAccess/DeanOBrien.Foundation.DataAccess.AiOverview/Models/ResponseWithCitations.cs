using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeanOBrien.Foundation.DataAccess.AiOverview.Models
{
    public class ResponseWithCitations
    {
        public bool IsAjax { get; set; }
        public string SearchTerm { get; set; }
        public string Response { get; set; }
        public string SystemPrompt { get; set; }
        public string SystemPromptB64 { get; set; }
        public List<Citation> Citations { get; set; }
    }
    public class Citation
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Link { get; set; }
        public string Content { get; set; }
    }
}
