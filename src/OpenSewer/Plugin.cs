using BepInEx;
using BepInEx.Logging;
using OpenSewer.Utility;
using System;
using UnityEngine;

namespace OpenSewer;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public class Plugin : BaseUnityPlugin
{
    internal const string PluginGuid = "com.opensewer.mod";
    internal const string PluginName = "OpenSewer";
    internal const string PluginVersion = "0.5.0";

    internal static GuiRunner GUIRunner;
    internal static ManualLogSource Log;
    public static bool DebugEnabled = true;

    public static void DLog(string msg)
    {
        if (!DebugEnabled) return;
        Log?.LogInfo($"[OpenSewerDebug] {msg}");
    }

    private void Awake()
    {
        Log = Logger;

        Console.WriteLine($"Plugin {PluginGuid} is loaded!");
        DLog("Plugin Awake completed");
        DLog("Creating GuiRunner...");
        try
        {
            var gui = gameObject.AddComponent<GuiRunner>();
            if (gui == null)
            {
                Logger.LogError("[OpenSewerDebug] ERROR: GuiRunner creation returned null. Aborting GUI setup.");
                GUIRunner = null;
                return;
            }

            GUIRunner = gui;
            GUIRunner.enabled = false;
            DLog("GuiRunner created OK");
            DLog($"GuiRunner.enabled={GUIRunner.enabled}");
        }
        catch (Exception ex)
        {
            Logger.LogError($"[OpenSewerDebug] ERROR: Exception while creating GuiRunner: {ex}");
            GUIRunner = null;
            return;
        }

        gameObject.AddComponent<InputHandler>();
        gameObject.AddComponent<StatFreezer>();
        gameObject.AddComponent<TimeFreezer>();

        Application.targetFrameRate = 30;
    }
}



