using System.Collections.Generic;
using Common;

namespace DialogueEditor.Utilities {
    public static class CollectionUtility {
        public static void AddItem<T, TK>(this SerializableDictionary<T, List<TK>> serializableDictionary, T key,
                                          TK value) {
            if (serializableDictionary.ContainsKey(key)) {
                serializableDictionary[key].Add(value);

                return;
            }

            serializableDictionary.Add(key, new List<TK> { value });
        }
    }
}