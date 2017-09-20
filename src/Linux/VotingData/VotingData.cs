﻿using System;
using System.Collections.Generic;
using System.Fabric;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ServiceFabric.Services.Communication.AspNetCore;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.ServiceFabric.Data;

namespace VotingData
{
    /// <summary>
    /// The FabricRuntime creates an instance of this class for each service type instance. 
    /// </summary>
    internal sealed class VotingData : StatelessService
    {
        public VotingData(StatelessServiceContext context)
            : base(context)
        { }

        /// <summary>
        /// Optional override to create listeners (like tcp, http) for this service instance.
        /// </summary>
        /// <returns>The collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return new ServiceInstanceListener[]
            {
                new ServiceInstanceListener(serviceContext =>
                    new KestrelCommunicationListener(serviceContext, (url, listener) =>
                    {
                        try
                        {
                            Console.WriteLine("building WebHostBuilder");
                            //ServiceEventSource.Current.ServiceMessage(serviceContext, $"Starting Kestrel on {url}");
                            string rootDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

                            return new WebHostBuilder()
                                        .UseKestrel()
                                        .ConfigureServices(
                                            services => services
                                                .AddSingleton<StatelessServiceContext>(serviceContext)
                                                .AddSingleton<IReliableStateManager>(new Mocks.MockReliableStateManager()))
                                        .UseContentRoot(rootDir)
                                        .UseStartup<Startup>()
                                        .UseApplicationInsights()
                                        .UseServiceFabricIntegration(listener, ServiceFabricIntegrationOptions.UseUniqueServiceUrl)
                                        .UseUrls(url)
                                        .Build();
                        }
                        catch(Exception ex)
                        {
                            Console.WriteLine("Web HostBuilder exception: {0}", ex);
                            throw;
                        }
                    }))
            };
        }
    }
}
