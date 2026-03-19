using AlbionP2P.Domain.Aggregates;
using AlbionP2P.Domain.Events;
using AlbionP2P.Domain.Exceptions;
using AlbionP2P.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace AlbionP2P.Domain.Tests;

public class OrderTests
{
    static Order Valid(OrderType t = OrderType.Sell) =>
        new("user-1", "T8 Sword", ItemCategory.Weapon, 1, new Money(10_000_000), t, ServerRegion.Americas);

    [Fact] public void Create_OpenStatus()
        => Valid().Status.IsOpen.Should().BeTrue();

    [Fact] public void Create_ZeroQuantity_Throws()
        => ((Action)(() => new Order("u", "x", ItemCategory.Weapon, 0, new Money(1), OrderType.Sell, ServerRegion.Europe)))
           .Should().Throw<DomainException>().WithMessage("*quantidade*");

    [Fact] public void StartNegotiation_FromOpen_OK()
    { var o = Valid(); o.StartNegotiation(); o.Status.IsInNegotiation.Should().BeTrue(); }

    [Fact] public void StartNegotiation_FromClosed_Throws()
    { var o = Valid(); o.Close(); ((Action)o.StartNegotiation).Should().Throw<DomainException>(); }

    [Fact] public void Close_RaisesEvent()
    { var o = Valid(); o.Close(); o.DomainEvents.Should().ContainSingle(e => e is OrderClosedEvent); }

    [Fact] public void Cancel_ByNonOwner_Throws()
        => ((Action)(() => Valid().Cancel("other"))).Should().Throw<DomainException>().WithMessage("*criador*");

    [Fact] public void Cancel_ByOwner_Cancelled()
    { var o = Valid(); o.Cancel("user-1"); o.Status.Value.Should().Be(OrderStatusValue.Cancelled); }
}

public class DealTests
{
    static Deal Valid() => new(Guid.NewGuid(), "buyer-1", "seller-1", new Money(9_500_000));

    [Fact] public void Create_SameBuyerSeller_Throws()
        => ((Action)(() => new Deal(Guid.NewGuid(), "u", "u", new Money(1)))).Should().Throw<DomainException>().WithMessage("*mesmo usuário*");

    [Fact] public void Accept_BySeller_OK()
    { var d = Valid(); d.AcceptBySeller(); d.Status.Should().Be(DealStatusValue.SellerAccepted); }

    [Fact] public void Accept_ByBuyer_AfterSeller_OK()
    { var d = Valid(); d.AcceptBySeller(); d.AcceptByBuyer(); d.Status.Should().Be(DealStatusValue.BothAccepted); }

    [Fact] public void Accept_ByBuyer_BeforeSeller_Throws()
        => ((Action)(() => Valid().AcceptByBuyer())).Should().Throw<DomainException>().WithMessage("*vendedor*");

    [Fact] public void Complete_AfterBothAccept_RaisesEvent()
    { var d = Valid(); d.AcceptBySeller(); d.AcceptByBuyer(); d.Complete(); d.DomainEvents.Should().ContainSingle(e => e is DealCompletedEvent); }

    [Fact] public void AddMessage_ByParticipant_OK()
    { var d = Valid(); d.AcceptBySeller(); d.AddMessage("buyer-1", "Topei!"); d.Messages.Should().HaveCount(1); }

    [Fact] public void AddMessage_ByStranger_Throws()
        => ((Action)(() => Valid().AddMessage("x", "oi"))).Should().Throw<DomainException>().WithMessage("*participantes*");
}

public class MoneyTests
{
    [Fact] public void Negative_Throws()
        => ((Action)(() => new Money(-1))).Should().Throw<DomainException>().WithMessage("*negativo*");

    [Fact] public void Currency_IsSilver()
        => new Money(1000).Currency.Should().Be("Silver");
}
