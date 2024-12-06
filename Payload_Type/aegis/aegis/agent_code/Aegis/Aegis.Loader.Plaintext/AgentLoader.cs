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
            if (asmExe == null)
            {
                return;
            }

            List<string> sources = asmExe.GetManifestResourceNames().ToList();

            //Gotta load this one first since all the others basically rely on it
            Stream modelStream = asmExe.GetManifestResourceStream(sources.Find(item => item.Contains("Agent.Models.dll")));

            if (modelStream == null)
            {
                return;
            }

            alc.LoadFromStream(modelStream);

            //Load the rest of the DLLs except for the agent
            foreach (string aa in sources)
            {
                if (excludedDlls.Contains(aa))
                {
                    continue;
                }
                Stream s = asmExe.GetManifestResourceStream(aa);
                if (s != null)
                {
                    alc.LoadFromStream(s);
                }
            }
            // Get the entry point method
            Stream ad = asmExe.GetManifestResourceStream(sources.Find(item => item.Contains("Agent.dll")));
            Assembly agent;
            if (ad == null)
            {
                return;
            }
            agent = alc.LoadFromStream(ad);
            MethodInfo entryPoint = agent.EntryPoint;

            // Invoke the entry point method
            object[] parameters = new object[] { new string[0] }; // You can pass command-line arguments
            entryPoint.Invoke(null, parameters);
        }
    }
}
