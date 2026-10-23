using System.Reflection;

namespace Lingual.Modules.Identity;

public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}