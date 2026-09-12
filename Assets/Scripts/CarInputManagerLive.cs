using Assets.CryptoKartz.Scripts.Managers;
using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;
namespace Assets.CryptoKartz.Scripts
{
    public class CarInputManagerLive : NetworkBehaviour, INetworkRunnerCallbacks
    {

        private float steeringInput;
        private float throttleInput;
        [SerializeField] public CarControlDataLivePublisher _carControlPublisher;

        
        public override void Spawned()
        {
            Object.Runner.AddCallbacks(this);

        }

        /// <summary>
        /// Fixed update network.
        /// </summary>
        public override void FixedUpdateNetwork()
        {
            //if (!Runner.IsServer) return;

            //if(_carControlPublisher == null)
            //{
            //    Debug.Log("CarControllerLive: CarControlPublisher is null");
            //    return;
            //}

            //if (!Object.HasInputAuthority) return;



            if (GetInput<Player.PlayerInputProvider.CarInput>(out var input) == false) return;

            throttleInput = input.carControlValue.y;
            steeringInput = input.carControlValue.x;

            _carControlPublisher.setControl(steeringInput, throttleInput);

        }

        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
            
        }

        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
            
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            
        }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            
        }

        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            
        }

        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
        {
            
        }

        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {
            
        }

        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
        {
            
        }

        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
        {
            
        }

        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
        {
            
        }

        public void OnInput(NetworkRunner runner, NetworkInput input)
        {
            
        }

        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
        {
            
        }

        public void OnConnectedToServer(NetworkRunner runner)
        {
            
        }

        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {
            
        }

        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
        {
            
        }

        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
        {
            
        }

        public void OnSceneLoadDone(NetworkRunner runner)
        {
            
        }

        public void OnSceneLoadStart(NetworkRunner runner)
        {
            
        }

        void INetworkRunnerCallbacks.OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data)
        {
            throw new NotImplementedException();
        }
    }

}
