using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 租金配置：在哪一局收工时支付多少金币。
/// </summary>
public sealed record Bill(int Shift, int Amount);

/// <summary>
/// 账单展示快照，包含已付状态、剩余天数、失败标记及是否属于续租。
/// </summary>
public sealed record RentDue(int Day, int Amount, bool Paid, int DaysAway, bool Failed, bool Renewal = false);

/// <summary>
/// 一次租金尝试的应付、实付和缺口；余额不足时不部分扣款。
/// </summary>
public readonly record struct RentPayment(int Due, int Paid, int Shortfall);

/// <summary>
/// 保存账单与已付进度，仅完整支付才推进。默认主线的终点由内容配置决定。
/// </summary>
public sealed class RentContract
{
    private readonly List<Bill> _bills;
    private readonly int _contractBills;
    private readonly IRentContinuation? _continuation;

    public int PaidCount { get; private set; }
    public bool ContractPaid => PaidCount >= _contractBills;
    public Bill? Next
    {
        get
        {
            Ensure(PaidCount);
            return PaidCount < _bills.Count ? _bills[PaidCount] : null;
        }
    }

    public RentContract(IReadOnlyList<Bill> bills, IRentContinuation? continuation = null)
    {
        ArgumentNullException.ThrowIfNull(bills);
        if (bills.Any(bill => bill == null || bill.Shift <= 0 || bill.Amount < 0) ||
            bills.Zip(bills.Skip(1), (a, b) => a.Shift >= b.Shift).Any(invalid => invalid))
        {
            throw new ArgumentException("Bills must have increasing positive days and nonnegative amounts.", nameof(bills));
        }

        _bills = bills.ToList();
        _contractBills = bills.Count;
        _continuation = continuation;
    }

    private void Ensure(int index)
    {
        while (_bills.Count <= index && _bills.Count > 0 && _continuation != null)
        {
            var previous = _bills[^1];
            var next = _continuation.NextAfter(previous);
            if (next == null)
            {
                break;
            }

            if (next.Shift <= previous.Shift || next.Amount < 0)
            {
                throw new InvalidOperationException("Renewal rules must return a later bill with a nonnegative amount.");
            }

            _bills.Add(next);
        }
    }

    public RentPayment Settle(int day, int balance)
    {
        if (day <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(day));
        }

        if (balance < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(balance));
        }

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
