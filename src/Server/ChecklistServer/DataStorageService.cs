using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;

namespace ChecklistServer
{
    public static class DataStorageService
    {
        private static readonly Guid SchemaGuid = new("6ea71164-9b45-4c05-9c24-3fbfa1d11c77");
        private static Schema? _schema;

        private static Schema Schema
        {
            get
            {
                if (_schema != null) return _schema;
                _schema = Schema.Lookup(SchemaGuid);
                if (_schema != null) return _schema;
                var builder = new SchemaBuilder(SchemaGuid);
                builder.AddSimpleField("Json", typeof(string));
                builder.SetReadAccessLevel(AccessLevel.Public);
                builder.SetWriteAccessLevel(AccessLevel.Public);
                _schema = builder.Finish();
                return _schema;
            }
        }

        public static string SaveJson(string json, string? uniqueId = null)
        {
            var doc = RevitApi.UIDoc.Document;
            using var tx = new Transaction(doc, "Save DataStorage");
            tx.Start();
            DataStorage storage;
            if (string.IsNullOrEmpty(uniqueId))
            {
                storage = DataStorage.Create(doc);
            }
            else
            {
                storage = doc.GetElement(uniqueId) as DataStorage ?? DataStorage.Create(doc);
            }
            var entity = new Entity(Schema);
            entity.Set("Json", json);
            storage.SetEntity(entity);
            tx.Commit();
            return storage.UniqueId;
        }

        public static string? GetJson(string uniqueId)
        {
            var doc = RevitApi.UIDoc.Document;
            var storage = doc.GetElement(uniqueId) as DataStorage;
            if (storage == null) return null;
            var entity = storage.GetEntity(Schema);
            return entity.IsValid() ? entity.Get<string>("Json") : null;
        }

        public static void Delete(string uniqueId)
        {
            var doc = RevitApi.UIDoc.Document;
            using var tx = new Transaction(doc, "Delete DataStorage");
            tx.Start();
            var el = doc.GetElement(uniqueId);
            if (el != null) doc.Delete(el.Id);
            tx.Commit();
        }

        public static List<string> GetAll()
        {
            var doc = RevitApi.UIDoc.Document;
            var collector = new FilteredElementCollector(doc).OfClass(typeof(DataStorage));
            var ids = new List<string>();
            foreach (DataStorage ds in collector)
            {
                var entity = ds.GetEntity(Schema);
                if (entity.IsValid()) ids.Add(ds.UniqueId);
            }
            return ids;
        }
    }
}
