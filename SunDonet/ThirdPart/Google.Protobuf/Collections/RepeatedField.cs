﻿#region Copyright notice and license
// Protocol Buffers - Google's data interchange format
// Copyright 2015 Google Inc.  All rights reserved.
// https://developers.google.com/protocol-buffers/
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are
// met:
//
//     * Redistributions of source code must retain the above copyright
// notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above
// copyright notice, this list of conditions and the following disclaimer
// in the documentation and/or other materials provided with the
// distribution.
//     * Neither the name of Google Inc. nor the names of its
// contributors may be used to endorse or promote products derived from
// this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
// OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
// SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
// LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
// THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Google.Protobuf.Collections
{
    /// <summary>
    /// The contents of a repeated field: essentially, a collection with some extra
    /// restrictions (no null values) and capabilities (deep cloning).
    /// </summary>
    /// <remarks>
    /// This implementation does not generally prohibit the use of types which are not
    /// supported by Protocol Buffers but nor does it guarantee that all operations will work in such cases.
    /// </remarks>
    /// <typeparam name="T">The element type of the repeated field.</typeparam>
    [Serializable]
    public sealed class RepeatedField<T> : List<T>, IEquatable<RepeatedField<T>>
    {
        private static readonly EqualityComparer<T> EqualityComparer = ProtobufEqualityComparers.GetEqualityComparer<T>();
        //private static readonly T[] EmptyArray = new T[0];
        //private const int MinArraySize = 8;

        //private T[] array = EmptyArray;
        //private int count = 0;

        /// <summary>
        /// Creates a deep clone of this repeated field.
        /// </summary>
        /// <remarks>
        /// If the field type is
        /// a message type, each element is also cloned; otherwise, it is
        /// assumed that the field type is primitive (including string and
        /// bytes, both of which are immutable) and so a simple copy is
        /// equivalent to a deep clone.
        /// </remarks>
        /// <returns>A deep clone of this repeated field.</returns>
        public RepeatedField<T> Clone()
        {
            RepeatedField<T> clone = new RepeatedField<T>();
            if (Count != 0)
            {
                clone.AddRange(this);
                if (typeof(T) is IDeepCloneable<T>)
                {
                    for (int i = 0; i < Count; i++)
                    {
                        clone[i] = ((IDeepCloneable<T>)this[i]).Clone();
                    }
                }
            }
            return clone;
        }

        /// <summary>
        /// Adds the entries from the given input stream, decoding them with the specified codec.
        /// </summary>
        /// <param name="input">The input stream to read from.</param>
        /// <param name="codec">The codec to use in order to read each entry.</param>
        public void AddEntriesFrom(CodedInputStream input, FieldCodec<T> codec)
        {
            // TODO: Inline some of the Add code, so we can avoid checking the size on every
            // iteration.
            uint tag = input.LastTag;
            var reader = codec.ValueReader;
            // Non-nullable value types can be packed or not.
            if (FieldCodec<T>.IsPackedRepeatedField(tag))
            {
                int length = input.ReadLength();
                if (length > 0)
                {
                    int oldLimit = input.PushLimit(length);
                    while (!input.ReachedLimit)
                    {
                        AddItemWithCheckNull(reader(input));
                    }
                    input.PopLimit(oldLimit);
                }
                // Empty packed field. Odd, but valid - just ignore.
            }
            else
            {
                // Not packed... (possibly not packable)
                do
                {
                    AddItemWithCheckNull(reader(input));
                } while (input.MaybeConsumeTag(tag));
            }
        }

        /// <summary>
        /// Calculates the size of this collection based on the given codec.
        /// </summary>
        /// <param name="codec">The codec to use when encoding each field.</param>
        /// <returns>The number of bytes that would be written to a <see cref="CodedOutputStream"/> by <see cref="WriteTo"/>,
        /// using the same codec.</returns>
        public int CalculateSize(FieldCodec<T> codec)
        {
            if (Count == 0)
            {
                return 0;
            }
            uint tag = codec.Tag;
            if (codec.PackedRepeatedField)
            {
                int dataSize = CalculatePackedDataSize(codec);
                return CodedOutputStream.ComputeRawVarint32Size(tag) +
                    CodedOutputStream.ComputeLengthSize(dataSize) +
                    dataSize;
            }
            else
            {
                var sizeCalculator = codec.ValueSizeCalculator;
                int size = Count * CodedOutputStream.ComputeRawVarint32Size(tag);
                for (int i = 0; i < Count; i++)
                {
                    size += sizeCalculator(this[i]);
                }
                return size;
            }
        }

        private int CalculatePackedDataSize(FieldCodec<T> codec)
        {
            int fixedSize = codec.FixedSize;
            if (fixedSize == 0)
            {
                var calculator = codec.ValueSizeCalculator;
                int tmp = 0;
                for (int i = 0; i < Count; i++)
                {
                    tmp += calculator(this[i]);
                }
                return tmp;
            }
            else
            {
                return fixedSize * Count;
            }
        }

        /// <summary>
        /// Writes the contents of this collection to the given <see cref="CodedOutputStream"/>,
        /// encoding each value using the specified codec.
        /// </summary>
        /// <param name="output">The output stream to write to.</param>
        /// <param name="codec">The codec to use when encoding each value.</param>
        public void WriteTo(CodedOutputStream output, FieldCodec<T> codec)
        {
            if (Count == 0)
            {
                return;
            }
            var writer = codec.ValueWriter;
            var tag = codec.Tag;
            if (codec.PackedRepeatedField)
            {
                // Packed primitive type
                uint size = (uint)CalculatePackedDataSize(codec);
                output.WriteTag(tag);
                output.WriteRawVarint32(size);
                for (int i = 0; i < Count; i++)
                {
                    writer(output, this[i]);
                }
            }
            else
            {
                // Not packed: a simple tag/value pair for each value.
                // Can't use codec.WriteTagAndValue, as that omits default values.
                for (int i = 0; i < Count; i++)
                {
                    output.WriteTag(tag);
                    writer(output, this[i]);
                }
            }
        }

        /// <summary>
        /// Adds the specified item to the collection.
        /// </summary>
        /// <param name="item">The item to add.</param>
        public new void Add(T item)
        {
            AddItemWithCheckNull(item);
        }

        /// <summary>
        /// Adds the specified item to the collection.
        /// </summary>
        /// <param name="item">The item to add.</param>
        private void AddItemWithCheckNull(T item)
        {
            var returnItem = ProtoPreconditions.CheckNotNullUnconstrained(item, nameof(item));
            base.Add(returnItem);
        }

        /// <summary>
        /// Adds all of the specified values into this collection.
        /// </summary>
        /// <param name="values">The values to add to this collection.</param>
        public new void AddRange(IEnumerable<T> values)
        {
            AddRangeItemsWithCheckNull(values);
        }

        /// <summary>
        /// Adds all of the specified values into this collection.
        /// </summary>
        /// <param name="values">The values to add to this collection.</param>
        private void AddRangeItemsWithCheckNull(IEnumerable<T> values)
        {
            ProtoPreconditions.CheckNotNull(values, nameof(values));

            // Optimization 1: If the collection we're adding is already a RepeatedField<T>,
            // we know the values are valid.
            var otherRepeatedField = values as RepeatedField<T>;
            if (otherRepeatedField != null)
            {
                // make sure capacity of list is enough
                var requiredCapacity = Math.Max(Count + otherRepeatedField.Count, Capacity);
                if (Capacity < requiredCapacity)
                {
                    Capacity = requiredCapacity;
                }

                // combine list elements
                base.AddRange(otherRepeatedField);
                
                return;
            }

            // Optimization 2: The collection is an ICollection, so we can expand
            // just once and ask the collection to copy itself into the array.
            var collection = values as ICollection;
            if (collection != null)
            {
                var extraCount = collection.Count;
                // For reference types and nullable value types, we need to check that there are no nulls
                // present. (This isn't a thread-safe approach, but we don't advertise this is thread-safe.)
                // We expect the JITter to optimize this test to true/false, so it's effectively conditional
                // specialization.
                if (default(T) == null)
                {                   
                    // TODO: Measure whether iterating once to check and then letting the collection copy
                    // itself is faster or slower than iterating and adding as we go. For large
                    // collections this will not be great in terms of cache usage... but the optimized
                    // copy may be significantly faster than doing it one at a time.
                    foreach (var item in collection)
                    {
                        if (item == null)
                        {
                            throw new ArgumentException("Sequence contained null element", nameof(values));
                        }
                    }
                }
                // make sure capacity of list is enough
                var requiredCapacity = Math.Max(Count + collection.Count, Capacity);
                if (Capacity < requiredCapacity)
                {
                    Capacity = requiredCapacity;
                }

                base.AddRange(values);
                return;
            }

            // We *could* check for ICollection<T> as well, but very very few collections implement
            // ICollection<T> but not ICollection. (HashSet<T> does, for one...)

            // Fall back to a slower path of adding items one at a time.
            foreach (T item in values)
            {
                AddItemWithCheckNull(item);
            }
        }

        /// <summary>
        /// Adds all of the specified values into this collection. This method is present to
        /// allow repeated fields to be constructed from queries within collection initializers.
        /// Within non-collection-initializer code, consider using the equivalent <see cref="AddRangeItemsWithCheckNull"/>
        /// method instead for clarity.
        /// </summary>
        /// <param name="values">The values to add to this collection.</param>
        public void Add(IEnumerable<T> values)
        {
            AddRangeItemsWithCheckNull(values);
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as RepeatedField<T>);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            int hash = 0;
            for (int i = 0; i < Count; i++)
            {
                hash = hash * 31 + this[i].GetHashCode();
            }
            return hash;
        }

        /// <summary>
        /// Compares this repeated field with another for equality.
        /// </summary>
        /// <param name="other">The repeated field to compare this with.</param>
        /// <returns><c>true</c> if <paramref name="other"/> refers to an equal repeated field; <c>false</c> otherwise.</returns>
        public bool Equals(RepeatedField<T> other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }
            if (ReferenceEquals(other, this))
            {
                return true;
            }
            if (other.Count != this.Count)
            {
                return false;
            }
            EqualityComparer<T> comparer = EqualityComparer;
            for (int i = 0; i < Count; i++)
            {
                if (!comparer.Equals(this[i], other[i]))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Returns the index of the given item within the collection, or -1 if the item is not
        /// present.
        /// </summary>
        /// <param name="item">The item to find in the collection.</param>
        /// <returns>The zero-based index of the item, or -1 if it is not found.</returns>
        public new int IndexOf(T item)
        {
            ProtoPreconditions.CheckNotNullUnconstrained(item, nameof(item));
            EqualityComparer<T> comparer = EqualityComparer;
            for (int i = 0; i < Count; i++)
            {
                if (comparer.Equals(this[i], item))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Inserts the given item at the specified index.
        /// </summary>
        /// <param name="index">The index at which to insert the item.</param>
        /// <param name="item">The item to insert.</param>
        public new void Insert(int index, T item)
        {
            ProtoPreconditions.CheckNotNullUnconstrained(item, nameof(item));
            if (index < 0 || index > Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            base.Insert(index, item);
        }

        /// <summary>
        /// Gets or sets the item at the specified index.
        /// </summary>
        /// <value>
        /// The element at the specified index.
        /// </value>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        /// <returns>The item at the specified index.</returns>
        public new T this[int index]
        {
            get
            {
                return base[index];
            }
            set
            {
                ProtoPreconditions.CheckNotNullUnconstrained(value, nameof(value));
                base[index] = value;
            }
        }

        /// <summary>
        /// Returns a string representation of this repeated field, in the same
        /// way as it would be represented by the default JSON formatter.
        /// </summary>
        public override string ToString()
        {
            var writer = new StringWriter();
            JsonFormatter.Default.WriteList(writer, this);
            return writer.ToString();
        }      
    }
}
