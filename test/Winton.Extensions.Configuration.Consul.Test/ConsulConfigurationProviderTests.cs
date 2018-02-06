using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Primitives;
using Moq;
using NUnit.Framework;
using Winton.Extensions.Configuration.Consul.Parsers;

namespace Winton.Extensions.Configuration.Consul
{
    [TestFixture]
    [TestOf(nameof(ConsulConfigurationProvider))]
    internal sealed class ConsulConfigurationProviderTests
    {
        private const string _Key = "Test/Development";
        private Mock<IChangeToken> _changeTokenMock;
        private Mock<IConfigurationParser> _configParserMock;
        private Mock<IConfigQueryResult> _configQueryResultMock;
        private Mock<IConsulConfigurationClient> _consulConfigClientMock;

        private ConsulConfigurationProvider _consulConfigProvider;
        private Mock<IConsulConfigurationSource> _consulConfigSourceMock;

        [SetUp]
        public void SetUp()
        {
            _configParserMock = new Mock<IConfigurationParser>(MockBehavior.Strict);
            _consulConfigSourceMock = new Mock<IConsulConfigurationSource>(MockBehavior.Strict);
            _consulConfigSourceMock.Setup(ccs => ccs.Parser).Returns(_configParserMock.Object);
            _consulConfigSourceMock.Setup(ccs => ccs.Key).Returns(_Key);
            _consulConfigSourceMock.Setup(ccs => ccs.Optional).Returns(false);
            _consulConfigSourceMock.Setup(ccs => ccs.ReloadOnChange).Returns(false);
            _consulConfigSourceMock.Setup(ccs => ccs.OnLoadException).Returns(null);
            _consulConfigSourceMock.Setup(ccs => ccs.OnWatchException).Returns(() => ctx => { });

            _changeTokenMock = new Mock<IChangeToken>(MockBehavior.Strict);
            _configQueryResultMock = new Mock<IConfigQueryResult>(MockBehavior.Strict);

            _consulConfigClientMock = new Mock<IConsulConfigurationClient>(MockBehavior.Strict);
            _consulConfigClientMock
                .Setup(ccc => ccc.GetConfig())
                .ReturnsAsync(_configQueryResultMock.Object);
            _consulConfigClientMock
                .Setup(ccc => ccc.Watch(_consulConfigSourceMock.Object.OnWatchException))
                .Returns(_changeTokenMock.Object);

            _consulConfigProvider =
                new ConsulConfigurationProvider(_consulConfigSourceMock.Object, _consulConfigClientMock.Object);
        }

        [Test]
        public void ShouldCallSourceOnLoadExceptionActionWhenExceptionDuringLoad()
        {
            var calledOnLoadException = false;

            _consulConfigClientMock.Setup(ccc => ccc.GetConfig()).ThrowsAsync(new Exception());
            _consulConfigSourceMock
                .Setup(ccs => ccs.OnLoadException)
                .Returns(exceptionContext => { calledOnLoadException = true; });

            try
            {
                _consulConfigProvider.Load();
            }
            catch
            {
            }

            Assert.That(calledOnLoadException, Is.True);
        }

        [Test]
        public void ShouldHaveEmptyDataIfConfigDoesNotExistdAndIsOptional()
        {
            _consulConfigSourceMock.Setup(ccs => ccs.Optional).Returns(true);
            _configQueryResultMock.Setup(cqr => cqr.Exists).Returns(false);

            _consulConfigProvider.Load();

            Assert.That(
                _consulConfigProvider.GetChildKeys(Enumerable.Empty<string>(), string.Empty),
                Is.EqualTo(Enumerable.Empty<string>()).AsCollection);
        }

        [Test]
        public void ShouldNotOverwriteNonOptionalConfigIfDoesNotExistOnReload()
        {
            const string key = "Key";
            const string value = "Test";
            var originalData = new Dictionary<string, string> { { key, value } };
            Action<object> onChangeAction = null;

            _consulConfigSourceMock.Setup(ccs => ccs.Optional).Returns(false);
            _consulConfigSourceMock.Setup(ccs => ccs.ReloadOnChange).Returns(true);
            _configQueryResultMock.Setup(cqr => cqr.Exists).Returns(false);
            _changeTokenMock
                .Setup(ct => ct.RegisterChangeCallback(It.IsAny<Action<object>>(), It.IsAny<object>()))
                .Callback((Action<object> action, object state) => { onChangeAction = action; })
                .Returns(null as IDisposable);

            var configProvider = new ConsulConfigurationProvider(
                _consulConfigSourceMock.Object,
                _consulConfigClientMock.Object);

            DoLoad(originalData);

            onChangeAction(null);

            Assert.That(configProvider.TryGet(key, out string _), Is.True);
        }

        [Test]
        public void ShouldNotParseIfConfigBytesIsNullWhenLoad()
        {
            _consulConfigSourceMock.Setup(ccs => ccs.Optional).Returns(true);
            _configQueryResultMock.Setup(cqr => cqr.Exists).Returns(false);

            _consulConfigProvider.Load();

            Assert.That(
                () => _configParserMock.Verify(cp => cp.Parse(It.IsAny<MemoryStream>()), Times.Never),
                Throws.Nothing);
        }

        [Test]
        public void ShouldNotThrowExceptionIfOnLoadExceptionDoesSetIgnoreWhenExceptionDuringLoad()
        {
            var exception = new Exception("Failed to load from Consul agent");

            _consulConfigClientMock.Setup(ccc => ccc.GetConfig()).ThrowsAsync(exception);
            _consulConfigSourceMock
                .Setup(ccs => ccs.OnLoadException)
                .Returns(exceptionContext => { exceptionContext.Ignore = true; });

            Assert.That(() => _consulConfigProvider.Load(), Throws.Nothing);
        }

        [Test]
        [TestCase(true, TestName = "ShouldNotThrowIfDoesNotExistOnReloadWhenConfigOptional")]
        [TestCase(false, TestName = "ShouldNotThrowIfDoesNotExistOnReloadWhenConfigIsNotOptional")]
        public void ShouldNotThrowIfDoesNotExistOnReload(bool optional)
        {
            Action<object> onChangeAction = null;
            _consulConfigSourceMock.Setup(ccs => ccs.Optional).Returns(optional);
            _consulConfigSourceMock.Setup(ccs => ccs.ReloadOnChange).Returns(true);
            _configQueryResultMock.Setup(cqr => cqr.Exists).Returns(false);
            _changeTokenMock
                .Setup(ct => ct.RegisterChangeCallback(It.IsAny<Action<object>>(), It.IsAny<object>()))
                .Callback((Action<object> action, object state) => { onChangeAction = action; })
                .Returns(null as IDisposable);

            var configProvider =
                new ConsulConfigurationProvider(_consulConfigSourceMock.Object, _consulConfigClientMock.Object);

            Assert.That(() => onChangeAction(null), Throws.Nothing);
        }

        [Test]
        [TestCase("Key", "Key")]
        [TestCase("KEY", "key", TestName = "ShouldParseLoadedConfigIntoCaseInsensitiveDictionary")]
        public void ShouldParseLoadedConfigIntoDictionary(string actualKey, string lookupKey)
        {
            const string configValue = "Value";
            var parsedData = new Dictionary<string, string> { { actualKey, configValue } };

            DoLoad(parsedData);

            _consulConfigProvider.TryGet(lookupKey, out string actualValue);
            Assert.That(actualValue, Is.EqualTo(configValue));
        }

        [Test]
        public void ShouldReloadConfigIfReloadOnChangesAndDataInConsulHasChanged()
        {
            Action<object> onChangeAction = null;
            _consulConfigSourceMock.Setup(ccs => ccs.ReloadOnChange).Returns(true);
            _configQueryResultMock.Setup(cqr => cqr.Exists).Returns(false);
            _changeTokenMock
                .Setup(ct => ct.RegisterChangeCallback(It.IsAny<Action<object>>(), It.IsAny<object>()))
                .Callback((Action<object> action, object state) => { onChangeAction = action; })
                .Returns(null as IDisposable);

            var configProvider =
                new ConsulConfigurationProvider(_consulConfigSourceMock.Object, _consulConfigClientMock.Object);

            onChangeAction(null);

            Assert.That(() => _consulConfigClientMock.Verify(ccc => ccc.GetConfig()), Throws.Nothing);
        }

        [Test]
        public void ShouldSetExceptionInLoadExceptionContextWhenExceptionDuringLoad()
        {
            ConsulLoadExceptionContext actualExceptionContext = null;
            var expectedException = new Exception("Failed to load from Consul agent");

            _consulConfigClientMock.Setup(ccc => ccc.GetConfig()).ThrowsAsync(expectedException);
            _consulConfigSourceMock
                .Setup(ccs => ccs.OnLoadException)
                .Returns(exceptionContext => { actualExceptionContext = exceptionContext; });

            try
            {
                _consulConfigProvider.Load();
            }
            catch
            {
            }

            Assert.That(actualExceptionContext.Exception, Is.SameAs(expectedException));
        }

        [Test]
        public void ShouldSetSourceInLoadExceptionContextWhenExceptionDuringLoad()
        {
            ConsulLoadExceptionContext actualExceptionContext = null;

            _consulConfigClientMock.Setup(ccc => ccc.GetConfig()).ThrowsAsync(new Exception());
            _consulConfigSourceMock
                .Setup(ccs => ccs.OnLoadException)
                .Returns(exceptionContext => { actualExceptionContext = exceptionContext; });

            try
            {
                _consulConfigProvider.Load();
            }
            catch
            {
            }

            Assert.That(actualExceptionContext.Source, Is.SameAs(_consulConfigSourceMock.Object));
        }

        [Test]
        public void ShouldThrowExceptionIfOnLoadExceptionDoesNotSetIgnoreWhenExceptionDuringLoad()
        {
            var exception = new Exception("Failed to load from Consul agent");

            _consulConfigClientMock.Setup(ccc => ccc.GetConfig()).ThrowsAsync(exception);
            _consulConfigSourceMock
                .Setup(ccs => ccs.OnLoadException)
                .Returns(exceptionContext => { exceptionContext.Ignore = false; });

            Assert.That(() => _consulConfigProvider.Load(), Throws.Exception.SameAs(exception));
        }

        [Test]
        public void ShouldThrowIfConfigDoesNotExistAndIsNotOptonalWhenLoad()
        {
            ConsulLoadExceptionContext actualExceptionContext = null;
            _consulConfigSourceMock.Setup(ccs => ccs.Optional).Returns(false);
            _configQueryResultMock.Setup(cqr => cqr.Exists).Returns(false);

            _consulConfigSourceMock
                .Setup(ccs => ccs.OnLoadException)
                .Returns(exceptionContext => { actualExceptionContext = exceptionContext; });

            try
            {
                _consulConfigProvider.Load();
            }
            catch
            {
            }

            Assert.That(
                actualExceptionContext.Exception.Message,
                Is.EqualTo($"The configuration for key {_Key} was not found and is not optional."));
        }

        [Test]
        public void ShouldThrowIfParserIsNullWhenConstructed()
        {
            _consulConfigSourceMock.Setup(ccs => ccs.Parser).Returns((IConfigurationParser)null);

            Assert.That(
                () => new ConsulConfigurationProvider(_consulConfigSourceMock.Object, _consulConfigClientMock.Object),
                Throws.TypeOf<ArgumentNullException>()
                      .And.Message.Contains(nameof(_consulConfigSourceMock.Object.Parser)));
        }

        [Test]
        public void ShouldWatchForChangesIfSourceReloadOnChangesIsTrue()
        {
            _consulConfigSourceMock.Setup(ccs => ccs.ReloadOnChange).Returns(true);
            _changeTokenMock
                .Setup(ct => ct.RegisterChangeCallback(It.IsAny<Action<object>>(), It.IsAny<object>()))
                .Returns(null as IDisposable);

            var configProvider =
                new ConsulConfigurationProvider(_consulConfigSourceMock.Object, _consulConfigClientMock.Object);

            Assert.That(
                () => _consulConfigClientMock.Verify(ccs => ccs.Watch(_consulConfigSourceMock.Object.OnWatchException)),
                Throws.Nothing);
        }

        private void DoLoad(IDictionary<string, string> data)
        {
            _configParserMock.Setup(cp => cp.Parse(It.IsAny<MemoryStream>())).Returns(data);
            _configQueryResultMock.Setup(cqr => cqr.Exists).Returns(true);
            _configQueryResultMock.Setup(cqr => cqr.Value).Returns(
                new byte[]
                {
                });

            _consulConfigProvider.Load();
        }
    }
}