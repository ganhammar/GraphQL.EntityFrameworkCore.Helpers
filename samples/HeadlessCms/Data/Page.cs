using System.Collections.Generic;

namespace HeadlessCms.Data
{
    public class Page
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public int EditorId { get; set; }
        public User Editor { get; set; }
        public IEnumerable<Tag> Tags { get; set; }
    }
}