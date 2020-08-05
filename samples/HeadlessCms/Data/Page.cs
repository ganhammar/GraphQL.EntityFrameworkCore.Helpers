using System;
using System.Collections.Generic;

namespace HeadlessCms.Data
{
    public class Page
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public Guid EditorId { get; set; }
        public User Editor { get; set; }
        public IEnumerable<PageTag> PageTags { get; set; }
    }
}