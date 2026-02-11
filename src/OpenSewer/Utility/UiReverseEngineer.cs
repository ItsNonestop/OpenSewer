using BepInEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace OpenSewer.Utility
{
    internal class UiReverseEngineer : MonoBehaviour
    {
        private const KeyCode DumpAllUiKey = KeyCode.F8;
        private const KeyCode DumpHoveredUiKey = KeyCode.F9;
        private const KeyCode DumpHoveredCodeKey = KeyCode.F10;

        private static readonly string[] ProbePropertyNames =
        {
            "color", "text", "font", "fontSize", "alignment", "lineSpacing",
            "sprite", "overrideSprite", "material", "raycastTarget",
            "interactable", "transition", "targetGraphic", "image",
            "type", "fillMethod", "fillAmount", "preserveAspect"
        };

        private void Update()
        {
            if (Input.GetKeyDown(DumpAllUiKey))
                TryDumpFullUiHierarchy(out _);

            if (Input.GetKeyDown(DumpHoveredUiKey))
                TryDumpHoveredUi(out _);

            if (Input.GetKeyDown(DumpHoveredCodeKey))
                TryDumpHoveredBranchAsCode(6, out _);
        }

        public static bool TryDumpFullUiHierarchy(out string outputPath)
        {
            outputPath = string.Empty;

            try
            {
                Canvas[] canvases = Resources.FindObjectsOfTypeAll<Canvas>();
                StringBuilder sb = new(64 * 1024);
                sb.AppendLine($"OpenSewer UI Dump | {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
                sb.AppendLine($"Canvases: {canvases.Length}");
                sb.AppendLine();

                Array.Sort(canvases, (a, b) => string.CompareOrdinal(a.name, b.name));

                for (int i = 0; i < canvases.Length; i++)
                {
                    Canvas canvas = canvases[i];
                    if (canvas == null || canvas.transform == null)
                        continue;

                    var scaler = GetComponentByTypeName(canvas.gameObject, "UnityEngine.UI.CanvasScaler");
                    sb.AppendLine($"=== Canvas [{i}] {GetPath(canvas.transform)} ===");
                    sb.AppendLine($"activeSelf={canvas.gameObject.activeSelf} activeInHierarchy={canvas.gameObject.activeInHierarchy} enabled={canvas.enabled}");
                    sb.AppendLine($"renderMode={canvas.renderMode} sortingLayer={canvas.sortingLayerName} sortingOrder={canvas.sortingOrder} pixelPerfect={canvas.pixelPerfect}");
                    if (scaler != null)
                    {
                        string uiScaleMode = TryGetPropertyText(scaler, "uiScaleMode");
                        string referenceResolution = TryGetPropertyText(scaler, "referenceResolution");
                        string match = TryGetPropertyText(scaler, "matchWidthOrHeight");
                        sb.AppendLine($"CanvasScaler.uiScaleMode={uiScaleMode} refRes={referenceResolution} match={match}");
                    }

                    DumpTransformRecursive(canvas.transform, sb, 0);
                    sb.AppendLine();
                }

                outputPath = WriteDumpFile("ui_full", sb.ToString());
                Plugin.Log.LogInfo($"[OpenSewer] UI full dump written: {outputPath}");
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[OpenSewer] UI full dump failed: {ex}");
                return false;
            }
        }

        public static bool TryDumpHoveredUi(out string outputPath)
        {
            outputPath = string.Empty;

            try
            {
                Vector3 mouse = Input.mousePosition;
                Camera uiCam = null;
                StringBuilder sb = new();
                sb.AppendLine($"OpenSewer Hover Dump | {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"Mouse: {mouse}");

                if (!TryGetUiGraphics(out var graphics))
                {
                    Plugin.Log.LogWarning("[OpenSewer] Could not resolve UnityEngine.UI.Graphic for hover dump.");
                    return false;
                }

                List<Component> hits = new(64);

                for (int i = 0; i < graphics.Length; i++)
                {
                    var g = graphics[i] as Component;
                    if (g == null || !g.gameObject.activeInHierarchy)
                        continue;

                    bool raycastTarget = TryGetPropertyBool(g, "raycastTarget", false);
                    if (!raycastTarget)
                        continue;

                    RectTransform rt = g.transform as RectTransform;
                    if (rt == null)
                        continue;

                    if (RectTransformUtility.RectangleContainsScreenPoint(rt, mouse, uiCam))
                        hits.Add(g);
                }

                sb.AppendLine($"Hover hits: {hits.Count}");
                for (int i = 0; i < hits.Count; i++)
                {
                    var g = hits[i];
                    sb.AppendLine($"[{i}] {GetPath(g.transform)} :: {g.GetType().FullName}");
                    DumpComponentProps(g, sb, "  ");
                }

                outputPath = WriteDumpFile("ui_hover", sb.ToString());
                Plugin.Log.LogInfo($"[OpenSewer] UI hover dump written: {outputPath}");
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[OpenSewer] UI hover dump failed: {ex}");
                return false;
            }
        }

        public static bool TryDumpHoveredBranchAsCode(int maxDepth, out string outputPath)
        {
            outputPath = string.Empty;

            try
            {
                if (!TryFindTopHoveredTransform(out var target))
                {
                    Plugin.Log.LogWarning("[OpenSewer] No hovered UI element found for code dump.");
                    return false;
                }

                return TryDumpTransformAsCode(target, maxDepth, "ui_code_hover", out outputPath);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[OpenSewer] Hovered branch code dump failed: {ex}");
                return false;
            }
        }

        public static bool TryDumpPathAsCode(string path, int maxDepth, out string outputPath)
        {
            outputPath = string.Empty;

            try
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    Plugin.Log.LogWarning("[OpenSewer] Path code dump failed: path was empty.");
                    return false;
                }

                if (!TryFindByPath(path.Trim(), out var target) || target == null)
                {
                    Plugin.Log.LogWarning($"[OpenSewer] Path code dump failed: could not find transform at '{path}'.");
                    return false;
                }

                return TryDumpTransformAsCode(target, maxDepth, "ui_code_path", out outputPath);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[OpenSewer] Path code dump failed: {ex}");
                return false;
            }
        }

        private static bool TryDumpTransformAsCode(Transform root, int maxDepth, string prefix, out string outputPath)
        {
            outputPath = string.Empty;

            if (root == null)
                return false;

            int clampedDepth = Mathf.Clamp(maxDepth, 1, 20);
            StringBuilder sb = new(64 * 1024);
            sb.AppendLine($"OpenSewer UI C# Reference Dump | {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
            sb.AppendLine($"Root: {GetPath(root)}");
            sb.AppendLine($"MaxDepth: {clampedDepth}");
            sb.AppendLine();
            sb.AppendLine("// Generated reference: adapt naming/layout/style to your real implementation.");
            sb.AppendLine();

            var nameCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            EmitTransformAsCode(root, null, 0, clampedDepth, nameCounts, sb);

            outputPath = WriteDumpFile(prefix, sb.ToString());
            Plugin.Log.LogInfo($"[OpenSewer] UI code reference dump written: {outputPath}");
            return true;
        }

        private static void EmitTransformAsCode(
            Transform current,
            string parentVar,
            int depth,
            int maxDepth,
            Dictionary<string, int> nameCounts,
            StringBuilder sb)
        {
            if (current == null || depth > maxDepth)
                return;

            string indent = new(' ', depth * 4);
            string name = string.IsNullOrWhiteSpace(current.name) ? "Node" : current.name;
            string safeBase = SanitizeIdentifier(name);
            if (!nameCounts.TryGetValue(safeBase, out int existing))
                existing = 0;
            existing++;
            nameCounts[safeBase] = existing;

            string varName = existing == 1 ? safeBase : safeBase + existing;
            sb.Append(indent).Append("var ").Append(varName).Append(" = new GameObject(\"")
                .Append(EscapeString(name)).Append("\", typeof(RectTransform));").AppendLine();

            if (string.IsNullOrEmpty(parentVar))
                sb.Append(indent).Append("// root").AppendLine();
            else
                sb.Append(indent).Append(varName).Append(".transform.SetParent(").Append(parentVar).Append(".transform, false);").AppendLine();

            if (current is RectTransform rt)
            {
                sb.Append(indent).Append("var ").Append(varName).Append("Rt = ").Append(varName).Append(".GetComponent<RectTransform>();").AppendLine();
                sb.Append(indent).Append(varName).Append("Rt.anchorMin = new Vector2(").Append(FormatFloat(rt.anchorMin.x)).Append("f, ").Append(FormatFloat(rt.anchorMin.y)).Append("f);").AppendLine();
                sb.Append(indent).Append(varName).Append("Rt.anchorMax = new Vector2(").Append(FormatFloat(rt.anchorMax.x)).Append("f, ").Append(FormatFloat(rt.anchorMax.y)).Append("f);").AppendLine();
                sb.Append(indent).Append(varName).Append("Rt.pivot = new Vector2(").Append(FormatFloat(rt.pivot.x)).Append("f, ").Append(FormatFloat(rt.pivot.y)).Append("f);").AppendLine();
                sb.Append(indent).Append(varName).Append("Rt.anchoredPosition = new Vector2(").Append(FormatFloat(rt.anchoredPosition.x)).Append("f, ").Append(FormatFloat(rt.anchoredPosition.y)).Append("f);").AppendLine();
                sb.Append(indent).Append(varName).Append("Rt.sizeDelta = new Vector2(").Append(FormatFloat(rt.sizeDelta.x)).Append("f, ").Append(FormatFloat(rt.sizeDelta.y)).Append("f);").AppendLine();
            }

            sb.Append(indent).Append("// Source path: ").Append(GetPath(current)).AppendLine();

            var components = current.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                var component = components[i];
                if (component == null || component is Transform)
                    continue;

                sb.Append(indent).Append("// Component: ").Append(component.GetType().FullName).AppendLine();
                EmitComponentProperties(component, indent + "// ", sb);
            }

            sb.AppendLine();

            if (depth == maxDepth)
                return;

            for (int i = 0; i < current.childCount; i++)
                EmitTransformAsCode(current.GetChild(i), varName, depth + 1, maxDepth, nameCounts, sb);
        }

        private static void EmitComponentProperties(Component component, string prefix, StringBuilder sb)
        {
            Type type = component.GetType();
            for (int i = 0; i < ProbePropertyNames.Length; i++)
            {
                string propertyName = ProbePropertyNames[i];

                try
                {
                    PropertyInfo prop = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
                    if (prop == null || !prop.CanRead || prop.GetIndexParameters().Length > 0)
                        continue;

                    object value = prop.GetValue(component, null);
                    string text = FormatValue(value);
                    sb.Append(prefix).Append(propertyName).Append(" = ").AppendLine(text);
                }
                catch
                {
                    // Ignore properties that throw due to Unity state.
                }
            }
        }

        private static bool TryFindTopHoveredTransform(out Transform target)
        {
            target = null;
            if (!TryGetUiGraphics(out var graphics))
                return false;

            Vector3 mouse = Input.mousePosition;
            Camera uiCam = null;
            Component best = null;
            int bestDepth = int.MinValue;

            for (int i = 0; i < graphics.Length; i++)
            {
                var g = graphics[i] as Component;
                if (g == null || !g.gameObject.activeInHierarchy)
                    continue;

                bool raycastTarget = TryGetPropertyBool(g, "raycastTarget", false);
                if (!raycastTarget)
                    continue;

                if (g.transform is not RectTransform rt)
                    continue;

                if (!RectTransformUtility.RectangleContainsScreenPoint(rt, mouse, uiCam))
                    continue;

                int depth = GetTransformDepth(g.transform);
                if (depth > bestDepth)
                {
                    bestDepth = depth;
                    best = g;
                }
            }

            target = best?.transform;
            return target != null;
        }

        private static int GetTransformDepth(Transform t)
        {
            int depth = 0;
            Transform current = t;
            while (current != null)
            {
                depth++;
                current = current.parent;
            }
            return depth;
        }

        private static bool TryGetUiGraphics(out UnityEngine.Object[] graphics)
        {
            graphics = null;
            Type graphicType = Type.GetType("UnityEngine.UI.Graphic, UnityEngine.UI");
            if (graphicType == null)
                return false;

            graphics = Resources.FindObjectsOfTypeAll(graphicType);
            return graphics != null;
        }

        private static void DumpTransformRecursive(Transform root, StringBuilder sb, int depth)
        {
            if (root == null)
                return;

            string indent = new(' ', depth * 2);
            sb.Append(indent).Append("- ").Append(GetPath(root));
            sb.Append(" [active=").Append(root.gameObject.activeSelf)
              .Append(", inHierarchy=").Append(root.gameObject.activeInHierarchy).AppendLine("]");

            if (root is RectTransform rt)
            {
                sb.Append(indent).Append("  Rect: ");
                sb.Append("anchorMin=").Append(rt.anchorMin)
                  .Append(" anchorMax=").Append(rt.anchorMax)
                  .Append(" pivot=").Append(rt.pivot)
                  .Append(" anchoredPos=").Append(rt.anchoredPosition)
                  .Append(" sizeDelta=").Append(rt.sizeDelta)
                  .AppendLine();
            }

            Component[] comps = root.GetComponents<Component>();
            for (int i = 0; i < comps.Length; i++)
            {
                Component c = comps[i];
                if (c == null || c is Transform)
                    continue;

                sb.Append(indent).Append("  * ").Append(c.GetType().FullName).AppendLine();
                DumpComponentProps(c, sb, indent + "    ");
            }

            for (int i = 0; i < root.childCount; i++)
                DumpTransformRecursive(root.GetChild(i), sb, depth + 1);
        }

        private static void DumpComponentProps(Component component, StringBuilder sb, string indent)
        {
            if (component == null)
                return;

            Type type = component.GetType();
            for (int i = 0; i < ProbePropertyNames.Length; i++)
            {
                string propertyName = ProbePropertyNames[i];
                try
                {
                    PropertyInfo prop = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
                    if (prop == null || !prop.CanRead || prop.GetIndexParameters().Length > 0)
                        continue;

                    object value = prop.GetValue(component, null);
                    string text = FormatValue(value);
                    sb.Append(indent).Append(propertyName).Append(" = ").AppendLine(text);
                }
                catch
                {
                    // Some Unity properties throw depending on component state; skip.
                }
            }
        }

        private static string FormatValue(object value)
        {
            if (value == null)
                return "null";

            if (value is string s)
                return $"\"{s}\"";

            if (value is Color color)
                return $"Color({FormatFloat(color.r)}, {FormatFloat(color.g)}, {FormatFloat(color.b)}, {FormatFloat(color.a)})";

            if (value is Vector2 v2)
                return $"Vector2({FormatFloat(v2.x)}, {FormatFloat(v2.y)})";

            if (value is Vector3 v3)
                return $"Vector3({FormatFloat(v3.x)}, {FormatFloat(v3.y)}, {FormatFloat(v3.z)})";

            if (value is UnityEngine.Object uo)
                return $"{uo.GetType().Name}({uo.name})";

            return value.ToString();
        }

        private static string GetPath(Transform t)
        {
            if (t == null)
                return "(null)";

            Stack<string> parts = new();
            Transform cur = t;
            while (cur != null)
            {
                parts.Push(cur.name);
                cur = cur.parent;
            }

            return string.Join("/", parts.ToArray());
        }

        private static string WriteDumpFile(string kind, string content)
        {
            string dir = Path.Combine(Paths.BepInExRootPath, "LogOutput_OpenSewer");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, $"{kind}_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            File.WriteAllText(path, content, Encoding.UTF8);
            return path;
        }

        private static Component GetComponentByTypeName(GameObject go, string fullTypeName)
        {
            if (go == null || string.IsNullOrEmpty(fullTypeName))
                return null;

            Component[] components = go.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                Component c = components[i];
                if (c != null && string.Equals(c.GetType().FullName, fullTypeName, StringComparison.Ordinal))
                    return c;
            }

            return null;
        }

        private static string TryGetPropertyText(Component c, string propertyName)
        {
            try
            {
                PropertyInfo p = c.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
                if (p == null || !p.CanRead)
                    return "n/a";

                object value = p.GetValue(c, null);
                return value?.ToString() ?? "null";
            }
            catch
            {
                return "n/a";
            }
        }

        private static bool TryGetPropertyBool(Component c, string propertyName, bool defaultValue)
        {
            try
            {
                PropertyInfo p = c.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
                if (p == null || !p.CanRead || p.PropertyType != typeof(bool))
                    return defaultValue;

                object value = p.GetValue(c, null);
                return value is bool b ? b : defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        private static bool TryFindByPath(string path, out Transform result)
        {
            result = null;
            if (string.IsNullOrWhiteSpace(path))
                return false;

            string expected = path.Trim();
            var all = Resources.FindObjectsOfTypeAll<Transform>();
            for (int i = 0; i < all.Length; i++)
            {
                var t = all[i];
                if (t == null)
                    continue;
                if (!string.Equals(GetPath(t), expected, StringComparison.Ordinal))
                    continue;

                result = t;
                return true;
            }

            return false;
        }

        private static string SanitizeIdentifier(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "Node";

            StringBuilder sb = new(name.Length + 4);
            for (int i = 0; i < name.Length; i++)
            {
                char ch = name[i];
                if (char.IsLetterOrDigit(ch) || ch == '_')
                    sb.Append(ch);
            }

            if (sb.Length == 0)
                sb.Append("Node");

            if (!char.IsLetter(sb[0]) && sb[0] != '_')
                sb.Insert(0, '_');

            return sb.ToString();
        }

        private static string EscapeString(string value)
        {
            return (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static string FormatFloat(float value)
        {
            return value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
