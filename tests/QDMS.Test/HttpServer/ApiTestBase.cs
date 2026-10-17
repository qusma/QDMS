// -----------------------------------------------------------------------
// <copyright file="ApiTestBase.cs" company="">
// Copyright 2026 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityData;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using QDMS;
using QDMS.Server;
using QDMS.Server.Brokers;
using QDMS.Server.Http;
using QDMS.Server.Repositories;
using QDMS.Server.Services;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace QDMSTest.HttpServer
{
    /// <summary>
    /// Runs the real HTTP API in memory against a fresh Postgres database per test.
    /// Everything that isn't the metadata DB (bar storage, brokers, jobs) is mocked.
    /// </summary>
    public abstract class ApiTestBase
    {
        protected const string ApiKey = "testkey";

        protected WebApplication App;
        protected HttpClient Client;
        protected string ConnectionString;
        protected Mock<IDataStorage> DataStorageMock;
        protected Mock<IJobsRepository> JobsRepoMock;

        [SetUp]
        public async Task SetUpServer()
        {
            ConnectionString = await PostgresFixture.CreateDatabase();

            DataStorageMock = new Mock<IDataStorage>();
            JobsRepoMock = new Mock<IJobsRepository>();

            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            builder.Logging.ClearProviders();

            var services = builder.Services;
            services.AddDbContextFactory<MyDBContext>(o => DbSetup.ConfigureMetadataDb(o, ConnectionString));
            services.AddScoped<IMyDbContext>(sp => sp.GetRequiredService<MyDBContext>());
            services.AddScoped<IInstrumentSource, InstrumentRepository>();
            services.AddSingleton(DataStorageMock.Object);
            services.AddSingleton(JobsRepoMock.Object);
            services.AddSingleton(new Mock<IDatasourceService>().Object);
            services.AddSingleton(new Mock<IDividendsBroker>().Object);
            services.AddSingleton(new Mock<IEarningsAnnouncementBroker>().Object);
            services.AddSingleton(new Mock<IEconomicReleaseBroker>().Object);
            services.AddQdmsApiValidators();

            App = builder.Build();
            App.UseQdmsErrorHandling();
            App.MapQdmsApi(ApiKey);
            await App.StartAsync();

            Client = App.GetTestClient();
            Client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", ApiKey);
        }

        [TearDown]
        public async Task TearDownServer()
        {
            Client?.Dispose();
            if (App != null) await App.DisposeAsync();
        }

        /// <summary>
        /// A context on the test database, for seeding and checking results.
        /// </summary>
        protected MyDBContext CreateContext() => PostgresFixture.CreateMetadataContext(ConnectionString);

        /// <summary>
        /// Adds a datasource; instruments can't be saved without one.
        /// </summary>
        protected async Task<Datasource> AddDatasource(string name = "TestSource")
        {
            using var ctx = CreateContext();
            var ds = new Datasource { Name = name };
            ctx.Datasources.Add(ds);
            await ctx.SaveChangesAsync();
            return ds;
        }

        protected static async Task<T> Read<T>(HttpResponseMessage response) =>
            JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync(), HttpApi.SerializerSettings);

        protected static StringContent JsonBody(object value) =>
            new StringContent(JsonConvert.SerializeObject(value, HttpApi.SerializerSettings), Encoding.UTF8, "application/json");
    }
}
