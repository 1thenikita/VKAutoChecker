using VkNet;

namespace VKAutoChecker.Models;

public class VkProfile
{
    public string Token { get; set; }
    public bool IsWork { get; set; } = false;
    public VkApi? VkApi { get; set; }
}