using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Fusion;

namespace cryptokartz.Scripts.Car
{
    public class CarDataNetwork : NetworkBehaviour, INetworkStruct
    {
        public CarDataNetwork() { }

        
        [Networked] public int LevelId { get; set; }
        [Networked] public int ColorId { get; set; }
        [Networked, Capacity(14)]
        public string Vid { get; set; }

        public ChangeDetector changeDetector { get; private set; }

        public override void Spawned()
        {
            base.Spawned();

            changeDetector = GetChangeDetector(ChangeDetector.Source.SnapshotFrom);
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
                        case nameof(ColorId):
                            OnColorIdChanged();
                            break;
                        case nameof(LevelId):
                            OnLevelIdChanged();
                            break;
                        case nameof(Vid):
                            OnVidChanged();
                            break;
                    }

                }

            }

        }

        void OnColorIdChanged()
        {

        }

        void OnLevelIdChanged()
        {

        }

        void OnVidChanged()
        {

        }

    }
}
