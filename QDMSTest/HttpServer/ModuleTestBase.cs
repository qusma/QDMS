// -----------------------------------------------------------------------
// <copyright file="ModuleBase.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityData;
using Moq;
using Nancy;
using Nancy.Responses.Negotiation;
using Nancy.Testing;
using QDMS;
using QDMS.Server;
using QDMSServer;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace QDMSTest.HttpServer
{
    /// <summary>
    /// Base for testing Nancy modules
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TModule"></typeparam>
    public abstract class ModuleTestBase<T, TModule>
        where T : class
        where TModule : INancyModule
    {
        protected Browser Browser;
        protected Action<BrowserContext> BrowserCtx;
        protected Mock<DbSet<T>> DbSetMock;
        protected Mock<IMyDbContext> ContextMock = new Mock<IMyDbContext>();
        protected Mock<IInstrumentSource> InstrumentRepoMock = new Mock<IInstrumentSource>();
        private Mock<IDataStorage> _dataStorageMock = new Mock<IDataStorage>();

        public void SetUp(List<T> data)
        {
            //this is a super dirty hack...fluent validation is not used in this project directly
            //so the assembly is not loaded, so the Nancy ioc system doesn't find it, making tests fail
            //the solution is to use it, thus forcing it to be loaded.
            var asdf = new Nancy.Validation.FluentValidation.EmailAdapter();

            //Set up the IMyDbContext mock so the relevant set returns the provided data
            DbSetMock = SetUpDbSet(data);

            //set up the nancy bootstrapper
            var bootstrapper = new ConfigurableBootstrapper(with =>
            {
                //Add mock dependencies here
                with.Module<TModule>();
                with.Dependency<IMyDbContext>(ContextMock.Object);
                with.Dependency<IDataStorage>(_dataStorageMock.Object);
                with.Dependency<IInstrumentSource>(InstrumentRepoMock.Object);

                //Takes care of user athentication
                with.RequestStartup((container, pipelines, context) =>
                    {
                        context.CurrentUser = new ClaimsPrincipal(new GenericIdentity("admin"));
                    });
            });

            //The browser is used to create requests
            Browser = new Browser(bootstrapper, defaults: with =>
            {
                with.Accept(new MediaRange("application/json"));
            });

            BrowserCtx = new Action<BrowserContext>(with =>
            {
                with.HttpRequest();
                with.Header("Content-Type", "application/json");
            });
        }

        /// <summary>
        /// Creates a DbSet mock with all the necessary set-ups to return data, and adds it to the DbContext mock
        /// </summary>
        /// <typeparam name="T2"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        protected Mock<DbSet<T2>> SetUpDbSet<T2>(List<T2> data) where T2 : class
        {
            var dbSetMock = new Mock<DbSet<T2>>();
            dbSetMock.Setup(x => x.Include(It.IsAny<string>())).Returns(dbSetMock.Object);
            var q = dbSetMock.As<IQueryable<T2>>();
            var queriableData = data.AsQueryable();
            q.Setup(m => m.Provider).Returns(queriableData.Provider);
            q.Setup(m => m.Expression).Returns(queriableData.Expression);
            q.Setup(m => m.ElementType).Returns(queriableData.ElementType);
            q.Setup(x => x.GetEnumerator()).Returns(queriableData.GetEnumerator());
            dbSetMock.As<IDbAsyncEnumerable<T2>>()
                .Setup(m => m.GetAsyncEnumerator())
                .Returns(new AsyncEnumerator<T2>(data.GetEnumerator()));

            ContextMock.Setup(x => x.Set<T2>()).Returns(dbSetMock.Object);

            return dbSetMock;
        }
    }

    internal class AsyncEnumerator<T> : IDbAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;

        public AsyncEnumerator(IEnumerator<T> inner)
        {
            _inner = inner;
        }

        public void Dispose() => _inner.Dispose();

        public Task<bool> MoveNextAsync(CancellationToken cancellationToken) => Task.FromResult(_inner.MoveNext());

        public T Current => _inner.Current;
        object IDbAsyncEnumerator.Current => Current;
    }
}