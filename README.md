# Named Pipe Wrapper for .NET 8

A simple, easy to use, strongly-typed wrapper around .NET named pipes.

# Features

*  Create named pipe servers that can handle multiple client connections simultaneously.
*  Send strongly-typed messages between clients and servers: any serializable .NET object can be sent over a pipe and will be automatically serialized/deserialized, including cyclical references and complex object graphs.
*  Messages are sent and received asynchronously on a separate background thread and marshalled back to the calling thread (typically the UI).
*  Supports large messages - up to 300 MiB.

# Requirements

Requires .NET 8

# Usage

Message:

_(Class must be possible to serialize with Json. [How to write .NET objects as JSON (serialize)](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/how-to))_

```csharp
class SomeClass
{
    public int Id { get; set; }
    public string Text { get; set; }
}
```

Server:

```csharp
var server = new NamedPipeServer<SomeClass>("MyServerPipe");

server.ClientConnected += delegate(NamedPipeConnection<SomeClass> conn)
    {
        Console.WriteLine("Client {0} is now connected!", conn.Id);
        conn.PushMessage(new SomeClass { Text: "Welcome!" });
    };

server.ClientMessage += delegate(NamedPipeConnection<SomeClass> conn, SomeClass message)
    {
        Console.WriteLine("Client {0} says: {1}", conn.Id, message.Text);
    };

// Start up the server asynchronously and begin listening for connections.
// This method will return immediately while the server runs in a separate background thread.
server.Start();

// ...
```

Client:

```csharp
var client = new NamedPipeClient<SomeClass>("MyServerPipe");

client.ServerMessage += delegate(NamedPipeConnection<SomeClass> conn, SomeClass message)
    {
        Console.WriteLine("Server says: {0}", message.Text);
    };

// Start up the client asynchronously and connect to the specified server pipe.
// This method will return immediately while the client runs in a separate background thread.
client.Start();

// ...
```

# Tests
- If you run all tests, some tests may fail. If you do them one by one, there is no problem.

# History
- 2024-03-25 Ver 2.0

### Breaking Changes
- Changed .NET Framework 4.0 to .NET 8.
- Deprecated BinaryFormatter. `Serializable` attribute is not required.
- Changed BinaryFormatter to JsonSerializer.
- Changed WinForm to WPF on ExampleGUI.
- Changed nUnit to xUnit Test.

# Original Project
This is a fork of [Named Pipe Wrapper](https://github.com/acdvorak/named-pipe-wrapper).

# MIT License
Copyright (c) 2024 Taisidô Mikihiro ([Ganges](https://ganges.pro/))

Also See [Credits](CREDITS.md)