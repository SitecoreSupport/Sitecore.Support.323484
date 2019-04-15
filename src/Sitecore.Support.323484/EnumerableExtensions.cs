    namespace Sitecore.Support.Publishing.WebDeploy
    {
        using System;
        using System.Collections.Generic;
        using System.Runtime.CompilerServices;

        internal static class EnumerableExtensions
        {
            public static void Apply<T>(this IEnumerable<T> sequence, Action<T> action)
            {
                foreach (T local in sequence)
                {
                    action(local);
                }
            }
        }
    }
