[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Scellecs.Collections.TestSuite")]

namespace Scellecs.Collections {
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using Unity.IL2CPP.CompilerServices;

    [Serializable]
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.DivideByZeroChecks, false)]
    public sealed class IntHashSet : IEnumerable<int> {
        public int length;
        public int capacity;
        public int capacityMinusOne;
        public int lastIndex;
        public int freeIndex;

        public int[] buckets;
        public int[] slots;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IntHashSet() : this(0) {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IntHashSet(int capacity) {
            this.lastIndex = 0;
            this.length    = 0;
            this.freeIndex = -1;

            this.capacityMinusOne = HashHelpers.GetCapacity(capacity);
            this.capacity         = this.capacityMinusOne + 1;
            this.buckets          = new int[this.capacity];
            this.slots            = new int[this.capacity / 2];
        }

        IEnumerator<int> IEnumerable<int>.GetEnumerator() => this.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator() {
            Enumerator e;
            e.set     = this;
            e.index   = 0;
            e.current = default;
            return e;
        }

        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        [Il2CppSetOption(Option.DivideByZeroChecks, false)]
        public unsafe struct Enumerator : IEnumerator<int> {
            public IntHashSet set;

            public int index;
            public int current;

            public bool MoveNext() {
                fixed (int* slotsPtr = &this.set.slots[0]) {
                    for (var len = this.set.lastIndex; this.index < len; ++this.index) {
                        var v = *slotsPtr - 1;
                        if (v < 0) {
                            continue;
                        }

                        this.current = v;
                        ++this.index;

                        return true;
                    }

                    this.index   = this.set.lastIndex + 1;
                    this.current = default;
                    return false;
                }
            }

            public int Current => this.current;

            object IEnumerator.Current => this.current;

            void IEnumerator.Reset() {
                this.index   = 0;
                this.current = default;
            }

            public void Dispose() {
            }
        }
    }

    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.DivideByZeroChecks, false)]
    public static unsafe class IntHashSetExtensions {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Add(this IntHashSet hashSet, in int value) {
            var rem = value & hashSet.capacityMinusOne;

            fixed (int* slotsPtr = &hashSet.slots[0])
            fixed (int* bucketsPtr = &hashSet.buckets[0]) {
                int* slot;
                for (var i = *(bucketsPtr + rem) - 1; i >= 0; i = *(slot + 1)) {
                    slot = slotsPtr + i;
                    if (*slot - 1 == value) {
                        return false;
                    }
                }
            }

            int newIndex;
            if (hashSet.freeIndex >= 0) {
                newIndex = hashSet.freeIndex;
                fixed (int* s = &hashSet.slots[0]) {
                    hashSet.freeIndex = *(s + newIndex + 1);
                }
            }
            else {
                if (hashSet.lastIndex == hashSet.capacity * 2) {
                    var newCapacityMinusOne = HashHelpers.ExpandCapacity(hashSet.length);
                    var newCapacity         = newCapacityMinusOne + 1;

                    ArrayHelpers.Grow(ref hashSet.slots, newCapacity * 2);

                    var newBuckets = new int[newCapacity];

                    fixed (int* slotsPtr = &hashSet.slots[0])
                    fixed (int* bucketsPtr = &newBuckets[0]) {
                        for (int i = 0, len = hashSet.lastIndex; i < len; i += 2) {
                            var slotPtr = slotsPtr + i;

                            var newResizeIndex   = (*slotPtr - 1) & newCapacityMinusOne;
                            var newCurrentBucket = bucketsPtr + newResizeIndex;

                            *(slotPtr + 1) = *newCurrentBucket - 1;

                            *newCurrentBucket = i + 1;
                        }
                    }

                    hashSet.buckets          = newBuckets;
                    hashSet.capacityMinusOne = newCapacityMinusOne;
                    hashSet.capacity         = newCapacity;

                    rem = value & newCapacityMinusOne;
                }

                newIndex          =  hashSet.lastIndex;
                hashSet.lastIndex += 2;
            }

            fixed (int* slotsPtr = &hashSet.slots[0])
            fixed (int* bucketsPtr = &hashSet.buckets[0]) {
                var bucket = bucketsPtr + rem;
                var slot   = slotsPtr + newIndex;

                *slot       = value + 1;
                *(slot + 1) = *bucket - 1;

                *bucket = newIndex + 1;
            }

            ++hashSet.length;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Remove(this IntHashSet hashSet, in int value) {
            fixed (int* slotsPtr = &hashSet.slots[0])
            fixed (int* bucketsPtr = &hashSet.buckets[0]) {
                var rem = value & hashSet.capacityMinusOne;

                int next;
                var num = -1;

                for (var i = *(bucketsPtr + rem) - 1; i >= 0; i = next) {
                    var slot     = slotsPtr + i;
                    var slotNext = slot + 1;

                    if (*slot - 1 == value) {
                        if (num < 0) {
                            *(bucketsPtr + rem) = *slotNext + 1;
                        }
                        else {
                            *(slotsPtr + num + 1) = *slotNext;
                        }

                        *slot     = -1;
                        *slotNext = hashSet.freeIndex;

                        if (--hashSet.length == 0) {
                            hashSet.lastIndex = 0;
                            hashSet.freeIndex = -1;
                        }
                        else {
                            hashSet.freeIndex = i;
                        }

                        return true;
                    }

                    next = *slotNext;
                    num  = i;
                }

                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CopyTo(this IntHashSet hashSet, int[] array) {
            fixed (int* slotsPtr = &hashSet.slots[0]) {
                var num = 0;
                for (int i = 0, li = hashSet.lastIndex, len = hashSet.length; i < li && num < len; ++i) {
                    var v = *(slotsPtr + i) - 1;
                    if (v < 0) {
                        continue;
                    }

                    array[num] = v;
                    ++num;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Has(this IntHashSet hashSet, in int key) {
            fixed (int* slotsPtr = &hashSet.slots[0])
            fixed (int* bucketsPtr = &hashSet.buckets[0]) {
                var rem = key & hashSet.capacityMinusOne;

                int next;
                for (var i = *(bucketsPtr + rem) - 1; i >= 0; i = next) {
                    var slot = slotsPtr + i;
                    if (*slot - 1 == key) {
                        return true;
                    }

                    next = *(slot + 1);
                }

                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Clear(this IntHashSet hashSet) {
            if (hashSet.lastIndex <= 0) {
                return;
            }

            Array.Clear(hashSet.slots, 0, hashSet.lastIndex);
            Array.Clear(hashSet.buckets, 0, hashSet.capacityMinusOne);
            hashSet.lastIndex = 0;
            hashSet.length    = 0;
            hashSet.freeIndex = -1;
        }
    }

    [Serializable]
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.DivideByZeroChecks, false)]
    public sealed class IntHashMap<T> : IEnumerable<int> {
        public int length;
        public int capacity;
        public int capacityMinusOne;
        public int lastIndex;
        public int freeIndex;

        public int[] buckets;

        public T[]    data;
        public Slot[] slots;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IntHashMap(in int capacity = 0) {
            this.lastIndex = 0;
            this.length    = 0;
            this.freeIndex = -1;

            this.capacityMinusOne = HashHelpers.GetCapacity(capacity);
            this.capacity         = this.capacityMinusOne + 1;

            this.buckets = new int[this.capacity];
            this.slots   = new Slot[this.capacity];
            this.data    = new T[this.capacity];
        }

        IEnumerator<int> IEnumerable<int>.GetEnumerator() => this.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator() {
            Enumerator e;
            e.hashMap = this;
            e.index   = 0;
            e.current = default;
            return e;
        }

        [Serializable]
        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        [Il2CppSetOption(Option.DivideByZeroChecks, false)]
        public struct Slot {
            public int key;
            public int next;
        }

        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        [Il2CppSetOption(Option.DivideByZeroChecks, false)]
        public struct Enumerator : IEnumerator<int> {
            public IntHashMap<T> hashMap;

            public int index;
            public int current;

            public bool MoveNext() {
                for (; this.index < this.hashMap.lastIndex; ++this.index) {
                    ref var slot = ref this.hashMap.slots[this.index];
                    if (slot.key - 1 < 0) {
                        continue;
                    }

                    this.current = this.index;
                    ++this.index;

                    return true;
                }

                this.index   = this.hashMap.lastIndex + 1;
                this.current = default;
                return false;
            }

            public int Current => this.current;

            object IEnumerator.Current => this.current;

            void IEnumerator.Reset() {
                this.index   = 0;
                this.current = default;
            }

            public void Dispose() {
            }
        }
    }

    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.DivideByZeroChecks, false)]
    public static class IntHashMapExtensions {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Add<T>(this IntHashMap<T> hashMap, in int key, in T value, out int slotIndex) {
            var rem = key & hashMap.capacityMinusOne;

            for (var i = hashMap.buckets[rem] - 1; i >= 0; i = hashMap.slots[i].next) {
                if (hashMap.slots[i].key - 1 == key) {
                    slotIndex = -1;
                    return false;
                }
            }

            if (hashMap.freeIndex >= 0) {
                slotIndex         = hashMap.freeIndex;
                hashMap.freeIndex = hashMap.slots[slotIndex].next;
            }
            else {
                if (hashMap.lastIndex == hashMap.capacity) {
                    var newCapacityMinusOne = HashHelpers.ExpandCapacity(hashMap.length);
                    var newCapacity         = newCapacityMinusOne + 1;

                    ArrayHelpers.Grow(ref hashMap.slots, newCapacity);
                    ArrayHelpers.Grow(ref hashMap.data, newCapacity);

                    var newBuckets = new int[newCapacity];

                    for (int i = 0, len = hashMap.lastIndex; i < len; ++i) {
                        ref var slot = ref hashMap.slots[i];

                        var newResizeIndex = (slot.key - 1) & newCapacityMinusOne;
                        slot.next = newBuckets[newResizeIndex] - 1;

                        newBuckets[newResizeIndex] = i + 1;
                    }

                    hashMap.buckets          = newBuckets;
                    hashMap.capacity         = newCapacity;
                    hashMap.capacityMinusOne = newCapacityMinusOne;

                    rem = key & hashMap.capacityMinusOne;
                }

                slotIndex = hashMap.lastIndex;
                ++hashMap.lastIndex;
            }

            ref var newSlot = ref hashMap.slots[slotIndex];

            newSlot.key  = key + 1;
            newSlot.next = hashMap.buckets[rem] - 1;

            hashMap.data[slotIndex] = value;

            hashMap.buckets[rem] = slotIndex + 1;

            ++hashMap.length;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Set<T>(this IntHashMap<T> hashMap, in int key, in T value, out int slotIndex) {
            var rem = key & hashMap.capacityMinusOne;

            for (var i = hashMap.buckets[rem] - 1; i >= 0; i = hashMap.slots[i].next) {
                if (hashMap.slots[i].key - 1 == key) {
                    hashMap.data[i] = value;
                    slotIndex       = i;
                    return;
                }
            }

            if (hashMap.freeIndex >= 0) {
                slotIndex         = hashMap.freeIndex;
                hashMap.freeIndex = hashMap.slots[slotIndex].next;
            }
            else {
                if (hashMap.lastIndex == hashMap.capacity) {
                    var newCapacityMinusOne = HashHelpers.ExpandCapacity(hashMap.length);
                    var newCapacity         = newCapacityMinusOne + 1;

                    ArrayHelpers.Grow(ref hashMap.slots, newCapacity);
                    ArrayHelpers.Grow(ref hashMap.data, newCapacity);

                    var newBuckets = new int[newCapacity];

                    for (int i = 0, len = hashMap.lastIndex; i < len; ++i) {
                        ref var slot           = ref hashMap.slots[i];
                        var     newResizeIndex = (slot.key - 1) & newCapacityMinusOne;
                        slot.next = newBuckets[newResizeIndex] - 1;

                        newBuckets[newResizeIndex] = i + 1;
                    }

                    hashMap.buckets          = newBuckets;
                    hashMap.capacity         = newCapacity;
                    hashMap.capacityMinusOne = newCapacityMinusOne;

                    rem = key & hashMap.capacityMinusOne;
                }

                slotIndex = hashMap.lastIndex;
                ++hashMap.lastIndex;
            }

            ref var newSlot = ref hashMap.slots[slotIndex];

            newSlot.key  = key + 1;
            newSlot.next = hashMap.buckets[rem] - 1;

            hashMap.data[slotIndex] = value;

            hashMap.buckets[rem] = slotIndex + 1;

            ++hashMap.length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Remove<T>(this IntHashMap<T> hashMap, in int key, out T lastValue) {
            var rem = key & hashMap.capacityMinusOne;

            int next;
            var num = -1;
            for (var i = hashMap.buckets[rem] - 1; i >= 0; i = next) {
                ref var slot = ref hashMap.slots[i];
                if (slot.key - 1 == key) {
                    if (num < 0) {
                        hashMap.buckets[rem] = slot.next + 1;
                    }
                    else {
                        hashMap.slots[num].next = slot.next;
                    }

                    lastValue = hashMap.data[i];

                    slot.key  = -1;
                    slot.next = hashMap.freeIndex;

                    --hashMap.length;
                    if (hashMap.length == 0) {
                        hashMap.lastIndex = 0;
                        hashMap.freeIndex = -1;
                    }
                    else {
                        hashMap.freeIndex = i;
                    }

                    return true;
                }

                next = slot.next;
                num  = i;
            }

            lastValue = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Has<T>(this IntHashMap<T> hashMap, in int key) {
            var rem = key & hashMap.capacityMinusOne;

            int next;
            for (var i = hashMap.buckets[rem] - 1; i >= 0; i = next) {
                ref var slot = ref hashMap.slots[i];
                if (slot.key - 1 == key) {
                    return true;
                }

                next = slot.next;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetValue<T>(this IntHashMap<T> hashMap, in int key, out T value) {
            var rem = key & hashMap.capacityMinusOne;

            int next;
            for (var i = hashMap.buckets[rem] - 1; i >= 0; i = next) {
                ref var slot = ref hashMap.slots[i];
                if (slot.key - 1 == key) {
                    value = hashMap.data[i];
                    return true;
                }

                next = slot.next;
            }

            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetValueByKey<T>(this IntHashMap<T> hashMap, in int key) {
            var rem = key & hashMap.capacityMinusOne;

            int next;
            for (var i = hashMap.buckets[rem] - 1; i >= 0; i = next) {
                ref var slot = ref hashMap.slots[i];
                if (slot.key - 1 == key) {
                    return hashMap.data[i];
                }

                next = slot.next;
            }

            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetValueByIndex<T>(this IntHashMap<T> hashMap, in int index) => hashMap.data[index];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetKeyByIndex<T>(this IntHashMap<T> hashMap, in int index) => hashMap.slots[index].key;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int TryGetIndex<T>(this IntHashMap<T> hashMap, in int key) {
            var rem = key & hashMap.capacityMinusOne;

            int next;
            for (var i = hashMap.buckets[rem] - 1; i >= 0; i = next) {
                ref var slot = ref hashMap.slots[i];
                if (slot.key - 1 == key) {
                    return i;
                }

                next = slot.next;
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CopyTo<T>(this IntHashMap<T> hashMap, T[] array) {
            var num = 0;
            for (int i = 0, li = hashMap.lastIndex; i < li && num < hashMap.length; ++i) {
                if (hashMap.slots[i].key - 1 < 0) {
                    continue;
                }

                array[num] = hashMap.data[i];
                ++num;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Clear<T>(this IntHashMap<T> hashMap) {
            if (hashMap.lastIndex <= 0) {
                return;
            }

            Array.Clear(hashMap.slots, 0, hashMap.lastIndex);
            Array.Clear(hashMap.buckets, 0, hashMap.capacity);
            Array.Clear(hashMap.data, 0, hashMap.capacity);

            hashMap.lastIndex = 0;
            hashMap.length    = 0;
            hashMap.freeIndex = -1;
        }
    }

    [Serializable]
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.DivideByZeroChecks, false)]
    public sealed class UnsafeIntHashMap<T> : IEnumerable<int> where T : unmanaged {
        public int length;
        public int capacity;
        public int capacityMinusOne;
        public int lastIndex;
        public int freeIndex;

        public int[] buckets;

        public T[]   data;
        public int[] slots;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnsafeIntHashMap(in int capacity = 0) {
            this.lastIndex = 0;
            this.length    = 0;
            this.freeIndex = -1;

            this.capacityMinusOne = HashHelpers.GetCapacity(capacity);
            this.capacity         = this.capacityMinusOne + 1;

            this.buckets = new int[this.capacity];
            this.slots   = new int[this.capacity * 2];
            this.data    = new T[this.capacity];
        }

        IEnumerator<int> IEnumerable<int>.GetEnumerator() => this.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator() {
            Enumerator e;
            e.hashMap = this;
            e.index   = 0;
            e.current = default;
            return e;
        }

        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        [Il2CppSetOption(Option.DivideByZeroChecks, false)]
        public unsafe struct Enumerator : IEnumerator<int> {
            public UnsafeIntHashMap<T> hashMap;

            public int index;
            public int current;

            public bool MoveNext() {
                fixed (int* slotsPtr = &this.hashMap.slots[0]) {
                    for (; this.index < this.hashMap.lastIndex; this.index += 2) {
                        if (*(slotsPtr + this.index) - 1 < 0) {
                            continue;
                        }

                        this.current =  this.index;
                        this.index   += 2;

                        return true;
                    }
                }

                this.index   = this.hashMap.lastIndex + 1;
                this.current = default;
                return false;
            }

            public int Current => this.current;

            object IEnumerator.Current => this.current;

            void IEnumerator.Reset() {
                this.index   = 0;
                this.current = default;
            }

            public void Dispose() {
            }
        }
    }

    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.DivideByZeroChecks, false)]
    public static unsafe class UnsafeIntHashMapExtensions {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Add<T>(this UnsafeIntHashMap<T> hashMap, in int key, in T value, out int slotIndex) where T : unmanaged {
            var rem = key & hashMap.capacityMinusOne;

            fixed (int* slotsPtr = &hashMap.slots[0])
            fixed (int* bucketsPtr = &hashMap.buckets[0]) {
                int* slot;
                for (var i = *(bucketsPtr + rem) - 1; i >= 0; i = *(slot + 1)) {
                    slot = slotsPtr + i;
                    if (*slot - 1 == key) {
                        slotIndex = -1;
                        return false;
                    }
                }
            }

            if (hashMap.freeIndex >= 0) {
                slotIndex = hashMap.freeIndex;
                fixed (int* s = &hashMap.slots[0]) {
                    hashMap.freeIndex = *(s + slotIndex + 1);
                }
            }
            else {
                if (hashMap.lastIndex == hashMap.capacity * 2) {
                    var newCapacityMinusOne = HashHelpers.ExpandCapacity(hashMap.length);
                    var newCapacity         = newCapacityMinusOne + 1;

                    ArrayHelpers.Grow(ref hashMap.slots, newCapacity * 2);
                    ArrayHelpers.Grow(ref hashMap.data, newCapacity);

                    var newBuckets = new int[newCapacity];

                    fixed (int* slotsPtr = &hashMap.slots[0])
                    fixed (int* bucketsPtr = &newBuckets[0]) {
                        for (int i = 0, len = hashMap.lastIndex; i < len; i += 2) {
                            var slotPtr = slotsPtr + i;

                            var newResizeIndex   = (*slotPtr - 1) & newCapacityMinusOne;
                            var newCurrentBucket = bucketsPtr + newResizeIndex;

                            *(slotPtr + 1) = *newCurrentBucket - 1;

                            *newCurrentBucket = i + 1;
                        }
                    }

                    hashMap.buckets          = newBuckets;
                    hashMap.capacity         = newCapacity;
                    hashMap.capacityMinusOne = newCapacityMinusOne;

                    rem = key & hashMap.capacityMinusOne;
                }

                slotIndex         =  hashMap.lastIndex;
                hashMap.lastIndex += 2;
            }

            fixed (int* slotsPtr = &hashMap.slots[0])
            fixed (int* bucketsPtr = &hashMap.buckets[0])
            fixed (T* dataPtr = &hashMap.data[0]) {
                var bucket = bucketsPtr + rem;
                var slot   = slotsPtr + slotIndex;

                *slot       = key + 1;
                *(slot + 1) = *bucket - 1;

                *(dataPtr + slotIndex / 2) = value;

                *bucket = slotIndex + 1;
            }

            ++hashMap.length;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Remove<T>(this UnsafeIntHashMap<T> hashMap, in int key, out T lastValue) where T : unmanaged {
            fixed (int* slotsPtr = &hashMap.slots[0])
            fixed (int* bucketsPtr = &hashMap.buckets[0])
            fixed (T* dataPtr = &hashMap.data[0]) {
                var rem = key & hashMap.capacityMinusOne;

                int next;
                var num = -1;

                for (var i = *(bucketsPtr + rem) - 1; i >= 0; i = next) {
                    var slot     = slotsPtr + i;
                    var slotNext = slot + 1;

                    if (*slot - 1 == key) {
                        if (num < 0) {
                            *(bucketsPtr + rem) = *slotNext + 1;
                        }
                        else {
                            *(slotsPtr + num + 1) = *slotNext;
                        }

                        lastValue = *(dataPtr + i / 2);

                        *slot     = -1;
                        *slotNext = hashMap.freeIndex;

                        if (--hashMap.length == 0) {
                            hashMap.lastIndex = 0;
                            hashMap.freeIndex = -1;
                        }
                        else {
                            hashMap.freeIndex = i;
                        }

                        return true;
                    }

                    next = *slotNext;
                    num  = i;
                }

                lastValue = default;
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetValue<T>(this UnsafeIntHashMap<T> hashMap, in int key, out T value) where T : unmanaged {
            var rem = key & hashMap.capacityMinusOne;

            fixed (int* slotsPtr = &hashMap.slots[0])
            fixed (int* bucketsPtr = &hashMap.buckets[0])
            fixed (T* dataPtr = &hashMap.data[0]) {
                int* slot;
                for (var i = *(bucketsPtr + rem) - 1; i >= 0; i = *(slot + 1)) {
                    slot = slotsPtr + i;
                    if (*slot - 1 == key) {
                        value = *(dataPtr + i / 2);
                        return true;
                    }
                }
            }

            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetValueByKey<T>(this UnsafeIntHashMap<T> hashMap, in int key) where T : unmanaged {
            var rem = key & hashMap.capacityMinusOne;

            fixed (int* slotsPtr = &hashMap.slots[0])
            fixed (int* bucketsPtr = &hashMap.buckets[0])
            fixed (T* dataPtr = &hashMap.data[0]) {
                int next;
                for (var i = *(bucketsPtr + rem) - 1; i >= 0; i = next) {
                    if (*(slotsPtr + i) - 1 == key) {
                        return *(dataPtr + i / 2);
                    }

                    next = *(slotsPtr + i + 1);
                }
            }

            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetValueByIndex<T>(this UnsafeIntHashMap<T> hashMap, in int index) where T : unmanaged {
            fixed (T* d = &hashMap.data[0]) {
                return *(d + index / 2);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetKeyByIndex<T>(this UnsafeIntHashMap<T> hashMap, in int index) where T : unmanaged {
            fixed (int* d = &hashMap.slots[0]) {
                return *(d + index) - 1;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int TryGetIndex<T>(this UnsafeIntHashMap<T> hashMap, in int key) where T : unmanaged {
            var rem = key & hashMap.capacityMinusOne;

            fixed (int* slotsPtr = &hashMap.slots[0])
            fixed (int* bucketsPtr = &hashMap.buckets[0]) {
                int* slot;
                for (var i = *(bucketsPtr + rem) - 1; i >= 0; i = *(slot + 1)) {
                    slot = slotsPtr + i;
                    if (*slot - 1 == key) {
                        return i;
                    }
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Clear<T>(this UnsafeIntHashMap<T> hashMap) where T : unmanaged {
            if (hashMap.lastIndex <= 0) {
                return;
            }

            Array.Clear(hashMap.slots, 0, hashMap.lastIndex);
            Array.Clear(hashMap.buckets, 0, hashMap.capacity);
            Array.Clear(hashMap.data, 0, hashMap.capacity);

            hashMap.lastIndex = 0;
            hashMap.length    = 0;
            hashMap.freeIndex = -1;
        }
    }

    [Serializable]
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.DivideByZeroChecks, false)]
    public sealed class IntStack {
        public int length;
        public int capacity;

        public int[] data;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IntStack() {
            this.capacity = 4;
            this.data     = new int[this.capacity];
            this.length   = 0;
        }
    }

    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.DivideByZeroChecks, false)]
    public static unsafe class IntStackExtensions {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Push(this IntStack stack, in int value) {
            if (stack.length == stack.capacity) {
                ArrayHelpers.Grow(ref stack.data, stack.capacity <<= 1);
            }

            fixed (int* d = &stack.data[0]) {
                *(d + stack.length++) = value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Pop(this IntStack stack) {
            fixed (int* d = &stack.data[0]) {
                return *(d + --stack.length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Clear(this IntStack stack) {
            stack.data   = null;
            stack.length = stack.capacity = 0;
        }
    }

    [Serializable]
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.DivideByZeroChecks, false)]
    public sealed class FastList<T> : IEnumerable<T> {
        public T[] data;
        public int length;
        public int capacity;

        public EqualityComparer<T> comparer;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FastList() {
            this.capacity = 3;
            this.data     = new T[this.capacity];
            this.length   = 0;

            this.comparer = EqualityComparer<T>.Default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FastList(int capacity) {
            this.capacity = HashHelpers.GetCapacity(capacity);
            this.data     = new T[this.capacity];
            this.length   = 0;

            this.comparer = EqualityComparer<T>.Default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FastList(FastList<T> other) {
            this.capacity = other.capacity;
            this.data     = new T[this.capacity];
            this.length   = other.length;
            Array.Copy(other.data, 0, this.data, 0, this.length);

            this.comparer = other.comparer;
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => this.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator() {
            Enumerator e;
            e.list    = this;
            e.current = default;
            e.index   = 0;
            return e;
        }

        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        [Il2CppSetOption(Option.DivideByZeroChecks, false)]
        public struct ResultSwap {
            public int oldIndex;
            public int newIndex;
        }

        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        [Il2CppSetOption(Option.DivideByZeroChecks, false)]
        public struct Enumerator : IEnumerator<T> {
            public FastList<T> list;

            public T   current;
            public int index;

            public bool MoveNext() {
                if (this.index >= this.list.length) {
                    return false;
                }

                this.current = this.list.data[this.index++];
                return true;
            }

            public void Reset() {
                this.index   = 0;
                this.current = default;
            }

            public T           Current => this.current;
            object IEnumerator.Current => this.current;

            public void Dispose() {
            }
        }
    }

    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.DivideByZeroChecks, false)]
    public static class FastListExtensions {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Add<T>(this FastList<T> list) {
            var index = list.length;
            if (++list.length == list.capacity) {
                ArrayHelpers.Grow(ref list.data, list.capacity <<= 1);
            }

            return index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Add<T>(this FastList<T> list, T value) {
            var index = list.length;
            if (++list.length == list.capacity) {
                ArrayHelpers.Grow(ref list.data, list.capacity <<= 1);
            }

            list.data[index] = value;
            return index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddListRange<T>(this FastList<T> list, FastList<T> other) {
            if (other.length > 0) {
                var newSize = list.length + other.length;
                if (newSize > list.capacity) {
                    while (newSize > list.capacity) {
                        list.capacity <<= 1;
                    }

                    ArrayHelpers.Grow(ref list.data, list.capacity);
                }

                if (list == other) {
                    Array.Copy(list.data, 0, list.data, list.length, list.length);
                }
                else {
                    Array.Copy(other.data, 0, list.data, list.length, other.length);
                }

                list.length += other.length;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Swap<T>(this FastList<T> list, int source, int destination) => list.data[destination] = list.data[source];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<T>(this FastList<T> list, T value) => ArrayHelpers.IndexOf(list.data, value, list.comparer);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Remove<T>(this FastList<T> list, T value) => list.RemoveAt(list.IndexOf(value));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RemoveSwap<T>(this FastList<T> list, T value, out FastList<T>.ResultSwap swap) => list.RemoveAtSwap(list.IndexOf(value), out swap);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RemoveAt<T>(this FastList<T> list, int index) {
            --list.length;
            if (index < list.length) {
                Array.Copy(list.data, index + 1, list.data, index, list.length - index);
            }

            list.data[list.length] = default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool RemoveAtSwap<T>(this FastList<T> list, int index, out FastList<T>.ResultSwap swap) {
            if (list.length-- > 1) {
                swap.oldIndex = list.length;
                swap.newIndex = index;

                list.data[swap.newIndex] = list.data[swap.oldIndex];
                return true;
            }

            swap = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Clear<T>(this FastList<T> list) {
            if (list.length <= 0) {
                return;
            }

            Array.Clear(list.data, 0, list.length);
            list.length = 0;
        }

        //todo rework
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Sort<T>(this FastList<T> list) => Array.Sort(list.data, 0, list.length, null);

        //todo rework
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Sort<T>(this FastList<T> list, int index, int len) => Array.Sort(list.data, index, len, null);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] ToArray<T>(this FastList<T> list) {
            var newArray = new T[list.length];
            Array.Copy(list.data, 0, newArray, 0, list.length);
            return newArray;
        }
    }

    [Serializable]
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.DivideByZeroChecks, false)]
    public sealed unsafe class IntFastList : IEnumerable<int> {
        public int length;
        public int capacity;

        public int[] data;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IntFastList() {
            this.capacity = 3;
            this.data     = new int[this.capacity];
            this.length   = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IntFastList(int capacity) {
            this.capacity = HashHelpers.GetCapacity(capacity);
            this.data     = new int[this.capacity];
            this.length   = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IntFastList(IntFastList other) {
            this.capacity = other.capacity;
            this.data     = new int[this.capacity];
            this.length   = other.length;
            Array.Copy(other.data, 0, this.data, 0, this.length);
        }

        IEnumerator<int> IEnumerable<int>.GetEnumerator() => this.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator() {
            Enumerator e;
            e.intFastList = this;
            e.current     = default;
            e.index       = 0;
            return e;
        }

        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        [Il2CppSetOption(Option.DivideByZeroChecks, false)]
        public struct ResultSwap {
            public int oldIndex;
            public int newIndex;
        }

        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        [Il2CppSetOption(Option.DivideByZeroChecks, false)]
        public struct Enumerator : IEnumerator<int> {
            public IntFastList intFastList;

            public int current;
            public int index;

            public bool MoveNext() {
                if (this.index >= this.intFastList.length) {
                    return false;
                }

                fixed (int* d = &this.intFastList.data[0]) {
                    this.current = *(d + this.index++);
                }

                return true;
            }

            public void Reset() {
                this.index   = 0;
                this.current = default;
            }

            public int         Current => this.current;
            object IEnumerator.Current => this.current;

            public void Dispose() {
            }
        }
    }

    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.DivideByZeroChecks, false)]
    public static unsafe class IntFastListExtensions {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Add(this IntFastList list) {
            var index = list.length;
            if (++list.length == list.capacity) {
                ArrayHelpers.Grow(ref list.data, list.capacity <<= 1);
            }

            return index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Get(this IntFastList list, in int index) {
            fixed (int* d = &list.data[0]) {
                return *(d + index);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Set(this IntFastList list, in int index, in int value) {
            fixed (int* d = &list.data[0]) {
                *(d + index) = value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Add(this IntFastList list, in int value) {
            var index = list.length;
            if (++list.length == list.capacity) {
                ArrayHelpers.Grow(ref list.data, list.capacity <<= 1);
            }

            fixed (int* p = &list.data[0]) {
                *(p + index) = value;
            }

            return index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddListRange(this IntFastList list, IntFastList other) {
            if (other.length > 0) {
                var newSize = list.length + other.length;
                if (newSize > list.capacity) {
                    while (newSize > list.capacity) {
                        list.capacity <<= 1;
                    }

                    ArrayHelpers.Grow(ref list.data, list.capacity);
                }

                if (list == other) {
                    Array.Copy(list.data, 0, list.data, list.length, list.length);
                }
                else {
                    Array.Copy(other.data, 0, list.data, list.length, other.length);
                }

                list.length += other.length;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Swap(this IntFastList list, int source, int destination) => list.data[destination] = list.data[source];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf(this IntFastList list, int value) => ArrayHelpers.IndexOfUnsafeInt(list.data, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Remove(this IntFastList list, int value) => list.RemoveAt(list.IndexOf(value));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RemoveSwap(this IntFastList list, int value, out IntFastList.ResultSwap swap) => list.RemoveAtSwap(list.IndexOf(value), out swap);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RemoveAt(this IntFastList list, int index) {
            --list.length;
            if (index < list.length) {
                Array.Copy(list.data, index + 1, list.data, index, list.length - index);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool RemoveAtSwap(this IntFastList list, int index, out IntFastList.ResultSwap swap) {
            if (list.length-- > 1) {
                swap.oldIndex = list.length;
                swap.newIndex = index;
                fixed (int* d = &list.data[0]) {
                    *(d + swap.newIndex) = *(d + swap.oldIndex);
                }

                return true;
            }

            swap = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Clear(this IntFastList list) {
            if (list.length <= 0) {
                return;
            }

            Array.Clear(list.data, 0, list.length);
            list.length = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Sort(this IntFastList list) => Array.Sort(list.data, 0, list.length, null);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Sort(this IntFastList list, int index, int len) => Array.Sort(list.data, index, len, null);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int[] ToArray(this IntFastList list) {
            var newArray = new int[list.length];
            Array.Copy(list.data, 0, newArray, 0, list.length);
            return newArray;
        }
    }

    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.DivideByZeroChecks, false)]
    internal static class ArrayHelpers {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Grow<T>(ref T[] array, int newSize) {
            var newArray = new T[newSize];
            Array.Copy(array, 0, newArray, 0, array.Length);
            array = newArray;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<T>(T[] array, T value, EqualityComparer<T> comparer) {
            for (int i = 0, length = array.Length; i < length; ++i) {
                if (comparer.Equals(array[i], value)) {
                    return i;
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int IndexOfUnsafeInt(int[] array, int value) {
            fixed (int* arr = &array[0]) {
                var i = 0;
                for (int* current = arr, length = arr + array.Length; current < length; ++current) {
                    if (*current == value) {
                        return i;
                    }

                    ++i;
                }
            }


            return -1;
        }
    }

    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.DivideByZeroChecks, false)]
    internal static class HashHelpers {
        //https://github.com/dotnet/runtime/blob/master/src/libraries/System.Private.CoreLib/src/System/Collections/HashHelpers.cs#L32
        //different primes to fit n^2 - 1
        //todo expand to maxInt
        internal static readonly int[] capacities = {
            3,
            15,
            63,
            255,
            1023,
            4095,
            16383,
            65535,
            262143,
            1048575,
            4194303,
        };

        public static int ExpandCapacity(int oldSize) {
            var min = oldSize << 1;
            return min > 2146435069U && 2146435069 > oldSize ? 2146435069 : GetCapacity(min);
        }

        public static int GetCapacity(int min) {
            for (int index = 0, length = capacities.Length; index < length; ++index) {
                var prime = capacities[index];
                if (prime >= min) {
                    return prime;
                }
            }

            throw new Exception("Prime is too big");
        }
    }
}

namespace Unity.IL2CPP.CompilerServices {
    using System;

    internal enum Option {
        NullChecks         = 1,
        ArrayBoundsChecks  = 2,
        DivideByZeroChecks = 3
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true)]
    internal class Il2CppSetOptionAttribute : Attribute {
        public Il2CppSetOptionAttribute(Option option, object value) {
            this.Option = option;
            this.Value  = value;
        }

        public Option Option { get; }
        public object Value  { get; }
    }
}