using NUnit.Framework;
using System.Net;
using System.Net.Sockets;
using EchoServer;

namespace EchoServerTests;

public class ProgramTests
{
    private UdpClient _testReceiver;
    private int _testPort;
    private UdpTimedSender _sender;
    private EchoServer.EchoServer _server;

    [SetUp]
    public void Setup()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        _testPort = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        _testReceiver = new UdpClient(new IPEndPoint(IPAddress.Loopback, _testPort));
        
        _sender = new UdpTimedSender("127.0.0.1", _testPort);
        _server = new EchoServer.EchoServer(_testPort);
    }

    [TearDown]
    public void TearDown()
    {
        _sender?.Dispose();
        _testReceiver?.Dispose();
    }

    
    
    [Test]
    public void StartSending_CalledTwice_Test()
    {
        // Arrange
        _sender.StartSending(99999);

        // Assert
        var ex = Assert.Throws<InvalidOperationException>(() => 
        {
            // Act
            _sender.StartSending(99999);
        });

        Assert.That(ex.Message, Does.Contain("already running"));
    }

    [Test]
    public void StopSending_StartAfterStop_Test()
    {
        // Arrange
        _sender.StartSending(99999);

        // Act
        _sender.StopSending();

        // Assert
        Assert.DoesNotThrow(() => 
        {
            _sender.StartSending(99999);
        });
    }

    [Test]
    public void StopSending_TwiceStop_Test()
    {
        // Arrange
        _sender.StartSending(99999);

        // Act
        _sender.StopSending();

        // Assert
        Assert.DoesNotThrow(() => 
        {
            _sender.StopSending();
        });
    }

    [Test]
    public void StartSending_AfterDispose_Test()
    {
        // Arrange
        _sender.Dispose();

        // Assert
        var ex = Assert.Throws<ObjectDisposedException>(() => 
        {
            // Act
            _sender.StartSending(1000);
        });
    }

    [Test]
    public void Dispose_MultipleTimes_Test()
    {
        // Act
        _sender.Dispose();

        // Assert
        Assert.DoesNotThrow(() => 
        {
            _sender.Dispose();
            _sender.Dispose();
        });
    }




    [Test]
    public void Stop_NotStartedServer_Test()
    {
        // Assert
        Assert.DoesNotThrow(() => 
        {
            // Act
            _server.Stop();
        });
    }

    [Test]
    public async Task WriteAsync_and_ReadAsync_Test()
    {
        // Arrange
        var serverTask = _server.StartAsync();

        await Task.Delay(100); 

        using var client = new TcpClient();
        await client.ConnectAsync("127.0.0.1", _testPort);
        using var stream = client.GetStream();

        byte[] dataToSend = { 0x11, 0x22, 0x33, 0x44 };
        byte[] receiveBuffer = new byte[dataToSend.Length];

        // Act
        await stream.WriteAsync(dataToSend.AsMemory());
        int bytesRead = await stream.ReadAsync(receiveBuffer.AsMemory());

        // Assert
        Assert.That(bytesRead, Is.EqualTo(dataToSend.Length));
        Assert.That(receiveBuffer, Is.EquivalentTo(dataToSend));

        _server.Stop();
        try
        {
            await serverTask;
        }
        catch (SocketException)
        {
            Assert.Warn("Wrong server stopping logic");
        }
    }

    [Test]
    public async Task StartSending_Test()
    {
        // Arrange
        const byte HeaderByte1 = 0x04;
        const byte HeaderByte2 = 0x84;
        const int PayloadSize = 1024;

        // Act
        _sender.StartSending(intervalMilliseconds: 50);

        // Assert
        var receiveTask = _testReceiver.ReceiveAsync();
        var timeoutTask = Task.Delay(2000);

        var finishedTask = await Task.WhenAny(receiveTask, timeoutTask);

        Assert.That(finishedTask, Is.SameAs(receiveTask));

        var udpResult = await receiveTask;
        byte[] receivedBytes = udpResult.Buffer;

        _sender.StopSending();

        using (Assert.EnterMultipleScope()){
            Assert.That(receivedBytes, Has.Length.EqualTo(2 + sizeof(ushort) + PayloadSize));
            Assert.That(receivedBytes[0], Is.EqualTo(HeaderByte1));
            Assert.That(receivedBytes[1], Is.EqualTo(HeaderByte2));
        }
        

        ushort counter = BitConverter.ToUInt16(receivedBytes, 2);
        Assert.That(counter, Is.EqualTo(1));
    }
}
