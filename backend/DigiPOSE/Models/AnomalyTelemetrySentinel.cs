using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace DigiPOSE.Models
{
    public record TelemetryHazardEvent(string EventId, string Severity, string HazardType, string Description, string Timestamp, string LocationContext);

    // >>> [IoT / HARDWARE SENSOR ENDPOINT TELEMETRY SENTINEL]
    // Captures anomalous operational patterns (Blind Close discrepancies, Stale Shift bypass attempts, CRC hash invalidation)
    // and streams them directly to Administrator Command Center with CRITICAL HAZARD alerts.
    public static class AnomalyTelemetrySentinel
    {
        private static readonly ConcurrentQueue<TelemetryHazardEvent> _hazards = new();
        private const int MaxEvents = 100;

        public static void RecordHazard(string hazardType, string description, string severity = "CRITICAL HAZARD", string locationContext = "POS Terminal / Active Shift")
        {
            var evt = new TelemetryHazardEvent(
                Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                severity,
                hazardType,
                description,
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                locationContext
            );

            _hazards.Enqueue(evt);

            // Keep memory bounded to last 100 critical events in O(1) space
            while (_hazards.Count > MaxEvents && _hazards.TryDequeue(out _)) { }
        }

        public static IReadOnlyList<TelemetryHazardEvent> GetRecentHazards(int limit = 20)
        {
            return _hazards.Reverse().Take(limit).ToList().AsReadOnly();
        }
    }
}
