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
