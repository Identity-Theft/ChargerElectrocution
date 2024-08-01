using System.Collections;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace ChargerElectrocution.MonoBehaviours
{
    public class ItemChargerElectrocution : NetworkBehaviour
    {
        private Coroutine? _electrocutionCoroutine;
        public void Electrocute(ItemCharger socket)
        {
            Debug.Log("Attempting electrocution");
            if (_electrocutionCoroutine != null)
            {
                StopCoroutine(_electrocutionCoroutine);
            }
            _electrocutionCoroutine = StartCoroutine(ElectrocutionDelayed(socket));
        }

        public AudioSource? strikeAudio;
        public ParticleSystem? strikeParticle;

        public void Awake()
        {
            var stormyWeather = FindObjectOfType<StormyWeather>(true);
            GameObject audioSource = stormyWeather.targetedStrikeAudio.gameObject;
                
            // Copy GameObject and add to this object as a child
            strikeAudio = Instantiate(audioSource, transform).GetComponent<AudioSource>();
            strikeAudio.transform.localPosition = Vector3.zero;
            strikeAudio.gameObject.SetActive(true);
                
            strikeParticle = Instantiate(stormyWeather.explosionEffectParticle.gameObject, transform).GetComponent<ParticleSystem>();
            strikeParticle.transform.localPosition = Vector3.zero;
            strikeParticle.gameObject.SetActive(true);
        }

        private IEnumerator ElectrocutionDelayed(ItemCharger socket)
        {
            ChargerElectrocution.Logger.LogInfo("Electrocution started");
            
            socket.zapAudio.Play();
            yield return new WaitForSeconds(0.75f);
            socket.chargeStationAnimator.SetTrigger("zap");

            if (!NetworkObject.IsOwner) yield break;
                
            if (!NetworkObject.IsOwnedByServer)
            {
                ElectrocutedServerRpc(transform.position);
            }
            else
            {
                ElectrocutedClientRpc(transform.position);
                Electrocuted(transform.position);
            }
        }

        void Electrocuted(Vector3 position)
        {
            var stormyWeather = FindObjectOfType<StormyWeather>(true);

            CreateExplosion(position);

            strikeParticle?.Play();
            stormyWeather.PlayThunderEffects(position, strikeAudio);
        }
            
        private void CreateExplosion(Vector3 explosionPosition, bool spawnExplosionEffect = false)
        {
            float explosionRange = 5f;
            
            ChargerElectrocution.Logger.LogInfo("Spawning explosion at pos: {explosionPosition}");

            if (spawnExplosionEffect)
            {
                Instantiate(StartOfRound.Instance.explosionPrefab, explosionPosition, Quaternion.Euler(-90f, 0f, 0f), RoundManager.Instance?.mapPropsContainer?.transform).SetActive(value: true);
            }

            float localPlayerDist = Vector3.Distance(GameNetworkManager.Instance.localPlayerController.transform.position, explosionPosition);
                
            switch (localPlayerDist)
            {
                case < 14f:
                    HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
                    break;
                case < 25f:
                    HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
                    break;
            }

            foreach (var collider in Physics.OverlapSphere(explosionPosition, explosionRange, 2621448, QueryTriggerInteraction.Collide))
            {
                float colliderDist = Vector3.Distance(explosionPosition, collider.transform.position);
                    
                if (colliderDist > 4f && Physics.Linecast(explosionPosition, collider.transform.position + Vector3.up * 0.3f, 256, QueryTriggerInteraction.Ignore)) continue;

                if (collider.gameObject.layer != 3) continue;
                
                var playerControllerB = collider.gameObject.GetComponent<PlayerControllerB>();
                if (!playerControllerB || !playerControllerB.IsOwner) continue;
                
                // Calculate damage based on distance from explosion
                playerControllerB.DamagePlayer((int)(20 * (1f - Mathf.Clamp01(colliderDist / explosionRange))), causeOfDeath: CauseOfDeath.Electrocution);
                break;
            }
                
            foreach (var collider in Physics.OverlapSphere(explosionPosition, 10f, ~LayerMask.GetMask("Colliders")))
            {
                Rigidbody component = collider.GetComponent<Rigidbody>();
                if (!component) continue;
                        
                component.AddExplosionForce(70f, explosionPosition, 10f);
            }
        }

        [ServerRpc]
        void ElectrocutedServerRpc(Vector3 position)
        {
            ElectrocutedClientRpc(position);
        }
            
        [ClientRpc]
        void ElectrocutedClientRpc(Vector3 position)
        {
            Electrocuted(position);
        }
    }   
}

