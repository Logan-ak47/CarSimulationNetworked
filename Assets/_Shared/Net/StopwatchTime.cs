using System.Diagnostics;

namespace CarSim.Shared
{
    public static class StopwatchTime
    {
        private static readonly Stopwatch _stopwatch = Stopwatch.StartNew();

        public static uint TimestampMs()
        {
            return (uint)(_stopwatch.ElapsedMilliseconds & 0xFFFFFFFF);
        }
    }
}
