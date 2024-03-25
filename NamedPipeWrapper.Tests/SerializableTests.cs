using log4net.Appender;
using log4net.Config;
using log4net.Layout;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NamedPipeWrapper.Tests;

public class SerializableTests : IDisposable
{
    private static readonly log4net.ILog Logger =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    static SerializableTests()
    {
        var layout = new PatternLayout("%-6timestamp %-5level - %message%newline");
        var appender = new ConsoleAppender { Layout = layout };
        layout.ActivateOptions();
        appender.ActivateOptions();
        BasicConfigurator.Configure(appender);
    }

    private const string PipeName = "data_test_pipe";

    private NamedPipeServer<TestCollection> _server;
    private NamedPipeClient<TestCollection> _client;

    private TestCollection _expectedData;
    private int _expectedHash;
    private TestCollection _actualData;
    private int _actualHash;
    private bool _clientDisconnected;

    private DateTime _startTime;

    private readonly ManualResetEvent _barrier = new ManualResetEvent(false);

    private readonly IList<Exception> _exceptions = new List<Exception>();

    #region Setup and teardown

    public void SetUp()
    {
        Logger.Debug("Setting up test...");

        _barrier.Reset();
        _exceptions.Clear();

        _server = new NamedPipeServer<TestCollection>(PipeName);
        _client = new NamedPipeClient<TestCollection>(PipeName);

        _expectedData = null;
        _expectedHash = 0;
        _actualData = null;
        _actualHash = 0;
        _clientDisconnected = false;

        _server.ClientMessage += ServerOnClientMessage;

        _server.Error += OnError;
        _client.Error += OnError;

        _server.Start();
        _client.Start();

        // Give the client and server a few seconds to connect before sending data
        Thread.Sleep(TimeSpan.FromSeconds(1));

        Logger.Debug("Client and server started");
        Logger.Debug("---");

        _startTime = DateTime.Now;
    }

    private void OnError(Exception exception)
    {
        _exceptions.Add(exception);
        _barrier.Set();
    }

    public void TearDown()
    {
        Logger.Debug("---");
        Logger.Debug("Stopping client and server...");

        _server.ClientMessage -= ServerOnClientMessage;

        _server.Stop();
        _client.Stop();

        Logger.Debug("Client and server stopped");
        Logger.DebugFormat("Test took {0}", (DateTime.Now - _startTime));
        Logger.Debug("~~~~~~~~~~~~~~~~~~~~~~~~~~");
    }

    #endregion

    #region Events

    private void ServerOnClientMessage(NamedPipeConnection<TestCollection, TestCollection> connection, TestCollection message)
    {
        Logger.DebugFormat("Received collection with {0} items from the client", message.Count);
        _actualData = message;
        _actualHash = message.GetHashCode();
        _barrier.Set();
    }

    #endregion

    #region Tests

    [Fact]
    public void TestCircularReferences()
    {
        SetUp();
        _expectedData = new TestCollection();
        for (var i = 0; i < 10; i++)
        {
            var item = new TestItem(i, _expectedData, RandomEnum());
            _expectedData.Add(item);
        }
        _expectedHash = _expectedData.GetHashCode();

        _client.PushMessage(_expectedData);
        _barrier.WaitOne(TimeSpan.FromSeconds(5));

        if (_exceptions.Any())
            throw new AggregateException(_exceptions);

        //The original test tests that _actualHash is NotNull. However, the type of _actualHash is int, so I made it non-zero.
        Assert.True(_actualHash != 0, string.Format("Server should have received client's {0} item message", _expectedData.Count));
        Assert.True(_expectedHash == _actualHash, string.Format("Hash codes for {0} item message should match", _expectedData.Count));
        Assert.True(_expectedData.Count == _actualData.Count, string.Format("Collection lengths should be equal"));

        for (var i = 0; i < _actualData.Count; i++)
        {
            var expectedItem = _expectedData[i];
            var actualItem = _actualData[i];
            //"expectedItem == actualItem" will be false.
            Assert.True(expectedItem.Equals(actualItem), string.Format("Items at index {0} should be equal", i));
            Assert.True(actualItem.Parent == _actualData, string.Format("Item at index {0}'s Parent property should reference the item's parent collection", i));
        }
    }

    private TestEnum RandomEnum()
    {
        var rand = new Random().NextDouble();
        if (rand < 0.33)
            return TestEnum.A;
        if (rand < 0.66)
            return TestEnum.B;
        return TestEnum.C;
    }

    #endregion
    private bool disposedValue;
    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                TearDown();
            }
            disposedValue = true;
        }
    }
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}

class TestCollection : List<TestItem>
{
    public override int GetHashCode()
    {
        var strs = new List<string>(Count);
        foreach (var item in ToArray())
        {
            strs.Add(item.GetHashCode().ToString());
        }
        var str = string.Join(",", strs);
        return Hash(Encoding.UTF8.GetBytes(str)).GetHashCode();
    }

    /// <summary>
    /// Computes the SHA-1 hash (lowercase) of the specified byte array.
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns></returns>
    private static string Hash(byte[] bytes)
    {
        using (var sha1 = System.Security.Cryptography.SHA1.Create())
        {
            var hash = sha1.ComputeHash(bytes);
            var sb = new StringBuilder();
            for (var i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("x2"));
            }
            return sb.ToString();
        }
    }
}

class TestItem
{
    public int Id { get; set; }
    public TestCollection Parent { get; set; }
    public TestEnum Enum { get; set; }

    /// <summary>
    /// Default constructor required when deserializing Json
    /// </summary>
    public TestItem()
    {
    }
    public TestItem(int id, TestCollection parent, TestEnum @enum)
    {
        Id = id;
        Parent = parent;
        Enum = @enum;
    }

    protected bool Equals(TestItem other)
    {
        return Id == other.Id && Enum == other.Enum;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((TestItem)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (Id * 397) ^ (int)Enum;
        }
    }
}

enum TestEnum
{
    A = 1,
    B = 2,
    C = 3,
}
