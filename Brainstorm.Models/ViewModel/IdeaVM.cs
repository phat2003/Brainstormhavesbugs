using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Brainstorm.Models.ViewModel
{
    public class IdeaVM
    {
        public Idea idea { get; set; }
        [ValidateNever]
        public View view { get; set; }
        [ValidateNever]
        public React react { get; set; }
        [ValidateNever]
        public IEnumerable<SelectListItem> CategoryList { get; set; }
        [ValidateNever]
        public IEnumerable<SelectListItem> TopicList { get; set; }
        public int LikeCount { get; set; }
        public int DislikeCount { get; set; }
        public int ViewCount { get; set; }
    }
}
