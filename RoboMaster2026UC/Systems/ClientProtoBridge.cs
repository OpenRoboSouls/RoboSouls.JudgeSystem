using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Google.Protobuf;
using RoboSouls.JudgeSystem.RoboMaster2026UC.Proto;
using RoboSouls.JudgeSystem.Systems;
using Buff = RoboSouls.JudgeSystem.Systems.Buff;

namespace RoboSouls.JudgeSystem.RoboMaster2026UC.Systems;

/// <summary>
///     RoboMaster 2026 客户端协议发布服务
/// </summary>
public sealed class ClientProtoBridge(IMQService mqService)
{
    private readonly Dictionary<string, Action<byte[]>> _handlers = new();

    private AirSupportStatusSync _airSupportStatusSync;

    private DartSelectTargetStatusSync _dartSelectTargetStatusSync;

    private DeployModeStatusSync _deployModeStatusSync;

    private GlobalLogisticsStatus _gameLogisticsStatus;

    private GlobalSpecialMechanism _gameSpecialMechanism;

    private GameStatus _gameStatus;

    private GlobalUnitStatus _gameUnitStatus;

    private GuardCtrlResult _guardCtrlResult;

    private RaderInfoToClient _raderInfoToClient;

    private RobotDynamicStatus _robotDynamicStatus;

    private RobotInjuryStat _robotInjuryStat;

    private RobotModuleStatus _robotModuleStatus;

    private RobotPathPlanInfo _robotPathPlanInfo;

    private RobotPerformanceSelectionSync _robotPerformanceSelectionSync;

    private RobotPosition _robotPosition;

    private RobotRespawnStatus _robotRespawnStatus;

    private RobotStaticStatus _robotStaticStatus;

    private CancellationTokenSource? _routineCts;

    private RuneStatusSync _runeStatusSync;

    private SentinelStatusSync _sentinelStatusSync;

    private TechCoreMotionStateSync _techCoreMotionStateSync;

    private void RegisterHandler<T>(string topic, Action<T> handler, MessageParser<T> parser) where T : IMessage<T>
    {
        _handlers[topic] = buf =>
        {
            var message = parser.ParseFrom(buf);
            handler(message);
        };

        mqService.Subscribe(topic);
    }

    private void RegisterRoutineAction(int intervalHz, Action action)
    {
        // Task.Void(async () =>
        // {
        //     var delay = TimeSpan.FromSeconds(1.0 / intervalHz);
        //     while (!_routineCts.IsCancellationRequested)
        //     {
        //         action();
        //         await Task.Delay(delay, cancellationToken: _routineCts.Token);
        //     }
        // });
    }

    public void StartDaemon()
    {
        _routineCts = new CancellationTokenSource();
        mqService.OnMessageReceived += OnMessageReceived;
        RegisterHandler("RemoteControl", OnRemoteControl, RemoteControl.Parser);
        RegisterHandler("MapClickInfoNotify", OnMapClickInfoNotify, MapClickInfoNotify.Parser);
        RegisterHandler("AssemblyCommand", OnAssemblyCommand, AssemblyCommand.Parser);
        RegisterHandler("RobotPerformanceSelectionCommand", OnRobotPerformanceSelectionCommand,
            RobotPerformanceSelectionCommand.Parser);
        RegisterHandler("HeroDeployModeEventCommand", OnHeroDeployModeEventCommand, HeroDeployModeEventCommand.Parser);
        RegisterHandler("RuneActivateCommand", OnRuneActivateCommand, RuneActivateCommand.Parser);
        RegisterHandler("DartCommand", OnDartCommand, DartCommand.Parser);
        RegisterHandler("GuardCtrlCommand", OnGuardCtrlCommand, GuardCtrlCommand.Parser);
        RegisterHandler("AirSupportCommand", OnAirSupportCommand, AirSupportCommand.Parser);
        RegisterRoutineAction(5, SendGameStatus);
        RegisterRoutineAction(1, SendGlobalUnitStatus);
        RegisterRoutineAction(1, SendGlobalLogisticsStatus);
        RegisterRoutineAction(1, SendGlobalSpecialMechanism);
        RegisterRoutineAction(1, SendRobotInjuryStat);
        RegisterRoutineAction(1, SendRobotRespawnStatus);
        RegisterRoutineAction(1, SendRobotStaticStatus);
        RegisterRoutineAction(10, SendRobotDynamicStatus);
        RegisterRoutineAction(1, SendRobotModuleStatus);
        RegisterRoutineAction(1, SendRobotPosition);
        RegisterRoutineAction(1, SendRobotPathPlanInfo);
        RegisterRoutineAction(1, SendRaderInfoToClient);
        RegisterRoutineAction(1, SendTechCoreMotionStateSync);
        RegisterRoutineAction(1, SendRobotPerformanceSelectionSync);
        RegisterRoutineAction(1, SendDeployModeStatusSync);
        RegisterRoutineAction(1, SendRuneStatusSync);
        RegisterRoutineAction(1, SendSentinelStatusSync);
        RegisterRoutineAction(1, SendDartSelectTargetStatusSync);
        RegisterRoutineAction(1, SendGuardCtrlResult);
        RegisterRoutineAction(1, SendAirSupportStatusSync);
    }

    public void StopDaemon()
    {
        _handlers.Clear();
        mqService.OnMessageReceived -= OnMessageReceived;
        _routineCts?.Cancel();
        _routineCts?.Dispose();
        _routineCts = null;
    }

    private void OnDisable()
    {
        StopDaemon();
    }

    private void OnMessageReceived(string topic, byte[] payload)
    {
        if (_handlers.TryGetValue(topic, out var handler)) handler(payload);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void PublishMessage(string topic, IMessage message, int qosLevel = 1,
        CancellationToken cancellationToken = default)
    {
        mqService.Publish(topic, message.ToByteArray(), qosLevel, cancellationToken);
    }

    private void OnRemoteControl(RemoteControl message)
    {
    }

    private void SendGameStatus()
    {
        PublishMessage("GameStatus", _gameStatus);
    }

    private void SendGlobalUnitStatus()
    {
        PublishMessage("GlobalUnitStatus", _gameUnitStatus);
    }

    private void SendGlobalLogisticsStatus()
    {
        PublishMessage("GlobalLogisticsStatus", _gameLogisticsStatus);
    }

    private void SendGlobalSpecialMechanism()
    {
        PublishMessage("GlobalSpecialMechanism", _gameSpecialMechanism);
    }

    private void SendEvent(Event gameEvent)
    {
        PublishMessage("Event", gameEvent);
    }

    private void SendRobotInjuryStat()
    {
        PublishMessage("RobotInjuryStat", _robotInjuryStat);
    }

    private void SendRobotRespawnStatus()
    {
        PublishMessage("RobotRespawnStatus", _robotRespawnStatus);
    }

    private void SendRobotStaticStatus()
    {
        PublishMessage("RobotStaticStatus", _robotStaticStatus);
    }

    private void SendRobotDynamicStatus()
    {
        PublishMessage("RobotDynamicStatus", _robotDynamicStatus);
    }

    private void SendRobotModuleStatus()
    {
        PublishMessage("RobotModuleStatus", _robotModuleStatus);
    }

    private void SendRobotPosition()
    {
        PublishMessage("RobotPosition", _robotPosition);
    }

    private void SendBuff(Buff buff)
    {
    }

    private void SendPenaltyInfo(PenaltyInfo penaltyInfo)
    {
    }

    private void OnMapClickInfoNotify(MapClickInfoNotify message)
    {
    }

    private void SendRobotPathPlanInfo()
    {
        PublishMessage("RobotPathPlanInfo", _robotPathPlanInfo);
    }

    private void SendRaderInfoToClient()
    {
        PublishMessage("RaderInfoToClient", _raderInfoToClient);
    }

    private void OnAssemblyCommand(AssemblyCommand message)
    {
    }

    private void SendTechCoreMotionStateSync()
    {
        PublishMessage("TechCoreMotionStateSync", _techCoreMotionStateSync);
    }

    private void OnRobotPerformanceSelectionCommand(RobotPerformanceSelectionCommand message)
    {
    }

    private void SendRobotPerformanceSelectionSync()
    {
        PublishMessage("RobotPerformanceSelectionSync", _robotPerformanceSelectionSync);
    }

    private void OnHeroDeployModeEventCommand(HeroDeployModeEventCommand message)
    {
    }

    private void SendDeployModeStatusSync()
    {
        PublishMessage("DeployModeStatusSync", _deployModeStatusSync);
    }

    private void OnRuneActivateCommand(RuneActivateCommand message)
    {
    }

    private void SendRuneStatusSync()
    {
        PublishMessage("RuneStatusSync", _runeStatusSync);
    }

    private void SendSentinelStatusSync()
    {
        PublishMessage("SentinelStatusSync", _sentinelStatusSync);
    }

    private void OnDartCommand(DartCommand message)
    {
    }

    private void SendDartSelectTargetStatusSync()
    {
        PublishMessage("DartSelectTargetStatusSync", _dartSelectTargetStatusSync);
    }

    private void OnGuardCtrlCommand(GuardCtrlCommand message)
    {
    }

    private void SendGuardCtrlResult()
    {
        PublishMessage("GuardCtrlResult", _guardCtrlResult);
    }

    private void OnAirSupportCommand(AirSupportCommand message)
    {
    }

    private void SendAirSupportStatusSync()
    {
        PublishMessage("AirSupportStatusSync", _airSupportStatusSync);
    }
}