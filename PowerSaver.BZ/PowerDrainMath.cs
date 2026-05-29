namespace PowerSaver.BZ
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

        internal static float ToEasyCraftVirtualPower(float actualPower, float effectiveBaseMultiplier)
        {
            return DivideByEffectiveMultiplier(actualPower, effectiveBaseMultiplier);
        }

        internal static float ToChargerVirtualPower(float actualPower, float effectiveBaseMultiplier)
        {
            return DivideByEffectiveMultiplier(actualPower, effectiveBaseMultiplier);
        }

        internal static float ToEasyCraftVirtualConsumed(float actualConsumed, float requestedAmount, float effectiveBaseMultiplier)
        {
            float virtualConsumed = DivideByEffectiveMultiplier(actualConsumed, effectiveBaseMultiplier);
            if (virtualConsumed > requestedAmount)
            {
                return requestedAmount;
            }

            return virtualConsumed;
        }

        internal static float ToChargerVirtualConsumed(float actualConsumed, float requestedAmount, float effectiveBaseMultiplier)
        {
            float virtualConsumed = DivideByEffectiveMultiplier(actualConsumed, effectiveBaseMultiplier);
            if (virtualConsumed > requestedAmount)
            {
                return requestedAmount;
            }

            return virtualConsumed;
        }

        private static float DivideByEffectiveMultiplier(float amount, float effectiveMultiplier)
        {
            if (effectiveMultiplier <= 0f)
            {
                return amount;
            }

            return amount / effectiveMultiplier;
        }
    }
}
