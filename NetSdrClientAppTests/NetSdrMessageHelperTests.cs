using NetSdrClientApp.Messages;

namespace NetSdrClientAppTests
{
    public class NetSdrMessageHelperTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void GetControlItemMessageTest()
        {
            //Arrange
            var type = NetSdrMessageHelper.MsgTypes.Ack;
            var code = NetSdrMessageHelper.ControlItemCodes.ReceiverState;
            int parametersLength = 7500;

            //Act
            byte[] msg = NetSdrMessageHelper.GetControlItemMessage(type, code, new byte[parametersLength]);

            var headerBytes = msg.Take(2);
            var codeBytes = msg.Skip(2).Take(2);
            var parametersBytes = msg.Skip(4);

            var num = BitConverter.ToUInt16(headerBytes.ToArray());
            var actualType = (NetSdrMessageHelper.MsgTypes)(num >> 13);
            var actualLength = num - ((int)actualType << 13);
            var actualCode = BitConverter.ToInt16(codeBytes.ToArray());

            //Assert
            Assert.That(headerBytes.Count(), Is.EqualTo(2));
            Assert.That(msg.Length, Is.EqualTo(actualLength));
            Assert.That(type, Is.EqualTo(actualType));

            Assert.That(actualCode, Is.EqualTo((short)code));

            Assert.That(parametersBytes.Count(), Is.EqualTo(parametersLength));
        }

        [Test]
        public void GetDataItemMessageTest()
        {
            //Arrange
            var type = NetSdrMessageHelper.MsgTypes.DataItem2;
            int parametersLength = 7500;

            //Act
            byte[] msg = NetSdrMessageHelper.GetDataItemMessage(type, new byte[parametersLength]);

            var headerBytes = msg.Take(2);
            var parametersBytes = msg.Skip(2);

            var num = BitConverter.ToUInt16(headerBytes.ToArray());
            var actualType = (NetSdrMessageHelper.MsgTypes)(num >> 13);
            var actualLength = num - ((int)actualType << 13);

            //Assert
            Assert.That(headerBytes.Count(), Is.EqualTo(2));
            Assert.That(msg.Length, Is.EqualTo(actualLength));
            Assert.That(type, Is.EqualTo(actualType));

            Assert.That(parametersBytes.Count(), Is.EqualTo(parametersLength));
        }

        [Test]
        public void TranslateMessage_UnknownControlCode_Test()
        {
            // Arrange
            byte[] validMessage = NetSdrMessageHelper.GetControlItemMessage(
                NetSdrMessageHelper.MsgTypes.SetControlItem, 
                NetSdrMessageHelper.ControlItemCodes.ReceiverState, 
                new byte[] { 0x01 });

            validMessage[2] = 0xFF;
            validMessage[3] = 0xFF;

            // Act
            bool success = NetSdrMessageHelper.TranslateMessage(validMessage, out var type, out var code, out _, out _);

            // Assert
            Assert.That(success, Is.False);
            Assert.That(code, Is.EqualTo(NetSdrMessageHelper.ControlItemCodes.None));
        }

        [Test]
        public void GetSamples_Test()
        {
            // Arrange
            ushort sampleSizeBits = 16;
            ushort bytesPerSample = (ushort)(sampleSizeBits / 8);

            int[] expectedSamples = { 10, 255, 1024 };
            byte[] body = expectedSamples.SelectMany(i => BitConverter.GetBytes(i).Take(bytesPerSample)).ToArray();

            // Act
            var samples = NetSdrMessageHelper.GetSamples(sampleSizeBits, body).ToList();

            // Assert
            Assert.That(samples, Is.EquivalentTo(expectedSamples));
        }

        [Test]
        public void GetSamples_ArgumentOutOfRangeException_Test()
        {
            // Arrange
            const ushort invalidSampleSizeInBits = 40; 

            byte[] dummyBody = Array.Empty<byte>();

            // Act
            void act() => NetSdrMessageHelper.GetSamples(invalidSampleSizeInBits, dummyBody);

            // Assert
            var exception = Assert.Throws<ArgumentOutOfRangeException>((Action)act);
            Assert.That(exception.ParamName, Is.EqualTo("sampleSize"));
        }

        [Test]
        public void GetDataItemMessage_GetHeaderEdgeCase_Test()
        {
            // Arrange
            var type = NetSdrMessageHelper.MsgTypes.DataItem0;
            
            byte[] parameters = new byte[8192]; 
    
            // Act
            byte[] message = NetSdrMessageHelper.GetDataItemMessage(type, parameters);
    
            // Assert
            Assert.That(message[0], Is.EqualTo(0x00));
            Assert.That(message[1], Is.EqualTo(0x80));
        }
    
        [Test]
        public void GetMessage_ExceedsMaxMessageLength_Test()
        {
            // Arrange
            var type = NetSdrMessageHelper.MsgTypes.SetControlItem;
            var code = NetSdrMessageHelper.ControlItemCodes.ReceiverFrequency;
            
            byte[] parameters = new byte[8188];

            // Act
            void act() => NetSdrMessageHelper.GetControlItemMessage(type, code, parameters);

            // Assert
            Assert.That((Action)act, Throws.ArgumentException.With.Message.Contains("Message length exceeds allowed value"));
        }
    }
}