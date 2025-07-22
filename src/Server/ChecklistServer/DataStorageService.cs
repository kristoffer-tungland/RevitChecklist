using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;

namespace ChecklistServer
{
    public enum StorageType
    {
        Template,
        Check
    }

    public static class DataStorageService
    {
        private static readonly Guid TemplateSchemaGuid = new("da3f0c02-3d07-4f84-867b-dcbd82272d15");
        private static readonly Guid CheckSchemaGuid = new("b14f6b43-afe7-46cd-b13f-565ff6562a68");

        private static Schema? _templateSchema;
        private static Schema? _checkSchema;

        private static Schema GetSchema(StorageType type)
        {
            if (type == StorageType.Template)
            {
                if (_templateSchema != null) return _templateSchema;
                _templateSchema = Schema.Lookup(TemplateSchemaGuid);
                if (_templateSchema != null) return _templateSchema;
                var builder = new SchemaBuilder(TemplateSchemaGuid);
                builder.AddSimpleField("Json", typeof(string));
                builder.SetReadAccessLevel(AccessLevel.Public);
                builder.SetWriteAccessLevel(AccessLevel.Public);
                _templateSchema = builder.Finish();
                return _templateSchema;
            }

            if (_checkSchema != null) return _checkSchema;
            _checkSchema = Schema.Lookup(CheckSchemaGuid);
            if (_checkSchema != null) return _checkSchema;
            var b = new SchemaBuilder(CheckSchemaGuid);
            b.AddSimpleField("Json", typeof(string));
            b.SetReadAccessLevel(AccessLevel.Public);
            b.SetWriteAccessLevel(AccessLevel.Public);
            _checkSchema = b.Finish();
            return _checkSchema;
        }

        public static string SaveJson(string json, StorageType type, string transactionName, string? uniqueId = null)
        {
            return RevitApi.Invoke(() =>
            {
                var doc = RevitApi.UIDoc.Document;
                using var tx = new Transaction(doc, transactionName);
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
                var entity = new Entity(GetSchema(type));
                entity.Set("Json", json);
                storage.SetEntity(entity);
                tx.Commit();
                return storage.UniqueId;
            });
        }

        public static string? GetJson(string uniqueId, StorageType type)
        {
            return RevitApi.Invoke(() =>
            {
                var doc = RevitApi.UIDoc.Document;
                var storage = doc.GetElement(uniqueId) as DataStorage;
                if (storage == null) return null;
                var entity = storage.GetEntity(GetSchema(type));
                return entity.IsValid() ? entity.Get<string>("Json") : null;
            });
        }

        public static void Delete(string uniqueId)
        {
            RevitApi.Invoke(() =>
            {
                var doc = RevitApi.UIDoc.Document;
                using var tx = new Transaction(doc, "Delete DataStorage");
                tx.Start();
                var el = doc.GetElement(uniqueId);
                if (el != null) doc.Delete(el.Id);
                tx.Commit();
            });
        }

        public static List<string> GetAll(StorageType type)
        {
            return RevitApi.Invoke(() =>
            {
                var doc = RevitApi.UIDoc.Document;
                var collector = new FilteredElementCollector(doc).OfClass(typeof(DataStorage));
                var ids = new List<string>();
                var schema = GetSchema(type);
                foreach (DataStorage ds in collector)
                {
                    var entity = ds.GetEntity(schema);
                    if (entity.IsValid()) ids.Add(ds.UniqueId);
                }
                return ids;
            });
        }
    }
}
