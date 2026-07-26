using DigiPOSE.Models;
using System.Collections.Generic;

namespace DigiPOSE.Services
{
    /// <summary>
    /// >>> [ENTERPRISE_FISCAL_PRECISION]: O(1) Zero-Latency VAT Rounding & Balancing Engine.
    /// Eliminates financial reporting cent/penny mismatches caused by line-item level tax rounding vs overall gross tax calculations.
    /// </summary>
    public interface IVatBalancingEngine
    {
        /// <summary>
        /// Recomputes line-item tax amounts, reconciles rounding variance into the primary line item per tax rate bracket,
        /// and updates Master Order financial summary totals (Gross, Tax, Discount, Shipping, Change, and Total Amount).
        /// </summary>
        /// <param name="order">Target master order</param>
        /// <param name="details">List of line item details attached to the order</param>
        void BalanceVatAndCalculateTotal(Order order, IList<OrderDetail> details);
    }
}
