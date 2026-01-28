using System.Threading.Tasks;
using VitalRouter;

namespace RoboSouls.JudgeSystem.Events;

public class JudgeSystemEventFilter : ICommandInterceptor
{
    public async ValueTask InvokeAsync<T>(
        T command,
        PublishContext context,
        PublishContinuation<T> next
    )
        where T : ICommand
    {
        if (command is not IJudgeSystemEvent e) return;

        await next(command, context);
    }
}