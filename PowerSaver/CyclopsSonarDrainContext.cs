namespace PowerSaver
{
    internal static class CyclopsSonarDrainContext
    {
        [System.ThreadStatic]
        private static int _depth;

        internal static bool IsActive
        {
            get { return _depth > 0; }
        }

        internal static void Enter()
        {
            _depth++;
        }

        internal static void Exit()
        {
            if (_depth > 0)
            {
                _depth--;
            }
        }
    }
}
