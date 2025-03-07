//using CerbiClientLogging.Classes;
//using CerbiClientLogging.Classes.ClassTypes;
//using Moq;
//using Newtonsoft.Json;
//using NUnit.Framework;
//using RabbitMQ.Client;

//namespace UnitTests
//{
//    [TestFixture]
//    public class ConvertToJsonTests
//    {
//        private ConvertToJson _convertToJson;
//        private Mock<IConnection> _mockConnection;
//        private Mock<IModel> _mockChannel;

//        [SetUp]
//        public void Setup()
//        {
//            _convertToJson = new ConvertToJson();

//            _mockConnection = new Mock<IConnection>();
//            _mockChannel = new Mock<IModel>();
//        }

//        [Test]
//        public void ConvertMessageToJson_ValidEntity_ReturnsJsonString()
//        {
//            // Arrange
//            var log = new EntityBase(); // Replace with your concrete implementation of IEntityBase

//            // Act
//            var result = _convertToJson.ConvertMessageToJson(log);

//            // Assert
//            Assert.That(!string.IsNullOrEmpty(result), Is.True);
//            Assert.That(() => JsonConvert.DeserializeObject<EntityBase>(result), Throws.Nothing);
//        }

//        [Test]
//        public void ConvertApplicationMessageToJson_ValidApplicationEntity_ReturnsJsonString()
//        {
//            // Arrange
//            var log = new ApplicationEntity(); // Replace with your concrete implementation of IApplicationEntity

//            // Act
//            var result = _convertToJson.ConvertApplicationMessageToJson(log);

//            // Assert
//            Assert.That(!string.IsNullOrEmpty(result), Is.True);
//            Assert.That(!string.IsNullOrEmpty(result), Is.True);
//            Assert.That(() => JsonConvert.DeserializeObject<ApplicationEntity>(result), Throws.Nothing);
//        }
//    }
//}