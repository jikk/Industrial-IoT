﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.KeyVault {
    using Microsoft.Azure.IIoT.Auth.Clients;
    using Microsoft.Azure.IIoT.Auth;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Azure.KeyVault;
    using Autofac;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Retrieve a working Keyvault client to bootstrap keyvault
    /// communcation
    /// </summary>
    public class KeyVaultClientBootstrap : IDisposable {

        /// <summary>
        /// Get client
        /// </summary>
        public KeyVaultClient Client => _container.Resolve<KeyVaultClient>();

        /// <summary>
        /// Create bootstrap
        /// </summary>
        public KeyVaultClientBootstrap(IConfiguration configuration = null) {
            configuration ??= new ConfigurationBuilder()
                .AddConfiguration(configuration)
                .AddEnvironmentVariables(EnvironmentVariableTarget.User)
                .AddFromDotEnvFile()
                .Build();

            // Create container with bootstrap logger and configuration to create
            // keyvault client
            var builder = new ContainerBuilder();
            builder.RegisterInstance(configuration)
                .AsImplementedInterfaces();
            builder.RegisterModule(new LoggerProviderModule());
            builder.RegisterModule<KeyVaultAuthentication>();

            // Register keyvaultclient factory
            builder.Register(context => {
                var tokenSource = context.Resolve<IEnumerable<ITokenSource>>()
                    .FirstOrDefault(s => s.Resource == Http.Resource.KeyVault);

                return new KeyVaultClient(async (_, resource, scope) => {
                    if (tokenSource == null) {
                        return null;
                    }
                    var token = await tokenSource.GetTokenForAsync(
                        (resource + "/" + scope).YieldReturn());
                    return token?.RawToken;
                });
            }).AsSelf().AsImplementedInterfaces();
            _container = builder.Build();
        }

        /// <inheritdoc/>
        public void Dispose() {
            _container.Dispose(); // Disposes keyvault client
        }

        private readonly IContainer _container;
    }
}
