//---------------------------------------------------------------------
// <copyright company="Microsoft Corporation" file="AliasBuilder.cs">
//      Created by: tomtan at 7/9/2015 5:26:03 PM
//      Copyright (c) Microsoft Corporation.  All rights reserved.
//      Information Contained Herein is Proprietary and Confidential.
// </copyright>
//---------------------------------------------------------------------

namespace AdsAnswer.AnswerBuilder
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.IO;
    using AdsAnswer;
    using AdsAnswer.Utility;
    using AdsAnswer.Logging;
    using AdsAnswer.AnswerData;
    using AdsAnswer.AnswerData.OdysseyTable;

    public class AliasBuilder
    {
        private Feed feed;
        private HashSet<string> keyList = new HashSet<string>();
        private HashSet<string> keyBlackList = new HashSet<string>();
        private HashSet<string> aliasBlackList = new HashSet<string>();
        private List<string> aliasKPattern = new List<string>();
        private List<string> aliasAPattern = new List<string>();
        private Dictionary<string, AliasKeyword> aliasDict = new Dictionary<string, AliasKeyword>();

        private Action<EventType, string> feedWriteLog;

        public AliasBuilder(Feed feed, Action<EventType, string> writeLog)//, Action<string> addAttachFile)
        {
            this.feed = feed;
            this.feedWriteLog = writeLog;
        }

        private void WriteLog(EventType eventType, string format, params object[] args)
        {
            string message = string.Format(format, args);
            this.feedWriteLog(eventType, message);
        }

        private void ReadAliasFile()
        {
            string inputAliasFile = feed.AliasFile;
            if (File.Exists(inputAliasFile))
            {
                foreach (string line in IOHelper.ExtractLine(inputAliasFile))
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        string[] keyAliasPair = line.Split(new char[] { '\t', '^', ',', '，' }, StringSplitOptions.RemoveEmptyEntries);
                        if (keyAliasPair.Length != 4)
                        {
                            continue;
                        }

                        AliasKeyword value = new AliasKeyword(keyAliasPair[2].Trim(), feed.IntentionScore, feed.ProviderName);
                        float.TryParse(keyAliasPair[3], out value.Score);

                        this.TryAddRecord(keyAliasPair[0].Trim(), value);
                    }
                }
            }
        }

        private void ReadManualAliasFile()
        {
            string manualAliasFile = feed.ManualAliasFile;
            if (!string.IsNullOrWhiteSpace(manualAliasFile) && File.Exists(manualAliasFile))
            {
                string[] lines = File.ReadAllLines(manualAliasFile);
                foreach (string line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        string[] keyAliasPair = line.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);

                        if (keyAliasPair.Length != 2)
                        {
                            continue;
                        }

                        string key = keyAliasPair[0].Trim();
                        if (!keyList.Contains(key.ToLower())) continue;
                        string[] manualAliasList = keyAliasPair[1].Split(new char[] { '^' }, StringSplitOptions.RemoveEmptyEntries);

                        foreach (string alias in manualAliasList)
                        {
                            string alias2 = alias.Trim();
                            if (!string.IsNullOrEmpty(alias2))
                            {
                                this.TryAddRecord(alias2, new AliasKeyword(key, feed.IntentionScore, feed.ProviderName));
                            }
                        }
                    }
                }
            }
        }

        private void ReadPatternFile()
        {
            string patternFile = feed.AliasPatternFile;
            if (!string.IsNullOrWhiteSpace(patternFile) && File.Exists(patternFile))
            {
                string[] lines = File.ReadAllLines(patternFile);
                foreach (string line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        if (line.Contains("[KEY]"))
                            this.aliasKPattern.Add(line.Trim());
                        else if (line.Contains("[ALIAS]"))
                            this.aliasAPattern.Add(line.Trim());
                    }
                }
            }
        }

        private void ExpandAliasPattern()
        {
            ExpandAliasAPattern();
            ExpandAliasKPattern();
        }

        private void ExpandAliasAPattern()
        {
            List<string> aliasList = aliasDict.Keys.ToList();
            foreach(var alias in aliasList)
            {
                foreach(var pattern in aliasAPattern)
                {
                    TryAddRecord(pattern.Replace("[ALIAS]", alias), aliasDict[alias]);
                }
            }
        }

        private void ExpandAliasKPattern()
        {
            foreach (var key in keyList)
            {
                if (keyBlackList.Contains(key)) continue;
                foreach (var pattern in aliasKPattern)
                {
                    TryAddRecord(pattern.Replace("[KEY]", key), new AliasKeyword(key, feed.IntentionScore, feed.ProviderName));
                }
            }
        }

        private void ReadBlackListFile()
        {
            string manualBlackListFile = feed.BlackListFile;
            if (!string.IsNullOrWhiteSpace(manualBlackListFile) && File.Exists(manualBlackListFile))
            {
                string[] lines = File.ReadAllLines(manualBlackListFile);
                foreach (string line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        if(line.StartsWith("[KEY]"))
                            this.keyBlackList.Add(line.Substring("[KEY]".Length).ToLower().Trim());
                        else if(line.StartsWith("[ALIAS]"))
                            this.keyBlackList.Add(line.Substring("[ALIAS]".Length).ToLower().Trim());
                    }
                }
            }
        }

        private void TryAddRecord(string alias, AliasKeyword trv)
        {
            if (aliasBlackList.Contains(alias.ToLower()) || keyBlackList.Contains(trv.Key.ToLower()))
            {
                return;
            }

            if (!keyList.Contains(trv.Key.ToLower()))
            {
                return;
            }

            this.AddRecord(alias, trv);
        }

        private void AddRecord(string alias, AliasKeyword ak)
        {
            if (!aliasDict.ContainsKey(alias.ToLower()))
                aliasDict.Add(alias.ToLower(), ak);
        }
    }
}
