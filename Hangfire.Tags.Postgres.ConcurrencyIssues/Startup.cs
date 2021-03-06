using System;
using Hangfire.Console.Extensions.InitalizationIssueRepro;
using Hangfire.PostgreSql;
using Hangfire.Tags.PostgreSql;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Hangfire.Tags.Postgre.ConcurrencyIssues
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
        
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace).AddConsole());
            
            services.AddHangfire((provider, configuration) => configuration
                .UsePostgreSqlStorage(Configuration.GetConnectionString("HangfirePostgres"))
                .UseTagsWithPostgreSql(new TagsOptions(){TagsListStyle = TagsListStyle.Dropdown})
            );
            services.AddHangfireServer(options =>
            {
                options.Queues = new[] {"default", "worker-queue"};
                options.WorkerCount = Environment.ProcessorCount - 1;
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHangfireDashboard();
            app.UseRouting();
            app.UseEndpoints(endpoints => { endpoints.MapGet("/", async context => { await context.Response.WriteAsync("Hello World!"); }); });

            RecurringJob.AddOrUpdate<SimpleJob>("generator-1",job => job.RunGeneratorJob(JobType.Type1), Cron.Minutely);
            RecurringJob.AddOrUpdate<SimpleJob>("generator-2",job => job.RunGeneratorJob(JobType.Type2), Cron.Minutely);
            RecurringJob.AddOrUpdate<SimpleJob>("generator-3",job => job.RunGeneratorJob(JobType.Type2), Cron.Minutely);
            
            RecurringJob.Trigger("generator-1");
            RecurringJob.Trigger("generator-2");
            RecurringJob.Trigger("generator-3");
        }
    }
}