using System;
using System.Collections.Generic;

namespace ReSharperPlugin.BSMT_Rider.utils
{
    public struct TupleStruct<T1, T2> : IEquatable<TupleStruct<T1, T2>>
    {
        public readonly T1 Item1;
        public readonly T2 Item2;

        public TupleStruct(T1 item1, T2 item2)
        {
            Item1 = item1;
            Item2 = item2;
        }

        public bool Equals(TupleStruct<T1, T2> other)
        {
            return EqualityComparer<T1>.Default.Equals(Item1, other.Item1) && EqualityComparer<T2>.Default.Equals(Item2, other.Item2);
        }

        private sealed class T1T2EqualityComparer : IEqualityComparer<TupleStruct<T1, T2>>
        {
            public bool Equals(TupleStruct<T1, T2> x, TupleStruct<T1, T2> y)
            {
                return EqualityComparer<T1>.Default.Equals(x.Item1, y.Item1) && EqualityComparer<T2>.Default.Equals(x.Item2, y.Item2);
            }

            public int GetHashCode(TupleStruct<T1, T2> obj)
            {
                unchecked
                {
                    return (EqualityComparer<T1>.Default.GetHashCode(obj.Item1) * 397) ^ EqualityComparer<T2>.Default.GetHashCode(obj.Item2);
                }
            }
        }

        public static IEqualityComparer<TupleStruct<T1, T2>> T1T2Comparer { get; } = new T1T2EqualityComparer();

        public override bool Equals(object obj)
        {
            return obj is TupleStruct<T1, T2> other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (EqualityComparer<T1>.Default.GetHashCode(Item1) * 397) ^ EqualityComparer<T2>.Default.GetHashCode(Item2);
            }
        }

        public static bool operator ==(TupleStruct<T1, T2> left, TupleStruct<T1, T2> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TupleStruct<T1, T2> left, TupleStruct<T1, T2> right)
        {
            return !left.Equals(right);
        }

        public TupleStruct<TF1, TF2> Cast<TF1, TF2>() where TF1: T1 where TF2 : T2
        {
            return new((TF1) Item1, (TF2) Item2);
        }
    }
}