﻿//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Sanguosha.Lobby.Core
{
    using System;
    using System.Collections.Generic;

    public partial class Account
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public long Credits { get; set; }
        public long Wins { get; set; }
        public long Losses { get; set; }
        public string DisplayedName { get; set; }
        public long Quits { get; set; }
    }
}
