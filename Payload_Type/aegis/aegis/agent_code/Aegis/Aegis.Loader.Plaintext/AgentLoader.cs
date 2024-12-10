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


            using (Stream modelStream = asmExe.GetManifestResourceStream(sources.Find(item => item.Contains("Agent.Models.dll"))))
            using (Stream decompressorStream = new MemoryStream())
            {
                if (modelStream == null)
                {
                    return;
                }
                FileDecompressor.DecompressStream(modelStream, decompressorStream);
                alc.LoadFromStream(decompressorStream);
            }

            //Load the rest of the DLLs except for the agent
            foreach (string aa in sources)
            {
                if (excludedDlls.Contains(aa))
                {
                    continue;
                }
                using (Stream s = asmExe.GetManifestResourceStream(aa))
                using (Stream ds = new MemoryStream())
                {
                    if (s == null)
                    {
                        return;
                    }
                    FileDecompressor.DecompressStream(s, ds);
                    alc.LoadFromStream(ds);
                }
            }

            Assembly agent;
            using (Stream ad = asmExe.GetManifestResourceStream(sources.Find(item => item.Contains("Agent.dll"))))
            using (Stream ads = new MemoryStream())
            {
                if (ad == null)
                {
                    return;
                }
                FileDecompressor.DecompressStream(ad, ads);
                agent = alc.LoadFromStream(ads);
            }

            MethodInfo entryPoint = agent.EntryPoint;
            // Invoke the entry point method
            object[] parameters = new object[] { new string[0] }; // You can pass command-line arguments
            entryPoint.Invoke(null, parameters);

        }
    }
}
