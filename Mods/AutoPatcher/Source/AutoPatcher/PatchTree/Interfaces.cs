using System;
using System.Collections.Generic;
using Verse;

namespace AutoPatcher
{
    /// <summary>
    /// General port interface
    /// </summary>
    public interface IPort : IExposable, ILoadReferenceable
    {
        Type DataType { get; }
        int DataCount { get; }
        IEnumerable<T> GetData<T>();
        List<T> GetDataList<T>();
        void SetData<T>(List<T> value);
        void AddData<T>(T value);
        void AddData<T>(IEnumerable<T> value);
        void Clear();
        string PrintData();
        void RegisterPort(Node node, int portNumber, int portGroup, Port.InputOutput InOut);
    }
    /// <summary>
    /// Specific data port interface
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IPort<T> : IPort
    {
        List<T> Data { get; set; }
    }
}
