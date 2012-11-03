﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;

using Sanguosha.Core.UI;
using Sanguosha.Core.Skills;
using Sanguosha.Core.Players;
using Sanguosha.Core.Games;
using Sanguosha.Core.Triggers;
using Sanguosha.Core.Exceptions;

namespace Sanguosha.Core.Cards
{
    [Serializable]
    public abstract class CardHandler
    {
        Dictionary<DeckPlace, List<Card>> deckBackup;
        List<Card> cardsOnHold;

        public abstract CardCategory Category {get;}

        /// <summary>
        /// 临时将卡牌提出，verify时使用
        /// </summary>
        /// <param name="cards">卡牌</param>
        /// <remarks>第二次调用将会摧毁第一次调用时临时区域的所有卡牌</remarks>
        public virtual void HoldInTemp(List<Card> cards)
        {
            deckBackup = new Dictionary<DeckPlace, List<Card>>();
            foreach (Card c in cards)
            {
                Trace.Assert(c.Type != null);
                if ((c.Type is Equipment) && c.Place.DeckType == DeckType.Equipment)
                {
                    Equipment e = (Equipment)c.Type;
                    e.UnregisterTriggers(c.Place.Player);
                }
                if (!deckBackup.ContainsKey(c.Place))
                {
                    deckBackup.Add(c.Place, new List<Card>(Game.CurrentGame.Decks[c.Place]));
                    Game.CurrentGame.Decks[c.Place].Remove(c);
                }
            }
            cardsOnHold = cards;
        }

        /// <summary>
        /// 回复临时区域的卡牌到原来位置
        /// </summary>
        public virtual void ReleaseHoldInTemp()
        {
            foreach (Card c in cardsOnHold)
            {
                Trace.Assert(c.Type != null);
                if ((c.Type is Equipment) && c.Place.DeckType == DeckType.Equipment)
                {
                    Equipment e = (Equipment)c.Type;
                    e.RegisterTriggers(c.Place.Player);
                }
            }
            foreach (DeckPlace p in deckBackup.Keys)
            {
                Game.CurrentGame.Decks[p].Clear();
                Game.CurrentGame.Decks[p].AddRange(deckBackup[p]);
            }
            deckBackup = null;
            cardsOnHold = null;
        }

        public bool PlayerIsCardTargetCheck(ref Player source, ref Player dest, ICard card)
        {
            while (true)
            {
                GameEventArgs arg = new GameEventArgs();
                arg.Source = source;
                arg.Targets = new List<Player>();
                arg.Targets.Add(dest);
                arg.Card = card;
                try
                {

                    Game.CurrentGame.Emit(GameEvent.PlayerIsCardTarget, arg);
                    Trace.Assert(arg.Targets.Count == 1);
                    return true;
                }
                catch (TriggerResultException e)
                {
                    if (e.Status == TriggerResult.Fail)
                    {
                        Trace.TraceInformation("Player {0} refuse to be targeted by {1}", dest.Id, card.Type.CardType);
                        return false;
                    }
                    else if (e.Status == TriggerResult.Retry)
                    {
                        source = arg.Source;
                        dest = arg.Targets[0];
                        continue;
                    }
                    else
                    {
                        Trace.Assert(false);
                    }
                }
            }
        }

        public void NotifyCardUse(Player source, List<Player> dests, List<Player> secondary, ICard card)
        {
            List<Player> logTargets = LogTargetsModifier(source, dests);
            ActionLog log = new ActionLog();
            log.Source = source;
            log.Targets = logTargets;
            log.SecondaryTargets = secondary;
            log.SkillAction = null;
            log.GameAction = GameAction.Use;
            log.CardAction = card;
            Game.CurrentGame.NotificationProxy.NotifySkillUse(log);
            if (card is Card)
            {
                Card terminalCard = card as Card;
                if (terminalCard.Log == null)
                {
                    terminalCard.Log = new ActionLog();
                }
                terminalCard.Log.Source = source;
                terminalCard.Log.Targets = dests;
                terminalCard.Log.SecondaryTargets = secondary;
                terminalCard.Log.CardAction = card;
            }
            else if (card is CompositeCard)
            {
                foreach (var s in (card as CompositeCard).Subcards)
                {
                    if (s.Log == null)
                    {
                        s.Log = new ActionLog();
                    }
                    s.Log.Source = source;
                    s.Log.Targets = dests;
                    s.Log.SecondaryTargets = secondary;
                    s.Log.CardAction = card;
                }
            }
        }

        public virtual void Process(Player source, List<Player> dests, ICard card)
        {
            NotifyCardUse(source, dests, new List<Player>(), card);
            foreach (var player in dests)
            {
                Player p = player;
                Player src = source;
                if (PlayerIsCardTargetCheck(ref src, ref p, card)) 
                {
                    Process(src, p, card);
                }
            }
        }

        protected virtual List<Player> LogTargetsModifier(Player source, List<Player> dests)
        {
            return new List<Player>(dests);
        }

        protected abstract void Process(Player source, Player dest, ICard card);

        public virtual VerifierResult Verify(Player source, ISkill skill, List<Card> cards, List<Player> targets)
        {
            return VerifyHelper(source, skill, cards, targets, NotReforging(source, skill, cards, targets));
        }

        public virtual bool NotReforging(Player source, ISkill skill, List<Card> cards, List<Player> targets)
        {
            return true;
        }
        /// <summary>
        /// 卡牌UI合法性检查
        /// </summary>
        /// <param name="source"></param>
        /// <param name="skill"></param>
        /// <param name="cards"></param>
        /// <param name="targets"></param>
        /// <param name="notReforging">不是重铸中，检查PlayerCanUseCard</param>
        /// <returns></returns>
        protected VerifierResult VerifyHelper(Player source, ISkill skill, List<Card> cards, List<Player> targets, bool notReforging)
        {
            ICard card;
            if (skill != null)
            {
                CompositeCard c;
                if (skill is CardTransformSkill)
                {
                    CardTransformSkill s = skill as CardTransformSkill;
                    VerifierResult r = s.TryTransform(cards, null, out c);
                    if (r != VerifierResult.Success)
                    {
                        return r;
                    }
                    if (!(this.GetType().IsAssignableFrom(c.Type.GetType())))
                    {
                        return VerifierResult.Fail;
                    }
                    if (notReforging)
                    {
                        if (!Game.CurrentGame.PlayerCanUseCard(source, c))
                        {
                            return VerifierResult.Fail;
                        }
                    }
                    HoldInTemp(c.Subcards);
                    card = c;
                }
                else
                {
                    return VerifierResult.Fail;
                }
            }
            else
            {
                if (cards == null || cards.Count != 1)
                {
                    return VerifierResult.Fail;
                }
                card = cards[0];
                if (!(this.GetType().IsAssignableFrom(card.Type.GetType())))
                {
                    return VerifierResult.Fail;
                }

                if (notReforging)
                {
                    if (!Game.CurrentGame.PlayerCanUseCard(source, card))
                    {
                        return VerifierResult.Fail;
                    }
                }
                HoldInTemp(cards);
            }


            if (targets != null && targets.Count != 0)
            {
                if (notReforging)
                {
                    if (!Game.CurrentGame.PlayerCanBeTargeted(source, targets, card))
                    {
                        ReleaseHoldInTemp();
                        return VerifierResult.Fail;
                    }
                }
            }
            VerifierResult ret = Verify(source, card, targets);
            ReleaseHoldInTemp();
            return ret;
        }

        protected abstract VerifierResult Verify(Player source, ICard card, List<Player> targets);        

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>Used by UI Only!</remarks>
        public virtual string CardType
        {
            get { return this.GetType().Name; }
        }

    }

}
