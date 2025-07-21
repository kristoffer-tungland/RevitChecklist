using System.Collections.Generic;
#if NET48
using Autodesk.Revit.ApplicationServices;
#endif

namespace ChecklistServer
{
    public static class RevitApi
    {
        #if NET48
        private static object? _app;

        public static void Initialize(object app)
        {
            _app = app;
        }

        public static string GetCurrentUsername()
        {
            try
            {
                return (string?)(_app?.GetType().GetProperty("Username")?.GetValue(_app)) ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
        #else
        public static void Initialize(object? app) { }

        public static string GetCurrentUsername() => string.Empty;
        #endif

        public static List<string> PromptForElementSelection(string message, bool multiple)
        {
            // TODO: implement element selection using UIDocument when available
            return new List<string>();
        }
    }
}
