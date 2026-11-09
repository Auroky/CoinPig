using System;
using System.Collections.Generic;
using System.Linq;

public readonly record struct RentPayment(int Due, int Paid, int Shortfall);

/// <summary>Tracks the outstanding bills and advances only after a full payment.</summary>
public sealed class RentContract
{
    private readonly List<Bill> _bills;
    private readonly int _contractBills;
    private readonly IRentContinuation? _continuation;
    public int PaidCount { get; private set; }
    public bool ContractPaid => PaidCount >= _contractBills;
    public Bill? Next
    {
        get { Ensure(PaidCount); return PaidCount < _bills.Count ? _bills[PaidCount] : null; }
    }

    public RentContract(IReadOnlyList<Bill> bills, IRentContinuation? continuation = null)
    {
        ArgumentNullException.ThrowIfNull(bills);
        if (bills.Any(bill => bill == null || bill.Shift <= 0 || bill.Amount < 0) ||
            bills.Zip(bills.Skip(1), (a, b) => a.Shift >= b.Shift).Any(invalid => invalid))
            throw new ArgumentException("Bills must have increasing positive days and nonnegative amounts.", nameof(bills));
        _bills = bills.ToList(); _contractBills = bills.Count; _continuation = continuation;
    }
    private void Ensure(int index)
    {
        while (_bills.Count <= index && _bills.Count > 0 && _continuation != null)
        {
            var previous = _bills[^1];
            var next = _continuation.NextAfter(previous);
            if (next == null) break;
            if (next.Shift <= previous.Shift || next.Amount < 0)
                throw new InvalidOperationException("Renewal rules must return a later bill with a nonnegative amount.");
            _bills.Add(next);
        }
    }

    public RentPayment Settle(int day, int balance)
    {
        if (day <= 0) throw new ArgumentOutOfRangeException(nameof(day));
        if (balance < 0) throw new ArgumentOutOfRangeException(nameof(balance));
        int due = Next is Bill bill && bill.Shift <= day ? bill.Amount : 0;
        int shortfall = Math.Max(0, due - balance);
        if (Next is Bill next && next.Shift <= day && shortfall == 0)
        {
            PaidCount++;
            return new(due, due, 0);
        }
        return new(due, 0, shortfall);
    }

    public IReadOnlyList<RentDue> Schedule(int day, bool failed)
    {
        Ensure(checked(PaidCount + 2));
        return _bills.Select((bill, index) => new RentDue(bill.Shift, bill.Amount, index < PaidCount,
            Math.Max(0, bill.Shift - day), failed && index == PaidCount, index >= _contractBills)).ToArray();
    }
}
