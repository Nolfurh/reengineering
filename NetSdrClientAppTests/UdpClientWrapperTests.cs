using NUnit.Framework;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using NetSdrClientApp.Networking;

namespace NetSdrClientAppTests;

public class UdpClientWrapperTests
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
    public void GetHashCode_Test()
    {
        // Arrange
        int port = 443;
        var wrapper = new UdpClientWrapper(port);

        int expectedHashCode = -1688518782; 

        // Act
        int actualHashCode = wrapper.GetHashCode();

        // Assert
        Assert.That(actualHashCode, Is.EqualTo(expectedHashCode));
    }

    [Test]
    public void Equals_Test()
    {
        // Arrange
        var wrapper1 = new UdpClientWrapper(80);
        var wrapper2 = new UdpClientWrapper(80);
        var wrapper3 = new UdpClientWrapper(443);

        // Act
        var sameObjRes = wrapper1.Equals(wrapper2);
        var differentObjRes = wrapper1.Equals(wrapper3);

        // Assert
        Assert.That(sameObjRes, Is.True);
        Assert.That(differentObjRes, Is.False);
    }

    [Test]
    public void Equals_null_Test()
    {
        var wrapper = new UdpClientWrapper(_serverPort);
        Assert.That(wrapper.Equals(null), Is.False);
    }

    [Test]
    public async Task StartListeningAsync_Test()
    {
        // Arrange
        var wrapper = new UdpClientWrapper(_serverPort);
        var messageReceivedTcs = new TaskCompletionSource<byte[]>();
        
        wrapper.MessageReceived += (sender, data) =>
        {
            messageReceivedTcs.TrySetResult(data);
        };

        Task listenTask = wrapper.StartListeningAsync();

        using UdpClient testSender = new UdpClient();
        byte[] dataToSend = { 0xDE, 0xAD, 0xBE, 0xEF };

        // Act
        await testSender.SendAsync(dataToSend, dataToSend.Length, "127.0.0.1", _serverPort);
        
        var timeoutTask = Task.Delay(2000);
        var finishedTask = await Task.WhenAny(messageReceivedTcs.Task, timeoutTask);

        // Assert
        Assert.That(finishedTask, Is.SameAs(messageReceivedTcs.Task));
        Assert.That(messageReceivedTcs.Task.Result, Is.EquivalentTo(dataToSend));


        wrapper.StopListening();
        await listenTask;
    }

    [Test]
    public async Task StopListening_Test()
    {
        // Arrange
        var wrapper = new UdpClientWrapper(_serverPort);
        Task listenTask = wrapper.StartListeningAsync();

        // Act
        wrapper.StopListening();

        var timeoutTask = Task.Delay(2000);
        var finishedTask = await Task.WhenAny(listenTask, timeoutTask);

        // Assert
        Assert.That(finishedTask, Is.SameAs(listenTask));
        Assert.That(listenTask.IsCompletedSuccessfully, Is.True);
    }

    [Test]
    public async Task Exit_Test()
    {
        // Arrange
        var wrapper = new UdpClientWrapper(_serverPort);
        Task listenTask = wrapper.StartListeningAsync();

        // Act
        wrapper.Exit();

        var timeoutTask = Task.Delay(2000);
        var finishedTask = await Task.WhenAny(listenTask, timeoutTask);

        // Assert
        Assert.That(finishedTask, Is.SameAs(listenTask));
        Assert.That(listenTask.IsCompletedSuccessfully, Is.True);
    }
}
