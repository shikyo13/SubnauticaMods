using System;
using System.Linq;
using System.Reflection;
using PowerSaver.BZ;

namespace PowerSaver.BZ.Tests
{
    internal static class Program
    {
        private static int _failures;

        private static int Main()
        {
            CheckClose("Tool drains use the global multiplier", 0.75f,
                PowerDrainMath.GetToolMultiplier(0.75f));
            CheckClose("Default global drain remains 75 percent", 0.75f,
                GetSliderDefault(nameof(PowerSaverConfig.DrainMultiplier)));
            CheckClose("Default vehicle category keeps effective vehicle drain at 75 percent", 1f,
                GetSliderDefault(nameof(PowerSaverConfig.VehicleDrainMultiplier)));
            CheckClose("Default base category keeps effective base drain at 75 percent", 1f,
                GetSliderDefault(nameof(PowerSaverConfig.BaseDrainMultiplier)));
            CheckClose("Default effective vehicle drain is 75 percent", 0.75f,
                PowerDrainMath.GetVehicleMultiplier(
                    GetSliderDefault(nameof(PowerSaverConfig.DrainMultiplier)),
                    GetSliderDefault(nameof(PowerSaverConfig.VehicleDrainMultiplier))));
            CheckClose("Default effective base drain is 75 percent", 0.75f,
                PowerDrainMath.GetBaseMultiplier(
                    GetSliderDefault(nameof(PowerSaverConfig.DrainMultiplier)),
                    GetSliderDefault(nameof(PowerSaverConfig.BaseDrainMultiplier))));
            CheckFalse("EasyCraft compatibility is always on and not user-configurable",
                typeof(PowerSaverConfig).GetField("EnableEasyCraftCompatibility") != null);
            CheckTrue("Old generated category defaults are migrated to the fixed 1.0.1 defaults",
                PowerSaverConfigMigration.ShouldMigrateOnePointOneDefaults(null, 0.75f, 0.75f, 0.75f));
            CheckFalse("Already migrated configs are not migrated again",
                PowerSaverConfigMigration.ShouldMigrateOnePointOneDefaults("1.0.1", 0.75f, 0.75f, 0.75f));
            CheckTrue("Current config marker is 1.0.3",
                PowerSaverConfigMigration.CurrentConfigVersion == "1.0.3");
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
            CheckClose("Cyclops sonar vehicle drain can differ from EasyCraft base accounting", -3.75f,
                PowerDrainMath.AdjustVehicleDrain(-10f, 0.75f, 0.5f));
            CheckClose("Base consumption is scaled by global times base", -2.8125f,
                PowerDrainMath.AdjustBasePowerDelta(-5f, 0.75f, 0.75f));
            CheckClose("Base charging is not scaled", 5f,
                PowerDrainMath.AdjustBasePowerDelta(5f, 0.75f, 0.75f));
            CheckClose("EasyCraft virtual accounting still uses the base multiplier", 10f,
                PowerDrainMath.ToEasyCraftVirtualPower(
                    7.5f,
                    PowerDrainMath.GetBaseMultiplier(0.75f, 1.0f)));
            CheckClose("Virtual EasyCraft power converts scaled actual power to vanilla-equivalent power", 5f,
                PowerDrainMath.ToEasyCraftVirtualPower(2.8125f, 0.5625f));
            CheckClose("Virtual EasyCraft consumed power converts scaled consumption to requested accounting", 5f,
                PowerDrainMath.ToEasyCraftVirtualConsumed(2.8125f, 5f, 0.5625f));
            CheckClose("Virtual charger power keeps charger availability independent of reduced actual drain", 5f,
                PowerDrainMath.ToChargerVirtualPower(2.8125f, 0.5625f));
            CheckClose("Virtual charger consumed power keeps battery charge speed unchanged", 5f,
                PowerDrainMath.ToChargerVirtualConsumed(2.8125f, 5f, 0.5625f));
            CheckClose("Virtual EasyCraft consumed power is capped to the requested amount", 5f,
                PowerDrainMath.ToEasyCraftVirtualConsumed(4f, 5f, 0.5625f));
            CheckClose("Failed scaled consumption is still reported as a failure amount", 2f,
                PowerDrainMath.ToEasyCraftVirtualConsumed(1.125f, 5f, 0.5625f));
            CheckClose("Identity multiplier leaves virtual power unchanged", 5f,
                PowerDrainMath.ToEasyCraftVirtualPower(5f, 1f));
            CheckClose("Increased drain multiplier reduces virtual available power", 5f,
                PowerDrainMath.ToEasyCraftVirtualPower(10f, 2f));
            CheckFalse("Cyclops sonar context starts inactive", CyclopsSonarDrainContext.IsActive);
            CyclopsSonarDrainContext.Enter();
            CheckTrue("Cyclops sonar context enters active state", CyclopsSonarDrainContext.IsActive);
            CyclopsSonarDrainContext.Enter();
            CyclopsSonarDrainContext.Exit();
            CheckTrue("Cyclops sonar context supports nested enter and exit", CyclopsSonarDrainContext.IsActive);
            CyclopsSonarDrainContext.Exit();
            CheckFalse("Cyclops sonar context exits inactive state", CyclopsSonarDrainContext.IsActive);

            if (_failures == 0)
            {
                Console.WriteLine("PowerSaver.BZ compatibility tests passed.");
                return 0;
            }

            Console.Error.WriteLine($"PowerSaver.BZ compatibility tests failed: {_failures}");
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

        private static float GetSliderDefault(string fieldName)
        {
            FieldInfo field = typeof(PowerSaverConfig).GetField(fieldName);
            CustomAttributeData slider = field.CustomAttributes
                .First(attribute => attribute.AttributeType.FullName == "Nautilus.Options.Attributes.SliderAttribute");
            CustomAttributeNamedArgument defaultValue = slider.NamedArguments
                .First(argument => argument.MemberName == "DefaultValue");
            return Convert.ToSingle(defaultValue.TypedValue.Value);
        }
    }
}
