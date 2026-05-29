using System;
using PowerSaver;

namespace PowerSaver.Tests
{
    internal static class Program
    {
        private static int _failures;

        private static int Main()
        {
            CheckClose("Default global drain remains 75 percent", 0.75f,
                PowerDrainMath.GetToolMultiplier(0.75f));
            CheckClose("Default fixed vehicle category keeps effective vehicle drain at 75 percent", 0.75f,
                PowerDrainMath.GetVehicleMultiplier(0.75f, 1.0f));
            CheckClose("Default fixed base category keeps effective base drain at 75 percent", 0.75f,
                PowerDrainMath.GetBaseMultiplier(0.75f, 1.0f));
            CheckTrue("Old generated category defaults are migrated to the fixed 1.0.1 defaults",
                PowerSaverConfigMigration.ShouldMigrateOnePointOneDefaults(null, 0.75f, 0.75f, 0.75f));
            CheckFalse("Already migrated configs are not migrated again",
                PowerSaverConfigMigration.ShouldMigrateOnePointOneDefaults("1.0.1", 0.75f, 0.75f, 0.75f));
            CheckTrue("Current config marker is 1.0.1",
                PowerSaverConfigMigration.CurrentConfigVersion == "1.0.1");
            CheckFalse("Custom vehicle category values are not treated as generated defaults",
                PowerSaverConfigMigration.ShouldMigrateOnePointOneDefaults(null, 0.75f, 0.5f, 0.75f));
            CheckFalse("Custom base category values are not treated as generated defaults",
                PowerSaverConfigMigration.ShouldMigrateOnePointOneDefaults(null, 0.75f, 0.75f, 0.5f));
            CheckClose("Vehicle drains use global times vehicle", 0.5625f,
                PowerDrainMath.GetVehicleMultiplier(0.75f, 0.75f));
            CheckClose("Base drains use global times base", 0.5625f,
                PowerDrainMath.GetBaseMultiplier(0.75f, 0.75f));
            CheckClose("Tool drain amount is scaled by global", 7.5f,
                PowerDrainMath.AdjustToolDrain(10f, 0.75f));
            CheckClose("Vehicle drain amount is scaled by global times vehicle", 5.625f,
                PowerDrainMath.AdjustVehicleDrain(10f, 0.75f, 0.75f));
            CheckClose("Cyclops sonar power relay drain can be treated as vehicle drain", -5.625f,
                PowerDrainMath.AdjustVehicleDrain(-10f, 0.75f, 0.75f));
            CheckClose("Base consumption is scaled by global times base", -2.8125f,
                PowerDrainMath.AdjustBasePowerDelta(-5f, 0.75f, 0.75f));
            CheckClose("Base charging is not scaled", 5f,
                PowerDrainMath.AdjustBasePowerDelta(5f, 0.75f, 0.75f));
            CheckClose("Identity multiplier leaves values unchanged", 5f,
                PowerDrainMath.AdjustVehicleDrain(5f, 1f, 1f));
            CheckClose("Increased drain multipliers increase adjusted drain", 20f,
                PowerDrainMath.AdjustVehicleDrain(10f, 1f, 2f));

            if (_failures == 0)
            {
                Console.WriteLine("PowerSaver compatibility tests passed.");
                return 0;
            }

            Console.Error.WriteLine($"PowerSaver compatibility tests failed: {_failures}");
            return 1;
        }

        private static void CheckClose(string name, float expected, float actual)
        {
            if (Math.Abs(expected - actual) <= 0.0001f)
            {
                return;
            }

            _failures++;
            Console.Error.WriteLine($"{name}: expected {expected}, got {actual}");
        }

        private static void CheckFalse(string name, bool condition)
        {
            if (!condition)
            {
                return;
            }

            _failures++;
            Console.Error.WriteLine($"{name}: expected false, got true");
        }

        private static void CheckTrue(string name, bool condition)
        {
            if (condition)
            {
                return;
            }

            _failures++;
            Console.Error.WriteLine($"{name}: expected true, got false");
        }
    }
}
