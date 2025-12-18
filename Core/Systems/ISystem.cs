using System.Threading;
using System.Threading.Tasks;

namespace RoboSouls.JudgeSystem.Systems;

public interface ISystem
{
    public Task Reset(CancellationToken cancellation = new CancellationToken())
    {
        return Task.CompletedTask;
    }
}