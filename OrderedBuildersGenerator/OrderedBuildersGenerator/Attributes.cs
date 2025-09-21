using System;

namespace OrderedBuildersGenerator;

/// <summary>
/// Marks a class from which a step-builder API will be generated.
/// </summary>
/// <remarks>
/// Apply this to a class that contains method stubs annotated with
/// <see cref="UnorderedStep"/>, <see cref="OrderedStep"/>, and <see cref="BuildStep"/>.
/// The source generator scans the annotated class, reads its using directives,
/// type parameters, and constraints, and emits a concrete builder class plus
/// the step interfaces.
///
/// If <paramref name="resultClassName"/> is not provided, the generated class
/// name defaults to <c>{YourClassName}Generated</c>.
///
/// The generated class mirrors the constructors of the annotated class. If no
/// constructors are declared, a parameterless constructor is emitted.
/// </remarks>
/// <param name="resultClassName">
/// Optional override for the generated builder class name.
/// </param>
/// <example>
/// <code>
/// [StepBuilder("OrderBuilder")]
/// public class OrderConfig
/// {
///     [UnorderedStep] public void WithNote(string note) { ... }
///
///     [OrderedStep(StepOrder.One)]  public void WithCustomerId(Guid id) { ... }
///     [OrderedStep(StepOrder.Two)]  public void WithItems(IEnumerable&lt;Item&gt; items) { ... }
///
///     [BuildStep] public Order Build() { ... }
/// }
/// </code>
/// This will generate <c>OrderBuilder</c> with step interfaces enforcing the
/// WithCustomerId → WithItems → Build ordering, while <c>WithNote</c> is callable at any time.
/// </example>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class StepBuilder : Attribute
{
    /// <summary>
    /// Marks a class from which a step-builder API will be generated with the default generated name
    /// <c>{YourClassName}Generated</c>.
    /// </summary>
    public StepBuilder()
    {
    }

    /// <summary>
    /// Marks a class from which a step-builder API will be generated, specifying the generated class name.
    /// </summary>
    /// <param name="resultClassName">Override for the generated builder class name.</param>
    public StepBuilder(string resultClassName)
    {
    }
}

/// <summary>
/// Marks a builder method as an unordered step, i.e., callable at any time in the flow.
/// </summary>
/// <remarks>
/// Methods annotated with <see cref="UnorderedStep"/> are included on every step interface
/// (including the terminal build interface).
/// The generated API returns the current step interface to allow fluent chaining.
///
/// Method type parameters and constraints are preserved in the generated signatures.
/// </remarks>
/// <example>
/// <code>
/// [UnorderedStep]
/// public void WithTag(string tag) { ... }
/// </code>
/// This will be available on every generated step.
/// </example>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public class UnorderedStep : Attribute
{
}

/// <summary>
/// Marks a builder method as part of an ordered sequence of steps.
/// </summary>
/// <remarks>
/// Use this to enforce that certain steps are called in a specific order. The
/// <see cref="StepOrder"/> value determines the position within the sequence
/// (e.g., One, Two, Three). You may declare multiple methods with the same order; at runtime,
/// exactly one of them can be called for that position. Calling an order N method
/// transitions the fluent API to the interface for order N+1 (or to build when N is the last).
///
/// Method type parameters and constraints are preserved in the generated signatures.
/// </remarks>
/// <param name="order">The step position within the ordered sequence.</param>
/// <example>
/// <code>
/// [OrderedStep(StepOrder.One)]
/// public void WithCustomer(string name) { ... }
///
/// [OrderedStep(StepOrder.Two)]
/// public void WithItem(string sku, int qty) { ... }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public class OrderedStep : Attribute
{
    /// <summary>
    /// Marks a builder method as part of an ordered sequence of steps.
    /// </summary>
    /// <param name="order">The step position within the ordered sequence.</param>
    public OrderedStep(StepOrder order)
    {
    }
}

/// <summary>
/// Defines the relative position of an <see cref="OrderedStep"/> within the generated flow.
/// </summary>
public enum StepOrder
{
    One,
    Two,
    Three,
    Four,
    Five,
    Six,
    Seven,
    Eight,
    Nine,
    Ten,
    Eleven,
    Twelve,
    Thirteen,
    Fourteen,
    Fifteen,
    Sixteen
}

/// <summary>
/// Marks a builder method as a terminal build step that returns the final result.
/// </summary>
/// <remarks>
/// Methods annotated with <see cref="BuildStep"/> appear on the terminal build interface and on
/// the generated terminal class. Their original return type is preserved in the generated API.
/// If no ordered steps exist, the generated entry class itself is terminal and exposes these methods.
///
/// Method type parameters and constraints are preserved in the generated signatures.
/// </remarks>
/// <example>
/// <code>
/// [BuildStep]
/// public Order Build() => _state.ToOrder();
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public class BuildStep : Attribute
{
}