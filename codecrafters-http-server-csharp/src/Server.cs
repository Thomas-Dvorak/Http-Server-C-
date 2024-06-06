using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;

/*
    TODO:
    - Make all prepared reponses byte[] and send them using client.Client.Send(response)
*/

TcpListener server = new(IPAddress.Any, 4221);

Console.WriteLine("Connection to server accepted.");
Console.WriteLine("Starting server...");
server.Start();
Console.WriteLine("--------------------------------------------------------------------");
Console.WriteLine("  ________ __  _________ ______  ____  _  __ __   __  ____  _  __");
Console.WriteLine(" |__   __||__||        |/  ___/ /  _ \\| ' __|  | |  |/  _ \\| ' __|");
Console.WriteLine("   |  |   |  ||  |  |  |\\ __  \\|   __/|  |  \\  \\/  /|   __/|  |  ");
Console.WriteLine("   |__|   |__||__|__|__|/_____/\\_____||__|   \\___/  \\_____||__|   ");
Console.WriteLine("                                                                  ");
Console.WriteLine("--------------------------------------------------------------------");
Console.WriteLine("v1.0");
Console.WriteLine("Server started.");
Console.WriteLine("Waiting for a connection...");
Console.WriteLine("Use the curl command to send requests to the server. For more info, visit: https://curl.se/docs/tutorial.html");

// support multiple sockets
while (true) {
    TcpClient client = server.AcceptTcpClient();
    Console.WriteLine("Connection accepted. New socket created.");
    _ = Task.Run(async () => await HandleClient(client));
    // run sockets together
}

Task HandleClient(TcpClient client) {
    // handle each client connected to server
    Console.WriteLine("Client handling started...");
    byte[] response = Encoding.UTF8.GetBytes(GetResponseFromMessage(client));
    Console.WriteLine("Response processed. Sending to output...");
    client.GetStream().Write(response, 0, response.Length);
    Console.WriteLine("Response sent. Closing connection...");
    client.Close();
    return Task.CompletedTask;
}
// end of multiple sockets

string GetResponseFromMessage(TcpClient client) {
    // reads the lines
    // gets path and version
    // prepares response based on path and header info
    var lines = RecieveMessageFromClient(client).Split("\r\n");
    var headerParts = lines[0].Split(" ");
    var requestType = headerParts[0];
    var path = headerParts[1];
    var httpVersion = headerParts[2];
    string[] args = Environment.GetCommandLineArgs();
    // get command line arguments (i.e. --directory <directory>)
    // args = ["./your_server.sh", "--directory", "<directory>"]
    if (requestType.Equals("GET")) {
        // handle GET requests
        if (args.Length > 1 && args[1].Equals("--directory")) {
            if (path.StartsWith("/files/")) {
                // read the file if it exists and we're in the /file/ directory
                // error handling for nonexistent files or directories
                try {
                    string fileName = path[7..];
                    StreamReader reader = new(Path.Combine(args[2], fileName));
                    string data = reader.ReadToEnd();
                    return PrepareFileReadResponse(httpVersion, data);
                } catch (Exception e) {
                    Console.WriteLine("Error in reading file. File or directory does not exist.");
                    return PrepareNotFoundResponse(httpVersion);
                }
            } else {
                // you need to be in /files/
                return PrepareNotFoundResponse(httpVersion);
            }
        } else if (path.StartsWith("/echo/")) {
            // print {text} in from ~/echo/{text}
            bool isEncoding = false;
            string encoding = "";
            for (int i = 0; i < lines.Length; i++) {
                if (lines[i].ToLower().StartsWith("accept-encoding: ")) {
                    isEncoding = true;
                    encoding = lines[i].Substring(17);
                }
            }
            string[] allEncodings = encoding.Split(", ");
            encoding = FilterValidEncodings(allEncodings);
            if (isEncoding && ValidateEncoding(encoding)) {
                string text = path[6..];
                SendCompressedTextResponse(httpVersion, client, text, encoding);
                client.Close();
            } else {
                string text = path[6..];
                return PrepareTextResponse(httpVersion, text);
            }
        } else if (path.StartsWith("/user-agent")) {
            foreach (var line in lines) {
                if (line.StartsWith("User-Agent:")) {
                    string text = line[12..];
                    return PrepareTextResponse(httpVersion, text);
                }
            }
        } else if (path.Equals("/")) {
            // we could be in the root directory
            return PrepareOKConnectionResponse(httpVersion, false);
        } else if (path.Equals("/quit")) {
            // we stop running this file
            Console.WriteLine("Stopping server..");
            SendCloseMessage(httpVersion, client);
            client.Close();
            Environment.Exit(0);
        }
    } else if (requestType.Equals("POST")) {    
        // handle POST requests
        if (args.Length > 2 && args[1].Equals("--directory")) {
            // NOTE: The data to save will always be the las item in the 'lines' array.
            try {
                string fileName = path[7..];
                string fullPath = Path.Combine(args[2], fileName);
                string data = lines[lines.Length - 1];
                try {
                    FileStream file = File.Create(fullPath);
                    byte[] buffer = Encoding.UTF8.GetBytes(data);
                    file.Write(buffer, 0, buffer.Length);
                    file.Close();
                    return PrepareFileSavedResponse(httpVersion);
                } catch (Exception x) {
                    Console.WriteLine("Error in creating or saving file. File or directory may not exist.");
                    return PrepareNotFoundResponse(httpVersion);
                }
            } catch (Exception e) {
                Console.WriteLine("Error in reading file. File or directory does not exist.");
                return PrepareNotFoundResponse(httpVersion);
            }
        }
    } 
    return PrepareNotFoundResponse(httpVersion);
}

string FilterValidEncodings(string[] encodings) {
    foreach (var encoding in encodings) {
        if (ValidateEncoding(encoding)) {
            return encoding;
        }
    }
    return "";
}

bool ValidateEncoding(string type) {
    // more to be added later on
    switch (type) {
        case "gzip":
            return true;
        default:
            Console.WriteLine("Invalid compression scheme provided. The text will not be encoded. Use: gzip.");
            return false;
    }
}

byte[] CompressText(string text) {
    Console.WriteLine("Compressing text...");
    // transform the text to bytes
    byte[] textAsBytes = Encoding.UTF8.GetBytes(text);
   // MemoryStream msIn = new(textAsBytes);
    MemoryStream msOut = new();
    GZipStream gzip = new(msOut, CompressionMode.Compress, true);
   // msIn.CopyTo(gzip);
    // start a new compresser with ms as the stream to read from
    // Compress
    gzip.Write(textAsBytes, 0, textAsBytes.Length);
    gzip.Flush();
    // get the compressed text as bytes
    gzip.Close();
    Console.WriteLine("Text compression completed.");
    return msOut.ToArray();
}

void SendCompressedTextResponse(string httpVersion, TcpClient client, string text, string encodeType) {
    byte[] compressedText = CompressText(text);
    byte[] response = Encoding.UTF8.GetBytes($"{PrepareOKConnectionResponse(httpVersion, true)}Content-Encoding: {encodeType}\r\nContent-Type: text/plain\r\nContent-Length: {compressedText.Length}\r\n\r\n").Concat(compressedText).ToArray();
    // it's sending double (e.x. strawberrystrawberry)
    client.Client.Send(response);
}

void SendCloseMessage(string httpVersion, TcpClient client) {
    byte[] response = Encoding.UTF8.GetBytes($"{httpVersion} 990 Server Stopped\r\nMethod: quit\r\n");
    client.Client.Send(response);
}

// next 5 functions prepare responses to be sent back to the user

string PrepareFileSavedResponse(string httpVersion) {
    return $"{httpVersion} 201 Created\r\n\r\n";
}


string PrepareTextResponse(string httpVersion, string text) {
    return $"{PrepareOKConnectionResponse(httpVersion, true)}Content-Type: text/plain\r\nContent-Length: {text.Length}\r\n\r\n{text}";
}

string PrepareNotFoundResponse(string httpVersion) {
    return $"{httpVersion} 404 Not Found\r\n\r\n";
}

string PrepareOKConnectionResponse(string httpVersion, bool includeHeaders) {
    return $"{httpVersion} 200 OK\r\n" + (includeHeaders ? "" : "\r\n");
}

string PrepareFileReadResponse(string httpVersion, string fileContents) {
    return $"{PrepareOKConnectionResponse(httpVersion, true)}Content-Type: application/octet-stream\r\nContent-Length: {fileContents.Length}\r\n\r\n{fileContents}";
}

string RecieveMessageFromClient(TcpClient client) {
    // recieve command/message from client
    // make a buffer for recieving the data
    var buffer = new byte[1024];
    int bytesRead = client.GetStream().Read(buffer, 0, buffer.Length);
    // turn it into a string
    return Encoding.UTF8.GetString(buffer, 0, bytesRead);
}