using System.Collections.Generic;

namespace ZEFPlusTest
{
    public class Profile : EntityBase
    {
        public virtual ProfileId Id { get; set; }

        public virtual Dictionary<string, object> Properties { get; set; }
    }

    public enum ProfileId : int
    {
        One = 1,
        Two = 2,
        Three = 3,
        Four = 4,
        Five = 5
    }
}
