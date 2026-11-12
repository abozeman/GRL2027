using Fusion;

// Define the struct outside the class or nested inside it
public struct RaceStateBools : INetworkStruct
{
    public NetworkBool IsPreRace;
    public NetworkBool IsCountdown;
    public NetworkBool IsFinalCountdown;
    public NetworkBool IsInProgress;
    public NetworkBool IsFinished;
}