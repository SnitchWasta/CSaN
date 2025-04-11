using System.Net;
using System.Net.Sockets;
using System.Text;

class ProxyServer
{
    private const int Port = 8888;
    private const int BufferSize = 8192;
    private static List<string> Blacklist;

    static void Main()
    {
        LoadBlacklist();

        TcpListener listener = new TcpListener(IPAddress.Any, Port);
        listener.Start();

        Console.WriteLine("[INFO] Proxy server starting...");
        Console.WriteLine($"[INFO] Listening on port {Port}...");

        while (true)
        {
            TcpClient client = listener.AcceptTcpClient();
            _ = Task.Run(() => HandleClient(client));
        }
    }

    static void LoadBlacklist()
    {
        Blacklist = new List<string>();
        if (File.Exists("D:\\учёба 2 курс\\КСиС\\lab4\\proxy_server\\proxy_server\\blacklist.txt"))
        {
            foreach (var line in File.ReadAllLines("D:\\учёба 2 курс\\КСиС\\lab4\\proxy_server\\proxy_server\\blacklist.txt"))
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    var domain = line.Trim();
                    Blacklist.Add(domain);
                    Console.WriteLine($"[FROM BLACKLIST] Loaded: {domain}");
                }
            }
        }
    }

    static bool IsBlocked(string url)
    {
        Uri uri;
        try { uri = new Uri(url); }
        catch { return false; }

        foreach (var blocked in Blacklist)
        {
            if (uri.Host.Contains(blocked, StringComparison.OrdinalIgnoreCase) ||
                url.Contains(blocked, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    static async Task HandleClient(TcpClient client)
    {
        using NetworkStream clientStream = client.GetStream();
        byte[] buffer = new byte[BufferSize];

        try
        {
            int bytesRead = await clientStream.ReadAsync(buffer, 0, buffer.Length);
            string requestHeader = Encoding.ASCII.GetString(buffer, 0, bytesRead);

            string[] requestLines = requestHeader.Split(new[] { "\r\n" }, StringSplitOptions.None);
            string requestLine = requestLines[0];
            string[] parts = requestLine.Split(' ');

            if (parts.Length < 3)
            {
                client.Close();
                return;
            }

            string method = parts[0];
            string url = parts[1];

            if (IsBlocked(url))
            {
                Console.WriteLine($"[BLOCKED] {url}");
                string response = $"""
                    HTTP/1.1 403 Forbidden\r
                    Content-Type: text/html\r
                    \r
                    <html><body>
                    <h1>403 Forbidden</h1>
                    <p>Access to {url} is blocked by proxy.</p>
                    </body></html>
                    """;

                byte[] responseBytes = Encoding.ASCII.GetBytes(response);
                await clientStream.WriteAsync(responseBytes, 0, responseBytes.Length);
                client.Close();
                return;
            }

            Uri uri = new Uri(url);
            string host = uri.Host;
            int port = uri.Port != -1 ? uri.Port : 80;
            string path = uri.PathAndQuery;

            requestLines[0] = $"{method} {path} {parts[2]}";

            for (int i = 0; i < requestLines.Length; i++)
            {
                if (requestLines[i].StartsWith("Host:", StringComparison.OrdinalIgnoreCase))
                {
                    requestLines[i] = $"Host: {host}";
                    break;
                }
            }

            string newRequest = string.Join("\r\n", requestLines) + "\r\n\r\n";
            byte[] newRequestBytes = Encoding.ASCII.GetBytes(newRequest);

            using TcpClient serverClient = new TcpClient();
            await serverClient.ConnectAsync(host, port);

            using NetworkStream serverStream = serverClient.GetStream();
            await serverStream.WriteAsync(newRequestBytes, 0, newRequestBytes.Length);

            byte[] responseBuffer = new byte[BufferSize];
            int totalRead = 0;
            bool statusLogged = false;
            while ((bytesRead = await serverStream.ReadAsync(responseBuffer, 0, responseBuffer.Length)) > 0)
            {
                if (!statusLogged)
                {
                    string statusLine = Encoding.ASCII.GetString(responseBuffer, 0, Math.Min(bytesRead, 256));
                    string[] statusParts = statusLine.Split(new[] { "\r\n" }, StringSplitOptions.None);
                    if (statusParts.Length > 0)
                    {
                        string[] statusLineParts = statusParts[0].Split(' ');
                        if (statusLineParts.Length >= 2)
                        {
                            Console.WriteLine($"[OK] {url} -> {statusLineParts[1]}");
                            statusLogged = true;
                        }
                    }
                }

                await clientStream.WriteAsync(responseBuffer, 0, bytesRead);
                totalRead += bytesRead;
            }
        }
        catch (Exception ex)
        {
            
        }
        finally
        {
            client.Close();
        }
    }
}
