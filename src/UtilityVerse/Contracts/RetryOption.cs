﻿// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using System;

namespace UtilityVerse.Contracts;

/// <summary>
/// Retry entity
/// </summary>
public class RetryOption
{

    /// <summary>
    /// When it will retry again.
    /// </summary>
    public TimeSpan Delay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Retry Count. How many time it will try?
    /// </summary>
    public int Count { get; set; } = 1;
}
