using Autodesk.Revit.UI;

namespace ChecklistServer
{
    public static class RevitApi
    {
        private static UIApplication? _uiApp;

        public static void Initialize(UIApplication app)
        {
            _uiApp = app;
        }

        public static bool IsInitialized => _uiApp != null;

        private static UIApplication UIApp => _uiApp ?? throw new InvalidOperationException("Revit API not initialized. Call Initialize() first with UIApplication.");

        public static string GetCurrentUsername()
        {
            try
            {
                var user = UIApp.Application.Username;
                return string.IsNullOrEmpty(user) ? "Unknown User" : user;
            }
            catch
            {
                return Environment.UserName ?? "Unknown User";
            }
        }

        public static List<string> PromptForElementSelection(string message, bool multiple)
        {
            // TODO: implement element selection using UIDocument when available
            return new List<string>();
        }
    }
}
