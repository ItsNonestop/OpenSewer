using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace OpenSewer.Utility
{
    internal static class StatsProbe
    {
        private enum StatKind
        {
            Health,
            Hunger,
            Thirst,
            Depression,
            SleepNeed,
            WcNeed,
            Hygiene
        }

        private sealed class StatBinding
        {
            public object Target;
            public string TargetTypeName;
            public string ValueMemberName;
            public Func<float?> ReadValue;
            public string MaxMemberName;
            public Func<float?> ReadMax;
            public bool WasLogged;
        }

        private static readonly Dictionary<StatKind, StatBinding> Bindings = [];
        private static readonly Dictionary<StatKind, string[]> ValueAliases = new()
        {
            { StatKind.Health, ["health", "hp"] },
            { StatKind.Hunger, ["hunger", "food"] },
            { StatKind.Thirst, ["thirst", "water", "hydration"] },
            { StatKind.Depression, ["depression", "mood"] },
            { StatKind.SleepNeed, ["sleepneed", "sleep", "fatigue", "tired"] },
            { StatKind.WcNeed, ["wcneed", "toilet", "bladder"] },
            { StatKind.Hygiene, ["hygiene", "cleanliness"] }
        };

        private static readonly Dictionary<StatKind, string[]> MaxAliases = new()
        {
            { StatKind.Health, ["maxhealth", "healthmax", "maxhp", "hpmax"] },
            { StatKind.Hunger, ["maxhunger", "hungermax", "maxfood", "foodmax"] },
            { StatKind.Thirst, ["maxthirst", "thirstmax", "maxwater", "watermax"] },
            { StatKind.Depression, ["maxdepression", "depressionmax", "maxmood", "moodmax"] },
            { StatKind.SleepNeed, ["maxsleepneed", "sleepneedmax", "maxsleep", "sleepmax"] },
            { StatKind.WcNeed, ["maxwcneed", "wcneedmax", "maxtoilet", "toiletmax", "maxbladder", "bladdermax"] },
            { StatKind.Hygiene, ["maxhygiene", "hygienemax", "maxcleanliness", "cleanlinessmax"] }
        };

        private static readonly string[] CandidateTypeKeywords =
        [
            "player", "stat", "need", "health", "hunger", "thirst", "depression", "sleep", "hygiene", "toilet", "wc"
        ];

        private static readonly string[] CommonSingletonTypes =
        [
            "Player",
            "PlayerController",
            "PlayerStats",
            "PlayerStatus",
            "NeedController",
            "NeedsController",
            "StatsController"
        ];

        private static float nextProbeTime;
        private static bool isProbing;
        private const float ProbeIntervalSeconds = 1.5f;

        public static bool IsProbing => isProbing;

        public static void Tick()
        {
            if (Time.unscaledTime < nextProbeTime)
                return;

            nextProbeTime = Time.unscaledTime + ProbeIntervalSeconds;

            if (!IsWorldReady())
                return;

            isProbing = true;
            try
            {
                var candidates = BuildCandidateList();
                if (candidates.Count == 0)
                    return;

                foreach (StatKind kind in Enum.GetValues(typeof(StatKind)))
                {
                    if (HasBinding(kind))
                        continue;

                    if (TryCreateBinding(kind, candidates, out var binding))
                    {
                        Bindings[kind] = binding;
                        LogBinding(kind, binding);
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.DLog($"StatsProbe probe error: {ex.GetType().Name}: {ex.Message}");
            }
            finally
            {
                isProbing = false;
            }
        }

        public static bool TryGetSnapshot(out StatsSnapshot snapshot)
        {
            snapshot = new StatsSnapshot(
                Read(StatKind.Health),
                Read(StatKind.Hunger),
                Read(StatKind.Thirst),
                Read(StatKind.Depression),
                Read(StatKind.SleepNeed),
                Read(StatKind.WcNeed),
                Read(StatKind.Hygiene)
            );

            return snapshot.HasAny;
        }

        private static bool IsWorldReady()
        {
            return GameController.instance != null && GameUIController.instance != null;
        }

        private static bool HasBinding(StatKind kind)
        {
            if (!Bindings.TryGetValue(kind, out var binding))
                return false;

            return binding != null && binding.Target != null && binding.ReadValue != null;
        }

        private static List<object> BuildCandidateList()
        {
            var candidates = new List<object>(64);
            var seen = new HashSet<object>();

            AddCandidate(GameController.instance, candidates, seen);
            AddCandidate(GameUIController.instance, candidates, seen);
            AddCandidate(DeveloperConsole.Instance, candidates, seen);
            AddCandidate(BuildingSystem.instance, candidates, seen);
            AddCandidate(BackpackStorage.instance, candidates, seen);

            var ac = typeof(ItemDatabase).Assembly;
            foreach (var typeName in CommonSingletonTypes)
            {
                AddCandidate(TryReadSingleton(ac, typeName), candidates, seen);
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                AddRelatedObjects(candidates[i], candidates, seen);
            }

            var sceneBehaviours = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>();
            foreach (var mb in sceneBehaviours)
            {
                if (mb == null)
                    continue;

                var name = mb.GetType().Name;
                if (ContainsAny(name, CandidateTypeKeywords))
                    AddCandidate(mb, candidates, seen);
            }

            return candidates;
        }

        private static void AddCandidate(object obj, List<object> list, HashSet<object> seen)
        {
            if (obj == null || seen.Contains(obj))
                return;

            seen.Add(obj);
            list.Add(obj);
        }

        private static void AddRelatedObjects(object root, List<object> list, HashSet<object> seen)
        {
            if (root == null)
                return;

            var type = root.GetType();
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            foreach (var field in type.GetFields(flags))
            {
                if (!IsRelatedType(field.FieldType))
                    continue;

                object value;
                try
                {
                    value = field.GetValue(root);
                }
                catch
                {
                    continue;
                }

                AddCandidate(value, list, seen);
            }

            foreach (var prop in type.GetProperties(flags))
            {
                if (!prop.CanRead || prop.GetIndexParameters().Length > 0 || !IsRelatedType(prop.PropertyType))
                    continue;

                object value;
                try
                {
                    value = prop.GetValue(root, null);
                }
                catch
                {
                    continue;
                }

                AddCandidate(value, list, seen);
            }
        }

        private static bool IsRelatedType(Type type)
        {
            if (type == null)
                return false;

            if (type.IsPrimitive || type == typeof(string))
                return false;

            return ContainsAny(type.Name, CandidateTypeKeywords);
        }

        private static object TryReadSingleton(Assembly assembly, string typeName)
        {
            if (assembly == null || string.IsNullOrEmpty(typeName))
                return null;

            var type = assembly
                .GetTypes()
                .FirstOrDefault(t => string.Equals(t.Name, typeName, StringComparison.OrdinalIgnoreCase));
            if (type == null)
                return null;

            var flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            var prop = type.GetProperty("instance", flags) ?? type.GetProperty("Instance", flags);
            if (prop != null && prop.CanRead)
            {
                try { return prop.GetValue(null, null); } catch { }
            }

            var field = type.GetField("instance", flags) ?? type.GetField("Instance", flags);
            if (field != null)
            {
                try { return field.GetValue(null); } catch { }
            }

            return null;
        }

        private static bool TryCreateBinding(StatKind kind, List<object> candidates, out StatBinding binding)
        {
            binding = null;

            foreach (var candidate in candidates)
            {
                if (candidate == null)
                    continue;

                if (!TryFindNumericMember(candidate.GetType(), ValueAliases[kind], allowMaxNames: false, out var valueMember))
                    continue;

                var readValue = BuildReader(candidate, valueMember);
                if (readValue == null)
                    continue;

                string maxMemberName = null;
                Func<float?> readMax = null;

                if (TryFindNumericMember(candidate.GetType(), MaxAliases[kind], allowMaxNames: true, out var maxMember))
                {
                    maxMemberName = maxMember.Name;
                    readMax = BuildReader(candidate, maxMember);
                }
                else if (TryFindNumericMember(candidate.GetType(), ValueAliases[kind], allowMaxNames: true, out var fallbackMaxMember))
                {
                    // Last-resort: something like "healthMaximum" that also contains "health".
                    if (LooksLikeMax(fallbackMaxMember.Name))
                    {
                        maxMemberName = fallbackMaxMember.Name;
                        readMax = BuildReader(candidate, fallbackMaxMember);
                    }
                }

                binding = new StatBinding
                {
                    Target = candidate,
                    TargetTypeName = candidate.GetType().FullName ?? candidate.GetType().Name,
                    ValueMemberName = valueMember.Name,
                    ReadValue = readValue,
                    MaxMemberName = maxMemberName,
                    ReadMax = readMax
                };
                return true;
            }

            return false;
        }

        private static bool TryFindNumericMember(Type type, IEnumerable<string> aliases, bool allowMaxNames, out MemberInfo member)
        {
            member = null;
            if (type == null)
                return false;

            int bestScore = int.MinValue;
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            foreach (var field in type.GetFields(flags))
            {
                if (!IsNumericType(field.FieldType))
                    continue;

                if (TryScoreMember(field.Name, aliases, allowMaxNames, out int score) && score > bestScore)
                {
                    bestScore = score;
                    member = field;
                }
            }

            foreach (var prop in type.GetProperties(flags))
            {
                if (!prop.CanRead || prop.GetIndexParameters().Length > 0 || !IsNumericType(prop.PropertyType))
                    continue;

                if (TryScoreMember(prop.Name, aliases, allowMaxNames, out int score) && score > bestScore)
                {
                    bestScore = score;
                    member = prop;
                }
            }

            return member != null;
        }

        private static bool TryScoreMember(string memberName, IEnumerable<string> aliases, bool allowMaxNames, out int score)
        {
            score = int.MinValue;
            if (string.IsNullOrEmpty(memberName))
                return false;

            string normalized = Normalize(memberName);
            if (!allowMaxNames && LooksLikeMax(normalized))
                return false;

            bool matched = false;
            foreach (var alias in aliases)
            {
                string a = Normalize(alias);
                if (normalized == a)
                {
                    score = Math.Max(score, 1000);
                    matched = true;
                }
                else if (normalized.StartsWith(a, StringComparison.Ordinal))
                {
                    score = Math.Max(score, 800);
                    matched = true;
                }
                else if (normalized.Contains(a))
                {
                    score = Math.Max(score, 600);
                    matched = true;
                }
            }

            return matched;
        }

        private static Func<float?> BuildReader(object target, MemberInfo member)
        {
            if (target == null || member == null)
                return null;

            if (member is FieldInfo fi)
            {
                return () =>
                {
                    try { return ConvertToFloat(fi.GetValue(target)); } catch { return null; }
                };
            }

            if (member is PropertyInfo pi)
            {
                return () =>
                {
                    try { return ConvertToFloat(pi.GetValue(target, null)); } catch { return null; }
                };
            }

            return null;
        }

        private static StatValue Read(StatKind kind)
        {
            if (!Bindings.TryGetValue(kind, out var binding) || binding?.ReadValue == null)
                return new StatValue(false, 0f, false, 0f);

            float? value = binding.ReadValue.Invoke();
            if (!value.HasValue)
                return new StatValue(false, 0f, false, 0f);

            float? max = binding.ReadMax?.Invoke();
            bool hasMax = max.HasValue && max.Value > 0f;
            return new StatValue(true, value.Value, hasMax, hasMax ? max.Value : 0f);
        }

        private static void LogBinding(StatKind kind, StatBinding binding)
        {
            if (binding == null || binding.WasLogged)
                return;

            string maxName = string.IsNullOrEmpty(binding.MaxMemberName) ? "n/a" : binding.MaxMemberName;
            Plugin.DLog($"StatsProbe mapped {kind}: {binding.TargetTypeName}.{binding.ValueMemberName} (max: {maxName})");
            binding.WasLogged = true;
        }

        private static bool ContainsAny(string text, IEnumerable<string> patterns)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            string normalized = Normalize(text);
            foreach (var pattern in patterns)
            {
                if (normalized.Contains(Normalize(pattern)))
                    return true;
            }

            return false;
        }

        private static string Normalize(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return new string(value.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
        }

        private static bool LooksLikeMax(string memberNameOrNormalized)
        {
            string s = Normalize(memberNameOrNormalized);
            return s.Contains("max") || s.Contains("maximum");
        }

        private static bool IsNumericType(Type type)
        {
            if (type == null)
                return false;

            var t = Nullable.GetUnderlyingType(type) ?? type;
            return t == typeof(float)
                || t == typeof(double)
                || t == typeof(decimal)
                || t == typeof(int)
                || t == typeof(uint)
                || t == typeof(long)
                || t == typeof(ulong)
                || t == typeof(short)
                || t == typeof(ushort)
                || t == typeof(byte)
                || t == typeof(sbyte);
        }

        private static float? ConvertToFloat(object value)
        {
            if (value == null)
                return null;

            try
            {
                return Convert.ToSingle(value);
            }
            catch
            {
                return null;
            }
        }
    }
}
