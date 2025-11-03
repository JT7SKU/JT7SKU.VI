// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Solutions.Testing.Mocks;
using BotSkill.Tests.Utterances;

namespace BotSkill.Tests.Utilities
{
    public class SkillTestUtil
    {
        private static Dictionary<string, IRecognizerConvert> _utterances = new Dictionary<string, IRecognizerConvert>
        {
            { SampleDialogUtterances.Trigger, CreateIntent(SampleDialogUtterances.Trigger, BotSkillLuis.Intent.Sample) },
        };

        public static MockLuisRecognizer CreateRecognizer()
        {
            var recognizer = new MockLuisRecognizer(defaultIntent: CreateIntent(string.Empty, BotSkillLuis.Intent.None));
            recognizer.RegisterUtterances(_utterances);
            return recognizer;
        }

        public static BotSkillLuis CreateIntent(string userInput, BotSkillLuis.Intent intent)
        {
            var result = new BotSkillLuis
            {
                Text = userInput,
                Intents = new Dictionary<BotSkillLuis.Intent, IntentScore>()
            };

            result.Intents.Add(intent, new IntentScore() { Score = 0.9 });

            result.Entities = new BotSkillLuis._Entities
            {
                _instance = new BotSkillLuis._Entities._Instance()
            };

            return result;
        }
    }
}
