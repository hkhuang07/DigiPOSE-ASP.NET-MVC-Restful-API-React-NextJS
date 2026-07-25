// ============================================================================
// [DIGIPOSE HUD UTILITY FORMATTERS - CYBER-CINEMATIC STANDARDS]
// ============================================================================

/**
 * Formats decimal currency values into scannable high-density financial text.
 * Example: 24,500,000 VND
 */
export function formatCurrency(amount: number): string {
  if (isNaN(amount)) return "0.00 VND";
  return `${amount.toLocaleString("en-US", {
    minimumFractionDigits: 0,
    maximumFractionDigits: 2,
  })} VND`;
}

/**
 * Formats numeric quantities with leading zeros or standard separators for HUD scannability.
 */
export function formatQty(quantity: number): string {
  return quantity.toLocaleString("en-US");
}

/**
 * Generates technical timestamp string in military/lab syntax: YYYY.MM.DD // HH:mm:ss.SS
 */
export function getHudTimestamp(): string {
  const now = new Date();
  const year = now.getFullYear();
  const month = String(now.getMonth() + 1).padStart(2, "0");
  const day = String(now.getDate()).padStart(2, "0");
  const hours = String(now.getHours()).padStart(2, "0");
  const minutes = String(now.getMinutes()).padStart(2, "0");
  const seconds = String(now.getSeconds()).padStart(2, "0");
  const millis = String(now.getMilliseconds()).padStart(3, "0").substring(0, 2);

  return `${year}.${month}.${day} // ${hours}:${minutes}:${seconds}.${millis}`;
}

/**
 * Calculates percentage of limit or stock level for segmented progress rendering.
 */
export function getSegmentedBar(current: number, total: number = 10): string {
  const filled = Math.min(total, Math.max(0, Math.round((current / 100) * total)));
  const empty = total - filled;
  return `[${"█".repeat(filled)}${"░".repeat(empty)}]`;
}
