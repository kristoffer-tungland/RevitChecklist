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
        internal static ChecklistServer.Server? ServerInstance;

        public Result OnStartup(UIControlledApplication application)
        {
            var panel = application.CreateRibbonPanel("Quality Checklist");
            var buttonData = new PushButtonData(
                "StartChecklist",
                "Checklist",
                System.Reflection.Assembly.GetExecutingAssembly().Location,
                typeof(OpenBrowserCommand).FullName
            );
            panel.AddItem(buttonData);

            ChecklistServer.Logger.Log("Add-in started");

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            ServerInstance?.Stop();
            ChecklistServer.Logger.Log("Add-in shutdown");
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
                // Ensure RevitApi is initialized
                if (!ChecklistServer.RevitApi.IsInitialized)
                {
                    ChecklistServer.RevitApi.Initialize(commandData.Application);
                }

                if (App.ServerInstance == null)
                {
                    App.ServerInstance = new ChecklistServer.Server(AddinConfig.Url);
                }
                if (!App.ServerInstance.IsRunning)
                {
                    App.ServerInstance.Start();
                }

                Process.Start(new ProcessStartInfo(AddinConfig.Url) { UseShellExecute = true });
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                ChecklistServer.Logger.Log(ex);
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}
