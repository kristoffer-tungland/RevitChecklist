using System;
using System.Diagnostics;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;

namespace ChecklistAddin
{
    public static class AddinConfig
    {
        public const string Url = "http://localhost:51789/";
    }

    public class App : IExternalApplication
    {
        private ChecklistServer.Server? _server;

        public Result OnStartup(UIControlledApplication application)
        {
            // Subscribe to Idling event to get UIApplication as soon as possible
            application.Idling += OnIdling;

            _server = new ChecklistServer.Server(AddinConfig.Url);
            _server.Start();

            var panel = application.CreateRibbonPanel("Quality Checklist");
            var buttonData = new PushButtonData(
                "OpenChecklist",
                "Checklist",
                System.Reflection.Assembly.GetExecutingAssembly().Location,
                typeof(OpenBrowserCommand).FullName
            );
            panel.AddItem(buttonData);

            return Result.Succeeded;
        }

        private void OnIdling(object sender, Autodesk.Revit.UI.Events.IdlingEventArgs e)
        {
            // Initialize RevitApi with UIApplication when Revit becomes idle
            if (!ChecklistServer.RevitApi.IsInitialized && sender is UIApplication uiApp)
            {
                ChecklistServer.RevitApi.Initialize(uiApp);
                
                // Unsubscribe after initialization
                uiApp.Idling -= OnIdling;
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            _server?.Stop();
            return Result.Succeeded;
        }
    }

    [Transaction(TransactionMode.Manual)]
    public class OpenBrowserCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, Autodesk.Revit.DB.ElementSet elements)
        {
            try
            {
                // Ensure RevitApi is initialized (backup initialization)
                if (!ChecklistServer.RevitApi.IsInitialized)
                {
                    ChecklistServer.RevitApi.Initialize(commandData.Application);
                }
                
                Process.Start(new ProcessStartInfo(AddinConfig.Url) { UseShellExecute = true });
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}
