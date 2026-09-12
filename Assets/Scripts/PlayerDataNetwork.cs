using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Fusion;

namespace cryptokartz.Scripts.Player
{
    public class PlayerDataNetwork : NetworkBehaviour, INetworkStruct
    {
        public PlayerDataNetwork() { }

        [Networked]
        public bool spawnedAvatar { get; set; } = false;

        [Networked] public int PlayerId { get; set; }
        [Networked] public int AvatarIndex { get; set; }
        [Networked, Capacity(14)]
        public string PlayerTag { get; set; }

        public ChangeDetector changeDetector { get; private set; }

        public override void Spawned()
        {
            base.Spawned();

            changeDetector = GetChangeDetector(ChangeDetector.Source.SnapshotFrom);
            spawnedAvatar = !spawnedAvatar;
        }


        public override void Render()
        {
            base.Render();

            if (changeDetector != null && Object.HasStateAuthority)
            {
                foreach (var change in changeDetector.DetectChanges(this))
                {

                    switch (change)
                    {
                        case nameof(spawnedAvatar):
                            OnAvatarIndexChanged();
                            OnPlayerIdChanged();
                            break;
                    }

                }

            }

        }

        void OnAvatarIndexChanged()
        {

        }

        void OnPlayerIdChanged()
        {

        }

    }
}
