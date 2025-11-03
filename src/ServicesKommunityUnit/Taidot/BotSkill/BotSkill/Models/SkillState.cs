// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Luis;

namespace BotSkill.Models
{
    public class SkillState
    {
        public string Token { get; internal set; }

        public BotSkillLuis LuisResult { get; internal set; }

        public void Clear()
        {
        }
    }
}
