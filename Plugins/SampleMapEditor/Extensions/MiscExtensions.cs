using OpenTK;
using System.Collections.Generic;

namespace RedStarLibrary.Extensions
{
    internal static class MiscExtensions
    {
        public static Dictionary<string, float> ToDict(this Vector3 vector) => new Dictionary<string, float>() { {"X", vector.X }, { "Y", vector.Y }, { "Z", vector.Z } };
    }
}
