using NUnit.Framework;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using NetSdrClientApp.Networking;

namespace NetSdrClientAppTests;

public class TcpClientWrapperTests
{
    private TcpListener _testServer;
    private int _serverPort;

    [SetUp]
    public void Setup()
    {
        _testServer = new TcpListener(IPAddress.Loopback, 0);

        _testServer.Start();

        _serverPort = ((IPEndPoint)_testServer.LocalEndpoint).Port;
    }
    
    [TearDown]
    public void TearDown()
    {
        _testServer?.Stop();
        _testServer?.Dispose();
    }
    
    [Test]
    public void Connect_Test()
    {
        // Arrange
        var client = new TcpClientWrapper("127.0.0.1", _serverPort);

        // Act
        client.Connect();

        // Assert
        Assert.That(client.Connected, Is.True);
        
        client.Disconnect();
    }

    [Test]
    public void Disconnect_Test()
    {
        // Arrange
        var client = new TcpClientWrapper("127.0.0.1", _serverPort);
        client.Connect();

        // Act
        client.Disconnect();

        // Assert
        Assert.That(client.Connected, Is.False);
    }

    [Test]
    public async Task Connect_WhenAlreadyEstablishedConnection_Test()
    {
        // Arrange
        var client = new TcpClientWrapper("127.0.0.1", _serverPort);
        client.Connect();
        using TcpClient firstServerSideClient = await _testServer.AcceptTcpClientAsync();

        // Act
        client.Connect();
        var acceptNextConnectionTask = _testServer.AcceptTcpClientAsync();
        var timeoutTask = Task.Delay(500);
        var completedTask = await Task.WhenAny(acceptNextConnectionTask, timeoutTask);

        // Assert
        Assert.That(completedTask, Is.SameAs(timeoutTask));
        
        client.Disconnect();
    }

    [Test]
    public async Task SendMessageAsync_Test()
    {
        // Arrange
        var client = new TcpClientWrapper("127.0.0.1", _serverPort);
        client.Connect();

        using TcpClient serverSideClient = await _testServer.AcceptTcpClientAsync();
        using NetworkStream serverStream = serverSideClient.GetStream();

        byte[] dataToSend = { 0xDE, 0xAD, 0xBE, 0xEF };
        byte[] receiveBuffer = new byte[dataToSend.Length];
        
        // Act
        await client.SendMessageAsync(dataToSend);
        
        // Assert
        int bytesRead = await serverStream.ReadAsync(receiveBuffer, 0, receiveBuffer.Length);

        Assert.That(bytesRead, Is.EqualTo(dataToSend.Length));
        Assert.That(receiveBuffer, Is.EquivalentTo(dataToSend));
        
        client.Disconnect();
    }

    [Test]
    public void SendMessageAsync_WhenDisconnected_Test()
    {
        // Arrange
        var client = new TcpClientWrapper("127.0.0.1", _serverPort);

        // Act
        var ex = Assert.ThrowsAsync<InvalidOperationException>((AsyncTestDelegate) (async () => 
        {
            await client.SendMessageAsync(new byte[] { 0x01 });
        }));

        //Assert
        Assert.That(ex.Message, Does.Contain("Not connected"));
    }
    
    [Test]
    public async Task SendMessageAsync_StringParameterOverload_Test()
    {
        // Arrange
        var client = new TcpClientWrapper("127.0.0.1", _serverPort);
        client.Connect();

        using TcpClient serverSideClient = await _testServer.AcceptTcpClientAsync();
        using NetworkStream serverStream = serverSideClient.GetStream();

        string dataToSend = "Hello world";
        var dataByteCount = Encoding.UTF8.GetByteCount(dataToSend);
        byte[] receiveBuffer = new byte[dataByteCount];
        
        // Act
        await client.SendMessageAsync(dataToSend);
        
        // Assert
        int bytesRead = await serverStream.ReadAsync(receiveBuffer, 0, receiveBuffer.Length);

        Assert.That(bytesRead, Is.EqualTo(dataByteCount));
        Assert.That(receiveBuffer, Is.EquivalentTo(Encoding.UTF8.GetBytes(dataToSend)));
        
        client.Disconnect();
    }

    [Test]
    public async Task StartListeningAsync_Test()
    {
        // Arrange
        var client = new TcpClientWrapper("127.0.0.1", _serverPort);
        var eventFiredTcs = new TaskCompletionSource<byte[]>();
        client.MessageReceived += (sender, data) =>
        {
            eventFiredTcs.TrySetResult(data);
        };
        client.Connect();

        using TcpClient serverSideClient = await _testServer.AcceptTcpClientAsync();
        using NetworkStream serverStream = serverSideClient.GetStream();
        byte[] dataFromServer = { 0x01, 0x02, 0x03 };
        
        // Act
        await serverStream.WriteAsync(dataFromServer, 0, dataFromServer.Length);
        var timeoutTask = Task.Delay(2000);
        var finishedTask = await Task.WhenAny(eventFiredTcs.Task, timeoutTask);
        
        // Assert
        Assert.That(finishedTask, Is.SameAs(eventFiredTcs.Task));
        Assert.That(eventFiredTcs.Task.Result, Is.EquivalentTo(dataFromServer));
        client.Disconnect();
    }
}
