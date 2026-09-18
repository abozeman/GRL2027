using Fusion;
using System;
using System.Collections.Generic;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.GRL.Scripts.Player
{
    public class PlayerInputProvider : NetworkBehaviour, INetworkRunnerCallbacks, IBeforeUpdate
    {

        public struct CarInput : INetworkInput
        {
            public Vector2 carControlValue;
        }

        // Local variable to store the input polled.
        CarInput carInput = new CarInput();
        public InputActionReference ERLLeftStick;
        public InputActionReference ERLRightStick;
        private Vector2 carControlValue;
        

        public override void Spawned()
        {
            Object.Runner.AddCallbacks(this);
        }

        public void BeforeUpdate()
        {

            carControlValue = ERLLeftStick.action.ReadValue<Vector2>();
            carControlValue = ERLRightStick.action.ReadValue<Vector2>();

            if(carControlValue.x != 0 || carControlValue.y != 0) Debug.Log($"echoCarControlValue (x,y): ({carControlValue.x},{carControlValue.y}) ");

            carInput.carControlValue = carControlValue;
        }

        void INetworkRunnerCallbacks.OnInput(NetworkRunner runner, NetworkInput input)
        {

            var pRef = Object.Runner.LocalPlayer;
            var authority = Object.HasStateAuthority;

            //Debug.Log($"PlayerRef / HasInputAuthority: {pRef} ({authority}) ");

            if (Object.HasInputAuthority)
            {
                input.Set(carInput);

                var carOutput = new CarInput();
                input.TryGet(out carOutput);
                //Debug.Log($"carOutput (x,y): ({carOutput.carControlValue.x},{carOutput.carControlValue.y}) ");
                //carInput = default;
            }

        }

        #region Unused Callbacks

        void INetworkRunnerCallbacks.OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
            
        }

        void INetworkRunnerCallbacks.OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
            
        }

        void INetworkRunnerCallbacks.OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            
        }

        void INetworkRunnerCallbacks.OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            
        }

        void INetworkRunnerCallbacks.OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
        {
            
        }

        void INetworkRunnerCallbacks.OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            
        }

        void INetworkRunnerCallbacks.OnConnectedToServer(NetworkRunner runner)
        {
            
        }

        void INetworkRunnerCallbacks.OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            
        }

        void INetworkRunnerCallbacks.OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
        {
            
        }

        void INetworkRunnerCallbacks.OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {
            
        }

        void INetworkRunnerCallbacks.OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
        {
            
        }

        void INetworkRunnerCallbacks.OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {
            
        }

        void INetworkRunnerCallbacks.OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
        {
            
        }

        void INetworkRunnerCallbacks.OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
        {
            
        }

        //void INetworkRunnerCallbacks.OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
        //{
            
        //}

        void INetworkRunnerCallbacks.OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
        {
            
        }

        void INetworkRunnerCallbacks.OnSceneLoadDone(NetworkRunner runner)
        {
            
        }

        void INetworkRunnerCallbacks.OnSceneLoadStart(NetworkRunner runner)
        {
            
        }

        void INetworkRunnerCallbacks.OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
