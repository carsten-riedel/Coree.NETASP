using System.Collections;

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
        public T Item { get; set; }

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
            foreach (var item in items )
            {
                var trackableItem = (TrackableItem<T>)item;
                trackableItems.Add(trackableItem.Item);
                trackableItem.UpdateStateAndDate(TrackableState.ListItemConverted,timestamp);
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
                item.UpdateStateAndDate(TrackableState.RangeAdded,timestamp);
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
            _trackableItems[index].UpdateStateAndDate(TrackableState.ItemRemoved,DateTime.UtcNow);
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


}
