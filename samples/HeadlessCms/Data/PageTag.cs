using System;

namespace HeadlessCms.Data
{
    public class PageTag
    {
        public Guid PageId { get; set; }
        public Guid TagId { get; set; }
        public Page Page { get; set; }
        public Tag Tag { get; set; }
    }
}