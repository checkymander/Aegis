using Aegis.Models.Interfaces;

namespace Aegis.Mod.Delay
{
    public class AegisDelay : IMod
    {
        public async Task<bool> Check()
        {
            DateTime start = DateTime.Now;
            Random random = new Random();
            int d = random.Next(60000, 600000);
            //Sleep for a random amount of time between 1 and 10 minutes
            Thread.Sleep(d);

            DateTime end = DateTime.Now;

            double differential = GetSecondsBetween(start, end);

            //If our number is significantly less than d (giving a 10% buffer) then we're being analyzed in a sandbox and should exit
            if (differential < (d * 0.9))
            {
                return false;
            }
            return true;
        }
        public double GetSecondsBetween(DateTime earlierTimestamp, DateTime laterTimestamp)
        {
            TimeSpan timeDifference = laterTimestamp - earlierTimestamp;
            return timeDifference.TotalSeconds;
        }
    }
}
