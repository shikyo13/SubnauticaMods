namespace PowerSaver
{
    internal static class PowerDrainMath
    {
        internal static float GetToolMultiplier(float globalMultiplier)
        {
            return globalMultiplier;
        }

        internal static float GetVehicleMultiplier(float globalMultiplier, float vehicleMultiplier)
        {
            return globalMultiplier * vehicleMultiplier;
        }

        internal static float GetBaseMultiplier(float globalMultiplier, float baseMultiplier)
        {
            return globalMultiplier * baseMultiplier;
        }

        internal static float AdjustToolDrain(float amount, float globalMultiplier)
        {
            return amount * GetToolMultiplier(globalMultiplier);
        }

        internal static float AdjustVehicleDrain(float amount, float globalMultiplier, float vehicleMultiplier)
        {
            return amount * GetVehicleMultiplier(globalMultiplier, vehicleMultiplier);
        }

        internal static float AdjustBasePowerDelta(float amount, float globalMultiplier, float baseMultiplier)
        {
            if (amount >= 0f)
            {
                return amount;
            }

            return amount * GetBaseMultiplier(globalMultiplier, baseMultiplier);
        }
    }
}
