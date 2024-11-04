using Aegis.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Aegis.Utilities
{
    public static class Helpers
    {
        public static List<IMod> ParseAssemblyForMods()
        {
            List<IMod> mods = new List<IMod>();
            List<string> potentialMods = new List<string>() { "DelayExecution", "CalculatePi", "DomainLookup" };
            foreach (var mod in potentialMods)
            {
                try
                {
                    Assembly _asm = Assembly.Load($"Aegis.Mods.{mod}, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
                    foreach (Type t in _asm.GetTypes())
                    {
                        if (typeof(IMod).IsAssignableFrom(t))
                        {
                            IMod m = (IMod)Activator.CreateInstance(t);

                            if (m != null)
                            {
                                mods.Add(m);
                            }
                        }
                    }
                }
                catch
                {
                }
            }
            return mods;
        }
    }
}
