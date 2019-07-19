using Castle.DynamicProxy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;
using Xunit;
using Castle.Components.DictionaryAdapter;
using System.Collections;

namespace Microservices.Logging.Tests
{
    public enum LogLevel
    {
        Debug = 1,
        Error = 4,
        Fatal = 5,
        Info = 2,
        Trace = 0,
        Warn = 3,
    }
    public interface IExceptionBase
    {
        string ClassName { get; set; }
        string Message { get; set; }
        IExceptionBase InnerException { get; set; }
        string StackTrace { get; set; }
    }
    public interface ILoggerDto
    {
        Guid Id { get; set; }
        LogLevel LogLevel { get; set; }
        Guid AppId { get; set; }
        Guid EnvId { get; set; }
        string Message { get; set; }

    }
    public interface ILoggerExceptionDto : ILoggerDto
    {
        IExceptionBase Exception { get; set; }
    }


    public class ServiceDbContext<IDbModel> : DbContext
      where IDbModel : class
    {
        public ServiceDbContext(DbContextOptions<ServiceDbContext<IDbModel>> options)
         : base(options)
        {
       
            this.Database.EnsureCreated();
        }
        public ServiceDbContext() : this(new DbContextOptions<ServiceDbContext<IDbModel>>()) { }


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionString = @"Server=localhost;Database=EFGetStarted.AspNetCore.NewDb;Trusted_Connection=True;ConnectRetryCount=0";
            optionsBuilder.UseSqlServer(connectionString).EnableDetailedErrors().EnableSensitiveDataLogging();
        }

        public DbSet<IDbModel> DbEntries { get; set; }
    }

    public class LoggerIMP : ILoggerDto
    {
        public Guid Id { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public LogLevel LogLevel { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Guid AppId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Guid EnvId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string Message { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }

    public class InterfaceEFTests
    {
        [Fact]
        public void TestEFGeneration()
        {
            var proxyGen = new ProxyGenerator();



            //var client = new WebApiClient();
            //client.BaseUrl = "http://localhost:28180/api/ilogger/";
            //var i = new TestInterfaceInterceptor(client);

            var p = proxyGen.CreateInterfaceProxyWithoutTarget<ILoggerDto>();

            var pType = p.GetType();
            var boxedPContext = Activator.CreateInstance(pType);

            var factory = new DictionaryAdapterFactory();
            var dictionary = new Hashtable();
            var adapter = factory.GetAdapter<ILoggerDto>(dictionary);
            var ctxType = typeof(ServiceDbContext<>).MakeGenericType(adapter.GetType());
            var boxedContext = Activator.CreateInstance(ctxType);



            var compiledCtx = new ServiceDbContext<LoggerIMP>();
    

            var context = (ServiceDbContext<ILoggerDto>)boxedContext;
            adapter.AppId = Guid.NewGuid();
            adapter.EnvId = Guid.NewGuid();
            adapter.Id = Guid.NewGuid();
            adapter.LogLevel = LogLevel.Debug;
            adapter.Message = "Unit test";

            compiledCtx.Add(adapter);
            compiledCtx.SaveChanges();

            var loggerDtoFromDb = context.DbEntries.FirstAsync(x => x.Id == p.Id).GetAwaiter().GetResult();

            Assert.Equal(loggerDtoFromDb.Id, p.Id);
        }
    }
}
