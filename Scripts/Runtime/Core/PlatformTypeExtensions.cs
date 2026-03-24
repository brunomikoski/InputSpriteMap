namespace BrunoMikoski.InputSpriteMap
{
    public static class PlatformTypeExtensions
    {
        public static bool HasFlagFast(this PlatformType platformType, PlatformType flag)
        {
            return (platformType & flag) == flag;
        }
        
        public static bool HasAnyFlagFast(this PlatformType platformType, PlatformType flags)
        {
            return (platformType & flags) != 0;
        }
        
        public static bool HasAllFlagsFast(this PlatformType platformType, PlatformType flags)
        {
            return (platformType & flags) == flags;
        }
        
        
    }
}