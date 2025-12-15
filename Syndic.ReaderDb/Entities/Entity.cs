namespace Syndic.ReaderDb.Entities;

/// <summary>
/// Base type for all domain entities (DDD) providing an identity <see cref="Id"/> and domain event support.
/// </summary>
/// <remarks>
/// Equality is determined by the entity identity (Id) instead of by reference. This class also provides a
/// lightweight domain event collection that can be populated via <see cref="AddDomainEvent"/> and cleared with
/// <see cref="ClearDomainEvents"/>. Infrastructure (e.g., a Unit of Work or mediator pipeline) can dispatch
/// events after persistence.
/// </remarks>
public abstract class Entity
{
  /// <summary>
  /// Gets the unique identity of the entity. Set in the constructor and protected so derived types
  /// can adjust generation strategy if needed.
  /// </summary>
  public Guid Id { get; protected set; }

  // /// <summary>
  // /// Gets the domain events raised by this entity during the current unit of work.
  // /// </summary>
  // public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents?.AsReadOnly() ?? [];

  // private List<IDomainEvent>? _domainEvents;

  /// <summary>
  /// Initializes a new entity assigning a new Guid identifier by default.
  /// </summary>
  protected Entity()
  {
    Id = Guid.NewGuid();
  }

  /// <summary>
  /// Clears all accumulated domain events (typically after they have been dispatched).
  /// </summary>
  // public void ClearDomainEvents() => _domainEvents?.Clear();

  /// <summary>
  /// Adds a domain event to the internal list so it can be dispatched later.
  /// </summary>
  /// <param name="domainEvent">The domain event instance to add.</param>
  // protected void AddDomainEvent(IDomainEvent domainEvent)
  // {
  //   _domainEvents ??= [];
  //   _domainEvents.Add(domainEvent);
  // }

  /// <summary>
  /// Compares two entities for equality based on their identifiers.
  /// </summary>
  /// <param name="obj">Other object.</param>
  /// <returns><c>true</c> when the objects have the same non-empty Id.</returns>
  public override bool Equals(object? obj)
  {
    if (obj is not Entity other) return false;
    if (ReferenceEquals(this, other)) return true;
    return Id.Equals(other.Id);
  }

  /// <summary>
  /// Returns the hash code based on the entity Id.
  /// </summary>
  public override int GetHashCode() => Id.GetHashCode();
}
