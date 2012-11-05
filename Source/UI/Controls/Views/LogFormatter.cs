﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sanguosha.Core.Players;
using System.Windows;
using Sanguosha.Core.UI;
using Sanguosha.Core.Skills;
using System.Diagnostics;
using System.Windows.Documents;
using Sanguosha.Core.Cards;
using System.Windows.Controls;
using System.Windows.Media;

namespace Sanguosha.UI.Controls
{
    public class LogFormatter
    {
        public static string Translate(Player player)
        {
            if (player == null) return string.Empty;
            if (player.Hero == null)
            {
                return player.UserName ?? string.Empty;
            }
            string key = string.Format("Hero.{0}.Name", player.Hero.Name);
            string name = Application.Current.TryFindResource(key) as string;
            if (name == null) return string.Empty;
            return name;
        }

        public static string Translate(ISkill skill)
        {
            if (skill == null) return string.Empty;
            string key = string.Format("Skill.{0}.Name", skill.GetType().Name);
            string name = Application.Current.TryFindResource(key) as string;
            if (name == null) return string.Empty;
            return name;
        }

        public static string TranslateCardFootnote(ActionLog log)
        {
            string source = Translate(log.Source);
            string dest = string.Empty;
            Trace.Assert(log.Targets != null);
            if (log.Targets != null && log.Targets.Count == 1)
            {
                dest = "对" + Translate(log.Targets[0]);
            }
            string skill = Translate(log.SkillAction);
            string formatter = source;
            switch(log.GameAction)
            {
                case GameAction.None:
                    return formatter;
                case GameAction.Use:
                    return string.Format("{0}{1}{2}", source, dest, skill);
                case GameAction.Play:
                    return string.Format("{0}{2}{1}打出", source, dest, skill);
                case GameAction.PlaceIntoDiscard:
                    return string.Format("{0}{1}置入弃牌堆", source, skill);
                case GameAction.Discard:
                    return string.Format("{0}{1}弃置", source, skill);
            }
            return string.Empty;
        }

        public static IList<Inline> RichTranslate(CardHandler cardType)
        {
            IList<Inline> list = new List<Inline>();
            if (cardType == null) return list;
            string typeString = Application.Current.TryFindResource(string.Format("Card.{0}.Name", cardType.CardType)) as string;
            if (typeString != null)
            {
                list.Add(new Run(string.Format("【{0}】", typeString)) { Foreground = new SolidColorBrush(Colors.Yellow) });
            }
            return list;
        }

        public static IList<Inline> RichTranslate(Card c)
        {
            IList<Inline> list = new List<Inline>();
            if (c.Id < 0) return list;
            CardViewModel card = new CardViewModel() { Card = c };
            string typeString = Application.Current.TryFindResource(string.Format("Card.{0}.Name", card.TypeString)) as string;
            if (typeString != null)
            {
                list.Add(new Run("【" + typeString) { Foreground = new SolidColorBrush(Colors.Yellow) });
                Run run = new Run();
                run.Foreground = Application.Current.Resources[string.Format("Card.Suit.{0}.SuitBrush", card.Suit)] as Brush;
                run.Text = string.Format("{0}{1}", Application.Current.Resources[string.Format("Card.Suit.{0}.SuitText", card.Suit)], card.RankString);
                list.Add(run);
                list.Add(new Run("】") { Foreground = new SolidColorBrush(Colors.Yellow) });
            }
            return list;
        }

        public static IList<Inline> RichTranslate(IList<Card> cards)
        {
            List<Inline> list = new List<Inline>();
            if (cards.Count == 0) return list;
            else if (cards.Count > 1 || cards[0].Id < 0)
            {
                list.Add(new Run(string.Format("{0}张卡牌", cards.Count)));
            }
            
            foreach (var card in cards)
            {
                list.AddRange(RichTranslate(card));
            }
            return list;
        }

        public static IList<Inline> RichTranslate(CardViewModel card)
        {
            IList<Inline> list = new List<Inline>();
            string typeString = Application.Current.TryFindResource(string.Format("Card.{0}.Name", card.TypeString)) as string;
            if (typeString != null)
            {
                list.Add(new Run("【" + typeString) { Foreground = new SolidColorBrush(Colors.Yellow) });
                Run run = new Run();
                run.Foreground = Application.Current.Resources[string.Format("Card.Suit.{0}.SuitBrush", card.Suit)] as Brush;
                run.Text = string.Format("{0}{1}", Application.Current.Resources[string.Format("Card.Suit.{0}.SuitText", card.Suit)], card.RankString);
                list.Add(run);
                list.Add(new Run("】") { Foreground = new SolidColorBrush(Colors.Yellow) });
            }
            return list;
        }

        public static IList<Inline> RichTranslate(ICard card)
        {
            List<Inline> list = new List<Inline>();
            if (card is Card)
            {
                list.AddRange(RichTranslate(card as Card));
            }
            else
            {
                list.AddRange(RichTranslate(card.Type));
            }
            return list;
        }

        public static IList<Inline> RichTranslate(ISkill skill)
        {
            IList<Inline> list = new List<Inline>();
            string skillstr = string.Format("\"{0}\"", Translate(skill));
            list.Add(new Run(skillstr) { Foreground = new SolidColorBrush(Colors.Yellow) });
            return list;
        }
        
        public static string Translate(IList<Player> players)
        {
            if (players != null && players.Count > 0)
            {
                StringBuilder builder = new StringBuilder();
                builder.Append(Translate(players[0]));
                for (int i = 1; i < players.Count; i++)
                {
                    builder.Append("，");
                    builder.Append(Translate(players[i]));
                }
                return builder.ToString();
            }
            return string.Empty;
        }
        
        public static Paragraph RichTranslateMainLog(ActionLog log)
        {
            Paragraph paragraph = new Paragraph();
            string source = Translate(log.Source);
            string dests = Translate(log.Targets);
            
            IList<Inline> skillInline = RichTranslate(log.SkillAction);
            string formatter = source;
            switch (log.GameAction)
            {
                case GameAction.None:
                    if (log.SkillAction != null)
                    {
                        ISkill skill = log.SkillAction;
                        if (skill is TriggerSkill || skill is ActiveSkill)
                        {
                            paragraph.Inlines.Add(string.Format("{0}发动了技能", source));
                            paragraph.Inlines.AddRange(RichTranslate(log.SkillAction));
                            if (dests != string.Empty)
                            {
                                paragraph.Inlines.Add("，目标是" + dests);
                            }
                        }
                        else if (skill is CardTransformSkill)
                        {
                            CompositeCard card = log.CardAction as CompositeCard;
                            Trace.Assert(card != null);
                            paragraph.Inlines.Add(string.Format("{0}发动了技能", source));
                            paragraph.Inlines.AddRange(RichTranslate(log.SkillAction));
                            if (card.Subcards.Count > 0)
                            {
                                paragraph.Inlines.Add("，将");
                                paragraph.Inlines.AddRange(RichTranslate(card.Subcards));
                                paragraph.Inlines.Add("当作一张");
                                paragraph.Inlines.AddRange(RichTranslate(card.Type));
                                paragraph.Inlines.Add("打出");
                            }
                        }
                    }
                    break;
                case GameAction.Use:
                    string toDests = (dests == string.Empty) ?  string.Empty : ("对" + dests);
                    paragraph.Inlines.Add(string.Format("{0}{1}使用了", source, toDests));
                    paragraph.Inlines.AddRange(RichTranslate(log.CardAction));
                    break;
                case GameAction.Play:
                    paragraph.Inlines.Add(string.Format("{0}打出了", source));
                    paragraph.Inlines.AddRange(RichTranslate(log.CardAction));
                    break;
                case GameAction.PlaceIntoDiscard:
                    // return string.Format("{0}{1}置入弃牌堆", source, skill);
                    break;
                case GameAction.Discard:
                    //return string.Format("{0}{1}弃置", source, skill);
                    break;
            }
            return paragraph;
        }

        public static Paragraph RichTranslateCardMove(List<Card> cards, DeckPlace source, DeckPlace dest, GameAction reason)
        {
            string sourceStr = Translate(source.Player);
            string destStr = Translate(dest.Player);
            var cardsInline = RichTranslate(cards);
            Paragraph paragraph = new Paragraph();
            if (source.Player != null)
            {
                if (reason == GameAction.Discard)
                {
                    paragraph.Inlines.Add(string.Format("{0}弃置了", sourceStr));
                    paragraph.Inlines.AddRange(cardsInline);
                }
                else if (reason == GameAction.PlaceIntoDiscard)
                {
                    paragraph.Inlines.Add(string.Format("{0}将", sourceStr));
                    paragraph.Inlines.AddRange(cardsInline);
                    paragraph.Inlines.Add("置入了齐牌堆");
                }
            }
                       
            if (dest.Player != null)
            {
                bool added = true;
                if (source.DeckType == DeckType.Dealing && dest.DeckType == DeckType.Hand)
                {
                    paragraph.Inlines.Add(string.Format("{0}从牌堆里摸了", destStr));
                }
                else if (source.DeckType == DeckType.Discard)
                {
                    paragraph.Inlines.Add(string.Format("{0}从弃牌堆里回收了", destStr));
                }
                else if (source.Player == dest.Player)
                {
                    if (source.DeckType == DeckType.Hand && dest.DeckType == DeckType.Equipment)
                    {
                        paragraph.Inlines.Add(string.Format("{0}装备了", destStr));
                    }
                    else if (source.DeckType == DeckType.Hand && dest.DeckType == DeckType.Hand)
                    {
                        paragraph.Inlines.Add(string.Format("{0}获得了自己的", destStr));
                    }
                }
                else if (dest.DeckType == DeckType.Hand || dest.DeckType == DeckType.Equipment)
                {
                    var owners = (from card in cards select card.Owner).Distinct();
                    if (owners.Contains(null))
                    {
                        Trace.TraceWarning("Cannot resolve log: reason is {0}, from {1}{2} to {3}{4}", reason, Translate(source.Player), source.DeckType, Translate(dest.Player), dest.DeckType);
                        paragraph.Inlines.Add(string.Format("{0}获得了", destStr));
                    }
                    else
                    {
                        List<Player> players = new List<Player>(owners);
                        paragraph.Inlines.Add(string.Format("{0}获得了{1}的", destStr, Translate(players)));
                    }
                }
                else
                {
                    added = false;
                }
                if (added)
                {
                    paragraph.Inlines.AddRange(cardsInline);
                }                
            }
            return paragraph;
        }
    }
}
