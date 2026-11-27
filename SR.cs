using System;
using System.Collections.Generic;
using System.Reflection;

namespace Cjayride.TalentPace
{
    // Silent SkillsReworked type resolver. AccessTools.TypeByName logs HarmonyX warnings
    // even when a type is optional; this probes the SkillsReworked assembly only.
    internal static class SR
    {
        const BindingFlags Declared =
            BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static |
            BindingFlags.Public | BindingFlags.NonPublic;

        static readonly Dictionary<string, Type> _types = new Dictionary<string, Type>(8);
        static Assembly _assembly;
        static bool _scanned;

        internal static Assembly Assembly
        {
            get
            {
                EnsureScan();
                return _assembly;
            }
        }

        internal static Type Type(string fullName)
        {
            if (string.IsNullOrEmpty(fullName))
            {
                return null;
            }

            Type cached;
            if (_types.TryGetValue(fullName, out cached))
            {
                return cached;
            }

            EnsureScan();
            Type found = _assembly == null ? null : _assembly.GetType(fullName, false);
            if (found == null && _assembly != null)
            {
                found = FindTypeBySimpleName(_assembly, fullName);
            }

            if (found == null)
            {
                found = FindTypeInLoadedAssemblies(fullName);
            }

            _types[fullName] = found;
            return found;
        }

        // Empty parameters = first method with that name (any signature). AccessTools
        // treats empty as "zero args" and logs HarmonyX warnings on a miss.
        internal static MethodInfo Method(Type type, string name, params Type[] parameters)
        {
            if (type == null || string.IsNullOrEmpty(name))
            {
                return null;
            }

            try
            {
                MethodInfo[] methods = type.GetMethods(Declared);
                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo method = methods[i];
                    if (method.Name != name)
                    {
                        continue;
                    }

                    if (parameters == null || parameters.Length == 0)
                    {
                        return method;
                    }

                    ParameterInfo[] args = method.GetParameters();
                    if (args.Length != parameters.Length)
                    {
                        continue;
                    }

                    bool match = true;
                    for (int j = 0; j < args.Length; j++)
                    {
                        if (parameters[j] != null && args[j].ParameterType != parameters[j])
                        {
                            match = false;
                            break;
                        }
                    }

                    if (match)
                    {
                        return method;
                    }
                }
            }
            catch (Exception)
            {
            }

            return null;
        }

        internal static FieldInfo Field(Type type, string name)
        {
            if (type == null || string.IsNullOrEmpty(name))
            {
                return null;
            }

            try
            {
                return type.GetField(name, Declared);
            }
            catch (Exception)
            {
                return null;
            }
        }

        internal static PropertyInfo Property(Type type, string name)
        {
            if (type == null || string.IsNullOrEmpty(name))
            {
                return null;
            }

            try
            {
                return type.GetProperty(name, Declared);
            }
            catch (Exception)
            {
                return null;
            }
        }

        static Type FindTypeInLoadedAssemblies(string fullName)
        {
            Assembly[] assemblies;
            try
            {
                assemblies = AppDomain.CurrentDomain.GetAssemblies();
            }
            catch (Exception)
            {
                return null;
            }

            for (int i = 0; i < assemblies.Length; i++)
            {
                try
                {
                    Type type = assemblies[i].GetType(fullName, false);
                    if (type != null)
                    {
                        return type;
                    }
                }
                catch (Exception)
                {
                }
            }

            return null;
        }

        static void EnsureScan()
        {
            if (_scanned)
            {
                return;
            }

            _scanned = true;
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                Assembly assembly = assemblies[i];
                string name;
                try
                {
                    name = assembly.GetName().Name;
                }
                catch (Exception)
                {
                    continue;
                }

                if (name != "SkillsReworked")
                {
                    continue;
                }

                _assembly = assembly;
                return;
            }
        }

        static Type FindTypeBySimpleName(Assembly assembly, string fullName)
        {
            string simple = fullName;
            int dot = fullName.LastIndexOf('.');
            if (dot >= 0)
            {
                simple = fullName.Substring(dot + 1);
            }

            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types;
            }

            if (types == null)
            {
                return null;
            }

            Type match = null;
            for (int i = 0; i < types.Length; i++)
            {
                Type type = types[i];
                if (type == null)
                {
                    continue;
                }

                if (type.FullName == fullName || type.Name == simple)
                {
                    if (match != null && match != type)
                    {
                        return match;
                    }

                    match = type;
                }
            }

            return match;
        }
    }
}