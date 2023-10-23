﻿using HisoBOT.DB;
using HisoBOT.Services;
using Telegram.Bot;

namespace HisoBOT.Config;

public static class BotConfig
{
    public static void BotConfigure(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddHttpClient("telegram_bot_client")
            .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
            {
                TelegramBotClientOptions options = new(configuration["BotToken"]);
                return new TelegramBotClient(options, httpClient);
            });

        services.AddScoped<UserService>();
        services.AddScoped<HisobotService>();
        services.AddScoped<ProjectService>();
        services.AddScoped<UpdateHandlers>();
        services.AddDbContext<AppDbContext>();
        services.AddHostedService<ConfigureWebhook>();
    }
}

