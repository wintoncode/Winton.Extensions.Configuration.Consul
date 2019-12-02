// Copyright (c) Winton. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Consul;

namespace Winton.Extensions.Configuration.Consul
{
    /// <summary>A factory responsible for creating <see cref="IConsulClient" /> objects.</summary>
    internal interface IConsulClientFactory
    {
        /// <summary>Creates a new instance of an <see cref="IConsulClient" />.</summary>
        /// <returns>A new <see cref="IConsulClient" />.</returns>
        IConsulClient Create();
    }
}