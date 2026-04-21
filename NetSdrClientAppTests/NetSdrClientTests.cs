using Moq;
using NetSdrClientApp;
using NetSdrClientApp.Messages;
using NetSdrClientApp.Networking;
using static NetSdrClientApp.Messages.NetSdrMessageHelper;

namespace NetSdrClientAppTests;

public class NetSdrClientTests
{
    NetSdrClient _client;
    Mock<ITcpClient> _tcpMock;
    Mock<IUdpClient> _updMock;

    public NetSdrClientTests() { }

    [SetUp]
    public void Setup()
    {
        _tcpMock = new Mock<ITcpClient>();
        _tcpMock.Setup(tcp => tcp.Connect()).Callback(() =>
        {
            _tcpMock.Setup(tcp => tcp.Connected).Returns(true);
        });

        _tcpMock.Setup(tcp => tcp.Disconnect()).Callback(() =>
        {
            _tcpMock.Setup(tcp => tcp.Connected).Returns(false);
        });

        _tcpMock.Setup(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>())).Callback<byte[]>((bytes) =>
        {
            _tcpMock.Raise(tcp => tcp.MessageReceived += null, _tcpMock.Object, bytes);
        });

        _updMock = new Mock<IUdpClient>();

        _client = new NetSdrClient(_tcpMock.Object, _updMock.Object);
    }

    [Test]
    public async Task ConnectAsyncTest()
    {
        //act
        await _client.ConnectAsync();

        //assert
        _tcpMock.Verify(tcp => tcp.Connect(), Times.Once);
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Exactly(3));
    }

    [Test]
    public async Task DisconnectWithNoConnectionTest()
    {
        //act
        _client.Disconect();

        //assert
        //No exception thrown
        _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Once);
    }

    [Test]
    public async Task DisconnectTest()
    {
        //Arrange 
        await ConnectAsyncTest();

        //act
        _client.Disconect();

        //assert
        //No exception thrown
        _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Once);
    }

    [Test]
    public async Task StartIQNoConnectionTest()
    {

        //act
        await _client.StartIQAsync();

        //assert
        //No exception thrown
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
        _tcpMock.VerifyGet(tcp => tcp.Connected, Times.AtLeastOnce);
    }

    [Test]
    public async Task StartIQTest()
    {
        //Arrange 
        await ConnectAsyncTest();

        //act
        await _client.StartIQAsync();

        //assert
        //No exception thrown
        _updMock.Verify(udp => udp.StartListeningAsync(), Times.Once);
        Assert.That(_client.IQStarted, Is.True);
    }

    [Test]
    public async Task StopIQTest()
    {
        //Arrange 
        await ConnectAsyncTest();

        //act
        await _client.StopIQAsync();

        //assert
        //No exception thrown
        _updMock.Verify(tcp => tcp.StopListening(), Times.Once);
        Assert.That(_client.IQStarted, Is.False);
    }

    //TODO: cover the rest of the NetSdrClient code here

    [Test]
    public async Task ChangeFrequencyNoConnectionTest()
    {

        // Act
        await _client.ChangeFrequencyAsync(14000000, 1);

        // Assert
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
    }

    [Test]
    public async Task ChangeFrequencyTest()
    {
        // Arrange 
        await _client.ConnectAsync(); 
        _tcpMock.Invocations.Clear(); 
        long hz = 14000000;
        int channel = 1;
        var expectedChannelArg = (byte)channel;
        var expectedFrequencyArgs = BitConverter.GetBytes(hz).Take(5).ToArray();
        var expectedArgs = new byte[] { expectedChannelArg }.Concat(expectedFrequencyArgs).ToArray();
        var expectedMessage = NetSdrMessageHelper.GetControlItemMessage(
            MsgTypes.SetControlItem, 
            ControlItemCodes.ReceiverFrequency, 
            expectedArgs);

        // Act
        await _client.ChangeFrequencyAsync(hz, channel);

        // Assert
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.Is<byte[]>(
            actualMessage => actualMessage.SequenceEqual(expectedMessage)
        )), Times.Once);
    }
    
    [Test]
    public void UdpClient_MessageReceived_Test()
    {
        // Arrange
        var testFile = "samples.bin";
        if (File.Exists(testFile))
        {
            File.Delete(testFile);
        }
        short[] expectedSamples = { 1024, -512, 0, 32767, -32768, 0 };
        ushort expectedSequenceNumber = 1;

        List<byte> payloadBuilder = new();
        payloadBuilder.AddRange(BitConverter.GetBytes(expectedSequenceNumber));
        foreach (var sample in expectedSamples)
        {
            payloadBuilder.AddRange(BitConverter.GetBytes(sample));
        }

        byte[] payloadData = payloadBuilder.ToArray();
        byte[] validUdpMessage = NetSdrMessageHelper.GetDataItemMessage(NetSdrMessageHelper.MsgTypes.DataItem0, payloadData);

        try
        {
            // Act
            _updMock.Raise(udp => udp.MessageReceived += null, _updMock.Object, validUdpMessage);

            // Assert
            Assert.That(File.Exists(testFile), Is.True);

            using (FileStream fs = new FileStream(testFile, FileMode.Open, FileAccess.Read))
            using (BinaryReader br = new BinaryReader(fs))
            {
                Assert.That(fs.Length, Is.EqualTo(expectedSamples.Length * 2));
    
                for (int i = 0; i < expectedSamples.Length; i++)
                {
                    short actualSample = br.ReadInt16();
                    Assert.That(actualSample, Is.EqualTo(expectedSamples[i]), $"Incorrect sample at index {i}");
                }
            }
        }
        finally
        {
            if (File.Exists(testFile))
            {
                File.Delete(testFile);
            }
        }
    }
}
