using System;
using System.Collections;
using System.Collections.Generic;

namespace ArrangeAlgorithms.Extension
{
    /// <summary>
    /// Provides extension methods for collections and enumerators.
    /// </summary>
    public static class EnumerableExtension
    {
        /// <summary>
        /// Returns the element from the sequence that yields the maximum value according to the specified key selector.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="selector"/>.</typeparam>
        /// <param name="source">The sequence to return an element from.</param>
        /// <param name="selector">A function to extract the key for each element.</param>
        /// <param name="comparer">An optional <see cref="IComparer{T}"/> to compare keys.</param>
        /// <returns>The element in the sequence with the maximum key value.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> or <paramref name="selector"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the sequence <paramref name="source"/> contains no elements.</exception>
        public static TSource MaxBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector, IComparer<TKey> comparer = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (selector == null) throw new ArgumentNullException(nameof(selector));

            comparer = comparer ?? Comparer<TKey>.Default;
            using IEnumerator<TSource> enumerator = source.GetEnumerator();
            if (!enumerator.MoveNext())
            {
                throw new InvalidOperationException("Empty sequence");
            }

            TSource bestElement = enumerator.Current;
            TKey bestKey = selector(bestElement);
            while (enumerator.MoveNext())
            {
                TSource currentElement = enumerator.Current;
                TKey currentKey = selector(currentElement);
                if (comparer.Compare(currentKey, bestKey) > 0)
                {
                    bestElement = currentElement;
                    bestKey = currentKey;
                }
            }

            return bestElement;
        }

        /// <summary>
        /// Returns the element from the sequence that yields the minimum value according to the specified key selector.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="selector"/>.</typeparam>
        /// <param name="source">The sequence to return an element from.</param>
        /// <param name="selector">A function to extract the key for each element.</param>
        /// <param name="comparer">An optional <see cref="IComparer{T}"/> to compare keys.</param>
        /// <returns>The element in the sequence with the minimum key value.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> or <paramref name="selector"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the sequence <paramref name="source"/> contains no elements.</exception>
        public static TSource MinBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector, IComparer<TKey> comparer = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (selector == null) throw new ArgumentNullException(nameof(selector));

            comparer = comparer ?? Comparer<TKey>.Default;
            using IEnumerator<TSource> enumerator = source.GetEnumerator();
            if (!enumerator.MoveNext())
            {
                throw new InvalidOperationException("Empty sequence");
            }

            TSource bestElement = enumerator.Current;
            TKey bestKey = selector(bestElement);
            while (enumerator.MoveNext())
            {
                TSource currentElement = enumerator.Current;
                TKey currentKey = selector(currentElement);
                if (comparer.Compare(currentKey, bestKey) < 0)
                {
                    bestElement = currentElement;
                    bestKey = currentKey;
                }
            }

            return bestElement;
        }

        /// <summary>
        /// Converts a non-generic <see cref="IEnumerator"/> into a generic <see cref="List{T}"/>, filtering elements that match the target type.
        /// </summary>
        /// <typeparam name="T">The target type to filter and cast the elements to.</typeparam>
        /// <param name="enumerator">The enumerator to extract elements from.</param>
        /// <returns>A new <see cref="List{T}"/> containing the filtered elements of the specified type.</returns>
        public static List<T> ToList<T>(this IEnumerator enumerator)
        {
            if (enumerator == null) throw new ArgumentNullException(nameof(enumerator));

            List<T> list = new List<T>();
            while (enumerator.MoveNext())
            {
                if (enumerator.Current is T t)
                {
                    list.Add(t);
                }
            }
            return list;
        }
    }
}
