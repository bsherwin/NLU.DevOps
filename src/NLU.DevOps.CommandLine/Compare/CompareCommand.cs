﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.CommandLine.Compare
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using ModelPerformance;
    using Models;
    using NUnitLite;
    using static Serializer;

    internal static class CompareCommand
    {
        private const string TestMetadataFileName = "metadata.json";
        private const string TestStatisticsFileName = "statistics.json";

        public static int Run(CompareOptions options)
        {
            var parameters = CreateParameters(
                (ConfigurationConstants.ExpectedUtterancesPathKey, options.ExpectedUtterancesPath),
                (ConfigurationConstants.ActualUtterancesPathKey, options.ActualUtterancesPath),
                (ConfigurationConstants.CompareTextKey, options.CompareText.ToString(CultureInfo.InvariantCulture)),
                (ConfigurationConstants.TestLabelKey, options.TestLabel));

            var arguments = new List<string> { $"-p:{parameters}" };
            if (options.OutputFolder != null)
            {
                arguments.Add($"--work={options.OutputFolder}");
            }

            if (options.Metadata)
            {
                var expectedUtterances = Read<List<LabeledUtterance>>(options.ExpectedUtterancesPath);
                var actualUtterances = Read<List<ScoredLabeledUtterance>>(options.ActualUtterancesPath);
                var compareResults = TestCaseSource.GetNLUCompareResults(expectedUtterances, actualUtterances, options.CompareText);
                var metadataPath = options.OutputFolder != null ? Path.Combine(options.OutputFolder, TestMetadataFileName) : TestMetadataFileName;
                var statisticsPath = options.OutputFolder != null ? Path.Combine(options.OutputFolder, TestStatisticsFileName) : TestStatisticsFileName;
                Write(metadataPath, compareResults.TestCases);
                Write(statisticsPath, compareResults.Statistics);
            }

            new AutoRun(typeof(ConfigurationConstants).Assembly).Execute(arguments.ToArray());

            // We don't care if there are any failing NUnit tests
            return 0;
        }

        private static string CreateParameters(params (string, string)[] parameters)
        {
            var filteredParameters = parameters
                .Where(p => p.Item2 != null)
                .Select(p => $"{p.Item1}={p.Item2}");

            return string.Join(';', filteredParameters);
        }
    }
}
