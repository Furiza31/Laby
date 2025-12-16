namespace Labyrinth.Items
{
    /// <summary>
    /// Inventory of collectable items for rooms and players.
    /// </summary>
    /// <param name="item">Optional initial item in the inventory.</param>
    public abstract class Inventory
    {
        protected Inventory(ICollectable? item = null)
        {
            if (item is not null)
            {
                _items.Add(item);
            }
        }

        /// <summary>
        /// True if the room has an items, false otherwise.
        /// </summary>
        public bool HasItems => _items.Count > 0;

        /// <summary>
        /// Gets the type of the item in the room.
        /// </summary>
        public IEnumerable<Type> ItemTypes => _items.Select(item => item.GetType());

        /// <summary>
        /// Asynchronously list the item types currently in the inventory.
        /// </summary>
        public Task<IEnumerable<Type>> ListItemTypesAsync() =>
            Task.FromResult<IEnumerable<Type>>([.. ItemTypes]);

        /// <summary>
        /// Places an item in the inventory, removing it from another one.
        /// </summary>
        /// <param name="from">The inventory from which the item is taken. The item is removed from this inventory.</param>
        /// <exception cref="InvalidOperationException">Thrown if the room already contains an item (check with <see cref="HasItem"/>).</exception>
        public void MoveItemFrom(Inventory from, int nth = 0)
        {
            if (!from.HasItems)
            {
                throw new InvalidOperationException("No item to take from the source inventory");
            }
            _items.Add(from._items[nth]);
            from._items.RemoveAt(nth);
        }

        /// <summary>
        /// Asynchronously try to move an item from another inventory.
        /// </summary>
        /// <param name="from">Source inventory.</param>
        /// <param name="nth">Index of the item to move.</param>
        /// <returns>True if the item was moved, false if the source inventory changed.</returns>
        public Task<bool> TryMoveItemFromAsync(Inventory from, int nth = 0)
        {
            if (nth < 0 || nth >= from._items.Count)
            {
                return Task.FromResult(false);
            }

            MoveItemFrom(from, nth);
            return Task.FromResult(true);
        }

        protected IList<ICollectable> _items = [];
    }
}
