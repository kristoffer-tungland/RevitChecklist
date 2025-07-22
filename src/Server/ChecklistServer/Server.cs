using System;
using System.Net;
using System.Text;
using System.Text.Json;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using ChecklistServer.Models;

namespace ChecklistServer
{
    public class Server
    {
        private readonly HttpListener _listener;
        private readonly string _baseUrl;
        private readonly string _indexHtml;

        public bool IsRunning => _listener.IsListening;

        public Server(string baseUrl)
        {
            _baseUrl = baseUrl;
            _listener = new HttpListener();
            _listener.Prefixes.Add(baseUrl);
            _indexHtml = LoadIndexHtml();
        }

        private static string LoadIndexHtml()
        {
            var asm = typeof(Server).Assembly;
            using var stream = asm.GetManifestResourceStream(typeof(Server).Namespace + ".index.html");
            if (stream == null)
                return "<html><body>index.html not found</body></html>";
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        public void Start()
        {
            if (_listener.IsListening) return;
            _listener.Start();
            Console.WriteLine($"Server started at {_baseUrl}");
            Logger.Log($"Server started at {_baseUrl}");
            _listener.BeginGetContext(OnContext, null);
        }

        public void Stop()
        {
            if (_listener.IsListening)
            {
                _listener.Stop();
                _listener.Close();
                Console.WriteLine("Server stopped");
                Logger.Log("Server stopped");
            }
        }

        private void OnContext(IAsyncResult ar)
        {
            if (!_listener.IsListening) return;
            var context = _listener.EndGetContext(ar);
            _listener.BeginGetContext(OnContext, null);
            try
            {
                HandleRequest(context);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Logger.Log(ex);
                context.Response.StatusCode = 500;
                var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { error = ex.Message }));
                context.Response.OutputStream.Write(bytes, 0, bytes.Length);
            }
            finally
            {
                context.Response.Close();
            }
        }

        private void HandleRequest(HttpListenerContext context)
        {
            var req = context.Request;
            var res = context.Response;

            Console.WriteLine($"{req.HttpMethod} {req.Url!.AbsolutePath}");

            if (req.HttpMethod == "GET" && (req.Url.AbsolutePath == "/" || req.Url.AbsolutePath == "/index.html"))
            {
                var bytes = Encoding.UTF8.GetBytes(_indexHtml);
                res.ContentType = "text/html";
                res.ContentLength64 = bytes.Length;
                res.OutputStream.Write(bytes, 0, bytes.Length);
                return;
            }

            if (req.HttpMethod == "GET" && req.Url.AbsolutePath == "/api/status")
            {
                WriteJson(res, new { status = "ok" });
                return;
            }
            if (req.HttpMethod == "GET" && req.Url.AbsolutePath == "/api/user")
            {
                WriteJson(res, new { user = RevitApi.GetCurrentUsername() });
                return;
            }
            if (req.HttpMethod == "GET" && req.Url.AbsolutePath == "/api/log")
            {
                WriteJson(res, new { log = Logger.ReadAll() });
                return;
            }
            if (req.HttpMethod == "POST" && req.Url.AbsolutePath == "/api/select-elements")
            {
                using var reader = new StreamReader(req.InputStream, req.ContentEncoding);
                var body = reader.ReadToEnd();
                var payload = JsonSerializer.Deserialize<SelectElementsRequest>(body);
                var elements = RevitApi.PromptForElementSelection(
                    payload?.Message ?? string.Empty,
                    payload?.Count == "multiple",
                    payload?.AllowedCategories);
                WriteJson(res, new { status = "ok", selectedElementUniqueIds = elements });
                return;
            }

            if (req.Url.AbsolutePath.StartsWith("/api/templates"))
            {
                HandleTemplates(req, res);
                return;
            }

            if (req.Url.AbsolutePath.StartsWith("/api/checks"))
            {
                HandleChecks(req, res);
                return;
            }
            res.StatusCode = 404;
        }

        private void HandleTemplates(HttpListenerRequest req, HttpListenerResponse res)
        {
            if (req.HttpMethod == "GET" && req.Url.AbsolutePath == "/api/templates")
            {
                var templates = LoadTemplates();
                WriteJson(res, templates);
                return;
            }
            if (req.HttpMethod == "POST" && req.Url.AbsolutePath == "/api/templates")
            {
                using var reader = new StreamReader(req.InputStream, req.ContentEncoding);
                var body = reader.ReadToEnd();
                var template = JsonSerializer.Deserialize<Template>(body) ?? new Template();
                template.Id = Guid.NewGuid();
                template.CreatedBy = RevitApi.GetCurrentUsername();
                template.CreatedDate = DateTime.UtcNow;
                template.ModifiedBy = template.CreatedBy;
                template.ModifiedDate = template.CreatedDate;
                var json = JsonSerializer.Serialize(template);
                template.DataStorageUniqueId = DataStorageService.SaveJson(json, StorageType.Template, $"Created template {template.Name}");
                WriteJson(res, template);
                return;
            }

            if (req.HttpMethod == "PUT" && req.Url.AbsolutePath.StartsWith("/api/templates/"))
            {
                var idStr = req.Url.AbsolutePath.Substring("/api/templates/".Length);
                if (Guid.TryParse(idStr, out var id))
                {
                    var templates = LoadTemplates();
                    var tmpl = templates.FirstOrDefault(t => t.Id == id);
                    if (tmpl == null) { res.StatusCode = 404; return; }
                    using var reader = new StreamReader(req.InputStream, req.ContentEncoding);
                    var body = reader.ReadToEnd();
                    var update = JsonSerializer.Deserialize<Template>(body);
                    if (update != null)
                    {
                        tmpl.Name = update.Name;
                        tmpl.Description = update.Description;
                        tmpl.Sections = update.Sections;
                        tmpl.Archived = update.Archived;
                        tmpl.ModifiedBy = RevitApi.GetCurrentUsername();
                        tmpl.ModifiedDate = DateTime.UtcNow;
                        DataStorageService.SaveJson(JsonSerializer.Serialize(tmpl), StorageType.Template, $"Updated template {tmpl.Name}", tmpl.DataStorageUniqueId);
                        WriteJson(res, tmpl);
                        return;
                    }
                }
            }

            if (req.HttpMethod == "DELETE" && req.Url.AbsolutePath.StartsWith("/api/templates/"))
            {
                var idStr = req.Url.AbsolutePath.Substring("/api/templates/".Length);
                if (Guid.TryParse(idStr, out var id))
                {
                    var templates = LoadTemplates();
                    var tmpl = templates.FirstOrDefault(t => t.Id == id);
                    if (tmpl == null) { res.StatusCode = 404; return; }
                    DataStorageService.Delete(tmpl.DataStorageUniqueId);
                    WriteJson(res, new { status = "ok" });
                    return;
                }
            }

            if (req.HttpMethod == "POST" && req.Url.AbsolutePath.EndsWith("/archive"))
            {
                var idStr = req.Url.AbsolutePath.Substring("/api/templates/".Length);
                idStr = idStr.Replace("/archive", string.Empty);
                if (Guid.TryParse(idStr, out var id))
                {
                    var templates = LoadTemplates();
                    var tmpl = templates.FirstOrDefault(t => t.Id == id);
                    if (tmpl == null) { res.StatusCode = 404; return; }
                    tmpl.Archived = true;
                    tmpl.ModifiedBy = RevitApi.GetCurrentUsername();
                    tmpl.ModifiedDate = DateTime.UtcNow;
                    DataStorageService.SaveJson(JsonSerializer.Serialize(tmpl), StorageType.Template, $"Updated template {tmpl.Name}", tmpl.DataStorageUniqueId);
                    WriteJson(res, tmpl);
                    return;
                }
            }

            res.StatusCode = 404;
        }

        private void HandleChecks(HttpListenerRequest req, HttpListenerResponse res)
        {
            if (req.HttpMethod == "GET" && req.Url.AbsolutePath == "/api/checks")
            {
                WriteJson(res, LoadChecks());
                return;
            }

            if (req.HttpMethod == "POST" && req.Url.AbsolutePath == "/api/checks")
            {
                using var reader = new StreamReader(req.InputStream, req.ContentEncoding);
                var body = reader.ReadToEnd();
                var check = JsonSerializer.Deserialize<Check>(body) ?? new Check();
                check.Id = Guid.NewGuid();
                check.CreatedBy = RevitApi.GetCurrentUsername();
                check.CreatedDate = DateTime.UtcNow;
                check.ModifiedBy = check.CreatedBy;
                check.ModifiedDate = check.CreatedDate;
                var json = JsonSerializer.Serialize(check);
                check.DataStorageUniqueId = DataStorageService.SaveJson(json, StorageType.Check, $"Created check {check.Id}");
                WriteJson(res, check);
                return;
            }

            if (req.HttpMethod == "GET" && req.Url.AbsolutePath.StartsWith("/api/checks/"))
            {
                var idStr = req.Url.AbsolutePath.Substring("/api/checks/".Length);
                if (Guid.TryParse(idStr, out var id))
                {
                    var check = LoadChecks().FirstOrDefault(c => c.Id == id);
                    if (check == null) { res.StatusCode = 404; return; }
                    WriteJson(res, check);
                    return;
                }
            }

            if (req.HttpMethod == "PUT" && req.Url.AbsolutePath.StartsWith("/api/checks/"))
            {
                var idStr = req.Url.AbsolutePath.Substring("/api/checks/".Length);
                if (Guid.TryParse(idStr, out var id))
                {
                    var checks = LoadChecks();
                    var chk = checks.FirstOrDefault(c => c.Id == id);
                    if (chk == null) { res.StatusCode = 404; return; }
                    using var reader = new StreamReader(req.InputStream, req.ContentEncoding);
                    var body = reader.ReadToEnd();
                    var update = JsonSerializer.Deserialize<Check>(body);
                    if (update != null)
                    {
                        chk.TemplateUniqueId = update.TemplateUniqueId;
                        chk.TemplateSnapshot = update.TemplateSnapshot;
                        chk.CheckedElements = update.CheckedElements;
                        chk.Answers = update.Answers;
                        chk.Status = update.Status;
                        chk.ModifiedBy = RevitApi.GetCurrentUsername();
                        chk.ModifiedDate = DateTime.UtcNow;
                        DataStorageService.SaveJson(JsonSerializer.Serialize(chk), StorageType.Check, $"Updated check {chk.Id}", chk.DataStorageUniqueId);
                        WriteJson(res, chk);
                        return;
                    }
                }
            }

            res.StatusCode = 404;
        }

        private static List<Template> LoadTemplates()
        {
            var list = new List<Template>();
            foreach (var id in DataStorageService.GetAll(StorageType.Template))
            {
                var json = DataStorageService.GetJson(id, StorageType.Template);
                if (json == null) continue;
                try
                {
                    var t = JsonSerializer.Deserialize<Template>(json);
                    if (t != null) list.Add(t);
                }
                catch { }
            }
            return list;
        }

        private static List<Check> LoadChecks()
        {
            var list = new List<Check>();
            foreach (var id in DataStorageService.GetAll(StorageType.Check))
            {
                var json = DataStorageService.GetJson(id, StorageType.Check);
                if (json == null) continue;
                try
                {
                    var c = JsonSerializer.Deserialize<Check>(json);
                    if (c != null && c.TemplateSnapshot != null)
                        list.Add(c);
                }
                catch { }
            }
            return list;
        }

        private static void WriteJson(HttpListenerResponse res, object data)
        {
            var json = JsonSerializer.Serialize(data);
            var bytes = Encoding.UTF8.GetBytes(json);
            res.ContentType = "application/json";
            res.ContentLength64 = bytes.Length;
            res.OutputStream.Write(bytes, 0, bytes.Length);
        }
    }

    public class SelectElementsRequest
    {
        public string? Count { get; set; }
        public string[]? AllowedCategories { get; set; }
        public string? Message { get; set; }
    }
}
