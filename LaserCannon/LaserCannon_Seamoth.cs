﻿using Common.Modules;
using UnityEngine;
using UWE;

namespace LaserCannon
{
    public class LaserCannon_Seamoth : MonoBehaviour
    {
        [AssertNotNull]
        private SeaMoth seamoth;
        [AssertNotNull]
        private EnergyMixin energyMixin; 
        [AssertNotNull]
        private FMODAsset shootSound;
        [AssertNotNull]
        private FMOD_CustomLoopingEmitter loopingEmitter;
        [AssertNotNull]
        private GameObject laserBeam;
        [AssertNotNull]
        private LineRenderer lineRenderer;

        private const float powerConsumption = 0.01f;
        private const float laserDamage = 15f;
        private const float maxLaserDistance = 70f;

        private float idleTimer = 3f;
        private bool toggle;
        public int slotID;
        private bool shoot = false;
        private bool repeat;
        private bool laserCannonEnabled;
        private float targetDist;
        private Vector3[] beamPositions = new Vector3[3];        
        private GameObject targetGameobject;        

        public void Awake()
        {
            seamoth = gameObject.GetComponent<SeaMoth>();
        }

        private void Start()
        {            
            energyMixin = seamoth.GetComponent<EnergyMixin>();
            var repulsioncannon = Resources.Load<GameObject>("WorldEntities/Tools/RepulsionCannon").GetComponent<RepulsionCannon>();
            shootSound = Instantiate(repulsioncannon.shootSound, seamoth.transform);
            
            loopingEmitter = gameObject.AddComponent<FMOD_CustomLoopingEmitter>();
            loopingEmitter.asset = shootSound;           

            GameObject powerRelayPrefab = CraftData.InstantiateFromPrefab(TechType.PowerTransmitter, false);
            PowerFX powerFX = powerRelayPrefab.GetComponentInChildren<PowerFX>();

            laserBeam = Instantiate(powerFX.vfxPrefab, seamoth.transform);
            laserBeam.SetActive(false);

            Destroy(powerRelayPrefab);            
            
            lineRenderer = laserBeam.GetComponent<LineRenderer>();
            lineRenderer.startWidth = 0.4f;
            lineRenderer.endWidth = 0.4f;

            seamoth.onToggle += OnToggle;
            Utils.GetLocalPlayerComp().playerModeChanged.AddHandler(gameObject, new Event<Player.Mode>.HandleFunction(OnPlayerModeChanged));            
        }

        private void OnPlayerModeChanged(Player.Mode playerMode)
        {
            if (playerMode == Player.Mode.LockedPiloting)
            {
                OnEnable();
            }
            else
            {
                OnDisable();
            }
        }

        private void OnToggle(int slotID, bool state)
        {
            if (seamoth.GetSlotBinding(slotID) == LaserCannon.TechTypeID)
            {
                toggle = state;

                if (state)
                {                    
                    OnEnable();
                }
                else
                {                    
                    OnDisable();
                }
            }
        }

        public void OnEnable()
        {            
            laserCannonEnabled = Player.main.inSeamoth && toggle;
        }

        public void OnDisable()
        {            
            laserBeam.SetActive(false);
            loopingEmitter.Stop();
            laserCannonEnabled = false;
            shoot = false;
            repeat = false;
            Modules.SetInteractColor(Modules.Colors.White);
        }

        private void AddDamage(GameObject gameObject)
        {
            Vector3 position = gameObject.transform.position;           

            if (gameObject != null)
            {
                LiveMixin liveMixin = gameObject.GetComponent<LiveMixin>();
                if (!liveMixin)
                {                    
                    liveMixin = Utils.FindEnabledAncestorWithComponent<LiveMixin>(gameObject);
                }
                if (liveMixin)
                {                    
                    if (liveMixin.IsAlive())
                    {
                        liveMixin.TakeDamage(laserDamage, position, DamageType.Explosive, null);
                    }
                }                
            }
        }

        private Vector3 CalculateLaserBeam()
        {
            Camera camera = MainCamera.camera;
            Transform transform = camera.transform;
            Vector3 position = transform.position;
            Vector3 forward = transform.forward;

            Targeting.GetTarget(seamoth.gameObject, maxLaserDistance, out targetGameobject, out targetDist);

            if (targetDist == 0f)
            {
                return position + maxLaserDistance * forward;
            }
            else
            {
                AddDamage(targetGameobject);
                return position + targetDist * forward;
            }
        }
       
        public void Update()
        {
            if (laserCannonEnabled)
            { 
                if (energyMixin.charge < energyMixin.capacity * 0.1f)
                {
                    if (idleTimer > 0f)
                    {
                        toggle = false;
                        shoot = false;
                        repeat = false;
                        idleTimer = Mathf.Max(0f, idleTimer - Time.deltaTime);
                        HandReticle.main.SetInteractText("Warning!\nLow Power!", "Laser Cannon Disabled!", false, false, HandReticle.Hand.None);
                        Modules.SetInteractColor(Modules.Colors.Red);
                    }
                    else
                    {
                        idleTimer = 3;
                        seamoth.SlotKeyDown(slotID);
                    }
                }

                if (toggle)
                {
                    if (GameInput.GetButtonDown(GameInput.Button.LeftHand))
                    {
                        shoot = true;
                    }
                    if (GameInput.GetButtonUp(GameInput.Button.LeftHand))
                    {
                        shoot = false;
                        laserBeam.SetActive(false);
                    }
                    if (shoot)
                    {
                        RepeatCycle(Time.time, 0.4f);
                    }
                }
            }                   
        }
        
        public void LateUpdate()
        {
            if (laserCannonEnabled)
            {
                if (shoot && repeat)
                {
                    beamPositions[0] = seamoth.torpedoTubeLeft.transform.position;                    
                    beamPositions[1] = CalculateLaserBeam();
                    beamPositions[2] = seamoth.torpedoTubeRight.transform.position;
                    lineRenderer.positionCount = beamPositions.Length;
                    lineRenderer.SetPositions(beamPositions);
                    loopingEmitter.Play();
                    laserBeam.SetActive(true);                                        
                    WorldForces.AddExplosion(beamPositions[1], DayNightCycle.main.timePassed, 5f, 5f);
                    energyMixin.ConsumeEnergy(powerConsumption);
                }
                else
                {
                    loopingEmitter.Stop();
                    laserBeam.SetActive(false);
                }
            }           
        }
        
        private void RepeatCycle(float value, float length)
        {
            float x = Mathf.Repeat(value, length);
           
            if (x < 0.3f && x > 0f)
                repeat = true;
            else
                repeat = false;
        }        
    }
}
