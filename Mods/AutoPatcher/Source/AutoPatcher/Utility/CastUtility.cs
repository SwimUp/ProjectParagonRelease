using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Verse;

namespace AutoPatcher.Utility
{
    public static class CastUtility
    {
        #region Main cast functions
        public static bool CanCastTo(this Type from, Type to)
        {
            if (typeof(IEnumerable).IsAssignableFrom(from) && typeof(IEnumerable).IsAssignableFrom(to))
            {
                from = from.GetInterface(typeof(IEnumerable<>).Name).GenericTypeArguments.First();
                to = to.GetInterface(typeof(IEnumerable<>).Name).GenericTypeArguments.First();
            }
            if (from.IsAssignableFrom(to))
                return true;
            if (from.GetMethods(BindingFlags.Public | BindingFlags.Static).Where(t => (t.Name == "op_Implicit" || t.Name == "op_Explicit") && t.ReturnType == to).Any(t => t.GetParameters().First().ParameterType == from))
                return true;
            if (to.GetMethods(BindingFlags.Public | BindingFlags.Static).Where(t => (t.Name == "op_Implicit" || t.Name == "op_Explicit") && t.ReturnType == to).Any(t => t.GetParameters().First().ParameterType == from))
                return true;
            return false;
        }
        private static void AddItem<T, Tobj>(ref T collection, Tobj obj, bool all = false)
        {
            if (typeof(IList<Tobj>).IsAssignableFrom(typeof(T)))
            {
                if (collection == null)
                    collection = (T)(object)new List<Tobj>();
                var list = collection as IList<Tobj>;
                list.Add(obj);
                return;
            }
            if (typeof(IEnumerable<Tobj>).IsAssignableFrom(typeof(T)))
            {
                var enumerable = collection as IEnumerable<Tobj>;
                collection = (T)enumerable.AddItem(obj);
                return;
            }
            if (typeof(T).IsAssignableFrom(typeof(Tobj)))
                collection = (T)(object)obj;
        }
        private static MethodInfo TryCastToList_Method = AccessTools.Method(typeof(CastUtility), "TryCastToList");
        private static MethodInfo TryCastToObj_Method = AccessTools.Method(typeof(CastUtility), "TryCastToObj");
        private static MethodInfo TryCastToEnumerable_Method = AccessTools.Method(typeof(CastUtility), "TryCastToEnumerable");
        private static MethodInfo CastListToSavedList_Method = AccessTools.Method(typeof(CastUtility), "CastListToSavedList");
        private static MethodInfo CastSavedListToList_Method = AccessTools.Method(typeof(CastUtility), "CastSavedListToList");
        private static SavedList<T> CastListToSavedList<T>(List<T> obj) => (SavedList<T>)obj;
        private static List<T> CastSavedListToList<T>(SavedList<T> obj) => obj;
        private static bool TryCastLoop<Tobj, T, listTobj, listT>(this listTobj objs, out listT vals, out bool res, bool all = false)
        {
            res = false;
            vals = default;
            if (typeof(IList).IsAssignableFrom(typeof(Tobj)) && typeof(IList).IsAssignableFrom(typeof(T)))
            {
                var TobjType = typeof(Tobj).GetInterface(typeof(IList<>).Name).GenericTypeArguments.First();
                var TType = typeof(T).GetInterface(typeof(IList<>).Name).GenericTypeArguments.First();
                MethodInfo methodTobj = null;
                MethodInfo methodT = null;
                var flagTobj = typeof(SavedList<>).MakeGenericType(TobjType).IsAssignableFrom(typeof(Tobj));
                if (flagTobj)
                    methodTobj = CastSavedListToList_Method.MakeGenericMethod(TobjType);
                var flagT = typeof(SavedList<>).MakeGenericType(TType).IsAssignableFrom(typeof(T));
                if (flagT)
                    methodT = CastListToSavedList_Method.MakeGenericMethod(TType);
                if (TobjType == TType && typeof(listT) == typeof(T) && typeof(listTobj) == typeof(Tobj))
                {
                    if (flagT)
                        vals = (listT)methodT.Invoke(null, new object[] { objs });
                    else
                        vals = (listT)methodTobj.Invoke(null, new object[] { objs });
                    res = true;
                    return true;
                }
                var method = TryCastToList_Method.MakeGenericMethod(TobjType, TType);
                foreach (var obj in objs as IList)
                {
                    var Obj = obj;
                    if (flagTobj)
                        Obj = methodTobj.Invoke(null, new object[] { Obj });
                    var par = new object[] { Obj, null, all };
                    var currRes = (bool)method.Invoke(null, par);
                    var item = par[1];
                    if (flagT)
                        item = methodT.Invoke(null, new object[] { item });
                    res |= currRes;
                    if (currRes)
                        AddItem(ref vals, (T)item);
                }
                return true;
            }
            if (typeof(IEnumerable).IsAssignableFrom(typeof(Tobj)) && typeof(IEnumerable).IsAssignableFrom(typeof(T)))
            {
                var TobjType = typeof(Tobj).GetInterface(typeof(IEnumerable<>).Name).GenericTypeArguments.First();
                var TType = typeof(T).GetInterface(typeof(IEnumerable<>).Name).GenericTypeArguments.First();
                var method = TryCastToEnumerable_Method.MakeGenericMethod(TobjType, TType);
                foreach (var obj in objs as IEnumerable)
                {
                    var par = new object[] { obj, null, all };
                    var currRes = (bool)method.Invoke(null, par);
                    res |= currRes;
                    if (currRes)
                        AddItem(ref vals, (T)par[1]);
                }
                return true;
            }
            return false;
        }
        public static bool TryCastToObj<Tobj, T>(this Tobj obj, out T val, bool all = false)
        {
            val = default;
            if (obj is T cobj)
            {
                val = cobj;
                return true;
            }
            if (obj.TryCastLoop<Tobj, T, Tobj, T>(out val, out bool res, all))
                return res;
            var castOps = typeof(Tobj).GetMethods(BindingFlags.Public | BindingFlags.Static).Where(t => (t.Name == "op_Implicit" || t.Name == "op_Explicit") && t.ReturnType == typeof(T)).ToList();
            castOps.AddRange(typeof(T).GetMethods(BindingFlags.Public | BindingFlags.Static).Where(t => (t.Name == "op_Implicit" || t.Name == "op_Explicit") && t.ReturnType == typeof(T)));
            var castOp = castOps.FirstOrDefault(t => t.GetParameters().First().ParameterType == typeof(Tobj));
            if (castOp == null)
                return false;
            val = (T)castOp.Invoke(null, new object[] { obj });
            return true;
        }

        public static bool TryCastToEnumerable<Tobj, T>(this IEnumerable<Tobj> objs, out IEnumerable<T> vals, bool all = false)
        {
            if (objs is IEnumerable<T> cobjs)
            {
                vals = cobjs;
                return true;
            }
            if (objs.TryCastLoop<Tobj, T, IEnumerable<Tobj>, IEnumerable<T>>(out vals, out bool res, all))
                return res;
            vals = objs.OfType<T>();
            var castOps = typeof(Tobj).GetMethods(BindingFlags.Public | BindingFlags.Static).Where(t => (t.Name == "op_Implicit" || t.Name == "op_Explicit") && t.ReturnType == typeof(T)).ToList();
            castOps.AddRange(typeof(T).GetMethods(BindingFlags.Public | BindingFlags.Static).Where(t => (t.Name == "op_Implicit" || t.Name == "op_Explicit") && t.ReturnType == typeof(T)));
            var castOp = castOps.FirstOrDefault(t => t.GetParameters().First().ParameterType == typeof(Tobj));
            if (castOp == null)
                return !vals.EnumerableNullOrEmpty();
            var cobjs2 = objs.OfType<Tobj>();
            vals = cobjs2.Select(t => (T)castOp.Invoke(null, new object[] { t }));
            return true;
        }
        public static bool TryCastToList<Tobj, T>(this List<Tobj> objs, out List<T> vals, bool all = false)
        {
            if (objs is List<T> cobjs)
            {
                vals = cobjs;
                return true;
            }
            if (objs.TryCastLoop<Tobj, T, List<Tobj>, List<T>>(out vals, out bool res, all))
                return res;
            vals = new List<T>();
            if (!objs.AsEnumerable().TryCastTo<Tobj, T>(out var enumVal))
                return false;
            vals = enumVal?.ToList() ?? new List<T>();
            return true;
        }
        /*public static bool TryCastToTuple<Tobj, T>(this Tobj objs, out T vals) where Tobj : Tuple<> where T : Tuple
        {
            if (typeof(ITuple).IsAssignableFrom(typeof(Tobj)) && typeof(ITuple).IsAssignableFrom(typeof(T)))
            {
                var TobjType = typeof(Tobj).GetInterface(ITuple).GenericTypeArguments.First();
                var TType = typeof(T).GetInterface(typeof(IList<>).Name).GenericTypeArguments.First();
            }
            if (typeof(IList).IsAssignableFrom(typeof(Tobj)) && typeof(IList).IsAssignableFrom(typeof(T)))
            {
                var TobjType = typeof(Tobj).GetInterface(typeof(IList<>).Name).GenericTypeArguments.First();
                var TType = typeof(T).GetInterface(typeof(IList<>).Name).GenericTypeArguments.First();
                var method = AccessTools.Method(typeof(CastUtility), "TryCastToList", null,
                    new Type[] { TobjType, TType });
                vals = new List<T>();
                bool castRes = false;
                foreach (var obj in objs)
                {
                    var par = new object[] { obj, null };
                    var currRes = (bool)method.Invoke(null, par);
                    castRes |= currRes;
                    if (currRes)
                        vals.Add((T)par[1]);
                }
                return castRes;
            }
            if (typeof(IEnumerable).IsAssignableFrom(typeof(Tobj)) && typeof(IEnumerable).IsAssignableFrom(typeof(T)))
            {
                var TobjType = typeof(Tobj).GetInterface(typeof(IEnumerable<>).Name).GenericTypeArguments.First();
                var TType = typeof(T).GetInterface(typeof(IEnumerable<>).Name).GenericTypeArguments.First();
                var method = AccessTools.Method(typeof(CastUtility), "TryCastToEnumerable", null,
                    new Type[] { TobjType, TType });
                vals = new List<T>();
                bool castRes = false;
                foreach (var obj in objs)
                {
                    var par = new object[] { obj, null };
                    var currRes = (bool)method.Invoke(null, par);
                    castRes |= currRes;
                    if (currRes)
                        vals.Add((T)par[1]);
                }
                return castRes;
            }
            vals = null;
            if (!objs.AsEnumerable().TryCastTo<Tobj, T>(out var enumVal))
                return false;
            vals = enumVal?.ToList() ?? new List<T>();
            return true;
        }*/
        #endregion
        #region Helper functions generic
        public static bool TryCastTo<Tobj, T>(this Tobj objs, out T vals, bool all = false)
            => objs.TryCastToObj(out vals, all);
        public static bool TryCastTo<Tobj, T>(this IEnumerable<Tobj> objs, out IEnumerable<T> vals, bool all = false)
            => objs.TryCastToEnumerable(out vals, all);
        public static bool TryCastTo<Tobj, T>(this List<Tobj> objs, out List<T> vals, bool all = false)
        {
            var res = objs.TryCastToList(out vals, all);
            vals = vals ?? new List<T>();
            return res;
        }
        public static T CastTo<Tobj, T>(this Tobj obj)
        {
            obj.TryCastTo(out T val);
            return val;
        }
        public static IEnumerable<T> CastTo<Tobj, T>(this IEnumerable<Tobj> objs)
        {
            objs.TryCastTo<Tobj, T>(out var vals);
            return vals;
        }
        public static List<T> CastTo<Tobj, T>(this List<Tobj> objs)
        {
            objs.TryCastTo<Tobj, T>(out var vals);
            vals = vals ?? new List<T>();
            return vals;
        }
        #endregion
        #region Helper functions semi-generic
        public static bool TryCastTo<T>(this object obj, out T val, bool all = false)
        {
            var method = TryCastToObj_Method.MakeGenericMethod(obj.GetType(), typeof(T));
            var par = new object[] { obj, null, all };
            var res = (bool)method.Invoke(null, par);
            val = (T)par[1];
            return res;
        }
        public static bool TryCastTo<Tobj>(this Tobj obj, Type T, out object val, bool all = false)
        {
            var method = TryCastToObj_Method.MakeGenericMethod(typeof(Tobj), T);
            var par = new object[] { obj, null, all };
            var res = (bool)method.Invoke(null, par);
            val = par[1];
            return res;
        }
        public static bool TryCastTo<Tobj>(this IEnumerable<Tobj> objs, Type T, out object val, bool all = false)
        {
            var method = TryCastToEnumerable_Method.MakeGenericMethod(typeof(Tobj), T);
            var par = new object[] { objs, null, all };
            var res = (bool)method.Invoke(null, par);
            val = par[1];
            return res;
        }
        public static bool TryCastTo<Tobj>(this List<Tobj> objs, Type T, out object val, bool all = false)
        {
            val = Activator.CreateInstance(typeof(List<>).MakeGenericType(T));
            var IList = (IList)val;
            var method = TryCastToList_Method.MakeGenericMethod(typeof(Tobj), T);
            var par = new object[] { objs, null, all };
            var res = (bool)method.Invoke(null, par);
            foreach (var item in (IEnumerable)par[1])
                IList.Add(item);
            return res;
        }
        public static object CastTo<Tobj>(this Tobj obj, Type T, bool all = false)
        {
            obj.TryCastTo(T, out var val, all);
            return val;
        }
        public static object CastTo<Tobj>(this IEnumerable<Tobj> objs, Type T, bool all = false)
        {
            objs.TryCastTo(T, out var vals, all);
            return vals;
        }
        public static object CastTo<Tobj>(this List<Tobj> objs, Type T)
        {
            objs.TryCastTo(T, out var vals);
            vals = vals ?? Activator.CreateInstance(typeof(List<>).MakeGenericType(T));
            return vals;
        }
        #endregion
    }
}
