using System;
using System.Threading;
using System.Threading.Tasks;

namespace RoboSouls.JudgeSystem.Systems;

/// <summary>
///     外部消息队列服务，Mainly mqtt
/// </summary>
public interface IMQService
{
    public Task Subscribe(string topic, int qosLevel = 1, CancellationToken cancellation = new());
    public event Action<string, byte[]> OnMessageReceived;
    public Task Publish(string topic, byte[] data, int qosLevel = 1, CancellationToken cancellation = new());
}