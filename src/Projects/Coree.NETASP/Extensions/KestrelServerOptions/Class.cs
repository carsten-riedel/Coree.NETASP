using System.Collections;
using System.Reflection.Emit;
using System.Reflection;

using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace Coree.NETASP.Extensions.Options.KestrelServer
{
    //push
    public static partial class OptionsKestrelServerOptionsExtensions
    {
        //dd
        //dg
        public static void dd(this KestrelServerOptions kestrelServerOptions)
        {
            kestrelServerOptions.ListenAnyIP(80);
        }

        //codechange
        //sd
        //ikggu
        //fe
    }

    /// <summary>
    /// Represents the various states that a versioned item can have.
    /// </summary>
    public enum TrackableState
    {
        /// <summary>
        /// Indicates that a range of items was added.
        /// </summary>
        RangeAdded,

        /// <summary>
        /// Indicates that an item was added.
        /// </summary>
        ItemAdded,

        /// <summary>
        /// Indicates that an item was updated.
        /// </summary>
        ItemUpdated,

        /// <summary>
        /// Indicates that an item was initialized.
        /// </summary>
        ItemInitialized,

        /// <summary>
        /// Indicates that an item was converted from a list.
        /// </summary>
        ListItemConverted,

        /// <summary>
        /// Indicates that an item was inserted.
        /// </summary>
        ItemInserted,

        /// <summary>
        /// Indicates that an item was removed.
        /// </summary>
        ItemRemoved,
    }

    /// <summary>
    /// Represents an item with versioning and state tracking capabilities.
    /// </summary>
    /// <typeparam name="T">The type of the item.</typeparam>
    public class TrackableItem<T>
    {
        /// <summary>
        /// Gets or sets the item.
        /// </summary>
        public T Item
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the version timestamp of the item.
        /// </summary>
        public DateTime LastModified { get; private set; }

        /// <summary>
        /// Gets the state of the item.
        /// </summary>
        public TrackableState State { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TrackableItem{T}"/> class with the specified item.
        /// </summary>
        /// <param name="item">The item to be versioned.</param>
        public TrackableItem(T item)
        {
            Item = item;
            UpdateStateAndDate(TrackableState.ItemInitialized, DateTime.UtcNow);
        }

        /// <summary>
        /// Sets the state of the item.
        /// </summary>
        /// <param name="state">The new state of the item.</param>
        public void UpdateState(TrackableState state)
        {
            State = state;
        }

        /// <summary>
        /// Sets the version date of the item.
        /// </summary>
        /// <param name="dateTime">The version date to set.</param>
        public void UpdateDate(DateTime dateTime)
        {
            LastModified = dateTime;
        }

        /// <summary>
        /// Sets the state and version date of the item.
        /// </summary>
        /// <param name="state">The new state of the item.</param>
        /// <param name="dateTime">The version date to set.</param>
        public void UpdateStateAndDate(TrackableState state, DateTime dateTime)
        {
            UpdateState(state);
            UpdateDate(dateTime);
        }

        /// <summary>
        /// Defines an explicit conversion of a <see cref="TrackableItem{T}"/> to its underlying item of type <typeparamref name="T"/>.
        /// </summary>
        /// <param name="trackableItem">The <see cref="TrackableItem{T}"/> to convert.</param>
        public static explicit operator T(TrackableItem<T> trackableItem)
        {
            return trackableItem.Item;
        }

        /// <summary>
        /// Defines an implicit conversion of an item of type <typeparamref name="T"/> to a <see cref="TrackableItem{T}"/>.
        /// </summary>
        /// <param name="item">The item to convert to a <see cref="TrackableItem{T}"/>.</param>
        public static implicit operator TrackableItem<T>(T item)
        {
            return new TrackableItem<T>(item);
        }
    }

    public class TrackableList<T> : IList<TrackableItem<T>>
    {
        private List<TrackableItem<T>> _trackableItems = new List<TrackableItem<T>>();
        private List<TrackableItem<T>> _trackableRemovedItems = new List<TrackableItem<T>>();

        public TrackableItem<T> this[int index]
        {
            get => _trackableItems[index];
            set
            {
                _trackableItems[index] = value;
                _trackableItems[index].UpdateStateAndDate(TrackableState.ItemUpdated, DateTime.UtcNow);
            }
        }

        public static implicit operator TrackableList<T>(List<T> items)
        {
            var trackableItems = new TrackableList<T>();
            var timestamp = DateTime.UtcNow;
            foreach (var item in items)
            {
                var trackableItem = (TrackableItem<T>)item;
                trackableItems.Add(trackableItem.Item);
                trackableItem.UpdateStateAndDate(TrackableState.ListItemConverted, timestamp);
            }
            return trackableItems;
        }

        public static implicit operator List<T>(TrackableList<T> trackableList)
        {
            var items = new List<T>();
            foreach (var trackableItem in trackableList)
            {
                items.Add(trackableItem.Item);
            }
            return items;
        }

        public List<T> ToList()
        {
            return (List<T>)this;
        }

        public int Count => _trackableItems.Count;

        public bool IsReadOnly => false;

        public void AddRange(TrackableItem<T>[] items)
        {
            var timestamp = DateTime.UtcNow;
            foreach (var item in items)
            {
                _trackableItems.Add(item);
                item.UpdateStateAndDate(TrackableState.RangeAdded, timestamp);
            }
        }

        public void AddRange(T[] items)
        {
            var timestamp = DateTime.UtcNow;
            foreach (var item in items)
            {
                var trackableItem = (TrackableItem<T>)item;
                Add(trackableItem);
                trackableItem.UpdateStateAndDate(TrackableState.RangeAdded, timestamp);
            }
        }

        public void Add(TrackableItem<T> item)
        {
            _trackableItems.Add(item);
            item.UpdateStateAndDate(TrackableState.ItemAdded, DateTime.UtcNow);
        }

        public void Add(T item)
        {
            var trackableItem = (TrackableItem<T>)item;
            Add(trackableItem);
            trackableItem.UpdateStateAndDate(TrackableState.ItemAdded, DateTime.UtcNow);
        }

        public void Insert(int index, TrackableItem<T> item)
        {
            _trackableItems.Insert(index, item);
            item.UpdateStateAndDate(TrackableState.ItemInserted, DateTime.UtcNow);
        }

        public void Insert(int index, T item)
        {
            var trackableItem = (TrackableItem<T>)item;
            _trackableItems.Insert(index, trackableItem);
            trackableItem.UpdateStateAndDate(TrackableState.ItemInserted, DateTime.UtcNow);
        }

        public void Clear()
        {
            _trackableItems.Clear();
        }

        public bool Contains(TrackableItem<T> item)
        {
            return _trackableItems.Contains(item);
        }

        public bool Contains(T item)
        {
            return _trackableItems.Exists(vi => EqualityComparer<T>.Default.Equals(vi.Item, item));
        }

        public void CopyTo(TrackableItem<T>[] array, int arrayIndex)
        {
            _trackableItems.CopyTo(array, arrayIndex);
        }

        public IEnumerator<TrackableItem<T>> GetEnumerator()
        {
            return _trackableItems.GetEnumerator();
        }

        public List<TrackableItem<T>> GetIncludingRemoved()
        {
            return _trackableItems.Concat(_trackableRemovedItems).ToList();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int IndexOf(TrackableItem<T> item)
        {
            return _trackableItems.IndexOf(item);
        }

        public int IndexOf(T item)
        {
            return _trackableItems.FindIndex(vi => EqualityComparer<T>.Default.Equals(vi.Item, item));
        }

        public bool Remove(TrackableItem<T> item)
        {
            int index = IndexOf(item);
            if (index >= 0)
            {
                _trackableItems.RemoveAt(index);
                return true;
            }
            return false;
        }

        public bool Remove(T item)
        {
            int index = IndexOf(item);
            if (index >= 0)
            {
                _trackableItems.RemoveAt(index);
                return true;
            }
            return false;
        }

        public void RemoveAt(int index)
        {
            _trackableItems[index].UpdateStateAndDate(TrackableState.ItemRemoved, DateTime.UtcNow);
            _trackableRemovedItems.Add(_trackableItems[index]);
            _trackableItems.RemoveAt(index);
        }
    }

    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public double Price { get; set; }

        public Product(int id, string name, double price)
        {
            Id = id;
            Name = name;
            Price = price;
        }

        public override string ToString()
        {
            return $"Product ID: {Id}, Name: {Name}, Price: {Price}";
        }
    }

    /// <summary>
    /// Tracks property accesses on instances of type T.
    /// </summary>
    /// <typeparam name="T">The type of the tracked instance, must be a class with virtual properties.</typeparam>
    public class PropertyAccessTracker<T> where T : class, new()
    {
        private readonly T _instance;
        private readonly Dictionary<string, bool> _propertyAccessed = new Dictionary<string, bool>();

        /// <summary>
        /// Initializes a new instance of the PropertyAccessTracker.
        /// </summary>
        public PropertyAccessTracker()
        {
            _instance = CreateProxy();
        }

        /// <summary>
        /// Called when a property is accessed.
        /// </summary>
        /// <param name="propertyName">The name of the accessed property.</param>
        /// <remarks>This method updates the tracking dictionary and logs the access.</remarks>
        public void OnPropertyAccessed(string propertyName)
        {
            if (!_propertyAccessed.ContainsKey(propertyName))
            {
                _propertyAccessed[propertyName] = false;
            }
            _propertyAccessed[propertyName] = true;
            Console.WriteLine($"Property '{propertyName}' has been accessed.");
        }

        /// <summary>
        /// Checks if a property has been accessed.
        /// </summary>
        /// <param name="propertyName">The name of the property to check.</param>
        /// <returns>True if the property has been accessed; otherwise, false.</returns>
        public bool HasPropertyBeenAccessed(string propertyName)
        {
            return _propertyAccessed.ContainsKey(propertyName) && _propertyAccessed[propertyName];
        }

        /// <summary>
        /// Creates a proxy instance of type T with property access tracking.
        /// </summary>
        /// <returns>A new instance of T with property access tracking.</returns>
        private T CreateProxy()
        {
            var typeBuilder = CreateTypeBuilder();
            var proxyType = typeBuilder.CreateTypeInfo().AsType();
            return (T)Activator.CreateInstance(proxyType);
        }

        /// <summary>
        /// Creates a type builder for the proxy type.
        /// </summary>
        /// <returns>A TypeBuilder for the dynamic proxy.</returns>
        private TypeBuilder CreateTypeBuilder()
        {
            var assemblyName = new AssemblyName("DynamicPropertyAccessAssembly");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicPropertyAccessModule");
            var typeBuilder = moduleBuilder.DefineType($"{typeof(T).Name}Proxy", TypeAttributes.Public, typeof(T));

            foreach (var property in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.CanRead && property.CanWrite && property.GetMethod.IsVirtual && property.SetMethod.IsVirtual)
                {
                    CreateTrackedProperty(typeBuilder, property);
                }
            }

            typeBuilder.DefineDefaultConstructor(MethodAttributes.Public);
            return typeBuilder;
        }

        /// <summary>
        /// Creates tracked properties in the dynamic proxy.
        /// </summary>
        /// <param name="typeBuilder">The builder for the dynamic type.</param>
        /// <param name="property">The property to be tracked.</param>
        private void CreateTrackedProperty(TypeBuilder typeBuilder, PropertyInfo property)
        {
            FieldBuilder fieldBuilder = typeBuilder.DefineField($"_{property.Name.ToLower()}", property.PropertyType, FieldAttributes.Private);

            MethodBuilder getMethodBuilder = CreateGetMethodBuilder(typeBuilder, property, fieldBuilder);
            MethodBuilder setMethodBuilder = CreateSetMethodBuilder(typeBuilder, property, fieldBuilder);

            PropertyBuilder propertyBuilder = typeBuilder.DefineProperty(property.Name, PropertyAttributes.HasDefault, property.PropertyType, null);
            propertyBuilder.SetGetMethod(getMethodBuilder);
            propertyBuilder.SetSetMethod(setMethodBuilder);
        }

        /// <summary>
        /// Creates a getter method for a property.
        /// </summary>
        /// <param name="typeBuilder">The type builder.</param>
        /// <param name="property">The property info.</param>
        /// <param name="fieldBuilder">The backing field builder.</param>
        /// <returns>A MethodBuilder for the getter.</returns>
        private MethodBuilder CreateGetMethodBuilder(TypeBuilder typeBuilder, PropertyInfo property, FieldBuilder fieldBuilder)
        {
            MethodBuilder getMethodBuilder = typeBuilder.DefineMethod($"get_{property.Name}",
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                property.PropertyType, Type.EmptyTypes);

            ILGenerator getIl = getMethodBuilder.GetILGenerator();
            getIl.Emit(OpCodes.Ldarg_0);
            getIl.Emit(OpCodes.Ldstr, property.Name);
            getIl.Emit(OpCodes.Call, typeof(PropertyAccessTracker<T>).GetMethod(nameof(OnPropertyAccessed), BindingFlags.Public | BindingFlags.Instance));
            getIl.Emit(OpCodes.Ldarg_0);
            getIl.Emit(OpCodes.Ldfld, fieldBuilder);
            getIl.Emit(OpCodes.Ret);

            return getMethodBuilder;
        }

        /// <summary>
        /// Creates a setter method for a property.
        /// </summary>
        /// <param name="typeBuilder">The type builder.</param>
        /// <param name="property">The property info.</param>
        /// <param name="fieldBuilder">The backing field builder.</param>
        /// <returns>A MethodBuilder for the setter.</returns>
        private MethodBuilder CreateSetMethodBuilder(TypeBuilder typeBuilder, PropertyInfo property, FieldBuilder fieldBuilder)
        {
            MethodBuilder setMethodBuilder = typeBuilder.DefineMethod($"set_{property.Name}",
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                null, new Type[] { property.PropertyType });

            ILGenerator setIl = setMethodBuilder.GetILGenerator();
            setIl.Emit(OpCodes.Ldarg_0);
            setIl.Emit(OpCodes.Ldstr, property.Name);
            setIl.Emit(OpCodes.Call, typeof(PropertyAccessTracker<T>).GetMethod(nameof(OnPropertyAccessed), BindingFlags.Public | BindingFlags.Instance));
            setIl.Emit(OpCodes.Ldarg_0);
            setIl.Emit(OpCodes.Ldarg_1);
            setIl.Emit(OpCodes.Stfld, fieldBuilder);
            setIl.Emit(OpCodes.Ret);

            return setMethodBuilder;
        }

        /// <summary>
        /// Gets the instance of the tracked object.
        /// </summary>
        public T Instance => _instance;
    }






    public class ExampleClass
    {
        //ignore these
        private string _foo;
        private int _faa;


        //should be tracked need to be handled correctly by CreateTypeBuilder() CreateTrackedProperty
        public string Name { get; set; }

        //should be tracked need to be handled correctly by CreateTypeBuilder() CreateTrackedProperty
        public int Age { get; set; }
    }
}