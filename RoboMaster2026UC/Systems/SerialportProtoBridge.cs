using RoboSouls.JudgeSystem.Systems;
using VContainer;

namespace RoboSouls.JudgeSystem.RoboMaster2026UC.Systems;

public class SerialportProtoBridge
{
    [Inject]
    internal IMQService MQService { get; set; }
}