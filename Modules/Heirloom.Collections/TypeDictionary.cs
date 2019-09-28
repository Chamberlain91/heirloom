﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Heirloom.Collections
{
    /// <summary>
    /// Manages objects by their type hierarchy up to the base type, allowing access by enumeration of objects by type.
    /// </summary>
    /// <typeparam name="T">Some base type</typeparam>
    public sealed class TypeDictionary<T> : ITypeDictionary<T>
    {
        private readonly Dictionary<Type, List<T>> _typeLists;
        private readonly HashSet<T> _items;

        public TypeDictionary()
        {
            _typeLists = new Dictionary<Type, List<T>>();
            _items = new HashSet<T>();
        }

        public int Count => _items.Count;

        #region Add, Remove

        public void Add(T item)
        {
            if (item == null) { throw new ArgumentNullException(nameof(item)); }

            // Try to add item to bucket, was it already present?
            if (_items.Add(item))
            {
                // Was not present, add association with each ancestor type
                foreach (var type in GetAncestorTypes(item.GetType()))
                {
                    var list = GetListByType(type);
                    list.Add(item);
                }
            }
        }

        public bool Remove(T item)
        {
            // Try to remove item from bucket
            if (_items.Remove(item))
            {
                // Was removed, remove associate from each ancestor type
                foreach (var type in GetAncestorTypes(item.GetType()))
                {
                    var list = GetListByType(type);
                    list.Remove(item);
                }

                return true;
            }
            else
            {
                // Couldn't remove, never contained it.
                return false;
            }
        }

        #endregion

        #region Contains, ContainsType and Enumerate

        public bool Contains(T item)
        {
            return _items.Contains(item);
        }

        public bool ContainsType<X>() where X : T
        {
            return _typeLists.ContainsKey(typeof(X));
        }

        public IEnumerable<X> GetItemsByType<X>() where X : T
        {
            if (ContainsType<X>())
            {
                foreach (var x in GetListByType(typeof(X)))
                {
                    yield return (X) x; // todo: avoid cast...?
                }
            }
        }

        #endregion

        #region Helpers

        private List<T> GetListByType(Type type)
        {
            // Create type list if not already present
            if (!_typeLists.ContainsKey(type))
            {
                // TODO: Actually create polymorphic List<X>
                _typeLists[type] = new List<T>();
            }

            // Return appropriate type list
            return _typeLists[type];
        }

        private static IEnumerable<Type> GetAncestorTypes(Type type)
        {
            while (type != typeof(T))
            {
                // Emits each anscestor type
                yield return type;
                type = type.BaseType;
            }

            // Emits the root type
            yield return typeof(T);
        }

        #endregion

        #region IEnumerable<T>

        public IEnumerator<T> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}
