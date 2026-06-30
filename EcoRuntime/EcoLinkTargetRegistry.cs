using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using EcoHousingAdvisor.Domain;

namespace EcoHousingAdvisor.EcoRuntime
{
    public static class EcoLinkTargetRegistry
    {
        private static readonly ConcurrentDictionary<int, WeakReference<object>> Objects = new ConcurrentDictionary<int, WeakReference<object>>();

        public static HousingLinkTarget Register(HousingLinkTargetKind kind, object instance, string displayName)
        {
            if (instance == null)
            {
                return null;
            }

            var id = RuntimeHelpers.GetHashCode(instance);
            Objects[id] = new WeakReference<object>(instance);
            return new HousingLinkTarget(kind, displayName, instance.GetType().Name, id);
        }

        public static bool TryGet(HousingLinkTarget target, out object instance)
        {
            instance = null;
            return target?.RuntimeId != null
                && Objects.TryGetValue(target.RuntimeId.Value, out var reference)
                && reference.TryGetTarget(out instance);
        }
    }
}
