using DigiPOSE.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DigiPOSE.Services
{
    /// <summary>
    /// >>> [ENTERPRISE_FISCAL_ENGINE]: Production-grade VAT Balancing implementation adapted from Java reference architecture.
    /// Guarantees that the sum of line-item rounded VAT equals exact mathematical tax on gross totals.
    /// </summary>
    public class VatBalancingEngine : IVatBalancingEngine
    {
        public void BalanceVatAndCalculateTotal(Order order, IList<OrderDetail> details)
        {
            if (details == null || !details.Any())
            {
                order.GrossAmount = 0;
                order.TaxAmount = 0;
                order.DiscountAmount = 0;
                order.TotalAmount = order.ShippingFee;
                order.VatRoundingDifference = 0;
                if (order.TenderedAmount > 0)
                {
                    order.ChangeAmount = Math.Max(0, order.TenderedAmount - order.TotalAmount);
                }
                return;
            }

            order.VatRoundingDifference = 0;

            // Step 1: Compute initial individual line tax and reset tax balances
            foreach (var d in details)
            {
                decimal rawPreTax = (d.Quantity * d.UnitPrice) - d.DiscountAmount;
                d.TaxBalance = 0;
                d.TaxAmount = Math.Round(rawPreTax * (d.TaxRate / 100.0m), 2, MidpointRounding.AwayFromZero);
            }

            // Step 2: Group by VAT Rate and calculate rounding differences
            var taxGroups = details.GroupBy(d => Math.Round(d.TaxRate, 2)).ToList();

            foreach (var group in taxGroups)
            {
                decimal groupTaxRate = group.Key;
                if (groupTaxRate == 0) continue;

                // Exact theoretical tax on group's total taxable amount
                decimal totalTaxableInGroup = group.Sum(d => (d.Quantity * d.UnitPrice) - d.DiscountAmount);
                decimal theoreticalGroupTax = Math.Round(totalTaxableInGroup * (groupTaxRate / 100.0m), 2, MidpointRounding.AwayFromZero);

                // Actual sum of line-item taxes
                decimal currentSumLineTax = group.Sum(d => d.TaxAmount);

                // Compute variance (penny/cent difference)
                decimal diff = theoreticalGroupTax - currentSumLineTax;

                if (diff != 0)
                {
                    // Step 3: Assign full variance to the highest value line item in this tax bracket (deterministic balancing)
                    var primaryDetail = group.OrderByDescending(d => (d.Quantity * d.UnitPrice) - d.DiscountAmount)
                                             .ThenBy(d => d.ProductId)
                                             .FirstOrDefault();

                    if (primaryDetail != null)
                    {
                        primaryDetail.TaxBalance = diff;
                        primaryDetail.TaxAmount += diff;
                        order.VatRoundingDifference += diff;
                    }
                }
            }

            // Step 4: Re-evaluate net prices and total line amounts
            foreach (var d in details)
            {
                decimal preTax = (d.Quantity * d.UnitPrice) - d.DiscountAmount;
                d.TotalAmount = preTax + d.TaxAmount;
                if (d.Quantity > 0)
                {
                    d.NetPrice = Math.Round((d.TotalAmount - d.TaxAmount) / d.Quantity, 4, MidpointRounding.AwayFromZero);
                }
                else
                {
                    d.NetPrice = d.UnitPrice;
                }
            }

            // Step 5: Aggregate master totals
            order.GrossAmount = details.Sum(d => d.Quantity * d.UnitPrice);
            order.DiscountAmount = details.Sum(d => d.DiscountAmount);
            order.TaxAmount = details.Sum(d => d.TaxAmount);
            order.TotalAmount = details.Sum(d => d.TotalAmount) + order.ShippingFee;

            // Step 6: Automate cashier change amount settlement
            if (order.TenderedAmount > 0)
            {
                order.ChangeAmount = Math.Max(0, order.TenderedAmount - order.TotalAmount);
            }
            else
            {
                order.ChangeAmount = 0;
            }
        }
    }
}
