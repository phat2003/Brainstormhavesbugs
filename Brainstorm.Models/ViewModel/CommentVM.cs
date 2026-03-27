using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Brainstorm.Models.ViewModel
{
    public class CommentVM
    {
        public Comment comment { get; set; }
        public Idea idea { get; set; }
        public string ViewComment { get; set; }
    }
}
