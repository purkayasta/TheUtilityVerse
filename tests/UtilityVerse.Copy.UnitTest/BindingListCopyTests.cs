// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using System.ComponentModel;

namespace UtilityVerse.Copy.UnitTest;

public class BindingListCopyTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void CopySequence_PreservesBindingListSettings(bool covariant, bool populated)
    {
        var source = new BindingList<string>
        {
            AllowEdit = false,
            AllowRemove = false,
            AllowNew = true,
            RaiseListChangedEvents = false
        };

        if (populated)
        {
            source.Add("item");
        }

        var context = new CopyContext();
        var copy = covariant
            ? Assert.IsType<BindingList<string>>(context.CopySequence<object>(source, static (item, _) => item))
            : Assert.IsType<BindingList<string>>(context.CopySequence(source, static (item, _) => item));

        Assert.NotSame(source, copy);
        Assert.Equal(source.ToArray(), copy.ToArray());
        Assert.False(copy.AllowEdit);
        Assert.False(copy.AllowRemove);
        Assert.True(copy.AllowNew);
        Assert.False(copy.RaiseListChangedEvents);
    }

    [Fact]
    public void CopySequence_PreservesExplicitAllowNewFalse()
    {
        var source = new BindingList<Version> { AllowNew = false };
        var copy = Assert.IsType<BindingList<Version>>(new CopyContext().CopySequence(source, static (item, _) => item));

        Assert.False(copy.AllowNew);
    }

    [Fact]
    public void CopySequence_SnapshotsAllowNewAndExcludesSourceSubscribers()
    {
        var source = new BindingList<string>();
        var addingNotifications = 0;
        var listNotifications = 0;

        source.AddingNew += (_, args) =>
        {
            addingNotifications++;
            args.NewObject = "source handler";
        };
        source.ListChanged += (_, _) => listNotifications++;

        var copy = Assert.IsType<BindingList<string>>(new CopyContext().CopySequence(source, static (item, _) => item));
        var copyNotifications = 0;
        copy.ListChanged += (_, _) => copyNotifications++;

        Assert.True(copy.AllowNew);
        copy.Add("added");
        Assert.Throws<MissingMethodException>(() => copy.AddNew());
        Assert.Equal(1, copyNotifications);
        Assert.Equal(0, addingNotifications);
        Assert.Equal(0, listNotifications);
    }

    [Fact]
    public void CopySequence_PreservesDefaultBindingListSettings()
    {
        var source = new BindingList<int> { 42 };
        var copy = Assert.IsType<BindingList<int>>(new CopyContext().CopySequence(source, static (item, _) => item));

        Assert.True(copy.AllowEdit);
        Assert.True(copy.AllowRemove);
        Assert.True(copy.AllowNew);
        Assert.True(copy.RaiseListChangedEvents);
        Assert.Equal(42, Assert.Single(copy));
    }
}
