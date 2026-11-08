using System;
using System.Collections.Generic;
using System.Linq;

public readonly record struct RentPayment(int Due, int Paid, int Shortfall);

/// <summary>Tracks the outstanding bills and advances only after a full payment.</summary>
public sealed class RentContract
{
    private readonly IReadOnlyList<Bill> _bills;
    public int PaidCount { get; private set; }
    public Bill? Next => PaidCount < _bills.Count ? _bills[PaidCount] : null;

    public RentContract(IReadOnlyList<Bill> bills)
    {
        ArgumentNullException.ThrowIfNull(bills);
        if (bills.Any(bill => bill.Shift <= 0 || bill.Amount < 0) ||
            bills.Zip(bills.Skip(1), (a, b) => a.Shift >= b.Shift).Any(invalid => invalid))
            throw new ArgumentException("Bills must have increasing positive days and nonnegative amounts.", nameof(bills));
        _bills = Array.AsReadOnly(bills.ToArray());
    }

    public RentPayment Settle(int day, int balance)
    {
        int due = Next is Bill bill && bill.Shift <= day ? bill.Amount : 0;
        int shortfall = Math.Max(0, due - balance);
        if (Next is Bill next && next.Shift <= day && shortfall == 0)
        {
            PaidCount++;
            return new(due, due, 0);
        }
        return new(due, 0, shortfall);
    }

    public IReadOnlyList<RentDue> Schedule(int day, bool failed) => _bills.Select((bill, index) =>
        new RentDue(bill.Shift, bill.Amount, index < PaidCount, Math.Max(0, bill.Shift - day),
            failed && index == PaidCount)).ToArray();
}
