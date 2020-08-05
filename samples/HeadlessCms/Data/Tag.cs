using System;
using System.Collections.Generic;

namespace HeadlessCms.Data
{
    public class Tag
    {
        public Guid Id { get; set; }
        public string Value { get; set; }
        public IEnumerable<PageTag> PageTags { get; set; }
    }
}