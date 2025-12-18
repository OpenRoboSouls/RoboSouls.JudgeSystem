using System.Threading;
using System.Threading.Tasks;

namespace RoboSouls.JudgeSystem;

public abstract class JudgeSystem
{
    public virtual Task StartAsync(CancellationToken cancellation = new CancellationToken())
    {
        return Task.CompletedTask;
    }

    public virtual Task Reset(CancellationToken cancellation = new CancellationToken())
    {
        return Task.CompletedTask;
    }
}