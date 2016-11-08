using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Chocolate.AspNetCore.Configuration.Consul.Parsers;
using Moq;
using NUnit.Framework;

namespace Chocolate.AspNetCore.Configuration.Consul
{
    [TestFixture]
    [TestOf(nameof(ConsulConfigurationProvider))]
    internal sealed class ConsulConfigurationProviderTests
    {
        const string _Key = "Test/Development";

        private ConsulConfigurationProvider _consulConfigProvider;
        private Mock<IConsulConfigurationSource> _consulConfigSourceMock;
        private Mock<IConsulConfigurationClient> _consulConfigClientMock;
        private Mock<IConfigurationParser> _configParserMock;

        [SetUp]
        public void SetUp()
        {
            _configParserMock = new Mock<IConfigurationParser>(MockBehavior.Strict);
            _consulConfigSourceMock = new Mock<IConsulConfigurationSource>(MockBehavior.Strict);
            _consulConfigSourceMock.SetupGet(ccs => ccs.Parser).Returns(_configParserMock.Object);
            _consulConfigSourceMock.SetupGet(ccs => ccs.Key).Returns(_Key);
            _consulConfigSourceMock.SetupGet(ccs => ccs.Optional).Returns(false);
            _consulConfigSourceMock.SetupGet(ccs => ccs.ReloadOnChange).Returns(false);
            _consulConfigSourceMock.SetupGet(ccs => ccs.OnLoadException).Returns((Action<ConsulLoadExceptionContext>)null);

            _consulConfigClientMock = new Mock<IConsulConfigurationClient>(MockBehavior.Strict);

            _consulConfigProvider = new ConsulConfigurationProvider(_consulConfigSourceMock.Object, _consulConfigClientMock.Object);
        }

        [Test]
        public void ShouldThrowIfParserIsNullWhenConstructed()
        {
            _consulConfigSourceMock.SetupGet(ccs => ccs.Parser).Returns((IConfigurationParser)null);

            Assert.That(
                () => new ConsulConfigurationProvider(_consulConfigSourceMock.Object, _consulConfigClientMock.Object), 
                Throws.TypeOf<ArgumentNullException>()
                    .And.Message.Contains(nameof(_consulConfigSourceMock.Object.Parser)));
        }

        [Test]
        public void ShouldThrowIfKeyIsNullWhenConstructed()
        {
            _consulConfigSourceMock.SetupGet(ccs => ccs.Key).Returns((string)null);

            Assert.That(
                () => new ConsulConfigurationProvider(_consulConfigSourceMock.Object, _consulConfigClientMock.Object), 
                Throws.TypeOf<ArgumentNullException>()
                    .And.Message.Contains(nameof(_consulConfigSourceMock.Object.Key)));
        }

        [Test]
        public void ShouldThrowIfKeyIsWhitespaceWhenConstructed()
        {
            _consulConfigSourceMock.SetupGet(ccs => ccs.Parser).Returns(_configParserMock.Object);
            _consulConfigSourceMock.SetupGet(ccs => ccs.Key).Returns("   ");

            Assert.That(
                () => new ConsulConfigurationProvider(_consulConfigSourceMock.Object, _consulConfigClientMock.Object), 
                Throws.TypeOf<ArgumentNullException>()
                    .And.Message.Contains(nameof(_consulConfigSourceMock.Object.Key)));
        }

        [Test]
        [TestCase("Key", "Key")]
        [TestCase("KEY", "key", TestName = "ShouldParseLoadedConfigIntoCaseInsensitiveDictionary")]
        public void ShouldParseLoadedConfigIntoDictionary(string actualKey, string lookupKey)
        {
            const string configValue = "Value";
            var parsedData = new Dictionary<string, string>{{actualKey, configValue}};
            Stream stream = null;
            Task<Stream> configStreamTask = Task.FromResult(stream);

            _configParserMock.Setup(cp => cp.Parse(stream)).Returns(parsedData);
            _consulConfigClientMock.Setup(ccc => ccc.GetConfig(_Key, false)).Returns(configStreamTask);
            
            _consulConfigProvider.Load();

            string actualValue;
            _consulConfigProvider.TryGet(lookupKey, out actualValue);
            Assert.That(actualValue, Is.EqualTo(configValue));
        }

        [Test]
        public void ShouldLoadConfigOptionallyIfSourceOptionalIsSetTrue()
        {
            Stream stream = null;
            Task<Stream> configStreamTask = Task.FromResult(stream);

            _configParserMock.Setup(cp => cp.Parse(stream)).Returns(new Dictionary<string, string>());
            _consulConfigSourceMock.SetupGet(ccs => ccs.Optional).Returns(true);
            _consulConfigClientMock.Setup(ccc => ccc.GetConfig(_Key, true)).Returns(configStreamTask);
            
            _consulConfigProvider.Load();

            Assert.That(() => _consulConfigClientMock.Verify(ccc => ccc.GetConfig(_Key, true)), Throws.Nothing);
        }

        [Test]
        public void ShouldCallSourceOnLoadExceptionActionWhenExceptionDuringLoad()
        {
            bool calledOnLoadException = false;
            Exception exception = new Exception("Failed to load from Consul agent");
            Task<Stream> configStreamTask = Task.FromException<Stream>(exception);
            
            _consulConfigClientMock.Setup(ccc => ccc.GetConfig(It.IsAny<string>(), It.IsAny<bool>())).Returns(configStreamTask);
            _consulConfigSourceMock.SetupGet(ccs => ccs.OnLoadException).Returns(exceptionContext => {
                calledOnLoadException = true;
            });
            
            try
            {
                _consulConfigProvider.Load();
            }
            finally
            {
                Assert.That(calledOnLoadException, Is.True);
            }
        }

        [Test]
        public void ShouldSetExceptionInLoadExceptionContextWhenExceptionDuringLoad()
        {
            ConsulLoadExceptionContext actualExceptionContext = null;
            Exception exception = new Exception("Failed to load from Consul agent");
            Task<Stream> configStreamTask = Task.FromException<Stream>(exception);
            
            _consulConfigClientMock.Setup(ccc => ccc.GetConfig(It.IsAny<string>(), It.IsAny<bool>())).Returns(configStreamTask);
            _consulConfigSourceMock.SetupGet(ccs => ccs.OnLoadException).Returns(exceptionContext => {
                actualExceptionContext = exceptionContext;
            });
            
            try 
            {
                _consulConfigProvider.Load();
            }
            finally
            {
                Assert.That(actualExceptionContext.Exception, Is.SameAs(exception));
            }
        }

        [Test]
        public void ShouldThrowExceptionIfOnLoadExceptionDoesNotSetIgnoreWhenExceptionDuringLoad()
        {
            Exception exception = new Exception("Failed to load from Consul agent");
            Task<Stream> configStreamTask = Task.FromException<Stream>(exception);
            
            _consulConfigClientMock.Setup(ccc => ccc.GetConfig(It.IsAny<string>(), It.IsAny<bool>())).Returns(configStreamTask);
            _consulConfigSourceMock.SetupGet(ccs => ccs.OnLoadException).Returns(exceptionContext => {
                exceptionContext.Ignore = false;
            });

            Assert.That(() => _consulConfigProvider.Load(), Throws.Exception.SameAs(exception));
        }

        [Test]
        public void ShouldNotThrowExceptionIfOnLoadExceptionDoesSetIgnoreWhenExceptionDuringLoad()
        {
            Exception exception = new Exception("Failed to load from Consul agent");
            Task<Stream> configStreamTask = Task.FromException<Stream>(exception);
            
            _consulConfigClientMock.Setup(ccc => ccc.GetConfig(It.IsAny<string>(), It.IsAny<bool>())).Returns(configStreamTask);
            _consulConfigSourceMock.SetupGet(ccs => ccs.OnLoadException).Returns(exceptionContext => {
                exceptionContext.Ignore = true;
            });

            Assert.That(() => _consulConfigProvider.Load(), Throws.Nothing);
        }
    }
}