using System;
using System.Runtime.InteropServices;
using Photon.Bolt;
using UnityEngine;

namespace Hyperspace.Entities
{
    public class PlayerShip : EntityBehaviour<IPlayerShipState>
    {
        private CraftConfig _initialConfig;
        private ProjectileBase _projectile;
        private Generator _generator; 
        public Vector3 Velocity => _generator.Velocity;
        public Vector3 Position => _generator.Position;

        private Vector3 lastPosition = Vector3.zero;
        
        public override void Initialized()
        {
            base.Initialized();
        }

        private void Awake()
        {
            _projectile = new ProjectileBase();
            _generator = new Generator(this, new CraftState(transform.position));
        }

        public override void Attached()
        {
            //Bind the transform to the state transform
            _initialConfig = (CraftConfig)entity.AttachToken;
            state.CraftData.Energy = _initialConfig.Energy;
            
            state.SetTransforms(state.Transform, transform);
            state.OnFire += OnFire;
        }

        private void Update()
        {
            Debug.Log($"PositionDifference: {lastPosition - Position}");
            lastPosition = Position;
        }

        private void FixedUpdate()
        {
            _generator.FixedUpdate();
        }

        public void OnDestroy()
        {
            state.OnFire -= OnFire;
        }
        
        public override void SimulateController()
        {
            ICraftCommandInput input = CraftCommand.Create();
            input = Engine.InputManager.GetInputState(input);
            entity.QueueInput(input);

            if (_generator.CollisionDetected)
            {
                Debug.Log($"Simulated.CollisionDetected");
                Physics.DetectCollision(Position, Velocity, out RaycastHit hit);
                ICollisionCommandInput collisionInput = CollisionCommand.Create();
                collisionInput.HitNormal = hit.normal;
                collisionInput.Velocity = Velocity;
                collisionInput.Position = Position;
                entity.QueueInput(collisionInput);
            }
            
            if(state.CraftData.Energy < 100)
                state.CraftData.Energy += 1;
        }

        public override void ExecuteCommand(Command command, bool resetState)
        {
            switch (command)
            {
                case CraftCommand cmd when resetState:
                    _generator.SetCraftState(cmd.Result.Position, cmd.Result.Velocity, cmd.Result.Acceleration);
                    break;
                case CraftCommand cmd:
                {
                    CraftState craftState = _generator.ApplyForce(cmd.Input.Controller, cmd.Input.Thrust);
                    cmd.Result.Position = craftState.Position;
                    cmd.Result.Velocity = craftState.Velocity;
                    cmd.Result.Acceleration = craftState.Acceleration;
                    
                    if (cmd.IsFirstExecution)
                    {
                        if(cmd.Input.LightFire)
                            FireProjectile(cmd);
                    }

                    break;
                }
                case CollisionCommand ccmd when resetState:
                    _generator.SetCraftState(ccmd.Result.CollisionPosition, ccmd.Result.CollisionVelocity, ccmd.Result.CollisionAcceleration);
                    Debug.Log($"{BoltNetwork.IsServer} : Collision Reset {ccmd.Result.CollisionVelocity}");
                    break;
                case CollisionCommand ccmd:
                {
                    Vector3 collisionVelocity = Physics.ProcessCollision(ccmd.Input.HitNormal, ccmd.Input.Velocity);
                    CraftState craftState = _generator.ApplyCollision(ccmd.Input.Position, collisionVelocity, ccmd.Input.Acceleration);
                    ccmd.Result.CollisionPosition = craftState.Position;
                    ccmd.Result.CollisionVelocity = craftState.Velocity;
                    ccmd.Result.CollisionAcceleration = craftState.Acceleration;
                    Debug.Log($"{BoltNetwork.IsServer} : Collision Command {ccmd.Result.CollisionVelocity}");
                    break;
                }
            }
        }

        public void FireProjectile(CraftCommand command)
        {
            if (
                _projectile.FireFrame + _projectile.Cooldown <= BoltNetwork.ServerFrame 
                && state.CraftData.Energy - _projectile.Cost > 0
                )
            {
                var fireFrame = BoltNetwork.ServerFrame;
                state.CraftData.Energy -= _projectile.Cost;
                state.Fire();

                if (entity.IsOwner)
                {
                    _projectile.OnOwner(command, entity, _generator.GetCraftState());
                }
            }
        }
        
        private void OnFire()
        {
            _projectile.OnClient(entity, _generator.GetCraftState());
        }
    }

    internal sealed class Generator
    {
        private const float SPEED_MULTIPLIER = 30;
        private const float VELOCITY_CAP = 5;
        private EntityBehaviour<IPlayerShipState> _parent;
        private Rigidbody _rigidbody;
        private CraftState _state;
        private bool _collisionDetected;

        public BoltEntity Entity => _parent.entity;
        public Vector3 Velocity
        {
            get => _state.Velocity;
            internal set => _state.Velocity = value;
        }
        public Vector3 Acceleration
        {
            get => _state.Acceleration;
            private set => _state.Acceleration = value;
        }
        public Vector3 Position
        {
            get => _state.Position;
            set
            {
                if(Vector3.Dot(Position.normalized, value.normalized) > 0)
                    _state.Position = value;
            }
        }

        public bool CollisionDetected => _collisionDetected;

        public Generator(EntityBehaviour<IPlayerShipState> parent, CraftState state)
        {
            _parent = parent;
            _state = state;
            
            _rigidbody = parent.gameObject.GetComponent<Rigidbody>();
            
            if (_rigidbody == null)
                _rigidbody = parent.gameObject.AddComponent<Rigidbody>();

            _rigidbody.isKinematic = true;
            parent.transform.position = _state.Position;
        }

        public CraftState ApplyForce(Vector3 inputAxis, bool inputThrust)
        {
            Vector3 directionToMouse = (inputAxis - Position).normalized;

            if (inputThrust)
            {
                Acceleration = directionToMouse * BoltNetwork.FrameDeltaTime * SPEED_MULTIPLIER;
                Acceleration = Vector3.ClampMagnitude(Acceleration, 2f);
                Velocity += Acceleration * BoltNetwork.FrameDeltaTime;
                Velocity = Vector3.ClampMagnitude(Velocity, 5);
            }

            return _state;
        }

        public void SetCraftState(Vector3 resultPosition, Vector3 resultVelocity, Vector3 resultAcceleration)
        {
            Acceleration += resultAcceleration - Acceleration;
            Velocity += resultVelocity - Velocity;
            Position += resultPosition - Position;
        }

        public CraftState GetCraftState()
        {
            return _state;
        }

        public void FixedUpdate()
        {
            if(Entity.IsAttached == false || Entity.IsControllerOrOwner == false)
                return;

            if (_rigidbody.velocity.magnitude > 5)
            {
                Vector3 velocityScaled = _rigidbody.velocity.normalized * VELOCITY_CAP;
                _rigidbody.velocity = velocityScaled;
            }
            
            _collisionDetected = Physics.DetectCollision(Position, Velocity);
            
            Position += Velocity;
            _rigidbody.MovePosition(Position);
        }

        public CraftState ApplyCollision(Vector3 inputPosition, Vector3 collisionVelocity, Vector3 inputAcceleration)
        {
            //Position += inputPosition - Position;
            Velocity += collisionVelocity - Velocity;
            Acceleration += inputAcceleration - Acceleration;
            return _state;
        }
    }
}