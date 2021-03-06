﻿using System;
using JetBrains.Annotations;

namespace Lykke.Service.LiquidityEngine.Client.Models.PnLStopLossEngines
{
    /// <summary>
    /// Represents a pnl stop loss engine.
    /// </summary>
    [PublicAPI]
    public class PnLStopLossEngineModel
    {
        /// <summary>
        /// Identifier.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Asset pair identifier.
        /// </summary>
        public string AssetPairId { get; set; }

        /// <summary>
        /// PnL stop loss settings identifier (for global setting only).
        /// </summary>
        public string PnLStopLossSettingsId { get; set; }

        /// <summary>
        /// Time interval for calculating loss.
        /// </summary>
        public TimeSpan Interval { get; set; }

        /// <summary>
        /// PnL threshold.
        /// </summary>
        public decimal Threshold { get; set; }

        /// <summary>
        /// Markup.
        /// </summary>
        public decimal Markup { get; set; }

        /// <summary>
        /// Total negative PnL.
        /// </summary>
        public decimal TotalNegativePnL { get; set; }

        /// <summary>
        /// First time when negative PnL occured.
        /// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// Last time when negative PnL occured.
        /// </summary>
        public DateTime? LastTime { get; set; }

        /// <summary>
        /// The mode of the pnl stop loss engine.
        /// </summary>
        public PnLStopLossEngineMode Mode { get; set; }
    }
}
