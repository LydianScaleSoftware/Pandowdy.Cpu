// Copyright 2026 Mark D. Long
// Licensed under the Apache License, Version 2.0
// See LICENSE file for details

namespace Pandowdy.UI;

/// <summary>
/// Application-wide constants for the Pandowdy UI layer.
/// </summary>
public static class Constants
{
    /// <summary>
    /// UI refresh and polling frequency constants.
    /// </summary>
    public static class RefreshRates
    {
        /// <summary>
        /// Base refresh ticker frequency in Hz (frames per second).
        /// </summary>
        /// <remarks>
        /// The <see cref="Interfaces.IRefreshTicker"/> runs at this frequency,
        /// providing the base timing for all UI updates. ViewModels can sample
        /// this stream at lower rates if needed.
        /// </remarks>
        public const double BaseTickerHz = 60.0;

        /// <summary>
        /// Base refresh ticker period in milliseconds.
        /// </summary>
        /// <remarks>
        /// Calculated as 1000ms / <see cref="BaseTickerHz"/>.
        /// Approximately 16.67ms for 60 Hz.
        /// </remarks>
        public const double BaseTickerMs = 1000.0 / BaseTickerHz;

        /// <summary>
        /// Standard polling intervals for ViewModels.
        /// </summary>
        public static class Polling
        {
            /// <summary>
            /// 60 Hz polling interval (16.67ms) - matches base ticker rate.
            /// </summary>
            /// <remarks>
            /// Use for high-frequency updates that need to match display refresh rate.
            /// No sampling overhead since this matches the base ticker frequency.
            /// </remarks>
            public const double HighFrequencyMs = BaseTickerMs;

            /// <summary>
            /// 30 Hz polling interval (33ms) - half the base ticker rate.
            /// </summary>
            /// <remarks>
            /// Good balance between smoothness and performance for most status displays.
            /// </remarks>
            public const double MediumFrequencyMs = 1000.0 / 30.0;

            /// <summary>
            /// 20 Hz polling interval (50ms) - one-third the base ticker rate.
            /// </summary>
            /// <remarks>
            /// Efficient for non-critical status displays where 20 updates/second is sufficient.
            /// </remarks>
            public const double LowFrequencyMs = 1000.0 / 20.0;

            /// <summary>
            /// 10 Hz polling interval (100ms) - one-sixth the base ticker rate.
            /// </summary>
            /// <remarks>
            /// Use for infrequently changing data or performance-critical scenarios.
            /// </remarks>
            public const double VeryLowFrequencyMs = 1000.0 / 10.0;
        }
    }
}
