﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Simplification;

internal interface ISimplification
{
    SimplifierOptions DefaultOptions { get; }
    SimplifierOptions GetSimplifierOptions(AnalyzerConfigOptions options, SimplifierOptions? fallbackOptions);
}
