using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace ChecklistServer
{
    public static class RevitApi
    {
        private static UIApplication? _uiApp;
        private static ExternalEvent? _externalEvent;
        private static readonly ConcurrentQueue<Action> _queue = new();

        private class EventHandler : IExternalEventHandler
        {
            public string GetName() => "ChecklistExternalEvent";

            public void Execute(UIApplication app)
            {
                while (_queue.TryDequeue(out var action))
                {
                    try { action(); }
                    catch (Exception ex) { Logger.Log(ex); }
                }
            }
        }

        public static void Initialize(UIApplication app)
        {
            _uiApp = app;
            if (_externalEvent == null)
            {
                _externalEvent = ExternalEvent.Create(new EventHandler());
            }
        }

        public static bool IsInitialized => _uiApp != null && _externalEvent != null;

        private static UIApplication UIApp => _uiApp ?? throw new InvalidOperationException("Revit API not initialized. Call Initialize() first with UIApplication.");

        public static UIDocument UIDoc => UIApp.ActiveUIDocument;

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

        public static void Invoke(Action action)
        {
            if (_externalEvent == null)
                throw new InvalidOperationException("Revit API not initialized.");
            var tcs = new TaskCompletionSource<bool>();
            _queue.Enqueue(() =>
            {
                try
                {
                    action();
                    tcs.SetResult(true);
                }
               catch (Exception ex)
               {
                    Logger.Log(ex);
                    tcs.SetException(ex);
                }
            });
            _externalEvent.Raise();
            tcs.Task.Wait();
        }

        public static T Invoke<T>(Func<T> func)
        {
            if (_externalEvent == null)
                throw new InvalidOperationException("Revit API not initialized.");
            T? result = default;
            var tcs = new TaskCompletionSource<bool>();
            _queue.Enqueue(() =>
            {
                try
                {
                    result = func();
                    tcs.SetResult(true);
                }
               catch (Exception ex)
               {
                    Logger.Log(ex);
                    tcs.SetException(ex);
                }
            });
            _externalEvent.Raise();
            tcs.Task.Wait();
            return result!;
        }

        public static List<string> PromptForElementSelection(string message, bool multiple, IList<string>? allowedCategories = null)
        {
            return Invoke(() =>
            {
                var uidoc = UIDoc;
                ISelectionFilter? filter = null;
                if (allowedCategories != null && allowedCategories.Count > 0)
                {
                    var cats = new HashSet<BuiltInCategory>();
                    foreach (var c in allowedCategories)
                    {
                        if (Enum.TryParse(c, out BuiltInCategory bic))
                            cats.Add(bic);
                    }
                    filter = new CategorySelectionFilter(cats);
                }

                var ids = new List<string>();
                try
                {
                    if (multiple)
                    {
                        IList<Reference> refs = filter != null
                            ? uidoc.Selection.PickObjects(ObjectType.Element, filter, message)
                            : uidoc.Selection.PickObjects(ObjectType.Element, message);
                        foreach (var r in refs)
                        {
                            var el = uidoc.Document.GetElement(r);
                            if (el != null) ids.Add(el.UniqueId);
                        }
                    }
                    else
                    {
                        Reference? r = filter != null
                            ? uidoc.Selection.PickObject(ObjectType.Element, filter, message)
                            : uidoc.Selection.PickObject(ObjectType.Element, message);
                        if (r != null)
                        {
                            var el = uidoc.Document.GetElement(r);
                            if (el != null) ids.Add(el.UniqueId);
                        }
                    }
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                {
                    // user cancelled
                }
                return ids;
            });
        }

        private class CategorySelectionFilter : Autodesk.Revit.UI.Selection.ISelectionFilter
        {
            private readonly HashSet<BuiltInCategory> _categories;
            public CategorySelectionFilter(HashSet<BuiltInCategory> categories)
            {
                _categories = categories;
            }
            public bool AllowElement(Element elem)
            {
                return elem.Category != null && _categories.Contains((BuiltInCategory)elem.Category.Id.IntegerValue);
            }
            public bool AllowReference(Reference reference, XYZ position) => false;
        }
    }
}
