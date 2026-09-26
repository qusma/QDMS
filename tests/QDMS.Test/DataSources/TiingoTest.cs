using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using QDMS;
using QDMS.Server.DataSources.Tiingo;

namespace QDMSTest
{
    [TestFixture]
    public class TiingoTest
    {
        //AAPL around its 4:1 split on 2020-08-31, from the real API, with a made-up dividend on 2020-09-01
        private const string AaplJson =
            "[{\"date\":\"2020-08-27T00:00:00.000Z\",\"close\":500.04,\"high\":509.94,\"low\":495.33,\"open\":508.57,\"volume\":38888096,\"adjClose\":121.16,\"adjHigh\":123.56,\"adjLow\":120.02,\"adjOpen\":123.22,\"adjVolume\":155552384,\"divCash\":0.0,\"splitFactor\":1.0}," +
            "{\"date\":\"2020-08-28T00:00:00.000Z\",\"close\":499.23,\"high\":505.77,\"low\":498.31,\"open\":504.05,\"volume\":46907479,\"adjClose\":120.96,\"adjHigh\":122.54,\"adjLow\":120.74,\"adjOpen\":122.13,\"adjVolume\":187629916,\"divCash\":0.0,\"splitFactor\":1.0}," +
            "{\"date\":\"2020-08-31T00:00:00.000Z\",\"close\":129.04,\"high\":131.0,\"low\":126.0,\"open\":127.58,\"volume\":223505733,\"adjClose\":125.06,\"adjHigh\":126.96,\"adjLow\":122.12,\"adjOpen\":123.65,\"adjVolume\":223505733,\"divCash\":0.0,\"splitFactor\":4.0}," +
            "{\"date\":\"2020-09-01T00:00:00.000Z\",\"close\":134.18,\"high\":134.8,\"low\":130.53,\"open\":132.76,\"volume\":152470142,\"adjClose\":130.04,\"adjHigh\":130.64,\"adjLow\":126.51,\"adjOpen\":128.67,\"adjVolume\":152470142,\"divCash\":0.82,\"splitFactor\":1.0}," +
            "{\"date\":\"2020-09-02T00:00:00.000Z\",\"close\":131.4,\"high\":137.98,\"low\":127.0,\"open\":137.59,\"volume\":200118991,\"adjClose\":127.35,\"adjHigh\":133.73,\"adjLow\":123.09,\"adjOpen\":133.35,\"adjVolume\":200118991,\"divCash\":0.0,\"splitFactor\":1.0}]";

        private FakeHandler _handler;
        private Tiingo _tiingo;
        private List<ErrorArgs> _errors;

        [SetUp]
        public void SetUp()
        {
            _handler = new FakeHandler();
            _tiingo = CreateTiingo("testkey");
            _tiingo.Connect();
            _errors = new List<ErrorArgs>();
            _tiingo.Error += (s, e) => { lock (_errors) _errors.Add(e); };
        }

        [TearDown]
        public void TearDown()
        {
            _handler.Dispose();
        }

        [Test]
        public async Task ParsesRawPricesDividendsAndSplits()
        {
            _handler.Respond(HttpStatusCode.OK, AaplJson);

            var data = (await Request(Aapl(), BarSize.OneDay)).Data;

            Assert.That(data, Has.Count.EqualTo(5));
            Assert.That(data.Select(x => x.DT), Is.Ordered);
            Assert.That(data[0].DT, Is.EqualTo(new DateTime(2020, 8, 27)));
            Assert.That(data[0].DT.Kind, Is.EqualTo(DateTimeKind.Unspecified));
            Assert.That(data[0].Open, Is.EqualTo(508.57m));
            Assert.That(data[0].High, Is.EqualTo(509.94m));
            Assert.That(data[0].Low, Is.EqualTo(495.33m));
            Assert.That(data[0].Close, Is.EqualTo(500.04m));
            Assert.That(data[0].Volume, Is.EqualTo(38888096));

            Assert.That(data.Select(x => x.Split), Is.EqualTo(new decimal?[] { null, null, 4m, null, null }));
            Assert.That(data.Select(x => x.Dividend), Is.EqualTo(new decimal?[] { null, null, null, 0.82m, null }));
            Assert.That(_errors, Is.Empty);
        }

        [Test]
        public async Task ComputesAdjustedPricesFromRawPrices()
        {
            _handler.Respond(HttpStatusCode.OK, AaplJson);

            var data = (await Request(Aapl(), BarSize.OneDay)).Data;

            //after the last dividend, adjusted == raw
            Assert.That(data[3].AdjClose, Is.EqualTo(134.18m));
            Assert.That(data[4].AdjClose, Is.EqualTo(131.4m));

            //the dividend scales everything before it by close / (close + div)
            decimal divRatio = 134.18m / (134.18m + 0.82m);
            Assert.That((double)data[2].AdjClose, Is.EqualTo((double)(129.04m * divRatio)).Within(1e-9));
            Assert.That((double)data[2].AdjOpen, Is.EqualTo((double)(127.58m * divRatio)).Within(1e-9));

            //the split divides pre-split prices by 4
            Assert.That((double)data[1].AdjClose, Is.EqualTo((double)(499.23m / 4 * divRatio)).Within(1e-9));
            Assert.That((double)data[0].AdjHigh, Is.EqualTo((double)(509.94m / 4 * divRatio)).Within(1e-9));
        }

        [Test]
        public async Task RequestsTheRightUrlWithTheApiKey()
        {
            _handler.Respond(HttpStatusCode.OK, "[]");
            var instrument = Aapl();
            instrument.DatasourceSymbol = "BRK-B";

            await Request(instrument, BarSize.OneDay, new DateTime(2020, 1, 2), new DateTime(2021, 3, 4));

            var request = _handler.Requests.Single();
            Assert.That(request.RequestUri.ToString(), Is.EqualTo(
                "https://api.tiingo.com/tiingo/daily/brk-b/prices?startDate=2020-01-02&endDate=2021-03-04&resampleFreq=daily"));
            Assert.That(request.Headers.Authorization.Scheme, Is.EqualTo("Token"));
            Assert.That(request.Headers.Authorization.Parameter, Is.EqualTo("testkey"));
        }

        [Test]
        public async Task RequestsResampledDataForLowerFrequencies()
        {
            _handler.Respond(HttpStatusCode.OK, "[]");

            await Request(Aapl(), BarSize.OneWeek);

            Assert.That(_handler.Requests.Single().RequestUri.Query, Does.EndWith("&resampleFreq=weekly"));
        }

        [Test]
        public async Task EmptyResponseReturnsNoData()
        {
            _handler.Respond(HttpStatusCode.OK, "[]");

            var e = await Request(Aapl(), BarSize.OneDay);

            Assert.That(e.Data, Is.Empty);
            Assert.That(_errors, Is.Empty);
        }

        [Test]
        public async Task HttpErrorRaisesErrorAndAnswersWithNoData()
        {
            _handler.Respond(HttpStatusCode.NotFound, "{\"detail\":\"Error: Ticker 'NOSUCHTICKER' not found\"}");

            var e = await Request(Aapl(), BarSize.OneDay);

            Assert.That(e.Data, Is.Empty);
            Assert.That(_errors, Has.Count.EqualTo(1));
            Assert.That(_errors[0].ErrorCode, Is.EqualTo(404));
            Assert.That(_errors[0].ErrorMessage, Does.Contain("Ticker 'NOSUCHTICKER' not found"));
        }

        [Test]
        public async Task NetworkFailureRaisesErrorAndAnswersWithNoData()
        {
            _handler.Throw(new HttpRequestException("connection refused"));

            var e = await Request(Aapl(), BarSize.OneDay);

            Assert.That(e.Data, Is.Empty);
            Assert.That(_errors.Single().ErrorMessage, Does.Contain("connection refused"));
        }

        [Test]
        public async Task IntradayRequestsAreRejectedWithoutCallingTheApi()
        {
            var e = await Request(Aapl(), BarSize.OneHour);

            Assert.That(e.Data, Is.Empty);
            Assert.That(_errors, Has.Count.EqualTo(1));
            Assert.That(_handler.Requests, Is.Empty);
        }

        [Test]
        public async Task SkipsBarsWithZeroPrices()
        {
            _handler.Respond(HttpStatusCode.OK,
                "[{\"date\":\"2020-08-27T00:00:00.000Z\",\"close\":0,\"high\":0,\"low\":0,\"open\":0,\"volume\":0,\"divCash\":0.0,\"splitFactor\":1.0}," +
                "{\"date\":\"2020-08-28T00:00:00.000Z\",\"close\":10,\"high\":11,\"low\":9,\"open\":10,\"volume\":100,\"divCash\":0.0,\"splitFactor\":1.0}]");

            var data = (await Request(Aapl(), BarSize.OneDay)).Data;

            Assert.That(data.Single().DT, Is.EqualTo(new DateTime(2020, 8, 28)));
            Assert.That(data.Single().AdjClose, Is.EqualTo(10m));
        }

        [Test]
        public void StaysDisconnectedWithoutApiKey()
        {
            var tiingo = CreateTiingo(null);

            tiingo.Connect();

            Assert.That(tiingo.Connected, Is.False);
            Assert.That(_tiingo.Connected, Is.True);
        }

        private Tiingo CreateTiingo(string apiKey)
        {
            var settings = new Mock<ISettings>();
            settings.SetupGet(x => x.tiingoApiKey).Returns(apiKey);
            return new Tiingo(settings.Object, new HttpClient(_handler, disposeHandler: false));
        }

        private static Instrument Aapl() => new Instrument
        {
            ID = 1,
            Symbol = "AAPL",
            Datasource = new Datasource { Name = "Tiingo" }
        };

        private async Task<HistoricalDataEventArgs> Request(Instrument instrument, BarSize frequency, DateTime? start = null, DateTime? end = null)
        {
            var arrived = new TaskCompletionSource<HistoricalDataEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
            _tiingo.HistoricalDataArrived += (s, e) => arrived.TrySetResult(e);

            var request = new HistoricalDataRequest(instrument, frequency, start ?? new DateTime(2020, 8, 27), end ?? new DateTime(2020, 9, 2));
            _tiingo.RequestHistoricalData(request);

            var e = await arrived.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.That(e.Request, Is.SameAs(request));
            return e;
        }

        private class FakeHandler : HttpMessageHandler
        {
            private Func<HttpResponseMessage> _respond = () => new HttpResponseMessage(HttpStatusCode.InternalServerError);

            public List<HttpRequestMessage> Requests { get; } = new List<HttpRequestMessage>();

            public void Respond(HttpStatusCode status, string body) =>
                _respond = () => new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

            public void Throw(Exception ex) => _respond = () => throw ex;

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Requests.Add(request);
                return Task.FromResult(_respond());
            }
        }
    }
}
