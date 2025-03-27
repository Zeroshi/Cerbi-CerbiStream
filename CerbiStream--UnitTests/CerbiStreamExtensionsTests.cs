using CerbiStream.Logging.Configuration;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using CerbiStream.Extensions;


namespace UnitTests
{
    public class CerbiStreamExtensionsTests
    {
        [Fact]
        public void WithEncryptionFromConfiguration_ShouldApplyKeyAndIV()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Cerbi:Encryption:Key"] = Convert.ToBase64String(new byte[16]),
                    ["Cerbi:Encryption:IV"] = Convert.ToBase64String(new byte[16])
                }).Build();

            var options = new CerbiStreamOptions().WithEncryptionFromConfiguration(config);

            Assert.NotNull(options.EncryptionKey);
            Assert.NotNull(options.EncryptionIV);
        }

    }
}
