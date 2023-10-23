﻿using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace HisoBOT.Services;

public class UpdateHandlers
{
    private readonly ITelegramBotClient _botClient;
    private readonly UserService _userService;
    private readonly ProjectService _projectService;

    public UpdateHandlers(
        ITelegramBotClient botClient,
        UserService userService,
        ProjectService projectService)
    {
        _botClient = botClient;
        _userService = userService;
        _projectService = projectService;
    }

    public async Task HandleUpdateAsync(Update update, CancellationToken cancellationToken)
    {
        var handler = update switch
        {
            { Message: { } message } => BotOnMessageReceived(message, cancellationToken),
            { MyChatMember: { } myChatMember } => BotOnChatMember(myChatMember, cancellationToken),
            _ => UnknownUpdateHandlerAsync(update, cancellationToken)
        };

        await handler;
    }

    private async Task BotOnChatMember(ChatMemberUpdated myChatMember, CancellationToken cancellationToken)
    {
        if (myChatMember == null
            || myChatMember.NewChatMember.Status is ChatMemberStatus.Left
            || myChatMember.NewChatMember.Status is ChatMemberStatus.Kicked)
            return;

        var userIdFromAdd = myChatMember.From.Id;
        if (_userService.IsAdmin(userIdFromAdd))
        {
            if (myChatMember.NewChatMember is ChatMemberAdministrator)
            {
                await _botClient.SendTextMessageAsync(
                       chatId: userIdFromAdd,
                       text: $"Меня добавили в `{myChatMember.Chat.Type}`\nНазвание: `{myChatMember.Chat.Title}`\nID: `{myChatMember.Chat.Id}`",
                       parseMode: ParseMode.Markdown,
                       cancellationToken: cancellationToken);
            }
        }
        else
        {
            if (myChatMember.Chat.Type is ChatType.Private)
            {
                return;
            }
            await _botClient.LeaveChatAsync(myChatMember.Chat.Id);
        }
    }

    private async Task BotOnMessageReceived(Message message, CancellationToken cancellationToken)
    {
        if (message.Text is not { } messageText)
            return;
        
        if (message.Chat.Type is not ChatType.Private)
            return;

        var action = messageText switch
        {
            "/start" => StartCommand(message, cancellationToken),
            "Добавить проект" => CreateNewProjectCommand(message, cancellationToken),
            "Мои проекты" => MyProjectsCommand(message, cancellationToken),
            _ => ReceiveProjectNameAndChatID(message, cancellationToken),
        };

        Message sentMessage = await action;
    }

    private async Task<Message> StartCommand(Message message, CancellationToken cancellationToken)
    {
        string userIdText = $"Genesis hisobot вас приветсвует!\n\rВаш user id = `{message.From?.Id}`";
        var userId = message.From.Id;

        var buttons = new ReplyKeyboardMarkup(
            new[]
            {
                new[]
                {
                    new KeyboardButton("Добавить проект"),
                    new KeyboardButton("Удалить проект")
                },
                new[]
                {
                    new KeyboardButton("Мои проекты")
                }
            })
        { 
            ResizeKeyboard = true 
        };

        if (_userService.IsAdmin(userId))
        {
            return await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: userIdText,
                parseMode: ParseMode.Markdown,
                replyMarkup: buttons,
                cancellationToken: cancellationToken);
        }

        return null;
    }

    private async Task<Message> MyProjectsCommand(Message message, CancellationToken cancellationToken)
    {
        var userId = message.From.Id;

        var projects = _projectService.GetAllProjects();

        if (projects.Any())
        {
            var projectStrings = projects.Select(p => $"{p.ChatId}:{p.Name}");
            string projectsAsString = string.Join(Environment.NewLine, projectStrings);

            return await _botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: "Ваши проекты: ```\n\r" + projectsAsString + "```",
                        parseMode: ParseMode.Markdown,
                        cancellationToken: cancellationToken);
        }

        return await _botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: "Проектов не найдено!",
                        cancellationToken: cancellationToken);
    }

    private async Task<Message> CreateNewProjectCommand(Message message, CancellationToken cancellationToken)
    {
        string userIdText = $"Пришлите chatId и название проекта в таком формате:\n\nchatId:название_проекта";
        var userId = message.From.Id;

        if (_userService.IsAdmin(userId))
        {
            _userService.SetIsTypeProjectName(userId, true);
            return await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: userIdText,
                cancellationToken: cancellationToken);
        }

        return null;
    }

    private async Task<Message> ReceiveProjectNameAndChatID(Message message, CancellationToken cancellationToken)
    {
        string userIdText = $"*User id =* `{message.From?.Id}` <br/> Команда не найдена";
        var userId = message.From.Id;

        if (_userService.IsAdmin(userId) && _userService.IsTypeProjectName(userId))
        {
            await CreateProject(message, cancellationToken);
        }
        return message;
    }

    private async Task<Message> CreateProject(Message message, CancellationToken cancellationToken)
    {
        var chatIdAndProjectName = message.Text.Trim();
        string[] parts = chatIdAndProjectName.Split(':');

        if (parts.Length == 2)
        {
            string chatId = parts[0];
            string projectName = parts[1];

            var result = _projectService.CreateProject(chatId, projectName);
            if (result.Success is true)
            {
                _userService.SetIsTypeProjectName(message.From.Id, false);
                return await _botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: result.Message,
                    parseMode: ParseMode.Markdown,
                    cancellationToken: cancellationToken);
            }
            else
            {
                return await _botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: result.Message,
                    parseMode: ParseMode.Markdown,
                    cancellationToken: cancellationToken);
            }
        }

        return await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "Не верный формат введите заново!",
            parseMode: ParseMode.Markdown,
            cancellationToken: cancellationToken);
    }

    private Task UnknownUpdateHandlerAsync(Update update, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
