﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Sanguosha.Core.Players;

namespace Sanguosha.Core.Cards
{
    [Serializable]
    public class DeckType
    {
        static DeckType()
        {
            Dealing = new DeckType("Dealing");
            Discard = new DeckType("Discard");
            Compute = new DeckType("Compute");
            Hand = new DeckType("Hand");
            Equipment = new DeckType("Equipment");
            DelayedTools = new DeckType("DelayedTools");
            JudgeResult = new DeckType("JudgeResult");
            None = new DeckType("None");
        }

        public DeckType(string name)
        {
            this.name = name;
        }

        public override int GetHashCode()
        {
            return name.GetHashCode();
        }

        private string name;

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }
            if (!(obj is DeckType))
            {
                return false;
            }
            DeckType type2 = (DeckType)obj;
            return name == type2.name;
        }

        public static DeckType None;
        public static DeckType Dealing;
        public static DeckType Discard;
        public static DeckType Compute;
        public static DeckType Hand;
        public static DeckType Equipment;
        public static DeckType DelayedTools;
        public static DeckType JudgeResult;
    }
}