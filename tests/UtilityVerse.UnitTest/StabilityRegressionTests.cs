// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using System.Text;
using UtilityVerse.Contracts;
using UtilityVerse.Shared;

namespace UtilityVerse.UnitTest;

public class StabilityRegressionTests
{
    [Fact]
    public void AddRetry_ReturnsImmediately_WhenHandlerSucceeds()
    {
        var calls = 0;
        var options = new RetryOption { Count = 3, Delay = TimeSpan.Zero };
        Assert.Equal(42, Utility.AddRetry(() => ++calls == 1 ? 42 : throw new InvalidOperationException(), options));
        Assert.Equal(1, calls);
        Assert.Equal(3, options.Count);
    }

    [Fact]
    public void AddRetry_PropagatesFinalException_WhenAttemptsAreExhausted()
    {
        var calls = 0;
        var failure = new InvalidOperationException("final");
        var options = new RetryOption { Count = 3, Delay = TimeSpan.Zero };
        Assert.Same(failure, Assert.Throws<InvalidOperationException>(() => Utility.AddRetry<int>(() => { calls++; throw failure; }, options)));
        Assert.Equal(3, calls);
        Assert.Equal(3, options.Count);
    }

    [Fact]
    public async Task AddRetryAsync_WaitsBetweenAttempts_WhenHandlerFails()
    {
        var calls = 0;
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        var pending = Utility.AddRetryAsync(() => ++calls == 1 ? throw new InvalidOperationException() : 42,
            new RetryOption { Count = 2, Delay = TimeSpan.FromMilliseconds(100) });
        Assert.Equal(42, await pending);
        Assert.True(elapsed.Elapsed >= TimeSpan.FromMilliseconds(80));
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task AddRetryTaskAsync_RetriesAwaitedFailures_WhenHandlerIsAsynchronous()
    {
        var calls = 0;
        Assert.Equal(42, await Utility.AddRetryTaskAsync(async () =>
        {
            await Task.Yield();
            if (++calls < 3) throw new InvalidOperationException();
            return 42;
        }, new RetryOption { Count = 3, Delay = TimeSpan.Zero }));
        Assert.Equal(3, calls);
    }

    [Fact]
    public void AddRetry_RejectsInvalidOptionsAndHandler_WhenProvided()
    {
        Assert.Throws<ArgumentNullException>(() => Utility.AddRetry<int>(null));
        Assert.Throws<ArgumentOutOfRangeException>(() => Utility.AddRetry(() => 1, new RetryOption { Count = 0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => Utility.AddRetry(() => 1, new RetryOption { Delay = TimeSpan.FromMilliseconds(-1) }));
    }

    [Fact]
    public void ToStr_ReturnsSuccess_WhenArraysAreValid()
    {
        AssertSuccess(new[] { 1, 2 }.ToStr(","), "1,2");
        AssertSuccess(new[] { 'a', 'b' }.ToStr(","), "a,b");
        AssertSuccess(new[] { 1m, 2m }.ToStr(","), "1,2");
        AssertSuccess(new[] { 1d, 2d }.ToStr(","), "1,2");
        Assert.True(Array.Empty<decimal>().ToStr().HasError);
        Assert.True(new UtilityVerseResult<string>("legacy error").HasError);
        AssertSuccess(UtilityVerseResult<string>.Success("value"), "value");
        Assert.True(UtilityVerseResult<string>.Failure("error").HasError);
    }

    [Fact]
    public void ToPagedList_ReturnsExpectedPages_WhenIndexIsZeroBased()
    {
        var source = Enumerable.Range(1, 25).AsQueryable();
        Assert.Equal(Enumerable.Range(1, 10), source.ToPagedList(0, 10).Results);
        Assert.Equal(Enumerable.Range(11, 10), source.ToPagedList(1, 10).Results);
        Assert.Equal(Enumerable.Range(21, 5), source.ToPagedList(2, 10).Results);
        Assert.Empty(source.ToPagedList(3, 10).Results);
        Assert.True(source.ToPagedList(0, 10).IsValid);
        Assert.False(source.ToPagedList(-1, 10).IsValid);
        Assert.False(source.ToPagedList(0, 0).IsValid);
        Assert.False(source.ToPagedList(int.MaxValue, 10).IsValid);
        Assert.False(((IQueryable<int>)null).ToPagedList(0, 10).IsValid);
    }

    public struct Payload
    {
        public int Number { get; set; }
        public int Field;
        public string Name { get; set; }
    }

    [Fact]
    public void ByteConversion_UsesJsonForStructs_WhenRoundTripping()
    {
        var original = new Payload { Number = 42, Field = 5, Name = "safe" };
        var bytes = original.ToByteArray();
        Assert.False(bytes.HasError);
        Assert.StartsWith("{", Encoding.UTF8.GetString(bytes.Result));
        var copy = bytes.Result.To<Payload>();
        Assert.False(copy.HasError);
        Assert.Equal(original.Number, copy.Result.Number);
        Assert.Equal(original.Field, copy.Result.Field);
        Assert.Equal(original.Name, copy.Result.Name);
        Assert.True(new byte[] { 1 }.To<Payload>().HasError);
        Assert.True(Array.Empty<byte>().To<Payload>().HasError);
        Assert.True(((byte[])null).To<Payload>().HasError);
        Assert.False(((object)original).ToByteArray().HasError);
    }

    private static void AssertSuccess(UtilityVerseResult<string> result, string expected)
    {
        Assert.False(result.HasError);
        Assert.Equal(expected, result.Result);
    }
}
