using System.Net;

const string dbFile = "database.db";
List<long> database = [];

var listener = new HttpListener();
listener.Prefixes.Add("http://localhost:23756/scores/");
listener.Start();
LoadDatabase();
while (true)
    await listener.GetContextAsync().ContinueWith(ProcessRequest);

async Task ProcessRequest(Task<HttpListenerContext> task)
{
    var context = await task;
    Console.WriteLine($"Request: {context.Request.Url?.AbsolutePath} {string.Join(" ", context.Request.QueryString.AllKeys)}");
    switch (context.Request.Url?.AbsolutePath)
    {
        case "/scores/list":
            int count;
            if (!int.TryParse(context.Request.QueryString["count"], out count))
                count = database.Count;
            count = Math.Min(count, database.Count);
            var scores = new EnumerableQuery<long>(database).Order().Take(count);
            var bw = new BinaryWriter(context.Response.OutputStream);
            bw.Write(count);
            foreach (var score in scores) bw.Write(score);
            context.Response.StatusCode = 200;
            context.Response.ContentType = "application/octet-stream";
            break;
        case "/scores/send":
            var br = new BinaryReader(context.Request.InputStream);
            database.Add(br.ReadInt64());
            SaveDatabase();
            context.Response.StatusCode = 200;
            break;
        default:
            context.Response.StatusCode = 404;
            break;
    }
    context.Response.Close();
}

void LoadDatabase()
{
    if (!File.Exists(dbFile)) return;
    var br = new BinaryReader(
        File.Open(dbFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
    );
    database.Clear();
    var count = br.ReadInt32();
    for (var i = 0; i < count; i++)
        database.Add(br.ReadInt64());
}

void SaveDatabase()
{
    var bw = new BinaryWriter(
        File.Open(dbFile, FileMode.Create, FileAccess.Write, FileShare.Read)
    );
    bw.Write(database.Count);
    foreach (var score in database) bw.Write(score);
    bw.Close();
}