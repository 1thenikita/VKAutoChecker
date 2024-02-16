using Serilog;
using VKAutoChecker.Models;
using VkNet.Enums.StringEnums;
using VkNet.Exception;
using VkNet;
using VkNet.Model;

namespace VKAutoChecker.Services;

public class VkService : BackgroundService
{
    private HashSet<VkProfile> Profiles { get; set; } = new();
    private HashSet<GroupModel> Groups { get; set; } = new();

    public async Task Init()
    {
        if (string.IsNullOrEmpty(config.GetToken()))
        {
            Log.Fatal("Please set VK token!");
            Environment.Exit(0);
        }
        
        if (string.IsNullOrEmpty(config.GetGroupId()))
        {
            Log.Fatal("Please set group ID!");
            Environment.Exit(0);
        }

        var tokens = config.GetToken().Split(";");
        foreach (string token in tokens)
        {
            try
            {
                var profile = new VkProfile()
                {
                    Token = token
                };
                var vk = new VkApi();
                vk.Authorize(new ApiAuthParams()
                {
                    AccessToken = token
                });
                profile.VkApi = vk;
                Profiles.Add(profile);
            }
            catch (Exception e)
            {
                Log.Error(e, "Error with adding VK Profile");
            }
        }

        var groups = config.GetGroupId().Split(";");

        foreach (var group in groups)
        {
            try
            {
                Groups.Add(new()
                {
                    ID = long.Parse(group)
                });
            }
            catch (Exception e)
            {
                Log.Error(e, "Error with adding Group!");
            }
        }

        await Start();
    }

    private async Task Start()
    {
        Log.Information("Starting VK service...");

        if (config.GetLikedLastPosts())
        {
            LikeLastPosts(config.GetCountLastPosts());
        }

        else
        {
            Log.Information(
                "Edit config and select between liked_last_posts or liked_last_posts\nP.S Possible choose both");
        }

        if (config.GetLikedLastPosts())
        {
            await LikeNewPosts();
        }
        else
        {
            Log.Information(
                "Edit config and select between liked_last_posts or liked_last_posts\nP.S Possible choose both");
        }
    }

    private async Task LikeLastPosts(long count)
    {
        var timeStart = DateTime.Now;

        foreach (var vkProfile in Profiles)
        {
            var vk = vkProfile.VkApi;
            Log.Information($"Account {vk.Account.GetProfileInfo().Phone}");

            foreach (var group in Groups)
            {
                Log.Information($"Group {group.ID}");

                var lastId = GetLastId(vk, group.ID);
                var startPost = lastId - count ?? 0;

                Log.Information($"Last Post is {lastId ?? 0}");

                for (var id = startPost + 1; id <= lastId; id++)
                {
                    try
                    {
                        if (!IsExist(vk, group.ID, id))
                        {
                            continue;
                        }

                        if (!IsLiked(vk, group.ID, id))
                        {
                            try
                            {
                                vk.Likes.Add(new LikesAddParams
                                {
                                    Type = LikeObjectType.Post,
                                    OwnerId = group.ID,
                                    ItemId = id
                                });

                                Log.Information($"Liked post {id}");
                            }
                            catch (CaptchaNeededException ex)
                            {
                                Log.Warning(ex, $"Captcha needed to like new post!");
                                Log.Warning("Please enter captcha: {0}, sid: {1}", ex.Img, ex.Sid);

                                // todo loading captha
                                await Task.Delay(10 * 1000);
                                continue;
                                
                                await vk.Likes.AddAsync(new LikesAddParams
                                {
                                    Type = LikeObjectType.Post,
                                    OwnerId = group.ID,
                                    ItemId = id,
                                });
                            }
                            catch (PostAccessDeniedException exc)
                            {
                                Log.Information($"Post access denied {id}");
                            }
                            catch (VkApiException e)
                            {
                                Log.Information($"Network Error, {e.Message}");
                            }
                        }
                        else
                        {
                            Log.Information($"Post Already Liked {id}");
                        }

                        Task.Delay(config.GetCooldownLike()).Wait();
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, "Error with try like.");
                    }
                }
            }
        }

        var resultTime = (DateTime.Now - timeStart).TotalSeconds;
        Log.Information($"All Done in {Math.Abs(Math.Round(resultTime, 3))} seconds");
    }


    private async Task LikeNewPosts()
    {
        foreach (var vkProfile in Profiles)
        {
            var vk = vkProfile.VkApi;
            Log.Information($"Account {vk.Account.GetProfileInfo().Phone}");

            foreach (var group in Groups)
            {
                Log.Information($"Group {group.ID}");

                if (group.LastPostId == 0)
                {
                    group.LastPostId = GetLastId(vk, group.ID).GetValueOrDefault(0);
                }

                while (true)
                {
                    if (!IsLiked(vk, group.ID, GetLastId(vk, group.ID).GetValueOrDefault()))
                    {
                        try
                        {
                            vk.Likes.Add(new LikesAddParams
                            {
                                Type = LikeObjectType.Post,
                                OwnerId = group.ID,
                                ItemId = GetLastId(vk, group.ID).GetValueOrDefault()
                            });
                            Log.Information($"Liked New Post!");
                            group.LastPostId = GetLastId(vk, group.ID).GetValueOrDefault();
                        }
                        catch (CaptchaNeededException ex)
                        {
                            Log.Warning(ex, $"Captcha needed to like new post!");
                            Log.Warning("Please enter captcha: {0}, sid: {1}", ex.Img, ex.Sid);

                            // todo loading captha
                            await Task.Delay(10 * 1000);
                            continue;
                            await vk.Likes.AddAsync(new LikesAddParams
                            {
                                Type = LikeObjectType.Post,
                                OwnerId = group.ID,
                                ItemId = GetLastId(vk, group.ID).GetValueOrDefault(),
                            });
                        }
                        catch (PostAccessDeniedException e)
                        {
                            Log.Information(e, 
                                $"Post Access denied {GetLastId(vk, group.ID).GetValueOrDefault()}");
                            group.LastPostId = GetLastId(vk, group.ID).GetValueOrDefault();
                        }
                        catch (VkApiException e)
                        {
                            Log.Error(e, $"Network Error");
                        }
                    }
                    else
                    {
                        Log.Information(
                            $"All new posts liked, waiting {config.GetCooldownCheck()} seconds");
                        await Task.Delay(config.GetCooldownCheck() * 1000);
                    }
                }
            }
        }
    }

    private bool IsLiked(VkApi vk, long groupID, long postId)
    {
        bool noRepost = false;
        var res = vk.Likes.IsLiked(out noRepost, ownerId: groupID, type: LikeObjectType.Post,
            itemId: postId);
        return res;
    }

    private bool IsExist(VkApi vk, long groupID, long id)
    {
        var res = vk.Wall.GetById(new[] { $"{groupID}_{id}" });
        return res.Any();
    }

    private long? GetLastId(VkApi vk, long groupID)
    {
        var wall = vk.Wall.Get(new()
        {
            OwnerId = groupID,
            Count = 10
        });

        return wall.WallPosts.FirstOrDefault()?.Id;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!stoppingToken.IsCancellationRequested)
        {
            await Init();
        }
    }
}

public class config
{
    public static string GetToken()
    {
        return Environment.GetEnvironmentVariable("TOKEN") ??
               "";
        // Вставьте сюда код для получения токена из конфигурационного файла
    }

    public static string GetGroupId()
    {
        // return long.Parse(Environment.GetEnvironmentVariable("GroupID"));
        return Environment.GetEnvironmentVariable("GROUPID") ?? "";
        // Вставьте сюда код для получения ID группы из конфигурационного файла
    }

    public static bool GetLikedLastPosts()
    {
        return bool.Parse(Environment.GetEnvironmentVariable("LIKED_LAST_POSTS") ?? "true");
        // Вставьте сюда код для получения значения liked_last_posts из конфигурационного файла
    }

    public static long GetCountLastPosts()
    {
        return int.Parse((Environment.GetEnvironmentVariable("COUNT_LAST_POSTS") ?? "10"));
        // Вставьте сюда код для получения значения count_last_posts из конфигурационного файла
    }

    public static int GetCooldownLike()
    {
        return int.Parse((Environment.GetEnvironmentVariable("COOLDOWN_LIKE") ?? "1"));
        // Вставьте сюда код для получения значения cooldown_like из конфигурационного файла
    }

    public static int GetCooldownCheck()
    {
        return int.Parse((Environment.GetEnvironmentVariable("COOLDOWN_CHECK") ?? "60"));
        // Вставьте сюда код для получения значения cooldown_check из конфигурационного файла
    }
}