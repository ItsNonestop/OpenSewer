using UnityEngine;

namespace OpenSewer.Utility
{
    internal class InputHandler : MonoBehaviour
    {
        bool IsMenu => GameUIController.instance?.IsMenuVisible ?? false;
        bool IsInventory => GameUIController.instance?.InventoryIsVisible() ?? false;
        bool IsBuildMenu => BuildingSystemMenu.instance?.menuIsOpen ?? false;
        bool IsPaused => GameController.instance?.GameIsPaused ?? false;

        float nextUpdateLogTime;
        float nextGuardLogTime;

        void Update()
        {
            if (Time.time >= nextUpdateLogTime)
            {
                Plugin.DLog("InputHandler.Update heartbeat");
                nextUpdateLogTime = Time.time + 2f;
            }

            if (!GameUIController.instance || !ItemDatabase.instance)
            {
                if (Time.time >= nextGuardLogTime)
                {
                    Plugin.DLog($"Toggle blocked: MissingRefs GameUIController={(GameUIController.instance != null)} ItemDatabase={(ItemDatabase.instance != null)}");
                    nextGuardLogTime = Time.time + 2f;
                }
                return;
            }

            bool uPressed = Input.GetKeyDown(KeyCode.U);
            if (uPressed)
                Plugin.DLog("U pressed - attempting toggle");

            if (Plugin.GUIRunner == null)
            {
                if (uPressed || Time.time >= nextGuardLogTime)
                {
                    Plugin.DLog("Toggle blocked: GuiRunner is null");
                    nextGuardLogTime = Time.time + 2f;
                }
                return;
            }

            bool blockedByMenu = IsMenu && !IsInventory;
            bool blockedByBuildMenu = IsBuildMenu;
            bool blockedByPaused = IsPaused;
            if (blockedByMenu || blockedByBuildMenu || blockedByPaused)
            {
                if (uPressed || Time.time >= nextGuardLogTime)
                {
                    Plugin.DLog($"Toggle blocked: IsPaused={blockedByPaused} IsMenu={IsMenu} IsInventory={IsInventory} IsBuildMenu={blockedByBuildMenu}");
                    nextGuardLogTime = Time.time + 2f;
                }
                return;
            }

            if (uPressed)
            {
                if (Plugin.GUIRunner.enabled)
                    Close();
                else
                    Open();
            }

            if (InputManager.instance.GetKeyDown("Pause Menu") || InputManager.instance.GetKeyDown("Menu"))
            {
                Close();
            }
        }

        System.Collections.IEnumerator ControlsEnabled(bool enable)
        {
            yield return new WaitForEndOfFrame();
            if (enable)
                GameController.instance.ControlsEnabled(this.gameObject);
            else
                GameController.instance.ControlsDisabled(gameObject: this.gameObject, ShowCursor: true);
        }

        void Open()
        {
            Plugin.DLog("Open() called: enabling GuiRunner and disabling controls");
            Plugin.GUIRunner.Show();
            Plugin.DLog($"GuiRunner.enabled={Plugin.GUIRunner.enabled}");
            StartCoroutine(ControlsEnabled(false));
        }

        void Close()
        {
            Plugin.DLog("Close() called: disabling GuiRunner and enabling controls");
            Plugin.GUIRunner.Hide();
            Plugin.DLog($"GuiRunner.enabled={Plugin.GUIRunner.enabled}");
            StartCoroutine(ControlsEnabled(true));
        }
    }
}

