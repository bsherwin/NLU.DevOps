﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.ModelPerformance
{
    using System.Collections.Generic;

    /// <summary>
    /// NLU statistics.
    /// </summary>
    public class NLUStatistics
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NLUStatistics"/> class.
        /// </summary>
        /// <param name="text">Text confusion matrix.</param>
        /// <param name="intent">Intent confusion matrix.</param>
        /// <param name="entity">Entity confusion matrix.</param>
        /// <param name="entityValue">Entity value confusion matrix.</param>
        /// <param name="byIntent">By intent confusion matrix.</param>
        /// <param name="byEntityType">By entity type confusion matrix.</param>
        /// <param name="byEntityValueType">By entity value type confusion matrix.</param>
        public NLUStatistics(
            ConfusionMatrix text,
            ConfusionMatrix intent,
            ConfusionMatrix entity,
            ConfusionMatrix entityValue,
            IReadOnlyDictionary<string, ConfusionMatrix> byIntent,
            IReadOnlyDictionary<string, ConfusionMatrix> byEntityType,
            IReadOnlyDictionary<string, ConfusionMatrix> byEntityValueType)
        {
            this.Text = text;
            this.Intent = intent;
            this.Entity = entity;
            this.EntityValue = entityValue;
            this.ByIntent = byIntent;
            this.ByEntityType = byEntityType;
            this.ByEntityValueType = byEntityValueType;
        }

        /// <summary>
        /// Gets the text confusion matrix.
        /// </summary>
        public ConfusionMatrix Text { get; }

        /// <summary>
        /// Gets the intent confusion matrix.
        /// </summary>
        public ConfusionMatrix Intent { get; }

        /// <summary>
        /// Gets the entity confusion matrix.
        /// </summary>
        public ConfusionMatrix Entity { get; }

        /// <summary>
        /// Gets the entity value confusion matrix.
        /// </summary>
        public ConfusionMatrix EntityValue { get; }

        /// <summary>
        /// Gets the intent confusion matrix by intent.
        /// </summary>
        public IReadOnlyDictionary<string, ConfusionMatrix> ByIntent { get; }

        /// <summary>
        /// Gets the entity confusion matrix by entity type.
        /// </summary>
        public IReadOnlyDictionary<string, ConfusionMatrix> ByEntityType { get; }

        /// <summary>
        /// Gets the entity value confusion matrix by entity type.
        /// </summary>
        public IReadOnlyDictionary<string, ConfusionMatrix> ByEntityValueType { get; }
    }
}
