namespace PowerSaver
{
    internal static class PowerSaverConfigMigration
    {
        internal const string CurrentConfigVersion = "1.0.1";

        internal static bool ShouldMigrateOnePointOneDefaults(
            string configVersion,
            float globalMultiplier,
            float vehicleMultiplier,
            float baseMultiplier)
        {
            return string.IsNullOrEmpty(configVersion)
                && IsClose(globalMultiplier, 0.75f)
                && IsClose(vehicleMultiplier, 0.75f)
                && IsClose(baseMultiplier, 0.75f);
        }

        private static bool IsClose(float left, float right)
        {
            return System.Math.Abs(left - right) <= 0.0001f;
        }
    }
}
