﻿using System;
using System.Text.Json.Nodes;
using Lagrange.Core;
using Lagrange.Core.Message;
using Lagrange.Core.Message.Entity;
using YounBot.Utils;
using YounBot.Utils.Hypixel;
using static YounBot.Utils.MessageUtils;
namespace YounBot.Command.Implements;

public class HypixelCommand
{
    private readonly CooldownUtils _cooldown = new(30000);

    [Command("hypixel", "Hypixel玩家信息")]
    public async void Hypixel(BotContext context, MessageChain chain, string name)
    {
        var user = chain.FriendUin;
        if (!_cooldown.IsTimePassed(user!)) {
            if (_cooldown.ShouldSendCooldownNotice(user)) 
                await SendMessage(context, chain, $"你可以在 {_cooldown.GetLeftTime(user) / 1000} 秒后继续使用该指令");
            return;
        }

        var uuid = MojangApiUtils.GetUuidByName(name);

        _cooldown.Flag(user);

        var playerInfo = HypixelApiUtils.RequestAsync($"/player?uuid={uuid}").Result["player"]!.AsObject();
        string? rank;
        if (playerInfo.ContainsKey("rank") && !playerInfo["rank"]!.GetValue<string>().Equals("NORMAL"))
        {
            rank = HypixelApiUtils.ResolveRank(playerInfo["rank"]!.GetValue<string>());
        } else if (playerInfo.ContainsKey("monthlyPackageRank") && !playerInfo["monthlyPackageRank"]!.GetValue<string>().Equals("NONE"))
        {
            rank = HypixelApiUtils.ResolveRank(playerInfo["monthlyPackageRank"]!.GetValue<string>());
        } else if (playerInfo.ContainsKey("newPackageRank") && !playerInfo["newPackageRank"]!.GetValue<string>().Equals("NONE"))
        {
            rank = HypixelApiUtils.ResolveRank(playerInfo["newPackageRank"]!.GetValue<string>());
        }
        else
        {
            rank = null;
        }

        double level;
        
        try
        {
            level = Math.Round(ExpCalculator.GetExactLevel(playerInfo["networkExp"]!.GetValue<long>()) * 100) / 100.0;
        }
        catch (Exception)
        {
            level = 1.0;
        }

        string firstLogin = ConvertDate(playerInfo, "firstLogin");
        string lastLogin = ConvertDate(playerInfo, "lastLogin");
        string lastLogout = ConvertDate(playerInfo, "lastLogout");

        var statusInfo = HypixelApiUtils.RequestAsync($"/status?uuid={uuid}").Result["session"]!.AsObject();
        string stringOnlineStatus;
        if (!statusInfo["online"]!.GetValue<bool>())
        {
            stringOnlineStatus = "离线";
        }
        else
        {
            stringOnlineStatus = "在线 -> " + HypixelApiUtils.ResolveGameType(GetString(statusInfo, "gameType", "Lobby?"));
        }
    }
    
    public static double CalculatorR(int num1, int num2)
    {
        double result;
        try
        {
            result = Math.Round((num1 / (double)num2) * 100.0, 2);
        }
        catch (Exception)
        {
            result = num1;
        }
        return result;
    }

    public static int GetIntOrNull(JsonObject jsonObject, string key)
    {
        try
        {
            return jsonObject[key]!.GetValue<int>();
        }
        catch (Exception)
        {
            return 0;
        }
    }
    
    public static int GetInt(JsonObject jsonObject, string key, int defaultValue)
    {
        try
        {
            return jsonObject[key]!.GetValue<int>();
        }
        catch (Exception)
        {
            return defaultValue;
        }
    }

    public static string GetStringOrNull(JsonObject jsonObject, string key)
    {
        try
        {
            return jsonObject[key]!.GetValue<string>();
        }
        catch (Exception)
        {
            return "Null";
        }
    }
    
    public static string GetString(JsonObject jsonObject, string key, string defaultValue)
    {
        try
        {
            return jsonObject[key]!.GetValue<string>();
        }
        catch (Exception)
        {
            return defaultValue;
        }
    }
    
    public static string ConvertDate(JsonObject jsonObject, string key)
    {
        try
        {
            return TimeUtils.ConvertDate(jsonObject[key]!.GetValue<long>());
        }
        catch (Exception)
        {
            return "无法获取";
        }
    }
}