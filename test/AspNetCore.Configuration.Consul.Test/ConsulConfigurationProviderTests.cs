using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        [TestCase("Key", "Key")]
        [TestCase("KEY", "key", TestName = "ShouldParseLoadedConfigIntoCaseInsensitiveDictionary")]
        public void ShouldParseLoadedConfigIntoDictionary(string actualKey, string lookupKey)
        {
            const string configValue = "Value";
            var parsedData = new Dictionary<string, string>{{actualKey, configValue}};
            Task<byte[]> configStreamTask = Task.FromResult(new byte[]{});

            _consulConfigClientMock.Setup(ccc => ccc.GetConfig(false)).Returns(configStreamTask);
            _configParserMock.Setup(cp => cp.Parse(It.IsAny<MemoryStream>())).Returns(parsedData);
            
            _consulConfigProvider.Load();

            string actualValue;
            _consulConfigProvider.TryGet(lookupKey, out actualValue);
            Assert.That(actualValue, Is.EqualTo(configValue));
        }

        [Test]
        public void ShouldHaveEmptyDataIfConfigIsMissingAndOptional()
        {
            Task<byte[]> configBytesTask = Task.FromResult((byte[])null);
            _consulConfigSourceMock.SetupGet(ccs => ccs.Optional).Returns(true);
            _consulConfigClientMock.Setup(ccc => ccc.GetConfig(true)).Returns(configBytesTask);
            
            _consulConfigProvider.Load();

            Assert.That(
                _consulConfigProvider.GetChildKeys(Enumerable.Empty<string>(), ""),
                Is.EqualTo(Enumerable.Empty<string>()).AsCollection);
        }

        [Test]
        public void ShouldNotParseIfConfigBytesIsNullWhenLoad()
        {
            Task<byte[]> configBytesTask = Task.FromResult((byte[])null);
            _consulConfigClientMock.Setup(ccc => ccc.GetConfig(false)).Returns(configBytesTask);
            
            _consulConfigProvider.Load();

            Assert.That(() => _configParserMock.Verify(cp => cp.Parse(It.IsAny<MemoryStream>()), Times.Never), Throws.Nothing);
        }

        [Test]
        public void ShouldLoadConfigOptionallyIfSourceOptionalIsSetTrue()
        {
            Task<byte[]> configBytesTask = Task.FromResult((byte[])null);

            _consulConfigSourceMock.SetupGet(ccs => ccs.Optional).Returns(true);
            _consulConfigClientMock.Setup(ccc => ccc.GetConfig(true)).Returns(configBytesTask);
            
            _consulConfigProvider.Load();

            Assert.That(() => _consulConfigClientMock.Verify(ccc => ccc.GetConfig(true)), Throws.Nothing);
        }

        [Test]
        public void ShouldCallSourceOnLoadExceptionActionWhenExceptionDuringLoad()
        {
            bool calledOnLoadException = false;
            Exception exception = new Exception("Failed to load from Consul agent");
            Task<byte[]> configBytesTask = Task.FromException<byte[]>(exception);
            
            _consulConfigClientMock.Setup(ccc => ccc.GetConfig(It.IsAny<bool>())).Returns(configBytesTask);
            _consulConfigSourceMock.SetupGet(ccs => ccs.OnLoadException).Returns(exceptionContext => {
                calledOnLoadException = true;
            });
            
            try
            {
                _consulConfigProvider.Load();
            }
            catch {}

            Assert.That(calledOnLoadException, Is.True);
        }

        [Test]
        public void ShouldSetExceptionInLoadExceptionContextWhenExceptionDuringLoad()
        {
            ConsulLoadExceptionContext actualExceptionContext = null;
            Exception exception = new Exception("Failed to load from Consul agent");
            Task<byte[]> configBytesTask = Task.FromException<byte[]>(exception);
            
            _consulConfigClientMock.Setup(ccc => ccc.GetConfig(It.IsAny<bool>())).Returns(configBytesTask);
            _consulConfigSourceMock.SetupGet(ccs => ccs.OnLoadException).Returns(exceptionContext => {
                actualExceptionContext = exceptionContext;
            });
            
            try 
            {
                _consulConfigProvider.Load();
            }
            catch {}
            Assert.That(actualExceptionContext.Exception, Is.SameAs(exception));
        }

         [Test]
        public void ShouldSetSourceInLoadExceptionContextWhenExceptionDuringLoad()
        {
            ConsulLoadExceptionContext actualExceptionContext = null;
            Exception exception = new Exception("Failed to load from Consul agent");
            Task<byte[]> configBytesTask = Task.FromException<byte[]>(exception);
            
            _consulConfigClientMock.Setup(ccc => ccc.GetConfig(It.IsAny<bool>())).Returns(configBytesTask);
            _consulConfigSourceMock.SetupGet(ccs => ccs.OnLoadException).Returns(exceptionContext => {
                actualExceptionContext = exceptionContext;
            });
            
            try 
            {
                _consulConfigProvider.Load();
            }
            catch {}
            Assert.That(actualExceptionContext.Source, Is.SameAs(_consulConfigSourceMock.Object));
        }

        [Test]
        public void ShouldThrowExceptionIfOnLoadExceptionDoesNotSetIgnoreWhenExceptionDuringLoad()
        {
            Exception exception = new Exception("Failed to load from Consul agent");
            Task<byte[]> configBytesTask = Task.FromException<byte[]>(exception);
            
            _consulConfigClientMock.Setup(ccc => ccc.GetConfig(It.IsAny<bool>())).Returns(configBytesTask);
            _consulConfigSourceMock.SetupGet(ccs => ccs.OnLoadException).Returns(exceptionContext => {
                exceptionContext.Ignore = false;
            });

            Assert.That(() => _consulConfigProvider.Load(), Throws.Exception.SameAs(exception));
        }

        [Test]
        public void ShouldNotThrowExceptionIfOnLoadExceptionDoesSetIgnoreWhenExceptionDuringLoad()
        {
            Exception exception = new Exception("Failed to load from Consul agent");
            Task<byte[]> configBytesTask = Task.FromException<byte[]>(exception);
            
            _consulConfigClientMock.Setup(ccc => ccc.GetConfig(It.IsAny<bool>())).Returns(configBytesTask);
            _consulConfigSourceMock.SetupGet(ccs => ccs.OnLoadException).Returns(exceptionContext => {
                exceptionContext.Ignore = true;
            });

            Assert.That(() => _consulConfigProvider.Load(), Throws.Nothing);
        }
    }
}