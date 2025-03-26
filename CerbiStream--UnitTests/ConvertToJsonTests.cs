using CerberusLogging.Interfaces.Objects;
using CerbiClientLogging.Classes;
using CerbiClientLogging.Classes.ClassTypes;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace CerbiStream.Tests
{
    public class ConvertToJsonTests
    {
        public class FakeLog
        {
            public string Message { get; set; } = "Hello";
            public int Code { get; set; } = 200;
        }
        public class FakeApplicationEntity : IApplicationEntity
        {
            public string AppName { get; set; } = "CerbiApp";
            public string ApplicationMessage { get; set; } = "DefaultMessage";
            public string CurrentMethod { get; set; } = "UnitTestMethod";
            public IEntityBase EntityBase { get; set; } = new CerbiClientLogging.Classes.ClassTypes.EntityBase() as IEntityBase;
        }



        [Fact]
        public void ConvertMessageToJson_ShouldSerializeCorrectly()
        {
            var converter = new ConvertToJson();
            var log = new FakeLog { Message = "Hello", Code = 123 };

            var json = converter.ConvertMessageToJson(log);

            Assert.Contains("\"Message\":\"Hello\"", json);
            Assert.Contains("\"Code\":123", json);
        }

        [Fact]
        public void ConvertApplicationMessageToJson_ShouldSerializeApplicationEntity()
        {
            var converter = new ConvertToJson();
            var appEntity = new FakeApplicationEntity { AppName = "TestApp" };

            var json = converter.ConvertApplicationMessageToJson(appEntity);

            Assert.Contains("\"AppName\":\"TestApp\"", json);
        }
    }
}