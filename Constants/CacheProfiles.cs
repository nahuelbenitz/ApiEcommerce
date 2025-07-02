using Microsoft.AspNetCore.Mvc;

namespace ApiEcommerce.Constants
{
    public class CacheProfiles
    {
        public const string Default10 = "Default10";
        public const string Default20 = "Default20";

        public static CacheProfile Profile10 = new()
        {
            Duration = 10
        };

        public static CacheProfile Profile20 = new()
        {
            Duration = 20
        };
    }
}
