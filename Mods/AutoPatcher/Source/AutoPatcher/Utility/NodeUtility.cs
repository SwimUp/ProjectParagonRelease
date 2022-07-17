using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace AutoPatcher.Utility
{
    /// <summary>
    /// Helper functions
    /// </summary>
    public static class NodeUtility
    {
        public static List<NodeDef> allNodeDefs = new List<NodeDef>();
        private static Type thisType = typeof(NodeUtility);
        public static MethodInfo getDefListInfo = AccessTools.Method(thisType, "GetDefList");
        public static List<Def> GetDefList<T>() where T : Def
            => DefDatabase<T>.AllDefsListForReading.ConvertAll(t => (Def)t);
        public static Dictionary<MethodInfo, List<(int pos, int offset)>> methodInstructionOffset = new Dictionary<MethodInfo, List<(int, int)>>();
        public static Dictionary<MethodInfo, List<(int pos, int offset)>> enumerableMethodOffset = new Dictionary<MethodInfo, List<(int, int)>>();
        public static Dictionary<MethodInfo, List<EnumItemInfo>> enumerableItems = new Dictionary<MethodInfo, List<EnumItemInfo>>();
        public static void RegisterOffset(MethodInfo method, List<(int pos, int offset)> offset)
        {
            if (offset.NullOrEmpty())
                return;
            var inOffsets = offset;
            // shift enumerableItems
            if (enumerableItems.TryGetValue(method, out var items))
            {
                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    int start = item.startPos;
                    int end = item.endPos;
                    foreach (var newOffset in inOffsets)
                    {
                        if (start >= newOffset.pos)
                            start += newOffset.offset;
                        if (end >= newOffset.pos)
                            end += newOffset.offset;
                    }
                    items[i] = new EnumItemInfo(item.label, start, end);
                }
                enumerableItems[method] = items;
            }
            // update methodInstructionOffset
            var Offsets = methodInstructionOffset.TryGetValue(method, new List<(int pos, int offset)>());
            int totalOffset = 0;
            for (int i = 0; i < Offsets.Count; i++)
            {
                var currPos = Offsets[i].pos;
                var currOffset = Offsets[i].offset;
                currPos += totalOffset;
                if (!inOffsets.NullOrEmpty())
                {
                    var newOffset = inOffsets.First();
                    if (newOffset.pos == currPos)
                    {
                        currOffset += newOffset.offset;
                        goto PopInput;
                    }
                    else if (newOffset.pos < currPos)
                    {
                        Offsets.Insert(i, newOffset);
                        i++;
                        currPos += newOffset.offset;
                        goto PopInput;
                    }
                    goto PutBack;
                PopInput:
                    totalOffset += newOffset.offset;
                    inOffsets.RemoveAt(0);
                }
            PutBack:
                Offsets[i] = (currPos, currOffset);
            }
            Offsets.AddRange(inOffsets);
            methodInstructionOffset.SetOrAdd(method, Offsets);
        }
        public static void AddEnumerableItem(MethodInfo method, List<(Label label, int pos, int length)> newItems)
        {
            if (newItems.NullOrEmpty())
                return;
            
            var NewItems = newItems.ToList();
            var items = enumerableItems[method];
            var offsets = enumerableMethodOffset.TryGetValue(method, new List<(int pos, int offset)>());
            for (int i = 0; i < items.Count; i++)
            {
                if (NewItems.NullOrEmpty())
                    break;
                var newItem = NewItems.First();
                var item = items[i];
                if (item.startPos > newItem.pos)
                {
                    items.Insert(i, new EnumItemInfo(newItem.label, newItem.pos, newItem.pos + newItem.length - 1));
                    NewItems.RemoveAt(0);
                }
            }
            foreach (var newItem in NewItems)
                items.Add(new EnumItemInfo(newItem.label, newItem.pos, newItem.pos + newItem.length - 1));
            enumerableMethodOffset.SetOrAdd(method, offsets);
            enumerableItems[method] = items;
        }
    }
}
