﻿using HisoBOT.Models;
using Telegram.Bot.Types;

namespace HisoBOT.Commands.Interfaces
{
    public interface ICommand
    {
        public string Name { get; }
        public State State { get; }
        public Task Execute(Update update);
        public Task GetUpdate(Update update);
    }
}
