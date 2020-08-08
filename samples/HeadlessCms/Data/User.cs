using System.Collections.Generic;

namespace HeadlessCms.Data
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public IEnumerable<Page> Pages { get; set; }
    }
}