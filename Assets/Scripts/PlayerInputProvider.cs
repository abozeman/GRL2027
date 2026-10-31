using Fusion;
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

        private CarInput _carInput = new CarInput();
        public InputActionReference ERLLeftStick;
        public InputActionReference ERLRightStick;

        public override void Spawned()
        {
            // Only add callbacks if this specific client owns this object
            if (Object.HasInputAuthority)
            {
                Runner.AddCallbacks(this);
            }
        }

        public void BeforeUpdate()
        {
            if (!Object.HasInputAuthority) return;

            Vector2 inputVector = Vector2.zero;

            // 1. Check XR Controllers / Gamepad
            if (ERLLeftStick != null && ERLLeftStick.action != null)
            {
                inputVector = ERLLeftStick.action.ReadValue<Vector2>();
            }

            // If left stick is idle, check right stick (fixes the overwrite bug)
            if (inputVector == Vector2.zero && ERLRightStick != null && ERLRightStick.action != null)
            {
                inputVector = ERLRightStick.action.ReadValue<Vector2>();
            }

            // 2. WebXR / Browser Desktop Fallback (WASD or Arrows)
            if (Keyboard.current != null)
            {
                float x = 0;
                float y = 0;

                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) y = 1;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) y = -1;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x = -1;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x = 1;

                if (x != 0 || y != 0)
                {
                    inputVector = new Vector2(x, y);
                }
            }

            _carInput.carControlValue = inputVector;
        }

        void INetworkRunnerCallbacks.OnInput(NetworkRunner runner, NetworkInput input)
        {
            if (Object.HasInputAuthority)
            {
                // Pack the struct. Fusion will automatically send this over WebSockets 
                // to the Windows server's FixedUpdateNetwork loop.
                input.Set(_carInput);
            }
        }

        #region Unused Callbacks
        // (Keep all your existing empty INetworkRunnerCallbacks here to satisfy the interface)
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
        public void OnConnectedToServer(NetworkRunner runner) { }
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnSessionListUpdated(NetworkRunner runner, System.Collections.Generic.List<SessionInfo> sessionList) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, System.Collections.Generic.Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
        void INetworkRunnerCallbacks.OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ReadOnlySpan<byte> data) { }
        #endregion
    }
}