using System;

namespace HeadlessCms.Data
{
    public class PageTag
    {
        public int PageId { get; set; }
        public int TagId { get; set; }
        public Page Page { get; set; }
        public Tag Tag { get; set; }
    }
}