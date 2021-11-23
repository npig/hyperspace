using System;
using System.Runtime.InteropServices;
using ImGuiNET;
using Photon.Bolt;
using UnityEngine;
using UnityEngine.UIElements;

namespace Hyperspace.Entities
{
    public class PlayerShip : EntityBehaviour<IPlayerShipState>
    {
        private CraftConfig _initialConfig;
        private ProjectileBase _projectile;
        private Generator _generator; 
        public Vector3 Velocity => _generator.Velocity;
        public Vector3 Position => _generator.Position;
        
        public override void Initialized()
        {
            base.Initialized();
            // Hyperspace.Engine.Events.Emit<OnPlayerInitialisedState>(ObjectPool.Get<OnPlayerInitialisedState>().Init(this));
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
                Physics.DetectCollision(Position, Velocity, out RaycastHit hit);
                ICollisionCommandInput collisionInput = CollisionCommand.Create();
                collisionInput.CollisionDetected = true;
                collisionInput.HitNormal = hit.normal;
                collisionInput.Velocity = _generator.Velocity;
                entity.QueueInput(collisionInput);
            }

            if(state.CraftData.Energy < 100)
                state.CraftData.Energy += 1;
        }

        public override void ExecuteCommand(Command command, bool resetState)
        {
            if (command is CraftCommand cmd)
            {
                if (resetState)
                {
                    _generator.SetCraftState(cmd);
                }
                else
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
                }
            }
            else if (command is CollisionCommand ccmd)
            {
                if (resetState)
                {
                    _generator.Velocity = ccmd.Result.CollisionVelocity;
                }
                else
                {
                    Vector3 collisionVelocity = Physics.ProcessCollision(ccmd.Input.HitNormal, ccmd.Input.Velocity);
                    ccmd.Result.CollisionVelocity = collisionVelocity;
                    _generator.Velocity = collisionVelocity;
                }
            }

        }

        /*private void OnCollision(RaycastHit hit)
        {
            float dot = Vector3.Dot(hit.normal, _state.Velocity.normalized);
            Vector3 normalProjection = 2 * dot * hit.normal;
            Vector3 reflection = _state.Velocity.normalized - normalProjection;
            Vector3 result = reflection * (_state.Velocity.magnitude * .8f);
            _state.Velocity = result;
        }*/

        /*private void SetCraftState(Vector3 resultPosition, Vector3 resultVelocity, Vector3 resultAcceleration)
        {
            _state.Position = resultPosition;
            _state.Velocity = resultVelocity;
            _state.Acceleration = resultAcceleration;
            transform.position = (_state.Position - transform.position);
        }*/
        
        /*private void FireProjectile(CraftCommand command)
        {
            if (_projectile.fireFrame + _projectile.cooldown <= BoltNetwork.ServerFrame && state.CraftData.Energy - _projectile.cost > 0)
            {
                _projectile.fireFrame = BoltNetwork.ServerFrame;
                state.CraftData.Energy -= _projectile.cost;
                state.Fire();

                if (entity.IsOwner)
                {
                    _projectile.OnOwner(command, entity, _state);
                }
            }
        }*/
        
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
        //private CollisionData _collisionData;
        private EntityBehaviour<IPlayerShipState> _parent;
        private Rigidbody _rigidbody;
        private CraftState _state;

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
            private set => _state.Position = value;
        }

        public bool CollisionDetected
        {
            get => _state.CollisionDetected;
            set => _state.CollisionDetected = value;
        }


        public Generator(EntityBehaviour<IPlayerShipState> parent, CraftState state)
        {
            _parent = parent;
            _state = state;
            
            _rigidbody = parent.gameObject.GetComponent<Rigidbody>();
            
            if (_rigidbody == null)
                _rigidbody = parent.gameObject.AddComponent<Rigidbody>();

            _rigidbody.isKinematic = true;
            parent.transform.position = _state.Position;
            //Freeze Rotation and Position
            //Set Drag
        }

        public CraftState ApplyForce(Vector3 inputAxis, bool inputThrust)
        {
            Vector3 directionToMouse = (inputAxis - Position).normalized;
            Quaternion q = Quaternion.LookRotation(directionToMouse, Vector3.up);
            _parent.transform.localRotation = q;
            
            if (inputThrust)
            {
                //change to input direction
                Acceleration = directionToMouse * BoltNetwork.FrameDeltaTime * 30;
                Acceleration = Vector3.ClampMagnitude(Acceleration, 2f);
                Velocity += Acceleration * BoltNetwork.FrameDeltaTime;
            }
            
            Vector3.ClampMagnitude(Velocity, 15);
            _rigidbody.MovePosition(Position + Velocity);
            Position = _rigidbody.position;
            return _state;
        }
        
        public void SetCraftState(CraftCommand cmd)
        {
            /*Vector3 resultVelocity = cmd.Result.Velocity;
            
            if (cmd.Result.Collision)
            {
                Debug.Log($"$$ Set State $$");
                Physics.DetectCollision(cmd.Result.Position, cmd.Result.Velocity, out var hit);
                resultVelocity = Physics.ProcessCollision(hit, cmd.Result.Velocity);
            }*/
            
            SetCraftState(cmd.Result.Position, cmd.Result.Velocity, cmd.Result.Acceleration);
        }
        
        private void SetCraftState(Vector3 resultPosition, Vector3 resultVelocity, Vector3 resultAcceleration)
        {
            Position += (resultPosition - Position);
            Velocity += resultVelocity - Velocity;
            Acceleration = resultAcceleration;
            _rigidbody.MovePosition(Position + Velocity);
        }

        public CraftState GetCraftState()
        {
            return _state;
        }

        public void FixedUpdate()
        {
            if(Entity.IsAttached == false || Entity.IsControllerOrOwner == false)
                return; 
            
            _state.CollisionDetected = Physics.DetectCollision(Position, Velocity);
        }
    }
}