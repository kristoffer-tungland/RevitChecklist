using System;
using System.Net;
using System.Text;
using System.Text.Json;
using System.IO;
using ChecklistServer.Models;

namespace ChecklistServer
{
    public class Server
    {
        private readonly HttpListener _listener;
        private readonly string _baseUrl;
        public Server(string baseUrl)
        {
            _baseUrl = baseUrl;
            _listener = new HttpListener();
            _listener.Prefixes.Add(baseUrl);
        }

        public void Start()
        {
            _listener.Start();
            Console.WriteLine($"Server started at {_baseUrl}");
            _listener.BeginGetContext(OnContext, null);
        }

        public void Stop()
        {
            if (_listener.IsListening)
            {
                _listener.Stop();
                _listener.Close();
                Console.WriteLine("Server stopped");
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
            if (req.HttpMethod == "POST" && req.Url.AbsolutePath == "/api/select-elements")
            {
                using var reader = new StreamReader(req.InputStream, req.ContentEncoding);
                var body = reader.ReadToEnd();
                var payload = JsonSerializer.Deserialize<SelectElementsRequest>(body);
                var elements = RevitApi.PromptForElementSelection(payload?.Message ?? string.Empty, payload?.Count == "multiple");
                WriteJson(res, new { status = "ok", selectedElementUniqueIds = elements });
                return;
            }
            res.StatusCode = 404;
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
