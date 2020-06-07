// Copyright (c) Winton. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Collections.Generic;

namespace Winton.Extensions.Configuration.Consul.Parsers
{
    /// <summary>
    ///     Allows clients to rewrite configuration keys as they are parsed.
    /// </summary>
    public interface IConfigurationKeyValueRewriter
    {
        /// <summary>
        ///     Rewrites a configuration KeyValuePair.
        /// </summary>
        /// <param name="pair">The source <see cref="KeyValuePair{TKey, TValue}"/> pair read from Consul.</param>
        /// <returns>The rewritten <see cref="KeyValuePair{TKey, TValue}"/>.</returns>
        KeyValuePair<string, string> Rewrite(KeyValuePair<string, string> pair);
    }
}