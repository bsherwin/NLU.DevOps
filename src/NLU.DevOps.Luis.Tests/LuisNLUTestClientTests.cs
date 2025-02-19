﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace NLU.DevOps.Luis.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using FluentAssertions.Json;
    using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
    using Models;
    using Moq;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;

    [TestFixture]
    internal static class LuisNLUTestClientTests
    {
        [Test]
        public static void ThrowsArgumentNull()
        {
            Action nullLuisSettings = () => new LuisNLUTestClient(null, default(ILuisTestClient));
            Action nullLuisClient = () => new LuisNLUTestClient(new LuisSettings(), null);
            nullLuisSettings.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("luisSettings");
            nullLuisClient.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("luisClient");

            using (var luis = new LuisNLUTestClientBuilder().Build())
            {
                Func<Task> nullTestUtterance = () => luis.TestAsync(default(JToken));
                Func<Task> nullTestSpeechUtterance = () => luis.TestSpeechAsync(null);
                nullTestUtterance.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("query");
                nullTestSpeechUtterance.Should().Throw<ArgumentException>().And.ParamName.Should().Be("speechFile");
            }
        }

        [Test]
        public static async Task TestModel()
        {
            var test = "the quick brown fox jumped over the lazy dog";

            var builder = new LuisNLUTestClientBuilder();
            builder.LuisTestClientMock
                .Setup(luis => luis.QueryAsync(
                    It.Is<string>(query => query == test),
                    It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(new LuisResult
                {
                    Query = test,
                    TopScoringIntent = new IntentModel { Intent = "intent" },
                    Entities = new[]
                    {
                        new EntityModel
                        {
                            Entity = "the",
                            Type = "type",
                            StartIndex = 32,
                            EndIndex = 34,
                        },
                    },
                }));

            using (var luis = builder.Build())
            {
                var result = await luis.TestAsync(test).ConfigureAwait(false);
                result.Text.Should().Be(test);
                result.Intent.Should().Be("intent");
                result.Entities.Count.Should().Be(1);
                result.Entities[0].EntityType.Should().Be("type");
                result.Entities[0].EntityValue.Should().BeNull();
                result.Entities[0].MatchText.Should().Be("the");
                result.Entities[0].MatchIndex.Should().Be(1);
            }
        }

        [Test]
        public static async Task TestModelWithEntityResolution()
        {
            var test = "the quick brown fox jumped over the lazy dog";
            var valueResolution = new JObject { { "value", "THE" } };
            var valuesStringResolution = new JObject { { "values", new JArray { "Fox" } } };
            var valuesValueResolution = new JObject
            {
                { "values", new JArray { new JObject { { "value", "2018-11-16" } } } },
            };

            var builder = new LuisNLUTestClientBuilder();
            builder.LuisTestClientMock
                .Setup(luis => luis.QueryAsync(
                    It.Is<string>(query => query == test),
                    It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(new LuisResult
                {
                    Query = "the quick brown fox jumped over the lazy dog today",
                    Entities = new[]
                    {
                        new EntityModel
                        {
                            Entity = "the",
                            StartIndex = 0,
                            EndIndex = 2,
                            AdditionalProperties = new Dictionary<string, object>
                            {
                                { "resolution", valueResolution.DeepClone() },
                            },
                        },
                        new EntityModel
                        {
                            Entity = "brown fox",
                            StartIndex = 10,
                            EndIndex = 18,
                            AdditionalProperties = new Dictionary<string, object>
                            {
                                { "resolution", valuesStringResolution.DeepClone() },
                            },
                        },
                        new EntityModel
                        {
                            Entity = "today",
                            StartIndex = 45,
                            EndIndex = 49,
                            AdditionalProperties = new Dictionary<string, object>
                            {
                                { "resolution", valuesValueResolution.DeepClone() },
                            },
                        },
                    }
                }));

            using (var luis = builder.Build())
            {
                var result = await luis.TestAsync(test).ConfigureAwait(false);
                result.Entities.Count.Should().Be(3);
                result.Entities[0].EntityValue.Should().BeEquivalentTo(valueResolution["value"]);
                result.Entities[1].EntityValue.Should().BeEquivalentTo(valuesStringResolution["values"]);
                result.Entities[2].EntityValue.Should().BeEquivalentTo(valuesValueResolution["values"]);
            }
        }

        [Test]
        public static async Task TestSpeech()
        {
            var testFile = "somefile";
            var test = "the quick brown fox jumped over the lazy dog entity";

            var builder = new LuisNLUTestClientBuilder();
            builder.LuisTestClientMock
                .Setup(luis => luis.RecognizeSpeechAsync(
                    It.Is<string>(speechFile => speechFile == testFile),
                    It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(new SpeechLuisResult(
                    new LuisResult
                    {
                        Query = test,
                        TopScoringIntent = new IntentModel { Intent = "intent" },
                        Entities = new[]
                        {
                            new EntityModel
                            {
                                Entity = "entity",
                                Type = "type",
                                StartIndex = 45,
                                EndIndex = 50,
                            },
                        },
                    },
                    0)));

            using (var luis = builder.Build())
            {
                var result = await luis.TestSpeechAsync(testFile).ConfigureAwait(false);
                result.Text.Should().Be(test);
                result.Intent.Should().Be("intent");
                result.Entities.Count.Should().Be(1);
                result.Entities[0].EntityType.Should().Be("type");

                result.Entities[0].MatchText.Should().Be("entity");
                result.Entities[0].MatchIndex.Should().Be(0);
            }
        }

        [Test]
        public static async Task TestSpeechWithTextScore()
        {
            var testFile = "somefile";
            var test = "the quick brown fox jumped over the lazy dog entity";

            var builder = new LuisNLUTestClientBuilder();
            builder.LuisTestClientMock
                .Setup(luis => luis.RecognizeSpeechAsync(
                    It.Is<string>(speechFile => speechFile == testFile),
                    It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(new SpeechLuisResult(
                    new LuisResult
                    {
                        Query = test,
                        TopScoringIntent = new IntentModel { Intent = "intent" },
                    },
                    0.5)));

            using (var luis = builder.Build())
            {
                var result = await luis.TestSpeechAsync(testFile).ConfigureAwait(false);
                result.Text.Should().Be(test);
                result.Intent.Should().Be("intent");
                result.As<ScoredLabeledUtterance>().TextScore.Should().Be(0.5);
                result.As<ScoredLabeledUtterance>().Score.Should().Be(0);
            }
        }

        [Test]
        public static async Task TestWithPrebuiltEntity()
        {
            var test = "the quick brown fox jumped over the lazy dog";
            var builtinType = Guid.NewGuid().ToString();

            var builder = new LuisNLUTestClientBuilder();
            var prebuiltEntityTypes = new Dictionary<string, string>
            {
                { "type", "test" },
            };

            builder.LuisSettings = new LuisSettings(prebuiltEntityTypes);

            builder.LuisTestClientMock
                .Setup(luis => luis.QueryAsync(
                    It.Is<string>(query => query == test),
                    It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(new LuisResult
                {
                    Query = test,
                    TopScoringIntent = new IntentModel { Intent = "intent" },
                    Entities = new[]
                    {
                        new EntityModel
                        {
                            Entity = "the",
                            Type = "builtin.test",
                            StartIndex = 32,
                            EndIndex = 34
                        },
                    },
                }));

            using (var luis = builder.Build())
            {
                var result = await luis.TestAsync(test).ConfigureAwait(false);
                result.Text.Should().Be(test);
                result.Intent.Should().Be("intent");
                result.Entities.Count.Should().Be(1);
                result.Entities[0].EntityType.Should().Be("type");
                result.Entities[0].EntityValue.Should().BeNull();
                result.Entities[0].MatchText.Should().Be("the");
                result.Entities[0].MatchIndex.Should().Be(1);
            }
        }

        [Test]
        public static async Task TestSpeechAsyncNoMatchResponse()
        {
            var utterance = Guid.NewGuid().ToString();
            var builder = new LuisNLUTestClientBuilder();
            using (var luis = builder.Build())
            {
                var results = await luis.TestSpeechAsync(utterance).ConfigureAwait(false);
                results.Intent.Should().BeNull();
                results.Text.Should().BeNull();
                results.Entities.Should().BeNull();
            }
        }

        [Test]
        public static async Task NoLabeledIntentScore()
        {
            var test = "the quick brown fox jumped over the lazy dog";

            var builder = new LuisNLUTestClientBuilder();
            builder.LuisTestClientMock
                .Setup(luis => luis.QueryAsync(
                    It.Is<string>(query => query == test),
                    It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(new LuisResult
                {
                    Query = test,
                    TopScoringIntent = new IntentModel { Intent = "intent" },
                }));

            using (var luis = builder.Build())
            {
                var result = await luis.TestAsync(test).ConfigureAwait(false);
                result.Should().BeOfType(typeof(Models.LabeledUtterance));
            }
        }

        [Test]
        public static async Task WithLabeledIntentScore()
        {
            var test = "the quick brown fox jumped over the lazy dog";

            var builder = new LuisNLUTestClientBuilder();
            builder.LuisTestClientMock
                .Setup(luis => luis.QueryAsync(
                    It.Is<string>(query => query == test),
                    It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(new LuisResult
                {
                    Query = test,
                    TopScoringIntent = new IntentModel { Intent = "intent", Score = 0.42 },
                }));

            using (var luis = builder.Build())
            {
                var result = await luis.TestAsync(test).ConfigureAwait(false);
                result.Should().BeOfType(typeof(ScoredLabeledUtterance));
                result.As<ScoredLabeledUtterance>().Score.Should().Be(0.42);
            }
        }

        [Test]
        public static async Task NoEntityScore()
        {
            var test = "the quick brown fox jumped over the lazy dog";

            var builder = new LuisNLUTestClientBuilder();
            builder.LuisTestClientMock
                .Setup(luis => luis.QueryAsync(
                    It.Is<string>(query => query == test),
                    It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(new LuisResult
                {
                    Query = test,
                    TopScoringIntent = new IntentModel { Intent = "intent" },
                    Entities = new[]
                    {
                        new EntityModel
                        {
                            Entity = "the",
                            Type = "type",
                            StartIndex = 32,
                            EndIndex = 34,
                        },
                    },
                }));

            using (var luis = builder.Build())
            {
                var result = await luis.TestAsync(test).ConfigureAwait(false);
                result.Entities.Count.Should().Be(1);
                result.Entities[0].Should().BeOfType(typeof(Entity));
            }
        }

        [Test]
        public static async Task WithEntityScore()
        {
            var test = "the quick brown fox jumped over the lazy dog";

            var builder = new LuisNLUTestClientBuilder();
            builder.LuisTestClientMock
                .Setup(luis => luis.QueryAsync(
                    It.Is<string>(query => query == test),
                    It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(new LuisResult
                {
                    Query = test,
                    TopScoringIntent = new IntentModel { Intent = "intent" },
                    Entities = new[]
                    {
                        new EntityModel
                        {
                            Entity = "the",
                            Type = "type",
                            StartIndex = 32,
                            EndIndex = 34,
                            AdditionalProperties = new Dictionary<string, object>
                            {
                                { "score", 0.42 },
                            },
                        },
                    },
                }));

            using (var luis = builder.Build())
            {
                var result = await luis.TestAsync(test).ConfigureAwait(false);
                result.Entities.Count.Should().Be(1);
                result.Entities[0].Should().BeOfType(typeof(ScoredEntity));
                result.Entities[0].As<ScoredEntity>().Score.Should().Be(0.42);
            }
        }

        private class LuisNLUTestClientBuilder
        {
            public LuisSettings LuisSettings { get; set; } = new LuisSettings(null, null);

            public Mock<ILuisTestClient> LuisTestClientMock { get; } = new Mock<ILuisTestClient>();

            public LuisNLUTestClient Build() =>
                new LuisNLUTestClient(this.LuisSettings, this.LuisTestClientMock.Object);
        }
    }
}
