using Aegis.Loader;

namespace Aegis
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            AgentLoader ldr = new AgentLoader();
            await ldr.Go();
        }
    }
}