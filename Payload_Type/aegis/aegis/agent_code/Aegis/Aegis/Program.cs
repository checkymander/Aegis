using Aegis.Loader;
using Aegis.Models.Interfaces;
using Aegis.Utilities;
namespace Aegis
{
    public static class Program
    {

        public static async Task Main(string[] args)
        {
            foreach (IMod mod in Helpers.ParseAssemblyForMods())
            {
                if (!await mod.Check())
                {
                    return;
                }
            }
            AgentLoader ldr = new AgentLoader();
            await ldr.Go();
            Console.ReadKey();
        }
    }
}