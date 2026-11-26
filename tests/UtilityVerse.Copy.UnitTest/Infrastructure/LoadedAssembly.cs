// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using System.Collections;
using System.Reflection;
using System.Runtime.Loader;

namespace UtilityVerse.Copy.UnitTest.Infrastructure;

internal sealed class LoadedAssembly : IDisposable
{
    private readonly AssemblyLoadContext _context;
    private readonly Assembly _assembly;

    private LoadedAssembly(AssemblyLoadContext context, Assembly assembly)
    {
        _context = context;
        _assembly = assembly;
    }

    public static LoadedAssembly From(Stream stream)
    {
        var context = new AssemblyLoadContext(name: null, isCollectible: true);
        return new LoadedAssembly(context, context.LoadFromStream(stream));
    }

    public Type GetType(string fullName) =>
        _assembly.GetType(fullName, throwOnError: true)!;

    public dynamic Create(string fullName, params object?[] ctorArgs) =>
        Activator.CreateInstance(GetType(fullName), ctorArgs)!;

    public dynamic CreateList(string elementFullName, params object[] items)
    {
        var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(GetType(elementFullName)))!;
        foreach (var item in items)
            list.Add(item);
        return list;
    }

    public void Dispose() => _context.Unload();
}
