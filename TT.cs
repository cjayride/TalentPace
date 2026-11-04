using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Cjayride.TalentPace
{
    // Single cached resolver for every Talent Tree type this mod reflects into.
    // AccessTools.TypeByName scans every loaded assembly on each call, so the
    // original code paid that cost a dozen times during Awake. Here the assembly
    // set is scanned once, cached by name, and reused by all patch classes.
    internal static class TT
    {
        static readonly Dictionary<string, Type> _types = new Dictionary<string, Type>(16);
        static Assembly _assembly;
        static bool _scanned;
        static bool _found;

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
            Type cached;
            if (_types.TryGetValue(fullName, out cached))
            {
                return cached;
            }

            EnsureScan();
            Type found = _assembly == null ? null : _assembly.GetType(fullName);
            _types[fullName] = found;
            return found;
        }

        internal static MethodInfo Method(string typeName, string methodName, params Type[] parameters)
        {
            Type type = Type(typeName);
            return type == null ? null : AccessTools.DeclaredMethod(type, methodName, parameters);
        }

        internal static PropertyInfo Property(string typeName, string propertyName)
        {
            Type type = Type(typeName);
            return type == null ? null : AccessTools.DeclaredProperty(type, propertyName);
        }

        internal static FieldInfo Field(string typeName, string fieldName)
        {
            Type type = Type(typeName);
            return type == null ? null : AccessTools.DeclaredField(type, fieldName);
        }

        // True when the Talent Tree assembly is loaded. TalentWindow lives in it,
        // so one probe is enough and it needs no name matching.
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
                if (assembly.GetName().Name != "TalentTree")
                {
                    continue;
                }

                _assembly = assembly;
                _found = assembly.GetType("Core.TalentRuntime") != null;
                return;
            }
        }

        internal static bool Available
        {
            get
            {
                EnsureScan();
                return _found;
            }
        }

        // Talent Tree calls SyncToPlayer from several places, including a heartbeat
        // patch. Caching the runtime per player id avoids a dictionary lookup plus a
        // rebuild check on the damage path.
        static readonly Dictionary<long, TalentTreeRuntime> _runtimes = new Dictionary<long, TalentTreeRuntime>(8);
        static MethodInfo _getRuntime;
        static PropertyInfo _cacheProp;
        static bool _runtimeBound;

        internal sealed class TalentTreeRuntime
        {
            internal object Runtime;
            internal object Cache;
            internal int CacheVersion = -1;
        }

        // Returns the TalentCache for a player, refreshed only when the tree's own
        // CacheVersion changes. Null when Talent Tree is absent.
        internal static object CacheFor(Player player)
        {
            if (player == null)
            {
                return null;
            }

            if (!_runtimeBound)
            {
                _runtimeBound = true;
                _getRuntime = Method("Core.TalentRuntimeService", "Get", typeof(Player));
                _cacheProp = Property("Core.TalentRuntime", "Cache");
                _versionMember = Property("Core.TalentRuntime", "CacheVersion");
                if (_versionMember == null)
                {
                    _versionMember = Field("Core.TalentRuntime", "CacheVersion");
                }
            }

            if (_getRuntime == null || _cacheProp == null)
            {
                return null;
            }

            long id = player.GetPlayerID();
            TalentTreeRuntime entry;
            if (!_runtimes.TryGetValue(id, out entry))
            {
                entry = new TalentTreeRuntime();
                _runtimes[id] = entry;
            }

            if (entry.Runtime == null)
            {
                _getArg[0] = player;
                entry.Runtime = _getRuntime.Invoke(null, _getArg);
                _getArg[0] = null;
                if (entry.Runtime == null)
                {
                    return null;
                }

                entry.Cache = _cacheProp.GetValue(entry.Runtime, null);
                entry.CacheVersion = VersionOf(entry.Runtime);
                return entry.Cache;
            }

            int version = VersionOf(entry.Runtime);
            if (version != entry.CacheVersion)
            {
                entry.CacheVersion = version;
                entry.Cache = _cacheProp.GetValue(entry.Runtime, null);
            }

            return entry.Cache;
        }

        static int VersionOf(object runtime)
        {
            if (_versionMember == null && runtime != null)
            {
                Type type = runtime.GetType();
                _versionMember = (MemberInfo)type.GetProperty("CacheVersion")
                    ?? type.GetField("CacheVersion", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            }

            if (_versionMember is PropertyInfo property)
            {
                return (int)property.GetValue(runtime, null);
            }

            if (_versionMember is FieldInfo field)
            {
                return (int)field.GetValue(runtime);
            }

            return -1;
        }

        static MemberInfo _versionMember;
        static readonly object[] _getArg = new object[1];
    }
}
