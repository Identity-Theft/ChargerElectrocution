using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace ChargerElectrocution.Patches
{
    public class ItemChargerElectrocution
    {
        public static void Load()
        {
            On.ItemCharger.ChargeItem += ItemCharger_ChargeItem;
            On.ItemCharger.Update += ItemCharger_Update;
            On.GameNetworkManager.Start += GameNetworkManager_Start;
        }
        
        private static void GameNetworkManager_Start(On.GameNetworkManager.orig_Start orig, GameNetworkManager self)
        {
            orig(self);

            IReadOnlyList<NetworkPrefab>? prefabs = self.GetComponent<NetworkManager>()?.NetworkConfig?.Prefabs?.Prefabs;
            if (prefabs == null) return;

            foreach (var prefabContainer in prefabs)
            {
                GameObject? prefab = prefabContainer?.Prefab;

                if (prefab?.GetComponent<GrabbableObject>()?.itemProperties?.isConductiveMetal != true) continue;

                prefab.AddComponent<MonoBehaviours.ItemChargerElectrocution>();
            }
        }


        private static void ItemCharger_Update(On.ItemCharger.orig_Update orig, ItemCharger self)
        {
            orig(self);
            if (self.updateInterval != 0f || GameNetworkManager.Instance == null || GameNetworkManager.Instance.localPlayerController == null) return;
            
            self.triggerScript.interactable = GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer != null && (GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer.itemProperties.requiresBattery || GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer.itemProperties.isConductiveMetal);
        }

        private static void ItemCharger_ChargeItem(On.ItemCharger.orig_ChargeItem orig, ItemCharger self)
        {
            GrabbableObject currentlyHeldObjectServer = GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer;
            if (currentlyHeldObjectServer == null)
            {
                return;
            }
            if (!currentlyHeldObjectServer.itemProperties.requiresBattery)
            {
                if (currentlyHeldObjectServer.itemProperties.isConductiveMetal)
                {
                    currentlyHeldObjectServer.GetComponent<MonoBehaviours.ItemChargerElectrocution>().Electrocute(self);
                }
                return;
            }
            
            orig(self);
        }
    }
}