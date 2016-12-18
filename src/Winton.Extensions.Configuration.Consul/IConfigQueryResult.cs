// Copyright (c) Winton. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENCE.md in the project root for license information.

namespace Winton.Extensions.Configuration.Consul
{
    /// <summary>The result of a query for config in Consul.</summary>
    internal interface IConfigQueryResult
    {
        /// <summary>Gets a value indicating whether the config exists.</summary>
        bool Exists { get; }

        /// <summary>Gets the raw value of the config.</summary>
        byte[] Value { get; }
    }
}