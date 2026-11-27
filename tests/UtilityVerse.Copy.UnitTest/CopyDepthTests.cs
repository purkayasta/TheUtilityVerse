// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

namespace UtilityVerse.Copy.UnitTest;

public class CopyDepthTests
{
    [Fact]
    public void Copy_RejectsExcessiveDepthAndClearsPartialGraph()
    {
        var source = Chain(350);
        var context = new CopyContext();

        var error = Assert.Throws<NotSupportedException>(() => context.Copy(source));
        Assert.Contains("depth", error.Message);

        var retainedLink = new Node();
        source.Next = retainedLink;
        var retry = context.Copy(source);

        Assert.NotSame(source, retry);
        Assert.NotSame(retainedLink, Assert.IsType<Node>(retry.Next));
        Assert.Null(retry.Next.Next);
    }

    [Fact]
    public void Copy_PoisonsOperationWhenNestedDepthFailureIsSuppressed()
    {
        var source = Chain(350);
        var context = new CopyContext();

        var error = Assert.Throws<InvalidOperationException>(() => context.CopyGenerated(source, static (node, copyContext) =>
        {
            try
            {
                copyContext.Copy(node);
            }
            catch (NotSupportedException)
            {
                // Simulate a custom copier recovering locally.
            }

            return new Node();
        }));

        Assert.Contains("nested copy failed", error.Message);
    }

    [Fact]
    public void Copy_PreservesCyclesBelowTheDepthLimit()
    {
        var source = Chain(100);
        var current = source;

        while (current.Next != null)
        {
            current = current.Next;
        }

        current.Next = source;

        var copy = new CopyContext().Copy(source);
        current = copy;

        for (var index = 0; index < 100; index++)
        {
            current = Assert.IsType<Node>(current.Next);
        }

        Assert.NotSame(source, copy);
        Assert.Same(copy, current);
    }

    private static Node Chain(int length)
    {
        var root = new Node();
        var current = root;

        for (var index = 1; index < length; index++)
        {
            current.Next = new Node();
            current = current.Next;
        }

        return root;
    }

    private sealed class Node : IDeepCopyable
    {
        internal Node? Next { get; set; }

        object IDeepCopyable.Copy(CopyContext context) => context.CopyGenerated(this, static (source, copyContext) =>
        {
            var copy = new Node();
            copyContext.Register(source, copy);
            copy.Next = copyContext.Copy(source.Next);

            return copy;
        });
    }
}
