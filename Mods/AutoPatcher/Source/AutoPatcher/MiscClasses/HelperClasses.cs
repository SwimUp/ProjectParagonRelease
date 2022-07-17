using AutoPatcher.Utility;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using Verse;

namespace AutoPatcher
{
    public class LocalVar : IExposable
    {
        public MethodInfo method;
        public int index = -1;
        public LocalBuilder builder;
        public LocalVar(MethodInfo m, CodeInstruction instruction)
        {
            method = m;
            if (!instruction.IsLdloc() && !instruction.IsStloc())
                throw new Exception("Code instruction is not a Ldloc or Stloc");
            var t = instruction.ToLocalVar();
            index = t.index;
            builder = t.builder;
        }
        public LocalVar(LocalBuilder b)
        {
            builder = b;
        }
        public LocalVar(int i)
        {
            index = i;
        }
        public static readonly Dictionary<OpCode, int> OpCodeToInt = new Dictionary<OpCode, int>
        {
            { OpCodes.Ldloc_0, 0 },
            { OpCodes.Ldloc_1, 1 },
            { OpCodes.Ldloc_2, 2 },
            { OpCodes.Ldloc_3, 3 },
            { OpCodes.Stloc_0, 0 },
            { OpCodes.Stloc_1, 1 },
            { OpCodes.Stloc_2, 2 },
            { OpCodes.Stloc_3, 3 },
        };
        public static implicit operator LocalBuilder(LocalVar local) => local.builder;
        public static implicit operator int(LocalVar local) => local.index;
        public static explicit operator LocalVar(LocalBuilder local) => local == null ? null : new LocalVar(local);
        public static explicit operator LocalVar(int local) => local < 0 ? null : local > 3 ? null : new LocalVar(local);
        public void ExposeData()
        {
            Scribe_Values.Look(ref method, "method");
            Scribe_Values.Look(ref index, "index");
            Scribe_Values.Look(ref builder, "builder");
        }
    }
    public class TypeMethod : IExposable
    {
        public Type type;
        public Type ntype;
        public MethodInfo method;
        public TypeMethod() { }
        public TypeMethod(MethodInfo m)
        {
            method = m;
            ntype = method.DeclaringType;
            type = ntype;
            while (type.DeclaringType != null)
                type = type.DeclaringType;
            if (ntype == type)
                ntype = null;
        }
        public TypeMethod(Type t, Type nt, MethodInfo m)
        {
            type = t;
            ntype = nt;
            method = m;
        }
        public static implicit operator (Type, Type, MethodInfo)(TypeMethod tm) => (tm.type, tm.ntype, tm.method);
        public static explicit operator TypeMethod((Type, Type, MethodInfo) tm) => new TypeMethod(tm.Item1, tm.Item2, tm.Item3);
        public static explicit operator TypeMethod(MethodInfo method) => new TypeMethod(method);
        public override string ToString()
            => $"{type} : {ntype} : {method}";
        public void ExposeData()
        {
            var method_param = method?.GetParameters().Select(t => t.ParameterType).ToList();
            method_param = method_param.NullOrEmpty() ? null : method_param;
            var method_generics = method?.GetGenericArguments().ToList();
            method_generics = method_generics.NullOrEmpty() ? null : method_generics;
            var method_name = method?.Name;
            Scribe_Values.Look(ref type, "type");
            Scribe_Values.Look(ref ntype, "ntype");
            Scribe_Collections.Look(ref method_param, "method_param");
            Scribe_Collections.Look(ref method_generics, "method_generics");
            Scribe_Values.Look(ref method_name, "method_name");
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                try
                {
                    method_param = method_param.NullOrEmpty() ? null : method_param;
                    method_generics = method_generics.NullOrEmpty() ? null : method_generics;
                    method = AccessTools.Method(ntype ?? type, method_name, method_param?.ToArray(), method_generics?.ToArray());
                }
                catch (Exception e)
                {
                    Log.Error(e.Message);
                    var warn = new StringBuilder($"[[LC]AutoPatcher] TempWarning 648676: Could not find the method: {type} : {ntype} : {method_name}\n");
                    warn.AppendLine("Generics:");
                    if (method_generics.NullOrEmpty())
                        warn.AppendLine("    None");
                    else
                        foreach (var gen in method_generics)
                            warn.AppendLine($"{gen}");
                    warn.AppendLine("\nParameters:");
                    if (method_param.NullOrEmpty())
                        warn.AppendLine("    None");
                    else
                        foreach (var param in method_param)
                            warn.AppendLine($"{param}");
                    warn.AppendLine("\n" + e.Message);
                    Log.Warning(warn.ToString());
                    Controller.fromSave = false;
                }
            }
        }
    }
    public class EnumInfo : IExposable
    {
        public FieldInfo Current;
        public FieldInfo State;
        public LocalVar localState;
        public EnumInfo() { }
        public EnumInfo(FieldInfo cur, FieldInfo sta, LocalVar localSta = null)
        {
            Current = cur;
            State = sta;
            localState = localSta;
        }
        public static implicit operator (FieldInfo, FieldInfo)(EnumInfo info) => (info.Current, info.State);
        public static implicit operator (FieldInfo, FieldInfo, LocalVar)(EnumInfo info) => (info.Current, info.State, info.localState);
        public static explicit operator EnumInfo((FieldInfo, FieldInfo) info) => new EnumInfo(info.Item1, info.Item2);
        public static explicit operator EnumInfo((FieldInfo, FieldInfo, LocalVar) info) => new EnumInfo(info.Item1, info.Item2, info.Item3);
        public void ExposeData()
        {
            Scribe_Values.Look(ref Current, "Current");
            Scribe_Values.Look(ref State, "State");
            Scribe_Deep.Look(ref localState, "localState");
        }
    }
    public class EnumItemInfo : IExposable
    {
        public Label label;
        public int startPos;
        public int endPos;
        // public MethodInfo method;
        public EnumItemInfo() { }
        public EnumItemInfo(Label lab, int start, int end)
        {
            label = lab;
            startPos = start;
            endPos = end;
        }
        public static implicit operator (Label, int, int)(EnumItemInfo info) => (info.label, info.startPos, info.endPos);
        public static explicit operator EnumItemInfo((Label, int, int) info) => new EnumItemInfo(info.Item1, info.Item2, info.Item3);
        public void ExposeData()
        {
            var label_hash = label.GetHashCode();
            Scribe_Values.Look(ref label_hash, "label_hash");
            Scribe_Values.Look(ref startPos, "startPos");
            Scribe_Values.Look(ref endPos, "endPos");
            // Scribe_Values.Look(ref method, "method");
            //Log.Message($"Test 0.0: {label.GetHashCode()} : {label_hash}");
            //if (Scribe.mode == LoadSaveMode.LoadingVars)
            //    Traverse.Create(label).Field("m_label").SetValue(label_hash);
            //else if (Scribe.mode == LoadSaveMode.Saving)
            //    label_hash = (int)Traverse.Create(label).Field("m_label").GetValue();
            //Log.Message($"Test 0.1: {label.GetHashCode()} : {label_hash}");
            //foreach (var a in Traverse.Create(label).Fields())
            //    Log.Message($"Test 0.2: {a}");
        }
    }
    public class EnumItemPos<TargetT> : ItemPos<TargetT>
    {
        public int index;
        public EnumItemPos() : base() { }
        public EnumItemPos(int index, int pos, TargetT target) : base(pos, target)
        {
            this.index = index;
        }
        public static implicit operator (int, int, TargetT)(EnumItemPos<TargetT> item) => (item.index, item.pos, item.target);
        public static explicit operator EnumItemPos<TargetT>((int, int, TargetT) info) => new EnumItemPos<TargetT>(info.Item1, info.Item2, info.Item3);
        public override void ExposeData()
        {
            Scribe_Values.Look(ref index, "index");
            base.ExposeData();
        }
    }
    public class ItemPos<TargetT> : IExposable
    {
        public int pos;
        public TargetT target;
        public ItemPos() { }
        public ItemPos(int pos, TargetT target)
        {
            this.pos = pos;
            this.target = target;
        }
        public static implicit operator (int, TargetT)(ItemPos<TargetT> item) => (item.pos, item.target);
        public static explicit operator ItemPos<TargetT>((int, TargetT) info) => new ItemPos<TargetT>(info.Item1, info.Item2);
        public virtual void ExposeData()
        {
            Scribe_Values.Look(ref pos, "pos");
            var type = typeof(TargetT);
            var lookMode = LookMode.Undefined;
            Scribe_Universal.Look(ref target, "target", ref lookMode, ref type);
        }
    }
    public class SavedTuple : IExposable, ITuple
    {
        public List<object> Items = new List<object>();
        object ITuple.this[int index] => Items[index];
        int ITuple.Length => Items.Count;
        public virtual void ExposeData() { }
        public SavedTuple() { }
        public SavedTuple(ITuple tuple)
        {
            for (int i = 0; i < tuple.Length; i++)
                Items.Add(tuple[i]);
        }
    }
    public class SavedTuple<T1> : SavedTuple
    {
        public T1 Item1;
        public SavedTuple() : base() { }
        public SavedTuple(ITuple tuple) : base(tuple) { Item1 = (T1)tuple[0]; }
        public SavedTuple(T1 Item1)
        {
            this.Item1 = Item1;
            Items.Add(Item1);
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Type type = null;
            var lookMode = LookMode.Undefined;
            Scribe_Universal.Look(ref Item1, "Item1", ref lookMode, ref type);
        }
        public static implicit operator ValueTuple<T1>(SavedTuple<T1> savedTuple) => ValueTuple.Create(savedTuple.Item1);
        public static explicit operator SavedTuple<T1>(ValueTuple<T1> tuple) => new SavedTuple<T1>(tuple);
    }
    public class SavedTuple<T1, T2> : SavedTuple<T1>
    {
        public T2 Item2;
        public SavedTuple() : base() { }
        public SavedTuple(ITuple tuple) : base(tuple) { Item2 = (T2)tuple[1]; }
        public SavedTuple((T1, T2) tuple) : base(tuple.Item1)
        {
            Item1 = tuple.Item1;
            Items.Add(Item1);
            Item2 = tuple.Item2;
            Items.Add(Item2);
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Type type = null;
            var lookMode = LookMode.Undefined;
            Scribe_Universal.Look(ref Item2, "Item2", ref lookMode, ref type);
        }
        public static implicit operator (T1, T2)(SavedTuple<T1, T2> savedTuple) => ValueTuple.Create(savedTuple.Item1, savedTuple.Item2);
        public static explicit operator SavedTuple<T1, T2>((T1, T2) tuple) => new SavedTuple<T1, T2>(tuple);
    }
    public class SavedTuple<T1, T2, T3> : SavedTuple<T1, T2>
    {
        public T3 Item3;
        public SavedTuple() : base() { }
        public SavedTuple(ITuple tuple) : base(tuple) { Item3 = (T3)tuple[2]; }
        public SavedTuple((T1, T2, T3) tuple)
        {
            Item1 = tuple.Item1;
            Items.Add(Item1);
            Item2 = tuple.Item2;
            Items.Add(Item2);
            Item3 = tuple.Item3;
            Items.Add(Item3);
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Type type = null;
            var lookMode = LookMode.Undefined;
            Scribe_Universal.Look(ref Item3, "Item3", ref lookMode, ref type);
        }
        public static implicit operator (T1, T2, T3)(SavedTuple<T1, T2, T3> savedTuple) => ValueTuple.Create(savedTuple.Item1, savedTuple.Item2, savedTuple.Item3);
        public static explicit operator SavedTuple<T1, T2, T3>((T1, T2, T3) tuple) => new SavedTuple<T1, T2, T3>(tuple);
    }
    public class SavedTuple<T1, T2, T3, T4> : SavedTuple<T1, T2, T3>
    {
        public T4 Item4;
        public SavedTuple() : base() { }
        public SavedTuple(ITuple tuple) : base(tuple) { Item4 = (T4)tuple[3]; }
        public SavedTuple((T1, T2, T3, T4) tuple)
        {
            Item1 = tuple.Item1;
            Items.Add(Item1);
            Item2 = tuple.Item2;
            Items.Add(Item2);
            Item3 = tuple.Item3;
            Items.Add(Item3);
            Item4 = tuple.Item4;
            Items.Add(Item4);
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Type type = null;
            var lookMode = LookMode.Undefined;
            Scribe_Universal.Look(ref Item4, "Item4", ref lookMode, ref type);
        }
        public static implicit operator (T1, T2, T3, T4)(SavedTuple<T1, T2, T3, T4> savedTuple) => ValueTuple.Create(savedTuple.Item1, savedTuple.Item2, savedTuple.Item3, savedTuple.Item4);
        public static explicit operator SavedTuple<T1, T2, T3, T4>((T1, T2, T3, T4) tuple) => new SavedTuple<T1, T2, T3, T4>(tuple);
    }
    public class SavedTuple<T1, T2, T3, T4, T5> : SavedTuple<T1, T2, T3, T4>
    {
        public T5 Item5;
        public SavedTuple() : base() { }
        public SavedTuple(ITuple tuple) : base(tuple) { Item5 = (T5)tuple[4]; }
        public SavedTuple((T1, T2, T3, T4, T5) tuple)
        {
            Item1 = tuple.Item1;
            Items.Add(Item1);
            Item2 = tuple.Item2;
            Items.Add(Item2);
            Item3 = tuple.Item3;
            Items.Add(Item3);
            Item4 = tuple.Item4;
            Items.Add(Item4);
            Item5 = tuple.Item5;
            Items.Add(Item5);
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Type type = null;
            var lookMode = LookMode.Undefined;
            Scribe_Universal.Look(ref Item5, "Item5", ref lookMode, ref type);
        }
        public static implicit operator (T1, T2, T3, T4, T5)(SavedTuple<T1, T2, T3, T4, T5> savedTuple) => ValueTuple.Create(savedTuple.Item1, savedTuple.Item2, savedTuple.Item3, savedTuple.Item4, savedTuple.Item5);
        public static explicit operator SavedTuple<T1, T2, T3, T4, T5>((T1, T2, T3, T4, T5) tuple) => new SavedTuple<T1, T2, T3, T4, T5>(tuple);
    }
    public class SavedTuple<T1, T2, T3, T4, T5, T6> : SavedTuple<T1, T2, T3, T4, T5>
    {
        public T6 Item6;
        public SavedTuple() : base() { }
        public SavedTuple(ITuple tuple) : base(tuple) { Item6 = (T6)tuple[5]; }
        public SavedTuple((T1, T2, T3, T4, T5, T6) tuple)
        {
            Item1 = tuple.Item1;
            Items.Add(Item1);
            Item2 = tuple.Item2;
            Items.Add(Item2);
            Item3 = tuple.Item3;
            Items.Add(Item3);
            Item4 = tuple.Item4;
            Items.Add(Item4);
            Item5 = tuple.Item5;
            Items.Add(Item5);
            Item6 = tuple.Item6;
            Items.Add(Item6);
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Type type = null;
            var lookMode = LookMode.Undefined;
            Scribe_Universal.Look(ref Item6, "Item6", ref lookMode, ref type);
        }
        public static implicit operator (T1, T2, T3, T4, T5, T6)(SavedTuple<T1, T2, T3, T4, T5, T6> savedTuple) => ValueTuple.Create(savedTuple.Item1, savedTuple.Item2, savedTuple.Item3, savedTuple.Item4, savedTuple.Item5, savedTuple.Item6);
        public static explicit operator SavedTuple<T1, T2, T3, T4, T5, T6>((T1, T2, T3, T4, T5) tuple) => new SavedTuple<T1, T2, T3, T4, T5, T6>(tuple);
    }
    public class SavedTuple<T1, T2, T3, T4, T5, T6, T7> : SavedTuple<T1, T2, T3, T4, T5, T6>
    {
        public T7 Item7;
        public SavedTuple() : base() { }
        public SavedTuple(ITuple tuple) : base(tuple) { Item7 = (T7)tuple[6]; }
        public SavedTuple((T1, T2, T3, T4, T5, T6, T7) tuple)
        {
            Item1 = tuple.Item1;
            Items.Add(Item1);
            Item2 = tuple.Item2;
            Items.Add(Item2);
            Item3 = tuple.Item3;
            Items.Add(Item3);
            Item4 = tuple.Item4;
            Items.Add(Item4);
            Item5 = tuple.Item5;
            Items.Add(Item5);
            Item6 = tuple.Item6;
            Items.Add(Item6);
            Item7 = tuple.Item7;
            Items.Add(Item7);
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Type type = null;
            var lookMode = LookMode.Undefined;
            Scribe_Universal.Look(ref Item7, "Item7", ref lookMode, ref type);
        }
        public static implicit operator (T1, T2, T3, T4, T5, T6, T7)(SavedTuple<T1, T2, T3, T4, T5, T6, T7> savedTuple) => ValueTuple.Create(savedTuple.Item1, savedTuple.Item2, savedTuple.Item3, savedTuple.Item4, savedTuple.Item5, savedTuple.Item6, savedTuple.Item7);
        public static explicit operator SavedTuple<T1, T2, T3, T4, T5, T6, T7>((T1, T2, T3, T4, T5, T7) tuple) => new SavedTuple<T1, T2, T3, T4, T5, T6, T7>(tuple);
    }
    /*public class SavedTuple<T1, T2, T3, T4, T5, T6, T7, TRest> : SavedTuple<T1, T2, T3, T4, T5, T6, T7> where TRest : struct
    {
        public TRest? Rest;
        public SavedTuple() : base() { }
        public SavedTuple(ITuple tuple) : base(tuple) { Rest = (TRest)tuple[7]; }
        public SavedTuple(ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> tuple)
        {
            Item1 = tuple.Item1;
            Items.Add(Item1);
            Item2 = tuple.Item2;
            Items.Add(Item2);
            Item3 = tuple.Item3;
            Items.Add(Item3);
            Item4 = tuple.Item4;
            Items.Add(Item4);
            Item5 = tuple.Item5;
            Items.Add(Item5);
            Item6 = tuple.Item6;
            Items.Add(Item6);
            Item7 = tuple.Item7;
            Items.Add(Item7);
            Rest = tuple.Rest;
            Items.Add(Rest);
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Type type = null;
            var lookMode = LookMode.Undefined;
            Scribe_Universal.Look(ref Rest, "Rest", ref lookMode, ref type);
        }
        public static implicit operator (T1, T2, T3, T4, T5, T6, T7, TRest?)(SavedTuple<T1, T2, T3, T4, T5, T6, T7, TRest> savedTuple) => ValueTuple.Create(savedTuple.Item1, savedTuple.Item2, savedTuple.Item3, savedTuple.Item4, savedTuple.Item5, savedTuple.Item6, savedTuple.Item7, savedTuple.Rest);
        public static explicit operator SavedTuple<T1, T2, T3, T4, T5, T6, T7, TRest>(ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> tuple) => new SavedTuple<T1, T2, T3, T4, T5, T6, T7, TRest>(tuple);
    }*/
    public class SavedList : IList
    {
        public virtual IList IList => null;
        public bool IsReadOnly => IList.IsReadOnly;
        public bool IsFixedSize => IList.IsFixedSize;
        public int Count => IList.Count;
        public object SyncRoot => IList.SyncRoot;
        public bool IsSynchronized => IList.IsSynchronized;
        public object this[int index] { get => IList[index]; set => IList[index] = value; }
        public int Add(object value) => IList.Add(value);
        public bool Contains(object value) => IList.Contains(value);
        public void Clear() => IList.Clear();
        public int IndexOf(object value) => IList.IndexOf(value);
        public void Insert(int index, object value) => IList.Insert(index, value);
        public void Remove(object value) => IList.Remove(value);
        public void RemoveAt(int index) => IList.RemoveAt(index);
        public void CopyTo(Array array, int index) => IList.CopyTo(array, index);
        public IEnumerator GetEnumerator() =>  IList.GetEnumerator();
        object IList.this[int index] { get => IList[index]; set => IList[index] = value; }
}
    public class SavedList<T> : SavedList, IExposable, IList<T>
    {
        public List<T> list;
        public override IList IList => list;
        public SavedList() { }
        public SavedList(List<T> list) { this.list = list; }
        public SavedList(IEnumerable<T> collection) { list = new List<T>(collection); }
        public SavedList(int capacity) { list = new List<T>(capacity); }
        public T this[int index] { get => list[index]; set => list[index] = value; }
        public int Count => list.Count;
        public bool IsReadOnly => ((IList<T>)list).IsReadOnly;
        public bool IsFixedSize => ((IList)list).IsFixedSize;
        public object SyncRoot => ((IList)list).SyncRoot;
        public bool IsSynchronized => ((IList)list).IsSynchronized;
        public void Add(T item) => list.Add(item);
        public bool Contains(T item) => list.Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => list.CopyTo(array, arrayIndex);
        public void CopyTo(Array array, int index) => ((IList)list).CopyTo(array, index);
        public IEnumerator<T> GetEnumerator() => list.GetEnumerator();
        public int IndexOf(T item) => list.IndexOf(item);
        public void Insert(int index, T item) => list.Insert(index, item);
        public bool Remove(T item) => list.Remove(item);
        public void Remove(object value) => ((IList)list).Remove(value);
        public void RemoveAt(int index) => list.RemoveAt(index);
        IEnumerator IEnumerable.GetEnumerator() => list.GetEnumerator();
        public void ExposeData() => Scribe_Collections.Look(ref list, "List", LookMode.Undefined);
        public static implicit operator List<T>(SavedList<T> savedList) => savedList.list;
        public static explicit operator SavedList<T>(List<T> list) => new SavedList<T>(list);
    }
    /*public class SavedList<T> : List<T>, IExposable
    {
        public void ExposeData()
        {
            List<T> list = this;
            Scribe_Collections.Look(ref list, "SavedList");
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                Clear();
                AddRange(list);
            }
        }
    }*/
}
