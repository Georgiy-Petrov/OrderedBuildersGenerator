using System;
using System.Collections.Generic;
using OrderedBuildersGenerator;

public class Program
{
    public static void Main()
    {
        var order =
            new OrderBuilder()
                .WithNote("Leave at reception")
                .WithCustomer(Guid.NewGuid())
                .AddItem("SKU-001", 2)
                .Build();

        var email1 = new EmailBuilder() // mirrors EmailConfig()
            .To("user@example.com")
            .Body("Hello!")
            .Build();

        var email2 = new EmailBuilder("alerts@system") // mirrors EmailConfig(string)
            .To("ops@example.com")
            .Body("System up")
            .Build();

        var result = new GappedBuilder()
            .A() // Step One
            .C() // Step Three (next)
            .Build();

        var genericsResult = new BuilderWithGenericsGenerated<int>().WithStep<string>("").Build();
    }
}

public record Order(Guid CustomerId, IReadOnlyList<OrderItem> Items, string? Note);

public record OrderItem(string Sku, int Qty);

[StepBuilder("OrderBuilder")]
public class OrderBuilderConfig
{
    private Guid _customerId;
    private readonly List<OrderItem> _items = new();
    private string? _note;

    public OrderBuilderConfig()
    {
    } // mirrored to generated OrderBuilder()

    [UnorderedStep]
    public void WithNote(string? note)
    {
        _note = note;
    }

    [OrderedStep(StepOrder.One)]
    public void WithCustomer(Guid id)
    {
        _customerId = id;
    }

    [OrderedStep(StepOrder.Two)]
    public void AddItem(string sku, int qty)
    {
        _items.Add(new OrderItem(sku, qty));
    }

    [BuildStep]
    public Order Build()
    {
        return new Order(_customerId, _items, _note);
    }
}

[StepBuilder("EmailBuilder")]
public class EmailBuilderConfig
{
    private readonly string _defaultFrom;
    private string? _from = null;
    private string _to = "";
    private string _body = "";

    public EmailBuilderConfig() : this("noreply@example.com")
    {
    }

    public EmailBuilderConfig(string defaultFrom)
    {
        _defaultFrom = defaultFrom;
        _from = defaultFrom;
    }

    [UnorderedStep]
    public void From(string address)
    {
        _from = address;
    }

    [OrderedStep(StepOrder.One)]
    public void To(string address)
    {
        _to = address;
    }

    [OrderedStep(StepOrder.Two)]
    public void Body(string body)
    {
        _body = body;
    }

    [BuildStep]
    public Email Build()
    {
        return new Email(_from ?? _defaultFrom, _to, _body);
    }
}

public record Email(string From, string To, string Body);

[StepBuilder("GappedBuilder")]
public class GappedBuilderConfig
{
    [OrderedStep(StepOrder.One)]
    public void A()
    {
    }

    [OrderedStep(StepOrder.Three)]
    public void C()
    {
    }

    [BuildStep]
    public string Build()
    {
        return "ok";
    }
}

[StepBuilder]
public class BuilderWithGenerics<TInput> where TInput : struct
{
    [OrderedStep(StepOrder.One)]
    public void WithStep<TStage>(TStage stage)
    {
        /* ... */
    }

    [BuildStep]
    public TInput Build()
    {
        return new TInput();
    }
}