namespace OpenSewer
{
    internal class GuiRunner : GUIComponent
    {
        bool loggedOnGuiThisOpen;

        public void Show()
        {
            enabled = true;
            loggedOnGuiThisOpen = false;
        }

        public void Hide()
        {
            enabled = false;
            loggedOnGuiThisOpen = false;
        }

        void OnEnable()
        {
            loggedOnGuiThisOpen = false;
        }

        void OnDisable()
        {
            loggedOnGuiThisOpen = false;
        }

        void OnGUI()
        {
            if (!loggedOnGuiThisOpen)
            {
                Plugin.DLog("OnGUI drawing OK");
                loggedOnGuiThisOpen = true;
            }

            DrawGui();
        }
    }
}

